# Phase 4 Implementation Report

Ngày bắt đầu: 2026-08-22
Trạng thái: **Backend hoàn tất và verify; không có thay đổi UI-facing contract nên không cần Codex handoff**

## Quyết định phạm vi trước khi triển khai

Đã hỏi và được xác nhận trước khi code (xem [Decision Register](DecisionRegister.md) D-020/D-021/D-022):

1. **60 legacy itemId** (`sword_lvl1`, `body_lvl9`, ...) **giữ nguyên**, không bulk rename. Ghi nhận
   chính thức bằng D-022 thay vì đổi 60 asset — rủi ro cao, không phải thay đổi nhỏ nhất có thể kiểm
   chứng, và chưa có save nào release nên không có nhu cầu migration thật.
2. **Restore order** (progression → inventory → equipment → recalc stat → clamp health) được đặt
   trong `PlayerSpawnReadinessSource` đã có từ Phase 3 (mở rộng, không tạo `IGameplayReadinessSource`
   mới) — vì `GameplayReadinessGate` coi các source là độc lập/song song, không đảm bảo thứ tự giữa
   nhiều source; đặt toàn bộ chuỗi restore có thứ tự trong một component tránh vấn đề sequencing mà
   không cần sửa Gate.

## Baseline đã khảo sát trước khi code

- `InventoryManager`: static singleton, `List<InventorySlot>` (40→132 slot thực tế trong DemoScene),
  đã có sẵn `ToSaveData()`/`LoadFromSaveData()` nhưng **chưa từng được gọi ở đâu**.
- `EquipmentManager`: static singleton, `Dictionary<EquipSlot, EquipmentItemSO>`, **bug thật đã xác
  nhận**: `Equip()`/`Unequip()` gọi `InventoryManager.AddItem(...)` không kiểm tra kết quả — inventory
  đầy làm mất item vĩnh viễn.
- `ItemLookup`: `Resources.LoadAll` ghi đè âm thầm khi duplicate ID, không ai gọi.
- `InventorySeeder.Start()`: seed vô điều kiện mỗi lần scene load — đúng bug roadmap đã cảnh báo.
- `EquipmentCatalog`: asset tồn tại, không script nào tham chiếu; đã có validator riêng
  (`ContentValidationRunner`) nên không cần thêm.
- 0 test nào cho Inventory/Equipment trước Phase 4.

## Scope đã triển khai

### Resolver (D-020/D-021)

- `Assets/Scripts/Inventory/IItemResolver.cs` — `bool TryResolve(string itemId, out ItemSO item)`.
- `Assets/Scripts/Inventory/ResourcesItemResolver.cs` — thay `ItemLookup` (giữ nguyên, không xóa) bằng
  implementation báo lỗi rõ khi duplicate itemId thay vì ghi đè âm thầm.

### Save DTO

- `InventorySaveData` (đã tồn tại) — thêm `gold`.
- `EquipmentSaveData` mới — `List<{EquipSlot slot; string itemId;}>`, chỉ lưu slot có item.
- `GameSaveData` — thêm `inventory`/`equipment`; `CurrentSaveVersion` 2 → 3.
- `NewGameFactory.CreateDefault()` — `inventory`/`equipment` rỗng (seed sống, không bake).

### Transaction safety (bug thật đã sửa)

- `InventoryManager.HasCapacityFor(item, amount)` — capacity check thuần đọc, không mutate.
- `EquipmentManager.Equip()`/`Unequip()` — reorder để kiểm tra `HasCapacityFor` **trước khi** mutate
  bất kỳ state nào; thất bại thì không đụng gì (trước đây có thể mất item khi inventory đầy).

### Restore/capture

- `EquipmentManager.RestoreEquipped(slot, item)` — set trực tiếp, không qua `Equip()`/không đụng
  inventory. `RecalculateStats()` — public wrapper gọi một lần sau khi restore xong toàn bộ equipment.
  `ToSaveData()` — capture equipped state.
- `PlayerStat`: tách `RestoreProgression(level, exp, health)` cũ thành
  `RestoreProgression(level, exp)` + `RestoreHealth(health)` (giữ nguyên overload 3 tham số cũ) — để
  health chỉ được finalize **sau** khi equipment áp dụng xong, clamp đúng theo `MaxHealth` cuối cùng
  thay vì công thức delta của `ApplyEquipmentModifiers` (vốn thiết kế cho live equip, không phải restore).
- `InventorySeeder.Start()` xóa; thêm `SeedStartingInventory()` gọi tường minh.
- `PlayerSpawnReadinessSource` mở rộng: restore theo đúng thứ tự 6 bước (progression → position →
  inventory [seed nếu NewGame, else LoadFromSaveData qua resolver, log missing item recovery] →
  equipment [RestoreEquipped per slot] → RecalculateStats() một lần → RestoreHealth() cuối). Với
  NewGame, initial save (D-011) giờ capture cả inventory/equipment vừa seed, không chỉ player.

## Files thay đổi

Mới: `IItemResolver.cs`, `ResourcesItemResolver.cs`, `EquipmentSaveData.cs`,
`Tests/EditMode/ResourcesItemResolverTests.cs`, `Tests/EditMode/InventoryEquipmentSaveDataTests.cs`,
`Tests/PlayMode/InventoryManagerPlayModeTests.cs`, `Tests/PlayMode/EquipmentManagerPlayModeTests.cs`.

Sửa: `InventorySaveData.cs` (gold), `InventoryManager.cs` (HasCapacityFor, gold trong
save/restore, overload LoadFromSaveData qua resolver), `EquipmentManager.cs` (transaction reorder,
RestoreEquipped, RecalculateStats, ToSaveData), `PlayerStat.cs` (split RestoreProgression/
RestoreHealth), `InventorySeeder.cs` (bỏ auto-seed), `GameSaveData.cs` (version 3),
`NewGameFactory.cs`, `PlayerSpawnReadinessSource.cs`,
`Tests/PlayMode/PlayerSpawnReadinessSourcePlayModeTests.cs` (+4 test tích hợp).

Scene (Unity MCP): `DemoScene/_SceneContext/PlayerSpawnReadinessSource._inventorySeeder` wired tới
`InventoryManager/InventorySeeder` đã có sẵn.

## Tests

- EditMode: 26/26 PASS (6 mới: resolver x3, DTO round-trip x3).
- PlayMode: 26/26 PASS (15 mới):
  - `InventoryManagerPlayModeTests` (6): stack/max-stack, remove across stacks, insufficient qty,
    `HasCapacityFor` matches `AddItem`, save/load round-trip gold+slots, missing-item recovery report.
  - `EquipmentManagerPlayModeTests` (6): equip moves item + applies stat, replaced item returns to
    source slot, **replaced item không có chỗ trả về → Equip thất bại không mất gì**, **Unequip đầy
    inventory → thất bại không mất gì**, RestoreEquipped không đụng inventory + RecalculateStats
    idempotent, ToSaveData chỉ capture slot có item.
  - `PlayerSpawnReadinessSourcePlayModeTests` (+3): New Game seed đúng một lần + initial save capture
    đúng inventory seeded; Continue restore inventory/equipment/gold từ save thật (itemId thật qua
    `ResourcesItemResolver`) mà không seed lại; Load Slot A → Slot B không rò item (fresh
    InventoryManager mỗi lần, đúng cơ chế `GameplaySceneLifetime` production).
- Content Validation: không đổi — 0 error, 60 warning, 63 asset.
- Scene validator MainMenu/DemoScene: 0 issue.

## Manual verification (Play Mode thật, `InMemorySaveSlotRepository`, không đụng save thật)

- `RequestNewGame(1)`: 60 item được seed (đúng nội dung `ItemDatabase.asset` hiện tại — xem "Chưa
  hoàn thành" bên dưới), `UsedSlots=60`, initial save có 132 slot entries (đúng full capacity).
- `TryReturnToMainMenu()` → `RequestContinue(1)`: `UsedSlots=60` (không tăng lên 120) — xác nhận không
  seed lại.
- Console sạch trong toàn bộ kịch bản.

## Chưa hoàn thành / để lại cho phase sau

- **`ItemDatabase.asset` vẫn chứa cả 60 equipment** (amount 1 mỗi loại) thay vì một starting loadout
  đã curate — đây là nội dung/thiết kế, không phải kiến trúc; Phase 4 chỉ sửa CÁCH nó được seed (đúng
  một lần cho New Game), không sửa NỘI DUNG bên trong asset. Cần người dùng/designer quyết định loadout
  thật.
- `EquipmentCatalog.asset` vẫn không được tham chiếu ở đâu — để dành cho Shop (Phase 7) hoặc UI catalog
  picker sau này; không phải bug.
- Chưa có migration V1/V2 (không có `inventory`/`equipment`) → V3 — không cần vì chưa có save nào ở
  version cũ được release.
- Tutorial/Quest/World persistence — Phase 5/6/8.
- Player vẫn `DontDestroyOnLoad` static singleton (quyết định giữ nguyên từ Phase 3).

## Codex UI Handoff

**Không cần.** Toàn bộ signature UI-facing hiện có giữ nguyên:

- `InventoryManager.Slots`, `.Gold`, `.OnInventoryChanged` — không đổi.
- `InventorySlot.item`/`.quantity` — không đổi.
- `EquipmentManager.Equip(item, sourceSlot)`, `Unequip(slot)`, `Unequip(slot, targetSlot)`,
  `GetEquipped(slot)`, `.OnEquipmentChanged` — signature không đổi, chỉ sửa nội bộ để transaction an
  toàn hơn (equip/unequip khi inventory đầy giờ trả `false` thay vì mất item — UI hiện tại đã coi
  `Equip`/`Unequip` trả `bool` nên hành vi false này không cần code UI mới, nhưng nếu Codex muốn hiển
  thị lý do thất bại rõ hơn cho "inventory đầy" thì đó là việc UI có thể làm sau, không bắt buộc).

`InventorySlotUI.cs`, `EquipmentSlotUI.cs`, `GoldUI.cs`, `InventoryUI.cs` không cần sửa gì.
