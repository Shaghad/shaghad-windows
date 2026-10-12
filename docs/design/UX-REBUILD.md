# SHAGHAD UX rebuild

Local implementation and validation: 2026-10-12. Version remains **0.5.0-beta.1**. This is an unpublished working-tree build, not an updated beta.1 release. A future beta.2 is appropriate only after remaining live validation and release review.

## Baseline and root causes

The three referenced screenshots were not attached to this request. Inspection used MainWindow.xaml/code-behind, all three ShellViewModel partials, existing styles/resources/dialogs, ConnectionService, LibraryService, ServerSelectionService, PublicExitIp, SubscriptionDownloader and the real WPF smoke harness.

| Original problem | Root cause | Correction |
| --- | --- | --- |
| Busy Dashboard | Duplicate branding/status, fastest-server list and repeated unavailable metrics | Keep circular Connect, one status, selected server, prominent exit IP, speed and duration |
| Drawer covers controls poorly | A visible left Border without backdrop or disabled background | 230 DIP overlay, dark backdrop, subtle slide/fade, cyclic keyboard focus, Escape/outside close and focus return |
| Search resembles import | Search consumes remaining toolbar width | Fixed compact 230 DIP search with outline search icon and explicit placeholder |
| URL treated as profile | Clipboard command sends every string straight to ValidateAsync | Pure classification followed by the correct confirmation dialog |
| Ctrl+V lacks smart import | No shell paste routing | PreviewKeyDown outside editable controls; shared IDataObject import path |
| Empty subscriptions confusing | Fixed-height 280 DIP ListBox even when empty; technical hint paragraph | Centered illustrated empty state; collapsed list; disabled Update All until entries exist |
| Subscription add takes extra steps | Save-only dialog, separate update command | URL-first dialog with optional name, Save & Update, Save Only and Cancel |
| Bottom errors dominate | Notice bound directly to bottom window text | Compact live notification, short summary, detail dialog and dismiss action |
| Exit IP unclear | Small number, no refresh/time/unavailable explanation | Dedicated surface, Copy, Refresh, last verified time and explicit unavailable state |
| Routing ambiguity | Local listener readiness can be mistaken for global traffic routing | Separate verified HTTPS state and amber pending state; explicit Windows proxy routing summary |

## Dashboard and connection semantics

The circular power action remains visible and disabled without a compatible active profile. The setup action beneath it opens import. The right column contains connection status, server identity, Public Exit IP, rates and elapsed time. Advanced core/configuration controls remain in their existing contextual pages.

Green means a real proxied HTTPS verification succeeded; amber means the local core is ready but internet verification has not succeeded. Red represents connection failure. Missing traffic counters show a dash, never a fabricated zero. Only configured apps use the local proxy while Windows proxy is off. Existing explicit approval for changing Windows proxy is preserved.

PublicExitIp retains explicit SOCKS5 routing, normal TLS trust/hostname validation, public IPv4/IPv6 normalization, bounded response reads and timeouts. No direct fallback exists in the proxy measurement. Probe initiation, disconnect, core failure and suspend clear the visible address and verification timestamp. Language changes refresh the new computed IP labels.

Direct comparison is an explicit user action with a default-No consent dialog naming api64.ipify.org/api.ipify.org and explaining direct source-IP disclosure. The consent-tested comparison service requires two valid normalized measurements before reporting Changed/Same. Missing or failed measurements produce Unable to verify IP difference. Results from an obsolete connection generation are discarded. Measurements do not establish whole-device routing or anonymity.

## Navigation, accessibility and localization

The drawer is a WPF focus scope with cyclic Tab/Control-Tab navigation. Opening disables ContentHost, including its keyboard/input controls; notifications inherit that disabled state. The backdrop sits beneath the drawer and above content. Escape and backdrop clicking close it; navigation closes it even when selecting the current page. Closing restores focus to HamburgerButton. Windows caption controls remain native and top-right.

The Window, grids and drawer stay LTR. Persian labels/editable name fields use local RTL scopes. Technical URL/IP inputs remain LTR. The embedded Vazirmatn font and existing licensed SHAGHAD geometry/country assets are retained. Search uses Segoe MDL2 outline glyphs. ComboBox, ComboBoxItem, CheckBox and existing scrolling surfaces follow dark/light design tokens.

## Management and notifications

Server list virtualization, Auto selection, manual selection, testing, sorting, filters, favorites and isolated core compatibility checks are retained. Display names containing raw URI syntax are replaced with a neutral localized name in rows/active-server summaries. Share URIs and credentials are never inserted into notifications.

Subscriptions have compact entries with name, profile count, remaining traffic/expiration when provider metadata exists, last update and row Update/Edit/Remove actions. Removal requires confirmation. Add/edit duplicate URL rejection occurs inside the serialized encrypted-library mutation. A failed update retains usable profiles. A newly saved subscription remains available for retry; retrying the open dialog updates that same subscription instead of inserting another entry.

Notifications use green completion, amber unverified-connection and red failure accents, a bounded summary and a detail/dismiss action. Technical exception strings are not displayed. File/URL/duplicate input errors map to credential-free explanatory messages. Duplicate profile import explains that nothing new was added. A successful subscription update whose profiles already exist explains the zero-addition result.

## Safety and remaining work

No upstream v2rayN source, production encrypted profiles, tags, release artifacts or Windows proxy settings were changed by validation. The local package is framework-dependent and needs the .NET 10 Windows Desktop runtime. Existing beta.1 distribution was not overwritten and nothing was pushed.

Live provider download, verified live connection/exit-IP screenshot and real direct/proxy comparison remain unvalidated because no authorized reachable fixture was supplied. Interactive Computer Use was stopped by the user's physical Escape key before the end-to-end Ctrl+V check. Automated classification, WPF modal/focus/FileDrop and layout checks passed; this is not a claim that every physical mouse/keyboard path or every visible button was exercised. See the workflow validation report for exact evidence.
