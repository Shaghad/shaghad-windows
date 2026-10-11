using System.IO;
using System.Text.Json;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;
using Microsoft.Extensions.Logging;

namespace AJ.VPN.Desktop;

/// <summary>Explicit authorized profile tests in isolated encrypted storage. No system proxy writes.</summary>
internal static class ProductionConnectivityTest
{
    public static async Task RunAsync(string requestFile, string output)
    {
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new InvalidOperationException("Use a new empty output directory.");
        ProtectedFiles.CreatePrivateDirectory(output);
        var results = new List<object>();
        var stage = "subscription download";
        try
        {
            var downloader = new SubscriptionDownloader();
            var url = (await File.ReadAllTextAsync(requestFile)).Trim();
            var input = await downloader.DownloadAsync(url);
            stage = "profile validation";
            var validator = new ServiceLibProfileValidator();
            var library = new LibraryService(new EncryptedLibraryStore(output), validator, downloader);
            await library.InitializeAsync();
            await library.ImportAsync(input);
            using var logs = new SafeFileLoggerProvider(Path.Combine(output, "logs"));
            using var factory = LoggerFactory.Create(b => b.AddProvider(logs));
            using var backend = new LocalProxyBackend(library,
                new TrustedCoreCatalog(Path.Combine(AppContext.BaseDirectory, "cores")),
                new ServiceLibConfigurationGenerator(validator), output,
                Path.Combine(AppContext.BaseDirectory, "Shaghad.exe"), factory.CreateLogger<LocalProxyBackend>());
            var connection = new ConnectionService(backend);
            // One authorized profile per available protocol, with both installed engines.
            var profiles = library.Current.Profiles.GroupBy(p => p.Server.Protocol).Select(g => g.First()).ToArray();
            foreach (var kind in Enum.GetValues<CoreKind>())
            foreach (var profile in profiles)
            {
                stage = "core startup";
                await library.SelectAsync(profile.Server.Id);
                await library.SetCoreAsync(kind);
                try
                {
                    await connection.ConnectAsync(profile.Server);
                    results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol.ToString(), check = stage, passed = true });
                    foreach (var socks in new[] { false, true })
                    {
                        var probe = await ProxyConnectivityProbe.RunAsync(backend.Port!.Value, socks);
                        results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol.ToString(), check = socks ? "SOCKS5 HTTPS" : "HTTP CONNECT HTTPS", passed = probe.Success, probe.Message });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol.ToString(), check = stage, passed = false, message = ProxyConnectivityProbe.DescribeFailure(ex, false) });
                }
                finally
                {
                    await connection.DisconnectAsync();
                    results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol.ToString(), check = "core shutdown", passed = backend.Port == null && backend.OwnedProcessId == null });
                }
            }
            // Prove an explicit retry can recover after the preceding engine failures.
            if (profiles.Length > 0)
            {
                stage = "manual retry recovery";
                await library.SetCoreAsync(CoreKind.Xray);
                await library.SelectAsync(profiles[0].Server.Id);
                try
                {
                    await connection.ConnectAsync(profiles[0].Server);
                    var retry = await backend.ProbeAsync();
                    results.Add(new { check = stage, passed = retry.Success, retry.Message });
                }
                finally { await connection.DisconnectAsync(); }
            }
        }
        catch (Exception ex)
        {
            results.Add(new { check = stage, passed = false, message = ProxyConnectivityProbe.DescribeFailure(ex, false), exceptionType = ex.GetType().Name });
        }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new
            {
                results,
                unverified = new[] { "DNS resolution path/leaks", "IPv4 remote egress", "IPv6 remote egress", "plaintext HTTP requests", "live WinINet/browser/restoration/crash/external changes" }
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
