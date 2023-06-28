using BepInEx;
using System.Collections.Generic;
using Comfort.Common;
using UnityEngine;
using EFT;
using BatterySystem.Configs;

namespace BatterySystem
{
   /*TODO: 
    * equipping and removing headwear gives infinite nvg
    * switch to coroutines
	* Apply to Thermals aswell
	* Sound when toggling battery runs out or is removed or added
	* New model for battery
	* battery recharger - idea by Props
	*/
	[BepInPlugin("com.jiro.batterysystem", "BatterySystem", "1.0.0")]
	public class BatterySystemPlugin : BaseUnityPlugin
	{
		public static GameWorld gameWorld;
		public static float cooldown = 2.5f;
		public static Dictionary<string, float> itemDrainMultiplier = new Dictionary<string, float>();
		void Awake()
		{
			BatterySystemConfig.Init(Config);
			new BatterySystemPatch().Enable();
			new NightVisionPatch().Enable();
			//update dictionary with values
			//foreach (ItemTemplate template in ItemTemplates)
			{
				itemDrainMultiplier.Add("5c0696830db834001d23f5da", 1f); // PNV-10T Night Vision Goggles
				itemDrainMultiplier.Add("5c0558060db834001b735271", 2f); // GPNVG-18 Night Vision goggles
				itemDrainMultiplier.Add("5c066e3a0db834001b7353f0", 1f); // Armasight N-15 Night Vision Goggles
				itemDrainMultiplier.Add("57235b6f24597759bf5a30f1", 0.5f); // AN/PVS-14 Night Vision Monocular
				//itemDrainMultiplier.Add("5c110624d174af029e69734c", 4f); // T-7 Thermal Goggles with a Night Vision mount
			}
		}
		void Update() // battery is drained in Update() and applied
		{
			if (Time.time > cooldown) //&& BatterySystemConfig.EnableMod.Value)
			{
				cooldown = Time.time + 1;
				gameWorld = Singleton<GameWorld>.Instance;
				if (gameWorld == null || gameWorld.MainPlayer == null) return;
				BatterySystemPatch.CheckIfDraining();
				if (BatterySystemPatch.drainingBattery)
				{
					Mathf.Clamp(BatterySystemPatch.batteryResource.Value -= 1 / 36 * itemDrainMultiplier[BatterySystemPatch.headWearNVG.Item.TemplateId], 0, 100);// * BatterySystemConfig.DrainMultiplier.Value //Default battery lasts 1 hr * configmulti * itemmulti, itemmulti was dev_raccoon's idea!
				}
				else cooldown = float.PositiveInfinity; // doesn't run unless needed
			}
			//Item itemInHands = inventoryControllerClass.ItemInHands;
			//List<string> equippedTpl = inventoryControllerClass.Inventory.EquippedInSlotsTemplateIds;
		}
		/* Credit to Nexus and Fontaine for showing me this!
		private static IEnumerator LowerThermalBattery(Player player)
		{
			if (player == null)
			{
				yield break;
			}

			while (player.HealthController != null && player.HealthController.IsAlive)
			{
				yield return null;
				ThermalVisionComponent thermalVisionComponent = player.ThermalVisionObserver.GetItemComponent();
				if (thermalVisionComponent == null)
				{
					continue;
				}

				if (thermalVisionComponent.Togglable.On)
				{
					IEnumerable<ResourceComponent> resourceComponents = thermalVisionComponent.Item.GetItemComponentsInChildren<ResourceComponent>(false);
					foreach (ResourceComponent resourceComponent in resourceComponents)
					{
						if (resourceComponent == null)
						{
							thermalVisionComponent.Togglable.Set(false);
							continue;
						}

						Single targetValue = resourceComponent.Value - Instance.BatteryDrainRate.Value * Time.deltaTime;
						if (targetValue <= 0f)
						{
							targetValue = 0f;
						}

						if ((resourceComponent.Value = targetValue).IsZero())
						{
							thermalVisionComponent.Togglable.Set(false);
						}
					}
				}
			}*/
	}
}