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
        DataContext = viewModel; Resources["NavigationIconMargin"] = new Thickness(0,0,12,0);
        SourceInitialized += (_, _) => UpdateNativeCaption();
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
        Resources["CardPadding"] = new Thickness(compact ? 12 : 18);
        Resources["CardMargin"] = new Thickness(0, 0, 0, compact ? 8 : 12);
        Resources["ContentMargin"] = compact ? new Thickness(20, 8, 20, 8) : new Thickness(28, 22, 28, 22);
        Resources["SidebarPadding"] = compact ? new Thickness(10, 12, 10, 12) : new Thickness(10, 16, 10, 16);
        Resources["SidebarHeaderMargin"] = new Thickness(0, 0, 0, compact ? 10 : 22);
        Resources["NavigationPadding"] = sidebarCompact ? new Thickness(6) : compact ? new Thickness(10, 6, 10, 6) : new Thickness(12, 10, 12, 10);
        Resources["NavigationMinHeight"] = compact ? 36d : 44d;
        Resources["HeadingSize"] = compact ? 24d : 30d;
        Resources["WelcomeVisibility"] = compact ? Visibility.Collapsed : Visibility.Visible;
        Resources["DashboardHeroHeight"] = compact ? 206d : 240d;
        Resources["DashboardStatusSize"] = compact ? 26d : 32d;
    }
    private bool sidebarCompact;
    internal void ToggleSidebar(object sender, RoutedEventArgs e)
    {
        sidebarCompact = !sidebarCompact;
        SidebarColumn.Width = new GridLength(sidebarCompact ? 68 : 184);
        Resources["NavigationContentVisibility"] = sidebarCompact ? Visibility.Collapsed : Visibility.Visible;
        Resources["NavigationIconMargin"] = sidebarCompact ? new Thickness(0) : new Thickness(0,0,12,0);
        if (sidebarCompact) {
            Resources["NavigationPadding"] = new Thickness(6);
            SidebarExitButton.Content = "\uE8BB";
            SidebarExitButton.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
            SidebarExitButton.Padding = new Thickness(4);
        } else {
            UpdateLayoutDensity();
            SidebarExitButton.SetResourceReference(System.Windows.Controls.ContentControl.ContentProperty, "Exit");
            SidebarExitButton.SetResourceReference(FontFamilyProperty, "UiFont");
            SidebarExitButton.Padding = new Thickness(14,6,14,6);
        }
        foreach (var panel in Sidebar.Child is System.Windows.Controls.DockPanel dock ? dock.Children.OfType<System.Windows.Controls.StackPanel>() : [])
        {
            foreach (var child in panel.Children.OfType<FrameworkElement>())
                if (child is System.Windows.Controls.DockPanel) child.Visibility = sidebarCompact ? Visibility.Collapsed : Visibility.Visible;
        }
    }
    internal void UpdateNativeCaption()
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var brush = (System.Windows.Media.SolidColorBrush)FindResource("PageBrush");
        var color = brush.Color;
        int dark = color.R + color.G + color.B < 380 ? 1 : 0;
        DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        int caption = color.R | color.G << 8 | color.B << 16;
        DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        var text = ((System.Windows.Media.SolidColorBrush)FindResource("TextBrush")).Color;
        int foreground = text.R | text.G << 8 | text.B << 16;
        DwmSetWindowAttribute(handle, 36, ref foreground, sizeof(int));
    }
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    internal void OpenImport(object sender, RoutedEventArgs e)
    {
        var vm = (ShellViewModel)DataContext;
        if (!vm.CanEdit) return;
        vm.PrepareImport();
        var input = new System.Windows.Controls.TextBox { AcceptsReturn = true, Height = 160, MaxLength = 2_000_000, VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto, FlowDirection = FlowDirection.LeftToRight };
        input.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ProfileInput") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        var button = new System.Windows.Controls.Button { Content = FindResource("Validate"), Command = vm.ValidateCommand, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,16,0,8) };
        var status = new System.Windows.Controls.TextBlock();
        status.SetBinding(System.Windows.Controls.TextBlock.TextProperty, "ProfileResult");
        ShowEditor("Import server profiles", [new System.Windows.Controls.TextBlock { Text = FindResource("PreviewNotice").ToString(), Margin = new Thickness(0,0,0,16) }, input, button, status]);
        vm.ProfileInput = "";
    }
    internal void OpenSubscription(object sender, RoutedEventArgs e)
    {
        var vm = (ShellViewModel)DataContext;
        vm.EditingSubscriptionId = null; vm.SubscriptionName = ""; vm.SubscriptionUrl = "";
        ShowSubscriptionEditor();
    }
    private void EditSubscription(object sender, RoutedEventArgs e)
    {
        var vm = (ShellViewModel)DataContext;
        if (vm.SelectedSubscription is not { } sub || !vm.CanEdit) return;
        vm.EditingSubscriptionId = sub.Id; vm.SubscriptionName = sub.Name; vm.SubscriptionUrl = sub.Url;
        ShowSubscriptionEditor();
    }
    private void ShowSubscriptionEditor()
    {
        var vm = (ShellViewModel)DataContext;
        if (!vm.CanEdit) return;
        var name = new System.Windows.Controls.TextBox { MaxLength = 100 };
        name.SetResourceReference(FlowDirectionProperty, "ContentFlowDirection");
        name.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("SubscriptionName") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        var url = new System.Windows.Controls.TextBox { MaxLength = 8192, FlowDirection = FlowDirection.LeftToRight };
        url.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("SubscriptionUrl") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        var status = new System.Windows.Controls.TextBlock { Margin = new Thickness(0,12,0,0) };
        status.SetBinding(System.Windows.Controls.TextBlock.TextProperty, "Notice");
        ShowEditor(vm.EditingSubscriptionId == null ? "Add subscription" : "Edit subscription", [
            new System.Windows.Controls.TextBlock { Text = FindResource("SubscriptionName").ToString(), Margin = new Thickness(0,0,0,6) }, name,
            new System.Windows.Controls.TextBlock { Text = "HTTPS URL", Margin = new Thickness(0,16,0,6) }, url,
            new System.Windows.Controls.Button { Content = FindResource("Save"), Command = vm.AddSubscriptionCommand, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,16,0,0) }, status]);
        vm.SubscriptionUrl = ""; vm.EditingSubscriptionId = null;
    }
    private void ShowEditor(string title, FrameworkElement[] children)
    {
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(24) };
        foreach (var child in children) panel.Children.Add(child);
        var close = new System.Windows.Controls.Button { Content = FindResource("CloseEditor"), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,16,0,0) };
        var dialog = new Window { Title = "SHAGHAD · " + title, Icon = Icon, Owner = this, Width = 580, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel, DataContext = DataContext };
        dialog.Style = (Style)FindResource("ShaghadDialog");
        close.Click += (_, _) => dialog.Close(); panel.Children.Add(close);
        dialog.SourceInitialized += (_, _) => { var dark = ((System.Windows.Media.SolidColorBrush)FindResource("PageBrush")).Color.R < 80 ? 1 : 0; var handle = new System.Windows.Interop.WindowInteropHelper(dialog).Handle; DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)); }; dialog.ShowDialog();
    }
    private void OpenLicenses(object sender, RoutedEventArgs e) => new LicenseWindow { Owner = this, FlowDirection = FlowDirection.LeftToRight }.ShowDialog();
    private void OpenRepository(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        try { Process.Start(new ProcessStartInfo("https://github.com/Shaghad/shaghad-windows") { UseShellExecute = true }); }
        catch { MessageBox.Show(this, "The repository could not be opened. You can copy its address from About.", "SHAGHAD", MessageBoxButton.OK, MessageBoxImage.Information); }
    }
}
