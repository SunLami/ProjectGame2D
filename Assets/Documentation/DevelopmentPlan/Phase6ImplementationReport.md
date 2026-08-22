# Phase 6 Implementation Report

Ngày: 2026-08-22
Trạng thái: **Quest backend hoàn tất, tự vận hành đúng (42/42 EditMode, 47/47 PlayMode PASS,
Content Validation 0 error). Chưa có Quest UI/NPC prefab -- xem Codex UI Handoff.**

## Phạm vi

Toàn bộ backend theo `Roadmap.md` Phase 6 + `TutorialAndQuestProgression.md` +
`DataDrivenDevelopment.md` + `SaveAndWorldPersistence.md` + `QualityStrategy.md`:

- Quest Definition data-driven (ScriptableObject), Runtime State và Save DTO tách riêng.
- Objective handler cho Talk/Obtain/Craft/Purchase/Gather/Kill qua typed domain event, không polling.
- Prerequisite graph, Main Quest gate, Main Quest unlock reconciliation (không chỉ 1 bool cache).
- NPC quest interaction/turn-in capability service (không có NPC MonoBehaviour/prefab -- xem gap).
- Atomic/idempotent turn-in transaction.
- Save/restore quest progress, bump `CurrentSaveVersion` 4 → 5.
- Content validator cho Quest/QuestCatalog.
- Hai Quest Definition content thật dùng chung runtime handler, wired trong DemoScene.

**Không** dựng Quest Log/Tracker/NPC marker UI, không chỉnh `TutorialOverlayRoot`/`TutorialOverlayUI`/
Inventory UI (đúng Boundary của task).

## Kiến trúc (`Assets/Scripts/Quest/`)

### Definition layer

- `QuestObjectiveType` (Talk/Obtain/Craft/Purchase/Gather/Kill), `QuestStatus`
  (Locked/Available/Active/ReadyToTurnIn/Completed/Failed), `ObtainObjectiveMode`
  (CountAcquired/RequirePossession -- D-014).
- `QuestObjectiveDefinition` ([Serializable], không phải ScriptableObject riêng): `Type`, `TargetId`
  (npcId/itemId/resourceId/enemyId tùy Type), `TargetAreaId` (optional, chỉ Gather/Kill đọc),
  `TargetCount`, `ObtainMode`, `Description`.
- `QuestRewardDefinition`/`QuestRewardItemEntry`: danh sách item (stable itemId + quantity) + gold +
  experience.
- `QuestDefinition` (ScriptableObject): `questId` (save contract), `displayName`,
  `prerequisiteQuestIds`, `objectives`, `rewards`, `isTutorialQuest`, `isMainQuest`, `giverNpcId`,
  `turnInNpcId`. Không mutate runtime (đúng DataDrivenDevelopment.md).
- `QuestCatalog` (ScriptableObject, implements `IQuestResolver`): mảng `QuestDefinition` + lookup
  dictionary dựng một lần (lazy). `IQuestResolver` mirror `IItemResolver` (D-020).

### Runtime layer

- `QuestRuntimeState` (plain C#, không phải MonoBehaviour -- unit test nhanh theo QualityStrategy.md
  test pyramid): `Status`, `CurrentObjectiveIndex`, `ObjectiveCounters`. `TryProgressCurrentObjective`
  clamp tại targetCount rồi advance; `CompleteCurrentObjective` (boolean-gate cho
  RequirePossession); `RestoreProgress` không side effect, tự clamp index khi content đổi.
- `QuestObjectiveMatchers`: 6 hàm static thuần (Type + TargetId + optional TargetAreaId), không có
  branch theo questId cụ thể -- handler registry pattern của DataDrivenDevelopment.md.
- `QuestDomainEvents`: 6 typed event tĩnh (`NpcConversationCompleted`, `InventoryItemAdded`,
  `ItemCrafted`, `ItemPurchased`, `ResourceGathered`, `EnemyKilled`) + `RaiseX` public method cho
  từng event -- là **API công khai** mà hệ thống tương lai (dialogue/crafting/shop/resource) phải
  gọi. Đã wire thật cho Obtain (`InventoryManager.AddItem`) và Kill (`EnemyUniversal` chết); 4 loại
  còn lại là **integration gap** có chủ đích (xem mục riêng bên dưới).
- `QuestManager` (MonoBehaviour, persistent singleton như `TutorialManager`/`InventoryManager`, torn
  down qua `GameplaySceneLifetime`): subscribe 6 domain event, dispatch theo `CurrentObjective.Type`
  của từng quest `Active` (snapshot key trước khi lặp). `GetStatus` luôn derive Locked/Available từ
  prerequisite (không trust cache) trừ khi quest đã có runtime entry. `TryAcceptQuest`/`TryTurnIn`
  theo đúng atomic transaction trong TutorialAndQuestProgression.md (validate → capacity check toàn
  bộ reward trước → grant → mark Completed → emit event → reconcile Main Quest). `RestoreState`
  không phát bất kỳ event nào.
- `QuestNpcInteractionService` (plain C#, không MonoBehaviour): capability seam cho NPC tương lai --
  `TryGetOfferedQuest`, `TryAcceptQuest`, `ReportConversation`, `TryGetTurnInQuest`, `TryTurnIn` --
  validate đúng `giverNpcId`/`turnInNpcId` trước khi chạm `QuestManager`, đúng nguyên tắc "NPC
  MonoBehaviour không trực tiếp sửa QuestManager internals".

### Save layer

- `QuestProgressSaveData` (`questId`, `status`, `currentObjectiveIndex`, `objectiveCounters`),
  `QuestSaveData` (`List<QuestProgressSaveData>`). Thêm `GameSaveData.quests`, bump
  `CurrentSaveVersion` 4 → 5 (chưa có migration pipeline -- theo đúng hiện trạng Phase 2-5, mọi
  version khác bị `FileSaveSlotRepository` báo `IncompatibleVersion`).
- `NewGameFactory.CreateDefault()` thêm `quests = new QuestSaveData()` (rỗng -- Locked/Available
  luôn derive lại, không bake).
- `PlayerSpawnReadinessSource` thêm bước 8 (sau tutorial): `QuestManager.Instance.RestoreState(...)`,
  và ghi `snapshot.quests` vào initial save New Game giống các domain khác.

## Wiring vào hệ thống có sẵn (additive, không đổi public contract cũ)

- `InventoryManager.AddItem`: raise `QuestDomainEvents.RaiseInventoryItemAdded(itemId, requestedAmount)`
  chỉ khi toàn bộ `amount` yêu cầu được thêm thành công (không raise khi add thất bại/một phần).
  Thêm `InventoryManager.HasItemId(string itemId, int amount)` (method mới, không cần
  `IItemResolver`) để `ObtainObjectiveMode.RequirePossession` kiểm tra sở hữu trực tiếp qua itemId.
- `EnemyUniversal`: thêm 2 field mới `_enemyId` (stable Kill-objective ID, rỗng = không bao giờ tính)
  và `_areaId` (optional). Raise `QuestDomainEvents.RaiseEnemyKilled(_enemyId, _areaId)` đúng một lần
  trong `EnterState(State.Dead)` (cùng chỗ `GrantExperience()`, cũng chỉ chạy một lần do state machine
  không quay lại `Dead`).
- `ContentValidationRunner`: thêm `ValidateQuestDefinitions` (questId rỗng/trùng/format, objective
  target rỗng/targetCount<=0, reward item rỗng/quantity invalid, prerequisite tham chiếu unknown ID,
  prerequisite cycle qua DFS toàn đồ thị, `isMainQuest` thiếu prerequisite → Warning, catalog thiếu/
  duplicate/missing-from-catalog).

## Integration gap có chủ đích (ghi rõ theo yêu cầu Boundary)

Bốn objective type sau **không có hệ thống production thật** trong project hiện tại (không NPC/
Dialogue, không Crafting service, không Shop service, không Resource/Gather script) -- đây là quyết
định nhất quán với Boundary của task ("Nếu transaction service Phase 7 chưa tồn tại, chốt typed event
contract và test bằng event producer/fake phù hợp, đồng thời ghi rõ integration gap"):

| Objective | Event contract sẵn sàng | Producer thật | Gap |
|---|---|---|---|
| Talk | `QuestDomainEvents.NpcConversationCompleted(npcId, outcomeId)` | Chưa có Dialogue system | `QuestNpcInteractionService.ReportConversation` là entry point tương lai |
| Craft | `QuestDomainEvents.ItemCrafted(itemId, quantity, stationId)` | Chưa có CraftingService (Phase 7) | Roadmap Phase 7 sẽ gọi `RaiseItemCrafted` sau transaction thành công |
| Purchase | `QuestDomainEvents.ItemPurchased(itemId, quantity, shopId)` | Chưa có ShopService (Phase 7) | Roadmap Phase 7 sẽ gọi `RaiseItemPurchased` sau transaction thành công |
| Gather | `QuestDomainEvents.ResourceGathered(resourceId, quantity, areaId)` | Chưa có Resource/Gather script | Cần `ResourceDefinition`/gather interaction (chưa trong roadmap phase nào cụ thể) |

Obtain và Kill có producer thật (xem mục Wiring ở trên) và được verify sống trong DemoScene (mục
Manual verification). Cả 6 loại đều có PlayMode test dùng producer/fake gọi thẳng `QuestDomainEvents`,
đúng "Handler bỏ qua domain event không match definition parameters" trong QualityStrategy.md.

## Content đã tạo (Unity MCP)

- `Assets/Resources/Items/Quest/TutorialBadge.asset` (`item.quest.tutorial_badge`, non-stackable,
  icon placeholder mượn từ `BodyLv2` theo đúng tiền lệ "icon tạm thời" của project).
- `Assets/Resources/Items/Quest/WoodMaterial.asset` (`item.material.wood`, stackable, icon placeholder).
- `Assets/Quests/Definitions/Quest_TutorialCrafting001.asset` (`quest.tutorial.crafting.001`,
  `isTutorialQuest=true`, objectives: Kill `enemy.slime.green`×2 tại `area.tutorial`, Obtain
  `item.material.wood`×3 (`CountAcquired`); reward: `item.quest.tutorial_badge`×1 + 25 gold + 15 exp).
- `Assets/Quests/Definitions/Quest_Main001.asset` (`quest.main.001`, `isMainQuest=true`, prerequisite
  = `quest.tutorial.crafting.001`, objective: Kill `enemy.goblin.green`×1; reward: `item.material.wood`×5
  + 100 gold + 50 exp) -- chứng minh 2 variant dùng chung `QuestManager`/`QuestRuntimeState`/matcher
  runtime, không sửa code cho quest cụ thể.
- `Assets/Quests/QuestCatalog.asset` tham chiếu cả hai definition trên.

## Scene wiring (DemoScene, Unity MCP)

- GameObject **`QuestManager`** (root riêng, KHÔNG gắn vào `_SceneContext` -- cùng lý do đã ghi ở
  Phase 5 report: `DontDestroyOnLoad` sẽ kéo cả `_SceneContext` persistent nếu gắn nhầm), component
  `QuestManager` trỏ `QuestCatalog.asset`.
- `GameplaySceneLifetime._persistentGameplayRoots` thêm `QuestManager` để teardown đúng khi Return
  Main Menu, cùng nhóm với `InventoryManager`/`Equipment Manager`/`TutorialManager`.
- 3 `EnemyUniversal` có sẵn trong DemoScene (`Slime1`, `Slime2` → `enemy.slime.green`; `Goblin` →
  `enemy.goblin.green`), tất cả set `_areaId = "area.tutorial"` để Kill objective test được sống
  bằng gameplay thật, không chỉ qua `RaiseEnemyKilled` giả lập.

## Tests

- EditMode: 42/42 PASS (10 mới): `QuestObjectiveMatchersTests` (6 case, mỗi objective type + null
  guard), `QuestRuntimeStateTests` (clamp/advance, no-op sau ReadyToTurnIn, CompleteCurrentObjective,
  RestoreProgress clamp khi content đổi), `QuestSaveDataTests` (round-trip DTO + `GameSaveData`),
  `QuestCatalogTests` (resolve/missing/empty).
- PlayMode: 47/47 PASS (17 mới): `QuestManagerPlayModeTests` (prerequisite gate, accept-once, mỗi
  objective type chỉ tiến khi event khớp đúng target/area, Obtain hai mode riêng biệt, turn-in grant
  đúng-một-lần + double turn-in không regrant, turn-in thiếu capacity không consume/grant gì, restore
  không phát event + vẫn phản ứng event thật sau đó, restore quest ID không tồn tại bị drop không
  throw, Main Quest unlock đúng một lần, manager bị disable không phản ứng event) và
  `QuestNpcInteractionServicePlayModeTests` (offer/accept/turn-in chỉ đúng NPC cấu hình, ReportConversation
  cho Talk).
- Content Validation: 0 error, 60 warning (baseline không đổi), 69 asset checked (+5: 2 item, 2 quest
  definition, 1 catalog).
- DemoScene validator: chưa chạy `manage_scene validate` riêng lần này (không đổi hierarchy ngoài 1
  GameObject mới + set field trên GameObject có sẵn); Play Mode smoke test bên dưới thay thế phần
  compile/reference-integrity quan trọng nhất.

## Manual verification (Play Mode thật, DemoScene, `execute_code`)

- `QuestManager.Instance` tồn tại sau khi vào Play Mode, console sạch (0 error/warning) lúc load scene.
- Kịch bản end-to-end thật: `GetStatus("quest.tutorial.crafting.001")` = `Available` → `TryAcceptQuest`
  = true → 2× `RaiseEnemyKilled("enemy.slime.green", "area.tutorial")` + 1×
  `RaiseInventoryItemAdded("item.material.wood", 3)` → status = `ReadyToTurnIn` → `TryTurnIn` =
  `Success` → `GetStatus("quest.main.001")` = `Available`, `IsMainQuestUnlocked` = `true`. **PASS**.
- Enemy thật chết (`Slime1.TakeDamage(99999f)` qua `EnemyUniversal` thật, không giả lập) → không
  throw, `IsDead = true`. **PASS** (xác nhận `RaiseEnemyKilled` gọi được từ state machine thật mà
  không có exception khi không còn quest nào đang lắng nghe objective đó nữa).
- `InventoryManager.Instance.AddItem` gọi trực tiếp trên item thật load qua `Resources.Load` (không
  qua `QuestDomainEvents.Raise*` trực tiếp) → `HasItem` đúng sau đó, xác nhận hook trong
  `InventoryManager.AddItem` thật sự chạy được trong luồng gameplay bình thường, không chỉ trong test
  fixture. **PASS**.

## Known limitations / để lại cho phase sau

- Talk/Craft/Purchase/Gather chưa có producer thật -- xem mục Integration gap.
- `HasCapacityForRewards` kiểm tra từng reward item độc lập qua `InventoryManager.HasCapacityFor`;
  chưa tính trường hợp nhiều reward item khác nhau cùng cần slot trống mới (không cộng dồn số slot
  cần across nhiều entry trong cùng một lần check) -- rủi ro thấp vì reward hiện tại chỉ 1 item/quest,
  cần lưu ý nếu sau này một quest có nhiều loại reward item cùng lúc và inventory gần đầy.
  QualityStrategy.md P2 (không P0/P1) nếu xảy ra.
- `EquipmentManager.Unequip` gọi `InventoryManager.AddItem` khi trả item về túi -- sẽ raise
  `InventoryItemAdded` giống như nhặt mới. Không ảnh hưởng gameplay hiện tại (không quest nào test
  bằng item vừa unequip), nhưng nếu một Obtain quest tương lai dùng đúng itemId của item có thể
  equip/unequip, counter `CountAcquired` sẽ tăng cả khi unequip. Ghi nhận, chưa cần fix vì chưa có
  acceptance criteria nào phụ thuộc hành vi này.
- Quest Log/Tracker UI, NPC marker/prefab, dialogue UI -- Codex, theo Boundary.
- Manual gamepad/keyboard toàn bộ acceptance scenario (accept → progress → save/reload → turn-in →
  Main Quest persist qua reload) chưa chạy bằng người dùng thật -- `BLOCKED_MANUAL_TEST` như các phase
  trước, cần Quest UI tồn tại trước khi test này có ý nghĩa (không có UI thì người chơi không thấy gì).

## Codex UI Handoff

Xem [ClaudeToCodex.md](Handoffs/ClaudeToCodex.md), đánh dấu `READY_FOR_CODEX_UI`.

## Update 2026-08-22 — Gap response sau khi Codex dựng UI

Codex dựng `QuestUIRoot`/`TownElderNPC` (xem `CodexToClaude.md` mục "Phase 6 Quest UI → Claude
Follow-up") và báo lại 2 gap, cả hai đã xử lý:

1. `QuestManager.TryGetProgress(string questId, out QuestProgressSnapshot snapshot)` +
   `QuestProgressSnapshot` (`Assets/Scripts/Quest/QuestProgressSnapshot.cs`, `readonly struct`) --
   presentation read-model cho objective index/counters, `ObjectiveCounters` là defensive copy
   (`Clone()`), không leak mutable runtime collection ra UI.
2. Author `Description` cho toàn bộ objective của `quest.tutorial.crafting.001`/`quest.main.001`;
   `ContentValidationRunner` giờ báo Error cho objective description rỗng (required presentation
   field).

EditMode 42/42, PlayMode 48/48 (+1 test cho `TryGetProgress`), Content Validation 0 error/60
warning/69 asset. Verify sống trong DemoScene qua `execute_code`. Chi tiết contract chính xác nằm
trong `ClaudeToCodex.md` mục Update. Status handoff: `READY_FOR_CODEX_UI_BINDING`.
