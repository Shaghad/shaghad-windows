# Verified ServiceLib integration contract

## Phase 3 compatibility and diagnostics contract

`IProfileValidator.CoreCompatibilityError` provides a controlled error for requirements the selected engine cannot preserve. The concrete ServiceLib validator runs this before library core/profile assignment and again in `ServiceLibConfigurationGenerator`. UI core selection surfaces the same actionable message. Rejection never strips encryption, changes transport/security parameters or initializes the upstream application singleton. The existing storage transaction publishes a snapshot only after save succeeds.

Actual authorized VLESS profiles require Xray ML-KEM VLESS Encryption. Xray writes the original `VlessEncryption` into the native outbound; pinned sing-box lacks that field/capability. The integration therefore supports these profiles through Xray and refuses sing-box. Native core syntax acceptance is a separate layer and cannot prove remote security equivalence. See `docs/testing/SINGBOX-COMPATIBILITY.md` for normalized field comparison and failure-stage evidence.

`ProxyConnectivityProbe` has fixed explicit-proxy endpoints for HTTP, certificate-validated HTTPS and family-specific public exit measurements. There is no direct fallback, ambient proxy credential, cookie or redirect handling. `LocalProxyBackend` caches only current-session measured outcomes/public address and sanitized errors. Disconnect/core exit invalidates current measurements. `FoundationDiagnostics` displays these values and explicitly leaves packet-level DNS path/leak claims unverified. Target-domain forwarding is tested at HTTP CONNECT/SOCKS5 boundaries; server bootstrap still uses local DNS. No host DNS settings are managed.

Owned process output never goes to disk/UI. Bounded in-memory inspection maps known patterns to fixed categories; unknown stages remain unknown. `SafeFileLoggerProvider` still writes event metadata only. A late exit notification is guarded by connection generation; an explicit retry cleans dead resources before restarting. Core-crash recovery never changes profile or implements failover. Windows proxy ownership restoration remains separate and unchanged; this task did not mutate OS proxy settings.

The explicit connectivity harness accepts either an authorized encrypted test-library directory or a private subscription request file, uses a new isolated output directory, tests representative protocol/transport combinations and reports unsupported combinations separately. It deliberately kills only its owned core PID for crash validation. Required check failures produce nonzero exit; IPv6 endpoint failures are recorded without declaring a core defect. System Proxy tests require the separate procedure and approval in `docs/testing/WININET-INTERACTIVE-VALIDATION.md`.

Source: external/v2rayN/v2rayN, tag 7.25.4, commit 7d6a967c18c697f28dc6917122ed3a4993fcf336.

Actual APIs inspected:

| Component | Actual entry points | Adapter requirement |
|---|---|---|
| AppManager | Instance.InitApp(), InitComponents(), Config, GetProfileItem(string), ProfileItems(string), AppExitAsync(bool) | singleton storage/UI/global state; isolate initialization |
| ConfigHandler | LoadConfig(), SaveConfig(Config), AddBatchServers(Config,string,string,bool), SetDefaultServerIndex(Config,string), GetDefaultServer(Config) | upstream persistence is not secret-encrypted |
| FmtHandler | ResolveConfig(string,out string), GetShareUri(ProfileItem) | convert only safe metadata; do not expose credentials |
| CoreConfigContextBuilder | Build(Config,ProfileItem) | returns context and validation result; depends on AppManager DB/DNS/routing |
| CoreConfigHandler | GenerateClientConfig(CoreConfigContext,string?) | Xray/sing-box generators; validate before applying |
| CoreManager | Instance.Init(Config,Func<bool,string,Task>), LoadCore(CoreConfigContext?,CoreConfigContext?), CoreStop() | LoadCore has no cancellation or typed readiness return; callback is not proof of connectivity |
| CoreInfoManager | GetCoreInfo(ECoreType), GetCoreExecFile(CoreInfo?,out string) | resolve binaries only after provenance verification |
| SubscriptionHandler | UpdateProcess(Config,string,bool,Func<bool,string,Task>) | no CancellationToken; exception messages may expose URLs; do not forward raw callbacks |
| SpeedtestService | constructor(Config,Func<SpeedTestResult,Task>), RunLoop(ESpeedActionType,List<ProfileItem>,CancellationToken), ExitLoop() | distinguish TCP reachability from real proxy tests; bound concurrency |
| ProcessService | StartAsync(string), StopAsync(), HasExited, Id, Handle | retain handles for owned processes; never kill by image name |
| SysProxyHandler | UpdateSysProxy(Config,bool) | forced clear is insufficient for prior-state restoration |

There is no CoreConfigManager.cs at this revision. Phase 2 uses `CoreConfigV2rayService.GenerateClientSpeedtestConfig(int)` and `CoreConfigSingboxService.GenerateClientSpeedtestConfig(int)` with an explicitly constructed `CoreConfigContext`, AppConfig and node CoreType. These public single-profile generators bypass AppManager database/config lifecycle. SHAGHAD narrows the generated template to one selected outbound, one loopback mixed inbound and no TUN, API, direct fallback, downloaded rules or file logs. Each result must also pass the selected native core's configuration checker.

`D:/VPN-AJ/src/AJ.VPN.V2RayNAdapter/ServiceLibConfigurationGenerator.cs` is the configuration boundary; `D:/VPN-AJ/src/AJ.VPN.Infrastructure/LocalProxyBackend.cs` owns native lifecycle. AppManager/CoreManager/SubscriptionHandler/SysProxyHandler lifecycle is not initialized: it has global storage, uncancellable operations and proxy-clear semantics unsuitable for this phase. Subscription share-link parsing reuses FmtHandler through LibraryService; download limits/SSRF controls/encrypted transactions are owned by SHAGHAD. No upstream modifications were needed.

Native dependencies: Xray 26.3.27 and sing-box 1.14.3 Windows x64. Run `D:/VPN-AJ/scripts/install-cores.ps1 -Download`, then build/publish. Trust anchors are compiled from `D:/VPN-AJ/src/AJ.VPN.Infrastructure/core-manifest.json`; editable sidecars cannot authorize another binary. Missing, tampered, wrong-version, rejected-config and startup-exit states fail closed. Transport support varies: sing-box rejects XHTTP/KCP; the adapter rejects obsolete H2/QUIC, unknown/case-mismatched transports and ambiguous duplicate query parameters before upstream can silently fall back to raw TCP. Unknown security modes are rejected before TLS generation. Basic TCP/TLS variants of all four protocols passed native checks; the full transport matrix is not claimed.

TUN remains separate work requiring explicit privilege consent, adapter/driver verification and DNS/routes ownership. No startup elevation or driver installation occurs.
