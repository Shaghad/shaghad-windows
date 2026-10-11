# Upstream license review — preliminary

Reviewed 2026-10-11. This is an engineering inventory, not a legal compliance certification. Distribution remains gated on full dependency/license and release review.

v2rayN tag 7.25.4 includes GPL v3 in LICENSE and declares GPL-3.0 in Directory.Build.props. Linking ServiceLib means plan to distribute the combined VPN-AJ work under compatible GPL v3 terms; do not assume proprietary distribution is possible. Preserve upstream copyrights, license text and change notices, supply Corresponding Source including build scripts and the exact upstream revision, and provide installation information where GPL requires it. Separate executable packaging does not itself resolve derivative-work questions. License text is retained in external/v2rayN/LICENSE and copied to root LICENSE for the new GPL v3 project.

Hiddify LICENSE.md adds conditions about GitHub forks, automated releases, attribution, naming/UI restrictions, noncommercial use and share-alike. Their compatibility with GPL requires independent legal review. VPN-AJ includes no Hiddify source or visual assets. Source: https://github.com/hiddify/hiddify-app/blob/98387fe96b1ce5bc01ec463ab2a0d1a6f0c42d2e/LICENSE.md

Xray-core's upstream LICENSE is MPL-2.0: https://github.com/XTLS/Xray-core/blob/main/LICENSE . sing-box publishes GPL licensing: https://github.com/SagerNet/sing-box . Neither binary is bundled or executed in this milestone. Before bundling, inspect the exact selected tag's license, bundled data licenses and applicable notices; branch licenses are not sufficient for a release inventory.

NuGet transitive dependency licenses must be inventoried from restored packages before distribution. Native SQLite/Skia components require their own notices. Microsoft.Extensions packages and xUnit are selected stable packages; record resolved versions and package license metadata in THIRD-PARTY-NOTICES. System fonts and WPF icons are OS resources, not redistributed assets. No third-party icon/font downloads are included.
