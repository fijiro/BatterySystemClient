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

namespace BatterySystem
{
	public class BatterySystemPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		public static bool drainingBattery = false;
		public static Slot batterySlot = null;
		private static NightVision nightVision = null;

		[PatchPostfix]
		static void Postfix(ref Slot __instance)
		{
			if (!BatterySystemConfig.EnableMod.Value || Singleton<GameWorld>.Instance == null) return;

			Logger.LogInfo("--- BATTERYSYSTEM ---");
			Logger.LogInfo("At: " + Time.time);
			Logger.LogInfo("Parent: " + __instance.ParentItem);
			Logger.LogInfo("ParentItem.Template.Parent._id: " + __instance.ParentItem.Template.Parent._id);
			
			nightVision = GameObject.Find("FPS Camera").GetComponent<NightVision>();

			batterySlot = __instance;
			drainingBattery = false;
			if (__instance.Items.Count() == 0) // item being removed
			{
				Logger.LogInfo("Contains 0 Items " + __instance.ContainedItem);
			}
			else if (__instance.Items.Count() == 1) // item being added
			{
				Logger.LogInfo("Contains 1 Item: " + __instance.ContainedItem);
				//enable nvg
				if (__instance.ContainedItem.GetItemComponent<ResourceComponent>().Value > 0)
				{
					drainingBattery = true;
				}

			}
			else
			{
				Logger.LogInfo("all items: " + __instance.ParentItem.GetAllItems().Count());
			}

			nightVision.ApplySettings();
		}
	}
	public class NightVisionPatch : ModulePatch
	{
		public static NightVision nightVision;
		protected override MethodBase GetTargetMethod()
		{
			return typeof(NightVision).GetMethod(nameof(NightVision.ApplySettings));
		}

		[PatchPostfix]
		static void Postfix(ref NightVision __instance)
		{
			if (__instance.name == "FPS Camera" && BatterySystemPlugin.gameWorld != null)
			{
				nightVision = __instance;
				//TextureMask = FPS Camera
				//__instance.GetComponent<TogglableComponent>().Toggle(); maybe with this we can on/off?
				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo($"At: {Time.time}s");
				Logger.LogInfo("Item: " + __instance.GetComponent<Item>() );

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
