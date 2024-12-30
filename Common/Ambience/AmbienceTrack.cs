// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;
using Newtonsoft.Json;
using ReLogic.Utilities;
using Terraria.Audio;

namespace TerrariaOverhaul.Common.Ambience;

// This could be a struct, but Newtonsoft.Json doesn't handle their field initializers correctly.
// In 2024 that is!
public sealed class AmbienceTrack
{
	public string Name = string.Empty;
	public SlotId InstanceReference = SlotId.Invalid;
	public float VolumeChangeSpeed = 0.5f;
	public float Volume;
	public bool IsLooped = true;
	public bool SoundIsWallOccluded;
	public bool DisableSoundFiltering;

	[JsonRequired]
	public SoundStyle Sound;

	[JsonRequired]
	[JsonConverter(typeof(CalculatedSignalArrayJsonConverter))]
	public CalculatedSignal[] Variables = Array.Empty<CalculatedSignal>();

	public AmbienceTrack() { }
}
