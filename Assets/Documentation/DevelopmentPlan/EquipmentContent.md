# Equipment Content

## Source of truth

The Equipment Items sheet (`gid=0`) is implemented as 55 active `EquipmentItemSO` assets and 55 data-driven recipes. Stable item IDs remain unchanged so inventory and save references continue to resolve.

## Authoring rules

- Artificer crafts Sword, Body, Foot, Head, and Shield equipment at `station.forge`.
- Agnes is the display name of the Hermetist identity (`npc.town.hermetist`) and crafts Ring and
  Necklace equipment at `station.forge`.
- Every metal ingredient in the sheet resolves to its Bar item. `diamond` resolves to `item.material.diamond_bar`.
- The ambiguous `Clockmaker + 7 iron` row for `head_lvl5` is resolved as the previous-tier `Copper Helm + 7 Iron Bars`.
- The `Wood` ingredient for `ring_lvl4` resolves to `item.material.wood_log`.
- Equipment value includes the consumed previous-tier item's cumulative raw-material cost. Sell ranges are 110–120% of cumulative material sell value; buy ranges are twice the equipment sell range.
- Required level is the shared gate for both equipping and crafting. `CraftingManager` reads the
  output `EquipmentItemSO.requiredLevel` directly rather than duplicating level data on the recipe;
  the Crafting UI displays this level and rejects the transaction before consuming ingredients when
  the player level is too low. `EquipmentManager.Equip` continues to enforce the same field.
- Merric is the display name of the existing Artificer identity (`npc.town.artificer`). His
  `station.forge` recipe list contains Sword, Body, Foot, Head and Shield equipment.
- Agnes buys and sells Tanzanite, Citrine, Opal, Charoite and Emerald at
  `shop.town.trader_magic`. Her Ring/Necklace recipes use the same shared equip/craft level gate.

## Balance baseline

Stats rise monotonically inside each equipment family:

- Sword: Attack Damage.
- Body: Max Health and Defense.
- Foot: Move Speed and Dodge Chance.
- Head: Defense and Critical Chance.
- Shield: Defense and Damage Reduction.
- Ring: Attack Damage and Critical Chance.
- Necklace: Max Health and Health Regeneration.

The sheet leaves Foot and Head descriptions empty. The subsequent description pass added short in-game descriptions based on the equipment type and appearance.

## Removed legacy content

The sheet explicitly retires `shield_lvl9`, `ring_lvl9`, `ring_lvl10`, `necklace_lvl9`, and `necklace_lvl10`. Their assets and typed catalog references are removed. Older saves that contain these retired IDs use the existing fail-soft unresolved-item path and do not substitute another item silently.

## Validation

- Exactly 55 active equipment assets and one recipe per active equipment ID.
- Unique equipment item IDs and recipe IDs.
- Every recipe output and ingredient resolves through `ResourcesItemResolver`.
- No catalog reference may point at a retired equipment GUID.


## Inventory icon refresh — 2026-11-25

- All 55 equipment items point to dedicated 64×64 transparent, point-filtered sprites under
  `Assets/Art/ItemIcons`, named by stable item ID.
- The nine swords, eight body armors and six helmets use the first idle frame from their equipped
  SpriteLibraryAsset. The icon crop and padding preserve the frame's proportions.
- Foot, Shield, Ring and Necklace slots use the existing item art as their source. These slots
  currently have no independent equipped sprite in EquipmentManager.


### Equipment icon redrawing

The 55 equipment icons were redrawn with built-in ImageGen using contact sheets of the
equipped/source sprites as visual references. Each sprite keeps its stable asset path and is
cropped onto the existing 64×64 transparent canvas. The six helmet icons show hollow helmet
shells. Reference prompts preserve item order, silhouette, palette and equipment profile while
using crisp pixel clusters and articulated highlights. Weapon prompts emphasize distinct blade,
hilt and guard details, including the wooden Training Sword. Helmet prompts exclude all player
features. Body, boot, pendant, ring and shield prompts preserve their individual trim and shape.
