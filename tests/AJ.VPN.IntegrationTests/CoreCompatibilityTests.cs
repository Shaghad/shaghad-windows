using System.Text.Json.Nodes;
using AJ.VPN.Domain;
using AJ.VPN.Application;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;

namespace AJ.VPN.IntegrationTests;

public sealed class CoreCompatibilityTests
{
    private const string Base = "vless://00000000-0000-0000-0000-000000000001@example.invalid:443?";
    [Theory]
    [InlineData("security=none&type=httpupgrade&encryption=mlkem768x25519plus.native.0rtt.fixture", "VLESS Encryption")]
    [InlineData("security=tls&type=xhttp&encryption=none", "transport requires Xray")]
    [InlineData("security=tls&type=kcp&encryption=none", "transport requires Xray")]
    [InlineData("security=tls&type=tcp&headerType=http&encryption=none", "HTTP obfuscation")]
    public void SingboxCannotSilentlyDropRequiredSemantics(string query, string message)
    {
        var validator = new ServiceLibProfileValidator();
        var uri = Base + query;
        Assert.True(validator.Validate(uri).IsValid);
        Assert.Contains(message, validator.CoreCompatibilityError(uri, CoreKind.SingBox));
        var profile = new SavedProfile(validator.Validate(uri).Server!, uri);
        Assert.Throws<CoreCompatibilityException>(() => new ServiceLibConfigurationGenerator(validator).Generate(profile, CoreKind.SingBox, 19080));
    }
    [Fact]
    public void XrayRetainsVlessEncryptionUnchanged()
    {
        const string encryption = "mlkem768x25519plus.native.0rtt.fixture";
        var uri = Base + "type=httpupgrade&security=none&encryption=" + encryption;
        var validator = new ServiceLibProfileValidator();
        var profile = new SavedProfile(validator.Validate(uri).Server!, uri);
        var config = JsonNode.Parse(new ServiceLibConfigurationGenerator(validator).Generate(profile, CoreKind.Xray, 19080))!;
        Assert.Equal(encryption, config["outbounds"]![0]!["settings"]!["encryption"]!.ToString());
    }
    [Theory]
    [InlineData("tcp")]
    [InlineData("ws")]
    [InlineData("httpupgrade")]
    [InlineData("grpc")]
    public void OrdinaryTlsProfilesRemainCompatible(string transport)
    {
        var validator = new ServiceLibProfileValidator();
        var uri = Base + "encryption=none&security=tls&type=" + transport;
        foreach (var kind in Enum.GetValues<CoreKind>()) Assert.Null(validator.CoreCompatibilityError(uri, kind));
    }
    [Fact]
    public void CoreDiagnosticClassificationDoesNotReturnPayloads()
    {
        var category = OwnedCoreProcess.ClassifyFailure("dial tcp secret-host: secret-password connectex");
        Assert.Equal("Remote TCP establishment failed", category);
        Assert.Null(OwnedCoreProcess.ClassifyFailure("credential-only-payload"));
    }
    [Fact]
    public async Task IncompatibleCoreSelectionPreservesLibrarySnapshot()
    {
        var store = new MemoryStore();
        var validator = new ServiceLibProfileValidator();
        var library = new LibraryService(store, validator, new NoDownload());
        await library.InitializeAsync();
        await library.ImportAsync(Base + "type=httpupgrade&security=none&encryption=mlkem768x25519plus.native.0rtt.fixture");
        var original = library.Current;
        await Assert.ThrowsAsync<CoreCompatibilityException>(() => library.SetCoreAsync(CoreKind.SingBox));
        Assert.Same(original, library.Current);
        Assert.Same(original, store.Value);
        Assert.Equal(CoreKind.Xray, library.Current.Core);
    }
    private sealed class MemoryStore : ILibraryStore
    {
        public ProfileLibrary Value = ProfileLibrary.Empty;
        public Task<ProfileLibrary> LoadAsync(CancellationToken token = default) => Task.FromResult(Value);
        public Task SaveAsync(ProfileLibrary value, CancellationToken token = default) { Value = value; return Task.CompletedTask; }
    }
    [Fact]
    public async Task ImportStoresEncryptedProfileWithoutAssigningItToIncompatibleCore()
    {
        var store = new MemoryStore { Value = ProfileLibrary.Empty with { Core = CoreKind.SingBox } };
        var library = new LibraryService(store, new ServiceLibProfileValidator(), new NoDownload());
        await library.InitializeAsync();
        await library.ImportAsync(Base + "type=httpupgrade&security=none&encryption=mlkem768x25519plus.native.0rtt.fixture");
        Assert.Single(library.Current.Profiles);
        Assert.Null(library.Active);
        await library.SetCoreAsync(CoreKind.Xray);
        await library.SelectAsync(library.Current.Profiles[0].Server.Id);
        Assert.NotNull(library.Active);
    }
    private sealed class NoDownload : ISubscriptionDownloader
    { public Task<string> DownloadAsync(string url, CancellationToken token = default) => throw new InvalidOperationException(); }
}
