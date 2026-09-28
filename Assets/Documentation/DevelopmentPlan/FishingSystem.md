# Fishing System

Tài liệu này là source of truth cho cơ chế câu cá đầu tiên trong `MapNhat`.

## Quyết định gameplay

- Click trái vào `FishingSpotInteractable` trong tầm để bắt đầu; không tự chạy Player tới điểm câu.
- **D-05x (2026-09-28): cần mồi câu để bắt đầu.** Trước khi bắt đầu phải có một `FishingBaitItemSO`
  đang được chọn ở Quick Bar (số 1-8) với quantity > 0 -- không có mồi hợp lệ sẽ hiện thông báo
  "You don't have any bait to fish with." và không chuyển `GameState.FishingWaiting`. Đây là thay đổi
  so với quyết định gốc ("click tự do, không điều kiện tiên quyết") -- xem `DecisionRegister.md`. Quick
  bar vẫn dùng đúng contract đã có ở `FarmingSystem.md` (chọn item, domain đọc theo item được chọn);
  đây chính là "consumable về sau" đã dự trù.
- Mồi bị tiêu đúng 1 đơn vị ngay khi session Waiting thực sự bắt đầu (nghĩa là đã qua cả điều kiện mồi
  lẫn điều kiện Inventory còn chỗ) -- không tiêu nếu Inventory đầy hoặc không có mồi, vì session chưa
  từng bắt đầu trong hai trường hợp đó.
- **Bait tier:** `FishingBaitItemSO.Tier` (`Small`/`Medium`/`Large`) giới hạn `FishDefinitionSO` nào có
  thể roll ra qua `RequiredBaitTier`. Luật permissive: mồi tier cao câu được cá tier đó và mọi tier thấp
  hơn (mồi Large câu được Small+Medium+Large; mồi Small chỉ câu Small). `FishingSpotDefinition.TryRollFish`
  nhận `maxTier` và lọc `FishEntries` trước khi roll trọng số.
- Trước khi bắt đầu phải có ít nhất một ô Inventory trống. Mỗi con cá chiếm đúng một ô vì cân nặng
  và giá trị là dữ liệu riêng của instance.
- Giai đoạn chờ cá cắn dùng `GameState.FishingWaiting`: world vẫn chạy nhưng gameplay input bị khóa.
- Khi dấu `!` xuất hiện, click trái trong hook window để vào minigame. Bỏ lỡ sẽ quay lại chờ cá khác,
  không kết thúc session.
- Minigame dùng `GameState.FishingMinigame`: world pause qua policy của `GameStateManager`, UI không
  sửa trực tiếp `Time.timeScale`.
- Giữ chuột làm catch zone đi lên, thả chuột làm nó đi xuống. Chạm icon cá tăng progress; mất tiếp xúc
  làm progress giảm. Progress đầy trước khi hết giờ là thành công; hết giờ là thất bại.
- `Escape` hủy session ở mọi giai đoạn và trả về state trước đó.

## Data và runtime boundary

`FishDefinitionSO : ItemSO` là definition dùng chung, được resolve qua stable `itemId` như item khác:

- Icon dùng field `ItemSO.icon` để Inventory hiển thị tự động.
- `minimumWeightGrams`, `maximumWeightGrams` định nghĩa khoảng cân nặng.
- `pricePerKilogram` định nghĩa giá một kg.
- `FishId` chính là stable `itemId`; tên asset/display name không phải identity.

`FishInstanceData` là runtime/save state riêng của một con cá:

- `instanceId`: GUID ổn định của con cá đã bắt.
- `weightGrams`: số nguyên gram để tránh sai số float khi save và tính tiền.
- Tổng giá trị = `round(pricePerKilogram * weightGrams / 1000)`.

`FishingSpotDefinition` là definition của điểm câu: weighted fish table, thời gian chờ/hook/minigame,
chuyển động cá, catch zone và tốc độ tăng/giảm progress. `FishingMinigameController` chỉ giữ state
session tạm thời; không ghi kết quả vào definition.

## Inventory và persistence

- Cá chỉ được thêm qua `InventoryManager.TryAddFish`; generic `AddItem`/batch từ chối
  `FishDefinitionSO` để không làm mất metadata instance.
- `InventorySaveData.SlotData.fish` là payload tùy chọn. Item thường để null; fish hợp lệ phải có
  `instanceId` và `weightGrams > 0`.
- Fish instance payload được giới thiệu ở version 7; save schema toàn game hiện là version 9.
  Migration V6→V7 giữ nguyên inventory cũ và bổ sung payload nullable; các migration V7→V9 của
  quick bar/farming là additive và không thay đổi fish payload;
  save cũ không sinh cá giả.
- Restore cá luôn clamp cân nặng theo definition hiện tại, giữ nguyên `instanceId`, ép quantity = 1.
  Fish payload hỏng bị bỏ qua kèm warning thay vì tạo item nửa hợp lệ.

## Visual direction

Theo D-055: `FishingFeatureAuthoring.CreateFeaturePrefab()` dựng UI bằng `Image` màu phẳng
placeholder (chưa có bitmap). Đã thử 2 đợt gen asset Dark Inventory Style (`_v1` kích thước gốc bị
chê quá mỏng/phẳng; `_v2` phóng to `MinigamePanel`/gauge để sửa nhưng phát sinh lệch bố cục do phải
tính lại toạ độ tay). Owner quyết định **revert toàn bộ về code/asset gốc** (đã làm, khớp git HEAD)
và làm lại theo hướng: **asset bám đúng kích thước logic gốc — không sửa bất kỳ `sizeDelta`/
`anchoredPosition` nào trong `FishingFeatureAuthoring.cs`** khi tích hợp, để không còn rủi ro lệch bố
cục; phong cách đơn giản hơn `_v2` (bớt ornament nhiều lớp) nhưng vẫn khắc phục đúng 2 lỗi thật của
`_v1`: BitePrompt cần nền bán trong suốt phía sau ring (không để rỗng-giữa hoàn toàn), CatchZone cần
màu tương phản mạnh, khác hẳn tông của MovementTrack để không bị chìm.

Asset production (`Assets/Resources/UI/Fishing/DarkInventoryStyle/`), kích thước khớp 1:1 giá trị
hardcode hiện có trong `FishingFeatureAuthoring.cs`:

| File | Logic size (canvas 1920×1080) | Ghi chú |
|---|---|---|
| `fishing_waiting_panel_v3.png` | 500×74 | Bán trong suốt để thấy world; chứa TMP "Waiting for a bite..." |
| `fishing_bite_prompt_v3.png` | 150×150 | Ring + nền bán trong suốt bên trong (rút kinh nghiệm từ lần trước), quanh TMP "!" |
| `fishing_minigame_panel_v3.png` | 520×650 | Panel chứa Timer/MovementTrack/Slider/FishIcon |
| `fishing_movement_track_v3.png` | 150×490 | Channel đặc (phần thân opaque chiếm phần lớn chiều rộng), Fish/CatchZone di chuyển trong đó |
| `fishing_catch_zone_v3.png` | 128×120 (baseline) | Màu tương phản mạnh so với track; 9-slice border trên/dưới bắt buộc (chiều cao đổi runtime) |
| `fishing_slider_track_v3.png` | 46×490 | Nền progress dọc (dùng với `Slider.fillRect` gốc, không phải `Image.Filled`) |
| `fishing_slider_fill_v3.png` | 46×490 | Fill progress dọc |
| `fishing_result_panel_v3.png` | 620×180 | Panel Success/Fail |

Không đổi kích thước bitmap thật khác biệt logic size lần này — phải khớp đúng để zero code change.
Không đổi `FishingMinigameController`/`FishingMinigameUI` public API, timing hay `GameState` policy.

Sau khi `_v3` verify PASS, owner phát hiện thêm 2 vấn đề nội dung vẽ (không phải layout): board
`fishing_waiting_panel_v3` chỉ vẽ đặc ~42%/60% canvas khiến TMP tràn ra ngoài khung; 2 file slider
chỉ vẽ đặc ~30-35% chiều rộng khiến thanh gần như không đọc được tiến độ. Đã yêu cầu Codex vẽ lại 3
file này lấp ≥80-90% canvas (giữ nguyên tên/kích thước) — verify PASS, đồng thời phát hiện
`Fill Area` trong `CreateVerticalSlider` có padding 5px cứng từ code gốc (chưa từng gây chú ý khi
còn màu phẳng) khiến fill hẹp hơn track — đã sửa `offsetMin/Max` về 0.

### D-056: Reveal-on-catch (icon cá bí ẩn)

Theo D-056: `FishIcon` trong `MovementTrack` không còn hiện `_selectedFish.icon` thật trong lúc chơi
— dùng 1 sprite "cá bí ẩn" chung (`fishing_mystery_fish_icon.png`, đang chờ Codex, size khớp `FishIcon`
hiện có `62×62`). Loại cá thật chỉ lộ ra ở `ResultFishIcon` (child mới trong `ResultPanel`, `90×90`
tại `(85,0)` anchor trái-giữa, `ResultText` inset trái `140px` để không đè icon) khi
`CompleteCatch()` thành công; thất bại/hết giờ/đầy túi không hiện icon. `FishingMinigameUI.ShowResult`
nhận thêm tham số `Sprite revealedFishIcon = null` (optional, không phá caller cũ);
`FishingMinigameController.BeginResult` truyền `_selectedFish.icon` khi thành công, `null` khi thất
bại. Không đổi việc roll cá (vẫn random từ `FishingSpotDefinition.FishTable` lúc `BeginMinigame` như
cũ) — chỉ ẩn thông tin khỏi UI cho tới lúc kết quả.

## Scene và prefab integration

- Reusable prefab: `Assets/Prefabs/Fishing/FishingFeature.prefab`.
- River spot definition: `Assets/Game/Fishing/Definitions/FishingSpot.River.asset`.
- Fish definitions: `Assets/Resources/Items/Fish/` để `ResourcesItemResolver` resolve khi load save.
- `MapNhat/Wooden_FishingRod` có trigger collider và `FishingSpotInteractable`, tham chiếu controller
  của `FishingFeature` trong scene.
- Rebuild/install bằng menu `Tools/Project Game/Fishing/Build And Install MapNhat Fishing`.

## Authoring cá mới

1. Tạo `FishDefinitionSO` dưới `Assets/Resources/Items/Fish/`.
2. Gán stable `itemId` mới theo dot namespace, display name, icon, khoảng gram, giá/kg và
   `RequiredBaitTier` (Small/Medium/Large -- quyết định mồi nào câu được cá này).
3. Giữ `isStackable = false`, `maxStackSize = 1`.
4. Thêm asset vào `FishingSpotDefinition.FishTable` với weight lớn hơn 0.
5. Author `_minSellPrice`/`_maxSellPrice` nếu cá này được một NPC mua (D-042) -- khác với
   `PricePerKilogram` vốn chỉ dùng để tính giá trị hiển thị lúc bắt được cá.
6. Chạy `Tools/Project Game/Validate Content`, EditMode tests và thử happy/fail/missed-hook/full-inventory/
   no-bait/wrong-bait-tier trong scene integration trước khi promote content.

## Authoring mồi câu mới

1. Tạo `FishingBaitItemSO` (menu `Project Game 2D/Fishing/Fishing Bait Item`) dưới
   `Assets/Resources/Items/Fishing/`.
2. Gán stable `itemId` (`item.consumable.bait_<tier>`), display name, icon, giá Buy/Sell và `Tier`.
3. Bán qua Buy tab của một `ShopDefinition` (ví dụ `Shop_Dunstan.asset`) như item thường -- không cần
   wiring runtime riêng, người chơi gán vào Quick Bar như mọi item khác.

## Acceptance matrix

- Không có mồi hợp lệ ở Quick Bar (trống hoặc item khác) hoặc quantity 0: hiện thông báo, không tiêu
  mồi, không bắt đầu chờ.
- Có mồi nhưng Inventory đầy: hiện thông báo "Inventory full", không tiêu mồi (session chưa thực sự
  bắt đầu).
- Mồi tier thấp không roll ra được cá tier cao hơn trong cùng spot.
- Inventory đầy (đã có mồi hợp lệ): click spot chỉ hiện thông báo, khóa attack của cùng click và không
  bắt đầu chờ.
- Miss hook: quay lại Waiting và có bite mới (không tiêu thêm mồi -- chỉ tiêu một lần lúc bắt đầu).
- Minigame: world pause; progress tăng/giảm đúng tiếp xúc; timeout fail.
- Success: một fish instance vào đúng một slot, icon đúng, weight/value đúng công thức.
- Save/load: `itemId`, `instanceId`, weight round-trip; save V6 migrate lên V7 không mất slot cũ.
- Thoát/hủy: UI ẩn, coroutine dừng, state trước đó được khôi phục đúng một lần.

