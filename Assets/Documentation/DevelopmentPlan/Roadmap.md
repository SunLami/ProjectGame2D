# Development Roadmap

## Nguyên tắc triển khai

Mỗi phase tạo ra một lát cắt có thể kiểm chứng. Không xây đồng thời Save, Quest và World persistence
khi data contract chưa ổn định. Mọi thay đổi serialized asset phải được kiểm tra trong Unity, không chỉ
bằng static analysis.

## Data-Driven Gate áp dụng cho mọi phase

Mọi domain content mới phải tuân theo [Data-Driven Development Guide](DataDrivenDevelopment.md):

- Tách Definition asset, Runtime State và Save DTO.
- Có stable ID convention và không dùng GameObject/filename/array index làm save identity.
- Catalog/resolver là dependency explicit; không lookup Resources rải rác.
- Có validator cho ID, reference và cross-domain contract.
- Code handler định nghĩa behavior type; data định nghĩa content cụ thể.
- Chứng minh ít nhất hai content variants dùng cùng runtime code khi domain có tính biến thể.
- Không mutation ScriptableObject definition trong runtime.

Không phase nào được đánh dấu hoàn tất nếu content data mới chưa đạt các gate phù hợp với domain đó.

## Phase 0 — Ổn định project baseline

### Mục tiêu

Tạo một baseline build/play đáng tin trước khi thêm hệ thống nền tảng.

### Công việc

- Giữ `DemoScene.unity` làm integration playground chính thức; không rename thành GameScene.
- Chốt tên và đường dẫn `MainMenu.unity`.
- Chưa bắt buộc chốt topology world scene production; định nghĩa `sceneId`/`areaId` từ đầu.
- Tạo convention `_SceneContext`, feature prefab root và demo-only debug tools.
- Chốt stable ID naming convention và danh sách domain ID ban đầu.
- Inventory toàn bộ ScriptableObject/catalog hiện có; chưa refactor hàng loạt.
- Sửa Build Settings đang tham chiếu `SampleScene.unity` đã bị xóa.
- Lưu snapshot git sạch hoặc commit baseline có thể quay lại.
- Sửa compiler/runtime error chặn test, ưu tiên TMP Font Atlas và missing reference.
- Lập danh sách prefab/service hiện dùng `DontDestroyOnLoad`.
- Xác nhận Input System action map dành riêng cho Main Menu và Gameplay.

### Acceptance criteria

- Mở project không có compiler error.
- Có thể chạy trực tiếp DemoScene để test integration.
- Có thể chạy từ MainMenu qua New/Continue mock vào DemoScene trong giai đoạn development.
- Demo-only tool được nhận diện rõ và không vô tình phụ thuộc production code.
- Play mode 5 phút không có exception lặp lại trong Console.
- Scene/prefab thay đổi được lưu và version-control nhận đúng `.meta`.
- ID convention và data-driven boundaries được ghi nhận trước khi tạo save schema.

## Phase 1 — Scene bootstrap, GameState và GameSession

### Mục tiêu

Triển khai luồng `Booting → MainMenu → Loading → Playing` và phân tách rõ scene/menu overlay.

### Công việc

- Đổi `GameState.Menu` thành `GameState.GameplayMenu`.
- Đổi `GameMenuPage` thành `GameplayMenuPage`.
- Thêm `GameState.MainMenu`.
- Không tự động vào `Playing` trong `GameStateManager.Awake`.
- Thêm `GameBootstrap` xác định initial state dựa trên scene khởi động.
- Thêm `SceneFlowService` chịu trách nhiệm load scene; UI không gọi `SceneManager` trực tiếp.
- Thêm `GameSessionManager` giữ slot đang active và loại session `NewGame`/`LoadedGame`.
- Xác định transition failure path: load thất bại phải trở về Main Menu và hiển thị lỗi.
- Giữ gameplay overlay stack: `Playing → Paused → GameplayMenu(Settings) → Paused`.
- Cho phép development session chọn DemoScene làm gameplay target mà không hard-code nó vào save schema.

### Acceptance criteria

- Player build luôn bắt đầu ở Main Menu.
- Chạy trực tiếp DemoScene trong Editor có development bootstrap rõ ràng và không cần đi qua MainMenu.
- Không có gameplay input trong Main Menu/Loading.
- Không có frame gameplay hiển thị trước khi restore save xong.
- Trở về Main Menu giải phóng scene gameplay hiện tại (DemoScene hoặc world scene) và reset active session.
- Pause/Inventory/Settings hiện tại vẫn hoạt động sau rename.

## Phase 2 — Save slot metadata và file foundation

### Mục tiêu

Có ba save slot đáng tin cậy trước khi lưu gameplay domain.

### Công việc

- Xây `SaveSlotRepository` quản lý Slot 1–3.
- Định nghĩa `SaveSlotMetadata` và `GameSaveData` có `saveVersion`.
- Save DTO chỉ lưu stable ID/state delta, không serialize ScriptableObject/GameObject reference.
- Tách capture snapshot khỏi serialize/write file.
- Ghi file atomic bằng temp file, backup và replace.
- Thêm checksum hoặc validation tối thiểu cho JSON.
- Hỗ trợ slot trống, corrupted, compatible và incompatible version.
- Thêm delete slot có confirm; không overwrite slot ngoài lựa chọn người chơi.
- Thêm mock/in-memory repository để test UI không cần ghi thật.

### Acceptance criteria

- Tạo, đọc, overwrite và xóa độc lập cả ba slot.
- Crash/failure trong lúc ghi không phá save cuối cùng hợp lệ.
- Main Menu đọc metadata mà không load toàn bộ world data.
- Corrupted slot không làm crash Main Menu và không ảnh hưởng slot khác.

## Phase 3 — New Game/Continue và player restore trong DemoScene

### Mục tiêu

Hoàn tất hai đường vào gameplay bằng DemoScene làm integration target đầu tiên. Scene production dùng
cùng flow sau khi được tạo.

### Công việc

- Main Menu New Game mở slot selector và chỉ cho chọn slot trống hoặc overwrite có confirm.
- Tạo default save với stable `saveId`, `areaId = tutorial_area`, spawn ID `tutorial_start`.
- Continue chỉ hiển thị slot hợp lệ có metadata.
- Xây `SpawnRegistry` ánh xạ stable spawn ID thành Transform.
- Lưu `areaId` cùng position; không chỉ lưu raw transform.
- Restore PlayerStat, health, position và session play time theo thứ tự xác định.
- Chỉ chuyển sang `Playing` sau khi player đã spawn và camera đã bind.
- Scene target được resolve từ development config/session, không ghi chuỗi `DemoScene` vào domain save.

### Acceptance criteria

- New Game luôn spawn đúng Tutorial Area và không nhận item hai lần khi reload.
- Continue spawn sai số trong tolerance đã định tại đúng vị trí cũ.
- Load một slot không rò dữ liệu inventory/stat từ slot trước.
- Save mới và save cũ dùng chung gameplay path trong DemoScene sau bước restore.
- Cùng feature root có thể chạy trong một minimal test scene mà không mang toàn DemoScene theo.

## Phase 4 — Inventory, equipment và stat persistence

### Mục tiêu

Lưu/restore đầy đủ dữ liệu nhân vật hiện có.

### Công việc

- Chốt unique `itemId` và editor validation cho trùng/rỗng.
- Xây item catalog/resolver contract thống nhất; giữ Resources như migration implementation nếu cần.
- Tách item definition read-only khỏi runtime slot/save state.
- Bổ sung gold vào save.
- Bổ sung equipment slots vào save bằng item ID.
- Clamp quantity, stack size và item compatibility khi load.
- Sửa equip/unequip thành transaction để inventory đầy không mất item.
- Seed starting inventory chỉ khi tạo New Game, không chạy mỗi lần load scene.
- Recalculate stats từ base progression + equipment sau restore.

### Acceptance criteria

- Round-trip inventory/gold/equipment không thay đổi dữ liệu.
- Load thiếu item catalog không crash; báo warning và giữ report phục hồi.
- Không duplicate starter item khi load/reload.
- Không thể mất item khi inventory đầy trong equip/unequip.

## Phase 5 — Tutorial system

### Mục tiêu

Hướng dẫn thao tác cho nhân vật mới nhưng không biến tutorial thành game mode cứng.

### Công việc

- Xây `TutorialManager` và `TutorialStep` data-driven.
- Định nghĩa Tutorial Definition/Step Definition, completion condition handlers và catalog validation.
- Theo dõi movement, sprint, attack, inventory và equipment bằng domain event.
- Lưu current step/completed trong save.
- Cho phép rời khu tutorial; thiết kế prompt nhắc thay vì khóa sandbox.
- Xác định skip/replay policy (khuyến nghị: skip có confirm, replay không đổi progression).
- Hướng dẫn đi Town sau input tutorial.
- Dựng Tutorial/Town test area trong DemoScene bằng prefab/config có thể promote sang world scene.

### Acceptance criteria

- New Game bắt đầu đúng step đầu.
- Save/load giữa một step tiếp tục đúng step, không phát thưởng lần hai.
- Save cũ đã hoàn thành không bật tutorial lại.
- Tutorial không subscribe event trùng sau load hoặc scene lifecycle.

## Phase 6 — Quest foundation và Tutorial Quest chain

### Mục tiêu

Có quest data-driven, objective tracking và prerequisite mở Main Quest.

### Công việc

- Định nghĩa Quest Definition asset với stable `questId`.
- Định nghĩa runtime `QuestProgress` độc lập asset.
- Định nghĩa Quest Save DTO chỉ chứa ID, status và counters.
- Hỗ trợ objective cơ bản: talk, obtain, craft, purchase, gather và kill.
- Dùng gameplay event bus typed event; không poll toàn thế giới mỗi frame.
- Xây NPC quest interaction và turn-in validation.
- Xây chuỗi Tutorial Quest; hoàn thành chain phát `MainQuestUnlocked`.
- Lưu active/completed quest và objective counters.
- Chống phát thưởng lặp lại sau load hoặc double-click turn-in.

### Acceptance criteria

- Objective chỉ tăng từ event hợp lệ và không vượt target ngoài thiết kế.
- Save/load giữ đúng active step/counter.
- Main Quest NPC không cấp Main Quest trước prerequisite.
- Người chơi vẫn khám phá/craft/shop khi chưa làm Tutorial Quest.
- Hoàn thành Tutorial Quest mở Main Quest đúng một lần.

## Phase 7 — NPC Shop/Crafting và quest integration

### Mục tiêu

Hoàn chỉnh các tương tác cần cho Tutorial Quest.

### Công việc

- Tách `ShopService`, `CraftingService` khỏi UI.
- Transaction mua/bán/craft phải atomic đối với gold, nguyên liệu và inventory capacity.
- Phát typed events `ItemPurchased`, `ItemCrafted` cho QuestManager.
- NPC chỉ mở đúng interaction khi player trong range và gameplay state cho phép.
- Chốt recipe/item catalog validation.
- Shop/Recipe definition tách runtime transaction state; event chỉ phát sau transaction thành công.

### Acceptance criteria

- Giao dịch thất bại không trừ tiền/nguyên liệu một phần.
- Quest objective không phụ thuộc click UI; chỉ phụ thuộc transaction thành công.
- Save/load giữ kết quả giao dịch thông qua inventory/gold persistence.

## Phase 8 — World persistence

### Mục tiêu

Lưu các thay đổi lâu dài của sandbox mà không cố serialize mọi GameObject.

### Công việc

- Gán stable `persistentId` cho chest, unique pickup, boss và resource node cần lưu.
- Phân biệt definition ID với persistent instance ID.
- Xây registry phát hiện ID trùng trong editor.
- Chia entity thành persistent và respawn-by-rule.
- Lưu chest opened, unique pickup collected, boss defeated và resource respawn timestamp.
- Khôi phục world trước khi cho phép gameplay.
- Sửa MapManager/service scene reference để rebind sau load.
- Chứng minh persistent feature chạy cả DemoScene và minimal portability scene.

### Acceptance criteria

- Persistent entity giữ trạng thái qua save/load.
- Enemy thường respawn theo rule, không làm save phình không cần thiết.
- ID trùng bị phát hiện trước build.
- Object bị xóa/đổi giữa version không làm hỏng toàn bộ save.

## Phase 9 — Save/load UX trong gameplay scene

### Mục tiêu

Người chơi save/load/return Main Menu an toàn từ Pause Menu.

### Công việc

- Save Game ghi vào active slot; confirm trước overwrite nếu policy yêu cầu.
- Load Game hiển thị ba slot nhưng phân biệt rõ active slot.
- Block double-submit trong Saving/Loading.
- Hiển thị success/error và timestamp sau save.
- Return Main Menu hỏi save trước khi rời nếu có dirty session.
- Quit Desktop có lựa chọn save, quit without saving, cancel.

### Acceptance criteria

- Không thể tương tác gameplay trong Loading/Saving theo policy.
- Lỗi ghi file trả UI về state trước, không kẹt timeScale 0.
- Load slot khác reset sạch session cũ.
- Quit/return không tự ghi đè save nếu người chơi chưa xác nhận.

## Phase 10 — Hardening và content-ready milestone

### Mục tiêu

Đưa nền tảng thành trạng thái đủ an toàn để sản xuất content quest/world dài hạn.

### Công việc

- Save migration từ version N sang N+1.
- Automated tests theo Quality Strategy.
- Soak test save/load nhiều vòng và đổi slot liên tục.
- Profiling snapshot size, serialization time và allocations.
- Build test độc lập Editor.
- Recovery UX cho backup/corrupted/incompatible save.
- Tài liệu authoring quest, item, persistent entity và area/spawn ID.

### Acceptance criteria

- Test matrix bắt buộc đều pass.
- Không có P0/P1 known issue trong save, progression hoặc item transaction.
- Content designer có thể tạo Tutorial Quest mới mà không sửa manager core.
- Build player chạy đúng New Game và Continue trên máy sạch.

### Trạng thái: `CONTENT_READY` — 2026-08-23

Cả bốn acceptance criteria trên đã đạt: EditMode 58/58, PlayMode 141/141, Content Validation 0
error, DemoScene/MainMenu validator 0 issue; không có P0/P1 mở; `ContentAuthoringGuide.md` đủ để
designer tạo Tutorial Quest mới không sửa manager core; physical acceptance 24/24 bước PASS trên
Player build Windows64 thật (`New Game → DemoScene → Save (Empty/Overwrite/Save As/Delete) → Return
→ Continue → verify restore → Quit`). Chi tiết đầy đủ:
[Phase10ImplementationReport.md](Phase10ImplementationReport.md). Đây là phase cuối cùng của roadmap
nền tảng này — không có Phase 11 kế tiếp trong tài liệu; bước tiếp theo là sản xuất content thật
(quest/world) theo [ContentAuthoringGuide.md](ContentAuthoringGuide.md), hoặc chốt các decision còn
`Open`/`Proposed` trong `DecisionRegister.md` (D-010, D-013, D-019, ...) nếu muốn mở rộng nền tảng
thêm trước khi sản xuất content quy mô lớn.

## Trình tự bắt buộc

```text
Phase 0
  → Phase 1
  → Phase 2
  → Phase 3
  → Phase 4
  → Phase 5
  → Phase 6
  → Phase 7
  → Phase 8
  → Phase 9
  → Phase 10
```

Phase 5 và thiết kế Quest Definition của Phase 6 có thể nghiên cứu song song sau Phase 3, nhưng không
merge runtime progression trước khi save schema và domain event contract của Phase 4 ổn định.
