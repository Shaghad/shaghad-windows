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
        window.Width = 1200;
        window.Height = 860;
        if (window.Title != "SHAGHAD" || window.Icon == null || window.Icon.Width < 16)
            throw new InvalidOperationException("Window branding or ICO loading failed.");
        using var tray = new TrayIntegration(window, vm, visible: false);
        var ico = new IconBitmapDecoder(new Uri("pack://application:,,,/Assets/shaghad.ico"), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (!ico.Frames.Select(frame => frame.PixelWidth).Order().SequenceEqual(new[] { 16, 24, 32, 48, 64, 128, 256 }))
            throw new InvalidOperationException("WPF did not decode all seven ICO frames.");
        appearance(new(1, ThemePreference.Light, "en"));
        foreach (var page in new[] { "Dashboard", "Servers", "Subscriptions", "Routing", "Statistics", "Diagnostics", "Settings", "About" })
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
        var persian = (FontFamily)System.Windows.Application.Current.FindResource("PersianFont");
        if (!new Typeface(persian, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out var glyphs)
            || !glyphs.FamilyNames.Values.Any(name => name.Contains("Vazirmatn", StringComparison.Ordinal)))
            throw new InvalidOperationException("Embedded Vazirmatn font did not resolve.");
        var licenses = new LicenseWindow();
        Render(licenses, Path.Combine(output, "Licenses.png"));
        licenses.Close();
        tray.CheckMenuCallbacks();
        window.Hide();
        File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: SHAGHAD startup/DI; eight pages; dark and Persian RTL; embedded Vazirmatn; all seven WPF ICO frames; tray ICO decode and actual About callback; GPL/OFL viewer. Native tray visibility and taskbar interaction require live verification. No network or saved-preference mutation.\n");
    }
    private static void Render(Window window, string path)
    {
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(1200, 820));
        content.Arrange(new Rect(0, 0, 1200, 820));
        content.UpdateLayout();
        var image = new RenderTargetBitmap(1200, 820, 96, 96, PixelFormats.Pbgra32);
        image.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
