# Phase 7 Implementation Report

Ngày: 2026-08-22
Trạng thái: **Shop/Crafting backend hoàn tất, tự vận hành đúng (46/46 EditMode, 64/64 PlayMode
PASS, Content Validation 0 error). Chưa có Shop/Crafting UI -- xem Codex UI Handoff.**

## Phạm vi

Toàn bộ backend theo `Roadmap.md` Phase 7 + `DataDrivenDevelopment.md` + `SaveAndWorldPersistence.md`
+ `QualityStrategy.md`:

- `ShopService`/`CraftingService` tách khỏi UI, atomic với gold/nguyên liệu/inventory capacity.
- Data-driven Shop/Recipe definitions + catalog, cùng quy ước stable ID sẵn có trong
  `DataAssetStableIdInventory.md`.
- Typed events `ItemPurchased`/`ItemCrafted` (đã tồn tại từ Phase 6 `QuestDomainEvents`) giờ có
  producer thật, đóng integration gap của `QuestManager` đã ghi trong `Phase6ImplementationReport.md`.
- NPC capability seam (`ShopNpcInteractionService`/`CraftingNpcInteractionService`), không dựng
  Shop/Crafting UI.
- Content validation cho Shop/Recipe catalog.

**Không** dựng Shop/Crafting UI, không chỉnh `QuestUIRoot`, `TownElderNPC` visual, Inventory UI,
Tutorial UI hay bất kỳ layout/font nào (đúng Boundary của task).

## Kiến trúc

### `Assets/Scripts/Shop/`

- `ShopStockEntry` ([Serializable]): `itemId` (stable, resolved qua `IItemResolver`) + `price`.
  Không có stock quantity ở phase này -- shop bán không giới hạn; "Restock definition tương lai"
  (DataDrivenDevelopment.md) để lại cho phase sau khi cần.
- `ShopDefinition` (ScriptableObject): `shopId` (save/runtime contract), `displayName`, `npcId`
  (NPC sở hữu shop -- mirror `giverNpcId`/`turnInNpcId` pattern của `QuestDefinition`),
  `stock[]`, `sellPriceMultiplier`.
- `ShopCatalog` (ScriptableObject, implements `IShopResolver`): mirror `QuestCatalog` -- lookup
  dictionary dựng một lần.
- `ShopManager` (MonoBehaviour, persistent singleton như `QuestManager`/`InventoryManager`, torn
  down qua `GameplaySceneLifetime`): `TryPurchase`/`TrySell`. Validate toàn bộ (gameplay state, gold/
  possession, inventory capacity) **trước** khi mutate bất cứ gì; raise
  `QuestDomainEvents.RaiseItemPurchased` đúng một lần sau `TryPurchase` thành công. `TrySell` chỉ bán
  được item nằm trong stock của chính shop đó (xem Known limitations).
- `ShopNpcInteractionService` (plain C#, mirror `QuestNpcInteractionService`): capability seam cho
  NPC tương lai, validate đúng `npcId` sở hữu shop trước khi chạm `ShopManager`.

### `Assets/Scripts/Crafting/`

- `RecipeIngredientEntry`, `RecipeDefinition` (ScriptableObject): `recipeId`, `ingredients[]`,
  `outputItemId`/`outputQuantity`, `requiredStationTag` (optional, rỗng = craft được mọi nơi),
  `npcId` (optional, NPC cung cấp recipe này như một capability).
- `RecipeCatalog` (ScriptableObject, implements `IRecipeResolver`): mirror `ShopCatalog`.
- `CraftingManager` (MonoBehaviour, persistent singleton): `TryCraft(recipeId, stationTag, out
  result)`. Validate station tag khớp, từng ingredient đủ số lượng, và output đủ chỗ chứa **trước**
  khi consume/grant gì; raise `QuestDomainEvents.RaiseItemCrafted` đúng một lần sau thành công.
- `CraftingNpcInteractionService` (plain C#, mirror `ShopNpcInteractionService`).

### Gameplay-state gate

`ShopManager.TryPurchase`/`TrySell` và `CraftingManager.TryCraft` đều kiểm tra
`GameStateManager.AllowsGameplayInput` trước tiên (trả `GameplayNotAllowed` nếu không), đúng
Roadmap "NPC chỉ mở đúng interaction khi player trong range và gameplay state cho phép" -- range là
việc của UI (giống `QuestNpcInteractionUI` proximity trigger có sẵn), gameplay-state là an toàn ở
tầng transaction engine (defense in depth, không phụ thuộc UI gate đúng 100%).

## Vì sao không có Save DTO mới

Shop không có stock runtime thay đổi (unlimited, không depletion) và Crafting không có runtime state
nào giữa các lần craft -- theo DataDrivenDevelopment.md "Shop runtime giữ stock thay đổi **nếu thiết
kế cần**", ở đây thiết kế phase này không cần. Không có gì để restore, nên không thêm field vào
`GameSaveData`, không bump `CurrentSaveVersion`. Khi thêm limited/restocking stock ở phase sau, đó là
lúc cần `ShopRuntimeState` + save DTO thật.

## Content đã tạo (Unity MCP)

- `Assets/Resources/Items/Shop/IronOre.asset` (`item.material.iron`), `WoodPlank.asset`
  (`item.material.plank`), `HealthPotion.asset` (`item.consumable.health_potion`, Consumable type;
  hiệu ứng hồi máu khi dùng chưa implement ở phase này -- chỉ là item mua/craft được).
- `Assets/Shops/Definitions/Shop_TownGeneral.asset` (`shop.town.general`, `npcId = npc.town.elder`
  -- tái dùng NPC đã có từ Phase 6 thay vì tạo NPC mới): bán `item.material.wood` (5 gold) và
  `item.consumable.health_potion` (20 gold), `sellPriceMultiplier = 0.5`.
- `Assets/Crafting/Recipes/Recipe_WoodPlank.asset` (`recipe.material.plank`, không cần station):
  3× `item.material.wood` → 1× `item.material.plank`.
- `Assets/Crafting/Recipes/Recipe_HealthPotion.asset` (`recipe.consumable.health_potion`, cần
  `station.forge`): 2× `item.material.wood` + 1× `item.material.iron` → 1× `item.consumable.health_potion`.
  Hai recipe này chứng minh 2 variant dùng chung `CraftingManager`, một có station requirement một
  không, không sửa runtime code.
- `Assets/Shops/ShopCatalog.asset`, `Assets/Crafting/RecipeCatalog.asset`.

## Scene wiring (DemoScene, Unity MCP)

- GameObject **`ShopManager`** và **`CraftingManager`** (root riêng, không gắn vào `_SceneContext`,
  cùng lý do đã ghi ở Phase 5/6 report), trỏ `ShopCatalog.asset`/`RecipeCatalog.asset`.
- `GameplaySceneLifetime._persistentGameplayRoots` thêm cả hai để teardown đúng khi Return Main Menu.
- Không tạo/chỉnh NPC GameObject nào -- `npc.town.elder` trong data trỏ tới `TownElderNPC` đã có sẵn
  từ Phase 6, nhưng component `ShopInteraction`/`CraftingInteraction` UI thật (đọc
  `ShopNpcInteractionService`/`CraftingNpcInteractionService`) chưa được thêm vào prefab đó -- đó là
  việc của Codex khi dựng UI (xem Codex UI Handoff), không đụng tới prefab ở backend pass này.

## Content Validation

`ContentValidationRunner` thêm `ValidateShopDefinitions`/`ValidateRecipeDefinitions`: ID rỗng/trùng/
format, stock/ingredient rỗng, itemId rỗng/trùng/không tồn tại trong bất kỳ `ItemSO` nào (cross-
reference với danh sách item hợp lệ dùng chung với `ValidateItems`), quantity/price <= 0, thiếu
catalog, catalog thiếu/duplicate reference -- đúng mirror pattern của `ValidateQuestDefinitions`.

Kết quả: **0 error, 60 warning (baseline không đổi), 77 asset checked** (+8 so với cuối Phase 6: 3
item, 1 shop, 2 recipe, 2 catalog).

## Tests

- EditMode: 46/46 PASS (4 mới): `ShopCatalogTests`, `RecipeCatalogTests` (resolve/missing/empty,
  mirror `QuestCatalogTests`).
- PlayMode: 64/64 PASS (16 mới):
  - `ShopManagerPlayModeTests`: purchase thành công raise `ItemPurchased` đúng 1 lần, gold không đủ
    không trừ/không cấp gì, capacity không đủ không trừ gold, item không trong stock, gameplay không
    cho phép, sell chỉ bán được item trong stock chính shop đó ở giá chiết khấu, sell thiếu số lượng.
  - `CraftingManagerPlayModeTests`: craft thành công raise `ItemCrafted` đúng 1 lần và consume đúng
    ingredient, thiếu ingredient không consume gì, sai station (kể cả station khác chứ không chỉ
    thiếu), thiếu output capacity không consume ingredient, gameplay không cho phép.
  - `ShopCraftingNpcInteractionServicePlayModeTests`: cả hai service chỉ hoạt động qua đúng NPC sở
    hữu, NPC sai bị từ chối không mutate gì.
  - `QuestShopCraftingIntegrationPlayModeTests`: **transaction thật** qua `ShopManager.TryPurchase`/
    `CraftingManager.TryCraft` (không gọi `QuestDomainEvents.Raise*` trực tiếp) tiến đúng Purchase/
    Craft objective trên `QuestManager` thật -- chứng minh acceptance criteria "Quest objective không
    phụ thuộc click UI; chỉ phụ thuộc transaction thành công" bằng code, không chỉ bằng thiết kế.

## Manual verification (Play Mode thật, DemoScene, `execute_code`)

- `ShopManager.Instance`/`CraftingManager.Instance` tồn tại sau khi vào Play Mode, console sạch.
- Kịch bản thật: mua 1 Health Potion (100 gold → 80 gold, `ItemPurchased` fires), mua 5 Wood (→ đủ
  nguyên liệu), craft `recipe.material.plank` không cần station → thành công, craft
  `recipe.consumable.health_potion` không có station → `WrongStation`, cùng recipe với
  `"station.forge"` → thành công sau khi có đủ Iron Ore. Tổng cộng 2 `ItemPurchased` + 1 `ItemCrafted`
  events quan sát được qua subscribe trực tiếp trong Play Mode thật (không phải test giả lập). Console
  sạch trong toàn bộ kịch bản.

## Known limitations / để lại cho phase sau

- `TrySell` chỉ bán được item nằm trong chính stock của shop đó (dùng lại `price` của stock entry để
  tính giá bán). Một "general vendor mua mọi thứ" cần thêm base-value field trên `ItemSO` (đã được
  gợi ý trong `DataDrivenDevelopment.md` "Buy/sell base value") -- không thêm ở phase này vì không có
  acceptance criteria yêu cầu, tránh field không dùng tới trên toàn bộ 60+ item hiện có.
  QualityStrategy.md P3 nếu design sau này cần.
- `TryCraft` kiểm tra output capacity **trước khi** consume ingredient (cùng pattern với
  `QuestManager.TryTurnIn` reward capacity check ở Phase 6) -- một craft mà output chỉ vừa chỗ *sau
  khi* ingredient bị tiêu thụ (ví dụ ingredient cuối cùng không-stack-được giải phóng đúng 1 slot) sẽ
  bị từ chối dù về mặt logic có thể thành công. Rủi ro thấp, P2 theo QualityStrategy.md, cùng loại
  limitation đã ghi nhận ở Phase 6.
- `HealthPotion` là item mua/craft được nhưng chưa có hiệu ứng hồi máu khi dùng (không có hệ thống
  "use consumable" trong project) -- ngoài phạm vi Phase 7 (không có trong Roadmap acceptance
  criteria), chỉ tồn tại như content item hợp lệ để chứng minh Purchase/Craft objective.
  `ItemType.Consumable` đã tồn tại từ trước (`ItemSO.type`), không phải field mới.
  Không thêm consume-on-use logic ở phase này.
- Không có Shop/Crafting UI, không có `ShopInteraction`/`CraftingInteraction` component trên
  `TownElderNPC` prefab -- Codex, theo Boundary.
- Manual gamepad/keyboard cho toàn bộ acceptance scenario (mua/bán/craft qua UI thật) chưa chạy --
  `BLOCKED_MANUAL_TEST` như các phase trước, cần Shop/Crafting UI tồn tại trước khi test này có ý
  nghĩa.

## Codex UI Handoff

Xem [ClaudeToCodex.md](Handoffs/ClaudeToCodex.md), đánh dấu `READY_FOR_CODEX_UI`.
