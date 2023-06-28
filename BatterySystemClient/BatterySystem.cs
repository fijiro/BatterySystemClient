using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using HarmonyLib;
using Comfort.Common;
using UnityEngine;
using EFT;
using EFT.InventoryLogic;
using BSG.CameraEffects;

namespace BatterySystem
{
	public class BatterySystemPatch : ModulePatch
	{
		public static bool drainingBattery = false;
		private static Item batteryInNVG = null;
		public static ResourceComponent batteryResource = null;
		public static FieldInfo nvgOnField = null;
		private static InventoryControllerClass inventoryController = null;
		private static Slot headWearSlot = null;
		public static NightVisionComponent headWearNVG = null;

		public static void SetNvgComponents()
		{
			if (Singleton<GameWorld>.Instance == null) //uhh if the mod is disabled !BatterySystemConfig.EnableMod.Value || 
				return;

			headWearSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear);
			headWearNVG = headWearSlot?.ContainedItem.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault(); //default null else nvg item
			batteryInNVG = headWearNVG?.Item.GetAllItems().FirstOrDefault(item => item.TemplateId == "aaa-battery"); //default null else battery
			batteryResource = headWearNVG?.Item.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault(); //default null else resource

			//if (BatterySystemConfig.EnableLogs.Value)
			{

				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo("At: " + Time.time);
				Logger.LogInfo("headWearNVG: " + headWearNVG.Item);
				Logger.LogInfo("NVG: " + CameraClass.Instance.NightVision);
				Logger.LogInfo("Battery in NVG: " + batteryInNVG);
				Logger.LogInfo("Battery Resource: " + batteryResource);
			}
		}

		public static void CheckIfDraining()
		{
			//if (BatterySystemConfig.EnableLogs.Value)
			Logger.LogInfo("Checking battery at " + Time.time);
			if ((batteryResource?.Value ?? 0) > 0 && headWearNVG.Togglable.On && !CameraClass.Instance.NightVision.InProcessSwitching) // NVG has battery installed and headwear is equipped
			{

				Logger.LogInfo($"Battery {batteryResource.Value}");
				//CameraClass.Instance.NightVision.Color = headWearNVG.Template.Color;
				drainingBattery = true;

			}
			else if (CameraClass.Instance.NightVision.InProcessSwitching)
			{
				Logger.LogInfo("InProcessSwitching! " + Time.time);
				drainingBattery = false;
			}
			else
			{
				Logger.LogInfo("Battery null or NVG off");
				//CameraClass.Instance.NightVision.Color = Color.black;
				drainingBattery = false;
			}

			Logger.LogInfo("Turning NVG " + drainingBattery);
			nvgOnField.SetValue(CameraClass.Instance.NightVision, drainingBattery);
			Logger.LogInfo("Value set!");
		}


		protected override MethodBase GetTargetMethod()
		{
			inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(Singleton<GameWorld>.Instance.MainPlayer);
			nvgOnField = AccessTools.Field(typeof(NightVision), "_on");
			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		[PatchPostfix]
		static void Postfix(ref Slot __instance) // limit to only player
		{
			//if(__instance.i)
			SetNvgComponents();
			BatterySystemPlugin.cooldown = Time.time + 0.1f;
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
			if (__instance.name == "FPS Camera")   // if switching on with no battery or equipping with nvg on, turn off
			{
				BatterySystemPatch.SetNvgComponents();
				BatterySystemPlugin.cooldown = Time.time + 0.1f; //temp workaround kek
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

