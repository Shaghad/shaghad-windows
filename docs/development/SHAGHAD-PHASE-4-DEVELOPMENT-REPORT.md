# SHAGHAD PHASE 4 DEVELOPMENT REPORT

Date: 2026-10-11. Windows x64 development release: **0.4.0**. The requested Servers manager and operational Auto/Manual selection are implemented, tested and published locally. This report describes completed observations, not production certification.

Status vocabulary: **COMPLETED** means implemented and verified within the stated scope; **PARTIALLY COMPLETED** identifies a remaining coverage gap; **BLOCKED** identifies an unresolved prerequisite; **NOT STARTED** identifies work outside this phase. There are no unresolved blockers to the local Phase 4 delivery.

## 1. Initial Architecture Findings — COMPLETED

The existing WPF/.NET 10 solution already separated Domain, Application, Infrastructure and the pinned v2rayN ServiceLib adapter. DPAPI storage, trusted core hashes, owned processes and verified connection logic were retained. The old Servers form and Test All path needed replacement: candidate testing must not reuse the active connection backend or mutate the selected profile. The upstream gitlink remains `7d6a967c18c697f28dc6917122ed3a4993fcf336` (v2rayN 7.25.4); upstream source was not modified.

## 2. Servers UI Redesign — COMPLETED

A pinned Auto — Lowest Latency card sits above a searchable, filtered, sorted server list. A finite viewport and recycled WPF rows replace the nested scrolling/form layout. Rows show colorful local country flags or a neutral globe, protocol/core compatibility, real health/latency, favorite and connect controls, plus contextual actions. Imports are modal. Slim scrollbars, light/dark resources and English/Persian labels preserve SHAGHAD branding, the left sidebar and native caption placement. See [design decisions](../design/SERVER-PAGE-REDESIGN.md).

## 3. Auto Selection Architecture — COMPLETED

Auto is a persisted policy, not a fabricated server profile. Dashboard Connect resolves a fully evaluated compatible scope, excludes unsuccessful or missing measurements and chooses lowest measured latency with deterministic ID tie-breaking. Manual respects the explicit active profile. No healthy candidate means no connection. Five-minute monotonic cache entries include profile configuration, core, endpoint and network revision; forced retest invalidates the whole scope before workers run. A scope whose results expire during a long run fails closed. Cooldown/improvement settings are reserved architecture, not automatic switching. See [selection architecture](../architecture/AUTO-SERVER-SELECTION.md).

## 4. Latency Testing Implementation — COMPLETED

Each candidate gets a unique temporary root, one-profile snapshot and a new owned core backend. Up to three workers run concurrently. TLS-verified HTTPS requests to `https://www.gstatic.com/generate_204` traverse the candidate proxy; Stopwatch measures response time. Two successful attempts produce their median; one is Degraded; no successful response has no latency. Startup is bounded to 15 seconds and each request to five seconds. Progress and cancellation clean up owned jobs, listeners and configuration. This is endpoint-specific HTTPS latency, not raw ping or throughput. See [health testing](../architecture/SERVER-HEALTH-TESTING.md).

## 5. Core Compatibility Handling — COMPLETED; remote matrix PARTIALLY COMPLETED

Existing capability validation excludes unsupported candidates before core execution. Both published pinned cores passed native protocol/configuration checks. Actual requested remote connectivity was verified through Xray; an authorized compatible real sing-box provider profile was not tested. The client never bypasses compatibility to create an Auto fallback.

## 6. Favorites and Search — COMPLETED

Favorites and Auto/Manual survive encrypted reload. Search includes display name, protocol and country names/codes. Health/favorites filters and six sort orders treat failures and untested results as missing latency, never zero. Subscription replacement preserves stable IDs, favorites, country and selection by opaque wire identity even when remarks change. Countries require explicit metadata; a name emoji or hostname never establishes location. Bundled flag assets retain their MIT notice.

## 7. Dashboard Integration — COMPLETED

Dashboard displays the chosen policy and actual resolved connected profile. Its connected profile snapshot is independent of list edits and Test All results. Connection still uses the existing verified connection service. Selecting another server requires an explicit reconnect flow; no background failover was added. Actual exit IP is obtained through the active proxy, not candidate testing.

## 8. Process Isolation Validation — COMPLETED

Integration checks ran three candidates with distinct PIDs and ports while an active trusted Xray session remained running. The original PID, port, connection state, library selection and read-only Windows proxy snapshot stayed unchanged. Candidate processes, listeners and generated configuration were removed. Cancellation after readiness also cleaned up. The authorized real-provider Test All repeated the active-session preservation checks successfully. These checks did not enable system proxy, modify DNS/routes or implement TUN.

## 9. Changed Source Files — COMPLETED

| Area | Files and purpose |
| --- | --- |
| Domain | `src/AJ.VPN.Domain/Models.cs`: policy, favorites, health/results |
| Application | `Contracts.cs`, `LibraryService.cs`, new `ProfileIdentity.cs` and `ServerSelectionService.cs`: persistence, wire identity, bounded testing/ranking/browser behavior |
| Infrastructure | `ProtectedFiles.cs`, new `IsolatedCandidateTester.cs`: schema compatibility, isolated core testing and network revision |
| Desktop | `apps/windows/AJ.VPN.Desktop/App.xaml.cs`, `MainWindow.xaml(.cs)`, `ViewModels/ShellViewModel.cs`, `ShellViewModel.Premium.cs`: composition, manager UI and policy integration |
| Desktop assets | `Controls/ServerRowView.xaml(.cs)`, `CountryFlags.cs`, `Assets/Flags`, English/Persian resources: reusable rows, local flags and localization |
| Validation | `SmokeTest.cs`, `AutoSelectionSelfTest.cs`, `AuthorizedAutoSelectionTest.cs`, unit `ServerSelectionTests.cs`, integration `IsolatedServerTestingTests.cs` |
| Packaging | Desktop `.csproj` version/resources, `scripts/publish.ps1`, `scripts/export-flags.cjs`, flag license and Phase 4 documentation/evidence |

Paths in the Application/Infrastructure rows are relative to their corresponding projects; test paths are under `tests`. Existing unrelated work and commits were preserved. Git publication status: local uncommitted changes; no commit, push, force-push or remote PR was performed.

## 10. Build Results — COMPLETED

Release restore/build completed; final build reports **zero warnings and zero errors**. Existing scripts published `artifacts/shaghad-phase4/Shaghad.exe` with its DLLs, Xray 26.3.27, sing-box 1.14.3 and required legal notices. Both core hashes match the pinned manifest. The portable ZIP is `artifacts/SHAGHAD-Phase4-win-x64.zip`. This is a framework-dependent x64 Windows package requiring .NET 10 Windows Desktop Runtime. Package instructions and SHA-256 checksums accompany the output.

## 11. Automated Test Results — COMPLETED

**110 tests passed: 17 unit and 93 integration; zero failures or skips.** All **23 native core checks passed**. New coverage includes ranking, exclusions, ties, no healthy scope, cache expiry/invalidation, forced-cancellation invalidation of queued results, concurrency/cancellation, Manual precedence, search/sort/favorites, subscription identity and live process isolation. Logs and TRX: `artifacts/phase4-final-build.log`, `phase4-final-tests.log`, `Phase4FinalTestResults`. Durable native/UI/operational JSON evidence is under `docs/testing/evidence`. See [validation details](../testing/SERVER-SELECTION-VALIDATION.md).

## 12. Real Connectivity Results — COMPLETED for approved scope

The controlled loopback VLESS route performed real proxied HTTPS, measured 513 ms on its later retest, and excluded an unreachable fixture. This demonstrates the mechanism, not a remote VPN provider.

The user explicitly approved reading the exact existing encrypted library and testing only the matching German server. The published application found one match, selected it through Auto, connected from Dashboard, verified HTTPS and retrieved a public exit IP. Subsequent Test All preserved the active PID/port/resolved ID/state. The source encrypted library and Windows proxy snapshot remained unchanged. The final isolated retest measured **2,389 ms**, with two successful HTTPS requests. Source profiles, credentials, hostnames and exit addresses are absent from durable JSON. This certifies the observed requested server at test time, not the whole subscription or future availability.

## 13. Screenshot Evidence — COMPLETED within host coverage

The real running WPF app was captured at four requested sizes in both themes and both languages (16 Servers layouts), alongside the existing 32 Dashboard/About matrix. The 100/500/1000-server checks initially realized six rows each; scrolling continued to recycle rows. Final evidence comes from `artifacts/phase4-ui-release`; selected captures are under `docs/design/screenshots/phase4`. Offline performance fixtures are visibly labeled. The approved remote server-page capture is genuine and displays an earlier 2,459 ms result; the later JSON retest is 2,389 ms. Real connected Dashboard evidence stays in ignored artifacts because it contains the measured public IP. A duplicated post-Test-All Dashboard frame is not counted as independent visual proof.

## 14. Security Findings — COMPLETED within performed audit

DPAPI library storage and trusted core verification remain intact. Candidate roots and processes are privately owned and cleaned; tests do not alter system networking or contact unapproved saved profiles. The source approved library was read only. Initial automatic review rejected library use before explicit authorization; that action did not execute, and later explicit approval resolved it. The scoped current/history audit found public reference URLs and synthetic test/security patterns, not newly introduced credentials. Imported encrypted libraries and runtime evidence remain ignored; no secrets were committed. No upstream code was changed. This is a scoped audit, not an independent security certification.

## 15. Remaining Limitations — PARTIALLY COMPLETED / NOT STARTED

**PARTIALLY COMPLETED:** remote protocol/core coverage is limited to the requested Xray profile; screenshots cover this host's DPI; existing dynamic service messages are not all localized; DNS-only changes may not emit the observed network events. Large full scopes can outlive the five-minute freshness window and correctly return no winner. Existing core traffic telemetry remains unavailable rather than invented.

**NOT STARTED:** long-duration sleep/resume validation, packet-level leak testing, comprehensive Narrator/multi-monitor/DPI checks, clean-machine deployment, signed installer and production certification. TUN and automatic failover are excluded from this phase by the user's request.

**BLOCKED:** none for this local delivery. Broader real sing-box coverage requires an authorized compatible endpoint. No success is claimed for these remaining areas.
