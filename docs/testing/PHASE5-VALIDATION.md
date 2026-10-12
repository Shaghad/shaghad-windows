# Phase 5 validation — 2026-10-12

Version: 0.5.0-beta.1. This is an unsigned development prerelease, not production certification.

## Completed

- `scripts/build.ps1`: actual restore and Release build completed, zero warnings/errors.
- `scripts/test.ps1`: 122 passed, zero failed/skipped (29 unit, 93 integration). Original 110 tests retained; 12 new cases cover native counter parsing, unavailable telemetry, monotonic rates, reset/restart/overflow, quality, opt-in/Manual policy, consecutive failures, ranking, cooldown, exclusions and attempt budget.
- Native core matrix: 25 checks passed using Xray 26.3.27 and sing-box 1.14.3; includes both native telemetry responses, hash/version verification, protocol syntax, duplicate start preservation, graceful stop, unexpected exit, cancellation, invalid configuration, listener collision and real startup timeout.
- WPF smoke: eight pages, English/Persian, dark/light, 32 Dashboard/About shown-window checks at 1024x600, 1280x720, 1366x768 and 1920x1080. No clipping/scrolling in that matrix, LTR native shell/caption controls, fixed column positions, embedded fonts/icon, drawer focus open/close, server/subscription management and intentionally unreachable server readiness without false internet verification.
- Controlled loopback VLESS fixture: actual HTTPS verified, actual proxied IP measured, native upload/download counters increased after real requests, Auto resolved a real healthy profile and Test All preserved active PID/port. Windows proxy settings were unchanged. Captures are actual native WPF window renders; the test is explicitly labeled as a controlled local fixture, not provider connectivity.
- NuGet direct/transitive vulnerability query: no known vulnerable packages reported by configured NuGet sources at query time.
- Git current source/history pattern audit: reviewed public commit URL, scanner expression and intentionally invalid inputs. Exact path/rule/line-content hashes permit only those individually inspected false positives; unreviewed references stop the release gate. Historical flagged lines matched the inspected current lines. No secret values are emitted in audit reports.

## Package validation

The portable release is self-contained Windows x64. Required core binaries and upstream licenses are included; both binaries match the compiled trust manifest. Packaging rejects user settings, private runtime configs/logs and test artifacts, excludes symbols, verifies version metadata and generates/rechecks SHA256SUMS.txt. Final extracted-package results are recorded in the Phase 5 development report.

## Pending / limitations

Clean-machine VM and Windows 10 certification are pending; launching extracted files on the development host is not clean-machine validation. No real provider profile was used in this phase. Full remote protocol/transport matrix, physical suspend/resume, end-to-end failover under real outages, System Proxy ownership transfer and interactive Narrator/DPI testing remain pending. The compact matrix has disconnected/no cached-candidate data; three populated candidate rows at minimum size need additional validation. Installer compiler/signing certificate unavailable. No GitHub CI run or publication occurred without valid authenticated write access.

DNS/IP-family diagnostics measure target HTTPS responses through the proxy, not packet-level resolver paths. Packet capture/leak validation is a separate release gate. No OS DNS/firewall/routes/drivers/TUN changes were performed. No Kill Switch claim is made.

Final ZIP validation completed: extracted self-contained Shaghad.exe passed WPF/matrix, 25 native core checks on fresh retry, and actual HTTPS/IP/counter-increase fixture checks. One initial native run hit an unexplained Windows executable-sharing violation 32; retained in the development report. YamlDotNet workflow parse/action-pin checks passed. Eight actual native screenshot files are preserved under docs/design/screenshots/phase5.
