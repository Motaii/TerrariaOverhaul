﻿using Terraria;
using Terraria.Audio;

namespace TerrariaOverhaul.Utilities;

public sealed class NpcAudioTracker(NPC npc, bool trackCenter)
{
	private readonly int type = npc.type;
	private readonly int index = npc.whoAmI;

	public bool Callback(ActiveSound sound)
	{
		if (Main.gameMenu)
			return false;

		if (Main.npc[index] is not NPC { active: true } npc || npc.type != type) {
			return false;
		}

		if (trackCenter) {
			sound.Position = npc.Center;
		}

		return true;
	}
}
