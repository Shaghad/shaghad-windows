# SHAGHAD logo design

Three project-authored concepts were created as editable 512×512 SVG documents. This is a design assessment, not user research or trademark clearance.

| Concept | Usability | Distinctiveness of form | Scalability | Decision |
|---|---|---|---|---|
| A — Persian Minimal | Open bowl, teeth and three triangular dots read as ش; calm silhouette | Persian letterform becomes an open connected path | Broad rounded strokes and generous gaps | Retained as an alternative |
| B — Network Geometry | Angular node path explicitly suggests connectivity | Different routing-like construction; linked dots reduce letter recognition | Horizontal node link adds density at small sizes | Retained as an alternative |
| C — Premium Monogram, refined | Compact angular S rhythm with three centered square dots | Distinct geometric silhouette; the dots retain a subtle Persian association | Balanced arms, 48-unit stroke and more open counters; raster-checked at 16–48 px | Selected by the user |

The user selected C. Its refinement keeps the angular S and square-dot signature while improving optical centering, arm balance and small-size spacing. A/B remain intact as alternatives; the original C is retained separately for comparison. The updated comparison image includes **actual 16/24/32/48 pixel samples**, not only enlarged symbols.

## Selected geometry

Coordinate system: 512 square, no gradients. Main stroke: **48 units**, square caps and bevel joins. The body is rotationally symmetric around `(256,288)`, with equal 200-unit horizontal arms and matching 64-unit diagonal turns. Compared with the original C's 52-unit stroke and unequal arms, the lighter construction opens negative space while keeping the angular character.

Three **40×40** squares, corner radius **6**, start at `(172,100)`, `(236,100)` and `(300,100)`. Their centers are `(192,120)`, `(256,120)`, `(320,120)`: a 64-unit rhythm centered on the body, with a 24-unit gap. They retain the three-dot Persian reference. The whole mark is optically centered within the unchanged app tile.

The main path is:

```text
M384 192 H184 L120 256 L392 320 L328 384 H128
```

The app icon places the symbol in turquoise on a navy tile, radius 112 in a 512 viewBox. The standalone monochrome variant has no tile. The open negative space and separated dots are maintained in every export. A small-size raster uses Lanczos supersampling; tiny dots are naturally antialiased rather than artificially enlarged.

SHAGHAD lettering is drawn from original geometric paths, with rounded joins, consistent stroke width and 64-unit letter advance. It remains editable and independent of third-party font outlines. Persian spelling is rendered as text with Vazirmatn in the application, not embedded into the small icon.

## Files and reproducibility

Masters: `D:\VPN-AJ\branding\concepts\concept-a.svg`, `concept-b.svg`, `concept-c.svg` and `D:\VPN-AJ\branding\logo\*.svg`.

Comparison: `D:\VPN-AJ\branding\previews\concepts-preview.png` (1680×1080). Selected identity boards: `dark-preview.png` and `light-preview.png` (1400×840). These are **design previews**, not application screenshots.

Before/after: `D:\VPN-AJ\branding\previews\concept-c-refinement.png` (1400×900), with original C, refined C on dark/light backgrounds and actual-size samples. Archived original study: `D:\VPN-AJ\branding\concepts\concept-c-original.svg`. Active concept: `D:\VPN-AJ\branding\concepts\concept-c.svg`.

Geometry/export source: `D:\VPN-AJ\scripts\export-branding.cjs`. Renderer: local Node.js plus sharp/librsvg. The same source generates WPF DrawingImage geometry in `D:\VPN-AJ\apps\windows\AJ.VPN.Desktop\Assets\BrandGeometry.xaml`. Light/dark drawing resources are swapped as whole images to avoid cached Freezable brush lookup problems during theme changes. High contrast uses Windows system brushes.

The real ICO directory contains seven sizes, through 256; 512 is a separate PNG. `D:\VPN-AJ\scripts\validate-branding.ps1` parses SVG XML, checks dimensions, decodes every ICO PNG payload, validates supported System.Drawing size selection, checks SHA-256 and required contrast pairs. The WPF smoke check decodes all seven frames, including 256. The System.Drawing 256 selection limitation is consistent with its byte-width comparison in [the official implementation](https://github.com/dotnet/winforms/blob/main/src/System.Drawing.Common/src/System/Drawing/Icon.cs).
