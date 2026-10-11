# Network integration and smart selection design

These are design requirements for the next milestone, not implemented network features.

## Proxy ownership

Before applying WinINet settings, snapshot all affected values including absent values, auto-config URL, flags and bypass list in a user-protected recovery journal. Persist the exact written state and operation ID. On disconnect, restore only if the current settings still match the owned state; otherwise leave another application's changes intact and explain the conflict. Apply failures must roll back already-written fields. A crash-recovery prompt must explain the owned change before repair. Test enabled/disabled/PAC configurations, concurrent writers, process crash and write failures. Never use SysProxyHandler force-clear as restoration.

## Core lifecycle and trust

Resolve only pinned official release artifacts, validate expected hashes/signatures against a trusted manifest, and stage downloads before activation. Hashes fetched beside an untrusted artifact alone are insufficient provenance. Persist an atomic previous-version fallback. Use ProcessStartInfo.ArgumentList for VPN-AJ-managed commands, fixed config paths, no shell, and bounded stdout/stderr. Track only spawned process IDs/handles; add a Windows job object. Monitor unexpected exit, port readiness and an explicit protocol-aware HTTP probe separately. Shutdown is graceful first with a bounded kill of owned children only. No real engine is bundled yet.

## Selection and probes

Represent each measurement with server ID, protocol/core, probe kind, timestamp, outcome, latency and failure count. A TCP dial proves transport reachability only. A protocol-aware proxy URL test must use a bounded isolated test core and TLS verification, not alter the active server or Windows proxy. Set concurrency default to 3 (configurable 1–8), timeout 8 seconds, total-batch cancellation and bounded process teardown.

Exclude unavailable/stale candidates. Rank healthy candidates by smoothed proxy-test latency plus a recent-failure penalty. Keep the current healthy server unless improvement exceeds both 25% and 40 ms over at least three observations. Require a 60-second cooldown after switches; manual selection wins. These provisional thresholds require real-network tuning. Auto-failover is disabled until recovery and hysteresis tests pass. Never change server on one fluctuating measurement.

## Subscriptions and storage

Use HTTPS, enforce response size/time limits, validate every redirect and disallow credential forwarding across origins. Decide local/private-network endpoint policy explicitly. Protect URL credentials and profiles using user-bound DPAPI; keep public metadata separate. Download/stage/parse/validate before SQLite transaction replacement. Failed updates retain the previous set. Quotas/expiration are nullable provider assertions, not inferred measurements. External conversion services are off by default. No automatic scheduling before explicit user preference.

## TUN, routing and diagnostics

TUN is a separate milestone: explicit user activation, privilege explanation, engine/driver compatibility, adapter identity, route/DNS snapshots and failure cleanup. Do not trigger auto-elevation or install drivers at app startup. Routing validates all rules against selected core capability before atomically replacing a config. Simple presets must map to reviewed rule snapshots; Direct is routing policy, not app exit. Diagnostics must distinguish core state, local listener, proxied HTTPS, DNS, public IP and Windows ownership; redact export data and require a user export action. No unrequested background probe or telemetry.
