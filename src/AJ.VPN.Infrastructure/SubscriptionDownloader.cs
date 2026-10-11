using System.Net;
using System.Net.Sockets;
using AJ.VPN.Application;

namespace AJ.VPN.Infrastructure;

public sealed class SubscriptionDownloader : ISubscriptionDownloader
{
    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return false;
        var b = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return b[0] is not (0 or 10 or 127) && b[0] < 224 && !(b[0] == 100 && b[1] is >= 64 and <= 127) &&
                !(b[0] == 169 && b[1] == 254) && !(b[0] == 172 && b[1] is >= 16 and <= 31) && !(b[0] == 192 && b[1] == 168) &&
                !(b[0] == 192 && b[1] == 0) && !(b[0] == 198 && b[1] is 18 or 19) &&
                !(b[0] == 198 && b[1] == 51 && b[2] == 100) && !(b[0] == 203 && b[1] == 0 && b[2] == 113);
        // Only global unicast 2000::/3; reject documentation and transition ranges.
        return (b[0] & 0xe0) == 0x20 && !(b[0] == 0x20 && b[1] == 1 && (b[2] < 2 || (b[2] == 0x0d && b[3] == 0xb8))) &&
            !(b[0] == 0x20 && b[1] == 2);
    }
    public async Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(25));
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false, UseProxy = false, UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ConnectCallback = async (context, token) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, token);
                if (addresses.Length == 0 || addresses.Any(a => !IsPublicAddress(a))) throw new HttpRequestException("Subscription host must resolve only to public addresses.");
                // Connect directly to the checked IP; TLS still validates the original hostname.
                foreach (var address in addresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try { await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), token); return new NetworkStream(socket, true); }
                    catch { socket.Dispose(); token.ThrowIfCancellationRequested(); }
                }
                throw new HttpRequestException("Subscription host could not be reached.");
            }
        };
        using var client = new HttpClient(handler);
        var uri = LibraryService.ValidateSubscriptionUrl(url);
        for (var redirects = 0; redirects <= 3; redirects++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("SHAGHAD/0.2");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                if (redirects == 3 || response.Headers.Location == null) throw new InvalidDataException("Subscription redirect limit exceeded.");
                uri = LibraryService.ValidateSubscriptionUrl(new Uri(uri, response.Headers.Location).AbsoluteUri);
                continue;
            }
            if (!response.IsSuccessStatusCode) throw new InvalidDataException("Subscription server returned an HTTP error. Existing profiles were preserved.");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                if (buffer.Length + count > 2_000_000) throw new InvalidDataException("Subscription exceeds the size limit.");
                buffer.Write(chunk, 0, count);
            }
            return System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        }
        throw new InvalidDataException("Subscription download failed.");
    }
}
