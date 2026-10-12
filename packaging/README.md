# Windows installer

Inno Setup was selected for a small native per-user installer with stable upgrade identity and no drivers/network changes. `Shaghad.iss` is reproducible through `scripts/release.ps1 -BuildInstaller` when ISCC.exe is available in PATH. Tooling is not installed on this development host; no installer EXE or uninstall test is claimed.

The [upstream source license](https://github.com/jrsoftware/issrc/blob/main/license.txt) permits use with stated attribution/distribution conditions. Upstream also [requests commercial licenses](https://jrsoftware.org/isorder.php); review the precise compiler version's license and applicable commercial policy before adopting it in organizational release tooling. No development dependency is silently installed.

Current CI deliberately packages only the validated portable artifact; enabling installer publication requires installing/verifying the compiler and validating installation, upgrade and uninstall first. Signing requires a legitimate explicitly configured certificate.
