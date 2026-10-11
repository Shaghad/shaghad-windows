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
}
public interface ICoreBackend
{
    bool IsAvailable { get; }
    Task<CoreReadiness> StartAsync(Server server, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
