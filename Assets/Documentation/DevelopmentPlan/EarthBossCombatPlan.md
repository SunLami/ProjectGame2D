# Combat Boss Earth — thiết kế lại có Dash (D-086)

Trạng thái: **M1-M3 xong và kiểm chứng trong Play Mode (2026-10-03)**; còn M4 (animation tư thế) và M5 (cân bằng). Thay thế phần Địa của `BossEncounterConcepts.md` §2 (bản cũ viết khi player chưa có dash). Boss Thủy/Phong chưa làm.
Sprite boss đã chốt: `Assets/Art/BossArena/Earth/Boss/EarthGolem_Final_256.png` (golem đá có sừng cong, lõi xanh ở ngực).

## 1. Khả năng của player (nguồn: code + D-085)

| | Giá trị |
|---|---|
| Đi bộ / chạy | 2 / 4 đơn vị/s (chạy hao 25 stamina/s) |
| Dash | 3.2 đơn vị trong 0.2 s (16 đơn vị/s), hồi 0.8 s, 20 stamina, **miễn sát thương 0.12 s đầu** |
| Stamina | 100, hồi 18/s ⇒ tối đa ~5 dash liên tiếp, hồi 1 dash ≈ 1.1 s |
| Máu | 100 (+3/cấp ⇒ ~124 ở cấp 9); đòn đánh cơ bản 10 |

**Thời gian thoát vùng tròn bán kính R** (dash + chạy): `0.2 + (R − 3.2)/4` ⇒ R=3.6 → 0.30 s; R=5 → 0.65 s. Thêm phản xạ ~0.35 s ⇒ báo hiệu ≥ **0.9 s cho vùng vừa, ≥ 1.2 s cho vùng lớn**; i-frame 0.12 s tạo cửa sổ "né sát" (dash đúng lúc đòn chạm) — kỹ năng nâng cao, không bắt buộc.
Đòn của boss không được đòi quá **2 dash** trong một Combo để stamina không cạn.

## 2. Nguyên tắc thiết kế (rút từ dash)

1. Mọi đòn **đọc được bằng telegraph** (vòng/đường/cung đỏ có nền đầy dần); khóa mục tiêu ở đầu rồi đứng yên.
2. Đòn nhắm vị trí player dùng **vị trí dự đoán** (vận tốc × 0.5 s) để phạt việc chạy thẳng đều, không phạt dash.
3. Đòn dạng sóng/vòng có **độ dày ≈ quãng đường dash trong 0.12 s** (≈ 1.1 đơn vị ở tốc độ sóng 9) để dash i-frame có thể "xuyên" — kỹ năng nâng cao.
4. Cửa sổ phản công (**Recovery**) rõ và đủ dài để dùng skill; boss nhận thêm sát thương.
5. Chiến đấu diễn ra trên toàn sân; boss di chuyển lơ lửng chậm hơn tốc độ chạy (2.4 < 4 đơn vị/s).

## 3. Chỉ số mặc định (ScriptableObject, chỉnh một chỗ — user: "sát thương config sau")

- `B` (sát thương cơ sở) = **14**; HP boss **2400** (mục tiêu ~4–5 phút, player ~45% thời gian trong cửa sổ đánh, DPS bền ~20).
- Hệ số sát thương theo đòn nhân với `B`. Boss nhận sát thương ×1.25 trong Recovery (×1.5 sau tuyệt kỹ), ×1.15 khi nắm đấm đang bay.
- CC: Làm chậm hiệu lực 50%; Choáng chỉ ăn trong Recovery (kéo dài thêm tối đa 1 s/lần); Kéo/Hất tung miễn nhiễm.

## 4. Năm skill

| # | Skill | Telegraph (P1 → P3) | Hiệu ứng / sát thương | Ghi chú dash |
|---|---|---|---|---|
| 1 | **Thiết Quyền Nghiền** | vòng bán kính 3.6; 1.0 → 0.8 s; khóa mục tiêu sau 0.15 s | nổ + choáng 0.4 s; **1.0 B**. Biến thể *kép*: cú 2 nhắm vị trí mới sau 0.5 s, báo 0.7 s | dash thoát vòng; dash i-frame đúng lúc đòn chạm = né sát |
| 2 | **Mưa Đá Vỡ** | 6/9/12 vòng bán kính 2.0, báo 0.9 s, cách nhau 0.22 s; 2 vòng đầu nhắm vị trí dự đoán, còn lại ngẫu nhiên quanh player ≤ 9 (cách nhau ≥ 2.4) | đá rơi; **0.7 B** mỗi viên (dùng VFX thiên thạch hệ Địa) | đi len giữa các vòng; không cần dash |
| 3 | **Địa Mạch Gai** | 3/5/7 vệt cách nhau **22°** (khe đi được ≥ 1.6 ở cách boss 8 đơn vị), rộng 1.4, dài 22, báo 0.9 s | gai trồi dọc vệt (tốc độ lan 40 đơn vị/s); **0.9 B**, gai còn nguy hiểm khi còn hiển thị (~1.2 s) nhưng mỗi lần cast chỉ trúng player một lần (2026-10-03: trước đó chỉ trúng lúc trồi nên đi xuyên qua gai không sao); làm chậm player chưa có hook nên bỏ | bước ngang 1 dash là qua khe |
| 4 | **Phi Quyền** | vệt rộng 2.0 tới mép sân, báo 0.8 s | nắm đấm phải bay 24 đơn vị/s: **1.3 B**, rồi quay về thẳng sau 3 s: **0.6 B**; boss nhận ×1.15 khi nắm đấm xa | né sang bên cả hai chiều |
| 5 | **Cộng Hưởng Lõi** (tuyệt kỹ, đổi 2026-10-03 — D-095) | boss bay về tâm sân, lõi tụ 1.8 s; rồi **4 / 5 / 6 đợt đạn đá** (theo phase; tuyệt kỹ chỉ xuất hiện từ P2) cách nhau 0.9 s, mỗi đợt **20 / 24 / 28 viên bắn toả đều 360°**, tốc độ 8, mỗi đợt xoay lệch nửa bước nên làn an toàn đổi chỗ; nan hoa đỏ báo 0.5 s trước mỗi đợt | mỗi viên **0.8 B**, mỗi đợt chỉ trúng player một lần; sau đó **Recovery 4.5 s ×1.5** | đứng giữa hai viên (khe ≈ 2.6 đơn vị ở bán kính 10 với 24 viên), đổi làn khi đợt sau lệch nửa bước, hoặc dash i-frame xuyên qua |

  *(Bản cũ — vòng sóng khe an toàn 40° — bị bỏ vì user thấy gần như không né được; code `ResonanceRing` vẫn còn nhưng không dùng.)*

## 5. Dòng chiến đấu theo đợt (phase)

- **Đợt (wave)** = Combo (2–4 skill, nghỉ giữa skill 0.6 s → 0.4 s ở P3) → **Recovery** → boss bay lại vị trí → Đợt kế.
- **P1 (100–65%):** `A = Nghiền → Gai`, `B = Mưa → Nghiền`, xen kẽ; Recovery 3.0 s.
- **P2 (65–30%):** gầm chuyển phase 2 s (bất tử, vòng rune sàn bùng sáng); báo hiệu ×0.9; `C = Phi Quyền → Gai → Nghiền`, `D = Mưa → Phi Quyền`, `E = Nghiền kép → Gai`; **Cộng Hưởng Lõi sau mỗi 3 Đợt**; Recovery 2.5 s.
- **P3 (<30%, nứt xanh lan rộng):** báo hiệu ×0.8 (tối thiểu 0.6 s); `F = Mưa + Gai đồng thời → Phi Quyền`, `G = Cộng Hưởng → Nghiền → Nghiền`; tuyệt kỹ sau mỗi 2 Đợt; Recovery 1.8 s.
- Mở rộng (đã chốt "đánh lại để farm"): hệ số độ khó (tốc độ, HP, telegraph) nằm trong SO.

## 6. Kiến trúc (Data-Driven, không sửa `EnemyUniversal`)

```
Assets/Scripts/Boss/
  BossDefinition.cs     ScriptableObject: HP, B, tốc độ bay, phases[] {ngưỡng, báo hiệu ×, recovery, combos[]}
  BossController.cs     IDamageable/ISlowable/IStunnable/IVulnerable; trạng thái Spawning→Fighting→Dead; bộ định thời Phase→Đợt→Combo→Recovery
  BossSkills.cs         5 coroutine skill + helper gây sát thương lên Player
  BossTelegraph.cs      vòng/đường/cung đỏ vẽ bằng code (0 generation)
  BossHealthBarUI.cs    thanh máu trên cùng màn hình
  BossEncounterTestHarness.cs   (DemoScene) phím F6 gọi boss để thử
Assets/Prefabs/Bosses/EarthGolemBoss.prefab   (dựng bằng Editor script)
Assets/Data/Bosses/EarthGolemBoss.asset
```
- Sát thương lên player qua `Player.TakeDamage(dmg, dir, knockback)` (đã tôn trọng i-frame dash); mọi vùng đánh dùng `Physics2D.OverlapCircle/Box` và lọc `Player`.
- VFX: dùng lại kho hệ Địa (`Resources/VFX/Skills/Earth`: `Meteor_Fall`, `MeteorCrater_Smolder`, `RockSpike_Rise`, `Shockwave_Expand`, `EarthRune_Pulse`, `RockChunk_Hover`, `StunStar`) qua `SkillFrameAnimator`.
- Animation boss (giai đoạn sau, phương án B tách lớp): hiện tại thân nhấp nhô + squash/stretch + rung bằng code, nắm đấm Phi Quyền là sprite riêng.

## 6B. Kết quả triển khai & kiểm chứng (2026-10-03)

Code: `Assets/Scripts/Boss/` (`BossDefinition`, `BossController` + `BossSkills`, `BossTelegraph`, `BossHealthBarUI`, `BossEncounterTestHarness`), `Assets/Editor/BossPrefabBuilder.cs`, asset `Assets/Bosses/EarthGolem/EarthGolemBoss.{prefab,asset}`, harness trong **DemoScene** (F6 gọi boss, F7 xóa).
- **Play Mode (đo sát thương thật lên Player, B=14, giáp 2):** Slam −12 + hất lùi 2.4; Stone Rain 2 viên nhắm vị trí dự đoán −15.6; Spike Lanes 3 vệt, trúng một lần −10.6; Rocket Fist đi −16.2 / về −6.4 và boss nhận ×1.15 khi nắm đấm xa; Core Resonance mỗi vòng −13.4.
- **Dòng đợt (log `SkillStarted/RecoveryStarted/PhaseChanged`):** P1 `Slam→Gai`, Recovery 3 s ×1.25, `Mưa→Slam`; ngưỡng 65% ⇒ gầm 2 s ⇒ P2 `Phi quyền→Gai→Slam`, `Mưa→Phi quyền` (Recovery 2.5 s); ngưỡng 30% ⇒ P3 `Mưa+Gai song song→Phi quyền`, Recovery 1.8 s, rồi `Cộng hưởng lõi`. Vượt ngưỡng giữa chừng thì cắt combo, chuyển phase sau Recovery.
- **Chết:** hit-stop + vỡ vụn, dọn sạch telegraph/nắm đấm/vòng sóng/thanh máu, trả `timeScale`.
- **EditMode:** `BossDefinitionTests` 5 test (11 ca) đạt — ngưỡng phase, `PerPhase`, khe an toàn của hàng gai, thứ tự telegraph/recovery theo phase.
- **Chưa làm / ghi nhận:** (1) animation tư thế — hiện boss nhấp nhô + co giãn bằng code; (2) làm chậm player (gai) chưa có hook `ISlowable` ở Player; (3) choáng player = animation bị đánh (`_isHit`); (4) i-frame dash "né sát" chưa có thưởng (điểm mở §8.1); (5) Shrine/hiện từ tượng chưa nối (`BeginEncounter()` đã có hiệu ứng hiện dần); (6) cân bằng HP/B với level 1–9.

## 7. Lộ trình & tiêu chí

| Bước | Nội dung | Điều kiện qua |
|---|---|---|
| M1 | Khung (`BossController`, SO, telegraph, health bar) + boss thử spawn bằng F6 | Boss hiện, nhận sát thương, thanh máu chạy, Recovery nhận ×1.25 |
| M2 | 5 skill + phase/đợt theo §5 | Play Mode: từng skill đúng telegraph/thời gian/sát thương; combo chạy hết P1→P3 |
| M3 | EditMode test cho bộ định thời (ngưỡng phase, thứ tự combo, Recovery) | Test đạt |
| M4 | Art: hiện ra từ tượng (đã có), animation tư thế (phương án B) | — |
| M5 | Cân bằng B/HP/thời gian với player cấp 1–9 + dash | User duyệt |

## 8. Điểm mở

1. **Né sát thưởng gì?** Đề xuất (chưa làm): i-frame dash trùng lúc trúng đòn ⇒ hoàn 10 stamina + slow-mo 0.15 s (có `SkillScreenFX.HitStopAndSlowMo`). Cần user duyệt vì đổi cân bằng stamina.
2. Phạt chết: boss reset + tượng triệu hồi hiện lại (thiết kế cùng Shrine UI).
3. Khắc chế nguyên tố (Thủy>Địa +25%) vẫn chờ chấp nhận (D-070).
