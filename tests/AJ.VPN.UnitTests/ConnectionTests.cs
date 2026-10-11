using AJ.VPN.Application;
using AJ.VPN.Domain;
namespace AJ.VPN.UnitTests;
public sealed class ConnectionTests
{
    private static readonly Server Server = new("test", "Test fixture", "VLESS", "example.invalid", 443);
    [Fact]
    public async Task DuplicateConnectIsRejectedWhileStartupRuns()
    {
        var backend = new ControlledBackend();
        var service = new ConnectionService(backend);
        var first = service.ConnectAsync(Server);
        Assert.Equal(ConnectionState.Connecting, service.State);
        Assert.False(await service.ConnectAsync(Server));
        Assert.Equal(1, backend.Starts);
        backend.Started.SetResult(new(true, false));
        Assert.True(await first);
        Assert.Equal(ConnectionState.Connected, service.State);
        Assert.False(service.InternetVerified);
        await service.DisconnectAsync();
        Assert.Equal(ConnectionState.Disconnected, service.State);
        Assert.Equal(1, backend.Stops);
    }
    [Fact]
    public async Task CancelledStartupCleansUpUsingUncancelledToken()
    {
        var backend = new ControlledBackend();
        var service = new ConnectionService(backend);
        using var cancellation = new CancellationTokenSource();
        var connect = service.ConnectAsync(Server, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connect);
        Assert.Equal(ConnectionState.Error, service.State);
        Assert.Equal(1, backend.Stops);
        Assert.False(backend.StopWasCancelled);
    }
    [Fact]
    public async Task FailedReadinessNeverBecomesConnectedAndCanRetry()
    {
        var backend = new ControlledBackend();
        var service = new ConnectionService(backend);
        backend.Started.SetResult(new(false, false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConnectAsync(Server));
        Assert.Equal(ConnectionState.Error, service.State);
        Assert.False(service.InternetVerified);
        Assert.Equal(1, backend.Stops);
        backend.Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.Started.SetResult(new(true, true));
        Assert.True(await service.ConnectAsync(Server));
        Assert.True(service.InternetVerified);
    }
    private sealed class ControlledBackend : ICoreBackend
    {
        public bool IsAvailable => true;
        public int Starts, Stops;
        public bool StopWasCancelled;
        public TaskCompletionSource<CoreReadiness> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<CoreReadiness> StartAsync(Server server, CancellationToken cancellationToken)
        {
            Starts++;
            return Started.Task.WaitAsync(cancellationToken);
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stops++;
            StopWasCancelled = cancellationToken.IsCancellationRequested;
            return Task.CompletedTask;
        }
    }
}
