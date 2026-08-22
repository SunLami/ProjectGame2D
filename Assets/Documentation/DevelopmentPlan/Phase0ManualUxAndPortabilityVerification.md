# Phase 0 Manual UX and Portability Verification

Ngày kiểm tra: 2026-08-22  
Unity: 6000.5.4f1

## Quy ước kết quả

- **Pass:** đã thao tác/quan sát được kết quả bắt buộc.
- **Partial:** một phần contract đã được chứng minh, còn acceptance criterion chưa đạt.
- **Blocked:** môi trường kiểm thử không thể tạo input hoặc project chưa có fixture cần thiết.
- **Fail:** thao tác chạy được nhưng kết quả sai hoặc phát sinh lỗi.

Không chuyển `Blocked` hoặc `Partial` thành `Pass` chỉ dựa trên static code review.

## Manual UX — DemoScene

| Luồng | Cách kiểm tra | Kết quả | Ghi chú |
|---|---|---|---|
| Direct Play | Mở `DemoScene`, clear Console, vào Play Mode | **Pass** | Scene khởi động và render gameplay bình thường. |
| Game view focus | Click trực tiếp Game view | **Pass** | Unity báo Game view là focused pane. |
| Move / Sprint | Gửi WASD và Shift qua Windows automation | **Blocked** | Unity Input System không nhận SendInput; không có thay đổi runtime đủ để kết luận. |
| Attack | Mouse/Attack action trong Game view | **Blocked** | Không có input fixture đáng tin cậy để chứng minh animation, hitbox và damage. |
| Inventory hotkey | Gửi `I` khi Game view có focus | **Blocked** | State vẫn `Playing`; injected keyboard input không được Input System nhận. |
| Pause / Resume hotkey | Gửi `Esc` khi Game view có focus | **Blocked** | State vẫn `Playing`; injected keyboard input không được Input System nhận. |
| Equipment interactions | Mở Inventory, equip/unequip item | **Blocked** | Không mở được overlay qua input thật trong phiên automation. |
| Settings | Pause → Settings → Save/Decline | **Blocked** | Chưa đi được đến Settings bằng luồng người dùng thật. |
| Overlay input lock | Thử Move/Attack khi Inventory/Pause mở | **Blocked** | Phụ thuộc các luồng input phía trên. |
| Stress Esc/I | Spam 20 lần và kiểm tra state/history | **Blocked** | Không dùng runtime method invocation thay cho acceptance test bằng input. |

### Kết luận manual UX

Manual UX **chưa pass**. Windows automation điều khiển được chuột/focus Unity Editor nhưng keyboard
injection không được Unity Input System của project nhận. Chủ project hoặc QA cần chạy checklist còn
`Blocked` bằng bàn phím/chuột vật lý trong Editor. Idle soak 5 phút 05 giây trước đó vẫn pass nhưng không
thay thế manual input verification.

## Minimal Scene portability test

Scene: `Assets/Scenes/Tests/Phase0PortabilityTest.unity`

Scene được dựng bằng Unity MCP từ empty scene, không chỉnh YAML và không dùng script để tạo hierarchy.

### Composition

- `_SceneContext`: `EventSystem` + `InputSystemUIInputModule`.
- `_World`: marker root.
- `_Actors`: instance giữ prefab connection của `Slime1.prefab`.
- `_Features`: marker root.
- `_UI/GameplayCanvas`: Canvas, CanvasScaler, GraphicRaycaster.
- Root persistent prefab: `InventoryManager.prefab`.
- UI prefab instances: `InventoryUIController.prefab`, `PauseMenu.prefab`.
- Main Camera và Directional Light.

### Kết quả

| Contract | Kết quả | Bằng chứng / finding |
|---|---|---|
| Scene serialization | **Pass** | Unity scene validator: 0 missing script, 0 broken prefab, 0 issue. |
| Prefab source | **Pass** | Inventory, Pause và Slime đều được instantiate từ prefab asset, không copy unpacked object. |
| Empty-scene boot | **Pass** | `GameStateManager` tự bootstrap về `Playing`. |
| Inventory service lifetime | **Partial** | Khi đặt dưới `_Features`, `DontDestroyOnLoad` báo Error; prefab phải ở scene root với implementation hiện tại. Chuyển composition về root thì smoke sạch. |
| Inventory UI state | **Pass** | `OpenWindow` chuyển state thành `Menu/Inventory`, `Time.timeScale = 0`, UI hiển thị ngoài DemoScene. |
| Pause state | **Pass** | `OpenMenu` chuyển state thành `Paused`, `Time.timeScale = 0`, UI hiển thị ngoài DemoScene. |
| UI sizing | **Fail** | Inventory/Pause hiển thị quá nhỏ trong Canvas minimal; prefab đang mang scene-specific RectTransform/scale. |
| UI pointer/input | **Partial** | Có Canvas/EventSystem nhưng click Resume chưa chứng minh được callback; project UI action binding vẫn là việc Phase 1. |
| Enemy prefab runtime | **Pass** | `Slime1` render/animate/move ngoài DemoScene, không phát exception trong smoke. |
| Console smoke | **Pass** | Sau khi sửa composition lifetime: 30 giây Play Mode, 0 Error/0 Warning từ Unity MCP Console. |
| Hard-coded DemoScene lookup | **Pass (static scope)** | Không tìm thấy `DemoScene`/`Find(...)` trong các script Inventory, UI, Enemy và GameManagers được quét. |
| Player portability | **Blocked** | Project chưa có Player prefab; không được copy scene-only Player sang minimal scene để giả định drag-and-drop readiness. |
| Settings portability | **Blocked** | Project chưa có Settings feature prefab/composition root độc lập. |

### Findings phải chuyển sang phase sau

1. Ghi rõ `InventoryManager` là application/persistent root hoặc bỏ `DontDestroyOnLoad` khi ownership
   được migration; không được đặt component hiện tại dưới grouping root.
2. Chuẩn hóa UI prefab RectTransform/anchors/scale để không phụ thuộc Canvas của DemoScene.
3. Bind minimal/production EventSystem vào project `UI` action map theo Phase 1 contract.
4. Tạo Player feature prefab/composition root trước khi tuyên bố Player có thể kéo thả sang scene khác.
5. Tạo Settings prefab hoặc Gameplay UI composition root chứa Settings với dependency được document.

## Phase 0 gate

Minimal portability scene đã tồn tại và smoke runtime cơ bản pass, nhưng Phase 0 **chưa đóng** vì manual
keyboard/mouse UX còn Blocked và portability của Player/Settings/UI interaction chưa đạt. Scene test không
được thêm vào Build Settings; `DemoScene` vẫn là build index 0 tạm thời.
