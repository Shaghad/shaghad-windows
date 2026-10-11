using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
        foreach (var file in new[] { "failure.txt", "result.txt", "layout-results.json" }) File.Delete(Path.Combine(output, file));
        window.Width = 1200;
        window.Height = 760;
        window.Show();
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
        await CheckLayoutMatrixAsync(window, vm, output, appearance);
        await CheckManagementAsync(window, vm, output, appearance);
        window.Hide();
        File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: SHAGHAD startup/DI; eight pages; embedded Vazirmatn; seven ICO frames; tray About callback; GPL/OFL viewer. 32 shown-window layout checks: four sizes x two languages x two themes x Dashboard/About. LTR shell/native chrome, RTL text islands, LTR technical values, fixed sidebar/card positions, no page overflow or clipped text; long-page scrolling remains functional. Native dragging, snapping and caption interaction require separate interactive verification. No production saved-preference mutation. Management fixtures and an intentionally unreachable core/proxy check run only in isolated test storage.\n");
    }
    private static async Task ExecuteAsync(System.Windows.Input.ICommand command)
    {
        command.Execute(null);
        while (!command.CanExecute(null)) await Task.Delay(20);
    }
    private static async Task CheckManagementAsync(Window window, ShellViewModel vm, string output, Action<ApplicationSettings> appearance)
    {
        // Explicitly offline fixtures in the smoke test's isolated encrypted store.
        vm.ProfileInput = "vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=tls&type=tcp#Offline%20UI%20fixture";
        await ExecuteAsync(vm.ValidateCommand);
        if (!vm.HasActiveProfile || vm.Servers.Count != 1) throw new InvalidOperationException("Import or automatic selection failed.");
        vm.SelectedServerRow = vm.ServerRows[0];
        await ExecuteAsync(vm.ActivateCommand);
        vm.Search = "nonmatching";
        if (!vm.ServerView.IsEmpty) throw new InvalidOperationException("Server filter failed.");
        vm.Search = "";
        if (vm.ServerView.IsEmpty) throw new InvalidOperationException("Server filter reset failed.");
        vm.SortServersCommand.Execute("Server.Protocol");
        window.Width = 1280; window.Height = 720;
        appearance(new(1, ThemePreference.Dark, "en"));
        foreach (var page in new[] { "Dashboard", "Servers" }) {
            vm.CurrentPage = page;
            await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            Render(window, Path.Combine(output, page + "-offline-fixture.png"));
            RenderNative(window, Path.Combine(output, page + "-native.png"));
        }
        vm.SubscriptionName = "Offline UI fixture"; vm.SubscriptionUrl = "https://example.invalid/subscription";
        await ExecuteAsync(vm.AddSubscriptionCommand);
        if (vm.Subscriptions.Count != 1) throw new InvalidOperationException("Subscription creation failed.");
        vm.SelectedSubscriptionRow = vm.SubscriptionRows[0];
        vm.EditingSubscriptionId = vm.SelectedSubscription!.Id; vm.SubscriptionName = "Edited offline fixture";
        vm.SubscriptionUrl = "https://example.invalid/updated";
        await ExecuteAsync(vm.AddSubscriptionCommand);
        if (vm.Subscriptions.Single().Name != "Edited offline fixture") throw new InvalidOperationException("Subscription edit failed.");
        vm.CurrentPage = "Subscriptions";
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
        Render(window, Path.Combine(output, "Subscriptions-offline-fixture.png"));
        RenderNative(window, Path.Combine(output, "Subscriptions-native.png"));
        appearance(new(1, ThemePreference.Dark, "fa"));
        foreach (var page in new[] { "Servers", "Subscriptions" }) {
            vm.CurrentPage = page;
            await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            await Task.Delay(150);
            RenderNative(window, Path.Combine(output, page + "-persian-native.png"));
        }
        appearance(new(1, ThemePreference.Dark, "en"));
        vm.CurrentPage = "Dashboard";
        var main = (MainWindow)window;
        main.ToggleSidebar(window, new RoutedEventArgs());
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
        RenderNative(window, Path.Combine(output, "Dashboard-compact-native.png"));
        main.ToggleSidebar(window, new RoutedEventArgs());
        foreach (var editor in new[] { "Import", "Subscription" }) {
            Exception? captureFailure = null;
            _ = window.Dispatcher.BeginInvoke(() => {
                try {
                    var dialog = System.Windows.Application.Current.Windows.OfType<Window>().First(w => w != window && w.IsVisible);
                    dialog.UpdateLayout();
                    RenderNative(dialog, Path.Combine(output, editor + "-dialog-native.png"));
                    dialog.Close();
                }
                catch (Exception failure) { captureFailure = failure; foreach (var dialog in System.Windows.Application.Current.Windows.OfType<Window>().Where(w => w != window).ToArray()) dialog.Close(); }
            }, DispatcherPriority.ApplicationIdle);
            if (editor == "Import") main.OpenImport(window, new RoutedEventArgs()); else main.OpenSubscription(window, new RoutedEventArgs());
            if (captureFailure != null) throw captureFailure;
        }
        vm.ConnectCommand.Execute(null);
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
        RenderNative(window, Path.Combine(output, "Dashboard-connecting-native.png"));
        while (!vm.ConnectCommand.CanExecute(null)) await Task.Delay(50);
        if (vm.InternetVerified || vm.HasPublicIp) throw new InvalidOperationException("Unreachable fixture reported verified internet or a public IP.");
        if (vm.State != ConnectionState.Connected) throw new InvalidOperationException("Pinned core did not reach local readiness.");
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
        RenderNative(window, Path.Combine(output, "Dashboard-unverified-native.png"));
        await ExecuteAsync(vm.DisconnectCommand);
        if (vm.State != ConnectionState.Disconnected || vm.HasPublicIp || vm.Duration != "—") throw new InvalidOperationException("Disconnect did not clear session values.");
        await ExecuteAsync(vm.RemoveSubscriptionCommand);
        await ExecuteAsync(vm.RemoveServerCommand);
        if (vm.HasActiveProfile || vm.Subscriptions.Count != 0) throw new InvalidOperationException("Management removal failed.");
        File.WriteAllText(Path.Combine(output, "management-results.txt"), "PASS: isolated offline import, active selection, search/filter/reset, sort, subscription add/edit/remove, server removal. Fixture uses example.invalid; real core startup, failed HTTPS verification, no public IP, disconnect cleanup, compact navigation and both dialogs captured. No successful network connection or fabricated measurements.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr handle, IntPtr dc, uint flags);
    internal static void RenderNative(Window window, string path)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (!GetWindowRect(handle, out var rect)) throw new InvalidOperationException("Native window bounds unavailable.");
        using var bitmap = new System.Drawing.Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        var dc = graphics.GetHdc();
        try { if (!PrintWindow(handle, dc, 2)) throw new InvalidOperationException("Native window capture failed."); }
        finally { graphics.ReleaseHdc(dc); }
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static async Task CheckLayoutMatrixAsync(Window window, ShellViewModel vm, string output, Action<ApplicationSettings> appearance)
    {
        var rows = new List<object>();
        var shell = (FrameworkElement)window.FindName("ApplicationShell");
        var sidebar = (FrameworkElement)window.FindName("Sidebar");
        var host = (FrameworkElement)window.FindName("ContentHost");
        var viewer = (ScrollViewer)window.FindName("PageScrollViewer");
        foreach (var size in new[] { new Size(1024, 600), new Size(1280, 720), new Size(1366, 768), new Size(1920, 1080) })
        foreach (var theme in new[] { ThemePreference.Light, ThemePreference.Dark })
        {
            Dictionary<string, Point>? englishPositions = null;
            foreach (var language in new[] { "en", "fa" })
            {
                appearance(new(1, theme, language));
                window.Width = size.Width; window.Height = size.Height;
                foreach (var page in new[] { "Dashboard", "About" })
                {
                    vm.CurrentPage = page;
                    viewer.ScrollToTop();
                    await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
                    var filename = $"layout-{(int)size.Width}x{(int)size.Height}-{language}-{theme}-{page}.png";
                    Render(window, Path.Combine(output, filename));
                    if (page == "Dashboard") RenderNative(window, Path.Combine(output, "native-" + filename));
                    if (window.FlowDirection != FlowDirection.LeftToRight || shell.FlowDirection != FlowDirection.LeftToRight
                        || window.WindowStyle != WindowStyle.SingleBorderWindow || window.ResizeMode != ResizeMode.CanResize)
                        throw new InvalidOperationException("Native window/shell direction or chrome changed.");
                    var handle = new WindowInteropHelper(window).Handle;
                    var nativeStyle = (long)GetWindowLongPtr(handle, -16);
                    const long captionControls = 0x80000 | 0x40000 | 0x20000 | 0x10000;
                    if (handle == IntPtr.Zero || ((long)GetWindowLongPtr(handle, -20) & 0x00400000) != 0
                        || (nativeStyle & captionControls) != captionControls)
                        throw new InvalidOperationException("Native RTL chrome or standard resizing/caption controls regressed.");
                    var sidebarPoint = sidebar.TranslatePoint(new Point(), shell);
                    var contentPoint = host.TranslatePoint(new Point(), shell);
                    if (sidebarPoint.X > 1 || contentPoint.X < sidebar.ActualWidth || sidebar.FlowDirection != FlowDirection.LeftToRight)
                        throw new InvalidOperationException("Sidebar moved or content columns mirrored.");
                    var navigationBounds = Descendants(sidebar).OfType<Control>().Where(c => c is RadioButton or Button && c.IsVisible)
                        .Select(c => c.TransformToAncestor(sidebar).TransformBounds(new Rect(c.RenderSize))).OrderBy(r => r.Top).ToArray();
                    for (var i = 0; i < navigationBounds.Length; i++)
                        if (navigationBounds[i].Top < -1 || navigationBounds[i].Bottom > sidebar.ActualHeight + 1
                            || (i > 0 && navigationBounds[i - 1].Bottom > navigationBounds[i].Top + 1))
                            throw new InvalidOperationException("Sidebar navigation is clipped or overlaps.");
                    if (Math.Abs(window.ActualWidth - size.Width) > 1 || Math.Abs(window.ActualHeight - size.Height) > 1)
                        throw new InvalidOperationException("Requested running-window size was not applied.");
                    if (viewer.ScrollableHeight > 1 || viewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                        throw new InvalidOperationException($"Unexpected {page} scrolling: {language} {theme} {size}, extent={viewer.ExtentHeight}, viewport={viewer.ViewportHeight}.");
                    var panel = (FrameworkElement)window.FindName(page + "Page");
                    foreach (var text in Descendants(panel).OfType<TextBlock>().Where(t => t.IsVisible))
                    {
                        var bounds = text.TransformToAncestor(viewer).TransformBounds(new Rect(text.RenderSize));
                        if (bounds.Left < -1 || bounds.Top < -1 || bounds.Right > viewer.ActualWidth + 1 || bounds.Bottom > viewer.ActualHeight + 1
                            || text.DesiredSize.Height - text.Margin.Top - text.Margin.Bottom > text.ActualHeight + 1)
                            throw new InvalidOperationException($"Clipped {page} text at {language} {size}: {text.Name} / {text.Text}; bounds={bounds}, viewer={viewer.RenderSize}, desired={text.DesiredSize}, actual={text.RenderSize}");
                    }
                    var attribution = (TextBlock)window.FindName("AttributionText");
                    if (attribution.FlowDirection != (language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight)
                        || attribution.TextAlignment != TextAlignment.Left)
                        throw new InvalidOperationException("Localized paragraphs have the wrong direction/alignment.");
                    foreach (var name in new[] { "TechnicalEndpoint", "CoreEngineValue", "LatencyValue", "ApplicationVersionValue", "CoreVersionValue" })
                        if (((FrameworkElement)window.FindName(name)).FlowDirection != FlowDirection.LeftToRight)
                            throw new InvalidOperationException("Technical value inherited RTL.");

                    if (page == "Dashboard")
                    {
                        var positions = new[] { "ConnectionCard", "SelectedServerCard", "ConnectionDetails" }.ToDictionary(name => name,
                            name => ((FrameworkElement)window.FindName(name)).TranslatePoint(new Point(), viewer));
                        if (language == "en") englishPositions = positions;
                        else foreach (var (name, point) in positions)
                            if (englishPositions == null || Math.Abs(point.X - englishPositions[name].X) > 1 || Math.Abs(point.Y - englishPositions[name].Y) > 1)
                                throw new InvalidOperationException($"Dashboard {name} moved after language switching.");
                        var cards = positions.Keys.Select(name => (FrameworkElement)window.FindName(name))
                            .Select(c => c.TransformToAncestor(viewer).TransformBounds(new Rect(c.RenderSize))).OrderBy(r => r.Top).ToArray();
                        for (var i = 1; i < cards.Length; i++)
                            if (cards[i - 1].Bottom > cards[i].Top + 1) throw new InvalidOperationException("Dashboard cards overlap.");
                    }
                    rows.Add(new { Width = window.ActualWidth, Height = window.ActualHeight, Language = language, Theme = theme.ToString(), Page = page,
                        ShellDirection = shell.FlowDirection.ToString(), SidebarX = sidebarPoint.X, ContentX = contentPoint.X,
                        ScrollableHeight = viewer.ScrollableHeight, ClientWidth = shell.ActualWidth, ClientHeight = shell.ActualHeight, Passed = true });
                }
            }
        }
        // Long content must remain reachable, with a visible themed scrollbar when it overflows.
        window.Width = 1024; window.Height = 540; vm.CurrentPage = "Settings";
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
        if (viewer.ScrollableHeight <= 0 || viewer.ComputedVerticalScrollBarVisibility != Visibility.Visible)
            throw new InvalidOperationException("Long page lost its visible scrollbar.");
        viewer.ScrollToBottom();
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
        if (viewer.VerticalOffset <= 0) throw new InvalidOperationException("Long page cannot scroll.");
        vm.CurrentPage = "Dashboard";
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
        var action = (Button)window.FindName("DashboardAction");
        if (!vm.HasActiveProfile)
        {
            if (action.IsEnabled || action.Command != vm.ConnectCommand)
                throw new InvalidOperationException("Empty dashboard tries to start a core instead of importing a server.");

        }
        File.WriteAllText(Path.Combine(output, "layout-results.json"), System.Text.Json.JsonSerializer.Serialize(rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Render(Window window, string path)
    {
        var content = (FrameworkElement)window.Content;
        var width = content.ActualWidth > 0 ? content.ActualWidth : window.Width;
        var height = content.ActualHeight > 0 ? content.ActualHeight : window.Height - 40;
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        var image = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
        image.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
