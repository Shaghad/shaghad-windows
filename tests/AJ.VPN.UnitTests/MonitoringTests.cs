using AJ.VPN.Application;
using AJ.VPN.Domain;

namespace AJ.VPN.UnitTests;
public sealed class MonitoringTests
{
    private sealed class Clock : TimeProvider
    {
        public long Ticks;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => Ticks;
    }
    [Fact] public void RatesUseMonotonicCounterDifferences()
    {
        var clock = new Clock(); var calculator = new TrafficRateCalculator(clock);
        Assert.Null(calculator.Update(new("a", 100, 20))!.DownloadPerSecond);
        clock.Ticks = 2000;
        var rate = calculator.Update(new("a", 500, 120))!;
        Assert.Equal(200, rate.DownloadPerSecond); Assert.Equal(50, rate.UploadPerSecond);
    }
    [Fact] public void ResetRestartMissingAndOverflowNeverProduceNegativeRates()
    {
        var clock = new Clock(); var calculator = new TrafficRateCalculator(clock);
        calculator.Update(new("a", long.MaxValue, long.MaxValue)); clock.Ticks = 1000;
        Assert.Null(calculator.Update(new("a", 1, 1))!.DownloadPerSecond); clock.Ticks += 1000;
        Assert.Null(calculator.Update(new("b", 10, 10))!.DownloadPerSecond);
        Assert.Null(calculator.Update(null)); clock.Ticks += 1000;
        Assert.Null(calculator.Update(new("b", 20, 20))!.DownloadPerSecond);
        Assert.Null(calculator.Update(new("b", -1, 20)));
    }
    [Fact] public void XrayReadsOnlyOwnedProxyOutboundCounters()
    {
        var sample = TrafficCounterParser.ParseXray("""{"stat":[{"name":"outbound>>>proxy>>>traffic>>>downlink","value":"123"},{"name":"outbound>>>proxy>>>traffic>>>uplink","value":45},{"name":"outbound>>>direct>>>traffic>>>downlink","value":"99999"}]}""", "session");
        Assert.Equal(new TrafficCounters("session", 123, 45), sample);
        Assert.Null(TrafficCounterParser.ParseXray("{}", "a"));
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"downloadTotal\":-1,\"uploadTotal\":2}")]
    public void MissingOrInvalidSingBoxTelemetryIsUnavailable(string json) => Assert.Null(TrafficCounterParser.ParseSingBox(json, "a"));
    [Fact] public void SingBoxParsesNativeTotals() => Assert.Equal(new TrafficCounters("a", 100, 20), TrafficCounterParser.ParseSingBox("{\"downloadTotal\":100,\"uploadTotal\":20}", "a"));
    [Theory]
    [InlineData(100, "Excellent")][InlineData(400, "Good")][InlineData(900, "Degraded")]
    public void QualityUsesVerifiedHttpsLatency(long ms, string quality) => Assert.Equal(quality, ConnectionQuality.Classify(ConnectionState.Connected, new(true, ms, "fixture")));
    [Fact] public void ReadinessAloneIsUnverified()
    {
        Assert.Equal("Unverified", ConnectionQuality.Classify(ConnectionState.Connected, null));
        Assert.Equal("Offline", ConnectionQuality.Classify(ConnectionState.Disconnected, new(true, 1, "fixture")));
    }
    private static ServerTestResult Healthy(string id, int ms) => new(id, ServerHealth.Healthy, ms, DateTimeOffset.UtcNow, "fixture");
    [Fact] public void FailoverRequiresOptInAutoModeAndConsecutiveFailures()
    {
        var policy = new FailoverPolicy(); var candidates = new[] { Healthy("b", 20) };
        policy.Record(false); policy.Record(false);
        Assert.Null(policy.Choose(true, SelectionMode.Auto, "a", candidates));
        policy.Record(true); policy.Record(false); policy.Record(false);
        Assert.Null(policy.Choose(true, SelectionMode.Auto, "a", candidates)); policy.Record(false);
        Assert.Null(policy.Choose(false, SelectionMode.Auto, "a", candidates));
        Assert.Null(policy.Choose(true, SelectionMode.Manual, "a", candidates));
        Assert.Equal("b", policy.Choose(true, SelectionMode.Auto, "a", candidates));
    }
    [Fact] public void CooldownExclusionsAndBudgetBoundSwitches()
    {
        var clock = new Clock(); var policy = new FailoverPolicy(clock);
        var candidates = new[] { Healthy("a", 1), Healthy("b", 20), Healthy("c", 30) };
        for (var i = 0; i < 3; i++) policy.Record(false);
        Assert.Equal("b", policy.Choose(true, SelectionMode.Auto, "a", candidates)); policy.BeginAttempt("a", "b");
        for (var i = 0; i < 3; i++) policy.Record(false);
        Assert.Null(policy.Choose(true, SelectionMode.Auto, "b", candidates)); clock.Ticks = 120000;
        Assert.Equal("c", policy.Choose(true, SelectionMode.Auto, "b", candidates)); policy.BeginAttempt("b", "c");
        clock.Ticks += 120000; for (var i = 0; i < 3; i++) policy.Record(false);
        Assert.Null(policy.Choose(true, SelectionMode.Auto, "c", new[] { Healthy("d", 1) }));
        policy.Reset(); Assert.False(policy.CanAttempt);
    }
}
