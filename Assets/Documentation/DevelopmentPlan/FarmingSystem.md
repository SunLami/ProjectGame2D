# Farming System

Tài liệu này là source of truth cho quick bar item assignment và cơ chế farming ô cố định đầu tiên.

## Quick bar contract

Current D-115/D-117 contract supersedes historical bullets below: four slots, keys 1–4; only
ItemSO.usedItems can be assigned. Seeds/bait are Inactive and selected without consumption.
Potion/Food/Drink are Active. ActivateSlot emits ActiveItemUseRequested(ItemSO) only during gameplay
with positive quantity. Teammate must subscribe/unsubscribe one effect owner, validate use and own
atomic effect/Inventory consumption. No handler means no effect or consumption. Select is selection
only for UI clicks and drag/drop. Legacy ineligible IDs are preserved but cannot resolve as usable.

- Bốn ô Inventory và HUD dùng chung assignment theo stable `itemId`.
- Mỗi loại item đang sở hữu chỉ gán vào một ô. Gán sang ô mới chuyển binding; kéo ra ngoài
  quick bar chỉ xóa binding. Cả hai thao tác giữ nguyên số lượng trong Inventory.
- HUD hiển thị tổng số lượng trong Inventory. Khi số lượng về 0, cả Inventory và HUD bỏ
  binding; nhận lại item cần gán lại.
- Save giữ nguyên bốn assignment và `selectedIndex`. Khi restore, giữ ô có chỉ số thấp nhất
  cho ID trùng và bỏ binding đã hết hàng của item đủ điều kiện sử dụng.

## Farming gameplay

- Plot được đặt sẵn; không có tilling, watering, fertilizer, wither hoặc weather trong scope đầu.
- Empty plot chỉ hợp lệ khi selected quick slot resolve thành `SeedItemSO` còn quantity và Player trong 2.5 units.
- Click hợp lệ trừ đúng một seed rồi đặt `cropId` + `plantedAtUtcTicks`.
- Crop stage được tính từ UTC elapsed và `CropDefinition.stages`; không serialize sprite/stage index.
- Mature plot hover sáng nhẹ. Click harvest kiểm tra toàn bộ reward fit trước khi ẩn crop.
- Plot visual (2026-11-26, owner request): root `FarmPlot` có sprite đất `Plants_34` (`_plotRenderer`). Ô trống hover
  hiện `HoverOutline` viền trắng theo hình sprite đất (`_plotOutline`, thay cho `EmptyHighlight` cũ). Khi có crop,
  sprite đất bị ẩn và outline chuyển sang crop (`_cropOutline`); sau harvest/plot Empty sprite đất hiện lại.
- Depth/collision (2026-11-26, owner request): project Y-sort bằng Transparency Sort Axis (0,1,0). Crop dùng
  `Default/0` + `spriteSortPoint = Pivot` (sprite crop có pivot ở gốc cây) nên sort cùng actor theo trục Y; sprite đất
  `Default/-10` luôn nằm dưới actor. `CropVisual` có `BoxCollider2D` không-trigger (`_cropCollider`) ở gốc cây: chỉ bật
  khi crop đang hiển thị, rộng `30%` sprite (clamp 0.3–0.9), cao 0.25, tự đổi theo stage; tắt khi harvest/Empty.
  Click/hover vẫn qua collider trigger 0.9×0.9 ở root.
- Loot dùng cùng drop/fly presentation với resource/chest; Inventory chỉ commit khi loot tới Player.
- Nếu capacity/Player/commit thất bại, crop mature được khôi phục và có thể thử lại.
- Farming click là non-combat primary click: không tiêu stamina, không set Attack trigger, không phát `PlayerAttacked`.

## Data boundary

- `SeedItemSO : ItemSO`: item hạt giống, typed-reference tới `CropDefinition`.
- `CropDefinition`: stable `cropId`, harvest item/quantity, growth stages và sprite.
- `FarmingCatalog`: resolver explicit cho crop definitions; tối thiểu hai crop variants dùng cùng runtime.
- `FarmPlot`: placed instance có stable `plotId`; không dùng GameObject name/toạ độ/child index làm identity.
- `FarmingSaveData`: record `{ plotId, cropId, plantedAtUtcTicks }` cho plot đang có crop.

Save schema V8 thêm quick bar; V9 thêm farming; V11 thêm farm storage (V10 là fish payload, xem
`FishingSystem.md`). Migration additive-default, save cũ bắt đầu với quick bar rỗng, tất cả plot Empty
và farm storage rỗng.

## Farm storage (D-060)

- `FarmStorageManager`: container riêng của Player, tách khỏi `InventoryManager` và khác `Chest`
  (D-033, cấp thưởng một lần) -- đây là kho gửi/rút tự do, không giới hạn thời gian.
- Chỉ nhận `SeedItemSO` hoặc item khớp `CropDefinition.HarvestItem.itemId` nào đó trong
  `FarmingCatalog` (`FarmStorageManager.IsFarmingItem`); item khác bị từ chối, không có ngoại lệ.
- Capacity của Farm Storage đồng bộ với `InventoryManager.Slots.Count` (mặc định cùng 40 và tự mở
  rộng khi Inventory tăng slot); không tự thu nhỏ để không làm mất dữ liệu đã lưu. Stack theo đúng
  `ItemSO.maxStackSize` như Inventory.
- Mở qua NPC Leofrun (`npc.leofrun`): dialogue outcome `commerce.storage` (thêm vào
  `TraderNpcInteractionUI` cạnh `commerce.shop`/`commerce.crafting`) gọi `FarmStorageUI.Instance.Open`.
- Visual contract cập nhật theo owner review: `FarmStorageUI` có hai inventory grid cạnh nhau
  (`Inventory` và `Storage`), không dùng row list. Cột Inventory hiển thị toàn bộ item hiện có;
  cell không thuộc Farming vẫn hiện nhưng không interactable, chỉ seed/nông sản mới deposit được.
  Hai `ScrollRect` cuộn dọc; cả Inventory và Storage tạo cùng số grid slot theo capacity thật của
  Inventory và tăng chiều cao theo số hàng tương ứng. Mỗi cell dùng cùng
  `inventory_slot_reference_v4.png` với Inventory chính, cùng `inventory_grid_border_hd.png` và
  `inventory_close_thin_hd.png`; item cell hiển thị icon + quantity. Board riêng chỉ thay bố cục
  thành hai cột và giữ đúng palette/pixel-cluster warm brown, antique gold, sapphire của Inventory;
  tuyệt đối không có Equipment panel/slot.
- `FarmStorageSaveData`: record `{ itemId, quantity }` theo slot, resolve qua `IItemResolver` như
  Inventory/Quick Bar -- không dùng dictionary cứng.
- `FarmStorageUI` đã có art production và hai grid như visual contract trên; click Farming item cell
  bên trái deposit cả stack, click item cell bên phải withdraw cả stack. Có thể kéo-thả icon qua lại
  giữa hai grid để chuyển toàn stack; kéo item không thuộc Farming sang Storage bị từ chối và hiện
  feedback đỏ. Chưa có partial-quantity picker.

## Integration và acceptance

- Build/test ở DemoScene trước; feature prefab/catalog sau đó được promote sang MapNhat.
- Quick bar: assign/select/swap giữ đúng binding; quantity về 0 xóa binding; load save cũ dọn ô trùng và ô đã hết.
- Plant: sai item, hết seed, ngoài tầm hoặc plot không Empty đều không mutate gì.
- Growth: stage và Mature đúng qua save/load và thời gian app đóng.
- Harvest: inventory đầy giữ nguyên crop; thành công grant đúng một lần và plot Empty.
- Click plant/harvest không phát attack animation/event và không hao stamina.
- Duplicate/empty `plotId`, `cropId`, missing seed/harvest item/stage sprite là validation error.

## Items List expansion — D-119

Carrot/Grape/Cucumber/Chillie/Mushroom/Lemon/Apple use 60-second maturity and exactly three harvest items. Lemon/Apple are single-harvest crops, using the existing Empty-after-harvest flow. Eggplant was removed by D-120 (2026-11-25); saves map `crop.eggplant` plots and Eggplant seeds/produce to Carrot. Catalog additions are preserved by FarmingAuthoring rebuilds. Seeds are Inactive quick-bar assignments under the current four-slot D-115/D-117 contract. New seed/produce prices remain unset where the sheet provides none. See SheetContentExpansion.md for gates and provisional art.

## Quest farm supply (Act I 2-2) — 2026-11-25

- MapNhat `FarmingFeature/FarmingManager._catalog` is wired to `Assets/Game/Farming/Definitions/FarmingCatalog.asset`
  (it was null: restore reported every saved crop as unresolved and harvest items were not treated as
  farming items by Farm Storage).
- Leofrun offers the planting quest after Eadric. Plant five of each crop from the quest seeds. The harvest quest auto-starts after the final required planting and waits for actual crop maturity. Existing crops in saves are retained.
- `QuestFarmSupply` checks the active planting or harvest objectives and supplies missing seeds to Inventory, with Farm Storage as overflow. It counts held seeds and crops already planted. Quest 2-2 planting objectives accept all three crops in any order. On a live farm, matching existing plots can recover missing progress up to the objective target. Seed supply uses each crop’s remaining count. Each successful planting consumes one seed.

### Farm shop assortment

Leofrun sells seeds for Carrot, Grape, Cucumber, Chillie, Mushroom, Lemon and Apple. Eadric
offers the seven corresponding harvest items, with no seeds in his Buy tab. Sell-tab produce
lists use those same harvest items. Prices are authored on the ItemSO. The seed and produce Buy prices and the per-produce
payout when the player sells are listed below, in gold.

| Crop | Seed price | Produce price | Produce payout |
|---|---:|---:|---:|
| Carrot | 4–6 | 4 | 2 |
| Cucumber | 4–6 | 4–5 | 2 |
| Chillie | 6–8 | 5–6 | 3 |
| Mushroom | 6–8 | 5–6 | 3 |
| Grape | 6–8 | 6–7 | 3 |
| Lemon | 8–10 | 7–8 | 4 |
| Apple | 8–10 | 7–8 | 4 |

One seed yields three produce after maturity. Selling all three yields a net 0–2 G,
1–3 G or 2–4 G by seed-price tier. Each produce payout is below its cheapest Buy quote.
Seed sell values are 1–2, 2–3 or 3–4 G by tier for any shop that accepts seeds.

A planted crop displays progress and UTC seconds to harvest above its sprite. The seven production crops have 60 seconds of growth followed by a five-second harvest cooldown. A downward arrow replaces the timer once harvesting is allowed. The deadline is derived from the saved planting timestamp.

Quest 2-2b credits a crop objective when the required produce quantity is already in Inventory, including after Continue. Each harvest counts through the Gather event in any crop order.
