# Network integration and smart selection design

Phase 2 implements the core/proxy/subscription subset described in `D:/VPN-AJ/docs/development/DEVELOPMENT-REPORT.md`. Smart ranking, batch probes, TUN and advanced routing below remain future design requirements.

## Proxy ownership

Before applying WinINet settings, snapshot all affected values including absent values, auto-config URL, flags and bypass list in a user-protected recovery journal. Persist the exact written state and operation ID. On disconnect, restore only if the current settings still match the owned state; otherwise leave another application's changes intact and explain the conflict. Apply failures must roll back already-written fields. A crash-recovery prompt must explain the owned change before repair. Test enabled/disabled/PAC configurations, concurrent writers, process crash and write failures. Never use SysProxyHandler force-clear as restoration.

## Core lifecycle and trust

Phase 2 compares official executables against a compiled SHA-256 manifest before running `version`, and holds a read lock throughout activation. Installation is explicit; self-update/rollback is deferred. Version/config checks use ArgumentList, no shell and a ten-second deadline. Runtime children are created suspended, assigned to a kill-on-close Windows job, restricted to three inherited standard handles, then resumed. A ten-second readiness deadline checks PID ownership of a loopback listener and a SOCKS5 greeting. Shutdown sends CTRL_BREAK through a console helper, waits five seconds, then terminates only the owned job if needed. Raw output is drained with fixed buffers and discarded; sanitized lifecycle/stage events are retained. Internet verification is a separate explicit HTTPS request through the active proxy.

## Selection and probes

Represent each measurement with server ID, protocol/core, probe kind, timestamp, outcome, latency and failure count. A TCP dial proves transport reachability only. A protocol-aware proxy URL test must use a bounded isolated test core and TLS verification, not alter the active server or Windows proxy. Set concurrency default to 3 (configurable 1–8), timeout 8 seconds, total-batch cancellation and bounded process teardown.

Exclude unavailable/stale candidates. Rank healthy candidates by smoothed proxy-test latency plus a recent-failure penalty. Keep the current healthy server unless improvement exceeds both 25% and 40 ms over at least three observations. Require a 60-second cooldown after switches; manual selection wins. These provisional thresholds require real-network tuning. Auto-failover is disabled until recovery and hysteresis tests pass. Never change server on one fluctuating measurement.

## Subscriptions and storage

Phase 2 uses HTTPS, a 25-second deadline, three redirects and a two-megabyte decompressed body limit. Connections validate all resolved IPs, then dial a checked public IP while TLS verifies the original hostname; loopback/private/link-local/documentation/transition endpoints are rejected. No OS proxy, cookies, external converter, ambient credentials or forwarded authentication headers are used. Profiles and token-bearing URLs are stored together in a user-bound DPAPI file with private ACLs. Download/parse/validation precedes atomic encrypted-file replacement; failed updates retain the old snapshot and active share link. This bounded library does not require SQLite yet. Scheduling, quotas and auto-failover are deferred.

## TUN, routing and diagnostics

TUN is a separate milestone: explicit user activation, privilege explanation, engine/driver compatibility, adapter identity, route/DNS snapshots and failure cleanup. Do not trigger auto-elevation or install drivers at app startup. Routing validates all rules against selected core capability before atomically replacing a config. Simple presets must map to reviewed rule snapshots; Direct is routing policy, not app exit. Diagnostics must distinguish core state, local listener, proxied HTTPS, DNS, public IP and Windows ownership; redact export data and require a user export action. No unrequested background probe or telemetry.
