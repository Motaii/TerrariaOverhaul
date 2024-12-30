﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaOverhaul.Core.Configuration;
using TerrariaOverhaul.Core.Time;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Common.Movement;

public sealed class PlayerMovement : ModPlayer
{
	public static readonly int VelocityRecordSize = 5;

	public static readonly ConfigEntry<bool> EnableVerticalAccelerationChanges = new(ConfigSide.Both, true, "Movement");
	public static readonly ConfigEntry<bool> EnableHorizontalAccelerationChanges = new(ConfigSide.Both, true, "Movement");

	// The way to disable this has been removed due to vanilla jump velocity logic resetting velocity and clashing with many different features
	//public static readonly ConfigEntry<bool> EnableJumpPhysicsImprovements = new(ConfigSide.Both, true, "Movement");

	//public Timer NoJumpTime { get; set; }
	//public Vector2 PrevVelocity { get; set; }
	public Timer NoMovementTime { get; set; }
	public int VanillaAccelerationTime { get; set; }
	public Vector2? ForcedPosition { get; set; }
	public Vector2[] VelocityRecord { get; private set; } = new Vector2[VelocityRecordSize];

	private int maxPlayerJump;
	private int prevPlayerJump;

	private readonly Dictionary<string, (ulong endTime, MovementModifier modifier)> MovementModifiers = new();

	public override void Load()
	{
		IL_Player.JumpMovement += PlayerJumpMovementInjection;

		On_Player.UpdateJumpHeight += PlayerUpdateJumpHeightDetour;
	}

	public override void PreUpdate()
	{
		bool onGround = Player.OnGround();
		bool wasOnGround = Player.WasOnGround();

		// Update the data necessary for jump key holding logic
		if (Player.jump != prevPlayerJump) {
			if (Player.jump > prevPlayerJump) {
				maxPlayerJump = Player.jump;
			}

			prevPlayerJump = Player.jump;
		}

		if (!Player.wet) {
			bool wings = Player.wingsLogic > 0 && Player.controlJump && !onGround && !wasOnGround;
			bool wingFall = wings && Player.wingTime == 0;

			if (VanillaAccelerationTime > 0) {
				VanillaAccelerationTime--;
			} else if (!Player.slippy && !Player.slippy2 && EnableHorizontalAccelerationChanges) {
				// Horizontal acceleration
				if (onGround) {
					Player.runAcceleration *= 2f;
				} else {
					Player.runAcceleration *= 1.65f;
				}

				// Wind acceleration
				if (Player.FindBuffIndex(BuffID.WindPushed) >= 0) {
					if (Main.windSpeedCurrent >= 0f ? Player.velocity.X < Main.windSpeedCurrent : Player.velocity.X > Main.windSpeedCurrent) {
						Player.velocity.X += Main.windSpeedCurrent / (Math.Sign(Player.KeyDirection().X) == -Math.Sign(Main.windSpeedCurrent) ? 180f : 70f);
					}
				}

				Player.runSlowdown = onGround ? 0.275f : 0.02f;
			}

			if (NoMovementTime.Active) {
				Player.maxRunSpeed = 0f;
				Player.runAcceleration = 0f;
			} else if (Player.chilled) {
				Player.maxRunSpeed *= 0.6f;
			}

			if (EnableVerticalAccelerationChanges) {
				Player.maxFallSpeed = wingFall ? 10f : 1000f;

				// Falling friction & speed limit
				if (Player.velocity.Y > Player.maxFallSpeed) {
					Player.velocity.Y = Player.maxFallSpeed;
				} else if (Player.velocity.Y > 0f) {
					Player.velocity.Y *= 0.995f;
				}
			}
		}

		// Additional player agency over gravity.
		//if (Player.controlUp != Player.controlDown) {
		//	if (Player.controlDown) {
		//		Player.gravity *= 1.40f;
		//	} else if (Player.controlUp) {
		//		Player.gravity *= 0.80f;
		//	}
		//}

		HandleMovementModifiers();
	}

	public override void PostUpdate()
	{
		Array.Copy(VelocityRecord, 0, VelocityRecord, 1, VelocityRecord.Length - 1); // Shift

		VelocityRecord[0] = Player.velocity;

		if (ForcedPosition != null) {
			Player.position = ForcedPosition.Value;
			ForcedPosition = null;
		}

		Player.oldVelocity = Player.velocity;
	}

	public void SetMovementModifier(string id, int time, MovementModifier modifier)
	{
		MovementModifiers.TryGetValue(id, out var tuple);

		tuple.endTime = Math.Max(tuple.endTime, TimeSystem.UpdateCount + (ulong)time);
		tuple.modifier = modifier;

		MovementModifiers[id] = tuple;
	}

	private void HandleMovementModifiers()
	{
		List<string>? keysToRemove = null;

		foreach (var pair in MovementModifiers) {
			string id = pair.Key;
			var (endTime, modifier) = pair.Value;

			if (endTime <= TimeSystem.UpdateCount) {
				(keysToRemove ??= new List<string>()).Add(id);
				continue;
			}

			modifier.Apply(Player);
		}

		if (keysToRemove != null) {
			foreach (string key in keysToRemove) {
				MovementModifiers.Remove(key);
			}
		}
	}

	private static void PlayerUpdateJumpHeightDetour(On_Player.orig_UpdateJumpHeight orig, Player player)
	{
		orig(player);

		// Scale jump stats to compensate for the below injection's effects.
		// This shouldn't buff nor debuff the player, resulting in about the same feel as vanilla.
		const float VanillaJumpSpeed = 5.01f;
		const float VanillaJumpHeight = 15.0f;
		const float JumpSpeedMultiplier = 1.21f;
		const float JumpHeightMultiplier = 1.75f;
		const float JumpSpeedBonus = (VanillaJumpSpeed * JumpSpeedMultiplier) - VanillaJumpSpeed;
		const float JumpHeightBonus = (VanillaJumpHeight * JumpHeightMultiplier) - VanillaJumpHeight;

		//Player.jumpSpeed *= JumpSpeedMultiplier;
		//Player.jumpHeight = (int)(Player.jumpHeight * JumpHeightMultiplier);
		Player.jumpSpeed += JumpSpeedBonus;
		Player.jumpHeight += (int)JumpHeightBonus;
	}

	// Replace jump hold down logic with gravity scaling
	private static void PlayerJumpMovementInjection(ILContext context)
	{
		var cursor = new ILCursor(context);
		var Player = Main.LocalPlayer; // Only for nameof, so dumb.

		// Match 'if (jump > 0)'
		cursor.GotoNext(
			MoveType.After,
			i => i.Match(OpCodes.Ldarg_0),
			i => i.MatchLdfld(typeof(Player), nameof(Player.jump)),
			i => i.MatchLdcI4(0),
			i => i.MatchCgt() || i.MatchBle(out _)
		);

		// Match 'velocity.Y = (0f - jumpSpeed) * gravDir;'
		cursor.GotoNext(
			MoveType.Before,
			i => i.Match(OpCodes.Ldarg_0),
			i => i.MatchLdflda(typeof(Entity), nameof(Player.velocity)),
			i => i.MatchLdcR4(0f),
			i => i.MatchLdsfld(typeof(Player), nameof(Player.jumpSpeed)),
			i => i.MatchSub(),
			i => i.Match(OpCodes.Ldarg_0),
			i => i.MatchLdfld(typeof(Player), nameof(Player.gravDir)),
			i => i.MatchMul(),
			i => i.MatchStfld(typeof(Vector2), nameof(Vector2.Y))
		);

		var incomingLabels = cursor.IncomingLabels.ToArray();

		cursor.RemoveRange(9);
		cursor.Emit(OpCodes.Nop);

		foreach (var incomingLabel in incomingLabels) {
			incomingLabel.Target = cursor.Prev;
		}

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Action<Player>>(static player => {
			var playerMovement = player.GetModPlayer<PlayerMovement>();

			float maxGravity = 1.0f;
			float minGravity = 0.1f;
			float jumpFactor = MathHelper.Clamp(player.jump / (float)Math.Max(playerMovement.maxPlayerJump, 1), 0f, 1f);

			player.gravity *= MathHelper.Lerp(maxGravity, minGravity, jumpFactor);

			// Workaround for an issue of wings activating too late with the new jump arc.
			// This makes them activate almost immediately.
			if (player.wingsLogic > 0) {
				player.jump = 1;
			}
		});
	}
}
