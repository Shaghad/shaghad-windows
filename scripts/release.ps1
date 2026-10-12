param([switch]$SkipValidation, [switch]$BuildInstaller)
. "$PSScriptRoot/common.ps1"
[xml]$versionDocument = Get-Content (Join-Path $RepoRoot 'Version.props')
$version = [string]$versionDocument.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+-beta\.\d+$') { throw 'Expected beta SemVer.' }
$release = Join-Path $RepoRoot "artifacts/releases/v$version"
$publish = Join-Path $release 'portable'
if (Test-Path $release) { throw 'Release directory already exists. Use a fresh version or explicitly archive the previous artifacts.' }
New-Item -ItemType Directory -Path $publish -Force | Out-Null
Push-Location $RepoRoot
try {
    if (-not $SkipValidation) { & "$PSScriptRoot/build.ps1"; & "$PSScriptRoot/test.ps1" }
    & "$PSScriptRoot/install-cores.ps1"
    Invoke-Dotnet publish apps/windows/AJ.VPN.Desktop -c Release -r win-x64 --self-contained true -o $publish
    foreach ($entry in (Get-Content src/AJ.VPN.Infrastructure/core-manifest.json | ConvertFrom-Json)) {
        $binary = Join-Path $publish "cores/$($entry.RelativePath)"
        if ((Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -ne $entry.Sha256) { throw 'Packaged core hash mismatch.' }
    }
    Copy-Item LICENSE $publish
    New-Item -ItemType Directory -Path (Join-Path $publish 'legal') | Out-Null
    Copy-Item docs/legal/* (Join-Path $publish 'legal')
    Copy-Item branding/fonts/OFL.txt (Join-Path $publish 'legal/Vazirmatn-OFL.txt')
    Copy-Item branding/fonts/AUTHORS.txt (Join-Path $publish 'legal/Vazirmatn-AUTHORS.txt')
    Copy-Item Version.props (Join-Path $publish 'VERSION.props')
    Copy-Item "docs/releases/v$version.md" $release
    "SHAGHAD $version — Unsigned Development Pre-release. Windows x64, self-contained .NET 10. No TUN or Kill Switch. System Proxy requires approval. User data lives in LocalAppData/VPN-AJ and is preserved on upgrade. See release notes and legal notices." | Set-Content (Join-Path $publish 'README.txt')
    $forbidden = Get-ChildItem $publish -Recurse -File | Where-Object { $_.Name -match '^(library|settings|proxy-journal|config)\.json$|\.log$|\.trx$|\.pdb$' }
    # Publish symbols are intentionally excluded; data/config files must never enter a release.
    foreach ($symbol in @($forbidden | Where-Object Extension -eq '.pdb')) { Remove-Item -LiteralPath $symbol.FullName }
    if (@($forbidden | Where-Object Extension -ne '.pdb').Count) { throw 'Unexpected user/runtime artifact in publish output.' }
    $actualVersion = (Get-Item (Join-Path $publish 'Shaghad.exe')).VersionInfo.ProductVersion
    if (-not $actualVersion.StartsWith($version)) { throw 'Executable version mismatch.' }
    Compress-Archive -Path "$publish/*" -DestinationPath (Join-Path $release "SHAGHAD-v$version-win-x64-portable.zip") -CompressionLevel Optimal
    if ($BuildInstaller) {
        $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if (-not $compiler) { throw 'Inno Setup compiler unavailable; installer source is in packaging/Shaghad.iss.' }
        & $compiler.Source "/DReleaseVersion=$version" "/DPublishDirectory=$publish" "/DOutputDirectory=$release" packaging/Shaghad.iss
        if ($LASTEXITCODE) { throw 'Installer build failed.' }
    }
    $assets = Get-ChildItem $release -File
    $lines = foreach ($asset in $assets) { "$((Get-FileHash $asset.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $($asset.Name)" }
    $lines | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii
    & "$PSScriptRoot/verify-checksums.ps1" -Directory $release
    Write-Output $release
} finally { Pop-Location }
