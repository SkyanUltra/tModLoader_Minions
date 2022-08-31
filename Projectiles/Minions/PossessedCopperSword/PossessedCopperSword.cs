﻿using AmuletOfManyMinions.Core;
using AmuletOfManyMinions.Dusts;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.PossessedCopperSword
{
	public class CopperSwordMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<CopperSwordMinion>(), ProjectileType<CopperSwordMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Starry Skyslasher");
			Description.SetDefault("An enchanted sword will fight for you!");
		}
	}

	public class CopperSwordMinionItem : MinionItem<CopperSwordMinionBuff, CopperSwordMinion>
	{
		public override string Texture => "AmuletOfManyMinions/Projectiles/Minions/PossessedCopperSword/CopperSwordMinion";
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Starry Skyslasher");
			Tooltip.SetDefault("Summons an enchanted sword to fight for you!");
		}
		public override void ApplyCrossModChanges()
		{
			CrossMod.WhitelistSummonersShineMinionDefaultSpecialAbility(Item.type, CrossMod.SummonersShineDefaultSpecialWhitelistType.MELEE);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.damage = 13;
			Item.knockBack = 0.5f;
			Item.mana = 10;
			Item.width = 32;
			Item.height = 32;
			Item.value = Item.buyPrice(0, 0, 20, 0);
			Item.rare = ItemRarityID.Green;
		}
		public override void AddRecipes()
		{
			CreateRecipe(1).AddIngredient(ItemID.Feather, 8).AddIngredient(ItemID.FallenStar, 3).AddTile(TileID.SkyMill).Register();
		}
	}
	public class CopperSwordMinion : GroupAwareMinion
	{
		public override int BuffId => BuffType<CopperSwordMinionBuff>();
		private readonly float baseRoation = 3 * (float)Math.PI / 4f;
		private int hitsSinceLastIdle = 0;
		private int framesSinceLastHit = 0;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Starry SkySlasher");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[Projectile.type] = 1;
			IdleLocationSets.trailingInAir.Add(Projectile.type);
		}

		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.tileCollide = false;
			Projectile.localNPCHitCooldown = 20;
			AttackState = AttackState.IDLE;
			attackFrames = 60;
		}

		public override void OnHitTarget(NPC target)
		{
			if (hitsSinceLastIdle++ > 2)
			{
				AttackState = AttackState.RETURNING;
			}
			framesSinceLastHit = 0;
		}

		public override Vector2 IdleBehavior()
		{
			base.IdleBehavior();
			Vector2 idlePosition = Player.Center;
			idlePosition.X += -Player.direction * IdleLocationSets.GetXOffsetInSet(IdleLocationSets.trailingInAir, Projectile);
			idlePosition.Y += -5 * Projectile.minionPos;
			if (!Collision.CanHitLine(idlePosition, 1, 1, Player.Center, 1, 1))
			{
				idlePosition.X = Player.Center.X + 20 * -Player.direction;
				idlePosition.Y = Player.Center.Y - 5;
			}
			Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
			TeleportToPlayer(ref vectorToIdlePosition, 2000f);
			Lighting.AddLight(Projectile.Center, Color.LightYellow.ToVector3() * 0.125f);
			return vectorToIdlePosition;
		}

		public override Vector2? FindTarget()
		{
			if (FindTargetInTurnOrder(625f, Projectile.Top) is Vector2 target)
			{
				return target;
			}
			else
			{
				return null;
			}
		}

		public override void TargetedMovement(Vector2 vectorToTargetPosition)
		{
			// alway clamp to the idle position
			int inertia = 5;
			int speed = 8;
			vectorToTargetPosition.SafeNormalize();
			vectorToTargetPosition *= speed;
			if (Main.rand.NextBool(5))
			{
				Dust.NewDust(Projectile.Center,
					Projectile.width / 2,
					Projectile.height / 2, DustType<StarDust>(),
					-Projectile.velocity.X,
					-Projectile.velocity.Y);
			}
			if (framesSinceLastHit++ > 10)
			{
				Projectile.velocity = (Projectile.velocity * (inertia - 1) + vectorToTargetPosition) / inertia;
			}
			Projectile.rotation += (float)Math.PI / 9;
		}

		public override void IdleMovement(Vector2 vectorToIdlePosition)
		{
			// alway clamp to the idle position
			int inertia = 5;
			int maxSpeed = AttackState == AttackState.IDLE ? 12 : 8;
			if (vectorToIdlePosition.Length() < 32)
			{
				// return to the attacking state after getting back home
				AttackState = AttackState.IDLE;
				hitsSinceLastIdle = 0;
				framesSinceLastHit = 0;
			}
			Vector2 speedChange = vectorToIdlePosition - Projectile.velocity;
			if (speedChange.Length() > maxSpeed)
			{
				speedChange.SafeNormalize();
				speedChange *= maxSpeed;
			}
			Projectile.velocity = (Projectile.velocity * (inertia - 1) + speedChange) / inertia;

			float intendedRotation = baseRoation + Player.direction * (Projectile.minionPos * (float)Math.PI / 36);
			intendedRotation += Projectile.velocity.X * 0.05f;
			Projectile.rotation = intendedRotation;
		}
	}
}
