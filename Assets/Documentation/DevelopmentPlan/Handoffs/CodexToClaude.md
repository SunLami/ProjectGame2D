# Codex → Claude Handoff

## Phase A — 7 SFX UI common

Status: `SFX_PHASE_A_UI_ART_READY`

Ngày: 2026-09-28

Đã tạo đủ 7 file one-shot trong `Assets/Resources/Audio/SFX/UI/`. Tất cả là mono, 44.1 kHz,
PCM 16-bit WAV, đã cắt phần transient chính/bỏ phần im lặng thừa, fade mép 4 ms để tránh
click và peak-normalize -1 dBFS. Không sửa script/prefab.

| SFX ID / file | Freesound source | License | Tác giả | Thời lượng thực tế / đích |
| --- | --- | --- | --- | --- |
| `sfx.ui.hover` — `sfx_ui_hover.wav` | [Wooden Click](https://freesound.org/people/BenjaminNelan/sounds/321083/) | CC0 1.0 | BenjaminNelan | 0.120s / 0.15s |
| `sfx.ui.click_primary` — `sfx_ui_click_primary.wav` | [Metallic_Click](https://freesound.org/people/BlondPanda/sounds/778444/) | CC0 1.0 | BlondPanda | 0.250s / 0.25s |
| `sfx.ui.click_secondary` — `sfx_ui_click_secondary.wav` | [wooden click.wav](https://freesound.org/people/allaskas/sounds/677298/) | CC0 1.0 | allaskas | 0.200s / 0.25s |
| `sfx.ui.toggle` — `sfx_ui_toggle.wav` | [click_switch.wav](https://freesound.org/people/StarTowerStudio/sounds/424987/) | CC0 1.0 | StarTowerStudio | 0.200s / 0.20s |
| `sfx.ui.error` — `sfx_ui_error.wav` | [pong sound effect ui button](https://freesound.org/people/Troube/sounds/686543/) | CC0 1.0 | Troube | 0.298s / 0.40s |
| `sfx.ui.popup_open` — `sfx_ui_popup_open.wav` | [paper - folding 01.wav](https://freesound.org/people/Anthousai/sounds/398896/) | CC0 1.0 | Anthousai | 0.350s / 0.35s |
| `sfx.ui.popup_close` — `sfx_ui_popup_close.wav` | [Close Book 2](https://freesound.org/people/qubodup/sounds/862316/) | CC0 1.0 | qubodup | 0.243s / 0.25s |

Nguồn và license cũng đã ghi tại `Assets/Resources/Audio/SFX/CREDITS.md`. Toàn bộ đều là
CC0; không dùng CC-BY, NC hay license không rõ. File là derivative từ public MP3 preview do
Freesound cung cấp trên chính trang sound (download bản gốc yêu cầu tài khoản).

### Query không có kết quả CC0 khớp hoàn toàn

- `short low error tone / UI negative buzz soft`: không chọn các buzz/synth điện tử vì xung
  đột art direction. Dùng guitar note trầm, ngắn của Troube (0.298s), vẫn đúng dải
  0.2–0.4s của `AudioSfxSystem.md`, cần Claude duyệt sắc thái negative.
- `soft cloth parchment unfold with faint bell chime / UI panel open`: không tìm được một file
  CC0 sạch có cả parchment/cloth và faint bell trong cùng recording. Dùng transient gấp giấy/
  parchment của Anthousai, không trộn chuông từ file thứ tám để giữ đúng phạm vi 7 source.

---

## BottomHUD — icon contrast + EXP connector ornament fix

Status: `BOTTOMHUD_CONTRAST_AND_ORNAMENT_FIX_ART_READY`

Ngày: 2026-09-28

Đã sửa đúng 3 PNG được giao, giữ nguyên tên file, canvas, GUID/import metadata và không sửa code:

- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/map_icon_v1.png` — giữ biểu tượng
  bản đồ gấp/compass, chuyển mặt giấy sang ivory sáng, viền antique gold và giữ sapphire accent.
  Kích thước **1017×915 px**, RGB trung bình vùng opaque mới **(130.69, 128.78, 96.04)**
  (trước: `(86, 70, 47)`).
- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/stat_icon_v1.png` — giữ silhouette
  nhân vật trùm hood, chuyển hood/áo sang ivory–light silver, viền antique gold và sapphire gem.
  Kích thước **1117×1168 px**, RGB trung bình vùng opaque mới **(160.28, 149.94, 131.13)**
  (trước: `(43, 39, 41)`).
- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/unified_hud_frame_v1.png` — giữ
  **2172×424 px**, toàn bộ 8 quick-slot, 2 socket, rãnh EXP alpha-rỗng và phần khung còn lại;
  hai vai nối ở đầu rãnh EXP được bổ sung strut walnut đặc với highlight antique-gold để thay cảm
  giác gai đen rời rạc và giảm khoảng alpha giữa rãnh trên với thân HUD.

Điểm neo alpha của rãnh EXP tại cột trung tâm vẫn khớp file cũ chính xác:
`y=6 → 162`, `y=39 → 0`, `y=43 → 15`; tâm rãnh EXP tiếp tục alpha `0`, nên fill phía sau
không bị che. Không thay đổi 7 asset PlayerHUD/BottomHUD đã PASS.

---

## PlayerHUD + BottomHUD — Dark Inventory Style, locked size

Status: `PLAYERHUD_BOTTOMHUD_ART_READY`

Ngày: 2026-09-28

Đã tạo đủ 10 PNG RGBA đúng kích thước khóa trong entry nguồn, không sửa code/prefab và không
đụng các asset đã migrate trước đó.

### PlayerStatusHUD

- `Assets/Resources/UI/Gameplay/PlayerStatusHUD/DarkInventoryStyle/player_status_frame_v1.png`
  — **1949×626 px**, alpha bbox **99.59% rộng / 98.72% cao**.
- `Assets/Resources/UI/Gameplay/PlayerStatusHUD/DarkInventoryStyle/health_fill_v1.png`
  — **2065×125 px**, **98.06% / 90.40%**; giữ fill đỏ.
- `Assets/Resources/UI/Gameplay/PlayerStatusHUD/DarkInventoryStyle/stamina_fill_green_v1.png`
  — **2091×192 px**, **98.09% / 89.58%**; giữ fill xanh lá.
- `Assets/Resources/UI/Gameplay/PlayerStatusHUD/DarkInventoryStyle/default_avatar_v1.png`
  — **1254×1254 px**, **94.90% / 94.90%**.

Theo owner clarification, `player_status_frame_v1` chỉ giữ khung/rim; lòng Avatar, LevelBadge,
Health và Stamina đều **alpha 0** để các child image/fill nằm phía sau hiện qua đúng kiến trúc.

### UnifiedHUD / BottomHUD

- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/unified_hud_frame_v1.png`
  — **2172×424 px**, alpha bbox **95.99% rộng / 100.00% cao**.
- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/quick_slot_background_brown_v1.png`
  — **1254×1254 px**, **94.90% / 94.90%**.
- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/socket_background_round_brown_v1.png`
  — **1254×1254 px**, **94.90% / 94.90%**.
- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/experience_bar_fill_v1.png`
  — **2120×125 px**, **98.11% / 90.40%**; giữ fill xanh dương.
- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/map_icon_v1.png`
  — **1017×915 px**, **89.77% / 90.38%**; giữ nghĩa bản đồ.
- `Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle/stat_icon_v1.png`
  — **1117×1168 px**, **89.97% / 90.07%**; giữ nghĩa nhân vật/chỉ số.

Theo owner clarification, lòng rãnh EXP, 8 quick slots và 2 socket Map/Character trong
`unified_hud_frame_v1` đều **alpha 0** để các child asset/fill nằm phía sau hiện lên. Rãnh EXP đã
được kiểm tra alpha tại trái/tâm/phải đều bằng 0; ornament không còn che ngang lòng fill.

Tất cả `.meta` dùng Sprite Mode `Single`, Filter `Point`, Mipmap `Off`, Compression `None`,
PPU 100, Border `0,0,0,0`; 10 asset có 10 GUID riêng. Art direction dùng charcoal/walnut,
antique-gold mảnh và sapphire tiết chế theo D-041.

---

## Fishing mystery fish icon — readability revision

Status: `FISHING_MYSTERY_FISH_ICON_ART_READY`

Ngày: 2026-09-27

- Đã ghi đè `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_mystery_fish_icon.png` sau
  review Play Mode: silhouette cũ quá nhỏ/tối trên MovementTrack.
- Canvas giữ nguyên **124×124 px**, RGBA; metadata/GUID/import settings giữ nguyên.
- Silhouette mới dùng thân graphite lớn, outline bạc sáng và chắc hơn, vây/tail tối giản, dấu `?`
  antique-gold lớn hơn để đọc rõ ở kích thước runtime 62×62.
- Alpha bbox mới `(4,12)-(120,108)`: **93.55% rộng / 77.42% cao**, thay cho bản cũ
  87.1% rộng / 61.3% cao.
- Pixel-art vẫn dùng lưới logic 31×31 + nearest-neighbor 4×. Không sửa code hay asset khác.

---

## Fishing mystery fish icon

Status: `FISHING_MYSTERY_FISH_ICON_ART_READY`

Ngày: 2026-09-27

- Đã tạo `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_mystery_fish_icon.png`, đúng
  **124×124 px** (logic 62×62), RGBA.
- Icon là silhouette cá graphite/charcoal đơn giản, hướng sang phải, có dấu `?` antique-gold nhỏ;
  không dùng màu hoặc hoa văn từ icon cá thật.
- Pixel-art được chuẩn hóa trên lưới logic 31×31 rồi nearest-neighbor upscale 4×, cùng mật độ chi
  tiết với `fishing_bobber_bite_00.png`. Alpha bbox `(8,24)-(116,100)`, tương ứng khoảng
  **87.1% rộng / 61.3% cao** canvas, đủ rõ ở `FishIcon` logic 62×62.
- Import metadata: Sprite Mode `Single`, Filter `Point`, Mipmap `Off`, Compression `None`, PPU 100,
  Border `0,0,0,0`.
- Không sửa code và không thay đổi asset nào khác.

---

## Fishing UI V3 — padding fix

Status: `FISHING_UI_V3_PADDING_FIX_ART_READY`

Ngày: 2026-09-27

Đã ghi đè đúng 3 PNG `_v3`, giữ nguyên canvas, tên file, palette, alpha/import metadata và phong
cách pixel-art; chỉ scale nearest-neighbor + căn giữa phần nội dung để loại padding thừa:

- `fishing_waiting_panel_v3.png` — canvas **1000×148**, alpha bbox mới
  `(48,8)-(952,140)`: **90.40% rộng / 89.19% cao**.
- `fishing_slider_track_v3.png` — canvas **92×980**, alpha bbox mới
  `(6,2)-(86,978)`: **86.96% rộng / 99.59% cao**.
- `fishing_slider_fill_v3.png` — canvas **92×980**, alpha bbox mới
  `(6,0)-(86,980)`: **86.96% rộng / 100% cao**.

Track và Fill dùng cùng bbox ngang `x=6..86`, nên thẳng tâm khi chồng trong `Slider.fillRect`.
Không thay đổi 5 asset `_v3` còn lại, code, prefab hoặc `FishingSpot.River.asset`.

---

## Fishing UI V3 — locked-size art + bobber bite animation frames

Status: `FISHING_UI_V3_LOCKED_SIZE_ART_READY`

Ngày: 2026-09-27

Đã tạo lại đúng 8 asset Fishing UI `_v3` trong
`Assets/Resources/UI/Fishing/DarkInventoryStyle/`, khóa đúng bitmap 2× so với các giá trị logic
hardcode gốc, không sửa code/prefab hoặc `FishingSpot.River.asset`:

- `fishing_waiting_panel_v3.png` — **1000×148 px**.
- `fishing_bite_prompt_v3.png` — **300×300 px**; exterior alpha 0, tâm charcoal có alpha tối đa
  **242/255**, không bake dấu `!` hoặc icon để nhận phao animation riêng ở runtime.
- `fishing_minigame_panel_v3.png` — **1040×1300 px**.
- `fishing_movement_track_v3.png` — **300×980 px**; channel đặc tại hàng giữa chiếm khoảng
  **78.7%** chiều rộng bitmap, không còn là đường kẻ mảnh.
- `fishing_catch_zone_v3.png` — **256×240 px**; emerald rực + amber, tương phản mạnh với track
  charcoal/sapphire. Border 9-slice: **Left 0, Bottom 32, Right 0, Top 32 px**.
- `fishing_slider_track_v3.png` — **92×980 px**.
- `fishing_slider_fill_v3.png` — **92×980 px**; một cột cyan/sapphire liên tục, phù hợp
  `Slider.fillRect` gốc.
- `fishing_result_panel_v3.png` — **1240×360 px**.

Cả 8 file là RGBA, pixel-art logical 1/4 + nearest-neighbor 4×, Sprite Mode `Single`, Filter
`Point`, Mipmap `Off`, Compression `None`, PPU 100. Waiting/Bite/Minigame/Result có alpha bake
tối đa 242/255; không bake text.

### Owner follow-up: thay TMP `!` bằng phao câu animation

Owner đã thay yêu cầu TMP `!`: BitePrompt phải hiển thị phao câu ở chính giữa và giật/chìm xuống
nước. Codex đã tạo 6 sprite frame riêng, mỗi frame **96×128 px**, RGBA/Single/Point/Mipmap Off/
Compression None/PPU 100:

- `fishing_bobber_bite_00.png` — nổi trung tính.
- `fishing_bobber_bite_01.png` — bắt đầu bị kéo xuống.
- `fishing_bobber_bite_02.png` — chìm thêm.
- `fishing_bobber_bite_03.png` — thấp nhất, splash nhỏ.
- `fishing_bobber_bite_04.png` — bật lên.
- `fishing_bobber_bite_05.png` — ổn định lại.

`fishing_bobber_bite_preview.gif` là preview loop của chuỗi trên. Nhịp đề xuất theo frame:
**180ms, 90ms, 80ms, 100ms, 110ms, 180ms**, loop trong thời gian BitePrompt hiển thị.
`fishing_bobber_bite_loop.anim` đã author sẵn sprite-key loop dài 0.74 giây cho component
`UnityEngine.UI.Image`, dùng đúng 6 frame trên.
Claude cần bỏ/ẩn TMP `!`, tạo `Image` phao riêng ở chính giữa BitePrompt và phát 6 frame này;
không bake phao vào board để vẫn chỉnh được vị trí/kích thước/nhịp animation.

---

## Fishing BitePrompt V3 — dark backing fill

Status: `FISHING_BITE_PROMPT_BACKING_ART_READY`

Ngày: 2026-09-27

- Đã tạo `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_bite_prompt_v3.png`, đúng
  **360×360 px**, RGBA.
- Giữ nguyên byte-for-byte toàn bộ pixel ring/bevel/gem/phao/gợn nước đã duyệt từ `_v2`; chỉ
  thay vùng alpha kín chứa tâm ring bằng fill charcoal tối `(20,18,17)` với alpha
  **235/255 (92.2% opaque)** để TMP `!` có mặt phẳng tựa.
- Phần ngoài ring vẫn alpha `0`; không thay đổi silhouette, kích thước hay vị trí khung.
- Import metadata: Sprite Mode `Single`, Filter `Point`, Mipmap `Off`, Compression `None`,
  PPU 100, Border `0,0,0,0`.
- Không thay đổi 7 asset `_v2` còn lại và không sửa code/prefab.

---

## Fishing minigame UI V2 — visual correction assets

Status: `FISHING_UI_V2_ART_READY`

Ngày: 2026-09-27

Đã gen 8 asset pixel-art `_v2` mới trong
`Assets/Resources/UI/Fishing/DarkInventoryStyle/`; giữ nguyên toàn bộ `_v1`, không sửa code
hoặc prefab.

- `fishing_waiting_panel_v2.png` — 1000×148.
- `fishing_bite_prompt_v2.png` — 360×360; ring dày nhiều lớp bevel/highlight/shadow, thêm
  phao câu đỏ-trắng + dây câu ở đỉnh và gợn nước xanh ở đáy để đọc ngay là
  thông báo cá cắn câu; tâm vẫn trống cho TMP `!`.
- `fishing_minigame_panel_v2.png` — 1240×1560, tương ứng logic 620×780.
- `fishing_movement_track_v2.png` — 360×1200; channel đặc chiếm xấp xỉ **78% bề
  rộng bitmap**, thành bevel dày và hai đầu mút nhỏ đối xứng.
- `fishing_catch_zone_v2.png` — 320×280; đổi sang **emerald-green rực + amber-gold**
  thay vì sapphire/charcoal, tương phản mạnh với track và slider xanh. Border 9-slice:
  **Left 0, Bottom 40, Right 0, Top 40 px**.
- `fishing_slider_track_v2.png` — 128×1200; shaft dày là trọng tâm, end-cap tối giản.
- `fishing_slider_fill_v2.png` — 128×1200; sau review owner đã regen thành **một cột
  sapphire/cyan liền mạch duy nhất** từ đáy tới đỉnh, crop-safe cho `fillRect`
  BottomToTop; không segment, không khối xanh lá giữa thanh, không divider và không end-cap
  trang trí lấn át.
- `fishing_result_panel_v2.png` — 1240×360.

Cả 8 asset: RGBA, pixel-art logical 1/4 + nearest-neighbor 4×, Sprite Mode `Single`, Filter
`Point`, Mipmap `Off`, Compression `None`, PPU 100. Bốn panel Waiting/Bite/Minigame/Result bake
alpha tối đa **242/255 (94.9%)**. Không bake text, TMP label hoặc fish icon.

---

## Fishing minigame UI — Dark Inventory Style assets

Status: `FISHING_UI_ART_READY`

Ngày: 2026-09-27

Codex chỉ gen và chuẩn hóa 8 bitmap PNG **pixel-art** theo D-041; không sửa code,
prefab, gameplay, timing, `GameState` hoặc icon cá. Sau review owner, toàn bộ asset đã
regen từ bản painted ban đầu sang pixel art thật: logical resolution 1/4, palette giới hạn,
hard-alpha edge và nearest-neighbor upscale 4×; không còn anti-alias/vector-like rendering.

- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_waiting_panel_v1.png` — 1000×148.
- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_bite_prompt_v1.png` — 300×300.
- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_minigame_panel_v1.png` — 1040×1300.
- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_movement_track_v1.png` — 300×980.
- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_catch_zone_v1.png` — 256×240.
- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_slider_track_v1.png` — 92×980.
- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_slider_fill_v1.png` — 92×980.
- `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_result_panel_v1.png` — 1240×360.

Import của cả 8 asset: RGBA, Sprite Mode `Single`, Filter Mode `Point`, Mipmap `Off`, Compression
`None`, PPU 100. `fishing_waiting_panel_v1`, `fishing_bite_prompt_v1`,
`fishing_minigame_panel_v1` và `fishing_result_panel_v1` đã bake alpha tối đa **242/255
(94.9% opaque)** ngay trong PNG; exterior vẫn alpha 0.

`fishing_catch_zone_v1.png` có 9-slice border **Left 0, Bottom 32, Right 0, Top 32 px**; chỉ
bảo toàn cap trên/dưới khi runtime đổi chiều cao, chiều rộng cố định. Các asset còn
lại có Border `0,0,0,0`. Không asset nào bake text, số, dấu `!` hoặc fish icon.

---

## Abandon Quest confirmation — Dark Inventory Style board

Status: `ABANDON_QUEST_CONFIRMATION_ART_READY`

Ngày: 2026-09-27

- Đã gen lại duy nhất board
  `Assets/Resources/UI/SessionUX/DarkInventoryStyle/session_confirmation_board_v1.png` theo D-041:
  nền charcoal/walnut opaque, viền antique gold mảnh, một sapphire accent nhỏ và ornament tiết
  chế.
- Board chừa safe area liền mạch cho message TMP 2–3 dòng ở giữa và hai nút runtime ở đáy;
  không bake text, label, icon, button shape hoặc button outline.
- Kích thước đúng **1672×941 px**, RGBA; không có sai khác kích thước.
- Import metadata: Sprite Mode `Single`, Filter Mode `Point`, Mipmap `Off`, Compression `None`, PPU
  100, `Border = 0,0,0,0`; không có sai khác border.
- Không gen sprite nút; không sửa code, prefab, hierarchy, callback hoặc asset
  Quest Log/Tracker/Minimap khác.

---

## Quest Accept Popup — Dark Inventory Style board

Status: `QUEST_ACCEPT_POPUP_ART_READY`

Ngày: 2026-09-27

- Đã tạo `Assets/Resources/UI/Quest/QuestAccept1920/quest_accept_board_dynamic_rewards_v3.png`.
- Theo yêu cầu owner cập nhật sau khi review: board chỉ bake khung ngoài, banner tiêu đề rỗng
  và hai divider; **không còn bake nút**, text, icon hoặc reward socket.
- Tạo hai button background riêng, cùng kích thước **2172×724 px**, RGBA, không bake label:
  - `Assets/Resources/UI/Quest/QuestAccept1920/quest_accept_button_accept_v1.png` — sapphire/dark-blue.
  - `Assets/Resources/UI/Quest/QuestAccept1920/quest_accept_button_decline_v1.png` — charcoal/walnut trung tính.
- Kích thước đúng **1122×1402 px**, RGBA; không có sai khác kích thước.
- Import metadata: Sprite Mode `Single`, Filter Mode `Point`, Mipmap `Off`, Compression `None`, PPU
  100, `Border = 0,0,0,0`; không có sai khác border.
- Board và hai button đều dùng Sprite Mode `Single`, Filter Mode `Point`, Mipmap `Off`,
  Compression `None`, PPU 100, `Border = 0,0,0,0`.
- Chỉ thêm/cập nhật ba bitmap trên, metadata, DecisionRegister và handoff; không đụng
  `Tracker1920`, `inventory_slot_hd.png`, prefab, scene, code hoặc script.

---

## MainMenu Settings / Slot / Confirm — Dark Inventory Style assets

Status: `MAINMENU_SETTINGS_SLOT_CONFIRM_ART_READY`

Ngày: 2026-09-27

Codex chỉ gen và chuẩn hóa bitmap PNG theo D-054; không sửa code, prefab, scene, video hoặc các asset
Landing `_v1` đã có.

### SettingsPage

- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/settings_board_v1.png` — 1122×1402.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/settings_title_v1.png` — 2048×768.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/settings_checkbox_unchecked_v1.png` — 1254×1254.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/settings_checkbox_checked_v1.png` — 1254×1254.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/settings_slider_track_v1.png` — 2048×226.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/settings_slider_handle_v1.png` — 1254×1254.

### SlotPage

- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/save_slot_card_v1.png` — 1086×1448.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/slot_badge_v1.png` — 1983×793; không bake text/số,
  dùng chung cho cả ba slot.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/slot_delete_button_v1.png` — 1944×809; muted-red
  danger face, không bake label.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/slot_page_new_game_title_v1.png` — 2048×683.
- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/slot_page_continue_title_v1.png` — 2048×744.

### ConfirmOverlay / ErrorOverlay

- `Assets/Resources/UI/MainMenu/DarkInventoryStyle/overlay_dialog_board_v1.png` — 900×600; không bake
  message hoặc button.

Tất cả 12 asset: RGBA/alpha transparency, Sprite Mode `Single`, Filter Mode `Point`, Mipmap `Off`,
Compression `None`, PPU 100, Max Size 4096, `Border = 0,0,0,0`; gán bằng `Image.Type = Simple`.
Không có sai khác kích thước hoặc border so với bảng yêu cầu trong `ClaudeToCodex.md`.

---

## MainMenu Landing — Dark Inventory Style assets (D-054)

Status: `LANDING_DARK_INVENTORY_ASSETS_READY`

Ngày: 2026-09-26

Codex chỉ gen/chuẩn hóa asset; không sửa script, prefab, scene, video background hoặc keyframe fallback.

- Logo: `Assets/Resources/UI/MainMenu/DarkInventoryStyle/orynthals_logo_v1.png` — 2172×724 RGBA.
- Slogan: `Assets/Resources/UI/MainMenu/DarkInventoryStyle/orynthals_slogan_v1.png` — 2139×181 RGBA;
  giữ nguyên text `BEYOND THE GATE, YOUR STORY BEGINS.` và outline charcoal/sapphire để đọc trên
  video sáng.
- Landing board: `Assets/Resources/UI/MainMenu/DarkInventoryStyle/landing_actions_board_v1.png` —
  1122×1402 RGBA; **đã regen theo silhouette cổng làng**: mái ngói sapphire, xà/trụ walnut,
  hai banner và hai đèn treo; lòng charcoal opaque + gold mảnh, không bake button/text.
- Button Normal: `Assets/Resources/UI/MainMenu/DarkInventoryStyle/landing_action_button_v1.png` —
  1944×809 RGBA.
- Button Hover: `Assets/Resources/UI/MainMenu/DarkInventoryStyle/landing_action_button_hover_v1.png` —
  1944×809 RGBA; giữ cùng silhouette/khung với Normal, chỉ tăng sapphire focus/glow.

Import của cả 5 asset: Sprite Mode `Single`, Filter Mode `Point`, Mipmap `Off`, Compression `None`,
alpha transparency bật, PPU 100, Max Size 4096. **Không author 9-slice** (`Border = 0,0,0,0`);
Claude gán `Image.type = Simple` và giữ RectTransform/layout hiện tại. Không thay đổi
`mainmenu_background.mp4` hoặc `mainmenu_new_journey_dawn_v8.png`.

---

Status: `MINIMAP_TAG_ART_READY`

Ngày: 2026-09-26
Feature: Minimap Zone Name / Date shared 9-slice tag

- Asset: `Assets/Resources/UI/Map/DarkInventoryStyle/minimap_tag_v1.png`
- Sprite: `minimap_tag_v1`, 352×56 RGBA; dùng chung cho `ZonePlate` (~88×14 logic px) và
  `DatePlate` (~60×14 logic px).
- Gán `Image.type = Sliced` cho cả hai Image.
- Border 9-slice đã author trong `.meta`: **Left 20, Right 20, Top 12, Bottom 12** pixel trên file gốc.
- `Pixels Per Unit = 400`, nên border quy đổi xấp xỉ 5 logic px hai đầu và 3 logic px
  trên/dưới khi Canvas reference dùng 100 PPU. Không cần thay kích thước RectTransform hiện tại.
- Import: Sprite Single, Point, mipmap off, Compression None, alpha transparency on, pivot center.
- QC: phần giữa phẳng/không ornament để stretch ngang và giữ TMP dễ đọc; alpha bbox nằm trong
  canvas, không edge touch. Không sửa script/prefab/scene.

---

Status: `MAP_ART_READY`

Ngày: 2026-09-26
Feature: Minimap + FullMap Dark Inventory Style assets

## Asset sẵn sàng để gán Inspector

- `Assets/Resources/UI/Map/DarkInventoryStyle/minimap_frame_v1.png`
  - Sprite: `minimap_frame_v1`, 384×384 RGBA.
  - Gán `MinimapController._frameImage` / `UnifiedGameplayHUD.prefab/Minimap/Frame`.
- `Assets/Resources/UI/Map/DarkInventoryStyle/minimap_mask_v1.png`
  - Sprite: `minimap_mask_v1`, 384×384 RGBA; circle trắng đặc, alpha 0 bên ngoài, không viền.
  - Gán `MinimapController._maskImage` / `Minimap/MapView`. Bán kính mask khớp lòng trong frame; không dùng chung file frame.
- `Assets/Resources/UI/Map/DarkInventoryStyle/map_close_button_v1.png`
  - Sprite: `map_close_button_v1`, 176×176 RGBA.
  - Gán `MapPopup/CloseButton` `Image.sprite`.
- `Assets/Resources/UI/Map/DarkInventoryStyle/player_marker_v1.png`
  - Sprite: `player_marker_v1`, 64×64 RGBA, mũi tên hướng lên.
  - Asset nâng cấp tùy chọn cho marker Minimap/FullMap; nếu logic hiện tại không xoay theo facing, icon vẫn đọc rõ như marker vị trí.

## Import contract

Cả bốn texture đã có `.meta` chuẩn hóa: `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`,
`Filter Mode = Point`, `Generate Mip Maps = Off`, `Compression = None`, `Alpha Is Transparency = On`,
pivot center. Không sửa script/prefab/scene; Claude chỉ cần gán sprite và bỏ runtime placeholder theo handoff.

## Asset generation/QC

Frame, Close button và Player marker được sinh bằng built-in ImageGen với ba sprite Dark Inventory Style
hiện có làm visual reference, sau đó chỉ cleanup alpha fringe/crop/nearest-neighbor resize. Mask là
stencil chức năng tạo xác định theo lòng trong frame. QC xác nhận PNG RGBA, alpha 0 ngoài silhouette,
frame center trong suốt và mask chỉ có alpha 0/255.

---

Update 2026-09-22 — khoảng cách Quest row còn thấy dù layout spacing đã là `1` vì
`landing_action_button.png` chứa alpha padding lớn trên/dưới. Đã dùng compensated spacing `-15` trên
Quest list riêng để phần khung nhìn thấy cách nhau xấp xỉ 1px; không sửa shared MainMenu asset.

Update 2026-09-22 — theo feedback trực tiếp của owner, giảm spacing của
`QuestListPanel/Content.VerticalLayoutGroup` từ `4` xuống `1`; không đổi kích thước/font ở lượt này.

Update 2026-09-22 — Quest list density: `QuestListPanel/Content` trước đó có
`childControlHeight=false`, nên bỏ qua preferred height và dùng RectTransform row cao `100` cộng
spacing `8`. Lần chỉnh chỉ xuống `58` đã làm lộ thêm lỗi Title/Status vẫn giữ offset cũ và tràn khỏi
khung. Layout cuối đã đồng bộ toàn row: cao/preferred height `44`, spacing `4`, Title/Status chia đều
hai nửa và căn giữa, font `9`/`7.5`; danh sách gọn và chữ không còn nằm ngoài button.

Update 2026-09-22 — HUD layering: sửa sibling order trong `GameplayUIRoot.prefab` từ
`PlayerHUD → ... gameplay overlays ... → UnifiedGameplayHUD` thành
`PlayerHUD → UnifiedGameplayHUD → gameplay overlays`. Cả hai HUD giờ render phía sau Quest Log và
các gameplay menu khác trên Canvas chung; không đổi sorting layer, Canvas hay gameplay logic.

Status: `QUEST_TRACKING_AND_ABANDON_READY`

Ngày: 2026-09-22
Feature: Quest Log — Track/Untrack/Abandon

- `QuestManager` hiện sở hữu đúng một `trackedQuestId`; quest đầu tiên tự track, người chơi có thể
  Track/Untrack quest Active/Ready khác. Quest tracker và Quest Direction Indicator chỉ theo quest
  đang track; Available quest giver vẫn được chỉ dẫn để người chơi nhận nhiệm vụ.
- Abandon chỉ cho quest Active/Ready có `giverNpcId`, xóa toàn bộ runtime objective progress và đưa
  quest về Available theo prerequisite. Nhận lại bắt buộc qua `QuestNpcInteractionService` tại đúng
  giver NPC và bắt đầu lại từ 0. Quest Completed hoặc không có giver không được abandon.
- `GameplayUIRoot.prefab` có nút `TRACK QUEST`/`UNTRACK QUEST`, `ABANDON QUEST` và confirmation modal.
  Tái sử dụng asset Light Fantasy hiện có (`landing_action_button`, `slot_delete_button`,
  `session_confirmation_board_hd`), vì vậy không cần sinh raster asset mới.
- Save schema tăng v7→v8 với `QuestSaveData.trackedQuestId`; migration save v7 chọn quest Active/Ready
  đầu tiên. Tracking/abandon đều đánh dấu session dirty.
- Runtime, EditMode và PlayMode test assemblies build PASS. Có test cho round-trip tracking,
  abandon/reaccept đúng NPC, guard quest không giver/completed và migration V7→V8.

---

Status trước: `QUEST_DIRECTION_ART_READY`

Ngày: 2026-09-19
Feature: Training Area onboarding — Quest Direction Indicator pixel-art sprites

Update 2026-09-22: theo yêu cầu trực tiếp của owner, ground arrow không còn đứng cố định tại tâm
Player. `QuestDirectionIndicator.ShowGroundArrow()` giờ đặt RectTransform trên chu vi bán kính
`_groundArrowOrbitRadius = 0.75` world unit theo vector tới quest, đồng thời vẫn xoay sprite theo
cùng hướng. Đây là thay đổi presentation được owner chủ động yêu cầu sau handoff cũ; không đổi logic
Quest/Tutorial, target resolution hay trạng thái progression.

Verification update: `ProjectGame2D.Runtime.csproj` build PASS (0 error; 22 warning có sẵn). Unity
live test gọi `ShowGroundArrow()` với phải/trên/trái/dưới cho kết quả position lần lượt
`(0.75,0)`, `(0,0.75)`, `(-0.75,0)`, `(0,-0.75)`, mọi trường hợp radius đúng `0.750` và rotation
Z đúng `270/0/90/180`. Scene readback xác nhận radius `0.75`, target `QuestGroundArrow`, sprite dùng
chung `quest_target_arrow.png`; Console không có lỗi `QuestDirectionIndicator`.

Update 2026-09-22 (arrived marker removed): theo yêu cầu trực tiếp tiếp theo của owner, đã bỏ hoàn
toàn mũi tên trỏ xuống khi Player đã ở trong khu vực quest. Nhánh `IsPlayerInside(areaId)` giờ ẩn
ground/edge indicator và không hiện marker thay thế. Đã xóa hierarchy `QuestArrivedMarker` khỏi
`MapNhat`, cùng các field/method bobbing tương ứng trong `QuestDirectionIndicator`. Arrow xoay quanh
Player khi chưa tới nơi, NPC marker và mannequin marker không đổi.

## Hoàn thành

- Theo yêu cầu cập nhật ngày 2026-09-22, đã hợp nhất còn đúng **1 sprite arrow duy nhất**:
  `Assets/Resources/UI/QuestDirection/quest_target_arrow.png` (48x48). Hai asset ground/edge riêng
  đã xóa. Import dùng Sprite, Point filter, mipmap off, uncompressed và alpha thật.
- Đã gán trực tiếp trong `Assets/Scenes/MapNhat.unity` cho toàn bộ field của
  `QuestDirectionIndicator`: `_npcHeadMarkerImage`, `_groundArrowImage`, `_edgeIndicatorImage`.
- Đã gán target arrow vào `_arrowImage` của
  `Assets/Prefabs/World/Attacked_Manequin1.prefab` (`MannequinAttackIndicator`).
- Cả `_npcHeadMarkerImage`, `_groundArrowImage`, `_edgeIndicatorImage` và mannequin `_arrowImage`
  cùng tham chiếu đúng một target-arrow sprite;
  đã đưa tint của các `Image` liên quan về trắng để giữ nguyên palette.
- Chỉ sửa presentation logic của direction indicator theo yêu cầu owner; không đổi Quest/Tutorial
  progression hoặc target resolution.

## Ghi chú orientation

Target-arrow source hướng lên. Ground/edge dùng rotation runtime hiện hữu để luôn chỉ về vị trí
quest; `MannequinAttackIndicator.Awake()` và `NpcHeadMarker` xoay 180° để chỉ xuống.

## Verification

- Sprite processor QC: target arrow hợp lệ, không edge touch/paste clamp, alpha chroma-key sạch.
- Unity Editor import PASS: sprite 48x48, Point, mipmap off, Uncompressed, PPU 48.
- Serialized audit trên `MapNhat` và mannequin prefab: cả 4 field Image dùng chung GUID
  `3c0cc2bc1a69412c8ebf4e384a29e8d1`; `NpcHeadMarker` rotation Z = 180°.
- Console không có lỗi import/binding liên quan asset. Console đang có lỗi runtime cũ không thuộc
  scope từ `SoundFXManager`/`MapManager` thiếu key `Grass_No_Outline`; không chỉnh vì yêu cầu cấm
  đụng logic khác.

---

Status: `VERIFIED`

Ngày: 2026-08-22
Feature: Phase 3 MainMenu New Game/Continue UI

## UI đã triển khai

- Dựng scene-authored UI trong `Assets/Scenes/MainMenu.unity/_UI/MainMenuCanvas/MainMenuRoot` bằng
  Unity MCP; không tạo hierarchy lúc runtime.
- Landing có `New Game`, `Continue`, `Settings`, `Quit`.
- New Game/Continue dùng chung selector đúng ba slot.
- Slot view hiển thị trạng thái `Empty`, `Valid`, `Corrupted`, `IncompatibleVersion` bằng chữ; không chỉ
  dùng màu.
- Slot hợp lệ hiển thị dữ liệu backend thật: level, stable area ID, total play time và last-saved local
  time. Không hiển thị `characterName` hoặc `tutorialCompleted` vì hai domain này chưa tồn tại.
- Overwrite và delete luôn có confirm chỉ rõ slot; Quit có confirm.
- Operation failure có modal thân thiện.
- Khi `GameState.Loading`, `CanvasGroup` khóa interact/raycast và bỏ focus để chống double-submit.
- Main Menu Settings là subpage riêng, dùng chung `SettingsService`; không push
  `GameplayMenuPage.Settings`. Có SFX, Music, Fullscreen, Save và Cancel/restore snapshot.
- UI subscribe `OnSaveSlotListChanged` và `OnOperationFailed`; không polling.
- UI dùng `InputSystemUIInputModule`/project `UI` map; focus mặc định và `UI/Cancel` cho slot page,
  Settings và popup.
- Toàn bộ TMP text dùng `Assets/Fonts/DigitalDisco SDF v3.asset`.

## File thay đổi

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scripts/UI/MainMenuSaveSlotsUI.cs`
- `Assets/Documentation/DevelopmentPlan/Verification/*`
- Tài liệu handoff/report Phase 3.

## Verification

- Script validation: 0 diagnostic.
- MainMenu scene validator: 0 issue.
- Console trong các luồng kiểm tra: 0 error, 0 warning.
- Landing: focus mặc định `NewGameButton`.
- Settings: focus mặc định `SfxSlider`; simulated keyboard Escape qua Input System đóng Settings,
  restore Landing và focus `NewGameButton`.
- Quit: confirm mở và focus mặc định `CancelButton`.
- New Game Slot 1: `MainMenu → Loading → DemoScene/Playing`, Player Level 1.
- Pause → Return Main Menu: `MainMenu`, active session đã clear.
- Continue Slot 1: `MainMenu → Loading → DemoScene/Playing`, Player restore Level 1 và saved position
  `(0,0)`.
- Slot 1 test save do Codex tạo đã được xóa sau verification; Continue trở lại disabled.
- Manual physical gamepad: `BLOCKED_MANUAL_TEST` — chưa có người thao tác controller thật.

Ảnh evidence nằm trong `Assets/Documentation/DevelopmentPlan/Verification/`.

## Backend handoff đã xác nhận

- Commit `5b83d1a7` đã sửa metadata readback và thêm Player capture foundation.
- UI verify end-to-end từ save mới: `LEVEL 1`, `AREA area.tutorial`, play time thật và timestamp local
  khác `0` đều lấy từ `RefreshSlots()`/`SaveSlotInfo`.
- `characterName` và `tutorialCompleted` vẫn được ẩn đúng chủ đích; không hiển thị placeholder giả.
- Save-on-return vẫn thuộc D-017/Phase 9; UI không gọi `PlayerSaveCapture` và không tự ghi DTO.
- Slot 1 test được tạo để verify metadata rồi xóa qua UI; ba slot trở lại trạng thái trống.
- Screenshot mới: `Verification/MainMenu_UI_Metadata_Final.png`.

## Remaining verification

- Automated virtual gamepad PASS cho MainMenu `Navigate`, `Submit`, `Cancel`. Trong lần test này phát
  hiện và sửa deferred default-focus sau frame đầu; console sạch.
- Manual physical gamepad vẫn `BLOCKED_MANUAL_TEST`; gameplay controls chưa được đánh dấu PASS.
- Responsive alternate-aspect test: `NOT RUN` — `Screen.SetResolution` trong Editor không thay đổi Game
  View capture thật, nên không dùng screenshot đó làm evidence.
- Area hiện hiển thị stable ID thật. Khi có Area catalog/display-name resolver, backend cần cung cấp
  presentation value hoặc read model; UI không tự biến ID thành tên giả.

## Phạm vi Claude không nên chỉnh trực tiếp

- Không chỉnh hierarchy/layout/colors/font của `MainMenuRoot`.
- Nếu contract field/event thay đổi, cập nhật handoff để Codex rebind bằng Unity MCP.

---

# Phase 5 Tutorial Overlay UI

Status: `VERIFIED`

Ngày: 2026-08-22
Feature: Phase 5 Input Tutorial presentation

## UI đã triển khai

- Dựng scene-authored hierarchy tại
  `Assets/Scenes/DemoScene.unity/_UI/UICanvas/TutorialOverlayRoot` bằng Unity MCP.
- Prompt dùng layout toast gọn 360x76, neo góc dưới phải để không che Inventory/Equipment; popup xác
  nhận Skip vẫn là modal toàn màn hình vì chỉ mở theo yêu cầu người chơi.
- `TutorialOverlayUI` là presentation adapter đặt ngoài `Assets/Scripts/Tutorial/`; không sở hữu
  progression, không đổi `GameState`, không đổi time scale.
- Đọc `TutorialManager.Instance.CurrentStep` ngay trong `OnEnable`; có fallback bind ở `Start`
  cho trường hợp thứ tự `Awake/OnEnable` khiến singleton chưa sẵn sàng.
- Subscribe `OnStepChanged` để cập nhật `InstructionText`; subscribe `OnTutorialCompleted` để
  đóng popup và ẩn prompt. UI cũng ẩn khi `CurrentStep == null`.
- Nút `Skip` chỉ mở confirm. Chỉ `ConfirmSkipButton` mới gọi `TutorialManager.Skip()`;
  `CancelSkipButton` đóng popup và trả focus về Skip.
- Popup chọn `CancelSkipButton` mặc định; hai action có explicit left/right navigation.
- Toàn bộ TMP text dùng `DigitalDisco SDF v3`.
- Không sửa script trong `Assets/Scripts/Tutorial/`, `AreaTriggerZone.cs`, hoặc Tutorial data
  contract. Không cần backend gap mới.

## File thay đổi

- `Assets/Scenes/DemoScene.unity`
- `Assets/Scripts/UI/TutorialOverlayUI.cs`
- `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md`

## Verification

- Script validation: 0 diagnostic.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Serialized references của panel, text, Skip, Confirm và Cancel: đầy đủ.
- Font runtime/scene: `DigitalDisco SDF v3`.
- New Game tutorial presentation (Editor Play, tool-driven): cả sáu authored step lần lượt hiện đúng
  stable step ID và `InstructionText`; panel hiện ở từng step và tự ẩn khi hoàn tất.
- Domain-event update: phát `PlayerSprinted` ở step Sprint chuyển UI ngay sang nội dung Attack.
- Skip confirm: click Skip chỉ mở popup và `IsCompleted == false`; click Confirm mới làm
  `IsCompleted == true`, đóng popup và ẩn panel.
- Continue giữa chừng: restore `tutorial.controls.sprint`, disable/enable overlay, UI hiện ngay
  đúng step Sprint mà không chờ event tiếp theo.
- EditMode: 28/28 PASS.
- PlayMode: 32/32 PASS.
- Console: 0 error.

## Giới hạn kiểm tra

- Ba luồng UI đã được chạy trong Play Mode bằng Unity MCP. Việc thao tác vật lý toàn bộ chuỗi bằng
  bàn phím/gamepad và tạo save thật qua MainMenu vẫn nên được owner chạy một vòng acceptance cuối;
  phần save/restore backend tương ứng đã có PlayMode coverage.

## Phạm vi Claude không nên chỉnh trực tiếp

- Không chỉnh hierarchy/layout/colors/font của `TutorialOverlayRoot`.
- Nếu Tutorial contract cần icon hoặc presentation field mới, cập nhật handoff trước để Codex rebind;
  UI hiện không tự chế dữ liệu.

## Final Inventory overlap acceptance

- Đã mở Inventory thật trong Play Mode tại step `tutorial.controls.open_inventory`; event chuyển
  manager sang `tutorial.controls.equip_item` và toast vẫn hiển thị.
- Ở Game View 1920x1080, bounds đo được:
  - Tutorial prompt: `x 1017.60..1881.60, y 38.40..220.80`.
  - Equipment panel: `x 514.56..898.56, y 316.32..892.32`.
  - Inventory panel: `x 888.96..1453.44, y 313.44..895.20`.
- `Rect.Overlaps` với Equipment và Inventory đều `false`. Phase 5 UI overlap issue đã đóng.

---

# Next Claude Task — Phase 6 Quest Backend

Status: `READY_FOR_CLAUDE`

Claude hãy bắt đầu Phase 6 theo `Roadmap.md`, `TutorialAndQuestProgression.md`,
`DataDrivenDevelopment.md`, `SaveAndWorldPersistence.md`, `QualityStrategy.md` và các accepted
decision liên quan.

## Backend scope

- Quest Definition data-driven với stable `questId`, prerequisite IDs, objective definitions và
  rewards; không mutate definition asset ở runtime.
- Runtime Quest Progress tách khỏi Definition và Save DTO.
- Quest catalog/resolver cùng editor/content validation cho ID rỗng/trùng, missing target/reference,
  prerequisite cycle, invalid target count và reward.
- Typed gameplay event contracts và objective tracking cho Talk, Obtain, Craft, Purchase, Gather,
  Kill. Không polling mỗi frame và không phụ thuộc click UI.
- NPC quest interaction/turn-in validation qua capability/service rõ ràng; NPC không sửa internals
  của QuestManager.
- Tutorial Quest chain và prerequisite gate cho Main Quest. Người chơi chưa làm Tutorial Quest vẫn
  được tự do khám phá/craft/shop/gather.
- Atomic/idempotent turn-in: không consume/grant một phần, không duplicate reward khi double-submit
  hoặc restore.
- Save/restore active/completed quest, objective index/counters; restore không phát progression event
  hoặc grant reward. Cập nhật save version/migration/default/fixtures đúng tài liệu nếu schema đổi.
- EditMode/PlayMode tests cho quest graph, từng objective event, save round-trip, lifecycle
  subscription, double turn-in và Main Quest unlock đúng một lần.
- Tạo ít nhất hai Quest Definition variants dùng chung runtime handlers và chạy Content Validator.

## Boundary

- Không dựng hoặc chỉnh Quest Log/Tracker/NPC marker UI; đó là Codex task sau backend handoff.
- Không chỉnh `TutorialOverlayRoot`, `TutorialOverlayUI` hoặc layout Inventory.
- Không thêm logic Shop/Crafting giả vào UI. Nếu transaction service Phase 7 chưa tồn tại, chốt typed
  event contract và test bằng event producer/fake phù hợp, đồng thời ghi rõ integration gap.
- Không tự chế display data cho UI. Handoff phải cung cấp read-model/contract public ổn định.

## Handoff Claude → Codex bắt buộc

- Ghi đầy đủ vào `Handoffs/ClaudeToCodex.md`: public API/events/read models, status semantics,
  objective presentation fields, NPC interaction contract, save/version changes, authored assets,
  test/validator results và known integration gaps.
- Khi backend ổn định và sẵn sàng dựng UI, đánh dấu rõ `READY_FOR_CODEX_UI`.

---

# Phase 6 Quest UI → Claude Follow-up

Status: `VERIFIED`

Ngày cập nhật binding: 2026-08-23

## UI đã dựng

- `DemoScene/_UI/UICanvas/QuestUIRoot`: event-driven Quest Tracker và Quest Log.
- `Assets/Prefabs/Quest/TownElderNPC.prefab` và scene instance
  `DemoScene/_Actors/TownElderNPC`, stable ID `npc.town.elder`.
- NPC offer/accept/turn-in chỉ đi qua `QuestNpcInteractionService`.
- Quest UI đọc `Catalog.AllQuests`/`GetStatus` và subscribe
  `QuestAccepted`/`QuestProgressChanged`/`QuestCompleted`/`MainQuestUnlocked`; không polling.
- Input action `Gameplay/QuestLog`: keyboard `J`, gamepad D-pad Up. Cancel đóng qua state history
  hiện có.
- Toàn bộ text dùng `DigitalDisco SDF v3`.

## API/content binding đã hoàn tất

- UI gọi mới `QuestManager.TryGetProgress` trong mỗi event-driven refresh; không cache
  `QuestProgressSnapshot` qua nhiều frame, không dùng `ToSaveData()` và không reflection runtime.
- Tracker dùng `CurrentObjectiveIndex` để chỉ hiển thị objective hiện tại bằng authored
  `Description`, kèm `ObjectiveCounters[index] / TargetCount`.
- Khi objective cuối chuyển sang `ReadyToTurnIn` và index bằng `Objectives.Count`, tracker giữ
  objective cuối để hiển thị counter hoàn tất (ví dụ `3 / 3`) thay vì mất progress.
- Quest Log dùng authored Description cho toàn bộ objective; chỉ objective active nhận counter.
- `TryGetProgress == false` giữ tracker/empty-state cũ và không dựng counter giả.

## Runtime verification đã qua

- Initial: Tutorial Quest `Available`, Main Quest `Locked`, tracker ẩn.
- Quest Log mở thành `GameplayMenu/QuestLog`, empty state đúng trước accept.
- Player vào trigger: marker `!`, prompt offer đúng `The Blacksmith's Request`.
- Accept qua button/service: Tutorial Quest `Active`, tracker hiện và event refresh chạy.
- Counter Tutorial Quest đã verify tuần tự:
  - Accept: Kill `0 / 2`.
  - Kill event thứ nhất: `1 / 2`.
  - Kill event thứ hai: chuyển objective Wood thành `0 / 3`.
  - Add một Wood: `1 / 3`.
  - Add đủ ba Wood: `3 / 3`, status `ReadyToTurnIn`.
- Hai kill event đúng ID/area + `InventoryManager.AddItem(WoodMaterial, 3)`: status
  `ReadyToTurnIn`, marker đổi `?`.
- Turn-in qua đúng NPC/service: Tutorial Quest `Completed`, `IsMainQuestUnlocked == true`, Main
  Quest `Available`, feedback thành công.
- Accept Main Quest rồi disable/enable `QuestUIRoot`: tracker đọc ngay `A Call to Adventure`
  đang `Active`, không chờ event mới.
- Main Quest tracker hiển thị authored Goblin objective `0 / 1`, sau event đúng chuyển
  `1 / 1` và `ReadyToTurnIn`.
- EditMode: 42/42 PASS.
- PlayMode: 48/48 PASS.
- `QuestLogUI.cs` validation: 0 diagnostic.
- DemoScene validator: 0 issue; Console cuối sau test: 0 error, 0 warning.

---

# Phase 7 Shop/Crafting UI → Claude Follow-up

Status: `VERIFIED`

Ngày: 2026-08-23

## UI/capability đã dựng

- `DemoScene/_UI/UICanvas/CommerceUIRoot`: modal Shop và Crafting được author trực tiếp vào scene
  bằng Unity MCP; không tạo hierarchy runtime và không chỉnh Quest/Inventory/Tutorial UI.
- `ShopCraftingUI` hiển thị stock, giá mua/bán, gold, số lượng đang sở hữu, quantity 1..99, recipe,
  ingredient counter, output và required station. Toàn bộ TMP text dùng `DigitalDisco SDF v3`.
- `TownElderNPC.prefab` giữ nguyên Quest capability/visual, được bổ sung
  `TownElderCommerceInteractionUI` + world-space `CommerceInteractionCanvas` với hai capability
  SHOP/CRAFT cho `npc.town.elder`; không tạo NPC mới.
- Mọi giao dịch NPC đi qua `ShopNpcInteractionService`/`CraftingNpcInteractionService`; UI không
  gọi trực tiếp transaction API trên manager và không tự kiểm tra ownership.
- Mọi enum fail được map thành thông báo riêng (`InsufficientGold`, `InsufficientItemQuantity`,
  `InsufficientInventoryCapacity`, `WrongStation`, `InsufficientIngredients`, v.v.).
- Khi modal mở, `PlayerInput` được deactivate để chống điều khiển nhân vật/double interaction; modal
  đóng bằng nút X, Escape hoặc gamepad East. `GameState` giữ `Playing` vì transaction backend Phase 7
  chặn mọi state có `AllowsGameplayInput == false`.

## Runtime verification đã qua

- DemoScene thật: `ShopManager`, `CraftingManager`, `ShopCraftingUI`, PlayerInput đều tồn tại;
  state `Playing`, console không error/warning trong smoke flow.
- Shop UI: mua 4 Wood qua nút BUY, gold `100 → 80`; bán lại 4 Wood qua SELL, gold `80 → 88`, đúng
  multiplier hiện tại; inventory quantity cập nhật theo event.
- Mua Health Potion khi gold = 0: giao dịch từ chối và UI hiện đúng `Not enough gold.`; không crash.
- Crafting UI: mua 3 Wood rồi craft `recipe.material.plank` qua nút CRAFT thành công, Wood `3 → 0`,
  Wood Plank `0 → 1` và feedback `Crafted Wood Plank.`.
- Chọn `recipe.consumable.health_potion` với stationTag rỗng: bị từ chối và UI hiện đúng yêu cầu
  crafting station (`WrongStation`). Thành công tại `station.forge` đã được backend Phase 7 verify;
  DemoScene hiện chưa có production forge interaction để cấp stationTag đó.
- EditMode: 46/46 PASS. PlayMode: 64/64 PASS.
- Content Validation: 0 error, 60 warning baseline, 77 asset checked.
- DemoScene validator: 0 issue; prefab không missing script/broken reference.

## Backend gap

- Không phát hiện API/field thiếu mới. Không chỉnh `Assets/Scripts/Shop/` hoặc
  `Assets/Scripts/Crafting/`.
- Lưu ý kiến trúc đã có từ backend: nếu sau này Product Design yêu cầu Shop/Crafting dùng
  `GameplayMenuPage.Shop/Crafting` (pause world), transaction gate hiện tại sẽ trả
  `GameplayNotAllowed`. Phase này dùng modal state `Playing` + khóa PlayerInput để giữ đúng contract.

---

# Phase 8 World Persistence Scene Integration → Claude Follow-up

Status: `VERIFIED`

Ngày: 2026-08-23

## Scene presentation đã dựng

- Dùng Unity MCP author trực tiếp visual placeholder và interaction prompt cho ba entity tương tác
  trong `DemoScene/_World`: `Chest_TownGeneral`, `Pickup_AncientRelic`,
  `ResourceNode_WoodLog`.
- `PersistentWorldInteractionUI` chỉ gọi API public `TryOpen`, `TryCollect`, `TryHarvest` và đọc
  `IsOpened`/`IsCollected`/`IsAvailable`; không cấp item, không ghi persistence và không truy cập
  internals của World backend.
- Mỗi entity có trigger riêng và dùng action `Gameplay/Interact` hiện có (keyboard E/gamepad South).
  Prompt chỉ hiện khi player trong range và entity còn tương tác được.
- Chest có closed visual + `OpenedIndicator` được bind vào field `_openedIndicator` có sẵn.
- Resource node có available logs visual + `DepletedIndicator` được bind vào field
  `_depletedIndicator`; presentation đọc `IsAvailable` để tự trở lại available sau cooldown.
- Unique pickup có relic placeholder và vẫn để backend tự `SetActive(false)` toàn object sau collect.
- `BossTracker_ForestGuardian` được đặt cùng vị trí world với `ForestGuardianBoss` và mang
  `BossDefeatPresentation`. Banner/defeated indicator là con của tracker, không gắn vào boss, nên
  vẫn tồn tại sau khi `EnemyUniversal` destroy boss corpse.
- Toàn bộ text mới dùng `DigitalDisco SDF v3`. Không chỉnh `QuestUIRoot`, `CommerceUIRoot`,
  Inventory, Tutorial, MainMenu, MapManager hoặc SoundFXManager.

## Runtime verification đã qua

- Chest: proximity prompt hiện; tương tác qua presentation mở chest; `IsOpened == true`, closed
  visual tắt và `OpenedIndicator` bật.
- Unique pickup: proximity prompt hiện; collect thành công và GameObject tự inactive đúng backend.
- Resource node: proximity prompt hiện; harvest thành công; `IsAvailable == false`, available
  visual tắt và `DepletedIndicator` bật.
- Boss: gây lethal damage làm `BossDefeatTracker.IsDefeated == true`; sau khi
  `ForestGuardianBoss` đã bị destroy, tracker/banner vẫn tồn tại và hiển thị
  `FOREST GUARDIAN DEFEATED`.
- Console trong runtime smoke flow: 0 error, 0 warning.
- EditMode: 48/48 PASS. PlayMode: 85/85 PASS.
- Content Validation: 0 error, 60 warning baseline, 83 asset checked.
- DemoScene validator: 0 issue, không missing script/broken reference.

## Backend gap

- Không phát hiện API/field thiếu mới; không chỉnh file nào trong `Assets/Scripts/World/`,
  `WorldObjectRegistry` hoặc `PlayerSpawnReadinessSource`.
- Visual hiện là base placeholder có thể thay asset về sau mà không đổi interaction/persistence
  contract.

---

# Phase 9 Save/Load/Return/Quit UI → Claude Follow-up

Status: `VERIFIED`

Ngày: 2026-08-23

## Pause UI đã dựng

- Mở rộng `PauseMenuUI` hiện có và author trực tiếp hierarchy qua Unity MCP; không tạo controller
  hoặc save service thứ hai.
- Nút `SAVE GAME`, `LOAD GAME`, `RETURN MAIN MENU`, `QUIT DESKTOP` chỉ gọi
  `GameplaySessionController`; UI không capture DTO, không đọc repository và không gọi
  `Application.Quit()`.
- Save lắng nghe `OnSaveSucceeded` để hiển thị local timestamp sau atomic save và
  `OnOperationFailed` để hiện message thân thiện do backend cung cấp.
- Load overlay có ba slot, hiển thị `Empty`/`Valid`/`Corrupted`/`IncompatibleVersion`, metadata
  Level/Area/Play Time/Last Save, đánh dấu `ACTIVE` theo `ActiveSlotId`, và chỉ enable LOAD khi
  `CanLoad(slotId)` trả true.
- Return/Quit dùng đúng `OnConfirmationRequired` và popup ba nhánh riêng:
  Save and Return/Quit, Without Saving, Cancel; popup không tạo GameState mới.
- Escape/gamepad East đóng popup hoặc Load overlay; selection mặc định ưu tiên slot loadable hoặc
  nút Cancel/Back.
- Save/Load/Return/Quit đồng loạt disable khi `IsBusy`; runtime smoke xác nhận ở state `Saving` cả
  bốn nút đều non-interactable.
- Hiển thị `UNSAVED CHANGES`/`ALL CHANGES SAVED` từ controller dirty state. Toàn bộ TMP text mới
  dùng `DigitalDisco SDF v3`.
- Không chỉnh `GameplaySessionController`, `SessionDirtyTracker`, `GameSessionManager`,
  `SceneFlowService` hoặc các UI root ngoài PauseMenu.

## Verification

- Runtime DemoScene: Pause state mở đúng, Save/Load/Return/Quit đều interactable khi idle.
- Load overlay đọc dữ liệu thật hiện có: slot incompatible và slot empty đều hiển thị đúng và LOAD
  disabled. Môi trường kiểm tra hiện không có valid slot thứ hai nên không ghi đè/tạo save người dùng
  chỉ để chạy destructive cross-slot manual flow; backend cross-slot flow đã có PlayMode coverage.
- Dirty runtime: Return popup hiện đúng `SAVE AND RETURN` / `RETURN WITHOUT SAVING` / `CANCEL`;
  Cancel đóng popup và không transition.
- Dirty runtime: Quit popup hiện đúng `SAVE AND QUIT` / `QUIT WITHOUT SAVING` / `CANCEL`;
  không gọi quit trực tiếp từ UI.
- Busy runtime: Save/Load/Return/Quit đều disabled.
- Console trong UI smoke flow: 0 error, 0 warning.
- `PauseMenuUI.cs`: 0 compile error/diagnostic có ý nghĩa.
- EditMode: 48/48 PASS. PlayMode: 118/118 PASS.
- Content Validation: 0 error, 60 warning baseline, 83 asset checked.
- DemoScene validator: 0 issue, không missing script/broken reference.

## Backend gap

- Không phát hiện API/field thiếu mới. Phase 9 UI binding hoàn tất theo contract hiện tại.

---

# Phase 10 Player Build Manual Verification → Claude Follow-up

Status: `BACKEND_GAP_FOUND`

Ngày: 2026-08-23

## Kết quả click-through

- `C:\Users\havin\Phase10PlayerBuild\ProjectGame2D.exe`: tồn tại và launch thành công trên máy
  thật.
- MainMenu hiển thị đúng. Slot 1 được giữ nguyên, không chọn và không ghi đè.
- Chọn `CREATE` trên Slot 2 đang trống: PASS; transition vào DemoScene thành công.
- DemoScene nhận input gameplay: chuột hoạt động, phím di chuyển làm nhân vật di chuyển.
- Đã Skip tutorial qua popup confirm để loại trừ khả năng Tutorial overlay giữ input.
- **FAIL tại bước mở Pause Menu:** nhấn Escape khi Player window đang focus không mở
  `PauseMenuUI`. Thử lại sau khi Tutorial đã hoàn toàn ẩn vẫn không mở Pause.

## Backend/integration gap chặn milestone

- Vì Pause Menu không thể mở trong Player build, không thể truy cập `Save Game`, nên chuỗi bắt
  buộc `Save → Return Main Menu → Continue → verify restore → Quit Desktop` không thể tiếp tục.
- Các hạng mục vị trí nhân vật, inventory, quest state, tutorial state, world state và cross-slot
  isolation vì vậy có trạng thái `NOT VERIFIED`, không được xem là PASS.
- Đây là blocker cho content-ready verification của Phase 10. Cần Claude kiểm tra binding/runtime
  path của action `UI/Cancel`/Pause trong Player build trước khi yêu cầu chạy lại click-through.
- Slot 2 đã được dùng để bắt đầu New Game phục vụ test; Slot 1 save cũ dạng
  `IncompatibleVersion` không bị thay đổi.

## Phạm vi thay đổi

- Không sửa UI, scene, script, Build Settings hay backend.
- Chỉ cập nhật handoff này theo yêu cầu verification.

---

# Phase 10 Pause Input Fix → Claude Follow-up

Status: `READY_FOR_PHASE10_REVERIFICATION`

Ngày: 2026-08-23

## Root cause

- `MainMenu/EventSystem`, `DemoScene/EventSystem` và `GameInputCoordinator._projectActions` cùng thao
  tác trên một serialized `InputActionAsset`.
- Trong transition `MainMenu → DemoScene`, lifecycle hai scene overlap ngắn. Coordinator mới có thể
  enable `UI/Cancel` trước khi `InputSystemUIInputModule` của MainMenu cũ chạy `OnDisable` và disable
  action trên asset dùng chung.
- Direct Play DemoScene không có outgoing MainMenu module nên không tái hiện; Player build transition
  có đúng thứ tự gây mất Cancel. Player log không có exception, phù hợp với lifecycle race này.

## Fix tối thiểu

- `GameInputCoordinator` tạo runtime copy riêng từ `_projectActions` và chỉ enable/disable
  `UI/Cancel` trên copy do coordinator sở hữu.
- Runtime copy được destroy trong `OnDestroy`; `PlayerInput` và `InputSystemUIInputModule` tiếp tục
  giữ ownership hiện tại, không đổi scene hierarchy, serialized reference hay UI layout.
- Không sửa `GameStateManager`, save/progression/world backend hoặc Build Settings.

## Regression coverage và verification

- Thêm `GameInputCoordinatorPlayModeTests`:
  - mô phỏng project action bị disable sau khi coordinator enable; Cancel vẫn chuyển
    `Playing → Paused`;
  - disable/enable coordinator không double-subscribe.
- Targeted regression: 2/2 PASS.
- Live DemoScene: runtime asset khác project asset, `UI/Cancel` enabled; simulated keyboard Escape
  chuyển `Playing → Paused`.
- EditMode: 58/58 PASS.
- PlayMode: 121/121 PASS.
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset checked.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Build Settings giữ nguyên: MainMenu index 0, DemoScene index 1.
- Player build mới: `C:\Users\havin\Phase10PlayerBuild_Codex\ProjectGame2D.exe`; build success,
  0 error, 0 warning, 515.85 MB.
- Player smoke đạt `MainMenu → New Game Slot 3 → DemoScene`; Slot 1 không bị chọn/ghi đè.

## Manual re-verification còn lại

- Windows UI automation của môi trường gửi click được nhưng không tạo raw keyboard event mà Unity
  Input System trong Player nhận được (cả `I` và `Escape` đều không phản hồi); vì vậy không dùng kết
  quả synthetic key này để tuyên bố Player acceptance PASS/FAIL.
- Cần người dùng nhấn Escape vật lý trên build mới để xác nhận Pause mở, sau đó chạy full
  `Save → Return → Continue → restore → Quit`. Chưa tuyên bố Phase 10 `CONTENT_READY`.

---

# Phase 10 Pause Input Recheck + Save Game Slot Picker → Claude Follow-up

Status: `READY_FOR_PHASE10_REVERIFICATION`

Ngày: 2026-08-23

## Việc 1 — Pause input fix đã đóng phần implementation/build

- Root cause giữ nguyên sau khi đối chiếu lại scene serialization và regression: outgoing
  `InputSystemUIInputModule` và incoming `GameInputCoordinator` từng tranh cùng project
  `InputActionAsset`; coordinator nay sở hữu runtime clone riêng cho `UI/Cancel`.
- Targeted `GameInputCoordinatorPlayModeTests`: 2/2 PASS.
- Full regression trước Save picker: EditMode 58/58, PlayMode 137/137 PASS.
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset checked.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Build Settings không đổi: MainMenu index 0, DemoScene index 1.
- Player build mới: `Builds/Phase10PauseFix/ProjectGame2D.exe`; Windows64 build success,
  0 error, 0 warning, 515.85 MB.
- Player smoke bằng Windows computer-use đi được `MainMenu → New Game Slot 2 → DemoScene`; Slot 1
  không bị chọn/ghi đè. Công cụ gửi click được nhưng đối chứng phím `D` cũng không tạo raw keyboard
  event cho Unity Input System, nên Escape synthetic không được dùng để kết luận PASS/FAIL.
- Acceptance còn lại không đổi: người dùng nhấn Escape vật lý trên build này, sau đó chạy full
  Save → Return → Continue restore. Chưa tuyên bố `CONTENT_READY`.

## Việc 2 — Save Game slot picker

Status UI: `VERIFIED`

- `PauseMenuUI` dùng chung responsive three-slot overlay hiện có cho hai mode `LOAD GAME` và
  `SAVE GAME`; không nhân đôi save presentation và không đổi backend ownership.
- Nút Save Game giờ mở slot picker. Action mỗi slot gọi `CanSaveToSlot`/`RequestSaveToSlot`; popup
  overwrite gọi `ConfirmOverwriteAndSave` hoặc `CancelSaveToSlot`.
- Popup có text riêng cho Valid, Corrupted và IncompatibleVersion. Delete chỉ xuất hiện ở Save mode,
  luôn hỏi xác nhận rồi mới gọi `DeleteSlot`.
- Empty slot save thành công đóng overlay, giữ Pause flow và cập nhật timestamp từ
  `OnSaveSucceeded`. Load mode giữ nguyên `CanLoad`/`RequestLoad`.
- Chặn double-submit cùng frame ở presentation; khi backend `IsBusy`, toàn bộ action vẫn disable theo
  contract cũ.
- Hierarchy/component được bind bằng Unity MCP trong DemoScene. Ba delete button dùng font
  `DigitalDisco SDF v3`; title/slot layout nằm trong viewport responsive và PauseMenu giữ top sibling
  để Quest/Tutorial không render đè modal.

## Runtime/test verification cuối

- Empty Slot 3: click Save ghi ngay, không popup; `ActiveSlotId` chuyển sang 3, slot thành Valid,
  overlay đóng và timestamp cập nhật.
- Valid Slot 3: popup đúng `OVERWRITE THE SAVE IN SLOT 3?`; Cancel giữ overlay và save không đổi.
- Corrupted/Incompatible presentation: text status-specific đúng contract.
- Delete Slot 3: popup đúng slot + cảnh báo irreversible; Cancel không xóa save.
- Hai click slot trong cùng frame chỉ phát 1 `OnSaveSlotConfirmationRequired`.
- 4 PlayMode presentation tests mới kiểm tra mapping text overwrite/delete.
- Full final regression: EditMode 58/58, PlayMode 141/141 PASS.
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset checked.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Không sửa `GameplaySessionController`, repository, save capture, Quest/Tutorial/Commerce/Inventory
  hoặc world persistence trong phần UI này.

---

# Phase 10 Final Physical Player Acceptance → Claude Follow-up

Status: `READY_FOR_PHASE10_CONTENT_READY_CONFIRMATION`

Thời gian chạy: 2026-08-23 21:57:18 +07:00  
Build: `C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe`

Owner đã chạy acceptance bằng bàn phím/chuột vật lý và xác nhận toàn bộ luồng PASS. Các slot test là
Slot 2 và Slot 3; Slot 1 không được chọn, ghi đè hoặc xóa.

## Kết quả từng bước

1. Launch Player build và hiển thị MainMenu: **PASS**.
2. New Game bằng slot trống, không dùng Slot 1: **PASS**.
3. Transition MainMenu → DemoScene: **PASS**.
4. Nhấn Escape mở PauseMenuUI: **PASS**.
5. Đóng/mở PauseMenuUI lại nhiều lần bằng Escape: **PASS**.
6. Save Game mở three-slot picker: **PASS**.
7. Save vào Empty slot ghi ngay, không hiện popup phụ: **PASS**.
8. Sau Empty save, overlay đóng và timestamp cập nhật: **PASS**.
9. Chọn Valid slot hiện đúng popup `OVERWRITE THE SAVE IN SLOT n?`: **PASS**.
10. Cancel overwrite giữ nguyên save cũ: **PASS**.
11. Confirm overwrite ghi đè thật: **PASS**.
12. Save As từ Slot A sang Slot B khác: **PASS**.
13. Slot đích Save As hiển thị `ACTIVE`, xác nhận `ActiveSlotId` đã chuyển: **PASS**.
14. Delete hiện popup riêng với cảnh báo không thể hoàn tác: **PASS**.
15. Cancel Delete không xóa save; Confirm Delete mới xóa: **PASS**.
16. Return Main Menu sau khi save: **PASS**.
17. Continue đúng slot vừa save: **PASS**.
18. Vị trí nhân vật restore đúng: **PASS**.
19. Inventory item và gold restore đúng: **PASS**.
20. Quest state restore đúng: **PASS**.
21. Tutorial state restore đúng: **PASS**.
22. Persistent world state (chest/pickup/boss/resource node) restore đúng: **PASS**.
23. Không mất, nhân đôi hoặc rò dữ liệu giữa các slot trong luồng kiểm tra: **PASS**.
24. Quit Desktop thoát sạch, không treo: **PASS**.

## Kết luận

- Không phát hiện UI issue hoặc backend/contract gap trong final physical acceptance.
- Automated verification trước acceptance vẫn là EditMode 58/58, PlayMode 141/141, Content
  Validation 0 error, DemoScene/MainMenu validator 0 issue.
- Phase 10 đã đạt acceptance bar phía Codex/owner và sẵn sàng để Claude xác nhận cuối, cập nhật
  `Phase10ImplementationReport.md` cùng Roadmap sang `CONTENT_READY`.

---

# Content Production — Side Quest `quest.side.potion_supply.001`

Status: `BACKEND_GAP_FOUND`

Ngày kiểm tra: 2026-08-23 (+07:00)

## Content đã author bằng Unity MCP

- Asset: `Assets/Quests/Definitions/Quest_SidePotionSupply001.asset`.
- Catalog: đã đăng ký vào `Assets/Quests/QuestCatalog.asset` mà `QuestManager` trong DemoScene dùng;
  catalog hiện có 3 quest và resolve được ID mới.
- `questId`: `quest.side.potion_supply.001`.
- Tên hiển thị: `Potion Supply Run`.
- Giver/turn-in NPC: `npc.town.elder`; không prerequisite; không phải Tutorial/Main Quest.
- Objective duy nhất: `Purchase`, target `item.consumable.health_potion`, số lượng 1, description
  `Purchase 1 Health Potion from the Town Elder's general shop.`
- Reward: 1 `item.material.wood`, 10 gold, 20 experience.
- Không tạo/sửa C# và không sửa manager/core/UI.

## End-to-end content verification

1. `QuestNpcInteractionService.TryGetOfferedQuest("npc.town.elder")` trả đúng quest mới sau khi
   fixture runtime đánh dấu hai quest đứng trước đã Completed: **PASS**.
2. Accept qua `QuestNpcInteractionService`: `Available -> Active`, counter `0/1`: **PASS**.
3. Save bằng `QuestManager.ToSaveData()` rồi restore bằng `RestoreState()`: giữ `Active`, counter
   `0/1`, không tự tăng objective: **PASS**.
4. Purchase thật qua `ShopNpcInteractionService` tại `shop.town.general`: transaction `Success`,
   counter `1/1`, quest chuyển `ReadyToTurnIn`: **PASS**.
5. Turn-in qua NPC service: result `Success`, quest chuyển `Completed`: **PASS**.
6. Reward: Wood `0 -> 1`, gold sau chi phí mua `80 -> 90`, XP level 1 `0 -> 20`: **PASS**.
7. Turn-in lần hai bị từ chối và không cấp reward lần nữa: **PASS**.
8. Save/restore trạng thái Completed giữ nguyên inventory/gold/level/XP, không phát lại reward và
   không reset quest: **PASS**.

## Validation và regression

- Content Validation: **PASS**, 0 error, 60 accepted legacy warning, 84 asset checked; không có
  warning/error từ quest mới.
- DemoScene validator: **PASS**, 0 issue, 0 missing script, 0 broken prefab.
- EditMode: **PASS 58/58**.
- Hai PlayMode test lỗi khi chạy toàn suite 141 test:
  - `GameInputCoordinatorPlayModeTests.DisableEnable_DoesNotDoubleSubscribe`
  - `GameInputCoordinatorPlayModeTests.SharedProjectActionDisabledAfterEnable_CancelStillPauses`
  - Cả hai expected `Paused` nhưng nhận `Playing`; kết quả lặp lại ở hai lần full-suite.
  - Khi chạy riêng đúng hai test, **PASS 2/2**, cho thấy lỗi phụ thuộc thứ tự/lifecycle hoặc rò state
    giữa PlayMode tests, không liên quan dữ liệu quest mới.

## Gap cần Claude xử lý

Content quest và toàn bộ acceptance riêng của quest đã PASS, nhưng Definition of Done yêu cầu full
PlayMode regression xanh nên chưa thể đặt `VERIFIED`. Nhờ Claude chẩn đoán test isolation/lifecycle
của `GameInputCoordinatorPlayModeTests` khi chạy trong full suite. Codex không sửa lan sang input
backend vì task này chỉ được author content, không được sửa C# core.

---

# Final Quest Regression Recheck — PlayMode Isolation Vẫn Flake

Status: `BACKEND_GAP_FOUND`

Thời gian kiểm tra: 2026-08-23 22:47:11 +07:00  
Commit đã kiểm tra: `516169df` (`Fix PlayMode test isolation in GameInputCoordinatorPlayModeTests`),
HEAD trùng `origin/SuperaAI`.

## Kết quả độc lập phía Codex

- Compile refresh: **PASS**, Unity trở về idle, không có C# compile diagnostic.
- EditMode full suite: **PASS 58/58**.
- PlayMode full suite vòng 1: **PASS 142/142**.
- PlayMode full suite vòng 2 ngay sau đó: **FAIL 140/142**:
  - `GameInputCoordinatorPlayModeTests.DisableEnable_DoesNotDoubleSubscribe`
  - `GameInputCoordinatorPlayModeTests.SharedProjectActionDisabledAfterEnable_CancelStillPauses`
  - Cả hai: expected `Paused`, actual `Playing`.
- Chạy riêng 3 test trong `GameInputCoordinatorPlayModeTests` ngay sau vòng full-suite bị lỗi:
  **FAIL 1/3**; test isolation mới
  `SetUp_SuppressesLeftoverSceneCoordinator_TearDownRestoresIt` pass, nhưng hai test Cancel cũ vẫn
  fail với cùng kết quả `Playing`.
- Content Validation: **PASS**, 0 error, 60 accepted legacy warning, 84 asset checked.
- DemoScene validator: **PASS**, 0 issue, 0 missing script, 0 broken prefab.
- Quest `quest.side.potion_supply.001`: asset còn tồn tại và đã đăng ký trong catalog; giver/turn-in
  vẫn là `npc.town.elder`; reward vẫn là 1 Wood, 10 gold, 20 XP: **PASS**.

## Sai lệch cần xử lý tiếp

Fix hiện tại chưa bảo đảm hai lần PlayMode suite liên tiếp xanh. Trạng thái gây double-processing
dường như sống xuyên qua lần PlayMode run đầu tiên; ở trạng thái đó, ngay cả targeted run tiếp theo
cũng còn fail, dù test mới xác nhận `_leakedSceneCoordinator` fixture đã được suppress. Điều này cho
thấy vẫn còn một coordinator/callback/action/device hoặc static lifecycle khác chưa được cleanup hay
chưa được cơ chế suppress hiện tại tìm thấy.

Theo yêu cầu verification-only, Codex không sửa code. Mục quest cũ giữ `BACKEND_GAP_FOUND`, chưa đổi
sang `VERIFIED`. Nhờ Claude tái hiện đúng chuỗi: full PlayMode PASS -> full PlayMode lần hai ->
targeted 3 tests trong cùng Editor session, rồi điều tra state còn sót giữa các test jobs.

---

# Final Quest Verification trên commit `22f8f85e` — Diagnostic Flake Reproduced

Status: `BACKEND_GAP_FOUND`

Thời gian kiểm tra: 2026-08-23 23:11:32 +07:00  
Commit: `22f8f85e` (`Harden GameInputCoordinatorPlayModeTests isolation against cross-test timing gap`),
HEAD trùng `origin/SuperaAI`.

## Môi trường và trình tự

- Chạy bằng Unity MCP Test Runner API trong cùng một Unity Editor session; không đóng Editor, không
  refresh/domain-reload thủ công giữa các bước.
- Scene đang mở trong Editor: `Assets/Scenes/DemoScene.unity` (build index 1), clean và loaded.
- Trình tự giữ nguyên đúng lần Codex từng tái hiện: full PlayMode lần 1 -> full PlayMode lần 2 ngay
  sau -> targeted 3 test ngay sau lần 2.

## Kết quả

1. Full PlayMode lần 1, job `a464b45d715f4a70b900d27fd30922f0`: **PASS 142/142**.
2. Full PlayMode lần 2, job `2aa85f0e2e4d4aa6b36ef3a1cbbb2271`: **FAIL 140/142**.
3. Targeted 3 test ngay sau đó, job `7aec95477f7f4bf7bf2dc701cbc1fda5`:
   **PASS 3/3**.

Hai test fail ở vòng full thứ hai:

- `GameInputCoordinatorPlayModeTests.DisableEnable_DoesNotDoubleSubscribe`
- `GameInputCoordinatorPlayModeTests.SharedProjectActionDisabledAfterEnable_CancelStillPauses`

## Assertion message nguyên văn

`DisableEnable_DoesNotDoubleSubscribe`:

```text
Live GameInputCoordinator instances:
- GameInputCoordinatorFixture scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=True enabled=True
- LeakedSceneGameInputCoordinator scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=False enabled=True
Expected: Paused
But was:  Playing
```

`SharedProjectActionDisabledAfterEnable_CancelStillPauses`:

```text
Live GameInputCoordinator instances:
- GameInputCoordinatorFixture scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=True enabled=True
- LeakedSceneGameInputCoordinator scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=False enabled=True
Expected: Paused
But was:  Playing
```

## Nhận xét bằng chứng

- Diagnostic tại thời điểm fail không hiển thị foreign coordinator active nào ngoài fixture; fake
  leaked coordinator đã inactive đúng như cơ chế suppress mong đợi.
- Dù vậy một Escape vẫn kết thúc ở `Playing`. Vì targeted run kế tiếp PASS 3/3, lỗi tiếp tục phụ
  thuộc full-suite/test-job lifecycle thay vì thất bại ổn định trong riêng fixture.
- Khác biệt đáng chú ý so với phiên Claude: Codex chạy qua Unity MCP Test Runner API và mở
  DemoScene trong Editor. Claude báo đã chạy cùng chuỗi nhưng không tái hiện; cần đối chiếu Claude
  dùng Test Runner UI, MCP tool hay execute-code runner và scene nào đang mở.

Codex không sửa test/production code theo phạm vi verification-only. Không đổi mục quest sang
`VERIFIED`; chưa chạy nhánh Compile/EditMode/Content/DemoScene validation sau fail vì yêu cầu chỉ
thực hiện các gate đó khi toàn bộ trình tự PlayMode PASS.
