﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using TerrariaOverhaul.Core.Configuration;

namespace TerrariaOverhaul.Common.Balance;

internal sealed class DifficultyRebalanceSystem : ModSystem
{
	public static readonly ConfigEntry<bool> EnableDifficultyChanges = new(ConfigSide.Both, true, "Balance");
	public static readonly ConfigEntry<bool> EnableConsistentDifficulty = new(ConfigSide.Both, true, "Balance");

	private static bool isEnabled;

	public static bool IsActuallyMasterMode => Main.GameMode == GameModeID.Master || (Main.getGoodWorld && Main.GameMode == GameModeID.Expert);

	public override void Load()
	{
		UpdateDifficultyLevels();

		On_Main.UpdateCreativeGameModeOverride += UpdateCreativeGameModeOverrideDetour;

		// Keep loot master-mode-exclusive.
		On_Conditions.IsMasterMode.CanDrop += static (orig, self, info) => EnableConsistentDifficulty ? IsActuallyMasterMode : orig(self, info);
		On_Conditions.IsMasterMode.CanShowItemDropInUI += static (orig, self) => EnableConsistentDifficulty ? IsActuallyMasterMode : orig(self);
		On_Conditions.NotMasterMode.CanDrop += static (orig, self, info) => EnableConsistentDifficulty ? !IsActuallyMasterMode : orig(self, info);
		On_Conditions.NotMasterMode.CanShowItemDropInUI += static (orig, self) => EnableConsistentDifficulty ? !IsActuallyMasterMode : orig(self);
		On_ItemDropResolver.TryDropping += static (orig, self, info) => {
			if (EnableConsistentDifficulty && !info.IsInSimulation) {
				info.IsMasterMode &= IsActuallyMasterMode;
			}

			orig(self, info);
		};
	}

	public override void Unload()
	{
		// Reset everything to vanilla
		if (isEnabled) {
			UpdateDifficultyLevels(false);
		}
	}

	public override void PreUpdateEntities()
	{
		UpdateDifficultyLevels();
	}

	private static void UpdateDifficultyLevels()
	{
		bool shouldBeEnabled = EnableDifficultyChanges;

		//if (isEnabled != shouldBeEnabled) {
		UpdateDifficultyLevels(shouldBeEnabled);
		//}
	}

	private static void UpdateDifficultyLevels(bool shouldBeEnabled)
	{
		// This will unfortunately reset any changes from other mods

		Span<GameModeData> presets = new GameModeData[4] {
			GameModeData.NormalMode,
			GameModeData.ExpertMode,
			GameModeData.MasterMode,
			GameModeData.CreativeMode
		};

		if (shouldBeEnabled) {
			ModifyDifficultyLevels(presets);
		}

		for (int i = 0; i < presets.Length; i++) {
			Main.RegisteredGameModes[i] = presets[i];
		}

		Main.GameMode = Main.GameMode; // Reloads some cache
		isEnabled = shouldBeEnabled;
	}

	private static void ModifyDifficultyLevels(Span<GameModeData> presets)
	{
		// Unify various important values to expert mode's or nearby.

		for (int i = 0; i < presets.Length; i++) {
			presets[i] = presets[i] with {
				// Enemy health & defense -- This straight up should never differ, both too low and too high ruin satisfaction.
				EnemyMaxLifeMultiplier = 2.25f,
				EnemyDefenseMultiplier = 1.25f,
				// Knockback - Lowering it too much could ruin combat stunts.
				KnockbackToEnemiesMultiplier = 1.0f,
				// Debuff Time - Why not just make them more deadly rather than double times?
				DebuffTimeMultiplier = 1.0f,
				// Money drops - Expert gives too much money, Normal has too much grind.
				EnemyMoneyDropMultiplier = 1.75f, // From [ 1.0, 2.5, 2.5 ]

				// Everything derives from Master.
				IsExpertMode = true,
				IsMasterMode = true,
			};
		}

		ref var journey = ref presets[GameModeID.Creative];
		ref var normal = ref presets[GameModeID.Normal];
		ref var expert = ref presets[GameModeID.Expert];
		ref var master = ref presets[GameModeID.Master];

		normal.EnemyDamageMultiplier = 1.25f; // From 1.0
		expert.EnemyDamageMultiplier = 2.00f; // From 2.0
		master.EnemyDamageMultiplier = 3.25f; // From 3.0

		// From 1.0. Since Journey can configure its enemy difficulty, this is the lowest value it can choose.
		journey.EnemyDamageMultiplier = normal.EnemyDamageMultiplier * 0.5f;
	}

	// Forces the game to always think that it's master mode.
	private static void UpdateCreativeGameModeOverrideDetour(On_Main.orig_UpdateCreativeGameModeOverride orig)
	{
		if (!EnableDifficultyChanges) {
			orig();
			return;
		}

		if (EnableConsistentDifficulty) {
			[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_overrideForExpertMode")]
			extern static ref bool? OverrideForExpertMode(Main? c);

			[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_overrideForMasterMode")]
			extern static ref bool? OverrideForMasterMode(Main? c);

			OverrideForExpertMode(null) = true;
			OverrideForMasterMode(null) = true;
		}
	}
}
