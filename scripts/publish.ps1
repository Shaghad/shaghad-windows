param([string]$OutputDirectory = "artifacts/shaghad-windows-x64")
. "$PSScriptRoot/common.ps1"
$publishOutput = Join-Path $RepoRoot $OutputDirectory
Push-Location $RepoRoot
try {
    Invoke-Dotnet publish apps/windows/AJ.VPN.Desktop -c Release -r win-x64 --self-contained false -o $publishOutput
    Copy-Item (Join-Path $RepoRoot 'LICENSE') (Join-Path $publishOutput 'LICENSE')
    $legalOutput = Join-Path $publishOutput 'legal'
    New-Item -ItemType Directory -Path $legalOutput -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'docs/legal') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $legalOutput -Force }
    Copy-Item (Join-Path $RepoRoot 'branding/fonts/OFL.txt') (Join-Path $publishOutput 'legal/Vazirmatn-OFL.txt')
    Copy-Item (Join-Path $RepoRoot 'branding/fonts/AUTHORS.txt') (Join-Path $publishOutput 'legal/Vazirmatn-AUTHORS.txt')
    Copy-Item (Join-Path $RepoRoot 'src/AJ.VPN.Infrastructure/core-manifest.json') (Join-Path $legalOutput 'core-manifest.json')
    'SHAGHAD Phase 4 development build. Smart Auto selection, isolated HTTPS server testing and premium server manager. Pinned cores are included only when scripts/install-cores.ps1 has been run before publishing. Connectivity depends on your authorized server and current network; production readiness is not certified. No TUN, automatic failover or signed installer. See legal/CORE-NOTICES.md and the Phase 4 development report.' | Set-Content (Join-Path $publishOutput 'DEVELOPMENT-BUILD.txt')
} finally { Pop-Location }
