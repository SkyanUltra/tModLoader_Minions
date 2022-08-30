﻿using AmuletOfManyMinions.Core.Minions.Effects;
using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.BoneSerpent
{
	public class BoneSerpentMinionBuff : MinionBuff
	{
		internal override int[] ProjectileTypes => new int[] { ProjectileType<BoneSerpentCounterMinion>() };
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Bone Serpent");
			Description.SetDefault("A skeletal draconic serpent will fight for you!");
		}
	}

	public class BoneSerpentMinionItem : MinionItem<BoneSerpentMinionBuff, BoneSerpentCounterMinion>
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Bone Serpent Staff");
			Tooltip.SetDefault("Summons a skeletal draconic serpent to fight for you!\n"+
				"This minion empowers for each additional slot expended on it");

		}
		public override void ApplyCrossModChanges()
		{
			CrossMod.WhitelistSummonersShineMinionDefaultSpecialAbility(Item.type, CrossMod.SummonersShineDefaultSpecialWhitelistType.MELEE);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.knockBack = 0.5f;
			Item.mana = 10;
			Item.width = 32;
			Item.damage = 30;
			Item.height = 32;
			Item.value = Item.buyPrice(0, 1, 0, 0);
			Item.rare = ItemRarityID.Orange;
		}
	}

	public class BoneSerpentCounterMinion : CounterMinion
	{
		public override int BuffId => BuffType<BoneSerpentMinionBuff>();
		protected override int MinionType => ProjectileType<BoneSerpentMinion>();
	}

	public abstract class GroundTravellingWormMinion: WormMinion
	{
		internal int framesInAir;
		internal int framesInGround;
		internal GroundAwarenessHelper gHelper;
		internal int maxFramesInAir = 60;
		internal int idlingFrames;
		internal int idleRadius = 80;
		public override void SetDefaults()
		{
			base.SetDefaults();
			AttackThroughWalls = true;
			framesInAir = 0;
			framesInGround = 0;
			gHelper = new GroundAwarenessHelper(this);
		}

		public override void IdleMovement(Vector2 vectorToIdlePosition)
		{
			idlingFrames++;
			if(idlingFrames > 240)
			{
				framesInAir = 0; // reset poor air movement after spending long enough in the air
			}
			if (framesInAir < 2 * maxFramesInAir + 10 || Vector2.Distance(Player.Center, Projectile.Center) > 300f ||
				Math.Abs(Player.Center.Y - Projectile.Center.Y) > 80f)
			{
				base.IdleMovement(vectorToIdlePosition);
			}
		}

		public override void TargetedMovement(Vector2 vectorToTargetPosition)
		{
			if (framesInAir > maxFramesInAir && vectorToTargetPosition.Y < 0)
			{
				vectorToTargetPosition.Y = 0;
			}
			if (framesInAir < 2 * maxFramesInAir + 10)
			{
				base.TargetedMovement(vectorToTargetPosition);
			}
		}

		public override Vector2 IdleBehavior()
		{
			if (!gHelper.InTheGround(Projectile.Center))
			{
				framesInAir++;
				framesInGround = 0;
				Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.25f);
			}
			else
			{
				framesInGround++;
				framesInAir = 0;
			}
			if (framesInAir > 60 && Projectile.velocity.Y < 16)
			{
				Projectile.velocity.Y += 0.5f;
			}
			base.IdleBehavior();
			Vector2 idlePosition = Player.Top;
			int radius = Math.Abs(Player.velocity.X) < 4 ? idleRadius : 24;
			float idleAngle = IdleLocationSets.GetAngleOffsetInSet(IdleLocationSets.circlingHead, Projectile)
				+ 2 * MathHelper.Pi * GroupAnimationFrame / GroupAnimationFrames;
			idlePosition.X += radius * (float)Math.Cos(idleAngle);
			idlePosition.Y += radius * (float)Math.Sin(idleAngle);
			Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
			TeleportToPlayer(ref vectorToIdlePosition, 2000f);
			return vectorToIdlePosition;
		}

		public override Vector2? FindTarget()
		{
			float searchDistance = ComputeSearchDistance();
			if (PlayerTargetPosition(searchDistance, Player.Center, searchDistance * 0.67f, losCenter: Player.Center) is Vector2 target)
			{
				return target - Projectile.Center;
			}
			else if (SelectedEnemyInRange(searchDistance, searchDistance * 0.67f, losCenter: Player.Center) is Vector2 target2)
			{
				return target2 - Projectile.Center;
			}
			else
			{
				return null;
			}
		}
	}

	public class BoneSerpentMinion : GroundTravellingWormMinion
	{
		public override int BuffId => BuffType<BoneSerpentMinionBuff>();

		protected override int dustType => 30;
		public override int CounterType => ProjectileType<BoneSerpentCounterMinion>();

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Bone Serpent");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[Projectile.type] = 1;
		}


		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			idleRadius = 90;
			wormDrawer = new BoneSerpentDrawer();
		}

		protected override float ComputeSearchDistance()
		{
			return 500 + 25 * GetSegmentCount();
		}

		protected override float ComputeInertia()
		{
			if (framesInAir > 0 || framesInGround < 15)
			{
				return 30;
			}
			else
			{
				return Math.Max(16, 25 - GetSegmentCount());
			}
		}

		protected override float ComputeTargetedSpeed()
		{
			return Math.Min(12, 4 + 2 * GetSegmentCount());

		}

		protected override float ComputeIdleSpeed()
		{
			return ComputeTargetedSpeed() + 3;
		}
	}

	internal class BoneSerpentDrawer : WormDrawer
	{
		protected override void DrawHead()
		{
			Rectangle head = new Rectangle(56, 0, 48, 36);
			AddSprite(2, head);
		}

		protected override void DrawTail()
		{
			lightColor = new Color(
				Math.Max(lightColor.R, (byte)25),
				Math.Max(lightColor.G, (byte)25),
				Math.Max(lightColor.B, (byte)25));
			Rectangle tail = new Rectangle(0, 10, 24, 22);
			int dist = 32 + 20 * (SegmentCount + 1);
			AddSprite(dist, tail);
		}

		protected override void DrawBody()
		{
			Rectangle body = new Rectangle(28, 6, 24, 30);
			for (int i = 0; i < SegmentCount + 1; i++)
			{
				AddSprite(32 + 20 * i, body);
			}

		}


	}
}
