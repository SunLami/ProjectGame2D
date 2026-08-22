# Claude → Codex Handoff

Status: `READY_FOR_CODEX_UI`

Ngày: 2026-08-22
Feature: Phase 7 — Shop/Crafting backend (Shop/Crafting UI cần Codex dựng tiếp)

## Bối cảnh

Phase 7 Shop/Crafting backend đã hoàn tất và tự vận hành đúng (46/46 EditMode, 64/64 PlayMode PASS,
Content Validation 0 error, verify sống trong DemoScene qua `execute_code`). Chi tiết kiến trúc đầy
đủ: [Phase7ImplementationReport.md](../Phase7ImplementationReport.md).

Việc cần Codex làm: dựng Shop UI (mua/bán) và Crafting UI (chọn recipe/craft), cùng thêm
`ShopInteraction`/`CraftingInteraction` capability vào `TownElderNPC` (prefab đã có từ Phase 6, tái
dùng làm chủ shop/recipe ở phase này -- không cần NPC mới). Đây thuần là UI/Canvas + component bind
vào service có sẵn — **không cần** dựng Dialogue UI hay hệ thống resource/gather (chưa tồn tại,
ngoài phạm vi Phase 7).

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### ShopManager (`Assets/Scripts/Shop/ShopManager.cs`), qua `ShopManager.Instance`

Persistent singleton, luôn tồn tại trong DemoScene sau khi scene load xong.

```csharp
public IShopResolver Catalog { get; }   // .AllShops để liệt kê shop content
public bool TryPurchase(string shopId, string itemId, int quantity, out ShopTransactionResult result);
public bool TrySell(string shopId, string itemId, int quantity, out ShopTransactionResult result);
```

`ShopDefinition` (đọc qua `Catalog.AllShops` hoặc `Catalog.TryResolve(shopId, out def)`):
`ShopId`, `DisplayName`, `NpcId`, `Stock` (mỗi `ShopStockEntry` có `ItemId`/`Price`),
`SellPriceMultiplier`.

`ShopTransactionResult` enum: `Success`, `ShopNotFound`, `ItemNotInStock`, `InsufficientGold`,
`InsufficientInventoryCapacity`, `InsufficientItemQuantity`, `GameplayNotAllowed` -- dùng để hiển
thị lý do fail cụ thể.

**Lưu ý bán lại**: `TrySell` chỉ bán được item nằm trong chính `Stock` của shop đó (giá = `Price *
SellPriceMultiplier`). Bán item không thuộc stock trả `ItemNotInStock`/`ShopNotFound` tuỳ trường hợp
-- không phải bug, là scope quyết định (xem Known limitations trong report).

### CraftingManager (`Assets/Scripts/Crafting/CraftingManager.cs`), qua `CraftingManager.Instance`

```csharp
public IRecipeResolver Catalog { get; }   // .AllRecipes
public bool TryCraft(string recipeId, string stationTag, out CraftingTransactionResult result);
```

`RecipeDefinition`: `RecipeId`, `DisplayName`, `Ingredients` (mỗi `RecipeIngredientEntry` có
`ItemId`/`Quantity`), `OutputItemId`/`OutputQuantity`, `RequiredStationTag` (rỗng = craft mọi nơi,
truyền `null`/`""` cho `stationTag`), `NpcId`.

`CraftingTransactionResult` enum: `Success`, `RecipeNotFound`, `WrongStation`,
`InsufficientIngredients`, `InsufficientOutputCapacity`, `GameplayNotAllowed`.

### ShopNpcInteractionService / CraftingNpcInteractionService (plain C#, không MonoBehaviour)

NPC component tạo instance (`new ShopNpcInteractionService(ShopManager.Instance)`,
`new CraftingNpcInteractionService(CraftingManager.Instance)`) và gọi qua đây, giống pattern
`QuestNpcInteractionService` đã dùng cho `TownElderNPC`:

```csharp
// Shop
public bool TryGetShop(string npcId, out ShopDefinition shop);
public bool TryPurchase(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result);
public bool TrySell(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result);

// Crafting
public IReadOnlyList<RecipeDefinition> GetOfferedRecipes(string npcId);
public bool TryCraft(string npcId, string recipeId, string stationTag, out CraftingTransactionResult result);
```

Cả hai validate đúng `npcId` sở hữu shop/recipe trước khi chạm Manager -- NPC component không cần tự
kiểm tra ownership.

## Content thật đã có để test UI ngay (không cần tạo asset mới)

- `Assets/Shops/ShopCatalog.asset` → `shop.town.general` (`npcId = npc.town.elder`, tái dùng
  `TownElderNPC`): bán `item.material.wood` (5 gold), `item.consumable.health_potion` (20 gold).
- `Assets/Crafting/RecipeCatalog.asset` → 2 recipe (cả hai `npcId = npc.town.elder`):
  `recipe.material.plank` (3× Wood → 1× Plank, không cần station) và
  `recipe.consumable.health_potion` (2× Wood + 1× Iron Ore → 1× Health Potion, cần
  `stationTag = "station.forge"`).
- 3 item mới có icon placeholder: `item.material.iron`, `item.material.plank`,
  `item.consumable.health_potion` (`Assets/Resources/Items/Shop/`).
- Player không có sẵn Iron Ore trong starting inventory -- để test recipe cần station, seed tạm qua
  `Resources.Load<ItemSO>("Items/Shop/IronOre")` + `InventoryManager.Instance.AddItem(...)`, hoặc
  đợi hệ thống Gather (chưa tồn tại) cấp Iron Ore thật ở phase sau.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/Shop/`, `Assets/Scripts/Crafting/` -- nếu
  cần field/API mới (ví dụ stock quantity giới hạn, base sell value chung), báo lại Claude qua
  `CodexToClaude.md`.
- Không cần lo về Quest integration -- `ShopManager.TryPurchase`/`CraftingManager.TryCraft` đã tự
  raise `QuestDomainEvents.ItemPurchased`/`ItemCrafted` thật, `QuestManager` (Phase 6) đã subscribe
  sẵn; UI chỉ cần gọi transaction, không cần biết gì về Quest.
- Không cần dựng Dialogue UI hay Resource/Gather UI (chưa tồn tại, ngoài phạm vi Phase 7).
- Không chỉnh `QuestUIRoot`, visual của `TownElderNPC`, Inventory UI, Tutorial UI hay layout/font
  hiện có -- chỉ thêm component/Canvas mới cho Shop/Crafting.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: mở Shop tại `TownElderNPC`, mua Health Potion → gold trừ đúng, item vào inventory; mua khi
  không đủ gold → từ chối, không trừ gì.
  bán lại Wood cho đúng shop đó → gold cộng đúng theo `sellPriceMultiplier`.
- Manual: mở Crafting tại `TownElderNPC`, craft Wood Plank (không cần station) → thành công, nguyên
  liệu bị trừ đúng. Craft Health Potion không đứng gần station (nếu UI có khái niệm station theo
  vị trí) → `WrongStation`; đứng đúng chỗ có `stationTag = "station.forge"` → thành công.
- Manual: một quest test tạm với objective Purchase/Craft (không có sẵn trong content hiện tại, có
  thể tạo asset tạm chỉ để verify rồi xoá) → xác nhận progress lên `ReadyToTurnIn` sau giao dịch
  thật, không cần click nào khác ngoài nút Buy/Craft.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Shop/Crafting UI và mọi thay đổi trên
`TownElderNPC` prefab thuộc Codex. Khi xong, cập nhật `CodexToClaude.md` để Claude biết UI đã sẵn
sàng (không cần thay đổi gì phía backend trừ khi phát sinh gap mới).

---

# Phase 6 — Quest backend (Quest Log/Tracker/NPC UI cần Codex dựng tiếp)

Status: `READY_FOR_CODEX_UI_BINDING` (đã `VERIFIED` bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Update 2026-08-22 — Trả lời 2 gap trong `CodexToClaude.md` (`BACKEND_GAP_FOUND`)

Đã xử lý cả hai gap Codex báo lại sau khi dựng `QuestUIRoot`/`TownElderNPC`. Không đổi
`QuestUIRoot`, `TownElderNPC` prefab, Tutorial UI, Inventory UI hay bất kỳ layout/font nào.

### 1. Presentation API cho objective progress

Thêm `QuestManager.TryGetProgress` (không đổi `ToSaveData`, không cần reflection):

```csharp
public bool TryGetProgress(string questId, out QuestProgressSnapshot snapshot);
```

`QuestProgressSnapshot` (`Assets/Scripts/Quest/QuestProgressSnapshot.cs`) là `readonly struct`:

```csharp
public QuestStatus Status { get; }
public int CurrentObjectiveIndex { get; }
public IReadOnlyList<int> ObjectiveCounters { get; }   // đồng bộ index với QuestDefinition.Objectives
```

- Trả `false` (snapshot mặc định) nếu quest chưa có runtime entry -- tức đang `Locked`/`Available`,
  chưa accept lần nào, không có gì để hiển thị progress.
- `ObjectiveCounters` là **bản copy tại thời điểm gọi** (`Clone()` trên array runtime), không phải
  live reference -- sửa mảng trả về không ảnh hưởng `QuestRuntimeState` thật, và gọi lại
  `TryGetProgress` sau khi có event mới sẽ ra snapshot mới đúng dữ liệu. Không expose mutable
  collection ra ngoài Definition/Runtime/Save boundary.
- Dùng cùng `CurrentObjectiveIndex` để index vào `QuestDefinition.Objectives[index].Description`/
  `.TargetCount` cho instruction text + target hiện tại; `ObjectiveCounters[index]` là progress số
  (`counters[i]` ứng với `Objectives[i]`, kể cả objective đã qua).
- Ví dụ hiển thị "1/2 killed" cho objective Kill đang active:
  `int current = snapshot.ObjectiveCounters[snapshot.CurrentObjectiveIndex]; int target =
  quest.Objectives[snapshot.CurrentObjectiveIndex].TargetCount;`

Verify sống trong DemoScene (Play Mode thật): accept `quest.tutorial.crafting.001` →
`TryGetProgress` = true, `counters = [0,0]`; sau 1 `RaiseEnemyKilled` khớp objective 0 →
`counters = [1,0]`. Trước khi accept, `TryGetProgress` = false đúng như spec.

### 2. Description rỗng cho objective + validator

Đã author `Description` cho toàn bộ objective của cả hai quest hiện có
(`Assets/Quests/Definitions/Quest_TutorialCrafting001.asset`,
`Assets/Quests/Definitions/Quest_Main001.asset`) -- không còn objective nào rỗng text.

`ContentValidationRunner.ValidateQuestObjective` giờ báo **Error** (không phải Warning) cho
objective có `Description` rỗng/whitespace -- coi đây là required presentation field, đúng nguyên
tắc "Handoff phải cung cấp read-model/contract public ổn định, không tự chế display data cho UI"
(Roadmap Phase 6 Boundary). Content Validation chạy lại: **0 error, 60 warning (không đổi), 69
asset checked**.

### Test

- EditMode: 42/42 PASS (không đổi số lượng file mới -- chỉ patch content).
- PlayMode: 48/48 PASS (+1: `QuestManagerPlayModeTests.TryGetProgress_ReflectsLiveStateAndReturnsADefensiveCopy`
  -- verify false khi chưa accept, đúng status/index/counters sau accept + progress, và mutate bản
  copy trả về không leak vào runtime state thật).
- Play Mode smoke test qua Unity MCP `execute_code` trên DemoScene thật (không chỉ test giả lập):
  xác nhận `TryGetProgress` hoạt động đúng trên `QuestManager.Instance` thật, console sạch.

Không có thay đổi nào khác tới contract Phase 6 gốc (event/status semantics/NPC service không đổi).

## Bối cảnh

Phase 6 Quest backend đã hoàn tất và tự vận hành đúng (42/42 EditMode, 47/47 PlayMode PASS, Content
Validation 0 error, verify sống trong DemoScene qua `execute_code`). Chi tiết kiến trúc đầy đủ:
[Phase6ImplementationReport.md](../Phase6ImplementationReport.md).

Việc cần Codex làm: dựng Quest Log/Tracker UI (danh sách quest Active/ReadyToTurnIn/Completed,
objective progress hiển thị được cho người chơi) và NPC marker/interaction UI (offer quest / turn-in
prompt) trong DemoScene. Đây thuần là UI/Canvas + một NPC prefab tối thiểu để test — **không cần**
Dialogue/Shop/Crafting UI thật (những hệ thống đó chưa tồn tại, xem "Integration gap" bên dưới).

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### QuestManager (`Assets/Scripts/Quest/QuestManager.cs`), qua `QuestManager.Instance`

Persistent singleton, luôn tồn tại trong DemoScene sau khi scene load xong (giống
`TutorialManager.Instance`/`InventoryManager.Instance`).

```csharp
public IQuestResolver Catalog { get; }                 // .AllQuests để liệt kê toàn bộ quest content
public bool IsMainQuestUnlocked { get; }

public event Action<string> QuestAccepted;             // questId
public event Action<string> QuestProgressChanged;      // questId -- objective counter/status đổi
public event Action<string> QuestCompleted;             // questId -- fire đúng 1 lần khi TryTurnIn thành công
public event Action MainQuestUnlocked;                  // fire đúng 1 lần khi Tutorial Quest chain xong

public QuestStatus GetStatus(string questId);           // Locked/Available/Active/ReadyToTurnIn/Completed/Failed
public bool TryAcceptQuest(string questId);
public bool TryTurnIn(string questId, out QuestTurnInResult result);
```

`QuestDefinition` (đọc qua `Catalog.AllQuests` hoặc `Catalog.TryResolve(questId, out def)`) có field
đọc được: `QuestId`, `DisplayName`, `Objectives` (mỗi objective có `Type`, `TargetId`, `TargetCount`,
`Description` -- dùng `Description` để hiển thị text, đừng tự chế), `IsTutorialQuest`, `IsMainQuest`,
`GiverNpcId`, `TurnInNpcId`.

Objective progress hiện tại (counter/index) không có getter công khai trực tiếp trên
`QuestRuntimeState` qua `QuestManager` -- nếu UI cần hiển thị "2/3 killed", báo lại Claude qua
`CodexToClaude.md` để thêm getter (ví dụ `QuestManager.TryGetProgress(questId, out int index, out
int[] counters)`), đừng tự đọc reflection vào private field.

### QuestNpcInteractionService (`Assets/Scripts/Quest/QuestNpcInteractionService.cs`)

Plain C# (không phải MonoBehaviour) -- NPC component của Codex tạo một instance
(`new QuestNpcInteractionService(QuestManager.Instance)`) và gọi qua đây, **không** gọi thẳng
`QuestManager` cho logic liên quan tới NPC identity:

```csharp
public bool TryGetOfferedQuest(string npcId, out QuestDefinition quest);   // quest Available mà npcId này cho
public bool TryAcceptQuest(string npcId, string questId);
public bool TryGetTurnInQuest(string npcId, out QuestDefinition quest);    // quest ReadyToTurnIn tại npcId này
public bool TryTurnIn(string npcId, string questId, out QuestTurnInResult result);
public void ReportConversation(string npcId, string outcomeId);           // cho Talk objective (xem gap)
```

`QuestTurnInResult` enum: `Success`, `QuestNotFound`, `ObjectivesIncomplete`,
`InsufficientInventoryCapacity`, `AlreadyCompleted` -- dùng để hiển thị lý do fail cụ thể thay vì
generic "failed".

## Content thật đã có để test UI ngay (không cần tạo asset mới)

- `Assets/Quests/QuestCatalog.asset` chứa 2 quest:
  - `quest.tutorial.crafting.001` ("The Blacksmith's Request", Tutorial Quest): Kill
    `enemy.slime.green`×2 tại `area.tutorial` + Obtain `item.material.wood`×3. `giverNpcId` =
    `turnInNpcId` = `npc.town.elder`.
  - `quest.main.001` ("A Call to Adventure", Main Quest, prerequisite = quest trên): Kill
    `enemy.goblin.green`×1.
- 3 enemy có sẵn trong DemoScene đã gắn `enemyId` thật: `Slime1`/`Slime2` = `enemy.slime.green`,
  `Goblin` = `enemy.goblin.green` (tất cả `areaId = area.tutorial`) -- giết chúng bằng gameplay thật
  sẽ tiến quest thật, không cần giả lập.
- **Chưa có NPC GameObject/prefab nào trong scene** -- `npc.town.elder` chỉ là stable ID trong data,
  chưa có world object tương ứng. Codex cần tự tạo GameObject/prefab NPC tối thiểu (collider tương
  tác + component gọi `QuestNpcInteractionService`) để test luồng offer/accept/turn-in bằng gameplay
  thật; không có ràng buộc hierarchy/tên cụ thể nào từ phía Claude cho NPC này.

## Integration gap có chủ đích (đừng tự chế UI cho phần này)

4 objective type sau **chưa có hệ thống production thật** (không Dialogue/Crafting/Shop/Resource
system trong project) -- đây là quyết định đã ghi rõ trong
[Phase6ImplementationReport.md](../Phase6ImplementationReport.md):

- **Talk**: `QuestNpcInteractionService.ReportConversation(npcId, outcomeId)` là entry point, nhưng
  chưa có Dialogue UI/system thật gọi nó. Nếu Codex dựng NPC "nói chuyện" đơn giản (không phải full
  dialogue tree), có thể gọi `ReportConversation` trực tiếp khi player tương tác — đó là hợp lệ, không
  phải giả lập test.
- **Craft/Purchase**: cần `CraftingService`/`ShopService` (Phase 7, chưa tồn tại). Đừng dựng Shop/
  Crafting UI giả trong Phase 6 UI pass này.
- **Gather**: cần Resource/gather interaction script (chưa có phase cụ thể). Tương tự, không tự chế.

Nếu UI cần hiển thị các objective type này trước khi hệ thống thật tồn tại, hiển thị đúng
`Description`/`TargetCount` như objective khác — không cần logic tương tác thật cho tới khi Phase 7+.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/Quest/`, `InventoryManager.HasItemId`,
  `InventoryManager.AddItem` (chỗ raise `InventoryItemAdded`), hay `EnemyUniversal._enemyId`/`_areaId`
  -- nếu cần field/API mới (ví dụ progress getter ở trên), báo lại Claude qua `CodexToClaude.md`.
- Không cần lo về restore/save -- `QuestManager.RestoreState()` (backend, gọi từ
  `PlayerSpawnReadinessSource`) đã đảm bảo UI mở giữa chừng vẫn thấy đúng status/progress qua
  `GetStatus`/`Catalog`.
- Không cần dựng Shop/Crafting/Dialogue UI thật (xem Integration gap).
- Không chỉnh `TutorialOverlayRoot`/`TutorialOverlayUI` hay Inventory UI hiện có.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: NPC hiển thị đúng quest offer khi `quest.tutorial.crafting.001` = `Available`; sau accept,
  giết 2 slime + nhặt 3 wood → UI phản ánh `ReadyToTurnIn`; turn-in tại đúng NPC → reward vào
  inventory, quest biến mất khỏi active list, `quest.main.001` chuyển `Available`.
  Cách tạo Obtain event thật: `InventoryManager.Instance.AddItem` trên `item.material.wood`
  (`Resources.Load<ItemSO>("Items/Quest/WoodMaterial")`) -- không cần world pickup script mới nếu
  chưa có, chỉ cần test UI phản ứng đúng khi backend event fires.
- Manual: turn-in tại NPC sai (không phải `turnInNpcId`) bị từ chối, không hiện reward.
- Manual: Continue game giữa chừng quest -- UI hiện đúng status/progress ngay khi vào scene, không
  cần đợi event đầu tiên.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Quest Log/Tracker/NPC UI thuộc Codex. Khi xong, cập nhật
`CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía backend trừ khi phát
sinh gap mới, ví dụ cần thêm getter progress).

---

# Phase 5 — UI hiển thị Input Tutorial (instruction prompt + skip)

Status: `READY_FOR_CODEX` (đã VERIFIED bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Bối cảnh

Phase 5 backend (`TutorialManager`, domain event, save/restore, `AreaTriggerZone`) đã hoàn tất, tự
vận hành đúng (28/28 EditMode, 32/32 PlayMode PASS) nhưng **chưa có UI nào hiển thị cho người chơi
thấy**. Chi tiết kiến trúc đầy đủ: [Phase5ImplementationReport.md](../Phase5ImplementationReport.md).

Việc cần Codex làm: dựng một overlay nhỏ trong DemoScene hiển thị `InstructionText` của step tutorial
hiện tại, và nút Skip có confirm. Đây thuần là UI/Canvas — không cần đổi gameplay logic.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

`TutorialManager` (`Assets/Scripts/Tutorial/TutorialManager.cs`), truy cập qua
`TutorialManager.Instance` (persistent singleton, luôn tồn tại trong gameplay scene sau khi
`PlayerSpawnReadinessSource` restore xong):

```csharp
public TutorialStepDefinition CurrentStep { get; }   // null nếu đã completed hoặc chưa có definition
public bool IsCompleted { get; }
public event Action<TutorialStepDefinition> OnStepChanged;   // fire khi qua step mới
public event Action OnTutorialCompleted;                     // fire đúng 1 lần khi xong step cuối
public void Skip();                                           // nhảy thẳng completed, không phát OnStepChanged
```

`TutorialStepDefinition` có field đọc được: `StepId` (string), `Type` (enum, không cần hiển thị),
`InstructionText` (string — đây là nội dung để show lên UI).

## UI cần dựng

1. **Panel instruction** (góc màn hình, ví dụ top-center hoặc top-left, không che HUD/inventory hiện
   có) — hiển thị `CurrentStep.InstructionText`.
   - Ẩn hoàn toàn nếu `TutorialManager.Instance == null` hoặc `CurrentStep == null` (đã completed
     hoặc chưa init xong).
   - Subscribe `OnStepChanged` để đổi text khi qua step mới.
   - Subscribe `OnTutorialCompleted` để ẩn panel (kèm hiệu ứng nhẹ nếu muốn, không bắt buộc).
   - Khi UI vừa `OnEnable`/mở game giữa chừng (ví dụ Continue), đọc luôn `CurrentStep` hiện tại để
     hiển thị đúng ngay lập tức — không đợi event đầu tiên.
2. **Nút Skip** trên panel đó, có **popup confirm** trước khi gọi (theo D-008 — skip tutorial phải có
   xác nhận, không skip ngay khi bấm 1 lần). Sau khi user xác nhận: gọi
   `TutorialManager.Instance.Skip()`.
3. Panel này là **gameplay overlay thuần túy** giống Inventory/Pause hiện có — không đi qua
   `GameStateManager` state machine (tutorial không pause game, không chặn input), chỉ là Canvas hiển
   thị/ẩn theo event ở trên.

## Việc Codex KHÔNG cần làm

- Không cần đổi `TutorialManager`, domain event, hay bất kỳ script nào trong
  `Assets/Scripts/Tutorial/`, `Assets/Scripts/GameManagers/AreaTriggerZone.cs` — nếu thấy cần đổi field
  gì ở đó (ví dụ thêm icon cho step, thêm field mới trong `TutorialStepDefinition`), báo lại Claude
  qua `CodexToClaude.md` thay vì tự sửa (đây là ScriptableObject data contract, đổi ẩu có thể vỡ save
  cũ hoặc content asset đã tạo).
- Không cần lo về restore/save — `TutorialManager.RestoreState()` (backend) đã đảm bảo UI mở lên giữa
  chừng vẫn thấy đúng step hiện tại qua `CurrentStep`.
- Chưa cần làm UI cho `AreaTrigger_Town`/`ReachArea` riêng — step đó cũng chỉ là một `InstructionText`
  bình thường như các step khác, panel dùng chung.

## Nội dung step hiện có (để tham khảo hiển thị, đọc thật từ asset, đừng hardcode text trong UI script)

6 step trong `Assets/Tutorial/Tutorial_TutorialArea.asset`: Move → Sprint → Attack → OpenInventory →
EquipItem → ReachArea (`area.town`, placeholder position `(10,0,0)` trong DemoScene, sẽ dời khi có Town
thật). `InstructionText` hiện tại là placeholder — nếu cần văn bản hiển thị đẹp hơn, có thể tự sửa nội
dung field đó trực tiếp trên asset qua Unity Editor (đây là content, không phải code, Codex có thể sửa
tự do), không cần hỏi lại Claude cho việc đổi text thuần túy.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: New Game → panel hiện đúng step Move → đi bộ → panel đổi sang Sprint → ... → sau step cuối
  panel ẩn.
- Manual: bấm Skip → confirm popup hiện → xác nhận → panel ẩn ngay, không đi qua step trung gian.
- Manual: Continue game đã có tutorial dở dang → panel hiện đúng step đã lưu ngay khi vào scene.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho panel này thuộc Codex. Khi xong, cập nhật
`CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía backend trừ khi phát sinh
gap mới).
