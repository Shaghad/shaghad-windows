# VPN-AJ

Original Windows VPN client foundation in C# / .NET 10 / WPF with Clean Architecture and MVVM. This development build does **not** establish a VPN connection and is not production-ready.

Implemented: dashboard and seven-page navigation, real local diagnostics, atomic versioned appearance preferences, light/dark/system themes, high-contrast resources, English/Persian RTL infrastructure, serialized connection-state orchestration, and profile preview validation using actual v2rayN ServiceLib. No sample servers, fake traffic, core binary downloads, driver installation, elevation or networking changes.

## Build and run

Requirements: Windows supported by .NET 10 and SDK 10.0.401. Visual Studio is optional; if used, 2026 version 18+ is required. This workspace has a local SDK under `.tools/dotnet`; fresh clones can use a compatible installed SDK. Initialize dependencies with `git submodule update --init --recursive`.

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/smoke-test.ps1
./scripts/publish.ps1
# Launch the built desktop app:
./.tools/dotnet/dotnet apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/AJ.VPN.Desktop.dll
```

For an installed SDK use `dotnet` in the last command. Preferences and sanitized event metadata are stored under `%LOCALAPPDATA%/VPN-AJ`. Profile input is preview-only and not persisted. Closing the window exits. Connection, subscriptions, routing, real metrics, tray, autostart and TUN are visibly unavailable.

## Source and licensing

GPL v3; see LICENSE and docs/legal. ServiceLib is pinned to v2rayN 7.25.4 / `7d6a967c18c697f28dc6917122ed3a4993fcf336`. Upstream UI is not reused. Hiddify was reviewed for UX only; no source/assets copied. The authenticated-user fork is blocked by missing GitHub login. The provisional submodule fetches original upstream read-only.

See [development report](docs/development/DEVELOPMENT-REPORT.md), [architecture](docs/architecture/SYSTEM-ARCHITECTURE.md), [validation](docs/testing/VALIDATION.md) and [upstream procedure](docs/development/UPSTREAM-UPDATE.md). License/native dependency, signing, packaging and network safety validation remain release gates.
