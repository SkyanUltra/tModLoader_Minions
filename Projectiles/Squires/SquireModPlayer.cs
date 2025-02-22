﻿using AmuletOfManyMinions.Dusts;
using AmuletOfManyMinions.Items.Accessories.SquireSkull;
using AmuletOfManyMinions.Items.Accessories.TechnoCharm;
using AmuletOfManyMinions.Items.Armor.AridArmor;
using AmuletOfManyMinions.Items.Armor.RoyalArmor;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Squires
{
	public class SquireModPlayer : ModPlayer
	{
		public bool squireSkullAccessory;
		public int squireDebuffOnHit = -1;

		public float SquireRangeFlatBonus { get; set; }

		// depends on squire travel speed modifier, 5% per block
		public float SquireTravelSpeedMultiplier => 1 + SquireRangeFlatBonus * 0.05f / 16f;

		public float SquireAttackSpeedMultiplier { get; set; }
		
		// Also apply melee attack speed modifiers, up to a maximum of 45% of the original attack speed 
		public float FullSquireAttackSpeedModifier => Math.Max(0.45f, SquireAttackSpeedMultiplier + 1 - Player.GetTotalAttackSpeed(DamageClass.SummonMeleeSpeed));

		public float squireDamageMultiplierBonus;
		public float squireDamageOnHitMultiplier;
		internal int squireDebuffTime;
		internal bool royalArmorSetEquipped;
		internal bool squireBatAccessory;
		internal bool aridArmorSetEquipped;
		internal bool hardmodeOreSquireArmorSetEquipped;
		internal bool spookyArmorSetEquipped;
		internal bool squireTechnoSkullAccessory;
		internal bool graniteArmorEquipped;
		internal float usedMinionSlots;

		// shouldn't be hand-rolling key press detection but here we are
		private bool didReleaseTap;
		private bool didDoubleTap;
		

		public override void ResetEffects()
		{
			squireSkullAccessory = false;
			squireTechnoSkullAccessory = false;
			squireBatAccessory = false;
			royalArmorSetEquipped = false;
			aridArmorSetEquipped = false;
			graniteArmorEquipped = false;
			hardmodeOreSquireArmorSetEquipped = false;
			spookyArmorSetEquipped = false;
			SquireAttackSpeedMultiplier = 1;
			squireDamageOnHitMultiplier = 1;
			SquireRangeFlatBonus = 0;
			squireDamageMultiplierBonus = 0;
		}

		public bool HasSquire()
		{
			foreach (int squireType in SquireMinionTypes.squireTypes)
			{
				if (Player.ownedProjectileCounts[squireType] > 0)
				{
					return true;
				}
			}
			return false;
		}

		public Projectile GetSquire()
		{
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (p.active && p.owner == Player.whoAmI && SquireMinionTypes.Contains(p.type))
				{
					return p;
				}
			}
			return null;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier modifier)
		{
			if(!item.CountsAsClass<SummonDamageClass>() && usedMinionSlots > 0)
			{
				modifier -= ServerConfig.Instance.OtherDamageMinionNerf / 100f;
			}
			if (!SquireMinionTypes.Contains(item.shoot))
			{
				return;
			}
			//TODO maybe +=
			modifier *= (1f + squireDamageMultiplierBonus);
		}

		private void MyDidDoubleTap()
		{
			if (Main.myPlayer != Player.whoAmI && Main.netMode != NetmodeID.Server)
			{
				//Only do control related stuff on the local player
				return;
			}

			int tapDirection = Main.ReversedUpDownArmorSetBonuses ? 1 : 0;
			bool tappedRecently = Player.doubleTapCardinalTimer[tapDirection] > 0;
			bool didReleaseTapThisFrame = tapDirection == 0 ?
				Player.releaseDown :
				Player.releaseUp;
			bool didTapThisFrame = tapDirection == 0 ?
				Player.controlDown :
				Player.controlUp;
			didDoubleTap = false;
			if (!tappedRecently)
			{
				didReleaseTap = false;
			}
			else if (didReleaseTapThisFrame && tappedRecently)
			{
				didReleaseTap = true;
			}
			else if (tappedRecently && didReleaseTap && didTapThisFrame)
			{
				didDoubleTap = true;
				didReleaseTap = false;
			}
		}

		public override void PreUpdate()
		{
			MyDidDoubleTap();
		}

		private int modifiedFixedDamage(int damage)
		{
			return (int)(Player.GetDamage<SummonDamageClass>().ApplyTo(damage * squireDamageMultiplierBonus));
		}

		private void SummonSquireSubMinions()
		{
			Projectile mySquire = GetSquire();
			bool hasSquire = mySquire != null;
			if (!hasSquire)
			{
				return;
			}
			int skullType = ProjectileType<SquireSkullProjectile>();
			int technoSkullType = ProjectileType<TechnoCharmProjectile>();
			int crownType = ProjectileType<RoyalCrownProjectile>();
			int tumblerType = ProjectileType<AridTumblerProjectile>();
			var source = mySquire.GetSource_FromThis();
			bool canSummonAccessory = Player.whoAmI == Main.myPlayer;
			// summon the appropriate squire orbiter(s)
			if (canSummonAccessory && squireSkullAccessory && Player.ownedProjectileCounts[skullType] == 0)
			{
				Projectile.NewProjectile(source, mySquire.Center, mySquire.velocity, skullType, 0, 0, Player.whoAmI);
			}
			if (canSummonAccessory && squireTechnoSkullAccessory && Player.ownedProjectileCounts[technoSkullType] == 0)
			{
				Projectile.NewProjectile(source, mySquire.Center, mySquire.velocity, technoSkullType, 0, 0, Player.whoAmI);
			}
			if (canSummonAccessory && royalArmorSetEquipped && Player.ownedProjectileCounts[crownType] == 0)
			{
				Projectile.NewProjectile(source, mySquire.Center, mySquire.velocity, crownType, modifiedFixedDamage(12), 0, Player.whoAmI);
			}
			if (canSummonAccessory && aridArmorSetEquipped && Player.ownedProjectileCounts[tumblerType] == 0)
			{
				Projectile.NewProjectile(source, mySquire.Center, mySquire.velocity, tumblerType, modifiedFixedDamage(18), 0, Player.whoAmI);
			}

		}

		public override void PostUpdateEquips()
		{
			// Make sure this runs before PostUpdate()
			// LeveledCombatPetPlayer does some wacky stuff with Player.maxMinions there
			// so any other minion count altering code should run before then
			if (ServerConfig.Instance.SquireMinionSlot && GetSquire() != default)
			{
				Player.maxMinions = Math.Max(0, Player.maxMinions - 1);
			}
		}
		public override void PostUpdate()
		{

			SummonSquireSubMinions();
			// apply bat buff if set bonus active

			// undo buff from skull orbiter
			if (Player.ownedProjectileCounts[ProjectileType<SquireSkullProjectile>()] == 0)
			{
				squireDebuffOnHit = -1;
			}

			// count used minion slots
			usedMinionSlots = 0;
			for(int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if(p.active && p.owner == Player.whoAmI)
				{
					usedMinionSlots += p.minionSlots;
				}
			}
			if(usedMinionSlots > 0)
			{
				float damageReduction = ServerConfig.Instance.SquireDamageMinionNerf / 100f;
				squireDamageMultiplierBonus -= damageReduction;
			}
		}
	}

	class SquireCooldownBuff : ModBuff
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Squire Special Cooldown");
			Description.SetDefault("Your squire's special is on cooldown!");
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	class SquireTagDamageBuff: ModBuff
	{
		public override string Texture => "Terraria/Images/Buff_" + BuffID.BlandWhipEnemyDebuff;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Squire Tag");
			Description.SetDefault("Take 10% increased damage from summoner weapons");
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}

	}

	class SquireGlobalProjectile : GlobalProjectile
	{
		public static HashSet<int> isSquireShot;
		// buffs that affect your squire
		public static HashSet<int> squireBuffTypes;
		public static HashSet<int> squireDebuffTypes;

		public override void Load()
		{
			isSquireShot = new HashSet<int>();
			squireBuffTypes = new HashSet<int>();
			squireDebuffTypes = new HashSet<int>();
		}

		public override void Unload()
		{
			isSquireShot = null;
			squireBuffTypes = null;
			squireDebuffTypes = null;
		}
		private void doBuffDust(Projectile projectile, int dustType)
		{
			Vector2 dustVelocity = new Vector2(0, -Main.rand.NextFloat() * 0.25f - 0.5f);
			for (int i = 0; i < 3; i++)
			{
				Vector2 offset = new Vector2(10 * (i - 1), (i == 1 ? -4 : 4) + Main.rand.Next(-2, 2));
				Dust dust = Dust.NewDustPerfect(projectile.Top + offset, dustType, dustVelocity, Scale: 1f);
				dust.customData = projectile.whoAmI;
			}
		}

		// add buff/debuff dusts if we've got a squire affecting buff or debuff
		public override void PostAI(Projectile projectile)
		{
			if (!SquireMinionTypes.Contains(projectile.type))
			{
				return;
			}
			Player player = Main.player[projectile.owner];
			foreach (int buffType in player.buffType)
			{
				bool debuff = false;

				if (squireDebuffTypes.Contains(buffType))
				{
					debuff = true;
				}
				else if (!squireBuffTypes.Contains(buffType))
				{
					continue;
				}
				int timeLeft = player.buffTime[player.FindBuffIndex(buffType)];
				if (timeLeft % 60 == 0)
				{
					doBuffDust(projectile, debuff ? DustType<MinusDust>() : DustType<PlusDust>());
				}
				break;
			}
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			if (!SquireMinionTypes.Contains(projectile.type) && !isSquireShot.Contains(projectile.type))
			{
				return;
			}
			float multiplier = Main.player[projectile.owner].GetModPlayer<SquireModPlayer>().squireDamageOnHitMultiplier;
			if (multiplier == 1)
			{
				return;
			}
			// may need to manually apply defense formula
			damage = (int)(damage * multiplier);
		}

		public override void OnHitNPC(Projectile projectile, NPC target, int damage, float knockback, bool crit)
		{
			if (!SquireMinionTypes.Contains(projectile.type) && !isSquireShot.Contains(projectile.type))
			{
				return;
			}
			if(ServerConfig.Instance.SquiresDealTagDamage)
			{
				target.AddBuff(BuffType<SquireTagDamageBuff>(), 4 * 60);
			}
			SquireModPlayer player = Main.player[projectile.owner].GetModPlayer<SquireModPlayer>();
			int debuffType = player.squireDebuffOnHit;
			int duration = player.squireDebuffTime;
			if (debuffType == -1 || Main.rand.NextFloat() > 0.25f)
			{
				return;
			}
			target.AddBuff(debuffType, duration);
		}
	}
}
