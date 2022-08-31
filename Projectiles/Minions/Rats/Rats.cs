﻿using AmuletOfManyMinions.Items.Accessories;
using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.Rats
{
	public class RatsMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<RatsMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Aww, Rats!");
			Description.SetDefault("A group of rats will fight for you!");
		}
	}

	public class RatsMinionItem : MinionItem<RatsMinionBuff, RatsMinion>
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Rod of the Ratkeeper");
			Tooltip.SetDefault("Summons a horde of rats to fight for you!\nRats do a third of the listed damage\nIgnores 10 enemy defense");
		}
		public override void ApplyCrossModChanges()
		{
			CrossMod.WhitelistSummonersShineMinionDefaultSpecialAbility(Item.type, CrossMod.SummonersShineDefaultSpecialWhitelistType.MELEE);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.damage = 6;
			Item.knockBack = 0.5f;
			Item.mana = 10;
			Item.width = 28;
			Item.height = 28;
			Item.value = Item.buyPrice(0, 0, 5, 0);
			Item.rare = ItemRarityID.Blue;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			ApplyBuff(player);
			//summon 3 rats at a time
			for (int i = 0; i < 3; i++)
			{
				var p = Projectile.NewProjectileDirect(source, position, Vector2.Zero, ProjectileType<RatsMinion>(), damage, knockback, player.whoAmI);
				p.originalDamage = Item.damage;
			}
			return false;
		}
	}

	public class RatsMinion : SimpleGroundBasedMinion
	{
		public override int BuffId => BuffType<RatsMinionBuff>();

		// which of the 3 rats this is, affects some cosmetic behavior
		private int clusterIdx;

		private Dictionary<GroundAnimationState, (int, int?)> frameInfo = new Dictionary<GroundAnimationState, (int, int?)>
		{
			[GroundAnimationState.FLYING] = (8, 8),
			[GroundAnimationState.JUMPING] = (2, 8),
			[GroundAnimationState.STANDING] = (0, 1),
			[GroundAnimationState.WALKING] = (2, 8),
		};

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Rat (Friendly)");
			Main.projFrames[Projectile.type] = 9;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 8;
			Projectile.height = 16;
			Projectile.minionSlots = 0.333f;
			DrawOffsetX = -6;
			DrawOriginOffsetY = -6;
			attackFrames = 60;
			NoLOSPursuitTime = 300;
			StartFlyingHeight = 96;
			StartFlyingDist = 64;
			DefaultJumpVelocity = 4;
			MaxJumpVelocity = 12;
		}

		protected override void DoGroundedMovement(Vector2 vector)
		{

			// this one likes to jump while attacking
			// different rats like to jump different heights
			vector.Y -= 3 * (clusterIdx % 10);
			if(VectorToTarget is null)
			{
				vector.Y -= 16;
			}
			if (vector.Y < 0 && Math.Abs(vector.X) < StartFlyingHeight)
			{
				GHelper.DoJump(vector);
			}
			float xInertia = GHelper.stuckInfo.overLedge && !GHelper.didJustLand && Math.Abs(Projectile.velocity.X) < 2 ? 1.25f : 8;
			int xMaxSpeed = 7;
			if (VectorToTarget is null && Math.Abs(vector.X) < 8 && Math.Abs(Player.velocity.X) > 4)
			{
				Projectile.velocity.X = Player.velocity.X;
				return;
			}
			DistanceFromGroup(ref vector);
			if (AnimationFrame - LastHitFrame > 15)
			{
				Projectile.velocity.X = (Projectile.velocity.X * (xInertia - 1) + Math.Sign(vector.X) * xMaxSpeed) / xInertia;
			}
			else
			{
				Projectile.velocity.X = Math.Sign(Projectile.velocity.X) * xMaxSpeed * 0.75f;
			}
		}

		public override Vector2 IdleBehavior()
		{
			List<Projectile> rats = GetActiveMinions();
			Projectile head;
			if(rats.Count == 0)
			{
				clusterIdx = 0;
				head = Projectile;
			} else
			{
				clusterIdx = rats.IndexOf(Projectile);
				head = rats[0];
			}
			GHelper.SetIsOnGround();
			NoLOSPursuitTime = GHelper.isFlying ? 15 : 300;
			Vector2 idlePosition = Player.Center;
			// every rat should gather around the first rat
			idlePosition.X += -Player.direction * (8 + IdleLocationSets.GetXOffsetInSet(IdleLocationSets.trailingOnGround, head));
			if (!Collision.CanHitLine(idlePosition, 1, 1, Player.Center, 1, 1))
			{
				idlePosition = Player.Center;
			}
			idlePosition.X += (12 + rats.Count/3 ) * (float)Math.Sin(2 * Math.PI * ((GroupAnimationFrame % 60) / 60f + clusterIdx/(rats.Count + 1)));
			Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
			TeleportToPlayer(ref vectorToIdlePosition, 2000f);
			return vectorToIdlePosition;
		}

		public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			// each rat deals 1/3 damage, and ignores 10 defense
			// manually bypass defense
			// this may not be wholly correct
			int defenseBypass = 10;
			int defense = Math.Min(target.defense, defenseBypass);
			damage = (int)Math.Ceiling(damage / 3f);
			damage += defense / 2;
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			base.OnHitNPC(target, damage, knockback, crit);
			// add poison
			if(Main.rand.Next(0, 10) == 0)
			{
				target.AddBuff(BuffID.Poisoned, 300);
			}
		}
		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			GroundAnimationState state = GHelper.DoGroundAnimation(frameInfo, base.Animate);
		}
	}
}
