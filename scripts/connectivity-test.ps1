param(
    [Parameter(Mandatory)][string]$AuthorizedSource,
    [string]$AppPath = 'apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.dll',
    [ValidateRange(60,900)][int]$TimeoutSeconds = 600
)
. "$PSScriptRoot/common.ps1"
$app = [IO.Path]::GetFullPath((Join-Path $RepoRoot $AppPath))
$source = [IO.Path]::GetFullPath($AuthorizedSource)
if (-not (Test-Path -LiteralPath $source)) { throw 'Supply an authorized encrypted test library directory or a private text file containing its subscription URL.' }
if (-not (Test-Path -LiteralPath $app)) { throw 'Build the requested app first.' }
$output = Join-Path $RepoRoot ('artifacts/connectivity-' + [Guid]::NewGuid().ToString('N'))
# No transcript, raw core output or source contents. This mode never changes Windows proxy settings.
$process = Start-Process -FilePath $Dotnet -ArgumentList @('"'+$app+'"', '--production-connectivity-test', '"'+$source+'"', '"'+$output+'"') -WindowStyle Hidden -PassThru
for ($elapsed = 0; $elapsed -lt $TimeoutSeconds; $elapsed++) { if ($process.WaitForExit(1000)) { break } }
if (-not $process.HasExited) { $process.Kill($true); throw 'Connectivity test exceeded its bound; only this harness and owned children were stopped.' }
"Sanitized evidence: $output/result.json"
if ($process.ExitCode -ne 0) { throw "Required connectivity checks failed (exit $($process.ExitCode)); inspect sanitized evidence. Unsupported IPv6 conditions are reported separately." }
