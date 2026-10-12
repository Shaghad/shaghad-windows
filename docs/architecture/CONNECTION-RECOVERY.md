# Resume and network-change recovery

WPF subscribes to Windows power suspend/resume and network availability/address events and unregisters on close. Suspend pauses telemetry and invalidates displayed IP. Resume/network changes request a single serialized monitor pass only after session opt-in; explicit user Disconnect prevents reconnection. An already owned listener/process is probed through the existing backend before any restart, and a verified result leaves the PID and port unchanged.

Confirmed core error can recover the same saved profile with the original ConnectionService. Attempt budget/cooldown match failover, cancellation and generation checks reject stale work. Remote connectivity failures qualify for cached candidate failover only in Auto mode with failover enabled. No system DNS/routes/firewall/TUN changes occur. Approved System Proxy switching is blocked as documented in AUTO-FAILOVER.md.

Physical suspend/resume timing, interface churn, Windows policy interactions and long-running reliability require hardware/VM validation. Current automated evidence does not certify those OS behaviors.
