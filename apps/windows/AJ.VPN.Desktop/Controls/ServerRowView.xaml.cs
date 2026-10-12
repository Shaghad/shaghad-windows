using System.Windows;
using System.Windows.Controls;
namespace AJ.VPN.Desktop.Controls;
public partial class ServerRowView : UserControl
{
    public ServerRowView() => InitializeComponent();
    private void OpenMenu(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button) { menu.PlacementTarget = button; menu.IsOpen = true; }
    }
}
