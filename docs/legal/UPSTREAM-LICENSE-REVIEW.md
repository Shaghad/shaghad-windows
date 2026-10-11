# Upstream license review — preliminary

Reviewed 2026-10-11. This is an engineering inventory, not a legal compliance certification. Distribution remains gated on full dependency/license and release review.

v2rayN tag 7.25.4 includes GPL v3 in LICENSE and declares GPL-3.0 in Directory.Build.props. Linking ServiceLib means plan to distribute the combined VPN-AJ work under compatible GPL v3 terms; do not assume proprietary distribution is possible. Preserve upstream copyrights, license text and change notices, supply Corresponding Source including build scripts and the exact upstream revision, and provide installation information where GPL requires it. Separate executable packaging does not itself resolve derivative-work questions. License text is retained in external/v2rayN/LICENSE and copied to root LICENSE for the new GPL v3 project.

Hiddify LICENSE.md adds conditions about GitHub forks, automated releases, attribution, naming/UI restrictions, noncommercial use and share-alike. Their compatibility with GPL requires independent legal review. VPN-AJ includes no Hiddify source or visual assets. Source: https://github.com/hiddify/hiddify-app/blob/98387fe96b1ce5bc01ec463ab2a0d1a6f0c42d2e/LICENSE.md

Xray-core's upstream LICENSE is MPL-2.0: https://github.com/XTLS/Xray-core/blob/main/LICENSE . sing-box publishes GPL licensing: https://github.com/SagerNet/sing-box . Neither binary is bundled or executed in this milestone. Before bundling, inspect the exact selected tag's license, bundled data licenses and applicable notices; branch licenses are not sufficient for a release inventory.

sing-box's current default-branch LICENSE was inspected through GitHub's license API: GPL v3 or later plus a condition restricting derivative-work names or implied association without consent. GitHub reports NOASSERTION for this combined text. Exact-tag review and legal interpretation remain required before bundling. Current source: https://github.com/SagerNet/sing-box/blob/testing/LICENSE . No binary is included.

Resolved desktop NuGet license metadata is inventoried in NUGET-LICENSE-INVENTORY.md. sqlite-net-e's LICENSE.txt was read and contains MIT permission terms with Krueger Systems copyright. Native SQLite/Skia components require their own complete notices. Microsoft.Extensions packages and xUnit are selected stable packages. System fonts are OS resources, not redistributed assets. No third-party icon/font downloads are included. Dependency metadata review is not a full legal compatibility audit.
