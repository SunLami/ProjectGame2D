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

- **Theo D-054 (2026-09-26), art direction đổi sang Dark Inventory Style** (charcoal/walnut tối,
  antique gold mảnh, sapphire tiết chế — cùng ngôn ngữ D-041 đang dùng cho gameplay UI), thay cho
  "Light Fantasy bình minh" mô tả bên dưới (giữ lại để tham chiếu lịch sử/asset cũ trong lúc migrate
  từng phần). Video background và keyframe fallback dawn hiện tại chưa có quyết định thay thế cuối
  cùng — xem kế hoạch asset trong `Handoffs/ClaudeToCodex.md`.
- ~~Art direction là **Light Fantasy bình minh**, kể khoảnh khắc nhân vật rời cổng làng để bắt đầu hành
  trình; không dùng palette đêm/gothic cho landing page.~~ (đã override bởi D-054)
- Static fallback chuẩn hiện tại là `mainmenu_new_journey_dawn_v8.png`; video loop phải giữ cùng bố
  cục để UI bên trái không bị tranh chấp thị giác.
- Logo, landing board và button dùng chung ngôn ngữ vật liệu: gỗ sồi ấm, vải/xanh hoàng gia, viền vàng
  bình minh và pixel edge sắc. Button label vẫn là TMP/Digital Disco, không bake chữ vào sprite nền;
  slogan landing là wordmark sprite có outline riêng để giữ độ tương phản trên video sáng.
- Cụm landing được anchor theo 25% chiều ngang Canvas để giữ vùng trái ổn định khi đổi aspect ratio;
  các background graphic không nhận raycast.
- Landing button giữ sprite xanh dương ở Normal/keyboard-selected; chỉ pointer hover mới đổi sang sprite
  xanh lá, và phải khôi phục xanh dương khi pointer rời nút hoặc UI bị disable.
- **Theo D-054, `SettingsPage`/`SlotPage`/`ConfirmOverlay`/`ErrorOverlay` đã chuyển sang Dark Inventory
  Style** (asset `_v1` dưới `Assets/Resources/UI/MainMenu/DarkInventoryStyle/`, xem kết quả tích hợp ở
  entry `VERIFIED_MAINMENU_SETTINGS_SLOT_CONFIRM_INTEGRATION` trong `Handoffs/ClaudeToCodex.md`); mô tả
  "gỗ sồi/xanh hoàng gia" bên dưới chỉ còn đúng cho phần chưa migrate.
- `SlotPage` dùng thẻ hồ sơ dọc `save_slot_card_v1` (charcoal/walnut, nẹp gold). Metadata save vẫn là
  TMP/Digital Disco để dữ liệu động không bị bake vào asset; Primary/Back dùng `landing_action_button_v1`,
  Delete dùng `slot_delete_button_v1` (danger muted-red). Reskin không thay đổi binding, confirm flow
  hoặc save-slot contract.
- Tiêu đề mode của `SlotPage` (`NEW GAME`/`CONTINUE`) dùng wordmark sprite (`slot_page_new_game_title_v1`/
  `slot_page_continue_title_v1`, gán qua field `_newGameTitleSprite`/`_continueTitleSprite` trên
  `MainMenuSaveSlotsUI`, script tự đổi theo mode). Nhãn cố định `SLOT 1–3` dùng cấu trúc 2 con tách biệt
  dưới `Title`: `Badge` (Image nền `slot_badge_v1`, dùng chung cho cả 3 slot) và `Label` (TMP text "SLOT n")
  — **không đặt Image và TextMeshProUGUI trên cùng một GameObject cho trường hợp này**, vì cả hai đều bắt
  buộc `CanvasRenderer` riêng và một GameObject chỉ có một `CanvasRenderer` dùng chung, khiến chỉ một trong
  hai render được. Status, metadata và action label vẫn dùng TMP vì là dữ liệu động.
- `SettingsPage` dùng `settings_board_v1`/`settings_title_v1` (Dark Inventory); slider, toggle và
  Save/Cancel giữ component tương tác Unity nhưng presentation dùng sprite `_v1` đồng bộ MainMenu. Reskin
  không chuyển ownership ra khỏi `SettingsService`.
- SFX và Music dùng chung `settings_slider_track_v1`/`settings_slider_handle_v1` để hình học, hit target
  và feedback nhất quán; giá trị runtime vẫn do `UnityEngine.UI.Slider` và `SettingsService` sở hữu.
- Fullscreen dùng cặp `settings_checkbox_unchecked_v1`/`settings_checkbox_checked_v1` cùng hình học;
  `UnityEngine.UI.Toggle` sở hữu việc bật/tắt checkmark và tiếp tục gửi giá trị vào `SettingsService`.
- `ConfirmOverlay` và `ErrorOverlay` dùng chung `overlay_dialog_board_v1`; message vẫn là TMP vì thay đổi
  theo thao tác. Confirm/Close dùng `landing_action_button_v1`, Cancel dùng `slot_delete_button_v1`.
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
- `CharacterPopup/Window` dùng một outer board Dark Inventory thống nhất `760×410`, chia vùng Equipment
  `280×400` và Character Stats `450×400` bằng divider dọc nằm trong outer board. Outer board chỉ sở hữu
  silhouette, nền và divider; không bake title frame, level frame, stat section hoặc equipment slot.
- Mọi inner frame là `Image` riêng để designer chỉnh `RectTransform` độc lập: Equipment title,
  Character Stats title (`310×34` tại Y=132) và LevelBadge (`330×28` tại Y=94, cách title frame ~6px)
  dùng sprite 9-slice `character_inner_title_v1.png`; Vitals (`350×54`, Y=51), Combat (`350×72`, Y=-16),
  Mobility (`350×58`, Y=-85) và Recovery (`350×50`, Y=-143, cách nhau 4px) dùng bốn instance 9-slice của
  `character_stat_section_v1.png`. Theo D-051, cả hai bitmap này được import ở `SpriteImportMode.Multiple`
  với đúng 1 sprite entry và rect crop đo bằng pixel (không phải `Single`, vốn luôn bỏ qua rect tùy chỉnh
  và dùng nguyên canvas kể cả phần padding trong suốt, làm 9-slice co lại sai). Bảy slot Head, Weapon,
  Body, Shield, Necklace, Ring, Foot vẫn ở chế độ chỉ đọc: các slot chỉ giữ `Image` để hiển thị item từ
  `EquipmentManager`; label mỗi slot `74×14` tại `(0,-34)` so với slot; không giữ `EquipmentSlotUI`,
  click, drag/drop hoặc unequip callback.
- Bên phải là bảng Character Stats chia nhóm Vitals, Combat, Mobility và Recovery. Mỗi section: Header
  cao 16px tại `height/2-10`; Labels/Values cao `height-24` tại Y=-6 chung, `lineSpacing=1` (không dùng
  12 như bản đầu vì gây tràn frame). Label căn trái, value căn phải, header antique gold và dữ liệu động
  dùng TMP/Digital Disco; dữ liệu đọc từ `PlayerStat` và tự refresh khi stat/equipment thay đổi.
- `CloseButton` (`34×34`) là con trực tiếp của `Window` (không phải `CharacterStatsPanel`), Anchor/Pivot
  Top Right, `(-30,-30)`, để luôn nằm trong vùng charcoal và không đè ornament/gem góc của outer board.
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

`DarkLightFantasyUIStyleGuide.md` là nguồn chuẩn art direction gameplay UI theo D-041. Không duy trì
scene Style Guide riêng; mỗi UI được migration trực tiếp trên prefab nguồn, kiểm tra trong MapNhat ở
1920×1080 và giữ nguyên controller, callback, GameState cùng gameplay data ownership.

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
- `CloseBtn` là Button thật nằm trong safe inset góc trên phải, hiển thị asset
  `inventory_close_thin_hd.png` và tiếp tục gọi `InventoryWindowUI.CloseWindow`; board không bake dấu X.
- Footer board v5 chỉ có một currency well tương ứng Gold hiện tại. `CurrencyRow` căn giữa và chỉ chứa
  các currency đang được hệ thống cung cấp; hiện tại chỉ bật icon và TMP Gold động. Currency mới phải thêm một
  entry runtime/prefab tương ứng thì horizontal layout mới sinh thêm ô, không hiển thị placeholder rỗng.
- `GridScrollView` dùng vùng inset đã có trên unified board; grid runtime là 6 cột, cell `39x39`, spacing
  `7x4`, trong viewport `286x214` được đẩy lên 5 px. Năm hàng nhìn thấy chiếm 211 px theo chiều dọc,
  chừa safe inset để toàn bộ viền hàng cuối không bị khung panel che; grid vẫn scroll cho phần slot vượt quá năm hàng. Board có một opaque backdrop riêng để
  alpha trang trí không làm lộ scene bên dưới; backdrop luôn đứng sau board sau mọi lần chạy builder.
- Khối stats dưới character well là dữ liệu TMP runtime (`InventoryStatsUI`), bố trí 2 cột x 3 hàng với
  sáu icon riêng HP/ATK/DEF/SPD/CRIT/STA cùng các giá trị động; cập nhật từ `PlayerStat` và không sở hữu
  gameplay data. `UnifiedGameplayHUD/StatButton` tiếp tục dùng `stat_icon.png` của HUD và được bind tại
  source prefab, không dùng một trong sáu icon chi tiết của bảng InventoryStats. Mỗi cột stats dùng ba
  trục cố định icon/label/value; icon `14x14` và khoảng cách ngang không phụ thuộc silhouette nguồn để
  các icon rộng như ATK/SPD không chạm chữ hoặc giá trị. Value dùng width `48`, auto-size `6.5–8.5`
  và cột phải có inset riêng để chuỗi dài như `100/100` không vượt viền stat well.
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
- Panel giữ RectTransform `200×300` và dùng `GameplaySettings/DarkInventoryStyle/settings_board_v1.png`:
  nền charcoal/walnut, viền vàng mảnh và sapphire tiết chế theo D-041/Inventory v5.
- Save/Cancel giữ RectTransform `82×30` (asset legacy `41×15`), slider `110×14`, toggle khoảng `26×28`;
  label động vẫn dùng TMP/Digital Disco và control state tiếp tục do Unity UI sở hữu.
- SFX và Music dùng icon pixel-art riêng trong
  `GameplaySettings/DarkInventoryStyle/Icons/Processed/`, nền alpha trong suốt và giữ container `24×24`.
- Title gameplay Settings là TMP/Digital Disco runtime trong title ledge của board; không bake chữ vào asset.
- Gameplay Settings dùng safe area nội bộ: slider được hạ khỏi crest/ornament trên, toggle và action button
  cách đều theo trục dọc, Close nằm trong góc phải của board; không object tương tác nào vượt khỏi khung.
- Slider gameplay render theo thứ tự `Fill Area → Background → Handle Slide Area`, để fill nằm dưới track
  và không che viền/background presentation đồng bộ MainMenu.
- `settings_slider_track.png` là border-only overlay `2172×240` với lõi alpha trong suốt; MainMenu và
  gameplay Settings dùng chung asset để Fill phía dưới luôn nhìn thấy xuyên qua lòng track.
- Save/Cancel/Close dùng `ColorTint` với hover sapphire hoặc danger-red rõ ràng; mọi label dùng cream/gold
  đủ tương phản và đã kiểm tra không overflow trong Game View 1920×1080.

### Pause Menu visual direction

- DemoScene `_UI/PauseMenu` giữ nguyên `PauseMenuUI`, gameplay-state navigation, save/load flow và các
  button callback; reskin chỉ thay presentation trên scene object hiện có.
- Pause board giữ RectTransform legacy `164×340`, dùng
  `PauseMenu/DarkInventoryStyle/pause_menu_board_v1.png`: nền charcoal/walnut, viền vàng mảnh và sapphire
  tiết chế. Tiêu đề `PAUSED` là TMP runtime, không bake vào board.
- Các action button giữ bề rộng legacy `139.4`; chiều cao được chuẩn hóa `28` để toàn bộ danh sách nằm
  gọn trong board. Label cố định dùng TMP/Digital Disco thay vì bake chữ vào sprite.
- PauseMenu trình bày Resume/Inventory/Save/Load/Settings/Back to Menu/Exit; Shop và Craft không xuất hiện vì
  hai popup này được mở từ interaction context của NPC/crafting station theo kiến trúc tương tác gameplay.
- Resume/Settings/Inventory/Save/Load/Back to Menu dùng button pixel-art tách riêng khỏi board; Exit dùng
  danger tint đỏ, Close dùng icon thin chung với Inventory. Reskin không thay ownership save/session.
- Các action button PauseMenu dùng `ColorTint`: hover/Selected sapphire rõ ràng, pressed nâu-vàng và danger
  hover đỏ. State không thay đổi RectTransform hoặc thứ tự layout; tất cả button/text đã kiểm tra nằm trong board.

### SessionUX Save/Load overlay visual direction

- `MenuWindow/SessionUX/LoadOverlay` giữ nguyên `PauseMenuUI` slot binding, save/load/delete action,
  confirmation và session ownership; reskin chỉ thay presentation trên DemoScene.
- `LoadPanel` dùng anchor cố định giữa màn hình và bố cục compact `430×250`; ba slot card `116×165`
  tại X `-130/0/130`, đủ safe-area nhưng không để khoảng trống dư thừa. Builder chuẩn hóa local scale
  của panel/card/button về `1` để scale legacy không phóng khung hoặc co card sai tỉ lệ. Slot action dùng
  `96×24`, còn Back giữ hit target `120×28`.
- Metadata động trong mỗi slot dùng safe-area `78×58`, dịch phải `4 px`, inset ngang `2 px` và TMP
  auto-size `4.5–6.25`
  không wrap; chuỗi dài như area, play time và timestamp phải nằm hoàn toàn trong viền card ở cả Save và Load.
- Title Save/Load đặt giữa vùng đen của title ledge ở Y `-61`; nhãn `SLOT 1–3` nằm trong capsule nhỏ
  của card ở Y `-3`, không được đè viền trên hoặc rơi xuống vùng status.
- Board và card dùng nền charcoal/walnut, viền antique-gold mảnh và sapphire tiết chế, đồng bộ PauseMenu,
  Inventory và Gameplay Settings. Title Save/Load và nhãn `SLOT 1–3` là TMP runtime nằm trong safe-area;
  asset không bake text, badge, card hoặc control để designer tiếp tục chỉnh trong Editor.
- Primary/Back dùng button pixel-art tách riêng; Delete dùng danger tint đỏ. Toàn bộ action dùng
  `Selectable.ColorTint`: hover sapphire/đỏ, pressed nâu-vàng/đỏ sẫm. Builder gỡ hover component legacy
  và tắt Outline legacy để state cũ không ghi đè ColorTint hoặc tạo viền xanh quanh button/card.
- `ConfirmationPopup` giữ lớp dim toàn màn hình và dùng board pixel-art tách riêng
  `session_confirmation_board_v3.png`, panel `500×245`: charcoal/walnut, antique-gold và sapphire đồng bộ
  LoadOverlay. Asset không bake text hoặc button; message và action label tiếp tục là TMP động.
- Message dùng safe-area `390×48` tại Y `62` với auto-size `10–15`; button tách riêng `230×24`.
  Layout ba action ở Y `12/-22/-56`, layout hai action ở Y `-5/-45`; Save/Confirm dùng primary tint,
  bỏ qua lưu và Cancel dùng
  danger tint. Popup reskin không thay confirmation kind, callback hoặc session ownership.

### Tutorial overlay visual direction

- DemoScene `TutorialOverlayRoot` giữ nguyên `TutorialOverlayUI`, `TutorialManager`, step binding và
  Skip callback; reskin chỉ thay presentation, không sở hữu tutorial progression hoặc `Time.timeScale`.
- Theo D-049, `InstructionPanel` dùng RectTransform `360×92` và board Dark Inventory Style
  `Resources/UI/Tutorial/DarkInventoryStyle/tutorial_instruction_panel_v1.png`; board chỉ là
  background/frame, import ở `SpriteImportMode.Multiple` với rect crop đo bằng pixel (D-051) để bỏ
  phần padding trong suốt của bitmap thay vì kéo giãn cả canvas. `Header` là TMP/Digital Disco thật
  hiển thị `TUTORIAL` (antique gold), Anchor Top Center/Pivot Center, `120×20` tại `(0,-9)`, không bake
  wordmark vào bitmap và không tái dùng banner LightFantasy cũ. `InstructionText` Anchor/Pivot Middle
  Left, `245×32` tại `(18,-5)`, alignment Midline Left. `SkipButton` Anchor/Pivot Middle Right, `72×28`
  tại `(-48,-5)`, label stretch-fill toàn bộ nút để căn giữa tuyệt đối; Instruction và Skip dùng chung
  trục center-Y. `TutorialOverlayUI` ẩn `InstructionPanel` khi `GameStateManager` đang ở
  `GameState.GameplayMenu` để tránh đè lên nội dung Character Popup/Inventory đứng sau Tutorial trong
  sibling order của `GameplayUIRoot`; đây chỉ là presentation gate, không đổi step binding hay Skip
  callback. Skip dùng danger đỏ và hover chung MainMenu.
- `SkipConfirmation/Dialog` giữ RectTransform `570×245`, dùng `tutorial_skip_dialog_hd.png` với safe
  area dưới crest cho title/message TMP; Confirm Skip dùng danger đỏ, Keep Playing dùng primary xanh.
  Lớp dim chặn raycast trong lúc xác nhận và không thay đổi tutorial save contract.

### Minimap và FullMap visual direction

- Theo D-053 (thay D-052 dùng Camera runtime — bỏ vì lệch tỉ lệ khung hình, dễ ra viền đen, tốn render
  mỗi frame), Minimap (`UnifiedGameplayHUD.prefab/Minimap`, góc phải trên, hình tròn `96×96`, luôn hiện
  khi `GameState.Playing` và tự ẩn khi bất kỳ `GameplayMenu` nào mở) và FullMap (`MapPopup`, mở/đóng
  bằng `GameplayMenuPage.Map` qua phím `M` hoặc `BottomHUD/QuickSlotMap`, phủ kín 1920×1080) đều dùng
  chung một ảnh map tĩnh bake sẵn: `Tools/ProjectGame2D/UI/Bake Map Snapshot`
  (`MapSnapshotBaker.cs`) chụp một lần toàn bộ `BorderMap` (tự ẩn Player lúc chụp) thành
  `Assets/Resources/UI/Map/map_snapshot.png`. Player được biểu diễn bằng một icon marker hình thoi
  riêng (không phải camera sống chụp lại sprite Player thật).
- `MinimapController` hiển thị `RawImage` cuộn theo Player qua `uvRect` (crop một vùng nhỏ của
  `map_snapshot.png`), bên trong một `Mask` hình tròn; marker tự bù vị trí khi vùng crop bị kẹp ở rìa
  bản đồ để luôn đúng chỗ. `FullMapController` hiển thị nguyên `Image` chứa cả ảnh bên trong
  `RectMask2D`, scale kiểu "cover" (`Mathf.Min` giữa hai trục) để phủ kín màn hình không viền đen, zoom
  bằng lăn chuột đổi `localScale`. Cả hai dùng chung `MapWorldBounds` (đo
  `BorderMap.GetComponent<Collider2D>().bounds` runtime, không hardcode) để quy đổi world position của
  Player sang toạ độ normalized (0..1) khớp `map_snapshot.png`.
- `MapZoneManager` + `MapZoneTrigger` (tách biệt với `AreaTriggerZone`/`AreaZoneRegistry` — hệ đó phục
  vụ Tutorial "reach area" và quest direction indicator, dùng lại sẽ gây side-effect ngoài ý muốn) theo
  dõi tên khu vực Player đang đứng cho label trên Minimap; mặc định "Heart Village" cho bản đồ hiện có
  tới khi đặt thêm `MapZoneTrigger` chia nhỏ khu vực. Ngày hiển thị lấy trực tiếp `DateTime.Now`
  (`d MMM`, ví dụ "26 Sep") — không có hệ lịch/mùa giả lập riêng, khớp trực tiếp tỷ lệ 1 ngày thật = 1
  ngày game mà UTC crop (D-037) đã dùng.
- UI hiện tại dùng placeholder (panel màu phẳng, viền tròn vẽ runtime bằng `RuntimeCircleSprite` —
  không phải bitmap); khung Minimap tròn, icon la bàn, nút zoom +/- và style CloseButton FullMap theo
  Dark Inventory Style cần Codex gen sau (xem `Handoffs/ClaudeToCodex.md`) — không ảnh hưởng logic
  crop/zoom/zone/marker.

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
  Nếu nested prefab bị mất tham chiếu sprite, runtime khôi phục `QuestIcon` và `Chevron` từ
  `Resources/UI/Quest/Tracker1920` để không render thành ô trắng mặc định của `Image`.
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
- Theo D-050, board production là `Assets/Resources/UI/Quest/DarkInventoryStyle/quest_log_board_v1.png`
  (Dark Inventory Style), thay cho board LightFantasy `QuestLog1920` trước đây; mockup scroll cũ vẫn lưu
  tại `Assets/Documentation/DevelopmentPlan/quest_log_window_scroll_mockup_v2.png` làm tham chiếu bố cục.
  Board chỉ chứa outer frame, nền hai vùng và divider; không bake action-button frame. `TrackQuestButton`
  và `AbandonQuestButton` tiếp tục sở hữu sprite riêng `Assets/Resources/UI/Quest/QuestLog1920/quest_log_action_button.png`,
  nên khi runtime ẩn Button thì cả viền và text cùng biến mất. `QuestLogWindowPrefabBuilder.Load` luôn
  ép `SpriteImportMode.Single` + Point filtering khi import board, để tránh trường hợp Unity tự nhận
  diện bitmap thành nhiều sub-sprite và builder vô tình lấy nhầm một mảnh sprite phụ.
- Detail panel có `TRACK QUEST`/`UNTRACK QUEST` và `ABANDON QUEST`. Abandon luôn qua confirmation,
  reset toàn bộ tiến độ và nhắc người chơi quay lại đúng giver NPC để nhận lại. Quest không có
  `giverNpcId` không được abandon để tránh trạng thái progression không thể phục hồi.
- Dialog "Abandon Quest?" (GameObject `AbandonQuestConfirmation` hand-authored trong
  `Assets/Prefabs/UI/GameplayUIRoot.prefab`, field `QuestLogUI._abandonConfirmationRoot`) dùng board
  `Assets/Resources/UI/SessionUX/DarkInventoryStyle/session_confirmation_board_v1.png` (charcoal/
  walnut, viền gold, theo D-041) — asset này chỉ dùng đúng dialog này. `ConfirmAbandonButton` dùng
  `slot_delete_button_v1.png` (danger đỏ), `CancelAbandonButton` dùng `landing_action_button_v1.png`
  (đã có từ đợt MainMenu). Message là TMP màu Cream `RGBA(0.96, 0.91, 0.76)` — cùng hằng số dùng ở
  `QuestLogWindowPrefabBuilder`/`QuestAcceptPopupPrefabBuilder` để đồng bộ; **không dùng màu tối cho
  text trên board tối** (bug từng gặp: giữ nguyên màu nâu sẫm cũ dành cho board parchment sáng khiến
  message gần như không đọc được trên board charcoal mới).
- `QuestAcceptPopup` là popup dọc giữa màn hình, RectTransform `320×400` trên Canvas reference
  `800×600`, tương đương khoảng `768×960` physical pixel ở target `1920×1080`. Board dùng cùng gỗ
  sẫm, viền vàng, sapphire xanh và lá xanh của PlayerHUD/UnifiedGameplayHUD/QuestTracker; gameplay
  vẫn lộ rõ quanh popup và overlay chỉ dim nhẹ để giữ focus.
- Header, category icon, quest title, `OBJECTIVES`, objective rows, `REWARDS`, reward slots và hai
  button `ACCEPT`/`DECLINE` đều là GameObject thật trong `Assets/Prefabs/UI/QuestAcceptPopup.prefab`
  để designer chỉnh bằng Prefab Mode. Asset board không bake text hoặc dữ liệu quest; runtime bind
  category/objective/reward icon, progress, Gold và EXP mà không sở hữu progression hay save data.
- Board production của popup nằm tại
  `Assets/Resources/UI/Quest/QuestAccept1920/quest_accept_board_dynamic_rewards_v3.png` (theo D-038
  cập nhật 2026-09-27: board chỉ chứa frame/banner rỗng/divider, **không còn bake hình nút**). Board
  không bake reward socket; runtime chỉ tạo đúng số item reward thực tế, căn giữa và co slot khi danh
  sách dài. Vùng objective được chừa chiều cao cho nhiều dòng; quest ít objective không được tự kéo
  reward lên vì sẽ làm thay đổi nhịp bố cục giữa các quest.
- Nút `ACCEPT`/`DECLINE` là 2 Image/Button độc lập, sprite riêng
  `quest_accept_button_accept_v1.png` (sapphire)/`quest_accept_button_decline_v1.png` (charcoal trung
  tính), mỗi nút `120×40` theo đúng tỉ lệ 3:1 của bitmap nguồn `2172×724` để không bị méo hình; label
  vẫn là TMP child riêng, không bake vào sprite nút.

### Dialogue UI visual direction

- Dialogue dùng frame production tại
  `Resources/UI/Dialogue/DarkInventoryStyle/dialogue_frame_v4.png`: nền charcoal/walnut bán trong
  suốt, viền vàng pixel-art mảnh, sapphire tiết chế và nameplate tích hợp góc phải dưới. Asset không
  bake tên NPC, nội dung hoặc lựa chọn.
- Tên NPC, nội dung thoại và choice label luôn là TMP động với `DigitalDisco SDF v3`. Choice bình
  thường chỉ hiện text; toàn hàng vẫn là hit target. Hover hoặc keyboard/gamepad focus bật một viền
  vàng pixel 1 px được dựng bằng object UI riêng, không đổi kích thước hay vị trí hàng.
- Dialogue presentation không sở hữu quest outcome, shop/crafting transaction hoặc save data. Router/capability
  service cung cấp read model; UI chỉ render và phát intent lựa chọn/continue/cancel.
- Ở reference Canvas `800×450`, frame authoring `700×270` neo bottom-center, bottom inset 24 và hiển
  thị đồng đều ở scale `0.5`. Node có choice dùng body compact phía trên và tối đa 4 dòng choice phía
  dưới; node không có choice ẩn toàn bộ choice root và mở rộng body xuống safe area. TMP auto-size
  trong giới hạn, wrap/ellipsis thay vì tràn khung; continue indicator chỉ pulse khi page reveal xong.
- Khi Dialogue mở, scene binding ẩn gameplay `UICanvas` hiện tại và ghi nhớ `activeSelf`; khi đóng hoặc
  Dialogue bị destroy, trạng thái trước đó được khôi phục chính xác. `EventSystem` và Dialogue Canvas
  riêng vẫn hoạt động, nên trên màn hình chỉ còn Dialogue UI mà không làm mất lifecycle của HUD.

### Commerce UI layout

- Crafting navigation is an accordion with exactly seven equipment sections in the authored order:
  `Head`, `Body`, `Foot`, `Ring`, `Necklace`, `Shield`, `Sword`. All section headers remain visible,
  including empty sections. Activating a header expands only that section's blueprint rows; activating
  a blueprint then populates output item details and ingredient requirements. Recipes whose output is
  not one of those equipment slots are intentionally excluded from this equipment-crafting screen.
- Blueprint rows show the output item icon and name. The details panel reserves a distinct output icon
  and ingredient sockets; each occupied ingredient socket shows its item icon and `owned/required`
  count. Before a blueprint is selected the detail area contains only instructional empty-state copy.
  The crafting experience bar from the visual reference is not part of this project and must not be
  authored or simulated.

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
- Dialogue choices render dưới vùng body compact. Node không có choice mở rộng body vào vùng dưới;
  choice không được nổi ra ngoài frame. Danh sách hỗ trợ tối đa 4 choice một dòng; label auto-size
  trong giới hạn và dùng ellipsis thay vì mở rộng hit area vượt safe area.
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
