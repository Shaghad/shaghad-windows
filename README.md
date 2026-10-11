# SHAGHAD | شغاد

Modern Secure Network Client. Windows proxy client in C# / .NET 10 / WPF with Clean Architecture and MVVM, developed in the VPN-AJ repository. Phase 2 runs verified Xray and sing-box cores. Real internet connectivity requires your valid server profile and has not yet been validated; this unsigned development build is not production-ready.

Implemented: refined Concept C branding, existing themed/RTL desktop, VLESS/VMess/Trojan/Shadowsocks imports and clipboard import using ServiceLib, encrypted server and subscription storage, active selection, explicit HTTPS subscription updates, native configuration validation, owned core lifecycle, real HTTPS latency testing, sanitized diagnostics and opt-in Windows System Proxy with encrypted restoration journal. The dashboard distinguishes a ready local proxy from verified internet access. No sample servers, fake traffic, drivers, elevation or automatic networking changes.

## Build and run

Requirements: Windows supported by .NET 10 and SDK 10.0.401. Visual Studio is optional; if used, 2026 version 18+ is required. This workspace has a local SDK under `.tools/dotnet`; fresh clones can use a compatible installed SDK. Initialize dependencies with `git submodule update --init --recursive`.

```powershell
./scripts/install-cores.ps1 -Download  # pinned official Windows x64 releases; hashes checked
./scripts/build.ps1
./scripts/test.ps1
./scripts/core-smoke-test.ps1         # offline native checks; no real server or Windows proxy mutation
./scripts/smoke-test.ps1
./scripts/publish.ps1
# Launch the built desktop app:
./.tools/dotnet/dotnet apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.dll
```

For an installed SDK use `dotnet` in the last command. Executable: `Shaghad.exe`; publish output: `artifacts/shaghad-windows-x64`. Core installation is explicit; the application never downloads executables itself. Without the pinned binaries the UI remains usable and Diagnostics explains installation.

Use Servers to import your share links and select a core/server. Start the proxy on Dashboard, then explicitly test HTTPS (gstatic.com) to measure real request latency. Settings → Windows System Proxy requires a separate confirmation. Closing restores approved proxy changes before stopping owned processes. After a crash, explicitly review/restore the journal in Settings; external proxy changes are preserved.

Preferences stay under `%LOCALAPPDATA%/VPN-AJ`; `secrets/library.dpapi` and `secrets/proxy-recovery.dpapi` are Windows-user-encrypted. Credentials must be present briefly in an ACL-restricted runtime config while a core runs; it is deleted on stop, and marked abandoned sessions are cleaned at the next launch. TUN, advanced routing, public IP/traffic counters, autostart and automatic failover remain outside this phase. Dynamic errors/options are not fully translated into Persian.

Brand assets and reproducible export/validation instructions: [guidelines](branding/BRAND-GUIDELINES.md), [branding report](docs/design/SHAGHAD-BRANDING-REPORT.md) and [UI design system](docs/design/UI-DESIGN-SYSTEM.md).

## Source and licensing

GPL v3; see LICENSE and docs/legal. ServiceLib is pinned to v2rayN 7.25.4 / `7d6a967c18c697f28dc6917122ed3a4993fcf336`. Upstream UI is not reused. Hiddify was reviewed for UX only; no source/assets copied. The authenticated-user fork is blocked by missing GitHub login. The provisional submodule fetches original upstream read-only.

See [development report](docs/development/DEVELOPMENT-REPORT.md), [architecture](docs/architecture/SYSTEM-ARCHITECTURE.md), [validation](docs/testing/VALIDATION.md), [native core notices](docs/legal/CORE-NOTICES.md) and [upstream procedure](docs/development/UPSTREAM-UPDATE.md). Real connectivity, native proxy behavior with user approval, full dependency license compliance, signing and clean-machine packaging remain release gates.
