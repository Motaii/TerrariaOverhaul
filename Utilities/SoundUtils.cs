﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria.Audio;

namespace TerrariaOverhaul.Utilities;

public static class SoundUtils
{
	public static void UpdateLoopingSound(ref SlotId slot, in SoundStyle style, float volume, Vector2? position)
	{
		SoundEngine.TryGetActiveSound(slot, out var sound);

		if (volume > 0f) {
			if (sound == null) {
				slot = SoundEngine.PlaySound(in style, position);
				return;
			}

			sound.Position = position;
			sound.Volume = volume;
		} else if (sound != null) {
			sound.Stop();

			slot = SlotId.Invalid;
		}
	}
}
