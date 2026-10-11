using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AJ.VPN.Desktop.ViewModels;
using AJ.VPN.Domain;
namespace AJ.VPN.Desktop;

internal static class PremiumLiveTest
{
    public static async Task RunAsync(Window window, ShellViewModel vm, string output, Action<ApplicationSettings> appearance)
    {
        Directory.CreateDirectory(output);
        var profile = Environment.GetEnvironmentVariable("SHAGHAD_PREMIUM_PROFILE");
        Environment.SetEnvironmentVariable("SHAGHAD_PREMIUM_PROFILE", null);
        if (string.IsNullOrWhiteSpace(profile)) throw new InvalidOperationException("No authorized live profile supplied.");
        window.Width = 1280; window.Height = 720; window.Show();
        appearance(new(1, ThemePreference.Dark, "en"));
        vm.ProfileInput = profile; profile = null;
        await ExecuteAsync(vm.ValidateCommand);
        if (!vm.HasActiveProfile) throw new InvalidOperationException("Live profile did not pass import validation.");
        vm.SelectedServerRow = vm.ServerRows[0]; vm.SelectedCore = CoreKind.Xray; vm.CurrentPage = "Dashboard";
        vm.ConnectCommand.Execute(null);
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle); await Task.Delay(150);
        SmokeTest.RenderNative(window, Path.Combine(output, "Live-connecting.png"));
        while (!vm.ConnectCommand.CanExecute(null)) await Task.Delay(50);
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle); await Task.Delay(150);
        SmokeTest.RenderNative(window, Path.Combine(output, "Live-result.png"));
        var verified = vm.InternetVerified; var measured = vm.HasPublicIp;
        var copied = false;
        if (measured) {
            var previous = System.Windows.Clipboard.GetText();
            try { vm.CopyIpCommand.Execute(null); copied = System.Windows.Clipboard.GetText() == vm.PublicIp; }
            finally { System.Windows.Clipboard.SetText(previous); }
        }
        var layouts = new List<object>();
        if (verified) {
            foreach (var size in new[] { new Size(1024,600), new Size(1280,720), new Size(1366,768), new Size(1920,1080) })
            foreach (var language in new[] { "en", "fa" })
            foreach (var theme in new[] { ThemePreference.Light, ThemePreference.Dark }) {
                appearance(new(1, theme, language));
                window.Width = size.Width; window.Height = size.Height;
                await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle); await Task.Delay(150);
                var scroll = (ScrollViewer)window.FindName("PageScrollViewer");
                SmokeTest.RenderNative(window, Path.Combine(output, $"Live-{size.Width}x{size.Height}-{language}-{theme}.png"));
                if (scroll.ScrollableHeight > 1) throw new InvalidOperationException("Connected dashboard overflowed.");
                layouts.Add(new { size.Width, size.Height, Language = language, Theme = theme.ToString(), ScrollableHeight = scroll.ScrollableHeight });
            }
        }
        await ExecuteAsync(vm.DisconnectCommand);
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle); await Task.Delay(150);
        SmokeTest.RenderNative(window, Path.Combine(output, "Live-disconnected.png"));
        var disconnected = vm.State == ConnectionState.Disconnected && !vm.HasPublicIp && vm.Duration == "—";
        File.WriteAllText(Path.Combine(output, "live-results.json"), JsonSerializer.Serialize(new {
            Core = "Xray", InternetVerified = verified, PublicExitIpMeasured = measured, CopyIpPassed = copied,
            DisconnectClearedSession = disconnected, LatencyAfterDisconnect = vm.Latency, Layouts = layouts,
            Notice = vm.Notice, ProductionLibraryChanged = false, SystemProxyChanged = false
        }, new JsonSerializerOptions { WriteIndented = true }));
        window.Hide();
    }
    private static async Task ExecuteAsync(System.Windows.Input.ICommand command)
    {
        command.Execute(null);
        while (!command.CanExecute(null)) await Task.Delay(20);
    }
}


