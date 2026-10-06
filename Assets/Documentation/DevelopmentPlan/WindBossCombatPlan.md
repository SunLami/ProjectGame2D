# Combat Boss Wind — Cú Thần Thiên Phong (ĐÃ DUYỆT 2026-10-05, D-111)

Trạng thái: **user duyệt cả 4 điểm §9 (2026-10-05): 6 skill + gầm đổi phase, mép vực = va đập, gió trôi người chơi thật (0.8/1.1/1.45), HP 2400 / B 14 / Bão Táp làm vỡ cả hai cánh ngang; đã code — xem §10.** Map `BossArena_Wind` đã dựng (D-110, `BossArenaMapDesign.md` §17). User yêu cầu: gen boss Cú Mèo, animation boss, bộ skill "xịn sò", rồi animation cast cho từng skill. 
Concept: cú trắng-xanh-lavender, vương miện gạc gỗ có ngọc trăng khuyết xanh, mắt phát sáng, hai sợi ria lông dài, dây chuyền gỗ + tinh thể (ảnh user 2026-10-05). Tượng triệu hồi trong map đã dùng cùng thiết kế.
Phong cách đánh: **nhanh, bay, kiểm soát gió và mép sân**; khắc chế bởi Địa (đề xuất D-070, chưa Accepted).

## 1. Danh tính chiến đấu
- Water = địa hình đổi (triều). **Wind = không gian thẳng đứng + hướng gió**: cú *lên trời* (miễn nhiễm, ra khỏi khung) rồi *lao xuống*; gió thổi trôi người chơi; hình chữ thập của sân làm các cánh ngoài là nơi nguy hiểm.
- Cửa sổ phản công rõ ràng: **chỉ đánh được tốt khi cú đáp xuống** (Lao Vuốt, Bão Táp) hoặc hồi sức (Recovery).

## 2. Cơ chế riêng của map (nhẹ, không phá save)
- **Luồng gió (ArenaWind): CHỈ LÀ HIỆU ỨNG (user bỏ việc trôi, 2026-10-05).** Hướng gió đổi 15–20 s, mây/lá trong `SkyWeather` bay theo hướng đó; người chơi KHÔNG bị gió đẩy. (Thiết kế cũ — đã bỏ: người chơi bị trôi ≈ 0.8 đơn vị/s theo gió (dịch `Rigidbody2D.position`, cùng cách `PullPlayer` của Water; **không cần hook mới**). P2 ×1.4, P3 ×1.8.)
- **Mép vực = va chạm, không rơi.** Sân đã có `EdgeCollider2D` chặn người chơi nên không có hố để rơi. Bản đầu: **bị gió/đẩy ép vào mép gây sát thương va đập** (0.4 B nếu bị đẩy mạnh vào mép) thay vì "rơi và hồi sinh" của `BossEncounterConcepts.md` §4. *Cần user duyệt thay đổi này* (rơi cần teleport/hồi sinh người chơi = động vào tiến trình).
- Bốn **trụ gió** (đã đặt ở ±22) là **ổ che** cho Tường Gió; boss đi vòng qua (`BossObstacle`, D-106).
- Boss chỉ đi trong hình chữ thập (D-110 `ArenaPolygon`).

## 3. Chỉ số mặc định (SO, chỉnh một chỗ — giống Earth/Water)
HP **2400**, `B` = 14; nhận sát thương ×1.25 trong Recovery (×1.5 sau Bão Táp); làm chậm 50%; choáng chỉ trong Recovery; kéo/hất tung miễn nhiễm; **khi bay (Lên Trời) miễn sát thương**. Telegraph tối thiểu 0.8 s (vùng vừa) / 1.2 s (vùng lớn).

## 4. Sáu skill (mỗi skill có clip cast riêng)
| # | Skill | Cú làm gì | Telegraph → hiệu ứng | Né / đối phó |
|---|---|---|---|---|
| 1 | **Mưa Lông Vũ** (`Volley`) | giương hai cánh ra sau rồi hất về trước, lông bay thành quạt; P2 hai quạt chéo nhau; P3 xoắn ốc | quạt đỏ mỏng 60° báo 0.7 s; 9 → 15 → 21 lông, hơi dẫn hướng; lông cắm xuống sàn rồi tan | né ngang giữa các lông, dash xuyên khe |
| 2 | **Lao Vuốt** (`Dive`) | vỗ cánh bay lên (miễn nhiễm, ra khỏi khung), bóng cú lớn dần trên sân, rồi lao xuống; P3 lao 3 điểm liên tiếp | vòng đỏ r 3.5 báo 1.4 s dưới chân người chơi (khóa vị trí 0.4 s cuối); nổ + hất 4 đơn vị (nguy hiểm gần mép); sau đó **đáp 2 s** | chạy ra khỏi bóng; sau khi đáp: **cửa sổ đánh** |
| 3 | **Lốc Xoáy Nhỏ** (`Cyclones`) | hai cánh vỗ nhanh xen kẽ, thả 2–3 lốc con | vòng đỏ nhỏ báo 0.8 s tại chỗ lốc sinh ra; lốc đi lang thang + nghiêng dần về người chơi trong 6 s; chạm: hất tung + choáng ngắn | giữ khoảng cách, đi vòng qua trụ che |
| 4 | **Tường Gió** (`GaleWall`) | dang cánh ngang, nghiêng người rồi quét | dải đỏ ngang cả sân báo 1.5 s, **hiện rõ các "bóng" sau trụ gió là chỗ an toàn**; tường quét 14 đơn vị/s, đẩy mạnh về mép + sát thương va đập | nấp sau trụ, hoặc chạy ngược chiều và dash |
| 5 | **Lưỡi Gió Xoay** (`Crescents`) — *mới* | xoay người, hai cánh chém chéo, hai/ba lưỡi liềm gió | hai đường cung đỏ báo 0.9 s; lưỡi bay vòng cung từ ngoài vào rồi quay lại (boomerang), 1.0 B, dày 1.4 | đứng trong tâm cung hoặc dash đúng nhịp quay lại |
| 6 | **Bão Táp** (`Storm`, tuyệt kỹ) | bay vòng quanh sân, trời tối dần, gió nổi | **các đoạn ngoài rìa của hình chữ thập vỡ dần** (nhấp nháy báo 1.2 s mỗi đoạn, từ ngoài vào trong); vùng vỡ = vùng sát thương + gió đẩy; **"mắt bão"** an toàn ở tâm thu nhỏ dần; sau đó cú **đáp kiệt sức 4 s** (×1.5 sát thương nhận) | đứng trong mắt bão, nhớ thứ tự vỡ |

Gầm đổi phase: **Tiếng Hót Thiên Âm** (bất tử 2 s): vòng sóng âm lan ra đẩy người chơi ra xa + clip `Screech`.

## 5. Đợt (3 phase)
- **P1 (100–65%):** `A = Mưa Lông Vũ → Lao Vuốt (đáp)`, `B = Lốc Xoáy → Tường Gió`; Recovery 3.0 s; gió ×1.
- **P2 (65–30%):** gầm; gió ×1.4; `C = Lưỡi Gió → Mưa Lông Vũ`, `D = Tường Gió + Lốc Xoáy (song song)`, `E = Lao Vuốt → Lao Vuốt → Lưỡi Gió`; **Bão Táp sau mỗi 3 Đợt**; Recovery 2.5 s.
- **P3 (<30%):** gió ×1.8; báo ×0.8 (tối thiểu 0.6 s); `F = Lao Vuốt ba điểm`, `G = Bão Táp → Lưỡi Gió`; Bão Táp sau mỗi 2 Đợt; đáp ngắn 1.3 s; Recovery 1.8 s.

## 6. Art và animation (cắt lớp từ MỘT ảnh Pixen, miễn phí như cua D-105)
Cú gen một lần ở tư thế **hai cánh dang rộng đối xứng** (nhìn từ trên-trước) để tách lớp: thân, đầu (+ gạc), cánh trái, cánh phải, đuôi, hai chân. Clip (canvas lớn hơn sprite, 9 frame, PPU như cua):

| Clip | Dùng cho | Chuyển động |
|---|---|---|
| `Idle` | đứng/lơ lửng | cánh nhịp chậm, thân nhấp nhô |
| `Move` | đuổi/đổi chỗ | vỗ cánh nhanh, thân nghiêng |
| `Volley` | Mưa Lông Vũ | cánh ra sau, hất mạnh về trước, giữ, thu |
| `TakeOff` | Lao Vuốt (lên trời) | cúi người, cánh đập mạnh, thân vút lên + nhỏ dần + mờ |
| `Dive` | Lao Vuốt (xuống) | cánh khép, thân lao xuống, phóng to dần |
| `Perch` | đáp/kiệt sức/Recovery | cánh buông, đầu cúi, thở |
| `Flap` | Lốc Xoáy | hai cánh vỗ xen kẽ nhanh, thân lắc |
| `Sweep` | Tường Gió | dang cánh ngang, nghiêng thân, quét |
| `Slash` | Lưỡi Gió Xoay | xoay thân, hai cánh chém chéo |
| `Circle` | Bão Táp | bay nghiêng, cánh dang, thân xoay |
| `Screech` | gầm đổi phase | ngửa đầu, cánh dang, rung |

**Đã làm (2026-10-05):** base cú `Assets/Art/BossArena/Wind/Boss/OwlBase_256.png` (Pixen seed 1202, 4 generation cho 4 ứng viên) và **12 clip** `Boss/Anim/Owl_<Clip>_17f.png` (17 frame 384×384; bản đầu 9 frame bị user nhận xét "chưa mượt" nên dựng lại: `Tools/owl_render.py` tự render cục bộ — keyframe trong `Tools/owl_recipe.py`, nội suy Catmull-Rom tạo frame trung gian, xoay ở 4× để giữ cạnh pixel, 0 generation): Idle, Move, Volley, TakeOff, Dive, Perch, Flap, Sweep, Slash, Circle, Screech, Death. Cánh xoay quanh vai, đầu/thân nhấp nhô; TakeOff/Dive không scale trong clip (workbench không scale) — Unity phóng to/thu nhỏ + mờ dần bằng transform khi cú lên/xuống trời.

**VFX skill: tái dùng bộ VFX Wind của player** (đã animate Pixellab, cùng style D-080) thay vì gen mới: Mưa Lông Vũ ← `WindFeather_Flutter`; Lốc Xoáy ← `WindTornado_Spin`; Tường Gió ← `WindGust_Arc` / `WindGustPuff_Dissipate` ghép tile; Lưỡi Gió Xoay ← `WindBlade_Spin`; Tiếng Hót ← `WindRing_Flow` / `WindShockwave_Expand`; va chạm ← `WindImpact_Burst`; báo điểm ← `WindMarker_Pulse`. Bóng cú (Lao Vuốt) dựng bằng code (elip tối + telegraph). **Lưu ý:** hiệu ứng "gió thổi qua màn hình" (vệt trắng) đã bị user loại bỏ vì xấu (D-110) — không dùng vệt đường thẳng trên toàn màn hình.

## 7. Kiến trúc (tái dùng khung Water)
- `BossController` + `BossDefinition` + `BossTelegraph` + `BossClipPlayer`; id skill Wind thêm vào `BossSkillId` (không đổi/xoá id cũ), skill viết trong file mới `BossSkillsWind.cs` (partial). Clip Wind thêm vào `BossClipId`.
- `ArenaWind` (mới, trong scene): hướng/độ mạnh gió theo phase, trôi người chơi, đồng bộ hướng bay của lá/mây `SkyWeather`.
- `BossSfx` thêm nhóm `boss.wind_owl` (SFX làm sau, theo `SfxPipeline.md`).
- Không sửa `EnemyUniversal`, không đổi save.

## 8. Tiêu chí hoàn thành
Mỗi skill có telegraph đọc được và né được bằng chạy hoặc dash; clip cast chạy đúng lúc skill; Bão Táp đọc được thứ tự vỡ; không đổi gameplay Earth/Water; test đơn vị cho `BossDefinition` Wind; Play Mode đo sát thương thật; docs/DecisionRegister cập nhật cùng lúc.

## 9. Câu hỏi cho user — ĐÃ TRẢ LỜI (duyệt cả bốn)
1. Duyệt bộ **6 skill** + gầm đổi phase (thêm Lưỡi Gió Xoay so với concept cũ)?
2. Mép vực = **va đập** (bản đầu) thay cho "rơi và hồi sinh"?
3. Luồng gió trôi người chơi ≈ 0.8/1.1/1.4 đơn vị/s — OK hay chỉ làm hiệu ứng hình ảnh?
4. Độ khó: HP 2400, B 14, Bão Táp làm vỡ cả hai cánh ngang?

## 10. Kết quả bản chơi thử v0 (2026-10-05, D-111)

- **Đã dựng:** `BossSkillsWind.cs` (6 skill + gầm đổi phase), `WindHazard.cs` (lông vũ/lốc/lưỡi liềm), `ArenaWind.cs` (gió trôi người chơi 0.8/1.1/1.45, đổi hướng 15–20 s, mây/lá theo hướng gió), `OwlBossBuilder.cs` (prefab + `Assets/Bosses/WindOwl/WindOwlBoss.asset`), `BossArenaWindBuilder` gắn boss vào scene. Số liệu ở `BossDefinition` (mục "Wind"), test `BossDefinitionWindTests`.
- **Đã chạy trong Play Mode (`BossArena_Wind`, người chơi bất tử):** triệu hồi bằng `TrySummon` → nghi lễ 4 trụ gió bắn tia vào tượng → cú xuất hiện đủ thân; **Mưa Lông Vũ** bắn quạt lông ra đúng hướng; **Lốc Xoáy** hiện vòng cảnh báo rồi sinh lốc; console không lỗi. Người chơi bất tử thấy cú giết người chơi rất nhanh khi không bất tử (xem phần cân bằng).
- **Animation:** bản 9 frame đầu bị user nhận xét "chưa mượt" ⇒ dựng lại 17 frame (`Tools/owl_render.py`). Bài học: Unity thu nhỏ texture > 4096 px làm lệch cắt frame ⇒ `ConfigureSheet` nay đặt `maxTextureSize 8192`.
- **Chưa kiểm chứng bằng mắt:** Lao Vuốt (bóng + đáp + cửa sổ phản công), Tường Gió (ổ che sau trụ + va đập mép), Lưỡi Gió Xoay, Bão Táp (thứ tự vỡ hai cánh ngang), gầm đổi phase, ba phase đủ trận. Chưa đo cân bằng. Chưa có SFX Wind (thuộc `SfxPipeline.md`). Chưa chạy EditMode test qua test runner (MCP timeout).

### 10.1 Thử từng skill và cân bằng lần 1 (2026-10-05)

Cách thử: spawn cú bằng code ở trạng thái `Fighting` (không chạy vòng combo), người chơi lv1 (100 HP) **đứng yên** ở chỗ xấu nhất, đo máu mất sau từng skill (sau giáp):

| Skill | Hành vi quan sát | Máu mất khi đứng yên | Chỉnh |
|---|---|---|---|
| Lao Vuốt | bóng + vòng đỏ, cú hiện đáp xuống đúng chỗ, hất người chơi | 18 | giữ |
| Tường Gió | dải đỏ ở mép đầu, ô an toàn xanh sau trụ gió, tường quét và đẩy người chơi ~5 đơn vị | 12 | giữ |
| Lưỡi Gió Xoay | cung đỏ báo trước, hai lưỡi bay vòng cung rồi quay lại | 12 | giữ |
| Bão Táp | màn hình tối, hai cánh ngang vỡ từng cặp nhấp nháy đỏ rồi tối, gió đẩy người chơi về giữa | 25 | giữ |
| Mưa Lông Vũ | quạt lông bay ra đúng hướng | **39** (quá cao cho phase 1) | `featherDamage` 0.7 → 0.5, `featherHomingDegrees` 40 → 25 (≈ 27) |
| Lốc Xoáy | vòng báo trước rồi lốc đi lang thang | 11 (quá hiền) | `cycloneSpeed` 2.2 → 3 |

- **Gió trôi người chơi:** user báo "nhân vật cứ tự trôi khi không bấm di chuyển" — đúng hành vi thiết kế (D-111) nhưng không có dấu hiệu nên giống lỗi. Đã giảm 0.8/1.1/1.45 → **0.6/0.9/1.2** và thêm **mũi tên chỉ hướng gió trên đầu người chơi** (`ArenaWind`); chỉnh một chỗ ở `BossDefinition.windDriftByPhase`. Nếu user vẫn thấy khó chịu có thể bỏ trôi khi đứng yên hoặc tắt hẳn (đặt 0).
- **Chưa thử:** Lao Vuốt 3 điểm (phase 3), đủ ba phase tự đánh, gầm đổi phase, khả năng đánh trả thật (người chơi gây sát thương lên cú trong lúc đáp/Recovery), SFX Wind. Chưa chạy test runner (MCP timeout).

### 10.2 Chạy thử đủ ba phase + làm lại animation (2026-10-05)

- **Chạy tự động đủ 3 phase** (người chơi bất tử đứng yên, ép máu boss xuống 60% / 25%): P1 `A: Lông Vũ → Lao Vuốt`, `B: Lốc Xoáy → Tường Gió`; P2 `C: Lưỡi Gió → Lông Vũ`, `D: Tường Gió ∥ Lốc Xoáy`, `E: Lao Vuốt ×2 → Lưỡi Gió`, Bão Táp sau mỗi 3 đợt; P3 Lao Vuốt 3 điểm, Bão Táp mỗi 2 đợt; gầm đổi phase chạy. Không treo, console không lỗi do boss (có 2 lỗi UI sẵn có của `QuestLogUI`/`PauseMenuUI` lúc nạp scene).
- **Lỗi thiết kế tìm ra:** combo P3 `G` bắt đầu bằng Bão Táp trong khi P3 đã có Bão Táp mỗi 2 đợt ⇒ hai Bão Táp liên tiếp cách 1.4 s. Đổi: `G = Lưỡi Gió → Lông Vũ`, thêm `H = Tường Gió ∥ Lốc Xoáy`; Bão Táp chỉ còn từ nhịp "ultimate".
- **Cân bằng lần 2 (đứng yên, ~1 chu kỳ 20 s ở phase 1 mất ≈ 56 máu):** `wallDamage` 1.0 → 0.8, `diveDamage` 1.2 → 1.0, `cycloneDamage` 0.9 → 0.8 (kèm lần 1: Lông Vũ 0.5, lốc nhanh 3). Người chơi thật né được nên mất ít hơn nhiều; cần user chơi thật để chốt.
- **Animation (user nhận xét "cứng"):** (1) clip cú dựng lại bằng `Tools/owl_render.py` v2: cánh 3 đoạn (đoạn ngoài trễ theo nhịp vỗ), co cánh theo phối cảnh, đuôi trễ theo thân, đầu ngược chiều, 17 frame, nội suy Catmull-Rom; idle/move vỗ mạnh hơn. (2) VFX skill (`WindHazard` look): lông vũ lắc lư + vệt đuôi + hiện/biến mờ, lốc có bóng đất + lá xoay quanh + nhấp nhô, lưỡi gió có vệt dài, hai luồng gió ở đầu cánh mỗi lần tung skill, tường gió có lớp mờ phía sau + lá/bụi cuốn theo. Chưa dùng AI (`animate_image` khung 256 px ≈ 16 generation/clip, ngân sách còn ~85).

### 10.3 Animation AI cho cú (2026-10-05, user: "dùng animation AI")

- `animate_image` (D-081) trên ảnh cú gốc: **8 frame 256×256 = 8 generation** (không phải 16 như ước tính). Lần 1 ảnh gốc 256 đầy khung ⇒ cánh bị cắt ở mép khi xoè ngang (bỏ, tốn 8). Lần 2: thu cú còn 176 px đặt giữa khung 256 (lề 40 px, gửi bằng `first_frame_base64`) ⇒ cánh không bị cắt, vỗ có đổi dáng lông thật.
- Kết quả dùng làm **vòng bay lơ lửng** `Owl_IdleAI_17f.png` (frame 0..8 rồi 7..1 để lặp liền) cho clip **Idle, Move, Flap** (fps khác nhau). PPU của sheet này = 32×176/256 = 22 để cú giữ nguyên kích thước (pixel to hơn ≈ 1.45×).
- Các clip cast còn lại (Volley, TakeOff, Dive, Perch, Sweep, Slash, Circle, Screech, Death) vẫn là renderer cục bộ. Ngân sách Pixellab còn ≈ 69 generation (reset 2026-10-27): đề xuất gen tiếp Volley và TakeOff (8 generation/clip) nếu user thấy khác biệt pixel giữa clip AI và clip cục bộ chấp nhận được.

### 10.4 Volley + TakeOff bằng AI (2026-10-05)
- Gen thêm 2 clip `animate_image` (176 px trong khung 256, 8 frame mỗi clip = 16 generation): `Owl_VolleyAI_9f.png` (cánh giương rộng → hất mạnh, lông bị uốn → thu về; giữ ở frame 4 trong lúc telegraph) và `Owl_TakeOffAI_9f.png` (cánh gập, đập xuống, thân co lại). Thay clip `Volley`/`TakeOff` cục bộ trong `OwlBossBuilder`.
- Lưu ý: ở TakeOff frame 5–7 AI làm lệch nhẹ trăng khuyết trên trán; chấp nhận vì clip chạy ~0.7 s và cú mờ dần khi bay lên. Thân không tự bay lên trong clip — Unity dịch/mờ bằng pose.
- Pixellab còn ≈ 53 generation (reset 2026-10-27). Còn lại clip cục bộ: Dive, Perch, Sweep, Slash, Circle, Screech, Death.

### 10.5 Dive + Sweep + Slash bằng AI (2026-10-05)
- Gen thêm 3 clip `animate_image` (24 generation): `Owl_DiveAI_9f` (cánh khép lao xuống, nở ra khi đáp), `Owl_SweepAI_9f` (cánh quét ngang, AI tự vẽ luồng gió xanh quanh cánh ở frame 3–6), `Owl_SlashAI_9f` (cánh giơ cao rồi chém chéo, cánh khép chéo ôm thân ở frame 4–6). Giữ frame 3 (Sweep/Slash) trong lúc telegraph.
- Lỗi gặp: lần gửi base64 đầu tiên của Slash bị cắt giữa chừng (client MCP cắt tham số dài) ⇒ gửi lại bằng `first_frame_url` (frame 0 của clip Idle đã gen). Dùng URL thay base64 khi có thể.
- Pixellab còn ≈ 29 generation (reset 2026-10-27). Còn lại clip cục bộ: Perch, Circle, Screech, Death.

### 10.6 SFX Cú + sửa dash (2026-10-05)
- **SFX:** 24 id `sfx.bossowl.*` (33 file) tổng hợp bằng code (`Tools/sfx/synth_owl.py`, `catalog_bossowl.py`), bank "Boss Wind (Owl)". Gắn vào `BossSkillsWind` qua `OwlSfx(...)` (báo/tung/trúng của từng skill, loop lốc, sàn nháy/vỡ, bão), sự kiện chung (awaken/hit/recovery/phase/death) qua `OwlEventId` trong `BossSkills.cs`. Chưa nghe thử. Sửa lỗi `reverb()` lệch 1 mẫu khi độ dài lẻ.
- **Dash không dùng được ở Water/Wind:** `Player._dashScenes` (serialize trong từng scene) chỉ liệt kê `DemoScene` và `BossArena_Earth` — Water/Wind copy từ Earth nên thừa hưởng danh sách đó (D-089/D-093 chỉ bật cho Earth). Sửa: thêm `BossArena_Water`, `BossArena_Wind` vào cả ba scene arena và `EnableDash()` trong builder Water/Wind để rebuild không mất.
