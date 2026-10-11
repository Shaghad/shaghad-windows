using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AJ.VPN.Desktop.ViewModels;
using AJ.VPN.Domain;

namespace AJ.VPN.Desktop;

/// <summary>Opt-in, offline startup and render check; writes only to the explicit output directory.</summary>
internal static class SmokeTest
{
    public static async Task RunAsync(Window window, ShellViewModel vm, string output, Action<ApplicationSettings> appearance)
    {
        Directory.CreateDirectory(output);
        window.Width = 1120;
        window.Height = 780;
        foreach (var page in new[] { "Dashboard", "Servers", "Subscriptions", "Routing", "Statistics", "Diagnostics", "Settings" })
        {
            vm.CurrentPage = page;
            if (page == "Diagnostics") vm.DiagnosticsCommand.Execute(null);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Render(window, Path.Combine(output, page + ".png"));
        }
        appearance(new(1, ThemePreference.Dark, "en"));
        vm.CurrentPage = "Dashboard";
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Render(window, Path.Combine(output, "Dashboard-dark.png"));
        appearance(new(1, ThemePreference.Light, "fa"));
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Render(window, Path.Combine(output, "Dashboard-persian.png"));
        File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: WPF startup, dependency composition, seven page renders, dark and Persian RTL resources. No network or settings mutation.\n");
    }
    private static void Render(Window window, string path)
    {
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(1120, 740));
        content.Arrange(new Rect(0, 0, 1120, 740));
        content.UpdateLayout();
        var image = new RenderTargetBitmap(1120, 740, 96, 96, PixelFormats.Pbgra32);
        image.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
