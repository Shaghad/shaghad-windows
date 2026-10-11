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
    'SHAGHAD development foundation. No core binaries, VPN connectivity, signed installer or production release certification.' | Set-Content (Join-Path $RepoRoot 'artifacts/shaghad-windows-x64/DEVELOPMENT-BUILD.txt')
} finally { Pop-Location }
