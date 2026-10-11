using AJ.VPN.Infrastructure;

namespace AJ.VPN.IntegrationTests;

public sealed class ProxyRestorationTests
{
    private sealed class MemoryBackend : IProxySettingsBackend
    {
        public Dictionary<string, ProxyValue?> Values = new()
        {
            ["ProxyEnable"] = new(4, Number: 0), ["ProxyServer"] = new(1, Text: "previous.invalid:8080"), ["ProxyOverride"] = null,
            ["AutoConfigURL"] = new(1, Text: "https://example.invalid/pac?token=offline-fixture"), ["AutoDetect"] = new(4, Number: 1),
            ["Connections/DefaultConnectionSettings"] = new(3, Bytes: Blob()), ["Connections/SavedLegacySettings"] = null
        };
        public int Writes; public int FailAt;
        public Action? AfterWrite;
        public Dictionary<string, ProxyValue?> Read() => new(Values);
        public void Write(string name, ProxyValue? value) { Writes++; if (Writes == FailAt) throw new IOException(); Values[name] = value; AfterWrite?.Invoke(); }
        public void Notify() { }
    }
    private sealed class Journal : IProxyJournalStore
    {
        public ProxyJournal? Value;
        public Action? AfterSave;
        public Task<ProxyJournal?> ReadAsync() => Task.FromResult(Value);
        public Task WriteAsync(ProxyJournal value) { Value = value; AfterSave?.Invoke(); return Task.CompletedTask; }
        public void Delete() => Value = null;
    }
    private static byte[] Blob()
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(0x46); writer.Write(17); writer.Write(13);
        foreach (var text in new[] { "previous.invalid:8080", "<local>", "https://example.invalid/pac" })
        { var bytes = System.Text.Encoding.ASCII.GetBytes(text); writer.Write(bytes.Length); writer.Write(bytes); }
        writer.Write(new byte[32]); return stream.ToArray();
    }
    private static string Snapshot(Dictionary<string, ProxyValue?> value) => System.Text.Json.JsonSerializer.Serialize(value);
    [Fact]
    public async Task NoApprovalMeansNoRegistryOrJournalMutation()
    {
        var backend = new MemoryBackend(); var journal = new Journal(); var proxy = new SystemProxyService(backend, journal);
        await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.ApplyAsync(19080, false));
        Assert.Equal(0, backend.Writes); Assert.Null(journal.Value);
    }
    [Fact]
    public async Task ManualPacDiscoveryBinaryAndAbsentValuesRoundTripExactly()
    {
        var backend = new MemoryBackend(); var previous = Snapshot(backend.Values); var journal = new Journal();
        var proxy = new SystemProxyService(backend, journal); await proxy.ApplyAsync(19080, true);
        Assert.Equal(1, backend.Values["ProxyEnable"]!.Number); Assert.Null(backend.Values["AutoConfigURL"]);
        Assert.Equal(3, BitConverter.ToInt32(backend.Values["Connections/DefaultConnectionSettings"]!.Bytes!, 8));
        Assert.Contains("127.0.0.1:19080", System.Text.Encoding.ASCII.GetString(backend.Values["Connections/DefaultConnectionSettings"]!.Bytes!));
        Assert.DoesNotContain("previous.invalid", System.Text.Encoding.ASCII.GetString(backend.Values["Connections/DefaultConnectionSettings"]!.Bytes!));
        Assert.True(proxy.IsApplied); await proxy.RestoreAsync();
        Assert.Equal(previous, Snapshot(backend.Values)); Assert.Null(journal.Value);
        await proxy.RestoreAsync(); Assert.Equal(previous, Snapshot(backend.Values));
    }
    [Fact]
    public async Task CrashRecoveryRequiresExplicitApproval()
    {
        var backend = new MemoryBackend(); var previous = Snapshot(backend.Values); var journal = new Journal();
        await new SystemProxyService(backend, journal).ApplyAsync(19080, true);
        var recovery = new SystemProxyService(backend, journal); var writes = backend.Writes;
        await recovery.RestoreAsync(); Assert.Equal(writes, backend.Writes); Assert.True(await recovery.HasRecoveryAsync());
        await recovery.RestoreAsync(true); Assert.Equal(previous, Snapshot(backend.Values)); Assert.Null(journal.Value);
    }
    [Fact]
    public async Task ConcurrentExternalProxyChangeIsPreserved()
    {
        var backend = new MemoryBackend(); var journal = new Journal(); var proxy = new SystemProxyService(backend, journal);
        await proxy.ApplyAsync(19080, true); backend.Values["ProxyServer"] = new(1, Text: "external.invalid:9000");
        var external = Snapshot(backend.Values); var message = await proxy.RestoreAsync();
        Assert.Contains("preserved", message); Assert.Equal(external, Snapshot(backend.Values)); Assert.NotNull(journal.Value);
        await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.ApplyAsync(19081, true));
    }
    [Theory, InlineData(1), InlineData(3), InlineData(6)]
    public async Task PartialApplyFailureRollsBackWrittenValues(int failAt)
    {
        var backend = new MemoryBackend { FailAt = failAt }; var previous = Snapshot(backend.Values); var journal = new Journal();
        await Assert.ThrowsAsync<IOException>(() => new SystemProxyService(backend, journal).ApplyAsync(19080, true));
        Assert.Equal(previous, Snapshot(backend.Values)); Assert.Null(journal.Value);
    }
    [Fact]
    public async Task UnknownBinaryFormatIsRejectedBeforeAnyMutation()
    {
        var backend = new MemoryBackend(); backend.Values["Connections/DefaultConnectionSettings"] = new(3, Bytes: new byte[32]);
        var journal = new Journal(); await Assert.ThrowsAsync<InvalidDataException>(() => new SystemProxyService(backend, journal).ApplyAsync(19080, true));
        Assert.Equal(0, backend.Writes); Assert.Null(journal.Value);
    }
    [Fact]
    public async Task ExternalChangeDuringJournalPersistenceIsNotOverwritten()
    {
        var backend = new MemoryBackend();
        var journal = new Journal { AfterSave = () => backend.Values["ProxyServer"] = new(1, Text: "external.invalid:9000") };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SystemProxyService(backend, journal).ApplyAsync(19080, true));
        Assert.Equal("external.invalid:9000", backend.Values["ProxyServer"]!.Text);
        Assert.Equal(0, backend.Values["ProxyEnable"]!.Number); Assert.Null(journal.Value);
    }
    [Fact]
    public async Task CancellationDuringApplyRestoresPreviousValues()
    {
        using var cancellation = new CancellationTokenSource();
        var backend = new MemoryBackend(); var previous = Snapshot(backend.Values);
        backend.AfterWrite = () => { if (backend.Writes == 2) cancellation.Cancel(); };
        var journal = new Journal();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SystemProxyService(backend, journal).ApplyAsync(19080, true, cancellation.Token));
        Assert.Equal(previous, Snapshot(backend.Values)); Assert.Null(journal.Value);
    }
}
