﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Terraria.Audio;
using Terraria.ModLoader;

namespace TerrariaOverhaul.Common.Fires;

public class FireSystem : ModSystem
{
	public static readonly SoundStyle ExtinguishSound = new($"{nameof(TerrariaOverhaul)}/Assets/Sounds/Fire/Extinguish") {
		PitchVariance = 0.1f
	};
}
