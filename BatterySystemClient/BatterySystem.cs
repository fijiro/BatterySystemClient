﻿using System.Linq;
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
using System.Collections.Generic;

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

		public static bool isInSlot(Item item, List<Slot> slots)
		{
			foreach (Slot slot in slots)
			{
				if (item != null && slot.ContainedItem != null && item.IsChildOf(slot.ContainedItem)) return true;
			}
			return false;
		}

		public static void GenerateBatteryDictionary()
		{
			BatterySystemPlugin.batteryDictionary.Clear();

			if (GetheadWearSight() != null) // headwear
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
			headWearItem = PlayerInitPatch.headWearSlot.Items?.FirstOrDefault(); // default null else headwear
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
				&& (headWearNvg == null && headWearThermal != null
				? (headWearThermal.Togglable.On && !CameraClass.Instance.ThermalVision.InProcessSwitching)
				: (headWearNvg != null && headWearThermal == null ? headWearNvg.Togglable.On && !CameraClass.Instance.NightVision.InProcessSwitching
				: false));
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
				PlayerInitPatch.nvgOnField.SetValue(CameraClass.Instance.NightVision, false);
				PlayerInitPatch.thermalOnField.SetValue(CameraClass.Instance.ThermalVision, drainingHeadWearBattery);
			}
		}

		public static Dictionary<SightModVisualControllers, ResourceComponent> sightMods = new Dictionary<SightModVisualControllers, ResourceComponent>();
		private static bool drainingSightBattery = false;

		public static void SetSightComponents(SightModVisualControllers sightInstance)
		{
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Setting sight components at " + Time.time + " ---");
				Logger.LogInfo(sightInstance);
			}

			if (isInSlot(sightInstance.SightMod.Item, PlayerInitPatch.weaponSlotsList))
			{
				// if sight is already in dictionary, dont add it. if sight is unequipped, remove it
				if (!sightMods.ContainsKey(sightInstance) && sightInstance.gameObject.GetComponentsInChildren<CollimatorSight>(true).FirstOrDefault() != null)
				{ 
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("sightinstance is child of weaponSlot, adding to database " + sightInstance.SightMod.Item);
					sightMods.Add(sightInstance, sightInstance.SightMod.Item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault());
					GenerateBatteryDictionary();
				}
			}

			// if sight is unequipped, then remove it from being drained.
			else if (sightMods.ContainsKey(sightInstance))
			{ 
				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("sightinstance is not child of weaponSlot, removing");
				sightMods.Remove(sightInstance);
				GenerateBatteryDictionary();
			}
		}
		//foreach sight in database<sight, collimator>: if sight has component with resource then collimator on, else
		public static void CheckSightIfDraining()
		{ 
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- BATTERYSYSTEM: CHECK Sight battery at " + Time.time + " ---");

			SightModVisualControllers key = null;
			for (int i = 0; i < sightMods.Keys.Count; i++)
			{
				key = sightMods.Keys.ElementAt(i);
				if (key?.SightMod?.Item != null)
				{
					//sightmodvisualcontroller[scope_all_eotech_exps3(Clone)] = SightMod.sightComponent_0
					sightMods[key] = key.SightMod.Item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault();
					drainingSightBattery = (sightMods[key] != null && sightMods[key].Value > 0
						&& isInSlot(key.SightMod.Item, new List<Slot> { BatterySystemPlugin.gameWorld.MainPlayer.ActiveSlot }));

					if (BatterySystemPlugin.batteryDictionary.ContainsKey(key.SightMod.Item))
						BatterySystemPlugin.batteryDictionary[key.SightMod.Item] = drainingSightBattery;

					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Sight on: " + drainingSightBattery + " for " + key);
					// true for finding inactive reticles
					foreach (CollimatorSight col in key.gameObject.GetComponentsInChildren<CollimatorSight>(true)) 
					{
						if (BatterySystemConfig.EnableLogs.Value)
							Logger.LogInfo("Collimator in sightMod: " + col.gameObject);

						if (drainingSightBattery)
							col.gameObject.SetActive(true);
						else
							col.gameObject.SetActive(false);
					}
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
		public static Slot firstPrimaryWeaponSlot = null;
		public static Slot secondPrimaryWeaponSlot = null;
		public static Slot holsterSlot = null;
		public static Slot headWearSlot = null;
		public static List<Slot> weaponSlotsList = null;

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
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("PlayerInitPatch AT " + Time.time);
			BatterySystem.sightMods.Clear(); // remove old sight entries that were saved from previous raid
			inventoryController = (InventoryControllerClass)inventoryField.GetValue(Singleton<GameWorld>.Instance.MainPlayer); //Player Inventory
			firstPrimaryWeaponSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon);
			secondPrimaryWeaponSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon);
			holsterSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Holster);
			headWearSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear);
			weaponSlotsList = new List<Slot> { firstPrimaryWeaponSlot, secondPrimaryWeaponSlot, holsterSlot };
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
				if (PlayerInitPatch.headWearSlot.ContainedItem != null && __instance.ParentItem.IsChildOf(PlayerInitPatch.headWearSlot.ContainedItem))
				{ //if item in headwear slot applied
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of HeadWear!");
					BatterySystem.SetHeadWearComponents();
				}
				else if (BatterySystem.isInSlot(__instance.ContainedItem, PlayerInitPatch.weaponSlotsList))
				{ // if sight is removed and empty slot is applied, then remove the sight from sightdb
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
			if (__instance != null && BatterySystemConfig.EnableLogs.Value && BatterySystemPlugin.gameWorld != null)
			{
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
			{
				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("BATTERYSYSTEM: APPLYING NVG SETTINGS AT: " + Time.time);
				BatterySystem.SetHeadWearComponents();
				//temp workaround kek. have to do a coroutine that triggers after InProcessSwitching
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
					Logger.LogInfo("BATTERYSYSTEM: APPLYING THERMAL SETTINGS AT: " + Time.time);
				BatterySystem.SetHeadWearComponents();
				//temp workaround kek. have to do a coroutine that triggers after !InProcessSwitching.
			}
		}
	}
}