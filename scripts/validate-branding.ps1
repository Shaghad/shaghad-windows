$ErrorActionPreference = 'Stop'
$brandRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'branding'
Add-Type -AssemblyName System.Drawing
$svgFiles = Get-ChildItem -LiteralPath $brandRoot -Recurse -Filter '*.svg'
foreach ($file in $svgFiles) {
    $doc = [System.Xml.Linq.XDocument]::Load($file.FullName)
    if ($doc.Root.Name.LocalName -ne 'svg' -or -not $doc.Root.Attribute('viewBox')) { throw "Invalid SVG: $file" }
    if ($doc.Root.Name.NamespaceName -ne 'http://www.w3.org/2000/svg') { throw "Invalid SVG namespace: $file" }
}
foreach ($size in @(16,24,32,48,64,128,256,512)) {
    $pngPath = Join-Path $brandRoot "icons/shaghad-$size.png"
    $bitmap = [System.Drawing.Bitmap]::new($pngPath)
    try { if ($bitmap.Width -ne $size -or $bitmap.Height -ne $size) { throw "Wrong PNG dimensions: $pngPath" } }
    finally { $bitmap.Dispose() }
}
$icoPath = Join-Path $brandRoot 'icons/shaghad.ico'
$bytes = [IO.File]::ReadAllBytes($icoPath)
if ([BitConverter]::ToUInt16($bytes,0) -ne 0 -or [BitConverter]::ToUInt16($bytes,2) -ne 1 -or [BitConverter]::ToUInt16($bytes,4) -ne 7) { throw 'Invalid ICO header or frame count' }
$frameSizes = @(16,24,32,48,64,128,256)
for ($i = 0; $i -lt 7; $i++) {
    $entry = 6 + 16 * $i
    $width = if ($bytes[$entry] -eq 0) { 256 } else { [int]$bytes[$entry] }
    $height = if ($bytes[$entry+1] -eq 0) { 256 } else { [int]$bytes[$entry+1] }
    $length = [BitConverter]::ToUInt32($bytes,$entry+8)
    $offset = [BitConverter]::ToUInt32($bytes,$entry+12)
    if ($width -ne $frameSizes[$i] -or $height -ne $width -or $offset -lt 118 -or $offset + $length -gt $bytes.Length) { throw "Invalid ICO entry $i" }
    $frame = [IO.MemoryStream]::new($bytes, [int]$offset, [int]$length, $false)
    try {
        $bitmap = [System.Drawing.Bitmap]::new($frame)
        try { if ($bitmap.Width -ne $width -or $bitmap.Height -ne $height) { throw "Invalid ICO PNG frame $i" } }
        finally { $bitmap.Dispose() }
    } finally { $frame.Dispose() }
    # System.Drawing compares the encoded byte width literally (256 is encoded as zero).
    # Validate 256 by decoding its payload above, and all seven frames through WPF's smoke check.
    if ($width -lt 256) {
        $icon = [System.Drawing.Icon]::new($icoPath,$width,$height)
        try { if ($icon.Width -ne $width -or $icon.Height -ne $height) { throw "Windows ICO selection failed at $width" } }
        finally { $icon.Dispose() }
    }
}
$manifest = Get-Content (Join-Path $brandRoot 'asset-manifest.json') -Raw | ConvertFrom-Json
foreach ($asset in $manifest.outputs) {
    $assetPath = Join-Path $brandRoot $asset.path
    if ((Get-Item -LiteralPath $assetPath).Length -ne $asset.bytes -or (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash -ne $asset.sha256) { throw "Asset manifest mismatch: $($asset.path)" }
}
$contrast = Get-Content (Join-Path $brandRoot 'contrast-report.json') -Raw | ConvertFrom-Json
if (@($contrast | Where-Object { $_.pass -eq $false }).Count -gt 0) { throw 'Contrast validation failed' }
"PASS: $($svgFiles.Count) SVG documents; eight PNG sizes; seven independently decoded ICO frames, System.Drawing size selection 16-128; $($manifest.outputs.Count) asset hashes; required contrast pairs."
