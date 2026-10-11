using System.Net;
using System.Net.Http;
namespace AJ.VPN.Infrastructure;

/// <summary>No direct fallback. Default platform TLS trust and hostname validation remain enabled.</summary>
public static class PublicExitIp
{
    public static async Task<string?> FetchAsync(int port, CancellationToken token = default)
    {
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var handler = new SocketsHttpHandler {
            Proxy = new WebProxy($"socks5://127.0.0.1:{port}"), UseProxy = true,
            AllowAutoRedirect = false, UseCookies = false, MaxResponseHeadersLength = 16
        };
        using var client = new HttpClient(handler);
        try {
            using var response = await client.GetAsync("https://api64.ipify.org", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
            var bytes = new byte[128];
            var count = await body.ReadAsync(bytes, timeout.Token);
            if (count == bytes.Length) return null;
            var text = System.Text.Encoding.UTF8.GetString(bytes, 0, count).Trim();
            return IPAddress.TryParse(text, out var ip) && !IPAddress.IsLoopback(ip) ? ip.ToString() : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException) {
            token.ThrowIfCancellationRequested(); return null;
        }
    }
}
