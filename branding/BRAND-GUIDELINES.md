# SHAGHAD | شغاد

Modern Secure Network Client. Identity version 1.0, 11 October 2026.

SHAGHAD draws its name from Ferdowsi's Shahnameh. The identity references the Persian letter **ش**, not the character's actions. It makes no claim that Shaghad represented trust or protection. The product earns reliability through its behavior.

The selected symbol is Concept A, Persian Minimal: an open connected stroke, three upright teeth, and three separate dots. Its geometry was drawn for this project. No upstream logo, shield, lock, globe, historical portrait or ornamental motif is used. The wordmark is original vector lettering; interface labels use native typography.

## Assets and placement

| Purpose | Source relative to this directory |
|---|---|
| Standalone symbol on dark backgrounds | `logo/shaghad-symbol.svg` |
| Horizontal lockup on light backgrounds | `logo/shaghad-logo-light.svg` |
| Horizontal lockup on dark backgrounds | `logo/shaghad-logo-dark.svg` |
| General horizontal / vertical lockup | `logo/shaghad-logo-horizontal.svg`, `logo/shaghad-logo-vertical.svg` |
| One-color symbol | `logo/shaghad-monochrome.svg` |
| Application / tray icon | `icons/shaghad-app-icon.svg`, `icons/shaghad.ico` |
| Raster exports | `icons/shaghad-{16,24,32,48,64,128,256,512}.png` |
| Concept comparison / selected identity boards | `previews/concepts-preview.png`, `previews/dark-preview.png`, `previews/light-preview.png` |

The symbol uses a 512-unit square viewBox. Keep the entire square when preparing app icons. For independent placement allow at least 44 design units of clear space outside the painted mark. Keep the three dots; do not connect, rotate, mirror, stretch, add a shield or add shadows within the symbol. RTL layouts move the navigation, but the logo retains its original orientation.

Use the application icon at 16 px and above. At 16 px use the supplied raster, which preserves the dots with antialiasing; never add text. Use a horizontal wordmark at 160 px or wider, and keep product descriptions as separate readable text. The 512 PNG is intended for large previews and future platform export; Android adaptive masks and macOS icon containers are not implemented.

## Color

| Token | Hex | Role |
|---|---|---|
| Deep Navy | `#0B1120` | Dark canvas, icon tile, text on turquoise |
| Persian Turquoise | `#20C5C7` | Dark-theme symbol; primary button / selected navigation surface |
| Slate | `#1E293B` | Dark cards |
| Soft White | `#F8FAFC` | Light canvas; dark-theme primary text |
| Muted Gray | `#94A3B8` | Dark-theme secondary text |
| Deep Teal | `#087E80` | Light-theme symbol, links, focus |
| Light Secondary | `#526379` | Readable secondary text on light surfaces |
| Success | `#22C55E` / `#15803D` | Dark / light status text |
| Error | `#EF4444` | Error graphics; use `#F87171` for dark text and `#B91C1C` for light text |

Turquoise on white measures **2.13:1**, so it is decorative there. Navy text on turquoise measures **8.85:1**. Deep teal on white measures **4.88:1**. Gray on slate measures **5.71:1**. See `contrast-report.json` for every measured pairing. Color accompanies a status label; it never conveys connection state alone. These pair checks do not constitute a full accessibility certification.

## Typography and language

English interface: **Segoe UI**, supplied by Windows, not redistributed. Persian interface: **Vazirmatn**, original unmodified static Regular and SemiBold fonts, release v33.003, bundled as WPF resources. No system font installation is needed. Font license and copyright travel with the binaries: `fonts/OFL.txt` and `fonts/AUTHORS.txt`.

Use 14 DIP body text, 18 DIP section titles, 30 DIP page titles; small supporting labels are 11–13 DIP. Maintain Windows scaling rather than rasterizing interface text. Persian content is RTL; URLs, hostnames, versions, metrics and server URIs are explicit LTR islands. The English brand name remains uppercase SHAGHAD; Persian is شغاد. Keep the identity independent of the AJ.VPN internal namespaces and repository path.

## Export and verification

`scripts/export-branding.cjs` is the geometry source. It requires Node.js and `sharp` (available in this workstation's bundled Codex runtime). It writes editable SVG, raster PNG, a real seven-frame PNG-compressed ICO, WPF DrawingImage resources, contrast calculations and SHA-256 manifests. SVG masters need no font to render the wordmark. Preview-board captions use Segoe UI or a sans-serif fallback.

From `D:\VPN-AJ`:

```powershell
# With Node and sharp available (NODE_PATH may point to an existing sharp installation):
node ./scripts/export-branding.cjs
./scripts/validate-branding.ps1
./scripts/build.ps1
./scripts/test.ps1
./scripts/smoke-test.ps1
./scripts/publish.ps1
```

Never rename a PNG to ICO. The encoder writes ICONDIR and seven ICONDIRENTRY records containing 16/24/32/48/64/128/256 PNG payloads; 512 is provided separately. System.Drawing's size-selection logic treats the encoded 256 width byte as zero and can choose 128 instead; every payload is independently decoded and WPF validates all seven sizes.

Original logo geometry and export sources are part of this GPL v3 repository. No third-party artwork was incorporated. Trademark clearance has not been performed; originality here describes the creation process, not a worldwide uniqueness guarantee.

Further specifications: `D:\VPN-AJ\docs\design\BRAND-IDENTITY.md`, `LOGO-DESIGN.md`, `UI-DESIGN-SYSTEM.md`, `BRANDING-INTEGRATION.md` and `SHAGHAD-BRANDING-REPORT.md`.
