using System.Collections.ObjectModel;
using System.Windows.Input;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using Microsoft.Extensions.Logging;

namespace AJ.VPN.Desktop.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly ISettingsService settingsService;
    private readonly IDiagnosticsService diagnosticsService;
    private readonly ILogger<ShellViewModel> logger;
    private readonly ConnectionService connection;
    private readonly LibraryService library;
    private readonly ILocalProxyBackend backend;
    private readonly ICoreCatalog catalog;
    private readonly AJ.VPN.Infrastructure.SystemProxyService proxy;
    private CancellationTokenSource? networkCancellation;
    private Task? connectOperation;
    private SavedProfile? selectedServer;
    private Subscription? selectedSubscription;
    private CoreKind selectedCore;
    private string subscriptionName = "", subscriptionUrl = "", coreSummary = "Not checked", latency = "—";
    private bool busy;
    private string currentPage = "Dashboard";
    private string notice = "";
    private string profileInput = "";
    private string profileResult = "";
    private ThemePreference selectedTheme;
    private string selectedLanguage = "en";
    private ApplicationSettings settings = new();
    private bool settingsWritable = true;
    public ShellViewModel(ISettingsService settingsService, IDiagnosticsService diagnosticsService, ILogger<ShellViewModel> logger, ConnectionService connection,
        LibraryService library, ILocalProxyBackend backend, ICoreCatalog catalog, AJ.VPN.Infrastructure.SystemProxyService proxy)
    {
        this.settingsService = settingsService;
        this.diagnosticsService = diagnosticsService;
        this.logger = logger;
        this.connection = connection;
        this.library = library; this.backend = backend; this.catalog = catalog; this.proxy = proxy;
        InitializePremium();
        NavigateCommand = new RelayCommand(p => CurrentPage = p?.ToString() ?? "Dashboard");
        SaveCommand = new AsyncCommand(SaveAsync, HandleFailure);
        DiagnosticsCommand = new AsyncCommand(CheckAsync, HandleFailure);
        ValidateCommand = new AsyncCommand(ValidateAsync, HandleFailure);
        ExitCommand = new RelayCommand(_ => System.Windows.Application.Current.MainWindow?.Close());
        ClipboardCommand = new AsyncCommand(async () => { ProfileInput = System.Windows.Clipboard.GetText(); await ValidateAsync(); }, HandleFailure);
        ActivateCommand = new AsyncCommand(async () => { EnsureIdle(); if (SelectedServer != null) await library.SelectAsync(SelectedServer.Server.Id); RefreshLibrary(); }, HandleFailure);
        RemoveServerCommand = new AsyncCommand(async () => { EnsureIdle(); if (SelectedServer != null) await library.RemoveProfileAsync(SelectedServer.Server.Id); RefreshLibrary(); }, HandleFailure);
        ConnectCommand = new AsyncCommand(async () => { connectOperation = ConnectAsync(); await connectOperation; }, HandleFailure);
        DisconnectCommand = new AsyncCommand(DisconnectAsync, HandleFailure);
        ProbeCommand = new AsyncCommand(ProbeAsync, HandleFailure);
        AddSubscriptionCommand = new AsyncCommand(async () => { EnsureIdle(); if (EditingSubscriptionId != null) await library.EditSubscriptionAsync(EditingSubscriptionId, SubscriptionName, SubscriptionUrl); else await library.AddSubscriptionAsync(SubscriptionName, SubscriptionUrl); EditingSubscriptionId = null; SubscriptionUrl = ""; RefreshLibrary(); Notice = "Subscription saved with Windows user encryption. Update explicitly to import its servers."; }, HandleFailure);
        UpdateSubscriptionCommand = new AsyncCommand(async () =>
        {
            EnsureIdle(); if (SelectedSubscription == null) return;
            Busy = true; networkCancellation = new();
            try { await library.UpdateSubscriptionAsync(SelectedSubscription.Id, networkCancellation.Token); RefreshLibrary(); Notice = "Subscription updated. Imported profiles passed validation."; }
            finally { Busy = false; networkCancellation.Dispose(); networkCancellation = null; }
        }, HandleFailure);
        RemoveSubscriptionCommand = new AsyncCommand(async () => { EnsureIdle(); if (SelectedSubscription != null) await library.RemoveSubscriptionAsync(SelectedSubscription.Id); RefreshLibrary(); }, HandleFailure);
        RefreshCoresCommand = new AsyncCommand(RefreshCoresAsync, HandleFailure);
        ApplyProxyCommand = new AsyncCommand(ApplyProxyAsync, HandleFailure);
        RestoreProxyCommand = new AsyncCommand(async () =>
        {
            var approved = proxy.IsApplied || Confirm("Restore the previous Windows proxy settings recorded by SHAGHAD? Any changes made outside SHAGHAD will be preserved.");
            if (approved) Notice = await proxy.RestoreAsync(true); Raise(nameof(ConnectionMode));
        }, HandleFailure);
        connection.StateChanged += (_, _) => System.Windows.Application.Current.Dispatcher.InvokeAsync(RefreshState);
        backend.UnexpectedExit += (_, _) => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try { Notice = "Core exited unexpectedly. " + await proxy.RestoreAsync(); await backend.StopAsync(CancellationToken.None); }
            catch { HandleFailure(new InvalidOperationException()); }
            RefreshState();
        });
    }
    public ApplicationSettings Settings => settings;
    public ConnectionState State => connection.State;
    public bool InternetVerified => connection.InternetVerified;
    public string Endpoint => backend.Port is int port ? $"127.0.0.1:{port}" : Resource("ProxyStopped");
    public string Verification => Resource(InternetVerified ? "InternetVerified" : "InternetUnverified");
    public string ActiveServer => library.Active?.Server.Name ?? Resource("NoServer");
    public bool HasActiveProfile => library.Active != null;
    private static string Resource(string key) => System.Windows.Application.Current.TryFindResource(key) as string ?? key;
    public void RefreshLocalizedText()
    {
        Raise(nameof(ActiveServer)); Raise(nameof(Endpoint)); Raise(nameof(Verification)); Raise(nameof(ConsumerStatus)); Raise(nameof(Location)); Raise(nameof(ConnectionMode)); Raise(nameof(NeedsServer));
        if (ServerView != null) RefreshPremiumLibrary();
    }
    public string CoreSummary { get => coreSummary; private set => Set(ref coreSummary, value); }
    public string Latency { get => latency; private set => Set(ref latency, value); }
    public bool Busy { get => busy; private set { Set(ref busy, value); Raise(nameof(CanEdit)); } }
    public bool CanEdit => !Busy && State is ConnectionState.Disconnected or ConnectionState.Error;
    public SavedProfile? SelectedServer { get => selectedServer; set => Set(ref selectedServer, value); }
    public Subscription? SelectedSubscription { get => selectedSubscription; set => Set(ref selectedSubscription, value); }
    public CoreKind SelectedCore
    {
        get => selectedCore;
        set
        {
            var profile = SelectedServer ?? library.Active;
            if (profile != null && library.CoreCompatibilityError(profile, value) is string error)
            { Notice = error; Raise(nameof(SelectedCore)); return; }
            if (Set(ref selectedCore, value)) Raise(nameof(CoreDisplay));
        }
    }
    public string CoreDisplay => SelectedCore == CoreKind.SingBox ? "sing-box" : "Xray";
    public IReadOnlyList<CoreKind> Cores { get; } = Enum.GetValues<CoreKind>();
    public string SubscriptionName { get => subscriptionName; set => Set(ref subscriptionName, value); }
    public string SubscriptionUrl { get => subscriptionUrl; set => Set(ref subscriptionUrl, value); }
    public ObservableCollection<SavedProfile> Servers { get; } = [];
    public ObservableCollection<Subscription> Subscriptions { get; } = [];
    public string ApplicationVersion => typeof(ShellViewModel).Assembly.GetName().Version?.ToString(3) ?? "Unavailable";
    public string GitHubRepository => "https://github.com/Shaghad/shaghad-windows";
    public string CurrentPage { get => currentPage; set => Set(ref currentPage, value); }
    public string Notice { get => notice; private set => Set(ref notice, value); }
    public string ProfileInput { get => profileInput; set => Set(ref profileInput, value); }
    public string ProfileResult { get => profileResult; private set => Set(ref profileResult, value); }
    public ThemePreference SelectedTheme { get => selectedTheme; set => Set(ref selectedTheme, value); }
    public string SelectedLanguage { get => selectedLanguage; set => Set(ref selectedLanguage, value); }
    public IReadOnlyList<ThemePreference> Themes { get; } = Enum.GetValues<ThemePreference>();
    public IReadOnlyList<string> Languages { get; } = ["en", "fa"];
    public ObservableCollection<DiagnosticResult> Diagnostics { get; } = [];
    public ICommand NavigateCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DiagnosticsCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ClipboardCommand { get; }
    public ICommand ActivateCommand { get; }
    public ICommand RemoveServerCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ProbeCommand { get; }
    public ICommand AddSubscriptionCommand { get; }
    public ICommand UpdateSubscriptionCommand { get; }
    public ICommand RemoveSubscriptionCommand { get; }
    public ICommand RefreshCoresCommand { get; }
    public ICommand ApplyProxyCommand { get; }
    public ICommand RestoreProxyCommand { get; }
    public event EventHandler<ApplicationSettings>? AppearanceChanged;
    public async Task InitializeAsync()
    {
        try { settings = await settingsService.LoadAsync(); }
        catch
        {
            settingsWritable = false;
            Notice = "Settings could not be loaded. Existing file is preserved; saving is disabled until it is repaired.";
            logger.LogWarning(new EventId(201), "Settings load failed");
        }
        SelectedTheme = settings.Theme;
        SelectedLanguage = settings.Language;
        try { await library.InitializeAsync(); RefreshLibrary(); }
        catch { Notice = "Encrypted server library could not be loaded. Existing data is preserved and changes are disabled."; logger.LogWarning(new EventId(203), "Library load failed"); }
        try { if (await proxy.HasRecoveryAsync()) Notice = "A Windows proxy recovery journal exists. Review Settings and explicitly restore before applying a new proxy."; }
        catch { Notice = "Proxy recovery journal is unreadable. System Proxy changes are blocked until it is repaired."; }
    }
    private async Task SaveAsync()
    {
        if (!settingsWritable) { Notice = "Saving is disabled to preserve unreadable settings."; return; }
        var updated = new ApplicationSettings(1, SelectedTheme, SelectedLanguage);
        await settingsService.SaveAsync(updated);
        settings = updated;
        AppearanceChanged?.Invoke(this, updated);
        Notice = SelectedLanguage == "fa" ? "تنظیمات ذخیره شد." : "Preferences saved.";
        logger.LogInformation(new EventId(202), "Settings saved");
    }
    private async Task CheckAsync()
    {
        var results = await diagnosticsService.CheckAsync();
        Diagnostics.Clear();
        foreach (var result in results) Diagnostics.Add(result);
        logger.LogInformation(new EventId(300), "Local diagnostics completed");
    }
    private async Task ValidateAsync()
    {
        EnsureIdle();
        var input = ProfileInput;
        ProfileInput = "";
        await library.ImportAsync(input);
        RefreshLibrary();
        ProfileResult = "Validated profiles imported and encrypted for this Windows user.";
        logger.LogInformation(new EventId(401), "Profile import completed");
    }
    private void EnsureIdle() { if (!CanEdit) throw new InvalidOperationException("Disconnect before changing the server library or core."); }
    private void RefreshLibrary()
    {
        var subId = SelectedSubscription?.Id;
        Servers.Clear(); foreach (var item in library.Current.Profiles) Servers.Add(item);
        Subscriptions.Clear(); foreach (var item in library.Current.Subscriptions) Subscriptions.Add(item);
        SelectedServer = library.Active; SelectedSubscription = Subscriptions.FirstOrDefault(s => s.Id == subId) ?? Subscriptions.FirstOrDefault(); RefreshPremiumLibrary();
        SelectedCore = library.Current.Core; Raise(nameof(ActiveServer)); Raise(nameof(HasActiveProfile)); Raise(nameof(NeedsServer)); Raise(nameof(Protocol)); Raise(nameof(Location));
    }
    private void RefreshState()
    {
        if (State is ConnectionState.Disconnected or ConnectionState.Error)
        {
            ipCancellation?.Cancel(); PublicIp = "—"; Latency = "—"; connectedAt = null;
        }
        foreach (var name in new[] { nameof(State), nameof(InternetVerified), nameof(Endpoint), nameof(Verification), nameof(CanEdit), nameof(ConsumerStatus), nameof(CanProbe), nameof(ConnectionMode), nameof(Duration) }) Raise(name);
    }
    private async Task RefreshCoresAsync()
    {
        var lines = new List<string>();
        foreach (var kind in Cores)
        {
            try { var core = await catalog.FindAsync(kind); lines.Add($"{kind} {core.Version} · SHA-256 verified"); }
            catch { lines.Add($"{kind} · missing or untrusted (run scripts/install-cores.ps1 -Download and rebuild)"); }
        }
        CoreSummary = string.Join("\n", lines);
    }
    private async Task ConnectAsync()
    {
        EnsureIdle(); if (SelectedServer == null) { Notice = "Import and select a server first."; return; }
        Busy = true; Latency = "—"; networkCancellation = new();
        try
        {
            await library.SelectAsync(SelectedServer.Server.Id, networkCancellation.Token);
            await library.SetCoreAsync(SelectedCore, networkCancellation.Token);
            await connection.ConnectAsync(SelectedServer.Server, networkCancellation.Token); connectedAt = DateTimeOffset.UtcNow; await ProbeAsync();
            CoreSummary = backend.Version;
            Notice = InternetVerified ? "HTTPS verified through the selected server. System Proxy can be enabled in Settings." : "Local proxy ready; internet access could not be verified. Retry the HTTPS test.";
        }
        finally { Busy = false; networkCancellation.Dispose(); networkCancellation = null; RefreshState(); RefreshLibrary(); }
    }
    private async Task DisconnectAsync()
    {
        networkCancellation?.Cancel(); ipCancellation?.Cancel(); PublicIp = "—";
        if (connectOperation != null) { try { await connectOperation; } catch { } }
        // Restore the approved settings before stopping the listener.
        try { Notice = await proxy.RestoreAsync(); }
        finally { await connection.DisconnectAsync(); }
        Latency = "—"; PublicIp = "—"; connectedAt = null; RefreshState();
    }
    private async Task ProbeAsync()
    {
        if (State != ConnectionState.Connected || backend.Port is not int port) return;
        ipCancellation?.Cancel();
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(networkCancellation?.Token ?? CancellationToken.None);
        ipCancellation = operation;
        PublicIp = "—";
        try {
            var result = await backend.ProbeAsync(operation.Token);
            if (State != ConnectionState.Connected || backend.Port != port || operation.IsCancellationRequested) return;
            connection.RecordProbe(result);
            Latency = result.Success && result.Milliseconds is long ms ? $"{ms} ms" : "—";
            if (library.Active is { } active) pings[active.Server.Id] = result.Success ? Latency : "Unavailable"; RefreshPremiumLibrary();
            Notice = result.Message; RefreshState();
            if (result.Success) {
                var ip = backend.PublicExitAddress;
                if (State == ConnectionState.Connected && backend.Port == port && !operation.IsCancellationRequested) PublicIp = ip ?? "—";
            }
        }
        finally { if (ReferenceEquals(ipCancellation, operation)) ipCancellation = null; }
    }
    private async Task ApplyProxyAsync()
    {
        if (State != ConnectionState.Connected || backend.Port is not int port) { Notice = "Start a local proxy first."; return; }
        if (!Confirm($"Change this Windows user's System Proxy to 127.0.0.1:{port}? Automatic configuration (PAC) and auto-detection will be disabled while SHAGHAD owns these settings. Previous settings will be encrypted and restored on disconnect or exit. Applications that ignore System Proxy are unaffected.")) return;
        await proxy.ApplyAsync(port, true);
        // Recheck after asynchronous journal persistence in case the core exited during approval/apply.
        if (backend.Port != port || State != ConnectionState.Connected) Notice = await proxy.RestoreAsync();
        else Notice = "Windows System Proxy applied with your approval. Disconnect or Restore will recover the previous settings."; Raise(nameof(ConnectionMode));
    }
    private static bool Confirm(string message) => System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, message, "SHAGHAD · Windows System Proxy", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes;
    public async Task ShutdownAsync() { await DisconnectAsync(); }
    private void HandleFailure(Exception _)
    {
        Notice = _ switch
        {
            CoreCompatibilityException compatibility => compatibility.Message,
            OperationCanceledException => "Operation cancelled or timed out. No successful connection was recorded.",
            System.Security.Cryptography.CryptographicException => "Windows could not decrypt the local library. Existing encrypted data was preserved.",
            System.Net.Http.HttpRequestException => "HTTPS subscription download failed. Check the URL, public host and TLS certificate. Existing servers were preserved.",
            System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException => "Core executable is missing. Run scripts/install-cores.ps1 -Download, then rebuild or publish.",
            System.IO.InvalidDataException => "Configuration rejected. Check the share-link format, supported transport and selected core; subscription updates require a supported HTTPS share-link list. Existing data was preserved.",
            _ => "Operation failed. Check profile validity, selected core and local Diagnostics. Existing saved data was preserved."
        };
        logger.LogError(new EventId(500), "Operation failed");
    }
}
