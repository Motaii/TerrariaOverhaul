﻿// Copyright (c) 2020-2024 Mirsario & Contributors.
// Released under the GNU General Public License 3.0.
// See LICENSE.md for details.

using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaOverhaul.Common.Hooks.Items;
using TerrariaOverhaul.Common.Items;
using TerrariaOverhaul.Core.ItemComponents;
using TerrariaOverhaul.Core.Networking;
using TerrariaOverhaul.Utilities;

namespace TerrariaOverhaul.Common.Charging;

#pragma warning disable IDE0060 // Remove unused parameter

public sealed class ItemPowerAttacks : ItemComponent, IModifyCommonStatModifiers, ICanDoMeleeDamage, ICanTurnDuringItemUse
{
	public delegate bool CanStartPowerAttackDelegate(Item item, Player player);

	public bool CanRelease;
	public float ChargeLengthMultiplier = 2f;
	public SingleOrGradient<CommonStatModifiers> StatModifiers = new();

	private Timer charge;

	public bool PowerAttack { get; private set; }

	public Timer Charge => charge;
	public bool IsCharging => Charge.Active;

	public override void Load()
	{
		// AltFunctionUse hook doesn't fit, since it relies on 'ItemID.Sets.ItemsThatAllowRepeatedRightClick' for repeated uses.
		// Also it's better to execute power attack code after all other mods are done with their AltFunctionUse hooks.
		IL_Player.ItemCheck_ManageRightClickFeatures += context => {
			var il = new ILCursor(context);

			int isButtonHeldLocalId = -1;

			il.GotoNext(
				// bool flag2 = flag;
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(out isButtonHeldLocalId),
				i => i.MatchLdloc(isButtonHeldLocalId),
				i => i.MatchStloc(out _),
				// if (!ItemID.Sets.ItemsThatAllowRepeatedRightClick[inventory[selectedItem].type] && !Main.mouseRightRelease)
				i => i.MatchLdsfld(typeof(ItemID.Sets), nameof(ItemID.Sets.ItemsThatAllowRepeatedRightClick))
				// ...
			);

			il.GotoNext(
				MoveType.Before,
				// if (!controlUseItem && altFunctionUse == 1)
				i => i.Match(OpCodes.Ldarg_0),
				i => i.MatchLdfld(typeof(Player), nameof(Player.controlUseItem)),
				i => i.MatchBrtrue(out _),
				//
				i => i.Match(OpCodes.Ldarg_0),
				i => i.MatchLdfld(typeof(Player), nameof(Player.altFunctionUse)),
				i => i.MatchLdcI4(1)
				// ...
			);
			il.HijackIncomingLabels();

			int initialCheckSuccessLocalId = il.AddLocalVariable(typeof(bool));

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, isButtonHeldLocalId);
			il.EmitDelegate((Player player, bool isButtonHeld) => {
				return isButtonHeld && player.altFunctionUse == 0;
			});
			il.Emit(OpCodes.Stloc, initialCheckSuccessLocalId);

			// Move to right before the end of the method
			il.GotoNext(MoveType.Before, i => i.Match(OpCodes.Ret));
			il.HijackIncomingLabels();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, initialCheckSuccessLocalId);
			il.EmitDelegate((Player player, bool initialCheckSuccess) => {
				if (!initialCheckSuccess || player.altFunctionUse != 0 || player.shieldRaised) {
					return;
				}

				if (player.HeldItem is not Item item) {
					return;
				}

				if (item.TryGetGlobalItem(out ItemPowerAttacks itemPowerAttacks) != true) {
					return;
				}

				itemPowerAttacks.AttemptPowerAttackStart(item, player);

				/*
				player.altFunctionUse = 1;
				player.controlUseItem = true;
				*/
			});
		};

		On_Player.ItemCheck_OwnerOnlyCode += PlayertemCheckOwnerOnlyCodeDetour;
	}

	public override void HoldItem(Item item, Player player)
	{
		// The charge is ongoing
		if (IsCharging) {
			ChargeUpdate(item, player);
		}

		// If the charge just ended on its own, or is released
		if ((!Charge.FreezeTime.HasValue && Charge.UnclampedValue == 0 && Charge.CurrentTime != 0) || (CanRelease && Charge.Active && !player.controlUseTile)) {
			ChargeEnd(item, player);
		}

		// Not charging and the use has ended
		if (!IsCharging && player.itemAnimation <= 1) {
			PowerAttack = false;
		}
	}

	public bool AttemptPowerAttackStart(Item item, Player player)
	{
		if (!Enabled || !player.IsLocal()) {
			return false;
		}

		if (player.itemAnimation > 0 || IsCharging) {
			return false;
		}

		if (!ItemLoader.CanUseItem(item, player)) {
			return false;
		}

		if (!player.CheckMana(item)) {
			return false;
		}

		if (!ICanStartPowerAttack.Invoke(item, player)) {
			return false;
		}
		
		uint chargeLength = (uint)CombinedHooks.TotalAnimationTime(item.useAnimation * ChargeLengthMultiplier, player, item);

		StartPowerAttack(item, player, chargeLength);

		return true;
	}

	public void StartPowerAttack(Item item, Player player, uint chargeLength)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && player.IsLocal()) {
			MultiplayerSystem.SendPacket(new PowerAttackStartPacket(player, (int)chargeLength));
		}

		charge.Set(chargeLength);

		// How does this happen?
		if (!player.IsLocal()) {
			player.controlUseTile = true;
		}
	}

	private void ChargeUpdate(Item item, Player player)
	{
		player.itemTime = 2;
		player.itemAnimation = player.itemAnimationMax;
	}

	private void ChargeEnd(Item item, Player player)
	{
		charge.Freeze();

		PowerAttack = true;

		player.GetModPlayer<PlayerItemUse>().ForceItemUse();
	}

	void IModifyCommonStatModifiers.ModifyCommonStatMultipliers(Item item, Player player, ref CommonStatModifiers multipliers)
	{
		if (!PowerAttack) {
			return;
		}

		float chargeProgress = charge.Progress;
		CommonStatModifiers powerMultipliers;

		if (StatModifiers.Gradient is Gradient<CommonStatModifiers> gradient) {
			powerMultipliers = gradient.GetValue(chargeProgress);
		} else {
			powerMultipliers = CommonStatModifiers.Lerp(
				in CommonStatModifiers.Default,
				in StatModifiers.Single,
				chargeProgress
			);
		}

		multipliers *= powerMultipliers;
	}

	bool ICanDoMeleeDamage.CanDoMeleeDamage(Item item, Player player)
		=> /*!IsCharging*/ Charge.UnclampedUnfrozenValue < 0;

	bool? ICanTurnDuringItemUse.CanTurnDuringItemUse(Item item, Player player)
		=> IsCharging ? true : null;

	private static void PlayertemCheckOwnerOnlyCodeDetour(On_Player.orig_ItemCheck_OwnerOnlyCode orig, Player player, ref Player.ItemCheckContext context, Item sItem, int weaponDamage, Microsoft.Xna.Framework.Rectangle heldItemFrame)
	{
		if (player.HeldItem is { IsAir: false } item && item.TryGetGlobalItem(out ItemPowerAttacks powerAttacks) && powerAttacks.IsCharging) {
			return;
		}

		orig(player, ref context, sItem, weaponDamage, heldItemFrame);
	}
}
