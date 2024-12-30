// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaOverhaul.Common.Movement;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Content.Items.Accessories;

[AutoloadEquip(EquipType.Neck)]
public class BunnyPaw : ModItem
{
	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		var rule = ItemDropRule.Common(ModContent.ItemType<BunnyPaw>(), 100);

		Main.ItemDropsDB.RegisterToMultipleNPCs(
			rule,
			NPCID.Bunny,
			NPCID.BunnySlimed,
			NPCID.BunnyXmas,
			NPCID.CorruptBunny,
			NPCID.CrimsonBunny,
			NPCID.ExplosiveBunny,
			NPCID.PartyBunny,
			NPCID.TownBunny
		);
	}
	
	public override void SetDefaults()
	{
		// Accessory properties.
		Item.accessory = true;
		// Universal properties.
		Item.width = 32;
		Item.height = 32;
		Item.rare = ItemRarityID.Green;
		Item.scale = 0.25f;
		Item.value = Item.sellPrice(0, 0, 1);
	}

	public override void AddRecipes() => this.CreateRecipe(r => {
		r.AddIngredient(ItemID.Bunny, 5);
		r.AddTile(TileID.Sawmill);
	});

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		if (!player.TryGetModPlayer(out PlayerBunnyhopCombos bunnyhopCombos)) {
			return;
		}

		bunnyhopCombos.BoostBonusPerCombo += 0.035f;
	}
	
	public override bool CanRightClick()
		=> true;

	public override bool ConsumeItem(Player player)
		=> false;

	public override void RightClick(Player player)
	{
		if (!player.TryGetModPlayer(out PlayerBunnyhopCombos bunnyhopCombos)) {
			return;
		}

		bool isNowEnabled = !bunnyhopCombos.AudioEnabled;
		var color = new Color(138, 39, 48);

		Main.NewText(OverhaulMod.Instance.GetTextValue($"CommonTooltips.RightClickAudioCues{(isNowEnabled ? "Enabled" : "Disabled")}"), color);

		bunnyhopCombos.AudioEnabled = isNowEnabled;
	}
}
