# Phong (Wind/Anemo) Skill VFX — Art Style Guide (chốt 2026-10-03)

Kế thừa `ThuySkillVfxArtStyleGuide.md` (D-072) và `GeoSkillVfxArtStyleGuide.md` (D-075): cùng độ chunky, cùng pipeline kỹ thuật, cùng
quy tắc **mọi hiệu ứng skill phải là animation Pixellab** (không dùng sprite tĩnh xoay/mờ bằng code). Chỉ đổi chất liệu và bảng màu (D-080).

## 1. Công thức prompt chuẩn

Luôn chèn vào prompt `create_1_direction_object`:

> HARD-EDGED chunky pixel art, flat cel-shaded color bands only — pure white, pale mint, jade green, bright white-mint
> highlight — NOT a smooth gradient, NOT airbrushed glow, bold thick black pixel outline, high contrast, no background.

Gió vô hình nên phải gợi bằng hình: **vệt gió cong xoáy, lá nhỏ, lông, hạt bụi, đường kẻ trắng song song**. Mép phải
sắc/nhọn dạng lưỡi liềm, không tròn trơn. Hình khối gió = các dải cong chồng nhau có đầu nhọn kiểu "gió xoáy".

## 2. Animation
- `animate_object` v3, `frame_count` 8 → 9 frame; prompt phải ghi "keeping the same size and position, looping" cho loop.
- Loop ghép **ping-pong** (0..8..1) để không có seam; animation "co/chìm/tan" gen xong thì **đảo ngược** để có "lan/mọc".
- Xem contact sheet trước khi import; loại frame thủng/ngả màu/lệch pattern. Hình "C"/"(" gen ra có thể sai chiều: lật ngang bằng PIL.
- Tải ảnh bằng `curl` (Python `urllib` thiếu chứng chỉ SSL trong môi trường này).

## 3. Pipeline kỹ thuật
Giống các style guide trước: 176×176px, view top-down, sprite sheet 1 hàng ngang, PPU 48, Point filter, Uncompressed, pivot theo loại
(chân đối tượng: 0.15-0.2; vật thể tâm: 0.5). Thư mục asset: `Assets/Resources/VFX/Skills/Wind/`, tên file tiếng Anh. Sinh `.meta` qua
Unity MCP (`execute_code`), cắt `SpriteImportMode.Multiple`.

## 4. Asset đã chốt
| Skill | File | Mô tả |
|---|---|---|
| Skill 1 — Đao Phong (lưỡi) | `WindBlade_Spin.png` | Lưỡi gió trăng khuyết trắng-mint, dòng gió + lá chảy dọc, 16 frame ping-pong |
| Skill 1 — Đao Phong (nổ) | `WindImpact_Burst.png` | Nổ vệt gió + lá tỏa ra rồi tan, 7 frame một lần |
| Skill 1 — vệt gió | `WindGustPuff_Dissipate.png` | Xoáy gió nhỏ quay và tan, 9 frame một lần |
| Skill 2 — Cuồng Phong (lốc xoáy) | `WindTornado_Spin.png` | Phễu lốc xoáy trắng-mint, lá xoay quanh, 16 frame ping-pong (Pixen + animate_image) |
| Skill 2 — Cuồng Phong (vòng gió) | `WindRing_Flow.png` | Vòng gió xoắn quanh tâm rỗng, 16 frame ping-pong (Pixen + animate_image) |
| Skill 3 — Trận Gió (cung gió) | `WindGust_Arc.png` | Cung gió ")" vệt song song trắng-mint + lá, 16 frame ping-pong (cắt từ ảnh 2 cung) |
| Skill 4 — Đại Bàng Gió | `WindRoc_Flap.png` | Đại bàng gió trắng-mint cánh giương rộng nhìn sang phải, vỗ cánh, 9 frame lặp |
| Skill 4 — lông gió | `WindFeather_Flutter.png` | Chiếc lông mint đung đưa trong gió, 9 frame lặp (xoay quanh người, rơi mưa, bắn hai bên) |
| Skill 4 — lá bay | `WindLeaf_Tumble.png` | Chiếc lá xanh lật xoay trong gió, 9 frame lặp (lá bay xuyên màn hình) |
| Skill 4 — vòng đánh dấu hạ cánh | `WindMarker_Pulse.png` | Vòng trắng mảnh + mũi tên hướng tâm + vòng xanh ngọc nhấp nháy, 12 frame ping-pong (bỏ frame tô đặc) |
| Skill 4 — sóng xung kích | `WindShockwave_Expand.png` | Vòng vệt gió trắng-mint + lá tỏa ra, 7 frame (đảo ngược từ "co về tâm"; đã lọc đĩa xám giữa) |

## 5. Chi phí gen Pixellab (đo 2026-10-03)
- Gói Tier 1: 2000 generation/chu kỳ (cấp lại 27/10). `create_1_direction_object` 176px = 20 gen; `animate_object` v3 176px 8 frame ≈ 4 gen;
  `create_image_pixen` = 1 gen; `animate_image` 176px 8 frame = 4 gen. **Rẻ nhất: Pixen + `animate_image` ≈ 5 gen/asset** (so với ~24).
  Bộ Wind S1 gen bằng cách object cũ (3 asset); từ Skill 2 thử Pixen + `animate_image` và kiểm tra chất lượng trước khi dùng hàng loạt.
- **Quy trình rẻ đã kiểm chứng (Skill 2):** `create_image_pixen` (176px, `no_background=true`, 1 gen) → `animate_image` với `first_frame_url`
  là link `download` của ảnh (8 frame, 4 gen) → tải từng frame bằng `curl .../mcp/images/<job_id>/download?index=N` (không cần xem ảnh inline,
  rẻ token) → kiểm tra frame bằng số điểm ảnh khác 0 và độ khác biệt giữa frame (PIL) thay vì nhìn contact sheet.
- **Bài học (Skill 3):** prompt "crescent/wave" cho Pixen ra hình xoáy cuộn; prompt "closing parenthesis symbol ')' ... no spiral, no curl" cho ra 2 cung "( )" —
  animate cả ảnh rồi cắt lấy một cung bằng PIL (không tốn thêm gen) vì chỉ truyền được ảnh bằng URL của Pixellab, không sửa ảnh trước khi animate được.
- **Bài học (Pixen vòng/ring):** prompt "hollow ring / empty center" vẫn cho Pixen tô đĩa xám ở giữa. Cách xử lý không tốn gen: lọc màu xám trung tính (độ bão hòa < 0.16, value 0.22-0.62) thành trong suốt ở mọi frame — vệt gió trắng-mint (value cao) và viền đen (value thấp) được giữ lại. Kiểm tra bằng alpha ở tâm = 0.
- **Quy tắc luồng cast ultimate:** nhân vật/vật triệu hồi phải có cảnh vào (bay vào từ ngoài khung hình, mờ dần) thay vì xuất hiện đột ngột; hướng sprite phải theo hướng đi; bỏ mọi effect không có nghĩa vật lý/ý đồ; mọi effect chính (nhất là cú kết) phải nằm trong khung camera.

