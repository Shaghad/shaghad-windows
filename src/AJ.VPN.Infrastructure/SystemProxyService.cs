using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace AJ.VPN.Infrastructure;

public sealed record ProxyValue(int Kind, string? Text = null, int? Number = null, byte[]? Bytes = null);
public sealed record ProxyJournal(Dictionary<string, ProxyValue?> Previous, Dictionary<string, ProxyValue?> Owned, bool Applied = false);
public interface IProxySettingsBackend
{
    Dictionary<string, ProxyValue?> Read();
    void Write(string name, ProxyValue? value);
    void Notify();
}
public interface IProxyJournalStore
{
    Task<ProxyJournal?> ReadAsync();
    Task WriteAsync(ProxyJournal value);
    void Delete();
}
public sealed class EncryptedProxyJournalStore(string directory) : IProxyJournalStore
{
    private readonly string path = Path.Combine(directory, "secrets", "proxy-recovery.dpapi");
    public Task<ProxyJournal?> ReadAsync() { ProtectedFiles.CreatePrivateDirectory(Path.GetDirectoryName(path)!); return ProtectedFiles.ReadAsync<ProxyJournal>(path); }
    public Task WriteAsync(ProxyJournal value) => ProtectedFiles.WriteAsync(path, value);
    public void Delete() { if (File.Exists(path)) File.Delete(path); }
}
public sealed class SystemProxyService(IProxySettingsBackend backend, IProxyJournalStore store)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private ProxyJournal? active;
    public bool IsApplied => active?.Applied == true;
    public async Task<bool> HasRecoveryAsync() => await store.ReadAsync() != null;
    private static bool Equal(ProxyValue? a, ProxyValue? b) => JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);
    public async Task ApplyAsync(int port, bool userApproved, CancellationToken token = default)
    {
        if (!userApproved) throw new InvalidOperationException("Explicit user approval is required before changing Windows System Proxy.");
        if (port is < 1024 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        await gate.WaitAsync(token);
        try
        {
            if (await store.ReadAsync() != null) throw new InvalidOperationException("Restore or resolve the previous proxy journal before applying new settings.");
            var previous = backend.Read();
            var owned = new Dictionary<string, ProxyValue?>(previous)
            {
                ["ProxyEnable"] = new(4, Number: 1),
                ["ProxyServer"] = new(1, Text: $"127.0.0.1:{port}"),
                ["ProxyOverride"] = new(1, Text: "<local>;localhost;127.*;[::1]"),
                ["AutoConfigURL"] = null, ["AutoDetect"] = new(4, Number: 0)
            };
            foreach (var key in new[] { "Connections/DefaultConnectionSettings", "Connections/SavedLegacySettings" })
                if (previous.TryGetValue(key, out var value) && value != null)
                {
                    if (value.Kind != 3 || value.Bytes is not { Length: >= 12 }) throw new InvalidDataException("Unsupported Windows connection settings; no proxy settings were changed.");
                    owned[key] = value with { Bytes = BuildConnectionBlob(value.Bytes, $"127.0.0.1:{port}", "<local>;localhost;127.*;[::1]") };
                }
            if (previous.GetValueOrDefault("Connections/DefaultConnectionSettings") == null)
                throw new InvalidDataException("Windows LAN connection settings are unavailable; no proxy settings were changed.");
            active = new(previous, owned);
            await store.WriteAsync(active); // Durable, encrypted recovery exists before the first registry write.
            try
            {
                var expected = new Dictionary<string, ProxyValue?>(previous);
                foreach (var key in owned.Keys)
                {
                    token.ThrowIfCancellationRequested();
                    var current = backend.Read();
                    if (expected.Any(p => !current.TryGetValue(p.Key, out var value) || !Equal(value, p.Value)))
                        throw new InvalidOperationException("Windows proxy settings changed during application; external changes were preserved.");
                    backend.Write(key, owned[key]); expected[key] = owned[key];
                }
                backend.Notify();
                var applied = backend.Read();
                if (owned.Any(p => !applied.TryGetValue(p.Key, out var value) || !Equal(value, p.Value)))
                    throw new InvalidOperationException("Windows did not retain the requested proxy settings; external changes were preserved.");
                active = active with { Applied = true }; await store.WriteAsync(active);
            }
            catch { await RestoreLockedAsync(active, true); throw; }
        }
        finally { gate.Release(); }
    }
    public static byte[] BuildConnectionBlob(byte[] original, string server, string bypass)
    {
        // Refuse unknown formats instead of corrupting a managed or future Windows configuration.
        if (original.Length is < 24 or > 65536 || BitConverter.ToInt32(original, 0) != 0x46) throw new InvalidDataException("Unsupported Windows LAN proxy data format.");
        var offset = 12;
        for (var i = 0; i < 3; i++)
        {
            if (offset + 4 > original.Length) throw new InvalidDataException("Invalid Windows LAN proxy data.");
            var length = BitConverter.ToInt32(original, offset); offset += 4;
            if (length < 0 || length > original.Length - offset) throw new InvalidDataException("Invalid Windows LAN proxy data.");
            offset += length;
        }
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output);
        writer.Write(original, 0, 8); writer.Write(3); // DIRECT | PROXY; PAC and auto-detection off.
        foreach (var text in new[] { server, bypass, "" }) { var bytes = System.Text.Encoding.ASCII.GetBytes(text); writer.Write(bytes.Length); writer.Write(bytes); }
        writer.Write(original, offset, original.Length - offset);
        return output.ToArray();
    }
    public async Task<string> RestoreAsync(bool recoveryApproved = false)
    {
        await gate.WaitAsync();
        try
        {
            var journal = active ?? (recoveryApproved ? await store.ReadAsync() : null);
            if (journal == null) return "No proxy changes owned by this session.";
            return await RestoreLockedAsync(journal, !journal.Applied);
        }
        finally { gate.Release(); }
    }
    private Task<string> RestoreLockedAsync(ProxyJournal journal, bool partial)
    {
        var current = backend.Read();
        if (!partial && journal.Owned.Any(pair => !current.TryGetValue(pair.Key, out var value) || !Equal(value, pair.Value)))
            return Task.FromResult("Windows proxy settings changed outside SHAGHAD. They were preserved; recovery journal retained for manual review.");
        foreach (var pair in journal.Previous)
            if (current.TryGetValue(pair.Key, out var value) && Equal(value, journal.Owned[pair.Key])) backend.Write(pair.Key, pair.Value);
        backend.Notify(); store.Delete(); active = null;
        return Task.FromResult("Previous Windows proxy settings restored.");
    }
}
public sealed class WindowsProxySettingsBackend : IProxySettingsBackend
{
    private const string Root = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private static readonly string[] Names = ["ProxyEnable", "ProxyServer", "ProxyOverride", "AutoConfigURL", "AutoDetect", "Connections/DefaultConnectionSettings", "Connections/SavedLegacySettings"];
    public Dictionary<string, ProxyValue?> Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using (var machinePolicy = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings"))
            if (machinePolicy?.GetValue("ProxySettingsPerUser") is int perUser && perUser == 0)
                throw new InvalidOperationException("Machine-managed proxy settings are outside SHAGHAD's current-user scope.");
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var policy = hive.OpenSubKey(@"Software\Policies\Microsoft\Internet Explorer\Control Panel");
            if (policy?.GetValue("Proxy") is int disabled && disabled != 0)
                throw new InvalidOperationException("Proxy changes are restricted by Windows policy.");
        }
        var result = new Dictionary<string, ProxyValue?>();
        foreach (var name in Names)
        {
            var split = name.Split('/');
            using var key = Registry.CurrentUser.OpenSubKey(Root + (split.Length == 2 ? @"\Connections" : ""));
            var valueName = split[^1];
            if (key == null || !key.GetValueNames().Contains(valueName, StringComparer.OrdinalIgnoreCase)) { result[name] = null; continue; }
            var kind = key.GetValueKind(valueName);
            var value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            result[name] = value switch
            {
                int n when kind == RegistryValueKind.DWord => new((int)kind, Number: n),
                string text when kind is RegistryValueKind.String or RegistryValueKind.ExpandString => new((int)kind, Text: text),
                byte[] bytes when kind == RegistryValueKind.Binary => new((int)kind, Bytes: bytes),
                _ => throw new InvalidDataException("Unsupported proxy registry value; no settings were changed.")
            };
        }
        return result;
    }
    public void Write(string name, ProxyValue? value)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (!Names.Contains(name)) throw new ArgumentException("Unsupported proxy setting.");
        var split = name.Split('/');
        using var key = Registry.CurrentUser.CreateSubKey(Root + (split.Length == 2 ? @"\Connections" : ""), true);
        if (value == null) key.DeleteValue(split[^1], false);
        else key.SetValue(split[^1], value.Bytes as object ?? value.Text as object ?? value.Number!.Value, (RegistryValueKind)value.Kind);
    }
    public void Notify()
    {
        if (!InternetSetOptionW(IntPtr.Zero, 39, IntPtr.Zero, 0) || !InternetSetOptionW(IntPtr.Zero, 37, IntPtr.Zero, 0))
            throw new IOException("Windows could not refresh the System Proxy settings.");
    }
    [DllImport("wininet.dll", SetLastError = true)] private static extern bool InternetSetOptionW(IntPtr internet, int option, IntPtr buffer, int length);
}
