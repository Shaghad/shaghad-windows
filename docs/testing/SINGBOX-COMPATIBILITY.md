# Core compatibility — 2026-10-11

## Root cause

The authorized subscription's VLESS profiles require Xray VLESS Encryption in the ML-KEM family. They are supported by SHAGHAD through the pinned Xray 26.3.27. They are not equivalent to ordinary VLESS with `encryption=none`. No credential, encryption key material, UUID, server hostname, path or subscription URL is included here.

The pinned upstream adapter writes `ProtocolExtraItem.VlessEncryption` to Xray's outbound `settings.encryption`. Its sing-box outbound generator does not have an equivalent field. Before this fix, sing-box 1.14.3 accepted the generated JSON and opened a local listener, but sent ordinary VLESS to an endpoint requiring VLESS Encryption. A native syntax check could not detect the missing remote security requirement.

The defect was **silent loss of profile security semantics during core assignment/generation**. A target HTTPS `SecureConnectionError` was a secondary symptom, not proof of a certificate problem. Changing the HTTPUpgrade Host fallback did not repair it; that experiment was removed. TLS verification remains enabled.

## Actual normalized comparison

The initial equality-only comparison inspected 16 authorized VLESS profiles. Thirteen produced configurations for both engines; three XHTTP profiles were already rejected by upstream's sing-box generator. All profiles retained their VLESS Encryption in Xray. Later checks reject all 16 on sing-box before execution.

| Semantic field | Original failing profile comparison |
|---|---|
| Remote address / port | Equal; values withheld |
| UUID | Equal; value withheld |
| VLESS Encryption | Xray preserves required ML-KEM parameters; sing-box omits them — decisive mismatch |
| Outer transport security | `none` on this authorized profile; this does not mean its VLESS Encryption is absent |
| TLS server_name / REALITY public_key / short_id | Absent on this profile; no real TLS/REALITY-profile coverage claimed |
| Fingerprint / uTLS / ALPN | Absent on this profile; absent values are not positive feature validation |
| Flow | Equal, empty |
| Transport / path / explicit Host | HTTPUpgrade and private path matched; explicit Host absent; no early-data query |
| Implicit HTTP Host | Engines have different port defaults; tested normalization did not fix the failure and was removed |
| DNS | Xray default bootstrap resolver; sing-box explicitly local bootstrap resolver; no forced address family |
| Local server DNS answers | Four final representative profiles resolved to one IPv4 answer and no IPv6 answers; addresses withheld |

Raw TCP (`raw` in Xray) and absent sing-box transport both mean plain TCP. JSON property names alone are insufficient to compare security, transport or resolver behavior.

## Failure-stage assessment

| Stage | Evidence / boundary |
|---|---|
| Local core/listener | Both engines started; PID-owned loopback listener and SOCKS greeting succeeded in the baseline |
| Remote TCP/HTTPUpgrade | Xray's successful end-to-end requests prove a usable path; sing-box's individual remote transport stage was not packet-traced |
| Remote VLESS security/protocol | Generated configs demonstrate that sing-box cannot negotiate the required VLESS Encryption |
| Remote outer TLS/REALITY | Not configured on the failing profile; not the cause identified here |
| Target HTTPS | Xray certificate-validated requests succeeded; baseline sing-box failed here at the caller, without proving a bad target certificate |

No raw core output is persisted. Bounded in-memory matching emits fixed error categories only; unclassified errors remain explicitly unclassified.

## Implemented policy

| Profile requirement | Xray | Pinned sing-box adapter |
|---|---|---|
| VLESS Encryption other than empty/none | Preserve; native check still required | Reject with actionable Xray recommendation |
| raw TCP, WS, HTTPUpgrade, basic gRPC | Generate, native check required | Generate when other requirements are compatible; native check required |
| XHTTP / KCP | Generate, native check required | Reject; never substitute transport |
| raw TCP with HTTP obfuscation | Preserve | Reject: sing-box HTTP transport is a different wire protocol |
| gRPC custom authority or multi mode | Preserve in Xray configuration | Reject where adapter cannot preserve it |
| Vision flow | Require raw TCP plus TLS/REALITY | Same validation before generation |
| Shadowsocks requiring external transport plugin | Subject to existing core/native validation | Reject; no external plugin is installed by SHAGHAD |

Compatibility is checked in the UI core selector, library assignment and configuration generator. A rejected assignment preserves the previous library snapshot and core. Imports remain stored; an incompatible profile is not made active on the wrong core. Existing import/removal fallback policy only considers compatible profiles; this is not automatic ranking or failover. No security parameter is removed, rewritten or downgraded. No upstream source or native core was modified.

## Verified and unavailable coverage

The same supplied profiles pass real Xray connectivity on representative HTTPUpgrade, WS, raw TCP and XHTTP transports. Real sing-box success remains unverified because the supplied profiles require unsupported encryption; the user requested support for these same profiles rather than supplying a different subscription. SHAGHAD supports them through Xray. Supporting ML-KEM inside stock sing-box would require an upstream/native-core capability change outside this adapter fix.

Official reference: [Xray VLESS outbound and Encryption](https://xtls.github.io/en/config/outbounds/vless.html), [sing-box VLESS outbound fields](https://sing-box.sagernet.org/configuration/outbound/vless/), [sing-box transports](https://sing-box.sagernet.org/configuration/shared/v2ray-transport/). Installed versions and local upstream code were also inspected; current web documentation alone is not proof of a pinned binary's capabilities.
