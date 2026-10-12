# UX workflow validation — 2026-10-12

## Scope and evidence

This report concerns the unpublished local UX rebuild at version 0.5.0-beta.1. The three original screenshots were not supplied. All test imports used synthetic example.invalid fixtures and isolated storage; the production library and Windows proxy settings were not modified.

| Validation | Actual result |
| --- | --- |
| Release WPF build | Passed, zero warnings/errors; artifacts/ux-build-direct.log |
| win-x64 local framework-dependent publish | Passed; artifacts/ux-publish.log |
| Unit tests, including direct-IP consent/failure/normalization | 36 passed, zero skipped/failed; artifacts/ux-tests/ux-unit-final.trx |
| Integration regression and smart import | 110 passed, zero skipped/failed; artifacts/ux-tests/ux-integration-final.trx |
| Actual shown-window layout | 32 Dashboard/About cases, four sizes × two languages × two themes, passed; artifacts/ux-validation-complete/layout-results.json |
| Actual server virtualization | Existing smoke harness passed with 100, 500 and 1000 synthetic profiles and bounded realized rows |
| Management regression | Existing smoke harness passed import, selection, search/filter/sort, subscription add/edit/remove and failed real HTTPS probe through an offline core |
| New real WPF workflow harness | Passed modal confirm/cancel, list refresh, empty states, URL prefill/cancel without saving, IDataObject FileDrop, editor detection, drawer modality/focus/Escape/backdrop and notification redaction |
| Interactive keyboard drawer | Tab/Space opened drawer; background disabled; Escape returned focus to hamburger, observed through accessibility tree |
| Physical Ctrl+V and drag/drop | Pending: Computer Use stopped by user's physical Escape key before completing this check |
| Live subscription Save & Update | Not exercised against a real authorized provider; transactional downloader/parser behavior covered by local fixtures |
| Verified live exit IP and direct comparison | Pending: no authorized reachable live test profile supplied; no address fabricated |
| Every visible button / native resize/snap / screen reader | Not exhaustively exercised physically; do not infer full interaction certification from layout tests |

The sandbox initially prevented private temporary/DPAPI test storage and test-host execution. Authorized runs outside it completed the isolated checks. An intermediate workflow test failed because a synthetic MouseButtonEventArgs lacked RoutedEvent; the harness was corrected and final workflow checks passed. A running preview briefly locked the build apphost; closing it allowed a clean build. Native screenshots taken without render settling were stale; final captures wait for rendering and drawer animation. Earlier evidence directories are exploratory; use the final paths below.

## Screenshots

Final requested captures are in **artifacts/ux-rebuild-local/screenshots/** and originate from the real WPF window/dialogs:

| File | Actual state |
| --- | --- |
| Dashboard-ClosedMenu.png | Disconnected, empty library |
| Dashboard-OpenMenu.png | Modal left drawer/backdrop, keyboard focus in drawer |
| Dashboard-Disconnected.png | Disabled persistent circular Connect and exit-IP unavailable |
| Servers-WithProfiles.png | Explicit OFFLINE synthetic profile; no invented ping |
| Servers-Empty.png | Compact search, add/paste toolbar and empty server state |
| Subscription-Empty.png | Centered actionable empty state, no empty ListBox rectangle |
| Subscription-WithEntries.png | Explicit OFFLINE subscription saved without download |
| Subscription-AddDialog.png | URL prefill and Save & Update / Save Only / Cancel |
| SmartClipboard-Import.png | Actual profile confirmation, recognized count |
| Dashboard-VerificationPending.png | Real local core ready, unreachable offline server, internet unverified |
| Dashboard-Disconnected-Dark-en/fa.png | Dark English/Persian |
| Dashboard-Disconnected-Light-en/fa.png | Light English/Persian |
| Clipboard-FileDrop.png | Confirmation from actual WPF file-drop data object |
| Error-Notification.png | Credential-free failure summary/detail affordance |

**Dashboard-Connected.png is intentionally absent:** a green verified connection and measured exit IP cannot be produced honestly without a reachable authorized profile. The amber pending screenshot is not a substitute for successful connectivity evidence.

## Repeatable commands

From repository PowerShell, load scripts/common.ps1 so the bundled SDK/NuGet paths are used:

```powershell
. ./scripts/common.ps1
& $Dotnet build apps/windows/AJ.VPN.Desktop/AJ.VPN.Desktop.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
& $Dotnet test tests/AJ.VPN.UnitTests/AJ.VPN.UnitTests.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
& $Dotnet test tests/AJ.VPN.IntegrationTests/AJ.VPN.IntegrationTests.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:DOTNET_ROOT = Join-Path $PWD '.tools/dotnet'
Start-Process apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.exe -ArgumentList '--smoke-test','D:\VPN-AJ\artifacts\ux-validation-repeat' -WindowStyle Hidden
Start-Process apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.exe -ArgumentList '--ux-workflow-test','D:\VPN-AJ\artifacts\ux-workflow-repeat' -WindowStyle Hidden
```

Both harness flags create isolated temporary encrypted storage. --ux-preview opens an isolated empty library for manual testing and must not be mistaken for the production library. The framework-dependent local package needs .NET 10 Windows Desktop runtime; the bundled SDK's DOTNET_ROOT can be used during repository validation.

## Release gate

Finish physical clipboard/drop/editor checks, exercise live subscription import with consent, verify a real proxy exit IP, test comparison cancellation/approval against the declared providers, and complete remaining button/native-window/screen-reader review before calling this production-ready or publishing beta.2. Version/tag/release artifacts remain unchanged. No GitHub push occurred.
