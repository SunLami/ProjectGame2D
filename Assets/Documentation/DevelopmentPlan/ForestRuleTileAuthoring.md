# Forest RuleTile Authoring

## Nguồn dữ liệu

- Mẫu chuẩn: `Assets/Tiles/Tilesets/Forest Top-Down Tileset Pixel Art/Forest.tmx`.
- Sprite nguồn giữ Pixels Per Unit bằng `16`, khớp tile size `16x16` của TMX.
- TMX không có Wang Set, Terrain metadata hoặc custom property mô tả autotile. Vì vậy quy tắc được học từ cách các tile xuất hiện trong từng layer mẫu, không phải metadata do Tiled cung cấp.

## Asset được sinh

Thư mục đích: `Assets/Tiles/RuleTiles/Forest`.

- RuleTile nối địa hình: `Forest_Water`, `Forest_Ground`, `Forest_MainSpace`, `Forest_ElevatedSpace`, `Forest_Lianas`.
- RuleTile biến thể trang trí: `Forest_GroundSpots`, `Forest_RockSpots`, `Forest_WaterLilies`, `Forest_GrassElements`, `Forest_Reeds`.
- `Objects`, `stairs` và các cụm kiến trúc không được trộn ngẫu nhiên hoặc ép thành autotile, vì hình dạng của chúng phụ thuộc bố cục nhiều ô. Chúng tiếp tục được đặt từ TMX/palette như tile thường.

## Cách học quy tắc

Với mỗi ô có tile trong một layer địa hình, builder đọc trạng thái có/không có tile của tám ô lân cận. Mỗi mẫu tám hướng trở thành một `RuleTile.TilingRule`; các sprite từng xuất hiện với cùng mẫu được dùng làm biến thể ngẫu nhiên. Sprite xuất hiện nhiều nhất là fallback.

Các GID có cờ flip trong layer địa hình không được dùng làm sprite mẫu để tránh học sai hướng. Chúng vẫn tham gia xác định vùng có tile. Với layer trang trí, cờ flip được chuẩn hóa về sprite nguồn vì RuleTile ngẫu nhiên không thể giữ transform riêng của từng lần xuất hiện trong TMX.

## Rebuild và kiểm tra

1. Chỉnh sửa rồi lưu `Forest.tmx` trong Tiled.
2. Trong Unity chạy `Tools > Project Game > Tiles > Rebuild Forest RuleTiles`.
3. Chạy `Tools > Project Game > Tiles > Validate Forest RuleTiles`.
4. Kiểm tra Console: builder báo số rule, sprite, mẫu flip bị bỏ qua hoặc chuẩn hóa.
5. Sơn thử các góc trong/ngoài, cạnh thẳng, vùng kín và ô đơn trên Tilemap trước khi đưa vào DemoScene.

Builder cập nhật asset hiện có thay vì xóa và tạo lại, nên GUID và các reference đang dùng được giữ nguyên.
