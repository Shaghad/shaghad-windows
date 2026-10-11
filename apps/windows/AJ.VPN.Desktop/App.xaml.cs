using System.Configuration;
using System.Data;
using System.Windows;

using System.IO;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;
using AJ.VPN.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Windows.Media;

namespace AJ.VPN.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? services;
    private ApplicationSettings appearance = new();
    private TrayIntegration? tray;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var smoke = e.Args.Length == 2 && e.Args[0] == "--smoke-test";
        try { await InitializeAsync(e, smoke); }
        catch (Exception ex) when (smoke)
        {
            Directory.CreateDirectory(Path.GetFullPath(e.Args[1]));
            File.WriteAllText(Path.Combine(e.Args[1], "failure.txt"), ex.ToString());
            Shutdown(1);
        }
    }
    private async Task InitializeAsync(StartupEventArgs e, bool smoke)
    {
        var data = smoke ? Path.Combine(Path.GetFullPath(e.Args[1]), "preferences")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VPN-AJ");
        services = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(new SafeFileLoggerProvider(Path.Combine(data, "logs"))))
            .AddSingleton<ISettingsService>(new JsonSettingsService(data))
            .AddSingleton<IProfileValidator, ServiceLibProfileValidator>()
            .AddSingleton<ICoreBackend, UnavailableCoreBackend>()
            .AddSingleton<ConnectionService>()
            .AddSingleton<IDiagnosticsService, FoundationDiagnostics>()
            .AddSingleton<ShellViewModel>()
            .AddSingleton<MainWindow>()
            .BuildServiceProvider();
        var vm = services.GetRequiredService<ShellViewModel>();
        await vm.InitializeAsync();
        ApplyAppearance(vm.Settings);
        vm.AppearanceChanged += (_, settings) => ApplyAppearance(settings);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        MainWindow = services.GetRequiredService<MainWindow>();
        MainWindow.FlowDirection = vm.Settings.Language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        if (smoke)
        {
            await SmokeTest.RunAsync(MainWindow, vm, Path.GetFullPath(e.Args[1]), ApplyAppearance);
            Shutdown(0);
            return;
        }
        MainWindow.Show();
        tray = new TrayIntegration(MainWindow, vm);
        services.GetRequiredService<ILogger<App>>().LogInformation(new EventId(100), "Application started");
    }
    private void ApplyAppearance(ApplicationSettings settings)
    {
        appearance = settings;
        ApplyTheme();
        var dictionaries = Resources.MergedDictionaries;
        var language = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.StartsWith("Resources/") == true);
        if (language != null) dictionaries.Remove(language);
        dictionaries.Add(new ResourceDictionary { Source = new Uri($"Resources/{(settings.Language == "fa" ? "Persian" : "English")}.xaml", UriKind.Relative) });
        var persianFont = new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Vazirmatn");
        Resources["PersianFont"] = persianFont;
        Resources["UiFont"] = settings.Language == "fa" ? persianFont : new FontFamily("Segoe UI");
        if (MainWindow != null) MainWindow.FlowDirection = settings.Language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }
    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        tray?.Dispose();
        services?.Dispose();
        base.OnExit(e);
    }
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Dispatcher.InvokeAsync(ApplyTheme);
    private void ApplyTheme()
    {
        var dark = appearance.Theme == ThemePreference.Dark;
        if (appearance.Theme == ThemePreference.System)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            dark = key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        var previous = Resources.MergedDictionaries.FirstOrDefault(d => d.Source?.OriginalString.StartsWith("Themes/") == true);
        if (previous != null) Resources.MergedDictionaries.Remove(previous);
        Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = new Uri($"Themes/{(SystemParameters.HighContrast ? "HighContrast" : dark ? "Dark" : "Light")}.xaml", UriKind.Relative) });
        // Swap the entire DrawingImage: cached Freezables do not inherit refreshed theme brushes reliably.
        Resources["ShaghadSymbol"] = FindResource($"ShaghadSymbol{(SystemParameters.HighContrast ? "HighContrast" : dark ? "Dark" : "Light")}");
    }
}

