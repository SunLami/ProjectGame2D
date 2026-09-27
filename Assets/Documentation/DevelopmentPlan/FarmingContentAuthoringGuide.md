# Farming Content Authoring Guide

Tài liệu này hướng dẫn thêm một loại hạt giống và cây trồng mới mà không sửa runtime manager,
`FarmPlot` hoặc scene. Ví dụ xuyên suốt dùng Strawberry.

## Tổng quan dependency

```text
Harvest ItemSO
      ↑
CropDefinition ← SeedItemSO
      ↑
FarmingCatalog

SeedItemSO → ItemDatabase (chỉ khi muốn cấp seed lúc New Game)
```

Mọi plot hiện có đều có thể trồng mọi `SeedItemSO` hợp lệ trong `FarmingCatalog`; không tạo prefab
plot hoặc manager riêng cho từng crop.

## 1. Chuẩn bị sprite

Chuẩn bị tối thiểu:

- Icon hạt giống.
- Icon sản phẩm thu hoạch.
- Tối thiểu hai sprite cây: vừa gieo và trưởng thành. Khuyến nghị bốn stage để chuyển cảnh rõ hơn.

Import Settings khuyến nghị cho pixel art:

- `Texture Type`: `Sprite (2D and UI)`.
- `Sprite Mode`: `Single` hoặc `Multiple` tùy source texture.
- `Filter Mode`: `Point`.
- `Compression`: `None`.
- `Pixels Per Unit`: đồng nhất với sprite cây đang dùng trong project.

## 2. Tạo item sản phẩm thu hoạch

Trong `Assets/Resources/Items/Farming`, chọn:

```text
Create → Scriptable Objects → Item
```

Đặt tên asset `Crop.Strawberry` và cấu hình:

| Field | Giá trị ví dụ |
|---|---|
| `Item Id` | `item.crop.strawberry` |
| `Item Name` | `Strawberry` |
| `Description` | `Fresh strawberry.` |
| `Icon` | Sprite quả Strawberry |
| `Type` | `Material` |
| `Is Stackable` | `true` |
| `Max Stack Size` | `99` |

Item cần nằm dưới `Resources/Items` để `ResourcesItemResolver` có thể khôi phục Inventory và Quickbar
từ stable `itemId`.

## 3. Tạo CropDefinition

Trong `Assets/Game/Farming/Definitions`, chọn:

```text
Create → Game → Farming → Crop Definition
```

Đặt tên asset `Crop.Strawberry` và cấu hình:

| Field | Giá trị ví dụ |
|---|---|
| `Crop Id` | `crop.strawberry` |
| `Harvest Item` | `Crop.Strawberry` vừa tạo |
| `Minimum Harvest Quantity` | `1` |
| `Maximum Harvest Quantity` | `3` |

Ví dụ mảng `Stages`:

| Element | Ý nghĩa | Duration Seconds |
|---|---|---:|
| 0 | Vừa gieo | 10 |
| 1 | Cây non | 15 |
| 2 | Cây lớn | 20 |
| 3 | Trưởng thành | 0.1 |

Mỗi element phải có sprite. Thời gian trưởng thành là tổng duration của mọi stage trước stage cuối;
ví dụ trên là `10 + 15 + 20 = 45` giây. Duration của stage cuối không làm cây trưởng thành thêm lần nữa.

## 4. Tạo SeedItemSO

Trong `Assets/Resources/Items/Farming`, chọn:

```text
Create → Game → Farming → Seed Item
```

Đặt tên asset `Seed.Strawberry` và cấu hình:

| Field | Giá trị ví dụ |
|---|---|
| `Item Id` | `item.seed.strawberry` |
| `Item Name` | `Strawberry Seeds` |
| `Description` | `Plant to grow Strawberry.` |
| `Icon` | Icon hạt Strawberry |
| `Type` | `Material` |
| `Is Stackable` | `true` |
| `Max Stack Size` | `99` |
| `Crop` | `Crop.Strawberry` definition |

Nếu `Crop` bị bỏ trống, seed vẫn có thể xuất hiện trong Inventory nhưng không thể gieo.

## 5. Đăng ký crop trong FarmingCatalog

Mở `Assets/Game/Farming/Definitions/FarmingCatalog.asset`, tăng kích thước mảng `Crops` và thêm
`Crop.Strawberry`:

```text
Crops
├── Crop.Carrot
├── Crop.Eggplant
└── Crop.Strawberry
```

Catalog bắt buộc chứa crop mới. Nếu thiếu, `FarmPlot` từ chối gieo để tránh tạo runtime state không
thể resolve khi save/load.

## 6. Chọn nguồn cấp seed

### Cấp cho New Game

Mở `Assets/Resources/Items/ItemDatabase.asset`, thêm entry:

```text
Item: Seed.Strawberry
Amount: 12
```

Thay đổi chỉ áp dụng cho New Game tạo sau khi asset được cập nhật. Save hiện có không tự nhận seed.

### Shop, crafting hoặc quest

Nếu seed được mua, craft hoặc nhận thưởng thì không cần thêm vào starting `ItemDatabase`. Thêm
`Seed.Strawberry` vào definition/catalog tương ứng của domain cấp item.

## 7. Stable ID convention

Dùng chữ thường, phân tách bằng dấu chấm và không đổi ID sau khi đã có production save:

```text
crop.strawberry
item.seed.strawberry
item.crop.strawberry
```

Không dùng filename, display name, tọa độ hoặc array index làm save identity. Khi duplicate asset,
phải đổi ID ngay để tránh validator báo duplicate.

## 8. Validation

Chạy:

```text
Tools → Project Game → Validate Content
```

Crop mới phải không có các lỗi sau:

- `itemId` hoặc `cropId` rỗng, sai format hoặc trùng.
- `SeedItemSO.Crop` bị thiếu.
- `CropDefinition.HarvestItem` bị thiếu.
- Ít hơn hai growth stages hoặc stage thiếu sprite.
- Crop không nằm trong bất kỳ `FarmingCatalog` nào.

## 9. Play Mode acceptance test

1. Tạo New Game mới nếu seed được thêm vào starting `ItemDatabase`.
2. Nhấn `I`, xác nhận Inventory có Strawberry Seeds.
3. Kéo seed xuống Quickbar và chọn bằng click hoặc phím `1–8`.
4. Tới gần plot trong phạm vi 2.5 units, hover và click gieo.
5. Xác nhận đúng một seed bị trừ và stage đầu xuất hiện.
6. Quan sát các stage; với ví dụ trên cây mature sau khoảng 45 giây.
7. Hover cây mature và click thu hoạch.
8. Xác nhận loot bay về Player, Inventory nhận 1–3 Strawberry và plot trở về Empty.
9. Xác nhận gieo/thu hoạch không chạy Attack animation và không tiêu attack stamina.
10. Save sau khi gieo, thoát hơn thời gian trưởng thành rồi Continue; cây phải mature theo UTC.

## 10. Lưu ý về Farming authoring menu

Menu sau đang dựng content mẫu Carrot/Eggplant và ghi lại mảng catalog:

```text
Tools → Project Game → Farming → Build And Install Farming
```

Không chạy lại menu sau khi thêm crop thủ công, nếu chưa cập nhật `FarmingAuthoring.cs`; nếu không,
crop mới có thể bị loại khỏi `FarmingCatalog`. Nếu project cần rebuild thường xuyên, bổ sung crop vào
authoring script hoặc refactor tool để chỉ tạo default khi asset chưa tồn tại và không ghi đè catalog
do content designer quản lý.

## Checklist ngắn cho mỗi crop mới

- [ ] Harvest `ItemSO` nằm dưới `Resources/Items` và có stable `itemId`.
- [ ] `CropDefinition` có stable `cropId`, harvest item, quantity và sprites hợp lệ.
- [ ] `SeedItemSO` nằm dưới `Resources/Items`, có icon và tham chiếu đúng crop.
- [ ] Crop đã được thêm vào `FarmingCatalog`.
- [ ] Seed có nguồn cấp hợp lệ: New Game, shop, crafting hoặc quest.
- [ ] Content Validation không có lỗi liên quan.
- [ ] Plant/growth/harvest/save-load/non-combat acceptance test đều pass.
