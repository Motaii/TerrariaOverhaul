using Terraria.Audio;
using Terraria.ModLoader;
using TerrariaOverhaul.Common.Footsteps;
using TerrariaOverhaul.Common.Tags;
using TerrariaOverhaul.Core.PhysicalMaterials;
using TerrariaOverhaul.Core.Tags;

namespace TerrariaOverhaul.Common.PhysicalMaterials;

public sealed class GlassPhysicalMaterial : PhysicalMaterial, ITileTagAssociated, IFootstepSoundProvider
{
	public TagData TileTag { get; } = OverhaulTileTags.Glass;

	// Footsteps
	public SoundStyle? FootstepSound { get; } = new($"{nameof(TerrariaOverhaul)}/Assets/Sounds/Footsteps/Glass/Step", 5) {
		Volume = 0.5f,
		PitchVariance = 0.2f,
	};
}
