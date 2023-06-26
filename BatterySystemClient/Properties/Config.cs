﻿using BepInEx.Configuration;

namespace BatterySystem.Configs
{
	internal class BatterySystemConfig
	{
		public static ConfigEntry<bool> EnableMod { get; private set; }
		public static ConfigEntry<float> DrainMultiplier { get; private set; }

		public static void Init(ConfigFile Config)
		{
			string generalSettings = "General Settings";

			EnableMod = Config.Bind(generalSettings, "Enable Mod", true,
				new ConfigDescription("Enable or disable the mod.",
				null,
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 100 })); 
			DrainMultiplier = Config.Bind(generalSettings, "Battery Drain Multiplier", 1f,
				new ConfigDescription("Adjust the drain multiplier when nvg is on. By default a battery lasts an hour.",
				new AcceptableValueRange<float>(0f, 5f),
				new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));

		}
	}
}
