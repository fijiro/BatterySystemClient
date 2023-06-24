using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using UnityEngine;
using EFT.InventoryLogic;
using BatterySystem.Configs;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using GPUInstancer;
//TextureMask = FPS Camera

namespace BatterySystem
{

	public class BatterySystemPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		public static bool drainBattery = false;
		public static Slot batterySlot = null;

		[PatchPostfix]
		static void Postfix(ref Slot __instance)
		{
			if (!BatterySystemConfig.EnableMod.Value || Singleton<GameWorld>.Instance == null) return;
			if (__instance.ContainedItem.TemplateId != "aaa-battery") return;
			batterySlot = __instance;
			NightVision nightVision = GameObject.Find("FPS Camera").GetComponent<NightVision>();
			
			Logger.LogInfo("--- BATTERYSYSTEM ---");
			Logger.LogInfo("Parent: " + __instance.ParentItem);
			Logger.LogInfo("ParentP: " + __instance.ParentItem.Parent);
			Logger.LogInfo("Container: " + __instance.ParentItem.Parent.Container.ToString());
			Logger.LogInfo("ContainerName: " + __instance.ParentItem.Parent.ContainerName);

			if (__instance.Items.Count() == 0) // item being added
			{
				Logger.LogInfo("Contains 0: " + __instance.ContainedItem);
				drainBattery = false; //stop burning resource
				
				if (__instance.ParentItem.TemplateId == "5c0558060db834001b735271" || __instance.ParentItem.TemplateId == "5c066e3a0db834001b7353f0") //pgnvg-avnis || armasight n-15 + nvg
				{
					//disable nvg
					Logger.LogInfo("Removing item from NVGs!");

				}
				else Logger.LogInfo("ParentItem.TemplateId Unknown: " + __instance.ParentItem.TemplateId);
			}
			else if (__instance.Items.Count() == 1) // item being added
			{
				Logger.LogInfo("Contains 1: " + __instance.ContainedItem);

				drainBattery = true;//start burning resource
				//GClass1810 burnResource = new GClass1810();
				//GClass1812 burnables = new GClass1812();
				//GClass2184 gClass2184 = new GClass2184();
				//Logger.LogInfo(gClass2184.CompatibleItems.ToString());
				//GClass2361 gClass2361 = new GClass2361(__instance.ContainedItem.TemplateId, gClass2184);
				//burnResource.SetItems(gClass2361[]);
				//if (burnResource.SupplyItems[0].TemplateId == "") { }
				if (__instance.ParentItem.TemplateId == "5c0558060db834001b735271" || __instance.ParentItem.TemplateId == "5c066e3a0db834001b7353f0") //pgnvg-anvis || armasight n-15 + nvg
				{
					//enable nvg
					Logger.LogInfo("Adding item to NVGs!");
				}
			}

			//Player player = Singleton<GameWorld>.Instance.MainPlayer;
			//InventoryControllerClass inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
			//Item helmet = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).FindItem("aaa-battery");
			//Logger.LogInfo(helmet);
			nightVision.ApplySettings();
		}
	}
	public class NightVisionPatch : ModulePatch
	{

		private static Color temp = Color.black;
		public static NightVision nightVision;
		protected override MethodBase GetTargetMethod()
		{
			return typeof(NightVision).GetMethod(nameof(NightVision.ApplySettings));
		}

		[PatchPostfix]
		static void Postfix(ref NightVision __instance)
		{
			nightVision = __instance;
			//if(__instance.name != "FPS Camera") return;
			Logger.LogInfo("--- BATTERYSYSTEM ---");
			Logger.LogInfo("Nvg apply settings: " + __instance);
			Logger.LogInfo($"At: {Time.time}s");
			if (BatterySystemPlugin.gameWorld == null || BatterySystemPlugin.gameWorld.MainPlayer == null) { }
			else if (BatterySystemPatch.drainBattery == true) //enable nvg
			{
				__instance.Color.r = 0.1f;
				__instance.Color.g = 1;
				__instance.Color.b = 0.1f;
			}
			else if (BatterySystemPatch.drainBattery == false) //disable
			{
				temp = __instance.Color;
				__instance.Color = Color.black;

			}
		}
	}

	public class NightVisionSwitchPatch : ModulePatch
	{

		protected override MethodBase GetTargetMethod()
		{
			return typeof(NightVision).GetMethod(nameof(NightVision.StartSwitch)); // Startswitch(BOOL), false = off, true = on
		}

		[PatchPostfix]
		static void Postfix(ref NightVision __instance, bool on)
		{
			//__instance.enabled = false disables StartSwitch, but animation still happens
			Logger.LogInfo("On: " + on + ", switching at: " + Time.time);
			//set color to black for the switch duration
		}
	}
	public class ForceSwitchPatch : ModulePatch
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
	}
}
