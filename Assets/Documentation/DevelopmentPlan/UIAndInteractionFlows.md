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

MainMenu dùng video pixel-art 16:9 làm background presentation: phát không tiếng, loop liên tục và
cover theo aspect ratio màn hình. Ảnh keyframe tĩnh được giữ làm fallback cho tới khi frame video đầu
tiên sẵn sàng hoặc khi thiết bị không phát được video; background không nhận raycast và không sở hữu
navigation/state.

### MainMenu visual direction

- Art direction là **Light Fantasy bình minh**, kể khoảnh khắc nhân vật rời cổng làng để bắt đầu hành
  trình; không dùng palette đêm/gothic cho landing page.
- Static fallback chuẩn hiện tại là `mainmenu_new_journey_dawn_v8.png`; video loop phải giữ cùng bố
  cục để UI bên trái không bị tranh chấp thị giác.
- Logo, landing board và button dùng chung ngôn ngữ vật liệu: gỗ sồi ấm, vải/xanh hoàng gia, viền vàng
  bình minh và pixel edge sắc. Button label vẫn là TMP/Digital Disco, không bake chữ vào sprite nền;
  slogan landing là wordmark sprite có outline riêng để giữ độ tương phản trên video sáng.
- Cụm landing được anchor theo 25% chiều ngang Canvas để giữ vùng trái ổn định khi đổi aspect ratio;
  các background graphic không nhận raycast.
- Landing button giữ sprite xanh dương ở Normal/keyboard-selected; chỉ pointer hover mới đổi sang sprite
  xanh lá, và phải khôi phục xanh dương khi pointer rời nút hoặc UI bị disable.
- `SlotPage` dùng thẻ hồ sơ dọc đồng bộ landing: khung gỗ sồi, nẹp vàng, nền xanh hoàng gia và huy hiệu
  ở đầu thẻ. Metadata save vẫn là TMP/Digital Disco để dữ liệu động không bị bake vào asset; primary/Back
  dùng button xanh, còn Delete dùng cùng hình học với palette đỏ cảnh báo. Reskin không thay đổi binding,
  confirm flow hoặc save-slot contract.
- Tiêu đề mode của `SlotPage` (`NEW GAME`/`CONTINUE`) và nhãn cố định `SLOT 1–3` dùng wordmark/badge
  sprite để khóa căn chỉnh và art direction; status, metadata và action label vẫn dùng TMP vì là dữ liệu động.
- `SettingsPage` dùng settings board gỗ sồi/xanh hoàng gia/viền vàng và title wordmark sprite; slider,
  toggle và Save/Cancel giữ component tương tác Unity nhưng presentation dùng palette xanh–vàng và button
  sprite đồng bộ MainMenu. Reskin không chuyển ownership ra khỏi `SettingsService`.
- SFX và Music dùng chung slider track/handle sprite để hình học, hit target và feedback nhất quán; giá trị
  runtime vẫn do `UnityEngine.UI.Slider` và `SettingsService` sở hữu.
- Fullscreen dùng cặp checkbox sprite unchecked/checked cùng hình học; `UnityEngine.UI.Toggle` sở hữu việc
  bật/tắt checkmark và tiếp tục gửi giá trị vào `SettingsService`.
- `ConfirmOverlay` và `ErrorOverlay` dùng dialog board cùng bộ gỗ sồi/xanh hoàng gia/viền vàng; message
  vẫn là TMP vì thay đổi theo thao tác. Confirm/Close dùng button xanh, Cancel dùng button đỏ cảnh báo.
- `LoadingOverlay` khóa input và hiển thị thanh tiến trình responsive neo từ 8% đến 92% chiều rộng Canvas.
  Fill chạy trái→phải theo `AsyncOperation.progress` của `SceneFlowService` (chuẩn hóa dải Unity 0..0.9),
  kèm phần trăm 0–100%. Scene activation được giữ lại cho tới khi UI đã render 100% và feedback hoàn tất
  ngắn; gameplay restore tiếp tục do `GameplayReadinessGate` sở hữu sau khi scene được load.
  Frame là sprite viền siêu ngang có lòng alpha trong suốt; progress dùng sprite fill riêng và được render
  phía trên/lọt trong viền, tránh trường hợp nền đặc của frame che mất hiệu ứng fill.
  Khi Loading bắt đầu, Landing/Slot/Settings và các Confirm/Error popup đều được ẩn; chỉ background cùng
  LoadingOverlay được giữ lại. Nếu transition thất bại, page đã khởi tạo thao tác được khôi phục.

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
├─ Save Game → Save Slot Overlay → (Empty: Saving trực tiếp | Valid/Corrupted/IncompatibleVersion:
│  Confirm Overwrite → Saving) → Paused (Phase 10: chọn slot, không tự ghi vào ActiveSlotId)
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
