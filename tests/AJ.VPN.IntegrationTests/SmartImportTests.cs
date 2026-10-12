using System.Text;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using AJ.VPN.V2RayNAdapter;

namespace AJ.VPN.IntegrationTests;

public sealed class SmartImportTests
{
    private const string Vless = "vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=tls&type=tcp#Fixture";
    private const string Trojan = "trojan://fixture@example.invalid:443?security=tls#Fixture";
    private sealed class Store : ILibraryStore
    {
        public ProfileLibrary Value = ProfileLibrary.Empty;
        public Task<ProfileLibrary> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Value);
        public Task SaveAsync(ProfileLibrary value, CancellationToken cancellationToken = default) { Value = value; return Task.CompletedTask; }
    }
    private sealed class Downloader : ISubscriptionDownloader
    {
        public int Calls;
        public string Content = Trojan;
        public Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult(Content); }
    }
    private static LibraryService Library(Store store, Downloader downloader) => new(store, new ServiceLibProfileValidator(), downloader);
    [Theory]
    [InlineData(Vless, 1)]
    [InlineData(Trojan, 1)]
    [InlineData(Vless + "\n" + Trojan, 2)]
    public void ShareLinksAreInspectedWithRealValidator(string input, int count)
    {
        var library = Library(new(), new());
        var inspection = SmartImport.Inspect(input, s => library.Parse(s).Count);
        Assert.Equal(ImportKind.Profiles, inspection.Kind); Assert.Equal(count, inspection.Count);
    }
    [Theory, MemberData(nameof(PhaseTwoTests.Profiles), MemberType = typeof(PhaseTwoTests))]
    public void ClipboardSupportsAllFourExistingProtocols(string input, string protocol)
    {
        var library = Library(new(), new());
        Assert.Equal(ImportKind.Profiles, SmartImport.Inspect(input, s => library.Parse(s).Count).Kind);
    }
    [Fact]
    public async Task InspectionAndCancellationHaveNoSideEffects()
    {
        var store = new Store(); var downloader = new Downloader(); var library = Library(store, downloader);
        await library.InitializeAsync();
        Assert.Equal(ImportKind.Subscription, SmartImport.Inspect("https://example.invalid/sub?token=synthetic", s => library.Parse(s).Count).Kind);
        Assert.Equal(ImportKind.Profiles, SmartImport.Inspect(Convert.ToBase64String(Encoding.UTF8.GetBytes(Vless)), s => library.Parse(s).Count).Kind);
        Assert.Empty(store.Value.Profiles); Assert.Empty(store.Value.Subscriptions); Assert.Equal(0, downloader.Calls);
    }
    [Theory]
    [InlineData("search Germany")]
    [InlineData("http://example.invalid/sub")]
    [InlineData("{\"outbounds\":[]}")]
    [InlineData("file:///c:/test")]
    public void OrdinaryOrUnsupportedTextIsNotImported(string input) => Assert.Equal(ImportKind.Unsupported, SmartImport.Inspect(input, _ => throw new InvalidDataException()).Kind);
    [Theory]
    [InlineData("https://user:pass@example.invalid/sub")]
    [InlineData("https://example.invalid/#fragment")]
    [InlineData("https://example.invalid/sub\nhttps://example.invalid/other")]
    public void MalformedSubscriptionUrlsAreRejected(string input) => Assert.Throws<InvalidDataException>(() => SmartImport.Inspect(input, _ => 1));
    [Fact]
    public async Task DuplicateAndFailedUpdatePreserveProfilesAndSelection()
    {
        var store = new Store(); var downloader = new Downloader(); var library = Library(store, downloader);
        await library.InitializeAsync(); await library.ImportAsync(Vless);
        var selected = library.Active!.Server.Id;
        await library.AddSubscriptionAsync("Fixture", "https://example.invalid/sub");
        await Assert.ThrowsAsync<InvalidDataException>(() => library.AddSubscriptionAsync("Again", "https://example.invalid/sub"));
        await library.AddSubscriptionAsync("Second", "https://example.invalid/second");
        await Assert.ThrowsAsync<InvalidDataException>(() => library.EditSubscriptionAsync(library.Current.Subscriptions.Last().Id, "Second", "https://example.invalid/sub"));
        await library.UpdateSubscriptionAsync(library.Current.Subscriptions.First().Id);
        Assert.Equal(2, library.Current.Profiles.Count); Assert.Equal(selected, library.Active!.Server.Id);
        var prior = library.Current; downloader.Content = "invalid";
        await Assert.ThrowsAsync<InvalidDataException>(() => library.UpdateSubscriptionAsync(prior.Subscriptions.First().Id));
        Assert.Same(prior, library.Current);
    }
    [Fact]
    public async Task FileReadsAreBoundedStrictUtf8AndAllowlisted()
    {
        var directory = Path.Combine(Path.GetTempPath(), "shaghad-import-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "fixture.txt"); await File.WriteAllTextAsync(path, Vless);
            Assert.Equal(Vless, await SmartImport.ReadFileAsync(path));
            await Assert.ThrowsAsync<InvalidDataException>(() => SmartImport.ReadFileAsync(Path.ChangeExtension(path, "json")));
            await File.WriteAllBytesAsync(path, [0xff, 0xfe]);
            await Assert.ThrowsAsync<InvalidDataException>(() => SmartImport.ReadFileAsync(path));
            await File.WriteAllBytesAsync(path, new byte[SmartImport.MaximumBytes + 1]);
            await Assert.ThrowsAsync<InvalidDataException>(() => SmartImport.ReadFileAsync(path));
        }
        finally { Directory.Delete(directory, true); }
    }
}
