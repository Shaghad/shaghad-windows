# Phase 2 audit — 2026-10-11

Baseline: af0f8a8. The SHAGHAD Concept C identity and existing AJ.VPN solution are retained.

## Why the foundation cannot connect

- Desktop DI registers UnavailableCoreBackend, whose IsAvailable is always false. ConnectionService never starts a process.
- ServiceLibProfileValidator parses metadata only. The share URI/credentials are deliberately discarded; there is no server collection or selection store.
- No core executables, pinned executable hashes, version checks, configuration validation, process supervision or proxy ownership journal exist.
- Subscription CRUD/downloads and secure persistence are missing. Core readiness is modeled but has no production implementation.
- CoreConfigHandler/ContextBuilder/CoreManager use upstream AppManager singletons, database paths and global state. AppManager initialization/exit would touch v2rayN storage and force proxy cleanup. Reusing that lifecycle directly would violate isolation and restoration requirements.

## Integration boundary

Use the pinned ServiceLib parser and its public single-profile GenerateClientSpeedtestConfig(port) generators with an explicit CoreConfigContext and initialized Config. They generate loopback mixed listeners without normal inbound/statistics/AppManager initialization. SHAGHAD validates the generated JSON and executes the selected engine's native configuration check. No upstream source patch is planned.

SHAGHAD owns user-bound DPAPI storage, trusted executable discovery, configuration files in an ACL-restricted session directory, process/job ownership, startup and shutdown, connection-state notifications and explicit proxied HTTPS measurement. Core-ready and verified internet remain separate facts. No TUN or automatic network change is permitted.

## Dependencies and configuration

Windows x64/.NET 10 Desktop runtime; official pinned Xray and sing-box binaries, exact executable SHA-256 hashes and version output; initialized ServiceLib configuration models; available loopback TCP port; a valid imported user profile to establish real connectivity. HTTPS subscriptions require a public HTTPS URL and bounded validated downloads. DPAPI storage remains in the existing VPN-AJ application data directory. The setup script verifies official release package digests and executable hashes; executables are excluded from Git.

System Proxy is opt-in via a separate UI confirmation naming the change and restoration behavior. Previous values, including absent values/PAC/bypass and connection flags, must be journaled before writes. Recovery must compare the owned state and preserve concurrent changes. Automated tests use an in-memory proxy settings backend, never the real registry. Real connectivity testing requires a user-provided profile; none was supplied with this request.
