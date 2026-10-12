using System.Text;

namespace AJ.VPN.Application;

public enum ImportKind { Unsupported, Profiles, Subscription }
public sealed record ImportInspection(ImportKind Kind, string Content, int Count = 0);

/// <summary>Pure inspection: never downloads, persists or executes clipboard content.</summary>
public static class SmartImport
{
    public const int MaximumBytes = 2_000_000;
    public static ImportInspection Inspect(string text, Func<string, int> validateProfiles)
    {
        if (text.Length > MaximumBytes) throw new InvalidDataException("Import exceeds the 2 MB limit.");
        text = text.Trim();
        if (text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            LibraryService.ValidateSubscriptionUrl(text);
            if (text.Any(char.IsWhiteSpace)) throw new InvalidDataException("Enter one HTTPS subscription URL.");
            return new(ImportKind.Subscription, text);
        }
        if (text.Length == 0) return new(ImportKind.Unsupported, "");
        try { return new(ImportKind.Profiles, text, validateProfiles(text)); }
        catch (InvalidDataException) { return new(ImportKind.Unsupported, ""); }
    }
    public static async Task<string> ReadFileAsync(string path, CancellationToken token = default)
    {
        // The existing parser accepts share-link lists, not Xray JSON or YAML schemas.
        if (!string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Only .txt share-link lists or Base64 subscriptions are supported. JSON and YAML configurations are not supported.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        if (stream.Length > MaximumBytes) throw new InvalidDataException("Import exceeds the 2 MB limit.");
        using var memory = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (memory.Length + count > MaximumBytes) throw new InvalidDataException("Import exceeds the 2 MB limit.");
            memory.Write(buffer, 0, count);
        }
        var bytes = memory.ToArray();
        var offset = bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? 3 : 0;
        try { return new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset); }
        catch (DecoderFallbackException) { throw new InvalidDataException("Use a UTF-8 .txt share-link list."); }
    }
}
