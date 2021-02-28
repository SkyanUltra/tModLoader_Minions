﻿using AmuletOfManyMinions.Core.Minions.Pathfinding;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AmuletOfManyMinions.Items.Accessories.PassivePathfindingAccessories
{
	class MinionCompass : ModItem
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Compass of Many Minions");
			Tooltip.SetDefault(
				"Allows your minions to automatically attack around corners in a 15 tile radius.");
		}

		public override void SetDefaults()
		{
			item.width = 30;
			item.height = 32;
			item.accessory = true;
			item.value = Item.sellPrice(silver: 50);
			item.rare = ItemRarityID.Green;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetModPlayer<MinionPathfindingPlayer>().passivePathfindingRange = 20 * 15;
		}
	}
}
