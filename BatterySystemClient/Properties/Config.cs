using BepInEx.Configuration;

namespace BatterySystem.Configs
{
	internal class BatterySystemConfig
	{
		public static ConfigEntry<bool> EnableMod { get; private set; }
		public static ConfigEntry<bool> EnableLogs { get; private set; }
		public static ConfigEntry<float> DrainMultiplier { get; private set; }
		//public static ConfigEntry<int> SpawnDurabilityMin { get; private set; }
		//public static ConfigEntry<int> SpawnDurabilityMax { get; private set; }

		public static ConfigEntry<float> CompressorMixerVolume { get; private set; }
		public static ConfigEntry<float> MainMixerVolume { get; private set; }
		public static ConfigEntry<float> CompressorGain { get; private set; }
		//Singleton<BetterAudio>.Instance.Master.GetFloat("CompressorHighFrequenciesGain", out temp);

		private static string generalSettings = "General Settings";

		public static void Init(ConfigFile Config)
		{
			{
				EnableMod = Config.Bind(generalSettings, "Enable Mod", true,
					new ConfigDescription("Enable or disable the mod. Requires the game to be restarted.",
					null,
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = 100 }));

				EnableLogs = Config.Bind(generalSettings, "Enable Logs", false,
					new ConfigDescription("Enable or disable logging.",
					null,
					new ConfigurationManagerAttributes { IsAdvanced = true, Order = 50 }));

				DrainMultiplier = Config.Bind(generalSettings, "Battery Drain Multiplier", 1f,
					new ConfigDescription("Adjust the drain multiplier when NVG is on. By default a battery lasts an hour on NVGs and 2.5 hours on collimators.",
					new AcceptableValueRange<float>(0f, 10f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));

				/*SpawnDurabilityMin = Config.Bind(generalSettings, "Spawn Durability Min", 5,
					new ConfigDescription("Adjust the minimum durability a battery can spawn with on bots.",
					new AcceptableValueRange<int>(0, 100),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -50 }));

				SpawnDurabilityMax = Config.Bind(generalSettings, "Spawn Durability Max", 15,
					new ConfigDescription("Adjust the maximum durability a battery can spawn with on bots. This must be ATLEAST the same value as Spawn Durability Minimum.",
					new AcceptableValueRange<int>(0, 100),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -100 }));
				
				CompressorMixerVolume = Config.Bind(generalSettings, "CompressorMixerVolume", -3f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-30f, 10f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -180 }));

				MainMixerVolume = Config.Bind(generalSettings, "MainMixerVolume", -0f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-30f, 10f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -230 }));
				*/
			}
		}
	}
}
