namespace AJ.VPN.Domain;

public enum ConnectionState { Disconnected, Connecting, Connected, Disconnecting, Error }
public enum ProxyMode { SystemProxy, Tun, Direct }
public enum ThemePreference { System, Light, Dark }
public sealed record ApplicationSettings(int SchemaVersion = 1, ThemePreference Theme = ThemePreference.System, string Language = "en");
public sealed record Server(string Id, string Name, string Protocol, string Host, int Port, string? Country = null);
public sealed record DiagnosticResult(string Status, string Explanation, string PossibleCause, string RecommendedAction);
public sealed record ProfileValidation(bool IsValid, Server? Server, string Message);
public sealed record CoreReadiness(bool IsReady, bool InternetVerified);
public enum CoreKind { Xray, SingBox }
public sealed record SavedProfile(Server Server, string ShareUri, string? SubscriptionId = null, bool IsFavorite = false)
{
    public override string ToString() => Server.Name;
}
public sealed record Subscription(string Id, string Name, string Url, DateTimeOffset? UpdatedAt = null, long? RemainingBytes = null, DateTimeOffset? ExpiresAt = null)
{
    public override string ToString() => Name;
}
public sealed record ProfileLibrary(int SchemaVersion, List<SavedProfile> Profiles, List<Subscription> Subscriptions,
    string? ActiveId = null, CoreKind Core = CoreKind.Xray, SelectionMode SelectionMode = SelectionMode.Manual)
{
    public static ProfileLibrary Empty => new(1, [], []);
    public override string ToString() => "Encrypted profile library";
}
public sealed record CoreInstallation(CoreKind Kind, string Path, string Version, string Sha256);
public sealed record ProxyProbe(bool Success, long? Milliseconds, string Message);
public enum SelectionMode { Manual, Auto }
public enum ServerHealth { Untested, Testing, Healthy, Degraded, Timeout, Failed, Unsupported, Cancelled }
public sealed record ServerTestResult(string ProfileId, ServerHealth Health, long? Milliseconds, DateTimeOffset TestedAt, string Explanation, int SuccessfulAttempts = 0)
{
    public bool IsHealthy => Health is ServerHealth.Healthy or ServerHealth.Degraded && Milliseconds is >= 0;
}
public sealed record ServerTestProgress(int Total, int Completed, int Successful, int Failed, ServerTestResult Result);
public sealed record AutoResolution(string? ResolvedServerId, IReadOnlyList<ServerTestResult> Results, bool CompleteScope);
public sealed class CoreCompatibilityException(string safeMessage) : Exception(safeMessage);



public sealed record SubscriptionDownload(string Content, long? RemainingBytes = null, DateTimeOffset? ExpiresAt = null);
