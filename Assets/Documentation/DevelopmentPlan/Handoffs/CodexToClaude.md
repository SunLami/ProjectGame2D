# Codex → Claude Handoff

Status: `VERIFIED`

Ngày: 2026-08-22
Feature: Phase 3 MainMenu New Game/Continue UI

## UI đã triển khai

- Dựng scene-authored UI trong `Assets/Scenes/MainMenu.unity/_UI/MainMenuCanvas/MainMenuRoot` bằng
  Unity MCP; không tạo hierarchy lúc runtime.
- Landing có `New Game`, `Continue`, `Settings`, `Quit`.
- New Game/Continue dùng chung selector đúng ba slot.
- Slot view hiển thị trạng thái `Empty`, `Valid`, `Corrupted`, `IncompatibleVersion` bằng chữ; không chỉ
  dùng màu.
- Slot hợp lệ hiển thị dữ liệu backend thật: level, stable area ID, total play time và last-saved local
  time. Không hiển thị `characterName` hoặc `tutorialCompleted` vì hai domain này chưa tồn tại.
- Overwrite và delete luôn có confirm chỉ rõ slot; Quit có confirm.
- Operation failure có modal thân thiện.
- Khi `GameState.Loading`, `CanvasGroup` khóa interact/raycast và bỏ focus để chống double-submit.
- Main Menu Settings là subpage riêng, dùng chung `SettingsService`; không push
  `GameplayMenuPage.Settings`. Có SFX, Music, Fullscreen, Save và Cancel/restore snapshot.
- UI subscribe `OnSaveSlotListChanged` và `OnOperationFailed`; không polling.
- UI dùng `InputSystemUIInputModule`/project `UI` map; focus mặc định và `UI/Cancel` cho slot page,
  Settings và popup.
- Toàn bộ TMP text dùng `Assets/Fonts/DigitalDisco SDF v3.asset`.

## File thay đổi

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scripts/UI/MainMenuSaveSlotsUI.cs`
- `Assets/Documentation/DevelopmentPlan/Verification/*`
- Tài liệu handoff/report Phase 3.

## Verification

- Script validation: 0 diagnostic.
- MainMenu scene validator: 0 issue.
- Console trong các luồng kiểm tra: 0 error, 0 warning.
- Landing: focus mặc định `NewGameButton`.
- Settings: focus mặc định `SfxSlider`; simulated keyboard Escape qua Input System đóng Settings,
  restore Landing và focus `NewGameButton`.
- Quit: confirm mở và focus mặc định `CancelButton`.
- New Game Slot 1: `MainMenu → Loading → DemoScene/Playing`, Player Level 1.
- Pause → Return Main Menu: `MainMenu`, active session đã clear.
- Continue Slot 1: `MainMenu → Loading → DemoScene/Playing`, Player restore Level 1 và saved position
  `(0,0)`.
- Slot 1 test save do Codex tạo đã được xóa sau verification; Continue trở lại disabled.
- Manual physical gamepad: `BLOCKED_MANUAL_TEST` — chưa có người thao tác controller thật.

Ảnh evidence nằm trong `Assets/Documentation/DevelopmentPlan/Verification/`.

## Backend handoff đã xác nhận

- Commit `5b83d1a7` đã sửa metadata readback và thêm Player capture foundation.
- UI verify end-to-end từ save mới: `LEVEL 1`, `AREA area.tutorial`, play time thật và timestamp local
  khác `0` đều lấy từ `RefreshSlots()`/`SaveSlotInfo`.
- `characterName` và `tutorialCompleted` vẫn được ẩn đúng chủ đích; không hiển thị placeholder giả.
- Save-on-return vẫn thuộc D-017/Phase 9; UI không gọi `PlayerSaveCapture` và không tự ghi DTO.
- Slot 1 test được tạo để verify metadata rồi xóa qua UI; ba slot trở lại trạng thái trống.
- Screenshot mới: `Verification/MainMenu_UI_Metadata_Final.png`.

## Remaining verification

- Automated virtual gamepad PASS cho MainMenu `Navigate`, `Submit`, `Cancel`. Trong lần test này phát
  hiện và sửa deferred default-focus sau frame đầu; console sạch.
- Manual physical gamepad vẫn `BLOCKED_MANUAL_TEST`; gameplay controls chưa được đánh dấu PASS.
- Responsive alternate-aspect test: `NOT RUN` — `Screen.SetResolution` trong Editor không thay đổi Game
  View capture thật, nên không dùng screenshot đó làm evidence.
- Area hiện hiển thị stable ID thật. Khi có Area catalog/display-name resolver, backend cần cung cấp
  presentation value hoặc read model; UI không tự biến ID thành tên giả.

## Phạm vi Claude không nên chỉnh trực tiếp

- Không chỉnh hierarchy/layout/colors/font của `MainMenuRoot`.
- Nếu contract field/event thay đổi, cập nhật handoff để Codex rebind bằng Unity MCP.

---

# Phase 5 Tutorial Overlay UI

Status: `VERIFIED`

Ngày: 2026-08-22
Feature: Phase 5 Input Tutorial presentation

## UI đã triển khai

- Dựng scene-authored hierarchy tại
  `Assets/Scenes/DemoScene.unity/_UI/UICanvas/TutorialOverlayRoot` bằng Unity MCP.
- Prompt dùng layout toast gọn 360x76, neo góc dưới phải để không che Inventory/Equipment; popup xác
  nhận Skip vẫn là modal toàn màn hình vì chỉ mở theo yêu cầu người chơi.
- `TutorialOverlayUI` là presentation adapter đặt ngoài `Assets/Scripts/Tutorial/`; không sở hữu
  progression, không đổi `GameState`, không đổi time scale.
- Đọc `TutorialManager.Instance.CurrentStep` ngay trong `OnEnable`; có fallback bind ở `Start`
  cho trường hợp thứ tự `Awake/OnEnable` khiến singleton chưa sẵn sàng.
- Subscribe `OnStepChanged` để cập nhật `InstructionText`; subscribe `OnTutorialCompleted` để
  đóng popup và ẩn prompt. UI cũng ẩn khi `CurrentStep == null`.
- Nút `Skip` chỉ mở confirm. Chỉ `ConfirmSkipButton` mới gọi `TutorialManager.Skip()`;
  `CancelSkipButton` đóng popup và trả focus về Skip.
- Popup chọn `CancelSkipButton` mặc định; hai action có explicit left/right navigation.
- Toàn bộ TMP text dùng `DigitalDisco SDF v3`.
- Không sửa script trong `Assets/Scripts/Tutorial/`, `AreaTriggerZone.cs`, hoặc Tutorial data
  contract. Không cần backend gap mới.

## File thay đổi

- `Assets/Scenes/DemoScene.unity`
- `Assets/Scripts/UI/TutorialOverlayUI.cs`
- `Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md`

## Verification

- Script validation: 0 diagnostic.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Serialized references của panel, text, Skip, Confirm và Cancel: đầy đủ.
- Font runtime/scene: `DigitalDisco SDF v3`.
- New Game tutorial presentation (Editor Play, tool-driven): cả sáu authored step lần lượt hiện đúng
  stable step ID và `InstructionText`; panel hiện ở từng step và tự ẩn khi hoàn tất.
- Domain-event update: phát `PlayerSprinted` ở step Sprint chuyển UI ngay sang nội dung Attack.
- Skip confirm: click Skip chỉ mở popup và `IsCompleted == false`; click Confirm mới làm
  `IsCompleted == true`, đóng popup và ẩn panel.
- Continue giữa chừng: restore `tutorial.controls.sprint`, disable/enable overlay, UI hiện ngay
  đúng step Sprint mà không chờ event tiếp theo.
- EditMode: 28/28 PASS.
- PlayMode: 32/32 PASS.
- Console: 0 error.

## Giới hạn kiểm tra

- Ba luồng UI đã được chạy trong Play Mode bằng Unity MCP. Việc thao tác vật lý toàn bộ chuỗi bằng
  bàn phím/gamepad và tạo save thật qua MainMenu vẫn nên được owner chạy một vòng acceptance cuối;
  phần save/restore backend tương ứng đã có PlayMode coverage.

## Phạm vi Claude không nên chỉnh trực tiếp

- Không chỉnh hierarchy/layout/colors/font của `TutorialOverlayRoot`.
- Nếu Tutorial contract cần icon hoặc presentation field mới, cập nhật handoff trước để Codex rebind;
  UI hiện không tự chế dữ liệu.

## Final Inventory overlap acceptance

- Đã mở Inventory thật trong Play Mode tại step `tutorial.controls.open_inventory`; event chuyển
  manager sang `tutorial.controls.equip_item` và toast vẫn hiển thị.
- Ở Game View 1920x1080, bounds đo được:
  - Tutorial prompt: `x 1017.60..1881.60, y 38.40..220.80`.
  - Equipment panel: `x 514.56..898.56, y 316.32..892.32`.
  - Inventory panel: `x 888.96..1453.44, y 313.44..895.20`.
- `Rect.Overlaps` với Equipment và Inventory đều `false`. Phase 5 UI overlap issue đã đóng.

---

# Next Claude Task — Phase 6 Quest Backend

Status: `READY_FOR_CLAUDE`

Claude hãy bắt đầu Phase 6 theo `Roadmap.md`, `TutorialAndQuestProgression.md`,
`DataDrivenDevelopment.md`, `SaveAndWorldPersistence.md`, `QualityStrategy.md` và các accepted
decision liên quan.

## Backend scope

- Quest Definition data-driven với stable `questId`, prerequisite IDs, objective definitions và
  rewards; không mutate definition asset ở runtime.
- Runtime Quest Progress tách khỏi Definition và Save DTO.
- Quest catalog/resolver cùng editor/content validation cho ID rỗng/trùng, missing target/reference,
  prerequisite cycle, invalid target count và reward.
- Typed gameplay event contracts và objective tracking cho Talk, Obtain, Craft, Purchase, Gather,
  Kill. Không polling mỗi frame và không phụ thuộc click UI.
- NPC quest interaction/turn-in validation qua capability/service rõ ràng; NPC không sửa internals
  của QuestManager.
- Tutorial Quest chain và prerequisite gate cho Main Quest. Người chơi chưa làm Tutorial Quest vẫn
  được tự do khám phá/craft/shop/gather.
- Atomic/idempotent turn-in: không consume/grant một phần, không duplicate reward khi double-submit
  hoặc restore.
- Save/restore active/completed quest, objective index/counters; restore không phát progression event
  hoặc grant reward. Cập nhật save version/migration/default/fixtures đúng tài liệu nếu schema đổi.
- EditMode/PlayMode tests cho quest graph, từng objective event, save round-trip, lifecycle
  subscription, double turn-in và Main Quest unlock đúng một lần.
- Tạo ít nhất hai Quest Definition variants dùng chung runtime handlers và chạy Content Validator.

## Boundary

- Không dựng hoặc chỉnh Quest Log/Tracker/NPC marker UI; đó là Codex task sau backend handoff.
- Không chỉnh `TutorialOverlayRoot`, `TutorialOverlayUI` hoặc layout Inventory.
- Không thêm logic Shop/Crafting giả vào UI. Nếu transaction service Phase 7 chưa tồn tại, chốt typed
  event contract và test bằng event producer/fake phù hợp, đồng thời ghi rõ integration gap.
- Không tự chế display data cho UI. Handoff phải cung cấp read-model/contract public ổn định.

## Handoff Claude → Codex bắt buộc

- Ghi đầy đủ vào `Handoffs/ClaudeToCodex.md`: public API/events/read models, status semantics,
  objective presentation fields, NPC interaction contract, save/version changes, authored assets,
  test/validator results và known integration gaps.
- Khi backend ổn định và sẵn sàng dựng UI, đánh dấu rõ `READY_FOR_CODEX_UI`.

---

# Phase 6 Quest UI → Claude Follow-up

Status: `VERIFIED`

Ngày cập nhật binding: 2026-08-23

## UI đã dựng

- `DemoScene/_UI/UICanvas/QuestUIRoot`: event-driven Quest Tracker và Quest Log.
- `Assets/Prefabs/Quest/TownElderNPC.prefab` và scene instance
  `DemoScene/_Actors/TownElderNPC`, stable ID `npc.town.elder`.
- NPC offer/accept/turn-in chỉ đi qua `QuestNpcInteractionService`.
- Quest UI đọc `Catalog.AllQuests`/`GetStatus` và subscribe
  `QuestAccepted`/`QuestProgressChanged`/`QuestCompleted`/`MainQuestUnlocked`; không polling.
- Input action `Gameplay/QuestLog`: keyboard `J`, gamepad D-pad Up. Cancel đóng qua state history
  hiện có.
- Toàn bộ text dùng `DigitalDisco SDF v3`.

## API/content binding đã hoàn tất

- UI gọi mới `QuestManager.TryGetProgress` trong mỗi event-driven refresh; không cache
  `QuestProgressSnapshot` qua nhiều frame, không dùng `ToSaveData()` và không reflection runtime.
- Tracker dùng `CurrentObjectiveIndex` để chỉ hiển thị objective hiện tại bằng authored
  `Description`, kèm `ObjectiveCounters[index] / TargetCount`.
- Khi objective cuối chuyển sang `ReadyToTurnIn` và index bằng `Objectives.Count`, tracker giữ
  objective cuối để hiển thị counter hoàn tất (ví dụ `3 / 3`) thay vì mất progress.
- Quest Log dùng authored Description cho toàn bộ objective; chỉ objective active nhận counter.
- `TryGetProgress == false` giữ tracker/empty-state cũ và không dựng counter giả.

## Runtime verification đã qua

- Initial: Tutorial Quest `Available`, Main Quest `Locked`, tracker ẩn.
- Quest Log mở thành `GameplayMenu/QuestLog`, empty state đúng trước accept.
- Player vào trigger: marker `!`, prompt offer đúng `The Blacksmith's Request`.
- Accept qua button/service: Tutorial Quest `Active`, tracker hiện và event refresh chạy.
- Counter Tutorial Quest đã verify tuần tự:
  - Accept: Kill `0 / 2`.
  - Kill event thứ nhất: `1 / 2`.
  - Kill event thứ hai: chuyển objective Wood thành `0 / 3`.
  - Add một Wood: `1 / 3`.
  - Add đủ ba Wood: `3 / 3`, status `ReadyToTurnIn`.
- Hai kill event đúng ID/area + `InventoryManager.AddItem(WoodMaterial, 3)`: status
  `ReadyToTurnIn`, marker đổi `?`.
- Turn-in qua đúng NPC/service: Tutorial Quest `Completed`, `IsMainQuestUnlocked == true`, Main
  Quest `Available`, feedback thành công.
- Accept Main Quest rồi disable/enable `QuestUIRoot`: tracker đọc ngay `A Call to Adventure`
  đang `Active`, không chờ event mới.
- Main Quest tracker hiển thị authored Goblin objective `0 / 1`, sau event đúng chuyển
  `1 / 1` và `ReadyToTurnIn`.
- EditMode: 42/42 PASS.
- PlayMode: 48/48 PASS.
- `QuestLogUI.cs` validation: 0 diagnostic.
- DemoScene validator: 0 issue; Console cuối sau test: 0 error, 0 warning.

---

# Phase 7 Shop/Crafting UI → Claude Follow-up

Status: `VERIFIED`

Ngày: 2026-08-23

## UI/capability đã dựng

- `DemoScene/_UI/UICanvas/CommerceUIRoot`: modal Shop và Crafting được author trực tiếp vào scene
  bằng Unity MCP; không tạo hierarchy runtime và không chỉnh Quest/Inventory/Tutorial UI.
- `ShopCraftingUI` hiển thị stock, giá mua/bán, gold, số lượng đang sở hữu, quantity 1..99, recipe,
  ingredient counter, output và required station. Toàn bộ TMP text dùng `DigitalDisco SDF v3`.
- `TownElderNPC.prefab` giữ nguyên Quest capability/visual, được bổ sung
  `TownElderCommerceInteractionUI` + world-space `CommerceInteractionCanvas` với hai capability
  SHOP/CRAFT cho `npc.town.elder`; không tạo NPC mới.
- Mọi giao dịch NPC đi qua `ShopNpcInteractionService`/`CraftingNpcInteractionService`; UI không
  gọi trực tiếp transaction API trên manager và không tự kiểm tra ownership.
- Mọi enum fail được map thành thông báo riêng (`InsufficientGold`, `InsufficientItemQuantity`,
  `InsufficientInventoryCapacity`, `WrongStation`, `InsufficientIngredients`, v.v.).
- Khi modal mở, `PlayerInput` được deactivate để chống điều khiển nhân vật/double interaction; modal
  đóng bằng nút X, Escape hoặc gamepad East. `GameState` giữ `Playing` vì transaction backend Phase 7
  chặn mọi state có `AllowsGameplayInput == false`.

## Runtime verification đã qua

- DemoScene thật: `ShopManager`, `CraftingManager`, `ShopCraftingUI`, PlayerInput đều tồn tại;
  state `Playing`, console không error/warning trong smoke flow.
- Shop UI: mua 4 Wood qua nút BUY, gold `100 → 80`; bán lại 4 Wood qua SELL, gold `80 → 88`, đúng
  multiplier hiện tại; inventory quantity cập nhật theo event.
- Mua Health Potion khi gold = 0: giao dịch từ chối và UI hiện đúng `Not enough gold.`; không crash.
- Crafting UI: mua 3 Wood rồi craft `recipe.material.plank` qua nút CRAFT thành công, Wood `3 → 0`,
  Wood Plank `0 → 1` và feedback `Crafted Wood Plank.`.
- Chọn `recipe.consumable.health_potion` với stationTag rỗng: bị từ chối và UI hiện đúng yêu cầu
  crafting station (`WrongStation`). Thành công tại `station.forge` đã được backend Phase 7 verify;
  DemoScene hiện chưa có production forge interaction để cấp stationTag đó.
- EditMode: 46/46 PASS. PlayMode: 64/64 PASS.
- Content Validation: 0 error, 60 warning baseline, 77 asset checked.
- DemoScene validator: 0 issue; prefab không missing script/broken reference.

## Backend gap

- Không phát hiện API/field thiếu mới. Không chỉnh `Assets/Scripts/Shop/` hoặc
  `Assets/Scripts/Crafting/`.
- Lưu ý kiến trúc đã có từ backend: nếu sau này Product Design yêu cầu Shop/Crafting dùng
  `GameplayMenuPage.Shop/Crafting` (pause world), transaction gate hiện tại sẽ trả
  `GameplayNotAllowed`. Phase này dùng modal state `Playing` + khóa PlayerInput để giữ đúng contract.

---

# Phase 8 World Persistence Scene Integration → Claude Follow-up

Status: `VERIFIED`

Ngày: 2026-08-23

## Scene presentation đã dựng

- Dùng Unity MCP author trực tiếp visual placeholder và interaction prompt cho ba entity tương tác
  trong `DemoScene/_World`: `Chest_TownGeneral`, `Pickup_AncientRelic`,
  `ResourceNode_WoodLog`.
- `PersistentWorldInteractionUI` chỉ gọi API public `TryOpen`, `TryCollect`, `TryHarvest` và đọc
  `IsOpened`/`IsCollected`/`IsAvailable`; không cấp item, không ghi persistence và không truy cập
  internals của World backend.
- Mỗi entity có trigger riêng và dùng action `Gameplay/Interact` hiện có (keyboard E/gamepad South).
  Prompt chỉ hiện khi player trong range và entity còn tương tác được.
- Chest có closed visual + `OpenedIndicator` được bind vào field `_openedIndicator` có sẵn.
- Resource node có available logs visual + `DepletedIndicator` được bind vào field
  `_depletedIndicator`; presentation đọc `IsAvailable` để tự trở lại available sau cooldown.
- Unique pickup có relic placeholder và vẫn để backend tự `SetActive(false)` toàn object sau collect.
- `BossTracker_ForestGuardian` được đặt cùng vị trí world với `ForestGuardianBoss` và mang
  `BossDefeatPresentation`. Banner/defeated indicator là con của tracker, không gắn vào boss, nên
  vẫn tồn tại sau khi `EnemyUniversal` destroy boss corpse.
- Toàn bộ text mới dùng `DigitalDisco SDF v3`. Không chỉnh `QuestUIRoot`, `CommerceUIRoot`,
  Inventory, Tutorial, MainMenu, MapManager hoặc SoundFXManager.

## Runtime verification đã qua

- Chest: proximity prompt hiện; tương tác qua presentation mở chest; `IsOpened == true`, closed
  visual tắt và `OpenedIndicator` bật.
- Unique pickup: proximity prompt hiện; collect thành công và GameObject tự inactive đúng backend.
- Resource node: proximity prompt hiện; harvest thành công; `IsAvailable == false`, available
  visual tắt và `DepletedIndicator` bật.
- Boss: gây lethal damage làm `BossDefeatTracker.IsDefeated == true`; sau khi
  `ForestGuardianBoss` đã bị destroy, tracker/banner vẫn tồn tại và hiển thị
  `FOREST GUARDIAN DEFEATED`.
- Console trong runtime smoke flow: 0 error, 0 warning.
- EditMode: 48/48 PASS. PlayMode: 85/85 PASS.
- Content Validation: 0 error, 60 warning baseline, 83 asset checked.
- DemoScene validator: 0 issue, không missing script/broken reference.

## Backend gap

- Không phát hiện API/field thiếu mới; không chỉnh file nào trong `Assets/Scripts/World/`,
  `WorldObjectRegistry` hoặc `PlayerSpawnReadinessSource`.
- Visual hiện là base placeholder có thể thay asset về sau mà không đổi interaction/persistence
  contract.

---

# Phase 9 Save/Load/Return/Quit UI → Claude Follow-up

Status: `VERIFIED`

Ngày: 2026-08-23

## Pause UI đã dựng

- Mở rộng `PauseMenuUI` hiện có và author trực tiếp hierarchy qua Unity MCP; không tạo controller
  hoặc save service thứ hai.
- Nút `SAVE GAME`, `LOAD GAME`, `RETURN MAIN MENU`, `QUIT DESKTOP` chỉ gọi
  `GameplaySessionController`; UI không capture DTO, không đọc repository và không gọi
  `Application.Quit()`.
- Save lắng nghe `OnSaveSucceeded` để hiển thị local timestamp sau atomic save và
  `OnOperationFailed` để hiện message thân thiện do backend cung cấp.
- Load overlay có ba slot, hiển thị `Empty`/`Valid`/`Corrupted`/`IncompatibleVersion`, metadata
  Level/Area/Play Time/Last Save, đánh dấu `ACTIVE` theo `ActiveSlotId`, và chỉ enable LOAD khi
  `CanLoad(slotId)` trả true.
- Return/Quit dùng đúng `OnConfirmationRequired` và popup ba nhánh riêng:
  Save and Return/Quit, Without Saving, Cancel; popup không tạo GameState mới.
- Escape/gamepad East đóng popup hoặc Load overlay; selection mặc định ưu tiên slot loadable hoặc
  nút Cancel/Back.
- Save/Load/Return/Quit đồng loạt disable khi `IsBusy`; runtime smoke xác nhận ở state `Saving` cả
  bốn nút đều non-interactable.
- Hiển thị `UNSAVED CHANGES`/`ALL CHANGES SAVED` từ controller dirty state. Toàn bộ TMP text mới
  dùng `DigitalDisco SDF v3`.
- Không chỉnh `GameplaySessionController`, `SessionDirtyTracker`, `GameSessionManager`,
  `SceneFlowService` hoặc các UI root ngoài PauseMenu.

## Verification

- Runtime DemoScene: Pause state mở đúng, Save/Load/Return/Quit đều interactable khi idle.
- Load overlay đọc dữ liệu thật hiện có: slot incompatible và slot empty đều hiển thị đúng và LOAD
  disabled. Môi trường kiểm tra hiện không có valid slot thứ hai nên không ghi đè/tạo save người dùng
  chỉ để chạy destructive cross-slot manual flow; backend cross-slot flow đã có PlayMode coverage.
- Dirty runtime: Return popup hiện đúng `SAVE AND RETURN` / `RETURN WITHOUT SAVING` / `CANCEL`;
  Cancel đóng popup và không transition.
- Dirty runtime: Quit popup hiện đúng `SAVE AND QUIT` / `QUIT WITHOUT SAVING` / `CANCEL`;
  không gọi quit trực tiếp từ UI.
- Busy runtime: Save/Load/Return/Quit đều disabled.
- Console trong UI smoke flow: 0 error, 0 warning.
- `PauseMenuUI.cs`: 0 compile error/diagnostic có ý nghĩa.
- EditMode: 48/48 PASS. PlayMode: 118/118 PASS.
- Content Validation: 0 error, 60 warning baseline, 83 asset checked.
- DemoScene validator: 0 issue, không missing script/broken reference.

## Backend gap

- Không phát hiện API/field thiếu mới. Phase 9 UI binding hoàn tất theo contract hiện tại.

---

# Phase 10 Player Build Manual Verification → Claude Follow-up

Status: `BACKEND_GAP_FOUND`

Ngày: 2026-08-23

## Kết quả click-through

- `C:\Users\havin\Phase10PlayerBuild\ProjectGame2D.exe`: tồn tại và launch thành công trên máy
  thật.
- MainMenu hiển thị đúng. Slot 1 được giữ nguyên, không chọn và không ghi đè.
- Chọn `CREATE` trên Slot 2 đang trống: PASS; transition vào DemoScene thành công.
- DemoScene nhận input gameplay: chuột hoạt động, phím di chuyển làm nhân vật di chuyển.
- Đã Skip tutorial qua popup confirm để loại trừ khả năng Tutorial overlay giữ input.
- **FAIL tại bước mở Pause Menu:** nhấn Escape khi Player window đang focus không mở
  `PauseMenuUI`. Thử lại sau khi Tutorial đã hoàn toàn ẩn vẫn không mở Pause.

## Backend/integration gap chặn milestone

- Vì Pause Menu không thể mở trong Player build, không thể truy cập `Save Game`, nên chuỗi bắt
  buộc `Save → Return Main Menu → Continue → verify restore → Quit Desktop` không thể tiếp tục.
- Các hạng mục vị trí nhân vật, inventory, quest state, tutorial state, world state và cross-slot
  isolation vì vậy có trạng thái `NOT VERIFIED`, không được xem là PASS.
- Đây là blocker cho content-ready verification của Phase 10. Cần Claude kiểm tra binding/runtime
  path của action `UI/Cancel`/Pause trong Player build trước khi yêu cầu chạy lại click-through.
- Slot 2 đã được dùng để bắt đầu New Game phục vụ test; Slot 1 save cũ dạng
  `IncompatibleVersion` không bị thay đổi.

## Phạm vi thay đổi

- Không sửa UI, scene, script, Build Settings hay backend.
- Chỉ cập nhật handoff này theo yêu cầu verification.

---

# Phase 10 Pause Input Fix → Claude Follow-up

Status: `READY_FOR_PHASE10_REVERIFICATION`

Ngày: 2026-08-23

## Root cause

- `MainMenu/EventSystem`, `DemoScene/EventSystem` và `GameInputCoordinator._projectActions` cùng thao
  tác trên một serialized `InputActionAsset`.
- Trong transition `MainMenu → DemoScene`, lifecycle hai scene overlap ngắn. Coordinator mới có thể
  enable `UI/Cancel` trước khi `InputSystemUIInputModule` của MainMenu cũ chạy `OnDisable` và disable
  action trên asset dùng chung.
- Direct Play DemoScene không có outgoing MainMenu module nên không tái hiện; Player build transition
  có đúng thứ tự gây mất Cancel. Player log không có exception, phù hợp với lifecycle race này.

## Fix tối thiểu

- `GameInputCoordinator` tạo runtime copy riêng từ `_projectActions` và chỉ enable/disable
  `UI/Cancel` trên copy do coordinator sở hữu.
- Runtime copy được destroy trong `OnDestroy`; `PlayerInput` và `InputSystemUIInputModule` tiếp tục
  giữ ownership hiện tại, không đổi scene hierarchy, serialized reference hay UI layout.
- Không sửa `GameStateManager`, save/progression/world backend hoặc Build Settings.

## Regression coverage và verification

- Thêm `GameInputCoordinatorPlayModeTests`:
  - mô phỏng project action bị disable sau khi coordinator enable; Cancel vẫn chuyển
    `Playing → Paused`;
  - disable/enable coordinator không double-subscribe.
- Targeted regression: 2/2 PASS.
- Live DemoScene: runtime asset khác project asset, `UI/Cancel` enabled; simulated keyboard Escape
  chuyển `Playing → Paused`.
- EditMode: 58/58 PASS.
- PlayMode: 121/121 PASS.
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset checked.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Build Settings giữ nguyên: MainMenu index 0, DemoScene index 1.
- Player build mới: `C:\Users\havin\Phase10PlayerBuild_Codex\ProjectGame2D.exe`; build success,
  0 error, 0 warning, 515.85 MB.
- Player smoke đạt `MainMenu → New Game Slot 3 → DemoScene`; Slot 1 không bị chọn/ghi đè.

## Manual re-verification còn lại

- Windows UI automation của môi trường gửi click được nhưng không tạo raw keyboard event mà Unity
  Input System trong Player nhận được (cả `I` và `Escape` đều không phản hồi); vì vậy không dùng kết
  quả synthetic key này để tuyên bố Player acceptance PASS/FAIL.
- Cần người dùng nhấn Escape vật lý trên build mới để xác nhận Pause mở, sau đó chạy full
  `Save → Return → Continue → restore → Quit`. Chưa tuyên bố Phase 10 `CONTENT_READY`.

---

# Phase 10 Pause Input Recheck + Save Game Slot Picker → Claude Follow-up

Status: `READY_FOR_PHASE10_REVERIFICATION`

Ngày: 2026-08-23

## Việc 1 — Pause input fix đã đóng phần implementation/build

- Root cause giữ nguyên sau khi đối chiếu lại scene serialization và regression: outgoing
  `InputSystemUIInputModule` và incoming `GameInputCoordinator` từng tranh cùng project
  `InputActionAsset`; coordinator nay sở hữu runtime clone riêng cho `UI/Cancel`.
- Targeted `GameInputCoordinatorPlayModeTests`: 2/2 PASS.
- Full regression trước Save picker: EditMode 58/58, PlayMode 137/137 PASS.
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset checked.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Build Settings không đổi: MainMenu index 0, DemoScene index 1.
- Player build mới: `Builds/Phase10PauseFix/ProjectGame2D.exe`; Windows64 build success,
  0 error, 0 warning, 515.85 MB.
- Player smoke bằng Windows computer-use đi được `MainMenu → New Game Slot 2 → DemoScene`; Slot 1
  không bị chọn/ghi đè. Công cụ gửi click được nhưng đối chứng phím `D` cũng không tạo raw keyboard
  event cho Unity Input System, nên Escape synthetic không được dùng để kết luận PASS/FAIL.
- Acceptance còn lại không đổi: người dùng nhấn Escape vật lý trên build này, sau đó chạy full
  Save → Return → Continue restore. Chưa tuyên bố `CONTENT_READY`.

## Việc 2 — Save Game slot picker

Status UI: `VERIFIED`

- `PauseMenuUI` dùng chung responsive three-slot overlay hiện có cho hai mode `LOAD GAME` và
  `SAVE GAME`; không nhân đôi save presentation và không đổi backend ownership.
- Nút Save Game giờ mở slot picker. Action mỗi slot gọi `CanSaveToSlot`/`RequestSaveToSlot`; popup
  overwrite gọi `ConfirmOverwriteAndSave` hoặc `CancelSaveToSlot`.
- Popup có text riêng cho Valid, Corrupted và IncompatibleVersion. Delete chỉ xuất hiện ở Save mode,
  luôn hỏi xác nhận rồi mới gọi `DeleteSlot`.
- Empty slot save thành công đóng overlay, giữ Pause flow và cập nhật timestamp từ
  `OnSaveSucceeded`. Load mode giữ nguyên `CanLoad`/`RequestLoad`.
- Chặn double-submit cùng frame ở presentation; khi backend `IsBusy`, toàn bộ action vẫn disable theo
  contract cũ.
- Hierarchy/component được bind bằng Unity MCP trong DemoScene. Ba delete button dùng font
  `DigitalDisco SDF v3`; title/slot layout nằm trong viewport responsive và PauseMenu giữ top sibling
  để Quest/Tutorial không render đè modal.

## Runtime/test verification cuối

- Empty Slot 3: click Save ghi ngay, không popup; `ActiveSlotId` chuyển sang 3, slot thành Valid,
  overlay đóng và timestamp cập nhật.
- Valid Slot 3: popup đúng `OVERWRITE THE SAVE IN SLOT 3?`; Cancel giữ overlay và save không đổi.
- Corrupted/Incompatible presentation: text status-specific đúng contract.
- Delete Slot 3: popup đúng slot + cảnh báo irreversible; Cancel không xóa save.
- Hai click slot trong cùng frame chỉ phát 1 `OnSaveSlotConfirmationRequired`.
- 4 PlayMode presentation tests mới kiểm tra mapping text overwrite/delete.
- Full final regression: EditMode 58/58, PlayMode 141/141 PASS.
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset checked.
- DemoScene validator: 0 issue, 0 missing script, 0 broken prefab.
- Không sửa `GameplaySessionController`, repository, save capture, Quest/Tutorial/Commerce/Inventory
  hoặc world persistence trong phần UI này.

---

# Phase 10 Final Physical Player Acceptance → Claude Follow-up

Status: `READY_FOR_PHASE10_CONTENT_READY_CONFIRMATION`

Thời gian chạy: 2026-08-23 21:57:18 +07:00  
Build: `C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe`

Owner đã chạy acceptance bằng bàn phím/chuột vật lý và xác nhận toàn bộ luồng PASS. Các slot test là
Slot 2 và Slot 3; Slot 1 không được chọn, ghi đè hoặc xóa.

## Kết quả từng bước

1. Launch Player build và hiển thị MainMenu: **PASS**.
2. New Game bằng slot trống, không dùng Slot 1: **PASS**.
3. Transition MainMenu → DemoScene: **PASS**.
4. Nhấn Escape mở PauseMenuUI: **PASS**.
5. Đóng/mở PauseMenuUI lại nhiều lần bằng Escape: **PASS**.
6. Save Game mở three-slot picker: **PASS**.
7. Save vào Empty slot ghi ngay, không hiện popup phụ: **PASS**.
8. Sau Empty save, overlay đóng và timestamp cập nhật: **PASS**.
9. Chọn Valid slot hiện đúng popup `OVERWRITE THE SAVE IN SLOT n?`: **PASS**.
10. Cancel overwrite giữ nguyên save cũ: **PASS**.
11. Confirm overwrite ghi đè thật: **PASS**.
12. Save As từ Slot A sang Slot B khác: **PASS**.
13. Slot đích Save As hiển thị `ACTIVE`, xác nhận `ActiveSlotId` đã chuyển: **PASS**.
14. Delete hiện popup riêng với cảnh báo không thể hoàn tác: **PASS**.
15. Cancel Delete không xóa save; Confirm Delete mới xóa: **PASS**.
16. Return Main Menu sau khi save: **PASS**.
17. Continue đúng slot vừa save: **PASS**.
18. Vị trí nhân vật restore đúng: **PASS**.
19. Inventory item và gold restore đúng: **PASS**.
20. Quest state restore đúng: **PASS**.
21. Tutorial state restore đúng: **PASS**.
22. Persistent world state (chest/pickup/boss/resource node) restore đúng: **PASS**.
23. Không mất, nhân đôi hoặc rò dữ liệu giữa các slot trong luồng kiểm tra: **PASS**.
24. Quit Desktop thoát sạch, không treo: **PASS**.

## Kết luận

- Không phát hiện UI issue hoặc backend/contract gap trong final physical acceptance.
- Automated verification trước acceptance vẫn là EditMode 58/58, PlayMode 141/141, Content
  Validation 0 error, DemoScene/MainMenu validator 0 issue.
- Phase 10 đã đạt acceptance bar phía Codex/owner và sẵn sàng để Claude xác nhận cuối, cập nhật
  `Phase10ImplementationReport.md` cùng Roadmap sang `CONTENT_READY`.

---

# Content Production — Side Quest `quest.side.potion_supply.001`

Status: `BACKEND_GAP_FOUND`

Ngày kiểm tra: 2026-08-23 (+07:00)

## Content đã author bằng Unity MCP

- Asset: `Assets/Quests/Definitions/Quest_SidePotionSupply001.asset`.
- Catalog: đã đăng ký vào `Assets/Quests/QuestCatalog.asset` mà `QuestManager` trong DemoScene dùng;
  catalog hiện có 3 quest và resolve được ID mới.
- `questId`: `quest.side.potion_supply.001`.
- Tên hiển thị: `Potion Supply Run`.
- Giver/turn-in NPC: `npc.town.elder`; không prerequisite; không phải Tutorial/Main Quest.
- Objective duy nhất: `Purchase`, target `item.consumable.health_potion`, số lượng 1, description
  `Purchase 1 Health Potion from the Town Elder's general shop.`
- Reward: 1 `item.material.wood`, 10 gold, 20 experience.
- Không tạo/sửa C# và không sửa manager/core/UI.

## End-to-end content verification

1. `QuestNpcInteractionService.TryGetOfferedQuest("npc.town.elder")` trả đúng quest mới sau khi
   fixture runtime đánh dấu hai quest đứng trước đã Completed: **PASS**.
2. Accept qua `QuestNpcInteractionService`: `Available -> Active`, counter `0/1`: **PASS**.
3. Save bằng `QuestManager.ToSaveData()` rồi restore bằng `RestoreState()`: giữ `Active`, counter
   `0/1`, không tự tăng objective: **PASS**.
4. Purchase thật qua `ShopNpcInteractionService` tại `shop.town.general`: transaction `Success`,
   counter `1/1`, quest chuyển `ReadyToTurnIn`: **PASS**.
5. Turn-in qua NPC service: result `Success`, quest chuyển `Completed`: **PASS**.
6. Reward: Wood `0 -> 1`, gold sau chi phí mua `80 -> 90`, XP level 1 `0 -> 20`: **PASS**.
7. Turn-in lần hai bị từ chối và không cấp reward lần nữa: **PASS**.
8. Save/restore trạng thái Completed giữ nguyên inventory/gold/level/XP, không phát lại reward và
   không reset quest: **PASS**.

## Validation và regression

- Content Validation: **PASS**, 0 error, 60 accepted legacy warning, 84 asset checked; không có
  warning/error từ quest mới.
- DemoScene validator: **PASS**, 0 issue, 0 missing script, 0 broken prefab.
- EditMode: **PASS 58/58**.
- Hai PlayMode test lỗi khi chạy toàn suite 141 test:
  - `GameInputCoordinatorPlayModeTests.DisableEnable_DoesNotDoubleSubscribe`
  - `GameInputCoordinatorPlayModeTests.SharedProjectActionDisabledAfterEnable_CancelStillPauses`
  - Cả hai expected `Paused` nhưng nhận `Playing`; kết quả lặp lại ở hai lần full-suite.
  - Khi chạy riêng đúng hai test, **PASS 2/2**, cho thấy lỗi phụ thuộc thứ tự/lifecycle hoặc rò state
    giữa PlayMode tests, không liên quan dữ liệu quest mới.

## Gap cần Claude xử lý

Content quest và toàn bộ acceptance riêng của quest đã PASS, nhưng Definition of Done yêu cầu full
PlayMode regression xanh nên chưa thể đặt `VERIFIED`. Nhờ Claude chẩn đoán test isolation/lifecycle
của `GameInputCoordinatorPlayModeTests` khi chạy trong full suite. Codex không sửa lan sang input
backend vì task này chỉ được author content, không được sửa C# core.

---

# Final Quest Regression Recheck — PlayMode Isolation Vẫn Flake

Status: `BACKEND_GAP_FOUND`

Thời gian kiểm tra: 2026-08-23 22:47:11 +07:00  
Commit đã kiểm tra: `516169df` (`Fix PlayMode test isolation in GameInputCoordinatorPlayModeTests`),
HEAD trùng `origin/SuperaAI`.

## Kết quả độc lập phía Codex

- Compile refresh: **PASS**, Unity trở về idle, không có C# compile diagnostic.
- EditMode full suite: **PASS 58/58**.
- PlayMode full suite vòng 1: **PASS 142/142**.
- PlayMode full suite vòng 2 ngay sau đó: **FAIL 140/142**:
  - `GameInputCoordinatorPlayModeTests.DisableEnable_DoesNotDoubleSubscribe`
  - `GameInputCoordinatorPlayModeTests.SharedProjectActionDisabledAfterEnable_CancelStillPauses`
  - Cả hai: expected `Paused`, actual `Playing`.
- Chạy riêng 3 test trong `GameInputCoordinatorPlayModeTests` ngay sau vòng full-suite bị lỗi:
  **FAIL 1/3**; test isolation mới
  `SetUp_SuppressesLeftoverSceneCoordinator_TearDownRestoresIt` pass, nhưng hai test Cancel cũ vẫn
  fail với cùng kết quả `Playing`.
- Content Validation: **PASS**, 0 error, 60 accepted legacy warning, 84 asset checked.
- DemoScene validator: **PASS**, 0 issue, 0 missing script, 0 broken prefab.
- Quest `quest.side.potion_supply.001`: asset còn tồn tại và đã đăng ký trong catalog; giver/turn-in
  vẫn là `npc.town.elder`; reward vẫn là 1 Wood, 10 gold, 20 XP: **PASS**.

## Sai lệch cần xử lý tiếp

Fix hiện tại chưa bảo đảm hai lần PlayMode suite liên tiếp xanh. Trạng thái gây double-processing
dường như sống xuyên qua lần PlayMode run đầu tiên; ở trạng thái đó, ngay cả targeted run tiếp theo
cũng còn fail, dù test mới xác nhận `_leakedSceneCoordinator` fixture đã được suppress. Điều này cho
thấy vẫn còn một coordinator/callback/action/device hoặc static lifecycle khác chưa được cleanup hay
chưa được cơ chế suppress hiện tại tìm thấy.

Theo yêu cầu verification-only, Codex không sửa code. Mục quest cũ giữ `BACKEND_GAP_FOUND`, chưa đổi
sang `VERIFIED`. Nhờ Claude tái hiện đúng chuỗi: full PlayMode PASS -> full PlayMode lần hai ->
targeted 3 tests trong cùng Editor session, rồi điều tra state còn sót giữa các test jobs.

---

# Final Quest Verification trên commit `22f8f85e` — Diagnostic Flake Reproduced

Status: `BACKEND_GAP_FOUND`

Thời gian kiểm tra: 2026-08-23 23:11:32 +07:00  
Commit: `22f8f85e` (`Harden GameInputCoordinatorPlayModeTests isolation against cross-test timing gap`),
HEAD trùng `origin/SuperaAI`.

## Môi trường và trình tự

- Chạy bằng Unity MCP Test Runner API trong cùng một Unity Editor session; không đóng Editor, không
  refresh/domain-reload thủ công giữa các bước.
- Scene đang mở trong Editor: `Assets/Scenes/DemoScene.unity` (build index 1), clean và loaded.
- Trình tự giữ nguyên đúng lần Codex từng tái hiện: full PlayMode lần 1 -> full PlayMode lần 2 ngay
  sau -> targeted 3 test ngay sau lần 2.

## Kết quả

1. Full PlayMode lần 1, job `a464b45d715f4a70b900d27fd30922f0`: **PASS 142/142**.
2. Full PlayMode lần 2, job `2aa85f0e2e4d4aa6b36ef3a1cbbb2271`: **FAIL 140/142**.
3. Targeted 3 test ngay sau đó, job `7aec95477f7f4bf7bf2dc701cbc1fda5`:
   **PASS 3/3**.

Hai test fail ở vòng full thứ hai:

- `GameInputCoordinatorPlayModeTests.DisableEnable_DoesNotDoubleSubscribe`
- `GameInputCoordinatorPlayModeTests.SharedProjectActionDisabledAfterEnable_CancelStillPauses`

## Assertion message nguyên văn

`DisableEnable_DoesNotDoubleSubscribe`:

```text
Live GameInputCoordinator instances:
- GameInputCoordinatorFixture scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=True enabled=True
- LeakedSceneGameInputCoordinator scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=False enabled=True
Expected: Paused
But was:  Playing
```

`SharedProjectActionDisabledAfterEnable_CancelStillPauses`:

```text
Live GameInputCoordinator instances:
- GameInputCoordinatorFixture scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=True enabled=True
- LeakedSceneGameInputCoordinator scene=InitTestScene324ed7b5-ad46-4127-8558-dcf2924e64d3 activeInHierarchy=False enabled=True
Expected: Paused
But was:  Playing
```

## Nhận xét bằng chứng

- Diagnostic tại thời điểm fail không hiển thị foreign coordinator active nào ngoài fixture; fake
  leaked coordinator đã inactive đúng như cơ chế suppress mong đợi.
- Dù vậy một Escape vẫn kết thúc ở `Playing`. Vì targeted run kế tiếp PASS 3/3, lỗi tiếp tục phụ
  thuộc full-suite/test-job lifecycle thay vì thất bại ổn định trong riêng fixture.
- Khác biệt đáng chú ý so với phiên Claude: Codex chạy qua Unity MCP Test Runner API và mở
  DemoScene trong Editor. Claude báo đã chạy cùng chuỗi nhưng không tái hiện; cần đối chiếu Claude
  dùng Test Runner UI, MCP tool hay execute-code runner và scene nào đang mở.

Codex không sửa test/production code theo phạm vi verification-only. Không đổi mục quest sang
`VERIFIED`; chưa chạy nhánh Compile/EditMode/Content/DemoScene validation sau fail vì yêu cầu chỉ
thực hiện các gate đó khi toàn bộ trình tự PlayMode PASS.
