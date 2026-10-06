# Boss Arena Art Style Guide — "skill-grade" (viết lại 2026-10-03)

Thuộc D-083 (`BossArenaMapDesign.md`). **Thay thế** bản đầu (hướng tileset Wang 16px + remap palette Craftpix) sau phản hồi của user:
"tileset chưa thực sự chi tiết; nhìn cách gen asset skill — tôi hài lòng độ chi tiết đó; MapBoss phải đẹp, chuyên nghiệp hơn".

## 1. Vì sao bỏ hướng cũ

- `create_topdown_tileset` 16px (Wang) chỉ cho nền phẳng, thô, ít chi tiết, không ép được palette; gen tường thất bại 3 lần (ra hình "tòa nhà có cửa sổ"). Đã xóa toàn bộ asset thử (7 tileset, 26 generation) và công cụ remap.
- Asset skill dùng canvas **176 px, PPU 48** (≈ 48 px/đơn vị), cel-shade cứng, viền đen đậm, tương phản cao — dày đặc chi tiết gấp ~3 lần tile 16px (16 px/đơn vị). Map boss phải **cùng mật độ pixel** với skill/nhân vật, nếu không skill sẽ "lệch tông" trên nền.

## 2. Pipeline mới: dựng sân bằng ảnh Pixen chi tiết (không dùng tileset 16px)

| Mục | Quy tắc |
|---|---|
| Công cụ | `create_image_pixen` (1 generation/ảnh, canvas ≤ 512×512 diện tích, mỗi cạnh bội 4); prop dùng `no_background=true`; vật có animation → `animate_image` (D-081). Sửa/nối cạnh: `edit_image_pixen` / `inpaint` |
| Mật độ | **PPU 48**, Point filter, Uncompressed, `alphaIsTransparency` cho prop (giống Resources/VFX) |
| Công thức prompt | `top-down view` + chất liệu cụ thể + `HARD-EDGED chunky pixel art, flat cel-shaded color bands, bold dark outlines, high contrast, no gradients` + `highly detailed`, `single color black outline`, `view: high top-down` (đúng công thức skill ở `GeoSkillVfxArtStyleGuide.md` §1) |
| Sàn | Các mảng sàn lớn (vd 448×320 ≈ 9.3×6.7 đơn vị) ghép thành sân; mỗi mảng duy nhất, không lặp ô. Ẩn đường nối bằng decal (nứt, rêu, mảnh vỡ, vệt rune) và vật thể đặt chồng |
| Tường/viền | Ảnh Pixen riêng cho từng cạnh/góc (không dùng Wang) |
| Prop | Pixen `no_background`, có bóng đổ mềm, kích thước lớn (cột 176×256 ≈ 3.7×5.3 đơn vị) |
| Vật động | Nước, mây, rune phát sáng, lá/lông bay, trụ triệu hồi: `animate_image` 8 frame (4 gen) — bắt buộc cho thứ nào "sống" |
| Nhiều bản | Gen ≥ 2–3 bản mỗi prop quan trọng, chọn bản đẹp nhất (không sửa tay frame lỗi) |
| Camera | Arena ortho ≈ 12.5 → ~43 px/đơn vị trên màn 1080p, gần 1:1 với art 48 PPU (sắc nét) |

### 2A. Nền lớn trống + object + chuyển động (2026-10-03)

- **Cập nhật (user): gen CẢ SÂN TRỐNG trong MỘT ảnh Pixen** (kích thước ảnh không quan trọng, scale trong Unity; không gian trong ảnh phải rộng như ảnh minh họa). Canvas cùng tỉ lệ sân: sân 92×57 (1.6:1) ⇒ thử 640×400 / 576×360 (sân 46×38 trước đó dùng 512×424). Canvas ngang 640×400 làm Pixen vẽ **hai phòng cạnh nhau** → tránh. Vật thể tương tác/tiền cảnh/chuyển động (cột, bệ trụ, rune sáng, bụi...) gen riêng. Phần ghép mảng dưới đây chỉ là phương án dự phòng.
- **Nền trống (phương án dự phòng):** gen các mảng sàn Pixen (448×320 hoặc 512×512), prompt "seamless continuous floor, filling frame edge to edge, no border/wall/objects/rune lines", cùng `seed` họ + cùng mô tả màu. Thực nghiệm 4 mảng: mỗi mảng chi tiết đẹp nhưng **lệch tông và tối viền (vignette)**; ghép bằng `Tools/arena_floor_stitch.py` (cắt phần giữa bỏ viền tối → cân màu theo mảng tham chiếu → ghép bằng **seam tối ưu chạy dọc rãnh vữa** thay vì hòa trộn mềm, vì hòa trộn gây viền kép). Kết quả `Assets/Art/BossArena/Earth/Pilot_FloorStitch_2x2.png` đọc như một mặt sàn liền. Cải thiện: thêm "even lighting, no vignette, no dark corners" vào prompt; cắt rộng hơn.
- **Object** gen riêng, `no_background=true`, đặt chồng lên nền (đồng thời che nếu còn đường nối).
- **Chuyển động:** vòng lặp `animate_image` hoặc particle; danh sách theo hệ ở `BossArenaMapDesign.md` §3B.
- Số mảng: ~12–16 mảng gốc/map + lật/xoay; ~1 generation/mảng.

## 2B. Mẫu prompt nền sân (bắt buộc đủ các khối này; user 2026-10-03 "prompt gen ảnh cần kĩ hơn")

1. **Camera + mục đích:** `Top-down pixel art game map of one single empty <chất liệu> courtyard, camera looking straight down.`
2. **LAYOUT BY ZONES theo % ảnh:** mặt tiền tường trên (≈17%: gạch, hàng cột đều nhau, ĐÚNG MỘT ngách ở chính giữa ngang); tường dưới (≈8%, ĐÚNG MỘT cổng ở chính giữa); hai viền bên mỏng (≈5%); phần còn lại là sàn.
3. **Chất liệu sàn:** phiến chữ nhật đều xếp hàng gọn, chỉ nứt/sứt nhỏ; **cấm tường minh** "engraved symbols, carved panels, squares, patterns, circles, objects, shadows from objects" (hoa văn là object riêng).
4. **Thảm thực vật:** cỏ/rêu chỉ ở bốn góc và mép; **trung tâm giữ đá trơn**.
5. **Ánh sáng:** đều, "no vignette, no dark corners, no cast shadows".
6. **Style:** công thức skill (HARD-EDGED chunky, flat cel-shaded, bold dark outlines, high contrast).
7. **Phủ định cuối:** "empty: no characters, creatures, statues, props, altar, fountain".
8. Canvas gần vuông (512×424), gen ≥ 3 seed, chọn bản có sàn đồng đều + ngách/cổng gần giữa.

## 3. Mẫu pilot đã gen & duyệt hướng (Earth)

**Nền sân trống Earth (đã chọn):** `BaseMap_Earth_Empty_1320x804.png` (nguồn gen `BaseMap_Earth_Source_512x424.png`). Sàn đi được ≈ 1230×625 px: so với sân gốc (400×290 px) là **×3.1 ngang, ×2.2 dọc**; user yêu cầu thêm ×1.5 ngang/dọc lần 2 so với bản 835×420 (đo trên ảnh: ×1.47 / ×1.49). Cách làm: gen 1 ảnh gần vuông 512×424 bằng prompt có cấu trúc (§2B) ⇒ cắt tại tâm ngách, nới nửa trái bằng cách **lặp một đoạn sàn tại điểm khớp tối ưu** (65 px × 6) rồi **lật đối xứng** (ngách và cổng nằm đúng giữa), nới dọc bằng lặp đoạn 86 px × 5 (đoạn dọc phải CỐ ĐỊNH 212–298; để thuật toán tự tìm lại đã chọn nhầm đoạn 165 px làm sàn dọc ×2.4). Chạy qua `Tools/arena_extend_reference.py`. Thất bại cần nhớ: mọi canvas ngang ≥ ~1.4:1 (640×400, 576×384, 576×360, 544×352) đều bị Pixen thêm một dải ngăn/phòng thừa (thử 9 lần, cả prompt "single undivided") ⇒ **không gen ảnh ngang trực tiếp**. Lần đầu nới thất bại vì hoa văn khắc bị lặp và ngách lệch ⇒ nền gốc phải **trống hoàn toàn** (hoa văn/rune là object gen riêng). Chi phí nhóm nền sân: 14 generation.

| File | Nội dung | Đánh giá |
|---|---|---|
| `Assets/Art/BossArena/Earth/Pilot_PlazaFloor_448x320.png` | Sàn plaza đá nứt, kênh rune xanh lục hình sao 6 cánh, rêu/cỏ trong khe, mảnh vỡ, cột đổ ở các góc | Đạt mật độ chi tiết skill; rune xanh lục khớp golem (concept 3) |
| `Assets/Art/BossArena/Earth/Pilot_StonePillar_176x256.png` | Cột đá nứt nhiều khối, glyph rune xanh, rêu/dây leo, mảnh vỡ chân cột, bóng đổ | Đạt; dùng làm vật cản/cột trang trí |

Chi phí: 2 generation (363 → 361 còn lại, reset 2026-10-27).

## 4. Bảng màu theo hệ (khớp concept boss + màu skill của hệ)

| Hệ | Chủ đạo | Điểm nhấn | Tránh |
|---|---|---|---|
| Địa | nâu đất, be/cát, đá xám-nâu | rune **xanh lục phát sáng** (từ golem) | màu nâu cam của skill Địa chiếm hết (rune xanh để skill nổi) |
| Thủy | xanh-trắng ngà (vỏ cua), xanh navy-xanh biển, xám đá ướt | san hô đỏ-nâu (râu cua), bọt trắng | cyan bão hòa (trùng skill Thủy) |
| Phong | trắng-lam-tím nhạt (lông cú), đá trắng ngà, mây | vàng đồng vương miện, ngọc xanh lam phát sáng | trắng/mint đặc (trùng skill Phong) |

## 5. Bộ object + chuyển động Earth (2026-10-03)

Thư mục: `Assets/Art/BossArena/Earth/Objects/` (tĩnh) và `Objects/Anim/` (sprite sheet ngang 9 frame = frame gốc + 8 frame, lặp). Bố cục tham chiếu: `Preview_Earth_Layout_1320x804.png`. **Quy tắc đặt decor: `BossArenaMapDesign.md` §3C** (giữa sân trống hoàn toàn; decor chỉ ở viền/góc/tường; boss đi khắp sân).

| Object | Canvas | Vị trí | Chuyển động |
|---|---|---|---|
| `SummonShrine` (bệ trụ triệu hồi) | 96×80 | khu ngách bắc, giữa | `SummonShrine_Glow_9f` — ngọc + rune + đầu gai sáng nhịp, tia sáng nhỏ |
| `FloorSeal` (hoa văn sàn trung tâm, decal phẳng không collider) | 256×256 | tâm sân | `FloorSeal_RuneGlow_8f` — **sinh bằng code từ chính ảnh** (rãnh tối → rune xanh sáng nhịp sin, 8 frame, 0 generation); đặt chồng lên seal |
| `GolemStatue` | 64×80 | 4 góc (trong mảng cỏ), lật theo bên | `GolemStatue_RunePulse_9f` — rune/rêu ngực nhấp sáng nhẹ |
| `Brazier` (lò lửa) | 48×64 | dọc viền sát tường bắc/nam | `Brazier_Flame_9f` — lửa xanh-trắng bập bùng + tàn lửa |
| `Banner` (cờ tường) | 40×72 | trên tường bắc, giữa các cột | `Banner_Sway_9f` — đung đưa |
| `Vines` (dây leo) | 48×72 | tường bắc | `Vines_Sway_9f` — đung đưa |
| `GrassTuft` | 32×32 | 4 góc | `GrassTuft_Sway_9f` — lay |
| `Boulders`, `BrokenPillar` | 64×48, 48×64 | chỉ trong mảng cỏ ở góc (không giữa sân) | tĩnh |

**Chuyển động môi trường bằng code/hạt (chưa làm, cần Unity):** bụi/đốm sáng lơ lửng, đá vụn rơi lác đác từ tường, đom đóm xanh quanh bệ trụ — không collider, tắt được ở chất lượng thấp.

**Bài học gen object:**
- Object phải vẽ **ở đúng mật độ pixel của nền** (nền ≈ 9 px/đơn vị: phiến sàn ~28 px, cột tường ~24×56 px) ⇒ canvas nhỏ 32–96 px. Bản 176 px đầu quá chi tiết so với nền nên bỏ.
- Pixen **thiên về đế hình thoi (isometric)** dù prompt cấm ("NOT isometric/diamond"); `view: side` + "front-facing 2D RPG sprite like Zelda A Link to the Past" giúp nhiều nhưng bệ trụ/tượng vẫn hơi thoi. Chấp nhận ở mức hiện tại; nếu muốn đế vuông thật phải gen nhiều seed.
- `BrokenPillar` ra giống ngôi đền nhỏ (không đúng cột gãy) — chỉ dùng làm đồ góc; gen lại nếu cần.
- `animate_image` 8 frame trên sprite ≤ 96×80 chỉ tốn **1 generation/animation**; giới hạn **8 job song song**.
- Chi phí bộ object Earth: 9 + 5 (object lệch kiểu isometric đã bỏ) + 7 + 6 animation ≈ 27; tổng nhóm Boss Arena Earth từ đầu: ~73 (389 → 316 còn lại).

## 6. Tượng triệu hồi giữa sân, trình tự triệu hồi và hiệu ứng thời tiết Earth (2026-10-03)

**Yêu cầu user:** tượng triệu hồi ở GIỮA sân; khi bỏ ngọc và kích hoạt, tượng biến mất và boss từ từ hiện ra; phần còn lại chủ yếu là hiệu ứng môi trường — vd bụi cát bay qua lại ngay màn hình chơi như sa mạc.
(Cập nhật §3C: tượng ở giữa là ngoại lệ hợp lệ vì **biến mất trước khi trận đánh bắt đầu**; trụ ở ngách bắc ở Earth được thay bằng tượng giữa sân — `SummonShrine` cũ chỉ còn là phương án dự phòng.)

| Asset | File | Ghi chú |
|---|---|---|
| Tượng triệu hồi (đã chọn bản B: golem tan, ổ ngọc ở ngực, rune xanh trên tay, bệ vuông) | `Objects/Summon/SummonStatue_B.png` (96×112) | Hợp bảng màu sân và concept golem |
| Tượng idle (rune/ổ ngọc sáng nhịp) | `Objects/Summon/SummonStatue_Idle_9f.png` | 8 frame lặp |
| Tượng tan biến | `Objects/Summon/SummonStatue_Vanish_17f.png` | 16 frame: rune flare → nứt → vỡ thành đá/bụi; frame cuối trống (đã ghim). Bệ còn lại đến frame 15 ⇒ trong Unity cho **bệ mờ dần** ở 2–3 frame cuối |
| Bụi tan | `Objects/Summon/DustPuff_Anim_9f.png` | Dùng chồng lên lúc tượng vỡ |
| Vòng rune sàn sáng | `Objects/FloorSeal_RuneGlow_8f.png` | Sinh bằng code (0 gen); cho **sáng mạnh dần** lúc triệu hồi |
| Cát bay (mảng lớn) | `Objects/Weather/SandGust.png`, `SandGust_Anim_9f.png` | Ảnh gần tĩnh (animation chỉ nhích nhẹ) ⇒ chuyển động chính bằng code (tịnh tiến + alpha) |
| Cỏ khô lăn | `Objects/Weather/Tumbleweed.png`, `Tumbleweed_Roll_9f.png` | Animation lăn yếu ⇒ xoay Transform bằng code khi tịnh tiến |

**Trình tự triệu hồi (đề xuất, thực thi khi làm Shrine UI):** (1) bỏ đủ ngọc, UI tắt → tượng idle rực lên, vòng rune sàn sáng dần, rung màn hình nhẹ; (2) phát `Vanish` + `DustPuff` + tia sáng xanh; bệ mờ dần; (3) **boss hiện ra từ từ** (alpha 0→1 trong 2–3 s, kèm bụi và đốm xanh bay lên, boss bất tử + chưa hành động trong lúc hiện); (4) boss `Idle` rồi vào phase 1. Không có vật gì khác nằm giữa sân sau bước (2).

**Hệ thống thời tiết/môi trường Earth (mục tiêu chính phần hiệu ứng):**
- **Mảng cát bay** (`SandGust`): spawn ngẫu nhiên mỗi 20–50 s, bay ngang khung camera (trái→phải hoặc ngược, lật sprite), tốc độ 6–10 đơn vị/s, alpha 0.35–0.6, 2 lớp parallax; vẽ **trước Player nhưng dưới HUD** (đi ngang ngay màn hình chơi); không cản gameplay.
- **Cỏ khô lăn** (`Tumbleweed`): mỗi 40–90 s lăn ngang sân sát mép; không collider.
- **Hạt cát** (code): hạt nhỏ 1–3 px trôi theo gió, mật độ thấp; đá vụn rơi lác đác từ tường; đom đóm xanh quanh tượng.
- Cường độ giảm hoặc tắt khi chất lượng thấp; gió có thể mạnh lên theo phase boss (sau).

## 7. Boss Earth — concept & ứng viên (user 2026-10-03)

Concept: golem đá lơ lửng — đầu sọ góc cạnh mắt xanh, mảnh đá nhọn trôi sau đầu, vai khối đá, ngực khắc rune xanh, lõi xanh phát sáng ở bụng, hai cẳng tay/nắm đấm lớn tách rời có ngón vuốt đá, mảnh đá nhỏ trôi giữa thân và nắm đấm, bóng cam, không chân.
Ứng viên Pixen 256×256 ở `Assets/Art/BossArena/Earth/Boss/`: `EarthGolem_cand_1201.png` (nắm đấm gắn thân, sừng cong), `_1202` (có chân — lệch concept), **`_1203` (lơ lửng, mảnh đá nhọn sau đầu, lõi bụng sáng, nắm đấm lớn — gần concept nhất, đề xuất chọn)**.
**Chi phí animation boss:** `animate_image` tính theo diện tích — khung 256×256 × 8 frame ≈ 16 generation/animation (ngân sách: idle + tấn công + hiện ra ≈ 50+), nên cân nhắc khung 160–192 px cho các animation hoặc tách bộ phận (thân lơ lửng nhấp nhô + nắm đấm riêng bằng code). **Thiết kế boss/skill/wave vẫn làm riêng sau.**
