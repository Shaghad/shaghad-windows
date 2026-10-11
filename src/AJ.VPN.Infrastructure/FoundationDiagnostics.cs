using AJ.VPN.Application;
using AJ.VPN.Domain;

namespace AJ.VPN.Infrastructure;

public sealed class FoundationDiagnostics(ICoreCatalog catalog, ILocalProxyBackend backend) : IDiagnosticsService
{
    public async Task<IReadOnlyList<DiagnosticResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<DiagnosticResult> results =
        [
            new("Info", $"Runtime: {Environment.Version}; Windows: {OperatingSystem.IsWindows()}", "Local runtime information", "Use the supported Windows deployment matrix."),
            new("Local Proxy Status", backend.Port is int port ? $"PID-owned loopback listener ready: 127.0.0.1:{port}" : "No active local proxy listener.", "Internet verification is a separate explicit HTTPS probe.", "Import a valid server, connect, then test HTTPS through the proxy."),
            new("Manual control", "System Proxy requires explicit approval. TUN, system DNS and routes are not managed.", "A recovery journal protects approved proxy changes.", "Use Settings to apply or recover Windows proxy settings.")
        ];
        var active = backend.Port != null;
        var probe = active ? backend.LastProbe : null;
        results.Add(new("Core Status", active ? $"Running: {backend.Version}" : "Stopped or exited", "Process and listener readiness do not prove remote connectivity.", "Connect using a compatible core."));
        results.Add(new("Remote Connectivity", probe == null ? "Not measured for the current session." : probe.Success ? "HTTPS endpoint reached through the selected proxy." : probe.Message, "Explicit proxy measurement only; no direct fallback.", "Use Test HTTPS / latency to refresh."));
        results.Add(new("HTTPS Validation", probe?.Success == true ? "Default platform certificate and hostname validation passed." : "Not verified for the current session.", "A secure-handshake failure can also follow a remote protocol failure.", "Keep TLS validation enabled; check core compatibility first."));
        results.Add(new("DNS Resolution", probe?.Success == true ? "A domain request succeeded through the proxy. Resolver path and leaks are not measured." : "Domain request success has not been measured.", "Core server bootstrap uses the local resolver; applications bypassing the proxy can use system DNS.", "Use the documented controlled DNS leak procedure; System Proxy is not DNS isolation."));
        results.Add(new("Public Exit IP", active ? backend.PublicExitAddress ?? "Not measured or unavailable." : "Not measured for the current session.", "Measured using an explicit SOCKS5 HTTPS IP endpoint; never logged.", "Run the HTTPS test with a reachable server."));
        results.Add(new("Latency", probe?.Success == true && probe.Milliseconds is long latency ? $"{latency} ms (HTTPS request elapsed time)" : "Not measured.", "This is HTTP request timing, not ICMP ping or throughput.", "Run the HTTPS test to measure again."));
        results.Add(new("Last Connection Error", backend.LastError ?? "No recorded connection error.", "Only sanitized categories and controlled compatibility messages are shown.", "Retry after resolving the reported cause."));
        foreach (var kind in Enum.GetValues<CoreKind>())
        {
            try { var core = await catalog.FindAsync(kind, cancellationToken); results.Add(new("Verified", $"{kind} {core.Version}: executable hash and version match.", "Pinned official Windows x64 release.", "Native configuration validation runs before every connection.")); }
            catch (OperationCanceledException) { throw; }
            catch { results.Add(new("Unavailable", $"{kind}: executable missing, untrusted or unable to initialize.", "A pinned executable is required.", "Run scripts/install-cores.ps1 -Download and rebuild or publish.")); }
        }
        return results;
    }
}
