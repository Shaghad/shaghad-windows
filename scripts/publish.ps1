. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Invoke-Dotnet publish apps/windows/AJ.VPN.Desktop -c Release -r win-x64 --self-contained false -o artifacts/shaghad-windows-x64
    Copy-Item (Join-Path $RepoRoot 'LICENSE') (Join-Path $RepoRoot 'artifacts/shaghad-windows-x64/LICENSE')
    $legalOutput = Join-Path $RepoRoot 'artifacts/shaghad-windows-x64/legal'
    New-Item -ItemType Directory -Path $legalOutput -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'docs/legal') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $legalOutput -Force }
    Copy-Item (Join-Path $RepoRoot 'branding/fonts/OFL.txt') (Join-Path $RepoRoot 'artifacts/shaghad-windows-x64/legal/Vazirmatn-OFL.txt')
    Copy-Item (Join-Path $RepoRoot 'branding/fonts/AUTHORS.txt') (Join-Path $RepoRoot 'artifacts/shaghad-windows-x64/legal/Vazirmatn-AUTHORS.txt')
    Copy-Item (Join-Path $RepoRoot 'src/AJ.VPN.Infrastructure/core-manifest.json') (Join-Path $legalOutput 'core-manifest.json')
    'SHAGHAD Phase 2 development build. Pinned cores are included only when scripts/install-cores.ps1 has been run before publishing. Real internet connectivity requires your valid server and has not been certified. No TUN, signed installer or production release certification. See legal/CORE-NOTICES.md.' | Set-Content (Join-Path $RepoRoot 'artifacts/shaghad-windows-x64/DEVELOPMENT-BUILD.txt')
} finally { Pop-Location }
