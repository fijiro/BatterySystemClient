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
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Assertions.Must;

namespace BatterySystem
{
	public class BatterySystem
	{
		public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("BatterySystem");

		public static Item headWearItem = null;
		private static NightVisionComponent headWearNvg = null;
		private static ThermalVisionComponent headWearThermal = null;
		private static bool drainingHeadWearBattery = false;
		public static ResourceComponent headWearBattery = null;

		public static Item GetheadWearSight() // returns the special device goggles that are equipped
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

			foreach (SightModVisualControllers sightController in sightMods.Keys) //sights
			{
				if (sightController.SightMod.Item != null && !BatterySystemPlugin.batteryDictionary.ContainsKey(sightController.SightMod.Item))
					BatterySystemPlugin.batteryDictionary.Add(sightController.SightMod.Item, false);
			}
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Generated battery dictionary: ---");
				foreach (Item i in BatterySystemPlugin.batteryDictionary.Keys)
				{
					Logger.LogInfo(i);
				}
			}
		}

		public static void SetHeadWearComponents()
		{
			headWearItem = PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).Items?.FirstOrDefault(); // default null else headwear
			headWearNvg = headWearItem?.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault(); //default null else nvg item
			headWearThermal = headWearItem?.GetItemComponentsInChildren<ThermalVisionComponent>().FirstOrDefault(); //default null else thermal item
			headWearBattery = GetheadWearSight()?.Parent.Item.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault(); //default null else resource
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: SetHeadWearComponents: ---");
				Logger.LogInfo("At: " + Time.time);
				Logger.LogInfo("headWearItem: " + headWearItem);
				Logger.LogInfo("headWearNVG: " + headWearNvg);
				Logger.LogInfo("headWearParent: " + GetheadWearSight()?.Parent.Item);
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
				? (headWearThermal.Togglable.On && !CameraClass.Instance.ThermalVision.InProcessSwitching)
				: (headWearNvg.Togglable.On && !CameraClass.Instance.NightVision.InProcessSwitching));
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


		private static Dictionary<SightModVisualControllers, ResourceComponent> sightMods = new Dictionary<SightModVisualControllers, ResourceComponent>();
		private static bool drainingSightBattery = false;

		public static void SetSightComponents(SightModVisualControllers sightInstance)
		{
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Setting sight components at " + Time.time + " ---");
				Logger.LogInfo(sightInstance);
			}
			if (PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem != null && sightInstance.SightMod.Item.IsChildOf(PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem))
			{
				if (!sightMods.ContainsKey(sightInstance))
				{ // if sight is already in dictionary, dont add it. if sight is unequipped, remove it
					Logger.LogInfo("sightinstance is child of weaponSlot, adding to database " + sightInstance.SightMod.Item);
					sightMods.Add(sightInstance, sightInstance.SightMod.Item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault());
					GenerateBatteryDictionary();
				}
			}
			else if (sightMods.ContainsKey(sightInstance))
			{ // if sight is unequipped, then remove it from being drained.
				Logger.LogInfo("sightinstance is not child of weaponSlot, removing");
				sightMods.Remove(sightInstance);
				GenerateBatteryDictionary();
			}
		}

		public static void CheckSightIfDraining()
		{ //foreach sight in database<sight, collimator>: if sight has component with resource then collimator on, else
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- BATTERYSYSTEM: CHECK Sight battery at " + Time.time + " ---");

			SightModVisualControllers key = null;
			for (int i = 0; i < sightMods.Keys.Count; i++) 
			{
				key = sightMods.Keys.ElementAt(i);
				//sightmodvisualcontroller[scope_all_eotech_exps3(Clone)] = this.sightComponent_0
				sightMods[key] = key.SightMod.Item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault();
				drainingSightBattery = (sightMods[key] != null && sightMods[key].Value > 0);

				if (key.SightMod.Item != null && BatterySystemPlugin.batteryDictionary.ContainsKey(key.SightMod.Item))
					BatterySystemPlugin.batteryDictionary[key.SightMod.Item] = drainingSightBattery; 

				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("Sight on: " + drainingSightBattery + " for " + key);

				foreach (CollimatorSight col in key.gameObject.GetComponentsInChildren<CollimatorSight>(true)) // true for finding inactive reticles
				{
					Logger.LogInfo("Collimator in sightMod: " + col.gameObject);
					if (drainingSightBattery) 
						col.gameObject.SetActive(true); 
					else
						col.gameObject.SetActive(false);
				}
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
				}
				if (PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem != null && __instance.ParentItem.IsChildOf(PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem)) //if item in headwear slot applied
				{
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of HeadWear!");
					BatterySystem.SetHeadWearComponents();
				}
				else if (PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem != null && __instance.ParentItem.IsChildOf(PlayerInitPatch.inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem))
				{// if sight is removed and empty slot is applied, then remove the sight from sightdb
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of PrimarySlot!");
				}
				else if (__instance.ParentItem.IsChildOf(PlayerInitPatch.inventoryController.Inventory.Equipment))
				{
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of Equipment!");
				}
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
			if (BatterySystemConfig.EnableLogs.Value && BatterySystemPlugin.gameWorld != null && __instance != null)
			{
				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("--- BATTERYSYSTEM: UpdateSightMode at: " + Time.time + " for: " + __instance.SightMod.Item + " ---");

				BatterySystem.SetSightComponents(__instance);
			}
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
				//temp workaround kek. have to do a coroutine that triggers after !InProcessSwitching.
			}
		}
	}
}