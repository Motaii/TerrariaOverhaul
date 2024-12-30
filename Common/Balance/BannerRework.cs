﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaOverhaul.Core.Configuration;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Common.Balance;

public sealed class BannerReworkSystem : ModSystem
{
	private static bool bannerDamageDisabled;
	private static ItemID.BannerEffect[]? defaultBannerEffects;

	public static readonly ConfigEntry<bool> EnableBannerRework = new(ConfigSide.Both, true, "Balance");

	public override void Load()
	{
		On_NPC.NPCLoot_DropItems += (orig, npc, closestPlayer) => {
			orig(npc, closestPlayer);

			if (ShouldDoubleLoot(npc)) {
				orig(npc, closestPlayer);
			}
		};

		On_NPC.NPCLoot_DropMoney += (orig, npc, closestPlayer) => {
			orig(npc, closestPlayer);

			// Prevent picked up coins from being doubled too.
			npc.extraValue = 0;

			if (ShouldDoubleLoot(npc)) {
				orig(npc, closestPlayer);
			}
		};
	}

	public override void PreUpdatePlayers()
	{
		bool enable = EnableBannerRework.Value;

		if (enable != bannerDamageDisabled) {
			if (enable) {
				defaultBannerEffects ??= ItemID.Sets.BannerStrength;

				ItemID.Sets.BannerStrength = ItemID.Sets.Factory.CreateCustomSet(new ItemID.BannerEffect(1f, 1f, 1f, 1f));
			} else {
				ItemID.Sets.BannerStrength = defaultBannerEffects ?? throw new Exception($"{nameof(BannerReworkSystem)} failed miserably!");
			}

			bannerDamageDisabled = enable;
		}
	}

	private static bool ShouldDoubleLoot(NPC npc)
	{
		if (npc.type < NPCID.None || !EnableBannerRework) {
			return false;
		}
		
		int bannerId = Item.NPCtoBanner(npc.BannerID());

		if (bannerId <= 0) {
			return false;
		}
		
		const float MinDistanceSquared = 2048f * 2048f;

		Vector2 npcCenter = npc.Center;

		foreach (var player in ActiveEntities.Players) {
			if (player.HasNPCBannerBuff(bannerId) && Vector2.DistanceSquared(player.Center, npcCenter) < MinDistanceSquared) {
				return true;
			}
		}

		return false;
	}
}

public sealed class BuffBannerRework : GlobalBuff
{
	public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
	{
		if (BannerReworkSystem.EnableBannerRework && type == BuffID.MonsterBanner) {
			tip = OverhaulMod.Instance.GetTextValue("Banners.BannerBuffDescription");
		}
	}
}

public sealed class ItemBannerRework : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!BannerReworkSystem.EnableBannerRework || !item.consumable || item.createTile <= -1) {
			return;
		}

		string vanillaDescription = Language.GetTextValue("CommonItemTooltip.BannerBonus");
		string overhaulDescription = OverhaulMod.Instance.GetTextValue("Banners.BannerItemDescription");

		foreach (var line in tooltips) {
			string text = line.Text;

			line.Text = line.Text.Replace(vanillaDescription, $"{overhaulDescription}\r\n[c/a5fc8d:");

			if (line.Text != text) {
				line.Text += "]";
			}
		}
	}
}
