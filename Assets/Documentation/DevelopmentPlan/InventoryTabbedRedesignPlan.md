# Inventory Tabbed Redesign Plan

Demo fixture 2026-10-14: DemoScene InventorySeeder references DemoInventorySeed (18 item types:
one sword, ten each material/orb/consumable/seed/bait). DemoScene PlayerSpawnReadinessSource explicitly
passes the fixture to the live InventoryManager's seeder during Development readiness, when empty.
This replaces the broken scene-seeder Start hook: Bootstrap owns the surviving manager and destroys
DemoScene's duplicate manager/seeder. A persistent attempt guard prevents repeat seeding on travel;
LoadedGame never receives this seed. Production seeder configuration and saves are unchanged.
Owner will verify by entering Play Mode; no automatic Play Mode transition was performed.

2026-10-14 D-117 supersedes old categories: All / Equipment / Resources / Consumption / Others.
Consumption groups Potion/Food/Drink; Others groups Orb/Seed/Fishing Bait. Four Quick Bar slots
require Used Items; Active requests use, Inactive selects for farming/fishing. Teammate owns effects.

Owner revision 2026-10-07: dùng nguyên ảnh khung `inventory_complete_frame_approved.png`
do owner cung cấp thay cho khung trang trí ghép riêng. Giữ tỷ lệ ảnh; text/icon/nút X và vùng
drag/drop là overlay riêng. Yêu cầu này supersede phần tách khung trang trí bên dưới.

## 1. Mục tiêu

Trạng thái 2026-10-07: owner đã duyệt D-114 và yêu cầu tích hợp prefab/kiểm tra tại 1920x1080.
Đã triển khai category metadata, bookmark controller, Quick Assign mirror, builder module và source
prefab. Visual QA đang kiểm tra screenshot; trạng thái test/verification cuối cùng ghi trong
`InventoryTabbedImplementationReport.md`.

Độ lại bố cục và mở rộng presentation của `Assets/Prefabs/InventoryUIController.prefab`, nhưng giữ
nguyên Style Art Inventory v5 hiện tại làm chuẩn tuyệt đối, đồng thời bổ sung:

- Năm bookmark lọc: `ALL`, `WEAPON`, `RESOURCES`, `FOOD`, `POTION`.
- Cụm `QUICK ASSIGN` bốn ô đánh số `1–4` ở chân cửa sổ để kéo item từ Inventory vào nhanh.
- Khung ngoài, khung grid, bookmark, slot, trạng thái tương tác và icon category đồng bộ cùng một
  pixel-art language.
- Giữ nguyên character preview, bảy equipment socket, stats runtime, Gold runtime, tooltip,
  drag/drop, discard confirmation và menu lifecycle hiện có.

Ảnh mẫu người dùng cung cấp chỉ là reference về bố cục bookmark + quick assignment. Không copy palette
hoặc material của ảnh mẫu, và cũng không diễn giải yêu cầu là đổi Inventory sang một style tối mới.
Bản mới phải khớp trực tiếp Inventory v5 hiện tại về tông vàng cổ–nâu ấm, độ sáng, nét viền, sapphire
góc, pixel density và chất liệu khung; text động tiếp tục dùng Digital Disco.

## 2. Kết quả audit hiện trạng

- Source prefab là `Assets/Prefabs/InventoryUIController.prefab`; DemoScene và MapNhat phải tiếp tục
  dùng prefab connection, không tạo scene-only override.
- Board v5 hiện là landscape `600x335`, bên trái có preview/equipment/stats, bên phải có grid 6x5 và
  footer Gold. Grid có ScrollRect và Inventory thực tế có thể lớn hơn vùng 30 slot nhìn thấy.
- `InventoryUI` hiện bind cell trực tiếp theo index/page; chưa có filter/category state.
- `ItemSO.type` chỉ có `Weapon`, `Armor`, `Material`, `Consumable`. Không đủ dữ liệu để tách Food và
  Potion một cách đúng đắn.
- Quick bar hiện có tám slot, assignment lưu stable `itemId`, hỗ trợ kéo từ `InventorySlotUI`, đổi vị
  trí giữa quick slot, chọn bằng click/phím số và giữ assignment khi quantity về 0.
- D-028 đã chốt tám quick slot. Vì vậy cụm bốn ô mới trong Inventory không được âm thầm thay quick bar
  thành bốn slot hoặc tạo một save contract thứ hai.

## 3. Quyết định đề xuất cần owner duyệt

### 3.1 Quick Assign trong Inventory

Phương án đề xuất: bốn ô `1–4` là một presentation thứ hai của `QuickBarManager` index `0–3`.

- Kéo item vào ô cập nhật đúng assignment hiện có.
- Kéo giữa các ô dùng contract hiện có.
- Right-click xóa assignment; quantity đọc tổng từ Inventory.
- Không trừ item khi gán; item chỉ được tiêu thụ bởi gameplay handler tương ứng.
- Slot 5–8 vẫn tồn tại trên HUD gameplay và trong save, không bị xóa hay migration.

Nếu owner muốn quick bar toàn game chỉ còn bốn slot, đây là thay đổi gameplay/save/input riêng: phải
re-open D-028, cập nhật `QuickBarSaveData.SlotCount`, migration dữ liệu cũ, HUD, input 5–8 và test. Không
gộp thay đổi đó vào lượt reskin.

### 3.2 Phân loại bookmark

Thêm metadata presentation/data-authoring vào `ItemSO`, tách khỏi `ItemType` đang phục vụ contract cũ:

```text
InventoryCategoryFlags
├─ Weapon
├─ Resource
├─ Food
└─ Potion
```

- `ALL` không cần flag và luôn hiển thị toàn bộ capacity theo thứ tự thật.
- Một item có thể có nhiều flag nếu content cần thiết, nhưng authoring mặc định chỉ chọn một category.
- `EquipmentItemSO` slot Weapon tự validate có `Weapon`; armor không nằm trong năm tab mới và vẫn thấy
  ở `ALL`.
- Material/resource node drops author `Resource`.
- Food và Potion vẫn có thể giữ `ItemType.Consumable` để không phá logic đang dùng type cũ.
- `SeedItemSO` và `FishingBaitItemSO` đề xuất thuộc `Resource` trong scope năm tab này. Nếu muốn một tab
  `TOOLS/UTILITY` riêng thì phải đổi yêu cầu trước khi dựng art.
- Metadata nằm trên definition asset nên không đổi save schema/version; stable `itemId` không đổi.

Không suy category từ folder, class name, display name hoặc icon.

## 4. Hướng thiết kế UI mới

### 4.1 Bố cục tại Canvas 1920x1080

Giữ cửa sổ landscape và tăng có kiểm soát từ `600x335` lên khoảng `680x400` trên Canvas reference
`800x600`, sau khi prototype kiểm tra không che vùng UI quan trọng.

```text
InventoryWindow
├─ OuterBoard + OpaqueBackdrop
├─ Header: title runtime/wordmark + CloseButton
├─ CharacterColumn
│  ├─ CharacterPreview + 7 EquipmentSlotUI
│  └─ StatsWell (2 x 3 runtime stats)
└─ ItemsColumn
   ├─ CategoryBookmarks (All / Weapon / Resources / Food / Potion)
   ├─ GridFrame
   │  └─ ScrollRect / Viewport / FilteredGrid (6 x 5 visible)
   └─ Footer
      ├─ QuickAssign 1–4
      └─ Gold badge + TMP value
```

Bookmark đặt ngay trên grid, dùng silhouette/icon nhỏ và TMP label thật; không bake chữ vào texture.
Selected bookmark dùng sapphire inset + gold highlight. Hover/pressed/disabled không đổi hình học.

Quick Assign đặt trong footer, tách thị giác khỏi item grid bằng divider vàng mảnh. Mỗi ô dùng cùng
base geometry với quick bar hiện có nhưng giảm ornament; số `1–4`, quantity, icon item và selected state
là object runtime riêng.

### 4.2 Art direction Pixellab

Tạo một contact sheet/concept board trước, sau đó gen từng module trên nền solid magenta hoặc alpha rõ
ràng để xử lý/slice ổn định. Mọi module phải cùng pixel density, hướng sáng trên-trái và độ dày outline.

Palette mục tiêu phải lấy mẫu trực tiếp từ Inventory v5 hiện tại:

- Nâu ấm/nâu chocolate: nền panel và khung gỗ đúng độ sáng Inventory v5.
- Vàng cổ sáng: vùng khung chính và đường phân vùng, không hạ toàn bộ UI thành charcoal tối.
- Warm taupe: lòng slot và vùng kéo-thả.
- Antique gold: viền 1–2 pixel visual weight.
- Sapphire: selected/focus, tối đa một điểm nhấn lớn mỗi panel.
- Ivory/warm gray: text runtime.

Không được thay đổi hue/contrast tổng thể theo concept mới nếu làm Inventory trông tối hơn hoặc khác
vật liệu so với asset v5 đang dùng.

Không gen item icon, character, Gold amount, số lượng, label tab hoặc số phím vào board.

### 4.3 Asset manifest dự kiến

Lưu source/processed dưới `Assets/Resources/UI/Inventory/DarkInventoryTabbed/`:

Không dùng một bitmap board nguyên khối. Mọi vùng bên trong phải là module riêng để designer kéo thả,
đổi kích thước và để layout responsive bằng RectTransform/Layout Group:

1. `inventory_outer_frame_v1.png` — chỉ outer border rỗng, tâm alpha; dùng 9-slice.
2. `inventory_panel_frame_v1.png` — frame 9-slice dùng độc lập cho preview/stats/grid.
3. `inventory_bookmark_v1.png` — base tab 9-slice/point-filtered.
4. `inventory_bookmark_selected_v1.png` — cùng geometry, selected state.
5. `inventory_quick_slot_v1.png` — một slot base tái sử dụng cho Quick Assign 1–4.
6. `inventory_quick_slot_selected_v1.png` — selection overlay, alpha thật.
7. `inventory_slot_v5.png` — một slot tái sử dụng cho grid/equipment.
8. `inventory_gold_well_v1.png` — currency well không có số/text/icon bake.
9. `inventory_close_v2.png` — Button X độc lập neo góc trên phải.
10. `category_icons_v1.png` hoặc năm sprite rời — All, Weapon, Resource, Food, Potion.
11. `equipment_empty_hints_v2.png` — chỉ gen lại nếu slot v5 làm silhouette cũ lệch style.

Mỗi PNG cần import `Sprite (2D and UI)`, Point filter, no mipmap, alpha transparency; frame co giãn cần
border 9-slice được author rõ. Giữ prompt, raw output và processing metadata để tái tạo được asset.

## 5. Kiến trúc và hành vi filter

Tạo một controller presentation nhỏ (ví dụ `InventoryCategoryTabsUI`) giữ category đang chọn và yêu cầu
`InventoryUI` refresh. Không đưa filter state vào save; mỗi lần mở Inventory mặc định `ALL`.

- `ALL`: bind đúng toàn bộ `InventoryManager.Slots`, giữ semantics swap/drag hiện tại.
- Category khác: lấy các `InventorySlot` không rỗng có flag phù hợp, giữ nguyên thứ tự slot gốc và bind
  compact vào grid; phần cell dư là visual filler, không phải slot dữ liệu mới.
- Kéo từ kết quả lọc vào equipment/quick assign giữ nguyên reference tới `InventorySlot` thật.
- Kéo vào một cell item thật có thể swap như hiện tại.
- Cell filler trong filtered view không nhận drop để tránh vô tình chuyển item sang một index ẩn.
- Khi inventory thay đổi, filter hiện tại refresh; nếu item không còn match thì biến mất khỏi view sau
  transaction.
- Tooltip, discard ngoài window và `MarkDropHandled()` tiếp tục hoạt động.

Không sort/mutate `InventoryManager.Slots` chỉ để trình bày category.

## 6. Các phase triển khai

### Phase A — Chốt UX/data contract

- Owner duyệt quick slots 1–4 là mirror của quick bar index 0–3.
- Owner duyệt mapping Seed/Bait vào `Resources` và Armor chỉ xuất hiện trong `ALL`.
- Ghi quyết định Accepted vào `DecisionRegister.md` trước khi code/gen production asset.

Gate: không còn ambiguity với D-028 và category mapping.

### Phase B — Wireframe và concept art

- Chụp baseline prefab tại 1920x1080.
- Dựng wireframe đúng hierarchy runtime, kiểm tra kích thước grid, tab, footer và tooltip.
- Gen 2–3 concept board Pixellab cùng một prompt/style seed; owner chọn một direction.
- Gen module asset theo manifest, xử lý alpha/slice và import settings.

Gate: concept được owner duyệt; không có text/data runtime bị bake vào board.

### Phase C — Data-driven bookmark

- Thêm category flags và validation/authoring helper.
- Gán category cho toàn bộ item catalog hiện có; validator báo item chưa phân loại hoặc mapping mâu thuẫn.
- Thêm EditMode tests cho mapping Weapon/Resource/Food/Potion và subclass Seed/Bait.

Gate: catalog sạch warning bắt buộc; stable IDs và save schema không đổi.

### Phase D — Prefab/runtime integration

- Thêm tab buttons, filter controller và filtered binding vào source prefab.
- Thêm bốn `QuickBarSlotUI` mirror index 0–3 trong footer; tái sử dụng behavior hiện có thay vì tạo
  manager/save data mới.
- Thay board/frame/slot/close/currency presentation và giữ nguyên callback/controller ownership.
- Cập nhật builder editor hiện có hoặc thay bằng builder mới idempotent; chạy lại không nhân đôi object.

Gate: prefab không missing reference; DemoScene và MapNhat giữ prefab connection.

### Phase E — Verification và promotion

- EditMode: category mapping, filtered order, không mutate Inventory slots.
- PlayMode: tab switching, scroll >30 slot, tooltip, equip, swap, discard, Inventory→Quick 1–4,
  Quick→Quick, right-click clear, quantity 0, save/load assignment tám slot.
- Kiểm tra keyboard/gamepad focus và pointer states; close/back/I/Esc đúng state machine.
- Visual QA tại 1920x1080 và ít nhất 16:9 nhỏ hơn; không overflow, không che tooltip, Console sạch.
- Test trong DemoScene trước, sau đó smoke test prefab source ở MapNhat.
- Cập nhật tài liệu visual direction, Decision Register và implementation report/handoff.

Gate hoàn tất: code + prefab + asset + tests + screenshots + documentation đồng bộ.

## 7. Definition of Done

- Năm bookmark lọc đúng bằng metadata, không hard-code tên/folder.
- `ALL` vẫn truy cập toàn bộ item, bao gồm Armor/Quest/Other chưa có bookmark riêng.
- Bốn ô trong Inventory phản chiếu chính xác quick bar 1–4; slot 5–8 và save cũ không mất.
- Drag/drop/equip/discard/tooltip/Gold/character preview/stats không regression.
- Art mới đồng bộ D-041, point-filtered, alpha/slice đúng; không có text/data động bake vào bitmap.
- Source prefab là nguồn chuẩn cho DemoScene và MapNhat.
- Automated tests pass và manual Play Mode QA có ảnh before/after ở 1920x1080.

## 8. Ngoài phạm vi lượt redesign

- Giảm quick bar toàn game từ tám xuống bốn slot.
- Thay đổi input binding gameplay hoặc cơ chế consume item.
- Tạo item icon/content mới cho Food/Potion/Seed/Bait.
- Thay save schema, item quantity contract hoặc Inventory capacity.
- Redesign Character Popup, Shop/Crafting hay BottomHUD ngoài phần cần giữ visual compatibility.
