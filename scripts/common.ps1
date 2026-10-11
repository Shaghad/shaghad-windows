$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME = Join-Path $RepoRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $RepoRoot '.tools/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$localSdk = Join-Path $RepoRoot '.tools/dotnet/dotnet.exe'
$script:Dotnet = if (Test-Path $localSdk) { $localSdk } else { 'dotnet' }
function Invoke-Dotnet {
    & $script:Dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}
