# Ý tưởng 3 Boss — môi trường & bộ skill khác nhau (Địa / Thủy / Phong)

Trạng thái: **ĐỀ XUẤT — chờ user duyệt** (2026-10-03). Quyết định: D-084. Thuộc `BossArenaMapDesign.md` (D-083). Boss Địa: sprite đã chốt (`Assets/Art/BossArena/Earth/Boss/EarthGolem_Final_256.png`).

## 0. Ràng buộc từ code hiện có (ảnh hưởng thiết kế né/telegraph)

- **Cập nhật 2026-10-03 (D-085):** player đã có **Dash** (Space, ~3.2 đơn vị/0.2 s, hồi 0.8 s, 20 stamina, miễn sát thương 0.12 s) ở DemoScene ⇒ phần "không có lướt" bên dưới là **lỗi thời**; telegraph có thể rút ngắn ~20–30% và hitbox cần tính i-frame; cân lại khi làm boss.
- Player: đi bộ **2 đơn vị/s**, chạy **×2 = 4 đơn vị/s** (stamina 100, hao 25/s ≈ 4 s chạy, hồi 18/s); **KHÔNG có lướt/lăn né** — chỉ có tỉ lệ né ngẫu nhiên (`DodgeChance`, tối đa 50%). ⇒ mọi đòn phải né được bằng *đi bộ/chạy trong thời gian báo hiệu* (telegraph), không dựa vào lướt.
- Máu cơ bản 100 (+3/cấp), đòn đánh cơ bản 10; `PlayerStat.ReceiveDamage` có Defense/DamageReduction.
- Boss **không dùng `EnemyUniversal`** (máy trạng thái Idle/Patrol/Chase/Attack, 3 kiểu đòn Melee/Area/Projectile, không có phase/combo/telegraph). Đề xuất **`BossController` mới** (không sửa `EnemyUniversal`, giữ nguyên code cũ): triển khai `IDamageable/ISlowable/IStunnable/IVulnerable`, dùng lại `EnemyHealthBar`, `BossDefeatTracker`, các VFX skill hệ Địa/Thủy/Phong làm hiệu ứng đòn boss. Dữ liệu boss/skill/combo là **ScriptableObject** (đúng Data-Driven gate).
- Sát thương/HP boss: user đã nói "đợi làm boss sẽ config lại" ⇒ mọi số dưới đây chỉ là **giá trị khởi điểm trong SO**, chỉnh một chỗ.

## 1. Khung chung (3 boss cùng một "bộ não", khác dữ liệu)

- **Cấu trúc đợt (wave):** một trận = nhiều **Phase** (theo % máu). Mỗi Phase lặp các **Đợt** = `Combo` (2–4 skill nối nhau, nghỉ ngắn 0.6–1 s) → **Recovery** (boss kiệt sức 2–4.5 s, nhận thêm sát thương + hở điểm yếu) → di chuyển lại vị trí → Đợt kế. Người chơi học nhịp "né → trả đòn trong Recovery".
- **Telegraph thống nhất:** vùng nguy hiểm tô đỏ (viền + nền mờ), có thanh/quầng đang đầy dần theo thời gian báo hiệu; đòn dài hơn 1 s chỉ khóa mục tiêu ở đầu rồi đứng yên. Vẽ bằng code (không tốn generation). Thời gian báo hiệu tối thiểu bảo đảm player *đi bộ ra khỏi vùng* được với bán kính nhỏ, *chạy* được với vùng lớn.
- **Quy tắc kiểm soát (CC):** Làm chậm hoạt động 50%; Choáng chỉ ăn trong Recovery (kéo dài thêm tối đa 1 s/lần); Kéo/Hất tung miễn nhiễm. Boss không bị đẩy lùi.
- **Điểm yếu nguyên tố** (D-070 mới là đề xuất, chưa Accepted): Thủy khắc Địa, Phong khắc Thủy, Địa khắc Phong ⇒ hệ số sát thương +25% khi đánh boss bằng bộ skill khắc; cần user chấp nhận mới đưa vào code.
- **Đánh lại để farm** (user đã chốt): tăng độ khó sau này bằng hệ số trong SO (tốc độ, HP, rút ngắn telegraph).

---

## 2. BOSS ĐỊA — Thạch Nhân Địa Chấn (golem đá lơ lửng)

**Sân:** Thạch Đài — đấu trường đá kín, chữ nhật, trống, đối xứng, trụ triệu hồi ở giữa (biến mất khi bắt đầu). **Danh tính chiến đấu:** *nặng, đọc được, chiếm vùng* — sân là vũ khí; người chơi được thử khả năng **vị trí và đi/chạy đúng hướng**. Nhịp chậm–vừa.

| # | Skill | Telegraph | Hiệu ứng | Né/đối phó | Sát thương* |
|---|---|---|---|---|---|
| 1 | **Thiết Quyền Nghiền** | vòng đỏ bán kính 4 đặt vào vị trí player, khóa sau 0.1 s, nổ sau 1.5 s | giáng nắm đấm: nổ + choáng 0.5 s nếu trúng; để lại hố nứt (trang trí) | chạy ra khỏi vòng | 1.0 |
| 2 | **Mưa Đá Vỡ** | 6/9/12 vòng đỏ bán kính 2.2 (2 vòng đầu nhắm player, còn lại ngẫu nhiên quanh player ≤10), mỗi vòng 1.2 s | đá lơ lửng rơi (dùng VFX thiên thạch hệ Địa) | đi len giữa các vòng | 0.7 mỗi viên |
| 3 | **Địa Mạch Gai** | 3/5 vệt đỏ hình quạt ±25° dài 20, rộng 1.6, khoảng trống an toàn ≥2.4, báo 1.1 s | gai đá trồi theo hàng (VFX gai hệ Địa) + làm chậm | bước ngang vuông góc | 0.9 + slow |
| 4 | **Phi Quyền** | vệt đỏ rộng 2.2 tới mép sân, báo 1.0 s | nắm đấm phải tách khỏi thân, bay thẳng 18 đơn vị/s rồi **quay về chậm 3 s** (đòn thứ hai khi quay) | tránh vệt cả hai chiều; khi nắm đấm xa, boss nhận +15% sát thương (cửa sổ phản công) | 1.3 đi / 0.6 về |
| 5 | **Cộng Hưởng Lõi** (tuyệt kỹ) | boss tới giữa sân, lõi sáng dần 2 s, 3 vòng sóng mở rộng 9 đơn vị/s cách nhau 1 s, mỗi vòng có **khe an toàn 40°** (vẽ vòng cung xanh báo trước, xoay khác nhau mỗi vòng) | sóng địa chấn lan toàn sân | đứng đúng khe | 1.1 mỗi vòng; sau đó **Recovery dài 4.5 s ×1.5 sát thương nhận** |

\* nhân với `B` (sát thương cơ sở boss, SO).

**Đợt:** Phase 1 (100–65%): `A = Nghiền → Gai`, `B = Mưa → Nghiền`, xen kẽ, Recovery 3.0 s. Phase 2 (65–30%, gầm thét chuyển phase 2 s bất tử + rune sàn bùng sáng): `C = Phi Quyền → Gai → Nghiền`, `D = Mưa → Phi Quyền`, `E = Nghiền ×2 → Gai`; sau mỗi 3 Đợt có **Cộng Hưởng Lõi**; Recovery 2.5 s. Phase 3 (<30%, nứt xanh lan rộng): thời gian báo hiệu −15%, `F = Mưa + Gai đồng thời → Phi Quyền`, `G = Cộng Hưởng → Nghiền ×2`; tuyệt kỹ sau mỗi 2 Đợt; Recovery 1.8 s.

**Hoạt cảnh/animation boss:** Idle (lơ lửng nhấp nhô), Nghiền (giơ nắm đấm → giáng), Mưa (giơ hai tay, mảnh đá bay lên), Giậm (Gai), Phóng quyền, Tụ lõi + Giải phóng, Kiệt sức (rũ người, lõi hở), Bị đánh (giật), Gầm (đổi phase), Chết (vỡ vụn).

---

## 3. BOSS THỦY — Cua Giáp Thủy Triều (concept cua xanh/trắng)

**Sân:** Đảo Hồ Thần — đảo tròn giữa hồ, không tường, biên là nước. **Cơ chế đặc trưng của map: TRIỀU (chu kỳ nước dâng/rút ~40 s).** Triều lên: vành ngoài đảo ngập nông (người chơi chậm 40%, cua nhanh gấp đôi trong nước), phần đất an toàn co lại tới ~60%. Triều rút: lộ bãi cát (thêm chỗ) và hang cua. **Danh tính chiến đấu:** *địa hình đổi liên tục, kiểm soát vị trí, đọc gợn nước* — người chơi phải **quản lý chỗ đứng theo thủy triều**, không thể "chạy vòng tròn" mãi. Nhịp nhanh hơn Địa, đòn ngắn hơn.

| # | Skill | Telegraph | Hiệu ứng | Né/đối phó |
|---|---|---|---|---|
| 1 | **Kìm Kẹp** | nón đỏ phía trước càng, báo 0.8 s | vung càng hai nhịp (kẹp trái → kẹp phải) + kẹp giữ chân 0.6 s nếu trúng cả hai | lùi chéo / đứng sau lưng sau nhịp 1 |
| 2 | **Bong Bóng Giam** | bóng nước hiện chậm bay về phía player | bong bóng chậm (3 đơn vị/s) nhốt player 1.5 s rồi nổ nếu chạm; phá bằng đòn đánh | đánh nổ sớm hoặc né |
| 3 | **Lặn Phục Kích** | gợn nước di chuyển bám theo player (tín hiệu), cuối cùng vòng đỏ bán kính 3 dưới chân 0.8 s | cua lặn xuống (vô hình, bất tử) rồi trồi lên nổ + hất tung | đi theo hướng đúng khi gợn nước tới gần |
| 4 | **Sóng Triều** | dải đỏ ngang một phía, báo 1.5 s, **có 1–2 chỗ hổng ở các tảng đá** | một đợt sóng quét qua đảo đẩy lùi + ngập tạm | đứng sau tảng đá / chỗ hổng |
| 5 | **Xoáy Nước** (tuyệt kỹ) | vòng đỏ lớn bao phủ 60% sân, báo 2 s | hút player về tâm xoáy 3 s, chạy thoát bằng sprint; xoáy tiếp tục vòi nước phun | chạy ra rìa đúng hướng; sau đó Recovery 4 s |

**Đợt (3 phase):** P1 Kìm Kẹp → Bong Bóng, Lặn Phục Kích → Sóng; P2 thêm Xoáy mỗi 3 Đợt và **triều dâng nhanh gấp đôi**; P3 "Triều Cường": đảo ngập chỉ còn 3 tảng đá an toàn nhỏ, cua phun vòi nước liên tục, người chơi nhảy giữa các tảng (di chuyển có chủ đích, né theo nhịp phun). Lợi thế: bộ skill **Phong** khắc Thủy (D-070 đề xuất).

---

## 4. BOSS PHONG — Cú Thần Thiên Phong (concept cú trắng-tím, vương miện)

**Sân:** Thiên Đài — đài trời lơ lửng không đối xứng, mép hư không. **Cơ chế đặc trưng của map: GIÓ & MÉP VỰC.** Gió đổi hướng theo chu kỳ 15–20 s (lá/lông bay chỉ hướng + mũi tên trên mây): người chơi bị trôi nhẹ theo gió (≈1 đơn vị/s), đợt gió mạnh đẩy về phía mép; rơi khỏi mép = mất lượng máu lớn rồi xuất hiện lại ở tâm (không chết ngay). Có vài cột đá/vòm gãy làm **ổ che gió**. Cú **bay khỏi khung hình** (bất khả xâm phạm) rồi lao xuống — pha *Bay* và pha *Đáp* quyết định lúc có thể đánh. **Danh tính chiến đấu:** *nhanh, di chuyển thẳng đứng, kiểm soát mép và gió* — người chơi cân bằng giữa né và không bị gió đẩy ra vực.

| # | Skill | Telegraph | Hiệu ứng | Né/đối phó |
|---|---|---|---|---|
| 1 | **Mưa Lông Vũ** | quạt đỏ mỏng 60°, báo 0.7 s | 9–15 lông phóng thành quạt, hơi dẫn hướng nhẹ | né ngang / nấp sau cột |
| 2 | **Lao Vuốt** | bóng cú lớn dần trên sân + vòng đỏ bán kính 3.5, báo 1.4 s (cú ở trên cao, ngoài khung hình) | lao xuống, nổ + đẩy lùi 4 đơn vị (nguy hiểm gần mép), sau đó **đáp xuống** 2 s (cửa sổ phản công) | chạy ra khỏi bóng |
| 3 | **Lốc Xoáy Nhỏ** | 2–3 vòng nhỏ đỏ xuất hiện, rồi lốc di chuyển tự do | lốc lang thang 6 s, chạm là hất tung + choáng ngắn | giữ khoảng cách, dùng cột chắn |
| 4 | **Tường Gió** | dải đỏ ngang cả sân với 1–2 **ổ che sau cột** tối, báo 1.5 s | tường gió quét ngang đẩy người chơi mạnh về mép vực | nấp sau cột / đi ngược hướng |
| 5 | **Bão Táp** (tuyệt kỹ) | trời tối dần, cú bay vòng trên cao | **các mảng sàn ngoài rìa vỡ dần** vào trong theo từng mảng (báo trước nhấp nháy), một "mắt bão" an toàn thu nhỏ; sau đó cú đáp kiệt sức 4 s | đứng trong mắt bão | 

**Đợt:** P1 Lông → Lao Vuốt (đáp), Lốc → Tường; P2 thêm Bão mỗi 3 Đợt, gió mạnh hơn, Lốc nhiều hơn; P3 Cú bay gần như liên tục, đáp ngắn hơn (1.3 s), Bão mỗi 2 Đợt. Lợi thế: bộ **Địa** khắc Phong (đề xuất).

---

## 5. So sánh để ba trận khác nhau thật sự

| | **Địa** | **Thủy** | **Phong** |
|---|---|---|---|
| Nhịp | chậm–vừa, đòn nặng | vừa–nhanh, đòn ngắn | nhanh, đòn từ trên cao |
| Map đóng vai trò | sân tĩnh, là vũ khí của boss | địa hình đổi theo thủy triều | hư không + gió cuốn |
| Kỹ năng người chơi được thử | đọc vùng, đi/chạy đúng hướng | quản lý vị trí theo triều, đọc gợn nước | giữ thăng bằng gió/mép, nấp cột |
| Cửa sổ phản công | Recovery sau Combo, khi nắm đấm xa | sau khi cua trồi lên/sau Xoáy | khi Cú đáp xuống |
| Tuyệt kỹ | sóng khe an toàn | xoáy hút + 3 tảng đá | mảng sàn vỡ + mắt bão |
| Khắc chế (đề xuất) | Thủy | Phong | Địa |

## 6. Animation & ngân sách Pixellab (còn 295 generation đến 2026-10-27)

- `animate_image` tính theo diện tích khung: khung boss 256×256 × 8 frame ≈ **8–9 generation/animation**. Địa cần ~10 animation (≈95 gen); ba boss ≈ 300 ⇒ **vượt ngân sách**.
- **Phương án đề xuất (B): tách lớp.** Thân (lơ lửng nhấp nhô bằng code) + hai nắm đấm + đầu làm sprite riêng; chỉ gen animation 4 frame cho tư thế đòn (≈ 4 gen) rồi nội suy bằng code (tween, squash/stretch, rung). Ước tính ≈ 50 gen/boss.
- Phương án A (đủ animation khung 256): ~95 gen/boss, đẹp nhất nhưng cần chờ reset.
- Phương án C (3 tư thế tĩnh + code): ~25 gen/boss, rẻ nhất.
- Khuyến nghị: làm **Địa theo B** để chốt quy trình, rồi quyết định tiếp.
- VFX đòn dùng lại tối đa kho hiện có (thiên thạch, gai, sóng, lốc hệ Địa/Thủy/Phong); bổ sung ít asset mới (nắm đấm bay, vòng sóng lõi, gợn nước, lông vũ).

## 7. Lộ trình triển khai (sau khi user duyệt)

1. `BossController` + `BossDefinitionSO` (Phase/Combo/Skill/Telegraph) + EditMode test cho trình tự (khóa phase theo % máu, thứ tự combo, Recovery). Kiểm tra Play Mode bằng boss thử (hộp).
2. Địa: 5 skill lần lượt, mỗi skill kiểm tra Play Mode + screenshot telegraph.
3. Art boss Địa (phương án B) + hoạt cảnh hiện ra/biến mất tượng.
4. Cân bằng (HP, B, thời gian) trong SO với bộ skill Thủy khi đánh Địa.
5. Lặp cho Thủy rồi Phong (mỗi boss kèm cơ chế map riêng: triều, gió/mép).

## 8. Điểm mở cần user quyết định

1. Duyệt danh tính 3 boss (Địa nặng-vùng; Thủy triều-địa hình; Phong gió-mép-bay).
2. Chấp nhận hệ số khắc chế nguyên tố (Thủy>Địa, Phong>Thủy, Địa>Phong, +25%)?
3. Phương án animation: A / B (khuyến nghị) / C.
4. Mức phạt rơi khỏi mép (Phong): % máu hay chỉ mất lượt?
5. Chấp nhận `BossController` mới thay vì mở rộng `EnemyUniversal`?
