# ORYNTHALS — Kịch bản quay màn hình (Run Sheet)

Tài liệu này dùng **khi ngồi chơi game và bấm record**. Các cảnh được xếp theo **thứ tự quay** (gom theo save
slot và khu vực để đỡ phải đi lại), không theo thứ tự dựng phim. Mỗi cảnh ghi rõ mã shot trailer (T..) và đoạn demo (D..)
trong [KichBan_Trailer_Demo.md](KichBan_Trailer_Demo.md) mà cảnh đó phục vụ.

Tổng cộng: **6 buổi quay, 46 cảnh**, khoảng 4–6 giờ quay nếu đã dựng save sẵn.

---

## 0. Trước khi quay

### 0.1. Điều khiển (để tra nhanh)

| Phím | Chức năng |
|---|---|
| WASD | Di chuyển |
| Shift (giữ) | Chạy (tốn stamina) |
| Chuột trái | Đánh / click điểm câu / click ô đất / **click NPC** (rê chuột lên NPC trong tầm sẽ hiện "Talk", rồi click trái; phím E không dùng để nói chuyện) |
| Space | Dash |
| Chuột phải (giữ) | Shield |
| Q / E / R / T | Bắt đầu ngắm skill trong loadout → click trái để xác nhận |
| 1–4 | Chọn hoặc dùng ô quick bar |
| I | Inventory |
| P | Skills Panel (ở MapNhat, Skills Panel còn mở được qua thoại với Agnes) |
| M | Full Map |
| Esc | Pause Menu (Save Game, Return Main Menu) |

### 0.2. Hai bản Build

DevPanel chỉ hiện khi asset `Assets/Settings/Debug/DevModeSettings.asset` bật `Is Dev Mode Enabled`.

| Bản Build | DevMode | Dùng để |
|---|---|---|
| **Build A — Chuẩn bị** | Bật | Dựng save slot: Level Up, Spawn Item, Gold, God Mode, Teleport tới spawn hoặc NPC |
| **Build B — Quay** | Tắt | Quay mọi cảnh không cần God Mode, màn hình sạch hoàn toàn |

Hai bản dùng chung thư mục save, nên save dựng ở Build A sẽ mở được ở Build B. Cảnh boss nếu muốn bật God Mode thì quay
bằng Build A. Nếu nút mở DevPanel hiện trên màn hình, đặt nó ở góc và crop hoặc che khi dựng.

> Sau khi quay xong, **nhớ tắt lại DevMode** trước khi build bản phát hành (đúng như ghi chú trong `DevModeSettings`).

### 0.3. Sao lưu save

Save nằm ở `~/Library/Application Support/DefaultCompany/ProjectGame2D/Saves` (macOS). Sau khi dựng xong mỗi slot
ở phần 0.5, sao lưu cả thư mục để có thể khôi phục và quay lại bao nhiêu lần cũng được:

```bash
cp -R ~/Library/Application\ Support/DefaultCompany/ProjectGame2D/Saves ~/Desktop/OrynthalsSaves_Backup
```

Khôi phục (đóng game trước):

```bash
rm -rf ~/Library/Application\ Support/DefaultCompany/ProjectGame2D/Saves && cp -R ~/Desktop/OrynthalsSaves_Backup ~/Library/Application\ Support/DefaultCompany/ProjectGame2D/Saves
```

### 0.4. Thiết lập OBS / Recorder

- Quay cửa sổ game ở chế độ **fullscreen**, 1920×1080 hoặc 2560×1440, **60 fps**.
- Định dạng MKV (an toàn khi bị crash), CQP 15–18. Sau đó dùng *Remux* sang MP4.
- Hai track âm thanh nếu được: track 1 là âm thanh game, track 2 để trống (dự phòng cho lời bình).
- Ẩn con trỏ trong OBS ở các cảnh không có UI (chỉ khi game không tự vẽ con trỏ riêng).
- **Đặt tên file theo mã cảnh và số lần quay:** `R05_t1.mkv`, `R05_t2.mkv`, …
- **Quy tắc đệm:** bấm record, **chờ 2 giây** rồi mới làm, xong việc **chờ thêm 2 giây** rồi mới dừng. Khi dựng sẽ
  cần phần đệm này để chuyển cảnh.

### 0.4b. Quay bằng Unity Recorder (đã cài, bản 5.1.8)

Recorder chỉ chạy trong Unity Editor. Mở bằng: Window → General → Recorder → Recorder Window → **+ Add Recorder** → **Movie**.

| Thiết lập | Giá trị |
|---|---|
| Source | **Game View** nếu muốn có HUD · **Targeted Camera** (Main Camera) nếu muốn bỏ HUD |
| Output Resolution | 2560×1440 hoặc 1920×1080 |
| Format | MP4 (H.264), hoặc ProRes nếu muốn chất lượng gốc để dựng |
| Capture Audio | Bật |
| Playback | **Constant**, Target 60 fps |
| File Name | đặt theo mã cảnh, ví dụ `R04_<Take>` |

Dùng Recorder cho các cảnh cần sạch, đẹp: R04 qua cầu, R05 và R42–R46 (cảnh đứng yên), R24–R35 (từng skill).
Dùng OBS trên bản Build cho các cảnh chơi dài, cần căn thời điểm né: R22, R36, R38–R40 (boss). Lý do: khi bật Constant
mà máy yếu, game trong Editor sẽ chạy chậm lại lúc quay.

### 0.5. Dựng 3 save slot (dùng Build A)

**Slot 1 — New Game:** không cần dựng. Mỗi lần quay Buổi 1 thì chọn New Game và ghi đè lên slot 1.

**Slot 2 — Giữa Act I (dùng cho Buổi 2 và 3):**
1. New Game ở slot 2 và chơi đúng chuỗi quest: Wigheard → Cuthbert → Oswin → Eadric (trả quest đầy đủ).
2. Dừng **ngay trước khi nói chuyện với Leofrun**. Esc → Save Game → slot 2 → sao lưu thành bản `Slot2_TruocLeofrun`.
3. DevPanel → Spawn Item: `item.consumable.bait_small` × 20 (Roadmap ghi Quest 3 chưa có nguồn mồi trong game).
4. Lưu lại slot 2.

**Slot 3 — Cấp cao (dùng cho Buổi 4, 5, 6):**
1. New Game ở slot 3. Có thể bỏ qua Intro.
2. DevPanel → bấm **Level Up** khoảng **45 lần**. Học đủ 12 skill cần 42 Skill Point (mỗi hệ 2+3+4+5 = 14 điểm). Không dùng *Set Level* vì nút này không cộng Skill Point.
3. Chơi tới Agnes để mở Skills Panel (hoặc dùng phím P) → học đủ 12 skill.
4. Cho đồ: Gold 9999, thức ăn và thuốc (xem itemId trong Inventory hoặc `ResourceContent.md`).
5. Đi qua cổng phía bắc sang MapDuy, đứng gần **trụ teleport trước Ruined Temple** → Save vào slot 3 → sao lưu.

### 0.6. Loadout dùng lại nhiều lần

Mỗi lần chỉ gắn được 4 skill vào Q/E/R/T. Đổi loadout trong Skills Panel trước mỗi nhóm cảnh:

| Mã | Q | E | R | T | Dùng cho |
|---|---|---|---|---|---|
| **L-Water** | Spiral Water Bolt | Water Vortex | Spiral Water Beam | Threefold Tsunami | R24–R27 |
| **L-Earth** | Rock Bolt | Rock Arena | Rock Spike Field | Meteor Storm | R28–R31 |
| **L-Wind** | Returning Wind Blade | Whirlwind | Gale Fan | Wind Roc | R32–R35 |
| **L-Combo** | Spiral Water Bolt | Rock Spike Field | Whirlwind | Water Vortex | R21, R36, D05 |
| **L-Boss** | Spiral Water Bolt | Water Vortex | Threefold Tsunami | Meteor Storm | Buổi 5 |

---

## BUỔI 1 — MỞ ĐẦU & LÀNG HEORTE (Slot 1, Build B, ~45 phút)

### R01 · Main Menu → New Game
- **Dùng cho:** D01 (0:12–0:18)
- **Bắt đầu ở:** màn hình Main Menu (tắt rồi mở lại game, chờ qua splash StudioIntro)
- **Các bước:**
  1. Bắt đầu ghi, để Main Menu đứng yên **4 giây** (để có nền chữ cho logo).
  2. Rê chuột chậm qua các nút rồi click **New Game**.
  3. Màn chọn slot hiện ra: rê chậm qua **cả 3 slot** (mỗi slot khoảng 1 giây) → chọn slot 1 → xác nhận ghi đè nếu được hỏi.
  4. Giữ ghi qua màn Loading cho đến khi Intro bắt đầu.
- **Số lần quay:** 2
- **Đạt khi:** chuột di chuyển chậm, đều, không click nhầm.

### R02 · Splash studio (tùy chọn)
- **Dùng cho:** T01
- **Các bước:** khởi động game, ghi từ lúc màn đen đến hết splash `LogoSaigonMelon` (tối thiểu 8 giây).
- **Ghi chú:** có thể lấy thẳng file `LogoSaigonMelon.mp4` trong project thay vì quay, chất lượng sẽ tốt hơn.

### R03 · Intro Bard
- **Dùng cho:** D01 (0:18–0:30)
- **Ghi chú:** trailer dùng **file video gốc** trong `Assets/Cinematics/Intro/Videos/` (IntroScene1–5), không cần quay lại.
  Cảnh này chỉ quay để demo thấy được **khung thoại và các nút của game**.
- **Các bước:**
  1. Tiếp nối R01 (hoặc ghi riêng).
  2. Để 2 câu thoại đầu tự hiện, mỗi câu bấm **Space** sau khi đọc xong (khoảng 3 giây mỗi câu).
  3. Rê chuột lên nút **SKIP SCENE** (giữ 1 giây cho thấy nút) → click. Lặp lại ở 1–2 cảnh tiếp theo.
  4. Tiếp tục ghi qua Outro (fade đen) cho đến khi Timeline qua cầu bắt đầu. **Không dừng record**, chuyển luôn sang R04.
- **Số lần quay:** 1–2

### R04 · Qua cầu vào Heorte ⭐
- **Dùng cho:** T08, D01 (0:30–0:40)
- **Các bước:**
  1. Timeline tự chạy: nhân vật đi từ đầu trái cầu sang đầu phải cầu trong khoảng 4 giây. **Không bấm gì cả**, Timeline tự ẩn HUD.
  2. Khi Timeline kết thúc và HUD hiện lại, đứng yên **3 giây** rồi dừng ghi.
- **Số lần quay:** **3–5**. Muốn quay lại thì Esc → Return Main Menu → New Game slot 1 → SKIP INTRO.
- **Đạt khi:** không có vật gì che nhân vật, không giật khung hình. Đây là shot quan trọng của trailer.

### R05 · B-roll làng Heorte
- **Dùng cho:** T09, nền chữ trong demo
- **Các bước:**
  1. Đi tới chỗ thấy được nhiều NPC và quầy hàng nhất. Đứng yên, **không chạm phím**.
  2. Ghi **15 giây**.
  3. Lặp lại ở 3 vị trí khác: cổng làng, khu nông trại, bến câu cá.
- **Ghi chú:** nếu game có chế độ ẩn HUD thì bật lên. Nếu không có, khi dựng sẽ crop phần trung tâm.

### R06 · Wigheard giao quest A New Journey
- **Dùng cho:** T10, D02 (0:40–0:48)
- **Bắt đầu ở:** cách Wigheard khoảng 4–5 bước chân, quay mặt về phía ông ấy
- **Các bước:**
  1. Ghi. Đi bộ (không chạy) tới gần Wigheard.
  2. Rê chuột lên Wigheard, chờ chữ **Talk** hiện ra (giữ 1 giây) → click trái.
  3. Đọc thoại, mỗi câu bấm tiếp sau khoảng **2,5 giây** (đủ để người xem đọc).
  4. Hộp quest hiện lên: **giữ 2 giây** rồi mới bấm **Accept**.
  5. Chờ thông báo nhận Training Sword hiện ra, giữ 2 giây.
- **Số lần quay:** 2–3 (muốn quay lại thì New Game slot 1 → SKIP INTRO)

### R07 · Inventory: lướt tab và trang bị kiếm
- **Dùng cho:** T11, D02 (0:48–0:56)
- **Các bước:**
  1. Ghi, bấm **I**.
  2. Click lần lượt các tab **All → Weapon → Resources → Food → Potion**, mỗi tab dừng 1 giây. Sau đó quay lại tab **Weapon**.
  3. Rê chuột lên Training Sword để tooltip hiện **2 giây**.
  4. Kéo hoặc trang bị kiếm vào ô vũ khí. Dừng 2 giây để thấy nhân vật cầm kiếm.
  5. Đóng Inventory.
- **Số lần quay:** 2

### R08 · Trả quest Wigheard, Tracker chỉ đường
- **Dùng cho:** D02 (0:56–1:04)
- **Các bước:**
  1. Ghi. Đứng yên 2 giây để thấy Quest Tracker ở giữa bên trái màn hình.
  2. Click Wigheard → trả quest → thông báo phần thưởng.
  3. Đứng yên 3 giây để Tutorial banner và Tracker cập nhật chỉ sang **Cuthbert**.
  4. Đi về hướng Cuthbert khoảng 3 giây.

### R09 · Cuthbert và shop
- **Dùng cho:** T15, D02 (1:04–1:15)
- **Các bước:**
  1. Ghi. Click Cuthbert → hộp quest **A Drink Before the Road** → Accept (thấy nhận 140 G).
  2. Mở shop. Rê chuột lên **Carrot Juice** cho tooltip hiện 1 giây → mua 5 → mua 5 **Grape Juice**.
  3. Dừng 2 giây để thấy số Gold đã giảm → đóng shop.
- **Số lần quay:** 2
- **Đạt khi:** các thao tác mua dứt khoát, thấy rõ số Gold thay đổi.

---

## BUỔI 2 — NÔNG TRẠI & CÂU CÁ (Slot 2, Build B, ~1 giờ + thời gian chờ cây lớn)

> **Thứ tự quan trọng:** quay R10 trước (gieo hạt), sau đó lưu lại. Trong lúc chờ cây chín thì quay R13–R15 (câu cá).
> Cây chín rồi mới quay R11–R12. Thời gian lớn của cây do `CropDefinition` quy định và chạy theo giờ thực.

### R10 · Leofrun: nhận hạt, gán quick bar, gieo hạt ⭐
- **Dùng cho:** T12, D03 (1:15–1:25)
- **Bắt đầu ở:** khôi phục bản `Slot2_TruocLeofrun`, đứng gần Leofrun
- **Các bước:**
  1. Ghi. Click Leofrun → thoại → hộp quest **Back to the Fields** → Accept (nhận hạt Carrot, Grape, Lemon).
  2. Bấm **I** → kéo **Carrot Seed** vào ô quick bar số 1 → đóng Inventory.
  3. Bấm **1** (giữ 1 giây cho thấy ô sáng lên).
  4. Đi tới hàng ô đất trống, đứng giữa hàng (trong tầm 2,5 đơn vị) → **click 5 ô liên tiếp** với nhịp đều, khoảng 0,5 giây mỗi ô.
  5. Đứng lại 2 giây nhìn hàng mầm vừa gieo.
- **Quay thêm:** gieo nốt Grape và Lemon (tổng 15 ô) mà không cần ghi, sau đó **Save vào slot 2**.
- **Số lần quay:** 2–3 (khôi phục `Slot2_TruocLeofrun` để quay lại)

### R11 · Cây đang lớn có đồng hồ đếm ngược
- **Dùng cho:** D03 (1:25–1:30)
- **Các bước:** ngay sau R10, rê chuột qua vài ô cây đang lớn để thấy chỉ báo đếm ngược. Ghi **10 giây**.

### R12 · Thu hoạch ⭐
- **Dùng cho:** T13, D03 (1:30–1:35)
- **Bắt đầu ở:** khi cây đã chín (ô có dấu sẵn sàng)
- **Các bước:**
  1. Ghi. Đứng yên 2 giây cho thấy hàng cây chín có dấu sẵn sàng.
  2. Rê chuột lên ô đầu tiên (ô sáng nhẹ) → click. **Chờ vật phẩm bay về người nhân vật** rồi mới click ô tiếp theo.
  3. Thu hoạch 3–5 ô liền nhau.
  4. Đứng lại 2 giây.
- **Số lần quay:** chỉ quay được một lần cho mỗi đợt cây chín. **Trước khi quay, sao lưu save** để khôi phục và quay lại.
- **Đạt khi:** thấy rõ đường bay của vật phẩm.

### R13 · Farm Storage
- **Dùng cho:** D03 (1:35–1:40)
- **Các bước:** ghi → mở Farm Storage → kéo một chồng nông sản vào kho → dừng 2 giây → đóng.

### R14 · Câu cá ⭐
- **Dùng cho:** T14, D03 (1:40–1:52)
- **Bắt đầu ở:** bến câu, có Worm Bait, Inventory còn ít nhất 1 ô trống
- **Các bước:**
  1. Ghi. Bấm **I** → kéo Worm Bait vào quick bar → đóng → bấm phím của ô đó để chọn mồi.
  2. Click trái vào điểm câu (trong tầm).
  3. Chờ cá cắn. **Phần chờ sẽ được cắt khi dựng**, cứ để ghi.
  4. Khi dấu **`!`** hiện → click trái ngay → chơi minigame.
  5. Cá lên: giữ thông báo bắt được cá **3 giây**.
- **Số lần quay:** 4–6 (mỗi lần là một con cá khác nhau, chọn lần minigame trông kịch tính nhất)
- **Đạt khi:** có ít nhất một lần minigame giằng co rồi mới thắng.
- **Quay thêm (tùy chọn):** một lần bỏ lỡ dấu `!` để demo cho thấy game quay lại chờ cá khác.

### R15 · Cân nặng cá và trả quest với Oswin
- **Dùng cho:** D03 (1:52–2:05)
- **Điều kiện:** đã bắt đủ 5 cá cho quest **Cast a Line**
- **Các bước:**
  1. Ghi. Bấm **I** → rê chuột lên **2 con cá khác nhau** (mỗi con 2 giây) để thấy cân nặng khác nhau.
  2. Đóng Inventory → đi tới Oswin → click → trả quest → nhận **Trailrunner Fish Broth** → dừng 2 giây.

---

## BUỔI 3 — LÒ RÈN & PHÉP THUẬT (tiếp Slot 2, Build B, ~30 phút)

> Chơi tiếp slot 2 cho tới lúc Merric có quest. Sao lưu trước R16 để quay lại được.

### R16 · Merric và rèn Brasstan Edge ⭐
- **Dùng cho:** T16, D04 (2:05–2:18)
- **Các bước:**
  1. Ghi. Click Merric → quest **The Ring of the Forge** → Accept/trả → nhận **10 Gold Bars**.
  2. Quest **Forge the Brasstan Edge** tự bắt đầu. Mở Crafting của Merric.
  3. Chọn công thức **Brasstan Edge**: **dừng 2 giây** để thấy nguyên liệu (Training Sword + 10 Gold Bars).
  4. Bấm Craft → dừng 2 giây cho thấy vật phẩm mới.
  5. Đóng Crafting → trả quest cho Merric.
- **Số lần quay:** 2

### R17 · Lên cấp
- **Dùng cho:** D04 (2:18–2:25)
- **Các bước:** quay lúc trả quest mà nhân vật lên cấp (thường ngay trong R16). Nếu bị lỡ, dùng Build A → DevPanel
  **Level Up**, nhưng phải đóng DevPanel trước khi bấm (giữ màn hình sạch) — trường hợp này chỉ lấy được hiệu ứng lên cấp.

### R18 · Agnes và Skills Panel ⭐
- **Dùng cho:** T17, D04 (2:25–2:40)
- **Các bước:**
  1. Ghi. Click Agnes → quest **Far From Your Kind** → trong thoại chọn **"Teach me a skill."**
  2. Skills Panel mở ra: dừng 2 giây cho thấy toàn bộ panel.
  3. Rê chuột lần lượt lên một skill mỗi hệ (Water, Earth, Wind), mỗi skill để tooltip hiện **2 giây**.
  4. Trên **Spiral Water Bolt**: bấm **Learn** trong tooltip.
  5. Kéo Spiral Water Bolt vào ô **Q** → dừng 2 giây → đóng panel.
- **Số lần quay:** 2–3

---

## BUỔI 4 — VÙNG HOANG DÃ MapDuy (Slot 3, Build B, ~1,5 giờ)

> Khôi phục slot 3. Trước mỗi nhóm cảnh, mở Skills Panel và đổi loadout theo bảng 0.6.
> Nếu chết giữa chừng thì khôi phục save và quay lại.

### R19 · Qua cổng phía bắc sang MapDuy ⭐
- **Dùng cho:** T18, D05 (2:40–2:44)
- **Bắt đầu ở:** MapNhat, cách cổng phía bắc khoảng 6–8 bước (dùng Build A → Teleport nếu cần, rồi Save)
- **Các bước:** ghi → đi bộ (không chạy) qua cổng → giữ ghi qua màn chuyển → đi tiếp 3 giây vào MapDuy.
- **Số lần quay:** 2

### R20 · Full Map và Minimap
- **Dùng cho:** D05 (2:44–2:48)
- **Các bước:** ghi → bấm **M** → dừng 3 giây → đóng → dừng 2 giây để thấy Minimap.

### R21 · Chiến đấu cơ bản với Slime
- **Dùng cho:** T19, D05 (2:48–2:58) · Loadout: L-Combo (không dùng skill trong cảnh này)
- **Bắt đầu ở:** rìa vùng xanh có Slime1/Slime2
- **Các bước:**
  1. Ghi. Đứng yên 2 giây để bầy Slime nhảy tới gần.
  2. Chém thường bằng chuột trái, hạ 2–3 con.
  3. Giữ **Shift** chạy vòng sang chỗ khác. Thấy thanh stamina giảm.
- **Số lần quay:** 2

### R22 · Goblin và Dash ⭐
- **Dùng cho:** T20, D05 (2:58–3:02)
- **Bắt đầu ở:** khoảng trống màu đỏ (vùng Goblin1)
- **Các bước:** ghi → để một Goblin lao tới → khi nó vừa ra đòn thì **Space** Dash sang ngang → quay lại chém 2–3 nhát.
- **Số lần quay:** **4–6** (canh thời điểm Dash cho đẹp)
- **Đạt khi:** đòn của Goblin trượt rõ ràng nhờ Dash.

### R23 · Ent và Shield ⭐
- **Dùng cho:** T21, D05 (3:02–3:06)
- **Bắt đầu ở:** vùng cam phía tây (Ent)
- **Các bước:** ghi → đứng trước Ent → khi Ent vung đòn thì **giữ chuột phải** để Shield đỡ, khiên vỡ → lùi lại → chém trả.
- **Số lần quay:** 3–4

### R24–R35 · Từng skill riêng (12 cảnh)

Áp dụng chung cho mỗi skill:
1. Đứng ở chỗ có **nhiều quái tụ lại** (vùng Slime đông nhất). Riêng skill có thêm cột đá hay tường thì đứng gần tường hoặc cột.
2. Ghi → chờ 2 giây → bấm phím skill → **ngắm chậm 0,5 giây** (để thấy chỉ báo ngắm) → click → giữ ghi đến khi VFX tan hết + 2 giây.
3. Quay **3 lần** mỗi skill (chờ hết cooldown). Chọn lần trúng nhiều quái nhất.

| Mã | Skill | Phím (loadout) | Dùng cho | Gợi ý để có shot đẹp |
|---|---|---|---|---|
| R24 | Spiral Water Bolt | Q (L-Water) | T22 | Bắn dọc một hàng Slime đang xếp thẳng |
| R25 | Water Vortex | E (L-Water) | T26 | Đặt vào giữa bầy để thấy quái bị hút vào tâm |
| R26 | Spiral Water Beam | R (L-Water) | Dự phòng | Quét tia theo đường bầy quái đang lao tới |
| R27 | Threefold Tsunami | T (L-Water) | T33 (dự phòng) | Quay Wide, để 3 đợt sóng và rồng nước lọt khung |
| R28 | Rock Bolt | Q (L-Earth) | Dự phòng | Bắn Goblin ở tầm xa |
| R29 | Rock Arena | E (L-Earth) | T27 | Đặt lên một nhóm 3–4 quái |
| R30 | Rock Spike Field | R (L-Earth) | T23 | Hướng quạt gai về phía Goblin đang chạy tới |
| R31 | Meteor Storm | T (L-Earth) | T33, D06 | Đặt vào giữa bầy đông nhất, quay Wide |
| R32 | Returning Wind Blade | Q (L-Wind) | T24 | Đứng xa để thấy lưỡi gió bay đi rồi quay về |
| R33 | Whirlwind | E (L-Wind) | T25 | Cho lốc chạy qua bầy Slime |
| R34 | Gale Fan | R (L-Wind) | T27 | Đứng sao cho quái bị đẩy vào tường hoặc cột (có thêm sát thương và choáng) |
| R35 | Wind Roc | T (L-Wind) | T28 | Quay Wide, đợi đúng lúc chim quắp quái lên rồi ném |

### R36 · Combo 4 skill ⭐
- **Dùng cho:** T27, D05 (3:06–3:20) · Loadout: **L-Combo**
- **Các bước:**
  1. Kéo một bầy lớn theo mình (chạy qua vùng Slime cho chúng đuổi theo), rồi quay lại đối mặt.
  2. Ghi. **T** Water Vortex hút bầy lại → **E** Rock Spike Field làm choáng → **R** Whirlwind nhấc bổng → **Q** Spiral Water Bolt kết liễu.
  3. Dừng 2 giây để thấy cả 4 ô SkillHUD (góc dưới phải) đang hồi chiêu và thanh mana giảm.
- **Số lần quay:** **5+** (đây là shot khó, cần nhịp nhanh và gọn)
- **Đạt khi:** cả 4 skill trong khoảng 6 giây, kết thúc khi bầy quái chết gần hết.

### R37 · Trụ teleport Ruined Temple ⭐
- **Dùng cho:** T29, D06 (3:20–3:26)
- **Các bước:**
  1. Ghi. Đi tới trụ teleport trước Ruined Temple → tương tác.
  2. Trình chọn đích hiện ra: rê chậm qua cả 3 đấu trường, mỗi cái 1 giây → chọn **Earth**.
  3. Giữ ghi qua màn chuyển cho đến khi vào Thạch Đài và boss xuất hiện.
- **Số lần quay:** 2

---

## BUỔI 5 — BOSS (Slot 3, ~1,5–2 giờ)

> Loadout: **L-Boss**. Ăn đồ, hồi đầy máu và mana trước mỗi trận. Nếu muốn chắc không chết thì dùng Build A, bật
> **God Mode**, rồi đóng DevPanel trước khi ghi.
>
> **Cách quay boss:** ghi **trọn cả trận liên tục** (một file dài), không dừng giữa chừng. Khi dựng sẽ tìm các khoảnh
> khắc đẹp trong file đó. Mỗi boss quay **tối thiểu 2 trận đầy đủ**. Sau đó quay thêm một số "shot săn" riêng
> (bên dưới) nếu trong trận bị lỡ.

### R38 · Earth Golem — trận đầy đủ ⭐⭐
- **Dùng cho:** D06 toàn bộ (3:20–4:20), T30a, T30b, T33, D00 (cold open)
- **Chiến thuật để có hình đẹp** (không chỉ để thắng):

| Giai đoạn | Việc cần làm | Vì sao |
|---|---|---|
| Phase 1 | **Đừng đánh vội.** Né từng đòn rõ ràng: chạy ra khỏi vòng đỏ của *Thiết Quyền Nghiền*, bước ngang qua các vệt của *Địa Mạch Gai*, luồn giữa các vòng của *Mưa Đá Vỡ*. Né sát mép vòng đỏ | Lời bình demo cần thấy rõ "đọc báo hiệu rồi di chuyển" |
| Recovery | Khi boss rũ người: dồn **Q** và **E** | Cho thấy "cửa sổ" phản công |
| Đổi phase | Đứng ở khoảng cách vừa để lọt cả boss lẫn rune sàn khi nó gầm | Shot T30a |
| *Phi Quyền* | Đứng tránh vệt đỏ, để nắm đấm bay ngang qua rồi quay về | Demo 3:48–3:56 |
| *Cộng Hưởng Lõi* | Luồn qua làn an toàn giữa các đợt đạn đá, **Dash** qua đợt cuối | Shot T30b và cold open |
| Recovery dài sau tuyệt kỹ | **R** Threefold Tsunami → **T** Meteor Storm | Shot T33 |
| Hạ boss | Đứng yên 4 giây sau khi boss vỡ vụn | Đoạn kết D06 |

- **Số lần quay:** 2–3 trận.
- **Shot săn thêm** (mỗi shot một file ngắn): `R38a` boss gầm đổi phase · `R38b` một đợt Cộng Hưởng Lõi né hoàn hảo · `R38c` Meteor Storm trúng boss đang Recovery · `R38d` boss chết.

### R39 · Tidal Crab — trận đầy đủ ⭐
- **Dùng cho:** T31a, T31b, D07 (4:20–4:27)
- **Vào trận:** trụ teleport → chọn **Water** → Bãi Biển Triều Cường
- **Ưu tiên quay được:**
  - Lúc **triều dâng**: nước tràn lên vành ngoài, vùng đứng được co lại. Đứng ở chỗ thấy được mép nước dâng.
  - *Lặn Phục Kích*: cua lặn xuống cát → gò cát đuổi theo nhân vật → vòng đỏ hiện ra → Dash ra ngoài → cua trồi lên nổ.
  - *Xoáy Nước*: vòng đỏ lớn mở ra → nhân vật chạy và Dash thoát ra rìa.
- **Số lần quay:** 2 trận

### R40 · Sky Owl — trận đầy đủ ⭐
- **Dùng cho:** T32a, T32b, D07 (4:27–4:35)
- **Vào trận:** trụ teleport → chọn **Wind** → Thiên Đài
- **Ưu tiên quay được:**
  - *Lao Vuốt*: cú bay khỏi khung → bóng cú lớn dần trên sàn → chạy ra khỏi bóng → cú lao xuống.
  - Gió đẩy nhân vật trôi về phía mép vực trong khi nhân vật cố đi ngược lại.
  - *Bão Táp*: trời tối, sàn ở hai rìa vỡ dần, nhân vật đứng giữa vùng an toàn.
- **Số lần quay:** 2 trận

---

## BUỔI 6 — LƯU GAME & B-ROLL (Slot 3, Build B, ~20 phút)

### R41 · Save và Continue
- **Dùng cho:** D08 (4:35–4:40)
- **Các bước:**
  1. Ghi ở MapDuy. **Esc** → Pause Menu → **Save Game** → chọn slot 3 → Overwrite.
  2. **Return Main Menu** → **Continue** → chọn slot 3 → Loading.
  3. Nhân vật hiện lại đúng chỗ cũ. Đứng yên 2 giây.

### R42–R46 · B-roll không có thao tác

Mỗi cảnh đứng yên, không chạm phím, ghi **15 giây**. Dùng làm nền chữ, thẻ tiêu đề và cảnh chuyển.

| Mã | Vị trí |
|---|---|
| R42 | Ruined Temple và trụ teleport (MapDuy) |
| R43 | Rừng có Ent ở xa (MapDuy) |
| R44 | Thạch Đài khi chưa vào trận (nếu có thể) |
| R45 | Bãi Biển Triều Cường |
| R46 | Thiên Đài |

---

## Phụ lục A — Bảng theo dõi buổi quay

In ra hoặc chép sang bảng tính. Đánh dấu ✅ vào lần quay tốt nhất để dựng nhanh hơn.

| Mã | Tên cảnh | Ưu tiên | t1 | t2 | t3 | t4 | t5 | Lần tốt nhất | Ghi chú |
|---|---|---|---|---|---|---|---|---|---|
| R01 | Main Menu → New Game | Thường | | | | | | | |
| R03 | Intro Bard | Thường | | | | | | | |
| R04 | Qua cầu | ⭐ | | | | | | | |
| R05 | B-roll làng | Thường | | | | | | | |
| R06 | Wigheard | Thường | | | | | | | |
| R07 | Inventory | Thường | | | | | | | |
| R08 | Trả quest + Tracker | Thường | | | | | | | |
| R09 | Cuthbert shop | Thường | | | | | | | |
| R10 | Gieo hạt | ⭐ | | | | | | | |
| R11 | Cây đang lớn | Thường | | | | | | | |
| R12 | Thu hoạch | ⭐ | | | | | | | |
| R13 | Farm Storage | Thường | | | | | | | |
| R14 | Câu cá | ⭐ | | | | | | | |
| R15 | Cân nặng cá + Oswin | Thường | | | | | | | |
| R16 | Merric rèn đồ | ⭐ | | | | | | | |
| R17 | Lên cấp | Thường | | | | | | | |
| R18 | Agnes + Skills Panel | ⭐ | | | | | | | |
| R19 | Cổng phía bắc | ⭐ | | | | | | | |
| R20 | Full Map | Thường | | | | | | | |
| R21 | Chém Slime | Thường | | | | | | | |
| R22 | Dash né Goblin | ⭐ | | | | | | | |
| R23 | Shield đỡ Ent | ⭐ | | | | | | | |
| R24–R35 | 12 skill | ⭐ | | | | | | | |
| R36 | Combo 4 skill | ⭐ | | | | | | | |
| R37 | Trụ teleport | ⭐ | | | | | | | |
| R38 | Earth Golem | ⭐⭐ | | | | | | | |
| R39 | Tidal Crab | ⭐ | | | | | | | |
| R40 | Sky Owl | ⭐ | | | | | | | |
| R41 | Save và Continue | Thường | | | | | | | |
| R42–R46 | B-roll | Thường | | | | | | | |

## Phụ lục B — Xử lý sự cố khi quay

| Sự cố | Cách xử lý |
|---|---|
| Chết giữa cảnh | Khôi phục save từ bản sao lưu, quay lại. Cảnh boss thì dùng God Mode (Build A) |
| Quest bị lệch thứ tự, không làm được cảnh tiếp theo | Khôi phục `Slot2_TruocLeofrun` hoặc bản sao lưu gần nhất |
| Cây chưa chín khi cần quay R12 | Quay trước R14–R18 trong lúc chờ. Lần sau nhớ gieo sẵn từ đầu buổi |
| Không có mồi câu | Build A → DevPanel → Spawn Item `item.consumable.bait_small` |
| Thiếu Skill Point | Build A → bấm **Level Up** (không dùng *Set Level* vì nút này không cộng điểm) |
| Game giật khi quay | Giảm độ phân giải ghi xuống 1080p, tắt các ứng dụng khác, kiểm tra OBS dùng hardware encoder |
| Lỗi hoặc kẹt ở trụ teleport, quái MapDuy, boss | Roadmap ghi các phần này còn chờ kiểm tra Play Mode. Ghi lại lỗi, báo cho team sửa trước, rồi quay lại cảnh đó sau |
