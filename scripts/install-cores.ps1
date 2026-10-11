param([switch]$Download)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$stage = Join-Path $root '.tools/core-stage'
$releases = @(
    @{ Name='xray'; Version='26.3.27'; Archive='Xray-windows-64.zip'; Url='https://github.com/XTLS/Xray-core/releases/download/v26.3.27/Xray-windows-64.zip'; ArchiveHash='d004c39288ce9ada487c6f398c7c545f7d749e44bdfdd59dbc9f865afba4e1ad'; Exe='xray/xray.exe'; Hash='15c2d007954ac53ba69b80ec91242786b3c0b71d52649165b4ca1d5cc96ef8f1'; License='xray/LICENSE' },
    @{ Name='sing-box'; Version='1.14.3'; Archive='sing-box-1.14.3-windows-amd64.zip'; Url='https://github.com/SagerNet/sing-box/releases/download/v1.14.3/sing-box-1.14.3-windows-amd64.zip'; ArchiveHash='d8b86ef58936dba9230ec6a57ffa4243933f3c14ef4809828b6c553440ed1518'; Exe='sing-box/sing-box-1.14.3-windows-amd64/sing-box.exe'; Hash='5c19bb3f8c12d4e85b3d47fe63123a7624b314b58dd7a8e32f51004f1c4d2faf'; License='sing-box/sing-box-1.14.3-windows-amd64/LICENSE' }
)
New-Item -ItemType Directory -Path $stage -Force | Out-Null
foreach ($release in $releases) {
    $archive = Join-Path $stage $release.Archive
    if ($Download -and !(Test-Path -LiteralPath $archive)) { Invoke-WebRequest -Uri $release.Url -OutFile $archive }
    if (!(Test-Path -LiteralPath $archive)) { throw 'Core archive missing. Run scripts/install-cores.ps1 -Download to fetch pinned official releases.' }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $release.ArchiveHash) { throw 'Core archive integrity check failed.' }
    $exe = Join-Path $stage $release.Exe
    if (!(Test-Path -LiteralPath $exe)) { Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $stage $release.Name) -Force }
    if ((Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash -ne $release.Hash) { throw 'Core executable integrity check failed.' }
    $destination = Join-Path $root "cores/$($release.Name)"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $exe -Destination (Join-Path $destination "$($release.Name).exe") -Force
    Copy-Item -LiteralPath (Join-Path $stage $release.License) -Destination (Join-Path $destination 'LICENSE') -Force
    Write-Output "$($release.Name) $($release.Version): verified and installed"
}
