. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try { Invoke-Dotnet test VPN-AJ.slnx -c Release --no-build --logger trx --results-directory artifacts/TestResults }
finally { Pop-Location }
