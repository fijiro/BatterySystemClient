using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using UnityEngine;
using EFT.InventoryLogic;
using BatterySystem.Configs;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using EFT.CameraControl;
using EFT.Interactive;
using System;
using Aki.Reflection.Utils;

namespace BatterySystem
{
	public class BatterySystemPatch : ModulePatch
	{
		public static bool drainingBattery = false;
		private static Item batteryInNVG = null;
		public static ResourceComponent batteryResource = null;

		private static InventoryControllerClass inventoryController = null;
		private static Slot headWearSlot = null;
		public static NightVisionComponent headWearNVG = null;
		public static NightVision nightVision = null;
		//private static bool nightVision_on = false;

		public static void checkBattery()
		{
			if (batteryResource != null) // NVG has battery installed and headwear is equipped
			{
				Logger.LogInfo("Contains item with resource: " + batteryResource.Item);
				if (batteryResource.Value > 0 && nightVision.On) //enable nvg if has battery
				{
					nightVision.Color = headWearNVG.Template.Color;
					drainingBattery = true;
				}
				else
				{
					nightVision.Color = Color.black;
					drainingBattery = false;
				}
			}
			else if (nightVision != null) {
			
				nightVision.Color = Color.black;
				drainingBattery = false;
			}
		}

		protected override MethodBase GetTargetMethod()
		{
			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		[PatchPostfix]
		static void Postfix(ref Slot __instance)
		{

			Logger.LogInfo("--- BATTERYSYSTEM ---");
			Logger.LogInfo("At: " + Time.time);
			Logger.LogInfo(__instance);

			if (!BatterySystemConfig.EnableMod.Value || BatterySystemPlugin.gameWorld == null) //uhh if disabled
			{
				drainingBattery = false; // if the headwear is removed, stop draining battery and return to skip following statements
				return;
			}

			inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(BatterySystemPlugin.gameWorld.MainPlayer);
			headWearSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear);

			if (headWearSlot.Items.Count() == 0)
			{
				drainingBattery = false; // if the headwear is removed, stop draining battery and return to skip following statements
				return;
			}

			headWearNVG = headWearSlot.ContainedItem.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault();
			Logger.LogInfo(headWearNVG);
			Logger.LogInfo(headWearNVG.Item);

			nightVision = GameObject.Find("FPS Camera").GetComponent<NightVision>();
			Logger.LogInfo("NVG: " + nightVision);

			batteryInNVG = headWearSlot.ContainedItem.GetAllItems().FirstOrDefault(item => item.TemplateId == "aaa-battery");
			if(batteryInNVG == null)
			{
				batteryResource = null;
				Logger.LogInfo("Battery in NVG is null!");
				drainingBattery = false;
				return;
			}
			Logger.LogInfo("Battery in NVG: " + batteryInNVG);

			batteryResource = headWearNVG.Item.GetItemComponentsInChildren<ResourceComponent>(false).First();
			Logger.LogInfo("Battery Resource: " + batteryResource);

			// i need to somehow find the "aaa-battery" when helmet is moved
			//Logger.LogInfo("_on: " + (NightVision)AccessTools.Field(typeof(NightVision), "_on").GetValue(GameObject.Find("FPS Camera")));
			//nightvision._on saves functionality, only disables color, how to access? 

			checkBattery();
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
			if (__instance.name == "FPS Camera" && BatterySystemPlugin.gameWorld != null)
			{
				InventoryControllerClass temp = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(BatterySystemPlugin.gameWorld.MainPlayer);
				temp.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ApplyContainedItem();
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
return typeof(PlayerCameraController).GetMethod("method_4", BindingFlags.Instance | BindingFlags.NonPublic); // nvg toggle button pressed
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
}
