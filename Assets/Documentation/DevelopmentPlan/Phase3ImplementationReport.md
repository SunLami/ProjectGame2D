# Phase 3 Implementation Report

Ngày bắt đầu: 2026-08-22
Trạng thái: **PARTIAL — code/backend/UI hoàn tất và đã verify; chỉ còn 1 blocker ngoài phạm vi code**
(xem "Chốt Phase 3" bên dưới)

## Chốt Phase 3 — 2026-08-22

Re-run toàn bộ validation sau khi Codex hoàn tất UI + automated gamepad simulation
(`Handoffs/CodexToClaude.md`, status `VERIFIED`):

- Compile: `PASS` — 0 Error/0 Warning.
- EditMode tests: `PASS` — 20/20.
- PlayMode tests: `PASS` — 11/11.
- Content Validation: `PASS` — 0 error, 60 warning (baseline legacy-ID không đổi), 63 asset.
- MainMenu scene validator: `PASS` — 0 issue.
- DemoScene scene validator: `PASS` — 0 issue.
- Windows x64 Development Build: `PASS` — succeeded 49,19s, 581,98 MB, 0 Error/0 Warning
  (`Builds/Phase3Closure/ProjectGame2D.exe`, build này stale từ Phase 1 trước khi chốt nên đã build lại).
- Runtime launch độc lập Editor: `PASS` — process sống/`Responding=True`, `Player.log` không có
  NullReference/MissingReference/UnassignedReference/exception/crash (chỉ có init log chuẩn + 1
  `Curl error 35` non-gameplay đã ghi nhận từ Phase 0/1).
- Automated virtual-gamepad UI Navigate/Submit/Cancel (Codex): `PASS` — có evidence trong
  `InputSystemInventory.md` mục "MainMenu automated gamepad simulation".
- **Manual physical gamepad:** `BLOCKED_MANUAL_TEST` — chưa có người thao tác controller thật. Đây là
  blocker duy nhất còn lại, và nó là external/manual-only (không phải code/backend blocker) — không
  suy đoán PASS, không tự động chuyển trạng thái này.

**Kết luận:** không còn blocker code/backend nào chặn Phase 4. Phase 3 được coi là đóng ở mức
implementation, chỉ treo lại `BLOCKED_MANUAL_TEST` cho gamepad vật lý — mục này sẽ được xác nhận khi có
người dùng thật thao tác, không gate việc bắt đầu Phase 4.

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

## Codex UI integration — 2026-08-22

- Đã dựng MainMenu scene-authored UI cho New Game/Continue với đúng ba slot, overwrite/delete confirm,
  operation error và Loading input lock.
- Landing bổ sung Settings và Quit; Main Menu Settings dùng chung `SettingsService` nhưng có navigation
  riêng, không dùng gameplay menu state.
- UI dùng project `UI` action map, có default focus và Cancel/back policy; toàn bộ TMP text dùng
  `DigitalDisco SDF v3`.
- MainMenu scene validator: 0 issue; script validation: 0 diagnostic; console acceptance flow sạch.
- New Game → DemoScene → Return MainMenu → Continue chạy thành công với initial snapshot; Slot 1 test
  save đã được xóa sau verify.
- Chi tiết và backend gaps xem [Codex → Claude Handoff](Handoffs/CodexToClaude.md).

## Backend gaps từ Codex — sửa 2026-08-22

Codex báo 3 gap trong `Handoffs/CodexToClaude.md`. Xem [Claude → Codex Handoff](Handoffs/ClaudeToCodex.md)
cho task tiếp theo thuộc Codex.

1. **Root cause của `lastSavedUtcTicks = 0`:** `FileSaveSlotRepository.WriteSave` đã ghi đúng
   `metadata.json` với timestamp thật, nhưng `GetSlotInfo` **chưa bao giờ đọc lại `metadata.json`** —
   nó luôn tự dựng lại `SaveSlotMetadata` từ `save.json` (`BuildMetadata`), vốn không có timestamp.
   `metadata.json` ghi ra chỉ để... không ai dùng. Đã sửa: `GetSlotInfo` giờ ưu tiên đọc
   `metadata.json` (khớp `saveId` + `contentChecksum` với save đang đọc để tránh dùng metadata cũ/stale),
   chỉ dựng lại từ `save.json` khi `metadata.json` mất/hỏng (lúc đó `lastSavedUtcTicks` đúng là không
   thể phục hồi, giữ `0` = "unknown"). `BuildMetadata` cũng được bổ sung `characterLevel` (từ
   `data.player.level`) và `areaId` (từ `data.player.location.areaId`). `characterName` **không** được
   set (không có domain đặt tên nhân vật, D-013 vẫn Open) và `tutorialCompleted` **không** được set
   (chưa có tutorial domain, Phase 5) — đúng yêu cầu "không tạo dữ liệu giả".
2. **Player snapshot capture:** thêm `Assets/Scripts/Save/PlayerSaveCapture.cs` — pure C#,
   `PlayerSaveCapture.Capture(PlayerStat, Transform, areaId, fallbackSpawnId)` đọc trực tiếp
   level/XP/health từ `PlayerStat` và vị trí từ `Transform`, trả về `PlayerSaveData`. Đây là đường
   DUY NHẤT được chấp nhận để biến live state thành save data — UI không được tự tạo/sửa `GameSaveData`.
   Hàm này **chưa được gọi ở đâu** trong Phase 3 (xem điểm 3).
3. **Không tự động save khi Return Main Menu:** xác nhận rõ — `PlayerSaveCapture` chỉ là capture API,
   không tự wire vào `SceneFlowService.TryReturnToMainMenu` hay bất kỳ đâu. Việc quyết định NÊN gọi nó
   khi nào (Save Game từ Pause Menu, hoặc dirty-session confirm khi Return Main Menu) thuộc D-017/Phase 9,
   chưa được chấp thuận ở Phase 3. Vì vậy "Continue khớp đúng vị trí vừa rời" (gap #3 Codex nêu) vẫn chỉ
   đúng với vị trí đã có sẵn trong snapshot — capture vị trí gameplay hiện tại là việc của Phase 9.
4. **Double-submit thật (phát hiện thêm, không nằm trong 3 gap Codex báo):** `MainMenuController`
   không có guard nào chống hai lần gọi `RequestNewGame`/`RequestContinue` liên tiếp trước khi scene
   load xong — lần gọi thứ hai âm thầm ghi đè `GameSessionManager.Current` bằng một `GameSaveData` mới
   trong khi lần đầu vẫn đang transition, khiến scene load xong sẽ restore nhầm session. Đã thêm guard
   `CanStartRequest()` (`!SceneFlowService.IsTransitioning && !GameSessionManager.HasActiveSession`) ở
   đầu cả hai method.

### Tests mới (2026-08-22)

- EditMode (20 tổng, 3 mới trong `FileSaveSlotRepositoryTests.cs`):
  `Metadata_MatchesPlayerSnapshot_AndDoesNotFabricateCharacterName`,
  `Metadata_LastSavedTimestamp_IsPersistedAndUpdatesOnEachWrite`,
  `Metadata_SlotsAreIsolated_DifferentPlayerDataPerSlot`.
- PlayMode (11 tổng, 3 mới):
  `PlayerSaveCapturePlayModeTests.Capture_ReadsLiveStatAndTransform`,
  `PlayerSpawnReadinessSourcePlayModeTests.CapturedSnapshot_RoundTripsThroughWriteAndContinueRestore`
  (capture → write → Continue restore bằng `PlayerSaveCapture` thật, không phải `PlayerSaveData` tự
  dựng tay), `MainMenuControllerPlayModeTests.DoubleSubmit_SecondRequestIsRejectedWithoutOverwritingSession`.
- Toàn bộ suite: **EditMode 20/20 PASS, PlayMode 11/11 PASS**. Content Validation không đổi (0 error,
  60 warning, 63 asset).
- Verify thủ công Play Mode thật (temp save path, không đụng save thật): `RequestNewGame(1)` →
  `GetSlotInfo(1).Metadata` có `lastSavedUtcTicks` thật (khác 0), `characterLevel = 1`,
  `areaId = area.tutorial`, `characterName = ""`, `tutorialCompleted = false`. Double-submit
  `RequestNewGame(2)` gọi hai lần liên tiếp: lần hai bị từ chối đúng 1 lần qua `OnOperationFailed`,
  chỉ một session/scene load tiến hành, kết thúc ở `Playing` sạch console.

### Sự cố tooling gặp phải khi verify (không phải lỗi code)

Trong lúc thêm các fix trên, Unity Editor rơi vào trạng thái compile pipeline bị kẹt: file mới
(`PlayerSaveCapture.cs`) không được đưa vào compile set của `ProjectGame2D.Runtime` dù `.meta` hợp lệ
và asset đã import — biểu hiện là `CS0103` liên tục ở hai file test tham chiếu nó, ngay cả sau nhiều lần
force-recompile, xóa cache `Library/ScriptAssemblies`, và `AssetDatabase.Refresh(ForceUpdate)`. Chỉ
`Assets > Reimport All` mới thực sự khôi phục pipeline. Ghi lại để nếu gặp lại triệu chứng tương tự
(file mới không được compiler nhận diện dù mọi thứ khác đúng), thử `Reimport All` trước khi nghi ngờ code.

## Chưa hoàn thành trong Phase 3 / để lại cho phase sau

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
- `Valid` → `Metadata.characterLevel`, `Metadata.areaId`, `Metadata.totalPlayTimeSeconds`,
  `Metadata.lastSavedUtcTicks` giờ đều là dữ liệu thật (cập nhật 2026-08-22, xem mục "Backend gaps từ
  Codex — sửa 2026-08-22" bên dưới) — có thể hiển thị trực tiếp trên danh sách slot mà không cần đọc
  full save. `Metadata.characterName` vẫn luôn rỗng (chưa có domain đặt tên nhân vật, D-013 Open — đừng
  hiển thị placeholder giả, ẩn field này hoặc dùng nhãn kiểu "Slot N") và `Metadata.tutorialCompleted`
  vẫn luôn `false` (chưa có tutorial domain, Phase 5) — không coi đây là dữ liệu thật cho tới khi báo lại.
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
