# SHAGHAD branding integration

Date: 2026-10-11. Existing solution: `D:\VPN-AJ\VPN-AJ.slnx`.

The repository and source project names remain VPN-AJ/AJ.VPN. Domain, Application, Infrastructure and V2RayNAdapter namespaces and references are unchanged. Branding is a presentation concern. No ServiceLib/v2rayN Core code or submodule content was modified for branding.

## Executable and migration decision

The desktop project's **AssemblyName is Shaghad**, producing **Shaghad.exe / Shaghad.dll**. RootNamespace stays AJ.VPN.Desktop, which also preserves XAML x:Class names. ProjectReference paths still point to AJ.VPN.Desktop.csproj. No installer, autostart registration, file association, updater or executable-path migration existed in the inspected foundation.

Build/publish/smoke entry points were updated. The new publish folder is `D:\VPN-AJ\artifacts\shaghad-windows-x64`; older build artifacts are untouched. The development distribution remains framework-dependent win-x64, requiring the .NET 10 Windows Desktop runtime. It is unsigned and contains no VPN engine binaries.

Preferences and logs deliberately remain at `%LOCALAPPDATA%\VPN-AJ`. Renaming the storage path would orphan existing appearance preferences. The JSON schema and services are unchanged. A future installed version must evaluate shortcut, updater, autostart and package identity migration independently of this assembly rename.

## Integration map

| Surface | File and resulting behavior |
|---|---|
| Executable / shell icon | `apps/windows/AJ.VPN.Desktop/AJ.VPN.Desktop.csproj`: ApplicationIcon references the real ICO; metadata Title/Product SHAGHAD |
| Window / taskbar | `MainWindow.xaml`: Title SHAGHAD and embedded Assets/shaghad.ico; normal Windows taskbar association |
| Navigation / dashboard | `MainWindow.xaml`: vector symbol, bilingual name, shared cards and actual state label |
| Settings | `MainWindow.xaml`: branded appearance section and About navigation |
| About | `MainWindow.xaml`, `ViewModels/ShellViewModel.cs`: own assembly version, truthful absent-core message, configured GitHub URL, upstream attribution |
| Font and logo resources | `AJ.VPN.Desktop.csproj`, `App.xaml`, `Assets/BrandGeometry.xaml`: bundled fonts/ICO/licenses and vector image resources |
| Tray | `TrayIntegration.cs`, `App.xaml.cs`: standalone icon; Open, About and Exit; double-click restores; disposed on app exit |
| License viewer | `LicenseWindow.cs`: embedded full GPL v3 and font OFL text in a modal window |
| Repository-link error dialog | `MainWindow.xaml.cs`: SHAGHAD caption; only the fixed repository URL is opened |
| Publisher | `scripts/publish.ps1`: renamed output plus complete font OFL/AUTHORS and existing legal inventory |

All relative file entries above are under **D:\VPN-AJ**. Icon master: `D:\VPN-AJ\branding\icons\shaghad.ico`. Embedded resource URIs do not contain the former desktop assembly name.

## Tray, notifications and startup

A tray integration did not exist, so a small Windows Forms NotifyIcon wrapper was added to the WPF application. It only navigates/restores/exits; it does not control proxy, engine or OS configuration. Tooltip is SHAGHAD plus actual state. The app still exits when the main window closes; it does not silently minimize into the tray. Existing service initialization remains in order. NotifyIcon and owned graphics/menu resources are disposed on exit.

There was no toast/balloon notification service and no installer. No artificial notifications, installer project or background startup registration were added. Available local dialogs carry SHAGHAD branding. The app opens directly after initialization; no delayed splash or fake progress bar is used.

## Licensing and provenance

SHAGHAD is independent of official v2rayN and Hiddify. About names v2rayN ServiceLib 7.25.4, 2dust/contributors and GPL v3; Hiddify has no copied assets/code. The root GPL license is retained. Full dependency/native license completion remains a pre-existing distribution gate.

Vazirmatn Regular/SemiBold v33.003 are unmodified OFL fonts from the official release. `branding/fonts/OFL.txt` and `AUTHORS.txt` are included and copied to development output. WPF resources embed the fonts and copyright-bearing OFL file. Segoe UI is referenced from Windows without redistribution. See `BRAND-IDENTITY.md` for source and hashes.

## Validation boundaries

Run the export/asset validation scripts, Release build, existing tests, WPF smoke check and publish. The smoke mode uses isolated preferences under its explicit output directory; it renders eight pages, theme and RTL cases, resolves embedded fonts, decodes seven ICO frames and invokes the actual tray About callback. That callback briefly shows/restores the window before the check hides/exits it. It makes no network change.

The live Windows runtime verified the titled window and About navigation, and produced a screenshot showing the small title-bar icon. The available computer-use window inventory does not expose the shell taskbar/tray surface, so their on-screen placement, overflow menu and native pointer interaction remain a manual verification item. Icon decode and tray callback checks must not be described as a successful native shell interaction test.

System.Drawing picks 128 when requesting the 256 ICO size due to its encoded-width comparison. The seven payloads decode independently; WPF checks the full 16–256 frame set. This is documented instead of removing the valid high-resolution icon frame.
