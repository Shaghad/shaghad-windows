# Phase 2 native core notices

SHAGHAD is independent of XTLS, SagerNet and v2rayN. This is an unsigned local development build, not a certified production distribution.

| Component | Pinned source/release | License | Installed license |
|---|---|---|---|
| Xray-core 26.3.27 Windows x64 | [XTLS release and corresponding source](https://github.com/XTLS/Xray-core/releases/tag/v26.3.27) | Mozilla Public License 2.0 | `cores/xray/LICENSE` |
| sing-box 1.14.3 Windows amd64 | [SagerNet release and corresponding source](https://github.com/SagerNet/sing-box/releases/tag/v1.14.3) | GNU GPL v3 or later, as stated by upstream | `cores/sing-box/LICENSE` |

Executables are unmodified upstream release files. `D:/VPN-AJ/scripts/install-cores.ps1` verifies pinned SHA-256 archive and executable digests and copies each executable with its upstream LICENSE. It excludes Wintun, drivers, geo datasets and other archive contents. Binaries stay in ignored `D:/VPN-AJ/cores`; rebuild/publish copies installed executables and licenses. The application itself does not download executables. The embedded trust manifest is `D:/VPN-AJ/src/AJ.VPN.Infrastructure/core-manifest.json`.

Sources and exact tags are linked above; retain the full corresponding source and all required dependency notices/source offers before distributing binaries outside the development team. These links alone are not a complete GPL redistribution compliance process. No proprietary or newly drawn brand asset is taken from either core. Full transitive native dependency notice review and signed packaging remain release gates.
