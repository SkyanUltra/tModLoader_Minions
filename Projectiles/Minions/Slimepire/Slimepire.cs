﻿using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.Slimepire
{
	public class SlimepireMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<SlimepireMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Slimepire");
			Description.SetDefault("A vampire slime will fight for you!");
		}
	}

	public class SlimepireMinionItem : MinionItem<SlimepireMinionBuff, SlimepireMinion>
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Slimepire Staff");
			Tooltip.SetDefault("Summons a vampiric slime to fight for you!\nIgnores 15 enemy defense");
		}
		public override void ApplyCrossModChanges()
		{
			CrossMod.WhitelistSummonersShineMinionDefaultSpecialAbility(Item.type, CrossMod.SummonersShineDefaultSpecialWhitelistType.MELEE);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.damage = 20;
			Item.knockBack = 0.5f;
			Item.mana = 10;
			Item.width = 28;
			Item.height = 28;
			Item.value = Item.buyPrice(0, 2, 0, 0);
			Item.rare = ItemRarityID.LightRed;
		}
	}

	public class SlimepireMinion : SimpleGroundBasedMinion
	{
		public override int BuffId => BuffType<SlimepireMinionBuff>();
		private float intendedX = 0;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Slimepire");
			Main.projFrames[Projectile.type] = 6;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 20;
			Projectile.height = 20;
			DrawOffsetX = (Projectile.width - 44) / 2;
			DrawOriginOffsetY = 0;
			attackFrames = 60;
			NoLOSPursuitTime = 300;
			StartFlyingHeight = 96;
			StartFlyingDist = 64;
			DefaultJumpVelocity = 4;
			MaxJumpVelocity = 12;
			searchDistance = 825;
		}

		protected override bool DoPreStuckCheckGroundedMovement()
		{
			if (!GHelper.didJustLand)
			{
				Projectile.velocity.X = intendedX;
				// only path after landing
				return false;
			}
			return true;
		}

		protected override bool CheckForStuckness()
		{
			return true;
		}
		protected override void DoGroundedMovement(Vector2 vector)
		{
			// always jump "long" if we're far away from the enemy
			if (Math.Abs(vector.X) > StartFlyingDist && vector.Y < -32)
			{
				vector.Y = -32;
			}
			GHelper.DoJump(vector);
			int maxHorizontalSpeed = vector.Y < -64 ? 4 : 8;
			if(TargetNPCIndex is int idx && vector.Length() < 64)
			{
				// go fast enough to hit the enemy while chasing them
				Vector2 targetVelocity = Main.npc[idx].velocity;
				Projectile.velocity.X = Math.Max(4, Math.Min(maxHorizontalSpeed, Math.Abs(targetVelocity.X) * 1.25f)) * Math.Sign(vector.X);
			} else
			{
				// try to match the player's speed while not chasing an enemy
				Projectile.velocity.X = Math.Max(1, Math.Min(maxHorizontalSpeed, Math.Abs(vector.X) / 16)) * Math.Sign(vector.X);
			}
			intendedX = Projectile.velocity.X;
		}

		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			minFrame = GHelper.isFlying ? 0 : 4;
			maxFrame = GHelper.isFlying ? 4 : 6;
			base.Animate(minFrame, maxFrame);
		}

		public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			// manually bypass defense
			// this may not be wholly correct
			int defenseBypass = 15;
			int defense = Math.Min(target.defense, defenseBypass);
			damage += defense / 2;
		}
	}
}
