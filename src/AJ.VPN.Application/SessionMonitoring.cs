using AJ.VPN.Domain;
using System.Text.Json;

namespace AJ.VPN.Application;

public sealed record TrafficCounters(string Session, long Downloaded, long Uploaded);
public sealed record TrafficRates(long Downloaded, long Uploaded, double? DownloadPerSecond, double? UploadPerSecond);
public interface ITrafficBackend
{
    Task<TrafficCounters?> ReadTrafficAsync(CancellationToken token = default);
}
/// <summary>Only timestamped counters from an owned core; missing data breaks the rate baseline.</summary>
public sealed class TrafficRateCalculator(TimeProvider? provider = null)
{
    private readonly TimeProvider time = provider ?? TimeProvider.System;
    private TrafficCounters? previous;
    private long timestamp;
    public TrafficRates? Update(TrafficCounters? sample)
    {
        if (sample == null || sample.Downloaded < 0 || sample.Uploaded < 0) { previous = null; return null; }
        var now = time.GetTimestamp();
        var seconds = previous == null ? 0 : time.GetElapsedTime(timestamp, now).TotalSeconds;
        var valid = previous != null && sample.Session == previous.Session && seconds > 0
            && sample.Downloaded >= previous.Downloaded && sample.Uploaded >= previous.Uploaded;
        var result = new TrafficRates(sample.Downloaded, sample.Uploaded,
            valid ? (sample.Downloaded - previous!.Downloaded) / seconds : null,
            valid ? (sample.Uploaded - previous!.Uploaded) / seconds : null);
        previous = sample; timestamp = now; return result;
    }
}
public static class TrafficCounterParser
{
    public static TrafficCounters? ParseXray(string json, string session)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("stat", out var stats)) return null;
        long? down = null, up = null;
        foreach (var item in stats.EnumerateArray())
        {
            var name = item.GetProperty("name").GetString();
            if (name is not ("outbound>>>proxy>>>traffic>>>downlink" or "outbound>>>proxy>>>traffic>>>uplink")) continue;
            // Protobuf JSON omits zero-valued scalar fields.
            var present = item.TryGetProperty("value", out var value);
            var count = !present ? 0 : value.ValueKind == JsonValueKind.String ? long.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture) : value.GetInt64();
            if (count < 0) return null;
            if (name.EndsWith("downlink", StringComparison.Ordinal)) down = count; else up = count;
        }
        return down.HasValue && up.HasValue ? new(session, down.Value, up.Value) : null;
    }
    public static TrafficCounters? ParseSingBox(string json, string session)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("downloadTotal", out var down) || !root.TryGetProperty("uploadTotal", out var up)) return null;
        return down.TryGetInt64(out var d) && up.TryGetInt64(out var u) && d >= 0 && u >= 0 ? new(session, d, u) : null;
    }
}
public static class ConnectionQuality
{
    public static string Classify(ConnectionState state, ProxyProbe? probe) => state != ConnectionState.Connected ? "Offline"
        : probe is not { Success: true, Milliseconds: >= 0 } ? "Unverified"
        : probe.Milliseconds <= 150 ? "Excellent" : probe.Milliseconds <= 500 ? "Good" : "Degraded";
}
/// <summary>Consecutive failures, monotonic cooldown and a session-wide attempt budget.</summary>
public sealed class FailoverPolicy(TimeProvider? provider = null)
{
    private readonly TimeProvider time = provider ?? TimeProvider.System;
    private int failures, attempts;
    private long? lastAttempt;
    private readonly HashSet<string> excluded = new(StringComparer.Ordinal);
    public int FailureThreshold { get; init; } = 3;
    public int MaximumAttempts { get; init; } = 2;
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromMinutes(2);
    public void Reset() { failures = attempts = 0; lastAttempt = null; excluded.Clear(); }
    public void Record(bool success) => failures = success ? 0 : failures + 1;
    public bool CanAttempt => failures >= FailureThreshold && attempts < MaximumAttempts
        && (lastAttempt == null || time.GetElapsedTime(lastAttempt.Value) >= Cooldown);
    public string? Choose(bool enabled, SelectionMode mode, string current, IEnumerable<ServerTestResult> recent)
    {
        if (!enabled || mode != SelectionMode.Auto || !CanAttempt) return null;
        return ServerSelectionService.Rank(recent.Where(r => r.ProfileId != current && !excluded.Contains(r.ProfileId)))?.ProfileId;
    }
    public void BeginAttempt(string current, string candidate)
    {
        excluded.Add(current); excluded.Add(candidate); attempts++; failures = 0; lastAttempt = time.GetTimestamp();
    }
}
