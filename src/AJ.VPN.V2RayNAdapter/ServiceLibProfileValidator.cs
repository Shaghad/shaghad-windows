using AJ.VPN.Application;
using AJ.VPN.Domain;
using ServiceLib.Handler.Fmt;

namespace AJ.VPN.V2RayNAdapter;

public sealed class ServiceLibProfileValidator : IProfileValidator
{
    public ProfileValidation Validate(string input)
    {
        // Initial supported subset intentionally excludes arbitrary JSON/YAML and custom core arguments.
        if (input.Length > 16_384 || input.Contains('\n') || input.Contains('\r') ||
            !Uri.TryCreate(input, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("vless" or "trojan" or "socks" or "socks5" or "ss"))
            return new(false, null, "Enter one supported share URI (VLESS, Trojan, SOCKS or Shadowsocks). Batch import is unavailable.");
        try
        {
            var profile = FmtHandler.ResolveConfig(input, out _);
            if (profile == null || !profile.IsValid() || string.IsNullOrWhiteSpace(profile.Address) || profile.Port is < 1 or > 65535)
                return new(false, null, "The profile is invalid. Check its protocol, host, port and credentials.");
            if (string.Equals(profile.AllowInsecure, "true", StringComparison.OrdinalIgnoreCase))
                return new(false, null, "Profiles that disable TLS verification are not accepted.");
            var server = new Server(Guid.NewGuid().ToString("N"), string.IsNullOrWhiteSpace(profile.Remarks) ? "Imported server" : profile.Remarks,
                profile.ConfigType.ToString(), profile.Address, profile.Port);
            return new(true, server, "Validated by v2rayN ServiceLib. Preview only; credentials are not saved and connection is unavailable.");
        }
        catch { return new(false, null, "Profile validation failed. No profile was saved."); }
    }
}
