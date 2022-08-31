﻿using AmuletOfManyMinions.Core;
using AmuletOfManyMinions.Projectiles.Minions.GoblinGunner;
using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.GoblinTechnomancer
{
	public class GoblinTechnomancerMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<GoblinTechnomancerMinion>(), ProjectileType<GoblinTechnomancerProbeMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Goblin Technomancer");
			Description.SetDefault("The goblin technomancer will fight for you!");
		}
	}

	public class GoblinTechnomancerMinionItem : MinionItem<GoblinTechnomancerMinionBuff, GoblinTechnomancerProbeMinion>
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Shadowflame Probe Controller");
			Tooltip.SetDefault("Summons a goblin technomancer to fight for you!\n"+
				"This minion empowers for each additional slot expended on it");
		}
		
		public override void ApplyCrossModChanges()
		{
			CrossMod.WhitelistSummonersShineMinionDefaultSpecialAbility(Item.type, CrossMod.SummonersShineDefaultSpecialWhitelistType.RANGED);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.damage = 58;
			Item.knockBack = 5.5f;
			Item.mana = 10;
			Item.width = 28;
			Item.height = 28;
			Item.value = Item.buyPrice(platinum: 2);
			Item.rare = ItemRarityID.Yellow;
		}
	}

	// Uses LocalAI[0] to indicate whether the projectile is close to its orbit position
	public class GoblinTechnomancerProbeMinion : HeadCirclingGroupAwareMinion
	{
		public override int BuffId => BuffType<GoblinTechnomancerMinionBuff>();
		int lastShootFrame = 0;

		bool isCloseToCenter
		{
			get => Projectile.localAI[0] == 1;
			set => Projectile.localAI[0] = value ? 1 : 0;
		}

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Goblin Technomancer Probe");
			Main.projFrames[Projectile.type] = 1;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 16;
			Projectile.height = 16;
			attackFrames = 30;
			Projectile.timeLeft = 3;
			maxSpeed = 14;
			idleInertia = 1;
			targetSearchDistance = 950;
			DealsContactDamage = false;
			circleHelper.idleCircle = 20;
			circleHelper.idleCircleHeight = 8;
			circleHelper.IdleBumble = false;
			circleHelper.MyGetIdleSpaceSharingMinions = GetIdleSpaceSharingMinions;
			circleHelper.GetCenterOfRotation = CenterOfRotation;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			return !isCloseToCenter;
		}

		public List<Projectile> GetIdleSpaceSharingMinions()
		{
			return GetMinionsOfType(Projectile.type);
		}

		public Vector2 CenterOfRotation()
		{
			Projectile center = GetMinionsOfType(ProjectileType<GoblinTechnomancerMinion>()).FirstOrDefault();
			return center == default ? Player.Top : center.Bottom + new Vector2(0, 4);
		}

		public override void AfterMoving()
		{
			// Lifted from EmpoweredMinion.cs
			int minionType = ProjectileType<GoblinTechnomancerMinion>();
			if (Player.whoAmI == Main.myPlayer && Player.ownedProjectileCounts[minionType] == 0 && IsPrimaryFrame)
			{
				// hack to prevent multiple 
				if (GetMinionsOfType(Projectile.type)[0].whoAmI == Projectile.whoAmI)
				{
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Player.Top, Vector2.Zero, minionType, Projectile.damage, Projectile.knockBack, Main.myPlayer);
				}
			}
		}

		public override void IdleMovement(Vector2 VectorToIdlePosition)
		{
			isCloseToCenter = VectorToIdlePosition.LengthSquared() < 16 * 16;
			base.IdleMovement(VectorToIdlePosition);
		}

		public override void TargetedMovement(Vector2 VectorToTargetPosition)
		{
			int travelSpeed = 14;
			int projectileVelocity = 20;
			int inertia = 10;
			Projectile.spriteDirection = 1;
			Projectile.rotation = (-VectorToTargetPosition).ToRotation();
			Vector2 lineOfFire = VectorToTargetPosition;
			Vector2 oppositeVector = -VectorToTargetPosition;
			oppositeVector.SafeNormalize();
			float targetDistanceFromFoe = 64f;
			if (TargetNPCIndex is int targetIdx && Main.npc[targetIdx].active)
			{
				// use the average of the width and height to get an approximate "radius" for the enemy
				NPC npc = Main.npc[targetIdx];
				Rectangle hitbox = npc.Hitbox;
				targetDistanceFromFoe += (hitbox.Width + hitbox.Height) / 4;
			}
			VectorToTargetPosition += targetDistanceFromFoe * oppositeVector;
			if (Player.whoAmI == Main.myPlayer && IsMyTurn() &&
				AnimationFrame - lastShootFrame >= attackFrames &&
				VectorToTargetPosition.LengthSquared() < 96 * 96)
			{
				lineOfFire.SafeNormalize();
				lineOfFire *= projectileVelocity;
				lastShootFrame = AnimationFrame;
				Projectile.NewProjectile(
					Projectile.GetSource_FromThis(),
					Projectile.Center,
					VaryLaunchVelocity(lineOfFire),
					ProjectileType<GoblinGunnerBullet>(),
					Projectile.damage,
					Projectile.knockBack,
					Main.myPlayer);
				SoundEngine.PlaySound(SoundID.Item11, Projectile.position);
			}
			DistanceFromGroup(ref VectorToTargetPosition);
			if (VectorToTargetPosition.Length() > travelSpeed)
			{
				VectorToTargetPosition.SafeNormalize();
				VectorToTargetPosition *= travelSpeed;
			}
			Projectile.velocity = (Projectile.velocity * (inertia - 1) + VectorToTargetPosition) / inertia;
		}
	}

	public class GoblinTechnomancerMinion : EmpoweredMinion
	{
		public override int BuffId => BuffType<GoblinTechnomancerMinionBuff>();
		public override int CounterType => ProjectileType<GoblinTechnomancerProbeMinion>();

		private int framesSinceLastHit;
		protected override int dustType => DustID.Shadowflame;

		private Vector2 lastShotVector;

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Goblin Technomancer");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[Projectile.type] = 1;
			IdleLocationSets.trailingInAir.Add(Projectile.type);
		}
		public override void LoadAssets()
		{
			AddTexture(Texture + "_Arms");
			AddTexture(Texture + "_Gun");
		}

		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 42;
			Projectile.tileCollide = false;
			framesSinceLastHit = 0;
			DealsContactDamage = false;
			AttackThroughWalls = true;
			UseBeacon = false;
		}

		private void DrawProbes(Color lightColor, int spriteDirectionFilter)
		{
			List<Projectile> closeProbes = GetMinionsOfType(CounterType)
				.Where(p => p.localAI[0] == 1 && p.spriteDirection == spriteDirectionFilter)
				.ToList();
			Texture2D texture = TextureAssets.Projectile[CounterType].Value;
			SpriteEffects effects = spriteDirectionFilter == -1 ? SpriteEffects.FlipHorizontally : 0;
			Rectangle bounds = texture.Bounds;
			Vector2 origin = bounds.Center.ToVector2();
			foreach (Projectile probe in closeProbes)
			{
				Main.EntitySpriteDraw(texture, probe.Center - Main.screenPosition,
					bounds, lightColor, probe.rotation,
					origin, 1, effects, 0);
			}

		}


		public override bool PreDraw(ref Color lightColor)
		{
			DrawProbes(lightColor, -1);
			return true;
		}

		public override void PostDraw(Color lightColor)
		{
			Texture2D texture = ExtraTextures[0].Value;
			int frame = 0;
			float shootSlope = default;
			if (FramesSinceHadTarget < 30 && lastShotVector != default && framesSinceLastHit <= 30)
			{
				float denominator = Math.Max(Math.Abs(lastShotVector.X), 1);
				shootSlope = lastShotVector.Y / denominator;
			}
			if (shootSlope != default)
			{
				if (shootSlope > 0.75f)
				{
					frame = 1;
				}
				else if (shootSlope > -0.75f)
				{
					frame = 2;
				}
				else
				{
					frame = 3;
				}
				DrawWeapon(lightColor);
			}
			Rectangle bounds = new Rectangle(0, frame * texture.Height / 4, texture.Width, texture.Height / 4);
			Vector2 pos = Projectile.Center;
			SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : 0;
			Main.EntitySpriteDraw(texture, pos - Main.screenPosition,
				bounds, lightColor, Projectile.rotation,
				bounds.GetOrigin(), 1, effects, 0);

			DrawProbes(lightColor, 1);
		}

		// lifted from WeaponHoldingSquire
		private float GetWeaponAngle(Vector2 attackVector)
		{
			if (Projectile.spriteDirection == 1)
			{
				return attackVector.ToRotation();
			}
			else
			{
				// this code is rather unfortunate, but need to normalize
				// everything to [-Math.PI/2, Math.PI/2] for arm drawing to work
				float angle = (float)-Math.PI + attackVector.ToRotation();
				if (angle < -Math.PI / 2)
				{
					angle += 2 * (float)Math.PI;
				}
				return angle;
			}
		}

		private void DrawWeapon(Color lightColor)
		{
			Vector2 offset = lastShotVector;
			offset.Y *= -1;
			offset.SafeNormalize();
			Texture2D texture = ExtraTextures[1].Value;
			Rectangle bounds = new Rectangle(0, 0, texture.Width, texture.Height);
			float r = GetWeaponAngle(offset);
			Vector2 pos = Projectile.Center + new Vector2(0, 8) + 16 * offset;
			SpriteEffects effects = Projectile.spriteDirection == 1 ? 0 : SpriteEffects.FlipHorizontally;
			Main.EntitySpriteDraw(texture, pos - Main.screenPosition,
				bounds, lightColor, r, bounds.GetOrigin(), 1, effects, 0);
		}

		public override Vector2 IdleBehavior()
		{
			base.IdleBehavior();
			Vector2 idlePosition = Player.Top;
			idlePosition.X += -Player.direction * IdleLocationSets.GetXOffsetInSet(IdleLocationSets.trailingInAir, Projectile);
			idlePosition.Y += -24 + 8 * (float)Math.Sin(MathHelper.TwoPi * (AnimationFrame % 120) / 120);
			Vector2 VectorToIdlePosition = idlePosition - Projectile.Center;
			if (!Collision.CanHitLine(idlePosition, 1, 1, Player.Center, 1, 1))
			{
				idlePosition.X = Player.Top.X;
				idlePosition.Y = Player.Top.Y - 16;
			}
			TeleportToPlayer(ref VectorToIdlePosition, 2000f);
			return VectorToIdlePosition;
		}

		public override void TargetedMovement(Vector2 VectorToTargetPosition)
		{
			// stay floating behind the Player at all times
			IdleMovement(VectorToIdle);
			framesSinceLastHit++;
			int rateOfFire = Math.Max(25, 60 - 5 * EmpowerCount);
			int projectileVelocity = 40;
			if (framesSinceLastHit++ > rateOfFire && TargetNPCIndex is int npcIdx)
			{
				NPC target = Main.npc[npcIdx];
				// try to predict the position at the time of impact a bit
				lastShotVector = VectorToTargetPosition;
				lastShotVector.Y *= -1;
				VectorToTargetPosition += (VectorToTargetPosition.Length() / projectileVelocity) * target.velocity;
				VectorToTargetPosition.SafeNormalize();
				VectorToTargetPosition *= projectileVelocity;
				Vector2 pos = Projectile.Center;
				framesSinceLastHit = 0;
				Projectile.spriteDirection = VectorToTargetPosition.X > 0 ? 1 : -1;
				if (Main.myPlayer == Player.whoAmI)
				{
					Projectile.NewProjectile(
						Projectile.GetSource_FromThis(),
						pos,
						VaryLaunchVelocity(VectorToTargetPosition),
						ProjectileType<GoblinGunnerBullet>(),
						Projectile.damage,
						Projectile.knockBack,
						Main.myPlayer);
					SoundEngine.PlaySound(SoundID.Item11, Projectile.position);
				}
			}
		}

		protected override int ComputeDamage()
		{
			return (int)(baseDamage / 2 + (baseDamage / 8) * EmpowerCountWithFalloff()); // only scale up damage a little bit
		}

		private Vector2? GetTargetVector()
		{
			float searchDistance = ComputeSearchDistance();
			if (PlayerTargetPosition(searchDistance, Player.Center) is Vector2 target)
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
			Vector2? target = GetTargetVector();
			return target;
		}

		protected override float ComputeSearchDistance() => 800 + 20 * EmpowerCount;

		protected override float ComputeInertia() => 5;

		protected override float ComputeTargetedSpeed() => 16;

		protected override float ComputeIdleSpeed() => 16;

		protected override void SetMinAndMaxFrames(ref int minFrame, ref int maxFrame)
		{
			minFrame = 0;
			maxFrame = 0;
		}

		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			base.Animate(minFrame, maxFrame);
			if (Math.Abs(Projectile.velocity.X) > 2 && VectorToTarget is null)
			{
				Projectile.spriteDirection = Projectile.velocity.X > 0 ? 1 : -1;
			}
			Projectile.rotation = Projectile.velocity.X * 0.025f;
		}

		public override bool CheckActive()
		{
			if (base.CheckActive() && Player.ownedProjectileCounts[CounterType] == 0 && AnimationFrame > 2)
			{
				Projectile.Kill();
				return false;
			}

			return true;
		}
	}
}
