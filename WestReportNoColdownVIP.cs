using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Newtonsoft.Json;
using VipCoreApi;
using WestReportSystemApiReborn;

namespace WestReportNoColdownVIP
{
    public class WestReportNoColdownVIP : BasePlugin
    {
        public override string ModuleName => "WestReportNoCooldownVIP";
        public override string ModuleVersion => "v1.0";
        public override string ModuleAuthor => "E!N";
        public override string ModuleDescription => "Module disabling the delay of sending reports for VIP players with a certain group";

        private IWestReportSystemApi? _wrsApi;
        private IVipCoreApi? _vipCoreApi;
        private NoCooldownVIPConfig? _config;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            string configDirectory = GetConfigDirectory();
            EnsureConfigDirectory(configDirectory);
            string configPath = Path.Combine(configDirectory, "NoCooldownVIPConfig.json");
            _config = NoCooldownVIPConfig.Load(configPath);

            InitializeDependencies();

            if (_vipCoreApi == null || _wrsApi == null)
            {
                Console.WriteLine($"{ModuleName} | Error: Essential services (WestReportSystem API and/or VIP CORE) are not available.");
            }
            else
            {
                _wrsApi.OnReportSend += ResetCooldown;
                Console.WriteLine($"{ModuleName} | Successfully subscribed to report send events.");
            }
        }
        private static string GetConfigDirectory()
        {
            return Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/configs/plugins/WestReportSystem/Modules");
        }

        private void EnsureConfigDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"{ModuleName} | Created configuration directory at: {directoryPath}");
            }
        }

        private void InitializeDependencies()
        {
            _vipCoreApi = new PluginCapability<IVipCoreApi>("vipcore:core").Get();
            _wrsApi = IWestReportSystemApi.Capability.Get();
        }

        private void ResetCooldown(CCSPlayerController? sender, CCSPlayerController violator, string reason)
        {
            if (_config == null)
            {
                Console.WriteLine($"{ModuleName} | Error: Configuration is not loaded.");
                return;
            }

            string? groups = _config.NoCooldownVIPGroup;
            var players = Utilities.GetPlayers();
            if (groups != null)
            {
                foreach (var player in players)
                {
                    string? vip_group = _vipCoreApi?.GetClientVipGroup(player);

                    if (vip_group == groups)
                    {
                        _wrsApi?.WRS_ClearCooldown(player);
                    }
                }
            }
        }

        public override void Unload(bool hotReload)
        {
            if (_wrsApi != null)
            {
                _wrsApi.OnReportSend -= ResetCooldown;
            }
        }

        public class NoCooldownVIPConfig
        {
            public string? NoCooldownVIPGroup { get; set; }

            public static NoCooldownVIPConfig Load(string configPath)
            {
                if (!File.Exists(configPath))
                {
                    NoCooldownVIPConfig defaultConfig = new();
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented));
                    return defaultConfig;
                }

                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<NoCooldownVIPConfig>(json) ?? new NoCooldownVIPConfig();
            }
        }
    }
}