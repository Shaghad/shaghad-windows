# Feature comparison

| Concern | v2rayN source | Hiddify reviewed UX | VPN-AJ direction |
|---|---|---|---|
| Primary workflow | profile table, status bar, extensive commands | central connection control and selected profile | clear connection card with separate server/subscription pages |
| Core depth | Xray/sing-box configuration, routing, DNS, tests | repository-backed connection state and proxy indicator | reuse ServiceLib behind adapter |
| Advanced controls | dense configuration and routing capabilities | separate settings/features | progressive disclosure, capability-aware unavailable states |
| Failure handling | callback/log driven in inspected managers | typed connection failure/notifier | actionable sanitized diagnostics |
| Windows integration | WinINet, scheduled tasks, tray, TUN | cross-platform adaptive Flutter shell | own Windows lifecycle, reversible proxy ownership |
| Themes/accessibility | WPF/Avalonia, ReactiveUI | centralized theme, semantics and focus handling | WPF Fluent controls, semantic tokens, English/Persian RTL |
| Confidence | reviewed code; upstream runtime not tested | selected source; runtime UX not tested | measured test results only |
