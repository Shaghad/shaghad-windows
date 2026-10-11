using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AJ.VPN.Infrastructure;

namespace AJ.VPN.IntegrationTests;

public sealed class ProxyTlsVerificationTests
{
    [Fact]
    public async Task UntrustedTargetCertificateThroughHttpConnectIsRejected()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=www.gstatic.com", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder(); san.AddDnsName("www.gstatic.com"); request.CertificateExtensions.Add(san.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(10));
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        try
        {
            using var serving = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            var server = ServeAsync();
            var probe = ProxyConnectivityProbe.RunAsync(((IPEndPoint)listener.LocalEndpoint).Port, false, timeout.Token);
            var result = await probe;
            serving.Cancel();
            await server;
            Assert.False(result.Success);
            Assert.Contains("TLS verification failed", result.Message);
            async Task ServeAsync()
            {
                try
                {
                    while (!serving.IsCancellationRequested)
                    {
                        using var peer = await listener.AcceptTcpClientAsync(serving.Token);
                        var stream = peer.GetStream(); var header = new StringBuilder(); var buffer = new byte[1];
                        while (header.Length < 2048 && !header.ToString().EndsWith("\r\n\r\n"))
                        { await stream.ReadExactlyAsync(buffer, serving.Token); header.Append((char)buffer[0]); }
                        await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n"), serving.Token);
                        using var tls = new SslStream(stream, true);
                        try { await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate }, serving.Token); }
                        catch (Exception ex) when (ex is AuthenticationException or IOException) { }
                    }
                }
                catch (OperationCanceledException) when (serving.IsCancellationRequested) { }
            }
        }
        finally { listener.Stop(); }
    }
}
