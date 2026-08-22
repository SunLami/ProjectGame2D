# Data Asset, Catalog and Stable ID Inventory

Ngày kiểm kê: 2026-08-21  
Phạm vi: custom ScriptableObject types trong `Assets/Scripts` và mọi asset instance của các type đó.
Unity/package assets như Tile, SpriteLibraryAsset, InputActionAsset, font và render settings được tính là
referenced assets, không phải custom gameplay definition domain trong bảng này.

## Custom ScriptableObject types

| Type | Vai trò | Asset instances | Có stable ID? | Catalog/resolver hiện tại |
|---|---|---:|---|---|
| `ItemSO` | Base item definition | 0 direct; 60 subclass instances | `itemId` | `ItemLookup` quét Resources |
| `EquipmentItemSO` | Equipment definition + stats/visual | 60 | Kế thừa `itemId` | `EquipmentCatalog` và `ItemDatabase` |
| `ItemDatabase` | Starting-item entries + amount | 1 | Không cần content ID ở vai trò fixture | Scene `InventorySeeder` dùng trực tiếp |
| `EquipmentCatalog` | Mảng equipment theo slot | 1 | Không có catalog ID | Typed asset references |
| `TileDataSO` | Tile surface → footstep audio | 1 | Không có | `MapManager` quét `Resources/TileDatas` |

Chưa có custom ScriptableObject definition/catalog cho Enemy, Quest, Tutorial, Recipe, Shop, NPC,
Area/Spawn hoặc world persistent entity. Những domain này phải được tạo data-driven từ đầu ở phase tương ứng.

## Equipment item inventory

Có 60 `EquipmentItemSO` assets:

| Slot | Số asset | Level hiện có |
|---|---:|---|
| Head | 6 | 4–9 |
| Body | 8 | 2–9 |
| Weapon | 9 | 1–9 |
| Ring | 10 | 1–10 |
| Necklace | 10 | 1–10 |
| Foot | 8 | 1–8 |
| Shield | 9 | 1–9 |

Kết quả kiểm tra:

- 0 ID rỗng.
- 0 ID trùng trong 60 assets.
- 60/60 có icon, description và policy non-stackable/max stack 1 phù hợp equipment.
- 23 visual equipment assets thuộc Head/Body/Weapon đều có SpriteLibraryAsset.
- 37 Ring/Necklace/Foot/Shield chưa có SpriteLibraryAsset; hiện phù hợp vì runtime visual switch chỉ xử lý Head/Body/Weapon.
- `headSpriteLibraryAsset` optional đang null trên toàn bộ assets; không phải missing required reference.
- Không tìm thấy runtime code ghi ngược vào fields của ItemSO/TileDataSO.

## Catalog completeness

| Asset | References | Unique | Missing GUID | Nhận xét |
|---|---:|---:|---:|---|
| `ItemDatabase.asset` | 60 | 60 | 0 | Hiện chứa toàn bộ equipment với amount 1 |
| `EquipmentCatalog.asset` | 60 | 60 | 0 | Bao phủ đúng toàn bộ 60 equipment assets theo slot |
| `RockTileData.asset` | 1089 tiles + 9 walk clips + 10 run clips | 1108 | 0 | Một surface definition rất lớn, load qua Resources |

`ItemDatabase` hiện không phải catalog trung lập: nó được `InventorySeeder` dùng để cấp toàn bộ 60 item
khi scene start. Đây là demo fixture/technical debt, không được dùng làm New Game default production.

`ItemLookup.BuildFromResources` ghi `lookup[itemId] = item`; nếu sau này có duplicate ID, asset sau sẽ
ghi đè âm thầm. Project chưa có catalog validator/build gate.

## Stable ID audit

Toàn bộ 60 item IDs đang theo dạng legacy như `sword_lvl1`, `body_lvl9`, `ring_lvl10`:

- Lowercase ASCII và unique: đạt.
- Có semantic content: đạt một phần.
- Dot-separated domain hierarchy theo source-of-truth: **không đạt**.
- Validator/alias/migration: chưa có.

Không bulk rename ID ở Phase 0. Trước khi save schema production được phát hành, Phase 4 phải chuyển
sang convention chuẩn hoặc khai báo legacy IDs là alias. Ví dụ mapping định hướng:

```text
sword_lvl1   → item.weapon.sword.level.001
body_lvl2    → item.armor.body.level.002
head_lvl4    → item.armor.head.level.004
ring_lvl10   → item.accessory.ring.level.010
```

Tên display, filename và Unity GUID không được thay thế stable ID trong save.

## Stable ID convention áp dụng cho content mới

Regex nền tảng:

```text
^[a-z0-9]+(?:\.[a-z0-9_]+)+$
```

Namespace ban đầu:

| Domain | Pattern | Ví dụ |
|---|---|---|
| Item/equipment | `item.<category>.<name>[.<variant>]` | `item.weapon.sword.iron.001` |
| Enemy definition | `enemy.<family>.<variant>` | `enemy.slime.green` |
| Quest | `quest.<chain>.<name>[.<sequence>]` | `quest.tutorial.crafting.001` |
| Tutorial step | `tutorial.<flow>.<step>` | `tutorial.controls.move` |
| NPC | `npc.<area>.<role>` | `npc.town.blacksmith` |
| Recipe | `recipe.<category>.<output>` | `recipe.weapon.sword.iron` |
| Shop | `shop.<area>.<owner_or_type>` | `shop.town.blacksmith` |
| Area | `area.<name>` | `area.tutorial` |
| Spawn | `spawn.<area>.<name>` | `spawn.tutorial.start` |
| Surface definition | `surface.<name>` | `surface.rock` |
| Persistent world instance | `world.<kind>.<area>.<unique>` | `world.chest.town.blacksmith.01` |

Definition ID, persistent instance ID và runtime instance ID là ba khái niệm riêng. ID đã xuất hiện
trong save không được tái sử dụng hoặc đổi mà không có alias/migration.

## Data boundary findings

- Item: đã có Definition (`ItemSO`), Runtime (`InventorySlot`) và Save DTO (`InventorySaveData`), nhưng
  resolver/validation chưa đạt contract và fields definition vẫn public mutable.
- Equipment: definition tốt ở mức data variation; runtime equipped state còn nằm trong manager và chưa có save DTO.
- Tile surface: definition tồn tại nhưng không có ID/catalog explicit; runtime service phụ thuộc Resources và scene Tilemap.
- Enemy: attack profiles là serialized prefab data, chưa có EnemyDefinition/stable enemy ID/catalog.
- Quest/Tutorial/Crafting/Shop/NPC/World: chưa có definition/runtime/save implementation.

## Migration gates

### Trước Phase 2 save schema

- Chỉ cho phép save fields dùng stable ID namespace đã document.
- Không serialize Unity GUID, filename, GameObject name hoặc array index.

### Phase 4 item/equipment

- Chốt mapping 60 legacy item IDs trước khi có save thật.
- Tạo một item catalog/resolver contract; Resources có thể là backend tạm.
- Validator chặn empty, duplicate, invalid format và missing catalog reference.
- Tách `ItemDatabase` thành explicit New Game/demo loadout thay vì giả làm catalog.
- Thêm equipment save DTO bằng item ID và transaction validation.

### Phase 5–8 domain mới

- Mỗi domain tạo Definition + Runtime + Save DTO + Catalog/Resolver cùng lúc.
- Mỗi catalog có duplicate/missing/cross-reference validator.
- Có ít nhất hai variants dùng cùng runtime handler trước khi tuyên bố data-driven.

## Verification checklist

- [x] Inventory mọi custom ScriptableObject type và instance.
- [x] Kiểm tra item ID rỗng/trùng/format.
- [x] Kiểm tra catalog completeness và missing GUID.
- [x] Ghi stable ID namespace cho domain ban đầu.
- [x] Editor/catalog validator tồn tại; chạy qua `Tools/Project Game/Validate Content`.
- [ ] Legacy item ID mapping được chốt và áp dụng.
- [ ] Catalog/resolver là dependency explicit.
- [ ] New Game loadout tách khỏi toàn bộ item catalog.
- [ ] Enemy/Quest/Tutorial/Recipe/Shop/NPC/World definitions được tạo ở đúng phase.
