# Thủy Skill VFX — Art Style Guide (chốt 2026-10-02)

Tài liệu này là **công thức chuẩn bắt buộc** khi gen bất kỳ VFX nào cho bộ skill Thủy (và là khung tham
khảo khi mở rộng sang Địa/Phong — xem D-070). Áp dụng cho Skill 1 (Giọt Nước Xoáy — projectile + burst,
đã re-gen đồng bộ 2026-10-02) và Skill 2 (Lốc Xoáy Nước — vùng hiệu ứng, chốt 2026-10-02). Mọi asset
VFX Thủy sau này (Skill 3 — Đợt Sóng, Skill 4 — Lá Chắn Thủy Triều) phải bám đúng công thức này trước
khi tích hợp vào Unity; nếu lệch, phải chạy lại Pixellab cho tới khi khớp — không chấp nhận gen 1 lần
rồi dùng luôn.

## 1. Công thức prompt chuẩn (copy gần nguyên văn khi gen asset mới)

**Phong cách bắt buộc** — luôn chèn các cụm sau vào prompt `create_1_direction_object`:

> HARD-EDGED chunky pixel art, flat cel-shaded color bands only — dark navy, mid blue, cyan, bright
> white-cyan highlight core — NOT a smooth gradient, NOT airbrushed glow, bold thick black pixel
> outline, high contrast, no background.

**Chất liệu nước phải "turbulent" (chốt 2026-10-02, thay thế công thức "flat blob" cũ của Skill 1 v1)**:
mọi hình khối nước (projectile, burst, vùng hiệu ứng) phải có **mép răng cưa/lồi lõm bất quy tắc**
(jagged irregular wave edges), có **bọt trắng/whitecap** ngắt quãng viền ngoài, không được là đường viền
tròn trơn mượt. Cụm bắt buộc:

> jagged irregular wave edges broken up by white foam/whitecap crests — NOT a smooth flat outline.

Nếu hiệu ứng có hình "lốc xoáy"/"hút nước" (khuôn Vòng tròn mục tiêu), bắt buộc có **lỗ tối ở tâm**
(KHÔNG phát sáng ở tâm) để đọc được là nước đang xoáy/hút xuống, chứ không phải vòng tròn phép thuật
phát sáng:

> a DARK hole/funnel at the very center (deep dark blue-black void, NOT bright or glowing) — must look
> like churning turbulent draining water, NOT a smooth magic circle, NOT a geometric spinner or portal.

### Bài học thất bại — không lặp lại

- **Pinwheel/bullseye nhiều dải màu phẳng đồng tâm hoàn hảo** → luôn bị nhận nhầm là "vòng phép
  thuật"/"spinner vô cực", không đọc được là nước. Nguyên nhân: hình học quá đều, không có độ nhiễu/mép
  răng cưa nào gợi chất lỏng.
- **Nhiều vòng ripple mỏng đồng tâm mượt** → đọc được là nước nhưng quá chi tiết/mượt so với phong cách
  chunky pixel art của Skill 1 gốc — nhìn như vector/airbrush, lệch style.
- **Core sáng rực ở tâm cho hiệu ứng lốc xoáy** → đọc thành "cổng dịch chuyển"/năng lượng, không phải
  nước đang hút xuống. Core sáng chỉ dùng cho projectile (mũi đầu) hoặc burst (điểm va chạm), KHÔNG dùng
  cho tâm vòng xoáy nước.
- **`image_to_pixelart` (convert ảnh/video thật sang pixel art)** → cho kết quả lốm đốm/mờ (nhiễu hạt),
  không phải hard-edge cel-shade. KHÔNG dùng cho style này — luôn gen bằng text-prompt
  (`create_1_direction_object`/`animate_object`), kể cả khi có video/ảnh tham khảo ý tưởng (dùng để mô
  tả lại bằng lời, không feed trực tiếp làm ảnh nguồn).

## 2. Animation — tránh giật/lặp khi ghép nhiều đoạn

- Mỗi lệnh `animate_object` (mode v3) chỉ cho tối đa ~16 frame yêu cầu (`frame_count`), trả về
  frame_count+1 frame thật sự khác nhau (gồm frame gốc). **Không cố ép tổng frame của 1 animation dài
  (vd 59 frame) bằng cách lặp/kéo dài một đoạn ngắn** — lặp frame giống hệt nhau nhiều lần tạo cảm giác
  "đứng hình"/giật, và ghép nhiều lần gen độc lập (khác object/khác lần gọi) tạo "seam" lệch style ngay
  tại điểm nối (đã gặp: nối giữa đoạn recede và đoạn dissolve từ 2 object khác nhau ra 2 kiểu vòng xoáy
  khác hẳn nhau).
- Khi cần 1 animation dài hơn 16 frame với chuyển động thật khác nhau xuyên suốt: **ưu tiên dùng chung 1
  object** cho toàn bộ chuỗi (base → animate nhiều lần nối tiếp bằng `animation_group_id` nếu hợp lệ,
  hoặc ghép các đoạn từ CÙNG 1 lần gen bằng kỹ thuật "reverse-frame" — xem bên dưới) thay vì trộn nhiều
  object khác nhau cho từng đoạn.
- **Kỹ thuật reverse-frame** (rẻ, tái dùng tốt): gen 1 animation "recede" (từ trạng thái đỉnh thu nhỏ
  dần về trạng thái nghỉ), rồi dựng chuỗi hiển thị = `reversed(frames[1:])` (đoạn "rise", bỏ frame trùng
  tại điểm nối) + `frames` gốc (đoạn "fall") → có đủ rise-hold-fall mà chỉ tốn 1 lần gen.
- Luôn xem contact sheet (lưới toàn bộ frame, đánh số) trước khi import Unity — kiểm tra: (a) không có
  frame lỗi/khác biệt đột ngột (outlier — đã gặp 1 frame nhỏ bất thường giữa chuỗi, phải loại bỏ), (b)
  không có đoạn "đứng hình" quá dài (nhiều frame liền trông gần như giống hệt nhau), (c) frame đầu tiên
  của object base không dính vệt/mảnh vỡ thừa ngoài silhouette chính (artifact gen lỗi, đã gặp 1 lần —
  phải gen lại base object từ đầu, không sửa tay).

## 3. Pipeline kỹ thuật (giữ nguyên từ §4 `SkillVfxPipeline.md`, nhắc lại các số đo cụ thể)

- Canvas gen: **176×176px**, `view="top-down"`.
- Import Unity: `TextureImporterType.Sprite`, `SpriteImportMode.Multiple`, `FilterMode.Point`,
  `TextureImporterCompression.Uncompressed`, `alphaIsTransparency=true`, `mipmapEnabled=false`,
  `spritePixelsPerUnit=48`. Sprite sheet là 1 hàng ngang các ô 176×176, cắt bằng
  `TextureImporter.spritesheet`, pivot mỗi frame `(0.5, 0.5)`.
- VFX projectile/quạt định hướng: gen **1 hướng** (`create_1_direction_object`, mũi hướng +X), xoay
  bằng `Transform.rotation` theo góc aim thật lúc runtime — KHÔNG bake 8 hướng (D-071). Bắt buộc mô tả
  rõ "nose pointing exactly straight right (0 degrees), symmetric top-to-bottom around the horizontal
  center axis" để tránh lỗi nghiêng khi xoay (đã gặp ở Skill 1 v3, sửa bằng ràng buộc này).
- VFX vùng hiệu ứng tại chỗ (vòng tròn mục tiêu, hào quang): gen **1 hướng**, không cần xoay runtime,
  loop animation tại chỗ trong suốt thời gian tồn tại của vùng hiệu ứng.
- Input ảnh tham khảo (base64) cho Pixellab bị **giới hạn ngầm ~1200-1225 ký tự** trước khi bị cắt cụt
  bởi MCP client — nếu cần truyền ảnh tham khảo, phải resize nhỏ (≤32×32px) và quantize màu (≤5-6 màu)
  trước khi base64-encode, verify độ dài chuỗi trước khi gửi.
- Scale prefab phải khớp bán kính gameplay thật: đo bề ngang pixel thật của frame rộng nhất (quét hàng
  ngang có alpha>0 rộng nhất), quy đổi ra world-unit theo PPU, rồi tính `scale = (2 × bán_kính_thiết_kế) /
  bề_rộng_world_tại_scale_1` — không áng chừng bằng mắt.
- Sorting: VFX mặt đất (vùng hiệu ứng, decal) phải **cùng Sorting Layer với Enemy** (`Default`, không
  phải `Player`) để Y-sort (Transparency Sort Mode = Custom Axis trục Y, đã cấu hình ở
  `GraphicsSettings.asset`) hoạt động đúng. Khi tâm hiệu ứng trùng chính xác toạ độ Y của một entity khác
  (trường hợp thường gặp — player target đúng vị trí enemy), Y-sort hoà nên không có thứ tự rõ ràng; đặt
  `sortingOrder` thấp hẳn (vd `-100`) trong cùng layer để đảm bảo VFX mặt đất luôn vẽ sau nhân vật, không
  phụ thuộc may rủi thứ tự khi Y trùng nhau.

## 4. Asset tham khảo đã chốt (dùng làm style reference khi gen asset mới)

Thư mục asset: `Assets/Resources/VFX/Skills/Water/` (đổi tên từ `Thuy` → `Water` 2026-10-02 — tên thư
mục/file asset dùng tiếng Anh vì game dùng tiếng Anh, chỉ tài liệu giữ tiếng Việt; đã dọn hết file nháp/
phiên bản cũ không còn prefab nào tham chiếu tới, chỉ giữ lại đúng bản cuối đang dùng thật).

| Skill | File | Mô tả |
|---|---|---|
| Skill 1 — Giọt Nước Xoáy (projectile) | `WaterDropletProjectile_Fly.png` | Comet nước bay ngang, mép răng cưa + bọt trắng, core sáng ở mũi |
| Skill 1 — Giọt Nước Xoáy (burst) | `WaterDropletProjectile_Impact.png` | Nổ tia nước toả tròn từ tâm va chạm, tắt dần |
| Skill 2 — Lốc Xoáy Nước (vùng hiệu ứng) | `WaterWhirlpool_Idle.png` | Vòng xoáy nước xoay tại chỗ, lỗ tối ở tâm, sóng lồi lõm + bọt trắng quanh rìa |
| Skill 3 — Tia Nước Xoáy (dải tia liên tục) | `WaterBeam_Flow.png` | Dải nước áp lực chảy ngang, giọt nước/mảnh nước văng rõ ở mép trên dưới (không phải laser trơn), lặp (`SpriteRenderer.Tiled`) theo chiều dài tia |

Mọi asset VFX Thủy tiếp theo (Skill 4) nên dùng 1 trong 4 file trên làm style reference trực tiếp khi
review kết quả gen mới — nếu không giống rõ rệt về: (1) mật độ chi tiết, (2) mép răng cưa + bọt trắng
(phải là nước, không được đọc thành laser/năng lượng trơn), (3) bảng màu 4 tông navy/blue/cyan/
white-cyan, thì coi như chưa đạt, phải gen lại.

## 5. Skill 4 — asset hiệu ứng ultimate (2026-10-03)

Tất cả là animation Pixellab (yêu cầu user: không dùng sprite tĩnh): vòng xoáy tụ lực, quả cầu nước, gợn sóng cong,
nước phun, rồng nước, vũng nước, giọt ướt, gợn mưa. Cùng công thức §1; riêng gợn sóng cong gen ra mở sang phải nên phải lật ngang
để phình về phía trước. Chi tiết và danh sách file: `SkillVfxPipeline.md` §7.7.1.

## Quy tắc công cụ gen (bắt buộc từ 2026-10-03, D-081)
Mọi asset gen MỚI cho nguyên tố này (và nguyên tố khác) phải dùng **Pixen + `animate_image`** (≈5 gen/asset) thay cho
`create_1_direction_object` + `animate_object` (≈24 gen/asset). Quy trình đầy đủ: `SkillVfxPipeline.md` §4. Các công thức prompt/style
trong tài liệu này vẫn đúng, chỉ đổi công cụ gen.

