# SHAGHAD Phase 4 validation

## Executed automated checks

Release restore/build completed with zero warnings/errors. The final suite records **110 passed tests: 17 unit and 93 integration; zero failures/skips**. TRX and text logs are under `artifacts/Phase4FinalTestResults` and `artifacts/phase4-final-{restore,build,tests}.log`. The published core smoke test also passed all 23 native checks; its durable evidence is `docs/testing/evidence/phase4-native.json`.

New unit coverage includes lowest-latency ranking, failure/timeout/unsupported/cancellation exclusion, missing measurement exclusion, deterministic ties, empty/no-healthy scope, cached reuse/forced retest, five-minute monotonic expiry, profile/core/network/removal invalidation, bounded workers/cancellation, Manual policy precedence, Auto preference and favorites, search and latency sorting. Existing connection/compatibility/security tests continue to run.

New integration coverage starts actual trusted Xray jobs/listeners. Three simultaneous candidates have distinct PIDs and ports from the active backend; their owned PIDs exit and ports become unreachable after cleanup. The active PID, port, state, library selection and read-only Windows proxy snapshot remain unchanged. Cancellation after candidate readiness cleans its listener/process/configuration. Subscription replacement preserves stable ID/favorite/country/Auto mode, including changed display remarks. Actual DPAPI reload preserves favorite/Auto defaults and older schema defaults to Manual.

## UI

`--smoke-test` uses fresh private storage, not the normal profile library/preferences. The Phase 4 extension loads 100, 500 and 1000 visibly named offline fixtures, tests country search/favorites/filtering, and captures the real WPF app in four requested sizes × two themes × two languages. Recycling checks run before and after scrolling. The initial performance runs realized six rows for each library size. Per-server progress and real cancellation are captured. Auto's no-healthy case confirms Dashboard Connect does not start an untested profile.

The existing Dashboard/About matrix, management operations, native caption positions, embedded fonts/icons, eight pages and long-page scroll checks are retained. `artifacts/phase4-ui-release/phase4-server-layout-results.json` and screenshots contain the actual final run. Earlier failed runs are retained under separately named artifact folders; they found a deferred CollectionView reset error and an assertion that incorrectly treated recycled offscreen row controls as clipped toolbar buttons. Production collection replacement now uses one Reset notification and bounds checks distinguish the scrolling viewport.

## Operational Auto with controlled networking

`--auto-selection-self-test` creates its own trusted Xray VLESS server listening only on loopback with a clearly labelled synthetic UUID. Its server outbound forwards to actual public HTTPS measurement endpoints. The client imports that controlled route and an unreachable `.invalid` fixture into an isolated encrypted library, measures real TLS-verified HTTPS responses, chooses only the healthy profile, connects using Dashboard Auto and retests while connected.

Executed result: Auto resolved the healthy local fixture, Dashboard Auto HTTPS verification succeeded, an actual public exit IP was obtained, and Test All preserved the active PID, port and resolved profile. Read-only Windows proxy comparison was unchanged. The later full-scope retest measured **513 ms**, two successful requests, for the controlled local route; the unreachable candidate failed with no latency. This is an actual HTTPS measurement at test time, not an example or remote VPN provider result. Evidence: `artifacts/phase4-auto-controlled/result.json` and two native WPF captures. No Windows DNS/routes/TUN/proxy settings were changed.

## Authorized remote provider validation — completed for requested server

The user identified the German server `🇩🇪  𝔻𝔼『👑』`, then explicitly approved reading `D:\VPN-AJ\artifacts\phase3-completion-live\secrets\library.dpapi` and testing only its matching saved endpoint. Automatic approval review initially rejected the unapproved library/credential use; that command did not execute. The later approved run used `--authorized-auto-test` against the published Phase 4 package and found one matching profile. It imported only that profile into new isolated encrypted storage and never modified the source library.

Executed outcome: Auto resolved the actual healthy profile; Dashboard Auto connected and verified HTTPS; public exit IP retrieval succeeded; subsequent Test All preserved the active PID, listener, resolved ID and connected state. The read-only Windows proxy snapshot was unchanged. The final recorded retest measured **2,389 ms**, median of two successful TLS-verified HTTPS requests. No raw share link, credential, subscription URL, hostname or public exit address is included in JSON evidence. The country remained unknown: the remark's flag emoji is not verified geographic metadata. Scope is this requested matching server, not the entire provider subscription. Evidence: `artifacts/phase4-authorized-release/result.json` and `docs/testing/evidence/phase4-authorized-auto.json`. The server-page capture shows the earlier 2,459 ms measurement; measurements differ with time. Real Dashboard captures may include the actual public exit IP and remain in ignored local artifacts. The similarly named post-Test-All capture duplicates the Dashboard frame and is not used as distinct visual evidence; session preservation is verified by the test's PID/port/state assertions.

## Reproduction

Use the existing workspace SDK/scripts:

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/publish.ps1 -OutputDirectory artifacts/shaghad-phase4
# Use the installed Windows Desktop runtime or workspace .NET host:
./.tools/dotnet/dotnet.exe artifacts/shaghad-phase4/Shaghad.dll --smoke-test artifacts/phase4-smoke-reproduction
./.tools/dotnet/dotnet.exe artifacts/shaghad-phase4/Shaghad.dll --auto-selection-self-test artifacts/phase4-controlled-reproduction
```

The controlled self-test performs real external HTTPS networking; availability of the target/network is a prerequisite. It never changes system networking or contacts a provider using user credentials. Normal unit/integration tests use controlled fixtures. Published core self-test remains available for Xray/sing-box native protocol/lifecycle checks.

## Remaining limits

The requested German server passed at test time; this is not whole-subscription or protocol-matrix certification. Compatible real sing-box server coverage still requires an authorized compatible profile. Long-duration sleep/resume and packet-level DNS leak tests are not performed. Network events may not detect DNS-only changes. Screenshots cover current host DPI, not every Windows environment, assistive technology or caption gesture. Full-scope Auto can take time for very large libraries; progress is cancellable, and cache expiration during the scope prevents claiming a global winner from stale results. A signed installer/production certification is outside this publication. No automatic failover, TUN or automatic system networking change was implemented.
