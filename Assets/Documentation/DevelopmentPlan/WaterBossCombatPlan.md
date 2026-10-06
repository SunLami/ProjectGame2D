# Combat Boss Water — Cua Giáp Thủy Triều (ĐỀ XUẤT, chờ user duyệt)

Trạng thái: **user duyệt bộ skill 2026-10-03; bản chơi thử v0 đã dựng (D-101) — xem §10** (thứ tự user đặt: map → boss → skill → duyệt → dựng). Thay phần Thủy của `BossEncounterConcepts.md` §3 (viết trước khi có bãi biển, D-097). Cùng khung với `EarthBossCombatPlan.md`; số liệu player lấy từ đó (đi bộ 2 / chạy 4 đơn vị/s, dash 3.2 đơn vị/0.2 s, i-frame 0.12 s, stamina 100).
Boss: cua D (`Assets/Art/BossArena/Water/Boss/CrabCand_D_seed404_256.png`). Map: Bãi Biển Triều Cường (`BossArenaMapDesign.md` §16). Clip đã có: Idle, Move (AI), Snap, Recovery (cắt lớp).

## 1. Danh tính chiến đấu (khác Địa)
Địa = **nặng, theo vùng, bay lơ lửng chậm**. Thủy = **nhanh, địa hình đổi, kiểm soát chỗ đứng**: cua bò ngang, *lặn xuống cát*, gọi sóng từ biển, và **thủy triều** quyết định sân nào đứng được. Người chơi phải quản lý chỗ đứng theo triều (không "chạy vòng tròn" mãi).

## 2. Cơ chế triều (cơ chế riêng của map)
- **Dải triều** ≈ 8 đơn vị dưới đường bờ. Triều dâng/rút theo chu kỳ **40 s** (P1); trạng thái nước nông phủ dải này lúc dâng.
- Trong nước nông: **player chậm 40%**, **cua nhanh ×2** (dry 2.6 → nước 5.2 đơn vị/s, nhanh hơn player chạy 4) ⇒ đứng trong nước là rủi ro, nhưng sát bờ thì đòn cua yếu hơn (xem Sóng).
- **P2:** chu kỳ ×2 nhanh (20 s). **P3 "Triều Cường":** nước phủ tới giữa bãi (~55% chiều cao sân), chỉ còn dải cát khô phía nam + 2 cồn cát nhô; chu kỳ vẫn 20 s nhưng không rút hết.
- Dựng bằng code (mép nước trượt lên/xuống + animation sóng đã có `SeaWave_Tile_8f`), không đụng collider. Vùng nước nông kiểm tra theo toạ độ y của mép nước hiện tại.
- **Phụ thuộc kỹ thuật (cần user chấp nhận):** `Player` chưa có hook làm chậm (`ISlowable`) nên phải thêm `Player.ApplySlow(multiplier, seconds)` nhỏ trong `PlayerMovement` (nhân tốc độ, không đụng save/tiến trình).

## 3. Chỉ số mặc định (SO, chỉnh một chỗ)
- `B` = **14**, HP **2400** (mục tiêu ~4 phút: cua nhanh hơn nên đòn ngắn hơn). Hệ số nhận sát thương như Earth: ×1.25 trong Recovery (×1.5 sau tuyệt kỹ).
- CC: làm chậm hiệu lực 50%; choáng chỉ trong Recovery; kéo/hất tung miễn nhiễm. Khi **lặn** cua miễn sát thương.
- Khắc chế nguyên tố (D-070 vẫn *đề xuất, chưa Accepted*): Phong khắc Thủy ⇒ +25% nếu user chấp nhận.
- Nghiêng nhịp: telegraph ngắn hơn Earth một chút nhưng không dưới **0.8 s** (vùng vừa) / **1.2 s** (vùng lớn) để dash còn dùng được.

## 4. Năm skill
| # | Skill | Telegraph (P1 → P3) | Hiệu ứng / sát thương | Cách đối phó |
|---|---|---|---|---|
| 1 | **Kìm Kẹp** (clip `Snap`) | hình quạt 100°, bán kính 5, phía trước càng; 0.9 → 0.7 s; 2 nhịp (càng trái rồi càng phải sau 0.5 s, quạt xoay 25°) | mỗi nhịp **1.0 B**; trúng cả hai ⇒ giữ chân 0.6 s *(cần hook; v1 chỉ hất nhẹ)* | lùi chéo ngoài quạt; hoặc đứng sau lưng sau nhịp 1; dash i-frame đúng nhịp |
| 2 | **Bong Bóng Giam** | cua phun 3/4/5 bong bóng bay chậm (**3 đơn vị/s**) về vị trí dự đoán player (nhìn thấy được, không cần báo thêm; bán kính 1.0) | chạm player: vỡ, **0.9 B** + hất; đánh trúng bong bóng (1 đòn) thì phá sớm, không sát thương | đánh nổ sớm, hoặc lách qua; không cần dash |
| 3 | **Lặn Phục Kích** (clip `Burrow` → `Emerge`, mới) | cua **vùi xuống cát** (miễn sát thương 1.2 s), **gò cát** chạy bám player **6 đơn vị/s trong 2.0 s** (nước nông: gợn nước thay cho gò cát); rồi gò dừng, vòng đỏ bán kính **3.2** tại chỗ đó, báo **0.8 s** | trồi lên nổ: **1.2 B** + hất tung; sau trồi cua đứng yên 0.8 s (cửa sổ đánh nhỏ) | đừng đứng trên vòng khi nó hiện; dash qua; đánh gò chặn đường **không** được |
| 4 | **Sóng Triều** | một dải đỏ ngang từ biển (bắc) quét xuống nam, báo **1.5 s**; **2 khe hở rộng 4.5** (P3: 1 khe 4.0 nhưng 2 đợt liên tiếp) hiện rõ chỗ không có sóng | mặt sóng tốc độ **14 đơn vị/s**, dày 1.2 (dash i-frame xuyên được): **1.2 B** + đẩy lùi 3 đơn vị về phía nam | chui vào khe, hoặc dash xuyên đúng lúc |
| 5 | **Xoáy Nước** (tuyệt kỹ) | vòng đỏ lớn **bán kính 9** mở giữa sân, báo **2.0 s**; cua đứng giữa | **hút** player về tâm **3 s tốc độ 3.5 đơn vị/s** *(cần hook ngoại lực; chạy 4 thoát chậm)*; sát thương tick **0.5 B/0.5 s** trong vòng trong bán kính 2.5; sau đó 4 vòi nước xoay quanh cua 2 s (**0.8 B** mỗi vòi trúng 1 lần); rồi **Recovery 4.5 s ×1.5** | chạy ra rìa ngay, dash + chạy, đứng giữa hai vòi |

*Số liệu tính theo mục 1-2 của `EarthBossCombatPlan.md` (thời gian thoát vùng bán kính R = 0.2 + (R − 3.2)/4 s).* Không đòi quá **2 dash** trong một Combo.

## 5. Đợt (3 phase)
- **P1 (100–65%):** `A = Kìm Kẹp → Bong Bóng`, `B = Lặn Phục Kích → Sóng Triều`; Recovery 3.0 s; triều 40 s.
- **P2 (65–30%):** gầm 2 s (bất tử), **triều ×2 nhanh**; báo ×0.9; `C = Kìm Kẹp → Lặn → Bong Bóng`, `D = Sóng Triều + Bong Bóng (song song)`, `E = Lặn kép → Kìm Kẹp`; **Xoáy Nước sau mỗi 3 Đợt**; Recovery 2.5 s.
- **P3 (<30% — "Triều Cường"):** báo ×0.8 (tối thiểu 0.6 s); nước phủ tới giữa bãi; `F = Bong Bóng (song song) → Lặn → Kìm Kẹp`, `G = Xoáy → Kìm Kẹp ×2`; Xoáy sau mỗi 2 Đợt; Recovery 1.8 s.

## 6. Art/animation cần thêm (cắt lớp miễn phí trừ khi nói khác)
Đã có: `Crab_Idle`, `Crab_Move` (AI), `Crab_Snap` (Kìm Kẹp), `Crab_Recovery`.
Cần thêm: **`Crab_Burrow`** (thân chìm xuống, cát bắn) + **`Crab_Emerge`** (bật lên), **`Crab_Spit`** (ngả người, hai càng đưa lên miệng — phun bong bóng), **`Crab_Whirl`** (xoay người, hai càng giơ cao — Xoáy), có thể dùng lại `Crab_Snap` cho Sóng (đập càng xuống nước). VFX dùng lại kho `Resources/VFX/Skills/Water`: `WaterOrb_Wobble` (bong bóng), `WaterPuddle_Ripple`/`WaterRipple_Shimmer` (gợn của gò), `WaterTsunamiWave_Flow` (sóng), `WaterWhirlpool_Idle` + `WaterGatherRing_Swirl` (xoáy), `WaterSpray_Settle` (vòi), `WaterRainSplash_Hit`/`WaterDropletProjectile_Impact` (vỡ bong bóng). Gò cát: 1 sprite mới nhỏ (Pixen + animate, ~3 gen) nếu `WaterPuddle_Ripple` không đọc rõ trên cát.

## 7. Kiến trúc (không sửa `EnemyUniversal`; tái dùng khung Earth)
- Tái dùng `BossController` + `BossDefinition` + `BossTelegraph` + `BossClipPlayer`; **thêm id skill Thủy** vào `BossSkillId` (không đổi/xoá id cũ) và `case` mới trong `SkillRoutine`; skill Thủy viết trong file mới `BossSkillsWater.cs` (partial) để không động vào skill Earth; clip Thủy thêm vào `BossClipId` + `BossClipSetup`.
- `TideController` (mới, trong scene arena): cung cấp `WaterLineY`, `IsShallow(point)`, chu kỳ theo phase; boss/skill hỏi nó. `BossArenaWaterBuilder` (Editor) dựng scene `BossArena_Water` từ `BossArenaEarthBuilder` (đổi nền, object, bệ ốc tù và, totem, shrine/ritual, camera), tuân D-090/D-091.
- Phụ thuộc cần user duyệt: `Player.ApplySlow` (làm chậm khi nước nông), tuỳ chọn `Player` bị giữ chân/hút (chỉ cho Kìm Kẹp nhịp 2 và Xoáy). Nếu không duyệt hook: v1 bỏ giữ chân, Xoáy chỉ gây sát thương + hất ra ngoài thay vì hút.

## 8. Tiêu chí hoàn thành
Mỗi skill có telegraph đọc được và né được bằng chạy hoặc dash; triều đổi địa hình theo phase; không đổi gameplay Earth; test đơn vị cho `BossDefinition` Thủy; Play Mode đo sát thương thật như Earth §6B; docs/DecisionRegister cập nhật cùng lúc.

## 9. Câu hỏi cho user (cần chốt trước khi dựng)
1. Duyệt bộ **5 skill** trên (Kìm Kẹp, Bong Bóng Giam, Lặn Phục Kích, Sóng Triều, Xoáy Nước) hay đổi/thêm/bớt?
2. **Triều ảnh hưởng gameplay** (chậm 40% khi ở nước nông, cua nhanh ×2) — đồng ý thêm hook `Player.ApplySlow`?
3. Có muốn **giữ chân (Kìm Kẹp) và hút (Xoáy)** player không (cần hook ngoại lực), hay bản đầu bỏ?
4. Độ khó: HP 2400, B 14, cho P3 "Triều Cường" nước phủ ~55% sân — có ổn không?

## 10. Kết quả bản chơi thử v0 (2026-10-03, D-101)

- Đã chạy trong Play Mode (`BossArena_Water`, summon bằng F6/`TrySummon`): cua vào trận, **5 skill đều chạy và gây sát thương/hiệu ứng thật**: Kìm Kẹp (quạt hai nhịp, máu player 100 → 78.8 → 54.8 khi đứng trong quạt), Bong Bóng (bong bóng bay chậm, trúng player 100 → 78.8), Lặn Phục Kích (gò chạy theo → vòng đỏ → trồi lên), Sóng Triều (dải đỏ với 2 khe; trúng 100 → 85.2 và bị đẩy lùi), Xoáy Nước (hút x 8 → 2.8, vòi xoay hất và gây sát thương; máu 100 → 52.4 tổng cộng). Triều: mép nước dao động, `IsShallow` đúng, `ApplySlow` hoạt động.
- **Chưa có / còn thô:** shrine + nghi lễ + Water Orb (summon bằng F6), object/decor map, clip riêng của Lặn/Phun/Xoáy (đang mượn frame), giữ chân của Kìm Kẹp, âm thanh, hiển thị nước nông chỉ là một tấm nước mờ + dải bọt, chưa test hết ba phase bằng đánh thật, cân bằng chưa đo.
- Đã sửa trong lúc test: cua bị ẩn mãi nếu skill Lặn bị ngắt (giờ luôn hiện lại), bong bóng quá to che HUD (nay khớp vùng trúng).

### 10.1 Cập nhật (D-102): vòng chơi đầy đủ
- Shrine ốc tù và + Water Orb, 4 totem san hô bắn tia aqua, clip Burrow riêng; hiệu ứng nước thay đá cho xuất hiện/chết/chuyển phase.
- **Trận đầy đủ đã chạy** (player bất tử, ×3 tốc độ): thứ tự skill đúng thiết kế — P1 `A: Kìm Kẹp→Bong Bóng`, `B: Lặn→Sóng`; P2 `C: Kìm Kẹp→Lặn→Bong Bóng`, `D: Sóng+Bong Bóng`, `E: Lặn→Lặn→Kìm Kẹp`, rồi **Xoáy Nước**; P3 `F`, `G`...; cua chết → tượng hiện lại, triều rút về, bong bóng dọn sạch.
- Chưa làm: giữ chân Kìm Kẹp, clip riêng cho Phun và Xoáy, decor/chuyển động môi trường còn lại, âm thanh, cân bằng.

### 10.2 Cập nhật (D-104): hoàn thiện skill + SFX
- Giữ chân Kìm Kẹp **đã làm** (0.6 s khi trúng cả hai nhịp; nhịp 1 hất nhẹ). Clip riêng cho Phun (Spit) và Xoáy (Whirl) **đã làm**.
- Art mới cho skill: gò cát (`SandMound_Move`), cát bắn (`SandBurst_Hit`), vệt càng (`ClawSlash_Hit`), tường sóng (`TidalWall_Flow`); vòi Xoáy dùng `WaterBeam_Flow`. Không còn thanh màu trơn trong skill nào của cua.
- SFX: 19 sound `sfx.bossw.*` (xem `SfxPipeline.md`), mỗi skill có tiếng báo/tung đòn/trúng, loop cho gò cát và xoáy.
- Còn lại: decor/chuyển động môi trường, cân bằng (chưa đo), hiển thị nước nông của triều vẫn là tấm nước mờ.
- **D-105:** cua được gen lại tư thế hai càng giơ lên (đủ chân hai bên) và toàn bộ clip dựng lại từ bản đó (thêm `Slam` cho Sóng Triều và `Resonance` cho gầm đổi phase).
- **D-106:** cua đuổi theo player giữa các skill; Kìm Kẹp là chuỗi dí-và-chém; Bong Bóng là 3 đợt vòng tròn 360° tăng dần; thủy triều dùng asset bờ nước animate (`TideEdge_Flow`).
- **D-107:** Xoáy Nước hiện vùng hút + tầm vòi trước khi mở xoáy; vòi là xúc tuộc nước quay tăng tốc rồi đảo chiều, hất player vào tâm, kết thúc bằng vòng nước bùng ra; totem san hô có collider đủ lớn.
