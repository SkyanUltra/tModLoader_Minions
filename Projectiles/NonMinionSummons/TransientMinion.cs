﻿using AmuletOfManyMinions.Projectiles.Minions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AmuletOfManyMinions.Projectiles.NonMinionSummons
{
	public abstract class TransientMinion : SimpleMinion
	{
		internal override int BuffId => -1;

		internal virtual bool tileCollide => true;
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.minion = false;
			Projectile.minionSlots = 0;
			useBeacon = false;
		}

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			// This is necessary for right-click targeting
			ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = false;

			ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
			ProjectileID.Sets.MinionShot[Projectile.type] = true;

			// These below are needed for a minion
			// Denotes that this projectile is a pet or minion
			Main.projPet[Projectile.type] = false;
			// This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
			ProjectileID.Sets.MinionSacrificable[Projectile.type] = false;
			// Don't mistake this with "if this is true, then it will automatically home". It is just for damage reduction for certain NPCs
		}

		public override Vector2? FindTarget()
		{
			return null;
		}
		public override Vector2 IdleBehavior()
		{
			return Vector2.Zero;
		}

		public override void Behavior()
		{
			base.Behavior();
			Projectile.tileCollide = tileCollide;
		}

		public override void TargetedMovement(Vector2 vectorToTargetPosition)
		{
			return;
		}

		public override bool CheckActive()
		{
			// no-op
			return true;
		}
	}
}
