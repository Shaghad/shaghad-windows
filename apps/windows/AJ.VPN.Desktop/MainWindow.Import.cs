using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AJ.VPN.Application;
using AJ.VPN.Desktop.ViewModels;

namespace AJ.VPN.Desktop;

public partial class MainWindow
{
    private ShellViewModel Vm => (ShellViewModel)DataContext;
    private string Text(string key) => FindResource(key).ToString()!;
    private void InitializeSmartImport()
    {
        AllowDrop = true;
        Vm.ClipboardImportRequested += async (_, _) => await InspectClipboardAsync();
        Sidebar.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((_, e) => { if (e.OriginalSource is RadioButton) CloseDrawer(); }));
        PreviewKeyDown += async (_, e) =>
        {
            if (e.Key != Key.V || Keyboard.Modifiers != ModifierKeys.Control || Sidebar.Visibility == Visibility.Visible || IsEditing(Keyboard.FocusedElement as DependencyObject)) return;
            e.Handled = true; await InspectClipboardAsync();
        };
        PreviewDragOver += (_, e) =>
        {
            if (IsEditing(e.OriginalSource as DependencyObject)) return;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && Vm.CanEdit ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        };
        Drop += async (_, e) =>
        {
            if (IsEditing(e.OriginalSource as DependencyObject)) return;
            e.Handled = true; await InspectDataAsync(e.Data);
        };
    }
    internal static bool IsEditing(DependencyObject? current)
    {
        while (current != null)
        {
            if (current is TextBoxBase or PasswordBox || current is ComboBox { IsEditable: true }) return true;
            current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }
        return false;
    }
    private async Task InspectClipboardAsync()
    {
        try { if (Clipboard.GetDataObject() is { } data) await InspectDataAsync(data); }
        catch (Exception error) { Vm.ReportImportError(error); }
    }
    internal async Task InspectDataAsync(IDataObject data)
    {
        if (!Vm.CanEdit) return;
        try
        {
            string content;
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])data.GetData(DataFormats.FileDrop);
                if (files.Length != 1) throw new InvalidDataException("Import one UTF-8 .txt file at a time.");
                content = await SmartImport.ReadFileAsync(files[0]);
            }
            else if (data.GetDataPresent(DataFormats.UnicodeText)) content = (string)data.GetData(DataFormats.UnicodeText);
            else throw new InvalidDataException("Paste VPN share links, Base64 subscription content, an HTTPS URL or a UTF-8 .txt file.");
            await InspectContentAsync(content);
        }
        catch (Exception error) { Vm.ReportImportError(error); }
    }
    internal async Task InspectContentAsync(string content)
    {
        var result = Vm.InspectImport(content);
        if (result.Kind == ImportKind.Subscription)
        {
            Vm.EditingSubscriptionId = null; Vm.SubscriptionUrl = result.Content; Vm.SubscriptionName = new Uri(result.Content).Host;
            ShowSubscriptionDialog(); return;
        }
        if (result.Kind != ImportKind.Profiles) throw new InvalidDataException("Unsupported import. Use VLESS, VMess, Trojan or Shadowsocks share links, Base64 lists or an HTTPS subscription URL.");
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(Label("ImportServersQuestion", 22));
        panel.Children.Add(new TextBlock { Text = $"{result.Count} {Text("RecognizedProfiles")}", Margin = new Thickness(0,12,0,18) });
        var dialog = CreateDialog(Text("ImportServer"), panel);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        var import = ActionButton("ImportConfirm", async () => { await Vm.ImportConfirmedAsync(result.Content); dialog.Close(); }, dialog);
        actions.Children.Add(import); actions.Children.Add(CancelButton(dialog)); panel.Children.Add(actions);
        dialog.ShowDialog();
    }
    private TextBlock Label(string key, double size = 14)
    {
        var label = new TextBlock { Text = Text(key), FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,4,0,6) };
        label.SetResourceReference(FlowDirectionProperty, "ContentFlowDirection"); return label;
    }
    private Window CreateDialog(string title, StackPanel panel)
    {
        var dialog = new Window { Title = "SHAGHAD · " + title, Owner = this, Icon = Icon, Width = 540, SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel, FlowDirection = FlowDirection.LeftToRight,
            Style = (Style)FindResource("ShaghadDialog") };
        dialog.SourceInitialized += (_, _) => { int dark = ((SolidColorBrush)FindResource("PageBrush")).Color.R < 80 ? 1 : 0; var handle = new System.Windows.Interop.WindowInteropHelper(dialog).Handle; DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)); };
        dialog.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && dialog.IsEnabled) { dialog.Close(); e.Handled = true; } };
        return dialog;
    }
    private Button ActionButton(string key, Func<Task> action, Window dialog)
    {
        var button = new Button { Content = Text(key), Style = (Style)FindResource("ShaghadPrimaryButton") };
        button.Click += async (_, _) =>
        {
            dialog.IsEnabled = false;
            try { await action(); }
            catch (Exception error) { Vm.ReportImportError(error); MessageBox.Show(dialog, Vm.Notice, Text("OperationFailed"), MessageBoxButton.OK, MessageBoxImage.Warning); }
            finally { dialog.IsEnabled = true; }
        };
        return button;
    }
    private Button CancelButton(Window dialog)
    {
        var button = new Button { Content = Text("CancelLabel"), IsCancel = true };
        button.Click += (_, _) => dialog.Close(); return button;
    }
    private void ShowImportDialog()
    {
        if (!Vm.CanEdit) return;
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(Label("PasteLink", 22));
        var input = new TextBox { AcceptsReturn = true, Height = 130, MaxLength = SmartImport.MaximumBytes, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FlowDirection = FlowDirection.LeftToRight };
        panel.Children.Add(input);
        var dialog = CreateDialog(Text("ImportServer"), panel);
        var actions = new WrapPanel { Margin = new Thickness(0,16,0,0) };
        actions.Children.Add(ActionButton("ContinueImport", async () => { await InspectContentAsync(input.Text); dialog.Close(); }, dialog));
        actions.Children.Add(ActionButton("ClipboardImport", async () => { await InspectClipboardAsync(); }, dialog));
        actions.Children.Add(ActionButton("ImportFile", async () =>
        {
            var picker = new Microsoft.Win32.OpenFileDialog { Filter = "UTF-8 share-link list (*.txt)|*.txt", CheckFileExists = true };
            if (picker.ShowDialog(dialog) == true) { await InspectContentAsync(await SmartImport.ReadFileAsync(picker.FileName)); dialog.Close(); }
        }, dialog));
        actions.Children.Add(CancelButton(dialog)); panel.Children.Add(actions);
        dialog.Loaded += (_, _) => input.Focus(); dialog.ShowDialog();
    }
    internal void ShowSubscriptionDialog()
    {
        if (!Vm.CanEdit) return;
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(Label("AddSubscription", 22)); panel.Children.Add(Label("SubscriptionUrlLabel"));
        var url = new TextBox { Text = Vm.SubscriptionUrl, MaxLength = 8192, FlowDirection = FlowDirection.LeftToRight };
        panel.Children.Add(url); panel.Children.Add(Label("OptionalSubscriptionName"));
        var name = new TextBox { Text = Vm.SubscriptionName, MaxLength = 100 };
        name.SetResourceReference(FlowDirectionProperty, "ContentFlowDirection"); panel.Children.Add(name);
        var dialog = CreateDialog(Text("AddSubscription"), panel);
        var progress = Label("UpdatingSubscription"); progress.Visibility = Visibility.Collapsed; panel.Children.Add(progress);
        async Task Save(bool update)
        {
            Vm.SubscriptionUrl = url.Text; Vm.SubscriptionName = name.Text;
            if (update) progress.Visibility = Visibility.Visible;
            try { await Vm.SaveSubscriptionAsync(update); dialog.Close(); }
            finally { progress.Visibility = Visibility.Collapsed; }
        }
        var actions = new WrapPanel { Margin = new Thickness(0,16,0,0) };
        actions.Children.Add(ActionButton("SaveUpdate", () => Save(true), dialog));
        actions.Children.Add(ActionButton("SaveOnly", () => Save(false), dialog)); actions.Children.Add(CancelButton(dialog)); panel.Children.Add(actions);
        dialog.Loaded += (_, _) => url.Focus(); dialog.ShowDialog();
        Vm.SubscriptionUrl = ""; Vm.EditingSubscriptionId = null;
    }
    private void DismissNotification(object sender, RoutedEventArgs e) => Vm.DismissNotice();
    private void ShowNotificationDetails(object sender, RoutedEventArgs e) => ShowEditor(Text("ViewDetails"), [Label("NotificationTitle"), new TextBlock { Text = Vm.Notice, TextWrapping = TextWrapping.Wrap }]);
    internal void CloseBackdrop(object sender, MouseButtonEventArgs e) { CloseDrawer(); e.Handled = true; }
}
