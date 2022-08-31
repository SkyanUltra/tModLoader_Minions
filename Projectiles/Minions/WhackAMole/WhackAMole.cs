﻿using AmuletOfManyMinions.Core;
using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.WhackAMole
{
	public class WhackAMoleMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<WhackAMoleCounterMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Jellybean Mole");
			Description.SetDefault("A magic mole will fight for you!");
		}
	}

	public class WhackAMoleMinionItem : MinionItem<WhackAMoleMinionBuff, WhackAMoleCounterMinion>
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Magic Jelly Bean Jar");
			Tooltip.SetDefault("Summons a stack of magic moles to fight for you!\n"+
				"This minion empowers for each additional slot expended on it");
		}
		public override void ApplyCrossModChanges()
		{
			CrossMod.WhitelistSummonersShineMinionDefaultSpecialAbility(Item.type, CrossMod.SummonersShineDefaultSpecialWhitelistType.MELEE);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 32;
			Item.height = 32;
			Item.damage = 34;
			Item.value = Item.buyPrice(0, 15, 0, 0);
			Item.rare = ItemRarityID.LightRed;
		}
		public override void AddRecipes()
		{
			CreateRecipe(1).AddIngredient(ItemID.SoulofLight, 10).AddIngredient(ItemID.PixieDust, 15).AddIngredient(ItemID.StarinaBottle, 5).AddTile(TileID.Anvils).Register();
		}
	}

	public class WhackAMoleMinionProjectile : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			Main.projFrames[Projectile.type] = 1;
			ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
			ProjectileID.Sets.MinionShot[Projectile.type] = true;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.penetrate = 2;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 60;
			Projectile.usesLocalNPCImmunity = true;
		}

		public override void AI()
		{
			Projectile.rotation += 0.25f;
		}

		public override void Kill(int timeLeft)
		{
			int dustIdx = Dust.NewDust(Projectile.Center, 8, 8, 192, newColor: WhackAMoleMinion.shades[(int)Projectile.ai[0]], Scale: 1.2f);
			Main.dust[dustIdx].velocity = Projectile.velocity / 2;
			Main.dust[dustIdx].noLight = false;
			Main.dust[dustIdx].noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition,
				texture.Bounds, WhackAMoleMinion.shades[(int)Projectile.ai[0]], Projectile.rotation,
				texture.Bounds.Center.ToVector2(), 1, 0, 0);
			return false;
		}
	}

	public class WhackAMoleCounterMinion : CounterMinion
	{

		public override int BuffId => BuffType<WhackAMoleMinionBuff>();
		protected override int MinionType => ProjectileType<WhackAMoleMinion>();
	}
	public class WhackAMoleMinion : EmpoweredMinion
	{

		public override int BuffId => BuffType<WhackAMoleMinionBuff>();
		public override int CounterType => ProjectileType<WhackAMoleCounterMinion>();

		protected override int dustType => DustID.Dirt;

		protected int idleGroundDistance = 128;
		protected int idleStopChasingDistance = 800;
		protected int lastHitFrame = -1;
		protected int AnimationFrames = 60;
		protected int TeleportFrames = 60;
		private int projectileIndex;
		private GroundAwarenessHelper gHelper;
		private static Vector2[] positionOffsets =
		{
			Vector2.Zero,
			new Vector2(-16, 0),
			new Vector2(-8, -14),
			new Vector2(16, 0),
			new Vector2(8, -14),
			new Vector2(0, -28)
		};

		// x offset of every mole
		private static int[] xOffsets = { 0, 8, 8, 0, 0, 0 };

		private static int[] widths = { 24, 32, 32, 48, 48, 48 };
		private static Rectangle[] platformBounds =
		{
			new Rectangle(0, 0, 28, 22),
			new Rectangle(0, 24, 44, 22),
			new Rectangle(0, 24, 44, 22),
			new Rectangle(0, 48, 52, 24),
			new Rectangle(0, 48, 52, 24),
			new Rectangle(0, 48, 52, 24),
		};

		// color of every mole
		public static Color[] shades =
		{
			new Color(101, 196, 255),
			new Color(153, 221, 146),
			new Color(255, 101, 132),
			new Color(233, 229, 146),
			new Color(173, 101, 255),
			new Color(255, 101, 244),
		};

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Whack-a-mole");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[Projectile.type] = 1;
			IdleLocationSets.trailingOnGround.Add(Projectile.type);
		}
		public override void LoadAssets()
		{
			AddTexture(Texture + "_Ground");
		}


		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
			Projectile.localNPCHitCooldown = 30;
			AttackThroughWalls = false;
			FrameSpeed = 5;
			AnimationFrame = 0;
			Projectile.hide = true;
			projectileIndex = 0;
			gHelper = new GroundAwarenessHelper(this)
			{
				ScaleLedge = ScaleLedge,
				GetUnstuck = DoTeleport,
				IdleFlyingMovement = IdleFlyingMovement,
				IdleGroundedMovement = IdleGroundedMovement
			};
			Pathfinder.modifyPath = gHelper.ModifyPathfinding;
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			gHelper.DoTileCollide(oldVelocity);
			return false;
		}

		// offset of each individual mole

		private int DrawIndex => Math.Max(0, Math.Min(shades.Length, EmpowerCount) - 1);

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
			// draw in reverse order for layering purposes
			float headBobOffset = (float)(2 * Math.PI / Math.Max(DrawIndex, 1));
			for (int i = DrawIndex; i >= 0; i--)
			{
				int offsetPixels;
				Vector2 pos = Projectile.Center;
				if (gHelper.teleportStartFrame is int teleportStart)
				{
					int teleportFrame = AnimationFrame - teleportStart;
					int teleportHalf = TeleportFrames / 2;
					float heightToSink = 24 - positionOffsets[i].Y;
					if (teleportFrame < teleportHalf)
					{
						offsetPixels = -(int)(heightToSink * teleportFrame / teleportHalf);
					}
					else
					{
						offsetPixels = -(int)(heightToSink * (TeleportFrames - teleportFrame) / teleportHalf);
					}
					pos.X += positionOffsets[i].X;
					pos.Y += positionOffsets[i].Y / 2;
				}
				else
				{
					offsetPixels = (int)(3 * Math.Sin(headBobOffset * i + 2 * Math.PI * (AnimationFrame % AnimationFrames) / AnimationFrames));
					pos += positionOffsets[i];
				}
				Rectangle bounds = new Rectangle(0, 0, 24, 24 + offsetPixels);
				pos.Y += 4 - (offsetPixels / 2);
				pos.X += xOffsets[DrawIndex];
				SpriteEffects effects = Projectile.velocity.X < 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				Main.EntitySpriteDraw(texture, pos - Main.screenPosition,
					bounds, shades[i], 0,
					bounds.Center.ToVector2(), 1, effects, 0);
			}
			DrawPlatform(ref lightColor);
			return false;
		}

		private void DrawPlatform(ref Color lightColor)
		{
			Texture2D platform = ExtraTextures[0].Value;
			Rectangle bounds = platformBounds[DrawIndex];
			Vector2 pos = Projectile.Bottom + new Vector2(0, bounds.Height / 2);
			Main.EntitySpriteDraw(platform, pos - Main.screenPosition,
				bounds, lightColor, 0, bounds.GetOrigin(), 1, 0, 0);

		}

		public override Vector2 IdleBehavior()
		{
			base.IdleBehavior();
			NoLOSPursuitTime = gHelper.isFlying ? 15 : 300;
			Vector2 idlePosition = gHelper.isFlying ? Player.Top : Player.Bottom;
			Vector2 idleHitLine = Player.Center;
			idlePosition.X += -Player.direction * IdleLocationSets.GetXOffsetInSet(IdleLocationSets.trailingOnGround, Projectile);
			if (!Collision.CanHitLine(idleHitLine, 1, 1, Player.Center, 1, 1))
			{
				idlePosition = Player.Bottom;
			}
			Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
			TeleportToPlayer(ref vectorToIdlePosition, 2000f);
			gHelper.SetIsOnGround();
			if (gHelper.offTheGroundFrames == 20 || (gHelper.offTheGroundFrames > 60 && Main.rand.NextBool(120)) || (gHelper.isOnGround && gHelper.offTheGroundFrames > 20))
			{
				DrawPlatformDust();
			}
			Lighting.AddLight(Projectile.Center, Color.PaleGreen.ToVector3() * 0.75f);
			return vectorToIdlePosition;
		}

		private void DrawPlatformDust()
		{
			for (int i = 0; i < 3; i++)
			{
				Dust.NewDust(Projectile.Bottom - new Vector2(8, 0), 16, 16, 47, -Projectile.velocity.X / 2, -Projectile.velocity.Y / 2);
			}
		}


		public override void IdleMovement(Vector2 vectorToIdlePosition)
		{
			gHelper.DoIdleMovement(vectorToIdlePosition, VectorToTarget, ComputeSearchDistance(), idleGroundDistance);
		}

		public void IdleFlyingMovement(Vector2 vectorToIdlePosition)
		{
			Projectile.tileCollide = false;
			base.IdleMovement(vectorToIdlePosition);
		}

		private void DoTeleport(Vector2 destination, int startFrame, ref bool done)
		{
			Projectile.velocity = Vector2.Zero;
			int teleportFrame = AnimationFrame - startFrame;
			int width = widths[DrawIndex];
			if (teleportFrame == 1 || teleportFrame == 1 + TeleportFrames / 2)
			{
				Collision.HitTiles(Projectile.Bottom + new Vector2(-width / 2, 8), new Vector2(0, 8), width, 8);
			}
			if (teleportFrame == TeleportFrames / 2)
			{
				// do the actual teleport
				Projectile.position = destination;
			}
			else if (teleportFrame >= TeleportFrames)
			{
				done = true;
			}
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{
			behindNPCsAndTiles.Add(index);
		}

		public void IdleGroundedMovement(Vector2 vectorToIdlePosition)
		{
			StuckInfo info = gHelper.GetStuckInfo(vectorToIdlePosition);
			if (info.isStuck)
			{
				gHelper.GetUnstuckByTeleporting(info, vectorToIdlePosition);
			}
			gHelper.ApplyGravity();
			if (vectorToIdlePosition.Y < -Projectile.height && Math.Abs(vectorToIdlePosition.X) < 96)
			{
				gHelper.DoJump(vectorToIdlePosition);
			}
			if (AnimationFrame - lastHitFrame > 10)
			{
				float intendedY = Projectile.velocity.Y;
				base.IdleMovement(vectorToIdlePosition);
				Projectile.velocity.Y = intendedY;
			}
		}

		public override void OnHitTarget(NPC target)
		{
			lastHitFrame = AnimationFrame;
		}

		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
		{
			fallThrough = false;
			return true;
		}

		public override void TargetedMovement(Vector2 vectorToTargetPosition)
		{
			int attackRate = Math.Max(40, 65 - 5 * EmpowerCount);
			bool isAttackFrame = Player.whoAmI == Main.myPlayer && AnimationFrame % attackRate == 0;
			bool canHitTarget = isAttackFrame && Collision.CanHit(Projectile.Center, 1, 1, Projectile.Center + vectorToTargetPosition, 1, 1);
			bool isAbove = isAttackFrame && Math.Abs(vectorToTargetPosition.X) < 160 && vectorToTargetPosition.Y < -24;
			bool isAttackingFromAir = isAttackFrame && gHelper.isFlying;
			if (Player.whoAmI == Main.myPlayer && TargetNPCIndex is int targetIdx && isAttackFrame && canHitTarget && (isAbove || isAttackingFromAir))
			{
				Vector2 velocity = vectorToTargetPosition;
				velocity.SafeNormalize();
				velocity *= 12;
				velocity.X += Main.npc[targetIdx].velocity.X;
				Projectile.NewProjectile(
					Projectile.GetSource_FromThis(),
					Projectile.Center,
					VaryLaunchVelocity(velocity),
					ProjectileType<WhackAMoleMinionProjectile>(),
					Projectile.damage,
					Projectile.knockBack,
					Player.whoAmI,
					projectileIndex);
				projectileIndex = (projectileIndex + 1) % (DrawIndex + 1);
			}
			if (gHelper.isFlying)
			{
				// try to stay below target while flying at it
				vectorToTargetPosition.Y += 48;
			}
			IdleMovement(vectorToTargetPosition);
		}

		protected override int ComputeDamage()
		{
			return (int)(baseDamage + (baseDamage / 3) * EmpowerCountWithFalloff());
		}

		private Vector2? GetTargetVector()
		{
			float searchDistance = ComputeSearchDistance();
			if (PlayerTargetPosition(searchDistance, Player.Center, noLOSRange: searchDistance) is Vector2 target)
			{
				return target - Projectile.Center;
			}
			else if (SelectedEnemyInRange(searchDistance) is Vector2 target2)
			{
				return target2 - Projectile.Center;
			}
			else
			{
				return null;
			}
		}
		public override Vector2? FindTarget()
		{
			return GetTargetVector();
		}

		protected override float ComputeSearchDistance()
		{
			return 800 + 25 * EmpowerCount;
		}

		protected override float ComputeInertia()
		{
			return 5;
		}

		protected override float ComputeTargetedSpeed()
		{
			// ComputeTargetedSpeed is never called 
			// since the same AI is used for targetted and non-targetted movement
			return ComputeIdleSpeed();
		}

		protected override float ComputeIdleSpeed()
		{
			return gHelper.isFlying ? 13 : 8 + (VectorToTarget == null ? 0 : Math.Min(2, 0.5f * EmpowerCount));
		}

		protected override void SetMinAndMaxFrames(ref int minFrame, ref int maxFrame)
		{
			minFrame = 0;
			maxFrame = 0;
		}

		public override void AfterMoving()
		{
			gHelper.SetOffTheGroundFrames();
		}

		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			base.Animate(minFrame, maxFrame);
			if (Math.Abs(Projectile.velocity.X) > 4)
			{
				Projectile.spriteDirection = Projectile.velocity.X > 0 ? -1 : 1;
			}
		}

		public bool ScaleLedge(Vector2 vectorToIdlePosition)
		{
			Projectile.velocity.Y = -4;
			gHelper.isOnGround = false;
			if (gHelper.offTheGroundFrames < 20)
			{
				gHelper.offTheGroundFrames = 20;
			}
			return true;
		}
	}
}
