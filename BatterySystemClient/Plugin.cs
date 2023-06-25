using BepInEx;
using BatterySystem.Configs;
using EFT;
using EFT.InventoryLogic;
using Comfort.Common;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System.Collections;

namespace BatterySystem
{
	//todo: voiceline when adrenaline activates, cooldown?
	[BepInPlugin("com.jiro.batterysystem", "BatterySystem", "1.0.0")]
	public class BatterySystemPlugin : BaseUnityPlugin
	{
		public static GameWorld gameWorld;
		private static float cooldown = 5;
		public static Dictionary<string, Color> nvgDefaultColor = new Dictionary<string, Color>();
		public static Dictionary<string, float> itemDrainMultiplier = new Dictionary<string, float>();
		void Awake()
		{
			BatterySystemConfig.Init(Config);
			new BatterySystemPatch().Enable();
			new NightVisionPatch().Enable();
			//new ForceSwitchPatch().Enable();
			//update dictionary with values
			//foreach (ItemTemplate template in ItemTemplates)
			{
				nvgDefaultColor.Add("5c0696830db834001d23f5da", new Color(0, 255 / 255f, 32 / 255f, 254)); // PNV-10T Night Vision Goggles
				itemDrainMultiplier.Add("5c0696830db834001d23f5da", 1f);

				nvgDefaultColor.Add("5c0558060db834001b735271", new Color(83 / 255f, 255 / 255f, 69 / 255f, 254)); // GPNVG-18 Night Vision goggles
				itemDrainMultiplier.Add("5c0696830db834001d23f5da", 2f);
				nvgDefaultColor.Add("5c066e3a0db834001b7353f0", new Color(0, 255 / 255f, 243 / 255f, 254)); // Armasight N-15 Night Vision Goggles
				itemDrainMultiplier.Add("5c066e3a0db834001b7353f0", 1f);

				nvgDefaultColor.Add("57235b6f24597759bf5a30f1", new Color(183 / 255f, 255 / 255f, 86 / 255f, 254)); // AN/PVS-14 Night Vision Monocular
				itemDrainMultiplier.Add("57235b6f24597759bf5a30f1", 0.5f);

				//nvgDefaultColor.Add("5c110624d174af029e69734c", new Color(0, 255, 32)); // T-7 Thermal Goggles with a Night Vision mount
			}
		}
		void Update()
		{ 
			if (!BatterySystemConfig.EnableMod.Value) return;

			gameWorld = Singleton<GameWorld>.Instance;
			if (gameWorld == null || gameWorld.MainPlayer == null || BatterySystemPatch.batterySlot == null) return;
			if (Time.time > cooldown && BatterySystemPatch.drainingBattery)
			{
				//ResourceComponent resource = (ResourceComponent)AccessTools.Field(typeof(ResourceComponent), "Resource").GetValue(BatterySystemPatch.batterySlot.ContainedItem);
				ResourceComponent resourceComponent = BatterySystemPatch.batterySlot.ContainedItem.GetItemComponent<ResourceComponent>();
				if (resourceComponent.Value > 0 && NightVisionPatch.nightVision.On)
				{
					cooldown = (int)Time.time + 5;
					resourceComponent.Value -= 1 / 7.2f * BatterySystemConfig.DrainMultiplier.Value * itemDrainMultiplier[BatterySystemPatch.batterySlot.ParentItem.TemplateId]; //Default battery lasts 1 hr * configmulti * itemmulti
					BatterySystemPatch.batterySlot.ApplyContainedItem();
				}

				//Logger.LogInfo(gameWorld.MainPlayer.NightVisionObserver.GetItemComponent().Item);
				// EFT.InventoryLogic.NightVisionComponent
			}
			//gamePlayer = gameWorld.MainPlayer;
			//if (gamePlayer == null) return;

			//Item itemInHands = inventoryControllerClass.ItemInHands;
			//battery = BatterySystemPatch.batterySlot.ContainedItem;
			//List<string> equippedTpl = inventoryControllerClass.Inventory.EquippedInSlotsTemplateIds;
			//inventoryControllerClass.ContainedItems;


		}
		/* Credit to Nexus and fontaine for showing me this!
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