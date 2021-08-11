﻿using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace AmuletOfManyMinions
{
	[Label("Client Config")]
	public class ClientConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public static ClientConfig Instance => ModContent.GetInstance<ClientConfig>();

		public const string AnchorInventory = "Inventory";
		public const string AnchorHealth = "Health";
		public const string AnchorDefault = AnchorHealth;
		public static readonly string[] AnchorOptions = new string[] { AnchorInventory, AnchorHealth };

		public const string QuickDefendToggle = "Toggle";
		public const string QuickDefendHold = "Hold";
		public static readonly string[] QuickDefendOptions = new string[] { QuickDefendToggle, QuickDefendHold };

		// Miscellaneous config options
		[Header("General Configuration")]
		[Label("Minion Tactic UI Anchor Position")]
		[Tooltip("Choose between anchoring the UI with the right side of the inventory, or the left side of the health/minimap")]
		[DrawTicks]
		[OptionStrings(new string[] { AnchorInventory, AnchorHealth })]
		[DefaultValue(AnchorDefault)]
		public string TacticsUIAnchorPos;

		[Label("Minion Quick Defend Hotkey Style")]
		[Tooltip("Choose whether Minion Quick Defend is toggled on/off by the hotkey, or activated while the hotkey is held")]
		[DrawTicks]
		[OptionStrings(new string[] { QuickDefendToggle, QuickDefendHold})]
		[DefaultValue(QuickDefendToggle)]
		public string QuickDefendHotkeyStyle;

		[Label("Show Minion Variety Bonus")]
		[Tooltip("If true, displays the user's current minion variety bonus in a buff tooltip")]
		[DefaultValue(true)]
		public bool ShowMinionVarietyBonus;

		[Header("Tactics Configuration")]
		[Label("Tactics Ignore Vanilla Minion Target Reticle")]
		[Tooltip("If true, minions will ignore the vanilla minion target reticle in favor of the npc selected by the current tactic\n" +
			"Note: This does not apply to reticles placed by hitting enemies with a whip")]
		[DefaultValue(false)]
		public bool IgnoreVanillaTargetReticle;

		[Label("Enable Whip Quick Tactics Menu")]
		[Tooltip("If true, right clicking with a whip will bring up the Tactics Radial Menu\n" +
			"Note: May interfere with the right click of modded whips.")]
		[DefaultValue(false)]
		public bool WhipRightClickTacticsRadial;

		[JsonIgnore] //Hides it in UI and file
		public bool AnchorToInventory => TacticsUIAnchorPos == AnchorInventory;

		[JsonIgnore]
		public bool AnchorToHealth => TacticsUIAnchorPos == AnchorHealth;


		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			// Correct invalid names
			if (Array.IndexOf(AnchorOptions, TacticsUIAnchorPos) <= -1)
			{
				TacticsUIAnchorPos = AnchorDefault;
			}

			// Correct invalid names
			if (Array.IndexOf(QuickDefendOptions, QuickDefendHotkeyStyle) <= -1)
			{
				QuickDefendHotkeyStyle = QuickDefendToggle;
			}
		}
	}

	public class ServerConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;
		public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

		// tactics config options
		[Header("Tactics Configuration")]
		[Label("Whips Set Tactics Waypoint")]
		[Tooltip("If true, hitting an enemy with a whip will move the tactics waypoint to that enemy.")]
		[DefaultValue(true)]
		public bool WhipsSetWaypoint;

		[Label("Melee Squires Set Tactics Waypoint")]
		[Tooltip("If true, hitting an enemy directly with a squire will move the tactics waypoint to that enemy.")]
		[DefaultValue(false)]
		public bool SquiresSetWaypoint;

		[Label("Ranged Squires Set Tactics Waypoint")]
		[Tooltip("If true, hitting an enemy with a squire-shot projectile will move the tactics waypoint to that enemy.")]
		[DefaultValue(false)]
		public bool SquireProjSetWaypoint;
		// balance config options
		[Header("Balance Configuration")]

		[Range(20, 300)]
		[Increment(5)]
		[DefaultValue(100)]
		[Label("Global Damage Scale")]
		[Tooltip("Scale the damage of every item in the mod by a positive or negative percentage. 100% = default")]
		[Slider]
		public int GlobalDamageMultiplier;


		[Range(0, 80)]
		[DefaultValue(0)]
		[Label("Non-Minion Anti-Synergy")]
		[Tooltip("Reduce damage done by non-summon weapons while a minion is active")]
		[Slider]
		public int OtherDamageMinionNerf;

		[Range(0, 50)]
		[DefaultValue(0)]
		[Label("Minion/Squire Anti-Synergy")]
		[Tooltip("Reduce minion damage by a percentage while a squire is active")]
		[Slider]
		public int MinionDamageSquireNerf;

		[Range(0, 15)]
		[DefaultValue(0)]
		[Label("Squire/Minion Anti-Synergy")]
		[Tooltip("Reduce squire damage by a percentage for *each* active minion")]
		[Slider]
		public int SquireDamageMinionNerf;

		[Label("Minions Are Less Accurate")]
		[Tooltip("If enabled, minions will shoot less acurately and turn less sharply while chasing enemies")]
		[DefaultValue(false)]
		public bool MinionsInnacurate;

		[DefaultValue(false)]
		[Label("Squires Occupy a Minion Slot")]
		[Tooltip("If enabled, squires will occupy a minion slot")]
		public bool SquireMinionSlot;
	}
}
