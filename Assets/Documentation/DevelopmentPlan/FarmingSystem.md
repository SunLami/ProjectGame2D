# Farming System

Tài liệu này là source of truth cho quick bar item assignment và cơ chế farming ô cố định đầu tiên.

## Quick bar contract

- Có đúng 8 slot. Assignment lưu stable `itemId`, không lưu index/reference của `InventorySlot`.
- Kéo một item thường từ Inventory lên quick slot để gán; click hoặc phím số 1–8 để chọn.
- HUD resolve icon qua `IItemResolver` và hiển thị tổng quantity đang có trong Inventory.
- Item hết quantity không xóa assignment; slot hiển thị mờ/0 và hoạt động lại khi người chơi có item.
- Save lưu tám assignment và `selectedIndex`; restore bỏ qua item ID không resolve được nhưng không crash.
- Quick bar chỉ chọn item. Behavior sử dụng item thuộc domain nhận interaction (farming, consumable về sau).

## Farming gameplay

- Plot được đặt sẵn; không có tilling, watering, fertilizer, wither hoặc weather trong scope đầu.
- Empty plot chỉ hợp lệ khi selected quick slot resolve thành `SeedItemSO` còn quantity và Player trong 2.5 units.
- Click hợp lệ trừ đúng một seed rồi đặt `cropId` + `plantedAtUtcTicks`.
- Crop stage được tính từ UTC elapsed và `CropDefinition.stages`; không serialize sprite/stage index.
- Mature plot hover sáng nhẹ. Click harvest kiểm tra toàn bộ reward fit trước khi ẩn crop.
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
- Quick bar: assign/select/swap inventory/quantity zero/save-load đều giữ đúng binding.
- Plant: sai item, hết seed, ngoài tầm hoặc plot không Empty đều không mutate gì.
- Growth: stage và Mature đúng qua save/load và thời gian app đóng.
- Harvest: inventory đầy giữ nguyên crop; thành công grant đúng một lần và plot Empty.
- Click plant/harvest không phát attack animation/event và không hao stamina.
- Duplicate/empty `plotId`, `cropId`, missing seed/harvest item/stage sprite là validation error.
