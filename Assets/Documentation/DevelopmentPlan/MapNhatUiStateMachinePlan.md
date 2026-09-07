# MapNhat UI State Machine Integration Plan

> **Trạng thái tài liệu:** Proposed — kiến trúc đã chốt (D-019 Accepted 2026-09-07), chưa triển khai
> code/scene. Phạm vi đã xác nhận: mang **toàn bộ** `_UI` sang `MapNhat` trong một đợt, `MapNhat` chính
> thức là world scene production đầu tiên. Xem [Quyết định kiến trúc](#quyết-định-kiến-trúc-đã-chốt-d-019).

## Mục tiêu

Đưa toàn bộ gameplay UI (Pause, Inventory/Equipment, Settings, Quest Log, Shop/Crafting, Tutorial
overlay, Unified HUD, Dialogue) đang chỉ tồn tại trong `DemoScene` sang `MapNhat`, sao cho **cả hai
scene dùng chung đúng một UI state machine** — không tạo bản sao logic, không tạo `GameStateManager`
thứ hai — và bất kỳ world scene nào thêm sau này cũng lặp lại đúng quy trình này thay vì tự chế lại.

## Phát hiện quan trọng: state machine đã tồn tại

`GameStateManager` (`Assets/Scripts/GameManagers/GameStateManager.cs`) **đã** là UI/Game state machine
dùng chung: auto-bootstrap bằng `RuntimeInitializeOnLoadMethod` trước khi bất kỳ scene nào load,
`DontDestroyOnLoad`, và toàn bộ UI controller hiện có (`PauseMenuUI`, `InventoryWindowUI`, `SettingsUI`,
`QuestLogUI`, `ShopCraftingUI`, `TutorialOverlayUI`, `UnifiedGameplayHudController`, `DialogueUI`...)
đã subscribe `StateChanged`/gọi `PushState`/`OpenMenu` đúng theo
[GameStateManager.md](../GameStateManager.md). `SceneFlowService` cũng đã là singleton toàn app điều
phối load/unload scene.

**Vì vậy đây không phải bài toán "xây UI state machine mới"** — đó là bài toán **portability**: kéo
đúng feature package (UI View + scene service phụ trợ) từ `DemoScene` sang `MapNhat` theo quy trình đã
có sẵn ở [DemoSceneWorkflow.md](DemoSceneWorkflow.md), để `MapNhat` tham gia vào state machine đang
chạy toàn app thay vì tạo luồng riêng.

## Hiện trạng hai scene (đã kiểm tra bằng Unity MCP, 2026-09-07)

| Thành phần | DemoScene | MapNhat |
|---|---|---|
| `GameStateManager`, `SceneFlowService`, `GameSessionManager`, `SettingsService`, `GameCursorManager` | Application service, tự bootstrap — **không cần copy** | Giống hệt, đã tự có sẵn (app-wide) |
| `InventoryManager`, `EquipmentManager`, `QuestManager`, `ShopManager`, `CraftingManager`, `TutorialManager`, `SoundFXManager`, `MusicManager`, `PlayerStat` | `DontDestroyOnLoad` singleton — sống xuyên scene sau khi được tạo lần đầu | Không tồn tại object khởi tạo ban đầu nếu vào thẳng `MapNhat` mà chưa qua `DemoScene`/`MainMenu` |
| `_SceneContext` | `GameplaySceneLifetime`, `SceneDependencyReadinessSource`, `GameplayReadinessGate`, `SpawnRegistry`, `PlayerSpawnReadinessSource`, `WorldObjectRegistry`, `GameplaySessionController`, `SessionDirtyTracker` | Chỉ có `GameBootstrap` (mode `DevelopmentGameplay`) + `GameInputCoordinator` — theo đúng ghi chú "portability smoke integration cho Player" trong [DemoSceneWorkflow.md § MapNhat integration status](DemoSceneWorkflow.md#mapnhat-integration-status) |
| `_UI` (`UICanvas` + toàn bộ panel) | Có đầy đủ, là scene-embedded hierarchy (chưa đóng gói prefab) | **Không có** — 0 Canvas, 0 EventSystem |
| `MapManager` | Scene-scoped theo thiết kế (không `DontDestroyOnLoad`) | Chưa có instance |

Kết luận: các *Manager application-scope* không cần dựng lại — chúng tự tồn tại xuyên scene một khi đã
được tạo (ví dụ đi từ `MainMenu → DemoScene`). Việc thật sự cần làm là (a) đóng gói `_UI` thành **prefab
dùng chung**, và (b) bổ sung đúng bộ `_SceneContext` scene-service mà `MapNhat` đang thiếu để
`GameplayReadinessGate`/`GameplaySessionController` hoạt động giống `DemoScene`.

## Quyết định kiến trúc đã chốt (D-019)

[DecisionRegister.md](DecisionRegister.md) **D-019 — Accepted — 2026-09-07**: `MapNhat` là world scene
production chính thức đầu tiên (không chỉ là bài portability/smoke-test). `DemoScene` giữ nguyên vai trò
integration playground (D-001 không đổi) — không đổi tên, không rút feature ra khỏi `DemoScene`, chỉ
nhân bản/kéo sang `MapNhat` theo đúng nguyên tắc "kéo prefab giữ connection" của
[DemoSceneWorkflow.md](DemoSceneWorkflow.md).

Phạm vi UI đợt này: **toàn bộ** panel hiện có (HUD, Pause, Inventory/Equipment, Settings, Quest Log,
Shop/Crafting, Tutorial overlay, Dialogue) — không chia đợt nhỏ.

Hệ quả cần lưu ý cho phase sau (ngoài phạm vi plan này, chỉ ghi nhận):

- Khi có world scene thứ ba, phải quay lại đánh giá `SceneFlowService`/Build Settings có cần danh sách
  scene động thay vì hard-code hay không.
- `MainMenu → New Game/Continue` hiện resolve gameplay scene qua development config/session (theo
  `RuntimeArchitecture.md`); cần xác nhận lại config đó khi `MapNhat` trở thành target thật thay vì chỉ
  `DemoScene`.

## Nguyên tắc tránh xung đột khi làm từng phần

Dự án đang chạy song song nhiều tác nhân (Claude, Codex, chủ dự án chạy acceptance thủ công — xem
`Handoffs/`). Áp dụng đúng ranh giới đã có trong `DemoSceneWorkflow.md`:

- **Một hướng cắt duy nhất cho `_UI`**: chuyển `DemoScene/_UI/UICanvas` thành prefab **một lần**
  (Bước 1), sau đó `DemoScene` chỉ giữ prefab instance. Không ai được sửa trực tiếp hierarchy/layout
  bên trong prefab từ hai nơi cùng lúc — mọi thay đổi presentation sau này sửa trên prefab asset rồi cả
  hai scene tự nhận qua prefab connection.
- **`MapNhat` chỉ được cộng thêm, không sửa `DemoScene`** từ Bước 2 trở đi — đúng "Definition of Done"
  của `DemoSceneWorkflow.md` (`portability test passes outside DemoScene`).
- **Không đổi API/contract của bất kỳ Manager nào** (`InventoryManager`, `QuestManager`,
  `GameStateManager`...) — plan này thuần là scene composition, không phải backend.
- **Không chỉnh hierarchy/layout/màu/font** của bất kỳ panel nào đã đánh dấu "Claude không nên chỉnh
  trực tiếp" trong `Handoffs/CodexToClaude.md` (toàn bộ UI hiện có). Việc đóng prefab phải giữ nguyên
  cấu trúc, chỉ thay đổi container (scene → prefab asset).
- Mỗi bước dưới đây có tiêu chí hoàn tất rõ, chạy độc lập được — có thể dừng giữa chừng mà không để lại
  state dang dở phá `DemoScene` hiện có (đã `CONTENT_READY`, không được regress).

## Kế hoạch triển khai theo từng bước

Ánh xạ đúng quy trình 6 bước đã có ở [DemoSceneWorkflow.md § Quy trình phát triển một feature](DemoSceneWorkflow.md#quy-trình-phát-triển-một-feature).

### Bước 0 — Xác nhận phạm vi (chủ dự án) — ĐÃ CHỐT 2026-09-07

- [x] Hướng (2): `MapNhat` chính thức là world scene production — D-019 Accepted.
- [x] Scope UI: toàn bộ panel hiện có, mang sang trong một đợt.

### Bước 1 — Đóng gói `_UI` thành prefab dùng chung (Define contract + Build asset source) — ĐÃ XONG 2026-09-07

- Đã tạo `Assets/Prefabs/UI/GameplayUIRoot.prefab` từ `DemoScene/_UI/UICanvas` (756 object con, giữ
  nguyên toàn bộ hierarchy/component/serialized reference). `DemoScene/_UI/UICanvas` giờ là prefab
  instance `Connected` tới asset này (xác nhận bằng `PrefabUtility.GetPrefabInstanceStatus`).
- **Phát hiện bổ sung**: `DialogueUI` và `IntroCutscene` (hai Canvas root riêng, KHÔNG nằm trong
  `_UI/UICanvas`) hóa ra **đã sẵn là prefab từ trước**, không cần đóng gói lại:
  - `DialogueUI` → `Assets/Prefabs/UI/DialogueUI_v2.prefab` (đã Connected).
  - `IntroCutscene` → `Assets/Prefabs/Cinematics/IntroCutscene.prefab` (đã Connected, mặc định
    `activeSelf = false` trong scene, chỉ Timeline bật khi cần).
  - Cả hai đều là state chính thức của `GameStateManager` (`GameState.Dialogue`, `GameState.Cutscene`
    trong [GameStateManager.md](../GameStateManager.md)), nên **phạm vi "toàn bộ UI" của Bước 3 gồm cả
    ba prefab này**, không chỉ `GameplayUIRoot`.
- `EventSystem` (`InputSystemUIInputModule`) giữ riêng, không đóng vào `GameplayUIRoot.prefab` — theo
  checklist portability, cần xác nhận project không có hai `EventSystem` active cùng lúc khi nhiều scene
  load additive (áp dụng `LoadSceneMode.Single` hiện tại thì không xung đột).
- **Tiêu chí xong**: `DemoScene` chạy y hệt cũ (không regression), `GameplayUIRoot.prefab` tồn tại độc
  lập, không mất reference. ✅ Console chỉ có warning nội bộ MCP-serializer (không phải lỗi Unity), scene
  đã save.

### Bước 2 — Bổ sung `_SceneContext` scene-service + Manager instance cho `MapNhat` (Integrate)

**Phát hiện quan trọng khi khảo sát `DemoScene/_SceneContext` (2026-09-07):** `InventoryManager`,
`Equipment Manager`, `SoundFX Manager`, `MusicManager`, `TutorialManager`, `QuestManager`, `ShopManager`,
`CraftingManager`, `MapManager` là **scene instance** (static singleton tự `DontDestroyOnLoad` trong
`Awake`, theo `ServiceOwnershipLifecycle.md`), không phải application service có sẵn app-wide. `MapNhat`
hiện có 0 object trong số này ở root — nếu Direct Play `MapNhat` trước khi từng vào `DemoScene`/`MainMenu`
trong cùng phiên Editor, toàn bộ Inventory/Equipment/Quest/Shop/Crafting/Tutorial/Sound sẽ không hoạt
động. Bước 2 vì vậy gồm hai phần:

**2a — Nhân bản 9 Manager GameObject sang `MapNhat`:**

- `InventoryManager` (đã là prefab `Assets/Prefabs/InventoryManager.prefab`) + 8 object còn lại (chưa là
  prefab: `Equipment Manager`, `SoundFX Manager`, `MusicManager`, `TutorialManager`, `QuestManager`,
  `ShopManager`, `CraftingManager`, `MapManager`) — dùng `Instantiate` + `MoveGameObjectToScene` từ
  DemoScene sang MapNhat (load additive tạm thời) để giữ nguyên 100% serialized config/catalog reference
  thay vì tạo lại tay.
- **Rebind bắt buộc** các reference đang trỏ vào scene actor của DemoScene sang object tương ứng của
  chính `MapNhat`: `SoundFXManager` → Player `FootPos` + `MapManager` của MapNhat;
  `EquipmentManager` → ba `SpriteLibrary` (Head/Body/Weapon) của Player trong MapNhat;
  `MapManager` → `Player` + một `Tilemap` của MapNhat (ví dụ `Forest_Grass_ground`).

**2b — Thêm component `_SceneContext` còn thiếu:**

- `GameplaySceneLifetime` — `_persistentGameplayRoots` trỏ đúng 9 object vừa nhân bản + `Player` của
  MapNhat.
- `SceneDependencyReadinessSource` — `_requiredDependencies` = `Player` + `MapManager` của MapNhat.
- `GameplayReadinessGate` — `_readinessSources` = `SceneDependencyReadinessSource` +
  `PlayerSpawnReadinessSource` vừa thêm.
- `SpawnRegistry` — cần ít nhất một spawn point ổn định (`spawn.<area>.start`), tạo GameObject
  `PlayerSpawn_*` mới tại vị trí Player hiện tại trong MapNhat, không dùng tên GameObject làm ID.
- `PlayerSpawnReadinessSource` — `_playerStat`/`_playerTransform` = Player MapNhat,
  `_spawnRegistry`/`_worldRegistry` = chính `_SceneContext`, `_inventorySeeder` = `InventorySeeder` trên
  `InventoryManager` vừa nhân bản.
- `WorldObjectRegistry` — `_entries` để rỗng (MapNhat chưa có chest/pickup/resource node persistent nào;
  bổ sung khi content được author sau).
- `GameplaySessionController` — `_gameplaySceneName = "MapNhat"`, `_playerStat`/`_playerTransform` =
  Player MapNhat, `_worldRegistry` = `_SceneContext`.
- `SessionDirtyTracker` — không cần bind gì thêm (tự subscribe qua Manager instance vừa có ở MapNhat).

Giữ nguyên `GameBootstrap` (mode `DevelopmentGameplay`) và `GameInputCoordinator` đã có;
`GameInputCoordinator._inventoryWindow` sẽ bind ở Bước 3 khi `_UI` đã tồn tại trong MapNhat.

**Tiêu chí xong**: `MapNhat` Direct Play vẫn vào `Playing` đúng như hiện tại (không phá luồng Player
hiện có), toàn bộ 9 Manager singleton tồn tại và hoạt động độc lập trong MapNhat (không còn phụ thuộc
DemoScene từng được load trước đó), Console sạch, không duplicate singleton khi cả hai scene từng được
mở trong cùng phiên Editor.

**Trạng thái: ĐÃ XONG 2026-09-07.** Đã clone 9 GameObject Manager (`InventoryManager` — prefab instance
giữ connection, 8 object còn lại instantiate + `MoveGameObjectToScene`) từ `DemoScene` sang `MapNhat`,
rebind đúng: `EquipmentManager.headSpriteLibrary/bodySpriteLibrary/swordSpriteLibrary` → Player Head/
Body/Weapon của MapNhat; `SoundFXManager._playerFootPos` → Player/FootPos của MapNhat; `MapManager.
_player/_tilemap` → Player + `Forest_Grass_ground` của MapNhat. Tạo `PlayerSpawn_MapNhatStart` tại vị
trí Player hiện tại, `SpawnRegistry` đăng ký `spawn.mapnhat.start`. Thêm đủ 8 component `_SceneContext`
và bind chính xác theo cấu hình DemoScene (`WorldObjectRegistry._entries` để rỗng vì MapNhat chưa có
persistent world object).

Verify độc lập (đóng hẳn DemoScene khỏi Editor, chỉ `MapNhat` loaded, Direct Play):
`GameStateManager.CurrentState == Playing`, `InventoryManager/EquipmentManager/QuestManager/
ShopManager/CraftingManager/TutorialManager/MapManager.Instance` đều tồn tại — **PASS**.

**Gap phát hiện (không thuộc phạm vi Bước 2, đã xác nhận pre-existing bằng cách tái hiện y hệt trên
DemoScene gốc):** `MapManager.InitializeDictionary()` (`Assets/Scripts/GameManagers/MapManager.cs:44`)
ném `ArgumentNullException` vì `Assets/Resources/TileDatas/RockTileData.asset` có một entry `tiles`
trỏ tới TileBase asset bị thiếu/broken reference. Lỗi xảy ra y hệt khi Play `DemoScene` gốc (chưa từng
bị `MapNhat` integration đụng vào) — không phải regression của kế hoạch này, nhưng nên được ghi nhận và
sửa riêng.

### Bước 3 — Instantiate cả 3 prefab UI vào `MapNhat` (Integrate tiếp)

- Thêm `_UI` root vào `MapNhat`, kéo `GameplayUIRoot.prefab` + `EventSystem` vào.
- Kéo thêm `DialogueUI_v2.prefab` và `IntroCutscene.prefab` (giữ `activeSelf = false` mặc định giống
  DemoScene) — cả ba đều giữ prefab connection, không unpack.
- Không override bất kỳ layout/màu nào — chỉ bind scene-specific reference nếu prefab cần (ví dụ input
  action asset instance, camera reference nếu HUD cần).

**Trạng thái: ĐÃ XONG 2026-09-07.** Tạo `_UI` root trong MapNhat; instantiate `GameplayUIRoot.prefab`
(giữ prefab connection), `DialogueUI_v2.prefab` (đổi tên instance thành `DialogueUI` khớp DemoScene),
`IntroCutscene.prefab` (set `activeSelf = false` khớp mặc định DemoScene) — cả ba đều là prefab instance
thật, không unpack. Clone `EventSystem` từ DemoScene (giữ nguyên cấu hình `InputSystemUIInputModule`)
vào `_UI`. Bind `GameInputCoordinator._inventoryWindow` → `InventoryUIController` bên trong
`GameplayUIRoot` của MapNhat.

Verify độc lập (chỉ MapNhat loaded, Direct Play): `GameStateManager.Instance.Pause()` →
`CurrentState == Paused`; `OpenMenu(GameplayMenuPage.Inventory)` → `CurrentState == GameplayMenu`;
`ReturnToPreviousState()` → về đúng `Playing`. Toàn bộ state stack/history hoạt động đúng như DemoScene
— **PASS**. Console không phát sinh lỗi mới ngoài gap `MapManager`/`RockTileData.asset` đã ghi nhận ở
Bước 2 (đang xử lý ở task riêng).

**Tiêu chí xong**: `MapNhat` có Canvas hiển thị đúng HUD mặc định khi Play, không lỗi missing reference.

### Bước 4 — Verify trong MapNhat (Verify + Portability test) — ĐÃ XONG 2026-09-07

**EditMode**: 67/67 PASS.

**PlayMode full suite**: phát hiện 25 test fail, lặp lại y hệt (cùng danh sách, cùng message) khi:
(a) MapNhat mở, (b) DemoScene mở, (c) sau domain reload, (d) **trên baseline git HEAD gốc trước cả
Bước 1** (xác nhận bằng `git stash` tạm thời trên `DemoScene.unity` rồi chạy lại). Kết luận: **100%
pre-existing, không liên quan gì tới kế hoạch này.** Đã tách thành task riêng
(`task_e8b317d1`, không đụng vào phạm vi MapNhat) thay vì sửa lẫn ở đây.

**Smoke test thủ công qua `execute_code` (Play Mode thật, không phải test suite)**:
`GameStateManager.Pause()` → `Paused`; `OpenMenu(Inventory)` → `GameplayMenu`;
`ReturnToPreviousState()` → `Playing` — **PASS**, khớp behavior DemoScene.

### Bước 4b (bổ sung, theo yêu cầu 2026-09-07) — MainMenu → MapNhat trực tiếp, không qua DemoScene

Yêu cầu bổ sung: New Game/Continue từ MainMenu phải vào thẳng `MapNhat` sau Loading, không còn
`DemoScene`. Đã triển khai:

- `Assets/Scenes/MainMenu.unity` → `_SceneContext/MainMenuController._gameplaySceneName`:
  `"DemoScene"` → `"MapNhat"` (field này quyết định target cho cả `RequestNewGame` và
  `RequestContinue`, theo `MainMenuController.cs`).
- Build Settings: thêm `Assets/Scenes/MapNhat.unity` (index 2), giữ nguyên `MainMenu` (0) và
  `DemoScene` (1) ban đầu — sau đó theo yêu cầu người dùng (2026-09-07), **gỡ `DemoScene` khỏi Build
  Settings** vì flow production giờ chỉ còn `MainMenu → MapNhat`. Build Settings hiện tại: `MainMenu`
  (0), `MapNhat` (1). `DemoScene.unity` vẫn còn nguyên file và vẫn dùng để test/dựng feature trong
  Editor (Direct Play không cần scene nằm trong Build Settings) — feature test xong sẽ được promote
  sang `MapNhat` theo đúng quy trình 6 bước ở tài liệu này.
- `MapNhat/_SceneContext/SpawnRegistry`: thêm entry `NewGameFactory.TutorialStartSpawnId`
  (`"spawn.tutorial.start"`) trỏ vào `PlayerSpawn_MapNhatStart` — bắt buộc vì `NewGameFactory` hard-code
  đúng spawn ID này cho New Game; thiếu entry này Player sẽ không bị lỗi cứng nhưng sẽ log warning và
  giữ nguyên vị trí author-time thay vì spawn đúng chỗ.

**Verify end-to-end bằng `execute_code` trong Play Mode thật (MainMenu scene, không phải test suite)**:

1. `MainMenuController.RequestNewGame(2)` (Slot 2 trống) → `Loading` → chờ → `activeScene=MapNhat`,
   `state=Playing`, `Player position=(-24.70,-6.70,0)` đúng `spawn.tutorial.start` — **PASS**.
2. `GameplaySessionController.RequestSave()` tại MapNhat → `saveOk=True` — **PASS**.
3. `GameplaySessionController.RequestReturnToMainMenu()` → `Loading` → chờ → `activeScene=MainMenu`,
   `state=MainMenu` — **PASS**.
4. `MainMenuController.RequestContinue(2)` → `Loading` → chờ → `activeScene=MapNhat`,
   `state=Playing`, Player restore đúng vị trí đã save — **PASS**.
5. Xóa Slot 2 test (`DeleteSlot(2)`) để trả 3 slot về đúng trạng thái ban đầu (Slot 1 Valid không đụng,
   Slot 2/3 Empty) — **PASS**.
6. Console trong toàn bộ luồng: chỉ có 2 log đã biết (video codec warning không liên quan, và
   `ArgumentNullException` từ `MapManager`/`RockTileData.asset` đã tách task riêng
   `task_b121c6d0`) — không có lỗi mới.

Checklist gốc của Bước 4 (Playing↔Paused, Inventory menu, EditMode/PlayMode suite, Console sạch) đã
verify xong ở trên; phần Quest Log key/Settings/Save-Load UI bằng thao tác chuột/phím thật (không phải
qua `execute_code`) vẫn nên được người dùng tự chạy tay ít nhất một lần trong Editor để chắc chắn UX
mượt, tương tự cách project luôn yêu cầu physical acceptance trước khi đóng phase (xem `QualityStrategy.md`).

### Bước 5 — Cập nhật tài liệu (bắt buộc theo quy tắc quản trị) — ĐÃ XONG 2026-09-07

- [x] Cập nhật [DemoSceneWorkflow.md § MapNhat integration status](DemoSceneWorkflow.md#mapnhat-integration-status)
      với kết quả UI/manager/scene-service/MainMenu-routing đầy đủ (không còn "chỉ có Player").
- [x] Thêm dòng vào `README.md § Thứ tự đọc` trỏ tới tài liệu này (đã làm ở Bước 0).
- [x] D-019 trong `DecisionRegister.md` đã `Accepted` (đã làm ở Bước 0, trước khi Bước 1 bắt đầu).
- [x] `README.md § Trạng thái hiện tại`: cập nhật Build Settings (3 scene) và MainMenu routing mới.
- [ ] `Handoffs/ClaudeToCodex.md`: chưa ghi — để lại cho người dùng quyết định có cần Codex xác nhận
      visual/UX (project hiện dùng workflow Claude=backend/Codex=UI-authoring qua Unity MCP; phần UI
      trong kế hoạch này chỉ di chuyển/instantiate prefab có sẵn, không tạo/sửa layout mới, nên có thể
      không cần handoff riêng — nhưng đây là quyết định của người dùng, không tự ý bỏ qua).

## Trạng thái tổng thể kế hoạch: HOÀN TẤT (Bước 0–5), chờ verify thủ công cuối

Toàn bộ 6 bước đã triển khai và verify bằng tool (Unity MCP `execute_code`, EditMode/PlayMode test
runner, git-stash baseline comparison). Việc còn lại thuộc "Manual/build test" theo `QualityStrategy.md`
— người dùng tự chạy tay MainMenu → New Game → MapNhat bằng chuột/phím thật trong Editor (và lý tưởng
là một Player build) trước khi coi tính năng này production-ready 100%, đúng tinh thần "test suite pass
chưa đủ, cần physical acceptance" đã áp dụng cho mọi phase trước đó của dự án.

## Việc KHÔNG làm trong phạm vi plan này

- Không đổi `GameState`/`GameplayMenuPage` enum, không thêm state mới — HUD hiện tại đã đủ policy.
- Không đổi contract bất kỳ Manager nào (Inventory/Equipment/Quest/Shop/Crafting/Tutorial).
- Không tự quyết định D-019 thay chủ dự án — chỉ thực thi sau khi Bước 0 xác nhận.
- Không sửa `DemoScene` sau Bước 1 (chỉ đọc, không sửa thêm).
- Không tạo scene mới ngoài `MapNhat` trong plan này.

## Rủi ro và điểm cần theo dõi

- **`EventSystem` trùng khi multi-scene additive**: hiện project dùng `LoadSceneMode.Single` nên không
  có hai `EventSystem` cùng active, nhưng nếu tương lai đổi sang additive-load nhiều scene cùng lúc
  (không nằm trong roadmap hiện tại — xem "Ngoài phạm vi nền tảng" ở README.md) thì phải tách
  `EventSystem` khỏi `GameplayUIRoot` prefab.
- **`SpawnRegistry`/`WorldObjectRegistry` cần ID ổn định** cho `MapNhat` — không dùng tên GameObject làm
  ID, theo đúng `DataDrivenDevelopment.md`.
- **Manager application-scope giả định đã được tạo** (qua `MainMenu`/`DemoScene` trước đó). Nếu người
  dùng Direct Play thẳng `MapNhat` mà chưa từng qua luồng tạo các singleton này, `GameBootstrapMode.
  DevelopmentGameplay` hiện tại của `MapNhat` không tự seed Inventory/Equipment/... — cần xác nhận đây
  có phải use case cần hỗ trợ không (nếu có, `MapNhat` cần một `DevelopmentGameplay` fixture giống
  `DemoSceneInstaller`, chưa tồn tại).

## File liên quan

- `Assets/Documentation/GameStateManager.md`
- `Assets/Documentation/DevelopmentPlan/DemoSceneWorkflow.md`
- `Assets/Documentation/DevelopmentPlan/RuntimeArchitecture.md`
- `Assets/Documentation/DevelopmentPlan/ServiceOwnershipLifecycle.md`
- `Assets/Documentation/DevelopmentPlan/UIAndInteractionFlows.md`
- `Assets/Documentation/DevelopmentPlan/DecisionRegister.md` (D-019)
- `Assets/Scenes/DemoScene.unity`, `Assets/Scenes/MapNhat.unity`
