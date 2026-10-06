# Resource Content

Nguồn content: Google Sheet `Items List`, tab `Resources` (`gid=1236028827`), tích hợp ngày 2026-09-28.
Description được chủ động bỏ qua theo yêu cầu hiện tại.

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
