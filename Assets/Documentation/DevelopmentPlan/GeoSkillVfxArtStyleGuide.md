# Địa/Nham (Earth) Skill VFX — Art Style Guide (chốt 2026-10-02)

Kế thừa nguyên tắc của `ThuySkillVfxArtStyleGuide.md` (D-072) — cùng độ chunky, cùng pipeline kỹ thuật —
chỉ đổi chất liệu và bảng màu sang đá/đất (D-075). Mọi VFX bộ Địa/Nham phải bám công thức này.

## 1. Công thức prompt chuẩn

Luôn chèn các cụm sau vào prompt `create_1_direction_object`:

> HARD-EDGED chunky pixel art, flat cel-shaded color bands only — dark brown, mid earthy brown,
> tan/sand, bright pale-yellow highlight — NOT a smooth gradient, NOT airbrushed glow, bold thick black
> pixel outline, high contrast, no background.

Chất liệu đá: khối **góc cạnh, mép sứt mẻ bất quy tắc** (cracked angular rock, irregular jagged chipped
edges), mảnh vụn/bụi be nhỏ rải quanh — KHÔNG tròn trơn như quả bóng. Burst va chạm là **vụ nổ mảnh đá toả
tròn** quanh lõi sáng vàng nhạt, có bụi tan.

Ràng buộc hướng cho projectile: "nose pointing exactly straight right (0 degrees), symmetric top-to-bottom
around the horizontal center axis" (D-071, xoay Transform runtime).

## 2. Animation

- `animate_object` v3, `frame_count` 8 → 9 frame. Prompt animation phải ghi "rock stays the same size and
  position" cho projectile để tránh trôi khung.
- Burst: animation "shrinking and fading" thường có 1-2 frame cuối chỉ còn viền đen (xấu) → **loại các
  frame đó** sau khi xem contact sheet (Skill 1 Earth giữ 7/9 frame).
- Luôn xem contact sheet trước khi import; không sửa tay frame lỗi, gen lại nếu cần.

## 3. Pipeline kỹ thuật

Giống `ThuySkillVfxArtStyleGuide.md` §3: canvas 176×176, view top-down, sprite sheet 1 hàng ngang,
PPU 48, Point filter, Uncompressed, pivot (0.5, 0.5). Sorting: VFX mặt đất dùng layer `Default` với
`sortingOrder` âm; projectile dùng layer cùng Water (xem prefab). Thư mục asset:
`Assets/Resources/VFX/Skills/Earth/`, tên file tiếng Anh.

## 4. Asset đã chốt

| Skill | File | Mô tả |
|---|---|---|
| Skill 1 — Thạch Trụ (projectile) | `RockProjectile_Fly.png` | Tảng đá góc cạnh nhiều mảng nâu/be, mảnh vụn rung nhẹ phía sau, 9 frame |
| Skill 1 — Thạch Trụ (burst) | `RockProjectile_Impact.png` | Nổ mảnh đá + lõi sáng vàng nhạt, tan dần, 7 frame |
| Skill 2 — Đấu Trường Đá (cột đá) | `RockPillar_Rise.png` | Cụm gai đá cam-nâu trồi lên từ bụi đá (9 frame, tạo bằng đảo ngược animation chìm xuống) |
| Skill 2 — Đấu Trường Đá (nền) | `RockArena_Floor.png` | Nền đất nứt tròn viền đá |
| Dùng chung — icon choáng | `StunStar.png` | 1 ngôi sao vàng; code cho 3 sao xoay quanh đầu enemy (`SkillStunIndicator`) |

| Skill 2 — Đấu Trường Đá (nền, animation xuất hiện) | `RockArena_FloorAppear.png` | 7 frame nền loe ra từ vết nứt nhỏ (gen "co đều về tâm" rồi đảo ngược; bỏ frame thủng) |

### Bài học thất bại (Địa/Nham)

- `animate_object` cho prompt "sụp đổ/co lại" trên nền tròn → đất biến mất rồi hiện lốm đốm, hoặc chỉ mất vài
  tảng nhỏ ở mép, kèm frame thủng trong suốt; không tạo được "cột đá mọc lên" từ nền đã vẽ sẵn. Muốn cột đá
  mọc: gen cột đá riêng (object riêng + animation chìm xuống rồi đảo ngược) rồi đặt bằng code trên mép nền.
| Skill 3 — vòng gai khóa chân | `RockSpike_Rise.png` | 1 gai đá đơn mọc từ đất (9 frame, đảo ngược animation chìm); code xếp 10 gai thành vòng quanh chân enemy |

- Prompt gai phải ghi "exactly ONE single spike... no second spike, no cluster": lần gen đầu có "one smaller spike
  beside it" khiến mỗi sprite có 2 gai, xếp thành vòng nhìn như từng cặp.
| Skill 4 — thiên thạch | `Meteor_Fall.png` | Tảng đá cháy rơi thẳng, đầu đá ở dưới, đuôi lửa + khói hướng lên (16 frame ping-pong) |
| Skill 4 — hố va chạm | `MeteorCrater_Smolder.png` | Hố cháy đen bùng lửa cam rồi nguội thành than hồng (9 frame, 1 lần) |
| Skill 4 — vòng rune | `EarthRune_Pulse.png` | Vòng rune đất cam-vàng sáng tối dạng sóng (16 frame ping-pong) |
| Skill 4 — đá lơ lửng | `RockChunk_Hover.png` | Tảng đá xoay chậm lơ lửng (16 frame ping-pong) |
| Skill 4 — sóng xung kích | `Shockwave_Expand.png` | Vòng bụi + mảnh đá lan ra (7 frame; gen "co về tâm" rồi đảo ngược) |

- **Mọi hiệu ứng skill phải là animation Pixellab** (yêu cầu user 2026-10-02), không dùng sprite tĩnh chỉ xoay/mờ bằng code.
- Loop animation: ghép ping-pong (0..8..1) để không có seam; animation "co/chìm" gen xong thì đảo ngược để có "lan/mọc".

## Quy tắc công cụ gen (bắt buộc từ 2026-10-03, D-081)
Mọi asset gen MỚI cho nguyên tố này (và nguyên tố khác) phải dùng **Pixen + `animate_image`** (≈5 gen/asset) thay cho
`create_1_direction_object` + `animate_object` (≈24 gen/asset). Quy trình đầy đủ: `SkillVfxPipeline.md` §4. Các công thức prompt/style
trong tài liệu này vẫn đúng, chỉ đổi công cụ gen.

