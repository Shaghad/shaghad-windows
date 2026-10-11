# SHAGHAD Windows UI design system

The existing WPF/MVVM composition remains in `D:\VPN-AJ\apps\windows\AJ.VPN.Desktop`. No new framework or upstream UI has been introduced. The visual direction uses Windows 11-like cards, quiet borders, native typography and a desktop sidebar, while retaining standard WPF Window and control behavior.

## Tokens and themes

Resource dictionaries: `Themes/Light.xaml`, `Themes/Dark.xaml`, `Themes/HighContrast.xaml`. Theme selection remains Light / Dark / System. System observes Windows AppsUseLightTheme and refreshes on UserPreferenceChanged; high contrast overrides the selected theme. No system setting is changed by the app.

| Semantic resource | Use |
|---|---|
| PageBrush / SurfaceBrush / SidebarBrush | Canvas, card, navigation surfaces |
| TextBrush / SecondaryTextBrush | Primary and supporting text |
| AccentBrush / AccentTextBrush | Primary and selected controls with navy-on-turquoise text |
| SymbolBrush | Turquoise on dark, deep teal on light |
| BorderBrush | Subtle decorative card boundaries |
| InputBorderBrush / FocusBrush | Essential control outline and keyboard focus |
| InputBrush | Editable control surface |
| ConnectedBrush / ErrorBrush / WarningBrush | Semantic state colors, accompanied by text |
| StatusSurfaceBrush | Status badge background |
| UiFont / PersianFont | Segoe UI or bundled Vazirmatn |
| ShaghadSymbol | Theme-specific WPF vector DrawingImage |

Contrast calculations are in `D:\VPN-AJ\branding\contrast-report.json`; normal text pairings meet 4.5:1. Thin decorative card borders are not input affordances. Turquoise surfaces use dark text. Light symbol/link text uses deep teal because turquoise/white fails. High-contrast semantic values resolve Windows system colors.

## Shared components

All WPF styles are in `D:\VPN-AJ\apps\windows\AJ.VPN.Desktop\Styles\ShaghadDesignSystem.xaml`.

| Style | Implementation and behavior |
|---|---|
| ShaghadButton | WPF Button, minimum height 38 DIP, rounded border, keyboard focus thickness 3, pressed opacity feedback |
| ShaghadPrimaryButton | Shared button template, accent surface with contrast-safe text; dashboard primary action has a 48 DIP minimum |
| ShaghadCard | Border, 12 DIP radius, adaptive 14/22 DIP padding, 1 DIP decorative border |
| ShaghadServerCard | Card specialization used for current server / change navigation |
| ShaghadMetricCard | Shared optional metric style; unavailable IP/traffic cards are no longer shown on Dashboard |
| ShaghadStatusBadge / ConnectionLabel | Text-bearing badge; triggers for actual enum states |
| ShaghadNavigationItem | RadioButton with MDL2 Windows glyph, selected surface, hover and focus borders |
| ShaghadDialog | Theme-aware Window typography and surfaces |
| LocalizedText / LocalizedInput | Explicit human-language text islands; native RTL paragraph flow in Persian, physical right alignment |
| TechnicalText | Explicit LTR, Segoe UI, wrapping for endpoint, URL, engine and version values |
| ShaghadPageScrollBar | Theme-aware vertical thumb and native Track/PageUp/PageDown behavior, scoped to page scrolling |

Existing `Card` key aliases the new shared card style. Native TextBox and ComboBox retain keyboard/UI Automation behavior. No toggle was added because this milestone has no working boolean network feature to expose. Do not create a decorative toggle for unavailable proxy/TUN functionality.

## Layout and behavior

Main window: 1200×760 DIP default, 960×540 minimum. Sidebar: 216 DIP, always on the left. Content padding is 28×22 normally and 20×12 below 680 DIP height or 1100 DIP width. Navigation density changes from 44 to 36 DIP minimum height. Dashboard and About fit the tested 1024×540 outer window without vertical scrolling; longer forms retain an automatic visible scrollbar. WPF vector marks and DIP layout support Windows scaling; a full physical multi-monitor DPI matrix is still outside this verification.

Navigation: Dashboard, Servers, Subscriptions, Routing, Statistics, Diagnostics, Settings; About and explicit Exit are placed at the bottom. All navigation is driven by the existing NavigateCommand/CurrentPage binding. Standard Tab, Shift+Tab and radio-group arrow navigation remain available. Selected navigation uses a dark focus border on turquoise so selection does not conceal focus.

Dashboard follows the user's selected minimal professional direction: one connection surface, one selected-server row and quiet verification text. The primary action imports a server when none is selected, starts the real proxy when idle, or cancels/stops it while active. HTTPS testing is enabled only while the local proxy is ready; latency appears only after actual verification. Engine selection and local endpoint use real services. No unavailable IP/traffic cards or fabricated statistics are displayed. Core execution was implemented in Phase 2; see `D:/VPN-AJ/docs/architecture/CORE-INTEGRATION.md`.

There are no ornamental looping animations or pretend progress indicators. Press feedback is immediate; the process proceeds directly through its existing asynchronous settings/service initialization to MainWindow. No artificial splash delay or progress percentage was introduced.

Window, root grid, content columns, navigation templates and native caption always use LTR. No WindowChrome replacement, fake caption buttons or negative transforms are used. Language swaps fonts/strings and explicit text-island resources only. Persian uses Vazirmatn and native RTL paragraph flow; leading paragraph alignment inside that RTL coordinate system renders at the physical right, as verified in the running WPF renderer. Technical endpoints, URLs, profile URIs, engine names, latency and versions remain explicitly LTR. Some diagnostics and enum choices remain English. Body text is 14 DIP, section headings 18, page titles 24/30 and dashboard state 26/32. Shared localized line metrics keep card positions stable across languages. See `D:/VPN-AJ/docs/design/UI-REDESIGN-REPORT.md` for current evidence and limits.

Native ComboBox and TabControl chrome is retained rather than replacing their interaction templates. Some native chrome follows the OS theme instead of the app palette. A full Narrator, high-contrast and keyboard accessibility audit is not claimed.

## Evidence

`D:\VPN-AJ\artifacts\shaghad-smoke\` contains actual WPF RenderTargetBitmap output for eight pages, dark dashboard, Persian dashboard and license dialog. These are programmatic UI renders, not desktop screenshots. `D:\VPN-AJ\docs\design\evidence\windows-concept-c-2026-10-11.png` is the actual running-window capture with refined C, made with the computer-use runtime. The original A-identity capture, `windows-dashboard-2026-10-11.png`, is retained as historical evidence. Brand boards under `branding/previews` are designed identity previews.
