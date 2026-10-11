using System.Windows;
using AJ.VPN.Desktop.ViewModels;
namespace AJ.VPN.Desktop;
public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
