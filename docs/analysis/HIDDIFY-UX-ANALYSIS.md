# Hiddify UX source review

Reference only. No code, assets, branding or layout copied into the application. Source reviewed at main commit 98387fe96b1ce5bc01ec463ab2a0d1a6f0c42d2e using the GitHub tree API and selected immutable raw files. https://github.com/hiddify/hiddify-app/tree/98387fe96b1ce5bc01ec463ab2a0d1a6f0c42d2e

Actual paths: lib/features/home/widget/home_page.dart, connection_button.dart, empty_profiles_home_body.dart; lib/features/connection/notifier/connection_notifier.dart and model/connection_status.dart; lib/features/profile; lib/features/proxy; lib/features/settings; lib/core/theme/app_theme.dart; lib/core/router/adaptive_layout/my_adaptive_layout.dart.

HomePage uses Riverpod active profile state, a constrained scrollable body, ConnectionButton, ActiveProxyDelayIndicator and ActiveProxyFooter. This makes the primary task easy to discover. Adding a profile has an explicit Semantics label. Selected-profile context near the connection control reduces ambiguity. VPN-AJ uses its own sidebar and information cards, without the map background or branded visual design.

ConnectionNotifier separates repository interaction, connection states, notifications and failures. Profile management and proxy selection are separate features: useful progressive disclosure, but users may struggle with the distinction between provider profile and selected proxy. VPN-AJ should name these Subscription and Server and explain the relationship.

The adaptive shell handles breakpoints, keyboard focus movement and navigation state through go_router. This is stronger evidence for keyboard intent than screenshots alone; runtime accessibility and screen-reader behavior remain untested. Theme is centralized, enabling light/dark consistency. Empty-profile widget exists, but HomePage contains commented conditional branches: verify the actually reachable empty state before assuming polished behavior.

Automatic selection and latency are advertised in upstream README; delay indication is visible in the reviewed home source. Automatic selection accuracy, subscription expiration/traffic interpretation, settings information hierarchy, contrast, touch target sizes and error recovery have not been interactively audited. These are review gaps, not proven strengths. Follow-up review should inspect profile/update flows, proxy sorting and settings widgets and run the reference application. VPN-AJ will display provider quotas only when supplied, distinguish probe types, and avoid unexplained automatic switching.

Additional full-checkout inspection confirmed these flows:

- profile/details/profile_details_page.dart renders provider upload/download/total and expiration fields; profile/widget/profile_tile.dart supplies remaining-time/traffic semantics, updating indicators and tooltips. Provider metadata is useful context; interpretation and absent-header behavior still need runtime checks.
- profile/notifier/profiles_update_notifier.dart schedules a foreground worker every 15 minutes with a five-minute task timeout, then honors each remote profile's update interval. VPN-AJ should expose update state and preserve previous usable data on failure.
- proxy/overview/proxies_overview_notifier.dart persists sort mode (default delay), reports ServiceNotRunning when inactive and sorts observed outbound groups. proxies_overview_page.dart exposes urlTest("select"). These are proxy-oriented probes, not evidence from ICMP alone.
- settings/overview/settings_page.dart includes confirmed import actions and distinguishes anonymous export from export containing private data. General, inbound, DNS, routing and TLS sections are separate pages. This reduces density but increases navigation depth; VPN-AJ should place ordinary settings before expert controls.
- home/widget/connection_button.dart displays a connected core with invalid delay as “connecting,” and changes behavior for hot reload/reconnect. That can blur process state and verified connectivity. VPN-AJ should show these as separate facts.
- EmptyProfilesHomeBody offers an add-profile action. Adaptive layout contains focus routing and responsive shell branches. Their real reachability, keyboard order, contrast and assistive-technology performance remain untested.

License review is recorded separately; no Hiddify reuse is authorized by this design review.
