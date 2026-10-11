using AJ.VPN.Application;
using AJ.VPN.Domain;
using ServiceLib.Handler.Fmt;

namespace AJ.VPN.V2RayNAdapter;

public sealed class ServiceLibProfileValidator : IProfileValidator
{
    public ServiceLibProfileValidator()
    {
        // Upstream parsers catch and log exception text. No upstream file targets are configured.
        // Disable their logging before accepting any user-controlled payload.
        NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
    }
    public ProfileValidation Validate(string input)
    {
        // Initial supported subset intentionally excludes arbitrary JSON/YAML and custom core arguments.
        var scheme = input.Split("://", 2)[0].ToLowerInvariant();
        Uri.TryCreate(input, UriKind.Absolute, out var uri);
        if (input.Length > 16_384 || input.Contains('\n') || input.Contains('\r') ||
            scheme is not ("vless" or "vmess" or "trojan" or "ss") || (scheme != "vmess" && uri == null))
            return new(false, null, "Enter a VLESS, VMess, Trojan or Shadowsocks share URI.");
        try
        {
            var queryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in (uri?.Query ?? "").TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                var key = Uri.UnescapeDataString(pair[0]).ToLowerInvariant();
                if (!queryKeys.Add(key)) return new(false, null, "Ambiguous duplicate query parameters are not accepted.");
                var originalValue = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : "true";
                var value = originalValue.ToLowerInvariant();
                if (key == "type" && originalValue is not ("tcp" or "raw" or "ws" or "grpc" or "httpupgrade" or "xhttp" or "kcp"))
                    return new(false, null, "Unsupported transport. No profile was saved.");
                if (key == "security" && value is not ("" or "none" or "tls" or "reality"))
                    return new(false, null, "Unsupported TLS/security mode. No profile was saved.");
                if (key is "allowinsecure" or "insecure" or "skip-cert-verify" or "skiptlsverify" && value is not ("false" or "0"))
                    return new(false, null, "Profiles that disable TLS verification are not accepted.");
            }
            var profile = FmtHandler.ResolveConfig(input, out _);
            if (profile == null || !profile.IsValid() || string.IsNullOrWhiteSpace(profile.Address) || profile.Port is < 1 or > 65535)
                return new(false, null, "The profile is invalid. Check its protocol, host, port and credentials.");
            if (!string.IsNullOrEmpty(profile.AllowInsecure) && profile.AllowInsecure.ToLowerInvariant() is not ("false" or "0"))
                return new(false, null, "Profiles that disable TLS verification are not accepted.");
            if (string.IsNullOrWhiteSpace(profile.Password) ||
                (scheme is "vless" or "vmess" && !Guid.TryParse(profile.Password, out _)) ||
                profile.Network is not (null or "" or "tcp" or "raw" or "ws" or "grpc" or "httpupgrade" or "xhttp" or "kcp") ||
                profile.StreamSecurity is not (null or "" or "none" or "tls" or "reality") ||
                profile.Address.Any(char.IsControl) || profile.Address.Contains('/') || profile.Address.Length > 253)
                return new(false, null, "Invalid credentials, host or unsupported transport.");
            var name = new string((profile.Remarks ?? "").Where(c => !char.IsControl(c) && c is not (>= '\u202a' and <= '\u202e') and not (>= '\u2066' and <= '\u2069')).Take(100).ToArray());
            var server = new Server(Guid.NewGuid().ToString("N"), string.IsNullOrWhiteSpace(name) ? "Imported server" : name,
                profile.ConfigType.ToString(), profile.Address, profile.Port);
            return new(true, server, "Share URI validated by v2rayN ServiceLib. Native core validation runs before connecting.");
        }
        catch { return new(false, null, "Profile validation failed. No profile was saved."); }
    }
}
