using System.Net;
using System.Net.Sockets;
using System.Text;
using AJ.VPN.Infrastructure;

namespace AJ.VPN.IntegrationTests;

public sealed class ProxyDnsContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TargetHostnameIsSentToProxyInsteadOfAResolvedAddress(bool socks5)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var probe = ProxyConnectivityProbe.RunAsync(port, socks5, timeout.Token);
            using var peer = await listener.AcceptTcpClientAsync(timeout.Token);
            var stream = peer.GetStream();
            if (socks5)
            {
                var greeting = new byte[3]; await stream.ReadExactlyAsync(greeting, timeout.Token);
                Assert.Equal(new byte[] { 5, 1, 0 }, greeting);
                await stream.WriteAsync(new byte[] { 5, 0 }, timeout.Token);
                var request = new byte[5]; await stream.ReadExactlyAsync(request, timeout.Token);
                Assert.Equal(3, request[3]); // SOCKS ATYP domain, not IPv4/IPv6.
                var domain = new byte[request[4]]; await stream.ReadExactlyAsync(domain, timeout.Token);
                Assert.Equal("www.gstatic.com", Encoding.ASCII.GetString(domain));
                var targetPort = new byte[2]; await stream.ReadExactlyAsync(targetPort, timeout.Token);
                await stream.WriteAsync(new byte[] { 5, 4, 0, 1, 0, 0, 0, 0, 0, 0 }, timeout.Token);
            }
            else
            {
                var header = new StringBuilder(); var buffer = new byte[1];
                while (header.Length < 2048 && !header.ToString().EndsWith("\r\n\r\n"))
                {
                    await stream.ReadExactlyAsync(buffer, timeout.Token); header.Append((char)buffer[0]);
                }
                Assert.StartsWith("CONNECT www.gstatic.com:443 HTTP/1.1", header.ToString());
                await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 502 Bad Gateway\r\nContent-Length: 0\r\n\r\n"), timeout.Token);
            }
            Assert.False((await probe).Success);
        }
        finally { listener.Stop(); }
    }
}
