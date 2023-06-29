using BepInEx;
using System.Collections.Generic;
using Comfort.Common;
using UnityEngine;
using EFT;
using BatterySystem.Configs;
using EFT.InventoryLogic;
using System.Collections;

namespace BatterySystem
{
	/*TODO: 
	 * equipping and removing headwear gives infinite nvg
	 * switch to coroutines
	 * Apply to Thermals aswell
	 * Sound when toggling battery runs out or is removed or added
	 * New model for battery
	 * battery recharger - idea by Props
	 * make batteries uninsurable, because duhW
	 */
	[BepInPlugin("com.jiro.batterysystem", "BatterySystem", "1.1.0")]
	[BepInDependency("com.spt-aki.core", "3.5.7")]
	public class BatterySystemPlugin : BaseUnityPlugin
	{
		public static GameWorld gameWorld;
		public static float headWearCooldown = 2.5f;
		public static Dictionary<string, float> headWearDrainMultiplier = new Dictionary<string, float>();
		public static Dictionary<ResourceComponent, bool> batteryDictionary = new Dictionary<ResourceComponent, bool>();
		//resource drain all batteries that are on // using dictionary to help and sync draining batteries
		void Awake()
		{
			BatterySystemConfig.Init(Config);
			new PlayerInitPatch().Enable();
			new HeadWearDevicePatch().Enable();
			new SightDevicePatch().Enable();
			new NightVisionPatch().Enable();
			//update dictionary with values
			//foreach (ItemTemplate template in ItemTemplates)
			{
				headWearDrainMultiplier.Add("5c0696830db834001d23f5da", 1f); // PNV-10T Night Vision Goggles, AA Battery
				headWearDrainMultiplier.Add("5c0558060db834001b735271", 2f); // GPNVG-18 Night Vision goggles, CR123 battery pack
				headWearDrainMultiplier.Add("5c066e3a0db834001b7353f0", 1f); // Armasight N-15 Night Vision Goggles, single CR123A lithium battery
				headWearDrainMultiplier.Add("57235b6f24597759bf5a30f1", 0.5f); // AN/PVS-14 Night Vision Monocular, AA Battery
				headWearDrainMultiplier.Add("5c110624d174af029e69734c", 3f); // T-7 Thermal Goggles with a Night Vision mount, CR123
			}
		}
		void Update() // battery is drained in Update() and applied
		{
			if (Time.time > headWearCooldown && BatterySystemConfig.EnableMod.Value)
			{
				headWearCooldown = Time.time + 1;
				gameWorld = Singleton<GameWorld>.Instance;
				if (gameWorld == null || gameWorld.MainPlayer == null) return;

				HeadWearDevicePatch.CheckHeadWearIfDraining();


				if (CameraClass.Instance.NightVision.InProcessSwitching) headWearCooldown = Time.time + 0.02f; // workaround, fix this l8r
				else if (HeadWearDevicePatch.drainingBattery)
				{
					DrainBatteries();
				}
				else headWearCooldown = 5; // doesn't run unless needed
										   //currently if a bots equipment changes, then cooldown is reset.
			}
			//Item itemInHands = inventoryControllerClass.ItemInHands;
			//List<string> equippedTpl = inventoryControllerClass.Inventory.EquippedInSlotsTemplateIds;
		}
		private static void DrainBatteries()
		{
			//foreach (ResourceComponent resourceComponent in batteryDictionary.Keys)
			{
				//if (batteryDictionary[resourceComponent])
				{
					Mathf.Clamp(HeadWearDevicePatch.batteryResource.Value -= 1 / 36f
						* BatterySystemConfig.DrainMultiplier.Value
						* headWearDrainMultiplier[HeadWearDevicePatch.batteryResource.Item.Parent.Item.TemplateId], 0, 100);
					//Default battery lasts 1 hr * configmulti * itemmulti, itemmulti was dev_raccoon's idea!
				}
			}
			return;
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