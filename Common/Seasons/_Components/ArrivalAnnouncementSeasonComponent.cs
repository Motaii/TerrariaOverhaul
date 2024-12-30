﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Terraria;
using TerrariaOverhaul.Core.Components;

namespace TerrariaOverhaul.Common.Seasons;

[GlobalComponent]
public sealed class ArrivalAnnouncementSeasonComponent : SeasonComponent
{
	public override void OnSeasonBegin(Season season)
	{
		Main.NewText($"Season {season.Name} is here.");
	}
}
