using System.Drawing;
using System.Windows;
using AJ.VPN.Desktop.ViewModels;
using Forms = System.Windows.Forms;

namespace AJ.VPN.Desktop;

/// <summary>Brand presence and explicit navigation only. Closing the main window still exits.</summary>
internal sealed class TrayIntegration : IDisposable
{
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly Forms.ContextMenuStrip menu;
    private readonly Icon icon;
    private readonly Window window;
    private readonly ShellViewModel viewModel;
    public TrayIntegration(Window window, ShellViewModel viewModel, bool visible = true)
    {
        this.window = window;
        this.viewModel = viewModel;
        using var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/shaghad.ico")).Stream;
        using var original = new Icon(resource);
        icon = (Icon)original.Clone();
        menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open SHAGHAD", null, (_, _) => Open());
        menu.Items.Add("About SHAGHAD | درباره شغاد", null, (_, _) => Open("About"));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit SHAGHAD", null, (_, _) => window.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown()));
        notifyIcon = new Forms.NotifyIcon
        {
            Icon = icon,
            Text = $"SHAGHAD · {viewModel.State}",
            ContextMenuStrip = menu,
            Visible = visible
        };
        menu.Opening += (_, _) => notifyIcon.Text = $"SHAGHAD · {viewModel.State}";
        notifyIcon.DoubleClick += (_, _) => Open();
    }
    private void Open(string? page = null) => window.Dispatcher.Invoke(() =>
    {
        if (page != null) viewModel.NavigateCommand.Execute(page);
        window.Show();
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Activate();
    });
    internal void CheckMenuCallbacks()
    {
        // Exercise the actual About callback without terminating the validation process.
        ((Forms.ToolStripMenuItem)menu.Items[1]).PerformClick();
        if (viewModel.CurrentPage != "About") throw new InvalidOperationException("Tray navigation failed.");
        if (icon.Width < 16 || icon.Height < 16) throw new InvalidOperationException("Tray icon decoding failed.");
    }
    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        menu.Dispose();
        icon.Dispose();
    }
}
