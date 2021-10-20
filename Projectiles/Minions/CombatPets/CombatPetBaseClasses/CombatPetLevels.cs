﻿using AmuletOfManyMinions.Projectiles.Minions.CombatPets.CombatPetEmblems;
using AmuletOfManyMinions.Projectiles.Minions.VanillaClones;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AmuletOfManyMinions.Projectiles.Minions.CombatPets
{
	internal struct CombatPetLevelInfo
	{
		internal int Level;
		internal int BaseDamage; // The base damage done by combat pets at this level of progression
		internal int BaseSearchRange; // The base distance combat pets will seek from the player 
		internal float BaseSpeed; // How the AI actually uses speed varies quite a bit from type to type ...

		public CombatPetLevelInfo(int level, int damage, int searchRange, int baseSpeed)
		{
			Level = level;
			BaseDamage = damage;
			BaseSearchRange = searchRange;
			BaseSpeed = baseSpeed;
		}
	}
	class CombatPetLevelTable : ModSystem
	{
		internal static CombatPetLevelInfo[] PetLevelTable;

		public override void Load()
		{
			PetLevelTable = new CombatPetLevelInfo[]{
				new(0, 6, 500, 8), // Base level
				new(1, 8, 550, 8), // ore tier
				new(2, 11, 575, 8), // EoC - tier
				new(3, 16, 650, 9), // Dungeon Tier
				new(4, 24, 750, 12), // Post WoF
				new(5, 32, 800, 14), // Post Mech
				new(6, 42, 900, 15), // Post Plantera
				new(7, 48, 950, 15), // Post Pillars
				new(8, 60, 1100, 16) // Post Moon Lord
			};
		}

		public override void Unload()
		{
			base.Unload();
		}
	}

	class LeveledCombatPetModPlayer : ModPlayer
	{
		internal int PetLevel { get; set; }
		internal int PetDamage { get; set; }

		internal CombatPetLevelInfo PetLevelInfo => CombatPetLevelTable.PetLevelTable[PetLevel];


		public void UpdatePetLevel(int newLevel, int newDamage, bool fromSync = false)
		{
			bool didUpdate = newLevel != PetLevel || PetDamage != newDamage;
			PetLevel = newLevel;
			PetDamage = newDamage;
			if(didUpdate && !fromSync)
			{
				// TODO MP packet
			}
		}

		public override void PostUpdate()
		{
			// look for the best Combat Pet Emblem in the player's inventory, use that
			// to set the player's combat pet's damage
			// TODO maybe don't run every frame
			int maxLevel = 0;
			int maxDamage = CombatPetLevelTable.PetLevelTable[0].BaseDamage;
			for(int i = 0; i < Player.inventory.Length; i++)
			{
				Item item = Player.inventory[i];
				if(item.ModItem != null && item.ModItem is CombatPetEmblem petEmblem)
				{
					// choose max tier rather than max damage
					if(petEmblem.PetLevel > maxLevel)
					{
						maxLevel = petEmblem.PetLevel;
						maxDamage = item.damage;
					}
				}
			}
			UpdatePetLevel(maxLevel, maxDamage);
		}
	}

	public abstract class CombatPetBuff : MinionBuff
	{
		public CombatPetBuff(params int[] projIds) : base(projIds) { }

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Main.vanityPet[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			base.Update(player, ref buffIndex);
			for(int i = 0; i < projectileTypes.Length; i++)
			{
				int projType = projectileTypes[i];
				if(player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[projType] <= 0)
				{
					var p = Projectile.NewProjectileDirect(player.GetProjectileSource_Buff(buffIndex), player.Center, Vector2.Zero, projType, 0, 0, player.whoAmI);
					// p.originalDamage is updated in each frame by the minion itself
					p.originalDamage = 0;
				}
			}
		}
	}

	public abstract class CombatPetVanillaCloneBuff : CombatPetBuff
	{
		public abstract int VanillaBuffId { get; }
		public abstract string VanillaBuffName { get; }

		public CombatPetVanillaCloneBuff(params int[] projIds) : base(projIds) { }

		public override string Texture => "Terraria/Images/Buff_" + VanillaBuffId;
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault(Language.GetTextValue("BuffName." + VanillaBuffName) + " (AoMM Version)");
			Description.SetDefault(Language.GetTextValue("BuffDescription." + VanillaBuffName));
		}

	}

	public abstract class CombatPetMinionItem<TBuff, TProj> : VanillaCloneMinionItem<TBuff, TProj> where TBuff: ModBuff where TProj: Minion
	{

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			tooltips.Add(new TooltipLine(Mod, "CombatPetDescription", 
				"This pet's fighting spirit has been awakened!\n" +
				"It can be powered up by holding a Combat Pet Emblem.")
			{
				overrideColor = Color.LimeGreen
			});
		}

		public override bool Shoot(Player player, ProjectileSource_Item_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			ApplyBuff(player);
			return false;
		}
	}
}
