using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using Microsoft.Extensions.Logging;

namespace AJ.VPN.Infrastructure;

public sealed class LocalProxyBackend(LibraryService library, ICoreCatalog catalog, ICoreConfigurationGenerator generator,
    string dataDirectory, string helperExecutable, ILogger<LocalProxyBackend> logger) : ILocalProxyBackend, ITrafficBackend, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private OwnedCoreProcess? owned;
    private FileStream? executableLock;
    private string? session;
    private bool stopping;
    private int? telemetryPort;
    private string? telemetrySecret, telemetryExecutable;
    private CoreKind telemetryKind;
    public int? Port { get; private set; }
    public int? OwnedProcessId => owned?.Process.Id;
    public string Version { get; private set; } = "Not initialized";
    public bool LastStopWasGraceful { get; private set; }
    public string? LastCoreFailure => owned?.LastFailureCategory;
    public ProxyProbe? LastProbe { get; private set; }
    public string? LastError { get; private set; }
    public string? PublicExitAddress { get; private set; }
    public bool IsAvailable => library.Active != null;
    public event EventHandler? UnexpectedExit;
    public async Task<CoreReadiness> StartAsync(Server server, CancellationToken cancellationToken)
    {
        if (!await gate.WaitAsync(0, cancellationToken)) throw new InvalidOperationException("A core operation is already running.");
        var attempted = false;
        var failureEvent = 620;
        try
        {
            if (owned != null) throw new InvalidOperationException("A core is already owned by this application.");
            attempted = true;
            stopping = false;
            LastProbe = null; PublicExitAddress = null; LastError = null;
            var profile = library.Current.Profiles.Find(p => p.Server.Id == server.Id) ?? throw new InvalidDataException("Select a saved server.");
            var installation = await catalog.FindAsync(library.Current.Core, cancellationToken);
            executableLock = await TrustedCoreCatalog.OpenVerifiedAsync(installation.Path, installation.Sha256, cancellationToken);
            Version = $"{installation.Kind} {installation.Version}";
            failureEvent = 621;
            var runtime = Path.Combine(dataDirectory, "runtime");
            ProtectedFiles.CreatePrivateDirectory(runtime);
            session = Path.Combine(runtime, Guid.NewGuid().ToString("N"));
            ProtectedFiles.CreatePrivateDirectory(session);
            await File.WriteAllTextAsync(Path.Combine(session, ".shaghad-session"), "SHAGHAD-runtime-v1", cancellationToken);
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
            var path = Path.Combine(session, "config.json");
            System.Text.Json.Nodes.JsonObject configuration;
            try { configuration = System.Text.Json.Nodes.JsonNode.Parse(generator.Generate(profile, installation.Kind, port))!.AsObject(); }
            catch (System.Text.Json.JsonException) { throw new InvalidDataException("Generated configuration is not valid JSON."); }
            var telemetryListener = new TcpListener(IPAddress.Loopback, 0);
            telemetryListener.Start(); telemetryPort = ((IPEndPoint)telemetryListener.LocalEndpoint).Port; telemetryListener.Stop();
            telemetryKind = installation.Kind; telemetryExecutable = installation.Path;
            if (installation.Kind == CoreKind.Xray)
            {
                configuration["stats"] = new System.Text.Json.Nodes.JsonObject();
                configuration["policy"] = System.Text.Json.Nodes.JsonNode.Parse("{\"system\":{\"statsOutboundUplink\":true,\"statsOutboundDownlink\":true}}");
                configuration["api"] = new System.Text.Json.Nodes.JsonObject { ["tag"] = "telemetry", ["listen"] = $"127.0.0.1:{telemetryPort}",
                    ["services"] = new System.Text.Json.Nodes.JsonArray("StatsService") };
            }
            else
            {
                telemetrySecret = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
                configuration["experimental"] = new System.Text.Json.Nodes.JsonObject { ["clash_api"] = new System.Text.Json.Nodes.JsonObject {
                    ["external_controller"] = $"127.0.0.1:{telemetryPort}", ["secret"] = telemetrySecret,
                    ["access_control_allow_origin"] = new System.Text.Json.Nodes.JsonArray("http://127.0.0.1"), ["access_control_allow_private_network"] = false } };
            }
            await File.WriteAllTextAsync(path, configuration.ToJsonString(), cancellationToken);
            failureEvent = 622;
            var check = await TrustedCoreCatalog.RunCheckAsync(installation.Path,
                installation.Kind == CoreKind.Xray ? ["run", "-test", "-c", path] : ["check", "-c", path], cancellationToken);
            if (check.ExitCode != 0) throw new InvalidDataException("The core rejected the generated configuration. Check the selected core, transport and profile parameters.");
            cancellationToken.ThrowIfCancellationRequested();
            failureEvent = 623;
            owned = OwnedCoreProcess.Start(installation.Path, path, session);
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            startup.CancelAfter(TimeSpan.FromSeconds(10));
            while (true)
            {
                startup.Token.ThrowIfCancellationRequested();
                if (owned.Process.HasExited) throw new InvalidOperationException("The core exited during startup. No successful connection was recorded.");
                if (OwnsListener(owned.Process.Id, port) && await SocksReadyAsync(port, startup.Token)) break;
                await Task.Delay(100, startup.Token);
            }
            Port = port;
            var current = owned;
            _ = ObserveExitAsync(current);
            logger.LogInformation(new EventId(610), "Owned core and local listener are ready");
            return new(true, false);
        }
        catch (Exception ex)
        {
            if (attempted)
            {
                LastError = ex is CoreCompatibilityException compatibility ? compatibility.Message : failureEvent switch
                {
                    620 => "Core discovery or executable verification failed. Check the installed trusted core.",
                    621 => "Profile generation failed. Check profile and core compatibility.",
                    622 => "Native configuration validation failed. Check profile and core compatibility.",
                    _ => ex is OperationCanceledException && cancellationToken.IsCancellationRequested ? "Core startup was cancelled." : "Core startup failed or timed out before local readiness. Check listener availability."
                };
                logger.LogWarning(new EventId(failureEvent), "Core startup failed at a sanitized stage"); await CleanupAsync();
            }
            throw;
        }
        finally { gate.Release(); }
    }
    private async Task ObserveExitAsync(OwnedCoreProcess process)
    {
        try
        {
            await process.Process.WaitForExitAsync();
            if (stopping || owned != process) return;
            Port = null;
            LastProbe = null; PublicExitAddress = null; LastError = "The owned core exited unexpectedly. Reconnect to restart it.";
            logger.LogWarning(new EventId(611), "Owned core exited unexpectedly");
            UnexpectedExit?.Invoke(this, EventArgs.Empty);
        }
        catch (ObjectDisposedException) { } catch (InvalidOperationException) { }
    }
    private static async Task<bool> SocksReadyAsync(int port, CancellationToken token)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(500);
            using var client = new TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
            var stream = client.GetStream(); await stream.WriteAsync(new byte[] { 5, 1, 0 }, timeout.Token);
            var response = new byte[2]; await stream.ReadExactlyAsync(response, timeout.Token);
            return response[0] == 5 && response[1] == 0;
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException) { token.ThrowIfCancellationRequested(); return false; }
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Cleanup is mandatory even if the caller cancels. The native graceful wait is bounded.
        await gate.WaitAsync(CancellationToken.None);
        try { await CleanupAsync(); }
        finally { gate.Release(); }
    }
    private async Task CleanupAsync()
    {
        telemetryPort = null; telemetrySecret = null; telemetryExecutable = null;
        stopping = true; Port = null; LastProbe = null; PublicExitAddress = null;
        if (owned != null)
        {
            var process = owned; owned = null;
            try { await process.StopAsync(helperExecutable); LastStopWasGraceful = process.GracefulExit;
                logger.LogInformation(new EventId(612), "Owned core stopped; raw output was discarded"); }
            finally { process.Dispose(); }
        }
        executableLock?.Dispose(); executableLock = null;
        if (session != null)
        {
            var path = session; session = null;
            try { DeleteSession(path); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { logger.LogWarning(new EventId(613), "Private runtime cleanup requires retry"); }
        }
    }
    public static void CleanupAbandonedSessions(string dataDirectory)
    {
        var runtime = Path.Combine(dataDirectory, "runtime");
        ProtectedFiles.CreatePrivateDirectory(runtime);
        foreach (var path in Directory.EnumerateDirectories(runtime))
        {
            if (!Guid.TryParseExact(Path.GetFileName(path), "N", out _) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) continue;
            var marker = Path.Combine(path, ".shaghad-session");
            if (!File.Exists(marker) || (File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0 || new FileInfo(marker).Length != 18 || File.ReadAllText(marker) != "SHAGHAD-runtime-v1") continue;
            DeleteSession(path);
        }
    }
    private static void DeleteSession(string path)
    {
        if (Directory.EnumerateDirectories(path).Any() || Directory.EnumerateFiles(path).Any(f => Path.GetFileName(f) is not ("config.json" or ".shaghad-session")))
            throw new IOException("Unexpected files in the private runtime directory; preserved for review.");
        File.Delete(Path.Combine(path, "config.json")); File.Delete(Path.Combine(path, ".shaghad-session")); Directory.Delete(path);
    }
    public async Task<ProxyProbe> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var process = owned; var port = Port;
        if (process == null || port == null || process.Process.HasExited) return new(false, null, "Start a local proxy before testing.");
        var result = await ProxyConnectivityProbe.RunAsync(port.Value, false, cancellationToken);
        if (owned != process || Port != port || process.Process.HasExited) return new(false, null, "The proxy changed during the test.");
        LastProbe = result;
        LastError = result.Success ? null : LastCoreFailure ?? result.Message;
        PublicExitAddress = result.Success ? await PublicExitIp.FetchAsync(port.Value, cancellationToken) : null;
        if (owned != process || Port != port || process.Process.HasExited)
        { LastProbe = null; PublicExitAddress = null; return new(false, null, "The proxy changed during the test."); }
        logger.LogInformation(new EventId(result.Success ? 630 : 631), "Sanitized proxy probe outcome");
        return result;
    }
    private static bool OwnsListener(int processId, int port)
    {
        var size = 0; GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 3, 0);
        var memory = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(memory, ref size, false, 2, 3, 0) != 0) return false;
            var count = Marshal.ReadInt32(memory);
            for (var i = 0; i < count; i++)
            {
                var row = IntPtr.Add(memory, 4 + i * 24);
                var raw = Marshal.ReadInt32(row, 8);
                var localPort = ((raw & 255) << 8) | ((raw >> 8) & 255);
                if (Marshal.ReadInt32(row, 4) == 0x0100007f && localPort == port && Marshal.ReadInt32(row, 20) == processId) return true;
            }
            return false;
        }
        finally { Marshal.FreeHGlobal(memory); }
    }
    public async Task<TrafficCounters?> ReadTrafficAsync(CancellationToken token = default)
    {
        var process = owned; var id = session; var apiPort = telemetryPort; var exe = telemetryExecutable;
        if (process == null || process.Process.HasExited || id == null || apiPort == null || exe == null || Port == null
            || !OwnsListener(process.Process.Id, apiPort.Value)) return null;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(3));
            TrafficCounters? result;
            if (telemetryKind == CoreKind.Xray)
            {
                var query = await TrustedCoreCatalog.RunCheckAsync(exe, ["api", "statsquery", $"--server=127.0.0.1:{apiPort}", "-timeout=2", "-pattern=outbound>>>proxy>>>traffic>>>"], timeout.Token);
                result = query.ExitCode == 0 ? TrafficCounterParser.ParseXray(query.Output, id) : null;
            }
            else
            {
                using var handler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false, UseCookies = false };
                using var client = new HttpClient(handler) { MaxResponseContentBufferSize = 1024 * 1024 };
                client.DefaultRequestHeaders.Authorization = new("Bearer", telemetrySecret);
                var json = await client.GetStringAsync($"http://127.0.0.1:{apiPort}/connections", timeout.Token);
                result = TrafficCounterParser.ParseSingBox(json, id);
            }
            return owned == process && session == id && Port != null ? result : null;
        }
        catch (Exception ex) when (ex is OperationCanceledException or System.Text.Json.JsonException or HttpRequestException or FormatException or OverflowException or InvalidOperationException or IOException)
        { token.ThrowIfCancellationRequested(); return null; }
    }
    public void Dispose() { stopping = true; owned?.Dispose(); executableLock?.Dispose(); }
    [DllImport("iphlpapi.dll")] private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order, int family, int tableClass, uint reserved);
}
