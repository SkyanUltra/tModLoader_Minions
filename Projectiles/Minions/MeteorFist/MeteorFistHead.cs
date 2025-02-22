﻿using AmuletOfManyMinions.Core;
using Microsoft.Xna.Framework;
using Terraria;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.MeteorFist

{
	public class MeteorFistHead : SimpleMinion
	{
		protected int targetedInertia = 15;
		protected int targetedSpeed = 9;
		protected int maxDistanceFromPlayer = 600;
		protected int minDistanceToEnemy = 200;

		internal override int BuffId => BuffType<MeteorFistMinionBuff>();

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Meteor Fist");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[Projectile.type] = 2;
			IdleLocationSets.trailingInAir.Add(Projectile.type);
		}

		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 26;
			Projectile.height = 28;
			Projectile.tileCollide = false;
			dealsContactDamage = false;
			Projectile.minionSlots = 0f;
		}

		public override Vector2? FindTarget()
		{
			if (PlayerTargetPosition(600f, player.Center) is Vector2 target)
			{
				return target - Projectile.Center;
			}
			else if (SelectedEnemyInRange(600f) is Vector2 target2)
			{
				return target2 - Projectile.Center;
			}
			else
			{
				return null;
			}
		}

		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
		{
			fallThrough = true;
			return true;
		}
		public override Vector2 IdleBehavior()
		{
			Vector2 idlePosition = player.Top;
			idlePosition.X += -player.direction * IdleLocationSets.GetXOffsetInSet(IdleLocationSets.trailingInAir, Projectile);
			idlePosition.Y += -5;
			if (!Collision.CanHitLine(idlePosition, 1, 1, player.Center, 1, 1))
			{
				idlePosition = player.Center;
			}
			Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
			TeleportToPlayer(ref vectorToIdlePosition, 2000f);
			return vectorToIdlePosition;
		}

		public override void IdleMovement(Vector2 vectorToIdlePosition)
		{
			int inertia = 10;
			int maxSpeed = 16;
			Projectile.tileCollide = false;
			Vector2 speedChange = vectorToIdlePosition - Projectile.velocity;
			if (speedChange.Length() > maxSpeed)
			{
				speedChange.SafeNormalize();
				speedChange *= maxSpeed;
			}
			if(Projectile.velocity.X > 1)
			{
				Projectile.spriteDirection = -1;
			} else if (Projectile.velocity.X < -1)
			{
				Projectile.spriteDirection = 1;
			}
			Projectile.velocity = (Projectile.velocity * (inertia - 1) + speedChange) / inertia;
		}

		public override void TargetedMovement(Vector2 vectorToTargetPosition)
		{
			int inertia = targetedInertia;
			int maxSpeed = targetedSpeed;
			// move towards the enemy, but don't get too far from the player
			Projectile.spriteDirection = vectorToTargetPosition.X > 0 ? -1 : 1;
			Vector2 vectorFromPlayer = player.Center - Projectile.Center;
			if (vectorFromPlayer.Length() > maxDistanceFromPlayer)
			{
				vectorToTargetPosition = vectorFromPlayer;
			}
			else if (vectorToTargetPosition.Length() < minDistanceToEnemy)
			{
				vectorToTargetPosition *= -1;
			}
			vectorToTargetPosition.SafeNormalize();
			vectorToTargetPosition *= maxSpeed;
			Projectile.velocity = (Projectile.velocity * (inertia - 1) + vectorToTargetPosition) / inertia;
		}

		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			base.Animate(minFrame, maxFrame);
		}
	}
}
