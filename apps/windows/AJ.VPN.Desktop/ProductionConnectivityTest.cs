using System.IO;
using System.Diagnostics;
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
    public static async Task<bool> RunAsync(string requestFile, string output)
    {
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new InvalidOperationException("Use a new empty output directory.");
        ProtectedFiles.CreatePrivateDirectory(output);
        var results = new List<object>();
        var stage = "subscription download";
        try
        {
            var downloader = new SubscriptionDownloader();
            var saved = Directory.Exists(requestFile) ? await new EncryptedLibraryStore(requestFile).LoadAsync() : null;
            var input = saved != null ? string.Join('\n', saved.Profiles.Select(p => p.ShareUri)) : await downloader.DownloadAsync((await File.ReadAllTextAsync(requestFile)).Trim());
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
            // Representative authorized protocol/transport combinations, never synthetic remote credentials.
            static string Transport(SavedProfile p) => ServiceLib.Handler.Fmt.FmtHandler.ResolveConfig(p.ShareUri, out _)!.GetNetwork();
            var profiles = library.Current.Profiles.GroupBy(p => (p.Server.Protocol, Transport(p))).Select(g => g.First()).ToArray();
            foreach (var kind in Enum.GetValues<CoreKind>())
            foreach (var profile in profiles)
            {
                stage = "core startup";
                var transport = Transport(profile);
                if (validator.CoreCompatibilityError(profile.ShareUri, kind) is string error)
                {
                    results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol, transport, check = "compatibility", status = "unsupported", message = error });
                    continue;
                }
                try
                {
                    using (var dnsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        try
                        {
                            var addresses = await System.Net.Dns.GetHostAddressesAsync(profile.Server.Host, dnsTimeout.Token);
                            results.Add(new { core = kind.ToString(), transport, check = "local server bootstrap DNS", passed = addresses.Length > 0,
                                ipv4Answers = addresses.Count(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork),
                                ipv6Answers = addresses.Count(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) });
                        }
                        catch { results.Add(new { core = kind.ToString(), transport, check = "local server bootstrap DNS", passed = false }); }
                    }
                    // Reset to the original compatible core before selecting a different profile.
                    await library.SetCoreAsync(CoreKind.Xray);
                    await library.SelectAsync(profile.Server.Id);
                    await library.SetCoreAsync(kind);
                    await connection.ConnectAsync(profile.Server);
                    results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol, transport, check = stage, passed = true });
                    foreach (var socks in new[] { false, true })
                    foreach (var check in new[] { "HTTPS", "HTTP", "IPv4", "IPv6" })
                    {
                        var probe = await ProxyConnectivityProbe.RunEndpointAsync(backend.Port!.Value, socks, check);
                        await Task.Delay(100);
                        results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol, transport, check = (socks ? "SOCKS5 " : "HTTP proxy ") + check, passed = probe.Success, probe.Milliseconds, probe.Message, coreFailure = backend.LastCoreFailure });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol, transport, check = stage, passed = false, message = ex is CoreCompatibilityException c ? c.Message : ProxyConnectivityProbe.DescribeFailure(ex, false) });
                }
                finally
                {
                    await connection.DisconnectAsync();
                    results.Add(new { core = kind.ToString(), protocol = profile.Server.Protocol, transport, check = "core shutdown", passed = backend.Port == null && backend.OwnedProcessId == null });
                }
                await SaveAsync();
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
                    results.Add(new { check = stage, passed = retry.Success, retry.Message, publicExitMeasured = backend.PublicExitAddress != null });
                    using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
                    try { await backend.ProbeAsync(cancelled.Token); results.Add(new { check = "probe cancellation", passed = false }); }
                    catch (OperationCanceledException) { results.Add(new { check = "probe cancellation", passed = true }); }
                    var ownedPid = backend.OwnedProcessId ?? throw new InvalidOperationException();
                    using (var process = Process.GetProcessById(ownedPid)) { process.Kill(); await process.WaitForExitAsync(); }
                    var deadline = Stopwatch.StartNew();
                    while ((connection.State != ConnectionState.Error || backend.OwnedProcessId != null) && deadline.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(50);
                    results.Add(new { check = "owned core crash detected and cleaned", passed = connection.State == ConnectionState.Error && backend.Port == null && backend.OwnedProcessId == null });
                    await connection.ConnectAsync(profiles[0].Server);
                    var recovered = await backend.ProbeAsync();
                    results.Add(new { check = "manual restart after core crash", passed = recovered.Success });
                }
                finally { await connection.DisconnectAsync(); }
                using var startupCancellation = new CancellationTokenSource(); startupCancellation.Cancel();
                try { await connection.ConnectAsync(profiles[0].Server, startupCancellation.Token); results.Add(new { check = "startup cancellation", passed = false }); }
                catch (OperationCanceledException) { results.Add(new { check = "startup cancellation", passed = backend.OwnedProcessId == null && backend.Port == null }); }
            }
        }
        catch (Exception ex)
        {
            results.Add(new { check = stage, passed = false, message = ProxyConnectivityProbe.DescribeFailure(ex, false), exceptionType = ex.GetType().Name });
        }
        finally
        {
            await SaveAsync();
        }
        return !results.Select(item => System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(item))!)
            .Any(item => item["passed"]?.GetValue<bool>() == false && !(item["check"]?.ToString().Contains("IPv6") ?? false));
        async Task SaveAsync()
        {
            await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new
            {
                results,
                limitations = new[] { "Results apply only to listed authorized protocol/transport representatives", "DNS domain requests measured; resolver packet path/leaks not captured", "IPv6 failures do not establish a core defect without supported remote/network conditions", "live WinINet/browser/restoration/crash/external changes intentionally excluded" }
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
