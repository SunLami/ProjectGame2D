# Phase 8 Implementation Report

Ngày: 2026-08-23
Trạng thái: **World persistence backend hoàn tất, tự vận hành đúng (48/48 EditMode, 85/85 PlayMode
PASS, Content Validation 0 error, DemoScene + minimal portability scene verified). Không cần UI mới
-- chỉ cần Codex author scene/prefab visual khi cần (`READY_FOR_CODEX_SCENE_INTEGRATION`). Xem Codex
Handoff.**

## Nguồn tài liệu đã đọc trước khi triển khai

`README.md`, `Roadmap.md` (Phase 8), `DecisionRegister.md`, `DataDrivenDevelopment.md`,
`SaveAndWorldPersistence.md`, `QualityStrategy.md`, `ServiceOwnershipLifecycle.md`,
`DemoSceneWorkflow.md`, `Handoffs/CodexToClaude.md` (Phase 7 UI Follow-up, `VERIFIED`).

**Lưu ý tài liệu:** yêu cầu gốc trỏ tới `SceneArchitectureAndPortability.md` -- file này không tồn
tại trong `Assets/Documentation/DevelopmentPlan/`. Đã dùng `DemoSceneWorkflow.md` (mục "Scene
portability checklist"/"Quy trình phát triển một feature" bước 5) làm nguồn thay thế gần nhất, vì đó
là tài liệu chính thức duy nhất mô tả quy trình portability test trong `README.md`. Không tạo file
mới để khớp tên cũ; ghi rõ ở đây để tránh lặp lại nhầm lẫn.

## Phạm vi

Toàn bộ backend theo Roadmap Phase 8: registry/persistent ID, bốn loại entity persistent (chest,
unique pickup, boss, resource node), save/restore trước khi mở khóa gameplay, content validation
scene-scoped, và sửa `MapManager` (scene-reference rebind). Không dựng UI production, không đổi
`CommerceUIRoot`/`QuestUIRoot`/Inventory/Tutorial/MainMenu UI, không refactor Shop/Crafting/Quest
backend ngoài phạm vi trực tiếp liên quan.

## Kiến trúc (`Assets/Scripts/World/`)

### Contract chung

- `WorldObjectKind` (Chest, UniquePickup, Boss, ResourceNode) -- enemy thường **không** có kind
  riêng, cố ý: chúng respawn bằng cách luôn có mặt sẵn trong scene, không bao giờ nhận persistentId,
  đúng acceptance criteria "không tạo save record cho enemy thường".
- `IPersistentWorldObject`: `PersistentId`, `Kind`, `CaptureState()`, `RestoreState(state)`. Mọi
  entity persistent implement interface này trực tiếp trên chính component hành vi của nó (không có
  component "identity" tách rời) -- bốn loại có hành vi đủ khác nhau nên không có logic dùng chung
  ngoài chính interface.
- `WorldObjectState` (runtime, `readonly struct`): `Flag` (opened/collected/defeated tùy Kind) +
  `NextRespawnUtcTicks` (chỉ ResourceNode dùng).
- `WorldObjectSaveData`/`WorldSaveData`: DTO thuần tương ứng 1-1 với `WorldObjectState`, đúng
  "record theo ID và payload nhỏ" của SaveAndWorldPersistence.md, không phải một `GameDatabase` lớn.

### Registry (scene service)

`WorldObjectRegistry` (`Assets/Scripts/World/WorldObjectRegistry.cs`), mirror chính xác pattern
`SpawnRegistry` (Phase 3): mảng `MonoBehaviour[] _entries` bind qua Inspector, **không**
`Find`/`FindObjectsByType` ở runtime. Không phải singleton `DontDestroyOnLoad` -- sống/chết cùng
scene, đặt làm component trên `_SceneContext` cạnh `SpawnRegistry`/`PlayerSpawnReadinessSource`.

- `ToSaveData()`: capture toàn bộ object đã đăng ký.
- `RestoreState(data, missingIds)`: áp dụng state trực tiếp, không phát event/reward; persistentId
  trong save không khớp object nào trong scene được thêm vào `missingIds` thay vì throw (content bị
  xóa/đổi giữa version không làm hỏng toàn save, đúng acceptance criteria).
- Duplicate persistentId khi đăng ký: giữ entry đầu, log `Debug.LogError`, không throw.
- `Entries` (raw Inspector array, khác `Objects` runtime list) tồn tại riêng cho content validator
  Edit Mode đọc được mà không cần `Awake()` đã chạy.

### Bốn entity persistent

- `ChestInteractable`: `TryOpen(out granted)` cấp đúng một reward item stack (itemId+quantity
  authored inline, không phải LootTable riêng -- phạm vi Phase 8 là cơ chế persistence, không phải
  hệ thống loot-table tổng quát). Validate capacity trước khi mutate; thất bại không tiêu tốn gì,
  retryable.
- `UniquePickupInteractable`: `TryCollect(out granted)` cấp item rồi tự ẩn
  (`gameObject.SetActive(false)`, không destroy -- `WorldObjectRegistry` giữ reference trực tiếp,
  phải còn hợp lệ cho capture/restore về sau).
- `BossDefeatTracker`: **cố ý là GameObject riêng**, không phải component trên chính boss --
  `EnemyUniversal` tự destroy GameObject của nó `_deathLifetime` giây sau khi chết, nếu tracker nằm
  chung sẽ bị kéo theo, để lại reference chết trong `WorldObjectRegistry`. Subscribe
  `EnemyUniversal.Died` (event mới, fire đồng bộ ngay khi chết, trước delayed Destroy) để bắt state
  trước khi GameObject biến mất. Restore với `defeated=true` gọi `EnemyUniversal.RestoreDefeated()`
  (method mới) -- tắt GameObject trực tiếp, không qua `TakeDamage`/state machine, nên không phát lại
  `HealthChanged`/`Died`/`QuestDomainEvents.EnemyKilled` hay animation chết.
- `ResourceNodeInteractable`: `TryHarvest(out granted)` cấp item, phát
  `QuestDomainEvents.RaiseResourceGathered` (đóng nốt integration gap Gather còn lại từ Phase 6/7 --
  giờ cả 6/6 objective type Quest đều có ít nhất một producer thật), rồi set cooldown.
  `IsAvailable` so sánh trực tiếp `DateTime.UtcNow.Ticks` -- không `Update()`/polling (D-015, xem
  Decision Register).

### Tích hợp readiness/save

- `GameSaveData.world` (mới) + bump `CurrentSaveVersion` 5 → 6.
- `PlayerSpawnReadinessSource` (đã là `IGameplayReadinessSource` từ Phase 1) thêm bước 9: restore
  world **trước khi** báo ready -- world state luôn sẵn sàng trước khi gameplay mở khóa, đúng
  acceptance criteria, không cần readiness source riêng. `WriteInitialSave` (New Game) thêm capture
  world giống các domain khác.
- `NewGameFactory.CreateDefault()` thêm `world = new WorldSaveData()` (rỗng -- mọi object bắt đầu ở
  default state đã author trong scene).

### MapManager fix (scene-reference rebind)

`ServiceOwnershipLifecycle.md` đã ghi rõ `MapManager` là bug: `static` singleton +
`DontDestroyOnLoad` nhưng giữ `_tilemap`/`_player` là scene reference, cộng `_player =
FindAnyObjectByType<Player>()` trong `Awake()`. Hệ quả thực tế: sau một vòng scene reload thứ hai
(ví dụ Return Main Menu → vào lại gameplay), `MapManager` **mới** của scene mới bị tự `Destroy()` vì
`Instance != null`, còn `MapManager` **cũ** (giữ Tilemap/Player đã unload) tiếp tục sống --
footstep audio (`SoundFXManager.PlayFootSteps`) sẽ dùng dữ liệu chết.

Đã sửa: bỏ `DontDestroyOnLoad` và guard duplicate-destroy, `MapManager` giờ là **scene service**
đúng nghĩa -- sống/chết cùng scene, `Awake()` chỉ gán `Instance = this`, `OnDestroy()` clear
`Instance` nếu đang trỏ chính nó. Bỏ `FindAnyObjectByType<Player>()`; `_player` giờ bind qua
Inspector giống `_tilemap` đã có sẵn. Verify sống: reload `DemoScene` giữa Play Mode, đúng 1
`MapManager` tồn tại sau reload, `_player`/`_tilemap` là reference của scene mới, không exception,
console sạch (chi tiết ở mục Manual verification).

**Không đổi `SoundFXManager`** dù nó cũng giữ `_playerFootPos` (scene Transform) trong khi bản thân
là `DontDestroyOnLoad` -- cùng lớp bug nhưng nằm ngoài phạm vi "MapManager" được giao trực tiếp; ghi
rõ ở Known limitations để không âm thầm bỏ qua.

## Content đã tạo (Unity MCP)

- `Assets/Resources/Items/World/AncientRelic.asset` (`item.unique.ancient_relic`, non-stackable,
  icon placeholder mượn từ tiền lệ `BodyLv2` như các item trước).
- `Assets/Prefabs/World/Chest.prefab` -- prefab asset của `ChestInteractable` (portability proof,
  xem Runtime verification).
- DemoScene (`_World`): `Chest_TownGeneral` (`world.chest.town.general.01`, reward
  `item.material.iron`×2), `Pickup_AncientRelic` (`world.pickup.tutorial.relic.01`, reward
  `item.unique.ancient_relic`×1), `ResourceNode_WoodLog` (`world.resource.tutorial.wood_log.01`,
  resourceId `resource.wood.log`, reward `item.material.wood`×2, respawn 60s),
  `BossTracker_ForestGuardian` (`world.boss.forest.guardian.01`, tham chiếu enemy mới
  `ForestGuardianBoss`).
- `ForestGuardianBoss`: duplicate của `Goblin` với `enemyId` riêng
  (`enemy.boss.forest_guardian`, tách khỏi `enemy.goblin.green` để không lẫn với Kill objective của
  `quest.main.001`), đặt tại `(10, 5)` để không chồng enemy có sẵn.
- `_SceneContext` thêm component `WorldObjectRegistry` (4 entry) và
  `PlayerSpawnReadinessSource._worldRegistry` trỏ tới đúng component đó.

## Scene wiring & MapManager (DemoScene, Unity MCP)

- `MapManager._player` được bind thủ công tới `Player` GameObject thật (trước đây luôn `null` trong
  Inspector vì dựa hoàn toàn vào `FindAnyObjectByType`).
- Không cần GameObject mới nào khác cho World persistence -- toàn bộ nằm trên `_SceneContext`/`_World`
  đã có.

## Content Validation

`ContentValidationRunner` thêm `ValidatePersistentWorldObjects`: **khác** mọi validator trước (quét
`AssetDatabase`), phần này quét **scene(s) đang loaded trong Editor** vì bốn entity là MonoBehaviour
scene instance, không phải project asset. Kiểm tra: persistentId rỗng, persistentId trùng (kể cả
giữa nhiều scene loaded cùng lúc), format stable ID, và object có persistentId nhưng chưa được đăng
ký trong `WorldObjectRegistry` nào trong scene đó. Giới hạn có chủ đích: chỉ validate scene hiện
đang mở, chưa tự động quét toàn bộ scene trong Build Settings.

Kết quả: **0 error, 60 warning (baseline không đổi), 83 asset checked** (+6 so với cuối Phase 7: 1
item, 4 object có persistentId, 1 catalog check thêm từ WorldObjectRegistry).

## Tests

- EditMode: 48/48 PASS (2 mới): `WorldSaveDataTests` (round-trip DTO + `GameSaveData`, mirror
  `QuestSaveDataTests`).
- PlayMode: 85/85 PASS (23 mới):
  - `WorldObjectRegistryPlayModeTests`: capture đầy đủ object đăng ký, restore chỉ áp dụng đúng
    object khớp ID, unknown ID được báo cáo không throw, restore idempotent, duplicate persistentId
    giữ entry đầu (LogAssert.Expect cho Error log có chủ đích).
  - `PersistentWorldObjectsPlayModeTests`: Chest/UniquePickup/ResourceNode -- cấp đúng-một-lần,
    thất bại capacity không tiêu tốn gì, restore không cấp lại/không phát event, cooldown resource
    node đúng theo timestamp.
  - `BossDefeatTrackerPlayModeTests`: **enemy thật** (`EnemyUniversal` + `Rigidbody2D`/`Animator`)
    chết → `Died` fire đúng 1 lần → tracker ghi nhận; restore-defeated tắt boss im lặng, không
    `Died`/`HealthChanged`/`QuestDomainEvents.EnemyKilled` lặp lại; enemy thường không gắn tracker
    không bao giờ xuất hiện trong `WorldSaveData` (chứng minh acceptance criteria bằng test, không
    chỉ bằng thiết kế).
  - `PlayerSpawnReadinessSourcePlayModeTests` (+2): New Game capture world vào initial save;
    Continue restore world **trước khi** `IsReady`, unknown persistentId bị bỏ qua an toàn kèm
    warning log, không throw.
  - `MapManagerPlayModeTests` (mới, 3 test): không còn `DontDestroyOnLoad`, `OnDestroy` clear
    `Instance` đúng để lần scene load kế tiếp rebind sạch (không duplicate/không stale), gọi
    `GetCurrentTileAudioClip` qua reference tự bind không throw.

## Manual verification (Play Mode thật, DemoScene + minimal portability scene, `execute_code`)

- `WorldObjectRegistry.Objects.Count == 4` ngay sau khi vào Play Mode (đăng ký qua `Awake()`, không
  Find). Console sạch lúc load.
- Kịch bản thật tuần tự trên cả 4 object: `chest.TryOpen()`, `pickup.TryCollect()`,
  `node.TryHarvest()`, `boss.TakeDamage(999999f)` -- tất cả thành công, `bossTracker.IsDefeated ==
  true`, `registry.ToSaveData().objects.Count == 4`. **PASS**.
- Capture snapshot rồi `RestoreState` lại chính nó (mô phỏng reload): `missingCount == 0`, chest/
  pickup không cấp lại lần hai, resource node vẫn on cooldown, boss vẫn defeated -- **idempotent
  restore xác nhận bằng runtime thật, không chỉ test giả lập**. Console sạch.
- **Minimal portability scene** (`Assets/Scenes/Tests/Phase0PortabilityTest.unity`, quy ước có sẵn
  từ Phase 0): instantiate `Chest.prefab` với `persistentId` khác (`world.chest.portability_test.01`),
  chỉ phụ thuộc `InventoryManager` đã có sẵn trong scene đó (không `WorldObjectRegistry`, không
  `GameSessionManager` -- đúng phạm vi hẹp "component tự hoạt động ngoài DemoScene", không phải full
  save-integration harness). `TryOpen()` cấp reward đúng. Scene validator: 0 issue. 7 lỗi console khi
  vào Play Mode trong scene này đều từ `EquipmentSlotUI.cs` (prefab `PauseMenu`/`InventoryUIController`
  có sẵn từ Phase 0, không liên quan World) -- xác nhận qua stack trace, không phải regression của
  Phase 8.
- **MapManager rebind**: `SceneManager.LoadScene("DemoScene")` giữa Play Mode → đúng 1 `MapManager`
  tồn tại sau reload (`FindObjectsByType` đếm được 1), `_player`/`_tilemap` là reference scene mới,
  `GetCurrentTileAudioClip` không throw, console sạch.
- DemoScene validator (`manage_scene validate`): 0 issue trước và sau toàn bộ thay đổi.

## Known limitations / để lại cho phase sau

- `SoundFXManager` vẫn giữ `_playerFootPos` (scene Transform) trong khi là `DontDestroyOnLoad` --
  cùng lớp bug với `MapManager` cũ, đã ghi nhận trong `ServiceOwnershipLifecycle.md` nhưng nằm ngoài
  phạm vi "MapManager" được giao trực tiếp ở Phase 8. Chưa sửa, không âm thầm bỏ qua -- ghi rõ ở đây
  để phase sau xử lý cùng nhóm world/audio ownership.
- `ResourceNodeInteractable` không có visual auto-refresh khi hết cooldown (không polling mỗi frame
  theo yêu cầu kiến trúc) -- `IsAvailable` tính đúng on-demand, chỉ cần một lần tương tác hoặc một
  presentation script tương lai tự query lại để cập nhật hiển thị. Không ảnh hưởng tính đúng của
  giao dịch.
- Chest reward là item+quantity authored trực tiếp trên từng instance, chưa có `LootTableDefinition`
  dùng chung nhiều chest -- đúng phạm vi tối giản của Phase 8 (persistence mechanism, không phải
  loot-table system), có thể nâng cấp sau nếu cần nhiều chest chia sẻ bảng thưởng.
- Content validator persistent-world-object chỉ quét scene đang mở trong Editor, chưa tự động quét
  toàn bộ scene trong Build Settings (chưa có batch/CI multi-scene validation).
- D-015 (resource respawn clock) dùng UTC tuyệt đối, chưa có catch-up/rate-limit cho trường hợp
  offline rất dài (ví dụ hàng trăm resource node cùng respawn sau nhiều ngày không chơi) -- xem chi
  tiết quyết định trong `DecisionRegister.md`.
- Chưa có UI/visual production cho bốn entity (chỉ `_openedIndicator`/`_depletedIndicator` optional
  hook) -- đúng Boundary, để Codex author khi cần.

## Codex Handoff

Xem [ClaudeToCodex.md](Handoffs/ClaudeToCodex.md), đánh dấu `READY_FOR_CODEX_SCENE_INTEGRATION`
(Phase 8 không bắt buộc UI mới, nhưng liệt kê rõ phần scene/prefab visual Codex có thể bổ sung khi cần).
