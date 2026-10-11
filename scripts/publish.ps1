. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Invoke-Dotnet publish apps/windows/AJ.VPN.Desktop -c Release -r win-x64 --self-contained false -o artifacts/windows-x64
} finally { Pop-Location }
