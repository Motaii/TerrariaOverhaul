﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

namespace TerrariaOverhaul.Common.Seasons;

public class Winter : Season
{
	protected internal override void Init()
	{
		Components.Add(new SnowSeasonComponent());
	}
}
