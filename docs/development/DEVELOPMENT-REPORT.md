## Phase 3 — Current result, 2026-10-11

Continued the existing solution and preserved upstream source and uncommitted Phase 2 work. Added a reusable HTTPS probe supporting HTTP CONNECT and SOCKS5, sanitized TLS/DNS/tunnel/connection/timeout troubleshooting, metadata-only probe events and seven regression cases. Added an explicit --production-connectivity-test request-file output-directory mode using the existing subscription downloader, encrypted isolated library, trusted cores and owned-process cleanup. Credential-bearing request files belong only in ignored private storage; results contain no profile payloads. Manual retry recovery is implemented but its final harness run is pending.

An authorized subscription supplied VLESS profiles. The selected profile passed real TLS-verified HTTPS through both proxy types on Xray. Sing-box started/stopped but both HTTPS requests failed in the secure-connection category; the underlying cause remains unresolved. Other three protocols, DNS behavior, separate address-family checks and live Windows proxy behavior remain unverified. The user explicitly requested system proxy tests stay pending. No WinHTTP/DNS/routing, TUN or Auto Failover changes were made.

Release build passed with zero warnings/errors in separate Phase 3 outputs; 61 automated tests passed. WPF render smoke passed. Interactive inspection was partial, and the user stopped Computer Use with Escape. GitHub target is configured but remote identity/push permission remain unverified owing to invalid CLI authentication and restricted network/DNS failures. No source was published. A limited credential marker scan returned zero findings; it is not a full historical secret audit.

Detailed executed evidence and remaining limitations are recorded in docs/testing/VALIDATION.md, whose Phase 3 section supersedes older statements that no authorized profile was supplied. Existing historical milestone sections are retained.
# SHAGHAD DEVELOPMENT REPORT

## Phase 2 — Current result, 2026-10-11

Implemented within the existing `D:/VPN-AJ/VPN-AJ.slnx`, preserving the refined Concept C identity, namespaces, preferences path and upstream pin. The application now runs real local HTTP/SOCKS5 proxies through verified Xray 26.3.27 and sing-box 1.14.3. **No valid user profile/subscription URL was supplied: real remote connectivity and production readiness are not claimed.** Windows networking was not changed during implementation/testing.

### Audit and resolved blockers

`D:/VPN-AJ/docs/architecture/PHASE-2-AUDIT.md` records the baseline: UnavailableCoreBackend, discarded credentials, no profile library, no trusted binaries or native lifecycle, no subscription management and no proxy journal. These were replaced with the narrow ServiceLib adapter described in `D:/VPN-AJ/docs/architecture/CORE-INTEGRATION.md`. The complete upstream AppManager/CoreManager lifecycle is not initialized. No v2rayN Core or ServiceLib source was modified; gitlink remains `7d6a967c18c697f28dc6917122ed3a4993fcf336` / 7.25.4.

### Implemented functionality

- Explicit official binary installation, pinned archive/executable SHA-256 checks, compiled executable trust manifest, version verification and bounded retries for transient Windows sharing violations. Discovery is restricted to the application's `cores` directory, never arbitrary PATH executables.
- Actual ServiceLib share-link parsing for VLESS, VMess, Trojan and Shadowsocks. Clipboard/newline/base64-list imports, validation, encrypted persistence, display, selection and removal. Unknown transports and TLS-verification bypasses are rejected. Native core/capability validation occurs again before connection.
- Single-profile ServiceLib configuration generation, narrowed to one proxy outbound and an IPv4 loopback mixed listener; native `xray run -test -c` / `sing-box check -c` checks precede execution.
- Suspended process creation → private kill-on-close Windows job → resume, with only three standard handles inherited. PID-owned listener plus SOCKS5 handshake readiness; duplicate rejection; startup failure/timeout/cancellation; unexpected-exit tracking; graceful CTRL_BREAK via a console helper, then owned-job termination if needed. Other VPN/core processes are never killed by name.
- Connection states and internet verification are separate. An explicit TLS-verified HTTPS request through the active proxy to `https://www.gstatic.com/generate_204` measures real HTTP request latency. No public IP or traffic values are fabricated.
- HTTPS subscription URL CRUD and explicit updates. Public-address DNS validation/pinned IP connection, redirect/size/time bounds, no ambient proxy/cookies/credentials or external converter. Parse/validation precedes atomic replacement; failure preserves previous profiles and active share-link selection. Scheduling and quotas are deferred.
- Opt-in current-user Windows System Proxy. Separate confirmation, encrypted recovery journal before mutation, prior absent/manual/PAC/discovery/bypass/LAN binary values, apply-time concurrent-change checks, partial failure/cancellation rollback and ownership-aware restoration. Crash recovery requires explicit approval. Unknown LAN formats and machine/policy restrictions fail closed. Tests use an in-memory registry backend; native effect is unverified.
- Existing Dashboard, Servers, Subscriptions, Settings, Diagnostics, About and tray bind actual services/state. Old foundation placeholders were corrected. Version is 0.2.0 and executable remains `Shaghad.exe`. Normal close restores approved changes before stopping the core. Appearance and branding remain independent of internal architecture.

### Key implementation paths

| Area | Exact path |
|---|---|
| Library transactions | `D:/VPN-AJ/src/AJ.VPN.Application/LibraryService.cs` |
| Connection state | `D:/VPN-AJ/src/AJ.VPN.Application/ConnectionService.cs` |
| Parser / configuration adapter | `D:/VPN-AJ/src/AJ.VPN.V2RayNAdapter/ServiceLibProfileValidator.cs`, `D:/VPN-AJ/src/AJ.VPN.V2RayNAdapter/ServiceLibConfigurationGenerator.cs` |
| DPAPI and private ACLs | `D:/VPN-AJ/src/AJ.VPN.Infrastructure/ProtectedFiles.cs` |
| Core trust | `D:/VPN-AJ/src/AJ.VPN.Infrastructure/TrustedCoreCatalog.cs`, `D:/VPN-AJ/src/AJ.VPN.Infrastructure/core-manifest.json` |
| Process ownership / lifecycle | `D:/VPN-AJ/src/AJ.VPN.Infrastructure/OwnedCoreProcess.cs`, `D:/VPN-AJ/src/AJ.VPN.Infrastructure/LocalProxyBackend.cs` |
| Subscription download policy | `D:/VPN-AJ/src/AJ.VPN.Infrastructure/SubscriptionDownloader.cs` |
| Proxy ownership / recovery | `D:/VPN-AJ/src/AJ.VPN.Infrastructure/SystemProxyService.cs` |
| UI composition / binding | `D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/App.xaml.cs`, `D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/ViewModels/ShellViewModel.cs`, `D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/MainWindow.xaml` |
| Explicit core setup / offline verification | `D:/VPN-AJ/scripts/install-cores.ps1`, `D:/VPN-AJ/scripts/core-smoke-test.ps1`, `D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/CoreSelfTest.cs` |
| Tests | `D:/VPN-AJ/tests/AJ.VPN.IntegrationTests/PhaseTwoTests.cs`, `D:/VPN-AJ/tests/AJ.VPN.IntegrationTests/ProxyRestorationTests.cs` |
| Native licenses/source references | `D:/VPN-AJ/docs/legal/CORE-NOTICES.md` |

### Actual validation and local output

Restore/Release build succeeded with zero warnings/errors. **54 automated tests passed** (3 unit, 51 integration; zero failed/skipped). **23 offline native checks passed against the published package**, including all four protocols in both engines, graceful lifecycle, duplicate rejection, cancellation, unexpected exit, invalid config, occupied-port startup failure and an actual ten-second readiness timeout. All private self-test runtime configs were removed. WPF offline startup/render/ICO/font/tray callback checks passed. NuGet reported no known vulnerable managed direct/transitive packages at query time. Details/evidence: `D:/VPN-AJ/docs/testing/VALIDATION.md`.

Published executable: `D:/VPN-AJ/artifacts/shaghad-windows-x64/Shaghad.exe`. Installed/published cores and their licenses are under `D:/VPN-AJ/cores/{xray,sing-box}` and `D:/VPN-AJ/artifacts/shaghad-windows-x64/cores/{xray,sing-box}`. Large binary/test/publish artifacts remain ignored by Git; reproducible setup and the trust manifest are source files. No push or remote CI run was performed.

### Security findings and remaining limits

1. At-rest profiles and token-bearing URLs use current-user DPAPI and private ACLs. Native cores require plaintext credentials in a restricted runtime config while running. Normal stop deletes it; startup cleans marked abandoned sessions. Managed secret strings cannot be reliably erased, and DPAPI does not protect against malicious code running as the same Windows user.
2. Only loopback is exposed, with no inbound authentication. Other local applications can use that endpoint. This phase is a proxy client, not an all-traffic tunnel; server-host DNS bootstrap uses the system resolver. TUN/system DNS/routes/WinHTTP/RAS are not managed.
3. Core executable hashes/versions are verified; the compiled app is unsigned. Pinning does not defend against replacement of the whole app. Core self-updates/rollback, Authenticode release signing and clean-machine deployment are deferred.
4. Proxy restoration policy passes automated tests, including external writers and partial failures. Registry writes are not an atomic transaction with other applications. The known LAN binary format is checked strictly; actual WinINet behavior, policy variants and crash/logoff recovery on real settings need explicit user-approved validation. Recovery cannot safely overwrite external changes; conflicts retain the encrypted journal for manual review.
5. No real server, provider subscription, HTTPS success or real latency was validated. The full transport/cipher/REALITY matrix, HTTPS redirect/size behavior against live providers, long-duration stability and real network failures remain untested. Basic offline native protocol checks do not prove a working remote server.
6. Public IP/traffic counters, advanced routing, TUN, autostart, scheduling, automatic ranking/failover and full Persian translation are outside this phase. Native taskbar/tray shell behavior, Narrator/high DPI/multiple monitors and clean Windows editions need additional interactive validation.
7. Core LICENSE files and exact upstream source tags are included/referenced. Complete transitive native notice/source distribution compliance, signing and installer packaging remain release gates. No installer exists to update.

Primary implementation references: [Xray command validation](https://xtls.github.io/en/document/command), [sing-box 1.14.3 release](https://github.com/SagerNet/sing-box/releases/tag/v1.14.3), [Windows job lifetime](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects), [WinINet refresh options](https://learn.microsoft.com/en-us/windows/win32/wininet/option-flags). Local compiled behavior and executed tests, rather than documentation alone, establish the validation results above.

## Historical foundation report

The sections below describe the original foundation before branding and Phase 2. Their unavailable-feature statements are historical, superseded by the current result above.

Date: 2026-10-11, Asia/Tehran.

## 1. Development Environment — Completed

Inspected empty D:/VPN-AJ. Git available, no .NET SDK or GitHub CLI initially. Installed official SDK 10.0.401 and GitHub CLI locally under .tools; no system-wide installer. .NET 10 / WPF Release build verified on host Windows x64. See ../testing/VALIDATION.md.

## 2. GitHub Fork Status — Blocked

Local CLI confirms no authenticated GitHub host. No account login, existing fork, newly created fork or ownership was claimed. Original upstream is retained as provisional read-only submodule dependency. During development, Git commits and root remote Aj-VSCode → https://github.com/Shaghad/shaghad-windows.git were configured outside this agent's commands. Preserved without reset or push. This is not verified fork ownership. See UPSTREAM-UPDATE.md.

## 3. Upstream Version — Completed

GitHub Releases API returned latest stable 7.25.4. Pinned source commit 7d6a967c18c697f28dc6917122ed3a4993fcf336 at D:/VPN-AJ/external/v2rayN. Original UI remains untouched. License text retained; root GPL v3 LICENSE added.

## 4. Hiddify Analysis — Partially Completed

Inspected source tree and home, connection, profile/update/quota, proxy sort/test, settings, theme, router and accessibility-related source at 98387fe96b1ce5bc01ec463ab2a0d1a6f0c42d2e. Structured comparison and usability findings documented. No source/assets copied. Interactive UX/accessibility audit and separate legal interpretation of additional license conditions remain outstanding.

## 5. Architecture Decisions — Completed for foundation

Clean Architecture, MVVM, portable Domain/Application, isolated Windows Infrastructure, explicit ServiceLib adapter, DI. Original stable WPF styling with semantic themes; rejected experimental ThemeMode. Versioned atomic JSON for non-secret preferences; SQLite/DPAPI deferred until actual structured profile/subscription persistence. Connection service distinguishes readiness from verified internet. Network ownership, TUN, ranking and secure update designs documented.

## 6. Created Projects — Completed

Solution: D:/VPN-AJ/VPN-AJ.slnx.

- D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/AJ.VPN.Desktop.csproj
- D:/VPN-AJ/src/AJ.VPN.Domain/AJ.VPN.Domain.csproj
- D:/VPN-AJ/src/AJ.VPN.Application/AJ.VPN.Application.csproj
- D:/VPN-AJ/src/AJ.VPN.Infrastructure/AJ.VPN.Infrastructure.csproj
- D:/VPN-AJ/src/AJ.VPN.V2RayNAdapter/AJ.VPN.V2RayNAdapter.csproj
- D:/VPN-AJ/tests/AJ.VPN.UnitTests/AJ.VPN.UnitTests.csproj
- D:/VPN-AJ/tests/AJ.VPN.IntegrationTests/AJ.VPN.IntegrationTests.csproj

Upstream ServiceLib and ServiceLib.UdpTest are included as dependency projects, not new VPN-AJ code.

## 7. Implemented Features — Completed for foundation

Original dashboard/sidebar, seven navigable pages, local diagnostic results, persistent theme/language preferences, light/dark/system and high-contrast resources, English and Persian RTL infrastructure with LTR technical input, keyboard-capable WPF controls, explicit Exit, serialized connection-state service and event-metadata-only logging. Unsupported functionality is visibly unavailable. Servers supports actual upstream validation preview only; no persistent servers or credentials. No simulated metrics or connections.

Key source files:

- D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/App.xaml.cs
- D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/MainWindow.xaml
- D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/ViewModels/ShellViewModel.cs
- D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/Styles/DesignSystem.xaml
- D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/Resources/English.xaml
- D:/VPN-AJ/apps/windows/AJ.VPN.Desktop/Resources/Persian.xaml
- D:/VPN-AJ/src/AJ.VPN.Domain/Models.cs
- D:/VPN-AJ/src/AJ.VPN.Application/Contracts.cs
- D:/VPN-AJ/src/AJ.VPN.Application/ConnectionService.cs
- D:/VPN-AJ/src/AJ.VPN.Infrastructure/JsonSettingsService.cs
- D:/VPN-AJ/src/AJ.VPN.Infrastructure/SafeFileLoggerProvider.cs
- D:/VPN-AJ/src/AJ.VPN.Infrastructure/FoundationDiagnostics.cs

## 8. ServiceLib Integration Status — Partially Completed

Actual project reference compiles. D:/VPN-AJ/src/AJ.VPN.V2RayNAdapter/ServiceLibProfileValidator.cs invokes verified FmtHandler.ResolveConfig and ProfileItem.IsValid; converts to portable metadata and rejects TLS-bypass query flags before upstream loses them. Upstream logging is configured with no targets before user input is accepted. UnavailableCoreBackend explicitly prevents simulated core activation. AppManager initialization, real core lifecycle, profiles/subscriptions persistence and networking are not implemented due to unresolved storage isolation, provenance and cleanup prerequisites.

## 9. Build Results — Completed

Restore and Release build succeeded, zero warnings/errors. Local framework-dependent win-x64 publish succeeded: D:/VPN-AJ/artifacts/windows-x64/AJ.VPN.Desktop.exe. Development executable is unsigned and requires .NET 10 Desktop runtime. Build/test/publish scripts and Windows CI workflow created; remote CI not run.

## 10. Test Results — Completed within foundation scope

11 passed (3 unit, 8 integration cases), zero skipped/failed. WPF offline startup/render check passed across seven pages plus dark and Persian resources. NuGet reported no known vulnerable direct/transitive packages. Exact commands, evidence and untested areas: D:/VPN-AJ/docs/testing/VALIDATION.md. No real VPN success claimed.

## 11. Known Issues — Open

GitHub authentication missing. Core execution, saved profile import/selection, subscriptions, proxy ownership restoration, TUN, routing, traffic/public IP and automatic selection unavailable. Persian dynamic diagnostics/errors and option labels are not fully translated. Application control styling is original and Fluent-inspired, not a full Windows UI toolkit. Native/transitive license review, exact engine binary/data licenses, clean-machine deployment, interactive accessibility/DPI/multiple-monitor verification, signed packaging and crash recovery remain pending. Production readiness is not claimed.

## 12. Next Development Steps — Not Started except adapter parsing

Authenticate and verify/create the user's fork, retain exact stable pin. Add upstream storage isolation or a worker host. Implement DPAPI protected profile/subscription storage and transactional updates. Select and verify official engine binaries, generate validated configs through ServiceLib, own/monitor processes and prove proxied connectivity. Add ownership-aware System Proxy restoration with crash/concurrent-writer tests. Implement bounded proxy testing/ranking and real diagnostics. Validate TUN separately with explicit privilege action. Finish translation, accessibility, signing and packaging review.
