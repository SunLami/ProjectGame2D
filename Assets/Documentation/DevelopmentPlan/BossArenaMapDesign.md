# Thiết kế Map Boss (Boss Arena) — Thủy / Địa / Phong

Trạng thái: **Scene `BossArena_Earth` đã dựng (§10); phần còn lại là DESIGN — đã chốt toàn bộ §9 (2026-10-03)** (2026-10-03). Quyết định: D-083. Chưa gen Pixellab, chưa dựng scene.
Phạm vi tài liệu này: **chỉ map/scene đấu boss** + điểm nối (trụ teleport, trụ triệu hồi) ở mức *chỗ đặt/hợp đồng*. Chưa thiết kế boss, skill boss, wave (sẽ làm riêng sau).

## 1. Ý tưởng user (xác nhận đã hiểu)

1. Mỗi hệ có **1 scene map boss riêng**: `BossArena_Water`, `BossArena_Earth`, `BossArena_Wind` (tên đề xuất).
2. Map là **một sân đấu đơn giản, nhìn trọn trong một màn hình**, góc nhìn top-down 3/4 như ảnh tham chiếu 1 (Titan Souls): sân đá vuông, tường bao quanh, **ngách trụ triệu hồi ở giữa-trên**, nền trống rộng để né/di chuyển, cỏ/rêu ăn vào mép, vài vật cản lớn.
3. **Luồng dài hạn:** DemoScene có trụ teleport → click chuột trái → UI chọn map boss → teleport (đổi scene). Trong map boss: player đến gần trụ triệu hồi, click → UI Shrine → bỏ viên ngọc nguyên tố (từ inventory) → đủ ngọc thì nút Start → UI tắt → boss xuất hiện trong map.
4. Concept boss: Thủy = cua giáp xanh/trắng (ảnh 2), Địa = golem đá phát sáng xanh lục (ảnh 3), Phong = cú thần trắng/tím/xanh với vương miện (ảnh 4). Chúng quyết định **màu chủ đạo và chủ đề kiến trúc** của từng map.
5. Làm **tài liệu trước**, sau đó mới nhờ Pixellab gen; dựng map trong Scene dựa trên asset map có sẵn.

## 2. Đối chiếu tài liệu / hiện trạng — các điểm cần user biết

| # | Phát hiện | Ảnh hưởng / đề xuất |
|---|---|---|
| 1 | **Pixellab hiện KHÔNG có tileset/map nào** (`list_topdown_tilesets`, `list_maps`, `list_tiles_pro` đều 0; 62 object chỉ là VFX skill). "Asset map đã gen" thực ra là các **gói tileset có sẵn trong Unity** (`Assets/Tiles/Tilesets/…`) | Dựng map bằng gói có sẵn trước (xem §6); Pixellab chỉ gen phần **đặc thù boss** (trụ triệu hồi, vật trang trí theo hệ, tường/cổng) bằng Pixen theo D-081. Nếu user muốn gen cả tileset nền bằng Pixellab thì nói rõ để tính ngân sách |
| 2 | D-070 / `SkillVfxPipeline.md` §7 ghi "3 **Slime** Boss" nhưng concept art lần này là cua/golem/cú | Cần user xác nhận concept mới **thay thế** Slime Boss (đề xuất: có — cập nhật D-070 cùng D-083). Không ảnh hưởng thiết kế map |
| 3 | Chưa có tài liệu nào về **Shrine / viên ngọc triệu hồi / teleport-boss** | Ghi nhận ở D-083 là phạm vi *sau*; tài liệu này chỉ để sẵn **neo + collider + spawn** để sau gắn vào |
| 4 | Boss đã có hạ tầng lưu: `BossDefeatTracker` (persistent id, `IPersistentWorldObject`, restore silent) + `WorldObjectRegistry` **theo scene** | Mỗi arena có tracker riêng, persistent id riêng; **quyết định "boss đã hạ rồi thì arena còn đánh lại được không?"** là điểm mở §9 (ảnh hưởng save — AGENTS.md yêu cầu nêu trước) |
| 5 | Đổi scene đi qua `SceneFlowService` (fade overlay, `IsTransitioning`), điểm xuất hiện qua `SpawnRegistry` (spawnId → Transform), player/HUD sống ở scene `Bootstrap` | Teleport = nạp scene arena qua `SceneFlowService` + spawnId `arena_entry`; không dựng Player/HUD riêng trong arena (khớp DemoSceneWorkflow: arena chỉ chứa nội dung scene, không chứa persistent systems) |
| 6 | Camera DemoScene: orthographic size 8 (≈ 28.4×16 đơn vị 16:9); tile 16px = 1 unit | Arena phải nằm gọn trong khung — chốt camera cố định cho arena (§4.2) |
| 7 | Quy tắc DemoScene-first (D-082 áp dụng tương tự): làm trong DemoScene → user chốt → mới port | Scene arena là **scene độc lập mới**; DemoScene chỉ chứa trụ teleport tạm + UI chọn map |

## 3. Nguyên tắc thiết kế map boss

- **Đọc được trong một ánh nhìn:** toàn bộ sân nằm trong một khung camera cố định, không cuộn. Player luôn thấy boss + toàn bộ đạn/vùng cảnh báo.
- **Sân phải trống đủ để né:** ≥ 60% diện tích sân là nền phẳng đi được. Vật cản ít, lớn, đối xứng, dùng làm điểm nấp/chắn skill chứ không làm mê cung.
- **Một vật thể trung tâm duy nhất = trụ triệu hồi** (ngách phía trên). Mắt người chơi tự hiểu "boss sẽ ra từ đây".
- **Nhận diện hệ bằng màu + chất liệu nền, không bằng bố cục:** 3 map dùng **chung một khung layout** (nhanh dựng, nhanh cân bằng boss), chỉ khác bảng màu/chi tiết. Biến thể bố cục (nếu cần) để đến khi thiết kế boss.
- **Sạch cho VFX skill:** màu nền không trùng màu skill của chính hệ đó (VFX Thủy xanh/cyan trên nền Thủy phải vẫn nổi → nền Thủy hơi xám-xanh trầm, tránh cyan bão hòa). Kiểm tra bằng screenshot khi cast skill đúng hệ trong arena.
- Không có hazard nền (gai, nước sâu giết người…) ở bản đầu — hazard là một phần thiết kế boss.

## 3A. Ba không gian KHÁC NHAU (user 2026-10-03 — thay cho "khung layout chung")

> User: ba map boss phải thực sự khác nhau về **không gian**, không chỉ đổi màu. Vì vậy phần "chung một khung layout" ở §3/§4 bị **thay thế**: chỉ giữ chung *hợp đồng kỹ thuật* (§4.2: spawn point, bounds, camera cố định, shrine anchor, exit), còn **hình dạng sân, biên, nền, đặc tính di chuyển** khác hẳn nhau.

| | **Địa — Thạch Đài (đấu trường đá kín)** | **Thủy — Đảo Hồ Thần (đảo tròn giữa hồ)** | **Phong — Thiên Đài (đài trời lơ lửng)** |
|---|---|---|---|
| Hình sân | Chữ nhật lớn, đối xứng, **ba phía là vách đá/tường đền**, cổng vòm phía nam | **Hình tròn/bát giác** bán kính ~12; bao quanh là **mặt hồ** (không tường); 4 bán đảo đá nhô ra mặt nước | **Đài hình sao/chữ thập vỡ** không đối xứng, mép nham nhở; xung quanh là **biển mây + hư không**; vài đảo đá lơ lửng tách rời (trang trí) |
| Biên (collider) | Tường đá | Mép nước (collider vòng), có thể có hồ nông trang trí ở rìa | Mép vực (collider theo viền đài) |
| Nền | Plaza đá lát, kênh rune xanh lục hội tụ về bệ trụ ở phía bắc | Đá phiến ẩm rêu rêu + vòng nước nông (trang trí) + vòng rune xanh biển; vũng nước, rong, sen | Đá trắng ngà vòng khắc gió, mây trôi dưới đài (animated), lông/lá bay |
| Vật cản | 4 cột đá lớn đối xứng + 2 khối đá lớn | 4 trụ san hô/đá trồi từ nước nằm dọc rìa, vài tảng nhô (nấp skill) | Vài vòm/cột gãy, mảng đá lớn rải không đối xứng |
| Nơi đặt trụ triệu hồi | Ngách bắc, trên bệ cao 2 bậc | **Giữa đảo**, trên bục tròn thấp (boss trồi lên từ mặt nước quanh đảo) | **Trung tâm đài**, vòng bệ gió; boss hạ cánh từ trên cao |
| Cách nhìn / cảm giác | Nặng, vững, né theo hàng/ô | Mở, xoay vòng, né theo cung tròn | Thoáng, lệch, nguy hiểm vì sát mép vực |
| Gợi ý cho thiết kế boss sau (không ràng buộc) | Skill theo đường thẳng/ô lưới | Skill theo vòng/sóng từ rìa vào | Skill đẩy/hất, vùng gió quét |
| Kích thước interior | 34×20 | đường kính ~26 (diện tích tương đương) | ~34×20 bounding, diện tích đi được nhỏ hơn do mép nham nhở nên mở rộng bounding |

Ba silhouette phải đọc được khác nhau chỉ bằng hình bóng.

## 3B. Map LỚN + nền trống trước, object sau + chuyển động môi trường (user 2026-10-03)

User: "map phải to hơn nữa" (boss chắc chắn to hơn player nhiều; sân cũ quá nhỏ để đánh boss) / "gen 1 map trống, sau đó gen thêm các object bên trong để trang trí môi trường" / "map cũng phải có chuyển động môi trường".

1. **Kích thước (cập nhật 2026-10-03, lần 3):** đo từ ảnh minh họa user: sân ≈ 20 × 16 chiều-cao-player (player ≈ 1.5 đơn vị, đo từ MapNhat: ortho 8, ~100 px / 67.5 px-mỗi-đơn-vị) ⇒ ≈ 30 × 25 đơn vị, boss trong ảnh ≈ 3× player; boss game to hơn nên bản đầu chọn 46×38. **User yêu cầu thêm: tăng sân gấp 1.5 lần chiều dọc và gấp 2 lần chiều ngang — tính trên PHẦN SÀN ĐI ĐƯỢC TRONG ẢNH (user kiểm tra bằng mắt trên ảnh; lần đầu tôi chỉ nhân số đơn vị mà ảnh gen ra thì sàn không dọc hơn nên bị phản hồi) ⇒ interior Địa 92 × 50 đơn vị; user yêu cầu tiếp ×1.5 cả hai chiều ⇒ interior Địa 138 × 75 đơn vị** (≈ 92 × 50 chiều-cao-player; camera ortho ≈ 11 chỉ thấy ~28% chiều ngang sân — cần minimap/chỉ hướng và có thể nâng camera khi đánh boss; đánh giá khi thử với boss thật) (sàn cũ ≈ 46×33; tỉ lệ ≈ 1.84:1) (46×2, 33×1.5; ≈ 61 × 38 chiều-cao-player, ~3× diện tích bản 46×38). Thủy: bao bọc elip ~88×66 (mặt hồ + đảo); Phong: bounding 138×75. Camera đi theo player có Confiner (ortho ≈ 11 → khung 39×22; sân gấp ~2.4× màn hình theo chiều ngang, ~2.6× chiều dọc); góc nhìn vẫn top-down. Các số cũ 24×14, 34×20, 64×40, 46×38 **bỏ**. Tinh chỉnh khi thử boss thật. **Tỉ lệ cảm nhận:** sân phải đọc là *rộng thênh thang* — phiến sàn và mảng hoa văn LỚN, không rải phiến đá nhỏ dày đặc. Hệ quả: nền cần ảnh tỉ lệ 1.6:1 (xem Style Guide) và ~92×57 đơn vị cần đủ vật thể/ chuyển động môi trường để sân không trống rỗng.
2. **Hai lớp art:** (a) **Nền trống** (BaseMap): sàn + biên + vách/mặt nước/mây, KHÔNG có vật trang trí, rune hay cột; (b) **Object** gen riêng (Pixen no_background) đặt chồng: cột, tượng, mảnh vỡ, bụi cây, bệ trụ, trụ teleport... Nền trống phải đọc được là sân đấu hoàn chỉnh, object chỉ làm giàu thêm và che đường nối.
3. **Cách tạo nền lớn:** Pixen chỉ ra ≤ 512×512 mỗi ảnh (Pro 688×384, đắt 20-40 gen) nên nền là **ghép nhiều mảng** (xem `BossArenaArtStyleGuide.md` §2: cắt giữa mảng, cân màu, ghép bằng seam tối ưu chạy dọc rãnh vữa). Dùng ~12–16 mảng gốc khác nhau, lật/xoay để lấp đủ ô, decal duy nhất che lặp lại.
4. **Chuyển động môi trường (bắt buộc, mọi map):**
   - Địa: rune sáng nhịp xung (animate), bụi/đốm sáng lơ lửng, đá vụn rơi lác đác từ vách, ngọn lửa đuốc/lò than, dây leo đung đưa.
   - Thủy: mặt hồ động (vòng lặp sóng + gợn quanh bán đảo), phản chiếu/caustics lấp lánh trên đá ướt, thác nước ở vách nền, sậy/sen đung đưa, sương mỏng trôi.
   - Phong: biển mây trôi (cuộn parallax), đảo đá lơ lửng nhấp nhô lên xuống, lá/lông bay xuyên sân, vệt gió trắng, cờ/dải vải bay phấp phới.
   Mỗi hiệu ứng là sprite animation Pixellab (D-081) hoặc hạt (particle) nhẹ; không ảnh hưởng collider/gameplay; có thể tắt ở thiết lập chất lượng thấp.

### 3C. Quy tắc đặt decor (user 2026-10-03: "Boss sẽ đi xung quanh map để combat với Player — decor quá nhiều trên sân sẽ dư thừa")

1. **Vùng chiến đấu = toàn bộ sàn trống.** Boss di chuyển khắp sân (và có thể dash/charge xuyên sân), nên **không đặt vật cản/vật thể rời giữa sân**: không đá tảng, không cột gãy, không tượng ở giữa sân.
2. **Decor chỉ nằm ở viền:** (a) *gắn tường* (cờ, dây leo, lò lửa dựa tường phía bắc/nam); (b) *góc sân trong mảng cỏ* (tượng golem, đống đá nhỏ, cột gãy, cỏ); (c) *bệ trụ triệu hồi* ở khu ngách bắc (cố định, boss/path-finding né).
3. **Hoa văn sàn (seal) là decal phẳng, không collider**, không cản boss; chỉ làm điểm nhận diện + nơi rune sáng nhịp.
3b. **Ngoại lệ — tượng triệu hồi giữa sân (user):** đặt giữa sân trước trận; khi kích hoạt tượng biến mất (xem Style Guide §6) nên giữa sân vẫn trống khi chiến đấu.
4. **Vật cản chiến thuật** (cột/đá để nấp skill) KHÔNG đặt sẵn: nếu cần sẽ do thiết kế boss quyết định và spawn động (có thể phá được) theo từng phase.
5. **Va chạm:** vật gắn tường/góc dùng collider nhỏ sát tường, không lấn vào vùng chiến đấu (vành đai ≥ 8 đơn vị từ tường là vùng "luôn trống" cho boss).
6. Tổng số vật thể trên sân Earth giữ ở mức tối thiểu (~12 vật gắn tường/góc + bệ trụ + seal); chuyển động môi trường dùng hạt/hiệu ứng nền không cản.

## 4. Khung kỹ thuật (hợp đồng chung) + layout Địa

### 4.1 Kích thước (đơn vị Unity; art PPU 48 — xem `BossArenaArtStyleGuide.md`; mọi số là *đề xuất*, chỉnh khi thử camera). Bảng và sơ đồ dưới là khung của **Địa**; Thủy/Phong theo §3A

```
 ┌──────────────────────────────────────────────┐  ← tường ngoài (viền, dày 1)
 │  ████ tường nền (mặt đứng, 3-4 hàng) ████     │
 │   ║ cột ║            ║ cột ║                  │  ← hành lang cột sau tường
 │         ┌──────────────┐                      │
 │         │  NGÁCH TRỤ   │ ← trụ triệu hồi      │  ← ngách lõm 6×3, giữa-trên
 │   ┌─────┴──────────────┴─────┐                │
 │   │   mép cỏ/nước/gió ăn vào │                │
 │   │      NỀN SÂN 24 × 14     │  vật cản lớn ×2│
 │   │   (đá lát, ô vuông)      │  (đối xứng)    │
 │   │                          │                │
 │   └──────────┐   ┌───────────┘                │
 │              │CỔNG│ ← lối vào (spawn player)  │  ← giữa-dưới, rộng 4
 └──────────────┴───┴────────────────────────────┘
```

| Thông số | Giá trị đề xuất |
|---|---|
| Kích thước sân đi được (interior) | **34 × 20 tile** (user 2026-10-03: sân phải đủ rộng để đánh boss, cast skill và né skill boss; sân lớn hơn đề xuất đầu 24×14. Giá trị chỉnh được khi thử camera/skill thật) |
| Kích thước tổng gồm tường | ~ **40 × 25 tile** (mặt tường trên dày hơn: nhìn từ trên xuống thấy mặt đứng) |
| Camera arena (**lỗi thời — xem §3B: camera follow + Confiner, ortho ≈ 11, interior 138×75**) | Orthographic, **cố định** (không follow), size **≈ 12.5** → khung 44.4×25 bao trọn 40×25 (nhân vật nhỏ hơn DemoScene ~35%; nếu quá nhỏ thì chuyển camera follow có Confiner, sân giữ nguyên); tâm camera = tâm map. Chỉnh theo tỉ lệ màn hình thực tế |
| Ngách trụ | Lõm 6 × 3 tile ở giữa-trên; trụ ở tâm ngách; lối ra sân mở 4 tile |
| Cổng vào | Giữa-dưới, rộng 4 tile; `PlayerEntry` đặt ngay trong sân sát cổng |
| Vật cản | 2 khối lớn đối xứng (mỗi khối 3×3) cách tâm ±6 tile; 4 cột trang trí dọc tường trên (không collider chắn sân) |
| Khoảng cách tối thiểu | Từ trụ tới mép sân ≥ 8 tile; vùng boss xuất hiện (`BossSpawn`) ở tâm-trên sân, cách trụ ~5 tile (boss ra khỏi ngách, không đè lên trụ) |

### 4.2 Cấu trúc scene (mỗi arena giống nhau)

```
BossArena_<Element> (scene)
├─ Grid
│   ├─ Tilemap_Ground          (sắp xếp thấp nhất; nền đá lát)
│   ├─ Tilemap_GroundDetail    (cỏ/rêu/vết nứt/ô gạch hỏng, rải ngẫu nhiên)
│   ├─ Tilemap_WallBack        (mặt đứng tường trên + cột)
│   ├─ Tilemap_WallCollision   (TilemapCollider2D + CompositeCollider2D, ẩn render hoặc là chính layer tường)
│   └─ Tilemap_Overlay         (che phủ player khi đi sau cột/mép tường; sorting cao)
├─ Props                       (vật cản lớn, cột, trụ triệu hồi — mỗi cái prefab, tự Y-sort)
├─ SpawnPoints                 (SpawnRegistry: arena_entry, boss_spawn, shrine_anchor)
├─ ArenaBounds                 (BoxCollider2D trigger bao sân; dùng làm camera/biên đạn)
├─ ArenaCamera                 (camera cố định hoặc Confiner; thông số §4.1)
├─ ShrineInteractable          (collider + tương tác chuột; RỖNG LOGIC ở giai đoạn này — placeholder)
├─ BossDefeatTracker           (persistentId riêng; boss chưa tồn tại → để trống _boss)
├─ ExitReturn                  (điểm/portal quay về — xem §5)
└─ Lighting/Volume (nếu dự án đang dùng; theo tông từng hệ)
```

Sorting: dùng cùng quy ước Y-sort hiện có của DemoScene (Sorting Layer của Player/Enemy/VFX theo `ThuySkillVfxArtStyleGuide.md`/D-072); ground thấp nhất, overlay cao nhất.

## 5. Điểm nối với các hệ thống khác (chỉ hợp đồng, chưa implement)

| Điểm nối | Hợp đồng đề xuất |
|---|---|
| Trụ teleport (DemoScene, đồ tạm) | GameObject tương tác bằng click chuột trái → mở UI chọn map (3 nút Water/Earth/Wind, nút bị khóa nếu chưa đủ điều kiện — điều kiện do thiết kế tiến trình quyết sau) → `SceneFlowService` nạp scene arena, spawnId `arena_entry` |
| Quay về | `ExitReturn`: đặt cạnh cổng, dùng cùng cơ chế nạp scene quay về DemoScene (spawnId của trụ teleport). Khi đang đánh boss thì **khóa** (không thoát giữa trận) — quy tắc chi tiết khi thiết kế wave |
| Trụ triệu hồi + UI Shrine | Collider + `Interactable` click chuột (cùng kiểu tương tác chuột với NPC/trụ teleport); UI Shrine đọc inventory (viên ngọc là `ItemSO` data-driven: id `item.orb.<element>`), xác nhận → bắt đầu → UI tắt → spawn boss tại `boss_spawn`. **Chưa thiết kế/implement ở D-083** |
| Boss spawn | Prefab boss đặt vào `boss_spawn` lúc runtime; `BossDefeatTracker` gắn lại instance khi spawn (cần nối `Bind` lúc spawn — vấn đề đã ghi ở §2 #4) |
| Save | Chưa thêm field save mới. Trạng thái "đã hạ boss" tái dùng `WorldObjectRegistry` (Phase 8); các quyết định thêm xem §9 |
| Âm thanh/nhạc | Nhạc riêng từng arena qua `MusicManager` (sau); SFX kế hoạch tách riêng `CombatSkillSfxPlan.md` |

## 6. Nguồn asset & kế hoạch gen

> **Cập nhật 2026-10-03 (lần 2):** hướng tileset 16px/remap/Ruined Temple đã **bỏ**. Toàn bộ art arena do Pixellab gen bằng Pixen ở mật độ skill (PPU 48) theo `BossArenaArtStyleGuide.md`; bảng 6.1/6.2 dưới đây chỉ còn là tham chiếu ban đầu. Ngân sách: ~50–60 generation/map (sàn ~12 mảng + viền/tường ~10 + prop ~10 + vật động ~5×4), tổng ~170 trên 361 còn lại — cần cân đối với SFX/boss sau.

### 6.1 Tái dùng gói có sẵn (không tốn Pixellab)

| Hệ | Chủ đề | Gói tileset có sẵn dùng làm nền | Ghi chú |
|---|---|---|---|
| Thủy | Đền cổ ngập nước, đá xám-xanh trầm, hồ nông ăn vào mép | `Ruined Temple` + `Ruins Objects` (Blue-gray) + water coasts của `Swamp`/`Grassland` | Nền sân: đá lát; mép: nước nông (không đi được hoặc làm chậm — chỉ trang trí ở bản đầu) |
| Địa | Phế tích đá nâu, nứt, rêu úa | `Rockland` (Ground_rocks, Objects) + `Ruins Objects` (Brown / Brown-gray) | Gần ảnh 1 nhất; vật cản = khối đá lớn |
| Phong | Đền trên cao, đá trắng-be, cỏ tươi, lá bay | `Grassland` (Glades/ground_grass) + `Ruins Objects` (Blue-gray/sáng) | Tông sáng; trang trí lá/lông (có thể thêm VFX nền nhẹ sau) |

Cần **kiểm tra bản quyền/license** của từng gói đã ghi trong thư mục (`License.txt`) trước khi phát hành; không đổi so với các scene hiện có.

### 6.2 Phần gen bằng Pixellab (**chỉ khi user duyệt §9**; bắt buộc Pixen + animate khi cần animation — D-081)

| Asset | Mục đích | Cách gen | Ước tính generation |
|---|---|---|---|
| `Shrine_<Element>` ×3 (trụ triệu hồi: bệ đá + vòng ngọc + khe đặt ngọc, trạng thái Idle/phát sáng nhẹ) | Vật thể trọng tâm, đổi màu theo hệ | Pixen (1) + `animate_image` (4) mỗi trụ | ~5 × 3 = 15 |
| `ShrineActive_<Element>` ×3 (animation lúc triệu hồi) | Hiệu ứng khi bấm Start | Pixen + animate | ~15 |
| `TeleportPillar` (trụ teleport DemoScene, 1 bản, tạm) | Chỗ click mở UI | Pixen + animate (hoặc dùng prop có sẵn nếu user muốn tiết kiệm) | ~5 |
| `ArenaGate_<Element>` (cổng vào, tùy chọn) | Nhận diện lối vào | Pixen tĩnh (1) ×3 | ~3 |
| Trang trí đặc trưng hệ (cột/tượng cua, tượng golem, tượng cú) ×3 | Nhận diện hệ qua concept boss | Pixen tĩnh (1) ×3 + biến thể | ~3–6 |
Tổng đề xuất ban đầu: **~40–45 generation** (Tier-1 còn ≈ 389, reset 2026-10-27) — hoàn toàn nằm trong ngân sách. **Không gen nền tileset** trừ khi user yêu cầu (vì gói có sẵn đã đủ và đồng nhất với các map khác).
Style guide cho nhóm asset map: kế thừa bảng màu từng hệ (`ThuySkillVfxArtStyleGuide.md`, `GeoSkillVfxArtStyleGuide.md`, `WindSkillVfxArtStyleGuide.md`) nhưng **chất liệu đá/đền** (không phải VFX) — viết trong `BossArenaArtStyle` khi duyệt bước gen.

## 7. Lộ trình triển khai (đề xuất, mỗi bước kiểm tra trong Editor/Play Mode)

| Bước | Nội dung | Điều kiện qua |
|---|---|---|
| 0 | User chốt §9 → D-083 chuyển Accepted | — |
| 1 | Dựng `BossArena_Earth` bằng gói có sẵn (gần ảnh tham chiếu nhất) theo khung §4: tilemap, collider, camera, spawn points, ngách + trụ *placeholder* (prop có sẵn) | Vào scene thấy trọn sân trong một khung; Player đi lại, không lọt tường, Y-sort đúng |
| 2 | Nhân khung sang `BossArena_Water`, `BossArena_Wind` (đổi gói/palette) | 3 map đọc là 3 hệ khác nhau; skill cùng hệ vẫn nổi bật trên nền |
| 3 | Gen asset Pixellab đặc thù (§6.2) theo duyệt; thay placeholder | Asset đúng style, đã qua kiểm tra viền/seam |
| 4 | Trụ teleport + UI chọn map trong DemoScene → nạp arena → quay về (không logic summon) | Đi–về đúng spawn, không hỏng Player/HUD/save |
| 5 | (Thiết kế riêng sau) Shrine UI + ngọc + spawn boss + wave + skill boss | — |

## 8. Definition of Done (cho phần map)

- 3 scene arena nằm trong Build Settings, mở được độc lập và qua teleport; cấu trúc theo §4.2.
- Toàn sân trong khung camera cố định ở 16:9 và 16:10; không có điểm player kẹt/thoát khỏi sân.
- Mỗi arena có `SpawnRegistry` đủ `arena_entry`/`boss_spawn`/`shrine_anchor`, `BossDefeatTracker` với persistent id duy nhất (đã qua ContentValidation nếu có kiểm tra id).
- Asset Pixellab (nếu có) ghi nhận đầy đủ trong tài liệu style + bảng generation đã dùng; license các gói tái dùng đã kiểm tra.
- Tài liệu đồng bộ: `DecisionRegister.md` (D-083, cập nhật D-070 nếu user đồng ý), `README.md`, `DemoSceneWorkflow.md` (thêm bước trụ teleport khi làm bước 4).

## 9. Quyết định user (2026-10-03)

1. ✅ **Concept boss mới (cua/golem đá/cú thần) thay Slime Boss** — D-070 được cập nhật.
2. ✅ **Gen tileset nền mới bằng Pixellab** (`create_topdown_tileset`, 16px khớp PPU 16 của dự án; mỗi tileset ~3-4 generation). Style guide riêng: `BossArenaArtStyleGuide.md` (đo từ tileset game, MapNhat và 24 gói Craftpix; asset gen phải remap palette). **Cập nhật cùng ngày:** Pixellab gen *sàn + mép cỏ + prop*; *tường/cột/mặt đứng* dùng gói `Ruined Temple` vì Wang tileset của Pixellab không gen được tường dùng được (thử 2 lần) và để đồng bộ phong cách.
3. ✅ **Sân đủ rộng** để đánh boss, cast skill và né skill boss → interior 34×20 (§4.1).
4. ✅ **Sau khi hạ boss, arena vẫn mở để quay lại đánh**; sau này có thể thiết kế tăng độ khó cho các lần đánh lại (độ khó/thưởng lặp: thiết kế riêng cùng boss). **Mục đích đánh lại: farm item/vật phẩm** — nên bảng drop (data-driven `ItemSO`/drop table) và thưởng theo mức độ khó là phần của thiết kế boss, và arena phải chịu được vòng lặp vào–đánh–về nhiều lần (reset sạch trạng thái mỗi lần nạp scene, không rò rỉ VFX/đối tượng). **Hệ quả kỹ thuật cần nhớ khi làm Shrine/spawn:** `BossDefeatTracker.RestoreState` hiện *xóa im lặng* boss đã hạ — với arena đánh lại được, boss chỉ spawn qua Shrine nên tracker chỉ nên ghi cờ 'đã hạ lần đầu' (+ có thể thêm số lần hạ/mức độ khó sau này, cần mở rộng `WorldObjectState`/save version — phải ghi tài liệu save trước khi làm), không được dùng để chặn spawn.
5. ✅ **Dựng `BossArena_Earth` trước**, nhân sang Water và Wind.
6. ✅ **Trụ teleport DemoScene: gen riêng bằng Pixellab** (Pixen + animate_image, D-081).

## 10. Scene `BossArena_Earth` — đã dựng (2026-10-03, D-087)

**File:** `Assets/Scenes/BossArena_Earth.unity` (đã vào Build Settings). Dựng bằng `Tools > Project Game > Boss > Arena Earth - 1 Import Sprites` rồi `- 2 Build Scene` (idempotent; code ở `Assets/Editor/BossArenaEarthBuilder.cs`). Bắt đầu từ bản sao MapNhat để giữ đúng đường dây của scene production (`_SceneContext`, Player, Cinemachine, readiness gate); đã bỏ nội dung MapNhat (map rừng, câu cá, nông trại, cổng thoát, quest flow).

| Thành phần | Chi tiết |
|---|---|
| Nền | `Arena_Base` (BaseMap 1320×804, **PPU 9** ⇒ sàn đi được ≈ 141×71 đơn vị, gốc tọa độ = tâm sàn), vật thể rải ở viền theo §3C |
| Tượng triệu hồi + seal | `SummonStatue` (PPU 20, collider) ở tâm sân; `Arena_FloorSeal` PPU 12 + lớp rune sáng nhịp (`_SealGlow`) |
| Điều khiển | `BossArenaController`: `TrySummon()` → tượng tan (`SummonStatue_Vanish`) + bụi + seal bùng sáng + rung/flash → bệ mờ dần → boss hiện dần (`BeginEncounter`) + thanh máu; boss chết → sau 4 s tượng hiện lại (**đánh lại được**). F6 = triệu hồi, F7 = reset (debug). Shrine UI gọi `TrySummon()` sau này |
| Decor | 28 vật thể: cờ ×6, dây leo ×2, lò lửa ×4 (sát tường), tượng golem canh ×4 (4 góc, có collider nhỏ), đá/cột gãy ở 4 góc, cỏ ×8; 24 vật có animation (`AmbientLoop`) |
| Thời tiết | `ArenaWeather`: mảng cát bay ngang màn hình (đầu tiên sau 6 s, rồi 20–50 s/lần, trước Player), cỏ khô lăn (15 s rồi 40–90 s), hạt cát trôi theo gió |
| Camera | `CM Camera` ortho **11**, theo player, `CinemachineConfiner2D` bó trong ảnh nền (`BorderMap`) |
| Va chạm | `BorderMap` EdgeCollider quanh sàn đi được (±70.5 × −35.4…35) |
| Spawn | `SpawnRegistry`: `arena_entry` (0,−31), `boss_spawn` (0,0), `shrine_anchor` |

**Đã kiểm chứng Play Mode (qua Bootstrap):** nạp scene, player ở cửa vào, camera giữ trong bản đồ; tượng/seal/decor hiển thị đúng; triệu hồi → boss hiện giữa seal và tung Slam với telegraph; giết boss → tượng trở lại, `State=StatueIdle`; cát bay/cỏ lăn/hạt cát chạy; console sạch (đã sửa cảnh báo vận tốc hạt).

**Ghi nhận / việc còn lại (không chặn scene):**
1. **Teleport từ DemoScene** (trụ + UI chọn map) và **Shrine UI/ngọc** chưa làm; hiện chạy bằng cách mở scene này (PlayModeBootstrap nạp qua Bootstrap) và F6.
2. **Minimap/HUD** vẫn hiển thị "Heart Village" (HUD thuộc Bootstrap, ảnh bản đồ MapNhat) — cần ảnh minimap riêng cho arena.
3. **Tiếng bước chân im lặng:** `RockTileData` có 1089 tham chiếu tile nhưng tất cả đều null (asset hỏng), `MapManager` bind vào tilemap rỗng (không lỗi, không tiếng); sửa khi có tile data.
4. **Save:** scene này sao chép `PlayerSpawnReadinessSource`/`GameplaySessionController`; arena không có `FarmingManager` nên nếu lưu game ở đây `snapshot.farming` sẽ rỗng — cần quyết định "arena có phải vị trí lưu được không" trước khi nối teleport.
5. `BossDefeatTracker` chưa nối (hiện chỉ hỗ trợ `EnemyUniversal` và xóa boss khi restore — trái với arena đánh lại được); cần bản theo `BossController` + cờ "hạ lần đầu" (đổi save ⇒ ghi tài liệu save trước).
6. Boss nhấp nhô bằng code, chưa có animation tư thế; cân bằng HP/B chưa làm.

## 11. Cân lại kích thước (D-088, 2026-10-03) — theo thực hành của game chuyên nghiệp

**Phản hồi user:** map to, player nhỏ, camera nhỏ ⇒ đánh boss không thấy boss. **Nguyên nhân:** các số 46×38 → 92×50 → 138×75 đo theo *bố cục ảnh*, không theo *khung camera*: sân rộng 3.6 lần màn hình, boss cao 8 đơn vị (5.3 lần player).

**Thực hành phổ biến (Titan Souls, Hades, Zelda, Hollow Knight, Dead Cells):**
| Tiêu chí | Giá trị tham chiếu | Nhận xét |
|---|---|---|
| Player/chiều cao màn hình | ~5–6% (ảnh tham chiếu của user: 5.3%) | giữ sprite đủ lớn, không zoom-out để chứa sân |
| Boss/chiều cao màn hình | 15–25% (≈ 3–4 lần player) | boss đọc được ngay, không che hết sân |
| Sân / khung nhìn | ~1–1.6 lần | thấy boss gần như mọi lúc, vẫn đủ chỗ né |
| Camera | bám điểm giữa player↔boss + tự lùi vừa đủ giữ cả hai trong khung | cộng mũi tên chỉ boss khi ra ngoài màn hình |

**Số mới (đã áp dụng và đo trong Play Mode):**
| | Trước | Sau |
|---|---|---|
| Nền | PPU 9 | **PPU 20** — sàn đi được **63.6 × 31.7** đơn vị |
| Camera cơ sở | ortho 11 (39×22) | **ortho 12** (42.7×24), lùi tối đa **15.5**, bám điểm giữa player/boss (`BossCameraDirector`, trọng số boss 0.35) |
| Boss | 8 đơn vị | **6 đơn vị** (prefab scale 0.75) = 23% chiều cao màn hình |
| Player | 1.5 đơn vị | giữ nguyên = 6.3% chiều cao màn hình |
| Sân / khung nhìn | 3.6 × 3.2 | **1.5 × 1.3** |
| Seal | PPU 12 (21 đơn vị) | PPU 16 (16 đơn vị) |
| Decor | PPU 16 | PPU 26–30 (nhỏ lại theo tỉ lệ sân) |
| Giới hạn skill | không có | đòn nhắm/hàng gai/Phi quyền bị cắt theo biên sân (`SetArenaBounds`); boss không bay ra ngoài sân |
| Thêm | — | `BossOffscreenIndicator`: mũi tên ở rìa màn hình khi boss ngoài khung |

Đo thực tế khi boss tung Spike Lanes: boss viewport (0.37, 0.58), player (0.57, 0.41) — cả hai trong khung. Tham số điều chỉnh: `BossCameraDirector` (base/max ortho, trọng số, margin), `BossArenaController._walkableBounds`. Kích thước ở §3B/§4/§9 (92×50, 138×75) **lỗi thời** — thay bằng bảng này.

## 12. Trụ teleport + UI chọn map (D-089, 2026-10-03)

**Luồng:** DemoScene có `TeleportPillar` (sprite Pixellab `Assets/Art/Teleport/TeleportPillar_Idle_9f.png`, 80×112, PPU 26, đã animation: tinh thể lơ lửng, gem quay, rune sáng; vòng lặp đi–về 6 frame) tại (−6, 2.5), phía tây điểm xuất phát, **xa nhóm quái** (quái ở phía đông, đuổi trong 5 đơn vị). Rê chuột lên trụ trong tầm 2.5 → con trỏ Interact + viền sáng; **click trái** mở `BossTeleportSelectUI` (3 thẻ: **Earth Golem** sẵn sàng, **Tidal Crab** và **Sky Owl** "COMING SOON") → chọn thẻ → **TELEPORT** → `SceneTravel.TryTravel("BossArena_Earth", "arena_entry")`. Trong arena có trụ thứ hai (tiêu đề "LEAVE THE ARENA", thẻ Heart Village → `DemoScene` spawn `teleport_pillar_return`), **khóa khi đang triệu hồi/đánh boss** (`TeleportPillarInteractable._lockWhileFighting`).

**Code:** `Scripts/World/TeleportPillarInteractable.cs`, `SceneTravel.cs` (+ `PlayerTravelSnapshot`), `SceneTravelArrival.cs`; `Scripts/UI/BossTeleportSelectUI.cs` (dựng bằng code, phong cách Dark Light Fantasy, đẩy `GameState.Dialogue` khi mở, Esc đóng); tích hợp `GameCursorTargetResolver`/`GameCursorManager`; `PlayerHUDController.Refresh()`; công cụ `Assets/Editor/TeleportPillarBuilder.cs` (`Tools > Project Game > Boss > Teleport - Build Pillars`, idempotent).

**Cách chuyển scene:** `SceneFlowService.TryLoadGameplay` (giữ Bootstrap). Player là đối tượng *theo từng scene* nên `SceneTravel` giữ **bản chụp trong bộ nhớ** (level, kinh nghiệm, máu) rồi `SceneTravelArrival` ở scene đích khôi phục + đặt vị trí theo `SpawnRegistry` + làm mới HUD (`RestoreProgression` cố ý im lặng nên HUD không tự cập nhật chữ level). Kho đồ/trang bị/nhiệm vụ nằm ở Bootstrap nên sống sót tự nhiên. Không ghi đĩa.

**Rào chắn dữ liệu (theo AGENTS.md — rủi ro save/progression):**
- **Chỉ phiên không có save (Development) được du hành.** Phiên New Game/Continue có `GameSaveData` ⇒ `PlayerSpawnReadinessSource` sẽ khôi phục lại bản save ở scene đích (mất tiến độ chưa lưu, vị trí, nông trại) ⇒ trụ hiện thông báo *"Travel is unavailable in a saved game yet."* Cần quyết định riêng cách du hành trong phiên có save (chụp snapshot phiên trước khi đi, quy tắc lưu ở arena, nông trại/thế giới theo scene).
- Từ chối khi player đang gục (`"You cannot travel while defeated."`).

**Kiểm chứng Play Mode (DemoScene → arena → DemoScene):** level 2 / kinh nghiệm 50 / máu 70 giữ nguyên cả hai chiều; xuất hiện đúng `arena_entry` (0,−14) và `teleport_pillar_return` (−6, 0.1); HUD hiện level 2; UI hai tiêu đề đúng; trụ khóa khi `State != StatueIdle`. **Chưa tự kiểm chứng:** click chuột thật trên trụ (chuột thật ghi đè chuột giả lập trong phiên thử; đã kiểm tra phát hiện hover ở mức resolver — con trỏ báo Blocked khi ngoài tầm) — user thử giúp.

**Lỗi đã gặp và sửa:** (1) TMP tạo bằng code mặc định *NoWrap* nên mô tả thẻ bị cắt một dòng ⇒ đặt `textWrappingMode = Normal`; (2) trụ đặt ở phía đông nằm giữa nhóm Slime nên player về tới là bị đánh chết ⇒ dời sang phía tây; (3) HUD không cập nhật level sau khôi phục ⇒ `PlayerHUDController.Refresh()`; (4) HP 0 vẫn được mang sang scene mới ⇒ chặn du hành khi gục.

## 13. Shrine UI bỏ ngọc triệu hồi (D-090)

- Luồng: click tượng (Interact cursor, tầm 2.5) → `BossShrineUI` → đặt ngọc vào socket → AWAKEN → UI đóng → tượng biến mất, bụi, boss hiện dần.
- Dữ liệu: `item.material.orb_earth` (`Assets/Resources/Items/Materials/OrbEarth.asset`); số lượng yêu cầu `_summonItemCount` trên `BossArenaController`.
- Kiểm chứng Play Mode: 0 ngọc → AWAKEN khóa + gợi ý thiếu ngọc; có ngọc → PLACE → AWAKEN trừ đúng 1 (2→1), `State` Summoning→BossFight, boss xuất hiện, không lỗi console mới. **Chưa tự kiểm chứng:** click chuột thật vào tượng.
- Mở: nguồn ngọc (boss/loot sau này), Water/Wind orb tương tự, du hành khi phiên có SaveData.

## 14. Nghi lễ triệu hồi bằng laser (D-091)

- Luồng: AWAKEN → camera zoom ra toàn sân → 4 tượng gác tích năng mắt (1.2 s) → 4 laser đánh vào tượng triệu hồi (1.8 s) → nổ → bụi → boss hiện (State: Summoning → BossFight).
- Kiểm chứng Play Mode (BossArena_Earth, F6 giả lập AWAKEN, chậm 0.3×): tia từ cả 4 góc hội tụ đúng tượng giữa, nổ, boss xuất hiện, `SummonRitualFX` tự dọn, không lỗi console. Collider tượng gác: phủ y −0.1…1.4 so với gốc tượng (trước đó chỉ 0…0.9 nên đứng lên bệ được).
- Mở: animation tượng gác (mắt sáng/quay đầu), che HUD khi zoom, âm thanh nghi lễ (thuộc `CombatSkillSfxPlan`).

## 15. Skill nguyên tố + dash của player trong arena (D-093)

- Arena có `SkillTestHarness_DEBUG` (bản sao từ DemoScene) và dash bật cho `BossArena_Earth`. Tab đổi bộ Water → Earth → Wind, Q/E/R/T cast; Space = dash.
- Kiểm chứng Play Mode: dash được phép trong arena, harness hoạt động, cast Q vào chế độ nhắm. Chưa tự test từng skill đánh trúng boss (boss đã triển khai `IDamageable/ISlowable/IStunnable/IVulnerable`, cần bạn thử giúp).
- Mở: skill thật (mana/cooldown/UI hotbar), cân bằng sát thương skill vs HP boss 2400.

## 16. Water — "Bãi Biển Triều Cường" (ĐỀ XUẤT thay "Đảo Hồ Thần" §3, chờ user duyệt; user 2026-10-03)

User: *"phong cách đánh ngoài bãi biển có bãi cát và bãi biển"*. Đề xuất này **thay** cột Thủy "Đảo Hồ Thần" của D-083 (đảo tròn giữa hồ). Cơ chế triều (§3 `BossEncounterConcepts.md`) **giữ nguyên và được tận dụng tốt hơn** — bãi biển là nơi triều lên/xuống tự nhiên nhất. Chờ user duyệt rồi mới ghi D-097 và làm art.

### 16.1 Hình bóng (khác hẳn Địa chữ nhật kín)
**Vịnh bán nguyệt:** biển ở phía bắc *lõm vào* thành vịnh, bãi cát ôm vịnh; hai mỏm đá ngầm nhô ra biển ở hai góc trên; phía nam là cồn cát + vách đá có hang. Đọc bằng hình bóng: Địa = hộp vuông, Thủy = vành trăng lưỡi liềm, Phong = đài sao vỡ trên mây.

```
        ~~~~~~~~~~~~~~  BIỂN SÂU (collider mép nước, đồ trang trí: sóng, thuyền đắm, hải đăng xa)  ~~~~~~~~~~~~~~
   [Mỏm đá]  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  [Mỏm đá]
      "  .:'' '' ''  BÃI ƯỚT / VÙNG TRIỀU  (nước nông khi triều lên — player chậm 40%, cua nhanh ×2)  '' '' ''.:  "
   ,'                                                                                              ',
  /       BÃI CÁT KHÔ — vùng chiến đấu chính, TRỐNG (boss đi khắp nơi): gợn cát, vài vũng nước nhỏ        \
 |     [Totem san hô]                  [BỆ TƯỢNG CUA / ỐC TÙ VÀ — giữa bãi]                [Totem san hô]  |
 |                                                                                                        |
  \     [Totem san hô]            (đường mòn dấu chân dẫn về bệ)                    [Totem san hô]       /
   '.    CỒN CÁT + cỏ biển + dừa  ·····  VÁCH ĐÁ + CỬA HANG CUA (nơi cua trồi lên lúc vào trận)  ·····  .'
```

### 16.2 Các không gian (3 dải + vành)
| Dải | Vị trí | Vai trò |
|---|---|---|
| **Biển** | bắc, lõm vịnh | biên không đi được (collider mép nước); sóng loop, thuyền đắm, hải đăng xa, bọt trắng |
| **Bãi ướt / vùng triều** | dải ngay dưới biển, rộng ~8 đơn vị | **cơ chế riêng của map**: chu kỳ ~40 s nước dâng phủ dải này (nước nông: player chậm 40%, cua nhanh ×2) rồi rút để lộ vũng triều, vỏ sò, hang cua; P2 dâng nhanh ×2; P3 "Triều Cường" ngập gần hết bãi ướt + một phần bãi khô |
| **Bãi khô** | giữa, phần lớn diện tích | **sàn chiến đấu trống** (quy tắc §3C: không vật cản giữa sân); chỉ gợn cát, vài vũng nước nhỏ phẳng (decal) |
| **Cồn cát + vách đá** | nam | decor viền: dừa, cỏ biển, đống vỏ sò, củi trôi; **cửa hang cua** ở giữa vách = nơi cua trồi lên |
| **Hai mỏm đá ngầm** | hai góc bắc | trang trí + điểm neo tia nước nghi lễ (xem 16.4) |

### 16.3 Kích thước (theo D-088 — đo trên ảnh, không chỉ nhân số)
- Sân đi được (bãi ướt + bãi khô) **≈ 64 × 34 đơn vị** (cùng cỡ Địa 63.6 × 31.7) với **~8 đơn vị dải triều** nằm trong đó; bounding ảnh ≈ 92 × 58 vì có biển + cồn cát.
- Camera như Địa (ortho 12, boss fight tối đa 15.5). Player/boss tỉ lệ y hệt Địa (boss cua scale cùng cỡ ~6 đơn vị chiều ngang → càng dang ~8).
- Vành luôn trống ≥ 8 đơn vị từ biên cho boss (§3C.5).

### 16.4 Nghi lễ triệu hồi (khớp D-090/D-091)
- **Bệ giữa bãi:** tượng cua cổ hoặc **ốc tù và khổng lồ** nửa vùi trong cát (tượng triệu hồi).
- **4 Totem san hô** quanh bệ (thay 4 tượng gác golem): mắt/đá ngọc xanh biển phóng 4 tia nước về bệ → bệ nổ thành sóng cát/nước → **cua chui lên từ cát** (hoặc từ hang vách) thay vì hiện dần.
- Ngọc: **Water Orb** (`item.material.orb_water`, như Earth Orb D-090).

### 16.5 Chuyển động môi trường (bắt buộc §3B.4)
Bọt sóng vỗ liên tục ở mép nước (loop), **triều lên/xuống chạy bằng code** (mép nước trượt, lộ/phủ dải triều), lấp lánh trên cát ướt, lá dừa + cỏ biển đung đưa, bóng chim biển lướt qua sân, hạt cát bay theo gió, củi trôi nhấp nhô ở nước nông, cua nhỏ bò ở mép (trang trí), sương muối mỏng.

### 16.6 Gắn với skill cua (`BossEncounterConcepts.md` §3) — gợi ý chỉnh theo bãi biển
| Skill | Chỉnh cho bãi biển |
|---|---|
| Kìm Kẹp | giữ nguyên (cua đi ngang dọc mép nước) |
| Bong Bóng Giam | bong bóng bay từ biển vào |
| **Lặn Phục Kích** | cua **vùi xuống cát**: **gò cát** di chuyển bám theo thay vì gợn nước (đọc rõ trên cát khô), trồi lên nổ |
| **Sóng Triều** | sóng từ phía **biển (bắc) quét xuống bãi**, chỗ hổng = cồn cát/đá/củi trôi (đứng sau vật che) — đúng hình dạng bãi |
| Xoáy Nước | xoáy mở ở vùng triều/giữa bãi, hút về biển |

### 16.7 Kế hoạch art Pixellab (D-081 Pixen + animate_image; ngân sách còn ~198 gen đến 27/10, boss cua dùng cắt lớp miễn phí)
1. **Nền trống** (BaseMap): 1 ảnh Pixen ~512×424 vịnh cát (biển + bãi ướt + bãi khô + cồn), nhân dài theo quy trình Earth (`Tools/arena_extend_reference.py`); chuyển động triều = lớp nước/bọt riêng chồng lên (không bake vào nền). ~3–6 gen.
2. **Object** (no_background, PPU khớp map): bệ ốc tù và, totem san hô ×1 (nhân 4), mỏm đá ×2, dừa, cỏ biển, củi trôi, đống vỏ sò, thuyền đắm (trang trí). ~8–10 gen.
3. **Animation** (animate_image 64–96px, rẻ): bọt sóng loop, lá dừa/cỏ đung đưa, ngọc totem sáng, lấp lánh cát ướt, hạt cát. ~8–12 gen.
4. **Boss cua**: từ concept user (ảnh pixel art xanh/trắng) — làm sạch + tách nền (0 gen) + cắt lớp (đầu/mắt, hai càng, 6 chân, thân) animation miễn phí. 0–1 gen.
Tổng ước tính **≈ 20–30 gen**, còn dư.

### 16.8 Câu hỏi cho user (cần chốt trước khi ghi D-097 và làm art)
1. Duyệt thay "Đảo Hồ Thần" bằng **Bãi Biển Triều Cường** (vịnh bán nguyệt) chứ?
2. Tượng triệu hồi: **ốc tù và** hay **tượng cua cổ**? Totem san hô hay **cọc gỗ + đèn biển**?
3. Cua chui lên từ **cát giữa bãi** hay từ **hang ở vách nam**?
4. Triều phải **ảnh hưởng gameplay** (chậm 40% khi ở nước nông, cua nhanh ×2) như thiết kế, hay chỉ làm hiệu ứng nền ở bản đầu?

### 16.9 Kết quả nền (2026-10-03) — D-097

- Ứng viên: A (seed 21, bãi thẳng), B (seed 305, vịnh tròn), C (seed 777, vịnh nhỏ) ở `Assets/Art/BossArena/Water/Candidates/`; **user chọn A**.
- Nền chốt tạm: `Assets/Art/BossArena/Water/BaseMap_Water_Empty_1320x804.png` (nguồn `BaseMap_Water_Source_512x424.png`), sinh bằng `Tools/arena_extend_beach.py` (0 gen thêm). Sân đi được ≈ 1210 × 620 px (≈ Earth 1230 × 625).
- Còn lại cần làm sau khi duyệt: vài mảng cỏ/dừa góc dưới nằm sẵn trong nền (decor viền, hợp §3C), cần object riêng (bệ ốc tù và, totem, mỏm đá), lớp nước/bọt/triều, chuyển động môi trường.

### 16.10 Trạng thái dựng (D-102)
`BossArena_Water` có: nền + sóng animate, tấm nước triều, tượng ốc tù và (shrine), 4 totem san hô (ritual), cua + arena controller, trụ teleport về làng. Còn thiếu so với §16: mỏm đá, dừa/cỏ biển/vỏ sò/củi trôi, chim biển, cát bay.
