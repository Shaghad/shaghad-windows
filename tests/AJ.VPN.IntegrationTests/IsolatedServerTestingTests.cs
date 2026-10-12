using System.Net;
using System.Net.Sockets;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;
using Microsoft.Extensions.Logging.Abstractions;

namespace AJ.VPN.IntegrationTests;

public sealed class IsolatedServerTestingTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "SHAGHAD-isolation-tests", Guid.NewGuid().ToString("N"));
    private readonly ServiceLibProfileValidator validator = new();
    private const string Uri = "vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=tls&type=tcp#Offline-isolation-fixture";
    private static string Root
    {
        get { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d != null && !File.Exists(Path.Combine(d.FullName, "VPN-AJ.slnx"))) d = d.Parent; return d?.FullName ?? throw new DirectoryNotFoundException("Test repository root missing."); }
    }
    private ICoreCatalog Catalog => new TrustedCoreCatalog(Path.Combine(Root, "cores"));
    private string Helper => Path.Combine(Root, "apps/windows/AJ.VPN.Desktop/bin/Release/net10.0-windows/Shaghad.exe");
    private IsolatedCandidateTester Tester => new(Catalog, new ServiceLibConfigurationGenerator(validator), validator, directory, Helper, NullLogger<LocalProxyBackend>.Instance);
    private SavedProfile Fixture(string id) => new(validator.Validate(Uri).Server! with { Id = id }, Uri);

    [Fact]
    public async Task ConcurrentCandidatesLeaveActivePidPortSelectionAndProxyUnchanged()
    {
        var active = Fixture("active"); var store = new Store(ProfileLibrary.Empty with { Profiles = [active], ActiveId = "active" });
        var library = new LibraryService(store, validator, new Download()); await library.InitializeAsync();
        using var backend = new LocalProxyBackend(library, Catalog, new ServiceLibConfigurationGenerator(validator), directory, Helper, NullLogger<LocalProxyBackend>.Instance);
        var connection = new ConnectionService(backend);
        await connection.ConnectAsync(active.Server);
        var pid = backend.OwnedProcessId; var port = backend.Port!.Value; var snapshot = library.Current;
        var proxy = new WindowsProxySettingsBackend(); var proxyBefore = proxy.Read();
        try
        {
            var tester = Tester; var sessions = new System.Collections.Concurrent.ConcurrentBag<CandidateSession>();
            tester.CandidateReady += (_, session) => sessions.Add(session);
            var service = new ServerSelectionService(tester, validator, new Network());
            var result = await service.EvaluateAsync([Fixture("one"), Fixture("two"), Fixture("three")], CoreKind.Xray, true);
            Assert.Null(result.ResolvedServerId); Assert.All(result.Results, r => Assert.False(r.IsHealthy));
            Assert.Equal(pid, backend.OwnedProcessId); Assert.Equal(port, backend.Port); Assert.Same(snapshot, library.Current);
            Assert.Equal(ConnectionState.Connected, connection.State);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(proxyBefore), System.Text.Json.JsonSerializer.Serialize(proxy.Read()));
            Assert.Equal(3, sessions.Count); Assert.Equal(3, sessions.Select(s => s.ProcessId).Distinct().Count());
            foreach (var session in sessions) { Assert.NotEqual(pid, session.ProcessId); Assert.NotEqual(port, session.Port); Assert.False(await CanConnectAsync(session.Port)); Assert.False(IsRunning(session.ProcessId)); }
            using var client = new TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port);
            await client.GetStream().WriteAsync(new byte[] { 5, 1, 0 }); var response = new byte[2];
            await client.GetStream().ReadExactlyAsync(response); Assert.Equal(new byte[] { 5, 0 }, response);
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(directory, "candidate-tests"), "config.json", SearchOption.AllDirectories));
        }
        finally { await connection.DisconnectAsync(); }
        Assert.Null(backend.OwnedProcessId); Assert.False(await CanConnectAsync(port));
    }
    [Fact]
    public async Task CancellationCleansTemporaryCandidateConfigurationAndListener()
    {
        using var cancellation = new CancellationTokenSource();
        var tester = Tester; CandidateSession? session = null;
        tester.CandidateReady += (_, ready) => { session = ready; cancellation.Cancel(); };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tester.TestAsync(Fixture("cancel"), CoreKind.Xray, cancellation.Token));
        Assert.NotNull(session); Assert.False(await CanConnectAsync(session.Port)); Assert.False(IsRunning(session.ProcessId));
        Assert.Empty(Directory.EnumerateFiles(directory, "config.json", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateDirectories(Path.Combine(directory, "candidate-tests")));
    }
    [Fact]
    public async Task SubscriptionReplacementPreservesFavoriteCountryStableIdAndMode()
    {
        var download = new Download { Value = Uri }; var store = new Store(ProfileLibrary.Empty);
        var library = new LibraryService(store, validator, download); await library.InitializeAsync();
        await library.AddSubscriptionAsync("offline", "https://example.invalid/subscription"); var id = library.Current.Subscriptions[0].Id;
        await library.UpdateSubscriptionAsync(id); var original = library.Current.Profiles[0].Server.Id;
        await library.ToggleFavoriteAsync(original); await library.SetCountryAsync(original, "FI"); await library.SetSelectionModeAsync(SelectionMode.Auto);
        await library.UpdateSubscriptionAsync(id);
        Assert.Single(library.Current.Profiles); Assert.Equal(original, library.Current.Profiles[0].Server.Id);
        Assert.True(library.Current.Profiles[0].IsFavorite); Assert.Equal("FI", library.Current.Profiles[0].Server.Country);
        Assert.Equal(SelectionMode.Auto, library.Current.SelectionMode);
        download.Value = Uri.Replace("Offline-isolation-fixture", "Renamed-fixture");
        await library.UpdateSubscriptionAsync(id); Assert.Equal(original, library.Current.Profiles[0].Server.Id); Assert.True(library.Current.Profiles[0].IsFavorite); Assert.Equal("FI", library.Current.Profiles[0].Server.Country);
        var reopened = new LibraryService(store, validator, download); await reopened.InitializeAsync(); Assert.True(reopened.Current.Profiles[0].IsFavorite);
    }
    [Fact]
    public async Task EncryptedFavoriteAndAutoPreferenceSurviveReloadAndOlderSchemaDefaultsToManual()
    {
        var store = new EncryptedLibraryStore(directory);
        await store.SaveAsync(ProfileLibrary.Empty with { Profiles = [Fixture("favorite") with { IsFavorite = true }], SelectionMode = SelectionMode.Auto });
        var loaded = await store.LoadAsync(); Assert.True(loaded.Profiles[0].IsFavorite); Assert.Equal(SelectionMode.Auto, loaded.SelectionMode);
        var old = System.Text.Json.JsonSerializer.Deserialize<ProfileLibrary>("{\"SchemaVersion\":1,\"Profiles\":[],\"Subscriptions\":[],\"Core\":0}")!;
        Assert.Equal(SelectionMode.Manual, old.SelectionMode);
    }
    private static async Task<bool> CanConnectAsync(int port) { try { using var c = new TcpClient(); await c.ConnectAsync(IPAddress.Loopback, port); return true; } catch (SocketException) { return false; } }
    private static bool IsRunning(int pid) { try { using var p = System.Diagnostics.Process.GetProcessById(pid); return !p.HasExited; } catch (ArgumentException) { return false; } }
    private sealed class Store(ProfileLibrary value) : ILibraryStore
    {
        public Task<ProfileLibrary> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(value);
        public Task SaveAsync(ProfileLibrary library, CancellationToken cancellationToken = default) { value = library; return Task.CompletedTask; }
    }
    private sealed class Download : ISubscriptionDownloader { public string Value = ""; public Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default) => Task.FromResult(Value); }
    private sealed class Network : INetworkContext { public string Fingerprint => "controlled-fixture"; }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
