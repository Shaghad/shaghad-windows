. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Invoke-Dotnet publish apps/windows/AJ.VPN.Desktop -c Release -r win-x64 --self-contained false -o artifacts/windows-x64
    Copy-Item (Join-Path $RepoRoot 'LICENSE') (Join-Path $RepoRoot 'artifacts/windows-x64/LICENSE')
    Copy-Item (Join-Path $RepoRoot 'docs/legal') (Join-Path $RepoRoot 'artifacts/windows-x64/legal') -Recurse -Force
    'Development foundation. No core binaries, VPN connectivity, signed installer or production release certification.' | Set-Content (Join-Path $RepoRoot 'artifacts/windows-x64/DEVELOPMENT-BUILD.txt')
} finally { Pop-Location }
