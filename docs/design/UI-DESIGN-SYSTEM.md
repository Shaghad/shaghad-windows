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
| ShaghadButton | WPF Button, minimum height 44 DIP, rounded border, keyboard focus thickness 3, pressed opacity feedback |
| ShaghadPrimaryButton | Shared button template, accent surface with contrast-safe text |
| ShaghadCard | Border, 16 DIP radius, 24 DIP padding, 1 DIP decorative border |
| ShaghadServerCard | Card specialization used for current server / change navigation |
| ShaghadMetricCard | Shared compact card, 20 DIP padding, four equal desktop columns |
| ShaghadStatusBadge / ConnectionLabel | Text-bearing badge; triggers for actual enum states |
| ShaghadNavigationItem | RadioButton with MDL2 Windows glyph, selected surface, hover and focus borders |
| ShaghadDialog | Theme-aware Window typography and surfaces |

Existing `Card` key aliases the new shared card style. Native TextBox and ComboBox retain keyboard/UI Automation behavior. No toggle was added because this milestone has no working boolean network feature to expose. Do not create a decorative toggle for unavailable proxy/TUN functionality.

## Layout and behavior

Main window: 1200×860 DIP default, 800×600 minimum. Sidebar: 240 DIP. Content padding: 32 horizontal, 26 vertical. Content scrolls as required. WPF vector marks and DIP layout support Windows scaling; physical multi-monitor DPI behavior still requires a hardware matrix check.

Navigation: Dashboard, Servers, Subscriptions, Routing, Statistics, Diagnostics, Settings; About and explicit Exit are placed at the bottom. All navigation is driven by the existing NavigateCommand/CurrentPage binding. Standard Tab, Shift+Tab and radio-group arrow navigation remain available. Selected navigation uses a dark focus border on turquoise so selection does not conceal focus.

Dashboard separates connection status, current server, mode and metrics. State text comes from ConnectionService.State; it currently remains Disconnected because the production backend is unavailable. There is no invented server, IP, latency, throughput or traffic history. Connection controls remain disabled. Future working connectivity must add state-change notification and real telemetry to the existing view model before enabling controls.

There are no ornamental looping animations or pretend progress indicators. Press feedback is immediate; the process proceeds directly through its existing asynchronous settings/service initialization to MainWindow. No artificial splash delay or progress percentage was introduced.

English uses LTR; Persian moves navigation to the right and uses Vazirmatn with RTL content. Logo orientation, versions, share URIs, repository URL and numeric metric fields are preserved LTR. Existing diagnostic explanations and enum-based theme choices remain English; translating those dynamic strings is future localization work. Body text is 14 DIP, section headings 18 and page titles 30. Supporting labels are 11–13; readable text wrapping is explicit in SecondaryText.

Native ComboBox and TabControl chrome is retained rather than replacing their interaction templates. Some native chrome follows the OS theme instead of the app palette. A full Narrator, high-contrast and keyboard accessibility audit is not claimed.

## Evidence

`D:\VPN-AJ\artifacts\shaghad-smoke\` contains actual WPF RenderTargetBitmap output for eight pages, dark dashboard, Persian dashboard and license dialog. These are programmatic UI renders, not desktop screenshots. `D:\VPN-AJ\docs\design\evidence\windows-concept-c-2026-10-11.png` is the actual running-window capture with refined C, made with the computer-use runtime. The original A-identity capture, `windows-dashboard-2026-10-11.png`, is retained as historical evidence. Brand boards under `branding/previews` are designed identity previews.
