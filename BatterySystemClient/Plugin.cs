using BepInEx;
using BatterySystem.Configs;
using EFT;
using EFT.InventoryLogic;
using Comfort.Common;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System.Collections;
using System;

namespace BatterySystem
{
	//todo: voiceline when adrenaline activates, cooldown?
	[BepInPlugin("com.jiro.batterysystem", "BatterySystem", "1.0.0")]
	public class BatterySystemPlugin : BaseUnityPlugin
	{
		public static GameWorld gameWorld;
		public static Player gamePlayer;
		private static Item battery;
		private static int cooldown = 5;

		void Awake()
		{
			BatterySystemConfig.Init(Config);
			new BatterySystemPatch().Enable();
			new NightVisionPatch().Enable();
			new ForceSwitchPatch().Enable();
			new NightVisionSwitchPatch().Enable();
		}

		void Update()
		{
			gameWorld = Singleton<GameWorld>.Instance;
			if (gameWorld == null || gameWorld.MainPlayer == null) return;
			//if (gameWorld == null || gameWorld.MainPlayer == null) return;
			if (Time.time > cooldown)
			{
				cooldown = (int)Time.time + 1;

				if (BatterySystemPatch.drainBattery)
				{
					//BatterySystemPatch.batterySlot.ApplyContainedItem();
					//ResourceComponent resource = (ResourceComponent)AccessTools.Field(typeof(ResourceComponent), "Resource").GetValue(BatterySystemPatch.batterySlot.ContainedItem);
					 ResourceComponent resourceComponent = BatterySystemPatch.batterySlot.ContainedItem.GetItemComponent<ResourceComponent>();

					if (resourceComponent != null)
					{

						resourceComponent.Value -= 5;
						Logger.LogInfo(resourceComponent);
						Logger.LogInfo(resourceComponent.Value);
						Logger.LogInfo(resourceComponent.MaxResource);
					}
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
			/*
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
}