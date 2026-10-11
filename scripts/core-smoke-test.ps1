param([switch]$Published, [ValidateRange(60,300)][int]$TimeoutSeconds = 120)
. "$PSScriptRoot/common.ps1"
$output = Join-Path $RepoRoot ('artifacts/core-check-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$app = Join-Path $RepoRoot 'apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.dll'
if ($Published) { $app = Join-Path $RepoRoot 'artifacts/shaghad-windows-x64/Shaghad.dll' }
$process = Start-Process -FilePath $Dotnet -ArgumentList @('"' + $app + '"', '--core-self-test', '"' + $output + '"') -WindowStyle Hidden -PassThru
for ($elapsed = 0; $elapsed -lt $TimeoutSeconds; $elapsed++) { if ($process.WaitForExit(1000)) { break } }
if (-not $process.HasExited) {
    $process.Kill($true) # Only this script's self-test process and its owned children.
    throw "Native core check exceeded $TimeoutSeconds seconds. Output: $output"
}
if ($process.ExitCode -ne 0) { throw "Native core check failed. Inspect $output/failure.txt" }
Get-Content (Join-Path $output 'result.json')
