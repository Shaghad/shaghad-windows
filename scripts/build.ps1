. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Invoke-Dotnet restore VPN-AJ.slnx
    Invoke-Dotnet build VPN-AJ.slnx -c Release --no-restore
} finally { Pop-Location }
