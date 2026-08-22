# Phase 0 Baseline Report

Ngày rà soát: 2026-08-21  
Unity: 6000.5.4f1  
Scene integration: `Assets/Scenes/DemoScene.unity`

Tài liệu này ghi lại baseline đã kiểm chứng, không tuyên bố Phase 0 hoàn tất. Những acceptance criteria
phụ thuộc `MainMenu`, manual UX và portability của Player/Settings vẫn phải được hoàn thành ở các bước tiếp theo.

## Kết quả đã kiểm chứng

- `DemoScene` mở, được load và chạy trực tiếp trong Editor.
- Unity scene validator báo 0 missing script, 0 broken prefab và 0 scene issue.
- Các reference chính của Inventory, Pause Menu và Settings đều được gán.
- Player có Input Actions và gameplay action map; camera, light, EventSystem và world tilemap tồn tại.
- Input maps và live binding đã được kiểm kê tại [Input System Inventory](InputSystemInventory.md).
- Custom ScriptableObject/catalog/stable ID đã được kiểm kê tại [Data Asset and Stable ID Inventory](DataAssetStableIdInventory.md).
- Content validator đã compile và chạy trong Unity: 0 Error, 60 legacy-ID Warning, 63 assets checked.
- Editor compile baseline hiện 0 Error/0 Warning; override `CanCacheInspectorGUI` obsolete đã được loại bỏ.
- DemoScene idle soak 5 phút 05 giây trong Play Mode: 0 Error/0 Warning.
- Windows x64 development player build thành công và khởi chạy smoke 20 giây; không có gameplay exception.
- Minimal portability scene đã được dựng bằng Unity MCP và smoke sạch 30 giây sau khi sửa composition
  lifetime; chi tiết pass/partial/blocked nằm tại
  [Phase 0 Manual UX and Portability Verification](Phase0ManualUxAndPortabilityVerification.md).
- Enemy mẫu `Slime1`, `Slime2`, `Goblin` là prefab instances.
- Build Settings không còn trỏ tới `SampleScene.unity` đã bị xóa; tạm thời dùng `DemoScene` ở build index 0.
- Runtime scripts không còn import namespace editor-only/không dùng ở `SoundFXManager` và `MapManager`.

## Inventory scene hiện tại

### Gameplay và world

- `Player`: input, movement/combat/stat, collider, animation và attack children.
- `Grid/MainTileMap`: world tilemap hiện tại.
- `MapManager`: bind tilemap trực tiếp; tìm Player lúc `Awake` nếu reference chưa được serialize.
- `Slime1`, `Slime2`, `Goblin`: enemy integration samples.

### UI overlay trong GameScene

- `UICanvas`: equipment panels, inventory, pause và settings.
- `EventSystem`: UI input/event routing.
- `InventoryUIController`: đã bind `InventoryWindow`.
- `PauseMenu`: đã bind `MenuWindow` và Inventory UI.
- `SettingsUI`: đã bind window, SFX/music sliders, display toggles và sprites.

Các UI trên là gameplay overlay, không phải Main Menu Scene.

### Runtime services hiện diện

- `InventoryManager` + `InventorySeeder`.
- `Equipment Manager`.
- `SoundFX Manager`.
- `MusicManager`.
- `MapManager`.

Baseline hiện tại ghi nhận `InventoryManager`, `EquipmentManager`, `SoundFXManager`, `MapManager`,
`MusicManager` và `Player` đang gọi `DontDestroyOnLoad`. Vì Unity yêu cầu các object này nằm ở scene
root, chúng chưa được ép vào các grouping root. Phase 1 phải phân loại lại ownership trước khi thay đổi
lifecycle; đặc biệt `MapManager` đang giữ scene references nên không phù hợp làm application service
theo kiến trúc đích.

## Hierarchy đã chuẩn hóa

Các grouping root sau đã được tạo và lưu bằng Unity Editor:

- `_SceneContext`: composition marker; chưa gắn installer/registry giả khi contract chưa tồn tại.
- `_World`: Main Camera, Global Light 2D và Grid/Tilemap.
- `_Actors`: enemy test instances; Player tạm ở root do lifecycle hiện tại.
- `_Features`: marker cho feature roots tái sử dụng; persistent manager hiện tại tạm ở root.
- `_UI`: gameplay UICanvas và EventSystem.

Grouping root có transform mặc định `(0, 0, 0)`, rotation zero và scale one. Việc reparent giữ nguyên
world transform và serialized references. Convention hierarchy không được dùng làm runtime lookup.

## Baseline conventions

- `DemoScene` tiếp tục là integration playground, không đổi tên thành GameScene.
- Build index 0 dùng `DemoScene` chỉ là cấu hình tạm thời trước khi có `MainMenu.unity`.
- Feature mới phải hướng tới prefab/installer có dependency rõ; không phụ thuộc cứng tên DemoScene.
- Các manager rời rạc hiện tại được ghi nhận như technical debt, không refactor hàng loạt trong Phase 0.
- Definition asset, runtime state và save DTO phải tách biệt theo Data-Driven Development Guide.

## Việc còn lại trước khi đóng Phase 0

- Tạo `MainMenu.unity` và mock New/Continue vào DemoScene; sau đó đặt MainMenu ở build index 0.
- Chốt `_SceneContext`, feature prefab root và cách nhận diện demo-only debug tools bằng implementation thật.
- Áp dụng migration ownership theo [Service Ownership and Lifecycle](ServiceOwnershipLifecycle.md) ở đúng phase; bảng kiểm kê đã hoàn tất.
- Áp dụng stable ID migration/validator theo inventory đã chốt trước khi tạo save schema production.
- Phase 1 bind EventSystem vào project `UI` map và đổi `Player` map thành `Gameplay` có migration serialized binding.
- Chủ project/QA chạy manual keyboard/mouse qua Player/Combat/Inventory/Equipment/Pause/Settings;
  Windows automation không được Unity Input System nhận nên các mục này vẫn Blocked.
- Hoàn thiện portability của Player/Settings và UI input/sizing. Minimal scene boot/service/enemy smoke đã đạt,
  nhưng project chưa có Player prefab và UI prefab còn mang layout phụ thuộc DemoScene.
- Tạo git snapshot/commit baseline theo quy trình version control của dự án.

## Rủi ro đã ghi nhận, chưa sửa lớn

- `InventorySeeder` vẫn nằm trong DemoScene; cần policy rõ để không seed đè dữ liệu khi về sau restore save.
- Một số manager dùng singleton tĩnh nhưng ownership/lifecycle chưa thống nhất.
- Bảng ownership/lifecycle đã xác định hướng đích; code hiện tại chưa được migration để tránh refactor lớn trong Phase 0.
- `MapManager` còn fallback tìm Player trong scene; Phase 1/feature packaging cần explicit binding.
- Music source chưa có clip baseline; đây là content wiring, không phải lỗi runtime chặn Phase 0.
- `InventoryManager` hiện gọi `DontDestroyOnLoad`, vì vậy instance phải ở scene root; đặt prefab dưới
  `_Features` tạo Unity Error. Ownership migration phải giải quyết contract này thay vì chỉ reparent.
- Inventory/Pause prefab hiển thị quá nhỏ dưới Canvas minimal, cho thấy RectTransform/scale còn scene-specific.

Các mục trên được hoãn có chủ đích để tránh biến Phase 0 thành refactor feature lớn.

## Verification record — 2026-08-21

### Editor compile

- Unity script validation: 0 diagnostics cho Editor drawer đã sửa.
- Unity compile: 0 Error, 0 Warning.
- Thay đổi duy nhất là bỏ override API `PropertyDrawer.CanCacheInspectorGUI` mà Unity 6 xác nhận đã
  deprecated và không còn được sử dụng; behavior drawer động vẫn do `OnGUI`/`GetPropertyHeight` sở hữu.

### Play Mode soak

- Scene: `DemoScene` direct play.
- Thời lượng: 5 phút 05 giây.
- Console được kiểm tra định kỳ trong suốt soak.
- Kết quả: 0 Error, 0 Warning, không có exception lặp lại.
- Phạm vi: idle integration/lifecycle soak. Chưa đánh dấu pass cho manual movement/combat/overlay stress.

### Windows player build smoke

- Target: `StandaloneWindows64`, Development Build.
- Scene: `Assets/Scenes/DemoScene.unity`.
- Output local ignored: `Builds/Phase0Smoke/ProjectGame2D.exe`.
- Build: succeeded trong 268,61 giây; report 581,31 MB.
- Output thực tế: 323 files, 581,55 MB.
- Runtime launch: 20 giây ở windowed mode, sau đó test harness dừng process chủ động.
- `Player.log`: không có NullReference/MissingReference/UnassignedReference/assert/crash/gameplay exception.
- Có một `Curl error 35: Recv failure: Connection was reset` từ development player connection;
  không kèm gameplay failure và không xuất hiện trong Editor soak. Release build về sau phải xác nhận lại.

Build output nằm dưới thư mục đã được `.gitignore` loại trừ và không phải source artifact cần commit.
