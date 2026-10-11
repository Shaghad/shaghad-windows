# Technology decisions

- .NET 10 / WPF, matching upstream net10.0. Workspace-local SDK 10.0.401; no system SDK was present. CLI WPF builds need no Visual Studio installation. Visual Studio 2026 18+ is required if using its IDE: https://learn.microsoft.com/en-us/dotnet/core/install/windows
- Supported Windows deployment follows Microsoft's current .NET matrix, not every Windows 10 SKU. Current documentation limits Windows 10 support to listed LTSC/Enterprise editions. Windows 11 and ARM64 require separate packaging validation; this run targets x64.
- Use stable WPF controls with original Fluent-inspired semantic resources rather than adding another toolkit. The SDK marks ThemeMode experimental (WPF0001); it was rejected rather than suppressed. Windows theme changes use read-only registry detection and SystemEvents; high contrast uses system colors. No downloaded fonts, icons or visual assets.
- Microsoft.Extensions.DependencyInjection and Logging provide composition and structured event logging. Package restore/build verifies compatibility, not supply-chain completeness.
- ServiceLib remains a source project reference at a pinned commit. Its own Directory.Build.props and Directory.Packages.props control upstream compilation. Do not copy upstream ViewModels or initialize its desktop.
- Versioned atomic JSON is sufficient for foundation preferences; no empty SQLite abstraction. No secret storage until profiles/subscriptions persist.
- Upstream is provisionally a submodule pointing to the original read-only source because GitHub authentication is unavailable. Replace its origin with the verified authenticated fork before upstream modifications. This deviation is explicit; no fork is claimed.
