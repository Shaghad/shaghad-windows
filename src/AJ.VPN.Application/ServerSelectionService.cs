using System.Security.Cryptography;
using System.Text;
using AJ.VPN.Domain;

namespace AJ.VPN.Application;

public sealed record ServerTestingOptions(int MaxConcurrency = 3, int CacheCapacity = 5000)
{
    public TimeSpan CacheValidity { get; init; } = TimeSpan.FromMinutes(5);
    // Reserved for a later, explicitly approved switching policy. Phase 4 never switches a session.
    public TimeSpan SwitchCooldown { get; init; } = TimeSpan.FromMinutes(2);
    public double ImprovementRequired { get; init; } = 0.20;
}

/// <summary>Full-scope, bounded testing. Does not hold a connection backend or mutate the library.</summary>
public sealed class ServerSelectionService(ICandidateTester tester, IProfileValidator validator, INetworkContext network,
    ServerTestingOptions? options = null, TimeProvider? clock = null)
{
    public const string MeasurementEndpoint = "https://www.gstatic.com/generate_204";
    private readonly ServerTestingOptions settings = options ?? new();
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly object sync = new();
    private readonly Dictionary<string, CacheEntry> cache = [];
    private sealed record CacheEntry(string Key, ServerTestResult Result, long Timestamp);
    private string Key(SavedProfile profile, CoreKind core) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        profile.Server.Id + "\n" + profile.ShareUri + "\n" + core + "\n" + MeasurementEndpoint + "\n" + network.Fingerprint)));

    public ServerTestResult? Recent(SavedProfile profile, CoreKind core)
    {
        var key = Key(profile, core);
        lock (sync)
        {
            if (cache.TryGetValue(profile.Server.Id, out var entry) && entry.Key == key
                && time.GetElapsedTime(entry.Timestamp) < settings.CacheValidity) return entry.Result;
            cache.Remove(profile.Server.Id);
            return null;
        }
    }
    public void RetainProfiles(IEnumerable<string> ids)
    {
        var keep = ids.ToHashSet(StringComparer.Ordinal);
        lock (sync) foreach (var id in cache.Keys.Where(id => !keep.Contains(id)).ToArray()) cache.Remove(id);
    }
    public void RecordProbe(SavedProfile profile, CoreKind core, ProxyProbe probe)
    {
        var result = new ServerTestResult(profile.Server.Id, probe.Success && probe.Milliseconds is >= 0 ? ServerHealth.Degraded : ServerHealth.Failed,
            probe.Success ? probe.Milliseconds : null, time.GetUtcNow(), probe.Message, probe.Success ? 1 : 0);
        lock (sync)
        {
            if (cache.Count >= settings.CacheCapacity) cache.Remove(cache.MinBy(e => e.Value.Timestamp).Key);
            cache[profile.Server.Id] = new(Key(profile, core), result, time.GetTimestamp());
        }
    }
    public static ServerTestResult? Rank(IEnumerable<ServerTestResult> results) => results.Where(r => r.IsHealthy)
        .OrderBy(r => r.Milliseconds).ThenBy(r => r.ProfileId, StringComparer.Ordinal).FirstOrDefault();

    public Task<AutoResolution> ResolveForConnectionAsync(ProfileLibrary library, bool force = false,
        IProgress<ServerTestProgress>? progress = null, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (library.SelectionMode == SelectionMode.Auto) return EvaluateAsync(library.Profiles, library.Core, force, progress, token);
        var manual = library.Profiles.Find(p => p.Server.Id == library.ActiveId);
        if (manual != null && validator.CoreCompatibilityError(manual.ShareUri, library.Core) is string error) throw new CoreCompatibilityException(error);
        return Task.FromResult(new AutoResolution(manual?.Server.Id, [], true));
    }

    public async Task<AutoResolution> EvaluateAsync(IReadOnlyList<SavedProfile> profiles, CoreKind core, bool force,
        IProgress<ServerTestProgress>? progress = null, CancellationToken token = default)
    {
        if (settings.MaxConcurrency is < 1 or > 8 || settings.CacheCapacity is < 1 or > 5000)
            throw new InvalidOperationException("Candidate testing limits are invalid.");
        await gate.WaitAsync(token);
        try
        {
            var scope = profiles.DistinctBy(p => p.Server.Id).ToArray();
            if (force) lock (sync) foreach (var profile in scope) cache.Remove(profile.Server.Id);
            var results = new Dictionary<string, ServerTestResult>(StringComparer.Ordinal);
            var completed = 0; var successful = 0; var failed = 0;
            var context = network.Fingerprint;
            await Parallel.ForEachAsync(scope, new ParallelOptions { MaxDegreeOfParallelism = settings.MaxConcurrency, CancellationToken = token }, async (profile, ct) =>
            {
                var key = Key(profile, core);
                var result = force ? null : Recent(profile, core);
                if (result == null)
                {
                    lock (sync) cache.Remove(profile.Server.Id);
                    lock (results) progress?.Report(new(scope.Length, completed, successful, failed,
                        new(profile.Server.Id, ServerHealth.Testing, null, time.GetUtcNow(), "Testing through an isolated proxy.")));
                    var error = validator.CoreCompatibilityError(profile.ShareUri, core);
                    if (error != null) result = new(profile.Server.Id, ServerHealth.Unsupported, null, time.GetUtcNow(), error);
                    else
                    {
                        try { result = await tester.TestAsync(profile, core, ct); }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            lock (results) progress?.Report(new(scope.Length, completed, successful, failed,
                                new(profile.Server.Id, ServerHealth.Cancelled, null, time.GetUtcNow(), "Cancelled.")));
                            throw;
                        }
                        catch { result = new(profile.Server.Id, ServerHealth.Failed, null, time.GetUtcNow(), "Candidate test failed; no verified measurement."); }
                    }
                    if (result.Health != ServerHealth.Cancelled && key == Key(profile, core))
                    {
                        lock (sync)
                        {
                            if (cache.Count >= settings.CacheCapacity) cache.Remove(cache.MinBy(e => e.Value.Timestamp).Key);
                            cache[profile.Server.Id] = new(key, result, time.GetTimestamp());
                        }
                    }
                }
                lock (results)
                {
                    results[profile.Server.Id] = result;
                    completed++; if (result.IsHealthy) successful++; else failed++;
                    progress?.Report(new(scope.Length, completed, successful, failed, result));
                }
            });
            token.ThrowIfCancellationRequested();
            // A network change during the scope invalidates the ranking, even if every worker finished.
            var complete = context == network.Fingerprint && scope.All(p => Recent(p, core) != null);
            var values = results.Values.OrderBy(r => r.ProfileId, StringComparer.Ordinal).ToArray();
            return new(complete ? Rank(values)?.ProfileId : null, values, complete);
        }
        finally { gate.Release(); }
    }
}

public enum ServerFilter { All, Favorites, Healthy, Failed, Untested }
public enum ServerSort { LowestLatency, HighestLatency, Name, Country, Protocol, LastTested }
public static class ServerBrowser
{
    public static bool Matches(SavedProfile profile, ServerTestResult? result, string search, ServerFilter filter)
    {
        var server = profile.Server;
        var country = server.Country ?? "";
        if (country.Length == 2) { try { country += " " + new System.Globalization.RegionInfo(country).EnglishName + " " + new System.Globalization.RegionInfo(country).DisplayName; } catch (ArgumentException) { } }
        var match = server.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || server.Protocol.Contains(search, StringComparison.OrdinalIgnoreCase)
            || country.Contains(search, StringComparison.OrdinalIgnoreCase);
        return match && filter switch
        {
            ServerFilter.Favorites => profile.IsFavorite,
            ServerFilter.Healthy => result?.IsHealthy == true,
            ServerFilter.Failed => result?.Health is ServerHealth.Failed or ServerHealth.Timeout or ServerHealth.Unsupported,
            ServerFilter.Untested => result == null || result.Health == ServerHealth.Untested,
            _ => true
        };
    }
    public static int Compare(SavedProfile a, ServerTestResult? ar, SavedProfile b, ServerTestResult? br, ServerSort sort, bool favoritesFirst)
    {
        if (favoritesFirst && a.IsFavorite != b.IsFavorite) return a.IsFavorite ? -1 : 1;
        int value;
        if (sort is ServerSort.LowestLatency or ServerSort.HighestLatency)
        {
            var ah = ar?.IsHealthy == true; var bh = br?.IsHealthy == true;
            if (ah != bh) return ah ? -1 : 1;
            value = ah ? Nullable.Compare(ar!.Milliseconds, br!.Milliseconds) * (sort == ServerSort.HighestLatency ? -1 : 1) : 0;
        }
        else value = sort switch
        {
            ServerSort.Country => StringComparer.OrdinalIgnoreCase.Compare(a.Server.Country, b.Server.Country),
            ServerSort.Protocol => StringComparer.OrdinalIgnoreCase.Compare(a.Server.Protocol, b.Server.Protocol),
            ServerSort.LastTested => Nullable.Compare(br?.TestedAt, ar?.TestedAt),
            _ => StringComparer.OrdinalIgnoreCase.Compare(a.Server.Name, b.Server.Name)
        };
        return value != 0 ? value : StringComparer.Ordinal.Compare(a.Server.Id, b.Server.Id);
    }
}
