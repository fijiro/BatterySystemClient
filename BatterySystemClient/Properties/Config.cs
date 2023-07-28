using BepInEx.Configuration;

namespace BatterySystem.Configs
{
	internal class BatterySystemConfig
	{
		public static ConfigEntry<bool> EnableMod { get; private set; }
		public static ConfigEntry<bool> EnableLogs { get; private set; }
		public static ConfigEntry<float> DrainMultiplier { get; private set; }
		public static ConfigEntry<int> SpawnDurabilityMin { get; private set; }
		public static ConfigEntry<int> SpawnDurabilityMax { get; private set; }

		public static ConfigEntry<float> CompressorMixerVolume { get; private set; }
		public static ConfigEntry<float> OcclusionMixerVolume { get; private set; }
		public static ConfigEntry<float> EnvironmentMixerVolume { get; private set; }
		public static ConfigEntry<float> AmbientMixerVolume { get; private set; }
		public static ConfigEntry<float> GunsMixerVolume { get; private set; }
		public static ConfigEntry<float> MainMixerVolume { get; private set; }
		public static ConfigEntry<float> GunsMixerTinnitusSendLevel { get; private set; }
		public static ConfigEntry<float> MainMixerTinnitusSendLevel { get; private set; }
		public static ConfigEntry<float> AmbientMixerOcclusionSendLevel { get; private set; }
		public static ConfigEntry<float> CompressorAttack { get; private set; }
		public static ConfigEntry<float> CompressorGain { get; private set; }
		public static ConfigEntry<float> CompressorRelease { get; private set; }
		public static ConfigEntry<float> CompressorThreshold { get; private set; }
		public static ConfigEntry<float> CompressorDistortion { get; private set; }
		public static ConfigEntry<float> CompressorResonance { get; private set; }
		public static ConfigEntry<float> CompressorCutoff { get; private set; }
		public static ConfigEntry<float> CompressorLowpass { get; private set; }
		public static ConfigEntry<float> CompressorHighFrequenciesGain { get; private set; }
		//Singleton<BetterAudio>.Instance.Master.GetFloat("CompressorHighFrequenciesGain", out temp);

		private static string generalSettings = "General Settings";
		private static string audioSettings = "Headset Audio Mixer";

		public static void Init(ConfigFile Config)
		{
			{
				EnableMod = Config.Bind(generalSettings, "Enable Mod", true,
					new ConfigDescription("Enable or disable the mod.",
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

				SpawnDurabilityMin = Config.Bind(generalSettings, "Spawn Durability Min", 5,
					new ConfigDescription("Adjust the minimum durability a battery can spawn with on bots.",
					new AcceptableValueRange<int>(0, 100),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -50 }));

				SpawnDurabilityMax = Config.Bind(generalSettings, "Spawn Durability Max", 15,
					new ConfigDescription("Adjust the maximum durability a battery can spawn with on bots. This must be ATLEAST the same value as Spawn Durability Minimum.",
					new AcceptableValueRange<int>(0, 100),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -100 }));
			}
			/* unused, will do something with this soon
			{
				CompressorMixerVolume = Config.Bind(audioSettings, "CompressorMixerVolume", -3f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -180 }));
				OcclusionMixerVolume = Config.Bind(audioSettings, "OcclusionMixerVolume", -50f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -190 }));
				EnvironmentMixerVolume = Config.Bind(audioSettings, "EnvironmentMixerVolume", -50f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -200 }));
				AmbientMixerVolume = Config.Bind(audioSettings, "AmbientMixerVolume", -2f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -210 }));
				GunsMixerVolume = Config.Bind(audioSettings, "GunsMixerVolume", -50f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -220 }));
				MainMixerVolume = Config.Bind(audioSettings, "MainMixerVolume", -80f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -230 }));
				MainMixerTinnitusSendLevel = Config.Bind(audioSettings, "MainMixerTinnitusSendLevel", -80f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -240 }));
				GunsMixerTinnitusSendLevel = Config.Bind(audioSettings, "GunsMixerTinnitusSendLevel", -80f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -250 }));
				AmbientMixerOcclusionSendLevel = Config.Bind(audioSettings, "AmbientMixerOcclusionSendLevel", -17f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -260 }));
				CompressorAttack = Config.Bind(audioSettings, "CompressorAttack", 0f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -270 }));
				CompressorGain = Config.Bind(audioSettings, "CompressorGain", 0f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -280 }));
				CompressorRelease = Config.Bind(audioSettings, "CompressorRelease", 0f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -290 }));
				CompressorThreshold = Config.Bind(audioSettings, "CompressorThreshold", 0f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -300 }));
				CompressorDistortion = Config.Bind(audioSettings, "CompressorDistortion", 0f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -310 }));
				CompressorResonance = Config.Bind(audioSettings, "CompressorResonance", 0f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -320 }));
				CompressorCutoff = Config.Bind(audioSettings, "CompressorCutoff", 20f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -330 }));
				CompressorLowpass = Config.Bind(audioSettings, "CompressorLowpass", 21000f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(18000f, 22000f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -340 }));
				CompressorHighFrequenciesGain = Config.Bind(audioSettings, "CompressorHighFrequenciesGain", 1f,
					new ConfigDescription("",
					new AcceptableValueRange<float>(-100f, 100f),
					new ConfigurationManagerAttributes { IsAdvanced = false, Order = -350 }));
			}*/
		}
	}
}
