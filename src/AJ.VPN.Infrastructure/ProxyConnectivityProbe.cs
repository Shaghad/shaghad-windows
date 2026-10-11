using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using AJ.VPN.Domain;

namespace AJ.VPN.Infrastructure;

/// <summary>Explicit HTTPS checks; never returns exception text or disables TLS validation.</summary>
public static class ProxyConnectivityProbe
{
    public static string DescribeFailure(Exception failure, bool callerCancelled)
    {
        if (callerCancelled) return "The connection test was cancelled.";
        if (failure is OperationCanceledException) return "The proxy request timed out. Check server reachability and retry.";
        if (failure is AuthenticationException || failure is HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError })
            return "TLS verification failed or the secure handshake was interrupted. Check certificate trust, system clock and server transport; certificate validation remains enabled.";
        if (failure is HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError })
            return "Name resolution failed. Check the profile hostname and the core DNS configuration.";
        if (failure is HttpRequestException { HttpRequestError: HttpRequestError.ProxyTunnelError })
            return "The proxy tunnel was rejected. Check server authentication, transport and core compatibility.";
        if (failure is HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError })
            return "The connection failed. Check the local listener, remote server reachability and firewall permissions.";
        return "The proxy request failed. Check the selected profile and retry; internet access is unverified.";
    }

    public static async Task<ProxyProbe> RunAsync(int port, bool socks5, CancellationToken cancellationToken = default)
    {
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var handler = new SocketsHttpHandler
        {
            Proxy = new WebProxy($"{(socks5 ? "socks5" : "http")}://127.0.0.1:{port}"),
            UseProxy = true, AllowAutoRedirect = false, UseCookies = false,
            Credentials = null, DefaultProxyCredentials = null
        };
        using var client = new HttpClient(handler);
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync("https://www.gstatic.com/generate_204", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            return response.StatusCode == HttpStatusCode.NoContent
                ? new(true, watch.ElapsedMilliseconds, $"TLS-verified HTTPS response through {(socks5 ? "SOCKS5" : "HTTP CONNECT")} (gstatic.com). DNS path and remote address family are not established by this result.")
                : new(false, null, "The HTTPS probe returned an unexpected status. Check captive portals or server filtering.");
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(false, null, DescribeFailure(ex, false));
        }
    }
}
