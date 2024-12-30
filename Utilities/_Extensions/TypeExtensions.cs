﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;

namespace TerrariaOverhaul.Utilities;

public static class TypeExtensions
{
	public static string GetPath(this Type type)
		=> $"{GetDirectory(type)}/{type.Name}";

	public static string GetFullPath(this Type type)
		=> $"{GetFullDirectory(type)}/{type.Name}";

	public static string GetDirectory(this Type type)
	{
		string fullDirectory = GetFullDirectory(type);
		int firstSlash = fullDirectory.IndexOf('/');

		return firstSlash > 0 ? fullDirectory.Substring(firstSlash + 1) : string.Empty;
	}

	public static string GetFullDirectory(this Type type)
		=> type.Namespace?.Replace('.', '/') ?? string.Empty;
}
