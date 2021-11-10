﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using TerrariaOverhaul.Common.Systems.Time;
using TerrariaOverhaul.Core.Systems.Configuration;
using TerrariaOverhaul.Utilities.DataStructures;
using TerrariaOverhaul.Utilities.Extensions;

namespace TerrariaOverhaul.Common.ModEntities.Players
{
	public sealed class PlayerMovement : PlayerBase
	{
		public struct MovementModifier
		{
			public static readonly MovementModifier Default = new() {
				gravityScale = 1f,
				runAccelerationScale = 1f,
				maxRunSpeedScale = 1f
			};

			public float gravityScale;
			public float runAccelerationScale;
			public float maxRunSpeedScale;

			public void Apply(Player player)
			{
				player.gravity *= gravityScale;
				player.runAcceleration *= runAccelerationScale;
			}
		}

		public static readonly int VelocityRecordSize = 5;
		// The way to disable this has been removed due to vanilla jump velocity logic resetting velocity and clashing with many different features
		//public static readonly ConfigEntry<bool> EnableJumpPhysicsImprovements = new(ConfigSide.Both, "PlayerMovement", nameof(EnableJumpPhysicsImprovements), () => true);
		public static readonly ConfigEntry<bool> EnableVerticalAccelerationChanges = new(ConfigSide.Both, "PlayerMovement", nameof(EnableVerticalAccelerationChanges), () => true);
		public static readonly ConfigEntry<bool> EnableHorizontalAccelerationChanges = new(ConfigSide.Both, "PlayerMovement", nameof(EnableHorizontalAccelerationChanges), () => true);

		//public Timer noJumpTime;
		public Timer noMovementTime;
		public int vanillaAccelerationTime;
		public Vector2? forcedPosition;
		//public Vector2 prevVelocity;
		public Vector2[] velocityRecord = new Vector2[VelocityRecordSize];

		private int maxPlayerJump;
		private int prevPlayerJump;

		private readonly Dictionary<string, (ulong endTime, MovementModifier modifier)> MovementModifiers = new();

		public override void Load()
		{
			//Replace jump hold down logic with gravity scaling
			IL.Terraria.Player.JumpMovement += (context) => {
				var cursor = new ILCursor(context);

				//Match 'if (jump > 0)'
				cursor.GotoNext(
					MoveType.After,
					i => i.Match(OpCodes.Ldarg_0),
					i => i.MatchLdfld(typeof(Player), nameof(Player.jump)),
					i => i.MatchLdcI4(0),
					i => i.MatchCgt()
				);

				//Match 'velocity.Y = (0f - jumpSpeed) * gravDir;'
				cursor.GotoNext(
					MoveType.Before,
					i => i.Match(OpCodes.Ldarg_0),
					i => i.MatchLdflda(typeof(Entity), nameof(Entity.velocity)),
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

					player.gravity *= MathHelper.Lerp(1f, 0.1f, MathHelper.Clamp(player.jump / (float)Math.Max(playerMovement.maxPlayerJump, 1), 0f, 1f));
				});
			};
		}

		public override void PreUpdate()
		{
			bool onGround = Player.OnGround();
			bool wasOnGround = Player.WasOnGround();

			//Update the data necessary for jump key holding logic
			if (Player.jump != prevPlayerJump) {
				if (Player.jump > prevPlayerJump) {
					maxPlayerJump = Player.jump;
				}

				prevPlayerJump = Player.jump;
			}

			//Scale jump stats
			Player.jumpSpeed *= 1.21f;
			Player.jumpHeight = (int)(Player.jumpHeight * 1.75f);

			if (!Player.wet) {
				bool wings = Player.wingsLogic > 0 && Player.controlJump && !onGround && !wasOnGround;
				bool wingFall = wings && Player.wingTime == 0;

				if (vanillaAccelerationTime > 0) {
					vanillaAccelerationTime--;
				} else if (!Player.slippy && !Player.slippy2 && EnableHorizontalAccelerationChanges) {
					//Horizontal acceleration
					if (onGround) {
						Player.runAcceleration *= 2f;
					} else {
						Player.runAcceleration *= 1.65f;
					}

					//Wind acceleration
					if (Player.FindBuffIndex(BuffID.WindPushed) >= 0) {
						if (Main.windSpeedCurrent >= 0f ? Player.velocity.X < Main.windSpeedCurrent : Player.velocity.X > Main.windSpeedCurrent) {
							Player.velocity.X += Main.windSpeedCurrent / (Player.KeyDirection() == -Math.Sign(Main.windSpeedCurrent) ? 180f : 70f);
						}
					}

					Player.runSlowdown = onGround ? 0.275f : 0.02f;
				}

				if (noMovementTime.Active) {
					Player.maxRunSpeed = 0f;
					Player.runAcceleration = 0f;
				} else if (Player.chilled) {
					Player.maxRunSpeed *= 0.6f;
				}

				if (EnableVerticalAccelerationChanges) {
					Player.maxFallSpeed = wingFall ? 10f : 1000f;

					//Falling friction & speed limit
					if (Player.velocity.Y > Player.maxFallSpeed) {
						Player.velocity.Y = Player.maxFallSpeed;
					} else if (Player.velocity.Y > 0f) {
						Player.velocity.Y *= 0.995f;
					}
				}
			}

			HandleMovementModifiers();
		}

		public override void PostUpdate()
		{
			Array.Copy(velocityRecord, 0, velocityRecord, 1, velocityRecord.Length - 1); //Shift

			velocityRecord[0] = Player.velocity;

			if (forcedPosition != null) {
				Player.position = forcedPosition.Value;
				forcedPosition = null;
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
			List<string> keysToRemove = null;

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
	}
}
