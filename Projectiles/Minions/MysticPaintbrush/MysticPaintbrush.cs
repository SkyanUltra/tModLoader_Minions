﻿using AmuletOfManyMinions.Core;
using AmuletOfManyMinions.Items.Accessories;
using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.MysticPaintbrush
{
	public class MysticPaintbrushMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<MysticPaintbrushMinion>(), ProjectileType<MysticPaintbrushMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Mystic Palette");
			Description.SetDefault("An ethereal paintbrush will fight for you!");
		}
	}

	public class MysticPaintbrushMinionItem : MinionItem<MysticPaintbrushMinionBuff, MysticPaintbrushMinion>
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Mystic Palette");
			Tooltip.SetDefault("Summons an ethereal paintbrush to fight for you!\n"+
				"Can detect enemies through walls");
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.damage = 14;
			Item.knockBack = 0.5f;
			Item.mana = 10;
			Item.width = 34;
			Item.height = 34;
			Item.value = Item.buyPrice(0, 5, 0, 0);
			Item.rare = ItemRarityID.Blue;
		}
	}


	public class MysticPaintbrushMinion : TeleportingWeaponMinion
	{

		public override int BuffId => BuffType<MysticPaintbrushMinionBuff>();
		float windUpPerFrame = MathHelper.Pi / 60;
		float swingPerFrame = MathHelper.Pi / 20;
		float initialWindUp = MathHelper.PiOver4;

		protected override int searchDistance => 600;
		protected override int noLOSSearchDistance => 0;

		private Vector2 swingCenter = default;

		public override string GlowTexture => null;

		protected Color brushColor;
		protected override Vector3 lightColor => brushColor.ToVector3() * 0.75f;
		protected override int maxFramesInAir => 20;
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Mystic Palette");
		}

		public override void LoadAssets()
		{
			AddTexture(Texture + "_Glow");
		}


		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.tileCollide = false;
			AttackState = AttackState.IDLE;
			Projectile.minionSlots = 1;
			attackFrames = 90;
			AttackThroughWalls = true;
			UseBeacon = false;
			travelVelocity = 16;
			maxPhaseFrames = 30;
			targetIsDead = false;
		}


		public override void OnSpawn()
		{
			brushColor = Player.GetModPlayer<MinionSpawningItemPlayer>().GetNextColor();
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int alpha = 128;
			float phaseLength = maxPhaseFrames / 2;
			if (phaseFrames > 0 && phaseFrames < phaseLength)
			{
				alpha -= (int)(128 * phaseFrames / phaseLength);
			}
			else if (phaseFrames >= phaseLength && phaseFrames < maxPhaseFrames)
			{
				alpha = (int)(128 * (phaseFrames - phaseLength) / phaseLength);
			}
			lightColor = Color.White;
			Color translucentColor = new Color(lightColor.R, lightColor.G, lightColor.B, alpha);
			Color glowColor = new Color(brushColor.R, brushColor.G, brushColor.B, alpha);
			Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
			Texture2D glowTexture = ExtraTextures[0].Value;



			int height = texture.Height / Main.projFrames[Projectile.type];
			Rectangle bounds = new Rectangle(0, Projectile.frame * height, texture.Width, height);
			float r = Projectile.spriteDirection == 1 ? Projectile.rotation - MathHelper.PiOver4 : Projectile.rotation + MathHelper.PiOver4;
			SpriteEffects effects = Projectile.spriteDirection == 1 ? 0 : SpriteEffects.FlipHorizontally;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition,
				bounds, translucentColor, r, bounds.GetOrigin(), 1, effects, 0);

			Main.EntitySpriteDraw(glowTexture, Projectile.Center - Main.screenPosition,
				bounds, glowColor, r, bounds.GetOrigin(), 1, effects, 0);
			return false;
		}

		public override void WindUpBehavior(ref Vector2 vectorToTargetPosition)
		{
			//TODO void knife ai
			//This section might require a slight change of the behavior regarding the teleporting to work properly for MP
			//Randomized stuff should only be decided by the client
			//That would require a change of the ai so it doesnt move for other clients during this phase
			float swingDistance = 80;
			if (Main.myPlayer == Player.whoAmI && distanceFromFoe == default)
			{
				distanceFromFoe = swingDistance + Main.rand.Next(-20, 20); ;
				teleportAngle = Main.rand.NextFloat(MathHelper.TwoPi);
				Projectile.netUpdate = true;
				//Don't change position continuously, bandaid fix until a proper way for it to work in MP is figured out
			}
			else if (distanceFromFoe != default)
			{
				int swingFrame = phaseFrames - maxPhaseFrames / 2;
				// move to fixed position relative to NPC, preDraw will do phase in animation
				teleportDirection = teleportAngle.ToRotationVector2();
				swingCenter = targetNPC.Center + teleportDirection * distanceFromFoe;
				if (Projectile.minionPos % 2 == 0)
				{
					float swingAngle = (teleportAngle + MathHelper.Pi + initialWindUp + windUpPerFrame * swingFrame);
					Vector2 swingAngleVector = swingAngle.ToRotationVector2();
					Projectile.rotation = swingAngle + MathHelper.PiOver2;
					Projectile.Center = swingCenter + swingAngleVector * distanceFromFoe;
				}
				else
				{
					Projectile.rotation = teleportAngle + 3 * MathHelper.PiOver2;
					Projectile.Center = swingCenter + teleportDirection * phaseFrames;
				}
			}
		}

		public override void SwingBehavior(ref Vector2 vectorToTargetPosition)
		{
			if (framesInAir++ > maxFramesInAir)
			{
				targetNPC = null;
				AttackState = AttackState.RETURNING;
				return;
			}

			if (targetNPC != null && !targetIsDead)
			{
				swingCenter = targetNPC.Center + teleportDirection * distanceFromFoe;
			}
			teleportDirection = teleportAngle.ToRotationVector2();
			if (Projectile.minionPos % 2 == 0)
			{
				// move to fixed position relative to NPC, preDraw will do phase in animation
				float swingAngle = (teleportAngle + MathHelper.Pi + initialWindUp + windUpPerFrame * maxPhaseFrames / 2 - swingPerFrame * framesInAir);
				Vector2 swingAngleVector = swingAngle.ToRotationVector2();
				Projectile.rotation = swingAngle + MathHelper.PiOver2;
				Projectile.Center = swingCenter + swingAngleVector * distanceFromFoe;
			}
			else
			{
				vectorToTargetPosition.Normalize();
				Projectile.position = swingCenter + teleportDirection * (-12 * framesInAir + maxPhaseFrames);
				Projectile.rotation = teleportAngle + 3 * MathHelper.PiOver2;
			}
			Color dustColor = brushColor;
			dustColor.A = 200;
			int dustIdx = Dust.NewDust(Projectile.Center, 8, 8, 192, newColor: dustColor, Scale: 1.2f);
			Main.dust[dustIdx].velocity = Vector2.Zero;
			Main.dust[dustIdx].noLight = false;
			Main.dust[dustIdx].noGravity = true;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(teleportAngle);
			writer.Write(distanceFromFoe);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			teleportAngle = reader.ReadSingle();
			distanceFromFoe = reader.ReadSingle();
		}
	}
}
