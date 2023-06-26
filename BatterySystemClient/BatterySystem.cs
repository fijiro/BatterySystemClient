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
using BepInEx.Configuration;
using EFT.Hideout;
using System.Security.Policy;

namespace BatterySystem
{
	public class BatterySystemPatch : ModulePatch
	{
		public static bool drainingBattery = false;
		private static Item batteryInNVG = null;
		public static ResourceComponent batteryResource = null;

		//private static MethodInfo setStateMethod = AccessTools.Method(typeof(NightVision), "method_1");
		//private static PropertyInfo nvgOnProperty = null;
		//private static bool nightVision_On = false;
		private static InventoryControllerClass inventoryController = null;
		private static Slot headWearSlot = null;
		public static NightVisionComponent headWearNVG = null;
		//private static NightVision nightVision = null;
		//private static bool nightVision_On = false;

		public static void CheckBattery()
		{
			//if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo("At: " + Time.time);
			}
			if (!BatterySystemConfig.EnableMod.Value || Singleton<GameWorld>.Instance == null) //uhh if disabled
			{
				drainingBattery = false; // if the headwear is removed, stop draining battery and return to skip following statements
				return;
			}

			inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(Singleton<GameWorld>.Instance.MainPlayer);
			headWearSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear);
			if (headWearSlot.Items.Count() == 0)
			{
				drainingBattery = false; // if the headwear is removed, stop draining battery and return to skip following statements
				return;
			}

			headWearNVG = headWearSlot.ContainedItem.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault();

			//if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo(headWearNVG);
				Logger.LogInfo(headWearNVG.Item);
				Logger.LogInfo("NVG: " + CameraClass.Instance.NightVision);
			}

			batteryInNVG = headWearSlot.ContainedItem.GetAllItems().FirstOrDefault(item => item.TemplateId == "aaa-battery");
			if (batteryInNVG == null)
			{
				//if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo(headWearNVG.Item);
				Logger.LogInfo("Battery in NVG is null!");
				batteryResource = null;
			}
			batteryResource = headWearNVG.Item.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault();

			//if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("Battery in NVG: " + batteryInNVG);
				Logger.LogInfo("Battery Resource: " + batteryResource);
			}

			//if (BatterySystemConfig.EnableLogs.Value)
			Logger.LogInfo("Checking battery at " + Time.time);
			//CameraClass.Instance.NightVision.ApplySettings();
			if (batteryResource != null) // NVG has battery installed and headwear is equipped
			{
				if (batteryResource.Value > 0) //&& NightVisionPatch.nightVision.On enable nvg if has battery
				{
					//nightVision.Color = headWearNVG.Template.Color;
					//nightVision_On = true;
					Logger.LogInfo($"Battery {batteryResource.Value}, NVG On");
					//setStateMethod.Invoke(CameraClass.Instance.NightVision, new object[] { true });
					drainingBattery = true;
				}
				else
				{
					Logger.LogInfo($"Battery {batteryResource.Value}, NVG Off");
					//nightVision_On = false;
					//nightVision.Color = Color.black;
					//setStateMethod.Invoke(CameraClass.Instance.NightVision, new object[] { false });
					drainingBattery = false;
					CameraClass.Instance.NightVision.ApplySettings();
				}
			}
			else {
				Logger.LogInfo($"Battery null, NVG not null, NVG Off");
				//setStateMethod.Invoke(CameraClass.Instance.NightVision, new object[] { false });
				//nightVision_On = false;
				//nightVision.Color = Color.black;
				drainingBattery = false;
			}
			FieldInfo nvgOnProperty = AccessTools.Field(typeof(NightVision), "_on");
			Logger.LogInfo("Settingvalue!");
			nvgOnProperty.SetValue(CameraClass.Instance.NightVision, drainingBattery);
			Logger.LogInfo("Setvalue!");
		}

		protected override MethodBase GetTargetMethod()
		{
			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		[PatchPostfix]
		static void Postfix()
		{
			CheckBattery();
		}
	}
	/*
	public class NightVisionPatch : ModulePatch
	{
		public static NightVision nightVision = null;
		protected override MethodBase GetTargetMethod()
		{
			return typeof(NightVision).GetMethod("method_1", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		[PatchPostfix]
		static void Postfix(ref bool __instance)
		{

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
