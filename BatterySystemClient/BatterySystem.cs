using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using HarmonyLib;
using Comfort.Common;
using UnityEngine;
using EFT;
using EFT.InventoryLogic;
using BSG.CameraEffects;
using BatterySystem.Configs;
using System.Threading.Tasks;
using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System;

namespace BatterySystem
{
	public class BatterySystem
	{
		public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("BatterySystem");

		public static Item headWearItem = null;
		private static NightVisionComponent headWearNvg = null;
		private static ThermalVisionComponent headWearThermal = null;
		public static ResourceComponent headWearBattery = null;
		public static bool drainingHeadWearBattery = false;

		public static Item GetheadWearSight()
		{
			if (headWearNvg != null)
				return headWearNvg.Item;
			else if (headWearThermal != null)
				return headWearThermal.Item;
			else
				return null;
		}

		public static void GenerateBatteryDictionary()
		{
			BatterySystemPlugin.batteryDictionary.Clear();

			if (headWearItem != null) // headwear
				BatterySystemPlugin.batteryDictionary.Add(GetheadWearSight(), false);
			if (sightComponent != null) // sight
				BatterySystemPlugin.batteryDictionary.Add(sightComponent.Item, false);
		}

		public static void SetHeadWearComponents()
		{
			headWearItem = PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).Items?.FirstOrDefault(); // default null else headwear
			headWearNvg = headWearItem?.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault(); //default null else nvg item
			headWearThermal = headWearItem?.GetItemComponentsInChildren<ThermalVisionComponent>().FirstOrDefault(); //default null else thermal item
			headWearBattery = headWearItem?.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault(); //default null else resource
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo("At: " + Time.time);
				Logger.LogInfo("headWearItem: " + headWearItem);
				Logger.LogInfo("headWearNVG: " + headWearNvg);
				Logger.LogInfo("headWearThermal: " + headWearThermal);
				Logger.LogInfo("Battery in HeadWear: " + headWearBattery?.Item);
				Logger.LogInfo("Battery Resource: " + headWearBattery);
			}

			GenerateBatteryDictionary();
		}

		public static void CheckHeadWearIfDraining()
		{
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- BATTERYSYSTEM: Checking HeadWear battery at " + Time.time + " ---");

			drainingHeadWearBattery = headWearBattery != null && headWearBattery.Value > 0
				&& (headWearNvg == null
				? headWearThermal.Togglable.On
				: headWearNvg.Togglable.On);
			// headWear has battery with resource installed and headwear (nvg/thermal) isn't switching and is on

			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("hwItem: " + GetheadWearSight());
				Logger.LogInfo("Battery level " + headWearBattery?.Value + ", HeadWear_on: " + drainingHeadWearBattery);
			}
			if (headWearBattery != null && BatterySystemPlugin.batteryDictionary.ContainsKey(GetheadWearSight()))
				BatterySystemPlugin.batteryDictionary[GetheadWearSight()] = drainingHeadWearBattery;


			if (headWearNvg != null)
			{
				PlayerInitPatch.nvgOnField.SetValue(CameraClass.Instance.NightVision, drainingHeadWearBattery);
				PlayerInitPatch.thermalOnField.SetValue(CameraClass.Instance.ThermalVision, false);
			}
			else if (headWearThermal != null)
			{
				PlayerInitPatch.thermalOnField.SetValue(CameraClass.Instance.ThermalVision, drainingHeadWearBattery);
				PlayerInitPatch.nvgOnField.SetValue(CameraClass.Instance.NightVision, false);
			}
		}

		private static SightComponent sightComponent = null;
		public static SightModVisualControllers sightMod = null;
		private static ResourceComponent sightBattery = null;
		public static bool drainingSightBattery = false;
		//public static Dictionary<SightComponent, CollimatorSight> sightDictionary = new Dictionary<SightComponent, CollimatorSight>();

		public static void SetSightComponents(SightModVisualControllers sight)
		{
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- BATTERYSYSTEM: Setting sight components at " + Time.time + " ---");
			//active sight 
			sightMod = sight;
			sightComponent = sightMod?.SightMod.Item.GetItemComponentsInChildren<SightComponent>().FirstOrDefault();
			GenerateBatteryDictionary();
		}

		public static void CheckSightIfDraining()
		{ //foreach sight in database<sight, collimator>: if sight has component with resource then collimator on, else
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- BATTERYSYSTEM: Checking Sight battery at " + Time.time + " ---");

			sightBattery = sightComponent?.Item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault();
			drainingSightBattery = (sightBattery != null && sightBattery.Value > 0);

			if (sightComponent != null && BatterySystemPlugin.batteryDictionary.ContainsKey(sightComponent.Item))
				BatterySystemPlugin.batteryDictionary[sightComponent.Item] = drainingSightBattery;

			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("Sight on: " + drainingSightBattery + " for " + sightComponent.Item);

			if (drainingSightBattery) // visual changes for draining battery
			{

				sightMod.GetComponentsInChildren<CollimatorSight>().FirstOrDefault()?.gameObject.SetActive(true);
			}
			else
			{
				
				sightMod.GetComponentsInChildren<CollimatorSight>().FirstOrDefault()?.gameObject.SetActive(false);
			}

		}
	}
	public class PlayerInitPatch : ModulePatch
	{
		private static FieldInfo inventoryField = null;
		public static FieldInfo nvgOnField = null;
		public static FieldInfo thermalOnField = null;
		public static InventoryControllerClass inventoryController = null;

		protected override MethodBase GetTargetMethod()
		{
			nvgOnField = AccessTools.Field(typeof(NightVision), "_on");
			thermalOnField = AccessTools.Field(typeof(ThermalVision), "On");
			inventoryField = AccessTools.Field(typeof(Player), "_inventoryController");
			return typeof(Player).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		[PatchPostfix]
		private static async void Postfix(Task __result)
		{
			await __result;
			inventoryController = (InventoryControllerClass)inventoryField.GetValue(Singleton<GameWorld>.Instance.MainPlayer); //Player Inventory
			BatterySystem.SetHeadWearComponents();
		}
	}

	public class ApplyItemPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		[PatchPostfix]
		static void Postfix(ref Slot __instance) // limit to only player asap
		{
			if (BatterySystemConfig.EnableMod.Value && BatterySystemPlugin.gameWorld != null)
			{
				if (BatterySystemConfig.EnableLogs.Value)
				{
					Logger.LogInfo("BATTERYSYSTEM: APPLYING CONTAINED ITEM AT: " + Time.time);
					Logger.LogInfo("Slot parent: " + __instance.ParentItem);
					IEnumerable<SightComponent> sightComponents = PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).Items.FirstOrDefault()?.GetItemComponentsInChildren<SightComponent>();
					foreach (SightComponent component in sightComponents)
					{
						Logger.LogInfo("SightComponent: " + component.Item);
					}
				}
				if (BatterySystem.headWearItem != null &&
						__instance.ParentItem.IsChildOf(PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem)) //if item in headwear slot applied
				{
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of HeadWear!");
					//BatterySystemPlugin.headWearCooldown = Time.time + 0.1f;
				}
				else if (__instance.ParentItem.IsChildOf(PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ParentItem)) // installing sight or something
				{
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of WeaponSlots!");
					//BatterySystem.SetSightComponents();
				}
				else if (__instance.ParentItem.IsChildOf(PlayerInitPatch.inventoryController.Inventory.Equipment))
				{
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of Equipment!");
				}
				BatterySystem.SetHeadWearComponents();
			}
		}
	}
	public class SightDevicePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(SightModVisualControllers).GetMethod(nameof(SightModVisualControllers.UpdateSightMode));
		}

		[PatchPostfix]
		static void Postfix(ref SightModVisualControllers __instance)
		{
			if (__instance != BatterySystem.sightMod)
			{
				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("Battery UpdateSightMode at: " + Time.time + " for: " + __instance + __instance.SightMod.Item);

				BatterySystem.SetSightComponents(__instance);
			}
			//foreach (CollimatorSight c in cSight) // always only one collimator sight
			//c.gameObject.SetActive(false); // works!

		}
	}
	public class NvgHeadWearPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(NightVision).GetMethod(nameof(NightVision.StartSwitch));
		}

		[PatchPostfix]
		static void Postfix(ref NightVision __instance)
		{
			if (__instance.name == "FPS Camera" && BatterySystemConfig.EnableMod.Value && BatterySystemPlugin.gameWorld != null)
			// if switching on with no battery or equipping with nvg on, turn off
			{
				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("BATTERYSYSTEM: APPLYING NVG SETTINGS AT: " + Time.time);
				BatterySystem.SetHeadWearComponents();
				BatterySystemPlugin.headWearCooldown = Time.time + 1f;
				//temp workaround kek. have to do a coroutine that triggers after !InProcessSwitching.
			}
		}
	}
	public class ThermalHeadWearPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(ThermalVision).GetMethod(nameof(ThermalVision.StartSwitch));
		}

		[PatchPostfix]
		static void Postfix(ref ThermalVision __instance)
		{
			if (__instance.name == "FPS Camera" && BatterySystemConfig.EnableMod.Value && BatterySystemPlugin.gameWorld != null)
			// if switching on with no battery or equipping with nvg on, turn off
			{
				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("APPLYING THERMAL SETTINGS AT: " + Time.time);
				BatterySystem.SetHeadWearComponents();
				BatterySystemPlugin.headWearCooldown = Time.time + 0.1f;
				//temp workaround kek. have to do a coroutine that triggers after !InProcessSwitching.
			}
		}
	}
}