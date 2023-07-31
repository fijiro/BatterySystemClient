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
	[BepInPlugin("com.jiro.batterysystem", "BatterySystem", "1.3.0")]
	[BepInDependency("com.spt-aki.core", "3.5.8")]
	public class BatterySystemPlugin : BaseUnityPlugin
	{
		private static float _mainCooldown = 1f;
		private static Dictionary<string, float> _headWearDrainMultiplier = new Dictionary<string, float>();
		public static Dictionary<Item, bool> batteryDictionary = new Dictionary<Item, bool>();
		private static ResourceComponent res;
		//resource drain all batteries that are on // using dictionary to help and sync draining batteries
		public void Awake()
		{
			BatterySystemConfig.Init(Config);
			if (BatterySystemConfig.EnableMod.Value)
			{
				new PlayerInitPatch().Enable();
				new GetBoneForSlotPatch().Enable();
				new UpdatePhonesPatch().Enable();
				new ApplyItemPatch().Enable();
				new SightDevicePatch().Enable();
				new NvgHeadWearPatch().Enable();
				new ThermalHeadWearPatch().Enable();
				//foreach (ItemTemplate template in ItemTemplates) if(template has batteryslot)
				{
					_headWearDrainMultiplier.Add("5c0696830db834001d23f5da", 1f); // PNV-10T Night Vision Goggles, AA Battery
					_headWearDrainMultiplier.Add("5c0558060db834001b735271", 2f); // GPNVG-18 Night Vision goggles, CR123 battery pack
					_headWearDrainMultiplier.Add("5c066e3a0db834001b7353f0", 1f); // Armasight N-15 Night Vision Goggles, single CR123A lithium battery
					_headWearDrainMultiplier.Add("57235b6f24597759bf5a30f1", 0.5f); // AN/PVS-14 Night Vision Monocular, AA Battery
					_headWearDrainMultiplier.Add("5c110624d174af029e69734c", 3f); // T-7 Thermal Goggles with a Night Vision mount, Double AA
				}
			}
		}

		public void Update() // battery is drained in Update() and applied
		{
			if (Time.time > _mainCooldown && BatterySystemConfig.EnableMod.Value)
			{
				_mainCooldown = Time.time + 1f;

				//Singleton<CommonUI>.Instance.EditBuildScreen.gameObject.GetComponentInChildren<ModdingScreenSlotView>(); // UI way
				if (Singleton<GameWorld>.Instance?.MainPlayer == null || Singleton<GameWorld>.Instance.MainPlayer is HideoutPlayer || !Singleton<GameWorld>.Instance.MainPlayer.HealthController.IsAlive) return;
				BatterySystem.CheckHeadWearIfDraining();
				BatterySystem.CheckSightIfDraining();
				DrainBatteries();
			}
		}

		private static void DrainBatteries()
		{
			foreach (Item item in batteryDictionary.Keys)
			{
				if (batteryDictionary[item]) // == true
				{
					BatterySystem.Logger.LogInfo("Check drain item: " + item);
					//Default battery lasts 1 hr * configmulti * itemmulti, itemmulti was Hazelify's idea!
					if (BatterySystem.headWearBattery != null && item.IsChildOf(BatterySystem.headWearItem) //for headwear nvg/t-7
						&& BatterySystem.headWearItem.GetItemComponentsInChildren<TogglableComponent>().FirstOrDefault()?.On == true)
					
					{
						BatterySystem.headWearBattery.Value -= 1 / 36f
								* BatterySystemConfig.DrainMultiplier.Value
								* _headWearDrainMultiplier[BatterySystem.GetheadWearSight()?.TemplateId];
					}
					else if (item.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault() != null) //for sights + earpiece
					{
						BatterySystem.Logger.LogInfo("Draining item resource: " + item.GetItemComponentsInChildren<ResourceComponent>(false).First().Item);
						item.GetItemComponentsInChildren<ResourceComponent>(false).First().Value -= 1 / 100f
							* BatterySystemConfig.DrainMultiplier.Value; //2 hr
					}
					if(item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault()?.Value < 0)
					{
						BatterySystem.CheckEarPieceIfDraining();
						item.GetItemComponentsInChildren<ResourceComponent>().First().Value = 0f;
					}
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