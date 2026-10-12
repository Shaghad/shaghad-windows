# Smart clipboard import

## User flow

Ctrl+V with SHAGHAD focused outside a text editor inspects the Windows IDataObject. The Paste toolbar button and legacy ClipboardCommand use the same workflow. TextBox, PasswordBox, RichTextBox/TextBoxBase and editable ComboBox scopes retain normal paste. No clipboard inspection happens merely because the app gains focus, and inspection performs no network request or persistence.

| Content | Result |
| --- | --- |
| Valid VLESS, VMess, Trojan, Shadowsocks link(s) | Compact Import servers? confirmation with recognized count |
| Supported Base64 share-link list | Same confirmation; validated by the existing ServiceLib-backed parser |
| One valid HTTPS subscription URL | Add Subscription dialog prefilled with URL and suggested hostname |
| Ordinary search text, unsupported schema or malformed content | Concise unsupported-import explanation; nothing saved |
| FileDrop containing one .txt file | Bounded strict UTF-8 read, then the same classification/confirmation |

Inside the compact Servers search box, Ctrl+V remains ordinary text paste. No optional URL suggestion is currently implemented. Search never downloads clipboard URLs.

The Add Server dialog supports pasted input, Import Clipboard and Import File. Profile confirmation persists through LibraryService.ImportAsync and the existing encrypted atomic store, refreshes the visible list and reports the actual newly added count. Cancel leaves the library unchanged. Duplicate profiles show an already-imported result rather than a misleading successful count.

The subscription dialog shows URL first and an optional name. Save & Update persists the subscription, then explicitly downloads/parses/atomically replaces its profiles, with visible progress. Save Only performs no download. Cancel before submission performs neither action. The saved subscription survives a failed first update; an open-dialog retry retains its identity. Existing profiles survive download, parse or save failure.

## File and URL policy

- File extensions: **.txt only**, because the existing parser supports share-link lists/Base64 and has no supported JSON/YAML schema importer.
- Limit: 2,000,000 bytes per file, checked before and throughout the read. Text input is bounded to the existing 2,000,000-character parser limit.
- Encoding: strict UTF-8, optional UTF-8 BOM; invalid encoding is rejected.
- At most one clipboard/drop file per import. Directories, unsupported extensions and unexpected content are rejected. Imported files are never executed or sent to converters.
- Parser retains existing 1–2000 profile/list and 5000 saved-profile limits, supported protocol/security checks and core compatibility checks.
- Subscription URLs require HTTPS, no embedded userinfo, fragments or whitespace, and the existing length limit. Exact stored URL duplicates are rejected on add and edit. Canonically equivalent but textually different URLs are not coalesced.
- Existing downloader protections remain: public-address DNS checks, checked-IP connections, standard TLS hostname validation, bounded bodies, controlled HTTPS redirects and timeouts.

## Implementation and evidence

SmartImport is a pure application-layer classifier and bounded file reader. MainWindow.Import adapts WPF clipboard/file/drop data and editable-focus rules to the classifier and modal dialogs. ShellViewModel.Import performs confirmed mutations through the existing library. No raw exception, share URI or subscription token is placed in error details.

SmartImportTests exercises all four protocols through the real validator, multiple links, Base64, HTTPS classification without downloader calls, malformed URLs, ordinary/unsupported text, cancellation-by-inspection, strict encoding, size/extension policy, duplicate add/edit and failed-update/selection preservation. UxWorkflowValidation exercises actual WPF IDataObject FileDrop and real modal confirm/cancel flows in isolated storage.

Physical end-to-end Ctrl+V and drag/drop input remain pending after Computer Use was stopped by the user. The implemented event routing and editable-control detection are covered through code inspection and WPF checks, but are not represented as a completed physical input test.
