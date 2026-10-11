using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;
using Microsoft.Extensions.Logging;
namespace AJ.VPN.IntegrationTests;
public sealed class FoundationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "VPN-AJ-tests", Guid.NewGuid().ToString("N"));
    [Fact]
    public async Task SettingsRoundTripAndCancelledSavePreservesPreviousFile()
    {
        var store = new JsonSettingsService(directory);
        Assert.Equal(new ApplicationSettings(), await store.LoadAsync());
        var original = new ApplicationSettings(1, ThemePreference.Dark, "fa");
        await store.SaveAsync(original);
        Assert.Equal(original, await store.LoadAsync());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(new(), cancellation.Token));
        Assert.Equal(original, await store.LoadAsync());
    }
    [Fact]
    public async Task UnknownSchemaIsRejectedWithoutRewritingData()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        const string future = "{\"SchemaVersion\":99,\"Theme\":0,\"Language\":\"en\"}";
        await File.WriteAllTextAsync(path, future);
        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonSettingsService(directory).LoadAsync());
        Assert.Equal(future, await File.ReadAllTextAsync(path));
    }
    [Fact]
    public void ActualServiceLibParserReturnsMetadataWithoutCredentials()
    {
        var validator = new ServiceLibProfileValidator();
        // Reserved domain and fixed test UUID: never an actual server or credential.
        var result = validator.Validate("vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=tls&type=tcp#fixture");
        Assert.True(result.IsValid, result.Message);
        Assert.Equal("example.invalid", result.Server!.Host);
        Assert.Equal(443, result.Server.Port);
        Assert.Null(result.Server.Country);
        Assert.DoesNotContain("00000000", result.Message);
    }
    [Theory]
    [InlineData("https://example.invalid/?token=private")]
    [InlineData("vless://invalid")]
    [InlineData("{\"outbounds\":[]}")]
    public void UnsupportedOrMalformedProfilesAreRejectedWithoutEchoingSecrets(string input)
    {
        var result = new ServiceLibProfileValidator().Validate(input);
        Assert.False(result.IsValid);
        Assert.DoesNotContain("private", result.Message);
        Assert.Null(result.Server);
    }
    [Fact]
    public void LoggerNeverWritesMessageExceptionOrScopePayloads()
    {
        using var provider = new SafeFileLoggerProvider(directory);
        var logger = provider.CreateLogger("secret-category");
        using (logger.BeginScope("secret-scope"))
            logger.LogError(new EventId(42), new Exception("secret-exception"), "secret-token https://example.invalid/token");
        var content = File.ReadAllText(Path.Combine(directory, "events.jsonl"));
        Assert.DoesNotContain("secret", content);
        Assert.Contains("42", content);
    }
    public void Dispose()
    {
        // Only this test's unique directory is owned by the fixture.
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
