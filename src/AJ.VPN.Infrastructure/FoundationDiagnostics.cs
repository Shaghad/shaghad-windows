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
            new(backend.Port != null ? "Local proxy ready" : "Stopped", backend.Port is int port ? $"Loopback listener: 127.0.0.1:{port}" : "No active local proxy listener.", "Internet verification is a separate explicit HTTPS probe.", "Import a valid server, connect, then test HTTPS through the proxy."),
            new("Manual control", "System Proxy requires explicit approval. TUN, system DNS and routes are not managed.", "A recovery journal protects approved proxy changes.", "Use Settings to apply or recover Windows proxy settings.")
        ];
        foreach (var kind in Enum.GetValues<CoreKind>())
        {
            try { var core = await catalog.FindAsync(kind, cancellationToken); results.Add(new("Verified", $"{kind} {core.Version}: executable hash and version match.", "Pinned official Windows x64 release.", "Native configuration validation runs before every connection.")); }
            catch (OperationCanceledException) { throw; }
            catch { results.Add(new("Unavailable", $"{kind}: executable missing, untrusted or unable to initialize.", "A pinned executable is required.", "Run scripts/install-cores.ps1 -Download and rebuild or publish.")); }
        }
        return results;
    }
}
