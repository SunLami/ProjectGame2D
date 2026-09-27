# Dark Light Fantasy UI Style Guide

## Mục tiêu

Inventory v5 là visual reference chuẩn cho gameplay UI. Tài liệu này định nghĩa art direction dùng
chung khi xây lại từng UI; không tạo scene Style Guide và không chuyển ownership khỏi controller,
GameState hoặc gameplay service hiện có.

## Visual language

- Nền chính: charcoal gần đen, đủ opaque để nội dung không lẫn với world.
- Vật liệu phụ: walnut tối, warm taupe cho lòng slot và antique gold cho viền.
- Sapphire chỉ là accent ở title, corner hoặc focus; không lặp gem dày quanh mọi object.
- Viền mảnh, góc gọn, ornament tiết chế. Nội dung phải nổi hơn khung.
- Parchment sáng chỉ dùng khi yêu cầu đọc văn bản dài; gameplay popup mặc định theo nền tối Inventory.

## Typography

- Font chuẩn: `Digital Disco SDF v3`.
- Title: 30 px; section header: 22 px; label/value: 18 px; helper: 15 px tại 1920×1080.
- Title và section dùng antique gold; label/value dùng ivory; helper dùng warm gray.
- Text động luôn là TMP object thật, không bake vào sprite.
- Value căn phải; label căn trái; text không được mở rộng qua safe area để chữa cháy.

## Spacing và safe area

- Thang spacing chuẩn: 4 / 8 / 12 / 16 px.
- Nội dung cách viền panel tối thiểu 24 px tại 1920×1080.
- Icon cách label tối thiểu 8 px; label cách value tối thiểu 12 px.
- Các hàng và cột dùng trục cố định; không căn theo vùng alpha nhìn thấy của sprite.
- Kiểm tra cuối bắt buộc ở Game View 1920×1080.

## Component rules

- Panel: charcoal/walnut, gold thin border, tối đa một sapphire accent cho mỗi panel.
- Slot: warm taupe, gold thin border, icon nằm trong 76% safe area.
- Primary button: sapphire/dark blue; Danger button: muted red; cùng chiều cao và hình học.
- Close button: asset X xanh/vàng ở safe inset góc trên phải, có Button và vùng bấm thật.
- Tooltip: charcoal bán trong suốt, gold thin border, không ornament lớn; theo cursor và clamp Canvas.
- Currency: chỉ sinh ô cho currency có dữ liệu; hiện tại chỉ Gold, không giữ placeholder trống.
- Divider/scrollbar: mảnh, độ tương phản thấp hơn title và action chính.
- Shop/Crafting board chỉ bake khung ngoài và các panel cấu trúc. Inventory slot, tab, action button,
  quantity button, text và icon phải là object runtime/prefab riêng; không vẽ trùng chúng vào board.
- Commerce action/tab dùng sprite pixel-art riêng (cạnh bậc thang, point filtering), không dùng art
  vector/HD. Inventory rút gọn tái sử dụng `InventorySlotUI`, capacity từ
  `InventoryUIController/GridSlot` và cùng hierarchy ScrollRect/Viewport/GridSlot như Inventory chính;
  6×5 là vùng nhìn, các hàng còn lại cuộn dọc bằng chuột.
- `ShopWindow.prefab` và `CraftingWindow.prefab` là prefab authoring độc lập. `GameplayUIRoot` chỉ
  chứa nested prefab instance của hai asset này; designer chỉnh RectTransform/Text/Icon/Button trực
  tiếp trong Prefab Mode và thay đổi phải được phản ánh vào runtime mà không dựng lại board.
- Dialogue dùng một frame pixel-art tối, bán trong suốt, neo bottom-center; tên NPC nằm trong
  nameplate góc phải dưới. Nội dung thoại, tên và lựa chọn là TMP runtime, không bake vào frame.
  Khi node có lựa chọn, body nằm trên tối đa 4 hàng choice; khi không có lựa chọn body tự mở rộng
  xuống toàn bộ safe area. Choice bình thường chỉ là text có hit area; viền vàng mảnh chỉ hiện khi
  hover hoặc keyboard/gamepad focus. Text phải auto-size trong giới hạn và ellipsis thay vì tràn khung.

## Interaction states

- Normal, Hover, Pressed, Disabled phải giữ nguyên kích thước và vị trí.
- Hover tăng sáng vừa phải; không đổi sang một asset khác hình học.
- Disabled giảm saturation/alpha nhưng label vẫn đọc được.
- Focus keyboard/gamepad phải nhận diện được mà không chỉ dựa vào màu.

## Quy trình migration

1. Chụp baseline UI ở 1920×1080 và ghi lại hierarchy/callback hiện có.
2. Thay presentation theo tài liệu này, không sửa gameplay logic cùng lượt.
3. Giữ text/icon dữ liệu động thành object thật và xóa phần trang trí bake trùng lặp.
4. Kiểm tra hover, click, close/back, keyboard/gamepad và trạng thái ẩn/hiện.
5. Chụp screenshot sau sửa, kiểm tra overflow, safe area, z-order và Console.
6. Chỉ chuyển sang UI tiếp theo sau khi prefab nguồn và MapNhat cùng đạt acceptance.

## Thứ tự migration đề xuất

1. Character Popup.
2. Quest Log và Quest Accept.
3. Pause, Settings và Save/Load overlays.
4. Shop và Crafting.
5. Dialogue và Tutorial.
6. PlayerHUD, UnifiedGameplayHUD và QuestTracker.

HUD thường trực làm sau cùng vì cần kiểm tra mật độ thông tin và vùng gameplay không bị che.
