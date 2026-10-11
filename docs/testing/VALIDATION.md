# Validation — 2026-10-11

The foundation results below are retained as the original milestone record. Subsequent SHAGHAD branding validation, eight-page UI checks, ICO/font/tray callback evidence and renamed publish output are recorded in `D:\VPN-AJ\docs\design\SHAGHAD-BRANDING-REPORT.md`. Its Remaining Issues section distinguishes automated checks from unverified native shell behavior.

## Environment

Host Windows 10.0.26300, win-x64. Git 2.56.0.windows.2. Initial PATH dotnet had .NET 10.0.12 runtime/Desktop runtime but no SDK. Workspace-local official SDK 10.0.401 installed under D:/VPN-AJ/.tools/dotnet; MSBuild 18.9.11. No Visual Studio installation was found at the standard Installer/vswhere path. CLI WPF build succeeded without it. No system-wide dependency installer was run. CLI home/package cache are workspace-local; telemetry opt-out is set in scripts.

`Get-AuthenticodeSignature .tools/dotnet/dotnet.exe` returned Valid with signer .NET / Microsoft Corporation. This verifies the SDK host executable, not every file in the SDK or future core binaries.

GitHub CLI was initially missing; a local CLI was downloaded. `gh auth status` and `gh api user --jq .login` report no authenticated host. Fork lookup/creation and ownership verification remain blocked. Official latest release API returned v2rayN 7.25.4, whose checked-out commit is recorded in the gitlink.

## Executed final commands

Scripts use the local SDK and fail on nonzero dotnet exit codes.

| Command | Actual outcome |
|---|---|
| `dotnet restore VPN-AJ.slnx` via build.ps1 | Success; all projects restored/up to date |
| `dotnet build VPN-AJ.slnx -c Release --no-restore` | Success; zero warnings, zero errors; includes ServiceLib and ServiceLib.UdpTest Release builds |
| `dotnet test VPN-AJ.slnx -c Release --no-build --logger trx --results-directory artifacts/TestResults` | 11 passed: 3 application unit tests, 8 integration test cases; zero failures/skips |
| `scripts/smoke-test.ps1` | Success: WPF startup/DI, seven page renders, dark resources and Persian RTL. Offline opt-in app mode, not interactive UI automation |
| `scripts/publish.ps1` | Success: framework-dependent win-x64 development output at D:/VPN-AJ/artifacts/windows-x64; license/legal docs copied; unsigned, no core binaries |
| `dotnet list VPN-AJ.slnx package --vulnerable --include-transitive` | Success: NuGet reported no known vulnerable packages for all nine projects at query time |

Final passing test evidence: artifacts/TestResults/*2026-10-11_19_18_40*.trx. Render evidence: artifacts/smoke/*.png and result.txt. These outputs are intentionally ignored by Git. Image review examined dashboard, settings, dark mode and Persian RTL; corrected navigation contrast and selection synchronization. The remaining pages rendered successfully but were not all manually visually inspected.

## Test meaning

Unit tests cover duplicate startup rejection, cancellation cleanup with an uncancelled token, failed readiness, retry and distinction between core-ready and internet-verified. Integration tests exercise file-based settings roundtrip/cancelled write preservation, unknown-schema preservation, actual ServiceLib VLESS parsing using a reserved `.invalid` fixture, malformed/unsupported/TLS-bypass rejection and payload-free event logging. Test backends are confined to tests and never used by the app.

An added insecure-profile test initially failed because upstream parsing drops a TLS-bypass flag; the adapter now rejects it from the original query before parsing. Final tests passed after the fix. Initial build failures included a logger compile error and experimental WPF ThemeMode use; both were corrected, with ThemeMode removed rather than diagnostic suppression.

The restricted process sandbox caused an initial MSBuild failure without compiler diagnostics. Build/test/publish succeeded under approved execution outside that process restriction. Network retrieval also required approved network execution.

## Not validated

No real subscription URL, server credential or core binary was provided/used. VPN, internet/public IP, traffic counters, DNS, safe proxy restoration, engine provenance, TUN, privileged cleanup, signing/installer upgrades, clean machine deployment, ARM64, Windows 10 edition compatibility, multiple monitors/DPI, Narrator and live keyboard interaction remain untested. Upstream's own test project was inspected but not added/run (it references a beta assertion package). CI workflow exists but was not remotely run. NuGet vulnerability metadata is neither a full code audit nor complete license compliance.
