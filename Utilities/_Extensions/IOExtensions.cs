﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace TerrariaOverhaul.Utilities;

public static class IOExtensions
{
	// Vector2Int

	public static void WriteVector2Int(this BinaryWriter writer, Vector2Int vector)
	{
		writer.Write(vector.X);
		writer.Write(vector.Y);
	}

	public static Vector2Int ReadVector2Int(this BinaryReader reader)
	{
		return new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
	}

	// Half Vector2

	public static void WriteHalfVector2(this BinaryWriter writer, Vector2 vector)
	{
		writer.Write((Half)vector.X);
		writer.Write((Half)vector.Y);
	}

	public static Vector2 ReadHalfVector2(this BinaryReader reader)
	{
		return new Vector2((float)reader.ReadHalf(), (float)reader.ReadHalf());
	}

	// Players

	public static void TryWriteSenderPlayer(this BinaryWriter writer, Player player)
	{
		if (Main.netMode == NetmodeID.Server) {
			writer.Write((byte)player.whoAmI);
		}
	}

	public static bool TryReadSenderPlayer(this BinaryReader reader, int sender, out Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) {
			sender = reader.ReadByte();
		}

		player = Main.player[sender];

		return player != null && player.active;
	}
}
