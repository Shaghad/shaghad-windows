using AJ.VPN.Infrastructure;
namespace AJ.VPN.IntegrationTests;
public sealed class SubscriptionMetadataTests
{
    [Fact] public void OnlyCompleteProviderTrafficIsDisplayed()
    {
        Assert.Equal(850L, SubscriptionDownloader.ParseUsage("upload=50; download=100; total=1000").Remaining);
        Assert.Null(SubscriptionDownloader.ParseUsage("total=1000").Remaining);
        Assert.Null(SubscriptionDownloader.ParseUsage("upload=-50; download=100; total=1000").Remaining);
    }
    [Fact] public void InvalidExpirationAndUnlimitedTrafficStayUnavailable()
    {
        var result = SubscriptionDownloader.ParseUsage("upload=0; download=0; total=0; expire=9223372036854775807");
        Assert.Null(result.Remaining); Assert.Null(result.Expires);
    }
    [Fact] public void TrafficSubtractionDoesNotOverflow()
    {
        Assert.Equal(0L, SubscriptionDownloader.ParseUsage("upload=9223372036854775807; download=9223372036854775807; total=1000").Remaining);
    }
    [Fact] public void ExpirationUsesActualProviderTimestamp()
    {
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), SubscriptionDownloader.ParseUsage("expire=1700000000").Expires);
    }
}
