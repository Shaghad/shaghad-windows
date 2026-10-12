using AJ.VPN.Application;

namespace AJ.VPN.UnitTests;
public sealed class IpComparisonTests
{
    [Fact]
    public async Task NoConsentNeverMeasures()
    {
        var calls = 0;
        var result = await IpComparison.MeasureAsync(false, () => { calls++; return Task.FromResult<string?>("8.8.8.8"); }, () => { calls++; return Task.FromResult<string?>("1.1.1.1"); });
        Assert.Equal(0, calls); Assert.Equal(IpDifference.NotConsented, result.Difference);
    }
    [Theory]
    [InlineData("8.8.8.8", "1.1.1.1", IpDifference.Changed)]
    [InlineData("8.8.8.8", "8.8.8.8", IpDifference.Same)]
    [InlineData("2606:4700::1111", "2606:4700:0:0:0:0:0:1111", IpDifference.Same)]
    [InlineData(null, "8.8.8.8", IpDifference.Unavailable)]
    [InlineData("not an ip", "8.8.8.8", IpDifference.Unavailable)]
    public async Task DifferenceRequiresTwoValidMeasurements(string? direct, string? proxy, IpDifference expected) =>
        Assert.Equal(expected, (await IpComparison.MeasureAsync(true, () => Task.FromResult(direct), () => Task.FromResult(proxy))).Difference);
    [Fact]
    public async Task FailedMeasurementDoesNotClaimChanged() => Assert.Equal(IpDifference.Unavailable,
        (await IpComparison.MeasureAsync(true, () => throw new HttpRequestException(), () => Task.FromResult<string?>("8.8.8.8"))).Difference);
}
