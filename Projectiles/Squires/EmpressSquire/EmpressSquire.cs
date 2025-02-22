using AmuletOfManyMinions.Core;
using AmuletOfManyMinions.Core.Minions.Effects;
using AmuletOfManyMinions.Projectiles.Minions;
using AmuletOfManyMinions.Projectiles.Squires.SquireBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Squires.EmpressSquire
{
	public class EmpressSquireMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<EmpressSquireMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Crowned by the Empress of Light");
			Description.SetDefault("Check THIS out!");
		}
	}

	public class EmpressSquireMinionItem : SquireMinionItem<EmpressSquireMinionBuff, EmpressSquireMinion>
	{
		protected override string SpecialName => "Spectrum Supreme";
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Chalice of the Empress");
			Tooltip.SetDefault("Summons a squire\nThe priestess of light will fight for you!\nClick and hold to guide its attacks");
		}
		
		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.knockBack = 5f;
			Item.width = 24;
			Item.height = 38;
			Item.damage = 102;
			Item.value = Item.sellPrice(0, 10, 0, 0);
			Item.rare = ItemRarityID.Yellow;
		}
	}


	public class EmpressSquireMinion : WeaponHoldingSquire
	{
		internal override int BuffId => BuffType<EmpressSquireMinionBuff>();
		protected override int ItemType => ItemType<EmpressSquireMinionItem>();
		protected override int AttackFrames => 24;
		protected override string WingTexturePath => "AmuletOfManyMinions/Projectiles/Squires/Wings/AngelWings";
		protected override string WeaponTexturePath => Texture + "_Staff";

		protected override WeaponAimMode aimMode => WeaponAimMode.TOWARDS_MOUSE;

		protected override Vector2 WingOffset => new Vector2(-4, 0);

		protected override Vector2 WeaponCenterOfRotation => new Vector2(0, 4);

		protected override float projectileVelocity => 16;

		protected override SoundStyle? attackSound => SoundID.Item9 with { Volume = 0.33f };
		protected override SoundStyle? SpecialStartSound => SoundID.Item163;

		protected override int SpecialCooldown => 12 * 60;

		protected override int SpecialDuration => 6 * 60;

		public Color trailColor { get; private set; }

		private MotionBlurDrawer blurDrawer;
		private Texture2D solidTexture;
		private Texture2D solidWeaponTexture;

		private float weaponAngleOffset;

		private readonly static Color[] TrailColors = { new(247, 120, 224), new(255, 250, 60), new(112, 180, 255), };

		public readonly static Color[] SpecialColors = { 
			Color.Tomato,
			Color.Orange,
			new(255, 250, 60),
			Color.MediumSpringGreen,
			new(112, 180, 255),
			Color.MediumOrchid
		};


		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Priestess of Light");
			Main.projFrames[Projectile.type] = 5;
		}


		private Color InterpolateColorWheel(Color[] steps, float angle)
		{
			float normalAngle = angle % MathHelper.TwoPi;
			float radiansPerStep = MathHelper.TwoPi / steps.Length;
			int currentStep = (int)MathF.Floor(normalAngle * 1 / radiansPerStep);
			float stepFraction = (normalAngle - currentStep * radiansPerStep);
			int nextStep = currentStep == steps.Length - 1 ? 0 : currentStep + 1;
			return Color.Lerp(steps[currentStep], steps[nextStep], stepFraction);
		}

		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 30;
			Projectile.height = 32;
			DrawOriginOffsetY = -6;
			blurDrawer = new MotionBlurDrawer(5);
		}

		public override void AfterMoving()
		{
			base.AfterMoving();
			blurDrawer.Update(Projectile.Center, Projectile.velocity.LengthSquared() > 0.5f);
			// vanilla code for sparkly dust
			if (Main.rand.NextBool(12) || (Projectile.velocity.LengthSquared() > 2f && Main.rand.NextBool(3)))
			{
				int dustId = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 279, 0f, 0f, 100, default, 1);
				Main.dust[dustId].color = trailColor;
				Main.dust[dustId].velocity *= 0.3f;
				Main.dust[dustId].noGravity = true;
				Main.dust[dustId].noLight = true;
				Main.dust[dustId].fadeIn = 1f;
			}
		}
		public override void OnSpawn()
		{
			base.OnSpawn();
			// run this as late as possible, hope to avoid issues with asset loading
			if (!Main.dedServ)
			{
				solidTexture = SolidColorTexture.GetSolidTexture(Type);
				solidWeaponTexture = SolidColorTexture.GetSolidTexture("EmpressWeapon", WeaponTexture.Value);
			}
		}

		public override void SpecialTargetedMovement(Vector2 vectorToTargetPosition)
		{
			base.SpecialTargetedMovement(vectorToTargetPosition);
			// fun fact, this works on non-minions as well
			float baseAngle = MathHelper.TwoPi * animationFrame / 90f;
			List<Projectile> starProjectiles = GetMinionsOfType(ProjectileType<EmpressSpecialOrbitProjectile>());

			if(specialFrame % 18 == 0)
			{
				SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.33f }, Projectile.Center);
			}

			foreach(var starProj in starProjectiles)
			{
				float angle = baseAngle + MathHelper.TwoPi * starProj.ai[0] / SpecialColors.Length;
				float distance = 8 * Math.Min(12, specialFrame / 2);
				starProj.Center = Projectile.Center + angle.ToRotationVector2() * distance;
				if(player.whoAmI == Main.myPlayer && 
					specialFrame % 6 == 0 && (specialFrame / 6) % SpecialColors.Length == starProj.ai[0] &&
					Collision.CanHitLine(Projectile.Center,1,1,starProj.Center,1,1))
				{
					Vector2 angleVector = UnitVectorFromWeaponAngle();
					// shoot approximately at the horizon
					Vector2 launchVector = Projectile.Center + 800 * angleVector - starProj.Center;
					launchVector.SafeNormalize();
					launchVector *= ModifiedProjectileVelocity();
					Projectile.NewProjectile(
						Projectile.GetSource_FromThis(),
						starProj.Center,
						launchVector,
						ProjectileType<EmpressStarlightProjectile>(),
						3 * Projectile.damage / 4,
						Projectile.knockBack,
						Main.myPlayer,
						ai0: AIColorTransfer.FromColor(SpecialColors[(int)starProj.ai[0]]),
						ai1: 0.5f);

				}
			}
		}

		public override void OnStartUsingSpecial()
		{
			base.OnStartUsingSpecial();
			if(player.whoAmI != Main.myPlayer)
			{
				return;
			}
			for(int i = 0; i < 6; i++)
			{
				Projectile.NewProjectile(
					Projectile.GetSource_FromThis(),
					Projectile.Center, default,
					ProjectileType<EmpressSpecialOrbitProjectile>(),
					0, 0, Main.myPlayer, ai0: i);
			}
		}

		public override void OnStopUsingSpecial()
		{
			base.OnStopUsingSpecial();
			if(player.whoAmI != Main.myPlayer)
			{
				return;
			}
			List<Projectile> starProjectiles = GetMinionsOfType(ProjectileType<EmpressSpecialOrbitProjectile>());
			foreach(var starProj in starProjectiles)
			{
				starProj.Kill();
			}
		}

		public override Vector2 IdleBehavior()
		{
			trailColor = InterpolateColorWheel(usingSpecial ? SpecialColors: TrailColors, MathHelper.TwoPi * animationFrame / 90f);
			Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.5f);
			if(usingSpecial)
			{
				weaponAngleOffset = 0;
			}
			else if(animationFrame % 16 == 0)
			{
				weaponAngleOffset = Main.rand.NextFloat(-MathF.PI, MathF.PI) / 16f;
			}
			return base.IdleBehavior();
		}
		public override void StandardTargetedMovement(Vector2 vectorToTargetPosition)
		{
			base.StandardTargetedMovement(vectorToTargetPosition);
			if (attackFrame == 0 && !usingSpecial)
			{
				Vector2 angleVector = UnitVectorFromWeaponAngle();
				angleVector *= ModifiedProjectileVelocity();
				if (Main.myPlayer == player.whoAmI)
				{
					Projectile.NewProjectile(
						Projectile.GetSource_FromThis(),
						Projectile.Center,
						angleVector,
						ProjectileType<EmpressStarlightProjectile>(),
						Projectile.damage,
						Projectile.knockBack,
						Main.myPlayer,
						ai0: AIColorTransfer.FromColor(trailColor),
						ai1: 0.75f);
				}
			}
		}

		protected override float GetWeaponAngle()
		{
			return base.GetWeaponAngle() + weaponAngleOffset;
		}

		private void DrawWeaponOutline()
		{
			Rectangle bounds = GetWeaponTextureBounds(solidWeaponTexture);
			float r = SpriteRotationFromWeaponAngle();
			Vector2 pos = GetWeaponSpriteLocation();
			SpriteEffects effects = Projectile.spriteDirection == 1 ? 0 : SpriteEffects.FlipHorizontally;
			OutlineDrawer.DrawOutline(solidWeaponTexture, pos - Main.screenPosition, bounds, 
				trailColor * 0.5f, r, effects);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Rectangle bounds = new(0, 0, Projectile.width, Projectile.height - DrawOriginOffsetY);
			float scale = 1f;
			Vector2 offset = new(DrawOriginOffsetX, DrawOriginOffsetY + 3);
			SpriteEffects effects = Projectile.spriteDirection == 1 ? 0 : SpriteEffects.FlipHorizontally;
			for (int k = 0; k < blurDrawer.BlurLength; k++)
			{
				if(!blurDrawer.GetBlurPosAndColor(k, trailColor * 0.5f, out Vector2 blurPos, out Color blurColor)) { break; }
				Main.EntitySpriteDraw(solidTexture, blurPos + offset - Main.screenPosition, bounds, blurColor, 
					Projectile.rotation, bounds.GetOrigin(), scale, effects, 0);
				scale *= 0.9f;
			}
			OutlineDrawer.DrawOutline(solidTexture, Projectile.Center + offset - Main.screenPosition, bounds, 
				trailColor * 0.5f, Projectile.rotation, effects);
			if(IsAttacking())
			{
				DrawWeaponOutline();
			}
			return base.PreDraw(ref lightColor);
		}


		protected override float WeaponDistanceFromCenter() => 30;

		protected override int WeaponHitboxEnd() => 55;


		public override float MaxDistanceFromPlayer() => usingSpecial ? 600 : 400;

		public override float ComputeTargetedSpeed() => 14;

		public override float ComputeIdleSpeed() => 14;

	}
}
