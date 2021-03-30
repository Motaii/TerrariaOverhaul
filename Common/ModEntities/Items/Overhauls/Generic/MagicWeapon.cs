﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaOverhaul.Common.ModEntities.Items.Hooks;
using TerrariaOverhaul.Common.Systems.Camera.ScreenShakes;

namespace TerrariaOverhaul.Common.ModEntities.Items.Overhauls.Generic
{
	public partial class MagicWeapon : AdvancedItem, IShowItemCrosshair
	{
		public override ScreenShake OnUseScreenShake => new ScreenShake(4f, 0.2f);

		public override bool ShouldApplyItemOverhaul(Item item)
		{
			//Ignore weapons with non-magic damage types
			if(item.DamageType != DamageClass.Magic && !item.DamageType.CountsAs(DamageClass.Magic)) {
				return false;
			}

			//Avoid tools and placeables
			if(item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.createTile >= TileID.Dirt || item.createWall >= 0) {
				return false;
			}

			//Ignore weapons that don't shoot, don't use mana, or deal hitbox damage 
			if(item.shoot <= ProjectileID.None || item.mana <= 0 || !item.noMelee) {
				return false;
			}

			return true;
		}
		public override void HoldItem(Item item, Player player)
		{
			HoldItemCharging(item, player);
		}
		public override bool Shoot(Item item, Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockback)
		{
			ShootCharging(item, player, ref position, ref speedX, ref speedY, ref type, ref damage, ref knockback);

			return true;
		}

		public bool ShowItemCrosshair(Item item, Player player) => true;
	}
}
