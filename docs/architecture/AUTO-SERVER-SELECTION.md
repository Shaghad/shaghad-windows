# Auto server selection

Auto is a persisted `SelectionMode` on the existing encrypted `ProfileLibrary`, with backward-compatible Manual defaults. It is not a profile. The presentation's `ResolvedServerId` identifies a real saved profile and is transient: cached health and previous resolution are not trusted across application restarts. Manual selection keeps the explicit server. Favorites/country are durable profile metadata, stored by the same atomic DPAPI library transaction.

`ServerSelectionService.ResolveForConnectionAsync` implements the policy. Auto evaluates the complete declared library scope through `EvaluateAsync`; Manual returns the explicitly active profile after compatibility validation without ranking other candidates. Dashboard Connect resolves Auto first, verifies the library snapshot has not changed, selects that real ID and calls the original `ConnectionService.ConnectAsync`. The same local connection/proxy/public-IP workflow remains in use.

## Ranking

1. Snapshot real profiles, deduplicated by profile ID.
2. Validate against the selected core before launching candidate processes.
3. Reuse matching recent results unless forced to retest.
4. Test remaining candidates with bounded workers.
5. Rank only Healthy/Degraded results with a non-negative measured HTTPS latency.
6. Order by milliseconds, then opaque profile ID using ordinal comparison.
7. Resolve only if every profile in the declared scope has a valid recent result and the network context remained stable.

Timeout, failure, unsupported, untested and cancelled results cannot win. No healthy result means no resolution and no untested fallback. Degraded means one of two measurements succeeded; its latency is real. Core startup alone is never sufficient. A single actual active-session probe can populate a recent Degraded result with one successful measurement. The two-attempt isolated retest is preferred for ranking.

Evaluation can be called for a single explicit row test; this never updates the full-scope Auto winner. Test All can update the next disconnected Auto resolution, but it never switches or relabels an established session. Manual row Connect asks for confirmation before using the original disconnect/connect workflow. There is no automatic failover or reconnect. Architectural cooldown (two minutes) and improvement threshold (20%) are reserved policy parameters; no live switching uses them in Phase 4.

## Cache

The process-local cache is capped at 5,000 entries, configurable via `ServerTestingOptions`, with a five-minute monotonic validity window. Keys hash profile ID, complete share URI/configuration, selected core, the fixed HTTPS endpoint and detected network revision. Raw secrets are never emitted as cache identifiers. Profile/subscription configuration changes, core changes, network-address/availability events, expiry, removed profiles and explicit retests invalidate reuse. Results that expire during a large test scope prevent a global winner until a fresh complete scope is available. The UI drops stale measurements rather than presenting them as fresh.

Network event detection is bounded and avoids enumerating adapters for every row. Windows changes that do not emit those events (for example some DNS-only changes) may remain undetected until expiry or explicit retest. No persistent latency log/database is created. Updating compatible-core policy requires an application restart, which naturally clears the cache.

## Stable library metadata

`ProfileIdentity` hashes the entire wire configuration, excluding display fragments and VMess remarks. It does not identify by hostname or display name. Subscription replacement maps unchanged wire identities to previous opaque IDs, favorites and explicit country. A credential/transport/configuration change creates a new identity. Existing duplicate legacy identities are handled without throwing. Removed profiles are not retained as an Auto candidate. Existing subscriptions, schema version, encrypted files and upstream pin are preserved.

The application still refuses incidental library changes while connected through its original UI guard; a metadata favorite/country update is safe and does not reference the active core. The active Dashboard name/protocol come from the connection's captured server, independent of refreshed list selection.
