﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Terraria;
using Terraria.ModLoader;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Common.DamageSources;

// This weird system tries to guess damage sources based on callstack locations.
// Used in places like StrikeNPC hooks.
public class DamageSourceSystem : ModSystem
{
	public static DamageSource? CurrentDamageSource { get; private set; }

	public override void Load()
	{
		On_Player.ItemCheck_MeleeHitNPCs += (orig, player, item, itemRectangle, originalDamage, knockback) => {
			var oldSource = CurrentDamageSource;
			CurrentDamageSource = new DamageSource(item, new DamageSource(player));

			orig(player, item, itemRectangle, originalDamage, knockback);

			CurrentDamageSource = oldSource;
		};
		On_Player.ItemCheck_MeleeHitPVP += (orig, player, item, itemRectangle, originalDamage, knockback) => {
			var oldSource = CurrentDamageSource;
			CurrentDamageSource = new DamageSource(item, new DamageSource(player));

			orig(player, item, itemRectangle, originalDamage, knockback);

			CurrentDamageSource = oldSource;
		};
		On_Projectile.Damage += (orig, projectile) => {
			var oldSource = CurrentDamageSource;
			var owner = projectile.GetOwner();

			CurrentDamageSource = new DamageSource(projectile, owner != null ? new DamageSource(owner) : null);

			orig(projectile);

			CurrentDamageSource = oldSource;
		};
	}

	public override void Unload()
	{
		CurrentDamageSource = null;
	}

	public override void PostUpdateEverything()
	{
		CurrentDamageSource = null; // Reset just in case exceptions screw something over.
	}
}
