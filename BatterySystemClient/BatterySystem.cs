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
using EFT.CameraControl;

namespace BatterySystem
{
	public class BatterySystem
	{
		public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("BatterySystem");

		public static Item headWearItem = null;
		private static NightVisionComponent _headWearNvg = null;
		private static ThermalVisionComponent _headWearThermal = null;
		private static bool _drainingHeadWearBattery = false;
		public static ResourceComponent headWearBattery = null;

		public static Dictionary<SightModVisualControllers, ResourceComponent> sightMods = new Dictionary<SightModVisualControllers, ResourceComponent>();
		private static bool _drainingSightBattery = false;


		public static Item GetheadWearSight() // returns the special device goggles that are equipped
		{
			if (_headWearNvg != null)
				return _headWearNvg.Item;
			else if (_headWearThermal != null)
				return _headWearThermal.Item;
			else
				return null;
		}

		public static bool IsInSlot(Item item, Slot slot)
		{
			if (item != null && slot?.ContainedItem != null && item.IsChildOf(slot.ContainedItem)) return true;
			else return false;
		}

		public static void GenerateBatteryDictionary()
		{
			BatterySystemPlugin.batteryDictionary.Clear();

			if (GetheadWearSight() != null) // headwear
				BatterySystemPlugin.batteryDictionary.Add(GetheadWearSight(), false);

			foreach (SightModVisualControllers sightController in sightMods.Keys)
			{
				//only drain sights that are on equipped weapon
				if (IsInSlot(sightController.SightMod.Item, BatterySystemPlugin.gameWorld?.MainPlayer.ActiveSlot)
					&& !BatterySystemPlugin.batteryDictionary.ContainsKey(sightController.SightMod.Item))
					BatterySystemPlugin.batteryDictionary.Add(sightController.SightMod.Item, false);
			}

			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Generated battery dictionary: ---");
				foreach (Item i in BatterySystemPlugin.batteryDictionary.Keys)
					Logger.LogInfo(i);
				Logger.LogInfo("---------------------------------------------");
			}
		}

		public static void SetHeadWearComponents()
		{
			headWearItem = PlayerInitPatch.headWearSlot.Items?.FirstOrDefault(); // default null else headwear
			_headWearNvg = headWearItem?.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault(); //default null else nvg item
			_headWearThermal = headWearItem?.GetItemComponentsInChildren<ThermalVisionComponent>().FirstOrDefault(); //default null else thermal item
			headWearBattery = GetheadWearSight()?.Parent.Item.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault(); //default null else resource

			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: SetHeadWearComponents: " + Time.time + " ---");
				Logger.LogInfo("At: " + Time.time);
				Logger.LogInfo("headWearItem: " + headWearItem);
				Logger.LogInfo("headWearNVG: " + _headWearNvg);
				Logger.LogInfo("headWearParent: " + GetheadWearSight()?.Parent.Item);
				Logger.LogInfo("headWearThermal: " + _headWearThermal);
				Logger.LogInfo("Battery in HeadWear: " + headWearBattery?.Item);
				Logger.LogInfo("Battery Resource: " + headWearBattery);
			}

			GenerateBatteryDictionary();
		}

		public static void CheckHeadWearIfDraining()
		{
			_drainingHeadWearBattery = headWearBattery != null && headWearBattery.Value > 0
				&& (_headWearNvg == null && _headWearThermal != null
				? (_headWearThermal.Togglable.On && !CameraClass.Instance.ThermalVision.InProcessSwitching)
				: (_headWearNvg != null && _headWearThermal == null ? _headWearNvg.Togglable.On && !CameraClass.Instance.NightVision.InProcessSwitching
				: false));
			// headWear has battery with resource installed and headwear (nvg/thermal) isn't switching and is on

			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Checking HeadWear battery: " + Time.time + " ---");
				Logger.LogInfo("hwItem: " + GetheadWearSight());
				Logger.LogInfo("Battery level " + headWearBattery?.Value + ", HeadWear_on: " + _drainingHeadWearBattery);
				Logger.LogInfo("---------------------------------------------");
			}
			if (headWearBattery != null && BatterySystemPlugin.batteryDictionary.ContainsKey(GetheadWearSight()))
				BatterySystemPlugin.batteryDictionary[GetheadWearSight()] = _drainingHeadWearBattery;

			if (_headWearNvg != null)
			{
				PlayerInitPatch.nvgOnField.SetValue(CameraClass.Instance.NightVision, _drainingHeadWearBattery);
				PlayerInitPatch.thermalOnField.SetValue(CameraClass.Instance.ThermalVision, false);
			}
			else if (_headWearThermal != null)
			{
				PlayerInitPatch.nvgOnField.SetValue(CameraClass.Instance.NightVision, false);
				PlayerInitPatch.thermalOnField.SetValue(CameraClass.Instance.ThermalVision, _drainingHeadWearBattery);
			}
		}

		public static void SetSightComponents(SightModVisualControllers sightInstance)
		{
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Setting sight components at " + Time.time + " ---");
				Logger.LogInfo("For: " + sightInstance.SightMod.Item);
			}
			//before applying new sights, remove sights that are not on equipped weapon
			for (int i = sightMods.Keys.Count - 1; i >= 0; i--)
			{
				SightModVisualControllers key = sightMods.Keys.ElementAt(i);
				if (!IsInSlot(key.SightMod.Item, BatterySystemPlugin.gameWorld.MainPlayer.ActiveSlot))
				{
					sightMods.Remove(key);
				}
			}

			if (IsInSlot(sightInstance.SightMod.Item, BatterySystemPlugin.gameWorld.MainPlayer.ActiveSlot))
			{
				if (BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("Sight Found: " + sightInstance.SightMod.Item);
				// if sight is already in dictionary, dont add it
				if (!sightMods.Keys.Any(key => key.SightMod.Item == sightInstance.SightMod.Item)
					&& (sightInstance.gameObject.GetComponentsInChildren<CollimatorSight>(true).FirstOrDefault() != null
					|| sightInstance.SightMod.Item.TemplateId == "5a1eaa87fcdbcb001865f75e")) //reap-ir
				{
					sightMods.Add(sightInstance, sightInstance.SightMod.Item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault());
				}
			}
			GenerateBatteryDictionary();
		}
		//foreach sight in database<sight, collimator>: if sight has component with resource then collimator on, else
		public static void CheckSightIfDraining(SightComponent sight = null)
		{
			//ERROR:  If reap-ir is on and using canted collimator, enabled optic sight removes collimator effect. find a way to only drain active sight!
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: CHECK Sight battery at " + Time.time + " ---");
				if (sight != null)
					Logger.LogInfo("Sight From Subscription: " + sight);
			}
			//for because modifying sightMods[key]
			for (int i = 0; i < sightMods.Keys.Count; i++) 
			{ 
				SightModVisualControllers key = sightMods.Keys.ElementAt(i);
				if (key?.SightMod?.Item != null)
				{
					//sightmodvisualcontroller[scope_all_eotech_exps3(Clone)] = SightMod.sightComponent_0
					sightMods[key] = key.SightMod.Item.GetItemComponentsInChildren<ResourceComponent>().FirstOrDefault();
					_drainingSightBattery = (sightMods[key] != null && sightMods[key].Value > 0
						&& IsInSlot(key.SightMod.Item, BatterySystemPlugin.gameWorld?.MainPlayer.ActiveSlot));

					if (BatterySystemPlugin.batteryDictionary.ContainsKey(key.SightMod.Item))
						BatterySystemPlugin.batteryDictionary[key.SightMod.Item] = _drainingSightBattery;

					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Sight on: " + _drainingSightBattery + " for " + key + key.SightMod.Item);
					// true for finding inactive reticles
					foreach (CollimatorSight col in key.gameObject.GetComponentsInChildren<CollimatorSight>(true))
					{
						col.gameObject.SetActive(_drainingSightBattery);
					}
					foreach (OpticSight optic in key.gameObject.GetComponentsInChildren<OpticSight>(true))
					{
						optic.enabled = _drainingSightBattery;
					}
				}
			}
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("---------------------------------------------");
		}
	}

	internal class GameStartPatch : ModulePatch
	{
		private static FieldInfo _inventoryBotField;
		private static IBotGame _botGame;
		protected override MethodBase GetTargetMethod()
		{
			_inventoryBotField = AccessTools.Field(typeof(Player), "_inventoryController");
			return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
		}
		[PatchPrefix]
		public static void PatchPrefix()
		{
			// Sub to Event to get and add Bot when they spawn, credit to DrakiaXYZ!
			//unsubscribe so no duplicates
			_botGame = Singleton<IBotGame>.Instance;
			_botGame.BotsController.BotSpawner.OnBotCreated -= owner => DrainSpawnedBattery(owner);
			_botGame.BotsController.BotSpawner.OnBotCreated += owner => DrainSpawnedBattery(owner);
			//Singleton<Player>.Instance.OnSightChangedEvent -= sight => BatterySystem.CheckSightIfDraining(sight);
			//Singleton<Player>.Instance.OnSightChangedEvent += sight => BatterySystem.CheckSightIfDraining(sight);
		}

		private static void DrainSpawnedBattery(BotOwner owner)
		{
			Logger.LogInfo("DrainSpawnedBattery on Bot: " + owner?.GetPlayer + " at " + Time.time);
			InventoryControllerClass botInventory = (InventoryControllerClass)_inventoryBotField.GetValue(owner.GetPlayer);

			foreach (Item item in botInventory.EquipmentItems)
			{
				foreach (ResourceComponent resource in item.GetItemComponentsInChildren<ResourceComponent>())
				{
					Logger.LogInfo("Trying to drain" + resource.Item);
					resource.Value = Random.Range(
						Mathf.Min(BatterySystemConfig.SpawnDurabilityMin.Value, BatterySystemConfig.SpawnDurabilityMax.Value),
						Mathf.Max(BatterySystemConfig.SpawnDurabilityMin.Value, BatterySystemConfig.SpawnDurabilityMax.Value));

					if (BatterySystemConfig.EnableLogs.Value)
					{
						Logger.LogInfo("BATTERYSYSTEM: OnBotCreated: " + Time.time);
						Logger.LogInfo("Checking item from slot: " + item);
						Logger.LogInfo("Res value: " + resource.Value);
					}
				}
			}
		}
	}
	public class PlayerInitPatch : ModulePatch
	{
		private static FieldInfo _inventoryField = null;
		private static InventoryControllerClass _inventoryController = null;
		public static FieldInfo nvgOnField = null;
		public static FieldInfo thermalOnField = null;
		public static Slot headWearSlot = null;

		protected override MethodBase GetTargetMethod()
		{
			_inventoryField = AccessTools.Field(typeof(Player), "_inventoryController");
			nvgOnField = AccessTools.Field(typeof(NightVision), "_on");
			thermalOnField = AccessTools.Field(typeof(ThermalVision), "On");
			return typeof(Player).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		[PatchPostfix]
		private static async void Postfix(Player __instance, Task __result)
		{
			await __result;

			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("PlayerInitPatch AT " + Time.time + ", IsYourPlayer: " + __instance.IsYourPlayer + ", IsAI: " + __instance.IsAI);

			if (__instance.IsYourPlayer)
			{
				BatterySystem.sightMods.Clear(); // remove old sight entries that were saved from previous raid
				_inventoryController = (InventoryControllerClass)_inventoryField.GetValue(__instance); //Player Inventory
				headWearSlot = _inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear);
			}
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
				if (BatterySystem.IsInSlot(PlayerInitPatch.headWearSlot.ContainedItem, PlayerInitPatch.headWearSlot))
				{ //if item in headwear slot applied
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of HeadWear!");
					BatterySystem.SetHeadWearComponents();
				}
				else if (BatterySystem.IsInSlot(__instance.ContainedItem, BatterySystemPlugin.gameWorld?.MainPlayer.ActiveSlot))
				{ // if sight is removed and empty slot is applied, then remove the sight from sightdb
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of ActiveSlot!");
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
			if (__instance != null && BatterySystemConfig.EnableMod.Value && BatterySystemPlugin.gameWorld != null && BatterySystem.IsInSlot(__instance.SightMod.Item, BatterySystemPlugin.gameWorld?.MainPlayer.ActiveSlot))
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
				BatterySystem.SetHeadWearComponents();
				//temp workaround kek. have to do a coroutine that triggers after !InProcessSwitching.
			}
		}
	}
}