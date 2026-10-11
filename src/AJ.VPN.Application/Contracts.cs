using AJ.VPN.Domain;

namespace AJ.VPN.Application;

public interface ISettingsService
{
    Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
public interface IDiagnosticsService
{
    Task<IReadOnlyList<DiagnosticResult>> CheckAsync(CancellationToken cancellationToken = default);
}
public interface IProfileValidator
{
    ProfileValidation Validate(string input);
    string? CoreCompatibilityError(string input, CoreKind kind) => null;
}
public interface ICoreBackend
{
    bool IsAvailable { get; }
    Task<CoreReadiness> StartAsync(Server server, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
public interface ILibraryStore
{
    Task<ProfileLibrary> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ProfileLibrary library, CancellationToken cancellationToken = default);
}
public interface ICoreConfigurationGenerator
{
    string Generate(SavedProfile profile, CoreKind kind, int port);
}
public interface ICoreCatalog
{
    Task<CoreInstallation> FindAsync(CoreKind kind, CancellationToken cancellationToken = default);
}
public interface ILocalProxyBackend : ICoreBackend
{
    int? Port { get; }
    string Version { get; }
    ProxyProbe? LastProbe => null;
    string? LastError => null;
    string? PublicExitAddress => null;
    event EventHandler? UnexpectedExit;
    Task<ProxyProbe> ProbeAsync(CancellationToken cancellationToken = default);
}
public interface ISubscriptionDownloader
{
    Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default);
}

public interface ISubscriptionMetadataDownloader : ISubscriptionDownloader
{
    Task<SubscriptionDownload> DownloadWithMetadataAsync(string url, CancellationToken cancellationToken = default);
}
