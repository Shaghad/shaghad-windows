using System.Net;
using System.Net.Http;
namespace AJ.VPN.Infrastructure;

/// <summary>No direct fallback. Default platform TLS trust and hostname validation remain enabled.</summary>
public static class PublicExitIp
{
    public static string? NormalizeAddress(string text)
    {
        var value = text.Trim();
        if (!IPAddress.TryParse(value, out var ip) || !SubscriptionDownloader.IsPublicAddress(ip) ||
            (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && value != ip.ToString())) return null;
        return ip.ToString();
    }
    public static Task<string?> FetchDirectAsync(CancellationToken token = default) => FetchMeasuredAsync(null, token);
    public static Task<string?> FetchAsync(int port, CancellationToken token = default) => FetchMeasuredAsync(port, token);
    private static async Task<string?> FetchMeasuredAsync(int? port, CancellationToken token)
    {
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var handler = new SocketsHttpHandler {
            Proxy = port.HasValue ? new WebProxy($"socks5://127.0.0.1:{port}") : null, UseProxy = port.HasValue,
            AllowAutoRedirect = false, UseCookies = false, MaxResponseHeadersLength = 16
        };
        using var client = new HttpClient(handler);
        foreach (var endpoint in new[] { "https://api64.ipify.org", "https://api.ipify.org" }) {
            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            requestTimeout.CancelAfter(TimeSpan.FromSeconds(4));
            try {
                using var response = await client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, requestTimeout.Token);
                response.EnsureSuccessStatusCode();
                await using var body = await response.Content.ReadAsStreamAsync(requestTimeout.Token);
                var bytes = new byte[128];
                var count = 0;
                while (count < bytes.Length) {
                    var read = await body.ReadAsync(bytes.AsMemory(count), requestTimeout.Token);
                    if (read == 0) break;
                    count += read;
                }
                if (count == bytes.Length) continue;
                var address = NormalizeAddress(System.Text.Encoding.UTF8.GetString(bytes, 0, count));
                if (address != null) return address;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException) {
                token.ThrowIfCancellationRequested();
                if (timeout.IsCancellationRequested) return null;
            }
        }
        return null;
    }
}
