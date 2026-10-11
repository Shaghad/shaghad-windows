# Separate WinINet interactive validation procedure

Status: prepared, **not executed**. This task explicitly prohibited live Windows proxy changes. Existing ownership-aware restoration and encrypted journals are preserved.

## Required approval

Obtain explicit user approval immediately before a separate test session: temporary changes to this user's WinINet proxy, browser traffic tests, intentional application crash and external proxy modification scenarios, followed by restoration. Approval to run local core tests is not approval for these changes.

## Procedure

1. Close competing SHAGHAD test instances. Verify an authorized profile and explicit-proxy HTTPS success before applying any system setting.
2. Record current WinINet enable/server/bypass, PAC/discovery and relevant LAN connection binary settings in the existing encrypted recovery journal. Preserve value absence and registry types. Do not dump private PAC URLs or registry contents into logs.
3. Use the application's approval dialog to apply only its current-user WinINet loopback proxy. Verify browser HTTP/HTTPS traffic using an authorized test endpoint and normal certificate verification. Record pass/fail, not browsing payloads.
4. Disconnect normally. Verify the original settings, including absence/types/PAC/discovery/binary data, are restored. Verify browser traffic follows the original configuration.
5. Repeat apply, then intentionally terminate only that SHAGHAD test instance. Relaunch, approve journal recovery, and verify restoration. A native core crash and an application crash are separate cases.
6. In separately approved cases, make an external WinINet modification before apply, while owned and before restoration. Verify the application detects ownership loss and preserves external changes rather than overwriting them. Restore the baseline manually only with approval for that baseline.
7. Repeat enable/disable to confirm idempotence. If restoration cannot be verified, stop further mutations and preserve the encrypted journal for recovery.

Do not change WinHTTP, system DNS, routes, network adapters or TUN settings. Never kill other VPN/core processes by image name. Record tested Windows build, browser version, exact outcomes and unresolved restrictions; in-memory restoration tests are not live OS evidence.
