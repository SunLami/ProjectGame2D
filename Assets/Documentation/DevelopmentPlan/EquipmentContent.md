# Equipment Content

## Source of truth

The Equipment Items sheet (`gid=0`) is implemented as 55 active `EquipmentItemSO` assets and 55 data-driven recipes. Stable item IDs remain unchanged so inventory and save references continue to resolve.

## Authoring rules

- Artificer crafts Sword, Body, Foot, Head, and Shield equipment at `station.forge`.
- Hermetist crafts Ring and Necklace equipment at `station.forge`.
- Every metal ingredient in the sheet resolves to its Bar item. `diamond` resolves to `item.material.diamond_bar`.
- The ambiguous `Clockmaker + 7 iron` row for `head_lvl5` is resolved as the previous-tier `Copper Helm + 7 Iron Bars`.
- The `Wood` ingredient for `ring_lvl4` resolves to `item.material.wood_log`.
- Equipment value includes the consumed previous-tier item's cumulative raw-material cost. Sell ranges are 110–120% of cumulative material sell value; buy ranges are twice the equipment sell range.
- Required level is enforced by `EquipmentManager.Equip` and displayed in the inventory tooltip.

## Balance baseline

Stats rise monotonically inside each equipment family:

- Sword: Attack Damage.
- Body: Max Health and Defense.
- Foot: Move Speed and Dodge Chance.
- Head: Defense and Critical Chance.
- Shield: Defense and Damage Reduction.
- Ring: Attack Damage and Critical Chance.
- Necklace: Max Health and Health Regeneration.

The sheet intentionally leaves Foot and Head descriptions empty, so those assets keep an empty description instead of invented lore.

## Removed legacy content

The sheet explicitly retires `shield_lvl9`, `ring_lvl9`, `ring_lvl10`, `necklace_lvl9`, and `necklace_lvl10`. Their assets and typed catalog references are removed. Older saves that contain these retired IDs use the existing fail-soft unresolved-item path and do not substitute another item silently.

## Validation

- Exactly 55 active equipment assets and one recipe per active equipment ID.
- Unique equipment item IDs and recipe IDs.
- Every recipe output and ingredient resolves through `ResourcesItemResolver`.
- No catalog reference may point at a retired equipment GUID.

