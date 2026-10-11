. "$PSScriptRoot/common.ps1"
$output = Join-Path $RepoRoot 'artifacts/shaghad-smoke'
$app = Join-Path $RepoRoot 'apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.dll'
$process = Start-Process -FilePath $Dotnet -ArgumentList @('"' + $app + '"', '--smoke-test', '"' + $output + '"') -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(30000)) { throw "WPF smoke check exceeded 30 seconds. Process ID: $($process.Id)" }
if ($process.ExitCode -ne 0) { throw "WPF smoke check failed. Inspect $output/failure.txt" }
Get-Content (Join-Path $output 'result.txt')
