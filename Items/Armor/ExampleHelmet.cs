using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Terraria.ID;

namespace DemoMod.Items.Armor
{
	[AutoloadEquip(EquipType.Head)]
	public class ExampleHelmet : ModItem
	{
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Butterfly Helmet");
			Tooltip.SetDefault(""
				+ "3% increased minion damge"
				+ "+1 minion knockback");
		}

		public override void SetDefaults() {
			item.width = 18;
			item.height = 18;
			item.value = 10000;
			item.rare = ItemRarityID.White;
			item.defense = 2;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ItemType<ExampleBreastplate>() && legs.type == ItemType<ExampleLeggings>();
		}

		public override void UpdateEquip(Player player) {
			player.minionDamageMult += 0.05f;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "+1 maximum minions";
			player.maxMinions++;
		}
	}
}