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
        viewModel.CountryEditRequested += (_, id) => OpenCountryEditor(id);
        viewModel.SubscriptionEditRequested += (_, _) => EditSubscription(this, new RoutedEventArgs());
        SourceInitialized += (_, _) => UpdateNativeCaption();
        PreviewKeyDown += (_, key) => { if (key.Key == System.Windows.Input.Key.Escape && Sidebar.Visibility == Visibility.Visible) { CloseDrawer(); key.Handled = true; } };
        viewModel.PropertyChanged += (_, change) => { if (change.PropertyName == nameof(ShellViewModel.CurrentPage) && Sidebar.Visibility == Visibility.Visible) CloseDrawer(); };
        Microsoft.Win32.SystemEvents.PowerModeChanged += OnPowerModeChanged;
        System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailability;
        System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += OnNetworkAddress;
        Closed += (_, _) => { Microsoft.Win32.SystemEvents.PowerModeChanged -= OnPowerModeChanged; System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailability; System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= OnNetworkAddress; };
        InitializeSmartImport();
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
    private void OnPowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e) => Dispatcher.InvokeAsync(() => {
        var vm = (ShellViewModel)DataContext;
        if (e.Mode == Microsoft.Win32.PowerModes.Suspend) vm.SuspendMonitoring();
        else if (e.Mode == Microsoft.Win32.PowerModes.Resume) vm.ResumeMonitoring();
    });
    private void OnNetworkAvailability(object? sender, System.Net.NetworkInformation.NetworkAvailabilityEventArgs e) { if (e.IsAvailable) Dispatcher.InvokeAsync(() => ((ShellViewModel)DataContext).ResumeMonitoring()); }
    private void OnNetworkAddress(object? sender, EventArgs e) => Dispatcher.InvokeAsync(() => ((ShellViewModel)DataContext).ResumeMonitoring());
    private void UpdateLayoutDensity()
    {
        // Real layout adaptation: less spacing at short/narrow sizes, never transforms or fixed clipping.
        var compact = (ActualHeight > 0 ? ActualHeight : Height) < 680 || (ActualWidth > 0 ? ActualWidth : Width) < 1100;
        Resources["CardPadding"] = new Thickness(compact ? 12 : 18);
        Resources["CardMargin"] = new Thickness(0, 0, 0, compact ? 8 : 12);
        Resources["ContentMargin"] = compact ? new Thickness(20, 8, 20, 8) : new Thickness(28, 22, 28, 22);
        Resources["SidebarPadding"] = compact ? new Thickness(10, 12, 10, 12) : new Thickness(10, 16, 10, 16);
        Resources["SidebarHeaderMargin"] = new Thickness(0, 0, 0, compact ? 10 : 22);
        Resources["NavigationPadding"] = compact ? new Thickness(10, 6, 10, 6) : new Thickness(12, 10, 12, 10);
        Resources["NavigationMinHeight"] = compact ? 36d : 44d;
        Resources["HeadingSize"] = compact ? 24d : 30d;
        Resources["WelcomeVisibility"] = compact ? Visibility.Collapsed : Visibility.Visible;
        Resources["DashboardHeroHeight"] = compact ? 206d : 240d;
        Resources["DashboardStatusSize"] = compact ? 26d : 32d;
    }
    internal void ToggleSidebar(object sender, RoutedEventArgs e)
    {
        if (Sidebar.Visibility == Visibility.Visible) CloseDrawer();
        else
        {
            DrawerBackdrop.Visibility = Visibility.Visible; ContentHost.IsEnabled = false; Sidebar.Visibility = Visibility.Visible;
            Sidebar.Opacity = 1;
            var slide = new System.Windows.Media.TranslateTransform(); Sidebar.RenderTransform = slide;
            if (SystemParameters.ClientAreaAnimation) slide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new System.Windows.Media.Animation.DoubleAnimation(-24, 0, TimeSpan.FromMilliseconds(140)));
            if (SystemParameters.ClientAreaAnimation)
                Sidebar.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)));
            Sidebar.UpdateLayout();
            DrawerCloseButton.Focus();
            System.Windows.Input.FocusManager.SetFocusedElement(Sidebar, DrawerCloseButton);
        }
    }
    private void CloseDrawer()
    {
        Sidebar.BeginAnimation(OpacityProperty, null);
        Sidebar.Visibility = Visibility.Collapsed; DrawerBackdrop.Visibility = Visibility.Collapsed; ContentHost.IsEnabled = true;
        HamburgerButton.Focus();
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
        => ShowImportEditor(false);
    private void OpenClipboardImport(object sender, RoutedEventArgs e) => ShowImportEditor(true);
    private void OpenToolbarMenu(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { ContextMenu: { } menu } button) { menu.PlacementTarget = button; menu.IsOpen = true; }
    }
    private void OpenCountryEditor(string id)
    {
        var vm = (ShellViewModel)DataContext;
        var input = new System.Windows.Controls.TextBox { MaxLength = 2, Text = vm.ServerRows.First(r => r.Server.Id == id).Server.Country ?? "", FlowDirection = FlowDirection.LeftToRight };
        var save = new System.Windows.Controls.Button { Content = FindResource("Save") };
        save.Click += async (_, _) => { try { await vm.SaveCountryAsync(id, input.Text); } catch { MessageBox.Show(this, FindResource("CountryCodeHint").ToString()); } };
        ShowEditor(FindResource("SetCountry").ToString()!, [new System.Windows.Controls.TextBlock { Text = FindResource("CountryCodeHint").ToString() }, input, save]);
    }
    private void ShowImportEditor(bool clipboard)
    {
        if (clipboard) { _ = InspectClipboardAsync(); return; }
        ShowImportDialog();
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
    private void ShowSubscriptionEditor() => ShowSubscriptionDialog();
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
