# Kiểm tra combat 3 boss (Earth / Water / Wind) — 2026-10-05

Phạm vi: bản chơi thử hiện tại của `EarthGolem`, `WaterCrab`, `WindOwl` (`BossController` + `BossSkills*.cs`). Nguồn số liệu: (1) `Assets/Scripts/Debug/BossCombatAudit.cs` chạy Play Mode (cast từng skill ở phase 1 và phase 3, người chơi lv1 đứng yên, ghi thời lượng + sát thương; log `Logs/BossCombatAudit.txt`); (2) ảnh chụp Play Mode; (3) đọc code. **Giới hạn của phép đo:** người chơi đứng yên nên không đo được độ né được; skill "bắn rồi bỏ" (Lốc Xoáy, Lưỡi Gió, Bão Táp) kết thúc coroutine trước khi vật thể bay tới nên số "sát thương" thấp ≠ vô hại. Cân bằng thật cần người chơi thật.

## 1. Số đo (người chơi 100 HP, giáp 2, đứng yên)
| Boss | Skill | Thời lượng | Sát thương | Ghi chú |
|---|---|---|---|---|
| Earth | Slam / SlamDouble | 1.4 / 2.8 s | 12 / 24 | |
| Earth | StoneRain / SpikeLanes | 3.0 / 2.0 s | 15.6 / 10.6 | |
| Earth | RocketFist / CoreResonance | 5.4 / 6.8 s | 22.6 / 27.6 | |
| Water | ClawClamp P1→P3 | 4.2 → 5.2 s | 27.6 → 36.8 | |
| Water | BubbleTrap | 5.3 s | **0** | người đứng yên không bị trúng |
| Water | SandAmbush / TidalWave | 4.4 / 4.1 s | 14.8 / 14.8 | |
| Water | Whirlpool P1→P3 | 10 s | 45.9 → **73.5** | gần giết người đứng yên |
| Wind | FeatherVolley / TalonDive | 1.5 / 4.5 s (P3: 3 / 7.8 s) | 15 / 12 (P3: 10 / 43) | |
| Wind | GaleWall | 6 s | 9.2 | |
| Wind | Cyclones / CrescentBlades | 1.7 / 1.6 s | 0 (đo lỗi: vật thể bay sau khi skill kết thúc) | |
| Wind | **SkyStorm** | 7 s | **0** | **lỗi thiết kế, xem 2.1** |

Mọi skill cùng tên có sát thương **giống nhau ở mọi phase** (Earth hoàn toàn bằng nhau; Water chỉ ClawClamp/Whirlpool đổi): độ khó tăng chủ yếu nhờ báo trước ngắn hơn, combo dày hơn và nghỉ ngắn hơn — chưa có biến thể skill theo phase (thêm đòn, đổi pattern).

## 2. Lỗi / thiếu sót cần sửa trước (đã có bằng chứng)
1. **Bão Táp (Wind) vô hiệu với người đứng giữa.** Chỉ các cánh ngang vỡ, cánh dọc (cột giữa) không bao giờ vỡ ⇒ chỗ an toàn vĩnh viễn; "mắt bão thu nhỏ" trong kế hoạch chưa được dựng. Đây là tuyệt kỹ nhưng không đe doạ.
2. **Lốc Xoáy / Lưỡi Gió / Lông Vũ bắn rồi quên:** coroutine skill kết thúc trước khi vật thể tới, nên `Combo` chuyển skill kế tiếp và Recovery bắt đầu **trong lúc vật thể còn bay** — người chơi nhìn thấy "boss đã nghỉ nhưng đòn vẫn đang trúng". Pro game thường giữ boss ở trạng thái cast đến khi đòn kết thúc hoặc đánh dấu rõ.
3. **Không có miễn thương sau khi trúng đòn** (`PlayerStat.IsInvulnerable` chỉ dành cho dash): chuỗi nhiều đòn nhỏ (Whirlpool 5–8 lần, mưa lông vũ, Claw 3–4 lần) cộng dồn trong vài chục ms. Pro game cho 0.4–0.8 s i-frame + nháy sprite sau mỗi đòn.
4. **Phản hồi trúng đòn của người chơi nghèo:** chỉ tiếng + đẩy lùi + animation; không nháy đỏ sprite, không viền màn hình đỏ, không rung nhẹ theo mức sát thương, không hit-stop 40–60 ms.
5. **Không có hiển thị sát thương gây lên boss** (không số bay, không tia lửa tại điểm trúng, boss chỉ nháy đỏ 0.08 s + tiếng). Người chơi không "thấy" đòn của mình có tác dụng; không có số liệu để cân bằng.
6. **Mây trôi trước mặt người chơi (Wind) che telegraph:** ảnh chụp cho thấy mây alpha 0.4–0.6 phủ lên vòng báo đỏ và boss. Cần giảm alpha khi đang đánh hoặc để mây dưới lớp telegraph (order 95 > telegraph).
7. **Ba boss dùng một kiểu telegraph đỏ chung** (`BossTelegraph` tròn/đường), không có: màu theo loại (né bằng dash được / phải ra khỏi vùng / phải nấp), đường viền hoa văn theo hệ, âm thanh báo hiệu nhất quán. Dễ đọc nhưng "trông như prototype".

## 3. Đối chiếu với combat boss của game chuyên nghiệp (Hades, Titan Souls, Hollow Knight, Dead Cells…)
| Yếu tố | Hiện trạng | Mức |
|---|---|---|
| Đọc được đòn (báo trước, màu, âm) | có telegraph đỏ + SFX riêng từng skill; chưa phân loại màu | khá |
| Nhịp (Combo → Recovery → cửa sổ phản công) | có, HP bar đổi màu vàng khi Recovery | tốt |
| Hit feedback lên boss (số, tia lửa, hit-stop, nhấp nháy mạnh, rung sprite) | chỉ nháy 0.08 s | **yếu** |
| Hit feedback lên người chơi (i-frame, nháy, viền màn hình) | chỉ tiếng + đẩy | **yếu** |
| Biến thể theo phase (đổi pattern, thêm đòn, thay đổi arena) | chủ yếu đổi tốc độ/combo; Water có triều, Wind có sàn vỡ | trung bình |
| Cinematic mở/đóng (giới thiệu boss, tên, đổi phase, chết) | zoom nghi lễ + gầm + flash; chết = mờ dần + hạt | trung bình |
| Animation boss | Earth 8 clip, Water 9 clip cắt lớp, Wind 12 clip (8 clip AI) | khá, không đồng đều giữa 3 boss |
| VFX skill đồng nhất phong cách | Earth/Wind dùng bộ skill của người chơi; Water sinh riêng | khá |
| Poise/stagger (đánh đủ → boss choáng) | không có | thiếu (tuỳ chọn) |
| Chỉ báo chiến thuật (mũi tên khi boss ngoài màn hình, vùng an toàn xanh) | có OffscreenIndicator; vùng xanh chỉ ở Tường Gió & Resonance | trung bình |

## 4. Đề xuất — nhóm làm bằng code (0 generation)
A. **Sửa lỗi 2.1–2.3:** Bão Táp có "mắt bão" thu nhỏ (cả cánh dọc vỡ dần từ ngoài vào, chỉ chừa vòng tròn an toàn), boss giữ trạng thái cast tới khi vật thể kết thúc, i-frame 0.5 s + nháy sprite sau khi trúng.
B. **Hit feedback:** số sát thương bay (màu theo loại/ chí mạng), tia lửa/vòng sáng tại điểm trúng, hit-stop 40–60 ms khi trúng boss, rung sprite boss, viền màn hình đỏ + rung khi người chơi trúng.
C. **Telegraph v2:** màu theo loại (đỏ = phải thoát, vàng = dash xuyên được, xanh = chỗ an toàn), viền hoa văn theo hệ, nhịp "ping" âm thanh nhất quán, giảm alpha mây Wind trong lúc đánh.
D. **Biến thể theo phase** (Earth +đòn mới ở P3, Water thêm tia nước ở Bubble, Wind thêm lưỡi gió thứ 3), và **đồng bộ số phase trong dữ liệu**, không đổi hành vi hiện có.
E. **Công cụ:** giữ `BossCombatAudit` làm bài kiểm tra hồi quy; thêm bot né cơ bản (di chuyển theo vùng đỏ) để đo "độ né được".

## 5. Đề xuất — asset cần gen / dựng (còn ≈ 29 generation Pixellab đến 2026-10-27)
| Asset | Dùng cho | Cách làm | Ước tính |
|---|---|---|---|
| Hit-spark theo hệ (3 vòng sáng ngắn: đá / nước / gió) | tia lửa khi trúng boss / người chơi | `animate_image` 64 px, 8 frame | ~3 gen |
| Decal telegraph theo hệ (vòng rune đất, vòng sóng nước, vòng lông vũ gió) | thay vòng đỏ trơn | dựng bằng code từ sprite có sẵn + 1 ảnh Pixen/hệ | ~3 gen |
| Hào quang "cast" ở tay/cánh/càng (3 loop nhỏ) | báo boss đang tụ lực | `animate_image` 48–64 px | ~3 gen |
| Hiệu ứng chết boss (nổ vỡ theo hệ) | cảnh kết thúc | tái dùng bộ skill + hạt, ít gen | 0–3 gen |
| Clip còn lại của cú (Perch, Circle, Screech, Death) bằng AI | đồng bộ chất lượng | `animate_image` | 32 gen (**vượt ngân sách**: chọn 1–2 clip) |
| Clip bổ sung cho Earth/Water (đã đủ clip) | — | không cần | 0 |
Ngân sách gợi ý: hit-spark + decal + aura ≈ 9–12 gen, còn dư ≈ 17 gen cho 2 clip cú (Screech, Death).

## 6. Trạng thái
Báo cáo này chưa thay đổi gameplay. Chờ user chọn nhóm A–E / asset cần làm (khuyến nghị: **A → B → C**, rồi gen asset mục 5, rồi D).

## 7. Asset đã gen (2026-10-05, Pixellab Pixen + animate_image, D-081)
Thư mục `Assets/Resources/VFX/Skills/Combat/` (import bằng `Tools > Project Game > VFX > Import Boss Combat Sheets`, `BossCombatVfxImporter.cs`; ảnh gốc/ứng viên ở `Assets/Art/BossCombat/`):
| Asset | File | Định dạng | Ghi chú |
|---|---|---|---|
| Hit-spark Địa / Thủy / Phong | `HitSpark_{Earth,Water,Wind}.png` | dải 64 px, 9 / 9 / 7 frame, PPU 32 | Phong bỏ frame 3 (đốm xám xấu) |
| Hào quang cast Địa / Thủy / Phong | `CastAura_{Earth,Water,Wind}.png` | dải 64 px, 9 frame, lặp | quả cầu năng lượng, đặt ở tay/càng/cánh khi báo |
| Vòng telegraph Địa / Thủy / Phong | `TelegraphRing_{Earth,Water,Wind}.png` | 128 px đơn, PPU 16 | rune đá / sóng nước / lông vũ, viền cam báo nguy hiểm; Thủy và Phong bị AI vẽ lấp tâm nên đã khoét rỗng tâm bằng mặt nạ bán kính (0 generation) |
Chi phí: 9 Pixen + 6 animate_image = **15 generation** (còn ≈ 14 đến 2026-10-27). **Chưa nối vào game** — chờ làm nhóm B/C ở mục 4 (số sát thương/hit-spark/hit-stop và telegraph v2 sẽ dùng các asset này).

## 8. Đã làm theo nhóm A–C (2026-10-06) và đo lại
**Code mới/đổi:** `CombatFeedback.cs` (số sát thương bay `DamageNumber`, hit-spark theo hệ trên boss và trên người chơi, hit-stop 35–55 ms, nháy đỏ + chớp sprite + viền màn hình đỏ + rung khi người chơi trúng), `Player._hurtInvulnerabilitySeconds` (**0.45 s miễn thương sau mỗi đòn**, chỉnh trong Inspector, 0 = tắt; `Player.LastHurtTime`), `BossTelegraph.SetElement` (vòng art Địa/Thủy/Phong quay chậm quanh vùng báo tròn), `BossController.ShowCastAura` (hào quang cast mỗi khi bắt đầu skill), `SkyWeather` (mây mờ ×0.35 khi đang đánh boss), **Bão Táp "mắt bão"** (`BossDefinition.stormEyeRadii` = 17 → 13 → 9.5 → 6.5; sàn ngoài mắt vỡ dần từ rìa, overlay cắt theo hình platform bằng `SpriteMask`; `stormTierRects` giữ lại nhưng không dùng), cú giữ cast tới khi lông vũ/lưỡi gió bay hết (`WaitCastHazards`).

**Đo lại bằng `BossCombatAudit` (người chơi đứng yên, trước → sau):**
| Skill | Trước | Sau | Ý nghĩa |
|---|---|---|---|
| Wind SkyStorm | 0 | **40** (8 lần) | đứng giữa không còn an toàn; chỉ vòng "mắt" an toàn |
| Wind CrescentBlades | 1.6 s / 0 | 4.3 s / 12 | boss giữ cast tới khi lưỡi bay hết |
| Wind FeatherVolley | 1.5 s | 2.2 s | idem |
| Water Whirlpool P3 | 73.5 (8 lần) | 45.9 (5 lần) | miễn thương 0.45 s chặn cộng dồn |
| Water BubbleTrap | 0 | 6.8 | (do vị trí bubble ngẫu nhiên; vẫn thấp) |
| Earth/Water còn lại | — | không đổi | |
Không còn skill nào "HANG". **Chưa kiểm chứng bằng chơi thật:** cảm giác i-frame 0.45 s (chỉnh số này nếu thấy quá dễ/khó), độ rõ của số sát thương, hit-stop, vòng art; cần user thử. **Chưa làm:** nhóm D (biến thể skill theo phase) và telegraph đổi màu theo loại đòn (vàng = dash xuyên được, xanh = chỗ an toàn) — để lượt sau.

### 8.1 Telegraph theo loại đòn (2026-10-06)
`BossTelegraph.Line(..., dashable)`: **đỏ** = vùng phải thoát ra ngoài; **hổ phách** (`BossTelegraph.Dashable`) = đường/làn đòn lướt qua mà người chơi có thể **dash xuyên** (Spike Lanes, Rocket Fist, làn Bong Bóng, Sóng Triều, Tường Gió); **xanh** (`BossTelegraph.Safe`) = chỗ an toàn (đã có ở Tường Gió, Resonance). Vòng tròn (Slam, mưa đá, Lao Vuốt…) giữ màu đỏ. Chưa làm: biến thể skill theo phase (nhóm D).
