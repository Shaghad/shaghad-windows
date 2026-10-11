# Verified ServiceLib integration contract

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

There is no CoreConfigManager.cs at this revision; CoreConfigContextBuilder and CoreConfigHandler are the actual configuration boundary. Foundation invokes upstream ResolveConfig for explicit user validation of supported share URIs, with no AppManager initialization, DB writes or network changes. Core execution remains disabled until binary trust, storage isolation, lifecycle monitoring and proxy restoration tests exist.

Future worker-host isolation is preferable for upstream singleton lifecycle. A narrow reviewed upstream patch should add a storage root; never point VPN-AJ at a user's existing v2rayN data. TUN needs explicit privilege consent, adapter/driver verification, DNS/routes snapshots and failure cleanup. Never use upstream auto-elevation from startup.
