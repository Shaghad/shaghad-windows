# Contributing

Keep Domain and Application platform-neutral. Keep upstream types inside the adapter. Never commit subscription URLs, credentials, user configuration, binaries or build artifacts. Do not initialize upstream storage against existing v2rayN user data.

Run scripts/build.ps1, scripts/test.ps1 and scripts/smoke-test.ps1 before submitting. Explain actual behavior and limitations; no fabricated network results. Use reserved `.invalid` endpoints in tests only. New network behavior needs explicit capability, cleanup and security tests. Do not add TLS bypasses or image-name process termination.

Preserve GPL notices and exact upstream revisions. Do not copy Hiddify source or assets. Review transitive/native dependency licenses before redistribution. Follow docs/development/UPSTREAM-UPDATE.md for dependency changes; do not push to original upstream or an unverified destination.
