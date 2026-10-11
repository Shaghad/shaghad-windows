using AJ.VPN.Infrastructure;
namespace AJ.VPN.IntegrationTests;
public sealed class PublicExitIpTests
{
    [Theory]
    [InlineData("1.1.1.1", "1.1.1.1")]
    [InlineData("2606:4700:4700::1111", "2606:4700:4700::1111")]
    [InlineData("invalid", null)]
    [InlineData("<html>1.1.1.1</html>", null)]
    [InlineData("127.0.0.1", null)]
    [InlineData("::", null)]
    [InlineData("10.0.0.1", null)]
    [InlineData("::ffff:127.0.0.1", null)]
    [InlineData("192.0.2.1", null)]
    [InlineData("1.1", null)]
    public void ValidatesAddressResponse(string response, string? expected) =>
        Assert.Equal(expected, PublicExitIp.NormalizeAddress(response));

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PublicExitIp.FetchAsync(19081, cancellation.Token));
    }

    [Fact]
    public async Task ClosedProxyDoesNotFallBackToDirectInternet()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        Assert.Null(await PublicExitIp.FetchAsync(port));
    }
}
