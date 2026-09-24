# UI and Interaction Flows

## Runtime cursor presentation

- `GameCursorManager` is the single owner of `Cursor.SetCursor`; gameplay/UI components do not set cursor textures directly.
- The manager loads the eight 64x64 textures from `Resources/UI/Cursors` and restores `Default` over UI, menus, missing cameras and empty world space.
- While `GameState.Playing`, a 2D ray under the mouse resolves existing components without changing their interaction ownership: living Enemy -> Attack; quest/commerce NPC -> Talk; chest/unique pickup -> Interact; ResourceNode -> its authored Mining/Chopping/Gathering type.
- Proximity-gated world interactions show Blocked when unavailable or farther than 2.5 world units. Attack remains a target-identification cursor and is not coupled to melee range.
- `ResourceNodeInteractable` exposes its `ResourceNodeDefinition.HarvestType` to cursor presentation. Gathering cursor also owns the explicit left-click handoff to the node; reward, cooldown and persistence remain owned by the world/inventory systems.
- All cursor textures use genuine alpha, point toward the upper-left and keep per-texture hotspots documented in `Assets/Resources/UI/Cursors/README.md`.

### Resource harvesting flow

- Mining and Chopping nodes receive `harvestDamage` from each valid attack. Combat damage is not reused.
- Gathering requires hover in range, then left click. The node locks repeated input, blinks for its authored 1–1.5 second duration and completes without interruption or auto-walking.
- Completion rolls every loot-table entry, checks the entire batch against inventory capacity, hides the node, presents the drop/fly animation, then commits the batch when it reaches Player.
- A capacity failure leaves the node available and grants nothing. A completed node records `nextRespawnUtcTicks`, disappears, and becomes available after its UTC cooldown.

### NPC world interaction

- NPC interaction is mouse-first: hover an NPC collider in range to show Talk, then left-click the NPC.
- NPC components do not subscribe to `Gameplay/Interact`; keyboard E does not start NPC interaction.
- Hover outside the 2.5-unit interaction range shows Blocked. Clicking while blocked does nothing and never auto-moves Player.
- The current quest NPC capability receives this click; the production dialogue router will later become the single handoff point for Quest, Shop and Crafting NPC capabilities.

### Persistent chest interaction

- Chests use the Interact cursor and left-click while in range; `Gameplay/Interact`/keyboard E does
  not open a chest.
- A valid click first verifies the complete reward fits, then plays the authored four-frame opening
  sequence. At the end of frame four, loot drops into the world, flies to Player, and only then commits
  to Inventory and marks the persistent chest opened.
- If capacity becomes unavailable before commit, the chest returns to its closed frame, remains
  retryable, grants nothing, and does not change its persistent state.

### Resource-node depletion presentation

- Authored resource nodes may provide stable available/depleted sprite pairs. Successful harvest
  changes to the depleted sprite during the UTC cooldown instead of hiding the GameObject.
- When the cooldown expires or restore finds the node available, presentation returns to the
  available sprite. Availability, loot transaction and save state remain owned by
  `ResourceNodeInteractable`; sprites are presentation only.
- Resource loot presentation and Inventory slots both read the same `ItemSO.icon`; authored material
  icons therefore remain visually identical while dropping, flying to Player and after Inventory commit.
- Demo resource presentation references named sub-sprites from the original `Objects.png` and
  `Icons.png` atlases. Do not create standalone duplicate PNGs for individual node states or item icons.

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

### Player Health/Stamina HUD visual direction

- `PlayerHUD.prefab` là nguồn chuẩn cho HUD gameplay, neo top-left theo Canvas và không phụ thuộc tên
  hierarchy của DemoScene; DemoScene chỉ giữ prefab instance để integration.
- Theo D-029, `PlayerHUD.prefab` là HUD trạng thái bổ sung và không thay thế unified HUD đáy màn hình.
  HUD này có avatar tròn mặc định, Health đỏ, Stamina xanh lá và Level TMP. Avatar là sprite presentation
  có thể được thay qua `PlayerHUDController.SetAvatar`; luồng upload/chọn ảnh, kiểm tra file và persistence
  chỉ triển khai sau khi contract avatar được chốt.
- HUD chỉ có hai tài nguyên: Health đỏ và Stamina xanh lá. Khung pixel-art gỗ sồi/viền vàng/đá xanh
  dùng texture alpha rỗng; fill là `UnityEngine.UI.Image` riêng, giảm bằng `fillAmount` với origin trái để
  mép phải rút dần về trái.
- Health đọc `PlayerStat.OnHealthChanged`. Stamina là runtime resource: hao liên tục khi sprint thực sự
  đang di chuyển và hao một lần khi bắt đầu attack; walk không tiêu hao và attack bị chặn nếu không đủ
  chi phí. Stamina hồi khi không sprint và không được thêm vào save slot.
- HUD không sở hữu gameplay stat, input hoặc save; controller chỉ subscribe và trình bày giá trị normalized.
- Badge Level nằm dưới cụm icon, dùng khung sprite rỗng và `TMP/Digital Disco` cho chuỗi động `LV. <level>`;
  controller cập nhật từ `PlayerStat.OnLevelUp`, không bake số level vào texture.
- Frame HUD là overlay có alpha thật ở lõi hai track. `HealthFill` và `StaminaFill` dùng sprite riêng có
  pixel highlight/shadow, render dưới Frame và tiếp tục giảm bằng `Image.fillAmount` origin trái.

### Character Popup visual direction

- Trong `GameplayUIRoot.prefab`, `PlayerHUD` và `UnifiedGameplayHUD` phải đứng trước các gameplay
  overlay/menu trong sibling order. Vì dùng chung một Canvas, thứ tự này bảo đảm Inventory, Quest Log,
  Settings và các confirmation panel luôn render phủ lên toàn bộ HUD khi mở.
- `UnifiedGameplayHUD.prefab/CharacterPopup` là popup Character chuẩn của gameplay, mở/tắt bằng phím
  `C` hoặc `BottomHUD/StatButton`; lifecycle tiếp tục đi qua `GameStateManager` với
  `GameplayMenuPage.Character`, không tự pause world hoặc sở hữu input gameplay.
- Bên trái sao chép nguyên presentation/layout của `InventoryUIController.prefab/InventoryWindow/EquipmentPanel`
  (frame, parchment, ornament, silhouette và tọa độ slot), rồi scale đồng đều để vừa CharacterPopup.
  Bảy slot Head, Weapon, Body, Shield, Necklace, Ring, Foot vẫn ở chế độ chỉ đọc: các slot chỉ giữ
  `Image` để hiển thị item từ `EquipmentManager`; không giữ `EquipmentSlotUI`, click, drag/drop hoặc
  unequip callback.
- Bên phải là bảng Character Stats chia nhóm Vitals, Combat, Mobility và Recovery. Label căn trái,
  value căn phải, header xanh và dữ liệu động dùng TMP/Digital Disco; dữ liệu đọc từ `PlayerStat` và
  tự refresh khi stat/equipment thay đổi.
- Popup dùng dim overlay chặn raycast phía sau, Close button trả về state trước. Prefab là nguồn chuẩn;
  DemoScene chỉ giữ prefab instance và không có visual override riêng cho popup.

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
- Inventory dùng cùng ngôn ngữ Light Fantasy tối của PlayerHUD/UnifiedGameplayHUD/QuestTracker: gỗ
  walnut sẫm, lòng panel charcoal, viền vàng cổ, sapphire xanh và lá xanh tiết chế; label động tiếp
  tục dùng TMP/Digital Disco.
- Inventory presentation dùng một unified board: character/equipment/stats well bên trái, item grid và
  currency footer bên phải, nối bằng một divider chung. `InventoryCharacterPreviewUI` dùng camera phụ
  render chính Player runtime trong Hierarchy vào RenderTexture; Animator tạm chạy unscaled khi cửa sổ
  mở nên idle và appearance/equipment hiện tại được phản ánh trực tiếp. Preview không sở hữu equipment state.
- Unified board v5 dùng tỷ lệ landscape gần ảnh tham chiếu: cửa sổ runtime `600x335`, character/equipment
  well trái với đúng bảy socket (Head/Body/Foot và Weapon/Shield/Necklace/Ring), grid `6x5` nhìn thấy bên phải,
  stat well sạch và đúng một currency well Gold. Item/equipment cùng dùng slot v4 phẳng, viền vàng mảnh,
  lòng warm taupe và safe area icon 76%; không còn frame cam dày hoặc gem lớn lặp ở từng cell.
- Viền Inventory dùng biến thể `thin`: bề dày gỗ/vàng và ornament góc được giảm để ưu tiên vùng nội
  dung, tránh khung tranh chấp thị giác với lưới item/equipment.
- Palette nền Inventory chuyển sang charcoal/walnut tối; lòng slot giữ warm taupe để duy trì
  affordance và badge Gold tiếp tục bảo đảm tương phản với icon coin cùng số vàng.
- `TitleInventory` dùng wordmark sprite HD `INVENTORY` đồng bộ gỗ sồi/xanh hoàng gia/viền vàng với
  Main Menu; title là nội dung cố định, còn mọi label/dữ liệu động vẫn dùng TMP/Digital Disco.
- Footer board v5 chỉ có một currency well tương ứng Gold hiện tại. `CurrencyRow` căn giữa và chỉ chứa
  các currency đang được hệ thống cung cấp; hiện tại chỉ bật icon và TMP Gold động. Currency mới phải thêm một
  entry runtime/prefab tương ứng thì horizontal layout mới sinh thêm ô, không hiển thị placeholder rỗng.
- `GridScrollView` dùng vùng inset đã có trên unified board; grid runtime là 6 cột, cell `41x41`, spacing
  `4x4`, vẫn scroll cho phần slot vượt quá năm hàng nhìn thấy. Board có một opaque backdrop riêng để
  alpha trang trí không làm lộ scene bên dưới; backdrop luôn đứng sau board sau mọi lần chạy builder.
- Khối stats dưới character well là dữ liệu TMP runtime (`InventoryStatsUI`), bố trí 2 cột x 3 hàng với
  icon nhỏ và các giá trị HP/ATK/DEF/SPD/CRIT/STA; cập nhật từ `PlayerStat` và không sở hữu gameplay data.
- Mỗi equipment slot trống hiển thị silhouette tối theo đúng loại item; hint nằm trong slot runtime,
  tự ẩn khi có equipment thật và hiện lại khi tháo item, không bake vào board hoặc thay equipment state.
- Source prefab là nguồn chuẩn; DemoScene `_UI/InventoryUIController` giữ prefab connection và không tạo
  scene-only visual override.
- Mỗi `InventorySlotUI` hiển thị tooltip khi hover item và ẩn khi pointer rời slot hoặc bắt đầu drag.
  Tooltip là hierarchy thật trong `InventoryUIController.prefab`, đọc dữ liệu động từ `ItemSO` và
  `EquipmentItemSO`, nằm dưới cursor, theo `PointerMove` và tự lật/clamp theo Canvas để không tràn màn
  hình 1920×1080; asset ảnh không
  bake tên, icon, stat hoặc description. Tooltip dùng panel charcoal bán trong suốt, viền vàng mảnh
  và tối đa một sapphire nhỏ; không dùng crest/lá/ornament lớn vì chỉ là hover feedback tạm thời.

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
- `QuestTracker` là HUD không nền, không board và không lớp dim để không che gameplay. Vùng logic
  `190×230` tương ứng `456×552` physical pixel ở target `1920×1080`; icon nguồn là `384×384` và được
  thu nhỏ khi render để giữ cạnh sắc. Header gồm chevron button, icon cuộn quest, chữ `QUESTS` TMP và
  đường vàng. Nhấn toàn bộ header sẽ mở/đóng danh sách; chevron đổi hướng theo trạng thái.
- `Assets/Prefabs/UI/QuestTracker.prefab` là nguồn authoring: Header, Chevron, QuestIcon, GoldDivider,
  Viewport, Content và QuestRowTemplate phải tồn tại thành GameObject thật để designer chỉnh vị trí,
  kích thước và sprite trực tiếp trong Prefab Mode. Runtime chỉ clone RowTemplate và bind dữ liệu động;
  Bootstrap nhận thay đổi qua nested prefab trong `GameplayUIRoot.prefab`, không tạo scene-only override.
- Tracker chỉ hiển thị quest `Active`/`ReadyToTurnIn` đang được người chơi chọn Track thành danh sách dọc trong `ScrollRect` có mask,
  cuộn bằng mouse wheel khi nội dung vượt chiều cao. Thứ tự presentation bắt buộc là Main Quest → Side
  Quest → Daily Quest; trong cùng loại sắp theo display name. Mỗi quest hiển thị icon category và
  objective hiện tại hiển thị icon theo loại hành động, progress căn phải hoặc trạng thái `READY` màu
  vàng. QuestLogWindow tiếp tục sở hữu phần trình bày đầy đủ của quest.
- Trong Editor/Development Build, ba nút `TRACK MAIN QUEST`, `TRACK SIDE QUEST`, `TRACK DAILY QUEST`
  nằm ngay dưới `DEBUG LEVEL +1` và accept ba QuestDefinition `quest.debug.*` qua QuestManager để kiểm
  thử đồng thời Tracker/QuestLog. Debug quest không được persist hoặc làm session dirty và không nhận
  được trong non-development Player build.
- `QuestLogWindow/Window` dùng RectTransform `560×340` trên Canvas reference `800×600`, tương đương
  khoảng `1344×816` physical pixel ở target `1920×1080`. Safe area chia `QuestListPanel` `190×270`
  và `QuestDetailPanel` `330×270`; window nằm giữa nhưng vẫn để lộ gameplay quanh bốn cạnh.
- `QuestListPanel` dùng `ScrollRect` dọc với `Viewport + RectMask2D`, `Content + ContentSizeFitter`
  và scrollbar lane cố định phía phải. Row cao `43`, có category icon, title, status, category badge
  và tracked-pin; scrollbar không được chồng lên nội dung row.
- `QuestDetailPanel` có category icon/label, objective icon + tiến độ động, reward summary và action
  buttons. Header `QUEST LOG`, close button, filter, row, reward và action đều là GameObject thật trong
  `GameplayUIRoot.prefab` để designer chỉnh trực tiếp trong Prefab Mode; runtime chỉ bind dữ liệu.
- Mockup scroll lưu tại `Assets/Documentation/DevelopmentPlan/quest_log_window_scroll_mockup_v2.png`;
  board production là `Assets/Resources/UI/Quest/QuestLog1920/quest_log_board_dynamic_actions_v3.png`
  và atlas không bake text nằm tại `Assets/Resources/UI/Quest/QuestLog1920/quest_log_atlas_source.png`.
  Board không bake action-button frame; `TrackQuestButton` và `AbandonQuestButton` sở hữu sprite
  `quest_log_action_button.png`, nên khi runtime ẩn Button thì cả viền và text cùng biến mất.
- Detail panel có `TRACK QUEST`/`UNTRACK QUEST` và `ABANDON QUEST`. Abandon luôn qua confirmation,
  reset toàn bộ tiến độ và nhắc người chơi quay lại đúng giver NPC để nhận lại. Quest không có
  `giverNpcId` không được abandon để tránh trạng thái progression không thể phục hồi.
- `QuestAcceptPopup` là popup dọc giữa màn hình, RectTransform `320×400` trên Canvas reference
  `800×600`, tương đương khoảng `768×960` physical pixel ở target `1920×1080`. Board dùng cùng gỗ
  sẫm, viền vàng, sapphire xanh và lá xanh của PlayerHUD/UnifiedGameplayHUD/QuestTracker; gameplay
  vẫn lộ rõ quanh popup và overlay chỉ dim nhẹ để giữ focus.
- Header, category icon, quest title, `OBJECTIVES`, objective rows, `REWARDS`, reward slots và hai
  button `ACCEPT`/`DECLINE` đều là GameObject thật trong `Assets/Prefabs/UI/QuestAcceptPopup.prefab`
  để designer chỉnh bằng Prefab Mode. Asset board không bake text hoặc dữ liệu quest; runtime bind
  category/objective/reward icon, progress, Gold và EXP mà không sở hữu progression hay save data.
- Board production của popup nằm tại
  `Assets/Resources/UI/Quest/QuestAccept1920/quest_accept_board_dynamic_rewards_v2.png`. Board không
  bake reward socket; runtime chỉ tạo đúng số item reward thực tế, căn giữa và co slot khi danh sách
  dài. Vùng objective được chừa chiều cao cho nhiều dòng; quest ít objective không được tự kéo reward
  lên vì sẽ làm thay đổi nhịp bố cục giữa các quest.

### Dialogue UI visual direction

- Dialogue dùng bộ module tại `Resources/UI/Dialogue/LightFantasy`: khung hội thoại đáy màn hình có
  portrait aperture bên trái, vùng text bên phải, nameplate rời, choice button rời và continue indicator.
- Art direction giữ chung hệ Light Fantasy hiện tại: gỗ sồi ấm, parchment sáng, viền vàng cổ, lá xanh
  tiết chế và sapphire xanh. Asset ảnh không bake tên NPC, nội dung hoặc lựa chọn.
- Tên NPC, nội dung thoại và choice label luôn là TMP động với `DigitalDisco SDF v3`; hover/pressed/
  disabled của choice dùng Unity `Button.colors` trên cùng sprite để không nhân bản texture không cần thiết.
- Dialogue presentation không sở hữu quest outcome, shop/crafting transaction hoặc save data. Router/capability
  service cung cấp read model; UI chỉ render và phát intent lựa chọn/continue/cancel.
- Ở reference Canvas `800x450`, frame neo bottom-center khoảng `700x190`, chừa bottom inset 42; text
  và portrait nằm trong safe area của asset, continue indicator chỉ pulse khi page hiện tại reveal xong.
- Khi Dialogue mở, scene binding ẩn gameplay `UICanvas` hiện tại và ghi nhớ `activeSelf`; khi đóng hoặc
  Dialogue bị destroy, trạng thái trước đó được khôi phục chính xác. `EventSystem` và Dialogue Canvas
  riêng vẫn hoạt động, nên trên màn hình chỉ còn Dialogue UI mà không làm mất lifecycle của HUD.

### Commerce UI layout

- DemoScene `CommerceUIRoot` giữ nguyên `ShopCraftingUI`, NPC capability service, transaction callback
  và PlayerInput modal lifecycle; thay đổi layout không chuyển ownership mua/bán/craft sang UI.
- `ShopWindow` và `CraftingWindow` giữ authored RectTransform `1180×680` cùng toàn bộ anchor nội bộ,
  nhưng scale đồng đều `0.58` và neo giữa Canvas `800×450`, cho kích thước trình bày xấp xỉ
  `684×394`. Cách fit này giữ đúng tỉ lệ, typography và hit target tương đối, đồng thời bảo đảm window
  không vượt camera safe area ở reference resolution.
- `ShopWindow` và `CraftingWindow` dùng chung `commerce_window_board_hd.png`: parchment tan, gỗ sồi,
  viền vàng, lá xanh, gem xanh và crest búa rèn trung tính. List/detail dùng inner parchment panel;
  title badge, primary button, hover, close icon và Gold badge tái sử dụng hệ asset Light Fantasy hiện
  có. Shop name, gold, item/recipe details và feedback vẫn là TMP động; reskin không thay capability
  service hoặc transaction ownership.
- Commerce safe area dùng hai cột cố định trong board authored `1180×680`: list `400×440` tại
  `(-300,-48)`, detail `560×440` tại `(200,-48)`. Title/Close nằm trong header inset; Shop controls
  và Craft button nằm trong đáy detail panel, không chạm khung ngoài hoặc ornament của panel con.
  List content có top inset `102`; row template luôn inactive, clone runtime dùng height `50`, spacing
  `6` và không force-expand chiều cao. Vì vậy row đầu không chạm crest và item/recipe liên tiếp không
  tạo khoảng trống lớn giả tạo.
- Crafting detail dùng TMP rich text để phân cấp recipe name, section header, ingredient counter,
  output và station. Counter đủ/thiếu dùng xanh/đỏ; stable station ID như `station.forge` được format
  thành label thân thiện `Forge`. Recipe name canh giữa theo detail panel; các section còn lại canh
  trái để giữ khả năng quét thông tin. Đây chỉ là presentation, recipe ID và transaction data không đổi.
- Shop detail dùng TMP rich text với item name canh giữa, description và nhóm `ITEM DETAILS` canh trái;
  Owned/Buy total/Sell total có phân cấp và màu currency rõ ràng. Cụm transaction nằm hoàn toàn trong
  detail panel trên một hàng gọn, có safe padding khỏi nội dung, viền và ornament.

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
- Dialogue choices render inside the right-hand parchment, below a compact body-text region. Nodes
  without choices expand body text into that lower region; choice buttons never float above or outside
  the dialogue frame. Dynamic dialogue text and choice labels use Digital Disco at a compact readable
  scale, preserving the portrait/name/body/decision hierarchy. The in-frame choice list supports up to
  five single-line choices, with exactly one choice per row; labels auto-size within the
  documented minimum and use ellipsis rather than expanding a button beyond the parchment safe area.
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

### Fishing interaction

`FishingSpotInteractable` tham gia cùng cursor/range flow: trong tầm dùng Interact cursor, ngoài tầm
dùng Blocked và không tự di chuyển Player. Click bắt đầu chỉ hợp lệ khi Inventory có ô trống.

- `FishingWaiting`: hiện waiting/bite prompt, world tiếp tục chạy nhưng gameplay và menu input bị khóa.
- Click dấu `!` kịp thời chuyển sang `FishingMinigame`; bỏ lỡ quay lại waiting.
- `FishingMinigame`: world pause theo `GameStateManager`; UI đọc input giữ/thả chuột bằng unscaled time.
- Result hiển thị ngắn rồi pop state; UI không trực tiếp set `Time.timeScale`.
- `Escape` là cancel chung và phải trả đúng state trước đó.

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
