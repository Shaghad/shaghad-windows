using System.Text.Json.Nodes;
using AJ.VPN.Application;
using AJ.VPN.Domain;
using ServiceLib.Enums;
using ServiceLib.Handler.Fmt;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Services.CoreConfig;

namespace AJ.VPN.V2RayNAdapter;

public sealed class ServiceLibConfigurationGenerator(IProfileValidator validator) : ICoreConfigurationGenerator
{
    public string Generate(SavedProfile profile, CoreKind kind, int port)
    {
        if (port is < 1024 or > 65535 || !validator.Validate(profile.ShareUri).IsValid) throw new InvalidDataException("The saved profile is invalid.");
        var node = FmtHandler.ResolveConfig(profile.ShareUri, out _) ?? throw new InvalidDataException("Profile parsing failed.");
        var core = kind == CoreKind.Xray ? ECoreType.Xray : ECoreType.sing_box;
        node.CoreType = core;
        var config = new Config
        {
            CoreBasicItem = new() { Loglevel = "warning", LogEnabled = false, EnableCacheFile4Sbox = false },
            Mux4RayItem = new(), Mux4SboxItem = new(), GrpcItem = new(), KcpItem = new(),
            RoutingBasicItem = new(), TunModeItem = new(), GuiItem = new(), SimpleDNSItem = new(),
            Inbound = [], CoreTypeItem = [], HappyEyeballs4RayItem = new()
        };
        var context = new CoreConfigContext { Node = node, RunCoreType = core, AppConfig = config, IsWindows = true, IsTunEnabled = false };
        var result = kind == CoreKind.Xray ? new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(port)
            : new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(port);
        if (!result.Success || result.Data is not string json) throw new InvalidDataException("ServiceLib cannot generate this profile for the selected core. Try the other core or a supported transport.");
        var root = JsonNode.Parse(json)?.AsObject() ?? throw new InvalidDataException("Generated configuration is invalid.");
        // Narrow upstream's template to exactly one selected outbound: no direct fallback, API, TUN, file logs or rule downloads.
        var proxy = root["outbounds"]?.AsArray().FirstOrDefault(n => n?["tag"]?.GetValue<string>() == "proxy")?.DeepClone()
            ?? throw new InvalidDataException("The selected proxy outbound was not generated.");
        root["outbounds"] = new JsonArray(proxy);
        root.Remove("experimental"); root.Remove("api"); root.Remove("stats"); root.Remove("policy"); root.Remove("observatory");
        if (kind == CoreKind.Xray)
        {
            root["log"] = new JsonObject { ["loglevel"] = "warning", ["access"] = "none" };
            root.Remove("dns");
            root["routing"] = new JsonObject { ["domainStrategy"] = "AsIs", ["rules"] = new JsonArray() };
        }
        else
        {
            root["log"] = new JsonObject { ["level"] = "warn", ["timestamp"] = false };
            root["dns"] = new JsonObject { ["servers"] = new JsonArray(new JsonObject { ["type"] = "local", ["tag"] = "bootstrap" }) };
            root["route"] = new JsonObject { ["final"] = "proxy", ["default_domain_resolver"] = "bootstrap" };
        }
        var minimal = new JsonObject();
        foreach (var key in kind == CoreKind.Xray ? new[] { "log", "inbounds", "outbounds", "routing" } : new[] { "log", "inbounds", "outbounds", "route", "dns" })
            minimal[key] = root[key]?.DeepClone();
        return minimal.ToJsonString();
    }
}
