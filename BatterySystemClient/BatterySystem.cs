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

namespace BatterySystem
{
	public class BatterySystemPatch : ModulePatch
	{
		private static FieldInfo nvgOnField = null;
		private static FieldInfo inventoryField = null;
		private static InventoryControllerClass inventoryController = null;

		private static Item headWearItem = null;
		public static NightVisionComponent headWearNVG = null;
		private static Item batteryInNVG = null;

		public static ResourceComponent batteryResource = null;
		public static bool drainingBattery = false;

		public static void SetNvgComponents()
		{
			inventoryController = (InventoryControllerClass)inventoryField.GetValue(Singleton<GameWorld>.Instance.MainPlayer); //Player Inventory
			headWearItem = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).Items?.FirstOrDefault(); // default null else headwear
			headWearNVG = headWearItem?.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault(); //default null else nvg item
			batteryInNVG = headWearNVG?.Item.GetAllItems().FirstOrDefault(item => item.TemplateId == "aaa-battery"); //default null else battery, useless?
			batteryResource = batteryInNVG?.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault(); //default null else resource

			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo("At: " + Time.time);
				Logger.LogInfo("headWearItem: " + headWearItem);
				Logger.LogInfo("headWearNVG: " + headWearNVG);
				Logger.LogInfo("Battery in NVG: " + batteryInNVG);
				Logger.LogInfo("Battery Resource: " + batteryResource);
				Logger.LogInfo("NVG: " + CameraClass.Instance.NightVision);
			}
		}

		public static void CheckIfDraining()
		{
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- Checking battery at " + Time.time + " ---");

			if (CameraClass.Instance.NightVision.InProcessSwitching) // when switching, nvg is off.
			{
				drainingBattery = false;
			}
			else if ((batteryResource != null ? (batteryResource.Value > 0 ? true : false) : false)
				&& headWearNVG.Togglable.On) // NVG has battery installed and headwear is equipped
			{
				//CameraClass.Instance.NightVision.Color = headWearNVG.Template.Color;
				drainingBattery = true;
			}
			else
			{
				//CameraClass.Instance.NightVision.Color = Color.black;
				drainingBattery = false;
			}

			if(BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("Setting NVG_on: " + drainingBattery);

			nvgOnField.SetValue(CameraClass.Instance.NightVision, drainingBattery);
		}


		protected override MethodBase GetTargetMethod()
		{
			nvgOnField = AccessTools.Field(typeof(NightVision), "_on");
			inventoryField = AccessTools.Field(typeof(Player), "_inventoryController");

			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		[PatchPostfix]
		static void Postfix(ref Slot __instance) // limit to only player
		{
			if (BatterySystemConfig.EnableMod.Value || Singleton<GameWorld>.Instance != null)
			{
				if(BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("APPLYING CONTAINED ITEM AT: " + Time.time + __instance?.ParentItem?.Name);
				SetNvgComponents();
				BatterySystemPlugin.cooldown = Time.time + 0.01f;
			}
		}
	}

	public class NightVisionPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(NightVision).GetMethod(nameof(NightVision.ApplySettings));
		}

		[PatchPostfix]
		static void Postfix(ref NightVision __instance)
		{
			if (__instance.name == "FPS Camera" && BatterySystemConfig.EnableMod.Value)   // if switching on with no battery or equipping with nvg on, turn off
			{
				if(BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("APPLYING NVG SETTINGS AT: " + Time.time);
				BatterySystemPatch.SetNvgComponents();
				BatterySystemPlugin.cooldown = Time.time + 0.01f;
				//temp workaround kek. have to do a coroutine that triggers after !InProcessSwitching.
			}
		}
	}
}
/*
//TextureMask = FPS Camera
//__instance.GetComponent<TogglableComponent>().Toggle(); maybe with this we can on/off?
Logger.LogInfo("--- BATTERYSYSTEM : NVG ---");
Logger.LogInfo($"At: {Time.time}s");

if (__instance.On && BatterySystemPatch.drainingBattery) //enable nvg
{
	Logger.LogInfo("Using Color " + BatterySystemPlugin.nvgDefaultColor[BatterySystemPatch.batterySlot.ParentItem.TemplateId] + "for item " + BatterySystemPatch.batterySlot.ParentItem);
	__instance.Color = BatterySystemPlugin.nvgDefaultColor[BatterySystemPatch.batterySlot.ParentItem.TemplateId];
}
else //disable
{
	__instance.Color = Color.black;
}
}
}
}
/*public class ForceSwitchPatch : ModulePatch
{
protected override MethodBase GetTargetMethod()
{
return typeof(PlayerCameraController).GetMethod("method_4", BindingFlags.Instance | BindingFlags.NonPublic); // thermal toggle button pressed
}

[PatchPostfix]
static void Postfix(ref PlayerCameraController __instance)
{
NightVision nightVision = __instance.GetComponent<NightVision>();
Logger.LogInfo("--- BATTERYSYSTEM ---");
Logger.LogInfo("Method4 at" + Time.time);
if (__instance.Camera.name == "FPS Camera")
{
Logger.LogInfo("passed");
// call nvg-slot.applyitems();
//nightVision.StartSwitch(true);
//nightVision.enabled = false;
}
}
}*/

