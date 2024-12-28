﻿using Terraria.Audio;
using TerrariaOverhaul.Common.Footsteps;
using TerrariaOverhaul.Common.Tags;
using TerrariaOverhaul.Core.PhysicalMaterials;
using TerrariaOverhaul.Core.Tags;

namespace TerrariaOverhaul.Common.PhysicalMaterials;

public sealed class MossStonePhysicalMaterial : PhysicalMaterial, ITileTagAssociated, IFootstepSoundProvider
{
	public TagData TileTag { get; } = OverhaulTileTags.MossStone;
	// Footsteps
	public SoundStyle? FootstepSound { get; } = new($"{nameof(TerrariaOverhaul)}/Assets/Sounds/Footsteps/MossStone/Step", 8) {
		Volume = 0.5f,
		PitchVariance = 0.1f,
	};
}
