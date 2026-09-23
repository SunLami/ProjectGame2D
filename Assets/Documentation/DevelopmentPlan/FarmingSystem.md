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

Save schema V8 thêm quick bar; V9 thêm farming. Migration V7→V8/V8→V9 additive-default, save cũ bắt đầu
với quick bar rỗng và tất cả plot Empty.

## Integration và acceptance

- Build/test ở DemoScene trước; feature prefab/catalog sau đó được promote sang MapNhat.
- Quick bar: assign/select/swap inventory/quantity zero/save-load đều giữ đúng binding.
- Plant: sai item, hết seed, ngoài tầm hoặc plot không Empty đều không mutate gì.
- Growth: stage và Mature đúng qua save/load và thời gian app đóng.
- Harvest: inventory đầy giữ nguyên crop; thành công grant đúng một lần và plot Empty.
- Click plant/harvest không phát attack animation/event và không hao stamina.
- Duplicate/empty `plotId`, `cropId`, missing seed/harvest item/stage sprite là validation error.
