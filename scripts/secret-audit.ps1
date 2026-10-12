param([string]$Output = 'artifacts/phase3-secret-audit.json', [switch]$FailOnUnreviewed)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    $findings = [System.Collections.Generic.List[object]]::new()
    $patterns = @{
        AccessToken = 'gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|AKIA[A-Z0-9]{16}|(?:sk-|sk-proj-)[A-Za-z0-9_-]{24,}'
        PrivateKey = '-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----'
        CredentialUrl = 'https?://[^\s/:]+:[^\s/@]+@'
        SecretAssignment = '(?i)(?:api[_-]?key|access[_-]?token|client[_-]?secret|password)\s*[=:]\s*["'']?[A-Za-z0-9_+/=-]{24,}'
        Subscription = '(?i)https?://[^\s"''<>]+(?:[?&](?:token|key|auth)=)[A-Za-z0-9_-]{16,}'
        OpaqueUrlPath = '(?i)https?://[^\s"''<>]+/[A-Za-z0-9_-]{16,}(?:[\s"''<>]|$)'
    }
    function Inspect-Text([string]$text, [string]$location) {
        foreach ($rule in $patterns.Keys) {
            foreach ($match in [regex]::Matches($text, $patterns[$rule])) {
                # Report references only. Never echo a matched value.
                $findings.Add([pscustomobject]@{ location=$location; rule=$rule; line=1+([regex]::Matches($text.Substring(0,$match.Index),"`n")).Count })
            }
        }
        foreach ($match in [regex]::Matches($text, '(?i)(?:vless|trojan|ss)://[^\s"''<>]+|vmess://[A-Za-z0-9+/=_-]{40,}')) {
            $fixture = $match.Value.Contains('example.invalid') -or $match.Value.Contains('offline-') -or $match.Value.Contains('fixture')
            if ($match.Value.StartsWith('vmess://')) {
                try { $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($match.Value.Substring(8))); $fixture = $decoded.Contains('example.invalid') } catch { }
            }
            if (-not $fixture) { $findings.Add([pscustomobject]@{location=$location;rule='ShareLinkNeedsReview';line=1+([regex]::Matches($text.Substring(0,$match.Index),"`n")).Count}) }
        }
    }
    $extensions = '\.(cs|csproj|props|targets|slnx|json|jsonl|xml|xaml|config|md|txt|ps1|yml|yaml|toml|ini|env|pem|key|sh|cmd|bat|svg|resx|resw|html|css|js|ts|csv|tsv|sql|http|proto)$|(^|/)(\.env[^/]*|\.gitignore|\.gitmodules|\.editorconfig|LICENSE)$'
    $files = @(git ls-files --cached --others --exclude-standard | Where-Object { $_ -notlike 'external/*' -and $_ -match $extensions -and (Test-Path -LiteralPath $_ -PathType Leaf) })
    foreach ($file in $files) { Inspect-Text ([IO.File]::ReadAllText((Join-Path $repo $file))) "working:$file" }
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($entry in @(git rev-list --objects --all)) {
        $parts = $entry.Split(' ',2)
        if ($parts.Length -ne 2 -or $parts[1] -like 'external/*' -or $parts[1] -notmatch $extensions -or -not $seen.Add($parts[0])) { continue }
        if ((git cat-file -t $parts[0]) -ne 'blob') { continue }
        $content = (git cat-file blob $parts[0]) -join "`n"
        if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect historical blob.' }
        Inspect-Text $content "history:$($parts[0]):$($parts[1])"
    }
    Inspect-Text ((git log --all --format='%B') -join "`n") 'commit-messages'
    Inspect-Text ((git config --get-regexp '^remote\..*\.url$') -join "`n") 'remote-urls'
    $report = [pscustomobject]@{
        workingTextFiles=$files.Count; historicalTextBlobs=$seen.Count; findings=$findings.ToArray()
        limitations=@('Pattern-based audit is not proof of absence of all secrets','Ignored encrypted libraries and user data excluded','Binary images/assets and upstream repository history excluded','Every finding requires review; matched values are suppressed')
    }
    $destination = Join-Path $repo $Output
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    [IO.File]::WriteAllText($destination, ($report | ConvertTo-Json -Depth 6))
    if ($FailOnUnreviewed) {
        $reviewed = Get-Content (Join-Path $repo 'scripts/secret-audit-reviewed.json') | ConvertFrom-Json
        foreach ($finding in $findings) {
            $path = if ($finding.location.StartsWith('working:')) { $finding.location.Substring(8) } else { $finding.location.Split(':',3)[2] }
            $content = if ($finding.location.StartsWith('working:')) { [IO.File]::ReadAllText((Join-Path $repo $path)) } else { (git cat-file blob $finding.location.Split(':',3)[1]) -join "`n" }
            $line = ($content -split '\r?\n')[$finding.line - 1]
            $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($line)))
            if (-not @($reviewed | Where-Object { $_.path -eq $path -and $_.rule -eq $finding.rule -and $_.lineHash -eq $digest }).Count) { throw 'Unreviewed secret-audit reference; inspect the value-suppressed report before publication.' }
        }
    }
    "Scanned $($files.Count) current text files and $($seen.Count) historical text blobs; $($findings.Count) references need review. Values suppressed."
} finally { Pop-Location }
