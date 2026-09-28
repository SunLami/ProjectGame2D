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

Tất cả recipe dưới đây dùng `station.forge` và `npc.town.artificer`; sheet để trống cột Crafting Stations.

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
Resource hiện chỉ được thêm vào Sell catalog để không cho phép bỏ qua vòng lặp khai thác/chế tạo.

| Item | Stable ID | Buy min–max | Sell min–max | NPC nhận mua |
|---|---|---:|---:|---|
| Copper Ore | `item.material.copper_ore` | 6–8 | 3–4 | Artificer |
| Iron Ore | `item.material.iron` | 10–14 | 5–7 | Artificer |
| Gold Ore | `item.material.gold_ore` | 20–28 | 10–14 | Artificer |
| Tin Ore | `item.material.tin_ore` | 14–18 | 7–9 | Artificer |
| Diamond Ore | `item.material.diamond_ore` | 36–48 | 18–24 | Artificer |
| Copper Bar | `item.material.copper_bar` | 44–54 | 22–27 | Artificer |
| Iron Bar | `item.material.iron_bar` | 68–86 | 34–43 | Artificer |
| Gold Bar | `item.material.gold_bar` | 122–156 | 61–78 | Artificer |
| Steel Bar | `item.material.steel_bar` | 250–300 | 125–150 | Artificer |
| Tin Bar | `item.material.tin_bar` | 88–108 | 44–54 | Artificer |
| Diamond Bar | `item.material.diamond_bar` | 210–270 | 105–135 | Artificer |
| Leather | `item.material.leather` | 16–24 | 8–12 | Artificer |
| Wood Log | `item.material.wood_log` | 4–6 | 2–3 | Artificer |
| Coal | `item.material.coal` | 9–12 | 5–7 | Không có |
| Tanzanite | `item.material.tanzanite` | 80–110 | 40–55 | Hermetist |
| Citrine | `item.material.citrine` | 36–48 | 18–24 | Hermetist |
| Opal | `item.material.opal` | 48–64 | 24–32 | Hermetist |
| Charoite | `item.material.charoite` | 60–80 | 30–40 | Hermetist |
| Emerald | `item.material.emerald` | 110–150 | 55–75 | Hermetist |

## Khoảng trống content cần bổ sung

- Sheet không chỉ định crafting station. Forge/Artificer là quy ước tạm thời cho toàn bộ refining, kể cả Coal.
- Shop column không nói rõ NPC bán hay mua. Tích hợp hiện coi đây là danh sách NPC nhận mua từ người chơi.
- Năm gem dùng icon tham chiếu từ sheet. Ore/bar/leather/coal dùng các sprite phù hợp trong atlas material hiện có; đây là art tạm và có thể thay mà không đổi stable ID.
- Bốn node Iron/Gold/Tin/Diamond mới chỉ có `ResourceNodeDefinition`; chưa có world sprite, prefab hoặc vị trí scene.
- Dòng Leather có nội dung mô tả nhắc Hunter's Hut nhưng cột Shop ghi Artificer. Vì description đang ngoài scope, Artificer được ưu tiên theo cột dữ liệu có cấu trúc.
