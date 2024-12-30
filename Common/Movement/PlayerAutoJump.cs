﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Terraria;
using Terraria.ModLoader;
using TerrariaOverhaul.Core.Configuration;

namespace TerrariaOverhaul.Common.Movement;

public sealed class PlayerAutoJump : ModPlayer
{
	public static readonly ConfigEntry<bool> EnableAutoJump = new(ConfigSide.Both, true, "Movement", "Accessibility");

	public override void ResetEffects()
	{
		if (EnableAutoJump) {
			Player.autoJump = true;
		}
	}
}
