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
			//if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo("At: " + Time.time);
			}
			if (Singleton<GameWorld>.Instance == null) //uhh if the mod is disabled !BatterySystemConfig.EnableMod.Value || 
				return;

			headWearSlot = null;
			headWearNVG = null;
			batteryInNVG = null;
			batteryResource = null;

			inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(Singleton<GameWorld>.Instance.MainPlayer);
			headWearSlot = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear);

			if (headWearSlot.Items.Count() == 0) //if the headwear doesnt have anything, then it cant have a nvg
			{
				Logger.LogInfo("No headWear equipped at " + Time.time);
			}
			else if ((headWearNVG = headWearSlot.ContainedItem.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault()) == null) //no nvg item
			{
				Logger.LogInfo("No headWearNVG equipped at " + Time.time);
			}
			else if ((batteryInNVG = headWearNVG.Item.GetAllItems().FirstOrDefault(item => item.TemplateId == "aaa-battery")) != null)
			{
				batteryResource = headWearNVG.Item.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault();
				
				//if (BatterySystemConfig.EnableLogs.Value)
				{
					foreach (Item i in headWearSlot.Items)
					{
						Logger.LogInfo("INHEADWEAR: " + i);
					}
					Logger.LogInfo("headWearNVG: " + headWearNVG.Item);
					Logger.LogInfo("NVG: " + CameraClass.Instance.NightVision);
					Logger.LogInfo("Battery in NVG: " + batteryInNVG);
					Logger.LogInfo("Battery Resource: " + batteryResource);
				}
			}
		}

		public static void CheckIfDraining()
		{
			//Move to own function: bool isDraining
			//if (BatterySystemConfig.EnableLogs.Value)
			Logger.LogInfo("Checking battery at " + Time.time);
			if (batteryResource != null) // NVG has battery installed and headwear is equipped
			{
				if (batteryResource.Value > 0 && headWearNVG.Togglable.On && !CameraClass.Instance.NightVision.InProcessSwitching)
				{
					Logger.LogInfo($"Battery {batteryResource.Value}, NVG On");
					CameraClass.Instance.NightVision.Color = headWearNVG.Template.Color;
					drainingBattery = true;
				}
				else
				{
					Logger.LogInfo($"Battery {batteryResource.Value}, NVG Off");
					//CameraClass.Instance.NightVision.Color = Color.black;
					drainingBattery = false;
				}
			}
			else
			{
				Logger.LogInfo($"Battery null, NVG Off");
				//CameraClass.Instance.NightVision.Color = Color.black;
				drainingBattery = false;
			}

			Logger.LogInfo("Settingvalue!");
			nvgOnField.SetValue(CameraClass.Instance.NightVision, drainingBattery);
			Logger.LogInfo("Setvalue!");
		}


		protected override MethodBase GetTargetMethod()
		{
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
				CameraClass.Instance.NightVision.Color = Color.black;
				BatterySystemPlugin.cooldown = Time.time + 1; //temp workaround kek
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

