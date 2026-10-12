using System.Net.NetworkInformation;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using Microsoft.Extensions.Logging;

namespace AJ.VPN.Infrastructure;

/// <summary>Each test has its own immutable library, owned job, port and private runtime root.</summary>
public sealed class IsolatedCandidateTester(ICoreCatalog catalog, ICoreConfigurationGenerator generator, IProfileValidator validator,
    string dataDirectory, string helperExecutable, ILogger<LocalProxyBackend> logger) : ICandidateTester
{
    public event EventHandler<CandidateSession>? CandidateReady;
    public static void CleanupAbandonedSessions(string dataDirectory)
    {
        var tests = Path.Combine(dataDirectory, "candidate-tests");
        if (!Directory.Exists(tests)) return;
        ProtectedFiles.CreatePrivateDirectory(tests);
        foreach (var root in Directory.EnumerateDirectories(tests))
        {
            if (!Guid.TryParseExact(Path.GetFileName(root), "N", out _) || (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) continue;
            var runtime = Path.Combine(root, "runtime");
            if (Directory.EnumerateFileSystemEntries(root).Any(p => p != runtime)) continue;
            LocalProxyBackend.CleanupAbandonedSessions(root);
            if (!Directory.EnumerateFileSystemEntries(runtime).Any()) Directory.Delete(runtime);
            if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
        }
    }
    public async Task<ServerTestResult> TestAsync(SavedProfile profile, CoreKind core, CancellationToken token)
    {
        var root = Path.Combine(dataDirectory, "candidate-tests", Guid.NewGuid().ToString("N"));
        ProtectedFiles.CreatePrivateDirectory(root);
        var isolated = new LibraryService(new SnapshotStore(ProfileLibrary.Empty with { Profiles = [profile], ActiveId = profile.Server.Id, Core = core }), validator, new NoDownload());
        using var backend = new LocalProxyBackend(isolated, catalog, generator, root, helperExecutable, logger);
        try
        {
            await isolated.InitializeAsync(token);
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(token);
            startup.CancelAfter(TimeSpan.FromSeconds(15));
            await backend.StartAsync(profile.Server, startup.Token);
            CandidateReady?.Invoke(this, new(backend.OwnedProcessId!.Value, backend.Port!.Value));
            var latencies = new List<long>();
            var timedOut = false;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var request = CancellationTokenSource.CreateLinkedTokenSource(token);
                request.CancelAfter(TimeSpan.FromSeconds(5));
                try
                {
                    var result = await ProxyConnectivityProbe.RunAsync(backend.Port!.Value, false, request.Token);
                    if (result.Success && result.Milliseconds is >= 0) latencies.Add(result.Milliseconds.Value);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested) { timedOut = true; }
            }
            token.ThrowIfCancellationRequested();
            if (latencies.Count == 0) return Result(timedOut ? ServerHealth.Timeout : ServerHealth.Failed, null, "No TLS-verified HTTPS response through this candidate.");
            latencies.Sort();
            var median = latencies.Count == 1 ? latencies[0] : latencies[0] + (latencies[1] - latencies[0]) / 2;
            return Result(latencies.Count == 2 ? ServerHealth.Healthy : ServerHealth.Degraded, median,
                latencies.Count == 2 ? "Median of two TLS-verified HTTPS request measurements." : "One successful HTTPS measurement; second attempt failed.", latencies.Count);
        }
        catch (CoreCompatibilityException) { return Result(ServerHealth.Unsupported, null, "Unsupported by selected core; security parameters were preserved."); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return Result(ServerHealth.Timeout, null, "Isolated core startup timed out."); }
        catch (OperationCanceledException) { throw; }
        catch { return Result(ServerHealth.Failed, null, "Isolated core validation or proxy test failed."); }
        finally
        {
            await backend.StopAsync(CancellationToken.None);
            // Delete only this test's known empty directories. Unknown files are preserved for review.
            var runtime = Path.Combine(root, "runtime");
            if (Directory.Exists(runtime) && !Directory.EnumerateFileSystemEntries(runtime).Any()) Directory.Delete(runtime);
            if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
        }
        ServerTestResult Result(ServerHealth health, long? ms, string message, int count = 0) => new(profile.Server.Id, health, ms, DateTimeOffset.UtcNow, message, count);
    }
    private sealed class SnapshotStore(ProfileLibrary value) : ILibraryStore
    {
        public Task<ProfileLibrary> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(value);
        public Task SaveAsync(ProfileLibrary library, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Candidate snapshot is immutable.");
    }
    private sealed class NoDownload : ISubscriptionDownloader
    {
        public Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }
}
public sealed record CandidateSession(int ProcessId, int Port);
public sealed class NetworkContext : INetworkContext, IDisposable
{
    private long revision;
    public NetworkContext()
    {
        NetworkChange.NetworkAddressChanged += Changed;
        NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
    }
    // Cache is process-local. The revision avoids repeated interface enumeration on the UI thread.
    public string Fingerprint => Interlocked.Read(ref revision).ToString(System.Globalization.CultureInfo.InvariantCulture);
    private void Changed(object? sender, EventArgs args) => Interlocked.Increment(ref revision);
    private void AvailabilityChanged(object? sender, NetworkAvailabilityEventArgs args) => Interlocked.Increment(ref revision);
    public void Dispose() { NetworkChange.NetworkAddressChanged -= Changed; NetworkChange.NetworkAvailabilityChanged -= AvailabilityChanged; }
}
