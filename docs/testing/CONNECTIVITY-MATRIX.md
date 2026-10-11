# Connectivity matrix — 2026-10-11

## Scope and evidence

Only user-authorized VLESS profiles were used for real remote tests. One representative per available transport was selected in an isolated encrypted test library; this is not a test of every subscription server. Normal profiles/preferences and Windows proxy settings were not changed.

Evidence: `artifacts/phase3-verified-live/result.json` (53 observations: 43 passed, six IPv6 endpoint failures, four unsupported sing-box combinations). Final wrapper evidence is at `artifacts/connectivity-640f79d0e14d4f94805918a9be455eb5/result.json` (wrapper exit 0). The current-build follow-up `artifacts/phase3-public-diagnostics/result.json` repeated the same outcomes and verified a measured public exit IP; its durable sanitized copy is `docs/testing/evidence/phase3-connectivity.json`. Public exit addresses and private profile values are omitted from test evidence.

## Real Xray measurements

Each HTTP/HTTPS/address-family result below was exercised through both the HTTP proxy and SOCKS5. HTTPS uses default platform certificate/hostname validation. HTTP results are actual requests without target TLS, not merely CONNECT handshakes.

| Authorized VLESS transport | Startup / shutdown | HTTP | HTTPS | IPv4 exit | IPv6 exit |
|---|---|---|---|---|---|
| HTTPUpgrade + VLESS Encryption | Pass | Pass | Pass | Pass | Failed endpoint check; cause unconfirmed |
| WebSocket + VLESS Encryption | Pass | Pass | Pass | Pass | Failed endpoint check; cause unconfirmed |
| raw TCP + VLESS Encryption | Pass | Pass | Pass | Pass | Failed endpoint check; cause unconfirmed |
| XHTTP + VLESS Encryption | Pass | Pass | Pass | Pass | Pass |

IPv4/IPv6 are measured using family-specific HTTPS endpoints that return a validated public address of the requested family. A successful result proves remote exit reachability for that family, not the address family of the client-to-VLESS-server leg. All four server bootstrap measurements returned IPv4-only answers. A failure against an IPv6-only endpoint does not by itself establish an application defect or prove the provider has no IPv6 support.

## Protocol and core coverage

| Protocol/core | Real remote coverage |
|---|---|
| Xray VLESS | Verified as listed above; required encryption preserved |
| sing-box VLESS, supplied profiles | Unsupported requirement rejected before startup; baseline failure explained in SINGBOX-COMPATIBILITY.md |
| sing-box compatible ordinary VLESS | Native synthetic configuration/lifecycle checks only; no authorized compatible remote profile supplied |
| VMess / Trojan / Shadowsocks, both cores | Native synthetic parsing/config checks only; no authorized remote profiles supplied |

Synthetic reserved `.invalid` fixtures cannot authenticate to real servers and are never counted as protocol connectivity coverage.

## DNS and leak boundaries

- Actual server bootstrap DNS resolution succeeded for all four Xray representatives. This uses the Windows/local resolver; answer addresses and profile hosts are not logged.
- Successful domain HTTP/HTTPS requests through both proxies demonstrate usable target name resolution somewhere along the proxy path.
- Controlled loopback tests inspect the wire contract: HTTP CONNECT contains the target hostname and SOCKS5 sends domain ATYP 3, rather than a locally resolved target IP.
- Neither result proves absence of DNS leaks across the whole Windows host. Core server bootstrap uses local DNS; applications that bypass the explicit proxy continue to use their own networking and system DNS.
- Packet-level resolver attribution, randomized authoritative DNS probes, encrypted DNS transport coverage and browser leak scenarios remain unverified. No DNS servers, routes or adapter settings were changed.

A separate leak experiment should use an authorized test domain with a unique label and observe resolver queries in memory or at an authorized authoritative server. Compare explicit HTTP/SOCKS requests against an intentionally direct application request. Preserve only aggregate outcomes, never profile hostnames or raw captures. Do not interpret the expected local core-bootstrap query as proof that all target queries leaked. No such capture was run here.

## Stability and negative checks

| Check | Executed outcome |
|---|---|
| Manual retry of original Xray profile | Pass; HTTPS reverified |
| Cancelled active probe | Pass; cancellation propagated |
| Pre-cancelled startup | Pass; no owned process/listener retained |
| Kill only harness-owned core PID | Pass; readiness cleared, Error state reached, owned resources cleaned |
| Manual restart after that crash | Pass; real HTTPS reverified |
| Native duplicate start / occupied listener / invalid config | Pass in native self-test |
| Native real readiness timeout | Pass; ten-second timeout and owned cleanup |
| Untrusted target certificate through HTTP CONNECT | Rejected in controlled loopback TLS integration test; no trust-store changes |
| Incompatible core selection | Rejected; library snapshot preserved |
| Long-running load, sleep/resume, interface changes, sustained packet loss | Not tested |

Cancellation and crash tests do not implement automatic failover. Recovery is an explicit reconnect of the same authorized profile.

Windows System Proxy/browser/registry/crash-restoration tests were intentionally excluded. See WININET-INTERACTIVE-VALIDATION.md for a separate approval-gated procedure. Production readiness is not claimed.
