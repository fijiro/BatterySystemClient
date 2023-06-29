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

namespace BatterySystem
{
	public class PlayerInitPatch : ModulePatch
	{
		
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Player).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		[PatchPostfix]
		private static async void Postfix(Task __result)
		{
			await __result;
			HeadWearDevicePatch.SetHeadWearComponents();
		}
	}

	public class HeadWearDevicePatch : ModulePatch
	{
		private static FieldInfo nvgOnField = null;
		private static FieldInfo thermalOnField = null;
		private static FieldInfo inventoryField = null;
		private static InventoryControllerClass inventoryController = null;

		private static Item headWearItem = null;
		private static NightVisionComponent headWearNVG = null;
		private static ThermalVisionComponent headWearThermal = null;
		private static Item batteryInHeadWear = null;

		public static ResourceComponent batteryResource = null;
		public static bool drainingBattery = false;

		public static void SetHeadWearComponents()
		{
			inventoryController = (InventoryControllerClass)inventoryField.GetValue(Singleton<GameWorld>.Instance.MainPlayer); //Player Inventory
			headWearItem = inventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).Items?.FirstOrDefault(); // default null else headwear
			headWearNVG = headWearItem?.GetItemComponentsInChildren<NightVisionComponent>().FirstOrDefault(); //default null else nvg item
			headWearThermal = headWearItem?.GetItemComponentsInChildren<ThermalVisionComponent>().FirstOrDefault(); //default null else thermal item
			batteryInHeadWear = headWearItem?.GetAllItems().FirstOrDefault(item => item.GetItemComponent<ResourceComponent>() != null); //default null else battery, useless?
			if ((batteryResource = batteryInHeadWear?.GetItemComponentsInChildren<ResourceComponent>(false).FirstOrDefault()) != null)
			{ //default null else resource

				// ERROR //////////////////////////////////////////////////////////////
				Logger.LogInfo("Checking db."); // if battery is removed, it still stays in the db! remove this!
				//if (!BatterySystemPlugin.batteryDictionary.TryGetValue(batteryResource, out drainingBattery))
				//{
				//	BatterySystemPlugin.batteryDictionary.Add(batteryResource, drainingBattery);
				//}
			}
			if (BatterySystemConfig.EnableLogs.Value)
			{
				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo("At: " + Time.time);
				Logger.LogInfo("headWearItem: " + headWearItem);
				Logger.LogInfo("headWearNVG: " + headWearNVG);
				Logger.LogInfo("headWearThermal: " + headWearThermal);
				Logger.LogInfo("Battery in HeadWear: " + batteryInHeadWear);
				Logger.LogInfo("Battery Resource: " + batteryResource);
			}
		}

		public static void CheckHeadWearIfDraining()
		{
			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("--- Checking HeadWear battery at " + Time.time + " ---");

			else if ((batteryResource != null ? (batteryResource.Value > 0 ? true : false) : false)
				&& (headWearNVG == null ? !CameraClass.Instance.ThermalVision.InProcessSwitching : !CameraClass.Instance.NightVision.InProcessSwitching)
				&& headWearNVG.Togglable.On)
				// NVG has battery installed and headwear isn't switching and is on
			{
				drainingBattery = true;
			}
			else
			{
				drainingBattery = false;
			}

			BatterySystemPlugin.batteryDictionary[batteryResource] = drainingBattery;

			if (BatterySystemConfig.EnableLogs.Value)
				Logger.LogInfo("Setting HeadWear_on: " + drainingBattery);

			if(headWearNVG == null)
				thermalOnField.SetValue(CameraClass.Instance.ThermalVision, drainingBattery);
			else
				nvgOnField.SetValue(CameraClass.Instance.NightVision, drainingBattery);
		}

		protected override MethodBase GetTargetMethod()
		{
			nvgOnField = AccessTools.Field(typeof(NightVision), "_on");
			thermalOnField = AccessTools.Field(typeof(ThermalVision), "_on");
			inventoryField = AccessTools.Field(typeof(Player), "_inventoryController");

			return typeof(Slot).GetMethod(nameof(Slot.ApplyContainedItem));
		}

		[PatchPostfix]
		static void Postfix(ref Slot __instance) // limit to only player asap
		{
			if (BatterySystemConfig.EnableMod.Value && BatterySystemPlugin.gameWorld != null)
			{

				if (BatterySystemConfig.EnableLogs.Value) 
				{
					Logger.LogInfo("APPLYING CONTAINED ITEM AT: " + Time.time);
					Logger.LogInfo("Slot parent: " + __instance.ParentItem);

				}
				if (headWearItem != null && __instance.ParentItem.ParentRecursiveCheck(headWearItem))
				{
					Logger.LogInfo("Slot is child of headwear!");
				}
				SetHeadWearComponents();
				BatterySystemPlugin.headWearCooldown = Time.time + 0.1f;
			}
		}
	}
	public class SightDevicePatch : ModulePatch
	{
		private static SightComponent sightComponent = null;

		protected override MethodBase GetTargetMethod()
		{
			return typeof(SightComponent).GetMethod(nameof(SightComponent.SetScopeMode));
		}

		[PatchPostfix]
		static void Postfix(ref SightComponent __instance)
		{
			sightComponent = __instance;
		}
	}
	public class NightVisionPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(NightVision).GetMethod(nameof(NightVision.StartSwitch));
		}

		[PatchPostfix]
		static void Postfix(ref NightVision __instance)
		{
			if (__instance.name == "FPS Camera" && BatterySystemConfig.EnableMod.Value && BatterySystemPlugin.gameWorld != null)   
				// if switching on with no battery or equipping with nvg on, turn off
			{
				if(BatterySystemConfig.EnableLogs.Value)
					Logger.LogInfo("APPLYING NVG SETTINGS AT: " + Time.time);
				HeadWearDevicePatch.SetHeadWearComponents();
				BatterySystemPlugin.headWearCooldown = Time.time + 0.1f;
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

