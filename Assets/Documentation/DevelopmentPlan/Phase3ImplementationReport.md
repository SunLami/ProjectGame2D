# Phase 3 Implementation Report

Ngày bắt đầu: 2026-08-22
Trạng thái: **In progress**

## Quyết định phạm vi trước khi triển khai

`ServiceOwnershipLifecycle.md` ghi đích Phase 3-4 là chuyển Player thành scene-spawned actor (bỏ
`DontDestroyOnLoad`/static singleton). Refactor đó đụng `PlayerMovement`, `PlayerCombat`,
`EquipmentManager`, `MapManager`, `SoundFXManager` — rủi ro cao, không phải "thay đổi nhỏ nhất có thể
kiểm chứng". Đã hỏi và được xác nhận: **giữ nguyên lifecycle Player hiện tại** (vẫn `DontDestroyOnLoad`,
static singleton, đặt cố định trong DemoScene), chỉ thêm restore API. Refactor lifecycle đầy đủ để lại
cho khi thật sự cần (world scene production hoặc multi-character).

## Scope đã triển khai

- `Assets/Scripts/Save/PlayerLocationSaveData.cs`, `PlayerSaveData.cs` — DTO theo đúng shape đã spec
  trong [Save and World Persistence Plan](SaveAndWorldPersistence.md). `GameSaveData.player` mới,
  `CurrentSaveVersion` 1 → 2.
- `Assets/Scripts/Save/NewGameFactory.cs` — pure C#, tạo `GameSaveData` mặc định:
  `areaId = area.tutorial`, `fallbackSpawnId = spawn.tutorial.start`, level 1, health sentinel (dùng
  MaxHealth), vị trí NaN (chưa có saved position).
- `Assets/Scripts/GameManagers/SpawnRegistry.cs` — scene service map `spawnId → Transform`, bind qua
  Inspector, không lookup theo tên.
- `PlayerStat.RestoreProgression(level, currentExperience, health)`
  ([PlayerStat.cs](../../Scripts/Player/PlayerStat.cs)) — set progression từ save mà không phát
  `OnLevelUp` (restore không được trigger reward-adjacent event).
- `GameSessionManager`: `GameSession` mang thêm `SaveData` (GameSaveData); thêm overload
  `TryStartNewGame`/`TryStartLoadedGame` nhận `GameSaveData` (giữ nguyên overload cũ, không xóa API);
  thêm `SaveRepository` (mặc định `FileSaveSlotRepository`, tạo trong `Awake` — **không** phải field
  initializer, vì `Application.persistentDataPath` không được gọi từ field initializer/constructor của
  MonoBehaviour, xem mục Bug tìm thấy bên dưới) và `SetSaveRepositoryForTests` cho test injection.
- `Assets/Scripts/GameManagers/PlayerSpawnReadinessSource.cs` — `IGameplayReadinessSource` mới, cắm vào
  `GameplayReadinessGate` có sẵn từ Phase 1 (không sửa Gate). Đọc `GameSessionManager.Current.SaveData`,
  gọi `PlayerStat.RestoreProgression`, resolve vị trí qua `SpawnRegistry` (dùng saved position nếu có,
  fallback spawn nếu không, giữ nguyên vị trí + log warning nếu spawn id không tồn tại — recovery an
  toàn, không chặn Playing). Với session `NewGame`, ghi initial save đúng một lần sau restore (D-011).
- `Assets/Scripts/GameManagers/MainMenuController.cs` — contract non-visual cho MainMenu UI:
  `RequestNewGame(slotId)`, `RequestContinue(slotId)`, `CanContinue(slotId)`,
  `SlotRequiresOverwriteConfirm(slotId)`, `RefreshSlots()`, `DeleteSlot(slotId)`, events
  `OnSaveSlotListChanged`, `OnOperationFailed`. Không có Canvas/layout nào được tạo — xem mục Codex UI
  Handoff.

## Bug tìm thấy và sửa trong lúc verify

`GameSessionManager.SaveRepository` ban đầu được khởi tạo bằng field initializer
(`= new FileSaveSlotRepository()`), gọi `Application.persistentDataPath` trong constructor của
`FileSaveSlotRepository`. Unity cấm gọi API này từ constructor/field initializer của MonoBehaviour —
khi verify thủ công bằng Play Mode thật (không phải qua Unity Test Runner, vốn có bootstrap context
khác), lỗi này lộ ra rõ ràng: `UnityException: get_persistentDataPath is not allowed to be called from
a MonoBehaviour constructor...` kèm `NullReferenceException` theo sau. Đã sửa: chuyển khởi tạo vào
`Awake()`. Thêm test hồi quy `GameSessionManagerPlayModeTests.DefaultSaveRepository_IsConstructedWithoutError`.
Bài học: Unity Test Runner không phải lúc nào cũng lộ đúng timing bug như một phiên Play Mode tương tác
thật — cả hai đều cần thiết cho verification.

## Tests

- EditMode (17 tổng, 3 mới): `NewGameFactoryTests.cs` — default snapshot đúng field, hai lần tạo có
  `saveId` khác nhau, và round-trip `GameSaveData` (bao gồm sentinel `NaN` position) qua
  `FileSaveSlotRepository` thật.
- PlayMode (8 tổng, 4 mới):
  - `PlayerSpawnReadinessSourcePlayModeTests` — New Game restore đúng default + spawn tutorial + ghi
    initial save; Continue restore đúng progression/vị trí đã lưu và không ghi đè; không có session
    active thì không đụng Player, vẫn báo ready.
  - `GameSessionManagerPlayModeTests` — regression cho bug field-initializer ở trên.

## Verification record — 2026-08-22

- Script validation: 0 diagnostics.
- Editor compile: 0 Error/0 Warning.
- EditMode tests: **17/17 PASS**.
- PlayMode tests: **8/8 PASS**.
- Content Validation: 0 error, 60 warning (baseline không đổi), 63 asset checked.
- DemoScene/MainMenu scene validator: 0 issue sau khi wiring `SpawnRegistry`,
  `PlayerSpawnReadinessSource`, `MainMenuController` bằng Unity MCP.
- Play Mode thật (không phải test), từ MainMenu, dùng `InMemorySaveSlotRepository` để không đụng save
  thật:
  - `RequestNewGame(1)`: `MainMenu → Loading → Playing`; Player tại `(0,0,0)` (spawn.tutorial.start);
    Level 1, Health 100/100; initial save được ghi với `areaId = area.tutorial`.
  - Sửa trực tiếp save (giả lập một save cũ đã có vị trí/level khác: level 5, exp 7, vị trí
    `(12, -4)`), gọi `TryReturnToMainMenu()` rồi `RequestContinue(1)`: Player restore đúng
    `(12.00, -4.00)`, Level 5, Exp 7 — đúng dữ liệu đã lưu, không dùng fallback spawn.
  - `TryReturnToMainMenu()` sau đó: về `MainMenu`, `PlayerCount = 0`, `HasActiveSession = false`.
  - Console sạch trong toàn bộ kịch bản (sau khi sửa bug field initializer).

## Chưa hoàn thành trong Phase 3 / để lại cho phase sau

- **UI thật cho New Game/Continue slot selector** — `MainMenuController` đã sẵn contract, Codex cần dựng
  Canvas/button và gọi đúng method (xem Codex UI Handoff).
- Player vẫn là static singleton `DontDestroyOnLoad` cố định trong DemoScene — chưa refactor thành
  scene-spawned actor theo đích kiến trúc (quyết định phạm vi ở trên).
- Overwrite confirm popup — `SlotRequiresOverwriteConfirm(slotId)` đã có, nhưng popup UI thuộc Phase
  3 UI/Codex, chưa dựng.
- Restore inventory/equipment/tutorial/quest/world — Phase 4/5/6/8.
- Camera bind riêng biệt — camera hiện tại follow Player sẵn có từ trước, chưa cần thay đổi ở Phase 3.
- Save migration V1 (không có `player`) → V2 — không cần vì chưa có save V1 nào tồn tại (chưa release).
- Manual gamepad test cho New Game/Continue — vẫn `BLOCKED – requires physical user input` như Phase 1.

## Codex UI Handoff

**Component cần gắn:** `MainMenuController` đã có sẵn trên `MainMenu.unity/_SceneContext` (đã gắn qua
Unity MCP, field `_gameplaySceneName = "DemoScene"`). Không cần tạo lại.

**UI cần gọi:**
- Nút "New Game" trên một slot → sau khi user xác nhận (nếu `SlotRequiresOverwriteConfirm(slotId)` là
  `true`, hiện popup overwrite trước) → `MainMenuController.RequestNewGame(slotId)`.
- Nút "Continue" trên một slot → chỉ enable khi `CanContinue(slotId)` là `true` →
  `MainMenuController.RequestContinue(slotId)`.
- Nút xóa slot (nếu có) → `MainMenuController.DeleteSlot(slotId)`.
- Khi mở màn hình slot selector → gọi `MainMenuController.RefreshSlots()` để lấy `SaveSlotInfo[]` hiện
  tại (mỗi phần tử có `SlotId`, `Status` (`Empty|Valid|Corrupted|IncompatibleVersion`), `Metadata`
  — null trừ khi `Status == Valid`).

**Event cần subscribe:**
- `OnSaveSlotListChanged(SaveSlotInfo[])` — rebuild danh sách slot UI.
- `OnOperationFailed(string message)` — hiện lỗi thân thiện, không crash UI.

**State cần hiển thị theo `SaveSlotInfo.Status`:**
- `Empty` → nút New Game "Create", nút Continue disabled.
- `Valid` → hiển thị `Metadata.characterName`/`Level`/`areaId`/`totalPlayTimeSeconds`/`lastSavedUtcTicks`
  (lưu ý: `characterName`, `characterLevel`, `areaId`, `tutorialCompleted` trong `SaveSlotMetadata` vẫn
  đang ở giá trị mặc định — Phase 3 chưa populate các field này vào metadata, chỉ có trong
  `GameSaveData.player` khi đọc full save; nếu UI cần hiển thị level/area ngay trên danh sách slot mà
  không đọc full save, đây là việc cần làm thêm trước khi UI này hoàn thiện — báo lại nếu cần ưu tiên).
- `Corrupted`/`IncompatibleVersion` → hiển thị rõ trạng thái, cho phép `DeleteSlot` để dọn.

**Button enable/disable:**
- New Game: luôn enable (Empty → tạo thẳng; có data → confirm trước khi gọi).
- Continue: chỉ enable khi `CanContinue(slotId)`.

**Loading/error:** `GameStateManager.Instance.CurrentState == GameState.Loading` trong lúc scene
đang load + readiness gate chạy — UI nên khóa thao tác chọn slot khác trong lúc này (không double-submit
theo Quality Strategy). Chưa có progress-stage riêng (reading/scene/restoring/finalizing) — hiện tại chỉ
có một trạng thái Loading chung.

**Gamepad navigation:** slot list nên dùng project `UI` action map sẵn có (Navigate/Submit/Cancel), giữ
đúng convention MainMenu hiện tại.

**Thứ tự setup gợi ý bằng Unity MCP:**
1. Dựng Canvas/slot list UI trong `MainMenu.unity/_UI` theo `UIAndInteractionFlows.md` (New Game/Continue
   layout, 3 slot).
2. Mỗi slot button gọi đúng method ở trên qua UnityEvent hoặc script trung gian mỏng (không chứa logic).
3. Subscribe `OnSaveSlotListChanged`/`OnOperationFailed` từ script UI, không polling.
4. Verify bằng cách bấm New Game → xác nhận vào DemoScene, quay lại → Continue slot đó → vị trí/level
   khớp.
