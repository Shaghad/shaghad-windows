namespace AJ.VPN.Domain;

public enum ConnectionState { Disconnected, Connecting, Connected, Disconnecting, Error }
public enum ProxyMode { SystemProxy, Tun, Direct }
public enum ThemePreference { System, Light, Dark }
public sealed record ApplicationSettings(int SchemaVersion = 1, ThemePreference Theme = ThemePreference.System, string Language = "en");
public sealed record Server(string Id, string Name, string Protocol, string Host, int Port, string? Country = null);
public sealed record DiagnosticResult(string Status, string Explanation, string PossibleCause, string RecommendedAction);
public sealed record ProfileValidation(bool IsValid, Server? Server, string Message);
public sealed record CoreReadiness(bool IsReady, bool InternetVerified);
