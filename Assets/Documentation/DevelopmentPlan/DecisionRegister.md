# Architecture and Game-Design Decision Register

File này ghi các quyết định ảnh hưởng kiến trúc. `Proposed` là mặc định khuyến nghị để team có thể
tiếp tục thiết kế; phải đổi thành `Accepted` trước phase implementation liên quan.

| ID | Quyết định | Trạng thái | Mặc định đề xuất | Cần chốt trước |
|---|---|---|---|---|
| D-001 | Vai trò DemoScene | Accepted từ yêu cầu | Integration playground; feature được đóng prefab/installer rồi kéo sang scene thật | Phase 0 |
| D-002 | Số save slot | Accepted từ yêu cầu | Chính xác 3 slot | Phase 2 |
| D-003 | Continue behavior | Proposed | Mở danh sách save hợp lệ; thêm Continue Last sau | Phase 3 |
| D-004 | New Game overwrite | Proposed | Slot có dữ liệu cần confirm rõ; không overwrite một click | Phase 2 |
| D-005 | Save khi combat | Proposed | Chỉ manual save ngoài combat/danger; UI giải thích lý do | Phase 9 |
| D-006 | Inventory pause world | Proposed | Pause world ở bản offline đầu | Phase 1 |
| D-007 | Dialogue pause world | Proposed | World chạy nhưng player input khóa; đánh giá nguy cơ bị tấn công | Phase 5/6 |
| D-008 | Tutorial skip | Proposed | Cho skip có confirm; vẫn cần Tutorial Quest để mở Main Quest | Phase 5 |
| D-009 | Tutorial Quest bắt buộc | Accepted từ yêu cầu | Không bắt buộc sandbox; bắt buộc để nhận Main Quest | Phase 6 |
| D-010 | Player death | Open | Load active slot, respawn checkpoint hay mất tài nguyên? | Phase 1/3 |
| D-011 | Initial save timing | Proposed | Ghi save sau New Game restore thành công | Phase 3 |
| D-012 | Autosave | Proposed | Chưa có ở foundation; manual save trước | Phase 9/10 |
| D-013 | Character creation scope | Open | Tối thiểu character name; appearance nếu hệ thống sẵn sàng | Phase 3 |
| D-014 | Obtain objective semantics | **Accepted — 2026-08-22** | `ObtainObjectiveMode` field trên từng `QuestObjectiveDefinition`: `CountAcquired` (counter cộng dồn, không giảm khi dùng) hoặc `RequirePossession` (kiểm tra sở hữu hiện tại >= targetCount, không phải counter). Không có rule ngầm định toàn cục. | Phase 6 |
| D-015 | Resource respawn clock | **Accepted — 2026-08-23** | `nextRespawnUtcTicks` lưu `DateTime.UtcNow.Ticks` tuyệt đối tại thời điểm harvest + respawn duration; `IsAvailable` so sánh trực tiếp với `DateTime.UtcNow.Ticks` hiện tại, không polling. Elapsed thời gian thật (kể cả lúc app đóng) tự nhiên được tính vì dùng UTC tuyệt đối, không phải in-game playtime tích lũy; chưa có catch-up/rate-limit/batch simulation đặc biệt cho khoảng offline dài. | Phase 8 |
| D-016 | Save format | Proposed | JSON versioned + backup; cân nhắc compression/encryption sau | Phase 2 |
| D-017 | Return Main Menu dirty state | **Accepted — 2026-08-23** | Đúng theo proposed default: `GameplaySessionController.OnConfirmationRequired` khi dirty → Save and Return / Return Without Saving / Cancel; clean session Return trực tiếp không hỏi. | Phase 9 |
| D-024 | Dirty-session event contract | **Accepted — 2026-08-23** | `SessionDirtyTracker` (scene service) đánh dấu dirty qua: `InventoryManager.OnInventoryChanged`, `EquipmentManager.OnEquipmentChanged`, `PlayerStat.OnLevelUp`/`OnExperienceChanged`, `TutorialManager.OnStepChanged`/`OnTutorialCompleted`, `QuestManager.QuestAccepted`/`QuestProgressChanged`/`QuestCompleted`/`MainQuestUnlocked`, `WorldDomainEvents.WorldObjectChanged` (mới). Player position/di chuyển đơn thuần **không** làm dirty. `GameSessionManager.MarkDirty()` tự no-op khi `IsRestoring == true` nên toàn bộ restore path (kể cả seed New Game) không bao giờ dirty giả. | Phase 9 |
| D-018 | Settings ownership | Accepted kiến trúc | Shared SettingsService, hai navigation UI riêng | Phase 1 |
| D-019 | Production world scene topology | **Accepted — 2026-09-07** | `MapNhat` là world scene production chính thức đầu tiên (nhiều scene, không phải một scene duy nhất dùng chung với DemoScene). `DemoScene` tiếp tục là integration playground, không đổi vai trò. Save tiếp tục dùng stable `areaId`/spawn ID, không hard-code tên scene vào domain save. | Trước production world |
| D-020 | Data loading backend | **Accepted — 2026-08-22** | Domain phụ thuộc `IItemResolver`; `ResourcesItemResolver` là backend migration ban đầu | Phase 4 |
| D-021 | Definition authoring reference | **Accepted — 2026-08-22** | Typed asset reference trong Inspector (`ItemSO`/`EquipmentItemSO`), stable `itemId` tại save/runtime boundary | Phase 4–7 |
| D-022 | Legacy item ID convention | **Accepted — 2026-08-22** | Giữ nguyên 60 legacy underscore itemId hiện có (`sword_lvl1`, `body_lvl9`, ...) làm stable ID chính thức cho content hiện có; **không** bulk rename. Convention dot-namespace (`item.weapon.sword.001`) chỉ áp dụng cho item MỚI thêm sau Phase 4. Validator tiếp tục báo Warning (không phải Error) cho các legacy ID này. | Phase 4 |
| D-025 | Save migration strategy | **Accepted — 2026-08-23** | `SaveMigration`/`ISaveMigrationStep` chạy chuỗi N→N+1 additive-default (không parse raw JSON riêng từng version) tận dụng việc `JsonUtility` bỏ qua field lạ/thiếu. Chạy in-memory tại `FileSaveSlotRepository.TryLoadValid`, không bao giờ rewrite file trên đĩa; chỉ save thật (ghi mới) mới nâng version trên đĩa. Save cũ hơn `SaveMigration.MinimumSupportedVersion` (hiện = 1) hoặc mới hơn `CurrentSaveVersion` vẫn là `IncompatibleVersion`, không đoán shape. | Phase 10 |
| D-026 | Player build GUI verification trong môi trường này | **Accepted — 2026-08-23** | Windows Player build tự thân thành công (0 error/warning, `Player.log` init sạch), nhưng cửa sổ game không thể được điều khiển/chụp màn hình đáng tin cậy qua computer-use automation hiện có trong môi trường này (window rect hợp lệ từ Win32 API nhưng không khớp nội dung nhìn thấy được trong screenshot). Đây là giới hạn tooling môi trường, không phải lỗi code. Click-through smoke test (New Game→DemoScene→Save→Return→Continue→Quit) cần chạy thủ công bởi user/Codex trên máy thật. | Phase 10 |
| D-027 | Save Game slot-picker semantics | **Accepted — 2026-08-23** | Pause Menu "Save Game" mở slot picker thay vì ghi thẳng vào `ActiveSlotId`. Slot Empty ghi trực tiếp; slot Valid/Corrupted/IncompatibleVersion đều bắt buộc `OnSaveSlotConfirmationRequired` trước khi ghi (không có ngoại lệ im lặng cho bất kỳ status nào). Ghi thành công vào slot khác `ActiveSlotId` hiện tại ("Save As") tự động chuyển `ActiveSlotId` sang slot đó qua `GameSessionManager.SetActiveSlotId` — không reset `IsDirty`/`IsRestoring`/play-time base, chỉ đổi nhãn session. `DeleteSlot` không tự hỏi xác nhận (UI phải tự hỏi trước khi gọi, giống `MainMenuController.DeleteSlot`); xóa slot đang active không phá session đang chạy, save tiếp theo vào slot đó tự nhiên được coi là Empty vì không có autosave (D-012) nào có thể nhắm nhầm vào slot vừa xóa. | Phase 10 |
| D-028 | Unified gameplay HUD và quick bar | **Accepted mở rộng — 2026-09-22** | Gộp Health, Stamina, EXP, 8 quick slot, Stat và Map vào HUD đáy màn hình. Quick bar gán item bằng stable `itemId`, kéo từ Inventory vào slot, chọn bằng click/phím 1–8, hiển thị tổng quantity từ Inventory và lưu assignment + selected index. Assignment không trỏ trực tiếp vào inventory slot nên vẫn ổn định khi swap/stack; item hết quantity vẫn giữ assignment mờ để tự phục hồi khi có lại. | Content production |
| D-029 | Supplemental top-left player status HUD | **Accepted từ yêu cầu — 2026-08-25** | Thêm HUD riêng ở góc trái trên hiển thị Avatar mặc định, Health, Stamina xanh lá và Level; không di chuyển hoặc xóa dữ liệu tương ứng khỏi unified HUD dưới. `PlayerHUDController.SetAvatar` chỉ là presentation hook. Upload/chọn avatar, quyền truy cập file, validation ảnh và persistence chưa thuộc phạm vi quyết định này. | Content production |
| D-030 | Runtime cursor presentation | **Accepted từ yêu cầu — 2026-08-26** | Một `GameCursorManager` application service sở hữu `Cursor.SetCursor`, load tám texture từ `Resources/UI/Cursors` và phân loại hover từ component hiện có. Resource node author `ResourceHarvestType`; unavailable/out-of-range dùng Blocked. Cursor chỉ presentation, không sở hữu combat, quest, proximity interaction hay persistence. | Content production |
| D-031 | Data-driven resource harvesting | **Accepted từ yêu cầu — 2026-08-26** | Mining/Chopping dùng hit và `harvestDamage` độc lập combat; cả Mining, Chopping và Gathering đều flash silhouette trắng khi tương tác mà không tắt/bật sprite. Gathering click trong phạm vi rồi khóa 1–1.5 giây. `ResourceNodeDefinition` sở hữu loại khai thác, HP, `requiredToolType` (mặc định None), loot table nhiều entry và UTC respawn. Khi thu hoạch xong, node ẩn toàn bộ visual/collider thay vì dùng depleted sprite và tự hiện lại đúng hạn. Loot icon hiển thị ở kích thước lớn, rơi/bay tới Player, được kiểm tra sức chứa như một transaction và chỉ commit Inventory khi đến Player; thiếu chỗ thì node không bị tiêu thụ. Không tự chạy Player đến node. | Content production |
| D-032 | NPC world interaction input | **Accepted từ yêu cầu — 2026-08-26** | NPC không bind action `Gameplay/Interact`/phím E. Khi Playing, hover collider NPC trong phạm vi hiển thị Talk cursor; nhấn chuột trái gọi capability tương tác của NPC. Ngoài phạm vi dùng Blocked và không tự chạy Player. | Dialogue content production |
| D-033 | Persistent chest interaction | **Accepted từ yêu cầu — 2026-08-26** | Chest dùng Interact cursor và chuột trái trong phạm vi, không bind phím E. Animation mở hoàn tất trước loot presentation; Inventory và persistent opened state chỉ commit sau khi loot bay tới Player. Capacity failure không tiêu thụ reward và chest vẫn retry được. | Content production |
| D-034 | Gameplay Timeline Scene 01 | **Accepted từ yêu cầu — 2026-09-08** | Sau Intro, khóa input và dùng chính GameObject `Intro`/`Assets/Timeline/IntroTimeline.playable` để điều khiển Player đi từ trái sang phải qua cầu. Mỗi phân cảnh là Group Track rõ ràng trong cùng Timeline; chưa thêm NPC/camera/dialogue ở bước này. | Gameplay Timeline content |
| D-034 | Compact scrollable QuestTracker | **Accepted, revised theo yêu cầu — 2026-09-22** | Tracker là HUD không background/board/dim, target `1920×1080`; header có chevron, icon quest, TMP `QUESTS` và đường vàng. Nhấn header mở/đóng toàn bộ danh sách. Quest đang track xếp Main → Side → Daily, hiển thị icon category, icon objective, progress/READY và cuộn khi vượt chiều cao; QuestLogWindow giữ nội dung đầy đủ. Daily category chỉ là presentation metadata. Debug quest vẫn chỉ tồn tại trong Editor/Development Build, không persist hoặc làm session dirty. | Content production |
| D-035 | QuestLogWindow 1920 redesign | **Accepted, revised — 2026-09-23** | Quest Log là cửa sổ giữa màn hình `560×340` trên Canvas `800×600`, có list ScrollRect/Viewport/Mask/Scrollbar, icon category/objective, tracked pin, reward summary và hành động Track/Abandon. Hierarchy authoring nằm trong `GameplayUIRoot.prefab`; TMP giữ text động, board không bake nội dung quest hoặc action-button frame, để trạng thái ẩn Button loại bỏ đồng thời cả viền lẫn chữ. | Content production |
| D-035 | Fishing data, inventory và minigame contract | **Accepted từ yêu cầu — 2026-09-20** | Fish definition dùng stable `itemId` + icon; mỗi cá bắt được là instance không stack có GUID/cân nặng/giá trị riêng trong Inventory chung. Inventory đầy chặn bắt đầu. Waiting khóa gameplay nhưng world chạy; minigame pause world qua `GameStateManager`; miss hook trở lại waiting; mất tiếp xúc làm giảm progress. | Fishing content production |
| D-036 | Quest tracking và abandon contract | **Accepted từ yêu cầu — 2026-09-22** | Tối đa một quest Active/Ready được track và persist bằng stable quest ID; untrack không đổi progress. Abandon xóa toàn bộ runtime progress, chỉ áp dụng cho quest có giver NPC, và quest chỉ có thể được nhận lại từ đầu qua đúng giver NPC. | Quest content production |
| D-037 | Fixed-plot farming contract | **Accepted từ yêu cầu — 2026-09-22** | Farming dùng các ô đặt sẵn, không xới/tưới ở scope đầu. Chọn `SeedItemSO` từ quick bar rồi click ô trống trong tầm để trừ một seed và gieo. Crop lớn theo UTC kể cả khi app đóng, stage tính từ definition + `plantedAtUtcTicks`. Chỉ crop mature nhận harvest; capacity được kiểm tra trước, loot rơi/bay vào Player rồi mới commit Inventory và trả plot về Empty. Hover hợp lệ highlight nhẹ. Click farming bị tiêu thụ như non-combat interaction và không kích hoạt attack/stamina/`PlayerAttacked`. | Farming content production |
| D-038 | QuestAcceptPopup 1920 redesign | **Accepted, revised — 2026-09-23; cập nhật 2026-09-27** | Quest Accept là popup dọc giữa màn hình `320×400` trên Canvas `800×600`, dùng board charcoal/walnut, viền antique gold mảnh và sapphire tiết chế đồng bộ HUD. Category, objective và reward icons cùng toàn bộ text là dữ liệu động trên hierarchy prefab thật; board không bake reward socket. Nút Accept/Decline dùng hai sprite nền riêng, không bake vào board, để designer tự do chỉnh RectTransform; label vẫn là TMP runtime. Popup chỉ presentation và giữ nguyên callback Accept/Decline, quest progression và save contract hiện có. | Quest content production |
| D-039 | Inventory dark reskin, dynamic currency footer và item tooltip | **Accepted từ yêu cầu — 2026-09-23; cập nhật 2026-09-24** | Inventory dùng palette walnut/charcoal/gold/sapphire đồng bộ gameplay HUD; board không bake item data hoặc currency box. `CurrencyRow` chỉ hiện badge cho currency có dữ liệu (hiện tại chỉ Gold), không giữ placeholder rỗng. Hover `InventorySlotUI` mở tooltip prefab-authored với icon, type/equipment slot, stat modifiers, stack và description động; tooltip ưu tiên ngoài mép Inventory, tự clamp trong Canvas và ẩn khi pointer exit/drag. Navigation, drag/drop, equipment, stable item ID và save contract không đổi. | Inventory content production |
| D-040 | Unified Inventory board và idle character preview | **Accepted từ yêu cầu — 2026-09-24; cập nhật layout v5** | Inventory dùng board landscape liền khối gần ảnh tham chiếu: đúng 7 equipment socket quanh character, stats trái, grid scroll 6x5 và đúng một Gold well phải. Item/equipment dùng chung slot phẳng viền mảnh; slot equipment trống có silhouette loại item runtime và tự ẩn khi equip; stats là TMP runtime 2x3 đọc `PlayerStat`. Character preview render chính Player runtime trong Hierarchy bằng camera phụ/RenderTexture; Animator tạm chạy idle unscaled khi Inventory mở và được trả về update mode cũ khi đóng. Preview/UI không mutate equipment/save. | Inventory presentation |
| D-041 | Dark Light Fantasy UI System | **Accepted từ yêu cầu — 2026-09-25** | Lấy Inventory v5 làm master visual language cho gameplay UI: charcoal/walnut, viền vàng mảnh, sapphire tiết chế, Digital Disco, slot phẳng và safe spacing 4/8/12/16. `DarkLightFantasyUIStyleGuide.md` là nguồn chuẩn; không duy trì scene Style Guide. Migration từng UI giữ nguyên controller, callback, GameState và data ownership. | UI presentation migration |
| D-042 | NPC buy/sell quote from ItemSO | **Accepted từ yêu cầu — 2026-09-25** | Mọi ItemSO trong `Resources/Items` author `MinBuyPrice`/`MaxBuyPrice` và `MinSellPrice`/`MaxSellPrice`. Shop chỉ mua item nằm trong stock hoặc là output recipe do cùng NPC cung cấp. Domain roll unit-price trong khoảng inclusive khi item được chọn/thả và giữ quote cố định tới khi xác nhận, đổi item hoặc đổi quantity; UI không tự quyết định giá và giao dịch vẫn atomic. Asset cũ có range 0 fallback về ShopStockEntry để bảo toàn content hiện tại. | Commerce UI/data |
| D-044 | Per-NPC Buy/Sell ItemSO catalogs | **Accepted từ yêu cầu — 2026-09-25** | Mỗi ShopDefinition có hai danh sách ItemSO độc lập: Buy Items quyết định item hiện trong Buy Tab, Sell Items quyết định item NPC chấp nhận từ Sell Tab. Output recipe của cùng NPC vẫn là sell eligibility bổ sung. Stock itemId/price cũ chỉ là compatibility fallback; content mới thêm bằng cách kéo ItemSO vào danh sách, không sửa manager/UI code. | Commerce content authoring |
| D-045 | Commerce board/runtime element separation | **Accepted từ yêu cầu — 2026-09-25** | Shop/Crafting board chỉ chứa outer frame và panel cấu trúc. Inventory Commerce dùng runtime `InventorySlotUI`, lấy cùng capacity từ `InventoryUIController/GridSlot`; vùng nhìn 6×5 nằm trong ScrollRect/Viewport và cuộn dọc tới toàn bộ capacity. BUY/SELL tab, BUY/SELL/CRAFT action, quantity controls, close, text và icon là object/sprite riêng, tuyệt đối không bake vào board. Crafting category list là ScrollRect không scrollbar hiển thị và không bake row. Button sprite phải là pixel art point-filtered đồng bộ Inventory, không dùng vector/HD art. | Commerce UI presentation |
| D-046 | Editable Shop/Crafting authoring prefabs | **Accepted từ yêu cầu — 2026-09-25** | `ShopWindow.prefab` và `CraftingWindow.prefab` là hai asset authoring độc lập; `GameplayUIRoot.prefab` sử dụng nested prefab instance để mọi chỉnh sửa RectTransform/Text/Icon/Button trong Prefab Mode phản ánh trực tiếp vào runtime. Builder chỉ dùng khi chủ động rebuild toàn bộ và có thể ghi lại layout đã author thủ công. | Commerce UI authoring |
| D-047 | Compact dynamic Dialogue presentation | **Accepted từ yêu cầu — 2026-09-25; thu gọn theo runtime review** | Dialogue dùng frame dark-fantasy pixel-art bán trong suốt, bottom-center, hiển thị ở 50% kích thước authoring `700×270` trên Canvas `800×450`. Node không có choice cho body chiếm safe area lớn; node có choice chia body phía trên và tối đa 4 dòng choice phía dưới. Choice là text-click target không có button frame thường trực; viền vàng mảnh chỉ hiện khi hover/focus và phải reset khi đóng/chuyển/mở lại dialogue. Speaker name nằm trong nameplate phải dưới; mọi TMP auto-size/clamp và không được tràn frame. Asset không bake text hay choice. | Dialogue UI presentation |
| D-048 | Shared Intro/Gameplay dialogue visual language | **Accepted từ yêu cầu — 2026-09-25** | `IntroCutscene` giữ controller, Timeline, video và Skip/Next semantics hiện có nhưng dùng cùng `dialogue_frame_v4` dark-fantasy pixel-art với Dialogue gameplay. Speaker/body là TMP runtime trong safe area. NEXT/SKIP SCENE/SKIP INTRO dùng chung một button background pixel-art riêng, không bake label; hover/pressed do Unity Button điều khiển. | Intro cinematic presentation |
| D-049 | Tutorial overlay Dark Inventory Style migration | **Accepted từ yêu cầu — 2026-09-26** | `TutorialOverlayRoot/InstructionPanel` dùng board mới `tutorial_instruction_panel_v1.png` (Dark Inventory Style), giữ RectTransform `360×92`. Board chỉ là background/frame; `Header` là TMP/Digital Disco thật (không bake wordmark bitmap, không tái dùng banner LightFantasy cũ) và `InstructionText`/`SkipButton` tiếp tục là object runtime riêng `72×28`. `TutorialOverlayUI` ẩn `InstructionPanel` khi `GameStateManager.CurrentState == GameState.GameplayMenu` (ví dụ Character Popup) để tránh đè lên overlay khác đứng sau nó trong sibling order của `GameplayUIRoot`; đây chỉ là presentation gate, không đổi `TutorialManager`, step binding, Skip callback hay save/progression contract. | Tutorial UI presentation |
| D-050 | Quest Log Dark Inventory Style board và Single sprite import fix | **Accepted từ yêu cầu — 2026-09-26** | `QuestLogWindow/Window` dùng board mới `quest_log_board_v1.png` (Dark Inventory Style), giữ RectTransform `560×340` với `QuestListPanel 190×270`/`QuestDetailPanel 330×270` không đổi. Board chỉ chứa outer frame, nền hai vùng và divider; quest row, scrollbar, title, category/status, objective, reward, Track/Abandon và CloseButton tiếp tục là object runtime riêng, không bake vào board. `QuestLogWindowPrefabBuilder.Load` được sửa để luôn ép `SpriteImportMode.Single` + Point filtering bất kể trạng thái import mặc định của Unity — asset trước đó bị Unity tự nhận diện thành nhiều sub-sprite (`spriteMode: Multiple`) và code cũ chỉ kiểm tra `textureType`/`mipmapEnabled` nên bỏ qua việc sửa `spriteImportMode`, khiến board có thể render nhầm một mảnh sprite phụ 6×24 thay vì toàn bộ art 1120×680. | Quest Log UI presentation |
| D-051 | Character Popup/Tutorial sprite-rect trim fix và layout tuning theo Game View review | **Accepted từ yêu cầu — 2026-09-26** | Phát hiện `SpriteImportMode.Single` luôn bỏ qua rect tùy chỉnh ghi vào `spritesheet` (đã kiểm chứng thực nghiệm) và luôn dùng toàn bộ canvas làm sprite rect; ba bitmap `character_stat_section_v1.png`, `character_inner_title_v1.png`, `tutorial_instruction_panel_v1.png` đều có padding trong suốt quanh art thật nên phần padding bị tính vào vùng 9-slice/stretch, ép hình thật co lại. Fix bằng `SpriteImportMode.Multiple` với đúng 1 sprite entry và rect crop đo bằng pixel (không chỉnh bitmap gốc). Đồng thời tinh chỉnh layout theo review Game View 1920×1080: Character Popup `CloseButton` chuyển thành con trực tiếp của `Window` (Top Right, `(-30,-30)`, 34×34) để không đè ornament góc outer board; `TitleFrame` Y=132, `LevelBadge` Y=94; 4 stat section đổi kích thước/khoảng cách còn `Vitals 350×54@Y51`, `Combat 350×72@Y-16`, `Mobility 350×58@Y-85`, `Recovery 350×50@Y-143` (gap 4px); text trong section dùng `Header` cao 16 tại `height/2-10`, `Labels`/`Values` cao `height-24` tại Y=-6 với `lineSpacing=1` (bỏ công thức cũ `lineSpacing=12` gây tràn frame); Equipment slot label còn `74×14` tại `(0,-34)`. Tutorial `InstructionPanel`: `Header` Anchor Top Center/Pivot Center `120×20@(0,-9)`; `InstructionText` Anchor/Pivot Middle Left `245×32@(18,-5)` alignment Midline Left; `SkipButton` Anchor/Pivot Middle Right `72×28@(-48,-5)` với label stretch-fill để căn giữa tuyệt đối. Không đổi `CharacterPopupUI`, `TutorialOverlayUI`, callback hay GameState lifecycle. | Character Popup & Tutorial UI presentation |
| D-052 | Minimap + FullMap dùng camera runtime thay vì bitmap map | **Superseded bởi D-053 — 2026-09-26** | Minimap (góc phải, luôn hiện khi `GameState.Playing`) và FullMap (`GameplayMenuPage.Map`, mở bằng phím `M` hoặc `QuickSlotMap`) đều render bằng một Camera orthographic tạo runtime (theo pattern có sẵn của `InventoryCharacterPreviewUI`, không phải Camera author sẵn trong scene) ghi vào `RenderTexture` hiển thị qua `RawImage`; Player xuất hiện trên map vì camera nhìn thẳng xuống vị trí Player thật, không cần marker tổng hợp riêng. `MinimapController` bám theo Player với orthographic size nhỏ cố định; `FullMapController` khởi tạo khung hình vừa đúng bounds `BorderMap` (đo `Collider2D.bounds` runtime, không hardcode), cho zoom bằng lăn chuột (`IScrollHandler`, đổi `orthographicSize`, clamp trong khoảng `[fit×0.2, fit]`) và tự kẹp vị trí camera trong bounds. Thêm `MapZoneManager`/`MapZoneTrigger` (tách biệt hoàn toàn khỏi `AreaTriggerZone`/`AreaZoneRegistry` vốn phục vụ Tutorial "reach area"/quest direction, để tránh side-effect) theo dõi tên khu vực hiện tại cho label trên Minimap, mặc định "Heart Village" cho map hiện có tới khi có `MapZoneTrigger` con chia nhỏ khu vực. Ngày hiển thị trên Minimap lấy trực tiếp từ `DateTime.Now` (định dạng `d MMM`, ví dụ "26 Sep") — không xây hệ thống lịch/mùa giả lập riêng vì người yêu cầu chọn tỷ lệ 1 ngày thật = 1 ngày game, khớp trực tiếp với UTC thật D-037 đang dùng cho crop. UI hiện dùng placeholder trơn (không sprite khung/la bàn/nút zoom); asset Dark Inventory Style thật cho khung Minimap, la bàn góc, nút zoom +/- và style CloseButton FullMap cần Codex gen sau, không ảnh hưởng logic camera/zoom/zone bên dưới. | Map/Minimap UI & world systems |
| D-053 | Minimap + FullMap đổi sang ảnh map tĩnh bake sẵn (thay Camera+RenderTexture của D-052) | **Accepted từ yêu cầu — 2026-09-26** | Sau khi thấy Camera runtime (D-052) tạo cảm giác lạ so với RPG 2D thông thường (tỉ lệ khung hình lệch, dễ ra viền đen, chi phí render mỗi frame), đổi sang kiến trúc chuẩn của thể loại: `Tools/ProjectGame2D/UI/Bake Map Snapshot` (`MapSnapshotBaker.cs`) dùng Camera tạm trong Editor chụp một lần toàn bộ `BorderMap` (tự ẩn Player lúc chụp) thành `Assets/Resources/UI/Map/map_snapshot.png`, import Single sprite Point filter. Minimap (`MinimapController`) hiển thị `RawImage` cuộn theo Player qua `uvRect` (không phải camera sống), crop trong một `Mask` hình tròn (sprite tròn vẽ runtime bằng `RuntimeCircleSprite`, không phải bitmap — chờ Codex gen frame thật); marker vị trí tính bù khi crop bị kẹp ở rìa bản đồ để luôn đúng. FullMap (`FullMapController`) hiển thị nguyên `Image` chứa cả ảnh, scale theo kiểu "cover" để phủ kín 1920×1080 không viền đen, zoom bằng lăn chuột đổi `localScale` trong `RectMask2D`. Cả hai dùng chung `MapWorldBounds` (đo `BorderMap.Collider2D.bounds` runtime) để map world position → normalized (0..1) cho icon Player hình thoi riêng (không phải sprite Player thật). Sửa 2 bug trong lúc build: (1) Minimap "đơ" do `_hasBounds` chỉ check một lần trong `OnEnable` — thua race lúc `MapNhat`/`BorderMap` chưa load xong khi Minimap (sống trong Bootstrap) đã enable — fix bằng tự retry mỗi `LateUpdate` cho tới khi tìm thấy; (2) FullMap ra viền đen 2 bên do dùng công thức "contain" (`Mathf.Max`) thay vì "cover" (`Mathf.Min`) khi so khớp aspect ratio bounds map với viewport. Không đổi `GameStateManager`, phím `M`, hay lifecycle `GameplayMenuPage.Map`. | Map/Minimap UI & world systems |
| D-054 | MainMenu chuyển từ Light Fantasy bình minh sang Dark Inventory Style | **Accepted từ yêu cầu — 2026-09-26** | Override quyết định trước đó (D-041/`DarkLightFantasyUIStyleGuide.md` §Mục tiêu và `UIAndInteractionFlows.md` §MainMenu visual direction), vốn giới hạn Dark Inventory Style (charcoal/walnut/gold mảnh, theo Inventory v5) chỉ cho gameplay UI và quy định rõ MainMenu dùng "Light Fantasy bình minh" (gỗ sồi ấm, xanh hoàng gia, viền vàng bình minh) — "không dùng palette đêm/gothic cho landing page". Theo yêu cầu mới, toàn bộ MainMenu (Landing, SlotPage New Game/Continue, SettingsPage, ConfirmOverlay/ErrorOverlay, LoadingOverlay) sẽ đổi sang cùng charcoal/walnut/antique gold/sapphire tiết chế như gameplay UI, đồng bộ toàn bộ art direction của game. Video background (`mainmenu_background.mp4`) và static keyframe fallback (`mainmenu_new_journey_dawn_v8.png`) **giữ nguyên không đổi** — chỉ các UI board/panel/button chồng lên video (Landing, SlotPage, SettingsPage, Overlays) đổi sang Dark Inventory Style; board tối tạo tương phản trên nền video sáng, không cần Codex đụng tới video. Thực hiện theo từng nhóm UI, Landing trước tiên (xem `Handoffs/ClaudeToCodex.md`). Không đổi `SettingsService`, save-slot contract, confirm flow, hay bất kỳ binding/controller nào — thuần presentation migration giống các UI gameplay đã làm trước đó. | MainMenu UI presentation migration |
| D-055 | Fishing minigame UI Dark Inventory Style production | **Accepted từ yêu cầu — 2026-09-27; cập nhật 2026-09-27** | `FishingFeatureAuthoring.cs` hiện dựng toàn bộ UI (WaitingPanel, BitePrompt, MinigamePanel, MovementTrack, CatchZone, vertical progress Slider track/fill, ResultPanel) bằng `Image` màu phẳng placeholder, chưa từng có bitmap — đây là lượt gen asset đầu tiên cho Fishing UI, áp dụng D-041/`DarkLightFantasyUIStyleGuide.md` (charcoal/walnut nền, viền antique gold mảnh, sapphire tiết chế) làm chuẩn, đồng bộ với QuestTracker/Minimap/Dialogue vốn cũng là HUD overlay trong lúc world vẫn hiển thị. Vì là gen mới không phải reskin, kích thước bitmap không bắt buộc khớp 1:1 pixel logic cũ — Claude được quyền điều chỉnh `sizeDelta`/`Image.Type` (Simple/Sliced) trong authoring script cho khớp tỉ lệ asset thật miễn giữ đúng bố cục 4 state (Waiting/Bite/Minigame/Result) và không đổi `FishingMinigameController`/`FishingMinigameUI` public API, gameplay timing hay `GameState` policy. WaitingPanel/BitePrompt phải giữ alpha bán trong suốt để world vẫn thấy được (theo D-035, world tiếp tục chạy ở Waiting). CatchZone cần 9-slice-safe (chiều cao đổi runtime theo `normalizedSize`, chiều rộng cố định) để không méo khi resize. **Cập nhật sau khi owner review đợt gen đầu (`_v1`, xem screenshot thật):** BitePrompt ring quá phẳng/mỏng, không có độ dày/bevel như các khung vàng khác trong game (Minimap ring, Dialogue frame); MovementTrack chỉ là đường kẻ mảnh với 2 đầu mút trang trí bất cân xứng, không giống một "đường ray"; CatchZone cùng tông màu với track nên gần như vô hình; Slider có đầu mút vàng nặng hơn cả phần fill — tổng thể "thiếu chuyên nghiệp". Owner yêu cầu gen lại **toàn bộ 8 asset** (`_v2`), đồng thời mở rộng `MinigamePanel` từ `520×650` lên `620×780` để gauge có đủ không gian dày/rõ hơn. Bộ `_v2` (+ fix ring `_v3`) verify Play Mode PASS, nhưng owner review tiếp và thấy `MovementTrack`/`CatchProgress` bị lệch khoảng cách trong panel (gap giữa 2 gauge quá lớn so với lề) — đã chỉnh lại toạ độ 1 lần, nhưng owner sau đó quyết định **revert toàn bộ code/prefab/asset Fishing UI về đúng git HEAD** (bản gốc trước khi gen asset, xác nhận qua `git checkout`) thay vì tiếp tục vá. **Hướng đi mới (2026-09-27, vòng 3):** gen lại bộ asset bám đúng 1:1 kích thước logic đã hardcode sẵn trong `FishingFeatureAuthoring.cs` gốc (`500×74`/`150×150`/`520×650`/`150×490`/`128×120`/`46×490`×2/`620×180`) để tích hợp **không cần sửa bất kỳ `sizeDelta`/`anchoredPosition` nào** — loại bỏ hẳn rủi ro lệch bố cục ở gốc. Phong cách đơn giản hơn `_v2` nhưng vẫn giữ 2 bài học đã xác nhận đúng: BitePrompt cần nền bán trong suốt sau ring, CatchZone cần màu tương phản mạnh so với track. Xem chi tiết tại `FishingSystem.md` §Visual direction và `Handoffs/ClaudeToCodex.md`. | Fishing content production |
| D-056 | Fish reveal-on-catch (mystery icon trong minigame) | **Accepted từ yêu cầu — 2026-09-27** | Trong lúc minigame, `FishIcon` di chuyển trong `MovementTrack` không còn hiện đúng sprite của `_selectedFish` (đã roll ngẫu nhiên từ `FishingSpotDefinition.FishTable` ngay khi `BeginMinigame`) — thay bằng 1 icon "cá bí ẩn" chung (`fishing_mystery_fish_icon.png`, Dark Inventory Style, cùng kích thước 62×62 logic, không đổi layout). Loại cá thật chỉ lộ ra ở `ResultPanel` khi bắt thành công (`CompleteCatch`), hiện icon thật của `_selectedFish` cạnh message; thất bại/hết giờ/đầy túi không hiện icon. Việc roll cá vẫn diễn ra y hệt lúc trước (không đổi xác suất/timing), chỉ ẩn thông tin khỏi UI cho tới lúc kết quả — thuần presentation, không đổi `FishingSpotDefinition`/`FishDefinitionSO`/save contract. | Fishing content production |
| D-057 | PlayerHUD và BottomHUD Dark Inventory Style production | **Accepted từ yêu cầu — 2026-09-28; revert theo yêu cầu — 2026-09-28** | **Đã hoàn tác:** owner review Play Mode thật thấy bộ `_v1` có 2 lỗi (icon Map/Character chìm vào nền socket do cùng tông màu tối; nhánh nối rãnh EXP với thân khung dùng gai đen tạo cảm giác rời rạc so với dây lá liền mạch của bản gốc) và quyết định giữ nguyên UI `LightFantasy` gốc thay vì tiếp tục sửa. Cả 2 prefab (`PlayerHUD.prefab`, `UnifiedGameplayHUD.prefab`) đã `git checkout` về đúng sprite reference gốc (xem `Handoffs/ClaudeToCodex.md` entry `PLAYERHUD_BOTTOMHUD_REVERTED_TO_ORIGINAL_UI` cho danh sách GUID đầy đủ). 10 file `_v1` vẫn giữ trong project (không xoá) để tham khảo/dùng lại sau nếu cần, nhưng không còn prefab nào tham chiếu tới. `PlayerHUDController`/`UnifiedGameplayHudController` không đổi trong suốt quá trình — chỉ Sprite reference bị hoàn tác. Nội dung quyết định gốc (giữ nguyên phía dưới để tham khảo lịch sử): `PlayerHUD.prefab` (Health/Stamina/Avatar/Level) và `UnifiedGameplayHUD.prefab/BottomHUD` (quick slot đáy màn hình, exp bar, nút Map/Character) vẫn dùng 10 bitmap `LightFantasy` gốc (`player_status_frame`, `health_fill`, `stamina_fill_green`, `default_avatar`, `unified_hud_frame`, `quick_slot_background_brown`, `socket_background_round_brown` — dùng chung cho cả LevelBadge của PlayerHUD lẫn Map/Stat button của BottomHUD, `experience_bar_fill_blue_v2`, `map_icon`, `stat_icon`) — chưa từng migrate sang Dark Inventory Style dù các HUD khác (QuestTracker, Minimap, Dialogue, CharacterPopup) đã xong theo D-041. Rút kinh nghiệm từ đợt Fishing UI (D-055): **asset mới phải khớp 1:1 kích thước pixel file gốc**, không đổi bất kỳ `RectTransform`/`Image.Type`/`Slider` hay code layout nào trong `PlayerHUDController.cs`/`UnifiedGameplayHudController.cs` — chỉ thay `Sprite` reference. Theo `DarkLightFantasyUIStyleGuide.md`: charcoal/walnut nền, viền antique gold mảnh, sapphire tiết chế; Health giữ đỏ, Stamina giữ xanh lá, Experience giữ xanh dương (đổi khung/viền, không đổi tông màu fill để giữ ngôn ngữ đọc nhanh trạng thái). Không đổi `PlayerHUDController`/`UnifiedGameplayHudController` public API, `Image.fillAmount` origin, gameplay/stat/save logic — thuần presentation. Xem chi tiết tại `Handoffs/ClaudeToCodex.md`. | Gameplay HUD presentation migration |
| D-058 | Audio SFX system cho MainMenu/IntroCutscene/MapNhat | **Accepted từ yêu cầu — 2026-09-28** | Mở rộng `SoundFXManager` với `PlaySfx(string sfxId, float volumeScale)` resolve clip qua `SoundFXLibrary` (hạ tầng có sẵn nhưng chưa từng dùng), không tạo persistent singleton mới. SFX ID dùng dot-namespace `sfx.<category>.<action>`, file audio tại `Assets/Resources/Audio/SFX/<Category>/`. Toàn bộ catalog, convention và thứ tự triển khai theo phase (UI chung → MainMenu/IntroCutscene → MapNhat gameplay) đã liệt kê đầy đủ tại `AudioSfxSystem.md`. Handoff gen audio Phase A cho Codex tại `Handoffs/ClaudeToCodex.md`. | Phase A SFX |

## Quy tắc cập nhật

Mỗi decision khi Accepted cần ghi:

- Ngày và người/nhóm chốt.
- Lý do.
- Hệ thống/tài liệu bị ảnh hưởng.
- Có cần migration data hoặc UI không.

Ví dụ:

```text
D-005 — Accepted — 2026-xx-xx
Manual save bị khóa khi player đang trong combat hoặc trong danger area.
Lý do: tránh restore enemy/projectile transient state phức tạp ở version đầu.
Ảnh hưởng: CombatState query, Pause save button disabled reason, QA save matrix.
```

## Chi tiết quyết định Phase 4 — 2026-08-22

```text
D-020 — Accepted — 2026-08-22 — Claude (Phase 4 baseline, người dùng xác nhận)
Domain code (InventoryManager/EquipmentManager/PlayerSpawnReadinessSource) chỉ biết
IItemResolver.TryResolve(itemId, out item); ResourcesItemResolver (Resources.LoadAll) là
implementation duy nhất hiện có. Lý do: tách domain khỏi cơ chế load cụ thể, cho phép đổi backend
(Addressables, catalog asset) sau này mà không sửa domain logic.
Ảnh hưởng: Assets/Scripts/Inventory/IItemResolver.cs, ResourcesItemResolver.cs;
InventoryManager.LoadFromSaveData(resolver overload); PlayerSpawnReadinessSource.

D-021 — Accepted — 2026-08-22 — Claude (Phase 4 baseline, người dùng xác nhận)
Authoring tiếp tục dùng typed asset reference (ItemSO/EquipmentItemSO) trong Inspector
(ItemDatabase.Entry.item, EquipmentCatalog arrays); ranh giới save/runtime chuyển sang stable
itemId (string) qua resolver. Không đổi gì ở authoring layer hiện có.
Ảnh hưởng: không có thay đổi asset/authoring; chỉ xác nhận pattern đã tồn tại là đúng hướng.

D-022 — Accepted — 2026-08-22 — Claude + người dùng (đã hỏi trực tiếp trước khi code)
Lý do: 60 itemId hiện tại (dạng sword_lvl1) chưa theo convention dot-namespace trong
DataAssetStableIdInventory.md, nhưng bulk-rename 60 asset là rủi ro cao (có thể vỡ
ItemDatabase/EquipmentCatalog reference) và không phải "thay đổi nhỏ nhất có thể kiểm chứng".
Chưa có save nào release nên không có migration cần thiết — chỉ cần chốt rằng dạng legacy này
CHÍNH THỨC là stable ID hợp lệ.
Ảnh hưởng: ContentValidation.md giữ nguyên legacy ID ở mức Warning; DataAssetStableIdInventory.md
migration gate "chốt mapping" coi như đã hoàn tất bằng quyết định này, không phải bằng rename.
```

## Chi tiết quyết định Phase 6 — 2026-08-22

```text
D-014 — Accepted — 2026-08-22 — Claude (Phase 6 baseline)
Obtain objective hỗ trợ hai semantics tách biệt qua field ObtainObjectiveMode trên
QuestObjectiveDefinition thay vì một rule ngầm định áp cho toàn hệ thống Quest:
- CountAcquired: counter tăng theo InventoryItemAdded, không giảm khi item bị dùng/bán/equip.
- RequirePossession: không dùng counter; kiểm tra InventoryManager.HasItemId(itemId, targetCount)
  mỗi khi có InventoryItemAdded khớp target, hoàn thành objective ngay khi đang sở hữu đủ.
Ly do: TutorialAndQuestProgression.md yeu cau ro "khong tron hai semantics trong cung type ma
khong co field cau hinh ro" -- field tuong minh de designer chon dung y muon tung quest thay vi
Claude tu quyet dinh mot rule chung.
Anh huong: Assets/Scripts/Quest/ObtainObjectiveMode.cs, QuestObjectiveDefinition.cs,
QuestManager.HandleObtain, InventoryManager.HasItemId (moi, additive).
```

## Chi tiết quyết định Phase 8 — 2026-08-23

```text
D-015 — Accepted — 2026-08-23 — Claude (Phase 8 baseline)
ResourceNodeInteractable luu nextRespawnUtcTicks = DateTime.UtcNow.Ticks + respawnDuration tai thoi
diem harvest. IsAvailable so sanh truc tiep saved ticks voi DateTime.UtcNow.Ticks hien tai -- khong
Update()/polling moi frame, khong tick nen tang. Vi dung UTC tuyet doi (khong phai playtime tich luy
trong game), thoi gian thuc troi qua ke ca luc ung dung dong deu tu nhien duoc tinh vao respawn --
day la lua chon don gian nhat thoa man "khong lam save phinh", khong phai gia dinh ngam ve balance.
Chua co catch-up/rate-limit/batch simulation cho truong hop offline rat dai (vi du hang tram node
respawn dong loat) -- de lai cho phase sau neu game design can gioi han.
Anh huong: Assets/Scripts/World/ResourceNodeInteractable.cs, WorldObjectState.NextRespawnUtcTicks,
WorldObjectSaveData.nextRespawnUtcTicks.
```

## Chi tiết quyết định Phase 9 — 2026-08-23

```text
D-017 — Accepted — 2026-08-23 — Claude (Phase 9 baseline)
Prompt xac nhan chi hien khi GameSessionManager.IsDirty == true. GameplaySessionController.
RequestReturnToMainMenu()/RequestQuit() fire OnConfirmationRequired(kind) va khong tu lam gi khac --
UI hien popup roi goi ConfirmSaveAndReturn/ConfirmReturnWithoutSaving/CancelReturnToMainMenu (hoac
ban Quit tuong ung). Cancel khong doi GameState (dung "popup xac nhan la UI navigation con" cua
UIAndInteractionFlows.md). Save-and-X chi thuc su chuyen scene/quit sau khi ghi file thanh cong;
that bai giu nguyen Paused va bao OnOperationFailed.
Anh huong: Assets/Scripts/GameManagers/GameplaySessionController.cs,
GameplaySessionConfirmationKind.cs, GameplaySessionOperationResult.cs.

D-024 — Accepted — 2026-08-23 — Claude (Phase 9 baseline)
Ly do: RuntimeArchitecture.md "Event rules" da de nghi pattern IsRestoring nhung chua co
implementation cu the cho dirty-tracking; task Phase 9 yeu cau de xuat contract toi thieu neu chua
chot. Field/gameplay progression that su moi lam dirty; di chuyen don thuan khong dirty vi muc dich
dirty-flag la canh bao "co the mat tien do chua luu khi roi gameplay", khong phai theo doi moi thay
doi vi tri.
Anh huong: Assets/Scripts/GameManagers/GameSessionManager.cs (IsDirty/IsRestoring/MarkDirty/
ClearDirty/BeginRestore/EndRestore), SessionDirtyTracker.cs (moi), Assets/Scripts/World/
WorldDomainEvents.cs (moi), PlayerSpawnReadinessSource.cs (boc RestoreAll trong BeginRestore/
EndRestore).
```

**D-005 (Save khi combat) và D-012 (Autosave) vẫn `Proposed`, KHÔNG triển khai ở Phase 9:**
project hiện chưa có khái niệm "combat state"/"danger area" nào trong `GameStateManager` hay domain
khác để D-005 có thể bám vào (không có state/flag nào đánh dấu "đang combat"); D-012 (autosave) nằm
ngoài phạm vi Phase 9 theo đúng ghi chú "Chưa có ở foundation; manual save trước" — Phase 9 chỉ làm
manual Save Game. Không tự chế một cơ chế combat-lock để "xong" D-005 vì sẽ là quyết định gameplay
chưa được xác nhận; để lại nguyên trạng cho phase sau khi có combat state thật.

## Chi tiết quyết định D-019 — 2026-09-07

```text
D-019 — Accepted — 2026-09-07 — Claude (theo yêu cầu trực tiếp của người dùng)
MapNhat được chốt làm world scene production chính thức đầu tiên. Lý do: người dùng muốn mang toàn bộ
gameplay UI (HUD, Pause, Inventory/Equipment, Settings, Quest, Shop/Crafting, Tutorial) sang MapNhat và
dùng chung một GameStateManager/UI state machine với DemoScene, thay vì chỉ dừng ở portability
smoke-test như trạng thái trước đó. DemoScene giữ nguyên vai trò integration playground (D-001 không
đổi). Kế hoạch triển khai chi tiết: MapNhatUiStateMachinePlan.md.
Ảnh hưởng: Assets/Scenes/MapNhat.unity (thêm _UI + _SceneContext service đầy đủ), có thể ảnh hưởng
Build Settings/SceneFlowService target khi có nhiều world scene sau này. Chưa xác định topology cho
world scene thứ ba trở đi; sẽ đánh giá lại khi có nhu cầu cụ thể.
```

## Những quyết định không được hard-code trước khi chốt

- Hình phạt khi chết.
- Save/load trong combat.
- Offline resource regeneration.
- Character appearance serialization.
- Daily Quest reset clock.

Code foundation nên cung cấp extension point nhưng không tự chọn gameplay rule thay designer.
# Player HUD and transient stamina

- **Status:** Accepted — 2026-08-25.
- DemoScene/world gameplay HUD hiển thị đúng Health và Stamina ở góc trái trên Canvas.
- Stamina là tài nguyên runtime không persistent: drain khi sprint có chuyển động, regenerate khi ngừng;
  không thay đổi save schema/version. Presentation dùng prefab và event từ `PlayerStat`, không sở hữu state.

# Character Popup read-only equipment overview

- **Status:** Accepted — 2026-08-26.
- Character Popup mở từ phím `C` hoặc Stat Button trong UnifiedGameplayHUD và dùng
  `GameplayMenuPage.Character` đã có.
- Equipment preview chỉ đọc state từ `EquipmentManager`; không cho kéo thả, tháo hoặc thay item trong
  popup. Mọi tương tác equipment vẫn thuộc Inventory/Equipment UI hiện có.
- Character Stat panel chỉ trình bày các giá trị runtime từ `PlayerStat`; không sở hữu stat calculation,
  equipment mutation hay save data.

# Dialogue UI visual language

- **Status:** Accepted từ yêu cầu — 2026-08-26.
- Dialogue dùng bộ LightFantasy module gỗ sồi, parchment, vàng cổ, lá xanh và sapphire đồng bộ HUD,
  Quest và Tutorial.
- Portrait, nameplate, text area, choice button và continue indicator là presentation tách rời; tên NPC,
  nội dung và lựa chọn dùng TMP/Digital Disco, không bake vào texture.
- Dialogue UI không sở hữu quest outcome, shop/crafting transaction hoặc save data.

# Orynthals intro cutscene

- **Status:** Accepted từ yêu cầu content — 2026-09-06.
- Intro gồm Logo Intro, năm clip truyện, và Outro Transition; data asset giữ stable cutscene/segment ID,
  còn Timeline chỉ điều phối thứ tự presentation.
- New Game và Development preview có thể phát Intro; Continue không phát lại. Skip cinematic không
  ghi save, không đổi quest/tutorial/world progression.
- `GameState.Cutscene` dừng world và khóa gameplay input, nhưng cho phép UI/cursor để người chơi điều
  khiển thoại hoặc skip.

# Persistent chest left-click and delayed loot commit

- **Status:** Accepted từ yêu cầu — 2026-08-26.
- Chest dùng Interact cursor và chuột trái trong phạm vi; phím `Gameplay/Interact`/E không mở chest.
- Chest chạy đủ animation mở bốn frame trước khi tạo loot presentation. Loot chỉ được commit vào
  Inventory sau khi bay tới Player; persistent opened state chỉ đổi sau khi transaction thành công.
- Hệ thống kiểm tra sức chứa trước khi bắt đầu và kiểm tra lại trước khi tạo loot. Nếu không thể commit,
  chest trở về trạng thái đóng, không mất reward và vẫn có thể thử lại.

# D-043 — Equipment crafting accordion

- **Status:** Accepted từ yêu cầu — 2026-09-25.
- Crafting UI dùng đúng bảy mục `Head`, `Body`, `Foot`, `Ring`, `Necklace`, `Shield`, `Sword`; các mục
  luôn hiện theo thứ tự cố định và chỉ một mục được xổ danh sách blueprint tại một thời điểm.
- Chọn category chưa hiển thị chi tiết món. Chỉ sau khi chọn blueprint, panel chi tiết mới render output,
  icon nguyên liệu và số lượng `đang có/cần`.
- UI crafting equipment không hiển thị recipe Consumable/Material/Other và không có thanh kinh nghiệm
  crafting. Transaction/data ownership vẫn thuộc `CraftingManager`; UI chỉ render và phát intent.

# D-044 — Character Popup unified outer board, independent inner frames

- **Status:** Accepted từ yêu cầu — 2026-09-26.
- Character Popup dùng một outer board Dark Inventory thống nhất cho toàn `Window 760×410`, giữ hai
  vùng nội dung Equipment và Character Stats cùng divider dọc.
- Outer board không bake các khung nội dung. Title, LevelBadge, bốn stat section và bảy equipment slot
  là các `Image` riêng; title/level và stat section dùng sprite 9-slice để đổi kích thước độc lập mà
  không phải gen lại outer board.
- Text, icon trang bị, value động và CloseButton tiếp tục là object runtime; thay đổi chỉ thuộc
  presentation, không đổi `CharacterPopupUI`, `EquipmentManager`, `PlayerStat` hoặc menu lifecycle.
