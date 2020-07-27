﻿using DemoMod.Projectiles.Minions.MinonBaseClasses;
using log4net.Repository.Hierarchy;
using log4net.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace DemoMod.Projectiles.Minions.MinonBaseClasses
{
    public abstract class WormMinion<T>: EmpoweredMinion<T> where T: ModBuff
    {
        private float[] backingArray;
        public CircularLengthQueue PositionLog = null;
        public int framesSinceLastHit = 0;
        private SpriteBatch spriteBatch;
        private Texture2D texture;
        private Color lightColor;
		public override void SetStaticDefaults() {
			Main.projFrames[projectile.type] = 1;
		}

        public override void SetDefaults() {
			base.SetDefaults();
            backingArray = new float[255];
            CircularVectorQueue.Initialize(backingArray);
            PositionLog = new CircularLengthQueue(backingArray, queueSize: 32)
            {
                mod = mod
            };
        }

        protected virtual SpriteEffects GetEffects(float angle)
        {
            SpriteEffects effects = SpriteEffects.FlipHorizontally;
            angle = (angle + 2 * (float)Math.PI) % (2 * (float)Math.PI); // get to (0, 2PI) range
            if(angle > Math.PI/2 && angle < 3  * Math.PI /2)
            {
                effects |= SpriteEffects.FlipVertically;
            }
            return effects;

        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            texture = Main.projectileTexture[projectile.type];
            this.spriteBatch = spriteBatch;
            this.lightColor = lightColor;

            DrawTail();
            DrawBody();
            DrawHead();

            return false;
        }

        protected abstract void DrawTail();
        protected abstract void DrawBody();
        protected abstract void DrawHead();
        protected void AddSprite(float dist, Rectangle bounds, Color c = default)
        {
            Vector2 origin = new Vector2(bounds.Width / 2f, bounds.Height / 2f);
            Vector2 angle = new Vector2();
            Vector2 pos = PositionLog.PositionAlongPath(dist, ref angle);
            float r = angle.ToRotation();
            spriteBatch.Draw(texture, pos - Main.screenPosition,
                bounds, c == default ? lightColor : c, r,
                origin, 1, GetEffects(r), 0);
        }


        protected int GetSegmentCount()
        {
            return (int)projectile.minionSlots;
        }

        public override Vector2 IdleBehavior()
        {
            base.IdleBehavior();
            projectile.ai[1] = (projectile.ai[1] + 1) % 240;
            Vector2 idlePosition = player.Top;
            idlePosition.X += 48 * (float)Math.Cos(Math.PI * projectile.ai[1] / 60);
            idlePosition.Y += -48  + 8 * (float)Math.Sin(Math.PI * projectile.ai[1] / 60);
            Vector2 vectorToIdlePosition = idlePosition - projectile.Center;
            TeleportToPlayer(vectorToIdlePosition, 2000f);
            return vectorToIdlePosition;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            framesSinceLastHit = 0;
        }

        public override void TargetedMovement(Vector2 vectorToTargetPosition)
        {
            base.TargetedMovement(vectorToTargetPosition);
            float inertia = ComputeInertia();
            float speed = ComputeTargetedSpeed();
            vectorToTargetPosition.Normalize();
            vectorToTargetPosition *= speed;
            if(framesSinceLastHit ++ > 4)
            {
                projectile.velocity = (projectile.velocity * (inertia - 1) + vectorToTargetPosition) / inertia;
            } else
            {
                projectile.velocity.Normalize();
                projectile.velocity *= speed; // kick it away from enemies that it's just hit
            }
        }

        protected override int ComputeDamage()
        {
            return baseDamage/2 + (baseDamage / 2) * GetSegmentCount();
        }

        protected override void SetMinAndMaxFrames(ref int minFrame, ref int maxFrame)
        {
            minFrame = 0;
            maxFrame = 0;
        }

        public override void AfterMoving()
        {
            base.AfterMoving();
            PositionLog.AddPosition(projectile.position);
        }

        public override void Animate(int minFrame = 0, int? maxFrame = null)
        {
            base.Animate(minFrame, maxFrame);
        }
    }
}
