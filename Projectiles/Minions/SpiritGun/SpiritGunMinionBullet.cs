﻿using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using log4net.Repository.Hierarchy;
using log4net.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.SpiritGun
{
    class SpiritGunMinionBullet : Minion<ModBuff>
    {
        bool hitTarget;
        bool lookingForTarget;
        const int speed = 26;
        Vector2 velocity = default;
        Vector2 vectorToTarget = default;
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			Main.projFrames[projectile.type] = 1;
        }
        public override void SetDefaults()
		{
			projectile.width = 24;
			projectile.height = 2;
			projectile.friendly = true;
			projectile.penetrate = 3;
			projectile.tileCollide = true;
			projectile.timeLeft = 120;
            hitTarget = false;
            lookingForTarget = false;
            ProjectileID.Sets.Homing[projectile.type] = true;
            ProjectileID.Sets.MinionShot[projectile.type] = true;
		}
        private void LookForTarget()
        {
            if((PlayerTargetPosition(600) ?? ClosestEnemyInRange(600)) is Vector2 target)
            {
                velocity = target - projectile.Center;
                vectorToTarget = velocity;
                lookingForTarget = false;
                velocity.SafeNormalize(); 
                velocity *= speed;
                Dust.NewDust(projectile.Center, 8, 8, DustID.Confetti, -velocity.X, -velocity.Y);
            }           
        }

        public override void AI()
        {
			player = Main.player[projectile.owner];
            if(velocity == default)
            {
                velocity = new Vector2(projectile.ai[0], projectile.ai[1]);
                vectorToTarget = velocity;
                velocity.SafeNormalize();
                velocity *= speed;
            }
            if(hitTarget)
            {
                return;
            }
            if(vectorToTarget.LengthSquared() < speed * speed)
            {
                lookingForTarget = true;
            }
            if(lookingForTarget)
            {
                LookForTarget();
            }
            vectorToTarget -= velocity;
            projectile.rotation = velocity.ToRotation();
            projectile.velocity = velocity;
            Lighting.AddLight(projectile.position, Color.LightCyan.ToVector3());
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            hitTarget = true;
        }

        public override void Behavior()
        {
            // no-op
        }
    }
}