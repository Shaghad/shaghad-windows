# Hiddify UX source review

Reference only. No code, assets, branding or layout copied into the application. Source reviewed at main commit 98387fe96b1ce5bc01ec463ab2a0d1a6f0c42d2e using the GitHub tree API and selected immutable raw files. https://github.com/hiddify/hiddify-app/tree/98387fe96b1ce5bc01ec463ab2a0d1a6f0c42d2e

Actual paths: lib/features/home/widget/home_page.dart, connection_button.dart, empty_profiles_home_body.dart; lib/features/connection/notifier/connection_notifier.dart and model/connection_status.dart; lib/features/profile; lib/features/proxy; lib/features/settings; lib/core/theme/app_theme.dart; lib/core/router/adaptive_layout/my_adaptive_layout.dart.

HomePage uses Riverpod active profile state, a constrained scrollable body, ConnectionButton, ActiveProxyDelayIndicator and ActiveProxyFooter. This makes the primary task easy to discover. Adding a profile has an explicit Semantics label. Selected-profile context near the connection control reduces ambiguity. VPN-AJ uses its own sidebar and information cards, without the map background or branded visual design.

ConnectionNotifier separates repository interaction, connection states, notifications and failures. Profile management and proxy selection are separate features: useful progressive disclosure, but users may struggle with the distinction between provider profile and selected proxy. VPN-AJ should name these Subscription and Server and explain the relationship.

The adaptive shell handles breakpoints, keyboard focus movement and navigation state through go_router. This is stronger evidence for keyboard intent than screenshots alone; runtime accessibility and screen-reader behavior remain untested. Theme is centralized, enabling light/dark consistency. Empty-profile widget exists, but HomePage contains commented conditional branches: verify the actually reachable empty state before assuming polished behavior.

Automatic selection and latency are advertised in upstream README; delay indication is visible in the reviewed home source. Automatic selection accuracy, subscription expiration/traffic interpretation, settings information hierarchy, contrast, touch target sizes and error recovery have not been interactively audited. These are review gaps, not proven strengths. Follow-up review should inspect profile/update flows, proxy sorting and settings widgets and run the reference application. VPN-AJ will display provider quotas only when supplied, distinguish probe types, and avoid unexplained automatic switching.

License review is recorded separately; no Hiddify reuse is authorized by this design review.
