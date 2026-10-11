# v2rayN source analysis

Verified 2026-10-11 through https://api.github.com/repos/2dust/v2rayN/releases/latest: latest stable 7.25.4. Checked-out tag commit: 7d6a967c18c697f28dc6917122ed3a4993fcf336. Paths below are relative to external/v2rayN/v2rayN. This is a source review; upstream tests and live VPN behavior have not been validated.

| Area | Source and findings | Reuse decision |
|---|---|---|
| Startup | v2rayN/App.xaml.cs: per-executable EventWaitHandle, AppManager.InitApp, WindowDialog, InitComponents, ReactiveUI builder, MainWindowViewModel | original desktop startup; do not reuse UI |
| Persistence | ServiceLib/Handler/ConfigHandler.cs LoadConfig/SaveConfig; AppManager creates SubItem/ProfileItem/ServerStatItem/RoutingItem/ProfileExItem/DNSItem/FullConfigTemplateItem SQLite tables | path isolation and encrypted secrets needed |
| Initialization | MainWindowViewModel.Init initializes ProfileExManager, CoreManager, CertPemManager and StatisticsManager, then Reload | reproduce required sequence in adapter only after isolation |
| Core configuration | Handler/Builder/CoreConfigContextBuilder resolves immutable context, DNS/routing and validators; CoreConfigHandler dispatches generators | reuse with typed error translation |
| Xray | Services/CoreConfig/V2ray/CoreConfigV2rayService and Services/Statistics/StatisticsXrayService; CoreInfoManager identifies executable/arguments | reuse config generation, engine remains external |
| sing-box | Services/CoreConfig/Singbox/CoreConfigSingboxService, sample DNS/TUN resources, StatisticsSingboxService | reuse supported options after version checks |
| Process lifecycle | CoreManager.LoadCore generates config, CoreStop, starts main and optional pre-core; ProcessService uses System.Diagnostics.Process and string arguments; WindowsJobService owns handles | readiness/exit monitoring, safe arguments and cancellation adapter needed |
| Subscriptions | SubscriptionHandler.UpdateProcess downloads main/additional URLs, optionally uses conversion service, then imports through ConfigHandler; exceptions logged | disallow external conversion by default; redact callbacks; transactional update review |
| Import/export | Handler/Fmt/FmtHandler.ResolveConfig and GetShareUri dispatch protocol-specific formatters; AddBatchServers handles batches | reuse parsers; validate size/scheme first |
| Selection | ConfigHandler.SetDefaultServerIndex/GetDefaultServer, ProfilesViewModel orchestration | map ID to portable Server metadata |
| Speed testing | SpeedtestService.RunLoop supports cancellation and multiple actions, including TCP ping and core-based tests | TCP ping must not be called proxy connectivity; use bounded batches |
| System Proxy | Handler/SysProxy/SysProxyHandler and ProxySettingWindows use WinINet and PAC; forceDisable selects ForcedClear | cannot meet ownership restoration as-is |
| TUN | CoreManager consults immutable IsTunEnabled, may remove Windows TUN device; sample inbound/rules; CoreAdminManager non-Windows elevation | separate privileged milestone; no silent adapter changes |
| DNS | DNSItem, SimpleDNSItem, ConfigHandler.InitBuiltinDNS/SaveDNSItems and per-core resources | capability-specific validation |
| Routing | RoutingItem/RulesItem, ConfigHandler.SaveRoutingItem/SetDefaultRouting, context resolves referenced outbound nodes | atomic validated snapshots, not arbitrary UI strings |
| Tray | v2rayN/Views/StatusBarView.xaml uses H.NotifyIcon.Wpf TaskbarIcon menus | build own lifecycle/UI |
| Autostart | Handler/AutoStartupHandler.cs writes HKCU Run or scheduled logon task with highest run level | explicit user action only; no elevated default |
| Logging | Logging.Setup/SaveLog via NLog; core callbacks may include node summaries and exception messages | avoid forwarding confidential payloads |
| Updates | Services/UpdateService, CoreInfoManager update capability list, ViewModels/CheckUpdateViewModel | provenance/hash/signature gate required before execution |
| Privileges | normal local proxy can run unelevated; TUN/admin tasks have separate requirements | no elevation in foundation |
| Tests | ServiceLib.Tests: formatters, regex guard, request headers, CoreManager sudo decisions, context builder, Xray and sing-box generators | useful unit coverage; not Windows cleanup/end-to-end proof |

ServiceLib.csproj depends on Downloader, IPNetwork2, ReactiveUI, ReactiveUI.SourceGenerators, sqlite-net-e, Repobot.SQLite.Unofficial, NLog, WebDav.Client, YamlDotNet, QRCoder, CliWrap, ZXing.Net.Bindings.SkiaSharp and TaskScheduler, plus ServiceLib.UdpTest. Directory.Packages.props pins versions; Directory.Build.props targets net10.0. Upstream has a beta assertion package in its tests: VPN-AJ will use stable xUnit packages instead. Exact adapter API inventory is in ../architecture/CORE-INTEGRATION.md.

Review limitation: static inspection of named entry points and supporting implementations, not a full audit of every protocol/parser or transitive dependency. Do not equate existing upstream behavior with VPN-AJ security guarantees.
