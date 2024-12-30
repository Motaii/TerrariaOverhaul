﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Common.BloodAndGore;

[Autoload(Side = ModSide.Client)]
public class NPCBleeding : GlobalNPC
{
	public override bool PreAI(NPC npc)
	{
		// Bleed on low health.

		if (ChildSafety.Disabled && npc.GetMainSegment() == npc) {
			float bleedingRate = 12f;

			if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type]) {
				bleedingRate *= 2f;
			}

			int bleedEveryXTick = (int)Math.Ceiling(60 / bleedingRate);

			if (npc.life < npc.lifeMax / 2 && (Main.GameUpdateCount + npc.whoAmI * 15) % bleedEveryXTick == 0) {
				// TODO: Optimize this via a skip of enumeration?
				var bleedingNpc = npc.GetRandomSegment();

				NPCBloodAndGore.Bleed(bleedingNpc, 1);
			}
		}

		return true;
	}

	public override void OnKill(NPC npc)
	{
		if (!ChildSafety.Disabled) {
			return;
		}

		// Add extra blood on death.
		int count = (int)Math.Sqrt(npc.width * npc.height) / 12;

		NPCBloodAndGore.Bleed(npc, count);
	}
}
