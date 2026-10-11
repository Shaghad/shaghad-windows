using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;
using Microsoft.Extensions.Logging;

namespace AJ.VPN.Desktop;

/// <summary>Explicit offline native checks; fixtures cannot authenticate to any real server.</summary>
internal static class CoreSelfTest
{
    private sealed class NoDownload : ISubscriptionDownloader
    { public Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default) => throw new InvalidOperationException(); }
    private sealed class InvalidGenerator : ICoreConfigurationGenerator
    { public string Generate(SavedProfile profile, CoreKind kind, int port) => "{invalid-config}"; }
    private sealed class OccupiedPortGenerator(ICoreConfigurationGenerator generator, int occupied) : ICoreConfigurationGenerator
    { public string Generate(SavedProfile profile, CoreKind kind, int port) => generator.Generate(profile, kind, occupied); }
    private sealed class NoListenerGenerator : ICoreConfigurationGenerator
    { public string Generate(SavedProfile profile, CoreKind kind, int port) => "{\"log\":{\"loglevel\":\"warning\"},\"inbounds\":[],\"outbounds\":[{\"protocol\":\"blackhole\",\"tag\":\"proxy\"}]}"; }
    public static async Task RunAsync(string output)
    {
        // A caller must not accidentally point this fixture-only mode at a real user's library.
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new InvalidOperationException("Core self-tests require a new empty output directory.");
        Directory.CreateDirectory(output);
        var results = new List<string>();
        try
        {
            var validator = new ServiceLibProfileValidator();
            var generator = new ServiceLibConfigurationGenerator(validator);
            var catalog = new TrustedCoreCatalog(Path.Combine(AppContext.BaseDirectory, "cores"));
            var vmess = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"v\":\"2\",\"ps\":\"offline VMess fixture\",\"add\":\"example.invalid\",\"port\":\"443\",\"id\":\"00000000-0000-0000-0000-000000000001\",\"aid\":\"0\",\"scy\":\"auto\",\"net\":\"tcp\",\"tls\":\"tls\",\"sni\":\"example.invalid\"}"));
            string[] fixtures = [
                "vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=tls&type=tcp#offline-VLESS",
                "vmess://" + vmess,
                "trojan://offline-fixture-password@example.invalid:443?security=tls&type=tcp#offline-Trojan",
                "ss://" + Convert.ToBase64String(Encoding.UTF8.GetBytes("aes-128-gcm:offline-fixture-password")) + "@example.invalid:8388#offline-SS"
            ];
            foreach (var fixture in fixtures)
            {
                var validation = validator.Validate(fixture);
                if (!validation.IsValid) throw new InvalidDataException($"Offline {fixture.Split(':')[0]} fixture rejected: {validation.Message}");
            }
            var library = new LibraryService(new EncryptedLibraryStore(output), validator, new NoDownload());
            await library.InitializeAsync(); await library.ImportAsync(string.Join('\n', fixtures));
            using var log = new SafeFileLoggerProvider(Path.Combine(output, "logs"));
            using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(log));
            using var backend = new LocalProxyBackend(library, catalog, generator, output, Path.Combine(AppContext.BaseDirectory, "Shaghad.exe"), loggerFactory.CreateLogger<LocalProxyBackend>());
            foreach (var kind in Enum.GetValues<CoreKind>())
            {
                var installation = await catalog.FindAsync(kind);
                results.Add($"PASS {kind}: pinned hash and version {installation.Version}");
                foreach (var profile in library.Current.Profiles)
                {
                    var path = Path.Combine(output, "check.json");
                    try
                    {
                        await File.WriteAllTextAsync(path, generator.Generate(profile, kind, 19080));
                        var result = await TrustedCoreCatalog.RunCheckAsync(installation.Path, kind == CoreKind.Xray ? ["run", "-test", "-c", path] : ["check", "-c", path], CancellationToken.None);
                        if (result.ExitCode != 0) throw new InvalidDataException($"{kind} {profile.Server.Protocol} native config check failed: {result.Output}");
                        results.Add($"PASS {kind} {profile.Server.Protocol}: native configuration validation");
                    }
                    finally { File.Delete(path); }
                }
                await library.SetCoreAsync(kind);
                var readiness = await backend.StartAsync(library.Active!.Server, CancellationToken.None);
                if (!readiness.IsReady || readiness.InternetVerified || backend.Port == null) throw new InvalidDataException("Invalid readiness semantics.");
                var port = backend.Port.Value;
                try { await backend.StartAsync(library.Active.Server, CancellationToken.None); throw new InvalidDataException("Duplicate core was accepted."); }
                catch (InvalidOperationException) { results.Add($"PASS {kind}: duplicate start rejected"); }
                // The duplicate rejection must leave the original core alive.
                if (backend.Port != port) throw new InvalidDataException("Duplicate rejection stopped the active core.");
                await backend.StopAsync(CancellationToken.None);
                if (!backend.LastStopWasGraceful) throw new InvalidDataException($"{kind}: graceful console shutdown was not confirmed.");
                using var client = new TcpClient();
                try { await client.ConnectAsync(IPAddress.Loopback, port); throw new InvalidDataException("Listener survived core stop."); }
                catch (SocketException) { }
                results.Add($"PASS {kind}: PID-owned loopback SOCKS readiness, graceful stop and listener release; internet unverified");
                using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
                try { await backend.StartAsync(library.Active.Server, cancelled.Token); throw new InvalidDataException("Cancelled startup accepted."); }
                catch (OperationCanceledException) { results.Add($"PASS {kind}: cancellation prevents startup"); }
                var connection = new ConnectionService(backend);
                await connection.ConnectAsync(library.Active.Server);
                var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                connection.StateChanged += (_, _) => { if (connection.State == ConnectionState.Error) exited.TrySetResult(); };
                using (var process = System.Diagnostics.Process.GetProcessById(backend.OwnedProcessId!.Value)) process.Kill();
                await exited.Task.WaitAsync(TimeSpan.FromSeconds(5));
                if (connection.InternetVerified || backend.Port != null) throw new InvalidDataException("Unexpected exit retained ready state.");
                await connection.DisconnectAsync();
                results.Add($"PASS {kind}: unexpected owned-core exit clears readiness and connection state");
                using var failing = new LocalProxyBackend(library, catalog, new InvalidGenerator(), output, Path.Combine(AppContext.BaseDirectory, "Shaghad.exe"), loggerFactory.CreateLogger<LocalProxyBackend>());
                try { await failing.StartAsync(library.Active.Server, CancellationToken.None); throw new InvalidOperationException("Invalid configuration started."); }
                catch (InvalidDataException) { }
                if (failing.Port != null || failing.OwnedProcessId != null) throw new InvalidDataException("Failed startup retained a process.");
                results.Add($"PASS {kind}: native configuration failure is detected before startup and private config is removed");
                var occupied = new TcpListener(IPAddress.Loopback, 0); occupied.Start();
                try
                {
                    using var collision = new LocalProxyBackend(library, catalog, new OccupiedPortGenerator(generator, ((IPEndPoint)occupied.LocalEndpoint).Port), output, Path.Combine(AppContext.BaseDirectory, "Shaghad.exe"), loggerFactory.CreateLogger<LocalProxyBackend>());
                    try { await collision.StartAsync(library.Active.Server, CancellationToken.None); throw new InvalidDataException("A failed native listener was accepted."); }
                    catch (InvalidOperationException) { }
                    if (collision.Port != null || collision.OwnedProcessId != null) throw new InvalidDataException("Startup exit retained a core.");
                    results.Add($"PASS {kind}: native bind/startup failure is detected and cleaned up");
                }
                finally { occupied.Stop(); }
                if (kind == CoreKind.Xray)
                {
                    using var timeout = new LocalProxyBackend(library, catalog, new NoListenerGenerator(), output, Path.Combine(AppContext.BaseDirectory, "Shaghad.exe"), loggerFactory.CreateLogger<LocalProxyBackend>());
                    try { await timeout.StartAsync(library.Active.Server, CancellationToken.None); throw new InvalidDataException("A core without a listener was accepted."); }
                    catch (OperationCanceledException) { }
                    if (timeout.Port != null || timeout.OwnedProcessId != null) throw new InvalidDataException("Startup timeout retained a core.");
                    results.Add("PASS Xray: real ten-second readiness timeout terminates the owned core and removes its config");
                }
            }
            if (Directory.EnumerateDirectories(Path.Combine(output, "runtime")).Any()) throw new InvalidDataException("Private runtime sessions survived cleanup.");
            await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new { Passed = true, Checks = results, RealConnectivityTested = false, SystemProxyChanged = false }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            // This mode accepts no external profile input; diagnostic detail contains only reserved test fixtures.
            await File.WriteAllTextAsync(Path.Combine(output, "failure.txt"), ex.ToString());
            throw;
        }
    }
}
