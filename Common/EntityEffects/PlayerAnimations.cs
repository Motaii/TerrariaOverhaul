﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Common.EntityEffects;

public sealed class PlayerAnimations : ModPlayer
{
	public PlayerFrames? ForcedHeadFrame;
	public PlayerFrames? ForcedBodyFrame;
	public PlayerFrames? ForcedLegFrame;

	public override void PostUpdate()
	{
		static void TryForceFrame(ref Rectangle frame, ref PlayerFrames? newFrame)
		{
			if (newFrame.HasValue) {
				frame = newFrame.Value.ToRectangle();

				newFrame = null;
			}
		}

		TryForceFrame(ref Player.headFrame, ref ForcedHeadFrame);
		TryForceFrame(ref Player.bodyFrame, ref ForcedBodyFrame);
		TryForceFrame(ref Player.legFrame, ref ForcedLegFrame);
	}
}
