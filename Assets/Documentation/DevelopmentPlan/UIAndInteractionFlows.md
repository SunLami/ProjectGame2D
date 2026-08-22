# UI and Interaction Flows

## Hai hệ menu độc lập

### MainMenu Scene UI

Navigation nội bộ:

```text
Main Landing
├─ New Game → New Game Slot Selection → Confirm Create/Overwrite
├─ Continue → Existing Slot Selection → Confirm Load
├─ Settings → Main Menu Settings
└─ Quit → Confirm Quit
```

Main Menu Settings có thể dùng chung `SettingsService` với Gameplay Settings nhưng không dùng
`GameplayMenuPage.Settings` và không push gameplay state history.

### DemoScene/world scene overlay UI

```text
Playing
├─ Esc → Paused
├─ I → GameplayMenu(Inventory)
└─ Quest key → GameplayMenu(QuestLog)

Paused
├─ Resume → Playing
├─ Inventory → GameplayMenu(Inventory) → back → Paused
├─ Settings → GameplayMenu(Settings) → back → Paused
├─ Save Game → Saving → Paused
├─ Load Game → Load Slot Overlay → Loading
├─ Return Main Menu → Loading → MainMenu (dirty/save confirmation bổ sung ở Phase 9)
└─ Quit Desktop → confirmation flow
```

Popup xác nhận là UI navigation con, không tạo global `GameState` mới.

## Save slot presentation

Mỗi slot hiển thị:

- Empty hoặc character name.
- Level.
- Area display name.
- Total play time.
- Last saved local time.
- Tutorial completed indicator nếu cần.
- Corrupted/incompatible status rõ ràng.

Actions theo context:

| Context | Empty slot | Valid slot | Corrupted slot |
|---|---|---|---|
| New Game | Create | Confirm overwrite | Recover/delete/overwrite |
| Continue | Disabled | Load | Recover/delete |
| In-game Load | Disabled | Load | Recover/delete |

Delete và overwrite luôn có confirm chứa đúng slot/character để giảm thao tác nhầm.

## Input ownership

- MainMenu state: chỉ Main Menu action map.
- Loading/Saving: block gameplay và double-submit; có thể giữ cancel nếu operation hỗ trợ an toàn.
- Playing: gameplay action map.
- Paused/GameplayMenu: gameplay movement/combat bị khóa, UI action map hoạt động.
- Dialogue: movement/combat khóa; dialogue UI nhận confirm/cancel.
- Cutscene: input theo skip policy riêng.

Không chỉ dựa vào `Time.timeScale`. Input policy phải khóa cả callback Input System.

## Back/cancel policy

- Main Menu subpage Back quay về parent page.
- Pause Back/Resume về Playing.
- GameplayMenu mở từ Playing quay về Playing.
- GameplayMenu mở từ Paused quay về Paused.
- Confirm popup Back chỉ đóng popup.
- Loading không cho Back sau khi destructive transition bắt đầu.
- Saving có thể đóng visual overlay sau completion; không pop state hai lần.

State stack và UI navigation stack là hai lớp khác nhau.

## Save feedback

Saving UI cần:

- Spinner/icon và disable action gây ghi/load khác.
- Success feedback ngắn, timestamp cập nhật.
- Error message có mã thân thiện và hành động retry/cancel.
- Không báo thành công trước atomic replace hoàn tất.

Loading UI cần:

- Slot/character đang load.
- Progress theo stage nếu operation đủ dài: reading, scene, restoring, finalizing.
- Lỗi trở về màn trước an toàn.

## Interaction architecture

Player interaction nên qua một `InteractionController` chọn interactable gần nhất. Các interactable
phát intent/domain call:

- NPC dialogue.
- Quest giver.
- Shop.
- Crafting station.
- Resource node.
- Pickup/chest.

UI không tự tìm NPC bằng tag. Interaction context mang stable target ID và capability.

Khi mở Shop/Crafting/Dialogue:

1. Validate player còn trong interaction/range hoặc lock interaction session theo design.
2. Request đúng GameState/GameplayMenuPage.
3. Render read model.
4. Mọi transaction đi qua service.
5. Đóng UI trả state trước.

## Settings reuse

Tách logic khỏi presentation:

```text
SettingsService
├─ Load/Save PlayerPrefs
├─ Audio volumes
├─ Display mode/resolution
└─ Apply current settings

MainMenuSettingsUI → SettingsService
GameplaySettingsUI → SettingsService
```

Hai UI có thể dùng cùng prefab visual, nhưng lifecycle/navigation controller khác nhau.

## Accessibility và controller-ready constraints

- Không hard-code tutorial completion vào key `WASD`, `I` hoặc mouse click; dựa vào action/domain event.
- Slot/menu phải điều hướng được bằng keyboard/gamepad sau này.
- Focus mặc định phải được set khi panel mở.
- Khi panel đóng, focus trở về parent phù hợp.
- Không chỉ dùng màu để biểu thị corrupted/selected/disabled.

## UI acceptance tests

- Spam Esc/I không tạo state history sai hoặc panel chồng.
- Mở Settings từ Paused rồi Back trở về Paused.
- Mở Inventory từ Playing rồi Back trở về Playing.
- MainMenu Settings không làm GameState thành GameplayMenu.
- Double-click Load chỉ tạo một operation.
- Error Save/Load không để `Time.timeScale = 0` sai state.
