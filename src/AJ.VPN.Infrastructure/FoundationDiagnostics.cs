using AJ.VPN.Application;
using AJ.VPN.Domain;

namespace AJ.VPN.Infrastructure;

public sealed class FoundationDiagnostics(ICoreBackend backend) : IDiagnosticsService
{
    public Task<IReadOnlyList<DiagnosticResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<DiagnosticResult> results =
        [
            new("Info", $"Runtime: {Environment.Version}; Windows: {OperatingSystem.IsWindows()}", "Local runtime information", "Use the supported Windows deployment matrix."),
            new(backend.IsAvailable ? "Ready" : "Unavailable", "Core execution is not enabled in the foundation.", "Binary trust and isolated lifecycle are not validated.", "Complete core integration before connecting."),
            new("Not tested", "Internet, DNS, local proxy port and public IP have not been probed.", "No active core or user-selected probe.", "Run explicit connectivity diagnostics after core activation."),
            new("Unchanged", "VPN-AJ has not changed System Proxy, DNS, routes or adapters.", "Networking features are unavailable.", "No cleanup action is required for this build.")
        ];
        return Task.FromResult(results);
    }
}
