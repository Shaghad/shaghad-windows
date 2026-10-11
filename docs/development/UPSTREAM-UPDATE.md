# Upstream fork and update procedure

Current dependency: official upstream 2dust/v2rayN, tag 7.25.4, commit 7d6a967c18c697f28dc6917122ed3a4993fcf336. Fetch URL is original upstream; local push URL is disabled. GitHub CLI is workspace-local at `.tools/gh/bin/gh.exe` (resolve the executable with Get-ChildItem if archive layout changes). Authentication check returned no logged-in hosts. No fork or ownership was confirmed.

1. Authenticate interactively using `gh auth login`; never put a token in source or command text.
2. Run `gh auth status`, then `gh api user --jq .login`. Use that login, not the Git commit author or an assumed organization.
3. Query the authenticated account's repository/fork relationships before creating anything. Inspect repository parent/source full_name. If no existing fork of 2dust/v2rayN exists, run `gh repo fork 2dust/v2rayN --clone=false` and verify owner and parent through GitHub API.
4. Set `.gitmodules` URL and submodule origin to the verified fork; keep an upstream fetch remote pointing to https://github.com/2dust/v2rayN. Do not change the independent VPN-AJ remote on this basis.
5. For updates, inspect official stable release metadata and licenses first. Fetch that exact tag, resolve and record its commit, inspect adapter API changes and transitive/native dependencies, then detach the submodule at that commit.
6. Build, test, inspect UI, run real-core security/cleanup regression tests, review notices and commit the new gitlink with the compatibility report. Do not update automatically to a branch tip.

During this session an external actor configured the root remote `Aj-VSCode` to `https://github.com/Shaghad/shaghad-windows.git` and committed working files. That remote/history was preserved. It does not prove ownership or a v2rayN fork relationship. VPN-AJ was not pushed by this agent; confirm destination ownership and project intent before any future push.
