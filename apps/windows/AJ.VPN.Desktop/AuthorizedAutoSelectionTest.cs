using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using AJ.VPN.Desktop.ViewModels;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;

namespace AJ.VPN.Desktop;

internal static class AuthorizedAutoSelectionTest
{
    public static async Task RunAsync(Window window, ShellViewModel vm, LocalProxyBackend active, string libraryDirectory, string output, Action<ApplicationSettings> appearance)
    {
        Directory.CreateDirectory(output);
        var requested = Environment.GetEnvironmentVariable("SHAGHAD_PHASE4_SERVER_NAME");
        Environment.SetEnvironmentVariable("SHAGHAD_PHASE4_SERVER_NAME", null);
        if (string.IsNullOrWhiteSpace(requested)) throw new InvalidOperationException("Specify the authorized server display name.");
        var saved = await ProtectedFiles.ReadAsync<ProfileLibrary>(Path.Combine(libraryDirectory, "secrets", "library.dpapi")) ?? throw new InvalidOperationException("Authorized encrypted library is unavailable.");
        static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormKC).Where(c => !char.IsWhiteSpace(c)));
        var profiles = saved.Profiles.Where(p => Normalize(p.Server.Name).Contains(Normalize(requested), StringComparison.OrdinalIgnoreCase)).ToArray();
        if (profiles.Length == 0) throw new InvalidOperationException("The requested display name was not found in this encrypted library.");
        var before = JsonSerializer.Serialize(new WindowsProxySettingsBackend().Read());
        try
        {
            window.Show(); window.Width = 1280; window.Height = 720; appearance(new(1, ThemePreference.Dark, "en"));
            vm.ProfileInput = string.Join('\n', profiles.Select(p => p.ShareUri));
            await ExecuteAsync(vm.ValidateCommand);
            await ExecuteAsync(vm.AutoCommand);
            var resolution = vm.ResolvedServerId;
            vm.CurrentPage = "Servers"; await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            SmokeTest.RenderNative(window, Path.Combine(output, "Authorized-Auto-server-results.png"));
            if (resolution != null) await ExecuteAsync(vm.ConnectCommand);
            var connected = vm.State == ConnectionState.Connected && vm.InternetVerified;
            var pid = active.OwnedProcessId; var port = active.Port;
            if (connected) await ExecuteAsync(vm.TestAllCommand);
            var preserved = connected && active.OwnedProcessId == pid && active.Port == port && vm.State == ConnectionState.Connected && vm.ResolvedServerId == resolution;
            var proxyUnchanged = before == JsonSerializer.Serialize(new WindowsProxySettingsBackend().Read());
            vm.CurrentPage = "Dashboard"; await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            // Kept in ignored local artifacts because this real Dashboard may display a public exit IP.
            SmokeTest.RenderNative(window, Path.Combine(output, "Authorized-Auto-dashboard.png"));
            vm.CurrentPage = "Servers"; await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            SmokeTest.RenderNative(window, Path.Combine(output, "Authorized-Test-All-preserved-session.png"));
            var evidence = new {
                Scope = "Profiles matching the user's authorized German server display name; not the entire provider subscription",
                MatchedProfiles = profiles.Length, AutoResolvedHealthyCandidate = resolution != null,
                DashboardAutoConnectionHttpsVerified = connected, ActivePidAndPortPreservedByTestAll = preserved,
                PublicExitIpMeasured = vm.HasPublicIp, WindowsProxyUnchanged = proxyUnchanged,
                Measurements = vm.ServerRows.Select(r => new { r.Result?.Health, r.Result?.Milliseconds, r.Result?.SuccessfulAttempts }),
                CountrySource = "Unknown unless explicitly assigned; display-name emoji is not geographic evidence",
                SourceEncryptedLibraryChanged = false
            };
            File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } }));
            if (!connected || !preserved || !proxyUnchanged) throw new InvalidOperationException("Remote Auto or active-session preservation did not pass; see sanitized result.json.");
        }
        finally { await ExecuteAsync(vm.DisconnectCommand); window.Hide(); }
    }
    private static async Task ExecuteAsync(System.Windows.Input.ICommand command) { command.Execute(null); while (!command.CanExecute(null)) await Task.Delay(20); }
}
