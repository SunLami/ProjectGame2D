# Fishing System

Tài liệu này là source of truth cho cơ chế câu cá đầu tiên trong `MapNhat`.

## Quyết định gameplay

- Click trái vào `FishingSpotInteractable` trong tầm để bắt đầu; không tự chạy Player tới điểm câu.
- Trước khi bắt đầu phải có ít nhất một ô Inventory trống. Mỗi con cá chiếm đúng một ô vì cân nặng
  và giá trị là dữ liệu riêng của instance.
- Giai đoạn chờ cá cắn dùng `GameState.FishingWaiting`: world vẫn chạy nhưng gameplay input bị khóa.
- Khi dấu `!` xuất hiện, click trái trong hook window để vào minigame. Bỏ lỡ sẽ quay lại chờ cá khác,
  không kết thúc session.
- Minigame dùng `GameState.FishingMinigame`: world pause qua policy của `GameStateManager`, UI không
  sửa trực tiếp `Time.timeScale`.
- Giữ chuột làm catch zone đi lên, thả chuột làm nó đi xuống. Chạm icon cá tăng progress; mất tiếp xúc
  làm progress giảm. Progress đầy trước khi hết giờ là thành công; hết giờ là thất bại.
- `Escape` hủy session ở mọi giai đoạn và trả về state trước đó.

## Data và runtime boundary

`FishDefinitionSO : ItemSO` là definition dùng chung, được resolve qua stable `itemId` như item khác:

- Icon dùng field `ItemSO.icon` để Inventory hiển thị tự động.
- `minimumWeightGrams`, `maximumWeightGrams` định nghĩa khoảng cân nặng.
- `pricePerKilogram` định nghĩa giá một kg.
- `FishId` chính là stable `itemId`; tên asset/display name không phải identity.

`FishInstanceData` là runtime/save state riêng của một con cá:

- `instanceId`: GUID ổn định của con cá đã bắt.
- `weightGrams`: số nguyên gram để tránh sai số float khi save và tính tiền.
- Tổng giá trị = `round(pricePerKilogram * weightGrams / 1000)`.

`FishingSpotDefinition` là definition của điểm câu: weighted fish table, thời gian chờ/hook/minigame,
chuyển động cá, catch zone và tốc độ tăng/giảm progress. `FishingMinigameController` chỉ giữ state
session tạm thời; không ghi kết quả vào definition.

## Inventory và persistence

- Cá chỉ được thêm qua `InventoryManager.TryAddFish`; generic `AddItem`/batch từ chối
  `FishDefinitionSO` để không làm mất metadata instance.
- `InventorySaveData.SlotData.fish` là payload tùy chọn. Item thường để null; fish hợp lệ phải có
  `instanceId` và `weightGrams > 0`.
- Fish instance payload được giới thiệu ở version 7; save schema toàn game hiện là version 9.
  Migration V6→V7 giữ nguyên inventory cũ và bổ sung payload nullable; các migration V7→V9 của
  quick bar/farming là additive và không thay đổi fish payload;
  save cũ không sinh cá giả.
- Restore cá luôn clamp cân nặng theo definition hiện tại, giữ nguyên `instanceId`, ép quantity = 1.
  Fish payload hỏng bị bỏ qua kèm warning thay vì tạo item nửa hợp lệ.

## Scene và prefab integration

- Reusable prefab: `Assets/Prefabs/Fishing/FishingFeature.prefab`.
- River spot definition: `Assets/Game/Fishing/Definitions/FishingSpot.River.asset`.
- Fish definitions: `Assets/Resources/Items/Fish/` để `ResourcesItemResolver` resolve khi load save.
- `MapNhat/Wooden_FishingRod` có trigger collider và `FishingSpotInteractable`, tham chiếu controller
  của `FishingFeature` trong scene.
- Rebuild/install bằng menu `Tools/Project Game/Fishing/Build And Install MapNhat Fishing`.

## Authoring cá mới

1. Tạo `FishDefinitionSO` dưới `Assets/Resources/Items/Fish/`.
2. Gán stable `itemId` mới theo dot namespace, display name, icon, khoảng gram và giá/kg.
3. Giữ `isStackable = false`, `maxStackSize = 1`.
4. Thêm asset vào `FishingSpotDefinition.FishTable` với weight lớn hơn 0.
5. Chạy `Tools/Project Game/Validate Content`, EditMode tests và thử happy/fail/missed-hook/full-inventory
   trong scene integration trước khi promote content.

## Acceptance matrix

- Inventory đầy: click spot chỉ hiện thông báo, khóa attack của cùng click và không bắt đầu chờ.
- Miss hook: quay lại Waiting và có bite mới.
- Minigame: world pause; progress tăng/giảm đúng tiếp xúc; timeout fail.
- Success: một fish instance vào đúng một slot, icon đúng, weight/value đúng công thức.
- Save/load: `itemId`, `instanceId`, weight round-trip; save V6 migrate lên V7 không mất slot cũ.
- Thoát/hủy: UI ẩn, coroutine dừng, state trước đó được khôi phục đúng một lần.

