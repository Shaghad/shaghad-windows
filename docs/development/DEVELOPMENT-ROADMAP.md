# Development roadmap

## Foundation — completed with explicit limitations

Independent net10.0 solution, original WPF shell, DI, versioned preference persistence, event metadata logging, light/dark/system/high-contrast resources, English/Persian RTL foundation, seven navigation pages, diagnostics, connection orchestration, actual ServiceLib profile validation, tests and scripts. Subscriptions, routing and statistics pages are honest unavailable states. Persian dynamic errors, diagnostic text and option labels are not fully translated yet. No production networking is enabled.

## Real connectivity — partially started

ServiceLib compiles as a pinned source dependency and share-URI validation is exercised by integration tests. Next: storage-root seam or isolated worker host, encrypted profiles/subscription credentials, real CRUD and selected-server persistence, verified Xray/sing-box acquisition, config validation, process lifecycle monitoring, core readiness plus proxy connectivity probes, cancellation and bounded teardown. Then implement ownership-aware System Proxy integration and transactional subscription updates. No fabricated endpoints or metrics.

## Selection and diagnostics — not started beyond design

Bounded proxy probes, availability/failure history, ranking, hysteresis/cooldown, user-controlled diagnostics export, real traffic sources. See NETWORK-SAFETY.md for design constraints.

## Windows production integration — not started

Tray and explicit exit lifecycle, opt-in autostart, high DPI/multiple monitors and Narrator verification, signed packaging, upgrade/rollback, complete notices/SBOM and dependency provenance. TUN is a separate explicitly privileged milestone with adapter/DNS/route cleanup tests. No macOS or Android work in this phase.

## External blocker

GitHub login is required to verify/create the authenticated user's fork. Local work does not depend on completing this operation. Do not treat the existing root remote as verified ownership.
