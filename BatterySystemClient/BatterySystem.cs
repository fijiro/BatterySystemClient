using EFT;
using System;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using UnityEngine;
using EFT.InventoryLogic;
using BatterySystem.Configs;

namespace BatterySystem
{
	public class BatterySystemPatch : ModulePatch
	{

		protected override MethodBase GetTargetMethod()
		{
			return typeof(Slot).GetMethod(nameof(Slot.Add));
		}

		[PatchPostfix]
		static void Postfix(ref Slot __instance, Item item)
		{
			if (!BatterySystemConfig.EnableMod.Value) return;

			if (item.TemplateId == "aaa-battery" && (__instance.ParentItem.TemplateId == "5d1b5e94d7ad1a2b865a96b0" || __instance.ParentItem.TemplateId == "5c0558060db834001b735271")) //flir+gpnvg
			{
				Logger.LogInfo("--- BATTERYSYSTEM ---");
				Logger.LogInfo("Item: " + item.TemplateId);
				Logger.LogInfo($"At: {Time.time}");
				Logger.LogInfo($"To slot: {__instance.Name}");
				Logger.LogInfo($"Parent: {__instance.ParentItem.TemplateId}");

			}
		}
	}
}
