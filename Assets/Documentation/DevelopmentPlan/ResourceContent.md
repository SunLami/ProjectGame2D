# Resource Content

Nguồn content: Google Sheet `Items List`, tab `Resources` (`gid=1236028827`), tích hợp ngày 2026-09-28.
Mô tả tiếng Anh trong ItemSO đã được bổ sung sau đợt tích hợp sheet. Tooltip đọc công dụng đã có trong dữ liệu game.

## Node và loot

| Node ID | Output item ID | Số lượng | HP | Respawn |
|---|---|---:|---:|---:|
| `resource.ore.copper` | `item.material.copper_ore` | 2–3 | 5 | 30 giây |
| `resource.ore.iron` | `item.material.iron` | 2–3 | 5 | 30 giây |
| `resource.ore.gold` | `item.material.gold_ore` | 2–3 | 5 | 30 giây |
| `resource.ore.tin` | `item.material.tin_ore` | 2–3 | 5 | 30 giây |
| `resource.ore.diamond` | `item.material.diamond_ore` | 2–3 | 5 | 30 giây |

`item.material.iron` là stable ID legacy đã tồn tại; không đổi ID để tránh phá recipe/save cũ.
Các node giữ `HarvestToolType.None` vì `PlayerAttackHitbox` hiện gọi harvest hit mà không truyền tool đang trang bị.

## Recipe

Tất cả recipe dưới đây dùng `station.forge` và `npc.town.artificer`. Stable NPC ID được giữ để bảo
toàn reference hiện hành; tên nhân vật hiển thị là **Merric**. Merric cung cấp cả bảy recipe tại shop
`shop.town.trader_weapon_armor`; sheet để trống cột Crafting Stations.

| Output | Ingredients |
|---|---|
| Coal | 2 Wood Log |
| Copper Bar | 5 Copper Ore + 1 Coal |
| Iron Bar | 5 Iron Ore + 1 Coal |
| Gold Bar | 5 Gold Ore + 1 Coal |
| Steel Bar | 3 Iron Bar + 1 Coal |
| Tin Bar | 5 Tin Ore + 1 Coal |
| Diamond Bar | 5 Diamond Ore + 1 Coal |

## Economy baseline

Giá là gold mỗi item. `Buy` là giá người chơi mua từ NPC; `Sell` là giá NPC trả cho người chơi.
Merric mua lại và bán cho người chơi toàn bộ ore, bar, Leather, Wood Log và Coal trong bảng dưới.
Năm gem do Agnes — Hermetist (`npc.town.hermetist`) mua và bán tại `shop.town.trader_magic`.
`Buy min–max` là giá người chơi trả; `Sell min–max` là giá Merric hoặc Agnes trả cho người chơi.

| Item | Stable ID | Buy min–max | Sell min–max | NPC nhận mua |
|---|---|---:|---:|---|
| Copper Ore | `item.material.copper_ore` | 6–8 | 3–4 | Merric |
| Iron Ore | `item.material.iron` | 10–14 | 5–7 | Merric |
| Gold Ore | `item.material.gold_ore` | 20–28 | 10–14 | Merric |
| Tin Ore | `item.material.tin_ore` | 14–18 | 7–9 | Merric |
| Diamond Ore | `item.material.diamond_ore` | 36–48 | 18–24 | Merric |
| Copper Bar | `item.material.copper_bar` | 44–54 | 22–27 | Merric |
| Iron Bar | `item.material.iron_bar` | 68–86 | 34–43 | Merric |
| Gold Bar | `item.material.gold_bar` | 122–156 | 61–78 | Merric |
| Steel Bar | `item.material.steel_bar` | 250–300 | 125–150 | Merric |
| Tin Bar | `item.material.tin_bar` | 88–108 | 44–54 | Merric |
| Diamond Bar | `item.material.diamond_bar` | 210–270 | 105–135 | Merric |
| Leather | `item.material.leather` | 16–24 | 8–12 | Merric |
| Wood Log | `item.material.wood_log` | 4–6 | 2–3 | Merric |
| Coal | `item.material.coal` | 9–12 | 5–7 | Merric |
| Tanzanite | `item.material.tanzanite` | 80–110 | 40–55 | Agnes |
| Citrine | `item.material.citrine` | 36–48 | 18–24 | Agnes |
| Opal | `item.material.opal` | 48–64 | 24–32 | Agnes |
| Charoite | `item.material.charoite` | 60–80 | 30–40 | Agnes |
| Emerald | `item.material.emerald` | 110–150 | 55–75 | Agnes |

## Khoảng trống content cần bổ sung

- Sheet không chỉ định crafting station. `station.forge` là station đã chốt cho toàn bộ refining, kể cả Coal.
- Merric vừa bán vừa mua lại 14 resource theo yêu cầu ngày 2026-09-29; recipe output cũng thuộc danh sách giao dịch này.
- Năm gem dùng icon tham chiếu từ sheet. Ore/bar/leather/coal dùng các sprite phù hợp trong atlas material hiện có; đây là art tạm và có thể thay mà không đổi stable ID.
- Bốn node Iron/Gold/Tin/Diamond mới chỉ có `ResourceNodeDefinition`; chưa có world sprite, prefab hoặc vị trí scene.
- Dòng Leather có nội dung mô tả nhắc Hunter's Hut nhưng cột Shop ghi Artificer. Merric là tên hiển thị của Artificer hiện hành nên shop Merric được ưu tiên theo cột dữ liệu có cấu trúc.

## Items List expansion — D-119

2026-10-22 owner revision: `item.material.fish_fillet` is a stackable material at Gastronome. One selected fish instance (any current species, any weight) yields two fillets via crafting; price remains pending. Harvest Stew costs 2 Meat + 2 Carrot + 2 Mushroom + 2 Chillie. Trailrunner Fish Broth costs 2 Fish Fillet + 2 Lemon + 2 Carrot + 2 Cucumber and moves to Gastronome. Orchard Elixir costs one each Lemon/Apple/Carrot/Grape Juice; each Juice uses two corresponding produce. Stable output IDs and buff effects unchanged. See SheetContentExpansion.md for current verification.

New stable IDs: item.material.meat, item.material.bone, item.material.bone_ash, item.material.slime, item.material.health_herb and item.material.mana_herb. Bone refining outputs three Bone Ash per Bone; NPC/station remain empty. Tree drop is exactly three Wood Log; a presentation-only ResourceVisualVariants chooses among existing tree sprites. Health/Mana plants drop three matching herbs. Seven reusable node prefabs and three wildlife prefabs are authored under Assets/Prefabs/SheetContent; placed DemoScene nodes use explicit registry entries and unique persistent IDs. Wildlife is ordinary respawn-on-scene-load content; dropped loot is transient, retained when inventory is full. Unknown commerce prices are explicitly marked pending, not converted into free stock. Legacy ore/bar/gem IDs, Coal/Merric and station.forge decisions are unchanged.

### NPC shop price completion

Internal gold ranges for the remaining standard shop stock. The Buy UI displays the quoted
unit price for the current shop opening.

| Item | Buy | Sell |
|---|---:|---:|
| Carrot Juice | 10–12 | 4–5 |
| Grape Juice | 14–16 | 6–7 |
| Lemon Juice, Apple Juice | 18–21 | 8–10 |
| Orchard Elixir | 70–82 | 28–34 |
| Meat | 6–8 | 3–4 |
| Fish Fillet | 5–7 | 2–3 |
| Harvest Stew | 40–46 | 16–20 |
| Trailrunner Fish Broth | 46–54 | 20–24 |

Carrot and Grape Juice use standard prices during and outside the Act I quest.

### Animated hunting animals — 2026-11-25

Reusable animated prefabs for integration are `Assets/Prefabs/Animal/Boar.prefab`,
`Deer.prefab` and `Hare.prefab`. The four-direction 32×32 frames come from the shadowed
Boar, Deer and Hare sheets in the Top-Down Hunt Animals pack under Tiled. Dedicated sliced
copies under `Assets/Animations/Animals` preserve the original Tiled textures and their
import references.

All three have Idle, Walk, Run, Hurt and Death. Boar also has Attack in four directions.
WildlifeActor feeds facing and movement to the optional Animator, sends the contact-attack
and hurt triggers, and keeps the animated body alive through its death clip. Existing loot,
combat values and one-time quest kill handling still come from the corresponding definitions.
Hare is the visual prefab for `animal.rabbit`. Prefabs remain available for DemoScene-first
placement and do not add instances to a production scene.

The animated Boar, Deer and Hare share the enemy health bar art. Their bars appear after
damage, update from WildlifeActor health and hide on death. Boar stops while playing
Attack or Hurt. Each directional attack applies contact damage once at its animation
event if the player is still in range. A hit that interrupts the windup cancels its damage.

Depth sorting (2026-11-27, owner request): every wildlife prefab (`Prefabs/Animal` and
`Prefabs/SheetContent/Wildlife`) renders on `Default/0` with `Sprite Sort Point = Pivot`, the same
layer/order as Player, crops and `Forest_Tree` tiles, so the project's Y transparency-sort axis orders
them by the feet pivot. The world-space health bar canvas stays on `Default/20` above actors.
`SheetContentAuthoring` writes the same values when it rebuilds the fixtures.

### MapDuy encounter levels — 2026-11-26

MapDuy ordinary enemies and wildlife roll one inclusive random level when the actor initializes.
The scene stores both ends of each range as prefab instance overrides. The roll is independent
of position and remains fixed for that actor's lifetime. Slime and Hare use 1–4, Goblin and Deer
use 4–7, and Ent and Boar use 7–10. The same prefabs default to level 1 elsewhere.

Each level above 1 adds 10% of base maximum HP and 5% of base attack damage. Passive animals
continue to flee. Existing XP, loot and quest identities stay the same. The shared world-space
health bar shows the authored display name and rolled level above the fill when the bar appears.

### Knacker in MapDuy

Knacker trades Slime (buy 6–8 G, sell 2–3 G), Bone (buy 10–12 G, sell 4–5 G), and Bone Ash (buy 4–5 G, sell 1–2 G). His dialogue opens the shop or the crafting window. His crafting station converts one Bone into three Bone Ash.

### Herbalist in MapDuy

Herbalist buys and sells all four Health/Mana potion variants and both herbs. Crafting offers exactly the four potion recipes at station.herbalist using their existing ingredients. Herbs trade at 5–7 G buy and 2–3 G sell, small potions at 20/10 G, large potions at 42–48/16–19 G. The Small Health Potion quote preserves Cenhelm’s existing 20 G purchase and 10 G sell-back.

Wildlife death loot displays a ground icon for each item type, with a short delay before proximity pickup. The full bundle still uses one atomic inventory grant and stays in the scene when inventory is full. Hare drops 1 Meat and 1 Leather, Deer 2 Meat, 3 Leather and 1 Bone, and Boar 3 Meat, 2 Leather and 1 Bone.

### Slaughter drops

Goblin awards 50 EXP and 100 G, drops 3 Bone, and has a 5% chance to drop Earth Orb.
Slime awards 25 EXP and 50 G, drops 3 Slime, and has a 5% chance to drop Water Orb.
Ent (Woodling) awards 100 EXP and 150 G, drops 10 Wood Log, and has a 10% chance to drop Wind Orb.
Hare (Rabbit), Deer and Boar award 10, 5 and 20 EXP respectively. Their item quantities follow the existing wildlife table.
EXP and gold are granted on death. Items use the ground-loot pickup.
