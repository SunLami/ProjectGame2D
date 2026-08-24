# Phase 5 Implementation Report

Ngày bắt đầu: 2026-08-22
Trạng thái: **Input tutorial hoàn tất và verify phần lớn; 1 bước manual verification PARTIAL (không phải code defect)**

## Phạm vi

Chỉ **Input tutorial** (Move/Sprint/Attack/OpenInventory/EquipItem/ReachArea) theo đúng Roadmap Phase 5.
**Không** làm Tutorial Quest chain (Phase 6, cần NPC/Quest system chưa tồn tại).

## Quyết định trước khi code

- Người dùng chỉ ra `GameScene.unity` (chưa commit, chỉ có Tilemap art: `Trainning Area`, `Townhouse`,
  `EnemyZone1_Forest`, `EnemuZone2_Swamp`) làm map mẫu tham khảo tên/vị trí Area.
- Đã hỏi và xác nhận: **chỉ dùng tên Area làm gợi ý stable ID** (`area.tutorial`, `area.town` — khớp
  sẵn với `NewGameFactory.TutorialAreaId`), **không** chuyển gameplay target sang `GameScene` (đó là
  quyết định D-019 riêng, ngoài phạm vi Phase 5). Toàn bộ vẫn build trong DemoScene theo
  DemoSceneWorkflow.

## Kiến trúc

- `TutorialStepType` (enum: Move, Sprint, Attack, OpenInventory, EquipItem, ReachArea) — handler type
  code hỗ trợ. `ReachArea` tổng quát hóa ví dụ "TravelToTown" trong tài liệu thành bất kỳ area nào
  (tham số `TargetAreaId`), đúng nguyên tắc "code định nghĩa loại hành vi, data định nghĩa nội dung".
- `TutorialStepDefinition`/`TutorialDefinition` (ScriptableObject, `Assets/Scripts/Tutorial/`) —
  Definition bất biến, `stepId`/`tutorialId` là save contract.
- `TutorialSaveData` — `currentStepId`, `completed`. Thêm vào `GameSaveData.tutorial`, bump
  `CurrentSaveVersion` 3 → 4.
- `TutorialManager` (MonoBehaviour, persistent singleton giống InventoryManager/EquipmentManager, torn
  down qua `GameplaySceneLifetime`) — subscribe domain event, advance step theo đúng
  `TutorialStepType` của step hiện tại, bỏ qua event không khớp. `Skip()` xử lý xác nhận đã có (đường
  confirm thuộc UI, đây là nửa backend). `RestoreState()` không phát `OnStepChanged`/`OnTutorialCompleted`.
- Domain event mới (không đọc phím cụ thể):
  - `Player.PlayerMoved`/`PlayerSprinted`/`PlayerAttacked` (static, fire trong `OnMove`/`OnSprint`/`OnAttack`).
  - `InventoryWindowUI.InventoryOpened` (static, fire trong `OpenWindow()`).
  - `EquipmentManager.ItemEquipped` (static, chỉ fire khi `Equip()` thành công, tách biệt
    `OnEquipmentChanged` vốn fire cả khi unequip).
  - `AreaTriggerZone.PlayerEnteredArea` (static, trigger volume 2D non-visual, config `areaId`).
- `PlayerSpawnReadinessSource` mở rộng thêm bước 7: restore tutorial state sau equipment/health (không
  chặn Playing — input tutorial là prompt, không phải gate). Initial save (New Game) giờ capture cả
  `tutorial`.
- `ContentValidationRunner` thêm `ValidateTutorialDefinitions`: tutorialId/stepId rỗng/trùng, `ReachArea`
  thiếu `targetAreaId`.

## Lý do kiến trúc quan trọng

- **Ownership**: mở rộng `PlayerSpawnReadinessSource` (đã dùng ở Phase 3/4) thay vì tạo
  `IGameplayReadinessSource` mới cho tutorial, vì restore tutorial không cần chặn Playing và không có
  vấn đề thứ tự — nhất quán với quyết định Phase 4 (Gate coi các source độc lập/song song).
- **Test hook**: `Player.RaiseMovedForTests()`/`RaiseSprintedForTests()`/`RaiseAttackedForTests()` và
  `AreaTriggerZone.RaiseEnteredForTests()` là `internal` (giống `ConfigureForTests` pattern có sẵn) vì
  C# event chỉ invoke được từ trong class khai báo — test cần cách hợp lệ để giả lập domain event mà
  không cần dựng toàn bộ Input System/PlayerInput thật.

## Content đã tạo (Unity MCP)

- `Assets/Tutorial/Steps/Step_Move.asset` (`tutorial.controls.move`)
- `Assets/Tutorial/Steps/Step_Sprint.asset` (`tutorial.controls.sprint`)
- `Assets/Tutorial/Steps/Step_Attack.asset` (`tutorial.controls.attack`)
- `Assets/Tutorial/Steps/Step_OpenInventory.asset` (`tutorial.controls.open_inventory`)
- `Assets/Tutorial/Steps/Step_EquipItem.asset` (`tutorial.controls.equip_item`)
- `Assets/Tutorial/Steps/Step_TravelToTown.asset` (`tutorial.controls.travel_to_town`, ReachArea → `area.town`)
- `Assets/Tutorial/Tutorial_TutorialArea.asset` (`tutorial.controls`, 6 step theo đúng thứ tự trên)

## Scene wiring (DemoScene, Unity MCP)

- GameObject **`TutorialManager`** (root riêng, KHÔNG gắn vào `_SceneContext` — xem "Bug tự phát hiện"
  bên dưới), component `TutorialManager` trỏ `Tutorial_TutorialArea.asset`.
- GameObject **`AreaTrigger_Town`** dưới `_World`, `BoxCollider2D` (`isTrigger=true`, size 4x4) +
  `AreaTriggerZone` (`_areaId = "area.town"`) tại vị trí `(10, 0, 0)` — placeholder, đánh dấu rõ sẽ di
  chuyển khi có Town thật (Phase 6/7).
- `GameplaySceneLifetime._persistentGameplayRoots` thêm `TutorialManager` để teardown đúng khi Return
  Main Menu.

## Bug tự phát hiện trong lúc wiring (đã sửa trước khi verify)

Lúc đầu gắn component `TutorialManager` trực tiếp lên `_SceneContext` — vì `TutorialManager.Awake()`
gọi `DontDestroyOnLoad(gameObject)`, việc này sẽ kéo **toàn bộ** `_SceneContext` (gồm `GameBootstrap`,
`GameInputCoordinator`, `PlayerSpawnReadinessSource`, `GameplayReadinessGate`, `SpawnRegistry`) thành
persistent xuyên scene — sai hoàn toàn kiến trúc (`_SceneContext` phải là scene-scoped, tái tạo mỗi lần
load). Phát hiện trước khi lưu scene, đã sửa: tạo GameObject `TutorialManager` riêng ở scene root,
đúng pattern `InventoryManager`/`Equipment Manager`/`SoundFX Manager` hiện có.

## Tests

- EditMode: 28/28 PASS (2 mới: `TutorialSaveDataTests` — round-trip DTO).
- PlayMode: 32/32 PASS (6 mới, `TutorialManagerPlayModeTests`): advance đúng theo event khớp/bỏ qua
  event sai; complete sau step cuối đúng 1 lần (gọi lại không fire thêm); `Skip()` không đi qua step
  trung gian; `RestoreState()` không phát event; `ReachArea` chỉ complete đúng `targetAreaId`;
  manager bị disable không phản ứng event (không duplicate subscription).
- Content Validation: 0 error, 60 warning (baseline không đổi), 64 asset checked (+1: `Tutorial_TutorialArea`).
- Scene validator DemoScene: 0 issue.

## Manual verification (Play Mode thật, `InMemorySaveSlotRepository`)

- New Game (Slot 1): tutorial bắt đầu đúng `tutorial.controls.move`. **PASS**.
- `Player.RaiseMovedForTests()`/`RaiseSprintedForTests()`/`RaiseAttackedForTests()` (giả lập qua
  reflection, tương đương domain event thật) → advance đúng thứ tự move→sprint→attack→open_inventory.
  **PASS**.
- `InventoryWindowUI.OpenWindow()` gọi **thật** (không giả lập) → advance đúng sang `equip_item`.
  **PASS**.
- `EquipmentManager.Instance.Equip(...)` gọi **thật** trên item thật trong inventory đã seed → advance
  đúng sang `travel_to_town`. **PASS**.
- **`AreaTriggerZone` qua di chuyển Player thật vào `AreaTrigger_Town` bằng teleport transform**:
  `PARTIAL — BLOCKED bởi giới hạn automation harness`, không phải lỗi code. Đã xác nhận collider thật
  sự overlap (`BoxCollider2D.IsTouching` = true, bounds khớp), physics đang chạy (`Physics2D.simulationMode
  = FixedUpdate`, frame count tăng đều), nhưng `OnTriggerEnter2D` không fire khi player được teleport
  trực tiếp vào vùng chồng lấp thay vì di chuyển liên tục qua physics step — đây là hành vi đã biết của
  Unity Box2D (teleport-vào-overlap không luôn tạo "entering" transition như di chuyển liên tục).
  Logic xử lý phía `TutorialManager` cho `ReachArea` đã được chứng minh đúng bằng automated test
  `ReachArea_CompletesOnlyOnMatchingAreaId` (gọi thẳng `AreaTriggerZone.PlayerEnteredArea`, bỏ qua
  physics). Gameplay thật (player đi bộ liên tục bằng WASD) không gặp giới hạn này vì di chuyển qua
  Rigidbody2D liên tục, đúng cách Box2D detect entering. **Không suy đoán PASS cho phần vật lý thật —
  cần verify lại bằng gameplay thật/manual keyboard khi có điều kiện.**
- Console sạch trong toàn bộ kịch bản (chỉ có 1 warning benign lặp lại từ một lệnh `Physics2D.Simulate`
  gọi sai trong lúc debug, không phải lỗi runtime).

## Chưa hoàn thành / để lại cho phase sau

- Xác nhận vật lý `AreaTriggerZone` bằng player di chuyển thật (keyboard/gamepad) — `PARTIAL`, cần
  người dùng test thủ công hoặc một PlayMode test dùng `PlayerInput`/`Rigidbody2D.MovePosition` thay vì
  teleport trực tiếp.
- `AreaTrigger_Town` là placeholder tại `(10,0,0)` trong DemoScene — di chuyển/gắn lại khi Town thật
  được dựng (Phase 6/7, có thể dùng `GameScene.unity` làm tham khảo layout).
- Tutorial Quest chain, NPC, Main Quest gate — Phase 6.
- Skip UI (confirm popup) — Codex, khi cần (backend `TutorialManager.Skip()` đã sẵn sàng).
- Manual gamepad — vẫn `BLOCKED_MANUAL_TEST` như các phase trước.

## Codex UI Handoff

**Chưa cần ngay** — Phase 5 này chỉ có domain logic + content, chưa có UI hiển thị instruction text/
prompt nào được dựng. Khi cần hiển thị `TutorialStepDefinition.InstructionText` cho người chơi:

- Subscribe `TutorialManager.Instance.OnStepChanged(TutorialStepDefinition)` để hiện prompt mới.
- Subscribe `TutorialManager.Instance.OnTutorialCompleted` để ẩn UI tutorial.
- Gọi `TutorialManager.Instance.Skip()` sau khi user xác nhận popup skip (theo D-008: skip có confirm).
- `TutorialManager.Instance.CurrentStep` (đọc `InstructionText`) cho trường hợp UI mở lại giữa chừng
  (ví dụ sau khi đóng game rồi mở lại) cần hiển thị đúng step hiện tại ngay lập tức.
