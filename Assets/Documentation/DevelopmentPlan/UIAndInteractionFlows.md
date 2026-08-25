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

### Inventory visual direction

- `InventoryUIController.prefab` giữ nguyên navigation, drag/drop, equipment binding và gameplay-state
  contract; reskin không được chuyển ownership sang presentation.
- Inventory dùng cùng ngôn ngữ Light Fantasy với Main Menu: gỗ sồi ấm, nền xanh hoàng gia, viền vàng
  bình minh và pixel edge sắc; label động tiếp tục dùng TMP/Digital Disco.
- Asset thay thế giữ nguyên `RectTransform` để không làm thay đổi layout DemoScene, nhưng dùng texture HD
  theo tỷ lệ trình bày để tránh phóng đại sprite legacy quá nhỏ: Inventory board `768x640`, Equipment
  panel `256x640`, item/equipment slot `96x96`, Close `48x48`.
- Viền Inventory dùng biến thể `thin`: bề dày gỗ/vàng và ornament góc được giảm để ưu tiên vùng nội
  dung, tránh khung tranh chấp thị giác với lưới item/equipment.
- Palette nền Inventory dùng hệ màu ấm thay cho navy: panel chính là parchment tan dịu gần `#CDA77A`,
  lòng slot dùng warm taupe đậm hơn để giữ affordance, và badge Gold dùng caramel đậm để bảo đảm độ
  tương phản với icon coin cùng số vàng; viền gỗ/vàng và điểm nhấn xanh nhỏ vẫn được giữ làm accent.
- `TitleInventory` dùng wordmark sprite HD `INVENTORY` đồng bộ gỗ sồi/xanh hoàng gia/viền vàng với
  Main Menu; title là nội dung cố định, còn mọi label/dữ liệu động vẫn dùng TMP/Digital Disco.
- Cụm `Gold` dùng badge pixel-art xanh hoàng gia, viền gỗ/vàng mảnh nằm sau icon và TMP động; badge
  nới 5 UI unit theo chiều ngang và 4 UI unit theo chiều dọc để currency nổi bật, không chặn raycast.
- `GridScrollView` có inner frame riêng với viền gỗ/vàng mảnh để phân định rõ vùng item; `GridFrame` là
  border-only overlay có lòng alpha trong suốt, không nhận raycast, nằm ngoài `Viewport` và nới 12 UI
  unit ở cả bốn cạnh để không đè lên viền các `GridSlot` ngoài cùng; nền `GridScrollView` và `Viewport`
  đều trong suốt để không còn lớp xanh nằm dưới các slot.
- Source prefab là nguồn chuẩn; DemoScene `_UI/InventoryUIController` giữ prefab connection và không tạo
  scene-only visual override.

### Gameplay Settings visual direction

- DemoScene `_UI/SettingsUI` giữ nguyên `SettingsUI`, `SettingsService`, slider/toggle binding và gameplay
  menu lifecycle; reskin chỉ thay presentation trên scene object hiện có.
- Panel giữ RectTransform `200×300` (asset legacy `100×150`) nhưng dùng texture HD cùng tỉ lệ, nền
  parchment tan đồng bộ Inventory, viền gỗ/vàng và điểm nhấn xanh nhỏ đồng bộ MainMenu.
- Save/Cancel giữ RectTransform `82×30` (asset legacy `41×15`), slider `110×14`, toggle khoảng `26×28`;
  label động vẫn dùng TMP/Digital Disco và control state tiếp tục do Unity UI sở hữu.
- SFX và Music dùng icon sprite HD riêng, nền alpha trong suốt và giữ container `24×24` legacy để nhận
  diện nhanh ở kích thước nhỏ mà không cần label chữ.
- Title gameplay Settings dùng trực tiếp wordmark sprite `settings_title.png` dùng chung với MainMenu;
  TMP title legacy được tắt, còn label và dữ liệu động vẫn giữ TMP/Digital Disco.
- Gameplay Settings dùng safe area nội bộ: slider được hạ khỏi crest/ornament trên, toggle và action button
  cách đều theo trục dọc, Close nằm trong góc phải của board; không object tương tác nào vượt khỏi khung.
- Slider gameplay render theo thứ tự `Fill Area → Background → Handle Slide Area`, để fill nằm dưới track
  và không che viền/background presentation đồng bộ MainMenu.
- `settings_slider_track.png` là border-only overlay `2172×240` với lõi alpha trong suốt; MainMenu và
  gameplay Settings dùng chung asset để Fill phía dưới luôn nhìn thấy xuyên qua lòng track.

### Pause Menu visual direction

- DemoScene `_UI/PauseMenu` giữ nguyên `PauseMenuUI`, gameplay-state navigation, save/load flow và các
  button callback; reskin chỉ thay presentation trên scene object hiện có.
- Pause board giữ RectTransform legacy `164×340`, dùng texture HD portrait cùng tỷ lệ trình bày, nền
  parchment tan, viền gỗ/vàng mảnh, huy hiệu xanh và lá xanh đồng bộ Inventory/Gameplay Settings.
- Các action button giữ bề rộng legacy `139.4`; chiều cao được chuẩn hóa `28` để toàn bộ danh sách nằm
  gọn trong board. Label cố định dùng TMP/Digital Disco thay vì bake chữ vào sprite.
- PauseMenu trình bày Resume/Inventory/Save/Load/Settings/Back to Menu/Exit; Shop và Craft không xuất hiện vì
  hai popup này được mở từ interaction context của NPC/crafting station theo kiến trúc tương tác gameplay.
- Resume/Settings/Inventory/Save/Load/Back to Menu dùng primary button chung với MainMenu; Exit dùng danger button
  đỏ, Close dùng icon thin chung với Inventory. Reskin không thay ownership save/session.
- Các action button PauseMenu dùng `landing_action_button_hover.png` cho pointer Highlighted và
  keyboard/gamepad Selected; sprite state không thay đổi RectTransform hoặc thứ tự layout.

### SessionUX Save/Load overlay visual direction

- `MenuWindow/SessionUX/LoadOverlay` giữ nguyên `PauseMenuUI` slot binding, save/load/delete action,
  confirmation và session ownership; reskin chỉ thay presentation trên DemoScene.
- `LoadPanel` giữ RectTransform `776×430`; ba slot card dùng safe-area `240×320` tại X `-250/0/250`
  để không vượt viền panel. Slot action dùng `190×44`, còn Back giữ hit target `220×62`.
- Board và card dùng parchment tan, viền gỗ/vàng mảnh, accent xanh và lá đồng bộ PauseMenu, Inventory
  và Gameplay Settings. Title mode dùng hai banner ảnh `session_save_title_banner_hd.png` và
  `session_load_title_banner_hd.png`; `PauseMenuUI` đổi sprite theo Save/Load mode, còn TMP legacy chỉ giữ binding.
- Nhãn cố định `SLOT 1–3` dùng trực tiếp `slot_badge_1.png` đến `slot_badge_3.png`; TMP title legacy
  vẫn giữ binding nhưng tắt render, không thay RectTransform card do designer đã tinh chỉnh thủ công.
- Primary/Back dùng button chung MainMenu, Delete dùng danger button đỏ; chỉ pointer Highlighted dùng
  `landing_action_button_hover.png`. Keyboard/gamepad Selected giữ sprite Normal tương ứng để focus mặc
  định không làm button trông như đang được hover. LoadOverlay để `MainMenuButtonHoverVisual` sở hữu đổi
  sprite pointer thay vì `Selectable.SpriteSwap`, tránh Selected của slot đầu ghi đè hover; thay đổi visual
  không tác động callback.
- `ConfirmationPopup` giữ lớp dim RectTransform `800×450`, dùng board riêng `session_confirmation_board_hd.png`
  với panel gọn `580×330`: parchment tan, khung gỗ/vàng mảnh, accent xanh và lá đồng bộ LoadOverlay. Message
  cùng label action tiếp tục là TMP động; Save/Confirm dùng primary xanh, hành động bỏ qua lưu và Cancel dùng
  danger đỏ. Safe area dùng title `460×64`, button `340×48`; layout tự gom lại theo mode hai hoặc ba action
  để không object nào chạm hay vượt viền. Popup reskin không thay confirmation kind, callback hoặc session ownership.

### Tutorial overlay visual direction

- DemoScene `TutorialOverlayRoot` giữ nguyên `TutorialOverlayUI`, `TutorialManager`, step binding và
  Skip callback; reskin chỉ thay presentation, không sở hữu tutorial progression hoặc `Time.timeScale`.
- `InstructionPanel` dùng RectTransform `360×92`, tăng nhẹ chiều cao so với legacy để khung và instruction
  có safe area rõ ràng; board `tutorial_instruction_panel_hd.png` giữ parchment tan, gỗ/vàng mảnh, accent
  xanh nhỏ và lá tiết chế. Header TMP legacy tắt render và được thay bằng wordmark ảnh
  `tutorial_title_banner_hd.png`; instruction vẫn là TMP động. Skip dùng danger đỏ và hover chung MainMenu.
- `SkipConfirmation/Dialog` giữ RectTransform `570×245`, dùng `tutorial_skip_dialog_hd.png` với safe
  area dưới crest cho title/message TMP; Confirm Skip dùng danger đỏ, Keep Playing dùng primary xanh.
  Lớp dim chặn raycast trong lúc xác nhận và không thay đổi tutorial save contract.

### Quest UI visual direction

- DemoScene `QuestUIRoot` giữ nguyên `QuestLogUI`, QuestManager event binding, GameplayMenu lifecycle và
  callback Close; reskin chỉ thay presentation, không sở hữu quest progression hoặc save data.
- `QuestTracker` dùng thẻ dọc `190×230` thay cho thanh ngang legacy để objective dài dễ quét và mang
  hình thái quest journal chuyên nghiệp hơn. Board dùng parchment tan, khung gỗ/vàng mảnh; Header TMP
  legacy tắt render và dùng `quest_title_banner_hd.png`, objective vẫn là TMP động.
- `QuestLogWindow/Window` giữ RectTransform `650×380`; safe area nội bộ dùng `QuestListPanel` `190×255`
  và `QuestDetailPanel` `340×255`, neo giữa board để không tràn đáy hoặc che nhau. Board, panel, close icon
  và row button đồng bộ MainMenu, Inventory, Tutorial và SessionUX; không thay list/template binding.
- Tiêu đề cố định dùng banner ảnh `quest_title_banner_hd.png` với chữ `QUEST`; TMP header legacy tắt
  render. Detail fallback cũng dùng `QUEST`, còn quest title/status/objective tiếp tục là TMP động.

### Commerce UI layout

- DemoScene `CommerceUIRoot` giữ nguyên `ShopCraftingUI`, NPC capability service, transaction callback
  và PlayerInput modal lifecycle; thay đổi layout không chuyển ownership mua/bán/craft sang UI.
- `ShopWindow` và `CraftingWindow` giữ authored RectTransform `1180×680` cùng toàn bộ anchor nội bộ,
  nhưng scale đồng đều `0.58` và neo giữa Canvas `800×450`, cho kích thước trình bày xấp xỉ
  `684×394`. Cách fit này giữ đúng tỉ lệ, typography và hit target tương đối, đồng thời bảo đảm window
  không vượt camera safe area ở reference resolution.

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
