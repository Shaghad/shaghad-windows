using System.Net;
using System.Net.Sockets;
using AJ.VPN.Infrastructure;

namespace AJ.VPN.IntegrationTests;

public sealed class ConnectivityDiagnosticsTests
{
    [Theory]
    [InlineData(HttpRequestError.SecureConnectionError, "TLS verification failed")]
    [InlineData(HttpRequestError.NameResolutionError, "Name resolution failed")]
    [InlineData(HttpRequestError.ProxyTunnelError, "proxy tunnel was rejected")]
    [InlineData(HttpRequestError.ConnectionError, "connection failed")]
    public void FailureMessagesNeverIncludeRemotePayload(HttpRequestError error, string expected)
    {
        var exception = new HttpRequestException(error, "trojan://secret@example.invalid", new IOException("password=secret"));
        var message = ProxyConnectivityProbe.DescribeFailure(exception, false);
        Assert.Contains(expected, message);
        Assert.DoesNotContain("secret", message);
        Assert.DoesNotContain("example.invalid", message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosedLoopbackProxyCannotReportInternetSuccess(bool socks5)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var result = await ProxyConnectivityProbe.RunAsync(port, socks5);
        Assert.False(result.Success);
        Assert.Null(result.Milliseconds);
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ProxyConnectivityProbe.RunAsync(1, false, cancellation.Token));
    }
}
