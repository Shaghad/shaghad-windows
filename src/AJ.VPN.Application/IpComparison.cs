using System.Net;

namespace AJ.VPN.Application;

public enum IpDifference { NotConsented, Unavailable, Same, Changed }
public sealed record IpComparisonResult(string? Direct, string? Proxy, IpDifference Difference);
public static class IpComparison
{
    public static async Task<IpComparisonResult> MeasureAsync(bool consent, Func<Task<string?>> direct, Func<Task<string?>> proxy)
    {
        if (!consent) return new(null, null, IpDifference.NotConsented);
        async Task<string?> Measure(Func<Task<string?>> read)
        {
            try { var value = await read(); return IPAddress.TryParse(value, out var ip) ? ip.ToString() : null; }
            catch (Exception error) when (error is HttpRequestException or IOException or OperationCanceledException) { return null; }
        }
        var first = await Measure(direct); var second = await Measure(proxy);
        return new(first, second, first == null || second == null ? IpDifference.Unavailable : first == second ? IpDifference.Same : IpDifference.Changed);
    }
}
