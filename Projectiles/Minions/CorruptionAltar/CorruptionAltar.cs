﻿using AmuletOfManyMinions.Projectiles.Minions.CrimsonAltar;
using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using AmuletOfManyMinions.Projectiles.NonMinionSummons;
using log4net.Repository.Hierarchy;
using log4net.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.CorruptionAltar
{
    public class CorruptionAltarMinionBuff: MinionBuff
    {
        public CorruptionAltarMinionBuff() : base(ProjectileType<CorruptionAltarMinion>()) { }
        public override void SetDefaults()
        {
            base.SetDefaults();
			DisplayName.SetDefault("Corruption Altar");
			Description.SetDefault("A corruption altar will fight for you!");
        }
    }

    public class CorruptionAltarMinionItem: EmpoweredMinionItem<CorruptionAltarMinionBuff, CorruptionAltarMinion>
    {
        protected override int dustType => DustID.Blood;

        public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			DisplayName.SetDefault("Corruption Cell Staff");
			Tooltip.SetDefault("Summons a corruption altar to fight for you!");
		}

		public override void SetDefaults() {
			base.SetDefaults();
			item.knockBack = 3f;
			item.mana = 10;
			item.width = 32;
			item.height = 32;
            item.damage = 14;
			item.value = Item.buyPrice(0, 15, 0, 0);
			item.rare = ItemRarityID.LightRed;
		}
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofNight, 6);
            recipe.AddIngredient(ItemID.TitaniumBar, 12);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }

    public abstract class CorruptionAltarBaseEater : CrimsonAltarBaseCrimera
    {
        protected override float searchDistance => 200f;

        protected override int dustType => 14;
        protected override int dustFrequency => 12;

    }
    public class CorruptionAltarBigEater : CorruptionAltarBaseEater
    {
        public override string Texture => "AmuletOfManyMinions/Projectiles/Minions/CorruptionAltar/CorruptionAltarEater";

        protected override int dustType => 89;
        public override void SetDefaults()
        {
            base.SetDefaults();
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = GetTexture(Texture);
            Rectangle bounds = new Rectangle(0, 0, 
                texture.Bounds.Width, texture.Bounds.Height / 2);
            Vector2 origin = bounds.Center.ToVector2();
            Vector2 pos = projectile.Center;
            float r = projectile.rotation;
            spriteBatch.Draw(texture, pos - Main.screenPosition,
                bounds, lightColor, r,
                origin, 1.5f, 0, 0);
            return false;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.CursedInferno, 180);
        }
    }

    public class CorruptionAltarEater : CorruptionAltarBaseEater
    {
    }
    public class CorruptionAltarMinion : EmpoweredMinion<CorruptionAltarMinionBuff>
    {

        private int framesSinceLastHit;
        private int animationFrame;

		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			DisplayName.SetDefault("Corruption Cell");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[projectile.type] = 4;
		}

		public sealed override void SetDefaults() {
			base.SetDefaults();
			projectile.width = 40;
			projectile.height = 50;
			projectile.tileCollide = false;
            projectile.type = ProjectileType<CorruptionAltarMinion>();
            projectile.ai[0] = 0;
            projectile.ai[1] = 0;
            framesSinceLastHit = 0;
            projectile.friendly = true;
            attackThroughWalls = true;
            useBeacon = false;
		}



        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if(projectile.minionSlots < 4)
            {
                return;
            }
            Texture2D texture = GetTexture(Texture + "_Glow");
            Rectangle bounds = texture.Bounds;
            Vector2 origin = bounds.Center.ToVector2();
            Vector2 pos = projectile.Center;
            float r = projectile.rotation;
            spriteBatch.Draw(texture, pos - Main.screenPosition,
                bounds, Color.White, r,
                origin, 1, 0, 0);
        }

        public override Vector2 IdleBehavior()
        {
            base.IdleBehavior();
            Vector2 idlePosition = player.Top;
            idlePosition.X += 28 * -player.direction;
            idlePosition.Y += -8;
            animationFrame+= 1;
            if(!Collision.CanHitLine(idlePosition, 1, 1, player.Center, 1, 1))
            {
                idlePosition.X = player.Top.X;
                idlePosition.Y = player.Top.Y - 16;
            }
            idlePosition.Y += 4 * (float) Math.Sin(2 * Math.PI * animationFrame / 120f);
            Vector2 vectorToIdlePosition = idlePosition - projectile.Center;
            TeleportToPlayer(ref vectorToIdlePosition, 2000f);
            Lighting.AddLight(projectile.Center, Color.Purple.ToVector3() * 0.25f);
            return vectorToIdlePosition;
        }

        public override void IdleMovement(Vector2 vectorToIdlePosition)
        {
            base.IdleMovement(vectorToIdlePosition);
        }
        public override void TargetedMovement(Vector2 vectorToTargetPosition)
        {
            // stay floating behind the player at all times
            IdleMovement(vectorToIdle);
            framesSinceLastHit++;
            int rateOfFire = Math.Max(90, 120 -  5 * (int)projectile.minionSlots);
            if(framesSinceLastHit ++ > rateOfFire)
            {
                int minionsToSpawn = Math.Max(1, Main.rand.Next(1) + (int)projectile.minionSlots - 1);
                framesSinceLastHit = 0;
                for(int i = 0; i < minionsToSpawn; i++)
                {
                    bool summonBig = projectile.minionSlots >= 4 && Main.rand.Next(4) == 0; 
                    int projType  = summonBig ? ProjectileType<CorruptionAltarBigEater>() : ProjectileType<CorruptionAltarEater>();
                    float rangeSquare = Math.Min(120, vectorToTargetPosition.Length() / 2);
                    vectorToTargetPosition.X += Main.rand.NextFloat() * rangeSquare - rangeSquare/2; 
                    vectorToTargetPosition.Y += Main.rand.NextFloat() * rangeSquare - rangeSquare/2;
                    float projectileVelocity = summonBig? 9.5f : 12.5f;
                    vectorToTargetPosition.SafeNormalize();
                    vectorToTargetPosition *= projectileVelocity;
                    Vector2 pos = projectile.Center;
                    framesSinceLastHit = 0;
                    Projectile.NewProjectile(
                        pos, 
                        vectorToTargetPosition,
                        projType,
                        projectile.damage,
                        projectile.knockBack,
                        Main.myPlayer);
                }
            }
        }

        protected override int ComputeDamage()
        {
            return baseDamage + (baseDamage/8) * (int)projectile.minionSlots; // only scale up damage a little bit
        }

        private Vector2? GetTargetVector()
        {
            float searchDistance = ComputeSearchDistance();
            if (PlayerTargetPosition(searchDistance, player.Center) is Vector2 target)
            {
                return target - projectile.Center;
            }
            else if (ClosestEnemyInRange(searchDistance, player.Center) is Vector2 target2)
            {
                return target2 - projectile.Center;
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

        protected override float ComputeSearchDistance()
        {
            return 500 + 30 * projectile.minionSlots;
        }

        protected override float ComputeInertia()
        {
            return 5;
        }

        protected override float ComputeTargetedSpeed()
        {
            return 16;
        }

        protected override float ComputeIdleSpeed()
        {
            return 16;
        }

        protected override void SetMinAndMaxFrames(ref int minFrame, ref int maxFrame)
        {
            minFrame = 0;
            maxFrame = 0;
        }

        public override void Animate(int minFrame = 0, int? maxFrame = null)
        {
            projectile.spriteDirection = 1;
            projectile.frame = Math.Min(4, (int)projectile.minionSlots) - 1;
            projectile.rotation  =  (float)(Math.PI/8 * Math.Cos(2 * Math.PI * animationFrame / 120f));

            if(Main.rand.Next(120) == 0)
            {
                for(int i = 0; i < 3; i++)
                {
                    Dust.NewDust(projectile.Center, 16, 16, 14, Main.rand.Next(6) - 3, Main.rand.Next(6) - 3);
                }
            }
        }
    }
}