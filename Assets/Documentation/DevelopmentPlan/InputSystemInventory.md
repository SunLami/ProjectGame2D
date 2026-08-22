# Input System Action Map Inventory

Ngày kiểm kê: 2026-08-21  
Cập nhật migration: 2026-08-22  
Input asset: `Assets/Settings/InputSystem_Actions.inputactions`

## Kết quả tổng quan

Project có một Input Actions asset với hai map:

| Map hiện tại | Actions | Vai trò hiện tại | Trạng thái đích |
|---|---:|---|---|
| `Gameplay` | 10 | PlayerInput trong DemoScene; chỉ active khi policy cho phép gameplay | Đã migration trong Phase 1 |
| `UI` | 10 | EventSystem của MainMenu và DemoScene | Dùng chung cho MainMenu và gameplay overlays |

Không cần tạo map `MainMenu` riêng chỉ để nhân đôi Navigate/Submit/Cancel. `UI` là map presentation
dùng chung; `Gameplay` là map riêng cho actor. MainMenu chỉ enable UI/application actions và tuyệt đối
không có PlayerInput/gameplay action map.

## Map `Gameplay`

| Action | Type | Binding chính | Call site/wiring hiện tại |
|---|---|---|---|
| Move | Value/Vector2 | WASD, arrows, gamepad stick, joystick, XR | `PlayerMovement.OnMove` |
| Look | Value/Vector2 | pointer delta, gamepad right stick | Không có callback; combat đọc Pointer trực tiếp |
| Attack | Button | mouse left, Enter, gamepad West, touch, joystick, XR | `Player.OnAttack` |
| Interact | Button | E, gamepad North | Chưa có callback |
| Crouch | Button | C, gamepad East | Chưa có callback |
| Jump | Button | Space, gamepad South, XR | Chưa có callback |
| Previous | Button | 1, gamepad d-pad left | Chưa có callback |
| Next | Button | 2, gamepad d-pad right | Chưa có callback |
| Sprint | Button | Left Shift, gamepad stick press, XR | `PlayerMovement.OnSprint` |
| Inventory | Button | I, gamepad Select | `GameInputCoordinator` mở Inventory khi đang `Playing` |

PlayerInput dùng notification behavior `Invoke Unity Events`, default map `Gameplay`, không chốt default
control scheme và không bind `uiInputModule`. Việc không chốt scheme cho phép auto-switch, nhưng cần test
keyboard/mouse và gamepad khi Phase 1 làm UI navigation.

## Map `UI`

Project asset định nghĩa:

- Navigate, Submit, Cancel.
- Point, Click, RightClick, MiddleClick, ScrollWheel.
- TrackedDevicePosition và TrackedDeviceOrientation.
- Keyboard/mouse, gamepad, touch, joystick và XR bindings.

`MainMenu/EventSystem` và `DemoScene/EventSystem` hiện cùng tham chiếu project Input Actions asset.
Không còn scene nào trong build flow dùng package `DefaultInputActions` làm nguồn UI song song.

## Input đọc ngoài action map

| File | Input trực tiếp | Rủi ro |
|---|---|---|
| `PlayerCombat` | `Pointer.current.position` | Mouse/pointer aim nằm ngoài callback `Look`; cần contract aim rõ nếu hỗ trợ gamepad |

`GameInputCoordinator` đã thay polling Esc/I: `Gameplay/Inventory` mở Inventory và `UI/Cancel` xử lý
Back/Pause. Coordinator activate `PlayerInput` chỉ khi `AllowsGameplayInput`; project `Gameplay` map bị
disable trong MainMenu, còn `UI` tiếp tục active cho navigation.

## Control schemes

Asset khai báo năm schemes: Keyboard&Mouse, Gamepad, Touch, Joystick và XR. Gameplay hiện mới được
kiểm chứng thực tế bằng keyboard/mouse. Không tuyên bố hỗ trợ production cho Touch/Joystick/XR chỉ vì
template binding tồn tại.

## Phase 1 migration contract

1. Rename `Player` → `Gameplay` bằng Input Actions editor và kiểm tra lại serialized Unity Events.
2. Thêm action `Inventory` nếu gameplay design giữ phím mở nhanh; dùng `UI/Cancel` hoặc application Back
   flow cho Esc thay vì polling keyboard.
3. Bind EventSystem vào project `UI` actions; không dùng package DefaultInputActions song song.
4. MainMenu scene chỉ có UI navigation; không có PlayerInput component.
5. GameState/input coordinator enable `Gameplay` chỉ khi policy cho gameplay input; `UI` theo UI policy.
6. Không xóa Jump/Crouch/Previous/Next trước khi xác nhận chúng là planned actions hay template dư.

## Verification checklist

- [x] Keyboard/mouse: action wiring Move, Sprint, Attack được giữ theo action ID; Inventory và Back đã
  được mô phỏng bằng Input System (`I`, `Esc`).
- [ ] Gamepad: gameplay và UI Navigate/Submit/Cancel.
- [x] MainMenu không có PlayerInput và project `Gameplay` map bị disable.
- [x] MainMenu/Paused/GameplayMenu khóa PlayerInput; Loading/Saving dùng cùng policy path, cần được
  kiểm chứng lại khi có restore/save operation thật.
- [x] Pause và Inventory vẫn nhận `UI/Cancel`; Settings dùng cùng overlay/state policy.
- [x] Action/map ID cũ được giữ nên tutorial content sau này có thể theo action/event, không theo phím vật lý.
- [x] Không còn direct polling cho Esc/I sau migration.
- [x] PlayerInput Unity Events giữ nguyên action ID/callback và serialized display name đã đổi sang
  `Gameplay/...`.

## Trạng thái Phase 0

Inventory và target boundary đã hoàn tất. Chưa sửa Input Actions asset hoặc scene binding trong Phase 0
để tránh phá serialized events trước khi bootstrap/MainMenu/input coordinator của Phase 1 tồn tại.
