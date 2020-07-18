﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DemoMod.Projectiles.Minions
{
    public abstract class SimpleMinion<T> : Minion<T> where T : ModBuff
    {
		protected Vector2 vectorToIdle;
		protected Vector2? vectorToTarget;
		protected int? targetNPCIndex;
		protected Vector2 oldVectorToIdle;
		protected Vector2? oldVectorToTarget = null;
		public override void SetStaticDefaults() 
		{
            base.SetStaticDefaults();
			// This is necessary for right-click targeting
			ProjectileID.Sets.MinionTargettingFeature[projectile.type] = true;

			// These below are needed for a minion
			// Denotes that this projectile is a pet or minion
			Main.projPet[projectile.type] = true;
			// This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
			ProjectileID.Sets.MinionSacrificable[projectile.type] = true;
			// Don't mistake this with "if this is true, then it will automatically home". It is just for damage reduction for certain NPCs
			ProjectileID.Sets.Homing[projectile.type] = true;
		}

        public override void SetDefaults()
        {
            base.SetDefaults();
			// These below are needed for a minion weapon
			// Only controls if it deals damage to enemies on contact (more on that later)
			projectile.friendly = true;
			// Only determines the damage type
			projectile.minion = true;
			// Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
			projectile.minionSlots = 1f;
			// Needed so the minion doesn't despawn on collision with enemies or tiles
			projectile.penetrate = -1;
			// Makes the minion not go through tiles
			projectile.tileCollide = true;
        }


		// Here you can decide if your minion breaks things like grass or pots
		public override bool? CanCutTiles() {
			return false;
		}

		// This is mandatory if your minion deals contact damage (further related stuff in AI() in the Movement region)
		public override bool MinionContactDamage() {
			return true;
		}

		public abstract Vector2 IdleBehavior();
		public abstract Vector2? FindTarget();
		public abstract void IdleMovement(Vector2 vectorToIdlePosition);
		public abstract void TargetedMovement(Vector2 vectorToTargetPosition);

		public virtual void AfterMoving() { }
		public virtual void Animate(int minFrame = 0, int? maxFrame = null) {

			// This is a simple "loop through all frames from top to bottom" animation
			int frameSpeed = 5;
			projectile.frameCounter++;
			if (projectile.frameCounter >= frameSpeed) {
				projectile.frameCounter = 0;
				projectile.frame++;
				if (projectile.frame >= (maxFrame ?? Main.projFrames[projectile.type])) {
					projectile.frame = minFrame;
				}
			}
		}

        public override void Behavior()
        {
			vectorToIdle = IdleBehavior();
			vectorToTarget = FindTarget();
			if(vectorToTarget is Vector2 targetPosition)
            {
				TargetedMovement(targetPosition);
            } else
            {
                targetNPCIndex = null;
				IdleMovement(vectorToIdle);
            }
			AfterMoving();
			Animate();
			oldVectorToIdle = vectorToIdle;
			oldVectorToTarget = vectorToTarget;
        }


		// utility methods
		public void TeleportToPlayer(Vector2 vectorToIdlePosition, float maxDistance)
        {
			if(Main.myPlayer == player.whoAmI && vectorToIdlePosition.Length() > maxDistance)
            {
				projectile.position += vectorToIdlePosition;
				projectile.velocity = Vector2.Zero;
				projectile.netUpdate = true;
            }
        }

		public Vector2? PlayerTargetPosition(float maxRange, Vector2? centeredOn = null, float noLOSRange = 0)
        {
			Vector2 center = centeredOn ?? projectile.Center;
			if(player.HasMinionAttackTargetNPC)
            {
				NPC npc = Main.npc[player.MinionAttackTargetNPC];
				float distance = Vector2.Distance(npc.Center, center);
				if(distance < noLOSRange || (distance < maxRange && 
					Collision.CanHitLine(projectile.Center, projectile.width/2, projectile.height/2, npc.position, npc.width, npc.height)))
                {
					targetNPCIndex = player.MinionAttackTargetNPC;
					return npc.Center;
                }
            }
			return null;
        }
		public Vector2? ClosestEnemyInRange(float maxRange, Vector2? centeredOn = null, float noLOSRange = 0)
        {
			// don't try to find an enemy if the player already has a target
			if(player.HasMinionAttackTargetNPC)
            {
				return null;
            }

			Vector2 center = centeredOn ?? projectile.Center;
			Vector2 targetCenter = projectile.position;
			bool foundTarget = false;
			for(int i = 0; i < Main.maxNPCs; i++)
            {
				NPC npc = Main.npc[i];
				if(!npc.CanBeChasedBy())
                {
					continue;
                }
                float between = Vector2.Distance(npc.Center, center);
                bool closest = Vector2.Distance(center, targetCenter) > between;
				// don't let a minion infinitely chain attacks off progressively further enemies
                bool inRange = Vector2.Distance(npc.Center, player.Center) < maxRange;
                bool inNoLOSRange = Vector2.Distance(npc.Center, player.Center) < noLOSRange;
                bool lineOfSight =Collision.CanHitLine(projectile.Center, projectile.width/2, projectile.height/2, npc.position, npc.width, npc.height); 
				if((inNoLOSRange || (lineOfSight && inRange)) && (closest || !foundTarget))
                {
					targetNPCIndex = i;
					targetCenter = npc.Center;
					foundTarget = true;
                }
            }
			return foundTarget ? targetCenter : (Vector2?)null;
        }


        public List<Projectile> GetMinionsOfType(int projectileType)
        {
			var otherMinions = new List<Projectile>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				// Fix overlap with other minions
				Projectile other = Main.projectile[i];
				if (other.active && other.owner == projectile.owner && other.type == projectileType )
				{
					otherMinions.Add(other);
				}
			}
            otherMinions.Sort((x, y)=>x.minionPos - y.minionPos);
			return otherMinions;
        }
    }
}
