using System.Collections.ObjectModel;
using System.Windows.Input;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using Microsoft.Extensions.Logging;

namespace AJ.VPN.Desktop.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private readonly ISettingsService settingsService;
    private readonly IDiagnosticsService diagnosticsService;
    private readonly IProfileValidator validator;
    private readonly ILogger<ShellViewModel> logger;
    private readonly ConnectionService connection;
    private string currentPage = "Dashboard";
    private string notice = "";
    private string profileInput = "";
    private string profileResult = "";
    private ThemePreference selectedTheme;
    private string selectedLanguage = "en";
    private ApplicationSettings settings = new();
    private bool settingsWritable = true;
    public ShellViewModel(ISettingsService settingsService, IDiagnosticsService diagnosticsService, IProfileValidator validator, ILogger<ShellViewModel> logger, ConnectionService connection)
    {
        this.settingsService = settingsService;
        this.diagnosticsService = diagnosticsService;
        this.validator = validator;
        this.logger = logger;
        this.connection = connection;
        NavigateCommand = new RelayCommand(p => CurrentPage = p?.ToString() ?? "Dashboard");
        SaveCommand = new AsyncCommand(SaveAsync, HandleFailure);
        DiagnosticsCommand = new AsyncCommand(CheckAsync, HandleFailure);
        ValidateCommand = new AsyncCommand(ValidateAsync, HandleFailure);
        ExitCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());
    }
    public ApplicationSettings Settings => settings;
    public ConnectionState State => connection.State;
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
        var input = ProfileInput;
        ProfileInput = "";
        var result = await Task.Run(() => validator.Validate(input));
        ProfileResult = result.Message;
        if (result.Server is { } server) ProfileResult += $"\n{server.Protocol} · {server.Host}:{server.Port}";
        logger.LogInformation(new EventId(result.IsValid ? 401 : 402), "Profile validation completed");
    }
    private void HandleFailure(Exception _)
    {
        Notice = "The operation failed. Existing settings were preserved; check local file permissions and retry.";
        logger.LogError(new EventId(500), "Operation failed");
    }
}
