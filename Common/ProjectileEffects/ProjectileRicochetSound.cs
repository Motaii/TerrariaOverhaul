﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TerrariaOverhaul.Common.Tags;
using TerrariaOverhaul.Core.Configuration;

namespace TerrariaOverhaul.Common.ProjectileEffects;

[Autoload(Side = ModSide.Client)]
public class ProjectileRicochetSound : GlobalProjectile
{
	public static readonly ConfigEntry<bool> EnableBulletImpactAudio = new(ConfigSide.ClientOnly, true, "Guns");

	public static readonly SoundStyle RicochetSound = new($"{nameof(TerrariaOverhaul)}/Assets/Sounds/HitEffects/Ricochet", 2) {
		Volume = 0.1f,
	};

	public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
		=> OverhaulProjectileTags.Bullet.Has(projectile.type);

	public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
	{
		if (EnableBulletImpactAudio) {
			SoundEngine.PlaySound(RicochetSound, projectile.Center);
		}

		return true;
	}
}
