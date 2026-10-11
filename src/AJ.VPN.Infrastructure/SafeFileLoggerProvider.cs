using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AJ.VPN.Infrastructure;

/// <summary>Writes event metadata only. Message text, exception text and scopes are deliberately omitted.</summary>
public sealed class SafeFileLoggerProvider(string directory) : ILoggerProvider
{
    private readonly string logDirectory = directory;
    private readonly object gate = new();
    public ILogger CreateLogger(string categoryName) => new SafeLogger(this);
    public void Dispose() { }
    private sealed class SafeLogger(SafeFileLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            try
            {
                lock (owner.gate)
                {
                    Directory.CreateDirectory(owner.logDirectory);
                    var path = Path.Combine(owner.logDirectory, "events.jsonl");
                    if (File.Exists(path) && new FileInfo(path).Length > 1_048_576)
                        File.Move(path, path + ".previous", true);
                    File.AppendAllText(path, JsonSerializer.Serialize(new { at = DateTimeOffset.UtcNow, level = logLevel.ToString(), eventId = eventId.Id }) + Environment.NewLine);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
