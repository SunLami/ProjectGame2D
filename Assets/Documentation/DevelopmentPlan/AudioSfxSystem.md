# Audio SFX System — MainMenu / IntroCutscene / MapNhat

Status: `ACCEPTED (D-058) — Phase A đã giao Codex gen, xem Handoffs/ClaudeToCodex.md`
Ngày: 2026-09-28

Tài liệu này là nguồn chuẩn cho toàn bộ đợt sản xuất SFX một-shot (không phải nhạc nền — nhạc nền
tiếp tục do `MusicManager` sở hữu, không đổi) cho ba scene `MainMenu`, `IntroCutscene`, `MapNhat`.
Đọc tài liệu này trước khi sửa `SoundFXManager`/`SoundFXLibrary`, trước khi giao việc gen audio cho
Codex, và trước khi wire SFX vào bất kỳ UI/gameplay script nào.

## 1. Hiện trạng đã audit (2026-09-28)

- Toàn bộ game hiện chỉ phát đúng 2 loại âm thanh: nhạc nền (`MusicManager`) và footstep
  (`SoundFXManager.PlayFootSteps`). Không có Button, popup, tương tác world, combat, commerce hay
  quest nào phát SFX.
- [SoundFXLibrary.cs](../../Scripts/SoundFXLibrary.cs) đã tồn tại sẵn (dictionary `groupName →
  List<AudioClip>`, `GetRandomClip(name)`) nhưng **không có nơi nào trong code đang dùng nó** — đây
  là hạ tầng catalog dựng sẵn, sẽ được tái sử dụng thay vì viết catalog mới.
- [SoundFXManager.cs](../../Scripts/GameManagers/SoundFXManager.cs) là Application audio service
  (`ServiceOwnershipLifecycle.md`), static singleton, `DontDestroyOnLoad`, có một `AudioSource` dùng
  chung. Volume tổng do `SettingsService.SetSfxVolume` áp trực tiếp lên `AudioSource.volume`; mọi
  `PlayOneShot(clip, volumeScale)` tự động nhân với volume này — **không cần thêm cơ chế volume
  riêng cho SFX mới**.
- `SettingsService` chỉ có đúng hai kênh volume: `SfxVolume` và `MusicVolume` (không có kênh phụ
  Ambience/Voice riêng). Toàn bộ SFX trong tài liệu này đều đi qua kênh `SfxVolume` chung.

## 2. Kiến trúc áp dụng (không tạo manager mới)

Giữ đúng boundary "Application audio service" đã ghi trong `ServiceOwnershipLifecycle.md` — chỉ mở
rộng hai file có sẵn, không thêm persistent singleton mới:

1. **`SoundFXManager`** thêm một API tổng quát:
   ```csharp
   public static void PlaySfx(string sfxId, float volumeScale = 1f)
   ```
   Resolve clip qua `SoundFXLibrary.GetRandomClip(sfxId)` rồi `_audioSource.PlayOneShot(clip,
   volumeScale)`. Không tách AudioSource riêng cho SFX one-shot — `PlayOneShot` hỗ trợ chồng nhiều
   clip cùng lúc trên một AudioSource, đúng nhu cầu (VD nhiều hit combat dồn dập).
2. **`SoundFXLibrary`** trở thành catalog chính thức, đặt trên cùng persistent GameObject với
   `SoundFXManager` (Bootstrap). Mỗi `SoundFXGroup.groupName` = đúng một SFX ID trong bảng §4.
3. **UI button** dùng một component nhỏ `ButtonSfx` (mới, presentation-only) gắn `IPointerEnterHandler`/
   `IPointerClickHandler`, gọi `SoundFXManager.PlaySfx("sfx.ui.hover"/"sfx.ui.click_primary"...)`.
   Không sửa từng Button script hiện có.
4. **Gameplay/world/combat** gọi `SoundFXManager.PlaySfx(id)` trực tiếp tại đúng điểm domain event đã
   tồn tại (VD `ChestInteractable` sau khi animation mở xong, `PlayerAttackHitbox` khi swing) — SFX
   là presentation, không được đặt trong domain logic sở hữu save/progression.
5. **Không đổi** `MusicManager`, `SettingsService`, save schema, hay bất kỳ `GameState`/domain
   contract nào. Đây thuần là lớp presentation audio mới.

Việc code hoá `PlaySfx`/`ButtonSfx`/wiring từng điểm gọi là phần việc của Claude, **không phải của
Codex** — Codex chỉ gen file audio theo bảng §4.

## 3. Convention

- **SFX ID**: dot-namespace `sfx.<category>.<action>`, theo đúng tinh thần convention item mới của
  D-022 (`DecisionRegister.md`). ID là key tra `SoundFXLibrary`, không phải tên file.
- **Category** (= `groupName` = cũng là thư mục asset): `ui`, `combat`, `world`, `commerce`, `quest`,
  `dialogue`, `fishing`, `farming`, `system`.
- **File audio**: `Assets/Resources/Audio/SFX/<Category>/<sfx_id với dấu chấm đổi thành gạch dưới>.wav`
  — ví dụ `sfx.ui.click_primary` → `Assets/Resources/Audio/SFX/UI/sfx_ui_click_primary.wav`.
- **Import spec bắt buộc cho mọi file** (Claude tự set sau khi Codex gen xong, Codex không cần biết):
  Mono, PCM 44.1kHz, Load Type = Decompress On Load (clip one-shot ngắn), không loop.
- **Thời lượng**: one-shot UI/feedback 0.1–0.6s; combat/world impact 0.2–0.8s; các SFX có "thoại"/
  cảm xúc dài hơn (VD catch success) tối đa 1.5s. Không SFX nào vượt 2s — đây là hiệu ứng, không phải
  nhạc/ambience.

## 4. Danh mục SFX cần gen

Cột **Mô tả gen** dùng làm **search query trên Freesound.org** (nguồn free — không dùng
`generate_audio`/fal, xem lý do đổi ở §6). Tất cả theo tinh thần Dark Light Fantasy
(`DarkLightFantasyUIStyleGuide.md`, D-041) — âm sắc gỗ/kim loại cổ điển, ấm, không điện tử
hiện đại/8-bit trừ khi ghi chú riêng.

### Phase A — UI chung (áp dụng cả 3 scene, ưu tiên cao nhất vì đang câm 100%)

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.ui.hover` | Tiếng "tick" gỗ rất nhẹ, cụt ngắn, dùng khi hover qua button/menu item | 0.1–0.15s | Mọi Button hover |
| `sfx.ui.click_primary` | Tiếng click gỗ/kim loại ấm, xác nhận rõ ràng, dùng cho hành động chính (Confirm/New Game/Accept) | 0.15–0.25s | Confirm-style button |
| `sfx.ui.click_secondary` | Tiếng click nhẹ hơn click_primary, trung tính, dùng cho Back/Cancel/Close | 0.15–0.25s | Cancel/Close/Back button |
| `sfx.ui.toggle` | Tiếng cơ khí nhỏ kiểu gạt công tắc, dùng cho checkbox/fullscreen toggle | 0.15–0.2s | Settings toggle |
| `sfx.ui.error` | Tiếng cảnh báo trầm ngắn, không chói tai, dùng khi thao tác thất bại | 0.2–0.4s | `OnOperationFailed`, insufficient funds/materials, inventory full |
| `sfx.ui.popup_open` | Tiếng "vải/giấy da" mở nhẹ kèm chuông ngân rất khẽ | 0.2–0.35s | Mở Pause/Inventory/Settings/Quest Log/Character Popup |
| `sfx.ui.popup_close` | Biến thể ngược của popup_open, gọn hơn | 0.15–0.25s | Đóng các popup trên |

### Phase B — MainMenu & IntroCutscene

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.ui.click_primary` (tái dùng) | — | — | New Game/Continue/Overwrite confirm — [MainMenuController.cs](../../Scripts/GameManagers/MainMenuController.cs) |
| `sfx.ui.click_secondary` (tái dùng) | — | — | Delete Slot cancel, đóng Settings |
| `sfx.ui.error` (tái dùng) | — | — | `MainMenuController.OnOperationFailed` |
| `sfx.ui.click_secondary` (tái dùng) | — | — | Next/Skip Scene/Skip Intro — [IntroCutsceneController.cs](../../Scripts/Cinematics/Intro/IntroCutsceneController.cs) `_nextButton`/`_skipSceneButton`/`_skipIntroButton` |
| `sfx.cutscene.transition` *(P2 — tuỳ chọn)* | Whoosh điện ảnh ngắn, tối màu, dùng khi chuyển segment | 0.4–0.6s | `MoveToNextSegment` |

Ghi chú: Cutscene **không** cần bộ dialogue-blip riêng ở đợt đầu — rủi ro phá nhịp đọc thoại nếu gen
sai tempo; để lại Phase sau nếu owner muốn.

### Phase C — MapNhat (khối lớn nhất)

**Inventory/Equipment**

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.world.equip` | Tiếng kim loại/da khoác lên người, chắc gọn | 0.3–0.5s | Equip item — `InventoryWindowUI.cs` |
| `sfx.world.unequip` | Biến thể ngược, nhẹ hơn | 0.2–0.4s | Unequip item |
| `sfx.ui.item_pickup` | Tiếng nhặt/chạm item nhỏ, gọn, khác hover | 0.1–0.2s | Bắt đầu kéo item trong lưới |
| `sfx.ui.item_drop` | Tiếng đặt item vào ô, khớp nhẹ | 0.15–0.25s | Thả item vào slot hợp lệ |

**Quest**

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.quest.accept` | Chuông ngân ấm, cảm giác "bắt đầu hành trình" | 0.4–0.7s | `QuestAcceptPopupUI.cs` Accept |
| `sfx.quest.objective_complete` | Tiếng "tách" xác nhận ngắn, tích cực | 0.2–0.3s | Objective hoàn thành — `QuestTrackerUI.cs` |
| `sfx.quest.complete` | Chuông ngân dài hơn accept, rõ ràng là phần thưởng lớn | 0.6–1s | Turn-in quest hoàn tất |

**Commerce (Shop/Crafting)**

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.commerce.buy` | Tiếng đồng xu chạm nhẹ 2–3 lần | 0.3–0.5s | `ShopCraftingUI.cs` Buy thành công |
| `sfx.commerce.sell` | Biến thể buy, âm sắc trầm hơn một chút | 0.3–0.5s | Sell thành công |
| `sfx.commerce.craft` | Tiếng búa/đe chạm gọn, một nhịp | 0.3–0.5s | `CraftingManager.cs` Craft thành công |

**Dialogue NPC (gameplay, khác Intro)**

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.ui.click_secondary` (tái dùng) | — | — | Next-line/đóng — `DialogueUI.cs`, `QuestNpcInteractionUI.cs` |

**World interaction**

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.world.chest_open` | Bản lề gỗ/kim loại cũ kẽo kẹt, kèm khoá bật | 0.4–0.6s | `ChestInteractable.cs` sau animation mở |
| `sfx.world.pickup_unique` | Tiếng "sparkle" nhỏ, đặc biệt hơn item thường | 0.3–0.4s | `UniquePickupInteractable.cs` |
| `sfx.world.harvest_wood` | Tiếng rìu chặt gỗ, một nhát gọn | 0.2–0.35s | `ResourceNodeInteractable.cs` loại Chopping |
| `sfx.world.harvest_stone` | Tiếng cuốc/khoáng vật va đá | 0.2–0.35s | Loại Mining |
| `sfx.world.harvest_gather` | Tiếng bứt lá/cỏ nhẹ | 0.15–0.3s | Loại Gathering |

**Combat**

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.combat.player_attack` | Tiếng vung vũ khí xé gió | 0.2–0.35s | `PlayerAttackHitbox.cs` |
| `sfx.combat.hit_impact` | Tiếng va chạm trúng đòn, chắc | 0.15–0.3s | Đòn trúng địch/Player (`MannequinHitReaction.cs`, `EnemyUniversal.cs`) |
| `sfx.combat.enemy_death` | Âm hạ thấp dần, kết thúc rõ ràng | 0.4–0.6s | Enemy chết |
| `sfx.combat.projectile_launch` | Tiếng phóng vật thể qua không khí | 0.2–0.3s | `UniversalEnemyProjectile.cs` bắn |
| `sfx.combat.projectile_impact` | Tiếng nổ/va chạm nhỏ | 0.2–0.3s | Projectile trúng đích |

**Fishing** *(chỉ gen nếu fishing spot đã đặt trong MapNhat)*

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.fishing.cast` | Tiếng quăng cần, dây rít nhẹ rồi tõm nước | 0.4–0.6s | Bắt đầu fishing |
| `sfx.fishing.bite` | Tiếng "giật" nước đột ngột, báo hiệu cắn câu | 0.2–0.3s | Bite event |
| `sfx.fishing.reel` | Tiếng cuộn dây cơ khí lặp nhịp | 0.2–0.3s | Trong minigame kéo cá |
| `sfx.fishing.catch_success` | Tiếng nước bắn tung phấn khích, ngắn | 0.5–0.8s | Bắt thành công |
| `sfx.fishing.catch_fail` | Tiếng nước rơi tõm buồn, hạ tông | 0.3–0.5s | Bắt hụt/hết giờ |

**Farming** *(chỉ gen nếu farm plot đã đặt trong MapNhat)*

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.farming.plant` | Tiếng đất mềm, hạt rơi xuống | 0.2–0.35s | `FarmPlot.cs` gieo hạt |
| `sfx.farming.harvest` | Tiếng bứt cây trưởng thành, tươi mới | 0.25–0.4s | Thu hoạch crop mature |

**System**

| SFX ID | Mô tả gen | Thời lượng | Trigger |
| --- | --- | --- | --- |
| `sfx.system.save_success` | Chuông ngân rất khẽ, trấn an, khác hẳn quest complete | 0.3–0.5s | Save Game ghi file thành công |

Ambience theo khu vực (gió, nước, rừng) **không** nằm trong đợt này — khác cơ chế (loop dài, không
phải one-shot `PlayOneShot`), để lại phase sau nếu owner muốn.

## 5. Thứ tự triển khai

1. **Phase A** (UI chung 7 SFX) — phủ MainMenu + Pause/Inventory/Settings MapNhat, giá trị cao nhất vì
   mật độ tương tác cao nhất và đang câm hoàn toàn.
2. **Phase B** (MainMenu/IntroCutscene phần còn lại) — tái dùng phần lớn Phase A, chỉ thêm
   `sfx.cutscene.transition` nếu owner muốn.
3. **Phase C** (MapNhat gameplay) — theo thứ tự phụ: Inventory/Equipment → World interaction/Combat →
   Quest/Commerce → Fishing/Farming (chỉ nếu đã có trong scene).

Mỗi phase: Codex/Claude lấy asset theo bảng → Claude review file (đúng thời lượng, không clip, không
nền ồn) → import đúng spec §3 → set vào `SoundFXLibrary` groups trên Bootstrap → wire `PlaySfx` tại
đúng điểm code → Play Mode verify từng nhóm trước khi sang phase kế.

**Trạng thái Phase A (2026-09-28):** 7 file đã lấy từ Freesound và import (`CREDITS.md` đầy đủ).
Code đã xong: `SoundFXManager.PlaySfx`, `ButtonSfx` (component tự phát hover/click), 7 group đã set
vào `SoundFXLibrary` trên `Bootstrap.unity`, PlayMode test `SoundFXManagerPlayModeTests.cs`. **Còn
lại cần Unity Editor thật để hoàn tất** (không có Unity session sống trong môi trường này lúc code):
chạy `Tools/ProjectGame2D/Audio/Attach Button SFX To Open Scene` trên từng scene
(`MainMenu.unity`, `MapNhat.unity`) để gắn `ButtonSfx` vào toàn bộ Button/Toggle hiện có, review lại
phân loại Primary/Secondary trong Inspector, và Play Mode verify thật (nghe được, không lỗi console)
trước khi coi Phase A là Done theo Definition of Done chung của project.

## 6. Nguồn audio: Freesound.org (free) thay vì `generate_audio`/fal

`generate_audio` trong MCP hiện chỉ wire đúng 1 provider (`fal`, mọi model đều qua fal.ai) — tính phí
theo API key BYOK, không có lựa chọn free. Theo yêu cầu 2026-09-28, đổi nguồn sang **Freesound.org**:
thư viện sound effect free (CC0/CC-BY) do người dùng thật thu âm/upload, tìm bằng search query thay vì
generate bằng AI prompt.

**Quy tắc chọn file trên Freesound:**
- Ưu tiên license **CC0** (không cần credit). Chỉ dùng **CC-BY** khi không có lựa chọn CC0 phù hợp,
  và bắt buộc ghi lại tên tác giả + link gốc vào `Assets/Resources/Audio/SFX/CREDITS.md` (tạo mới nếu
  chưa có) — đây là điều kiện bắt buộc của license, không phải tuỳ chọn.
- Không dùng file có watermark/announcer voice đè lên, không dùng file có tạp âm nền lớn (foley thu
  trong phòng ồn).
- Ưu tiên file có sẵn độ dài gần đúng cột Thời lượng; nếu file gốc dài hơn, cắt (trim) về đúng phần
  âm thanh chính, bỏ khoảng lặng đầu/cuối — không upload nguyên file dài nhiều giây cho một hiệu ứng
  0.2s.

**Codex làm:**
- Tìm trên Freesound.org theo cột Mô tả gen (dùng làm search query, có thể diễn đạt lại bằng tiếng
  Anh cho ra kết quả tốt hơn), chọn đúng 1 file khớp nhất cho mỗi `sfx_id` theo bảng §4.
- Tải file, convert/trim về đúng convention §3: mono, PCM 44.1kHz WAV, đúng khoảng Thời lượng, không
  loop, không khoảng lặng thừa đầu/cuối.
- Đặt tên và lưu đúng path theo §3 (`Assets/Resources/Audio/SFX/<Category>/<sfx_id với dấu chấm đổi
  gạch dưới>.wav`).
- Ghi license + tác giả + URL gốc mỗi file vào `Assets/Resources/Audio/SFX/CREDITS.md`.
- Báo lại theo đúng pattern `Handoffs/ClaudeToCodex.md` → `CodexToClaude.md`: liệt kê file đã lấy,
  nguồn Freesound (URL), license, và bất kỳ trường hợp không tìm được file khớp mô tả cần Claude duyệt
  lại query hoặc đổi mô tả.

**Codex không cần/không được làm:**
- Không sửa `SoundFXManager.cs`, `SoundFXLibrary.cs`, hay bất kỳ script nào — chỉ lấy và chuẩn hoá
  audio.
- Không tự đặt tên file khác convention §3 (Claude cần tên khớp để wire tự động).
- Không dùng file license NC (non-commercial) hoặc không rõ license.
- Không lấy nhạc nền/ambience loop dài — ngoài phạm vi tài liệu này.
- Không lấy SFX nào chưa có trong bảng §4 mà không hỏi trước.

## 7. Definition of Done

- Toàn bộ SFX ID trong phase đang làm có file audio đã import đúng spec, gán vào đúng group trong
  `SoundFXLibrary` trên Bootstrap.
- `SoundFXManager.PlaySfx` có test PlayMode tối thiểu (resolve đúng clip theo ID, không lỗi khi ID
  không tồn tại), theo cùng chuẩn `MusicManagerPlayModeTests.cs`.
- Mọi điểm trigger liệt kê ở §4 đã gọi đúng `PlaySfx`, verify bằng Play Mode thật (nghe được, không
  phát trùng/lặp bất thường), không chỉ compile được (theo Definition of Done chung trong
  `README.md`).
- `SfxVolume` trong Settings tăng/giảm ảnh hưởng đúng mọi SFX mới, không cần code riêng từng chỗ.
- `Assets/Resources/Audio/SFX/CREDITS.md` liệt kê đủ license/tác giả/URL cho mọi file lấy từ
  Freesound (đặc biệt các file CC-BY).
- `DecisionRegister.md` D-058 chuyển từ Proposed sang Accepted, README index cập nhật link tài liệu
  này.

## 8. Phạm vi ngoài tài liệu này

- Ambience loop theo khu vực.
- Voice-over/thoại thật (khác dialogue blip).
- Audio Mixer/ducking nâng cao (hiện vẫn là volume nhân thủ công trên AudioSource, đủ dùng cho scope
  này).
- SFX cho scene ngoài MainMenu/IntroCutscene/MapNhat (chưa tồn tại trong Build Settings).
