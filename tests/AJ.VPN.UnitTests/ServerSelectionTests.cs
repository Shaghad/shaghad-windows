using AJ.VPN.Application;
using AJ.VPN.Domain;
namespace AJ.VPN.UnitTests;

public sealed class ServerSelectionTests
{
    private static SavedProfile Profile(string id, bool favorite = false) => new(new(id, "Server " + id, "VLESS", "example.invalid", 443, "FI"), "fixture:" + id, IsFavorite: favorite);
    private static ServerTestResult Result(string id, ServerHealth health, long? ms) => new(id, health, ms, DateTimeOffset.UtcNow, "Controlled fixture", ms == null ? 0 : 2);
    [Fact] public void RankingExcludesFailuresTimeoutsUnsupportedCancelledAndMissingMeasurements()
    {
        var values = new[] { Result("fail", ServerHealth.Failed, 0), Result("timeout", ServerHealth.Timeout, 0), Result("unsupported", ServerHealth.Unsupported, 0), Result("cancelled", ServerHealth.Cancelled, 0), Result("missing", ServerHealth.Healthy, null), Result("slow", ServerHealth.Healthy, 90), Result("fast", ServerHealth.Degraded, 20) };
        Assert.Equal("fast", ServerSelectionService.Rank(values)!.ProfileId);
    }
    [Fact] public void TiesAreDeterministicAndEmptyHasNoFallback()
    {
        Assert.Equal("a", ServerSelectionService.Rank([Result("z", ServerHealth.Healthy, 40), Result("a", ServerHealth.Healthy, 40)])!.ProfileId);
        Assert.Null(ServerSelectionService.Rank([])); Assert.Null(ServerSelectionService.Rank([Result("bad", ServerHealth.Failed, null)]));
    }
    [Fact] public async Task FullScopeUsesLowestLatencyAndReusesRecentCache()
    {
        var tester = new Tester(); var service = new ServerSelectionService(tester, new Validator(), new Network());
        var profiles = new[] { Profile("b"), Profile("a") };
        var first = await service.EvaluateAsync(profiles, CoreKind.Xray, false);
        Assert.True(first.CompleteScope); Assert.Equal("a", first.ResolvedServerId); Assert.Equal(2, tester.Calls);
        await service.EvaluateAsync(profiles, CoreKind.Xray, false); Assert.Equal(2, tester.Calls);
        await service.EvaluateAsync(profiles, CoreKind.Xray, true); Assert.Equal(4, tester.Calls);
    }
    [Fact] public async Task ExpiryConfigurationCoreEndpointContextAndRemovalInvalidateCache()
    {
        var time = new Clock(); var network = new Network(); var tester = new Tester();
        var service = new ServerSelectionService(tester, new Validator(), network, clock: time);
        var p = Profile("a"); await service.EvaluateAsync([p], CoreKind.Xray, false);
        time.Seconds = 300; Assert.Null(service.Recent(p, CoreKind.Xray));
        await service.EvaluateAsync([p], CoreKind.Xray, false);
        Assert.Null(service.Recent(p with { ShareUri = "changed" }, CoreKind.Xray));
        await service.EvaluateAsync([p], CoreKind.Xray, false); Assert.Null(service.Recent(p, CoreKind.SingBox));
        await service.EvaluateAsync([p], CoreKind.Xray, false); network.Value = "changed-network"; Assert.Null(service.Recent(p, CoreKind.Xray));
        await service.EvaluateAsync([p], CoreKind.Xray, false); service.RetainProfiles([]); Assert.Null(service.Recent(p, CoreKind.Xray));
        Assert.StartsWith("https://", ServerSelectionService.MeasurementEndpoint);
    }
    [Fact] public async Task CompatibilityIsFilteredBeforeTesterAndNoHealthyHasNoFallback()
    {
        var tester = new Tester(); var service = new ServerSelectionService(tester, new Validator { Unsupported = true }, new Network());
        var result = await service.EvaluateAsync([Profile("a")], CoreKind.SingBox, false);
        Assert.Null(result.ResolvedServerId); Assert.Equal(0, tester.Calls); Assert.Equal(ServerHealth.Unsupported, result.Results[0].Health);
    }
    [Fact] public async Task BoundedConcurrentWorkAndCancellationAreHonored()
    {
        var tester = new Tester { Delay = 100 }; var service = new ServerSelectionService(tester, new Validator(), new Network(), new(2));
        await service.EvaluateAsync(Enumerable.Range(0, 15).Select(i => Profile(i.ToString())).ToArray(), CoreKind.Xray, true);
        Assert.InRange(tester.Maximum, 1, 2);
        using var token = new CancellationTokenSource(20);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.EvaluateAsync([Profile("a"), Profile("b")], CoreKind.Xray, true, token: token.Token));
        Assert.Equal(0, tester.Running);
    }
    [Fact] public async Task EmptyScopeDoesNotStartCores()
    {
        var tester = new Tester(); var result = await new ServerSelectionService(tester, new Validator(), new Network()).EvaluateAsync([], CoreKind.Xray, false);
        Assert.Empty(result.Results); Assert.Null(result.ResolvedServerId); Assert.True(result.CompleteScope); Assert.Equal(0, tester.Calls);
    }
    [Theory]
    [InlineData(ServerSort.LowestLatency)] [InlineData(ServerSort.HighestLatency)]
    public void FailedAndUntestedAlwaysSortAfterVerifiedLatency(ServerSort sort)
    {
        Assert.True(ServerBrowser.Compare(Profile("a"), Result("a", ServerHealth.Healthy, 400), Profile("b"), null, sort, false) < 0);
        Assert.True(ServerBrowser.Compare(Profile("a"), Result("a", ServerHealth.Timeout, null), Profile("b"), Result("b", ServerHealth.Healthy, 5), sort, false) > 0);
    }
    [Fact] public void SearchCountryProtocolNameAndFavoritesWork()
    {
        var p = Profile("a", true);
        foreach (var search in new[] { "server a", "vless", "FI", "Finland" }) Assert.True(ServerBrowser.Matches(p, null, search, ServerFilter.All));
        Assert.False(ServerBrowser.Matches(p, null, "unknown", ServerFilter.All));
        Assert.True(ServerBrowser.Matches(p, null, "", ServerFilter.Favorites));
        Assert.True(ServerBrowser.Matches(p, null, "", ServerFilter.Untested));
        Assert.False(ServerBrowser.Matches(p, null, "", ServerFilter.Healthy));
        Assert.True(ServerBrowser.Compare(p, null, Profile("b"), null, ServerSort.Name, true) < 0);
    }
    [Fact] public async Task CacheIsBoundedAndChangingNetworkDuringTestingCannotResolve()
    {
        var network = new Network(); var tester = new Tester { OnTest = () => network.Value += "x" };
        var service = new ServerSelectionService(tester, new Validator(), network, new(1, 2));
        var result = await service.EvaluateAsync([Profile("a"), Profile("b"), Profile("c")], CoreKind.Xray, false);
        Assert.False(result.CompleteScope); Assert.Null(result.ResolvedServerId);
    }
    [Fact] public async Task AutoEvaluationDoesNotMutateManualSelectionAndModePersists()
    {
        var store = new Store(); var library = new LibraryService(store, new Validator(), new Download());
        await library.InitializeAsync(); await library.SelectAsync("b");
        await new ServerSelectionService(new Tester(), new Validator(), new Network()).EvaluateAsync(library.Current.Profiles, CoreKind.Xray, false);
        Assert.Equal("b", library.Current.ActiveId); Assert.Equal(SelectionMode.Manual, library.Current.SelectionMode);
        await library.SetSelectionModeAsync(SelectionMode.Auto); await library.ToggleFavoriteAsync("a");
        var reopened = new LibraryService(store, new Validator(), new Download()); await reopened.InitializeAsync();
        Assert.Equal(SelectionMode.Auto, reopened.Current.SelectionMode); Assert.True(reopened.Current.Profiles[0].IsFavorite);
    }
    [Fact] public async Task ManualPolicyReturnsExplicitSelectionWithoutTestingOtherServers()
    {
        var tester = new Tester(); var service = new ServerSelectionService(tester, new Validator(), new Network());
        var library = ProfileLibrary.Empty with { Profiles = [Profile("a"), Profile("b")], ActiveId = "b" };
        Assert.Equal("b", (await service.ResolveForConnectionAsync(library)).ResolvedServerId); Assert.Equal(0, tester.Calls);
        Assert.Equal("a", (await service.ResolveForConnectionAsync(library with { SelectionMode = SelectionMode.Auto })).ResolvedServerId); Assert.Equal(2, tester.Calls);
        Assert.Equal("b", library.ActiveId);
    }
    [Fact] public async Task CancelledForcedRetestInvalidatesQueuedOldResults()
    {
        var tester = new Tester(); var service = new ServerSelectionService(tester, new Validator(), new Network(), new(1));
        var profiles = Enumerable.Range(0, 5).Select(i => Profile(i.ToString())).ToArray();
        await service.EvaluateAsync(profiles, CoreKind.Xray, false);
        tester.Delay = 100;
        using var cancellation = new CancellationTokenSource(20);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.EvaluateAsync(profiles, CoreKind.Xray, true, token: cancellation.Token));
        Assert.All(profiles, p => Assert.Null(service.Recent(p, CoreKind.Xray)));
    }
    private sealed class Tester : ICandidateTester
    {
        public int Calls, Running, Maximum, Delay; public Action? OnTest;
        public async Task<ServerTestResult> TestAsync(SavedProfile p, CoreKind core, CancellationToken token)
        {
            Interlocked.Increment(ref Calls); var count = Interlocked.Increment(ref Running); lock (this) Maximum = Math.Max(count, Maximum);
            try { if (Delay > 0) await Task.Delay(Delay, token); OnTest?.Invoke(); return Result(p.Server.Id, ServerHealth.Healthy, p.Server.Id == "a" ? 20 : 50); }
            finally { Interlocked.Decrement(ref Running); }
        }
    }
    private sealed class Validator : IProfileValidator
    {
        public bool Unsupported;
        public ProfileValidation Validate(string input) => new(true, Profile("fixture").Server, "fixture");
        public string? CoreCompatibilityError(string input, CoreKind kind) => Unsupported ? "Unsupported fixture" : null;
    }
    private sealed class Network : INetworkContext { public string Value = "fixture"; public string Fingerprint => Value; }
    private sealed class Clock : TimeProvider { public long Seconds; public override long TimestampFrequency => 1; public override long GetTimestamp() => Seconds; }
    private sealed class Store : ILibraryStore
    {
        public ProfileLibrary Value = ProfileLibrary.Empty with { Profiles = [Profile("a"), Profile("b")] };
        public Task<ProfileLibrary> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Value);
        public Task SaveAsync(ProfileLibrary library, CancellationToken cancellationToken = default) { Value = library; return Task.CompletedTask; }
    }
    private sealed class Download : ISubscriptionDownloader { public Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default) => throw new InvalidOperationException(); }
}
