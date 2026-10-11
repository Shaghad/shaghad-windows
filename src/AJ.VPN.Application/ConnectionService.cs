using AJ.VPN.Domain;

namespace AJ.VPN.Application;

/// <summary>Serializes operations. Core readiness and internet verification are separate facts.</summary>
public sealed class ConnectionService
{
    private readonly ICoreBackend backend;
    public ConnectionService(ICoreBackend backend)
    {
        this.backend = backend;
        if (backend is ILocalProxyBackend local) local.UnexpectedExit += (_, _) => NotifyCoreExit();
    }
    private readonly SemaphoreSlim gate = new(1, 1);
    private long generation;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool InternetVerified { get; private set; }
    public event EventHandler? StateChanged;
    public async void NotifyCoreExit()
    {
        var observedGeneration = Interlocked.Read(ref generation);
        await gate.WaitAsync();
        try
        {
            if (observedGeneration != Interlocked.Read(ref generation)) return;
            InternetVerified = false;
            if (State is ConnectionState.Connected or ConnectionState.Connecting)
            {
                State = ConnectionState.Error;
                try { await backend.StopAsync(CancellationToken.None); } catch { /* Remain in Error; explicit retry performs cleanup again. */ }
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally { gate.Release(); }
    }
    public void RecordProbe(ProxyProbe result)
    {
        InternetVerified = State == ConnectionState.Connected && result.Success;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
    public async Task<bool> ConnectAsync(Server server, CancellationToken cancellationToken = default)
    {
        if (!await gate.WaitAsync(0, cancellationToken)) return false;
        try
        {
            if (State is not (ConnectionState.Disconnected or ConnectionState.Error)) return false;
            if (!backend.IsAvailable) throw new InvalidOperationException("Core activation is unavailable in this build.");
            Interlocked.Increment(ref generation);
            if (State == ConnectionState.Error) await backend.StopAsync(CancellationToken.None);
            State = ConnectionState.Connecting;
            StateChanged?.Invoke(this, EventArgs.Empty);
            try
            {
                var readiness = await backend.StartAsync(server, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!readiness.IsReady) throw new InvalidOperationException("Core readiness check failed.");
                if (State != ConnectionState.Connecting) throw new InvalidOperationException("Core exited before startup completed.");
                InternetVerified = readiness.InternetVerified;
                State = ConnectionState.Connected;
                StateChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch
            {
                InternetVerified = false;
                State = ConnectionState.Error;
                // Cleanup must run even when the caller has cancelled startup.
                await backend.StopAsync(CancellationToken.None);
                StateChanged?.Invoke(this, EventArgs.Empty);
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
            Interlocked.Increment(ref generation);
            State = ConnectionState.Disconnecting;
            StateChanged?.Invoke(this, EventArgs.Empty);
            try
            {
                await backend.StopAsync(cancellationToken);
                InternetVerified = false;
                State = ConnectionState.Disconnected;
                StateChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch { InternetVerified = false; State = ConnectionState.Error; StateChanged?.Invoke(this, EventArgs.Empty); throw; }
        }
        finally { gate.Release(); }
    }
}
