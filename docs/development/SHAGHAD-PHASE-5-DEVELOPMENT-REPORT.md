# SHAGHAD PHASE 5 DEVELOPMENT REPORT

Application version: **0.5.0-beta.1**; numeric assembly/file version **0.5.0.0**. Authoritative source: Version.props. Unsigned Development Pre-release.

## COMPLETED

Premium two-column Dashboard, original circular power control wired to existing commands, overlay left hamburger drawer with keyboard focus and Escape support, dark/light/system resources, LTR native caption and Persian text islands. Selected server/core/protocol/country, real proxied IP/copy, monotonic active-session duration and cached fastest candidates remain visible. No reference image was attached; the request's wireframe directed the original design.

Real Xray StatsService and authenticated sing-box Clash API counters supply proxy download/upload rates and totals. Management listeners bind ephemeral loopback ports and are checked against the owned PID; no system-wide counters or fabricated samples. Missing telemetry is unavailable. Quality uses actual successful HTTPS elapsed time (Excellent <=150 ms, Good <=500 ms, otherwise Degraded; unverified readiness and offline are separate). One-minute verification cadence and two-second telemetry reads are bounded and serialized.

Single-source version metadata, self-contained Windows x64 packaging script, verified core hashes, portable ZIP, checksums, release notes, installer source and immutable-pinned GitHub Actions prerelease workflow. Signing was checked: no configured code-signing certificate; application is NotSigned. No installer executable was fabricated.

Release build completed with zero warnings/errors. **122 automated tests passed**, preserving original 110. **25 native core checks passed**. Real WPF disconnected bilingual/theme matrix passed at four requested sizes. Controlled local VLESS integration verified real HTTPS, proxied exit IP and increasing native traffic counters while preserving active process ownership during isolated Test All. No Windows System Proxy was changed. See PHASE5-VALIDATION.md and local artifacts for detailed evidence.

## PARTIALLY COMPLETED

Optional failover/recovery are implemented and off by default, with three failures, two attempts, cooldown, cached compatible ranking, cancellation, manual override and attempt exclusions. Healthy resume verification preserves the owned process rather than restarting blindly. Windows power/network event hooks are registered and disposed. Physical sleep/resume and real-outage end-to-end failover have not been certified. Automatic reconnect while owning System Proxy is blocked; existing restoration workflow is used and the reason is reported. This is not a network Kill Switch.

Diagnostics retains sanitized core/listener/remote/TLS/DNS/error/latency/IP information and adds explicitly requested IPv4/IPv6 remote-exit checks. No DNS-leak or packet-loss protection claim. Packet capture and full family/provider matrix remain release gates.

UI evidence is from native WPF rendering. Connected captures use the clearly labeled controlled loopback fixture forwarding actual HTTPS, not an authenticated remote provider. Full three-candidate compact layout and full interactive accessibility/DPI testing remain pending.

## BLOCKED

GitHub publication: configured target remote is https://github.com/Shaghad/shaghad-windows.git, initial branch master. `gh auth status` identifies an invalid saved token for Shaghad. Authenticated identity and write permission therefore cannot be verified; no push or GitHub Release is claimed. Upstream submodule is clean at pinned 7d6a967c18c697f28dc6917122ed3a4993fcf336; no upstream source edits.

Installer EXE: Inno Setup compiler is unavailable in PATH and the standard Inno Setup 6 location. Reproducible per-user installer source is packaging/Shaghad.iss; directory choice, Start Menu and optional desktop shortcut, stable ID, uninstall and preserved LocalAppData are specified. No system-wide tool installation occurred. Installer upgrade/uninstall behavior remains untested.

Signing: no explicitly configured legitimate certificate was found; no signature claimed. Full corresponding-source/native dependency legal review is an additional public redistribution gate. Exact reviewed secret-audit false positives are recorded; the initially proposed blanket allowlisting was rejected by automatic approval review and replaced by individual line review with exact hash matching.

## NOT STARTED / next phase

TUN, packet-filter Kill Switch, packet-level DNS leak certification, clean-machine VM validation, full remote provider protocol coverage, signed installer certification and safe System Proxy ownership transfer across automatic reconnect. Never bypass Windows security prompts or imply all-device coverage from Local/System Proxy.

## Release artifacts and evidence

- Portable ZIP: D:/VPN-AJ/artifacts/releases/v0.5.0-beta.1/SHAGHAD-v0.5.0-beta.1-win-x64-portable.zip
- Checksums: D:/VPN-AJ/artifacts/releases/v0.5.0-beta.1/SHA256SUMS.txt
- Notes: docs/releases/v0.5.0-beta.1.md
- Installer path: unavailable; script at packaging/Shaghad.iss.
- Screenshots: docs/design/screenshots/phase5 (copied from actual native render outputs).
- Test evidence: artifacts/TestResults/*2026-10-12_13_57_54*.trx; artifacts/core-check-20261012-134840/result.json; artifacts/phase5-connected/result.json; artifacts/phase5-secret-audit-final.json.

Final extracted-package validation, checksum value and local Git release status are appended below after those operations complete.

## Changed source

Directory.Build.props/Version.props; desktop project metadata, MainWindow XAML/code, ShellViewModel/Premium/Monitoring, English/Persian resources, SmokeTest/CoreSelfTest/AutoSelectionSelfTest; ConnectionService generation, SessionMonitoring policy/parser/rates, LocalProxyBackend telemetry, FoundationDiagnostics; MonitoringTests; release/checksum/secret-audit scripts and reviewed-line manifest; installer script; release workflow; design/architecture/testing/release documentation, README and CHANGELOG. Pinned upstream source and real user configurations remain preserved.

## Final extracted-package results — COMPLETED
The actual ZIP was extracted to artifacts/phase5-extracted-final and Shaghad.exe launched with an intentionally nonexistent DOTNET_ROOT. Self-contained WPF startup/matrix, 25 native checks (fresh retry), and actual controlled-fixture HTTPS/IP/increasing traffic all passed. Numeric file version is 0.5.0.0; product metadata is 0.5.0-beta.1 plus the SDK's build revision metadata. Eight requested actual native screenshot files are in docs/design/screenshots/phase5.

An initial extracted native run hit Windows executable sharing violation 32 on sing-box; the fresh unchanged-package retry passed all 25 checks. The external lock's source was not established. This transient host behavior is recorded, not suppressed or described as clean-machine certification.

Portable ZIP SHA-256: af0212e4c0e21c14b23ccef2c789cd98a0c5149a3ca4bf99c228fa869a76f873. SHA256SUMS.txt verification passed. YAML was parsed successfully with the repository's YamlDotNet 18.1.0; all action refs are immutable 40-character SHAs. Remote CI remains unexecuted. Final tests: artifacts/TestResults/*2026-10-12_14_02_14*.trx (29 unit, 93 integration). Final package evidence: artifacts/phase5-final-smoke/result.txt, artifacts/phase5-final-core-retry/result.json, artifacts/phase5-final-connected/result.json.

Local release commit and annotated tag will contain this reconciled source snapshot. No remote push/publication is authorized by verified authentication in this session; the saved GitHub token is invalid.

## Local Git release status
Reconciled release source is committed on codex/phase5-release with annotated local tag v0.5.0-beta.1. The tag is local only; no remote push or GitHub Release publication occurred. Exact commit identity is available with git rev-parse v0.5.0-beta.1^{commit}. Authentication, installer and public distribution gates remain as documented.
