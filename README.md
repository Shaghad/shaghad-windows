# SHAGHAD | شغاد

Modern Secure Network Client. Original Windows VPN client foundation in C# / .NET 10 / WPF with Clean Architecture and MVVM, developed in the VPN-AJ repository. This development build does **not** establish a VPN connection and is not production-ready.

Implemented: original SHAGHAD identity and multi-resolution icon, dashboard and eight-page navigation including About/licenses, tray Open/About/Exit, real local diagnostics, atomic versioned appearance preferences, light/dark/system themes, high-contrast resources, English/Persian RTL with bundled OFL Vazirmatn, serialized connection-state orchestration, and profile preview validation using actual v2rayN ServiceLib. No sample servers, fake traffic, core binary downloads, driver installation, elevation or networking changes.

## Build and run

Requirements: Windows supported by .NET 10 and SDK 10.0.401. Visual Studio is optional; if used, 2026 version 18+ is required. This workspace has a local SDK under `.tools/dotnet`; fresh clones can use a compatible installed SDK. Initialize dependencies with `git submodule update --init --recursive`.

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/smoke-test.ps1
./scripts/publish.ps1
# Launch the built desktop app:
./.tools/dotnet/dotnet apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.dll
```

For an installed SDK use `dotnet` in the last command. The executable is `Shaghad.exe`; development publish output is `artifacts/shaghad-windows-x64`. Preferences and sanitized event metadata remain under `%LOCALAPPDATA%/VPN-AJ` to preserve existing data. Profile input is preview-only and not persisted. Closing the window exits and removes the tray icon. Connection, subscriptions, routing, real metrics, autostart and TUN are visibly unavailable.

Brand assets and reproducible export/validation instructions: [guidelines](branding/BRAND-GUIDELINES.md), [branding report](docs/design/SHAGHAD-BRANDING-REPORT.md) and [UI design system](docs/design/UI-DESIGN-SYSTEM.md).

## Source and licensing

GPL v3; see LICENSE and docs/legal. ServiceLib is pinned to v2rayN 7.25.4 / `7d6a967c18c697f28dc6917122ed3a4993fcf336`. Upstream UI is not reused. Hiddify was reviewed for UX only; no source/assets copied. The authenticated-user fork is blocked by missing GitHub login. The provisional submodule fetches original upstream read-only.

See [development report](docs/development/DEVELOPMENT-REPORT.md), [architecture](docs/architecture/SYSTEM-ARCHITECTURE.md), [validation](docs/testing/VALIDATION.md) and [upstream procedure](docs/development/UPSTREAM-UPDATE.md). License/native dependency, signing, packaging and network safety validation remain release gates.
