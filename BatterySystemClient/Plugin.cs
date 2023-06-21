using BepInEx;
using BatterySystem.Configs;
using EFT;
using EFT.InventoryLogic;
using Comfort.Common;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.ComponentModel;

namespace BatterySystem
{
	//todo: voiceline when adrenaline activates, cooldown?
	[BepInPlugin("com.jiro.batterysystem", "BatterySystem", "1.0.0")]
	public class BatterySystemPlugin : BaseUnityPlugin
	{
		void Awake()
		{
			BatterySystemConfig.Init(Config);
			new BatterySystemPatch().Enable();

		}
/*
		void Update()
		{
			gameWorld = Singleton<GameWorld>.Instance;
			var gamePlayer = gameWorld.AllPlayers[0];
			//gamePlayer.TryGetItemInHands<;
			var primary = EquipmentSlot.FirstPrimaryWeapon;
			var secondary = EquipmentSlot.SecondPrimaryWeapon;
			var holster = EquipmentSlot.Holster;
			var headwear = EquipmentSlot.Headwear;
			InventoryControllerClass inventoryControllerClass = new InventoryControllerClass(gamePlayer.Profile, true);
			var itemInHands = inventoryControllerClass.ItemInHands;
			//sheesh
			var slot = Singleton<Slot>.Instance.ContainedItem;
			//slot;
			player = GameObject.Find("PlayerSuperior(clone)");

		}*/
	}
}