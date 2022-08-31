﻿using AmuletOfManyMinions.Items.Accessories;
using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.ExciteSkull
{
	public class ExciteSkullMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<ExciteSkullMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Skull Biker");
			Description.SetDefault("The skeletal motorcyclist will fight for you!");
		}
	}

	public class ExciteSkullMinionItem : MinionItem<ExciteSkullMinionBuff, ExciteSkullMinion>
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Skeletal Keychain");
			Tooltip.SetDefault("Summons a skull biker to fight for you!");
		}
		public override void ApplyCrossModChanges()
		{
			CrossMod.WhitelistSummonersShineMinionDefaultSpecialAbility(Item.type, CrossMod.SummonersShineDefaultSpecialWhitelistType.MELEE);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.damage = 19;
			Item.knockBack = 0.5f;
			Item.mana = 10;
			Item.width = 28;
			Item.height = 28;
			Item.value = Item.buyPrice(0, 1, 50, 0);
			Item.rare = ItemRarityID.Green;
		}
	}

	public class ExciteSkullMinion : SimpleGroundBasedMinion
	{
		public override int BuffId => BuffType<ExciteSkullMinionBuff>();

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("ExciteSkull");
			Main.projFrames[Projectile.type] = 4;
		}
		public override void LoadAssets()
		{
			AddTexture(Texture + "_Rider");
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 32;
			Projectile.height = 28;
			Projectile.localNPCHitCooldown = 20;
			DrawOffsetX = -2;
			attackFrames = 60;
			NoLOSPursuitTime = 300;
			StartFlyingHeight = 96;
			StartFlyingDist = 64;
			DefaultJumpVelocity = 4;
			MaxJumpVelocity = 13;
			searchDistance = 700;
		}
		public override void PostDraw(Color lightColor)
		{
			Texture2D texture;
			Vector2 pos = Projectile.Center;
			SpriteEffects effects = Projectile.spriteDirection == 1 ? 0 : SpriteEffects.FlipHorizontally;
			texture = ExtraTextures[0].Value;
			int frameHeight = texture.Height / 8;
			Rectangle bounds = new Rectangle(0, (Projectile.minionPos % 8) * frameHeight, texture.Width, frameHeight);
			Main.EntitySpriteDraw(texture, pos + new Vector2(0, -10) - Main.screenPosition,
				bounds, lightColor, 0,
				new Vector2(bounds.Width/2, bounds.Height/2), 1, effects, 0);
		}

		protected override void DoGroundedMovement(Vector2 vector)
		{
			if (vector.Y < -Projectile.height && Math.Abs(vector.X) < StartFlyingHeight)
			{
				GHelper.DoJump(vector);
			}
			float xInertia = GHelper.stuckInfo.overLedge && !GHelper.didJustLand && Math.Abs(Projectile.velocity.X) < 2 ? 1.25f : 7;
			int xMaxSpeed = 9;
			if (VectorToTarget is null && Math.Abs(vector.X) < 8)
			{
				Projectile.velocity.X = Player.velocity.X;
				return;
			}
			DistanceFromGroup(ref vector);
			if (AnimationFrame - LastHitFrame > 10)
			{
				Projectile.velocity.X = (Projectile.velocity.X * (xInertia - 1) + Math.Sign(vector.X) * xMaxSpeed) / xInertia;
			}
			else
			{
				Projectile.velocity.X = Math.Sign(Projectile.velocity.X) * xMaxSpeed * 0.75f;
			}
		}

		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			if (GHelper.didJustLand)
			{
				Projectile.rotation = 0;
			}
			else
			{
				Projectile.rotation = -Projectile.spriteDirection * MathHelper.Pi / 8;
			}
			if (Math.Abs(Projectile.velocity.X) < 1)
			{
				return;
			}
			base.Animate(minFrame, maxFrame);
			if (((GHelper.didJustLand && Math.Abs(Projectile.velocity.X) > 4) || GHelper.isFlying) && AnimationFrame % 3 == 0)
			{
				int idx = Dust.NewDust(Projectile.Bottom, 8, 8, 16, -Projectile.velocity.X / 2, -Projectile.velocity.Y / 2);
				Main.dust[idx].alpha = 112;
				Main.dust[idx].scale = .9f;
			}
		}
	}
}
