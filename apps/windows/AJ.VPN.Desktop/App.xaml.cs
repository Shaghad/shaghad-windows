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
    private FileStream? instanceLock;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 3 && e.Args[0] == "--core-semantics-audit")
        { try { await CoreSemanticsAudit.RunAsync(Path.GetFullPath(e.Args[1]), Path.GetFullPath(e.Args[2])); Shutdown(0); } catch { Shutdown(1); } return; }
        if (e.Args.Length == 3 && e.Args[0] == "--production-connectivity-test")
        { try { Shutdown(await ProductionConnectivityTest.RunAsync(Path.GetFullPath(e.Args[1]), Path.GetFullPath(e.Args[2])) ? 0 : 2); } catch { Shutdown(1); } return; }
        if (e.Args.Length == 2 && e.Args[0] == "--signal-core" && int.TryParse(e.Args[1], out var pid))
        { Shutdown(OwnedCoreProcess.SignalConsoleGroup(pid) ? 0 : 1); return; }
        if (e.Args.Length == 2 && e.Args[0] == "--core-self-test")
        { try { await CoreSelfTest.RunAsync(Path.GetFullPath(e.Args[1])); Shutdown(0); } catch { Shutdown(1); } return; }
        var smoke = e.Args.Length == 2 && (e.Args[0] == "--smoke-test" || e.Args[0] == "--premium-live-test");
        try { await InitializeAsync(e, smoke); }
        catch (Exception ex)
        {
            if (smoke)
            {
                Directory.CreateDirectory(Path.GetFullPath(e.Args[1]));
                File.WriteAllText(Path.Combine(e.Args[1], "failure.txt"), ex.ToString());
            }
            else MessageBox.Show("SHAGHAD could not initialize. Another instance may be using this Windows user's data, or private storage may be unavailable. Existing data was preserved.", "SHAGHAD", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    private async Task InitializeAsync(StartupEventArgs e, bool smoke)
    {
        var data = smoke ? Path.Combine(Path.GetTempPath(), "SHAGHAD-premium-smoke", Guid.NewGuid().ToString("N"))
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VPN-AJ");
        ProtectedFiles.CreatePrivateDirectory(data);
        instanceLock = new FileStream(Path.Combine(data, "instance.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        LocalProxyBackend.CleanupAbandonedSessions(data);
        var coreDirectory = Path.Combine(AppContext.BaseDirectory, "cores");
        services = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(new SafeFileLoggerProvider(Path.Combine(data, "logs"))))
            .AddSingleton<ISettingsService>(new JsonSettingsService(data))
            .AddSingleton<IProfileValidator, ServiceLibProfileValidator>()
            .AddSingleton<ILibraryStore>(new EncryptedLibraryStore(data))
            .AddSingleton<ISubscriptionDownloader, SubscriptionDownloader>()
            .AddSingleton<LibraryService>()
            .AddSingleton<ICoreConfigurationGenerator, ServiceLibConfigurationGenerator>()
            .AddSingleton<ICoreCatalog>(new TrustedCoreCatalog(coreDirectory))
            .AddSingleton<LocalProxyBackend>(sp => new(sp.GetRequiredService<LibraryService>(), sp.GetRequiredService<ICoreCatalog>(), sp.GetRequiredService<ICoreConfigurationGenerator>(), data,
                Path.Combine(AppContext.BaseDirectory, "Shaghad.exe"), sp.GetRequiredService<ILogger<LocalProxyBackend>>()))
            .AddSingleton<ILocalProxyBackend>(sp => sp.GetRequiredService<LocalProxyBackend>())
            .AddSingleton<ICoreBackend>(sp => sp.GetRequiredService<LocalProxyBackend>())
            .AddSingleton<SystemProxyService>(new SystemProxyService(new WindowsProxySettingsBackend(), new EncryptedProxyJournalStore(data)))
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
        if (smoke)
        {
            if (e.Args[0] == "--premium-live-test") await PremiumLiveTest.RunAsync(MainWindow, vm, Path.GetFullPath(e.Args[1]), ApplyAppearance);
            else await SmokeTest.RunAsync(MainWindow, vm, Path.GetFullPath(e.Args[1]), ApplyAppearance);
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
        // Language affects text islands only. Window/chrome and structural panels always stay LTR.
        Resources["ContentFlowDirection"] = settings.Language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        // TextBlock's native RTL coordinate system maps its leading (Left) paragraph edge
        // to the physical right. Explicit Right here double-reverses alignment in an LTR shell.
        Resources["ContentTextAlignment"] = TextAlignment.Left;
        Resources["ContentAlignment"] = settings.Language == "fa" ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        (MainWindow?.DataContext as ShellViewModel)?.RefreshLocalizedText();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        tray?.Dispose();
        services?.Dispose();
        instanceLock?.Dispose();
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
        (MainWindow as MainWindow)?.UpdateNativeCaption();
        // Swap the entire DrawingImage: cached Freezables do not inherit refreshed theme brushes reliably.
        Resources["ShaghadSymbol"] = FindResource($"ShaghadSymbol{(SystemParameters.HighContrast ? "HighContrast" : dark ? "Dark" : "Light")}");
    }
}
