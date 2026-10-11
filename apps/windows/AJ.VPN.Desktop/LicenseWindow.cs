using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AJ.VPN.Desktop;

internal sealed class LicenseWindow : Window
{
    public LicenseWindow()
    {
        Title = "SHAGHAD · Open-source licenses";
        Width = 780; Height = 650; MinWidth = 500; MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Style = (Style)System.Windows.Application.Current.FindResource("ShaghadDialog");
        Icon = System.Windows.Application.Current.MainWindow.Icon;
        var tabs = new TabControl { Margin = new Thickness(20) };
        foreach (var (title, resource) in new[] { ("GPL v3 / v2rayN", "GPL-3.0.txt"), ("Vazirmatn / OFL 1.1", "Vazirmatn-OFL.txt") })
        {
            using var stream = System.Windows.Application.GetResourceStream(new Uri($"pack://application:,,,/Assets/Licenses/{resource}")).Stream;
            using var reader = new StreamReader(stream);
            tabs.Items.Add(new TabItem { Header = title, Content = new TextBox
            {
                Text = reader.ReadToEnd(), IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                FontFamily = new FontFamily("Segoe UI"), FlowDirection = FlowDirection.LeftToRight,
                Padding = new Thickness(16)
            } });
        }
        foreach (var (title, directory) in new[] { ("Xray-core / MPL 2.0", "xray"), ("sing-box / GPL v3+", "sing-box") })
        {
            var path = Path.Combine(AppContext.BaseDirectory, "cores", directory, "LICENSE");
            tabs.Items.Add(new TabItem { Header = title, Content = new TextBox
            {
                Text = File.Exists(path) ? File.ReadAllText(path) : "This core is not installed. Install only the pinned official release with its license. See docs/legal/CORE-NOTICES.md for source and license references.",
                IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Segoe UI"), FlowDirection = FlowDirection.LeftToRight, Padding = new Thickness(16)
            } });
        }
        Content = tabs;
    }
}
