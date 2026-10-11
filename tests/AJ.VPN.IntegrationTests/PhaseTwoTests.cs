using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;

namespace AJ.VPN.IntegrationTests;

public sealed class PhaseTwoTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "SHAGHAD-tests", Guid.NewGuid().ToString("N"));
    private const string Vless = "vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=tls&type=tcp#fixture";
    private const string Trojan = "trojan://offline-fixture-password@example.invalid:443?security=tls&type=ws#fixture";
    public static IEnumerable<object[]> Profiles()
    {
        yield return [Vless, "VLESS"];
        yield return [Trojan, "Trojan"];
        yield return ["ss://" + Convert.ToBase64String(Encoding.UTF8.GetBytes("aes-128-gcm:offline-fixture-password")) + "@example.invalid:8388", "Shadowsocks"];
        yield return ["vmess://" + Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"v\":\"2\",\"add\":\"example.invalid\",\"port\":443,\"id\":\"00000000-0000-0000-0000-000000000001\",\"aid\":0,\"net\":\"tcp\",\"tls\":\"tls\"}")), "VMess"];
    }
    [Theory, MemberData(nameof(Profiles))]
    public void AllFourProtocolsGenerateOnlyLoopbackAndSelectedProxy(string uri, string protocol)
    {
        var validator = new ServiceLibProfileValidator();
        var validation = validator.Validate(uri);
        Assert.True(validation.IsValid, validation.Message); Assert.Equal(protocol, validation.Server!.Protocol);
        foreach (var kind in Enum.GetValues<CoreKind>())
        {
            var config = JsonNode.Parse(new ServiceLibConfigurationGenerator(validator).Generate(new(validation.Server, uri), kind, 19080))!;
            Assert.Single(config["outbounds"]!.AsArray()); Assert.Equal("proxy", config["outbounds"]![0]!["tag"]!.GetValue<string>());
            Assert.Single(config["inbounds"]!.AsArray()); Assert.Equal("127.0.0.1", config["inbounds"]![0]!["listen"]!.GetValue<string>());
            Assert.Null(config["experimental"]); Assert.Null(config["api"]); Assert.DoesNotContain("tun", config["inbounds"]!.ToJsonString());
        }
    }
    [Theory]
    [InlineData("trojan://@example.invalid:443")]
    [InlineData("vless://00000000-0000-0000-0000-000000000001@example.invalid:443?type=unsupported")]
    [InlineData("trojan://fixture@example.invalid:443?allowInsecure=true")]
    [InlineData("vless://00000000-0000-0000-0000-000000000001@example.invalid:443?type=h2")]
    [InlineData("vless://00000000-0000-0000-0000-000000000001@example.invalid:443?type=quic")]
    [InlineData("vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=unknown-tls")]
    [InlineData("vless://00000000-0000-0000-0000-000000000001@example.invalid:443?type=WS")]
    [InlineData("vless://00000000-0000-0000-0000-000000000001@example.invalid:443?type=ws&type=tcp")]
    public void UnsafeOrUnsupportedProfilesFail(string uri) => Assert.False(new ServiceLibProfileValidator().Validate(uri).IsValid);
    [Fact]
    public async Task DpapiRoundTripTamperAndCancelledWritePreserveData()
    {
        if (!OperatingSystem.IsWindows()) return;
        var store = new EncryptedLibraryStore(directory);
        var validator = new ServiceLibProfileValidator();
        var library = ProfileLibrary.Empty with { Profiles = [new(validator.Validate(Trojan).Server!, Trojan)], Subscriptions = [new("s", "fixture", "https://example.invalid/?token=offline-fixture")] };
        await store.SaveAsync(library);
        var path = Path.Combine(directory, "secrets", "library.dpapi");
        var bytes = await File.ReadAllBytesAsync(path);
        Assert.DoesNotContain("offline-fixture", Encoding.UTF8.GetString(bytes));
        Assert.Equal(Trojan, (await store.LoadAsync()).Profiles[0].ShareUri);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(ProfileLibrary.Empty, cancelled.Token));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        bytes[^1] ^= 1; await File.WriteAllBytesAsync(path, bytes);
        await Assert.ThrowsAnyAsync<System.Security.Cryptography.CryptographicException>(() => store.LoadAsync());
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
    }
    [Fact]
    public async Task ImportAndSubscriptionReplacementAreAtomicAndPreserveSelection()
    {
        var store = new MemoryLibrary(); var download = new Download();
        var library = new LibraryService(store, new ServiceLibProfileValidator(), download);
        await library.InitializeAsync(); await library.ImportAsync(Vless);
        var manualId = library.Active!.Server.Id;
        await Assert.ThrowsAsync<InvalidDataException>(() => library.ImportAsync(Trojan + "\ninvalid://secret"));
        Assert.Single(library.Current.Profiles);
        await library.AddSubscriptionAsync("fixture", "https://example.invalid/?token=offline-fixture");
        var id = library.Current.Subscriptions[0].Id;
        download.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(Trojan));
        await library.UpdateSubscriptionAsync(id);
        Assert.Equal(2, library.Current.Profiles.Count); Assert.Equal(manualId, library.Active!.Server.Id);
        var subscribed = library.Current.Profiles.Single(p => p.SubscriptionId == id);
        await library.SelectAsync(subscribed.Server.Id);
        await library.UpdateSubscriptionAsync(id); Assert.Equal(Trojan, library.Active!.ShareUri);
        var saved = library.Current;
        download.Value = "{invalid-payload}";
        await Assert.ThrowsAsync<InvalidDataException>(() => library.UpdateSubscriptionAsync(id)); Assert.Same(saved, library.Current);
        download.Value = Trojan; store.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => library.UpdateSubscriptionAsync(id)); Assert.Same(saved, library.Current);
    }
    [Theory]
    [InlineData("http://example.invalid")]
    [InlineData("https://user:password@example.invalid")]
    [InlineData("file:///C:/secret")]
    [InlineData("https://example.invalid/#fragment")]
    public void SubscriptionUrlPolicyRejectsUnsafeSchemesAndCredentials(string url) => Assert.Throws<InvalidDataException>(() => LibraryService.ValidateSubscriptionUrl(url));
    [Theory]
    [InlineData("127.0.0.1", false)] [InlineData("10.0.0.1", false)] [InlineData("172.16.0.1", false)]
    [InlineData("192.168.1.1", false)] [InlineData("169.254.169.254", false)] [InlineData("100.64.0.1", false)]
    [InlineData("::1", false)] [InlineData("::ffff:127.0.0.1", false)] [InlineData("fc00::1", false)]
    [InlineData("fe80::1", false)] [InlineData("2001:db8::1", false)] [InlineData("8.8.8.8", true)]
    [InlineData("2606:4700:4700::1111", true)]
    public void SubscriptionConnectionsOnlyAllowPublicAddresses(string value, bool expected) => Assert.Equal(expected, SubscriptionDownloader.IsPublicAddress(IPAddress.Parse(value)));
    [Fact]
    public async Task MissingOrTamperedCoreCannotBeExecuted()
    {
        await Assert.ThrowsAnyAsync<IOException>(() => new TrustedCoreCatalog(directory).FindAsync(CoreKind.Xray));
        Directory.CreateDirectory(Path.Combine(directory, "xray")); await File.WriteAllTextAsync(Path.Combine(directory, "xray", "xray.exe"), "untrusted executable fixture");
        await Assert.ThrowsAsync<InvalidDataException>(() => new TrustedCoreCatalog(directory).FindAsync(CoreKind.Xray));
    }
    [Fact]
    public async Task CorruptLibraryNeverEnablesMutation()
    {
        var store = new MemoryLibrary { Fail = true };
        var library = new LibraryService(store, new ServiceLibProfileValidator(), new Download());
        await Assert.ThrowsAsync<IOException>(() => library.InitializeAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => library.ImportAsync(Vless));
        Assert.Empty(library.Current.Profiles);
    }
    private sealed class MemoryLibrary : ILibraryStore
    {
        public ProfileLibrary Value = ProfileLibrary.Empty; public bool Fail;
        public Task<ProfileLibrary> LoadAsync(CancellationToken cancellationToken = default) => Fail ? throw new IOException() : Task.FromResult(Value);
        public Task SaveAsync(ProfileLibrary library, CancellationToken cancellationToken = default) { if (Fail) throw new IOException(); Value = library; return Task.CompletedTask; }
    }
    private sealed class Download : ISubscriptionDownloader
    { public string Value = ""; public Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default) => Task.FromResult(Value); }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
