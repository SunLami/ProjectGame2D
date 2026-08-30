# Phase 9 Implementation Report

Ngày: 2026-08-23
Trạng thái: **Save/Load/Return/Quit backend hoàn tất, tự vận hành đúng (48/48 EditMode, 118/118
PlayMode PASS, Content Validation 0 error, DemoScene validator 0 issue, verify sống end-to-end qua
`execute_code` bao gồm reload scene thật). Chưa có UI Save/Load/Return/Quit trong Pause Menu -- xem
Codex Handoff. `READY_FOR_CODEX_UI`.**

## Nguồn tài liệu đã đọc trước khi triển khai

`README.md`, `Roadmap.md` (Phase 9), `DecisionRegister.md`, `RuntimeArchitecture.md`,
`SaveAndWorldPersistence.md`, `UIAndInteractionFlows.md`, `QualityStrategy.md`,
`ServiceOwnershipLifecycle.md`, `Handoffs/CodexToClaude.md` (Phase 8 Scene Integration, `VERIFIED`),
cùng `PlayerSaveCapture.cs`/`ISaveSlotRepository.cs`/`GameSessionManager.cs`/`SceneFlowService.cs`/
`MainMenuController.cs` (pattern gốc cho controller non-visual) và `PauseMenuUI.cs`/
`MainMenuSaveSlotsUI.cs` hiện có để không tạo API trùng.

## Phạm vi

Save Game vào active slot, Load Game (active slot hoặc slot khác) từ gameplay, Saving/Loading state
coordination, dirty-session tracking, Return Main Menu và Quit Desktop với 3-way confirm khi dirty.
Claude phụ trách toàn bộ backend/controller/tests; **không** dựng Pause Save/Load/Return/Quit UI --
đó là Codex sau handoff.

## Kiến trúc

### `GameplaySessionController` (`Assets/Scripts/GameManagers/`)

Non-visual scene controller trên `_SceneContext`, mirror chính xác `MainMenuController` (đã có từ
Phase 3) để Codex không phải học pattern mới:

```csharp
public int ActiveSlotId { get; }
public bool IsDirty { get; }
public bool IsBusy { get; }                          // true khi GameState là Saving hoặc Loading

public SaveSlotInfo[] RefreshSlots();
public bool CanLoad(int slotId);

public bool RequestSave();
public bool RequestLoad(int slotId);

public void RequestReturnToMainMenu();               // fire OnConfirmationRequired nếu dirty
public void ConfirmSaveAndReturn();
public void ConfirmReturnWithoutSaving();
public void CancelReturnToMainMenu();

public void RequestQuit();                           // fire OnConfirmationRequired nếu dirty
public void ConfirmSaveAndQuit();
public void ConfirmQuitWithoutSaving();
public void CancelQuit();

public event Action<SaveSlotInfo[]> OnSaveSlotListChanged;
public event Action OnSaveSucceeded;
public event Action<GameplaySessionOperationResult, string> OnOperationFailed;
public event Action<GameplaySessionConfirmationKind> OnConfirmationRequired;
```

Save capture **luôn** đi qua `PlayerSaveCapture.Capture(...)` (API đã tồn tại từ Phase 3, chưa từng
được wire vào bất kỳ trigger nào) + từng domain `ToSaveData()` (`InventoryManager`,
`EquipmentManager`, `TutorialManager`, `QuestManager`, `WorldObjectRegistry` -- tất cả API có sẵn từ
Phase 4-8) + `GameSessionManager.GetTotalPlayTimeSeconds()` (mới). Controller không tự ghi field
`GameSaveData` nào khác, không có "hệ thống save thứ hai".

`Saving`/`Loading` dùng đúng `GameState` đã tồn tại từ Phase 1 (`GameStatePolicy` đã sẵn
`AllowsGameplayInput = false` cho cả hai). Save Game dùng `GameStateManager.PushState(Saving)` rồi
`ReturnToPreviousState()` -- quay lại đúng state trước (thường là `Paused`) dù thành công hay thất
bại, không có nhánh nào bỏ sót return. Load Game dùng `ReplaceState(Loading)` sẵn có trong
`SceneFlowService.BeginSceneLoad`, không tự quản `Time.timeScale`.

### `GameSessionManager` mở rộng (dirty/restore/play-time)

Đúng ownership đã ghi trong `RuntimeArchitecture.md`: "`GameSessionManager` | Active slot, session
kind, **dirty/play time**":

```csharp
public bool IsDirty { get; }
public bool IsRestoring { get; }
public event Action<bool> DirtyStateChanged;

public void MarkDirty();       // no-op nếu đã dirty HOẶC đang restoring
public void ClearDirty();      // no-op nếu đã clean
public void BeginRestore();
public void EndRestore();
public long GetTotalPlayTimeSeconds();  // base từ save đã load + elapsed real-time của session hiện tại
```

`SetSession` (New Game/Continue/Development) luôn reset `IsDirty=false`, `IsRestoring=false` và mốc
play-time -- một session mới không bao giờ bắt đầu dirty.

### `SessionDirtyTracker` (scene service, mới)

Trên `_SceneContext`, subscribe domain event (không polling, không serialize để so sánh):
`InventoryManager.OnInventoryChanged`, `EquipmentManager.OnEquipmentChanged`,
`PlayerStat.OnLevelUp`/`OnExperienceChanged`, `TutorialManager.OnStepChanged`/`OnTutorialCompleted`,
`QuestManager.QuestAccepted`/`QuestProgressChanged`/`QuestCompleted`/`MainQuestUnlocked`,
`WorldDomainEvents.WorldObjectChanged` (mới, xem dưới). Mỗi handler chỉ gọi
`GameSessionManager.Instance.MarkDirty()` -- **không tự kiểm tra `IsRestoring`**, vì
`GameSessionManager.MarkDirty()` đã tự guard điều đó ở một điểm duy nhất (xem D-024). Player
position/di chuyển đơn thuần cố ý không nằm trong danh sách này.

`PlayerSpawnReadinessSource.RestoreAll` (toàn bộ restore, kể cả seed inventory cho New Game) giờ bọc
trong `GameSessionManager.BeginRestore()`/`EndRestore()` (try/finally) -- mọi
`RestoreProgression`/`RestoreEquipped`/`LoadFromSaveData`/`RestoreState` vẫn phát đúng các event cũ
(không đổi hành vi các domain khác) nhưng dirty-tracking biết bỏ qua đúng lúc.

### `WorldDomainEvents` (mới, `Assets/Scripts/World/`)

```csharp
public static event Action WorldObjectChanged;
public static void RaiseWorldObjectChanged();
```

`ChestInteractable.TryOpen`, `UniquePickupInteractable.TryCollect`, `ResourceNodeInteractable.TryHarvest`
(chỉ nhánh thành công) và `BossDefeatTracker` (khi `EnemyUniversal.Died` fire thật) đều raise event
này -- không raise từ `WorldObjectRegistry.RestoreState` hay `BossDefeatTracker.RestoreState`, nên
không cần guard `IsRestoring` ở phía World (đã đúng cấu trúc từ Phase 8, giờ tận dụng thêm cho dirty).

### `IApplicationQuitter` (mới)

```csharp
public interface IApplicationQuitter { void Quit(); }
public sealed class UnityApplicationQuitter : IApplicationQuitter { public void Quit() => Application.Quit(); }
```

`GameplaySessionController` chỉ gọi `Application.Quit()` gián tiếp qua interface này (lazy-created
`UnityApplicationQuitter` mặc định, thay được bằng fake trong test qua `ConfigureForTests`) --
automated test không bao giờ gọi `Application.Quit()` thật, đúng yêu cầu.

### Sửa `SceneFlowService` cho Load Game an toàn (không rò state giữa slot)

**Bug tiềm ẩn đã sửa trước khi nó xảy ra**: `SceneFlowService.BeginSceneLoad` trước đây chỉ gọi
`GameplaySceneLifetime.ReleaseForSceneExit()` khi `enterMainMenu == true`. Load Game (Phase 9, tính
năng mới) gọi lại `TryLoadGameplay("DemoScene")` **trong khi** một session gameplay khác đã đang
chạy -- các singleton `DontDestroyOnLoad` (`InventoryManager`, `EquipmentManager`, `QuestManager`,
...) của session cũ vẫn sống ngoài scene sắp unload, nên scene mới tự hủy các manager mới của chính
nó trong `Awake()` (vì `Instance != null`), để lại manager **cũ** (dữ liệu slot A) làm singleton
sống sót -- chính xác kiểu lỗi "Load slot A rồi B rò dữ liệu" mà acceptance criteria cấm. Đã sửa:
`ReleaseForSceneExit()` giờ luôn chạy trước mọi lần load scene (no-op an toàn khi chưa có gameplay
scene nào từng load, ví dụ MainMenu → DemoScene lần đầu). Verify sống bằng scene reload thật (xem
Runtime verification).

## Vì sao không có save schema mới

Không field/DTO mới nào được thêm vào `GameSaveData` -- Phase 9 chỉ là **trigger** cho pipeline
capture/write đã tồn tại đầy đủ từ Phase 3-8. `CurrentSaveVersion` giữ nguyên 6, không migration
mới, không golden fixture mới cần thiết.

## Quyết định đã chốt (xem `DecisionRegister.md`)

- **D-017** (Return Main Menu dirty state): Accepted, đúng proposed default -- Save and Return /
  Return Without Saving / Cancel.
- **D-024** (mới -- dirty-session event contract): Accepted, danh sách event tối thiểu ở trên.
- **D-005** (Save khi combat) và **D-012** (Autosave) **vẫn `Proposed`, KHÔNG triển khai** -- project
  chưa có khái niệm combat/danger state nào để D-005 bám vào; D-012 nằm ngoài phạm vi Phase 9 theo
  đúng roadmap ("Chưa có ở foundation; manual save trước"). Không tự chế combat-lock.

## Content/scene wiring (DemoScene, Unity MCP)

- `_SceneContext` thêm component `GameplaySessionController` (trỏ đúng `PlayerStat`/`Player
  Transform`/`WorldObjectRegistry` đã dùng chung với `PlayerSpawnReadinessSource`) và
  `SessionDirtyTracker` (không cần field -- tự bind `.Instance` khi `OnEnable`).
- Không tạo GameObject/prefab/UI mới nào khác.

## Tests

- EditMode: 48/48 PASS (không đổi -- Phase 9 không có DTO/ScriptableObject mới cần EditMode
  coverage).
- PlayMode: 118/118 PASS (33 mới):
  - `GameSessionManagerPlayModeTests` (+4): `MarkDirty`/`ClearDirty` chỉ fire event đúng lúc chuyển
    trạng thái thật; `IsRestoring` chặn `MarkDirty` đúng; session mới luôn reset dirty/restoring;
    `GetTotalPlayTimeSeconds` không bao giờ nhỏ hơn base đã load.
  - `SessionDirtyTrackerPlayModeTests` (10): Inventory/Gold/Tutorial-step/Quest-accept/World-object
    làm dirty đúng qua real domain call (không gọi thẳng `MarkDirty`); `RestoreEquipped`/
    `TutorialManager.RestoreState`/`QuestManager.RestoreState` (bọc `BeginRestore`/`EndRestore`)
    không bao giờ dirty; unsubscribe đúng khi tracker bị destroy (không leak callback vào
    `InventoryManager` đã mất listener).
  - `GameplaySessionControllerPlayModeTests` (~22, gồm 1 `[UnityTest]` reload scene thật): Save
    active slot thành công + refresh metadata + clear dirty; Save không active session bị từ chối;
    Save failure quay lại state trước, `Time.timeScale` đúng theo Paused (không kẹt); double-click
    Save/Load bị chặn qua `IsBusy`; slot rỗng/không hợp lệ bị từ chối đúng lý do; Return/Quit khi
    clean đi thẳng không hỏi; khi dirty fire `OnConfirmationRequired` và **không đổi GameState**;
    Save-and-Return/Save-and-Quit chỉ hoàn tất sau save thật thành công, thất bại giữ nguyên
    Paused/không quit và dirty vẫn `true`; Return-Without-Saving/Quit-Without-Saving không ghi save;
    Cancel không đổi gì. `RequestLoad_DifferentSlot_RealSceneReload_DoesNotLeakInventoryFromPreviousSlot`
    reload `DemoScene` **thật** hai lần với hai `GameSaveData` khác gold, xác nhận
    `InventoryManager.Gold` đúng slot B sau lần load thứ hai (chứng minh fix `SceneFlowService` bằng
    runtime thật, không chỉ bằng thiết kế).

## Manual/runtime verification (Play Mode thật, DemoScene, `execute_code`)

Toàn bộ chạy trên slot 2/3 thật (kiểm tra `GetAllSlotInfo()` trước khi dùng, chỉ dùng slot đang
`Empty`, xóa sạch sau khi xong -- không đụng slot 1 hiện có dữ liệu `IncompatibleVersion` của người
dùng):

- `AddGold(50)` → `IsDirty == true` → `RequestSave()` → `IsDirty == false`, `GameState == Paused`,
  đọc lại slot vừa ghi thấy đúng `gold = 50`. **PASS**.
- Ghi sẵn slot khác với `gold = 999`, gọi `RequestLoad(slotKhac)` → `GameState` chuyển `Loading` rồi
  tự về `Playing` sau khi `DemoScene` load lại thật -- `InventoryManager.Gold == 999` (không phải 50
  từ slot trước), và đúng **1** instance mỗi loại `MapManager`/`InventoryManager`/`QuestManager` tồn
  tại sau reload (xác nhận fix `SceneFlowService` hoạt động đúng với scene thật, không chỉ fixture
  test). **PASS**.
- `AddGold(1)` (dirty) → `RequestReturnToMainMenu()` → `OnConfirmationRequired(ReturnToMainMenu)`
  fire, `GameState` vẫn `Paused` (popup không tự đổi state). `ConfirmSaveAndReturn()` → `Loading` rồi
  tự về `MainMenu`. **PASS**.
- Console sạch (0 error, 0 warning) xuyên suốt toàn bộ kịch bản trên.
- Content Validation: 0 error, 60 warning (baseline không đổi), 83 asset checked (không đổi -- không
  có content mới).
- DemoScene validator: 0 issue.

Quit Desktop không thể verify sống trong Editor (sẽ đóng Unity Editor thật) -- verify bằng
`FakeApplicationQuitter` trong automated test (xem Tests ở trên) đúng theo yêu cầu "test được bằng
abstraction thay vì phụ thuộc trực tiếp `Application.Quit()`".

## Known limitations / để lại cho phase sau

- D-005 (save khi combat) và D-012 (autosave) chưa triển khai -- xem lý do ở mục Quyết định.
- `MainMenuSaveSlotsUI.cs` (Codex, MainMenu Quit) vẫn gọi `Application.Quit()` trực tiếp, chưa dùng
  `IApplicationQuitter` -- ngoài phạm vi Phase 9 (Roadmap ghi rõ "trong gameplay scene"), có thể hợp
  nhất sau nếu muốn một điểm test duy nhất cho mọi đường Quit.
- `RequestLoad`/`RequestSave` không tự giới hạn phải gọi từ `GameState.Paused` -- push/pop qua
  `GameStateManager` history vốn đã tổng quát đúng cho bất kỳ state gọi nào, và `IsBusy` đã chặn
  double-submit; UI (Codex) chỉ cần đặt nút Save/Load trong Pause Menu như
  `UIAndInteractionFlows.md` mô tả, không cần thêm ràng buộc phía backend.
- Không có UI Save/Load/Return/Quit nào được dựng -- Codex, theo Boundary.

## Codex Handoff

Xem [ClaudeToCodex.md](Handoffs/ClaudeToCodex.md), đánh dấu `READY_FOR_CODEX_UI`.
