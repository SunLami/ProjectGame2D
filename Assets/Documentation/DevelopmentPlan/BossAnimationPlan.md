# Kế hoạch animation boss Earth (phương án A — D-092)

Trạng thái: **M4a xong (2026-10-03)** — 8 clip đã gen, nạp vào `BossClipPlayer` và chạy trong Play Mode; Fist và Recovery đã thay bằng bản cắt lớp miễn phí (2026-10-03); còn duyệt cảm giác ra đòn với user. User chốt phương án A lần hai (đổi từ B). Nguồn: `EarthBossCombatPlan.md` §6B/M4, `BossEncounterConcepts.md` §6.

## 1. Vấn đề
Boss chỉ có 1 sprite (`EarthGolem_Final_256.png`), chuyển động hoàn toàn bằng code nên di chuyển và ra đòn trông đơ, không thấy được "đang cast skill nào".

## 2. Phương án A (user chọn)
Animation đầy đủ khung 256×256, 8 frame/clip, sinh bằng Pixellab `animate_image` (Pixen đã dùng cho sprite gốc, D-081) từ sprite gốc làm frame đầu. ≈ 9 generation/clip.
- Đã thử nghiệm phương án B (cắt lớp trong `pixelart_workbench draw`, miễn phí) và cho kết quả dùng được; **giữ làm phương án dự phòng/Water-Wind** (xem §6) nhưng không dùng cho Earth theo quyết định của user.
- Nguồn đưa vào Pixellab: ảnh still xuất từ workbench (`first_frame_url`), khớp từng pixel với `EarthGolem_Final_256.png` (base64 bị cắt khi truyền nên không dùng được).

## 3. Danh sách clip (8 frame, ~9 gen, dự phòng retry ≈ 95 gen tổng)
| Clip | Dùng ở | Mô tả chuyển động |
|---|---|---|
| `Idle` (loop) | mặc định | thở, lõi xanh nhấp nháy, đá nhỏ lơ lửng |
| `Move` (loop) | `DriftTowardPlayer/DriftTo` | trôi tới, thân nghiêng, hai tay đung đưa |
| `Slam` | Thiết Quyền Nghiền | giơ hai nắm đấm qua đầu (giữ) → giáng xuống → hồi |
| `Raise` | Mưa Đá Vỡ | dang hai tay lên, ngửa đầu (giữ) → hạ tay |
| `Stomp` | Địa Mạch Gai | hạ thấp, nện hai nắm đấm xuống đất (giữ) → hồi |
| `Fist` | Phi Quyền | kéo tay phải ra sau (giữ) → phóng; tay phải trống trong lúc nắm đấm bay |
| `Resonance` | Tụ Lõi + Giải phóng, chuyển phase | dang tay, lơ lửng lên, lõi bừng sáng, gầm |
| `Recovery` | Kiệt sức | gục xuống, đầu cúi, tay buông, lõi mờ |
Clip có thể có **điểm giữ** (hold): animator phát tới frame giữ, chờ `Continue()` từ skill (hết telegraph) rồi phát tiếp.

## 4. Kiến trúc
- `BossPoseAnimator`/`BossClipPlayer` trên boss: `Play(clip)`, `Hold`, `Continue()`, loop idle/move; thay `Sprite` của `_body`; giữ nguyên bob/tint/flash của `BossController`.
- `BossController`/`BossSkills` chỉ gọi hook đầu/cuối telegraph; không đổi gameplay, số liệu `BossDefinition`, timing telegraph.
- Sheet nhập bằng builder (PPU/pivot trùng sprite gốc); `BossPrefabBuilder` kiểm tra đủ clip.

## 5. Tiêu chí hoàn thành M4
- Mỗi skill có clip riêng nhìn thấy trong Play Mode; pivot/kích thước không nhảy giữa clip (bounds lệch < 4 px).
- Idle/Move/Recovery rõ ràng; không đổi sát thương/telegraph/timing; `BossDefinitionTests` pass; không lỗi console.
- Frame không vỡ phong cách (palette/viền) so với sprite gốc — duyệt từng clip bằng mắt.

## 6. Ngân sách & rủi ro (cần user biết)
- Còn **286 generation đến 2026-10-27** (kiểm tra 2026-10-03). Earth ≈ 95 gen ⇒ còn ≈ 190; Water + Wind cùng phương án A cần ≈ 190 nữa nên **gần như hết sạch** và không còn chỗ cho retry/skill VFX/SFX art. Đề xuất: Water/Wind dùng phương án B (cắt lớp miễn phí) hoặc đợi reset kỳ sau.
- `animate_image` mô tả chuyển động, không đảm bảo pose chính xác ⇒ có thể phải retry; đặt `last_frame` để ghim điểm kết khi cần.

## 7. Kết quả triển khai (2026-10-03)

- Đã chi **88 generation** (8 clip đầu + gen lại Raise/Recovery/Fist ≈ 8 clip × 8 gen + 3 × 8 gen); còn ≈ **198** đến 27/10. Sheet nằm ở `Assets/Art/BossArena/Earth/Boss/Anim/<Clip>_9f.png` (9 frame: frame 0 = sprite gốc), tải bằng `Tools/fetch_boss_clip.py`.
- Code: `BossClipPlayer.cs` (phát clip, điểm giữ, kéo giãn wind-up theo telegraph), hook ở `BossController`/`BossSkills` (Slam, Raise=Mưa Đá, Stomp=Địa Mạch Gai, Fist=Phi Quyền, Resonance=Tụ Lõi + chuyển phase, Recovery=kiệt sức + chết, Idle/Move theo `DriftTo*`), `BossClipSetup.cs` (import/slice + điền clip, được `BossPrefabBuilder` gọi). Điểm giữ: Slam 4, Raise 5, Stomp 4, Fist 5, Resonance 6, Recovery 6.
- Chất lượng (duyệt bằng mắt): **tốt** Resonance, Slam, Idle, Stomp, Raise (lần 2); **đạt** Move; **yếu** Fist (wind-up gần như không kéo tay, bù bằng cú đấm mở tay + tia sáng ở frame 7-8) và Recovery (chưa gục rõ, chỉ cúi đầu + lõi tối). Hai clip này có thể gen lại hoặc cắt lớp miễn phí (`pixelart_workbench draw`, xem thử nghiệm: tay giơ chữ V dùng được).
- Verify Play Mode: boss triệu hồi → Mưa Đá giữ pose giơ tay (Raise_9f_5), Địa Mạch Gai/Slam phát clip, Move khi trôi, không lỗi console. Chưa verify từng clip ở tốc độ thật khi chơi.

### 7.1 Fist và Recovery thay bằng cắt lớp (miễn phí, 2026-10-03)

- Bản `animate_image` của hai clip này yếu (wind-up không thấy, không gục) nên được dựng lại bằng `pixelart_workbench draw` từ chính sprite gốc: đầu / hai tay / thân là các lớp riêng, xoay quanh vai, đầu hạ xuống phía dưới thân; **0 generation**. Canvas 400×336 (offset 72,40) để tay giơ cao không bị cắt; PPU 32, pivot giữa ⇒ khớp vị trí với các clip 256.
- `Fist`: tay phải kéo cao ra sau + tay trái đỡ (frame 1-5, giữ ở 5), đấm ra (6-8). `Recovery`: thân hạ ~20 px, đầu chìm giữa hai vai, tay buông (giữ ở 6), rồi đứng dậy (7-8).
- Tải bằng `Tools/fetch_layered_clip.py <drawing_id> <Clip> 400 336 9`. Cùng kỹ thuật dùng cho Water/Wind (D-092 §6). Hạn chế: mép cắt phẳng dưới giáp vai khi tay xoay lớn, vài pixel thừa ở đá lơ lửng.
- Verify Play Mode: Phi Quyền hiện wind-up tay phải kéo ra sau trước khi nắm đấm phóng đi. Recovery chưa xem lại ở tốc độ thật.

## 8. Boss Water (cua D) — animation cắt lớp (D-099)

- Nguồn: `Assets/Art/BossArena/Water/Boss/CrabCand_D_seed404_256.png`. Phần cắt: râu trái/phải, đầu, càng trái/phải (khớp tại vai), chân trái/phải, thân. Pose chỉnh bằng góc xoay + dịch vị trí; râu đi theo đầu.
- Clip đã dựng (320×320, 9 frame, `Assets/Art/BossArena/Water/Boss/Anim/Crab_*_9f.png`): Idle, Move, Snap, Recovery. Kiểm tra: frame 0 của Idle khớp từng pixel với sprite gốc.
- Hạn chế: không có phần thân phía sau càng nên khi càng xoay lớn có chỗ hở nhỏ; một vài pixel viền thừa đã tự động xoá; càng chỉ xoay cứng (không mở/khép từng ngón).
- Chưa làm: tích hợp Unity (prefab, `BossClipPlayer`), skill và clip riêng của skill — chờ duyệt boss và duyệt thiết kế skill.
