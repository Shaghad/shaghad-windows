using System.Text.Json;
using AJ.VPN.Application;
using AJ.VPN.Domain;

namespace AJ.VPN.Infrastructure;

public sealed class JsonSettingsService(string directory) : ISettingsService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string file = Path.Combine(directory, "settings.json");
    public async Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(file)) return new();
            await using var stream = File.OpenRead(file);
            var settings = await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream, cancellationToken: cancellationToken)
                ?? throw new InvalidDataException("Settings file is empty.");
            Validate(settings);
            return settings;
        }
        finally { gate.Release(); }
    }
    public async Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        await gate.WaitAsync(cancellationToken);
        string? temporary = null;
        try
        {
            Directory.CreateDirectory(directory);
            temporary = Path.Combine(directory, $"settings-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(settings), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, file, overwrite: true);
        }
        finally
        {
            if (temporary != null && File.Exists(temporary)) File.Delete(temporary);
            gate.Release();
        }
    }
    private static void Validate(ApplicationSettings settings)
    {
        if (settings.SchemaVersion != 1) throw new InvalidDataException("Unsupported settings schema; existing data was preserved.");
        if (!Enum.IsDefined(settings.Theme) || settings.Language is not ("en" or "fa"))
            throw new InvalidDataException("Unsupported appearance settings.");
    }
}
