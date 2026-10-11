# Phase 3 completion audit — 2026-10-11

## Current result

The supplied VLESS profiles are supported through Xray without changing their required ML-KEM VLESS Encryption. The former sing-box failure is explained and incompatible assignments are rejected before execution. The installed sing-box cannot implement this encryption; no security downgrade or core substitution is performed. This resolves the application compatibility defect, **not** a claim that these profiles now work inside sing-box. No TUN, automatic server selection, failover or Windows proxy changes were implemented.

## Executed validation

| Command / check | Actual result |
|---|---|
| Workspace SDK Release build of `VPN-AJ.slnx`, `--no-restore --property:OutputPath=bin/Phase3Verified/` | Success; zero warnings/errors; isolated outputs preserve running app |
| Release tests with matching output property, TRX at `artifacts/Phase3FinalTests` | **92 passed: 3 unit + 89 integration; zero failed/skipped** |
| `scripts/core-smoke-test.ps1 -AppPath apps/windows/AJ.VPN.Desktop/bin/Phase3Verified/Shaghad.dll` | **23 native checks passed**; wrapper exit 0; both verified cores and all four synthetic protocol configs |
| `scripts/connectivity-test.ps1 -AuthorizedSource D:/VPN-AJ/artifacts/phase3-connectivity -AppPath apps/windows/AJ.VPN.Desktop/bin/Phase3Verified/Shaghad.dll` | Wrapper exit 0 for required checks; **53 observations: 43 pass, six IPv6 endpoint failures, four unsupported sing-box combinations** |
| Final equality/preservation audit using `--core-semantics-audit` | Exit 0; all 16 authorized profiles retain their Xray encryption and reject incompatible sing-box assignment |
| Final Release WPF `--smoke-test` with isolated preferences | Eight pages and 32 layout combinations passed; updated Diagnostics renders; no live system proxy changes |
| GitHub API and `git ls-remote` with network access | Authenticated as Shaghad; repository identity and push=true verified; default branch main; no publication |
| `scripts/secret-audit.ps1` | 131 current text files and 222 historical text blobs across nine reachable commits inspected; nine value-suppressed findings manually reviewed (scanner expression, invalid/synthetic fixtures and public source-reference URLs) |

Evidence: `artifacts/Phase3FinalTests/*2026-10-11_22_14_45*.trx`, `artifacts/core-check-20261011-220930/result.json`, `artifacts/connectivity-640f79d0e14d4f94805918a9be455eb5/result.json`, `artifacts/phase3-final-semantics.json`, `artifacts/phase3-verified-smoke/result.txt`. Sanitized durable copies are under `docs/testing/evidence/phase3-*`. A follow-up current-build run at `artifacts/phase3-public-diagnostics/result.json` repeated the same 53 outcomes and confirmed a measured public exit IP without recording its value; this is the durable connectivity copy. Public-IP parsing consumes the complete bounded response, rejects private/reserved/mapped-loopback addresses and rejects shorthand IPv4. Earlier attempts include a local TLS fixture timeout: the fixture failed to serve client retries, was repaired, then the targeted test and full suite passed. No certificate trust settings were modified.

## Meaning and remaining limits

Actual Xray HTTP/HTTPS/IPv4 requests passed through HTTP and SOCKS5 on representative HTTPUpgrade, WS, raw TCP and XHTTP profiles. XHTTP also passed IPv6; the other representatives failed IPv6 checks with unconfirmed endpoint/provider/network causes. This is not proof of a core defect or universal lack of provider IPv6. Server bootstrap DNS resolved IPv4-only answers for these representatives. Proxy DNS wire-contract tests prove domain forwarding; packet-level DNS leak absence remains unverified.

The original Xray profile passed manual retry, active-probe cancellation, pre-cancelled startup, deliberate owned-core crash detection/cleanup and manual reconnect with real HTTPS verification. Synthetic native tests additionally cover duplicate starts, occupied ports, native config failure, cancellation and real startup timeout. A controlled loopback TLS test rejects an untrusted target certificate through HTTP CONNECT.

No real VMess/Trojan/Shadowsocks profile was supplied. Real compatible sing-box remote connectivity remains unverified; native fixtures are not remote coverage. Outer TLS/REALITY/uTLS/ALPN combinations absent from the supplied profiles, packet-level DNS attribution, full server inventory, sustained load, sleep/resume, interface changes, clean-machine deployment and live browser/WinINet behavior remain unverified. The earlier interactive session was stopped by the user; this task used WPF smoke rendering, not further computer-use automation.

Git was master at 2489447, matching remote master when checked; remote main was bd7adb86e103f75182d58a6ed47d7b45a1ab155a and was not overwritten. Existing working changes were preserved. Upstream gitlink remains 7d6a967c18c697f28dc6917122ed3a4993fcf336; no upstream source edit. Secret scanning is pattern-based, includes reachable textual history and commit messages, excludes upstream history/ignored private data/binary assets, and is not mathematical proof of secret absence. No commit, push or source publication was performed in this task.

See `SINGBOX-COMPATIBILITY.md`, `CONNECTIVITY-MATRIX.md` and `WININET-INTERACTIVE-VALIDATION.md`. **The full production readiness gate remains open for the explicitly unverified cases.** TUN/failover work has not started.

## Historical initial Phase 3 attempt

The following earlier report is retained as history. Its authentication, retry and root-cause limitations are superseded by the completion audit above.

## Phase 3 — Production connectivity validation, 2026-10-11

The user supplied an authorized HTTPS subscription and explicitly kept live system proxy tests pending. The subscription token, raw links, profile names and hosts are omitted from this report. No source publication, force push, WinINet/WinHTTP, system DNS or routing changes were performed. No TUN or Auto Failover was implemented.

### Git and credential checks

Configured repository: https://github.com/Shaghad/shaghad-windows.git, remote Aj-VSCode. Current branch master at af0f8a8 is one commit ahead of the locally cached Aj-VSCode/master (d160d80). Existing extensive uncommitted Phase 2 work was preserved. GitHub CLI reports the configured token invalid; repository API and ls-remote failed with network/DNS errors in restricted execution. Actual repository identity and push permission remain unverified. Normal status/diff checks cannot enter submodule metadata under the sandbox; status with --ignore-submodules works. HEAD gitlink remains 7d6a967c18c697f28dc6917122ed3a4993fcf336. Upstream source was not edited.

A value-suppressed credential marker scan examined 137 tracked/nonignored candidate files outside upstream: zero findings for GitHub/AWS/private-key markers or the supplied subscription host/token. This is a limited scan, not certification of all secrets or history. Private request input and encrypted test library are under ignored artifacts; nothing was published.

### Executed results

- Release solution build succeeded with zero warnings/errors. Final build uses --property:OutputPath=bin/Phase3/ because the interactive app locked the normal Release output. An intermediate normal-output rebuild failed on that lock.
- Automated tests: 61 passed (3 unit, 58 integration), zero failed/skipped. Added tests cover sanitized error categories, real refused loopback connections for HTTP/SOCKS5 and cancellation. TRX evidence: artifacts/Phase3TestResults.
- Existing WPF smoke test passed eight pages and 32 layout combinations, fonts, icons, tray callback and license viewer. This is automated render evidence, not full interactive coverage.
- Production harness downloaded and validated the authorized subscription in isolated encrypted storage. Only VLESS was present. It selected one profile per protocol, not every subscription server.
- Xray VLESS: actual core startup/shutdown passed; HTTPS through HTTP CONNECT and SOCKS5 returned the expected 204 with normal TLS verification enabled.
- Sing-box VLESS: core startup/shutdown passed; both HTTPS proxy requests failed in the secure-connection category. The precise certificate/transport cause is unconfirmed. TLS verification was not bypassed.
- Sanitized network evidence: artifacts/phase3-connectivity/result.json. Raw process output is discarded; event logs retain metadata only.
- Windows interactive inspection confirmed the Release window, disconnected state, disabled HTTPS test before connection and real service-bound controls. Navigation attempts encountered geometry errors; a subsequent attempt was stopped by the user with Escape. No further computer-use actions were performed. Interactive navigation/browser coverage is incomplete.

### Remaining unverified

VMess/Trojan/Shadowsocks need authorized profiles absent from the supplied subscription. Plain HTTP request success, DNS resolution path/leaks and IPv4/IPv6 remote egress were not separately measured. Successful HTTPS does not prove these. A manual recovery retry was added to the harness after the first run but has not yet been executed. Real negative-certificate enforcement, long-running stability and all subscription profiles remain unverified. Live WinINet enable/disable, browser traffic, registry restoration, crash recovery and external modification tests remain pending at the user's request; existing in-memory tests are not live OS proof. Production readiness is not claimed.
# Validation — 2026-10-11

## Phase 2 — Current verified results

Windows x64, .NET SDK 10.0.401, existing nine-project solution and pinned ServiceLib 7.25.4. Existing Concept C branding, internal namespaces and upstream source are preserved.

| Executed command | Actual result |
|---|---|
| `D:/VPN-AJ/scripts/install-cores.ps1` | Official staged archives and executable hashes matched pinned values; installed Xray 26.3.27 and sing-box 1.14.3 with LICENSE files |
| `D:/VPN-AJ/scripts/build.ps1` | Restore and Release build succeeded; zero warnings/errors |
| `D:/VPN-AJ/scripts/test.ps1` | **54 passed: 3 unit + 51 integration; zero failed/skipped** |
| `D:/VPN-AJ/scripts/core-smoke-test.ps1 -Published` | **23 native checks passed** on the published executable's core/config/process implementation; exit code 0 |
| `D:/VPN-AJ/scripts/smoke-test.ps1` | WPF startup/DI, eight pages, Light/Dark, Persian RTL, Vazirmatn, seven ICO frames, actual tray About callback and license viewer passed |
| `D:/VPN-AJ/scripts/publish.ps1` | Framework-dependent win-x64 publish succeeded at `D:/VPN-AJ/artifacts/shaghad-windows-x64/Shaghad.exe`; both verified cores, their licenses and legal docs copied |
| `dotnet list VPN-AJ.slnx package --vulnerable --include-transitive` using workspace SDK | NuGet reported no known vulnerable managed packages across all nine projects; not a native binary/code/license audit |
| `git diff --check`, `git submodule status` | No whitespace errors; upstream pin remains 7d6a967c18c697f28dc6917122ed3a4993fcf336 / 7.25.4 |

Final automated test evidence: `D:/VPN-AJ/artifacts/TestResults/Victus_AJ-VICTUS_2026-10-11_20_57_29_net10.0.trx` and `D:/VPN-AJ/artifacts/TestResults/Victus_AJ-VICTUS_2026-10-11_20_57_29_net10.0[1].trx`. Published native evidence is recorded in `D:/VPN-AJ/docs/testing/evidence/phase2-native.json`, copied from `D:/VPN-AJ/artifacts/core-check-20261011-210133/result.json`. The self-test also rejected an existing nonempty output directory before writing anything; its sentinel file remained unchanged. WPF evidence: `D:/VPN-AJ/artifacts/shaghad-smoke/result.txt` and page PNGs; curated renders are under `D:/VPN-AJ/docs/testing/evidence/phase2-*.png`.

### What was actually exercised

- Real upstream parsing of all four protocols; generated configurations have exactly one selected outbound and loopback inbound. Unsupported transport/TLS bypass/malformed data are rejected. Batch import and base64 subscription replacement preserve previous data/active share link on parse/save failures.
- Actual Windows DPAPI roundtrip, plaintext absence in the encrypted file, tamper rejection and cancelled-write preservation. Missing/tampered executable rejection occurs before execution; payload-free logger behavior remains tested.
- HTTPS URL and public-address policy tests cover unsafe schemes, embedded credentials, fragments, private/link-local/loopback/mapped IPv6/documentation addresses and allowed public addresses. Subscription transaction tests use an injected downloader; no real provider URL was contacted.
- Proxy tests use an **in-memory settings backend**, not Windows registry writes: denied consent, absent/type/manual/PAC/discovery/binary roundtrip, crash recovery consent, external changes before/during/after apply, partial write failures, cancellation rollback, idempotence and unknown-format rejection.
- Actual native executables check all four protocol configs in both engines. Tests start real PID-owned loopback mixed listeners, complete a SOCKS5 greeting, reject duplicate starts without stopping the original process, gracefully stop and release ports, prevent cancelled starts, detect a killed owned core, reject invalid configs, detect occupied-port startup exits, and enforce a real ten-second Xray readiness timeout. All self-test configs are removed. Fixture hosts are reserved `.invalid` domains; credentials are deliberately nonfunctional synthetic test values.
- WPF renders are generated from the real views and services with an empty isolated library. Light, dark, Servers and Persian layout were visually inspected. These are **offline WPF renders, not native desktop screenshots or design mockups**. A normal app instance's actual UI Automation tree confirmed version 0.2, disconnected/unverified state, navigation and service-bound controls. An occluding foreground window prevented a useful native screenshot; it was not represented as SHAGHAD evidence. The inspection app exited through its normal close path.

### Corrections discovered through validation

Native and automated checks found and fixed VMess base64 handling (it is not a normal URI authority), upstream TCP→raw transport normalization, upstream fallback from unknown/obsolete/case-mismatched transports, ambiguous duplicate query parameters, unknown security modes that could otherwise lose TLS, duplicate-start cleanup affecting the active process, and transient post-exit executable sharing violations. Restricted standard-handle inheritance was added to prevent unrelated handles/pipes reaching a core. The expanded native matrix took about 67 seconds on one run: the original 60-second wrapper timed out, while its result file later showed all checks passed. The wrapper now uses one-second waits with a 120-second total bound and kills only its own self-test process on expiry; the final published-package invocation exited successfully.

### Explicitly not validated / not performed

No valid user profile or subscription URL was supplied. **Real remote authentication, proxy HTTPS success, real latency, provider updates and production readiness remain unverified.** No System Proxy, DNS, routes, adapters, WinHTTP or other system networking settings were changed. Native WinINet/PAC/policy/crash/logoff behavior requires explicit user-approved testing; automated restoration tests prove the ownership policy, not live OS effects. The full transport/cipher/REALITY matrix, HTTP downloader adversarial/live-provider behavior, long-running stability, ARM64, clean-machine Windows deployment, signing/installer migration, taskbar/tray shell interaction, full accessibility/DPI and complete Persian translation remain open. There is no TUN implementation, fake connected state, sample server in the normal library or committed real credential. Remote CI was not run.

## Historical foundation validation

The remaining sections are the original foundation record; unavailable-core and untested-proxy statements there describe that earlier milestone.

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
