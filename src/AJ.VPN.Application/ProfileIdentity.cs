using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
namespace AJ.VPN.Application;

/// <summary>Opaque wire-configuration identity. Remarks never identify a server; secrets never leave the hash.</summary>
public static class ProfileIdentity
{
    public static string Of(string shareUri)
    {
        var canonical = shareUri.Split('#', 2)[0];
        if (canonical.StartsWith("vmess://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded = canonical[8..].Replace('-', '+').Replace('_', '/');
                var node = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '='))))!.AsObject();
                node.Remove("ps"); node.Remove("remarks");
                var ordered = new JsonObject(); foreach (var field in node.OrderBy(e => e.Key, StringComparer.Ordinal)) ordered[field.Key] = field.Value?.DeepClone();
                canonical = "vmess://" + ordered.ToJsonString();
            }
            catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException or InvalidOperationException) { }
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
