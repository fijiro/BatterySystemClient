using BepInEx.Configuration;

namespace BatterySystem.Configs
{
	internal class BatterySystemConfig
	{
		public static ConfigEntry<bool> EnableMod { get; private set; }

		public static void Init(ConfigFile Config)
		{
			string configSettings = "General Settings";

			EnableMod = Config.Bind(configSettings, "Enable Mod", true,
				new ConfigDescription("Enable or disable the mod",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 100 }));

		}
	}
}
