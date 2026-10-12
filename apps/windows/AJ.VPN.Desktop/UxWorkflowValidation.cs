using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AJ.VPN.Desktop.ViewModels;
using AJ.VPN.Domain;

namespace AJ.VPN.Desktop;

/// <summary>Opt-in real WPF workflow checks in the existing isolated smoke-test storage.</summary>
internal static class UxWorkflowValidation
{
    internal const string Profile = "vless://00000000-0000-0000-0000-000000000001@example.invalid:443?security=tls&type=tcp#OFFLINE%20UX%20fixture";
    public static async Task RunAsync(MainWindow window, ShellViewModel vm, string output, Action<ApplicationSettings> appearance)
    {
        Directory.CreateDirectory(output); File.Delete(Path.Combine(output, "failure.txt")); window.Width = 1024; window.Height = 600; window.Show(); window.Activate();
        var checks = new List<string>();
        async Task Capture(string name)
        {
            await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle);
            await Task.Delay(180); SmokeTest.RenderNative(window, Path.Combine(output, name + ".png"));
        }
        foreach (var theme in new[] { ThemePreference.Dark, ThemePreference.Light })
        foreach (var language in new[] { "en", "fa" })
        {
            appearance(new(1, theme, language)); vm.CurrentPage = "Dashboard";
            await Capture($"Dashboard-Disconnected-{theme}-{language}");
        }
        appearance(new(1, ThemePreference.Dark, "en")); vm.CurrentPage = "Dashboard";
        await Capture("Dashboard-ClosedMenu"); await Capture("Dashboard-Disconnected");
        window.ToggleSidebar(window, new RoutedEventArgs());
        await Task.Delay(180); await Capture("Dashboard-OpenMenu");
        var host = (FrameworkElement)window.FindName("ContentHost"); var sidebar = (FrameworkElement)window.FindName("Sidebar");
        if (host.IsEnabled || !sidebar.IsKeyboardFocusWithin) throw new InvalidOperationException("Drawer modality/focus failed.");
        var escape = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        window.RaiseEvent(escape);
        if (!escape.Handled || sidebar.Visibility != Visibility.Collapsed || !((Button)window.FindName("HamburgerButton")).IsKeyboardFocused) throw new InvalidOperationException("Escape/focus return failed.");
        window.ToggleSidebar(window, new RoutedEventArgs());
        window.CloseBackdrop(window, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent });
        if (!host.IsEnabled || sidebar.Visibility != Visibility.Collapsed) throw new InvalidOperationException("Backdrop close failed.");
        checks.Add("Drawer backdrop, Escape, keyboard focus and background blocking passed.");
        if (!MainWindow.IsEditing(new TextBox()) || !MainWindow.IsEditing(new PasswordBox()) || !MainWindow.IsEditing(new RichTextBox()) || MainWindow.IsEditing(new Button())) throw new InvalidOperationException("Editable-control detection failed.");
        checks.Add("TextBox, PasswordBox, RichTextBox preserve ordinary paste.");
        vm.CurrentPage = "Servers"; await Capture("Servers-Empty");
        vm.CurrentPage = "Subscriptions"; await Capture("Subscription-Empty");
        // Capture and cancel: parsing and showing a modal must not persist anything.
        QueueDialog("SmartClipboard-Import", false);
        await window.InspectContentAsync(Profile);
        if (vm.Servers.Count != 0) throw new InvalidOperationException("Cancel persisted a profile.");
        QueueDialog("SmartClipboard-Confirmed", true);
        await window.InspectContentAsync(Profile);
        if (vm.Servers.Count != 1 || vm.ServerRows.Count != 1 || vm.NeedsServer) throw new InvalidOperationException("Import/list refresh failed.");
        await Capture("Servers-WithProfiles");
        checks.Add("Share-link confirmation cancellation and confirmed list refresh passed.");
        QueueDialog("Subscription-AddDialog", false);
        await window.InspectContentAsync("https://example.invalid/sub?token=synthetic");
        if (vm.Subscriptions.Count != 0) throw new InvalidOperationException("URL inspection downloaded or saved data.");
        checks.Add("HTTPS clipboard URL opens Add Subscription; cancel leaves library unchanged.");
        vm.SubscriptionUrl = "https://example.invalid/sub"; vm.SubscriptionName = "OFFLINE subscription fixture";
        await vm.SaveSubscriptionAsync(false);
        vm.DismissNotice(); await Capture("Subscription-WithEntries");
        var path = Path.Combine(output, "synthetic-filedrop.txt"); await File.WriteAllTextAsync(path, Profile);
        var data = new DataObject(DataFormats.FileDrop, new[] { path });
        QueueDialog("Clipboard-FileDrop", false); await window.InspectDataAsync(data);
        if (vm.Servers.Count != 1) throw new InvalidOperationException("FileDrop cancellation mutated the library.");
        checks.Add("Actual WPF IDataObject FileDrop uses bounded file reader and cancellable confirmation.");
        vm.ReportImportError(new InvalidDataException("https://example.invalid/?token=DO-NOT-DISPLAY"));
        if (vm.Notice.Contains("DO-NOT-DISPLAY") || vm.Notice.Contains("https://")) throw new InvalidOperationException("Error redaction failed.");
        await Capture("Error-Notification"); checks.Add("Error notification details exclude supplied URLs/tokens.");
        File.WriteAllLines(Path.Combine(output, "workflow-results.txt"), checks);
        window.Hide();

        void QueueDialog(string name, bool import)
        {
            _ = window.Dispatcher.BeginInvoke(() =>
            {
                var dialog = System.Windows.Application.Current.Windows.OfType<Window>().Single(w => w != window && w.IsVisible);
                dialog.UpdateLayout(); SmokeTest.Render(dialog, Path.Combine(output, name + ".png"));
                if (import) Descendants(dialog).OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                else dialog.Close();
            }, DispatcherPriority.ApplicationIdle);
        }
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
