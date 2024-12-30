﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using System;

namespace TerrariaOverhaul.Common.ConfigurationScreen;

public interface IConfigEntryController
{
	object? Value { get; set; }

	event Action? OnModified;
}
