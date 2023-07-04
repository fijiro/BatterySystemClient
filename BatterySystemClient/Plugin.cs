using BepInEx;
using System.Reflection;
using Aki.Reflection.Patching;
using HarmonyLib;
using System.Collections.Generic;
using Comfort.Common;
using UnityEngine;
using EFT;
using BatterySystem.Configs;
using EFT.InventoryLogic;
using System.Collections;
using BepInEx.Logging;
using System.Linq;

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
		public static float mainCooldown = 2.5f;
		public static Dictionary<string, float> headWearDrainMultiplier = new Dictionary<string, float>();
		public static Dictionary<Item, bool> batteryDictionary = new Dictionary<Item, bool>();
		//resource drain all batteries that are on // using dictionary to help and sync draining batteries
		void Awake()
		{
			BatterySystemConfig.Init(Config);
			new PlayerInitPatch().Enable();
			new ApplyItemPatch().Enable();
			new SightDevicePatch().Enable();
			new NvgHeadWearPatch().Enable();
			//update dictionary with values
			//foreach (ItemTemplate template in ItemTemplates)
			{
				headWearDrainMultiplier.Add("5c0696830db834001d23f5da", 1f); // PNV-10T Night Vision Goggles, AA Battery
				headWearDrainMultiplier.Add("5c0558060db834001b735271", 2f); // GPNVG-18 Night Vision goggles, CR123 battery pack
				headWearDrainMultiplier.Add("5c066e3a0db834001b7353f0", 1f); // Armasight N-15 Night Vision Goggles, single CR123A lithium battery
				headWearDrainMultiplier.Add("57235b6f24597759bf5a30f1", 0.5f); // AN/PVS-14 Night Vision Monocular, AA Battery
				headWearDrainMultiplier.Add("5c110624d174af029e69734c", 3f); // T-7 Thermal Goggles with a Night Vision mount, CR123
				//specter uses cr2032, hhs cr123
			}
		}
		void Update() // battery is drained in Update() and applied
		{


			if (Time.time > mainCooldown && BatterySystemConfig.EnableMod.Value)
			{
				mainCooldown = Time.time + 1f;
				gameWorld = Singleton<GameWorld>.Instance;
				if (gameWorld == null || gameWorld.MainPlayer == null) return;

				BatterySystem.CheckHeadWearIfDraining(); 
				BatterySystem.Logger.LogInfo("Togglable: " + BatterySystem.headWearItem.GetItemComponentsInChildren<TogglableComponent>().FirstOrDefault()?.On);
				BatterySystem.CheckSightIfDraining();
				DrainBatteries();
				//if (CameraClass.Instance.NightVision.InProcessSwitching) headWearCooldown = Time.time + 0.02f; // workaround, fix this l8r

				// doesn't run unless needed
			}
			//Item itemInHands = inventoryControllerClass.ItemInHands;
			//List<string> equippedTpl = inventoryControllerClass.Inventory.EquippedInSlotsTemplateIds;
		}
		private static void DrainBatteries()
		{
			foreach (Item item in batteryDictionary.Keys)
			{
				if (batteryDictionary[item]) // == true
				{
					if (BatterySystem.headWearItem != null && item.IsChildOf(BatterySystem.headWearItem) && BatterySystem.headWearItem.GetItemComponent<TogglableComponent>().On) //for headwear nvg/t-7
						Mathf.Clamp(BatterySystem.headWearBattery.Value -= 1 / 36f
							* BatterySystemConfig.DrainMultiplier.Value
							* headWearDrainMultiplier[BatterySystem.GetheadWearSight()?.TemplateId], 0, 100);

					else if( item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault() != null )
					{
						Mathf.Clamp(item.GetItemComponentsInChildren<ResourceComponent>().First().Value -= 1 / 72f
							* BatterySystemConfig.DrainMultiplier.Value, 0, 100); //2 hr
					}
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