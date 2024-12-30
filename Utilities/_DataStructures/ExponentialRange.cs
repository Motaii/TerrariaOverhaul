﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.Utilities;

namespace TerrariaOverhaul.Utilities;

public struct ExponentialRange()
{
	public float Min;
	public float Max;
	public float Exponent = 1f;

	[JsonConstructor]
	public ExponentialRange(float Min, float Max, float Exponent = 1f) : this()
	{
		this.Min = Min;
		this.Max = Max;
		this.Exponent = Exponent;
	}

	public readonly float RandomValue() => RandomValue(Main.rand);
	public readonly float RandomValue(UnifiedRandom rng) => Translate(rng.NextFloat());

	public readonly float Translate(float value01)
	{
		return MathHelper.Lerp(Min, Max, 1f - MathF.Pow(1f - value01, Exponent));
	}

	public readonly float DistanceFactor(float distance)
	{
		if (distance < Min) return 1f;

		float factor = 1f - MathF.Min(1f, MathF.Max(0f, distance - Min) / (Max - Min));
		if (!float.IsNormal(factor)) return 0f;

		return MathF.Pow(factor, Exponent);
	}
}
