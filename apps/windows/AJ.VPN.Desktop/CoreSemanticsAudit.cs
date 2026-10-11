using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using AJ.VPN.Domain;
using AJ.VPN.Infrastructure;
using AJ.VPN.V2RayNAdapter;

namespace AJ.VPN.Desktop;

internal static class CoreSemanticsAudit
{
    public static async Task RunAsync(string encryptedDirectory, string resultFile)
    {
        var library = await new EncryptedLibraryStore(encryptedDirectory).LoadAsync();
        var generator = new ServiceLibConfigurationGenerator(new ServiceLibProfileValidator());
        var results = new List<object>();
        foreach (var profile in library.Profiles.Where(p => p.Server.Protocol == "VLESS"))
        {
            try
            {
            var x = JsonNode.Parse(generator.Generate(profile, CoreKind.Xray, 19080))!["outbounds"]![0]!;
            var node = ServiceLib.Handler.Fmt.FmtHandler.ResolveConfig(profile.ShareUri, out _)!;
            if (new ServiceLibProfileValidator().CoreCompatibilityError(profile.ShareUri, CoreKind.SingBox) is string error)
            {
                var settings = x["settings"]!["vnext"]?[0]?["users"]?[0] ?? x["settings"]!;
                results.Add(new
                {
                    profileIndex = results.Count, core = "SingBox", status = "unsupported", reason = error,
                    xrayPreservesEncryption = settings["encryption"]?.ToString() == node.GetProtocolExtra().VlessEncryption,
                    xrayTransport = x["streamSettings"]?["network"]?.ToString(),
                    privateValuesOmitted = true
                });
                continue;
            }
            var s = JsonNode.Parse(generator.Generate(profile, CoreKind.SingBox, 19080))!["outbounds"]![0]!;
            var endpoint = x["settings"]!["vnext"]?[0] ?? x["settings"]!;
            var user = endpoint["users"]?[0] ?? endpoint;
            var stream = x["streamSettings"]!;
            var security = stream["security"]?.ToString() ?? "none";
            var tls = stream[security == "reality" ? "realitySettings" : "tlsSettings"];
            static string Value(JsonNode? value) => value?.ToJsonString() ?? "null";
            static bool Equal(JsonNode? a, JsonNode? b) => Value(a) == Value(b);
            results.Add(new
            {
                profileIndex = results.Count,
                serverEqual = Equal(endpoint["address"], s["server"]), portEqual = Equal(endpoint["port"], s["server_port"]),
                uuidEqual = Equal(user["id"], s["uuid"]), flowEqual = (user["flow"]?.ToString() ?? "") == (s["flow"]?.ToString() ?? ""),
                vlessEncryptionIsNone = (user["encryption"]?.ToString() ?? "none") == "none",
                vlessEncryptionIsEmpty = string.IsNullOrEmpty(user["encryption"]?.ToString()),
                vlessEncryptionIsMlkem = user["encryption"]?.ToString().StartsWith("mlkem", StringComparison.Ordinal) ?? false,
                singboxPacketEncoding = s["packet_encoding"]?.ToString() ?? "none",
                singboxOutboundKeys = s.AsObject().Select(p => p.Key).ToArray(),
                singboxInbound = JsonNode.Parse(generator.Generate(profile, CoreKind.SingBox, 19080))!["inbounds"]![0]!.AsObject().Select(p => p.Key).ToArray(),
                security, tlsEnabled = s["tls"]?["enabled"]?.GetValue<bool>() ?? false,
                serverNameEqual = Equal(tls?["serverName"], s["tls"]?["server_name"]),
                publicKeyEqual = Equal(tls?["publicKey"], s["tls"]?["reality"]?["public_key"]),
                shortIdEqual = Equal(tls?["shortId"], s["tls"]?["reality"]?["short_id"]),
                fingerprintEqual = Equal(tls?["fingerprint"], s["tls"]?["utls"]?["fingerprint"]),
                alpnEqual = Equal(tls?["alpn"], s["tls"]?["alpn"]),
                xrayTransport = stream["network"]?.ToString(), singboxTransport = s["transport"]?["type"]?.ToString() ?? "tcp",
                pathEqual = Equal(stream["httpupgradeSettings"]?["path"] ?? stream["wsSettings"]?["path"], s["transport"]?["path"]),
                hostEqual = Equal(stream["httpupgradeSettings"]?["host"] ?? stream["wsSettings"]?["headers"]?["Host"], s["transport"]?["host"] ?? s["transport"]?["headers"]?["Host"]),
                httpUpgradeEarlyData = stream["httpupgradeSettings"]?["path"]?.ToString().Contains("ed=") ?? false,
                explicitUpgradeHost = !string.IsNullOrEmpty(stream["httpupgradeSettings"]?["host"]?.ToString()),
                upgradePathHasQuery = stream["httpupgradeSettings"]?["path"]?.ToString().Contains('?') ?? false,
                upgradePathHasPercent = stream["httpupgradeSettings"]?["path"]?.ToString().Contains('%') ?? false,
                xrayHttpHeader = stream["tcpSettings"]?["header"]?["type"]?.ToString() ?? "none",
                xrayDomainStrategy = stream["sockopt"]?["domainStrategy"]?.ToString() ?? "default",
                singboxDomainStrategy = s["domain_strategy"]?.ToString() ?? "default"
            });
            }
            catch (Exception ex) { results.Add(new { profileIndex = results.Count, failureType = ex.GetType().Name }); }
        }
        await File.WriteAllTextAsync(resultFile, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    }
}
