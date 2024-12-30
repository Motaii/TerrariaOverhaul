﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaOverhaul.Common.BloodAndGore;
using TerrariaOverhaul.Common.Tags;
using TerrariaOverhaul.Content.Gores;
using TerrariaOverhaul.Core.Configuration;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Common.ProjectileEffects;

[Autoload(Side = ModSide.Client)]
public sealed class ProjectileArrowGore : GlobalProjectile
{
	public static readonly ConfigEntry<bool> EnableArrowFragments = new(ConfigSide.ClientOnly, true, "Archery");

	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
	{
		return OverhaulProjectileTags.WoodenArrow.Has(entity.type);
	}

	public override void OnKill(Projectile projectile, int timeLeft)
	{
		if (!EnableArrowFragments) {
			return;
		}

		var entitySource = projectile.GetSource_Death();

		Gore SpawnGore<T>() where T : ModGore
		{
			Vector2 position = projectile.oldPosition + projectile.Size * 0.5f;
			Vector2 velocity = (projectile.RealVelocity() * -0.25f).RotatedByRandom(MathHelper.ToRadians(30f));

			return Gore.NewGoreDirect(entitySource, position, velocity, ModContent.GoreType<T>());
		}

		var arrowHead = SpawnGore<ArrowHead>();

		SpawnGore<ArrowMiddle>();
		SpawnGore<ArrowBack>();

		if (projectile.type == ProjectileID.FireArrow && arrowHead is OverhaulGore oGore) {
			oGore.OnFire = true;
		}
	}
}
