# Claude → Codex Handoff

Status: `VERIFIED_FISHING_MYSTERY_ICON_AND_SLIDER_CAP_FIX`

Ngày: 2026-09-28
Feature: Tích hợp `fishing_mystery_fish_icon.png` (`FISHING_MYSTERY_FISH_ICON_ART_READY`, D-056) +
tự phát hiện và sửa thêm 1 bug UI: fill slider đè lên chốt vàng trang trí của track. Cả 2 đã verify
Play Mode PASS.

## 1. Mystery fish icon — force reimport & tích hợp

- Force reimport `fishing_mystery_fish_icon.png` qua `AssetDatabase.ImportAsset(...,
  ForceUpdate | ForceSynchronousImport)`; verify bbox khớp đúng Codex báo (93.55%/77.42%).
- `FishingFeatureAuthoring.cs`: **không cần sửa gì** — đã có sẵn `TryLoadSprite(UiFolder +
  "fishing_mystery_fish_icon.png")` từ đợt trước, chạy lại
  `Tools/Project Game/Fishing/Build And Install MapNhat Fishing` là prefab tự nhận sprite mới qua
  field `_mysteryFishSprite`.
- `FishIcon`: `sizeDelta = 62×62` (không đổi), `color = RGBA(1,1,1,1)`, `preserveAspect = true` —
  verify qua `PrefabUtility.LoadPrefabContents`.
- Verify Play Mode qua flow thật (`TryBeginFishing`→`BeginMinigame`): `_fishImage.sprite =
  fishing_mystery_fish_icon`, hiện đúng trên `MovementTrack`, không bị crop, đọc rõ silhouette + dấu
  "?" trên nền xanh dương. **Screenshot**: `Assets/Screenshots/screenshot-20260927-235318.png`.
- Xác nhận cá thật chỉ lộ ra ở `ResultPanel` khi `CompleteCatch()` — test trực tiếp, hiện đúng icon
  `Fish1_7`/tên cá/cân nặng/giá, không đổi hành vi so với đợt tích hợp reveal-on-catch trước.
  **Screenshot**: `Assets/Screenshots/screenshot-20260927-235349.png`.
- Console: không lỗi/warning mới liên quan Fishing.

## 2. Bug tự phát hiện: fill đè lên chốt vàng của track (owner báo qua screenshot thật)

Sau khi owner xem Play Mode, phát hiện thanh fill (slider) tràn lên che mất phần chốt vàng trang trí
ở 2 đầu `fishing_slider_track_v3` — hệ quả phụ của lần sửa padding trước đó (`Fill Area` inset về
đúng `(0,0)` để fill đủ rộng, nhưng đồng thời cũng xoá luôn khoảng hở dọc từng có, khiến fill tràn
vào đúng vùng chốt vàng ở 2 đầu).

- Đo bằng Python/PIL: chốt vàng flare chiếm khoảng `y=10..19` và `y=961..969` trên canvas 980px
  (thân kênh thẳng ổn định từ `y=19` đến `y=961`) — quy đổi logic ≈ 9-10px mỗi đầu.
- **`Assets/Editor/FishingFeatureAuthoring.cs`**: `Fill Area` đổi `offsetMin`/`offsetMax` từ
  `(0,0)`/`(0,0)` sang `(0,12)`/`(0,-12)` — **giữ nguyên 0 theo chiều ngang** (đúng yêu cầu trước, fill
  vẫn đủ rộng 46px), chỉ thêm lại 12px inset dọc mỗi đầu để fill dừng trước vùng chốt vàng.
- Verify Play Mode tại `progress = 0.9` (worst-case gần đầy) qua flow thật (ép `_progress` trên
  controller + `SetProgress` trên UI cùng lúc, vì `TickMinigame` sẽ ghi đè giá trị set một mình qua
  UI nếu không đồng bộ) — cả chốt vàng trên và dưới đều lộ ra đầy đủ, fill nằm gọn trong lòng kênh,
  không tràn: **PASS**. Không dùng asset mới, thuần chỉnh offset.
- Console: không lỗi/warning mới.

## Sai khác/vấn đề còn tồn tại

Không có. Fishing UI (D-055 + D-056) coi như hoàn tất đợt này.

---

Ngày: 2026-09-27
Feature: Gen 1 icon "cá bí ẩn" cho cơ chế reveal-on-catch mới (D-056) — owner muốn ẩn danh tính cá
trong lúc chơi minigame, chỉ lộ ra khi bắt thành công (xem entry `VERIFIED_FISHING_UI_V3_PADDING_FIX`
bên dưới cho bối cảnh bộ UI `_v3` đã hoàn thiện).

## Bối cảnh

- `FishIcon` (di chuyển trong `MovementTrack` lúc chơi) trước đây hiện đúng sprite thật của con cá đã
  roll ngẫu nhiên (`_selectedFish.icon`, ví dụ `Fish1_7` cho River Minnow) — owner muốn thay bằng 1
  icon chung, không tiết lộ loại cá.
- Code đã sẵn sàng chờ asset này (`FishingMinigameUI._mysteryFishSprite`, load qua
  `TryLoadSprite` — không crash nếu thiếu, chỉ ẩn `FishIcon` tạm thời cho tới khi asset tồn tại).
- Loại cá thật vẫn lộ ra bình thường ở `ResultPanel` khi thành công (dùng icon thật có sẵn trong
  `Assets/Tiles/Tilesets/Fishing and Gathering Pixel Art RPG Icons/`, không cần Codex gen thêm).

## Asset cần gen

Output: `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_mystery_fish_icon.png`

- Kích thước logic **62×62** (khớp `FishIcon.sizeDelta` hiện có, không đổi code/layout) → bitmap
  **124×124px** (2x, pixel-art thật, nearest 4x upscale như các asset `_v3` khác).
- Nội dung: silhouette cá đơn giản (tối màu/xám, có thể thêm dấu "?" nhỏ) — rõ ràng là "chưa biết
  loại gì", khác hẳn các icon cá thật đã có (không dùng lại màu/hoa văn của Fish1/Fish2...).
- Đồng bộ phong cách Dark Inventory Style/pixel-art của bộ `_v3` đã duyệt (tham chiếu
  `fishing_bobber_bite_00.png` về mật độ chi tiết/kích thước pixel cho phù hợp).
- RGBA, Sprite Mode Single, Filter Point, Mipmap Off, Compression None, PPU 100, Border 0.

## Việc Codex KHÔNG cần làm

- Không đổi 8 asset `_v3` hay 6 frame phao đã duyệt.
- Không sửa code — `FishingFeatureAuthoring.cs` đã có sẵn `TryLoadSprite(UiFolder +
  "fishing_mystery_fish_icon.png")`, chỉ cần file PNG xuất hiện đúng path là tự động gán khi Claude
  chạy lại rebuild.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`FISHING_MYSTERY_FISH_ICON_ART_READY`.

---

Status: `VERIFIED_FISHING_UI_V3_PADDING_FIX`

Ngày: 2026-09-27
Feature: Tích hợp 3 asset padding-fix (`FISHING_UI_V3_PADDING_FIX_ART_READY`) + sửa `Fill Area`
padding cứng phát hiện thêm trong lúc tích hợp — đã gán xong, verify Play Mode thật, PASS.

## Force reimport

Đã force reimport `fishing_waiting_panel_v3.png`, `fishing_slider_track_v3.png`,
`fishing_slider_fill_v3.png` qua `AssetDatabase.ImportAsset(..., ImportAssetOptions.ForceUpdate |
ForceSynchronousImport)`, verify lại bbox bằng Python/PIL khớp đúng số Codex báo (90.4%/89.2%,
86.96%/99.6%, 86.96%/100%).

## Prefab/authoring script thay đổi

- **`Assets/Editor/FishingFeatureAuthoring.cs`**: 1 thay đổi — `CreateVerticalSlider`'s `Fill Area`
  đổi `offsetMin`/`offsetMax` từ `(5,5)`/`(-5,-5)` (padding cứng có sẵn từ code gốc, chưa từng gây
  chú ý khi còn màu phẳng) sang `Vector2.zero` cả hai, để `Fill Area` stretch đúng full `CatchProgress`
  46×490 theo yêu cầu Codex.

## Kích thước thực tế

`CatchProgress` **46×490** (không đổi, đúng vị trí `(105,-10)`). `Fill Area` sau fix: `rect.size =
(46, 490)` (trước đó `(36, 480)` do padding 5px). `Track`/`Fill` Image đều `color = white`,
`Type.Simple`, stretch đầy `Fill Area`.

## Offset `fillRect`

`Slider.fillRect` trỏ đúng `Fill` (child của `Fill Area`), không còn offset trái/phải — verify qua
`fillRect.rect.width = 46` tại `slider.value = 0.5` (trước đó bị bó hẹp bởi padding cha).

## Kết quả Play Mode

- **WaitingPanel**: TMP "Waiting for a bite..." giờ nằm gọn trong khung, không tràn ra ngoài: **PASS**.
- **Slider tiến trình**: track/fill rõ ràng, dễ đọc tiến độ 0-100%, fill dâng đúng từ dưới lên theo
  `slider.value`; verify tại `value=0.6` — fill chiếm ~60% chiều cao, đầy chiều rộng track, không lệch
  trái/phải: **PASS**.
- Không có crop ngang bởi Mask/RectMask2D nào (không dùng mask ở component này).
- Console: không lỗi/warning mới liên quan Fishing.

## Vấn đề còn tồn tại

Không có sai khác so với yêu cầu.

---

Ngày: 2026-09-27
Feature: Sửa lỗi padding trên 3 asset của bộ `_v3` (`fishing_waiting_panel_v3.png`,
`fishing_slider_track_v3.png`, `fishing_slider_fill_v3.png`) — owner xem screenshot Play Mode thật và
phát hiện nội dung vẽ nhỏ hơn hẳn canvas, gây 2 vấn đề trực quan cụ thể.

## Vấn đề đo được (Python/PIL alpha bounding box, không phải cảm quan)

| File | Canvas | Bbox nội dung thật | Tỉ lệ lấp đầy |
| --- | --- | --- | --- |
| `fishing_waiting_panel_v3.png` | 1000×148 | (288,28)-(712,116) | **42% rộng / 60% cao** |
| `fishing_slider_track_v3.png` | 92×980 | (52,0)-(84,976) | **35% rộng** |
| `fishing_slider_fill_v3.png` | 92×980 | (48,0)-(76,980) | **30% rộng** |

So sánh: các asset khác trong cùng bộ (`bite_prompt`, `minigame_panel`, `result_panel`,
`movement_track`) đều lấp 81-98% canvas — 3 file trên là ngoại lệ, để nhiều padding trong suốt thừa.

## Hệ quả owner quan sát được qua Play Mode

1. **`WaitingPanel`**: TMP "Waiting for a bite..." kéo giãn theo đúng `RectTransform` 500×74 (khớp
   canvas), nhưng board thật chỉ vẽ ở một vùng nhỏ ở giữa → chữ nhìn to hơn hẳn khung.
2. **Slider tiến trình**: `CatchProgress` vốn đã hẹp theo thiết kế gốc (46 logic px, khoá cứng không
   đổi), nội dung vẽ chỉ chiếm ~30-35% trong đó → thanh hiển thị thực tế chỉ ~14-16px, gần như không
   đọc được tiến độ 0-100%. Icon cá (62px, không đổi) vì vậy nhìn to hơn hẳn slider.

## Yêu cầu sửa

Giữ NGUYÊN kích thước canvas (không đổi 1000×148 / 92×980 / 92×980, không đổi RectTransform trong
code) — chỉ vẽ lại để nội dung lấp gần đầy canvas, cùng tỉ lệ với các asset khác trong bộ (~85-95%):

- **`fishing_waiting_panel_v3.png`** (1000×148): board vẽ lấp ít nhất ~85% chiều rộng, ~85% chiều
  cao canvas — đủ chỗ cho TMP text kéo giãn full rect mà không tràn ra ngoài viền board.
- **`fishing_slider_track_v3.png`** (92×980): thanh nền vẽ lấp tối thiểu ~80% chiều rộng canvas
  (hiện chỉ 35%) — để ở logic 46px vẫn đủ dày nhìn thấy rõ.
- **`fishing_slider_fill_v3.png`** (92×980): thanh fill vẽ lấp tối thiểu ~80% chiều rộng canvas
  (hiện chỉ 30%), cùng vị trí ngang với track để không lệch tâm khi chồng lên nhau.

Giữ nguyên phong cách/màu đã duyệt (charcoal/walnut cho waiting panel; xanh dương cho track, vàng/gold
cho fill — theo đúng bộ `_v3` hiện tại), chỉ tăng tỉ lệ lấp đầy, không đổi palette.

## Việc Codex KHÔNG cần làm

- Không đụng 5 asset còn lại của bộ `_v3` (`bite_prompt`, `minigame_panel`, `movement_track`,
  `catch_zone`, `result_panel`) — đã verify tỉ lệ lấp đầy tốt (81-98%), không có vấn đề.
- Không đổi kích thước canvas hay tên file (giữ nguyên `_v3`, ghi đè trực tiếp).
- Không sửa code.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`FISHING_UI_V3_PADDING_FIX_ART_READY`, nêu tỉ lệ lấp đầy mới đo được cho cả 3 file.

---

Ngày: 2026-09-27
Feature: Tích hợp bộ Fishing UI `_v3` (khớp kích thước gốc) + phao câu cá animation thay dấu "!"
(`FISHING_UI_V3_LOCKED_SIZE_ART_READY`) — đã gán xong, verify Play Mode thật, PASS.

## File code/prefab đã sửa

- **`Assets/Editor/FishingFeatureAuthoring.cs`**: thêm `using UnityEditor.Animations;`, const
  `UiFolder`/`BobberControllerPath`; load 8 sprite `_v3` + `bobberFrame0` + `AnimationClip` đầu
  authoring; đổi 6 lệnh dựng panel màu phẳng (`CreatePanel`) sang `CreateSpritePanel` (sprite thật,
  `Image.Type.Simple`, `color = white`) — xoá hẳn `CreatePanel` vì không còn nơi dùng; `CatchZone`
  thêm `Image.Type = Sliced`; `CreateVerticalSlider` nhận thêm `trackSprite`/`fillSprite`, **giữ
  nguyên `Slider`/`fillRect` gốc, không đổi sang `Image.Type.Filled`**; xoá dòng tạo TMP
  `"Exclamation"`, thay bằng child `BobberIcon` (Image + Animator); thêm helper
  `GetOrCreateBobberController()` tạo `Assets/Prefabs/Fishing/FishingBobberBite.controller` (1 state
  `BiteLoop` chứa `fishing_bobber_bite_loop.anim`, idempotent — chỉ tạo nếu chưa có).
- **Không sửa** `FishingMinigameController.cs`, `FishingMinigameUI.cs`, `FishingSpotDefinition`,
  `FishDefinitionSO` — đúng yêu cầu "không đổi logic tiến trình nếu không thật sự cần".
- Chạy lại `Tools/Project Game/Fishing/Build And Install MapNhat Fishing` để cập nhật
  `FishingFeature.prefab`.

## Xác nhận không đổi `sizeDelta`/`anchoredPosition` gốc

Verify trực tiếp qua `PrefabUtility.LoadPrefabContents` sau rebuild — toàn bộ khớp 100% giá trị
hardcode gốc: `WaitingPanel 500×74`, `BitePrompt 150×150`, `MinigamePanel 520×650`,
`MovementTrack 150×490 @ (-75,-10)`, `CatchZone 128×120`, `CatchProgress 46×490 @ (105,-10)`,
`ResultPanel 620×180`. Không có bất kỳ dòng code nào sửa các con số này.

## 8 sprite `_v3` đã gắn

`WaitingPanel`→`fishing_waiting_panel_v3`, `BitePrompt`→`fishing_bite_prompt_v3`,
`MinigamePanel`→`fishing_minigame_panel_v3`, `MovementTrack`→`fishing_movement_track_v3`,
`ResultPanel`→`fishing_result_panel_v3` (`Image.Type.Simple`, `color = white`).

## Cấu hình CatchZone 9-slice

`CatchZone` → `fishing_catch_zone_v3`, `Image.Type = Sliced`, border đọc từ `.meta` đã author sẵn
(**Left 0, Bottom 32, Right 0, Top 32**, không hardcode lại trong code).

## Cấu hình Slider gốc

`CatchProgress` giữ đúng cơ chế `Slider`/`fillRect` gốc: background → `fishing_slider_track_v3`
(`Type.Simple`), `Fill` → `fishing_slider_fill_v3` (`Type.Simple`) bên trong `Fill Area` (inset 5px),
`slider.fillRect` trỏ đúng `Fill`. **Không** dùng `Image.Type.Filled`.

## BobberIcon và animation clip

`BobberIcon` là child GameObject riêng của `BitePrompt`, anchor/pivot `(0.5,0.5)`,
`anchoredPosition = (0,0)`, `sizeDelta = (48,64)` (đúng đề xuất, không cần giảm — verify trực quan
không chạm ring). `Image.sprite = fishing_bobber_bite_00`, `preserveAspect = true`, `color = white`.
`Animator.runtimeAnimatorController = FishingBobberBite.controller` (tạo mới, 1 state `BiteLoop`
chứa `fishing_bobber_bite_loop.anim`). Vì `BobberIcon` là con của `BitePrompt`, khi
`FishingMinigameUI.SetOnly()` deactivate `BitePrompt` (chuyển sang Minigame/Waiting/Result), Animator
tự dừng theo vòng đời GameObject — **không cần sửa `FishingMinigameUI.cs`** để đạt yêu cầu "animation
chỉ chạy khi BitePrompt hiển thị". TMP `"!"` đã bị xoá hoàn toàn khỏi authoring.

## Kết quả Play Mode (Game View 1920×1080, verify qua flow gameplay thật: `TryBeginFishing` → force
`BiteReady` → `ShowBitePrompt`/`BeginMinigame`/`ShowResult`)

1. WaitingPanel đúng kích thước 500×74, không lệch vị trí: **PASS**.
2. BitePrompt xuất hiện đúng board 150×150: **PASS**.
3. Không còn dấu "!": **PASS** (verify qua `PrefabUtility.LoadPrefabContents`, không tìm thấy
   GameObject "Exclamation").
4. Phao nằm chính giữa board, animation chạy (verify `Animator.GetCurrentAnimatorStateInfo(0)
   .normalizedTime` tăng liên tục qua nhiều lần loop, sprite hiện tại đổi từ frame `_00` sang `_03`
   giữa 2 lần chụp cách nhau ~15s thật): **PASS**.
5. Phao không chạm/vượt khỏi ring (verify qua screenshot zoom, bobber 48×64 nằm gọn trong vùng rỗng
   ~110px đường kính của ring): **PASS**.
6. MovementTrack là channel đặc (nền xanh dương liền khối, viền walnut/gold), không bị kéo thành
   đường kẻ mảnh: **PASS**.
7. CatchZone (màu emerald/xanh lá) nổi rõ trên nền xanh dương của MovementTrack — tương phản mạnh,
   dễ nhận diện: **PASS**.
8. CatchZone đổi chiều cao theo gameplay thật (`TickMinigame` tự tính lại theo vật lý catch-zone,
   không phải set tay) — cùng cơ chế 9-slice đã verify không méo ở đợt trước, art mới không đổi cách
   `Image.Type.Sliced` hoạt động: **PASS**.
9. Slider gốc vẫn tăng đúng qua `Slider.fillRect`: **PASS** (verify field `slider.fillRect` trỏ đúng
   `Fill`, không đổi cơ chế).
10. ResultPanel hiện đúng "Success! Caught a Blue Carp (1450g)", đọc rõ: **PASS**.
11. Console: không lỗi/warning mới liên quan Fishing.

**Screenshot**: `Assets/Screenshots/screenshot-20260927-224701.png` (BitePrompt có phao ở giữa),
`Assets/Screenshots/screenshot-20260927-224744.png` (Active Minigame). Không quay được GIF/video
trong phiên làm việc này (giới hạn công cụ); animation đã xác nhận trực tiếp qua state Animator +
so sánh sprite giữa 2 screenshot như mục 4.

## Sai khác/vấn đề còn tồn tại

- Không có sai khác so với yêu cầu.
- **Bug không liên quan phát hiện lại lần 2 (đã biết từ vòng trước, không phải do đợt sửa này gây
  ra):** `CreateFishingSpot()` trong `FishingFeatureAuthoring.cs` hardcode `entries.arraySize = 2`
  và chỉ set 2 loại cá (RiverMinnow, BlueCarp) — mỗi lần chạy "Build And Install" đều ghi đè mất
  8 loại cá khác đang có trong `FishingSpot.River.asset` (bản gốc có 10 loại). Đã tự phục hồi bằng
  `git checkout -- Assets/Game/Fishing/Definitions/FishingSpot.River.asset` sau mỗi lần rebuild trong
  phiên này. Đây là bug tồn tại từ trước, ngoài phạm vi yêu cầu UI — nên xử lý riêng (sửa
  `CreateFishingSpot` để chỉ thêm 2 fish này vào danh sách nếu chưa có, thay vì ghi đè toàn bộ mảng).

---

Ngày: 2026-09-27
Feature: Gen lại bộ 8 asset Fishing UI (`_v3`), vòng 3, sau khi owner yêu cầu **revert toàn bộ về bản
gốc** rồi làm lại theo hướng khoá cứng kích thước để không còn rủi ro lệch bố cục.

## Bối cảnh — vì sao revert

- Đợt `_v2` (phóng to `MinigamePanel` 520×650→620×780 để gauge to/rõ hơn) yêu cầu Claude tính lại
  toạ độ `anchoredPosition` cho `MovementTrack`/`CatchProgress` bằng tay. Sau khi tích hợp, owner phát
  hiện khoảng cách giữa 2 gauge bị lệch/mất cân đối trong panel — Claude đã sửa 1 lần nhưng owner
  quyết định không tiếp tục vá mà revert sạch `FishingFeatureAuthoring.cs`, `FishingMinigameUI.cs`,
  `FishingFeature.prefab`, `FishingSpot.River.asset` về đúng git HEAD (đã xác nhận qua `git checkout`,
  không còn sai khác) và xoá toàn bộ asset `_v1`/`_v2`/`_v3` cũ trong
  `Assets/Resources/UI/Fishing/DarkInventoryStyle/`.
- **Bài học giữ lại cho đợt này:** kích thước bitmap phải khớp ĐÚNG kích thước logic đã hardcode sẵn
  trong code gốc — không phóng to panel, không cần Claude tính lại bất kỳ toạ độ nào khi tích hợp.

## Kích thước BẮT BUỘC khớp 1:1 (không được đổi)

Đây là giá trị đang hardcode trong `FishingFeatureAuthoring.cs` (bản gốc, KHÔNG sửa):

- `WaitingPanel`: `500×74`
- `BitePrompt`: `150×150`
- `MinigamePanel`: `520×650`
- `MovementTrack`: `150×490`
- `CatchZone` (baseline, 9-slice, chiều cao đổi runtime): `128×120`
- `CatchProgress` track + fill (dùng với `Slider.fillRect` gốc — **không phải** `Image.Type.Filled`):
  mỗi cái `46×490`
- `ResultPanel`: `620×180`

## Art direction (giữ 2 bài học đã xác nhận đúng từ đợt `_v1`/`_v2`/`_v3` trước)

Theo `DarkLightFantasyUIStyleGuide.md` (D-041): charcoal/walnut nền, viền antique gold mảnh, sapphire
tiết chế. **Đơn giản hơn** bộ `_v2` trước đó (bớt bevel/ornament nhiều lớp, không cần ring dày cầu kỳ)
nhưng bắt buộc giữ đúng 2 điểm đã verify hiệu quả qua Play Mode thật:

1. **`fishing_bite_prompt_v3.png`**: ring quanh TMP "!" phải có **nền bán trong suốt bên trong**
   (~90-94% opaque, không để tâm hoàn toàn trong suốt như đợt `_v1` — lỗi đã xác nhận "ring nổi trôi
   trên world nhìn kì").
2. **`fishing_catch_zone_v3.png`**: màu **tương phản mạnh, khác tông** với `fishing_movement_track_v3`
   (ví dụ track tông xanh dương/tối thì catch zone dùng emerald/amber) — lỗi đã xác nhận ở `_v1`:
   catch zone cùng tông với track nên gần như vô hình khi chơi thật.
3. **`fishing_movement_track_v3.png`**: dù chỉ rộng 150px (hẹp hơn `_v2`), phần thân vẫn phải là
   channel đặc/opaque chiếm phần lớn chiều rộng khung — không phải đường kẻ mảnh có nhiều padding
   trong suốt hai bên như `_v1`.
4. Các asset còn lại (`WaitingPanel`/`MinigamePanel`/`ResultPanel`/Slider track+fill) làm đơn giản,
   gọn, đúng ngôn ngữ charcoal/walnut/gold — không cần đầu tư ornament nặng như `_v2`.

## Asset cần gen

Output: `Assets/Resources/UI/Fishing/DarkInventoryStyle/`

| Tên file | Kích thước px (2x logic, pixel-art thật, nearest 4x upscale) |
| --- | --- |
| `fishing_waiting_panel_v3.png` | 1000×148 |
| `fishing_bite_prompt_v3.png` | 300×300 |
| `fishing_minigame_panel_v3.png` | 1040×1300 |
| `fishing_movement_track_v3.png` | 300×980 |
| `fishing_catch_zone_v3.png` | 256×240 (9-slice border trên/dưới, đề xuất ~32px, ghi rõ số đã dùng) |
| `fishing_slider_track_v3.png` | 92×980 |
| `fishing_slider_fill_v3.png` | 92×980 |
| `fishing_result_panel_v3.png` | 1240×360 |

Tất cả: RGBA, Sprite Mode Single, Filter Point, Mipmap Off, Compression None, PPU 100.
`fishing_waiting_panel_v3`/`fishing_bite_prompt_v3`/`fishing_minigame_panel_v3`/
`fishing_result_panel_v3` bake alpha bán trong suốt ~90-96% (world phải nhìn xuyên qua được).

## Việc Codex KHÔNG cần làm

- Không đổi `FishingFeatureAuthoring.cs`, `FishingMinigameUI.cs`, `FishingMinigameController.cs` —
  file đã revert về đúng bản gốc, Claude chỉ cần thêm dòng load 8 sprite mới, không sửa
  `sizeDelta`/`anchoredPosition` nào.
- Không đụng `FishingSpot.River.asset` (đã revert về đúng 10-fish catalog gốc).
- Không đụng asset Quest/Minimap/MainMenu hay UI đã migrate khác.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`FISHING_UI_V3_LOCKED_SIZE_ART_READY`, liệt kê đúng tên file, border px đã dùng cho
`fishing_catch_zone_v3.png`, và xác nhận kích thước từng file khớp đúng bảng trên.

---

Ngày: 2026-09-27
Feature: Tích hợp `fishing_bite_prompt_v3.png` (`FISHING_BITE_PROMPT_BACKING_ART_READY`) — đã gán
xong, verify Play Mode thật, PASS.

## Prefab/authoring script đã cập nhật

- **`Assets/Editor/FishingFeatureAuthoring.cs`**: 1 dòng — `bitePromptSprite` load từ
  `fishing_bite_prompt_v2.png` → `fishing_bite_prompt_v3.png`. Không đổi gì khác.
- Chạy lại `Tools/Project Game/Fishing/Build And Install MapNhat Fishing` để prefab nhận sprite mới.

## Xác nhận sprite/cấu hình

- `BitePrompt.Image.sprite = fishing_bite_prompt_v3` (verify qua `PrefabUtility.LoadPrefabContents`).
- Kích thước: `RectTransform.sizeDelta = (150, 150)` — **không đổi**, khớp asset 360×360px (giữ đúng
  tỉ lệ như `_v2`).
- `Image.Type = Simple`, `Image.color = RGBA(1,1,1,1)` (trắng hoàn toàn, không nhân thêm alpha).
- Alpha runtime: dùng đúng alpha đã bake trong PNG (backing tối ~92% opaque đo tại tâm ảnh, viền
  ngoài ring vẫn alpha=0 trong suốt hoàn toàn).

## Kết quả Play Mode (Game View 1920×1080, verify qua flow gameplay thật: `TryBeginFishing` → force
`BiteReady` → `ShowBitePrompt`)

1. Chờ cá cắn câu → BitePrompt xuất hiện trên world: **PASS**.
2. Dấu "!" (TMP runtime, không đổi) giờ có nền charcoal tối làm điểm tựa, không còn lơ lửng trực tiếp
   trên world: **PASS**.
3. Phần ngoài ring vẫn trong suốt hoàn toàn, world nhìn xuyên qua bình thường: **PASS**.
4. Ring, bevel, phao (buoy) và gợn nước (ripple) không méo/đổi tỉ lệ so với `_v2`: **PASS**.
5. Console: không lỗi/warning mới liên quan Fishing (chỉ còn cảnh báo "no audio listener" có sẵn từ
   trước, không phải regression).

**Screenshot**: `Assets/Screenshots/screenshot-20260927-221056.png`.

## Sai khác/vấn đề còn tồn tại

Không có. Toàn bộ 8 asset `_v2` + `fishing_bite_prompt_v3` đã tích hợp và verify PASS qua Play Mode
thật. Fishing UI coi như hoàn tất đợt Dark Inventory Style này (D-055).

---

Ngày: 2026-09-27
Feature: Sửa `fishing_bite_prompt_v2.png` — owner xem screenshot Play Mode thật (đính kèm ở entry
`READY_FOR_CODEX_FISHING_UI_V2_PLAYMODE_REVIEW` bên dưới) và nhận xét ring "!" nổi trôi trên world,
không có gì đỡ phía sau, nhìn kì.

## Vấn đề

`fishing_bite_prompt_v2.png` (360×360) hiện chỉ có viền ring dày/bevel, tâm hoàn toàn trong suốt
(alpha=0) — đúng thiết kế gốc `_v1` nhưng giờ ring dày/nổi bật hơn nên việc thiếu mặt phẳng tựa phía
sau lộ rõ, khác hẳn `WaitingPanel`/`MinigamePanel`/`ResultPanel` đều có nền bán trong suốt baked sẵn.

## Yêu cầu sửa

Output: `Assets/Resources/UI/Fishing/DarkInventoryStyle/fishing_bite_prompt_v3.png` — **360×360px**,
giữ nguyên ring dày/bevel/gem đã có ở `_v2` (owner khen phần ring đẹp), chỉ **thêm 1 lớp fill tối bán
trong suốt bên trong ring** (khoảng ~90-94% opaque, cùng tinh thần alpha đã bake ở
`fishing_waiting_panel_v2`/`fishing_minigame_panel_v2`) để dấu "!" (TMP runtime, giữ nguyên) có nền
tựa vào thay vì lơ lửng trực tiếp trên world. Phần NGOÀI ring vẫn giữ trong suốt hoàn toàn (không đổi
hình dạng/kích thước tổng thể của khung).

## Việc Codex KHÔNG cần làm

- Không đổi 7 asset còn lại của bộ `_v2` (đã verify Play Mode PASS, owner không phàn nàn).
- Không sửa `FishingFeatureAuthoring.cs`, `FishingMinigameUI.cs`, `FishingMinigameController.cs`.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`FISHING_BITE_PROMPT_BACKING_ART_READY`.

---

Status: `READY_FOR_CODEX_FISHING_UI_V2_PLAYMODE_REVIEW`

Ngày: 2026-09-27
Feature: Tích hợp 8 asset `_v2` (`FISHING_UI_V2_ART_READY`) vào Fishing minigame UI — đã gán xong,
verify Play Mode thật cả 4 state qua flow gameplay thật (không chỉ set field), PASS phần lớn. Owner
xem screenshot Play Mode thật xong và phát hiện thêm 1 vấn đề mới ở BitePrompt — xem mục cuối.

## Prefab/script đã sửa

- **`Assets/Editor/FishingFeatureAuthoring.cs`**: đổi toàn bộ path `_v1`→`_v2`;
  `MinigamePanel` `520×650`→`620×780`; `MovementTrack` `150×490`→`180×600` @ `(-94,-20)`;
  `CatchZone` baseline `128×120`→`160×140`; xoá hẳn `CreateVerticalSlider` (dùng `Slider` component),
  thay bằng `CreateVerticalProgressFill` — bỏ `Slider`, dựng 2 Image độc lập (background `Type.Simple`
  + fill `Type.Filled`), size `46×490`→`64×600` @ `(120,-20)`.
- **`Assets/Scripts/Fishing/FishingMinigameUI.cs`**: field `_progressSlider` (`Slider`) đổi thành
  `_progressFill` (`Image`); `SetProgress()` đổi từ `_progressSlider.value = ...` sang
  `_progressFill.fillAmount = ...`. Đây là thay đổi tối thiểu bắt buộc vì asset contract yêu cầu
  `Image.Type = Filled` (không tương thích với cơ chế `Slider.fillRect` cũ) — không đổi public API
  nào khác của `FishingMinigameUI`/`FishingMinigameController`, không đổi gameplay/timing.

## Sprite `_v2` đã gán

`WaitingPanel`/`BitePrompt`/`MinigamePanel`/`MovementTrack`/`ResultPanel` → `Image.Type = Simple`,
`color = white`. `CatchZone` → `fishing_catch_zone_v2`, `Image.Type = Sliced`, border đọc từ
`.meta` đã author sẵn (**Left 0, Bottom 40, Right 0, Top 40**, không hardcode lại trong code).
`SliderFill` → `fishing_slider_fill_v2`, `Image.Type = Filled`, `fillMethod = Vertical`,
`fillOrigin = Bottom` (đúng yêu cầu); `SliderTrack` (background) → `fishing_slider_track_v2`,
`Type.Simple`.

## Kết quả Play Mode (Game View 1920×1080, verify qua flow gameplay thật: `TryBeginFishing` →
force `BiteReady` → `BeginMinigame`, không chỉ set field tĩnh; tạm tăng `_remainingTime` qua
reflection để có đủ thời gian chụp — xem ghi chú vận hành cuối)

1. **Waiting**: pill bán trong suốt, world vẫn thấy rõ — không đổi so với `_v1`, vẫn đúng.
2. **Bite prompt**: ring dày/có bevel/gem đúng yêu cầu, rõ ràng hơn hẳn `_v1`. **Screenshot**:
   `Assets/Screenshots/screenshot-20260927-215403.png`.
3. **Active minigame**: `MovementTrack` giờ là channel đặc (đo được ~78% opaque theo chiều rộng bitmap
   giữa hàng ngang, đúng target 70-80%); `CatchZone` màu emerald tương phản mạnh, dễ nhận diện ngay cả
   khi đặt trên track thật (khác hẳn `_v1` gần như vô hình); `SliderFill` dâng đúng từ dưới lên qua
   `fillAmount` thật do `TickMinigame` cập nhật theo tiếp xúc catch-zone/fish, đã chứng kiến 1 lần
   progress tự chạy tới 1.0 và trigger Success qua đúng gameplay logic (không phải set tay) — xác nhận
   `Image.Filled` hoạt động đúng end-to-end. **Screenshot**:
   `Assets/Screenshots/screenshot-20260927-215606.png`.
4. **CatchZone qua nhiều `normalizedSize`**: test tại 0.35 và qua tiến trình tự nhiên của
   `TickMinigame` (dao động runtime) — 2 cap 9-slice (border 40 trên/dưới) không méo ở bất kỳ chiều
   cao nào quan sát được.
5. **Success**: `ShowResult("Success! Caught a Blue Carp (1450g)")` hiện đúng, message rõ. **Screenshot**:
   `Assets/Screenshots/screenshot-20260927-215620.png`.
6. Console: không lỗi/warning mới liên quan Fishing.

## Vấn đề mới owner phát hiện sau khi xem screenshot Play Mode thật (CHƯA sửa, cần thêm 1 vòng)

**BitePrompt không có nền phía sau ring** — ring vàng nổi trôi trên world không có gì đỡ, nhìn "kì"
(nguyên văn owner). `fishing_bite_prompt_v2.png` giữ đúng thiết kế rỗng-giữa của `_v1` (tâm alpha=0,
chỉ có viền ring), nhưng giờ ring dày/nổi bật hơn nên việc thiếu backing fill lộ rõ hơn. Đề xuất: bake
thêm 1 lớp fill tối bán trong suốt (~90-94% opaque, cùng tinh thần `fishing_waiting_panel_v2`) bên
trong ring, cùng file hoặc file `_v3` riêng — để dấu "!" và ring có mặt phẳng tựa vào thay vì lơ lửng
trên world. Sẽ gửi yêu cầu riêng sau khi owner xác nhận hướng sửa.

## Ghi chú vận hành

Minigame thật chỉ có timer ngắn (~8s theo `FishingSpot.River.asset`), không đủ để chụp ảnh/kiểm tra
kỹ qua nhiều lệnh round-trip — đã tạm tăng `_remainingTime` qua reflection trong lúc test (không đụng
asset `FishingSpot.River.asset` thật, chỉ sửa runtime field trong phiên Play Mode, không persist).
Việc này cũng giải thích 1 lần đầu tiên `CatchZone`/`FishIcon` tạm thời không render
(`CanvasRenderer.materialCount = 0`) ngay sau khi ép state qua reflection ngoài vòng lặp `Update()`
bình thường — tự hết sau 1 frame khi có `Canvas.ForceUpdateCanvases()` hoặc Update tự nhiên chạy;
không phải bug ảnh hưởng gameplay thật.

---

Ngày: 2026-09-27
Feature: Gen lại **toàn bộ 8 asset** Fishing UI (`_v2`) sau khi owner review đợt `_v1` qua screenshot
Play Mode thật và đánh giá "thiếu chuyên nghiệp". Đây KHÔNG phải yêu cầu mới — cùng D-055, chỉ là
vòng sửa lỗi thị giác dựa trên bằng chứng thực tế.

## Vấn đề cụ thể owner chỉ ra (kèm 2 screenshot thật)

1. **BitePrompt** (`fishing_bite_prompt_v1`): ring chỉ là 1 đường viền vàng phẳng, mỏng, không có
   bevel/độ dày/đổ bóng như các khung vàng khác trong game (so với `minimap_frame_v1`,
   `dialogue_frame_v4`) — nhìn lạc tông, như placeholder.
2. **MovementTrack** (`fishing_movement_track_v1`): phần thân chỉ là 1 đường kẻ mảnh với 2 đầu mút
   trang trí to bất cân xứng — nhìn như một mũi tên/gậy trang trí, không giống "đường ray" cá bơi
   trong đó. Phần opaque thực tế chỉ chiếm phần nhỏ chiều rộng 300px của bitmap, phần lớn là padding
   trong suốt hai bên.
3. **CatchZone** (`fishing_catch_zone_v1`): dùng cùng tông xanh dương/vàng với chính track → gần như
   vô hình khi chơi thật, người chơi không nhận ra được vùng cần giữ chuột trong đó.
4. **Slider progress**: cùng vấn đề với track — đầu mút vàng trang trí chiếm trọng lượng thị giác lớn
   hơn cả phần fill thực sự đang chạy, khó đọc tiến trình ở cái nhìn thoáng qua.

Kết luận: tổng thể 2 gauge dọc (MovementTrack + Slider) và CatchZone "nhìn đồ chơi", thiếu độ nặng so
với mặt bằng chất lượng đã đạt được ở Minimap/Dialogue/MainMenu boards.

## Yêu cầu sửa cụ thể cho từng asset (bắt buộc đọc trước khi vẽ)

- **`fishing_bite_prompt_v2.png`**: ring dày, có bevel/highlight/shadow nhiều lớp giống ngôn ngữ
  `minimap_frame_v1`/`dialogue_frame_v4` — không phải 1 đường viền đơn sắc phẳng.
- **`fishing_movement_track_v2.png`**: phần THÂN (không tính 2 đầu mút trang trí) phải là 1 channel/
  rail ĐẶC, opaque, chiếm tối thiểu ~70-80% chiều rộng khung hình — đọc được ngay là "đường ray cá
  bơi", không phải đường kẻ mảnh có nhiều padding trong suốt hai bên.
- **`fishing_catch_zone_v2.png`**: màu tương phản MẠNH và khác hẳn tông của track/slider (ví dụ
  vàng/hổ phách rực hoặc xanh lá glow) — phải nổi bật ngay cả khi đặt chồng lên track thật trong game,
  không dùng lại tông xanh dương/vàng giống track.
- **`fishing_slider_track_v2.png`/`fishing_slider_fill_v2.png`**: đầu mút trang trí chỉ chiếm phần
  nhỏ, phần fill/track chính phải là trọng tâm thị giác, đủ dày để đọc tiến trình ở cái nhìn thoáng.
- **`fishing_waiting_panel_v2.png`**/**`fishing_result_panel_v2.png`**: giữ nguyên tinh thần đã ổn ở
  `_v1` (owner không phàn nàn 2 cái này), chỉ vẽ lại cho đồng bộ chất lượng với bộ `_v2`.
- **`fishing_minigame_panel_v2.png`**: kích thước tăng lên 620×780 (từ 520×650) — chừa nhiều không
  gian hơn cho track/slider dày hơn ở giữa.

## Art direction

Theo `DarkLightFantasyUIStyleGuide.md` (D-041): charcoal/walnut nền, viền antique gold mảnh, sapphire
tiết chế. Tham chiếu chất lượng/độ chi tiết của các asset đã duyệt trước đó cùng hệ: `minimap_frame_v1`,
`minimap_tag_v1`, `dialogue_frame_v4`, `landing_action_button_v1` — đều có bevel, highlight/shadow
nhiều lớp, ornament tiết chế nhưng rõ ràng, không phẳng đơn sắc.

## Asset cần gen (thay thế toàn bộ `_v1`)

Output: `Assets/Resources/UI/Fishing/DarkInventoryStyle/`

| Tên file | Kích thước px | Ghi chú |
| --- | --- | --- |
| `fishing_waiting_panel_v2.png` | 1000×148 | Giữ tinh thần `_v1`, vẽ lại đồng bộ chất lượng |
| `fishing_bite_prompt_v2.png` | 360×360 | Ring dày/bevel nhiều lớp |
| `fishing_minigame_panel_v2.png` | 1240×1560 | Panel lớn (logic 620×780) |
| `fishing_movement_track_v2.png` | 360×1200 | Thân channel đặc, opaque tối thiểu 70-80% chiều rộng |
| `fishing_catch_zone_v2.png` | 320×280 | Màu tương phản mạnh, khác tông track/slider; 9-slice border trên/dưới (đề xuất ~40px, ghi rõ số đã dùng) |
| `fishing_slider_track_v2.png` | 128×1200 | Nền progress dọc dày hơn |
| `fishing_slider_fill_v2.png` | 128×1200 | Fill progress dọc, màu đậm/rõ làm trọng tâm |
| `fishing_result_panel_v2.png` | 1240×360 | Giữ tinh thần `_v1` |

Tất cả: pixel-art thật (logic resolution 1/4, nearest-neighbor upscale 4x như `_v1` đã làm đúng),
RGBA, Sprite Mode Single, Filter Point, Mipmap Off, Compression None, PPU 100.
`fishing_waiting_panel_v2`/`fishing_bite_prompt_v2`/`fishing_minigame_panel_v2`/
`fishing_result_panel_v2` tiếp tục bake alpha bán trong suốt ~90-96% như `_v1` (đã đúng, giữ nguyên).

## Việc Codex KHÔNG cần làm

- Không sửa `FishingFeatureAuthoring.cs`, `FishingMinigameUI.cs`, `FishingMinigameController.cs` hay
  bất kỳ logic gameplay/timing/`GameState` nào — chỉ gen bitmap, Claude tự wire lại (đổi path `_v1`→
  `_v2`, chỉnh `sizeDelta` panel theo kích thước mới) và chạy lại
  `Tools/Project Game/Fishing/Build And Install MapNhat Fishing`.
- Không xoá 8 file `_v1` cũ (Claude sẽ dọn sau khi `_v2` verify PASS).
- Không đụng asset Quest/Minimap/MainMenu hay bất kỳ UI đã migrate khác.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`FISHING_UI_V2_ART_READY`, liệt kê đúng tên file, border px đã dùng cho `fishing_catch_zone_v2.png`,
và mô tả ngắn cách đã khắc phục từng vấn đề nêu trên (đặc biệt: tỉ lệ opaque của track, độ tương phản
màu catch zone so với track).

---

Ngày: 2026-09-27
Feature: Tích hợp 8 asset pixel-art Dark Inventory Style cho Fishing minigame UI
(`FISHING_UI_ART_READY`, D-055) — đã gán xong, verify Play Mode thật cả 4 state, PASS.

## Thay đổi (`Assets/Editor/FishingFeatureAuthoring.cs`)

- Thêm `UiFolder` const + `LoadSprite()` helper, load 8 sprite ở đầu `CreateFeaturePrefab()`.
- Thêm helper `CreateSpritePanel(...)` thay cho `CreatePanel(...)` cũ (Image màu phẳng) — xoá hẳn
  `CreatePanel` vì không còn nơi nào dùng.
- Gán sprite: `WaitingPanel`→`fishing_waiting_panel_v1`, `BitePrompt`→`fishing_bite_prompt_v1`,
  `MinigamePanel`→`fishing_minigame_panel_v1`, `MovementTrack`→`fishing_movement_track_v1`,
  `ResultPanel`→`fishing_result_panel_v1`, tất cả `Image.Type = Simple`, `color = Color.white`.
- `CatchZone` → `fishing_catch_zone_v1`, `Image.Type = Sliced` (border 0/32/0/32 đã author sẵn trong
  `.meta`, không hardcode lại trong code — đúng pattern đã dùng cho `minimap_tag_v1`).
- `CreateVerticalSlider` nhận thêm 2 tham số `Sprite trackSprite, fillSprite`, gán
  `fishing_slider_track_v1`/`fishing_slider_fill_v1`, giữ nguyên `Slider.fillRect`/`Direction.BottomToTop`.
- Không đổi `FishingMinigameUI.cs`, `FishingMinigameController.cs`, timing, `GameState` policy.

## Kết quả Play Mode (Game View 1920×1080)

Verify bằng reflection gọi thẳng `FishingMinigameController.TryBeginFishing/BeginMinigame` và
`FishingMinigameUI.ShowResult` (bỏ qua chờ ngẫu nhiên/click chuột thật), tạm tăng `_remainingTime`
qua reflection để có đủ thời gian chụp/kiểm tra kỹ (minigame gốc chỉ có ~8s, hết giờ giữa lúc thao
tác làm vài lần test đầu tiên bị timeout — không phải bug, chỉ là giới hạn thời gian test).

- **WaitingPanel**: pill bán trong suốt hiện đúng, world vẫn thấy rõ phía sau (đúng D-035).
- **BitePrompt**: ring vàng hiện đúng quanh TMP "!", căn giữa.
- **MinigamePanel**: frame lớn, timer, `MovementTrack` (rail mảnh trang trí) và slider dọc (nền +
  fill xanh) render đúng lớp, không bị méo.
- **CatchZone** (`Sliced`, border 32 trên/dưới): test bằng cách tô tạm màu debug (magenta) để xác
  nhận render đúng vị trí/kích thước tại nhiều `normalizedSize`, 2 cap không méo; đã trả lại
  `Color.white` sau test.
- **FishIcon**: đọc đúng `FishDefinitionSO.icon` (`Fish1_7`), test tương tự bằng màu debug (đỏ) rồi
  trả lại trắng — có lúc `CanvasRenderer.materialCount = 0` (không render) ngay sau khi ép state qua
  reflection do canvas chưa kịp rebuild 1 frame; gọi `Canvas.ForceUpdateCanvases()` hoặc để 1 frame
  Update tự nhiên trôi qua là khỏi — xác nhận đây là artifact của cách test (reflection ép state
  ngoài vòng lặp Update bình thường), không phải bug ảnh hưởng gameplay thật (người chơi luôn đi qua
  `Update()` mỗi frame nên không bao giờ gặp).
- **ResultPanel**: hiện đúng message "Success! Caught a River Minnow (620g)", đọc rõ.
- Console: không lỗi mới liên quan Fishing. Có gặp lại `PlayerLoop called recursively` (lỗi engine
  từng ghi nhận ở đợt Minimap) do gọi `manage_camera screenshot` dồn dập trong Play Mode để test 4
  state liên tiếp — không liên quan code/asset Fishing, Editor vẫn phản hồi bình thường sau khi dừng
  Play Mode, không cần restart.

## Ghi chú thiết kế (không phải bug, chỉ để lưu ý)

`fishing_catch_zone_v1` dùng palette xanh dương/viền vàng khá giống với 2 đầu mút trang trí của
`fishing_movement_track_v1` — ở màu thật (không debug tint) catch zone hơi khó phân biệt với track
bằng mắt thường trong ảnh chụp nén. Không tự ý đổi vì đây là quyết định art của Codex và không nằm
trong yêu cầu tích hợp; nêu ra để owner/Codex cân nhắc nếu cảm thấy cần tăng tương phản ở đợt sau.

---

Ngày: 2026-09-27
Feature: Gen asset Dark Inventory Style cho toàn bộ Fishing minigame UI (D-055) — đây là lượt gen đầu
tiên, KHÔNG phải reskin: `FishingFeatureAuthoring.cs` hiện dựng UI 100% bằng `Image` màu phẳng, chưa
từng có bitmap nào.

## Bối cảnh

- Fishing minigame có 4 state hiển thị lần lượt (không bao giờ chồng nhau), điều khiển bởi
  `FishingMinigameUI.cs`: `WaitingPanel` → `BitePrompt` → `MinigamePanel` → `ResultPanel`.
- Toàn bộ UI nằm trong `Assets/Prefabs/Fishing/FishingFeature.prefab`, dựng bởi
  `Assets/Editor/FishingFeatureAuthoring.cs` (`CreateFeaturePrefab()`), Canvas reference resolution
  1920×1080.
- `WaitingPanel` hiện trong lúc world vẫn chạy (D-035: "Waiting khóa gameplay input nhưng world vẫn
  chạy") — bắt buộc bán trong suốt để không che mất world.
- `CatchZone` (trong `MinigamePanel/MovementTrack`) đổi CHIỀU CAO liên tục mỗi frame theo
  `normalizedSize` (độ khó hook), chiều rộng cố định — cần art 9-slice-safe (có cap trên/dưới) để
  không bị méo khi runtime resize.
- Slider progress là thanh dọc (`Slider.Direction.BottomToTop`), fill co theo `fillRect` — chỉ cần
  2 bitmap tĩnh (track nền + fill), không cần animation.

## Art direction

- Theo `Assets/Documentation/DevelopmentPlan/DarkLightFantasyUIStyleGuide.md` (D-041): charcoal/walnut
  nền, viền antique gold mảnh, sapphire chỉ là accent nhỏ. Đồng bộ với QuestTracker/Minimap/Dialogue
  (cũng là HUD overlay trong lúc world hiển thị, không phải popup modal che toàn màn hình).
- `WaitingPanel`/`BitePrompt`/`MinigamePanel`/`ResultPanel` cần bake sẵn alpha bán trong suốt trong
  chính file PNG (khoảng 90-96% opaque, tương đương giá trị runtime hiện tại `0.92-0.96`) — không
  phải Claude chỉnh alpha qua code.
- Không bake text vào bất kỳ bitmap nào: "Waiting for a bite...", "!", số giây đếm ngược, message kết
  quả (Success/Fail) đều là TMP runtime thật, giữ nguyên.
- `FishIcon` dùng `FishDefinitionSO.icon` của từng loại cá (đã có sẵn per-fish, không cần gen icon
  chung ở đây).

## Asset cần gen

Output: `Assets/Resources/UI/Fishing/DarkInventoryStyle/`

| Tên file | Kích thước px | Vai trò |
| --- | --- | --- |
| `fishing_waiting_panel_v1.png` | 1000×148 | Nền pill cho dòng chữ "Waiting for a bite..." |
| `fishing_bite_prompt_v1.png` | 300×300 | Khung tròn quanh dấu "!" khi cá cắn câu |
| `fishing_minigame_panel_v1.png` | 1040×1300 | Panel lớn chứa toàn bộ minigame (timer, rail, slider, fish icon) |
| `fishing_movement_track_v1.png` | 300×980 | Rail dọc cố định, cá và catch zone di chuyển bên trong |
| `fishing_catch_zone_v1.png` | 256×240 | Vùng bắt cá nổi bật trên rail — **bắt buộc 9-slice border trên/dưới** vì chiều cao đổi liên tục runtime, chiều rộng cố định |
| `fishing_slider_track_v1.png` | 92×980 | Nền thanh progress dọc |
| `fishing_slider_fill_v1.png` | 92×980 | Fill thanh progress dọc (gold/sapphire glow) |
| `fishing_result_panel_v1.png` | 1240×360 | Panel hiện message Success/Fail |

Tất cả: RGBA, Sprite Mode Single, Filter Point, Mipmap Off, Compression None, PPU 100. Riêng
`fishing_catch_zone_v1.png` cần thêm border 9-slice hợp lý (ví dụ 24-32px trên/dưới) — ghi rõ số
pixel border đã dùng trong report để Claude gán đúng `Sprite.border`.

## Việc Codex KHÔNG cần làm

- Không sửa `FishingFeatureAuthoring.cs`, `FishingMinigameUI.cs`, `FishingMinigameController.cs`,
  `FishingSpotDefinition`, `FishDefinitionSO` hay bất kỳ logic gameplay/timing/`GameState` nào — chỉ
  gen bitmap, Claude tự wire vào authoring script và chạy lại
  `Tools/Project Game/Fishing/Build And Install MapNhat Fishing`.
- Không gen icon cá (đã có sẵn per-`FishDefinitionSO`).
- Không đụng asset Quest/Minimap/MainMenu hay bất kỳ UI đã migrate trước đó.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`FISHING_UI_ART_READY`, liệt kê đúng tên file đã tạo, và border pixel đã dùng cho
`fishing_catch_zone_v1.png` nếu khác 24-32px đề xuất.

---

Ngày: 2026-09-27
Feature: Tích hợp bản revised của Quest Accept Popup (board `_v3` + 2 button sprite riêng theo D-038
cập nhật 2026-09-27, xem yêu cầu gốc ở entry `READY_FOR_CODEX_QUEST_ACCEPT_POPUP` bên dưới) — đã gán
xong, verify Play Mode thật, PASS.

## Thay đổi

- `Assets/Editor/QuestAcceptPopupPrefabBuilder.cs`: `BoardPath` trỏ sang
  `quest_accept_board_dynamic_rewards_v3.png`; thêm `AcceptButtonPath`/`DeclineButtonPath` trỏ
  `quest_accept_button_accept_v1.png`/`quest_accept_button_decline_v1.png`. `ConfigureButton` nhận
  thêm tham số `Sprite`, gán `image.sprite`, `preserveAspect = true`, `color = Color.white` (bỏ hoàn
  toàn `color = (1,1,1,0.01)` của kiến trúc bake-vào-board cũ). Cả 2 nút đặt `width=120, height=40`
  (đúng tỉ lệ 3:1 của bitmap 2172×724, không méo hình) — hàng nút `ButtonRow` tăng từ cao 34→40 để
  vừa khít, tổng chiều rộng giữ nguyên 252 (120×2 + spacing 12).
- Đã chạy `Tools/ProjectGame2D/UI/Rebuild Quest Accept Popup 1920` và `PrefabUtility.SaveAsPrefabAsset`.

## Kết quả Play Mode (Game View 1920×1080, verify bằng cách gọi thẳng
`QuestAcceptPopupUI.Instance.Open(quest, callback)` qua code cho quest thật `Slimes at the Eastern
Field`, không cần đứng cạnh NPC)

- Board, 2 divider OBJECTIVES/REWARDS, category icon, objective icon render đúng Dark Inventory Style;
  không còn hình nút bake trong board.
- `ACCEPT` (sapphire) và `DECLINE` (charcoal trung tính) là 2 Image độc lập, label TMP nằm giữa, không
  biến dạng/che khuất, có thể chỉnh RectTransform riêng trong Prefab Mode như yêu cầu.
- Bấm Accept → callback `Action<bool>` nhận đúng `true`, popup đóng đúng qua `Close()`.
- Console: không exception/warning mới.
- Không đụng icon `Tracker1920`/`inventory_slot_hd.png`; không đổi `QuestAcceptPopupUI.cs`,
  `QuestManager`, callback, quest progression hay save contract.

---

Ngày: 2026-09-27
Feature: Tích hợp `session_confirmation_board_v1` vào dialog "Abandon Quest?" trong `QuestLogUI`
(`ABANDON_QUEST_CONFIRMATION_ART_READY`) + rewire 2 nút sang asset Dark Inventory Style có sẵn — đã
gán xong, verify Play Mode thật, PASS.

## Asset đã gán (`Assets/Prefabs/UI/GameplayUIRoot.prefab`, GameObject `AbandonQuestConfirmation`)

- `Dialog` (Image) ← `session_confirmation_board_v1` (1672×941, cùng kích thước file cũ nên
  RectTransform `Dialog` giữ nguyên `500×230`, không cần chỉnh).
- `ConfirmAbandonButton` (Image) ← `slot_delete_button_v1.png` (đã có từ đợt MainMenu).
- `CancelAbandonButton` (Image) ← `landing_action_button_v1.png` (đã có từ đợt MainMenu).
- Hai nút vốn đã là Image/Button độc lập với `Label` TMP là child riêng (không có bug share
  `CanvasRenderer` như case Slot badge trước đó); RectTransform 2 nút vốn đã cùng kích thước 165×38
  và đối xứng ±92 quanh tâm — không cần chỉnh layout.

## Bug phát hiện và fix khi tích hợp

`_abandonConfirmationMessage` (TMP `Message`) đang có màu nâu sẫm `RGBA(0.216, 0.122, 0.063)` —
đúng cho board parchment sáng màu cũ nhưng gần như không đọc được trên board charcoal mới (chữ tối
trên nền tối). Đổi sang màu Cream `RGBA(0.96, 0.91, 0.76, 1)` — cùng hằng số `Cream` đã dùng trong
`QuestLogWindowPrefabBuilder.cs`/`QuestAcceptPopupPrefabBuilder.cs` để đồng bộ toàn bộ text Quest
UI. Áp trực tiếp vào field TMP trong prefab (không đổi script/binding).

**Fix bổ sung (owner phát hiện qua screenshot sau khi verify lần đầu):** Label TMP `ABANDON` và
`CANCEL` trên 2 nút cũng bị sót cùng lỗi màu nâu sẫm `RGBA(0.216, 0.122, 0.063)` y hệt message — chữ
gần như vô hình trên nền nút đỏ/charcoal mới. Đổi cả 2 sang trắng `RGBA(1,1,1,1)`, verify lại Play
Mode với quest `Meet the Trainer`, chữ đọc rõ trên cả 2 nút, console sạch.

## Kết quả Play Mode (Game View 1920×1080, verify qua reflection gọi thẳng
`QuestLogUI.OpenQuestLog/SelectQuest/OpenAbandonConfirmation/ConfirmAbandonQuest` và
`Button.onClick.Invoke()` để test không cần chuột)

- Test với quest `Meet the Trainer` (đúng quest owner chụp màn hình gốc) và `Training Dummy
  Challenge`: dialog mới hiện đúng board Dark Inventory Style, message 3 dòng đọc rõ, không tràn
  viền/đè lên 2 nút.
- Confirm (`ABANDON`, đỏ danger) → gọi đúng `QuestManager.TryAbandonQuest`, quest bị bỏ, dialog tự
  đóng qua `HandleQuestAbandoned` → `CloseAbandonConfirmation` (không đổi code, hành vi cũ vẫn vậy).
- Cancel (`CANCEL`, charcoal/sapphire) → đóng dialog, quest giữ nguyên, không có side effect.
- Console: không exception/warning mới (chỉ còn cảnh báo "no audio listener" có sẵn từ trước, không
  liên quan).
- Không đụng asset Quest Log/Tracker/Minimap khác; không sửa `QuestLogUI.cs`, `QuestManager`,
  callback hay save contract.

---

Ngày: 2026-09-27
Feature: Gen lại asset Dark Inventory Style cho dialog "Abandon Quest?" trong Quest Log (owner chụp
màn hình thấy popup này còn palette parchment/gold-leaf cũ khi bấm Abandon trong `QuestLogUI`).

## Bối cảnh

- Popup này là GameObject `AbandonQuestConfirmation` hand-authored trực tiếp trong
  `Assets/Prefabs/UI/GameplayUIRoot.prefab` (không qua authoring script nào), field
  `QuestLogUI._abandonConfirmationRoot`. Cấu trúc: `Dialog` (Image board) → `Message` (TMP) +
  `ConfirmAbandonButton`/`CancelAbandonButton` (Image + TMP `Label` con).
- Board hiện dùng `Assets/Resources/UI/SessionUX/LightFantasy/session_confirmation_board_hd.png`
  (1672×941) — asset này **chỉ dùng đúng một chỗ này**, không dùng ở đâu khác trong project, nên
  regen không ảnh hưởng UI nào khác.
- Hai nút `ConfirmAbandonButton`/`CancelAbandonButton` hiện đang trỏ tạm vào 2 sprite MainMenu cũ
  (`slot_delete_button.png`, `landing_action_button.png`) — **không cần Codex gen gì cho 2 nút này**,
  Claude sẽ tự rewire sang `slot_delete_button_v1.png`/`landing_action_button_v1.png` đã có sẵn từ đợt
  MainMenu Dark Inventory Style trước, chỉ cần gen lại đúng cái board.

## Art direction

- Theo `DarkLightFantasyUIStyleGuide.md` (D-041, style gameplay UI hiện tại — cùng ngôn ngữ đã dùng
  cho Character Popup/Tutorial/Quest Log/Minimap). Charcoal/walnut tối, viền antique gold mảnh,
  sapphire tiết chế; không dùng lại palette parchment/gold-leaf cũ.
- Đây là dialog xác nhận 2 lựa chọn (Confirm/Cancel) đứng riêng, không phải panel lớn — giữ ornament
  tiết chế, chừa khoảng trống giữa cho message TMP 2-3 dòng và khoảng trống đáy cho 2 nút.
- Không bake text vào bitmap (message "Abandon {quest}? Progress will be lost..." và label 2 nút đều
  là TMP runtime, giữ nguyên).

## Asset cần gen

Output: `Assets/Resources/UI/SessionUX/DarkInventoryStyle/session_confirmation_board_v1.png`

- Kích thước: **1672×941px** (khớp 1:1 file cũ).
- RGBA, Sprite Mode Single, Filter Point, Mipmap Off, Compression None, PPU 100, `Border = 0,0,0,0`,
  `Image.Type = Simple`.

## Việc Codex KHÔNG cần làm

- Không gen sprite nút — 2 nút Confirm/Cancel đã có `_v1` từ đợt MainMenu, Claude tự rewire.
- Không sửa `QuestLogUI.cs`, `QuestManager`, `GameplayUIRoot.prefab` hierarchy/callback — Claude tự
  gán sprite mới vào field `Dialog.Image.sprite` sau khi có asset.
- Không đụng asset Quest Log/Tracker/Minimap khác đã chốt trước đó.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`ABANDON_QUEST_CONFIRMATION_ART_READY`, nêu tên file đã tạo và mọi sai khác kích thước/border nếu có
lý do kỹ thuật.

---

Status: `READY_FOR_CODEX_QUEST_ACCEPT_POPUP`

Ngày: 2026-09-27
Feature: Gen lại asset Dark Inventory Style cho `QuestAcceptPopupUI` (popup "QUEST OFFER" hiện lên sau
khi NPC mời quest — ảnh owner gửi kèm "Training Dummy Challenge" đang còn palette gỗ sồi/xanh lá cũ,
lệch với gameplay UI đã theo D-041/Dark Inventory Style).

## Bối cảnh

- Popup này dựng từ prefab `Assets/Prefabs/UI/QuestAcceptPopup.prefab`, authoring script
  `Assets/Editor/QuestAcceptPopupPrefabBuilder.cs`. Toàn bộ khung ngoài (viền lá/gold ở góc, banner
  "QUEST OFFER", 2 đường kẻ OBJECTIVES/REWARDS, và **cả hình khối 2 nút Accept/Decline**) đều được bake
  chung vào **một** bitmap nền `quest_accept_board_dynamic_rewards_v2.png`; 2 `Button` Accept/Decline
  chỉ là vùng bấm trong suốt (`color = (1,1,1,0.01)`) đặt đè lên đúng vị trí hình nút đã vẽ sẵn trong
  board — không có sprite nút riêng. Giữ nguyên đúng kiến trúc này khi regen, không tách nút ra thành
  sprite riêng.
- Icon category quest (`category_side/main/daily`) và icon objective (`objective_kill/collect/talk/location`)
  lấy từ `Assets/Resources/UI/Quest/Tracker1920/` — **đây là asset QuestLog/QuestTracker owner đã xác
  nhận giữ nguyên, không cần gen lại**. Ô reward dùng khung `inventory_slot_hd.png` của hệ Inventory
  (chưa tới lượt migrate) — **cũng không đụng tới**. Đợt này chỉ gen lại đúng 1 file board.

## Art direction

- Theo `Assets/Documentation/DevelopmentPlan/DarkLightFantasyUIStyleGuide.md` (D-041, đang áp dụng cho
  toàn bộ gameplay UI: Character Popup, Quest Log, Tutorial...). Charcoal/walnut tối làm nền, viền
  antique gold mảnh, sapphire chỉ là accent nhỏ (ví dụ dải "OBJECTIVES"/"REWARDS" hoặc viền nút Accept).
  Ornament lá/hoa văn hiện tại nếu giữ thì tiết chế lại theo tinh thần "nội dung nổi hơn khung", không
  bắt buộc giữ nguyên ornament lá cũ.
- Nút Accept: hình dáng sapphire/dark-blue (giống primary button của gameplay UI). Nút Decline: cùng
  hình học/kích thước với Accept nhưng palette trung tính/tối hơn (không cần đỏ cảnh báo vì đây là từ
  chối nhận quest, không phải hành động phá hủy dữ liệu).
- Không bake bất kỳ text nào vào bitmap (tiêu đề quest, mô tả objective, "0/3", "10 Gold  20 XP",
  label "ACCEPT"/"DECLINE") — toàn bộ đã là TMP runtime thật, giữ nguyên.
- Layout logic (vị trí icon, khung objective, khung reward, khoảng cách) do `QuestAcceptPopupPrefabBuilder.cs`
  set cứng bằng tọa độ theo virtual canvas 320×400 (CanvasScaler ScaleWithScreenSize) — board mới phải
  giữ đúng vùng trống cho: banner tiêu đề ở trên, icon category góc trái header, khối OBJECTIVES giữa,
  khối REWARDS dưới, hàng 2 nút ở đáy — không dịch chuyển các vùng này.

## Asset cần gen

Output: `Assets/Resources/UI/Quest/QuestAccept1920/quest_accept_board_dynamic_rewards_v3.png`

- Kích thước: **1122×1402px** (khớp 1:1 file `_v2` hiện tại).
- RGBA, Sprite Mode Single, Filter Point, Mipmap Off, Compression None, PPU 100, `Border = 0,0,0,0`
  (không sliced, `Image.Type = Simple`, `preserveAspect = true`).
- Bake sẵn trong art: khung ngoài + banner tiêu đề (chừa khoảng trắng cho TMP "QUEST OFFER" đè lên,
  hiện tại dùng TMP thật chứ không phải bake — banner chỉ cần là khung rỗng), 2 đường kẻ phân cách
  OBJECTIVES/REWARDS, và 2 hình khối nút Accept (sapphire)/Decline (trung tính) ở đúng vị trí đáy card.

## Việc Codex KHÔNG cần làm

- Không đụng `category_*`/`objective_*` trong `UI/Quest/Tracker1920/` — asset QuestLog đã chốt, không
  gen lại.
- Không đụng `inventory_slot_hd.png` (Inventory chưa tới lượt migrate).
- Không sửa `QuestAcceptPopupUI.cs`, `QuestAcceptPopupPrefabBuilder.cs`, `QuestManager`, callback
  Accept/Decline hay bất kỳ logic C# nào — chỉ gen bitmap, Claude sẽ tự chỉnh authoring script để trỏ
  sang file `_v3` và chạy lại `Tools/ProjectGame2D/UI/Rebuild Quest Accept Popup 1920`.
- Không sửa/ghi đè các thay đổi khác đang có trong worktree.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`QUEST_ACCEPT_POPUP_ART_READY`, nêu rõ tên file đã tạo và mọi sai khác kích thước/border nếu có lý do
kỹ thuật cần đổi.

---

Status: `VERIFIED_MAINMENU_SETTINGS_SLOT_CONFIRM_INTEGRATION`

Ngày: 2026-09-27
Feature: Tích hợp 12 asset Dark Inventory Style (`MAINMENU_SETTINGS_SLOT_CONFIRM_ART_READY` trong
CodexToClaude.md) vào SettingsPage, SlotPage (New Game + Continue), ConfirmOverlay/ErrorOverlay
trong `MainMenu.unity` — đã gán xong, verify Play Mode thật, PASS toàn bộ.

## Kết quả tích hợp

**Rewire miễn phí trước khi gán asset mới** (không cần Codex gen lại): 7 nút đang tái dùng
`landing_action_button`/`landing_action_button_hover` cũ đổi sang `_v1` đã có sẵn — `ConfirmButton`,
`SaveButton`, 3× `PrimaryButton` (Slot1/2/3), `CloseButton` (ErrorOverlay), `BackButton`.

**Field đã gán 12 sprite mới** (trực tiếp trong scene `MainMenu.unity`, `Image.Type = Simple`,
đúng RectTransform/anchor hiện có):
- `SettingsPage/Content` ← `settings_board_v1`; `SettingsPage/Title` ← `settings_title_v1`.
- `FullScreenToggle/Background` ← `settings_checkbox_unchecked_v1`; `.../Checkmark` ←
  `settings_checkbox_checked_v1`.
- `MusicSlider` và `SfxSlider` Background ← `settings_slider_track_v1`; Handle ←
  `settings_slider_handle_v1` (dùng chung 1 cặp track/handle cho cả hai slider).
- `SlotsRow/Slot1|2|3` (card nền) ← `save_slot_card_v1`; `.../DeleteButton` ← `slot_delete_button_v1`.
- `SlotHeader/SlotPageTitle` ← `slot_page_new_game_title_v1` mặc định; `MainMenuSaveSlotsUI`
  `_newGameTitleSprite`/`_continueTitleSprite` (SerializedObject) trỏ đúng `slot_page_new_game_title_v1`
  / `slot_page_continue_title_v1` để script tự đổi theo mode lúc runtime (không đổi logic
  `MainMenuSaveSlotsUI.cs`).
- `ConfirmOverlay/Dialog` và `ErrorOverlay/Dialog` ← `overlay_dialog_board_v1`;
  `ConfirmOverlay/.../CancelButton` ← `slot_delete_button_v1` (Confirm dùng `landing_action_button_v1`
  đã rewire ở trên).

**Bug phát hiện và fix khi tích hợp `slot_badge_v1`** (không có trong asset cũ vì lý do khác):
`SlotsRow/Slot{1,2,3}/Title` trước đây có cả `Image` (sprite `slot_badge_1/2/3` bake sẵn chữ "SLOT n")
và `TextMeshProUGUI` (text "SLOT n") **trên cùng một GameObject**, với TMP bị disable từ trước —
tức số slot vốn được bake vào bitmap cũ, TMP chỉ là phần thừa không dùng. Asset mới `slot_badge_v1`
không bake chữ (đúng theo style guide), nên bật TMP lên thì lộ ra vấn đề kiến trúc: `Image` và
`TextMeshProUGUI` trên cùng GameObject dùng chung một `CanvasRenderer` (component bắt buộc của cả
hai), nên chỉ một trong hai render được — bật TMP làm badge nền biến mất/chớp tắt tuỳ frame.
Fix: tách `Title` thành container rỗng chứa 2 con riêng — `Badge` (Image, sibling đầu, nền) và
`Label` (TextMeshProUGUI, sibling cuối, hiển thị "SLOT 1/2/3") — đúng pattern đã dùng sẵn ở
`DeleteButton/Label`, `PrimaryButton/Label`. Áp dụng cho cả Slot1/Slot2/Slot3.

## Kết quả Play Mode (Game View 1920×1080, verify qua `manage_camera screenshot` + gọi trực tiếp
`Button.onClick.Invoke()` để test navigation không cần chuột)

- SettingsPage: board/title/checkbox/2 slider render đúng, không chữ bị che; Cancel đưa về Landing
  đúng.
- New Game SlotPage: title "NEW GAME" đúng; 3 card `save_slot_card_v1`; badge "SLOT 1/2/3" đọc rõ sau
  fix Badge/Label; Overwrite/Delete đúng style (olive/danger).
- Continue SlotPage: title "CONTINUE" đúng; 3 slot Load/Delete đúng style; không regression.
- ConfirmOverlay: bấm Delete → dialog `overlay_dialog_board_v1` hiện đúng, message rõ, Confirm/Cancel
  đúng màu; bấm Cancel → `ConfirmOverlay.activeInHierarchy` về `false`, dismiss đúng.
- Console: không exception/warning mới ngoài cảnh báo codec `Color primaries 0` cũ (không liên quan,
  đã biết từ trước, không phải regression của lượt này).
- Không đổi `mainmenu_background.mp4`/`mainmenu_new_journey_dawn_v8.png`; không đổi
  `MainMenuSaveSlotsUI.cs`/`SettingsService`/navigation/callback ngoài việc gán 2 field sprite nêu
  trên.

## Ghi chú vận hành

Trong lúc verify có xuất hiện `PlayerLoop called recursively` (lỗi engine từng gặp ở đợt Minimap,
xem entry `VERIFIED_MAP_ART_INTEGRATION` phía dưới) sau một chuỗi gọi screenshot liên tục trong Play
Mode — không liên quan đến asset/script đợt này. Xử lý bằng cách dừng Play Mode, áp lại thay đổi cấu
trúc Badge/Label ở Edit Mode (an toàn, không mất do Play Mode không lưu), rồi vào lại Play Mode verify
lần hai và PASS sạch, không còn lỗi.

---

Ngày: 2026-09-27
Feature: Gen asset Dark Inventory Style cho 3 nhóm còn lại trong `MainMenu.unity` (theo D-054):
SettingsPage, SlotPage (New Game + Continue), ConfirmOverlay/ErrorOverlay. Landing group đã xong và
verify PASS trước đó (xem entry `VERIFIED_MAP_ART_INTEGRATION` phía dưới cho ví dụ định dạng report).

## Bối cảnh

- Owner xác nhận Landing group (logo, slogan, board, action button) đã lên Dark Inventory Style và
  chạy tốt trong Play Mode. Các màn còn lại (Settings, New Game, Continue, Confirm Popup, Overwrite
  Confirm, Quit Confirm) vẫn đang render bằng asset cũ `LightFantasy` (nền xanh/vàng ấm), lệch style
  với phần đã đổi.
- Claude đã tự rewire xong 7 nút đang tái dùng `landing_action_button`/`landing_action_button_hover`
  sang bản `_v1` đã có sẵn (ConfirmButton, SaveButton, 3× PrimaryButton, CloseButton, BackButton) —
  **không cần Codex gen lại các nút này**, chỉ còn các asset liệt kê dưới đây là thực sự thiếu.
- Toàn bộ asset cũ đang ở `Assets/Resources/UI/MainMenu/LightFantasy/`, import Single/Point/no
  mipmap/uncompressed, `Sprite Border = 0,0,0,0`, `Image.Type = Simple` (không sliced) trên tất cả
  object dùng chúng trong scene — asset mới giữ đúng cùng cấu hình import và cùng kích thước pixel
  1:1 để không phải sửa RectTransform/anchor.

## Art direction (bắt buộc đọc trước khi vẽ)

- Theo `Assets/Documentation/DevelopmentPlan/DarkLightFantasyUIStyleGuide.md` + quyết định D-054
  (MainMenu chuyển hẳn sang Dark Inventory Style, không còn giữ "Light Fantasy bình minh").
- Charcoal/walnut tối làm nền chính, viền antique gold mảnh, sapphire chỉ là accent nhỏ (title/góc),
  không rải gem dày. Ornament tiết chế, nội dung (text/icon) phải nổi hơn khung.
- Không bake text động vào sprite. Toàn bộ label, số slot, message hiện đang là TMP object thật —
  giữ nguyên; asset mới chỉ là khung/nền/nút, không có chữ.
- Giữ nguyên tuyệt đối `mainmenu_background.mp4` và `mainmenu_new_journey_dawn_v8.png` — nhóm này
  không liên quan đến 2 file đó.

## Asset cần gen — Nhóm 1: SettingsPage

Output: `Assets/Resources/UI/MainMenu/DarkInventoryStyle/`

| Tên file mới | Kích thước px (khớp asset cũ) | Vai trò |
| --- | --- | --- |
| `settings_board_v1.png` | 1122×1402 | Panel nền của SettingsPage (khung charcoal/walnut, viền gold mảnh) |
| `settings_title_v1.png` | 2048×768 | Wordmark "SETTINGS" |
| `settings_checkbox_unchecked_v1.png` | 1254×1254 | Checkbox Full Screen — trạng thái off |
| `settings_checkbox_checked_v1.png` | 1254×1254 | Checkbox Full Screen — trạng thái on (có dấu check sapphire/gold) |
| `settings_slider_track_v1.png` | 2048×226 | Track cho SFX/Music slider |
| `settings_slider_handle_v1.png` | 1254×1254 | Handle tròn cho slider (gold rim) |

## Asset cần gen — Nhóm 2: SlotPage (dùng chung cho New Game và Continue)

| Tên file mới | Kích thước px | Vai trò |
| --- | --- | --- |
| `save_slot_card_v1.png` | 1086×1448 | Nền 1 card slot (dùng chung cho cả 3 slot, chỉ khác text/badge đè lên) |
| `slot_badge_v1.png` | 1983×793 | Nền tag "SLOT n" phía trên card — **1 sprite dùng chung cho cả 3 slot** (số thứ tự đã là TMP text riêng đè lên, không cần 3 bản khác nhau như asset cũ `slot_badge_1/2/3`) |
| `slot_delete_button_v1.png` | 1944×809 | Nút Delete — danger button muted-red theo style guide, cùng hình học/chiều cao với `landing_action_button_v1` để không méo khi Unity stretch |
| `slot_page_new_game_title_v1.png` | 2048×683 | Wordmark tiêu đề trang khi ở mode New Game |
| `slot_page_continue_title_v1.png` | 2048×744 | Wordmark tiêu đề trang khi ở mode Continue |

## Asset cần gen — Nhóm 3: ConfirmOverlay / ErrorOverlay

| Tên file mới | Kích thước px | Vai trò |
| --- | --- | --- |
| `overlay_dialog_board_v1.png` | 900×600 | Nền dialog dùng chung cho Confirm Popup, Overwrite Confirm, Quit Confirm, và Error Overlay |

Lưu ý: nút Cancel/Confirm trong dialog này dùng chung `slot_delete_button_v1` (Cancel/danger) và
`landing_action_button_v1` đã có (Confirm) — không cần asset nút riêng cho overlay này.

## Việc Codex KHÔNG cần làm

- Không cần gen lại nút nào đã có `_v1` (landing_action_button, landing_action_button_hover) — đã
  rewire xong bằng code, không liên quan đến đợt gen này.
- Không đổi `mainmenu_background.mp4`, `mainmenu_new_journey_dawn_v8.png`, logo/slogan/board Landing.
- Không sửa `MainMenuSaveSlotsUI.cs`, `SettingsService`, navigation, callback hay bất kỳ logic C#
  nào — chỉ gen bitmap PNG + chuẩn hoá import (Sprite Mode Single, Filter Point, Mipmap Off,
  Compression None, alpha đúng), Claude sẽ tự gán vào scene.
- Không sửa/ghi đè các thay đổi khác đang có trong worktree.

## Báo lại khi xong

Ghi entry mới ở đầu `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md` với Status
`MAINMENU_SETTINGS_SLOT_CONFIRM_ART_READY`, liệt kê đúng tên file đã tạo (khớp bảng trên) và bất kỳ
sai khác về kích thước/border nếu có lý do kỹ thuật cần đổi.

---

Status: `VERIFIED_MAP_ART_INTEGRATION`

Ngày: 2026-09-26
Feature: Tích hợp `minimap_frame_v1` / `minimap_mask_v1` / `map_close_button_v1` / `player_marker_v1`
/ `minimap_tag_v1` vào Minimap + FullMap — đã gán xong, verify Play Mode thật, PASS toàn bộ.

## Kết quả tích hợp asset Map (4 asset đợt 1 + 1 tag banner đợt 2)

**Field/prefab đã gán** (`UnifiedGameplayHUD.prefab`, qua `MapUIAuthoring.Rebuild()`):
- `Minimap/Frame` (Image) ← `minimap_frame_v1`, Type Simple.
- `Minimap/MapView` (Image, có `Mask` showMaskGraphic=false) ← `minimap_mask_v1`, Type Simple;
  `MapView` đổi từ inset 6px sang khớp y hệt rect của `Frame` (offset 0,0,0,0) vì ring và mask dùng
  chung không gian canvas 384×384, mask được vẽ khít đúng lỗ tròn bên trong ring.
- `Minimap/MapView/Content/PlayerMarker` và `MapPopup/Viewport/MapImage/PlayerMarker` (Image) ←
  `player_marker_v1`, Type Simple, preserveAspect=true; bỏ hoàn toàn cấu trúc hình thoi 2 lớp
  (Outline+Dot) dựng bằng code cũ.
- `MapPopup/CloseButton` (Image) ← `map_close_button_v1`, Type Simple; xoá luôn child `Label` TMP
  "X" thừa vì art đã có sẵn chữ X.
- `Minimap/ZonePlate` và `Minimap/DatePlate` (Image, Type **Sliced**) ← `minimap_tag_v1` (border
  20/12/20/12 từ chính sprite, không hardcode lại trong code); TMP `ZoneText`/`DateText` vẫn là TMP
  runtime thật nằm đè lên, không bake chữ.
- `MinimapController.Awake()`: sửa để chỉ gán `RuntimeCircleSprite` (fallback code-vẽ) khi
  `_frameImage.sprite`/`_maskImage.sprite` đang `null` — không còn ghi đè sprite thật đã gán qua
  Inspector/authoring.

**Điều chỉnh layout đi kèm** (không đổi logic crop/bounds/zoom/zone/phím `M`/GameState):
- `Minimap` root đẩy xuống thêm 20px theo Y (từ `(-14,-14)` sang `(-14,-34)`) vì ring thật dày hơn
  placeholder cũ, cần chừa chỗ cho `ZonePlate` phía trên không đè lên ring.
- `ZonePlate` neo top-center, nằm hoàn toàn phía trên vòng tròn (không còn kiểu "Top" align cắt vào
  trong ring như bản nháp đầu); `DatePlate` neo bottom-center, nằm hoàn toàn phía dưới vòng tròn.
- Thêm rotation marker theo hướng Player (owner yêu cầu thêm sau khi thấy bản đầu marker đứng yên):
  lộ ra field `Player.FacingDirection` (đọc `_lastFacingDirection` có sẵn trong `PlayerMovement.cs`,
  vốn đã snap 4 hướng cho Animator) — property public 1 dòng, không đổi hành vi di chuyển. Marker
  luôn hướng lên khi facing Up, xoay đúng 90°/180°/270° cho Right/Down/Left.

## Kết quả Play Mode (Game View 1920×1080, do owner xác nhận)

Toàn bộ acceptance item đều PASS:
- Minimap tròn đúng kích thước logic 96×96, map crop không tràn viền, không có nền vuông hở góc.
- Frame/mask đồng tâm, khớp khít lỗ tròn trong ring.
- Player marker đúng vị trí giữa map và khi crop bị kẹp sát 4 cạnh world bounds; marker xoay đúng
  theo hướng di chuyển thật.
- FullMap mở/đóng đúng bằng `M`, zoom bằng lăn chuột không regression, Close button 44×44 hiển thị
  đúng và click đóng được.
- `ZonePlate`/`DatePlate` hiển thị đúng "Heart Village" / "26 Sep", hai đầu banner giữ nguyên hình,
  chỉ phần giữa co giãn; text căn giữa, không chạm viền vàng, không tràn.
- Banner đồng bộ trực quan với `minimap_frame_v1`.
- Không có Console error/warning mới liên quan Map UI hoặc asset import.
- Import setting của cả 5 file xác nhận không đổi sau reimport: Single, Point, Mipmap Off,
  Compression None, alpha đúng (đã đọc lại `.meta` sau `MapUIAuthoring.Rebuild()` để xác nhận).

## Gap còn lại

Không có gap chức năng. Việc còn mở duy nhất là quyết định thẩm mỹ nhỏ (không chặn gì): có thể tinh
chỉnh thêm khoảng cách/kích thước `ZonePlate`/`DatePlate` nếu owner muốn sau khi nhìn trực tiếp nhiều
lần chơi thử hơn, nhưng hiện tại đã đạt yêu cầu và không có lỗi kỹ thuật nào.

---

Status (mục cũ, đã đóng): `READY_FOR_CODEX_MAP_ART`

Ngày: 2026-09-26
Feature: Minimap (góc phải HUD) + FullMap (bấm `M`) -- logic/camera-thay-bằng-ảnh-tĩnh đã xong và
verify qua Play Mode thật, chỉ cần art. Không cần Codex sửa script nào, chỉ gen sprite rồi báo lại để
Claude gán vào field Inspector.

## Bối cảnh

Ban đầu dựng bằng Camera+RenderTexture sống (World Camera nhìn xuống world thật mỗi frame), nhưng sau
khi review đã đổi kiến trúc sang **ảnh map tĩnh** (đúng chuẩn thể loại 2D RPG này dùng, giống ảnh mẫu
owner gửi): `Tools/ProjectGame2D/UI/Bake Map Snapshot` chụp một lần toàn bộ `BorderMap` (Player tự ẩn
lúc chụp) thành `Assets/Resources/UI/Map/map_snapshot.png` (2048×1553, đã bake xong, chất lượng tốt).
Minimap hiển thị một vùng crop nhỏ cuộn theo Player (`RawImage.uvRect`) qua một `Mask` hình tròn;
FullMap hiển thị toàn bộ ảnh phủ kín màn hình 1920×1080 (kiểu "cover", không viền đen), zoom bằng lăn
chuột. Cả hai đều có icon Player hình thoi (marker riêng, không phải sprite Player thật) luôn đúng vị
trí kể cả khi crop bị kẹp ở rìa bản đồ. Đã fix xong bug "Minimap bị đơ" (race điều kiện lúc
`BorderMap` chưa load xong) và bug "viền đen 2 bên FullMap" (sai công thức cover/contain) -- cả hai đã
verify lại bằng Play Mode thật, hoạt động đúng.

Toàn bộ khung/icon hiện tại là placeholder dựng bằng code thuần (màu phẳng, `Outline` component, và
một hình tròn vẽ bằng thuật toán runtime cho Mask -- xem `RuntimeCircleSprite.cs`), **không phải
bitmap** -- cần Codex thay bằng art thật theo Dark Inventory Style.

## Asset cần gen

1. **Minimap frame** (gán vào `UnifiedGameplayHUD.prefab/Minimap`, component `Image` tên `Frame`,
   field `MinimapController._frameImage`) -- khung tròn walnut/charcoal viền vàng mảnh, đường kính
   logic hiện tại `96×96`. Có thể thêm icon la bàn nhỏ góc trên (không bắt buộc, thuần thẩm mỹ).
2. **Minimap mask shape** (component `Image` tên `MapView` bên trong `Minimap`, field
   `MinimapController._maskImage`) -- một sprite tròn trắng đặc (alpha 1 bên trong, 0 bên ngoài),
   dùng làm `Mask` để crop ảnh map bên trong thành hình tròn khớp đúng viền `Frame` ở trên. Nếu
   `Frame` đã là hình tròn đều, có thể dùng chung 1 sprite tròn cho cả hai field.
3. **FullMap Close button** (`MapPopup/CloseButton`, component `Image`) -- nút X kiểu Dark Inventory
   Style đồng bộ Character Popup/Quest Log, `44×44`, góc trên phải màn hình.
4. **Player marker** (dùng chung cho cả Minimap và FullMap, `MapUIAuthoring.CreatePlayerMarker`) --
   hiện là hình thoi 2 lớp (viền nâu đậm + lõi vàng) vẽ bằng `Image` phẳng xoay 45°. Nếu muốn icon đẹp
   hơn (mũi tên chỉ hướng theo Player facing, hoặc chấm tròn có viền), gen 1 sprite ~16-24px, Point
   filter, nền trong suốt.

Không cần gen lại `map_snapshot.png` -- đó là ảnh chụp thật từ world, không phải art cần vẽ tay; chỉ
tái-bake (chạy lại menu item) nếu địa hình world thay đổi lớn sau này.

## Việc Codex KHÔNG cần làm

- Không cần sửa `MinimapController.cs`, `FullMapController.cs`, `MapZoneManager.cs`,
  `MapZoneTrigger.cs`, `MapWorldBounds.cs`, `RuntimeCircleSprite.cs`, `MapSnapshotBaker.cs`, hay
  `MapUIAuthoring.cs` -- toàn bộ logic/camera/zoom/mask/marker đã chạy đúng và verify qua Play Mode
  thật. Nếu cần field/kích thước mới để khớp art, báo lại Claude qua `CodexToClaude.md`.
- Không cần tự đặt `RuntimeCircleSprite` sang không dùng nữa -- `MinimapController.Awake()` gán sprite
  runtime vào `_frameImage`/`_maskImage` mỗi lần chạy; chỉ cần gán sprite thật của Codex trực tiếp vào
  hai field đó qua Inspector (ghi đè lên trên), Claude sẽ xoá lời gọi `RuntimeCircleSprite.Get(...)`
  trong `Awake()` khi nhận sprite thật để tránh ghi đè ngược lại mỗi lần Play.
- Không cần đụng `GameStateManager`, `GameplayMenuPage.Map`, phím `M`, hay lifecycle mở/đóng FullMap --
  toàn bộ đã nối sẵn qua `UnifiedGameplayHudController`.
- Không cần lo phần zoom/pan FullMap hay scroll Minimap -- thuần code, không phụ thuộc art.

---

Status: `READY_FOR_CODEX_ARROW_ART`

Ngày: 2026-09-19
Feature: Training Area onboarding (Tutorial + Quest chain) backend xong, chỉ cần 4 sprite mũi tên/vòng tròn cho hệ thống quest direction indicator -- code đã chạy đúng với placeholder hình học sinh bằng code, chỉ cần thay `Image.sprite`.

## Bối cảnh

Đã dựng xong toàn bộ: Trainer NPC + 3 training dummy (HP/chết/respawn) trong `MapNhat` scene khu
TrainingArea, chuỗi quest `quest.trainer_greeting` → `quest.equip_weapon` (auto turn-in) →
`quest.trainer_killquest`, Tutorial 1-4 (Move/Sprint/OpenInventory/EquipItem) nối với quest qua
`TutorialStepType.WaitForQuest` mới, và hệ thống Quest Direction Indicator 2 nhánh (NPC target / Area
target). Toàn bộ đã verify qua Play Mode thật, không cần Codex sửa logic gì -- chỉ cần 4 sprite dưới
đây, sau đó kéo thả vào đúng field Inspector là xong, không cần đụng script.

## Asset cần gen (3 sprite, phong cách pixel art khớp game hiện tại)

Đã bỏ thiết kế vòng tròn ground marker theo yêu cầu -- "đã đến khu vực" giờ chỉ còn 1 mũi tên nhấp nhô
chỉ xuống dưới chân Player, dùng chung asset với mục 1 bên dưới.

1. **Bobbing target arrow** (`Assets/Scripts/UI/QuestDirectionIndicator.cs` field `_npcHeadMarkerImage`
   và `_arrivedMarkerArrowImage`, và `Assets/Scripts/World/MannequinAttackIndicator.cs` field
   `_arrowImage`) -- mũi tên nhấp nhô trỏ xuống, dùng chung cho "trên đầu NPC cần nói chuyện", "trên
   đầu hình nhân cần đánh", và "đã đến khu vực nhiệm vụ" (dưới chân Player). ~48x48px, nên có viền/glow
   nhẹ để nổi trên nền cỏ/nền da NPC.
2. **Ground direction arrow** (`QuestDirectionIndicator.cs` field `_groundArrowImage`) -- mũi tên dưới
   chân Player chỉ hướng khi đang di chuyển tới mục tiêu (NPC ở xa hoặc khu vực nhiệm vụ, chưa tới
   nơi). ~64x64px, nhìn từ góc top-down (game là top-down 2D).
3. **Edge-of-screen arrow** (`QuestDirectionIndicator.cs` field `_edgeIndicatorImage`) -- icon mũi tên
   bám rìa màn hình khi mục tiêu ở ngoài khung hình. ~40x40px, dạng compact, rõ hướng ở kích thước nhỏ.

Placeholder hiện tại (tam giác màu vàng-xanh sinh bằng code trong
`Assets/Scripts/UI/ProceduralArrowSprite.cs`) đã hoạt động đúng chức năng -- không có gì gấp về logic,
đây thuần là nâng cấp hình ảnh.

## Việc Codex KHÔNG cần làm

- Không cần sửa `QuestDirectionIndicator.cs`, `MannequinAttackIndicator.cs`, `QuestManager.cs`,
  `TutorialManager.cs`, `MannequinHurtbox.cs` hay bất kỳ script Quest/Tutorial nào -- toàn bộ logic đã
  xong và verify qua Play Mode. Nếu cần field/API mới, báo lại Claude qua `CodexToClaude.md`.
- Không cần tự đặt `ProceduralArrowSprite` sang trạng thái không dùng nữa -- nó tự động chỉ được dùng
  làm fallback khi field `Image.sprite` đang trống (`if (...Image.sprite == null)` trong Awake của mỗi
  script), nên gán sprite thật vào Inspector là đủ, không cần xoá code fallback.

---



Ngày: 2026-08-23
Feature: Root cause thật đã tìm ra bằng instrumentation trực tiếp — KHÔNG phải GameInputCoordinator/GameStateManager, mà là Input System event bị rớt khi Editor mất focus

## Root cause thật (có bằng chứng thực nghiệm, không còn suy luận)

Cảm ơn diagnostic bạn cung cấp — nó loại trừ chính xác giả thuyết "foreign coordinator" (đúng như
bạn quan sát: chỉ 2 coordinator, không có cái nào lạ active). Mình đã thêm instrumentation trực tiếp
vào `PressEscapeAndDiagnose()` (đếm `_cancelAction.performed` fire thật, log state trước/sau, log
`escapeKey.isPressed` trước/sau) và tái hiện được lỗi bằng đúng Unity MCP Test Runner API, scene
DemoScene đang mở, đúng trình tự full→full→targeted.

**Bằng chứng từ chính job đã fail** (`GameInputCoordinatorPlayModeTests.DisableEnable_
DoesNotDoubleSubscribe`, job full suite round 2):

```
PressEscapeAndDiagnose: fireCount=0 stateBefore=Playing stateAfter=Playing
actionEnabledAfter=True escapePressedBefore=False escapePressedAfter=False
Expected: Paused
But was:  Playing
```

**`fireCount=0`** — callback không hề fire, **không phải double-fire**. Và **`escapePressedAfter=
False`** — sau khi `InputSystem.QueueStateEvent(...)` + `InputSystem.Update()` chạy xong, bản thân
`Keyboard.escapeKey.isPressed` vẫn là `false`, tức là **sự kiện nhấn phím mô phỏng chưa từng được áp
dụng vào device** — không liên quan gì tới `GameInputCoordinator`, `GameStateManager`, hay bất kỳ
component/state nào khác, vì input còn chưa tới được tầng đó.

Job metadata của lần fail này ghi `editor_is_focused: false`. So sánh với nhiều lần PASS trước đó
(phần lớn có `editor_is_focused: true`) cho thấy tương quan: khi Unity Editor mất focus cửa sổ giữa
lúc chạy Test Runner (rất dễ xảy ra khi Test Runner được điều khiển từ bên ngoài qua Unity MCP), một
lần `QueueStateEvent` + `Update()` đơn lẻ có thể không kịp/không được áp dụng vào device trước khi
test đọc state — đây là vấn đề **độ tin cậy của việc mô phỏng input trong Test Runner**, không phải
bug ở bất kỳ script gameplay nào. Không có tương quan nào với quest content, với `GameInputCoordinator`,
hay với coordinator leftover — cả hai giả thuyết trước đó của cả hai bên đều sai hướng.

## Fix

**File đã sửa (chỉ file test):** `Assets/Scripts/Tests/PlayMode/GameInputCoordinatorPlayModeTests.cs`

`PressEscapeAndDiagnose()` giờ retry `QueueStateEvent` + `Update()` tối đa 5 lần, xác nhận thật sự
`_keyboard.escapeKey.isPressed == true` trước khi tiếp tục — thay vì giả định một lần gọi luôn thành
công. Nếu sau 5 lần vẫn không áp dụng được, test tự fail với message rõ ràng ("Input System
event-delivery failure, not anything GameInputCoordinator/GameStateManager could possibly react to")
thay vì để lỗi hiện ra dưới dạng nhầm lẫn "Expected Paused But was Playing" khó hiểu. Đây là làm cứng
hạ tầng test, không làm yếu assertion — vẫn đòi hỏi đúng kết quả `Paused` sau khi phím thật sự được
ghi nhận. Không đụng `GameInputCoordinator.cs`, `GameStateManager.cs`, quest content, scene, hay UI
nào.

## Verification (Unity MCP Test Runner API, cùng một Editor session, DemoScene đang mở, không refresh/đóng Editor giữa các bước)

1. Targeted `GameInputCoordinatorPlayModeTests`: **3/3 PASS**.
2. Full PlayMode suite lần 1: **142/142 PASS**.
3. Full PlayMode suite lần 2 (ngay sau lần 1 — **đúng điểm đã fail ở lần verify trước**): **142/142
   PASS**.
4. Full PlayMode suite lần 3 (ngay sau lần 2): **142/142 PASS**.
5. Targeted `GameInputCoordinatorPlayModeTests` ngay sau 3 lần full suite: **3/3 PASS**.
6. Full PlayMode suite lần 4, 5 (chạy thêm để tăng độ tin cậy vì lỗi vốn phụ thuộc timing/focus):
   **142/142 PASS** cả hai lần.
7. EditMode full suite: **58/58 PASS**.
8. Content Validation: 0 error, 60 legacy warning, **84 asset**.
9. DemoScene validator: 0 issue.
10. Quest content không bị đụng: `Assets/Quests/Definitions/Quest_SidePotionSupply001.asset` vẫn
    tồn tại, `questId: quest.side.potion_supply.001` còn nguyên.

Tổng cộng: **5 lần full PlayMode suite liên tiếp + 3 lần targeted**, tất cả PASS trong cùng một
Editor session, DemoScene mở suốt, không refresh/đóng Editor giữa các bước — bao gồm đúng round 2
(nơi lỗi từng xảy ra) và một round bổ sung ngay sau đó để chắc chắn.

## Kết luận

Khác với lần verify trước (chỉ làm cứng dựa trên suy luận, không tái hiện được lỗi), lần này **đã
tái hiện được lỗi thật bằng đúng Unity MCP Test Runner API**, xác định chính xác root cause bằng
instrumentation trực tiếp (fireCount=0, escapePressedAfter=false), và fix nhắm đúng vào cơ chế đó
(retry xác nhận input thực sự được device ghi nhận trước khi kiểm tra hệ quả). Sau fix, đã chạy lại
**đúng round từng fail** nhiều lần liên tiếp, tất cả xanh.

Status `READY_FOR_CODEX_FINAL_QUEST_VERIFICATION_ONLY` — vẫn chưa tự đặt `VERIFIED` vì đây là lỗi có
tính timing/focus, nên đề nghị Codex tự chạy lại một lần cuối theo đúng quy trình (full→full→targeted,
lặp nếu cần) trên máy của họ để xác nhận độc lập trước khi đóng hẳn mục quest
`quest.side.potion_supply.001` sang `VERIFIED`. Nếu môi trường của Codex khiến Editor mất focus
thường xuyên hơn (ví dụ chạy qua UI thay vì API), có thể cần tăng `maxAttempts` trong
`PressEscapeAndDiagnose()` — hiện đang là 5 lần retry.

---

# Phase 10 — Hardening và content-ready milestone (lịch sử — đã CONTENT_READY, xem mục review Save Game slot picker bên dưới)

Status (khi mục này được tạo): `CONTENT_READY`

Ngày: 2026-08-23
Feature: Phase 10 — Hardening và content-ready milestone — **ĐÃ ĐÓNG**

## Kết luận cuối

Owner đã chạy physical acceptance thật (bàn phím/chuột vật lý) trên
`C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe` và toàn bộ 24 bước **PASS** (chi tiết
từng bước trong `CodexToClaude.md § Phase 10 Final Physical Player Acceptance`). Đối chiếu với 4
acceptance criteria của Phase 10 trong `Roadmap.md`:

- Test matrix bắt buộc đều pass: EditMode 58/58, PlayMode 141/141. ✓
- Không có P0/P1 known issue trong save/progression/item transaction: xác nhận, không có. ✓
- Content designer có thể tạo Tutorial Quest mới không sửa manager core: `ContentAuthoringGuide.md`
  đủ, đã xác nhận ở Phase 10 Part 8. ✓
- Build player chạy đúng New Game và Continue trên máy sạch: **xác nhận bằng physical acceptance
  24/24 bước PASS**, bao gồm cả Save Game slot picker mới (Empty/Overwrite/Save As/Delete) và Pause
  Menu Escape fix. ✓

Cả 4 tiêu chí đều đạt → **Phase 10 chính thức `CONTENT_READY`.** Đã cập nhật
`Phase10ImplementationReport.md`, `Roadmap.md` và `README.md` với kết luận này.

Không còn task nào chờ Codex ở Phase 10. Cảm ơn về việc root-cause chính xác Pause input bug, dựng
đúng Save Game slot picker theo contract, và chạy physical acceptance đầy đủ, chi tiết từng bước —
đúng chuẩn chất lượng dự án cần.

## Việc tiếp theo (không thuộc Phase 10, chỉ ghi chú tham khảo)

Roadmap nền tảng không có Phase 11. Bước tiếp theo hợp lý là sản xuất content thật (quest/item/shop/
world entity) theo `ContentAuthoringGuide.md`, hoặc chốt các decision còn `Open`/`Proposed` trong
`DecisionRegister.md` (D-010 player death, D-013 character creation, D-019 production world topology)
nếu muốn mở rộng nền tảng trước. Sẽ có prompt riêng khi có yêu cầu cụ thể tiếp theo.

## Gamepad manual verification — vẫn còn mở, không chặn CONTENT_READY

Chưa từng có gamepad vật lý test xuyên suốt mọi phase (kể cả physical acceptance vừa rồi chỉ dùng
bàn phím/chuột). Đây là action item không blocking, có thể làm bất cứ lúc nào có gamepad thật.

---

# Phase 10 review (lịch sử — đã xác nhận PASS, xem kết luận cuối ở trên)

Status (khi mục này được tạo): `READY_FOR_CODEX_PHASE10_FINAL_MANUAL_VERIFICATION`

Ngày: 2026-08-23
Feature: Phase 10 — review kết quả Pause Input Fix + Save Game slot picker, build lại combined, chờ acceptance thủ công cuối

## Bối cảnh

Đã review kỹ cả hai phần Codex vừa hoàn thành (Pause Input Fix và Save Game slot picker) và tự chạy
lại toàn bộ verification độc lập — **cả hai đều PASS review, không có gap.** Chi tiết đầy đủ trong
[Phase10ImplementationReport.md § Part 7 follow-up](../Phase10ImplementationReport.md).

## Kết quả review

**Pause Input Fix:** root cause đúng (outgoing `InputSystemUIInputModule` và `GameInputCoordinator`
tranh cùng `InputActionAsset` trong lúc `MainMenu → DemoScene` overlap lifecycle). Fix dùng runtime
clone riêng cho coordinator (`Instantiate(_projectActions)`, destroy trong `OnDestroy`) là đúng hướng,
tối thiểu, không leak, không double-subscribe, không phụ thuộc hierarchy DemoScene cụ thể.
`GameInputCoordinatorPlayModeTests` tái hiện đúng race condition (không phải test hời hợt).

**Save Game slot picker:** wiring trong `PauseMenuUI` khớp đúng contract đã giao (Empty save ngay,
Valid/Corrupted/IncompatibleVersion đều qua `OnSaveSlotConfirmationRequired` không có ngoại lệ im
lặng, Delete luôn confirm trước, Save As đổi đúng `ActiveSlotId`, `RequestSave()`/Save-and-Return/
Save-and-Quit không bị đụng). Đã xem xét kỹ cơ chế chặn double-click bằng `Time.frameCount` theo yêu
cầu — **kết luận: đủ an toàn, không cần sửa.** Lý do: backend tự re-check status thật của slot ở mỗi
lần gọi `RequestSaveToSlot` (nên một click thứ hai ở frame sau, sau khi slot đã đổi status từ lần ghi
đầu, tự động rơi vào nhánh confirm thay vì ghi đè im lặng lần hai), và nút Confirm/Delete được bảo vệ
tự nhiên vì popup tự đóng ngay sau click đầu tiên (không còn gì để bấm lần hai).

## Verification độc lập (Claude tự chạy lại, khớp 100% số liệu Codex báo cáo)

- EditMode: 58/58 PASS.
- PlayMode: 141/141 PASS (0 regression).
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset.
- DemoScene validator: 0 issue. MainMenu validator: 0 issue (kiểm tra thêm).
- Build Settings không đổi: MainMenu index 0, DemoScene index 1.
- Build lại Player combined (chứa cả 2 thay đổi):
  `C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe` — Windows64, 0 error/0 warning,
  38.55s, 515.86 MB.
- Đã launch build này, `Player.log` init sạch (D3D12/PhysX/Input System, không exception), rồi dừng
  process. **Chưa** (và không thể trong môi trường này) xác nhận Escape thật sự mở Pause bằng bàn
  phím vật lý — giới hạn môi trường giống Part 7 gốc ([D-026](../DecisionRegister.md)), không phải
  nghi ngờ về code.

## Việc còn lại — acceptance thủ công cuối cùng (bàn phím vật lý)

Chạy trên build `C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe`:

```
MainMenu
→ New Game bằng slot trống (KHÔNG dùng Slot 1 — Slot 1 đang giữ save thật IncompatibleVersion)
→ DemoScene
→ Escape mở PauseMenuUI (đây là bug đã fix — xác nhận thật sự hoạt động)
→ Save Game → mở slot picker → thử: save vào slot Empty (ghi ngay), overwrite một slot Valid (có
  popup confirm), Save As sang slot khác (ActiveSlotId đổi theo), Delete một slot (có popup confirm)
→ Return Main Menu
→ Continue đúng slot vừa save
→ xác nhận vị trí, inventory, quest, tutorial và world state restore đúng
→ Quit Desktop
```

Báo lại PASS/FAIL cụ thể từng bước vào `CodexToClaude.md`. Nếu PASS toàn bộ, Phase 10 chuyển thành
`CONTENT_READY`. Nếu phát hiện bug thật (không phải do môi trường), ghi `BACKEND_GAP_FOUND` với bằng
chứng cụ thể.

## Việc Codex KHÔNG cần làm

- Không cần sửa thêm gì ở `GameInputCoordinator`, `PauseMenuUI`, hay `GameplaySessionController` trừ
  khi acceptance thủ công phát hiện vấn đề mới.
- Không cần dựng Recovery UI (vẫn không bắt buộc cho content-ready bar hiện tại).

---

# Save Game slot picker (lịch sử — đã VERIFIED, xem review ở trên)

Status (khi mục này được tạo): `READY_FOR_CODEX_SAVE_SLOT_UI`

Ngày: 2026-08-23
Feature: Phase 10 follow-up — Save Game slot picker (Pause Menu "Save Game" chọn 1 trong 3 slot thay vì ghi thẳng ActiveSlotId)

## Bối cảnh

Yêu cầu mới: bấm "Save Game" từ Pause Menu phải mở một overlay chọn slot (giống Load Game overlay đã
có), cho phép save vào slot trống, ghi đè slot đã có (sau confirm), "Save As" sang slot khác (đổi
`ActiveSlotId`), và xóa slot (sau confirm). Toàn bộ logic/validation/atomic write đã có sẵn trong
`GameplaySessionController` (`Assets/Scripts/GameManagers/GameplaySessionController.cs`) — Codex chỉ
cần dựng UI overlay gọi đúng API dưới đây, không tự capture save data, không tự đụng
`ISaveSlotRepository`. Xem [D-027](../DecisionRegister.md) cho quyết định semantics đầy đủ.

`RequestSave()` (không tham số, ghi thẳng vào `ActiveSlotId`) **vẫn giữ nguyên** — dùng cho luồng
Save-and-Return/Save-and-Quit hiện có trong `PauseMenuUI`, không đổi gì ở đó.

## Contract phía Claude cung cấp (mới, thêm vào GameplaySessionController — không đổi API cũ)

```csharp
// Slot picker cho nút "Save Game" mới trong Pause Menu.
public bool CanSaveToSlot(int slotId);              // gate hiển thị/enable từng nút slot trong overlay
public bool SlotRequiresOverwriteConfirm(int slotId); // true nếu slot đã có data (Valid/Corrupted/IncompatibleVersion)

public void RequestSaveToSlot(int slotId);   // slot Empty: ghi ngay. Slot khác: fire OnSaveSlotConfirmationRequired, KHÔNG ghi
public void ConfirmOverwriteAndSave();        // ghi slot đang pending sau khi user xác nhận overwrite
public void CancelSaveToSlot();               // đóng popup overwrite, không ghi gì, không đổi ActiveSlotId

public bool DeleteSlot(int slotId);           // UI PHẢI tự hỏi xác nhận trước khi gọi -- method này xóa luôn, không hỏi lại
```

Event mới:

```csharp
// (slotId, status của slot đó) -- status quyết định text popup ("Ghi đè save đã có" vs "Save này bị
// hỏng, xóa và ghi đè?" vs "Save này từ phiên bản không tương thích, xóa và ghi đè?")
public event Action<int, SaveSlotStatus> OnSaveSlotConfirmationRequired;
```

Event cũ vẫn dùng lại nguyên như Load Game overlay đã dùng: `OnSaveSlotListChanged` (gọi
`RefreshSlots()` để lấy `SaveSlotInfo[]` cho cả 3 slot), `OnSaveSucceeded`, `OnOperationFailed`
(`GameplaySessionOperationResult` có thêm giá trị mới `InvalidSlot` cho slotId ngoài phạm vi 1..3).

## Luồng UI đề xuất

```text
Pause → "Save Game" → mở Save Slot Overlay (RefreshSlots() để hiển thị 3 slot, dùng đúng UI đã
  dựng cho Load Game overlay, chỉ đổi hành vi click)

Click slot:
  CanSaveToSlot(slotId) == false → disable nút đó (đang IsBusy hoặc không có active session)
  SlotRequiresOverwriteConfirm(slotId) == false (Empty) → gọi RequestSaveToSlot(slotId) ngay,
    không cần popup phụ
  SlotRequiresOverwriteConfirm(slotId) == true → gọi RequestSaveToSlot(slotId), chờ
    OnSaveSlotConfirmationRequired(slotId, status) → hiển thị popup 2 lựa chọn (Overwrite/Cancel),
    text theo status:
      Valid → "Ghi đè save đã có ở Slot {n}?"
      Corrupted → "Save ở Slot {n} bị hỏng. Xóa và ghi đè bằng save hiện tại?"
      IncompatibleVersion → "Save ở Slot {n} không tương thích phiên bản này. Xóa và ghi đè?"
    → Overwrite: gọi ConfirmOverwriteAndSave()
    → Cancel: gọi CancelSaveToSlot(), đóng popup, quay lại Save Slot Overlay

Nút Delete cạnh mỗi slot (không phải Empty):
  Click → popup xác nhận riêng ("Xóa save ở Slot {n}? Không thể hoàn tác.")
  → Confirm: gọi DeleteSlot(slotId), overlay tự refresh qua OnSaveSlotListChanged
  → Cancel: đóng popup, không gọi gì

Save thành công (OnSaveSucceeded) → đóng toàn bộ overlay, quay lại Paused, hiển thị timestamp mới
  (đọc lại từ SaveSlotInfo của ActiveSlotId sau RefreshSlots()).
```

## Việc Codex KHÔNG cần làm

- Không tự capture `GameSaveData` hay đụng `ISaveSlotRepository` -- mọi write đi qua
  `RequestSaveToSlot`/`ConfirmOverwriteAndSave`.
- Không thay đổi `RequestSave()` (không tham số) hay luồng Save-and-Return/Save-and-Quit hiện có.
- Không thay đổi layout Load Game overlay đã có -- tái sử dụng cùng component hiển thị slot, chỉ đổi
  handler khi click.
- Không sửa Quest/Tutorial/Commerce/Inventory/world persistence.

## Test cần có phía Codex (theo Quality Strategy, cho phần UI)

- Click slot Empty → save ngay, không có popup phụ nào xuất hiện.
- Click slot Valid/Corrupted/IncompatibleVersion → đúng text popup theo status.
- Cancel popup overwrite → overlay giữ nguyên, không đổi gì.
- Double-click nút Save trong lúc `IsBusy` → nút đã disable, không gửi request thứ hai.

## Verification đã chạy phía Claude

- 58/58 EditMode, **137/137 PlayMode** (18 test mới cho slot picker: Empty save, Save As đổi
  ActiveSlotId, Valid/Corrupted/IncompatibleVersion đều yêu cầu confirm, Cancel không đổi gì,
  double-click bị chặn, Delete thường/active-slot/không rò slot khác, write-failure giữ nguyên save
  cũ) -- 0 regression.
- Content Validation 0 error, DemoScene validator 0 issue.

Sau khi dựng UI xong, cập nhật `CodexToClaude.md` với kết quả test/manual verify và trạng thái phù hợp
(README/Roadmap/QualityStrategy không cần đổi thêm trừ khi Codex phát hiện gap).

---

# Phase 10 — Hardening và content-ready milestone (không có UI mới cần dựng; chỉ cần verification thủ công)

Status (khi phase này bắt đầu): `READY_FOR_CODEX_VERIFICATION`

Ngày: 2026-08-23
Feature: Phase 10 — Hardening và content-ready milestone (không có UI mới cần dựng; chỉ cần verification thủ công)

## Bối cảnh

Phase 10 là audit/hardening, **không phải feature phase** — không có UI mới nào cần Codex dựng. Toàn
bộ chi tiết: [Phase10ImplementationReport.md](../Phase10ImplementationReport.md).

Đã hoàn tất và verify:
- Save migration pipeline (V1→Current, mọi save cũ được nâng cấp an toàn khi load, không rewrite file
  cho tới lần save thật tiếp theo).
- Soak test thật (120 chu kỳ save/load cùng slot, A→B→C, world snapshot 60 object, teardown/recreate)
  — tất cả PASS, kết quả số liệu thật trong report.
- Profiling baseline thật cho từng giai đoạn save pipeline (serialize/write/read/migration riêng biệt).
- Recovery UX backend contract: **đã đủ, không cần Codex build UI mới.** `MainMenuController` +
  `SaveSlotInfo.Status` (`Empty`/`Valid`/`Corrupted`/`IncompatibleVersion`) đã cung cấp mọi thứ cần để
  UI phân biệt corrupted/incompatible/empty và gọi `DeleteSlot`/`RefreshSlots` — xem Part 6 trong report
  nếu muốn build màn hình recovery đẹp hơn sau này, nhưng không bắt buộc cho content-ready.
- 6 tài liệu authoring content (item/quest/shop-recipe/persistent world entity/area-spawn/scene
  integration): [ContentAuthoringGuide.md](../ContentAuthoringGuide.md).
- Full regression: 58/58 EditMode, 119/119 PlayMode, Content Validation 0 error, DemoScene validator
  0 issue.
- Player build (Windows64): **build thành công, 0 error/0 warning**, nhưng click-through smoke test
  (New Game→DemoScene→Save→Return→Continue→Quit) **chưa verify được** trong môi trường automation này
  (cửa sổ game không capture/điều khiển được qua remote automation ở đây — không phải lỗi build/code,
  xem Part 7 trong report).

## Việc cần Codex/user làm (chỉ verification thủ công, không phải code mới)

1. Chạy build đã có sẵn tại `C:\Users\havin\Phase10PlayerBuild\ProjectGame2D.exe` trên máy thật (không
   phải qua remote automation), hoặc build lại từ Build Settings hiện tại (MainMenu index 0, DemoScene
   index 1 — không đổi).
2. Click-through: Launch → MainMenu → New Game (chọn slot **trống**, không phải Slot 1 — Slot 1 trên
   máy dev hiện đang giữ save thật `IncompatibleVersion` từ trước Phase 6, không được ghi đè) →
   DemoScene → Save → Return Main Menu → Continue (đúng slot vừa save) → xác nhận vị trí/inventory/
   quest/tutorial/world state đúng → (tuỳ chọn) Load một slot khác nếu có fixture an toàn → Quit
   Desktop.
3. Báo lại kết quả PASS/FAIL. Nếu PASS, Phase 10 chuyển thành `CONTENT_READY`.
4. Gamepad manual verification vẫn là action item tồn đọng từ mọi phase trước — không mới ở Phase 10.

## Việc Codex KHÔNG cần làm

- Không cần dựng Recovery UI mới (Corrupted/Incompatible screens) — không bắt buộc cho content-ready
  bar hiện tại; contract đã sẵn sàng nếu muốn làm sau.
- Không cần sửa `PauseMenuUI`, `QuestUIRoot`, `CommerceUIRoot`, Inventory UI, Tutorial UI hay bất kỳ
  world visual nào — Phase 10 không chạm layout của bất kỳ UI nào trong số này.
- Không cần thêm gameplay feature mới.

---

# Phase 9 — Save/Load/Return/Quit backend (Pause Menu Save/Load/Return/Quit UI cần Codex dựng)

Status (khi phase này bắt đầu): `READY_FOR_CODEX_UI`

Ngày: 2026-08-23
Feature: Phase 9 — Save/Load/Return/Quit backend (Pause Menu Save/Load/Return/Quit UI cần Codex dựng)

## Bối cảnh

Phase 9 Save/Load/Return/Quit backend đã hoàn tất và tự vận hành đúng (48/48 EditMode, 118/118
PlayMode PASS, Content Validation 0 error, DemoScene validator 0 issue, verify sống end-to-end qua
`execute_code` bao gồm reload `DemoScene` thật hai lần liên tiếp để chứng minh không rò dữ liệu giữa
slot). Chi tiết kiến trúc đầy đủ: [Phase9ImplementationReport.md](../Phase9ImplementationReport.md).

Việc cần Codex làm: thêm nút/UI Save Game, Load Game, Return Main Menu và Quit Desktop vào
`PauseMenuUI` hiện có (`Assets/Scripts/UI/PauseMenuUI.cs`), cùng slot overlay cho Load Game và popup
3 lựa chọn cho Return/Quit khi dirty. Toàn bộ logic/state/file I/O đã có sẵn qua
`GameplaySessionController`.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### GameplaySessionController (`Assets/Scripts/GameManagers/GameplaySessionController.cs`)

Đã gắn sẵn trên `_SceneContext` trong DemoScene, không cần tạo GameObject mới.

```csharp
public int ActiveSlotId { get; }
public bool IsDirty { get; }
public bool IsBusy { get; }   // true khi GameState là Saving hoặc Loading -- disable mọi nút Save/Load/Return/Quit khi true

public SaveSlotInfo[] RefreshSlots();   // đọc lại cả 3 slot, cũng tự fire OnSaveSlotListChanged
public bool CanLoad(int slotId);        // true chỉ khi slot đó Status == Valid

public bool RequestSave();
public bool RequestLoad(int slotId);

public void RequestReturnToMainMenu();          // clean -> return ngay; dirty -> fire OnConfirmationRequired, KHÔNG tự làm gì khác
public void ConfirmSaveAndReturn();             // chỉ Return sau khi save thật thành công
public void ConfirmReturnWithoutSaving();
public void CancelReturnToMainMenu();           // đóng popup, không đổi gì khác

public void RequestQuit();                      // clean -> quit ngay; dirty -> fire OnConfirmationRequired
public void ConfirmSaveAndQuit();
public void ConfirmQuitWithoutSaving();
public void CancelQuit();
```

Event/read-model:

```csharp
public event Action<SaveSlotInfo[]> OnSaveSlotListChanged;
public event Action OnSaveSucceeded;                                          // đã refresh slot xong khi fire
public event Action<GameplaySessionOperationResult, string> OnOperationFailed; // (lý do, message hiển thị được)
public event Action<GameplaySessionConfirmationKind> OnConfirmationRequired;   // ReturnToMainMenu hoặc Quit
```

`GameplaySessionOperationResult`: `Success`, `NoActiveSession`, `AlreadyBusy`, `SlotNotValid`,
`ReadFailed`, `WriteFailed`, `TransitionFailed` -- map từng giá trị sang message UI nếu muốn custom
hơn string mặc định đi kèm (string thứ hai trong `OnOperationFailed` đã là message thân thiện sẵn
dùng được luôn, không bắt buộc phải tự viết theo enum).

`GameplaySessionConfirmationKind`: `ReturnToMainMenu`, `Quit` -- dùng để chọn đúng popup 3 nút hiện
(nội dung khác nhau: "Save and Return"/"Return Without Saving"/"Cancel" vs "Save and Quit"/"Quit
Without Saving"/"Cancel"), gọi đúng `Confirm*`/`Cancel*` method tương ứng.

**Quan trọng**: `OnConfirmationRequired` **không** đổi `GameState` -- lúc này vẫn `Paused`. Popup chỉ
là UI navigation con (đúng `UIAndInteractionFlows.md`), không tạo `GameplayMenuPage` mới nếu không
cần. `Cancel*` cũng không đổi gì backend, chỉ cần đóng popup phía UI.

## Slot presentation dùng chung với MainMenu

`SaveSlotInfo`/`SaveSlotStatus`/`SaveSlotMetadata` là đúng type Codex đã dùng để dựng
`MainMenuSaveSlotsUI.cs` ở Phase 3 -- Load Game overlay trong gameplay có thể tái dùng cùng
presentation logic (Empty/Valid/Corrupted/IncompatibleVersion, level/area/playtime/last-saved) thay
vì tự viết lại. Điểm khác duy nhất: `GameplaySessionController.ActiveSlotId` cho biết slot nào đang
active để UI hiển thị rõ (theo `UIAndInteractionFlows.md`: "Load Game hiển thị ba slot nhưng phân
biệt rõ active slot").

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/GameManagers/GameplaySessionController.cs`,
  `SessionDirtyTracker.cs`, `GameSessionManager.cs`, `SceneFlowService.cs` -- nếu cần API/field mới,
  báo lại Claude qua `CodexToClaude.md`.
- Không cần tự capture save data hay gọi `ISaveSlotRepository` trực tiếp -- `RequestSave()` đã làm
  toàn bộ, UI chỉ gọi và lắng nghe event.
- Không cần tự theo dõi dirty state bằng tay -- đọc `controller.IsDirty` hoặc lắng nghe
  `GameSessionManager.Instance.DirtyStateChanged` nếu muốn hiển thị icon "unsaved changes" trong
  Pause Menu.
- Không cần lo về việc rò dữ liệu giữa các slot khi Load -- `SceneFlowService` đã được sửa để luôn
  teardown session cũ trước khi load session mới, verify sống bằng scene reload thật.
- Không gọi `Application.Quit()` trực tiếp -- không cần, `RequestQuit()`/`Confirm*Quit` đã dùng
  `IApplicationQuitter` nội bộ đúng yêu cầu testability.
- Không chỉnh `QuestUIRoot`, `CommerceUIRoot`, Inventory UI, Tutorial UI, MainMenu UI, hay layout/
  hierarchy hiện có của `PauseMenuUI` ngoài phần thêm mới cho Save/Load/Return/Quit.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: mở Pause → Save Game → thông báo thành công, timestamp/metadata cập nhật (kiểm tra lại
  qua Load Game overlay hoặc MainMenu).
- Manual: mở Pause → Load Game → chọn slot khác → xác nhận scene load lại, state cũ (inventory/
  quest/world) đúng của slot mới, không dính state slot cũ.
- Manual: chọn slot Corrupted/Empty trong Load Game overlay → bị disable hoặc hiện lỗi rõ, không
  crash.
- Manual: thay đổi gì đó (nhặt item, giết enemy, mở chest...) → `IsDirty == true` → bấm Return Main
  Menu → popup 3 lựa chọn hiện đúng. Test cả ba nhánh (Save and Return, Return Without Saving,
  Cancel).
- Manual: tương tự cho Quit Desktop (không cần thật sự quit app khi test bằng Editor Play Mode --
  `Application.Quit()` không có tác dụng trong Editor, chỉ log; đây là hành vi Unity bình thường,
  không phải bug).
- Manual: spam Save/Load/Return/Quit trong lúc `IsBusy == true` → chỉ một operation chạy, các lần
  bấm thêm bị từ chối êm (không crash, không double transition).

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Save/Load/Return/Quit UI trong Pause Menu thuộc Codex.
Khi xong, cập nhật `CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía
backend trừ khi phát sinh gap mới).

---

# Phase 8 — World Persistence backend (không cần UI mới; cần scene/prefab visual)

Status: `READY_FOR_CODEX_SCENE_INTEGRATION` (đã `VERIFIED` bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-23

## Bối cảnh

Phase 8 World Persistence backend đã hoàn tất và tự vận hành đúng (48/48 EditMode, 85/85 PlayMode
PASS, Content Validation 0 error, verify sống trong DemoScene + minimal portability scene qua
`execute_code`). Chi tiết kiến trúc đầy đủ: [Phase8ImplementationReport.md](../Phase8ImplementationReport.md).

Khác các phase trước, Phase 8 **không cần Codex dựng UI mới** -- bốn entity persistent (chest, unique
pickup, boss, resource node) hiện chưa có visual gì (không sprite, không animation, không prompt).
Việc cần Codex làm (khi có capacity, không gấp): thêm visual/interaction prompt cho bốn loại entity
này trong DemoScene, theo đúng contract public bên dưới -- không cần logic mới, chỉ cần trình bày.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### Bốn component persistent (`Assets/Scripts/World/`), tất cả implement `IPersistentWorldObject`

```csharp
// ChestInteractable
public bool IsOpened { get; }
public bool TryOpen(out bool granted);          // false + granted=false nếu đã mở hoặc hết chỗ chứa

// UniquePickupInteractable
public bool IsCollected { get; }
public bool TryCollect(out bool granted);        // tự SetActive(false) khi granted=true

// ResourceNodeInteractable
public bool IsAvailable { get; }                 // tính on-demand từ DateTime.UtcNow, không polling
public bool TryHarvest(out bool granted);

// BossDefeatTracker (đặt trên GameObject RIÊNG, không phải trên chính EnemyUniversal --
// xem "Lưu ý quan trọng" bên dưới)
public bool IsDefeated { get; }
```

Cả bốn đều có `PersistentId`/`Kind` (từ `IPersistentWorldObject`) và các field Inspector đã author
sẵn (`_rewardItemId`, `_itemId`, `_resourceId`, v.v. -- xem asset thật bên dưới để đọc giá trị cụ
thể, đừng đoán).

- `_openedIndicator` (Chest) / `_depletedIndicator` (ResourceNode): `GameObject` optional, tự động
  `SetActive` theo state nếu Codex gán -- có thể dùng ngay làm hook hiển thị mà không cần sửa script,
  chỉ cần kéo một GameObject con (icon/sprite khác) vào field đó qua Inspector.
- Pickup ẩn bằng cách tự `SetActive(false)` cả GameObject khi collected -- nếu cần hiệu ứng
  biến mất mượt hơn (fade/particle) thay vì biến mất tức thì, báo lại qua `CodexToClaude.md` để
  Claude đổi thành một event/hook riêng thay vì tự sửa `UniquePickupInteractable.cs`.

## Lưu ý quan trọng: `BossDefeatTracker` không nằm trên chính boss

`EnemyUniversal` tự `Destroy()` GameObject của nó vài giây sau khi chết (corpse lifetime). Nếu Codex
gắn thêm bất kỳ component nào cần sống lâu hơn con boss đó (ví dụ để hiển thị "Boss Defeated" banner)
thì phải đặt trên `BossDefeatTracker`'s GameObject (`BossTracker_ForestGuardian` trong DemoScene) chứ
không phải trên `ForestGuardianBoss` -- GameObject đó sẽ biến mất sau khi chết đúng như enemy thường.

## Content thật đã có để test ngay (không cần tạo asset mới)

Trong DemoScene (`_World`):

- `Chest_TownGeneral` (`world.chest.town.general.01`) -- mở ra nhận 2× Iron Ore.
- `Pickup_AncientRelic` (`world.pickup.tutorial.relic.01`) -- nhặt nhận 1× Ancient Relic (item mới,
  non-stackable).
- `ResourceNode_WoodLog` (`world.resource.tutorial.wood_log.01`) -- harvest nhận 2× Wood, cooldown
  60s, đồng thời phát Quest `ResourceGathered` event thật (đóng nốt integration gap Gather còn lại
  từ Phase 6/7).
- `BossTracker_ForestGuardian` (`world.boss.forest.guardian.01`) -- theo dõi enemy mới
  `ForestGuardianBoss` (duplicate của Goblin, `enemyId = enemy.boss.forest_guardian`, vị trí `(10,
  5)`, tách biệt hoàn toàn khỏi Kill objective `enemy.goblin.green` của `quest.main.001` nên không
  xung đột content Phase 7).
- `Assets/Prefabs/World/Chest.prefab` -- prefab asset của Chest (dùng để test portability;
  `Chest_TownGeneral` trong DemoScene hiện là instance rời, chưa link prefab này, có thể re-link nếu
  Codex muốn thống nhất workflow prefab-based).

Tất cả bốn đã đăng ký sẵn trong `WorldObjectRegistry` (component trên `_SceneContext`) -- save/load
đã hoạt động đúng, không cần Codex đụng vào phần đó.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/World/`, `WorldObjectRegistry`, hay
  `PlayerSpawnReadinessSource` -- nếu cần field/API mới (ví dụ hiệu ứng ẩn khác cho pickup, prompt
  UI riêng cho từng loại), báo lại Claude qua `CodexToClaude.md`.
- Không cần lo về save/restore -- `WorldObjectRegistry`/`PlayerSpawnReadinessSource` đã đảm bảo
  world state đúng trước khi Playing, idempotent, không phát event giả khi restore.
- Không chỉnh `QuestUIRoot`/`CommerceUIRoot`, Inventory UI, Tutorial UI, MainMenu UI.
- Không cần sửa `MapManager`/`SoundFXManager` -- `MapManager` đã được Claude sửa xong trong phase
  này (không còn `DontDestroyOnLoad`, rebind đúng khi scene reload).

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ visual/prompt/hierarchy cho bốn entity persistent thuộc Codex khi cần. Khi xong (hoặc nếu
quyết định không cần visual ở bước này), cập nhật `CodexToClaude.md`.

---

# Phase 7 — Shop/Crafting backend (Shop/Crafting UI cần Codex dựng tiếp)

Status: `VERIFIED` (đã Codex xác nhận, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Bối cảnh

Phase 7 Shop/Crafting backend đã hoàn tất và tự vận hành đúng (46/46 EditMode, 64/64 PlayMode PASS,
Content Validation 0 error, verify sống trong DemoScene qua `execute_code`). Chi tiết kiến trúc đầy
đủ: [Phase7ImplementationReport.md](../Phase7ImplementationReport.md).

Việc cần Codex làm: dựng Shop UI (mua/bán) và Crafting UI (chọn recipe/craft), cùng thêm
`ShopInteraction`/`CraftingInteraction` capability vào `TownElderNPC` (prefab đã có từ Phase 6, tái
dùng làm chủ shop/recipe ở phase này -- không cần NPC mới). Đây thuần là UI/Canvas + component bind
vào service có sẵn — **không cần** dựng Dialogue UI hay hệ thống resource/gather (chưa tồn tại,
ngoài phạm vi Phase 7).

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### ShopManager (`Assets/Scripts/Shop/ShopManager.cs`), qua `ShopManager.Instance`

Persistent singleton, luôn tồn tại trong DemoScene sau khi scene load xong.

```csharp
public IShopResolver Catalog { get; }   // .AllShops để liệt kê shop content
public bool TryPurchase(string shopId, string itemId, int quantity, out ShopTransactionResult result);
public bool TrySell(string shopId, string itemId, int quantity, out ShopTransactionResult result);
```

`ShopDefinition` (đọc qua `Catalog.AllShops` hoặc `Catalog.TryResolve(shopId, out def)`):
`ShopId`, `DisplayName`, `NpcId`, `Stock` (mỗi `ShopStockEntry` có `ItemId`/`Price`),
`SellPriceMultiplier`.

`ShopTransactionResult` enum: `Success`, `ShopNotFound`, `ItemNotInStock`, `InsufficientGold`,
`InsufficientInventoryCapacity`, `InsufficientItemQuantity`, `GameplayNotAllowed` -- dùng để hiển
thị lý do fail cụ thể.

**Lưu ý bán lại**: `TrySell` chỉ bán được item nằm trong chính `Stock` của shop đó (giá = `Price *
SellPriceMultiplier`). Bán item không thuộc stock trả `ItemNotInStock`/`ShopNotFound` tuỳ trường hợp
-- không phải bug, là scope quyết định (xem Known limitations trong report).

### CraftingManager (`Assets/Scripts/Crafting/CraftingManager.cs`), qua `CraftingManager.Instance`

```csharp
public IRecipeResolver Catalog { get; }   // .AllRecipes
public bool TryCraft(string recipeId, string stationTag, out CraftingTransactionResult result);
```

`RecipeDefinition`: `RecipeId`, `DisplayName`, `Ingredients` (mỗi `RecipeIngredientEntry` có
`ItemId`/`Quantity`), `OutputItemId`/`OutputQuantity`, `RequiredStationTag` (rỗng = craft mọi nơi,
truyền `null`/`""` cho `stationTag`), `NpcId`.

`CraftingTransactionResult` enum: `Success`, `RecipeNotFound`, `WrongStation`,
`InsufficientIngredients`, `InsufficientOutputCapacity`, `GameplayNotAllowed`.

### ShopNpcInteractionService / CraftingNpcInteractionService (plain C#, không MonoBehaviour)

NPC component tạo instance (`new ShopNpcInteractionService(ShopManager.Instance)`,
`new CraftingNpcInteractionService(CraftingManager.Instance)`) và gọi qua đây, giống pattern
`QuestNpcInteractionService` đã dùng cho `TownElderNPC`:

```csharp
// Shop
public bool TryGetShop(string npcId, out ShopDefinition shop);
public bool TryPurchase(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result);
public bool TrySell(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result);

// Crafting
public IReadOnlyList<RecipeDefinition> GetOfferedRecipes(string npcId);
public bool TryCraft(string npcId, string recipeId, string stationTag, out CraftingTransactionResult result);
```

Cả hai validate đúng `npcId` sở hữu shop/recipe trước khi chạm Manager -- NPC component không cần tự
kiểm tra ownership.

## Content thật đã có để test UI ngay (không cần tạo asset mới)

- `Assets/Shops/ShopCatalog.asset` → `shop.town.general` (`npcId = npc.town.elder`, tái dùng
  `TownElderNPC`): bán `item.material.wood` (5 gold), `item.consumable.health_potion` (20 gold).
- `Assets/Crafting/RecipeCatalog.asset` → 2 recipe (cả hai `npcId = npc.town.elder`):
  `recipe.material.plank` (3× Wood → 1× Plank, không cần station) và
  `recipe.consumable.health_potion` (2× Wood + 1× Iron Ore → 1× Health Potion, cần
  `stationTag = "station.forge"`).
- 3 item mới có icon placeholder: `item.material.iron`, `item.material.plank`,
  `item.consumable.health_potion` (`Assets/Resources/Items/Shop/`).
- Player không có sẵn Iron Ore trong starting inventory -- để test recipe cần station, seed tạm qua
  `Resources.Load<ItemSO>("Items/Shop/IronOre")` + `InventoryManager.Instance.AddItem(...)`, hoặc
  đợi hệ thống Gather (chưa tồn tại) cấp Iron Ore thật ở phase sau.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/Shop/`, `Assets/Scripts/Crafting/` -- nếu
  cần field/API mới (ví dụ stock quantity giới hạn, base sell value chung), báo lại Claude qua
  `CodexToClaude.md`.
- Không cần lo về Quest integration -- `ShopManager.TryPurchase`/`CraftingManager.TryCraft` đã tự
  raise `QuestDomainEvents.ItemPurchased`/`ItemCrafted` thật, `QuestManager` (Phase 6) đã subscribe
  sẵn; UI chỉ cần gọi transaction, không cần biết gì về Quest.
- Không cần dựng Dialogue UI hay Resource/Gather UI (chưa tồn tại, ngoài phạm vi Phase 7).
- Không chỉnh `QuestUIRoot`, visual của `TownElderNPC`, Inventory UI, Tutorial UI hay layout/font
  hiện có -- chỉ thêm component/Canvas mới cho Shop/Crafting.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: mở Shop tại `TownElderNPC`, mua Health Potion → gold trừ đúng, item vào inventory; mua khi
  không đủ gold → từ chối, không trừ gì.
  bán lại Wood cho đúng shop đó → gold cộng đúng theo `sellPriceMultiplier`.
- Manual: mở Crafting tại `TownElderNPC`, craft Wood Plank (không cần station) → thành công, nguyên
  liệu bị trừ đúng. Craft Health Potion không đứng gần station (nếu UI có khái niệm station theo
  vị trí) → `WrongStation`; đứng đúng chỗ có `stationTag = "station.forge"` → thành công.
- Manual: một quest test tạm với objective Purchase/Craft (không có sẵn trong content hiện tại, có
  thể tạo asset tạm chỉ để verify rồi xoá) → xác nhận progress lên `ReadyToTurnIn` sau giao dịch
  thật, không cần click nào khác ngoài nút Buy/Craft.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Shop/Crafting UI và mọi thay đổi trên
`TownElderNPC` prefab thuộc Codex. Khi xong, cập nhật `CodexToClaude.md` để Claude biết UI đã sẵn
sàng (không cần thay đổi gì phía backend trừ khi phát sinh gap mới).

---

# Phase 6 — Quest backend (Quest Log/Tracker/NPC UI cần Codex dựng tiếp)

Status: `READY_FOR_CODEX_UI_BINDING` (đã `VERIFIED` bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Update 2026-08-22 — Trả lời 2 gap trong `CodexToClaude.md` (`BACKEND_GAP_FOUND`)

Đã xử lý cả hai gap Codex báo lại sau khi dựng `QuestUIRoot`/`TownElderNPC`. Không đổi
`QuestUIRoot`, `TownElderNPC` prefab, Tutorial UI, Inventory UI hay bất kỳ layout/font nào.

### 1. Presentation API cho objective progress

Thêm `QuestManager.TryGetProgress` (không đổi `ToSaveData`, không cần reflection):

```csharp
public bool TryGetProgress(string questId, out QuestProgressSnapshot snapshot);
```

`QuestProgressSnapshot` (`Assets/Scripts/Quest/QuestProgressSnapshot.cs`) là `readonly struct`:

```csharp
public QuestStatus Status { get; }
public int CurrentObjectiveIndex { get; }
public IReadOnlyList<int> ObjectiveCounters { get; }   // đồng bộ index với QuestDefinition.Objectives
```

- Trả `false` (snapshot mặc định) nếu quest chưa có runtime entry -- tức đang `Locked`/`Available`,
  chưa accept lần nào, không có gì để hiển thị progress.
- `ObjectiveCounters` là **bản copy tại thời điểm gọi** (`Clone()` trên array runtime), không phải
  live reference -- sửa mảng trả về không ảnh hưởng `QuestRuntimeState` thật, và gọi lại
  `TryGetProgress` sau khi có event mới sẽ ra snapshot mới đúng dữ liệu. Không expose mutable
  collection ra ngoài Definition/Runtime/Save boundary.
- Dùng cùng `CurrentObjectiveIndex` để index vào `QuestDefinition.Objectives[index].Description`/
  `.TargetCount` cho instruction text + target hiện tại; `ObjectiveCounters[index]` là progress số
  (`counters[i]` ứng với `Objectives[i]`, kể cả objective đã qua).
- Ví dụ hiển thị "1/2 killed" cho objective Kill đang active:
  `int current = snapshot.ObjectiveCounters[snapshot.CurrentObjectiveIndex]; int target =
  quest.Objectives[snapshot.CurrentObjectiveIndex].TargetCount;`

Verify sống trong DemoScene (Play Mode thật): accept `quest.tutorial.crafting.001` →
`TryGetProgress` = true, `counters = [0,0]`; sau 1 `RaiseEnemyKilled` khớp objective 0 →
`counters = [1,0]`. Trước khi accept, `TryGetProgress` = false đúng như spec.

### 2. Description rỗng cho objective + validator

Đã author `Description` cho toàn bộ objective của cả hai quest hiện có
(`Assets/Quests/Definitions/Quest_TutorialCrafting001.asset`,
`Assets/Quests/Definitions/Quest_Main001.asset`) -- không còn objective nào rỗng text.

`ContentValidationRunner.ValidateQuestObjective` giờ báo **Error** (không phải Warning) cho
objective có `Description` rỗng/whitespace -- coi đây là required presentation field, đúng nguyên
tắc "Handoff phải cung cấp read-model/contract public ổn định, không tự chế display data cho UI"
(Roadmap Phase 6 Boundary). Content Validation chạy lại: **0 error, 60 warning (không đổi), 69
asset checked**.

### Test

- EditMode: 42/42 PASS (không đổi số lượng file mới -- chỉ patch content).
- PlayMode: 48/48 PASS (+1: `QuestManagerPlayModeTests.TryGetProgress_ReflectsLiveStateAndReturnsADefensiveCopy`
  -- verify false khi chưa accept, đúng status/index/counters sau accept + progress, và mutate bản
  copy trả về không leak vào runtime state thật).
- Play Mode smoke test qua Unity MCP `execute_code` trên DemoScene thật (không chỉ test giả lập):
  xác nhận `TryGetProgress` hoạt động đúng trên `QuestManager.Instance` thật, console sạch.

Không có thay đổi nào khác tới contract Phase 6 gốc (event/status semantics/NPC service không đổi).

## Bối cảnh

Phase 6 Quest backend đã hoàn tất và tự vận hành đúng (42/42 EditMode, 47/47 PlayMode PASS, Content
Validation 0 error, verify sống trong DemoScene qua `execute_code`). Chi tiết kiến trúc đầy đủ:
[Phase6ImplementationReport.md](../Phase6ImplementationReport.md).

Việc cần Codex làm: dựng Quest Log/Tracker UI (danh sách quest Active/ReadyToTurnIn/Completed,
objective progress hiển thị được cho người chơi) và NPC marker/interaction UI (offer quest / turn-in
prompt) trong DemoScene. Đây thuần là UI/Canvas + một NPC prefab tối thiểu để test — **không cần**
Dialogue/Shop/Crafting UI thật (những hệ thống đó chưa tồn tại, xem "Integration gap" bên dưới).

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### QuestManager (`Assets/Scripts/Quest/QuestManager.cs`), qua `QuestManager.Instance`

Persistent singleton, luôn tồn tại trong DemoScene sau khi scene load xong (giống
`TutorialManager.Instance`/`InventoryManager.Instance`).

```csharp
public IQuestResolver Catalog { get; }                 // .AllQuests để liệt kê toàn bộ quest content
public bool IsMainQuestUnlocked { get; }

public event Action<string> QuestAccepted;             // questId
public event Action<string> QuestProgressChanged;      // questId -- objective counter/status đổi
public event Action<string> QuestCompleted;             // questId -- fire đúng 1 lần khi TryTurnIn thành công
public event Action MainQuestUnlocked;                  // fire đúng 1 lần khi Tutorial Quest chain xong

public QuestStatus GetStatus(string questId);           // Locked/Available/Active/ReadyToTurnIn/Completed/Failed
public bool TryAcceptQuest(string questId);
public bool TryTurnIn(string questId, out QuestTurnInResult result);
```

`QuestDefinition` (đọc qua `Catalog.AllQuests` hoặc `Catalog.TryResolve(questId, out def)`) có field
đọc được: `QuestId`, `DisplayName`, `Objectives` (mỗi objective có `Type`, `TargetId`, `TargetCount`,
`Description` -- dùng `Description` để hiển thị text, đừng tự chế), `IsTutorialQuest`, `IsMainQuest`,
`GiverNpcId`, `TurnInNpcId`.

Objective progress hiện tại (counter/index) không có getter công khai trực tiếp trên
`QuestRuntimeState` qua `QuestManager` -- nếu UI cần hiển thị "2/3 killed", báo lại Claude qua
`CodexToClaude.md` để thêm getter (ví dụ `QuestManager.TryGetProgress(questId, out int index, out
int[] counters)`), đừng tự đọc reflection vào private field.

### QuestNpcInteractionService (`Assets/Scripts/Quest/QuestNpcInteractionService.cs`)

Plain C# (không phải MonoBehaviour) -- NPC component của Codex tạo một instance
(`new QuestNpcInteractionService(QuestManager.Instance)`) và gọi qua đây, **không** gọi thẳng
`QuestManager` cho logic liên quan tới NPC identity:

```csharp
public bool TryGetOfferedQuest(string npcId, out QuestDefinition quest);   // quest Available mà npcId này cho
public bool TryAcceptQuest(string npcId, string questId);
public bool TryGetTurnInQuest(string npcId, out QuestDefinition quest);    // quest ReadyToTurnIn tại npcId này
public bool TryTurnIn(string npcId, string questId, out QuestTurnInResult result);
public void ReportConversation(string npcId, string outcomeId);           // cho Talk objective (xem gap)
```

`QuestTurnInResult` enum: `Success`, `QuestNotFound`, `ObjectivesIncomplete`,
`InsufficientInventoryCapacity`, `AlreadyCompleted` -- dùng để hiển thị lý do fail cụ thể thay vì
generic "failed".

## Content thật đã có để test UI ngay (không cần tạo asset mới)

- `Assets/Quests/QuestCatalog.asset` chứa 2 quest:
  - `quest.tutorial.crafting.001` ("The Blacksmith's Request", Tutorial Quest): Kill
    `enemy.slime.green`×2 tại `area.tutorial` + Obtain `item.material.wood`×3. `giverNpcId` =
    `turnInNpcId` = `npc.town.elder`.
  - `quest.main.001` ("A Call to Adventure", Main Quest, prerequisite = quest trên): Kill
    `enemy.goblin.green`×1.
- 3 enemy có sẵn trong DemoScene đã gắn `enemyId` thật: `Slime1`/`Slime2` = `enemy.slime.green`,
  `Goblin` = `enemy.goblin.green` (tất cả `areaId = area.tutorial`) -- giết chúng bằng gameplay thật
  sẽ tiến quest thật, không cần giả lập.
- **Chưa có NPC GameObject/prefab nào trong scene** -- `npc.town.elder` chỉ là stable ID trong data,
  chưa có world object tương ứng. Codex cần tự tạo GameObject/prefab NPC tối thiểu (collider tương
  tác + component gọi `QuestNpcInteractionService`) để test luồng offer/accept/turn-in bằng gameplay
  thật; không có ràng buộc hierarchy/tên cụ thể nào từ phía Claude cho NPC này.

## Integration gap có chủ đích (đừng tự chế UI cho phần này)

4 objective type sau **chưa có hệ thống production thật** (không Dialogue/Crafting/Shop/Resource
system trong project) -- đây là quyết định đã ghi rõ trong
[Phase6ImplementationReport.md](../Phase6ImplementationReport.md):

- **Talk**: `QuestNpcInteractionService.ReportConversation(npcId, outcomeId)` là entry point, nhưng
  chưa có Dialogue UI/system thật gọi nó. Nếu Codex dựng NPC "nói chuyện" đơn giản (không phải full
  dialogue tree), có thể gọi `ReportConversation` trực tiếp khi player tương tác — đó là hợp lệ, không
  phải giả lập test.
- **Craft/Purchase**: cần `CraftingService`/`ShopService` (Phase 7, chưa tồn tại). Đừng dựng Shop/
  Crafting UI giả trong Phase 6 UI pass này.
- **Gather**: cần Resource/gather interaction script (chưa có phase cụ thể). Tương tự, không tự chế.

Nếu UI cần hiển thị các objective type này trước khi hệ thống thật tồn tại, hiển thị đúng
`Description`/`TargetCount` như objective khác — không cần logic tương tác thật cho tới khi Phase 7+.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/Quest/`, `InventoryManager.HasItemId`,
  `InventoryManager.AddItem` (chỗ raise `InventoryItemAdded`), hay `EnemyUniversal._enemyId`/`_areaId`
  -- nếu cần field/API mới (ví dụ progress getter ở trên), báo lại Claude qua `CodexToClaude.md`.
- Không cần lo về restore/save -- `QuestManager.RestoreState()` (backend, gọi từ
  `PlayerSpawnReadinessSource`) đã đảm bảo UI mở giữa chừng vẫn thấy đúng status/progress qua
  `GetStatus`/`Catalog`.
- Không cần dựng Shop/Crafting/Dialogue UI thật (xem Integration gap).
- Không chỉnh `TutorialOverlayRoot`/`TutorialOverlayUI` hay Inventory UI hiện có.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: NPC hiển thị đúng quest offer khi `quest.tutorial.crafting.001` = `Available`; sau accept,
  giết 2 slime + nhặt 3 wood → UI phản ánh `ReadyToTurnIn`; turn-in tại đúng NPC → reward vào
  inventory, quest biến mất khỏi active list, `quest.main.001` chuyển `Available`.
  Cách tạo Obtain event thật: `InventoryManager.Instance.AddItem` trên `item.material.wood`
  (`Resources.Load<ItemSO>("Items/Quest/WoodMaterial")`) -- không cần world pickup script mới nếu
  chưa có, chỉ cần test UI phản ứng đúng khi backend event fires.
- Manual: turn-in tại NPC sai (không phải `turnInNpcId`) bị từ chối, không hiện reward.
- Manual: Continue game giữa chừng quest -- UI hiện đúng status/progress ngay khi vào scene, không
  cần đợi event đầu tiên.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Quest Log/Tracker/NPC UI thuộc Codex. Khi xong, cập nhật
`CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía backend trừ khi phát
sinh gap mới, ví dụ cần thêm getter progress).

---

# Phase 5 — UI hiển thị Input Tutorial (instruction prompt + skip)

Status: `READY_FOR_CODEX` (đã VERIFIED bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Bối cảnh

Phase 5 backend (`TutorialManager`, domain event, save/restore, `AreaTriggerZone`) đã hoàn tất, tự
vận hành đúng (28/28 EditMode, 32/32 PlayMode PASS) nhưng **chưa có UI nào hiển thị cho người chơi
thấy**. Chi tiết kiến trúc đầy đủ: [Phase5ImplementationReport.md](../Phase5ImplementationReport.md).

Việc cần Codex làm: dựng một overlay nhỏ trong DemoScene hiển thị `InstructionText` của step tutorial
hiện tại, và nút Skip có confirm. Đây thuần là UI/Canvas — không cần đổi gameplay logic.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

`TutorialManager` (`Assets/Scripts/Tutorial/TutorialManager.cs`), truy cập qua
`TutorialManager.Instance` (persistent singleton, luôn tồn tại trong gameplay scene sau khi
`PlayerSpawnReadinessSource` restore xong):

```csharp
public TutorialStepDefinition CurrentStep { get; }   // null nếu đã completed hoặc chưa có definition
public bool IsCompleted { get; }
public event Action<TutorialStepDefinition> OnStepChanged;   // fire khi qua step mới
public event Action OnTutorialCompleted;                     // fire đúng 1 lần khi xong step cuối
public void Skip();                                           // nhảy thẳng completed, không phát OnStepChanged
```

`TutorialStepDefinition` có field đọc được: `StepId` (string), `Type` (enum, không cần hiển thị),
`InstructionText` (string — đây là nội dung để show lên UI).

## UI cần dựng

1. **Panel instruction** (góc màn hình, ví dụ top-center hoặc top-left, không che HUD/inventory hiện
   có) — hiển thị `CurrentStep.InstructionText`.
   - Ẩn hoàn toàn nếu `TutorialManager.Instance == null` hoặc `CurrentStep == null` (đã completed
     hoặc chưa init xong).
   - Subscribe `OnStepChanged` để đổi text khi qua step mới.
   - Subscribe `OnTutorialCompleted` để ẩn panel (kèm hiệu ứng nhẹ nếu muốn, không bắt buộc).
   - Khi UI vừa `OnEnable`/mở game giữa chừng (ví dụ Continue), đọc luôn `CurrentStep` hiện tại để
     hiển thị đúng ngay lập tức — không đợi event đầu tiên.
2. **Nút Skip** trên panel đó, có **popup confirm** trước khi gọi (theo D-008 — skip tutorial phải có
   xác nhận, không skip ngay khi bấm 1 lần). Sau khi user xác nhận: gọi
   `TutorialManager.Instance.Skip()`.
3. Panel này là **gameplay overlay thuần túy** giống Inventory/Pause hiện có — không đi qua
   `GameStateManager` state machine (tutorial không pause game, không chặn input), chỉ là Canvas hiển
   thị/ẩn theo event ở trên.

## Việc Codex KHÔNG cần làm

- Không cần đổi `TutorialManager`, domain event, hay bất kỳ script nào trong
  `Assets/Scripts/Tutorial/`, `Assets/Scripts/GameManagers/AreaTriggerZone.cs` — nếu thấy cần đổi field
  gì ở đó (ví dụ thêm icon cho step, thêm field mới trong `TutorialStepDefinition`), báo lại Claude
  qua `CodexToClaude.md` thay vì tự sửa (đây là ScriptableObject data contract, đổi ẩu có thể vỡ save
  cũ hoặc content asset đã tạo).
- Không cần lo về restore/save — `TutorialManager.RestoreState()` (backend) đã đảm bảo UI mở lên giữa
  chừng vẫn thấy đúng step hiện tại qua `CurrentStep`.
- Chưa cần làm UI cho `AreaTrigger_Town`/`ReachArea` riêng — step đó cũng chỉ là một `InstructionText`
  bình thường như các step khác, panel dùng chung.

## Nội dung step hiện có (để tham khảo hiển thị, đọc thật từ asset, đừng hardcode text trong UI script)

6 step trong `Assets/Tutorial/Tutorial_TutorialArea.asset`: Move → Sprint → Attack → OpenInventory →
EquipItem → ReachArea (`area.town`, placeholder position `(10,0,0)` trong DemoScene, sẽ dời khi có Town
thật). `InstructionText` hiện tại là placeholder — nếu cần văn bản hiển thị đẹp hơn, có thể tự sửa nội
dung field đó trực tiếp trên asset qua Unity Editor (đây là content, không phải code, Codex có thể sửa
tự do), không cần hỏi lại Claude cho việc đổi text thuần túy.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: New Game → panel hiện đúng step Move → đi bộ → panel đổi sang Sprint → ... → sau step cuối
  panel ẩn.
- Manual: bấm Skip → confirm popup hiện → xác nhận → panel ẩn ngay, không đi qua step trung gian.
- Manual: Continue game đã có tutorial dở dang → panel hiện đúng step đã lưu ngay khi vào scene.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho panel này thuộc Codex. Khi xong, cập nhật
`CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía backend trừ khi phát sinh
gap mới).
