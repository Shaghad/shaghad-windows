using AJ.VPN.Application;
using AJ.VPN.Domain;

namespace AJ.VPN.V2RayNAdapter;

public sealed class UnavailableCoreBackend : ICoreBackend
{
    public bool IsAvailable => false;
    public Task<CoreReadiness> StartAsync(Server server, CancellationToken cancellationToken) =>
        Task.FromException<CoreReadiness>(new NotSupportedException("Verified core execution is not available yet."));
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
