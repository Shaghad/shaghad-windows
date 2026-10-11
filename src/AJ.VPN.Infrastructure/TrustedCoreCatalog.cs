using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using AJ.VPN.Application;
using AJ.VPN.Domain;

namespace AJ.VPN.Infrastructure;

public sealed class TrustedCoreCatalog(string coreDirectory) : ICoreCatalog
{
    private sealed record Manifest(CoreKind Kind, string Version, string Sha256, string RelativePath);
    private readonly Dictionary<CoreKind, CoreInstallation> verifiedVersions = [];
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task<CoreInstallation> FindAsync(CoreKind kind, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
        using var manifestStream = typeof(TrustedCoreCatalog).Assembly.GetManifestResourceStream("AJ.VPN.Infrastructure.core-manifest.json")!;
        var entries = await JsonSerializer.DeserializeAsync<List<Manifest>>(manifestStream, cancellationToken: cancellationToken);
        var entry = entries!.Single(e => e.Kind == kind);
        var path = Path.GetFullPath(Path.Combine(coreDirectory, entry.RelativePath));
        await using var held = await OpenVerifiedAsync(path, entry.Sha256, cancellationToken);
        if (verifiedVersions.TryGetValue(kind, out var cached)) return cached;
        var version = await RunCheckAsync(path, ["version"], cancellationToken);
        if (version.ExitCode != 0 || !System.Text.RegularExpressions.Regex.IsMatch(version.Output, @"(?<![\d.])" + System.Text.RegularExpressions.Regex.Escape(entry.Version) + @"(?![\d.])"))
            throw new InvalidDataException("The core version does not match the trusted release.");
        var installation = new CoreInstallation(kind, path, entry.Version, entry.Sha256);
        verifiedVersions[kind] = installation;
        return installation;
        }
        finally { gate.Release(); }
    }
    public static async Task<FileStream> OpenVerifiedAsync(string path, string hash, CancellationToken token)
    {
        for (var item = new FileInfo(path) as FileSystemInfo; item != null; item = item is FileInfo f ? f.Directory : ((DirectoryInfo)item).Parent)
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Core reparse paths are not accepted.");
        FileStream stream;
        var started = Stopwatch.StartNew();
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try { stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); break; }
            catch (IOException ex) when ((ex.HResult & 0xffff) is 32 or 33 && started.Elapsed < TimeSpan.FromSeconds(5))
            { await Task.Delay(100, token); }
        }
        try
        {
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
            if (!actual.Equals(hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Core integrity verification failed. Install the pinned official release.");
            stream.Position = 0; return stream;
        }
        catch { stream.Dispose(); throw; }
    }
    public static async Task<(int ExitCode, string Output)> RunCheckAsync(string executable, string[] arguments, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var process = new Process { StartInfo = new(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true } };
        foreach (var arg in arguments) process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        static async Task<string> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
        {
            var captured = new System.Text.StringBuilder(); var buffer = new char[1024]; int n;
            while ((n = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0) if (captured.Length < 4096) captured.Append(buffer, 0, Math.Min(n, 4096 - captured.Length));
            return captured.ToString();
        }
        var stdout = ReadBoundedAsync(process.StandardOutput, timeout.Token); var stderr = ReadBoundedAsync(process.StandardError, timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token); return (process.ExitCode, await stdout + await stderr); }
        catch { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(CancellationToken.None); try { await Task.WhenAll(stdout, stderr); } catch { } throw; }
    }
}
