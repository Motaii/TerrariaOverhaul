﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

namespace TerrariaOverhaul.Core.AudioEffects;

public struct AudioEffectsModifier
{
	public delegate void ModifierDelegate(float intensity, ref AudioEffectParameters soundParameters, ref AudioEffectParameters musicParameters);

	public readonly string Id;

	public ModifierDelegate Modifier { get; set; }
	public int TimeLeft { get; set; }
	public int TimeMax { get; set; }

	public AudioEffectsModifier(int timeLeft, string id, ModifierDelegate modifier) : this()
	{
		Id = id;
		TimeMax = TimeLeft = timeLeft;
		Modifier = modifier;
	}
}
