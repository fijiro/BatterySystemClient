using BepInEx;
using System.Collections.Generic;
using Comfort.Common;
using UnityEngine;
using EFT;
using HarmonyLib;
using BatterySystem.Configs;
using EFT.InventoryLogic;
using System.Linq;
using System.Reflection;
using EFT.UI;
using EFT.UI.WeaponModding;

namespace BatterySystem
{
	/*TODO: 
	 * Enable switching to iron sights when battery runs out
	 * equipping and removing headwear gives infinite nvg
	 * switch to coroutines
	 * flir does not require batteries, make recharge craft
	 * Sound when toggling battery runs out or is removed or added
	 * battery recharger - idea by Props
	 */
	[BepInPlugin("com.jiro.batterysystem", "BatterySystem", "1.2.1")]
	[BepInDependency("com.spt-aki.core", "3.5.8")]
	public class BatterySystemPlugin : BaseUnityPlugin
	{
		public static GameWorld gameWorld;
		private static float _mainCooldown = 1f;
		private static Dictionary<string, float> _headWearDrainMultiplier = new Dictionary<string, float>();
		public static Dictionary<Item, bool> batteryDictionary = new Dictionary<Item, bool>();
		//resource drain all batteries that are on // using dictionary to help and sync draining batteries
		void Awake()
		{
			BatterySystemConfig.Init(Config);
			new GameStartPatch().Enable();
			new PlayerInitPatch().Enable();
			new ModdingScreenPatch().Enable();
			new ApplyItemPatch().Enable();
			new SightDevicePatch().Enable();
			new NvgHeadWearPatch().Enable();
			new ThermalHeadWearPatch().Enable();
			//update dictionary with values
			//foreach (ItemTemplate template in ItemTemplates)
			{
				_headWearDrainMultiplier.Add("5c0696830db834001d23f5da", 1f); // PNV-10T Night Vision Goggles, AA Battery
				_headWearDrainMultiplier.Add("5c0558060db834001b735271", 2f); // GPNVG-18 Night Vision goggles, CR123 battery pack
				_headWearDrainMultiplier.Add("5c066e3a0db834001b7353f0", 1f); // Armasight N-15 Night Vision Goggles, single CR123A lithium battery
				_headWearDrainMultiplier.Add("57235b6f24597759bf5a30f1", 0.5f); // AN/PVS-14 Night Vision Monocular, AA Battery
				_headWearDrainMultiplier.Add("5c110624d174af029e69734c", 3f); // T-7 Thermal Goggles with a Night Vision mount, Double AA
			}
		}

		void Update() // battery is drained in Update() and applied
		{
			if (Time.time > _mainCooldown && BatterySystemConfig.EnableMod.Value)
			{
				_mainCooldown = Time.time + 1f;
				gameWorld = Singleton<GameWorld>.Instance;
				
				//Singleton<CommonUI>.Instance.EditBuildScreen.gameObject.GetComponentInChildren<ModdingScreenSlotView>(); // UI way
				if (gameWorld == null || gameWorld.MainPlayer == null || !gameWorld.MainPlayer.HealthController.IsAlive) return;

				BatterySystem.CheckHeadWearIfDraining();
				BatterySystem.CheckSightIfDraining();
				DrainBatteries();
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
					if (BatterySystem.headWearBattery != null && item.IsChildOf(BatterySystem.headWearItem)
						&& BatterySystem.headWearItem.GetItemComponentsInChildren<TogglableComponent>().FirstOrDefault()?.On == true)
					//for headwear nvg/t-7
					{
						Mathf.Clamp(BatterySystem.headWearBattery.Value -= 1 / 36f
								* BatterySystemConfig.DrainMultiplier.Value
								* _headWearDrainMultiplier[BatterySystem.GetheadWearSight()?.TemplateId], 0, 100);
					}
					else if (item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault() != null) //for sights
					{
						Mathf.Clamp(item.GetItemComponentsInChildren<ResourceComponent>().First().Value -= 1 / 100f
							* BatterySystemConfig.DrainMultiplier.Value, 0, 100); //2 hr
					}
					//Default battery lasts 1 hr * configmulti * itemmulti, itemmulti was Hazelify's idea!
				}
			}
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