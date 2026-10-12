# Isolated server health testing

`IsolatedCandidateTester` has no reference to the active backend, ConnectionService, SystemProxyService, DNS configuration, routes or TUN. Each candidate receives an immutable in-memory library containing exactly one real profile and the selected core. It constructs a new LocalProxyBackend instance, reusing the existing validated lifecycle without reusing its active instance.

Each worker has a unique GUID root under `candidate-tests`, private current-user/SYSTEM ACLs, a temporary native configuration, an ephemeral IPv4 loopback mixed listener and a separate owned Windows Job Object. The existing lifecycle validates compiled trust hashes/versions, holds the executable open, runs native configuration checks, creates the child suspended, assigns its kill-on-close job, resumes it and requires PID-owned listener/SOCKS readiness. Port allocation races fail safely rather than using a foreign listener. No executable discovery on PATH or termination by image name is used.

## Real measurement

The existing `ProxyConnectivityProbe` sends a TLS-verified HTTPS request through the candidate HTTP CONNECT proxy to `https://www.gstatic.com/generate_204`. A 204 response is required. Platform certificate/hostname validation remains enabled, with no redirects/cookies/ambient credentials or direct fallback. Each request has a five-second cancellation budget; candidate startup has a separate 15-second overall budget around the existing native/readiness deadlines.

Two independently established requests are attempted. Stopwatch supplies monotonic HTTPS response-header timing. Two successes use their median (the midpoint, rounded down to integer milliseconds); one success is Degraded; no success is Timeout or Failed. This is HTTPS request latency, including proxy/TLS/endpoint work, not ICMP RTT or raw transport latency. Internet conditions and the consistent target can influence rankings. Active-session public IP retrieval stays in the existing connection backend; candidate tests do not query an IP service.

Default maximum candidate concurrency is three; options accept a bounded range of one to eight. Each worker awaits cleanup before releasing its slot. Native validation subprocesses belong to that worker, finish before its long-running core starts, and have existing timeouts. Work queues and result sets are bounded by the encrypted library's profile cap.

## Cleanup and security

Cancellation propagates through native checks/readiness/requests. `finally` always stops the owned backend with an uncancelled token, closes the owned job/executable handle and deletes only known private configurations/directories. Unknown files are retained for review. Startup cleans marked abandoned candidate sessions after the application instance lock is obtained. Job ownership handles unexpected application exits; configurations can remain after an abrupt host crash until next startup cleanup. Existing managed-string secret-erasure limitations remain.

All probe failures use fixed sanitized categories. Raw core output is inspected only by the existing bounded in-memory classifier. No share links, credentials, subscription URLs or public exit IPs are added to test evidence/logs. Xray-required encryption/transport requirements still pass through the unchanged ServiceLib compatibility/generation policy; incompatible sing-box candidates are excluded without rewriting security parameters.

Executed native tests verify distinct candidate PIDs/listeners, cleanup after completion/cancellation, unchanged active PID/port/selection and unchanged read-only Windows proxy snapshots. An owned loopback VLESS fixture also exercised actual HTTPS successes, operational Dashboard Auto connection and Test All during that active session. It is explicitly distinguished from remote provider coverage.
