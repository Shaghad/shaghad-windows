using System.Windows;
using AJ.VPN.Desktop.ViewModels;
using System.Diagnostics;
using System.Windows.Navigation;
namespace AJ.VPN.Desktop;
public partial class MainWindow : Window
{
    private bool shutdownComplete;
    private bool shuttingDown;
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        UpdateLayoutDensity();
        SizeChanged += (_, _) => UpdateLayoutDensity();
        Closing += async (_, args) =>
        {
            if (shutdownComplete) return;
            args.Cancel = true;
            if (shuttingDown) return;
            shuttingDown = true;
            IsEnabled = false;
            try { await viewModel.ShutdownAsync(); }
            catch { MessageBox.Show(this, "Proxy restoration could not complete. The encrypted recovery journal was retained; review Windows proxy settings on the next launch.", "SHAGHAD", MessageBoxButton.OK, MessageBoxImage.Warning); }
            finally { shutdownComplete = true; Close(); }
        };
    }
    private void UpdateLayoutDensity()
    {
        // Real layout adaptation: less spacing at short/narrow sizes, never transforms or fixed clipping.
        var compact = (ActualHeight > 0 ? ActualHeight : Height) < 680 || (ActualWidth > 0 ? ActualWidth : Width) < 1100;
        Resources["CardPadding"] = new Thickness(compact ? 14 : 22);
        Resources["CardMargin"] = new Thickness(0, 0, 0, compact ? 10 : 16);
        Resources["ContentMargin"] = compact ? new Thickness(20, 12, 20, 12) : new Thickness(28, 22, 28, 22);
        Resources["SidebarPadding"] = compact ? new Thickness(14, 12, 14, 12) : new Thickness(16, 22, 16, 22);
        Resources["SidebarHeaderMargin"] = new Thickness(0, 0, 0, compact ? 10 : 22);
        Resources["NavigationPadding"] = compact ? new Thickness(10, 6, 10, 6) : new Thickness(12, 10, 12, 10);
        Resources["NavigationMinHeight"] = compact ? 36d : 44d;
        Resources["HeadingSize"] = compact ? 24d : 30d;
        Resources["WelcomeVisibility"] = compact ? Visibility.Collapsed : Visibility.Visible;
        Resources["DashboardHeroHeight"] = compact ? 206d : 240d;
        Resources["DashboardStatusSize"] = compact ? 26d : 32d;
    }
    private void OpenLicenses(object sender, RoutedEventArgs e) => new LicenseWindow { Owner = this, FlowDirection = FlowDirection.LeftToRight }.ShowDialog();
    private void OpenRepository(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        try { Process.Start(new ProcessStartInfo("https://github.com/Shaghad/shaghad-windows") { UseShellExecute = true }); }
        catch { MessageBox.Show(this, "The repository could not be opened. You can copy its address from About.", "SHAGHAD", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
