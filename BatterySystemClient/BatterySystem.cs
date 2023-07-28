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
using System.Collections.Generic;
using EFT.CameraControl;
using EFT.UI;
using System;
using EFT.UI.Screens;
using EFT.InventoryLogic.BackendInventoryInteraction;
using System.Net;
using EFT.DataProviding;
using UnityEngine.Audio;

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

		private static Item _earPieceItem = null;
		private static ResourceComponent _earPieceBattery = null;
		private static bool _drainingEarPieceBattery = true;
		public static float compressorDistortion;
		public static float compressorMakeup;
		public static float compressor;

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

			if (_earPieceBattery != null) // earpiece
				BatterySystemPlugin.batteryDictionary.Add(_earPieceItem, false);

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

		public static void SetEarPieceComponents()
		{
			_earPieceItem = PlayerInitPatch.GetEquipmentSlot(EquipmentSlot.Earpiece).Items?.FirstOrDefault();
			_earPieceBattery = _earPieceItem?.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault();
			_drainingEarPieceBattery = true;
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Setting EarPiece components at: " + Time.time + " ---");
				Logger.LogInfo("headWearItem: " + _earPieceItem);
				Logger.LogInfo("Battery in Earpiece: " + _earPieceBattery?.Item);
				Logger.LogInfo("Battery Resource: " + _earPieceBattery);
			}
			GenerateBatteryDictionary();
			CheckEarPieceIfDraining();
		}

		public static void CheckEarPieceIfDraining()
		{
			if (_earPieceBattery != null && _earPieceBattery.Value > 0)
			{
				Logger.LogInfo("Turning Headset on! ");
				Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", compressorMakeup);
				Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", compressor);
				Singleton<BetterAudio>.Instance.Master.SetFloat("MainVolume", 0f);
				_drainingEarPieceBattery = true;
			}
			else if (_earPieceItem != null)
			{
				Logger.LogInfo("Turning Headset off! ");
				Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", 0f);
				Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", -30f);
				Singleton<BetterAudio>.Instance.Master.SetFloat("MainVolume", -30f);

				_drainingEarPieceBattery = false;
			}
			
			if (_earPieceBattery != null && BatterySystemPlugin.batteryDictionary.ContainsKey(_earPieceItem))
				BatterySystemPlugin.batteryDictionary[_earPieceItem] = _drainingEarPieceBattery;

			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Checking EarPiece battery: " + Time.time + " ---");
				Logger.LogInfo("EarPiece: " + _earPieceItem);
				Logger.LogInfo("Battery level " + _earPieceBattery?.Value + ", Is draining: " + _drainingEarPieceBattery);
				Logger.LogInfo("---------------------------------------------");
			}
		}

		public static void SetHeadWearComponents()
		{
			headWearItem = PlayerInitPatch.GetEquipmentSlot(EquipmentSlot.Headwear).Items?.FirstOrDefault(); // default null else headwear
			_headWearNvg = headWearItem?.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault(); //default null else nvg item
			_headWearThermal = headWearItem?.GetItemComponentsInChildren<ThermalVisionComponent>().FirstOrDefault(); //default null else thermal item
			headWearBattery = GetheadWearSight()?.Parent.Item.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault(); //default null else resource

			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM: Setting HeadWear components at: " + Time.time + " ---");
				Logger.LogInfo("headWearItem: " + headWearItem);
				Logger.LogInfo("headWearNVG: " + _headWearNvg);
				Logger.LogInfo("headWearParent: " + GetheadWearSight()?.Parent.Item);
				Logger.LogInfo("headWearThermal: " + _headWearThermal);
				Logger.LogInfo("Battery in HeadWear: " + headWearBattery?.Item);
				Logger.LogInfo("Battery Resource: " + headWearBattery);
			}
			GenerateBatteryDictionary();
			CheckHeadWearIfDraining();
		}

		public static void CheckHeadWearIfDraining()
		{
			_drainingHeadWearBattery = headWearBattery != null && headWearBattery.Value > 0
				&& (_headWearNvg == null && _headWearThermal != null
				? (_headWearThermal.Togglable.On && !CameraClass.Instance.ThermalVision.InProcessSwitching)
				: (_headWearNvg != null && _headWearThermal == null && _headWearNvg.Togglable.On && !CameraClass.Instance.NightVision.InProcessSwitching));
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
				PlayerInitPatch.nvgOnField.SetValue(CameraClass.Instance.NightVision, _drainingHeadWearBattery);
			
			else if (_headWearThermal != null)
				PlayerInitPatch.thermalOnField.SetValue(CameraClass.Instance.ThermalVision, _drainingHeadWearBattery);
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
			CheckSightIfDraining();
		}
		//foreach sight in database<sight, collimator>: if sight has component with resource then collimator on, else
		public static void CheckSightIfDraining()
		{
			//ERROR:  If reap-ir is on and using canted collimator, enabled optic sight removes collimator effect. find a way to only drain active sight!
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- BATTERYSYSTEM: Checking Sight battery at " + Time.time + " ---");

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
						Logger.LogInfo("Sight on: " + _drainingSightBattery + " for " + key.name);

					// true for finding inactive gameobject reticles
					foreach (CollimatorSight col in key.gameObject.GetComponentsInChildren<CollimatorSight>(true))
					{
						col.gameObject.SetActive(_drainingSightBattery);
					}
					foreach (OpticSight optic in key.gameObject.GetComponentsInChildren<OpticSight>(true))
					{
						if (key.SightMod.Item.TemplateId == "REAP-IR") // why is this here lmao
							continue;

						optic.enabled = _drainingSightBattery;
					}
				}
			}
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("---------------------------------------------");
		}
	}
	//unused for now
	internal class GameStartPatch : ModulePatch
	{
		//private static FieldInfo _inventoryBotField;
		//private static IBotGame _botGame;
		protected override MethodBase GetTargetMethod()
		{
			//_inventoryBotField = AccessTools.Field(typeof(Player), "_inventoryController");
			return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
		}
		[PatchPrefix]
		public static void PatchPrefix()
		{
			// Sub to Event to get and add Bot when they spawn, credit to DrakiaXYZ!
			//unsubscribe so no duplicates
			//_botGame = Singleton<IBotGame>.Instance;
			//_botGame.BotsController.BotSpawner.OnBotCreated -= owner => DrainSpawnedBattery(owner);
			//_botGame.BotsController.BotSpawner.OnBotCreated += owner => DrainSpawnedBattery(owner);
		}
	}

	public class PlayerInitPatch : ModulePatch
	{
		private static FieldInfo _inventoryField = null;
		private static InventoryControllerClass _inventoryController = null;
		private static InventoryControllerClass _botInventory = null;
		public static FieldInfo nvgOnField = null;
		public static FieldInfo thermalOnField = null;
		private static readonly System.Random _random = new System.Random();

		protected override MethodBase GetTargetMethod()
		{
			_inventoryField = AccessTools.Field(typeof(Player), "_inventoryController");
			nvgOnField = AccessTools.Field(typeof(NightVision), "_on");
			thermalOnField = AccessTools.Field(typeof(ThermalVision), "On");
			return typeof(Player).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		[PatchPostfix]
		public static async void Postfix(Player __instance, Task __result)
		{
			await __result;

			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("PlayerInitPatch AT " + Time.time + ", IsYourPlayer: " + __instance.IsYourPlayer + ", IsAI: " + __instance.FullIdInfo);

			if (__instance.IsYourPlayer)
			{
				BatterySystem.sightMods.Clear(); // remove old sight entries that were saved from previous raid
				_inventoryController = (InventoryControllerClass)_inventoryField.GetValue(__instance); //Player Inventory

				//Singleton<Player>.Instance.OnSightChangedEvent -= sight => BatterySystem.CheckSightIfDraining();
				//Singleton<Player>.Instance.OnSightChangedEvent += sight => BatterySystem.CheckSightIfDraining();
				BatterySystem.SetEarPieceComponents();
			}
			else //Spawned bots have their bal-, uh, batteries, drained
			{
				DrainSpawnedBattery(__instance);
			}
			Logger.LogInfo("---------------------------------------------");
		}

		private static void DrainSpawnedBattery(Player botPlayer)
		{
			_botInventory = (InventoryControllerClass)_inventoryField.GetValue(botPlayer);
			foreach (Item item in _botInventory.EquipmentItems)
			{
				//if item is in holster or firstprimaryweapon slot then>>
				for (int i = 0; i < item.GetItemComponentsInChildren<ResourceComponent>().Count(); i++)
				{
					ResourceComponent resource = item.GetItemComponentsInChildren<ResourceComponent>().ElementAt(i);
					if (resource.MaxResource > 0)
						resource.Value = _random.Next(BatterySystemConfig.SpawnDurabilityMin.Value, BatterySystemConfig.SpawnDurabilityMax.Value);

					if (BatterySystemConfig.EnableLogs.Value)
					{
						Logger.LogInfo("DrainSpawnedBattery on Bot: " + botPlayer + " at " + Time.time);
						Logger.LogInfo("Checking item from slot: " + item);
						Logger.LogInfo("Res value: " + resource.Value);
					}
				}
			}
		}
		public static Slot GetEquipmentSlot(EquipmentSlot slot)
		{
			return _inventoryController?.Inventory.Equipment.GetSlot(slot);
		}
	}

	//GClass697.GetBoneForSlot(EFT.InventoryLogic.IContainer container) throws the error
	public class GetBoneForSlotPatch : ModulePatch
	{
		//private static GClass697.GClass698 _gClass;
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass697).GetMethod(nameof(GClass697.GetBoneForSlot));
		}

		[PatchPrefix]
		public static void Prefix(ref GClass697 __instance, IContainer container)
		{
			//is compact collimator
			Logger.LogInfo("");
			Logger.LogInfo("--- BatterySystem: GetBoneForSlot at " + Time.time + " ---");
			Logger.LogInfo("GameObject: " + __instance.GameObject);
			Logger.LogInfo(" Container: " + container + container.ID);
			Logger.LogInfo("Items: " + container.Items.FirstOrDefault());
			//if bone == null then add a new tranform
			if (ModdingScreenPatch.IsCollimator(container?.Items.FirstOrDefault())) //replace collimator slot
			{
				Logger.LogInfo("Setting _gClass to " + __instance.ContainerBones[container].Item);
				Logger.LogInfo("GClass Item: " + __instance.ContainerBones[container].Item);
				Logger.LogInfo("GClass Bone: " + __instance.ContainerBones[container].Bone);
				Logger.LogInfo("GClass ItemView: " + __instance.ContainerBones[container].ItemView);
			}
			if (!__instance.ContainerBones.ContainsKey(container) && ModdingScreenPatch.IsCollimator(container?.ParentItem))
			{
				Logger.LogWarning("Trying to get bone for battery slot!");
				__instance.ContainerBones.Add(container, __instance.ContainerBones[container]);
			}
			Logger.LogInfo("---------------------------------------------");
		}
	}

	public class UpdatePhonesPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Player).GetMethod("UpdatePhonesReally", BindingFlags.NonPublic | BindingFlags.Instance);
		}
		[PatchPostfix]
		public static void PatchPostfix() //BetterAudio __instance
		{
			Logger.LogInfo("");
			Logger.LogInfo("UpdatePhonesPatch at " + Time.time);
			Singleton<BetterAudio>.Instance.Master.GetFloat("Compressor", out BatterySystem.compressor);
			Singleton<BetterAudio>.Instance.Master.GetFloat("CompressorMakeup", out BatterySystem.compressorMakeup);
		}
	}

	public class ModdingScreenPatch : ModulePatch
	{
		private static FieldInfo _slotFieldInfo = null;
		private static Slot[] _slot_0 = null;
		private static ItemObserveScreen<EditBuildScreen.GClass2746, EditBuildScreen> itemObserveScreen = null;
		protected override MethodBase GetTargetMethod()
		{
			_slotFieldInfo = AccessTools.Field(typeof(ItemObserveScreen<EditBuildScreen.GClass2746, EditBuildScreen>), "slot_0");
			return typeof(ItemObserveScreen<EditBuildScreen.GClass2746, EditBuildScreen>).GetMethod("method_6", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		[PatchPrefix]
		public static void Prefix() //LootItemClass weapon
		{
			itemObserveScreen = UnityEngine.Object.FindObjectOfType<ItemObserveScreen<EditBuildScreen.GClass2746, EditBuildScreen>>();
			Logger.LogInfo("--- BATTERYSYSTEM: ItemObserveScreen: " + Time.time + " ---");
			Logger.LogInfo("item: " + itemObserveScreen);
			_slot_0 = (Slot[])_slotFieldInfo.GetValue(itemObserveScreen);
			for (int i = _slot_0.Length - 1; i >= 0; i--)
			{
				Logger.LogInfo("Slot: " + _slot_0[i] + " Item: " + _slot_0[i].ContainedItem);
				if (IsCollimator(_slot_0[i].ParentItem.Parent.Item))
				{
					//_slot_0[i].RemoveItem();
					Logger.LogInfo("Removing slot: " + _slot_0[i]);
				}
			}
			Logger.LogInfo("---------------------------------------------");

		}
		public static bool IsCollimator(Item item)
		{
			if (item != null && (item.Template.Parent._id == "55818acf4bdc2dde698b456b" //compact collimator
				|| item.Template.Parent._id == "55818ad54bdc2ddc698b4569")) // collimator
				return true;
			else return false;
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
					Logger.LogInfo("BATTERYSYSTEM: ApplyItemPatch at: " + Time.time);
					Logger.LogInfo("Slot parent: " + __instance.ParentItem);
				}
				if (BatterySystem.IsInSlot(__instance.ContainedItem, PlayerInitPatch.GetEquipmentSlot(EquipmentSlot.Earpiece)))
				{
					if (BatterySystemConfig.EnableLogs.Value)
					{
						Logger.LogInfo("Slot is child of EarPiece!");
					}
				}
				else if (BatterySystem.IsInSlot(__instance.ParentItem, PlayerInitPatch.GetEquipmentSlot(EquipmentSlot.Headwear)))
				{ //if item in headwear slot applied
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of HeadWear!");
				}
				else if (BatterySystem.IsInSlot(__instance.ContainedItem, BatterySystemPlugin.gameWorld?.MainPlayer.ActiveSlot))
				{ // if sight is removed and empty slot is applied, then remove the sight from sightdb
					if (BatterySystemConfig.EnableLogs.Value)
						Logger.LogInfo("Slot is child of ActiveSlot!");
				}
				BatterySystem.SetEarPieceComponents();
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