# Save, Slots and World Persistence Plan

Save contract tuân theo mô hình Definition/Runtime/Save và stable ID trong
[Data-Driven Development Guide](DataDrivenDevelopment.md). Save không phải bản sao của ScriptableObject catalog.

## Save layout

Khuyến nghị lưu trong `Application.persistentDataPath/Saves`:

```text
Saves/
├─ Slot1/
│  ├─ metadata.json
│  ├─ save.json
│  └─ save.backup.json
├─ Slot2/
└─ Slot3/
```

Không dùng `PlayerPrefs` cho game save. PlayerPrefs chỉ phù hợp settings nhỏ như volume/display.

## Metadata contract

Metadata dùng để render slot mà không deserialize toàn bộ world:

```csharp
[Serializable]
public sealed class SaveSlotMetadata
{
    public int slotIndex;
    public string saveId;
    public int saveVersion;
    public string characterName;
    public int characterLevel;
    public long totalPlayTimeSeconds;
    public string areaId;
    public long lastSavedUtcTicks;
    public bool tutorialCompleted;
    public string contentChecksum;
}
```

Metadata có thể tái tạo từ `save.json` nếu bị mất, nhưng không được là nguồn dữ liệu progression duy nhất.

**Phase 2 implementation note (2026-08-22):** `contentChecksum` (SHA-256 hex của nội dung `save.json`
đã ghi) được thêm vào contract để đáp ứng yêu cầu "checksum hoặc validation tối thiểu cho JSON" của
Roadmap Phase 2. `GameSaveData` ở Phase 2 chỉ có `saveVersion`, `saveId`, `totalPlayTimeSeconds` — các field
player/inventory/equipment/tutorial/quest/world sẽ được phase tương ứng thêm sau, bump
`GameSaveData.CurrentSaveVersion` khi shape đổi.

**Phase 3 fix (2026-08-22):** `FileSaveSlotRepository.GetSlotInfo` ban đầu không hề đọc lại
`metadata.json` đã ghi (luôn dựng lại metadata từ `save.json`, bỏ qua `lastSavedUtcTicks` thật). Đã sửa
để ưu tiên đọc `metadata.json` (khớp `saveId`+`contentChecksum` với save đang load), chỉ fallback dựng
lại từ `GameSaveData` khi `metadata.json` mất/hỏng. `characterLevel`/`areaId` giờ lấy từ
`GameSaveData.player`; `characterName`/`tutorialCompleted` vẫn cố định mặc định vì chưa có domain tương
ứng — xem [Phase 3 Implementation Report](Phase3ImplementationReport.md).

## Root save contract

```csharp
[Serializable]
public sealed class GameSaveData
{
    public int saveVersion;
    public string saveId;
    public PlayerSaveData player;
    public InventorySaveData inventory;
    public EquipmentSaveData equipment;
    public TutorialSaveData tutorial;
    public QuestSaveData quests;
    public WorldSaveData world;
    public TimeSaveData time;
    public long totalPlayTimeSeconds;
}
```

Mỗi domain tự capture/restore DTO của mình qua interface hẹp; SaveManager không đọc private runtime
field của tất cả manager.

```csharp
public interface ISaveParticipant<TData>
{
    TData CaptureSaveData();
    void RestoreSaveData(TData data, SaveRestoreContext context);
}
```

Trong implementation thực tế có thể dùng coordinator explicit thay vì registry generic nếu registry
làm dependency/order khó nhìn. Restore order phải được code rõ và test.

## New Game defaults

Default snapshot phải được tạo bởi `NewGameFactory`, không lấy từ state ngẫu nhiên trong scene:

- Stable `saveId` mới.
- `areaId = tutorial_area`.
- `spawnId = tutorial_start` hoặc default position tương ứng.
- Level/base stats mặc định.
- Starter inventory chính xác một lần.
- Equipment mặc định.
- Tutorial step đầu.
- Quest lists sạch; Tutorial Quest chưa tự nhận nếu design yêu cầu nói chuyện NPC.
- World state sạch và seed/version nếu cần.

Default data cần test độc lập để designer đổi starter content không tạo save invalid.

## Player location

Lưu cả area và position:

```csharp
[Serializable]
public sealed class PlayerLocationSaveData
{
    public string sceneId;
    public string areaId;
    public float positionX;
    public float positionY;
    public string fallbackSpawnId;
}
```

Load policy:

1. Resolve scene bằng stable `sceneId` hoặc scene mapping của area.
2. Nếu area hợp lệ và position nằm trong playable bounds, dùng saved position.
3. Nếu position invalid, dùng `fallbackSpawnId` của area.
4. Nếu scene/area mất, dùng global safe spawn và ghi recovery warning.

Không save khi player đang ở transitional trigger hoặc ngoài bounds nếu chưa normalize vị trí an toàn.

## Atomic save transaction

```text
Capture immutable snapshot on main thread
→ validate snapshot
→ serialize to temp file
→ flush/close temp
→ validate temp can be read
→ rotate current save to backup
→ atomically replace current with temp
→ update metadata last
```

Nếu bất kỳ bước nào fail:

- Không xóa save hợp lệ trước đó.
- Không đánh dấu metadata bằng timestamp mới.
- Return GameState về state trước trong `finally`.
- Trả error code có thể hiển thị, không chỉ `Debug.Log`.

Chỉ cho một write operation trên một slot tại một thời điểm.

## Validation và versioning

Validation tối thiểu:

- `saveVersion` nằm trong supported range.
- `saveId` không rỗng và khớp metadata.
- DTO required không null.
- Numeric values finite và trong bounds.
- Quantity/level/currency không âm.
- Item/quest IDs được resolve hoặc đưa vào recovery report.
- Duplicate persistent IDs trong save được reject/merge theo policy rõ.

Migration chạy theo chuỗi:

```text
V1 → V2 → V3 → Current
```

Không viết migration `V1 → Current` riêng cho mỗi version vì dễ thiếu đường nâng cấp.

**Phase 2 hiện trạng:** chưa có migration pipeline (thuộc Phase 10). `FileSaveSlotRepository` hiện chỉ
chấp nhận `saveVersion == GameSaveData.CurrentSaveVersion`; mọi version khác (cũ hơn hoặc mới hơn) đều
báo `SaveSlotStatus.IncompatibleVersion` thay vì cố load một phần, đúng nguyên tắc "không load/overwrite
âm thầm" ở trên.

**Phase 3 hiện trạng (2026-08-22):** thêm `PlayerSaveData` (`level`, `currentExperience`, `health`,
`location`) và `PlayerLocationSaveData` (`sceneId`, `areaId`, `positionX`, `positionY`,
`fallbackSpawnId`) vào `GameSaveData.player`; bump `CurrentSaveVersion` 1 → 2 vì shape đổi. `health < 0`
là sentinel "dùng MaxHealth hiện tại" cho New Game. `positionX`/`positionY` là `NaN` khi chưa có vị trí
đã lưu (New Game) — restore đọc `HasSavedPosition`, nếu false thì resolve `fallbackSpawnId` qua
`SpawnRegistry` đúng theo load policy đã mô tả. `NewGameFactory.CreateDefault()` (pure C#,
`Assets/Scripts/Save/NewGameFactory.cs`) tạo snapshot mặc định với `areaId = area.tutorial`,
`fallbackSpawnId = spawn.tutorial.start`. Chưa có inventory/equipment/tutorial/quest/world trong
`GameSaveData` — sẽ thêm ở phase tương ứng.

**Phase 4 hiện trạng (2026-08-22):** thêm `GameSaveData.inventory` (`InventorySaveData` — đã tồn tại
thử nghiệm từ trước, nay bổ sung `gold`) và `GameSaveData.equipment` (`EquipmentSaveData` mới —
`List<{EquipSlot slot; string itemId;}>`, chỉ lưu slot đang có item). Bump `CurrentSaveVersion` 2 → 3.
`IItemResolver`/`ResourcesItemResolver` (D-020) là cầu nối itemId ↔ `ItemSO` cho cả capture lẫn
restore. `NewGameFactory.CreateDefault()` để `inventory`/`equipment` rỗng — starting loadout được seed
sống (live) đúng một lần cho New Game rồi capture lại vào initial save, không bake sẵn trong factory.
Restore order thật (`PlayerSpawnReadinessSource`): progression (không health) → position → inventory
(seed nếu NewGame, else `LoadFromSaveData` qua resolver) → equipment (`RestoreEquipped` per slot, không
qua `Equip()` UI path) → `RecalculateStats()` đúng một lần → `RestoreHealth()` cuối cùng (clamp theo
MaxHealth cuối cùng sau equipment, không dùng công thức delta của `ApplyEquipmentModifiers`) → tutorial
(`TutorialManager.RestoreState`, không phát `OnStepChanged`/`OnTutorialCompleted`).

**Phase 5 hiện trạng (2026-08-22):** thêm `GameSaveData.tutorial` (`TutorialSaveData` —
`currentStepId`, `completed`). Bump `CurrentSaveVersion` 3 → 4. Chỉ phần **Input tutorial**
(Move/Sprint/Attack/OpenInventory/EquipItem/ReachArea) — Tutorial Quest chain là Phase 6, cần
NPC/Quest system chưa tồn tại. `currentStepId = null` + `completed = false` nghĩa là "bắt đầu step
đầu tiên" khi restore. `TutorialManager` (`Assets/Scripts/Tutorial/`) subscribe domain event
(`Player.PlayerMoved/PlayerSprinted/PlayerAttacked`, `InventoryWindowUI.InventoryOpened`,
`EquipmentManager.ItemEquipped`, `AreaTriggerZone.PlayerEnteredArea`) — không đọc phím cụ thể, remap
vẫn hoàn thành được tutorial.

**Phase 6 hiện trạng (2026-08-22):** thêm `GameSaveData.quests` (`QuestSaveData` — danh sách
`QuestProgressSaveData { questId, status, currentObjectiveIndex, objectiveCounters }`). Bump
`CurrentSaveVersion` 4 → 5. Chỉ quest đã Active/ReadyToTurnIn/Completed được lưu; `Locked`/
`Available` luôn derive lại từ `prerequisiteQuestIds` lúc load (không lưu, đúng nguyên tắc "không
tin một bool duy nhất" của Main Quest gate). `QuestManager.RestoreState` set state trực tiếp qua
`QuestRuntimeState.RestoreProgress`, không phát `QuestAccepted`/`QuestProgressChanged`/
`QuestCompleted`/`MainQuestUnlocked`. `MainQuestUnlocked` (cached bool trên `QuestManager`) được
reconciliation lại mỗi lần `RestoreState`/`TryTurnIn` chạy bằng cách quét toàn bộ quest
`isTutorialQuest` trong catalog và kiểm tra `Completed`, không chỉ đọc lại giá trị cache cũ. Quest
không resolve được qua `QuestCatalog` (content bị xoá/đổi ID) bị drop kèm `Debug.LogWarning`, không
crash toàn save. `PlayerSpawnReadinessSource` thêm bước restore quest sau tutorial (bước 8) và ghi
`quests` vào initial save của New Game giống các domain khác.

## Inventory/equipment persistence

- Serialize item bằng stable `itemId`, không serialize ScriptableObject reference.
- Equipment save theo `EquipSlot → itemId`.
- Restore inventory trước equipment hoặc dùng DTO tách rõ ownership để không nhân đôi item.
- Recalculate stat sau khi equipment hoàn tất.
- Không gọi gameplay `Equip()` thông thường nếu method đó di chuyển item/phát event; cần restore API riêng.
- Item resolver là dependency explicit; backend Resources hiện tại không được rò vào SaveManager.

## Quest/tutorial persistence

- Lưu objective counter và quest status, không lưu UI state.
- Restore không phát reward hoặc objective-completed event.
- Tutorial lưu step hiện tại và completed flag.
- Main Quest unlock phải suy ra/validate từ prerequisite hoặc lưu story flag có reconciliation.

**Phase 6 implementation:** đúng theo các nguyên tắc trên — xem chi tiết contract, event và test
matrix trong [ClaudeToCodex.md](Handoffs/ClaudeToCodex.md) và `Assets/Scripts/Quest/`.

## World persistence policy

Không serialize mọi GameObject. Chia ba nhóm:

1. **Persistent unique:** chest, unique pickup, boss, story switch.
2. **Persistent timed:** resource node với next respawn timestamp.
3. **Rule-based transient:** enemy thường, VFX, projectile, dropped temporary object.

Mỗi object persistent có ID ổn định không phụ thuộc GameObject name/hierarchy. Editor validator phải phát hiện:

- ID rỗng.
- ID trùng trong DemoScene, world scene hoặc prefab placement.
- Prefab duplicate vô tình giữ cùng instance ID.

World DTO nên là danh sách record theo ID và payload nhỏ. Khi content bị xóa ở version mới, record không
resolve được được bỏ qua kèm warning thay vì làm hỏng toàn save.

## Save triggers phiên bản đầu

- Manual Save từ Pause Menu vào active slot.
- Initial save sau New Game restore thành công.
- Optional save khi Return Main Menu nếu người chơi xác nhận.
- Optional save khi Quit Desktop nếu người chơi xác nhận.

Chưa triển khai autosave định kỳ ở phase đầu. Khi thêm autosave, dùng cùng transaction/repository và
không ghi đè manual backup mà không có policy riêng.

**Phase 9 implementation note (2026-08-23):** cả 4 trigger trên đều đi qua cùng một điểm capture
(`GameplaySessionController` → `PlayerSaveCapture` + từng domain `ToSaveData()` →
`ISaveSlotRepository.WriteSave`), không có "hệ thống save thứ hai". Dirty-session tracking
(`GameSessionManager.IsDirty`, xem D-024) quyết định khi nào Return/Quit cần hỏi xác nhận; save
thành công luôn `ClearDirty()`. Chi tiết đầy đủ: [Phase9ImplementationReport.md](Phase9ImplementationReport.md).
