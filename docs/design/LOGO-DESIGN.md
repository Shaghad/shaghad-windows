# SHAGHAD logo design

Three project-authored concepts were created as editable 512×512 SVG documents. This is a design assessment, not user research or trademark clearance.

| Concept | Usability | Distinctiveness of form | Scalability | Decision |
|---|---|---|---|---|
| A — Persian Minimal | Open bowl, teeth and three triangular dots read as ش; calm silhouette | Persian letterform becomes an open connected path | Broad rounded strokes and generous gaps; best of these at 16 px | Selected |
| B — Network Geometry | Angular node path explicitly suggests connectivity | Different routing-like construction; linked dots reduce letter recognition | Horizontal node link adds density at small sizes | Retained as an alternative |
| C — Premium Monogram | Compact angular S rhythm | Visually separate from A/B; weaker Persian association | Thick diagonal mass survives, but open counters are less clear at 16 px | Retained as an alternative |

A is strongest because it retains the cultural reference and needs fewer elements to work at tray size. B's network explanation adds detail; C's Latin monogram weakens the preferred sheen direction. The comparison image includes **actual 16/24/32/48 pixel samples**, not only enlarged symbols.

## Selected geometry

Coordinate system: 512 square, no gradients. Main stroke: 44 units, round caps and joins. The open left bowl is balanced by three upright teeth; connected quadratics create a single readable rhythm. The dots use radius 22 at `(276,164)`, `(316,112)`, `(356,164)`. The triangular arrangement references sheen and avoids reducing the identity to a generic dotted S.

The main path is:

```text
M104 282 V310 Q104 368 162 368 H180 Q232 368 232 310 V232
M232 292 Q232 320 274 320 Q316 320 316 292 V232
M316 292 Q316 320 358 320 Q400 320 400 292 V232
```

The app icon places the symbol in turquoise on a navy tile, radius 112 in a 512 viewBox. The standalone monochrome variant has no tile. The open negative space and separated dots are maintained in every export. A small-size raster uses Lanczos supersampling; tiny dots are naturally antialiased rather than artificially enlarged.

SHAGHAD lettering is drawn from original geometric paths, with rounded joins, consistent stroke width and 64-unit letter advance. It remains editable and independent of third-party font outlines. Persian spelling is rendered as text with Vazirmatn in the application, not embedded into the small icon.

## Files and reproducibility

Masters: `D:\VPN-AJ\branding\concepts\concept-a.svg`, `concept-b.svg`, `concept-c.svg` and `D:\VPN-AJ\branding\logo\*.svg`.

Comparison: `D:\VPN-AJ\branding\previews\concepts-preview.png` (1680×1080). Selected identity boards: `dark-preview.png` and `light-preview.png` (1400×840). These are **design previews**, not application screenshots.

Geometry/export source: `D:\VPN-AJ\scripts\export-branding.cjs`. Renderer: local Node.js plus sharp/librsvg. The same source generates WPF DrawingImage geometry in `D:\VPN-AJ\apps\windows\AJ.VPN.Desktop\Assets\BrandGeometry.xaml`. Light/dark drawing resources are swapped as whole images to avoid cached Freezable brush lookup problems during theme changes. High contrast uses Windows system brushes.

The real ICO directory contains seven sizes, through 256; 512 is a separate PNG. `D:\VPN-AJ\scripts\validate-branding.ps1` parses SVG XML, checks dimensions, decodes every ICO PNG payload, validates supported System.Drawing size selection, checks SHA-256 and required contrast pairs. The WPF smoke check decodes all seven frames, including 256. The System.Drawing 256 selection limitation is consistent with its byte-width comparison in [the official implementation](https://github.com/dotnet/winforms/blob/main/src/System.Drawing.Common/src/System/Drawing/Icon.cs).
