using System.Windows;
using AJ.VPN.Desktop.ViewModels;
using System.Diagnostics;
using System.Windows.Navigation;
namespace AJ.VPN.Desktop;
public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    private void OpenLicenses(object sender, RoutedEventArgs e) => new LicenseWindow { Owner = this, FlowDirection = FlowDirection }.ShowDialog();
    private void OpenRepository(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        try { Process.Start(new ProcessStartInfo("https://github.com/Shaghad/shaghad-windows") { UseShellExecute = true }); }
        catch { MessageBox.Show(this, "The repository could not be opened. You can copy its address from About.", "SHAGHAD", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
