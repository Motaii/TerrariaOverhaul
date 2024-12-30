// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Terraria.Audio;
using TerrariaOverhaul.Common.Footsteps;
using TerrariaOverhaul.Common.Tags;
using TerrariaOverhaul.Core.PhysicalMaterials;
using TerrariaOverhaul.Core.Tags;

namespace TerrariaOverhaul.Common.PhysicalMaterials;
public sealed class CloudPhysicalMaterial : PhysicalMaterial, ITileTagAssociated, IFootstepSoundProvider
{
	public TagData TileTag { get; } = OverhaulTileTags.Cloud;
	// Footsteps
	public SoundStyle? FootstepSound { get; } = new($"{nameof(TerrariaOverhaul)}/Assets/Sounds/Footsteps/Cloud/Step", 5) {
		Volume = 0.5f,
		PitchVariance = 0.1f,
	};
}
