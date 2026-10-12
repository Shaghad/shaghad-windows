using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using AJ.VPN.Desktop.ViewModels;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;

namespace AJ.VPN.Desktop;

/// <summary>Opt-in end-to-end test using an owned loopback VLESS server. No provider credentials.</summary>
internal static class AutoSelectionSelfTest
{
    public static async Task RunAsync(Window window, ShellViewModel vm, LocalProxyBackend active, string output, Action<ApplicationSettings> appearance)
    {
        Directory.CreateDirectory(output);
        var root = Path.Combine(Path.GetTempPath(), "SHAGHAD-auto-fixture", Guid.NewGuid().ToString("N"));
        ProtectedFiles.CreatePrivateDirectory(root);
        var reserve = new TcpListener(IPAddress.Loopback, 0); reserve.Start(); var port = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
        const string identity = "00000000-0000-0000-0000-000000000001";
        var install = await new TrustedCoreCatalog(Path.Combine(AppContext.BaseDirectory, "cores")).FindAsync(CoreKind.Xray);
        await using var executable = await TrustedCoreCatalog.OpenVerifiedAsync(install.Path, install.Sha256, CancellationToken.None);
        var config = Path.Combine(root, "fixture.json");
        await File.WriteAllTextAsync(config, JsonSerializer.Serialize(new {
            log = new { loglevel = "none" },
            inbounds = new[] { new { listen = "127.0.0.1", port, protocol = "vless", settings = new { clients = new[] { new { id = identity } }, decryption = "none" } } },
            outbounds = new[] { new { protocol = "freedom" } }
        }));
        OwnedCoreProcess? server = null;
        var proxy = new WindowsProxySettingsBackend(); var before = JsonSerializer.Serialize(proxy.Read());
        try
        {
            var check = await TrustedCoreCatalog.RunCheckAsync(install.Path, ["run", "-test", "-c", config], CancellationToken.None);
            if (check.ExitCode != 0) throw new InvalidOperationException("Controlled local server configuration rejected.");
            server = OwnedCoreProcess.Start(install.Path, config, root);
            using var startup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!await ListeningAsync(port)) { startup.Token.ThrowIfCancellationRequested(); if (server.Process.HasExited) throw new InvalidOperationException("Local fixture exited."); await Task.Delay(50, startup.Token); }
            window.Show(); window.Width = 1280; window.Height = 720; appearance(new(1, ThemePreference.Dark, "en"));
            vm.ProfileInput = $"vless://{identity}@127.0.0.1:{port}?security=none&type=tcp#CONTROLLED%20LOOPBACK%20VLESS%20FIXTURE\nvless://{identity}@example.invalid:443?security=tls&type=tcp#UNREACHABLE%20OFFLINE%20FIXTURE";
            await ExecuteAsync(vm.ValidateCommand);
            await ExecuteAsync(vm.AutoCommand);
            var resolved = vm.ResolvedServerId;
            var healthy = vm.ServerRows.SingleOrDefault(r => r.Server.Id == resolved);
            if (healthy?.Result?.IsHealthy != true || healthy.Server.Host != "127.0.0.1") throw new InvalidOperationException("No successful TLS-verified path through the controlled local VLESS server.");
            vm.CurrentPage = "Servers"; await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            SmokeTest.RenderNative(window, Path.Combine(output, "Auto-verified-loopback-server.png"));
            await ExecuteAsync(vm.ConnectCommand);
            if (vm.State != ConnectionState.Connected || !vm.InternetVerified || vm.ResolvedServerId != resolved) throw new InvalidOperationException("Dashboard Auto did not connect through the resolved real profile.");
            var pid = active.OwnedProcessId; var listener = active.Port;
            await ExecuteAsync(vm.TestAllCommand);
            if (active.OwnedProcessId != pid || active.Port != listener || vm.State != ConnectionState.Connected || !vm.InternetVerified || vm.ResolvedServerId != resolved)
                throw new InvalidOperationException("Test All disturbed the established Auto connection.");
            if (before != JsonSerializer.Serialize(proxy.Read())) throw new InvalidOperationException("Windows proxy changed during isolated testing.");
            vm.CurrentPage = "Servers"; await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            SmokeTest.RenderNative(window, Path.Combine(output, "Auto-test-all-preserved-session.png"));
            File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new {
                Fixture = "Owned loopback VLESS server, synthetic UUID; freedom outbound to actual HTTPS measurement endpoints",
                ProviderConnectivity = "Not tested; no authorized remote provider profile available",
                AutoResolvedHealthyCandidate = true, DashboardConnectUsedAuto = true, ActivePidAndPortPreservedByTestAll = true,
                WindowsProxyUnchanged = true, ActualHttpsVerified = vm.InternetVerified, ActualExitIpMeasured = vm.HasPublicIp,
                Measurements = vm.ServerRows.Select(r => new { r.Result?.Health, r.Result?.Milliseconds, r.Result?.SuccessfulAttempts }),
                SelectionMode = vm.SelectedModeDisplay
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            await ExecuteAsync(vm.DisconnectCommand);
            if (server != null) { try { await server.StopAsync(Path.Combine(AppContext.BaseDirectory, "Shaghad.exe")); } finally { server.Dispose(); } }
            File.Delete(config); if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
            window.Hide();
        }
    }
    private static async Task<bool> ListeningAsync(int port) { try { using var c = new TcpClient(); await c.ConnectAsync(IPAddress.Loopback, port); return true; } catch (SocketException) { return false; } }
    private static async Task ExecuteAsync(System.Windows.Input.ICommand command) { command.Execute(null); while (!command.CanExecute(null)) await Task.Delay(20); }
}
