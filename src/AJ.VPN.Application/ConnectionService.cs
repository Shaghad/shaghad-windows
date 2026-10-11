using AJ.VPN.Domain;

namespace AJ.VPN.Application;

/// <summary>Serializes operations. Core readiness and internet verification are separate facts.</summary>
public sealed class ConnectionService(ICoreBackend backend)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool InternetVerified { get; private set; }
    public async Task<bool> ConnectAsync(Server server, CancellationToken cancellationToken = default)
    {
        if (!await gate.WaitAsync(0, cancellationToken)) return false;
        try
        {
            if (State is not (ConnectionState.Disconnected or ConnectionState.Error)) return false;
            if (!backend.IsAvailable) throw new InvalidOperationException("Core activation is unavailable in this build.");
            State = ConnectionState.Connecting;
            try
            {
                var readiness = await backend.StartAsync(server, cancellationToken);
                if (!readiness.IsReady) throw new InvalidOperationException("Core readiness check failed.");
                InternetVerified = readiness.InternetVerified;
                State = ConnectionState.Connected;
                return true;
            }
            catch
            {
                InternetVerified = false;
                State = ConnectionState.Error;
                // Cleanup must run even when the caller has cancelled startup.
                await backend.StopAsync(CancellationToken.None);
                throw;
            }
        }
        finally { gate.Release(); }
    }
    public async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!await gate.WaitAsync(0, cancellationToken)) return false;
        try
        {
            if (State == ConnectionState.Disconnected) return true;
            State = ConnectionState.Disconnecting;
            try
            {
                await backend.StopAsync(cancellationToken);
                InternetVerified = false;
                State = ConnectionState.Disconnected;
                return true;
            }
            catch { State = ConnectionState.Error; throw; }
        }
        finally { gate.Release(); }
    }
}
