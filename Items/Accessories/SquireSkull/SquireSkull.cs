﻿using AmuletOfManyMinions.Projectiles.NonMinionSummons;
using AmuletOfManyMinions.Projectiles.Squires;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AmuletOfManyMinions.Items.Accessories.SquireSkull
{
	class SquireSkullAccessory : ModItem
	{
		public override void SetStaticDefaults()
		{
			Tooltip.SetDefault("Enchants your squire with a cursed skull!\n" +
				"Slightly increases squire damage and adds a rotating debuff to squire attacks.");
			DisplayName.SetDefault("Necro Pendant");
		}

		public override void SetDefaults()
		{
			item.width = 30;
			item.height = 32;
			item.accessory = true;
			item.value = Item.sellPrice(gold: 5);
			item.rare = ItemRarityID.LightRed;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetModPlayer<SquireModPlayer>().squireSkullAccessory = true;
			player.GetModPlayer<SquireModPlayer>().squireAttackSpeedMultiplier *= 0.95f;
			player.GetModPlayer<SquireModPlayer>().squireDamageMultiplierBonus += 0.1f;
		}
	}

	class SquireSkullProjectile : SquireAccessoryMinion
	{

		int DebuffCycleFrames = 360;
		int AnimationFrames = 120;

		public override void SetStaticDefaults()
		{
			Main.projFrames[projectile.type] = 24;
		}

		private int debuffCycle => (animationFrame % DebuffCycleFrames) / (DebuffCycleFrames / 3);


		public override Vector2 IdleBehavior()
		{
			base.IdleBehavior();
			if(debuffCycle == 0)
			{
				squirePlayer.squireDebuffOnHit = BuffID.Bleeding;
				squirePlayer.squireDebuffTime = 180;
				Lighting.AddLight(projectile.position, Color.Red.ToVector3() * 0.25f);
			} else if (debuffCycle == 1)
			{
				squirePlayer.squireDebuffOnHit = BuffID.OnFire;
				squirePlayer.squireDebuffTime = 180;
				Lighting.AddLight(projectile.position, Color.Orange.ToVector3() * 0.25f);
			} else
			{
				squirePlayer.squireDebuffOnHit = BuffID.Poisoned;
				squirePlayer.squireDebuffTime = 180;
				Lighting.AddLight(projectile.position, Color.Aquamarine.ToVector3() * 0.25f);
			}
			int angleFrame = animationFrame % AnimationFrames;
			float angle = 2 * (float)(Math.PI * angleFrame) / AnimationFrames;
			Vector2 angleVector = 32 * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
			return (squire.Center + angleVector) - projectile.Center;
		}

		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			if(debuffCycle == 0)
			{

				minFrame = 0;
				maxFrame = 8;
			} else if (debuffCycle == 1)
			{
				minFrame = 8;
				maxFrame = 16;
			} else
			{
				minFrame = 16;
				maxFrame = 24;
			}
			base.Animate(minFrame, maxFrame);
		}

		protected override bool IsEquipped(SquireModPlayer player)
		{
			return player.squireSkullAccessory;
		}
	}
}