# Claude → Codex Handoff

Status: `READY_FOR_CODEX_FINAL_QUEST_VERIFICATION`

Ngày: 2026-08-23
Feature: Fix PlayMode test isolation cho `GameInputCoordinatorPlayModeTests` (2 test FAIL trong full suite khi có quest content mới)

## Root cause

**Không phải bug production, không liên quan gì tới quest content mới.** Đây là lỗi cô lập test
(test isolation) giữa `GameInputCoordinatorPlayModeTests` và các test class khác trong project.

Cả `Assets/Scenes/MainMenu.unity` và `Assets/Scenes/DemoScene.unity` đều có **component
`GameInputCoordinator` thật**, wire vào đúng project asset thật
(`Assets/Settings/InputSystem_Actions.inputactions`, guid `2bcd2660ca9b64942af0de543d8d7100`) —
xác nhận trực tiếp trong `MainMenu.unity` dòng 2263 (`_projectActions: {..., guid:
2bcd2660ca9b64942af0de543d8d7100}`).

Khi `GameplaySessionControllerPlayModeTests` chạy các `[UnityTest]` load scene thật (ví dụ
`RequestReturnToMainMenu_WhenClean_ReturnsDirectlyWithoutConfirmation`,
`RequestLoad_DifferentSlot_RealSceneReload_...`), scene `MainMenu.unity` được load thật qua
`SceneManager.LoadSceneAsync("MainMenu", Single)` và **đúng theo thiết kế** — test kết thúc ở đó,
không có "scene trống" nào để quay về. Vì project tắt domain reload
(`ProjectSettings/EditorSettings.asset: m_EnterPlayModeOptions: 1`), MainMenu's
`GameInputCoordinator` **vẫn sống và enabled** cho tới hết session Play, tức là còn tồn tại khi
`GameInputCoordinatorPlayModeTests` chạy sau đó trong cùng full-suite run.

Binding `<Keyboard>/escape` của `UI/Cancel` khớp với **bất kỳ** thiết bị keyboard nào, kể cả
`Keyboard` mà `GameInputCoordinatorPlayModeTests.SetUp()` tự tạo. Khi test giả lập một lần nhấn
Escape thật qua `InputSystem.QueueStateEvent`, **cả hai coordinator** (một từ MainMenu leftover,
một từ fixture của test) đều nhận callback `HandleCancelPerformed`:

1. Coordinator đầu tiên fire: `CurrentState == Playing` → `Pause()` → `Paused`.
2. Coordinator thứ hai fire (cùng frame): `CurrentState == Paused` → `ReturnToPreviousState()` →
   pop lại `Playing`.

Kết quả cuối: `Playing` — đúng khớp `Expected: Paused, Actual: Playing` đã báo cáo. Đây là hành vi
**giống hệt** triệu chứng double-subscribe thật, nhưng nguyên nhân là hai *instance* coordinator
khác nhau (một sống sót từ scene load thật của test class khác), không phải một component
double-subscribe với chính nó.

**Vì sao đúng "PASS khi chạy riêng, FAIL khi chạy full suite":** khi chỉ chạy riêng
`GameInputCoordinatorPlayModeTests`, không có test nào khác load MainMenu/DemoScene thật trước đó,
nên không có coordinator leftover nào tồn tại — chỉ có đúng một coordinator (của fixture) phản ứng
Escape, PASS bình thường.

**Vì sao đây không phải bug production:** Physical Player build acceptance (24/24 bước PASS, xem
`Phase10ImplementationReport.md § Part 7 final acceptance`) đã xác nhận Escape hoạt động đúng trong
gameplay thật — nơi tại một thời điểm luôn chỉ có đúng MỘT `GameInputCoordinator` sống (scene cũ bị
Unity destroy hoàn toàn trước khi scene mới hoàn tất load, do `LoadSceneMode.Single`). Tình huống
"hai coordinator cùng sống" chỉ xảy ra trong PlayMode test session (domain reload tắt + nhiều test
class load scene thật nối tiếp nhau), không xảy ra trong Player build thật.

## File đã sửa

- `Assets/Scripts/Tests/PlayMode/GameInputCoordinatorPlayModeTests.cs` — **chỉ sửa test, không đụng
  bất kỳ production code nào** (`GameInputCoordinator.cs` giữ nguyên 100% so với lần review trước).
  - `[SetUp]`: quét `Object.FindObjectsByType<GameInputCoordinator>(FindObjectsInactive.Exclude, ...)`
    trước khi tạo fixture; tạm `SetActive(false)` mọi coordinator đang sống tìm thấy (leftover từ
    scene thật của test class khác), lưu lại trong `_suppressedCoordinators`.
  - `[TearDown]`: `SetActive(true)` lại đúng những GameObject đã suppress, không để chúng bị vô hiệu
    hoá vĩnh viễn cho các test chạy sau.
  - Thêm `[OneTimeSetUp]`/`[OneTimeTearDown]`: tạo một `_leakedSceneCoordinator` giả lập (một
    `GameInputCoordinator` sống xuyên suốt cả fixture, y hệt leftover thật) để cơ chế suppress được
    test xác định (deterministic), không còn phụ thuộc vào việc test class khác có tình cờ chạy
    trước hay không.
  - Test mới: `SetUp_SuppressesLeftoverSceneCoordinator_TearDownRestoresIt` — assert trực tiếp cơ
    chế suppress/restore hoạt động đúng, thay vì chỉ suy luận gián tiếp qua hai test kia có pass hay
    không.

## Verification

- `GameInputCoordinatorPlayModeTests` chạy riêng: **3/3 PASS** (2 test cũ + 1 test mới).
- EditMode full suite: **58/58 PASS**.
- PlayMode full suite: **142/142 PASS** — chạy **hai lần liên tiếp** để đối chứng đúng cách Codex đã
  tái hiện lỗi (báo cáo gốc nói lỗi lặp lại ở 2 lần chạy) — cả hai lần đều xanh, không còn flake.
- Content Validation: 0 error, 60 legacy warning (không đổi), **84 asset** (khớp số Codex báo cáo
  sau khi thêm quest mới).
- DemoScene validator: 0 issue.
- Xác nhận quest content không bị đụng: `Assets/Quests/Definitions/Quest_SidePotionSupply001.asset`
  vẫn tồn tại, `questId: quest.side.potion_supply.001` còn nguyên trong file.
- Không tự chạy lại physical Player build test (không cần thiết — không có gì thay đổi trong
  production code/scene/Pause behavior, chỉ sửa test isolation).

## Kết luận

Fix đã đóng gap. Status `READY_FOR_CODEX_FINAL_QUEST_VERIFICATION` — theo đúng yêu cầu, Codex chạy
lại full regression một lần nữa phía mình rồi đổi mục quest `quest.side.potion_supply.001` từ
`BACKEND_GAP_FOUND` sang `VERIFIED` trong `CodexToClaude.md`.

---

# Phase 10 — Hardening và content-ready milestone (lịch sử — đã CONTENT_READY, xem mục review Save Game slot picker bên dưới)

Status (khi mục này được tạo): `CONTENT_READY`

Ngày: 2026-08-23
Feature: Phase 10 — Hardening và content-ready milestone — **ĐÃ ĐÓNG**

## Kết luận cuối

Owner đã chạy physical acceptance thật (bàn phím/chuột vật lý) trên
`C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe` và toàn bộ 24 bước **PASS** (chi tiết
từng bước trong `CodexToClaude.md § Phase 10 Final Physical Player Acceptance`). Đối chiếu với 4
acceptance criteria của Phase 10 trong `Roadmap.md`:

- Test matrix bắt buộc đều pass: EditMode 58/58, PlayMode 141/141. ✓
- Không có P0/P1 known issue trong save/progression/item transaction: xác nhận, không có. ✓
- Content designer có thể tạo Tutorial Quest mới không sửa manager core: `ContentAuthoringGuide.md`
  đủ, đã xác nhận ở Phase 10 Part 8. ✓
- Build player chạy đúng New Game và Continue trên máy sạch: **xác nhận bằng physical acceptance
  24/24 bước PASS**, bao gồm cả Save Game slot picker mới (Empty/Overwrite/Save As/Delete) và Pause
  Menu Escape fix. ✓

Cả 4 tiêu chí đều đạt → **Phase 10 chính thức `CONTENT_READY`.** Đã cập nhật
`Phase10ImplementationReport.md`, `Roadmap.md` và `README.md` với kết luận này.

Không còn task nào chờ Codex ở Phase 10. Cảm ơn về việc root-cause chính xác Pause input bug, dựng
đúng Save Game slot picker theo contract, và chạy physical acceptance đầy đủ, chi tiết từng bước —
đúng chuẩn chất lượng dự án cần.

## Việc tiếp theo (không thuộc Phase 10, chỉ ghi chú tham khảo)

Roadmap nền tảng không có Phase 11. Bước tiếp theo hợp lý là sản xuất content thật (quest/item/shop/
world entity) theo `ContentAuthoringGuide.md`, hoặc chốt các decision còn `Open`/`Proposed` trong
`DecisionRegister.md` (D-010 player death, D-013 character creation, D-019 production world topology)
nếu muốn mở rộng nền tảng trước. Sẽ có prompt riêng khi có yêu cầu cụ thể tiếp theo.

## Gamepad manual verification — vẫn còn mở, không chặn CONTENT_READY

Chưa từng có gamepad vật lý test xuyên suốt mọi phase (kể cả physical acceptance vừa rồi chỉ dùng
bàn phím/chuột). Đây là action item không blocking, có thể làm bất cứ lúc nào có gamepad thật.

---

# Phase 10 review (lịch sử — đã xác nhận PASS, xem kết luận cuối ở trên)

Status (khi mục này được tạo): `READY_FOR_CODEX_PHASE10_FINAL_MANUAL_VERIFICATION`

Ngày: 2026-08-23
Feature: Phase 10 — review kết quả Pause Input Fix + Save Game slot picker, build lại combined, chờ acceptance thủ công cuối

## Bối cảnh

Đã review kỹ cả hai phần Codex vừa hoàn thành (Pause Input Fix và Save Game slot picker) và tự chạy
lại toàn bộ verification độc lập — **cả hai đều PASS review, không có gap.** Chi tiết đầy đủ trong
[Phase10ImplementationReport.md § Part 7 follow-up](../Phase10ImplementationReport.md).

## Kết quả review

**Pause Input Fix:** root cause đúng (outgoing `InputSystemUIInputModule` và `GameInputCoordinator`
tranh cùng `InputActionAsset` trong lúc `MainMenu → DemoScene` overlap lifecycle). Fix dùng runtime
clone riêng cho coordinator (`Instantiate(_projectActions)`, destroy trong `OnDestroy`) là đúng hướng,
tối thiểu, không leak, không double-subscribe, không phụ thuộc hierarchy DemoScene cụ thể.
`GameInputCoordinatorPlayModeTests` tái hiện đúng race condition (không phải test hời hợt).

**Save Game slot picker:** wiring trong `PauseMenuUI` khớp đúng contract đã giao (Empty save ngay,
Valid/Corrupted/IncompatibleVersion đều qua `OnSaveSlotConfirmationRequired` không có ngoại lệ im
lặng, Delete luôn confirm trước, Save As đổi đúng `ActiveSlotId`, `RequestSave()`/Save-and-Return/
Save-and-Quit không bị đụng). Đã xem xét kỹ cơ chế chặn double-click bằng `Time.frameCount` theo yêu
cầu — **kết luận: đủ an toàn, không cần sửa.** Lý do: backend tự re-check status thật của slot ở mỗi
lần gọi `RequestSaveToSlot` (nên một click thứ hai ở frame sau, sau khi slot đã đổi status từ lần ghi
đầu, tự động rơi vào nhánh confirm thay vì ghi đè im lặng lần hai), và nút Confirm/Delete được bảo vệ
tự nhiên vì popup tự đóng ngay sau click đầu tiên (không còn gì để bấm lần hai).

## Verification độc lập (Claude tự chạy lại, khớp 100% số liệu Codex báo cáo)

- EditMode: 58/58 PASS.
- PlayMode: 141/141 PASS (0 regression).
- Content Validation: 0 error, 60 accepted legacy warning, 83 asset.
- DemoScene validator: 0 issue. MainMenu validator: 0 issue (kiểm tra thêm).
- Build Settings không đổi: MainMenu index 0, DemoScene index 1.
- Build lại Player combined (chứa cả 2 thay đổi):
  `C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe` — Windows64, 0 error/0 warning,
  38.55s, 515.86 MB.
- Đã launch build này, `Player.log` init sạch (D3D12/PhysX/Input System, không exception), rồi dừng
  process. **Chưa** (và không thể trong môi trường này) xác nhận Escape thật sự mở Pause bằng bàn
  phím vật lý — giới hạn môi trường giống Part 7 gốc ([D-026](../DecisionRegister.md)), không phải
  nghi ngờ về code.

## Việc còn lại — acceptance thủ công cuối cùng (bàn phím vật lý)

Chạy trên build `C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe`:

```
MainMenu
→ New Game bằng slot trống (KHÔNG dùng Slot 1 — Slot 1 đang giữ save thật IncompatibleVersion)
→ DemoScene
→ Escape mở PauseMenuUI (đây là bug đã fix — xác nhận thật sự hoạt động)
→ Save Game → mở slot picker → thử: save vào slot Empty (ghi ngay), overwrite một slot Valid (có
  popup confirm), Save As sang slot khác (ActiveSlotId đổi theo), Delete một slot (có popup confirm)
→ Return Main Menu
→ Continue đúng slot vừa save
→ xác nhận vị trí, inventory, quest, tutorial và world state restore đúng
→ Quit Desktop
```

Báo lại PASS/FAIL cụ thể từng bước vào `CodexToClaude.md`. Nếu PASS toàn bộ, Phase 10 chuyển thành
`CONTENT_READY`. Nếu phát hiện bug thật (không phải do môi trường), ghi `BACKEND_GAP_FOUND` với bằng
chứng cụ thể.

## Việc Codex KHÔNG cần làm

- Không cần sửa thêm gì ở `GameInputCoordinator`, `PauseMenuUI`, hay `GameplaySessionController` trừ
  khi acceptance thủ công phát hiện vấn đề mới.
- Không cần dựng Recovery UI (vẫn không bắt buộc cho content-ready bar hiện tại).

---

# Save Game slot picker (lịch sử — đã VERIFIED, xem review ở trên)

Status (khi mục này được tạo): `READY_FOR_CODEX_SAVE_SLOT_UI`

Ngày: 2026-08-23
Feature: Phase 10 follow-up — Save Game slot picker (Pause Menu "Save Game" chọn 1 trong 3 slot thay vì ghi thẳng ActiveSlotId)

## Bối cảnh

Yêu cầu mới: bấm "Save Game" từ Pause Menu phải mở một overlay chọn slot (giống Load Game overlay đã
có), cho phép save vào slot trống, ghi đè slot đã có (sau confirm), "Save As" sang slot khác (đổi
`ActiveSlotId`), và xóa slot (sau confirm). Toàn bộ logic/validation/atomic write đã có sẵn trong
`GameplaySessionController` (`Assets/Scripts/GameManagers/GameplaySessionController.cs`) — Codex chỉ
cần dựng UI overlay gọi đúng API dưới đây, không tự capture save data, không tự đụng
`ISaveSlotRepository`. Xem [D-027](../DecisionRegister.md) cho quyết định semantics đầy đủ.

`RequestSave()` (không tham số, ghi thẳng vào `ActiveSlotId`) **vẫn giữ nguyên** — dùng cho luồng
Save-and-Return/Save-and-Quit hiện có trong `PauseMenuUI`, không đổi gì ở đó.

## Contract phía Claude cung cấp (mới, thêm vào GameplaySessionController — không đổi API cũ)

```csharp
// Slot picker cho nút "Save Game" mới trong Pause Menu.
public bool CanSaveToSlot(int slotId);              // gate hiển thị/enable từng nút slot trong overlay
public bool SlotRequiresOverwriteConfirm(int slotId); // true nếu slot đã có data (Valid/Corrupted/IncompatibleVersion)

public void RequestSaveToSlot(int slotId);   // slot Empty: ghi ngay. Slot khác: fire OnSaveSlotConfirmationRequired, KHÔNG ghi
public void ConfirmOverwriteAndSave();        // ghi slot đang pending sau khi user xác nhận overwrite
public void CancelSaveToSlot();               // đóng popup overwrite, không ghi gì, không đổi ActiveSlotId

public bool DeleteSlot(int slotId);           // UI PHẢI tự hỏi xác nhận trước khi gọi -- method này xóa luôn, không hỏi lại
```

Event mới:

```csharp
// (slotId, status của slot đó) -- status quyết định text popup ("Ghi đè save đã có" vs "Save này bị
// hỏng, xóa và ghi đè?" vs "Save này từ phiên bản không tương thích, xóa và ghi đè?")
public event Action<int, SaveSlotStatus> OnSaveSlotConfirmationRequired;
```

Event cũ vẫn dùng lại nguyên như Load Game overlay đã dùng: `OnSaveSlotListChanged` (gọi
`RefreshSlots()` để lấy `SaveSlotInfo[]` cho cả 3 slot), `OnSaveSucceeded`, `OnOperationFailed`
(`GameplaySessionOperationResult` có thêm giá trị mới `InvalidSlot` cho slotId ngoài phạm vi 1..3).

## Luồng UI đề xuất

```text
Pause → "Save Game" → mở Save Slot Overlay (RefreshSlots() để hiển thị 3 slot, dùng đúng UI đã
  dựng cho Load Game overlay, chỉ đổi hành vi click)

Click slot:
  CanSaveToSlot(slotId) == false → disable nút đó (đang IsBusy hoặc không có active session)
  SlotRequiresOverwriteConfirm(slotId) == false (Empty) → gọi RequestSaveToSlot(slotId) ngay,
    không cần popup phụ
  SlotRequiresOverwriteConfirm(slotId) == true → gọi RequestSaveToSlot(slotId), chờ
    OnSaveSlotConfirmationRequired(slotId, status) → hiển thị popup 2 lựa chọn (Overwrite/Cancel),
    text theo status:
      Valid → "Ghi đè save đã có ở Slot {n}?"
      Corrupted → "Save ở Slot {n} bị hỏng. Xóa và ghi đè bằng save hiện tại?"
      IncompatibleVersion → "Save ở Slot {n} không tương thích phiên bản này. Xóa và ghi đè?"
    → Overwrite: gọi ConfirmOverwriteAndSave()
    → Cancel: gọi CancelSaveToSlot(), đóng popup, quay lại Save Slot Overlay

Nút Delete cạnh mỗi slot (không phải Empty):
  Click → popup xác nhận riêng ("Xóa save ở Slot {n}? Không thể hoàn tác.")
  → Confirm: gọi DeleteSlot(slotId), overlay tự refresh qua OnSaveSlotListChanged
  → Cancel: đóng popup, không gọi gì

Save thành công (OnSaveSucceeded) → đóng toàn bộ overlay, quay lại Paused, hiển thị timestamp mới
  (đọc lại từ SaveSlotInfo của ActiveSlotId sau RefreshSlots()).
```

## Việc Codex KHÔNG cần làm

- Không tự capture `GameSaveData` hay đụng `ISaveSlotRepository` -- mọi write đi qua
  `RequestSaveToSlot`/`ConfirmOverwriteAndSave`.
- Không thay đổi `RequestSave()` (không tham số) hay luồng Save-and-Return/Save-and-Quit hiện có.
- Không thay đổi layout Load Game overlay đã có -- tái sử dụng cùng component hiển thị slot, chỉ đổi
  handler khi click.
- Không sửa Quest/Tutorial/Commerce/Inventory/world persistence.

## Test cần có phía Codex (theo Quality Strategy, cho phần UI)

- Click slot Empty → save ngay, không có popup phụ nào xuất hiện.
- Click slot Valid/Corrupted/IncompatibleVersion → đúng text popup theo status.
- Cancel popup overwrite → overlay giữ nguyên, không đổi gì.
- Double-click nút Save trong lúc `IsBusy` → nút đã disable, không gửi request thứ hai.

## Verification đã chạy phía Claude

- 58/58 EditMode, **137/137 PlayMode** (18 test mới cho slot picker: Empty save, Save As đổi
  ActiveSlotId, Valid/Corrupted/IncompatibleVersion đều yêu cầu confirm, Cancel không đổi gì,
  double-click bị chặn, Delete thường/active-slot/không rò slot khác, write-failure giữ nguyên save
  cũ) -- 0 regression.
- Content Validation 0 error, DemoScene validator 0 issue.

Sau khi dựng UI xong, cập nhật `CodexToClaude.md` với kết quả test/manual verify và trạng thái phù hợp
(README/Roadmap/QualityStrategy không cần đổi thêm trừ khi Codex phát hiện gap).

---

# Phase 10 — Hardening và content-ready milestone (không có UI mới cần dựng; chỉ cần verification thủ công)

Status (khi phase này bắt đầu): `READY_FOR_CODEX_VERIFICATION`

Ngày: 2026-08-23
Feature: Phase 10 — Hardening và content-ready milestone (không có UI mới cần dựng; chỉ cần verification thủ công)

## Bối cảnh

Phase 10 là audit/hardening, **không phải feature phase** — không có UI mới nào cần Codex dựng. Toàn
bộ chi tiết: [Phase10ImplementationReport.md](../Phase10ImplementationReport.md).

Đã hoàn tất và verify:
- Save migration pipeline (V1→Current, mọi save cũ được nâng cấp an toàn khi load, không rewrite file
  cho tới lần save thật tiếp theo).
- Soak test thật (120 chu kỳ save/load cùng slot, A→B→C, world snapshot 60 object, teardown/recreate)
  — tất cả PASS, kết quả số liệu thật trong report.
- Profiling baseline thật cho từng giai đoạn save pipeline (serialize/write/read/migration riêng biệt).
- Recovery UX backend contract: **đã đủ, không cần Codex build UI mới.** `MainMenuController` +
  `SaveSlotInfo.Status` (`Empty`/`Valid`/`Corrupted`/`IncompatibleVersion`) đã cung cấp mọi thứ cần để
  UI phân biệt corrupted/incompatible/empty và gọi `DeleteSlot`/`RefreshSlots` — xem Part 6 trong report
  nếu muốn build màn hình recovery đẹp hơn sau này, nhưng không bắt buộc cho content-ready.
- 6 tài liệu authoring content (item/quest/shop-recipe/persistent world entity/area-spawn/scene
  integration): [ContentAuthoringGuide.md](../ContentAuthoringGuide.md).
- Full regression: 58/58 EditMode, 119/119 PlayMode, Content Validation 0 error, DemoScene validator
  0 issue.
- Player build (Windows64): **build thành công, 0 error/0 warning**, nhưng click-through smoke test
  (New Game→DemoScene→Save→Return→Continue→Quit) **chưa verify được** trong môi trường automation này
  (cửa sổ game không capture/điều khiển được qua remote automation ở đây — không phải lỗi build/code,
  xem Part 7 trong report).

## Việc cần Codex/user làm (chỉ verification thủ công, không phải code mới)

1. Chạy build đã có sẵn tại `C:\Users\havin\Phase10PlayerBuild\ProjectGame2D.exe` trên máy thật (không
   phải qua remote automation), hoặc build lại từ Build Settings hiện tại (MainMenu index 0, DemoScene
   index 1 — không đổi).
2. Click-through: Launch → MainMenu → New Game (chọn slot **trống**, không phải Slot 1 — Slot 1 trên
   máy dev hiện đang giữ save thật `IncompatibleVersion` từ trước Phase 6, không được ghi đè) →
   DemoScene → Save → Return Main Menu → Continue (đúng slot vừa save) → xác nhận vị trí/inventory/
   quest/tutorial/world state đúng → (tuỳ chọn) Load một slot khác nếu có fixture an toàn → Quit
   Desktop.
3. Báo lại kết quả PASS/FAIL. Nếu PASS, Phase 10 chuyển thành `CONTENT_READY`.
4. Gamepad manual verification vẫn là action item tồn đọng từ mọi phase trước — không mới ở Phase 10.

## Việc Codex KHÔNG cần làm

- Không cần dựng Recovery UI mới (Corrupted/Incompatible screens) — không bắt buộc cho content-ready
  bar hiện tại; contract đã sẵn sàng nếu muốn làm sau.
- Không cần sửa `PauseMenuUI`, `QuestUIRoot`, `CommerceUIRoot`, Inventory UI, Tutorial UI hay bất kỳ
  world visual nào — Phase 10 không chạm layout của bất kỳ UI nào trong số này.
- Không cần thêm gameplay feature mới.

---

# Phase 9 — Save/Load/Return/Quit backend (Pause Menu Save/Load/Return/Quit UI cần Codex dựng)

Status (khi phase này bắt đầu): `READY_FOR_CODEX_UI`

Ngày: 2026-08-23
Feature: Phase 9 — Save/Load/Return/Quit backend (Pause Menu Save/Load/Return/Quit UI cần Codex dựng)

## Bối cảnh

Phase 9 Save/Load/Return/Quit backend đã hoàn tất và tự vận hành đúng (48/48 EditMode, 118/118
PlayMode PASS, Content Validation 0 error, DemoScene validator 0 issue, verify sống end-to-end qua
`execute_code` bao gồm reload `DemoScene` thật hai lần liên tiếp để chứng minh không rò dữ liệu giữa
slot). Chi tiết kiến trúc đầy đủ: [Phase9ImplementationReport.md](../Phase9ImplementationReport.md).

Việc cần Codex làm: thêm nút/UI Save Game, Load Game, Return Main Menu và Quit Desktop vào
`PauseMenuUI` hiện có (`Assets/Scripts/UI/PauseMenuUI.cs`), cùng slot overlay cho Load Game và popup
3 lựa chọn cho Return/Quit khi dirty. Toàn bộ logic/state/file I/O đã có sẵn qua
`GameplaySessionController`.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### GameplaySessionController (`Assets/Scripts/GameManagers/GameplaySessionController.cs`)

Đã gắn sẵn trên `_SceneContext` trong DemoScene, không cần tạo GameObject mới.

```csharp
public int ActiveSlotId { get; }
public bool IsDirty { get; }
public bool IsBusy { get; }   // true khi GameState là Saving hoặc Loading -- disable mọi nút Save/Load/Return/Quit khi true

public SaveSlotInfo[] RefreshSlots();   // đọc lại cả 3 slot, cũng tự fire OnSaveSlotListChanged
public bool CanLoad(int slotId);        // true chỉ khi slot đó Status == Valid

public bool RequestSave();
public bool RequestLoad(int slotId);

public void RequestReturnToMainMenu();          // clean -> return ngay; dirty -> fire OnConfirmationRequired, KHÔNG tự làm gì khác
public void ConfirmSaveAndReturn();             // chỉ Return sau khi save thật thành công
public void ConfirmReturnWithoutSaving();
public void CancelReturnToMainMenu();           // đóng popup, không đổi gì khác

public void RequestQuit();                      // clean -> quit ngay; dirty -> fire OnConfirmationRequired
public void ConfirmSaveAndQuit();
public void ConfirmQuitWithoutSaving();
public void CancelQuit();
```

Event/read-model:

```csharp
public event Action<SaveSlotInfo[]> OnSaveSlotListChanged;
public event Action OnSaveSucceeded;                                          // đã refresh slot xong khi fire
public event Action<GameplaySessionOperationResult, string> OnOperationFailed; // (lý do, message hiển thị được)
public event Action<GameplaySessionConfirmationKind> OnConfirmationRequired;   // ReturnToMainMenu hoặc Quit
```

`GameplaySessionOperationResult`: `Success`, `NoActiveSession`, `AlreadyBusy`, `SlotNotValid`,
`ReadFailed`, `WriteFailed`, `TransitionFailed` -- map từng giá trị sang message UI nếu muốn custom
hơn string mặc định đi kèm (string thứ hai trong `OnOperationFailed` đã là message thân thiện sẵn
dùng được luôn, không bắt buộc phải tự viết theo enum).

`GameplaySessionConfirmationKind`: `ReturnToMainMenu`, `Quit` -- dùng để chọn đúng popup 3 nút hiện
(nội dung khác nhau: "Save and Return"/"Return Without Saving"/"Cancel" vs "Save and Quit"/"Quit
Without Saving"/"Cancel"), gọi đúng `Confirm*`/`Cancel*` method tương ứng.

**Quan trọng**: `OnConfirmationRequired` **không** đổi `GameState` -- lúc này vẫn `Paused`. Popup chỉ
là UI navigation con (đúng `UIAndInteractionFlows.md`), không tạo `GameplayMenuPage` mới nếu không
cần. `Cancel*` cũng không đổi gì backend, chỉ cần đóng popup phía UI.

## Slot presentation dùng chung với MainMenu

`SaveSlotInfo`/`SaveSlotStatus`/`SaveSlotMetadata` là đúng type Codex đã dùng để dựng
`MainMenuSaveSlotsUI.cs` ở Phase 3 -- Load Game overlay trong gameplay có thể tái dùng cùng
presentation logic (Empty/Valid/Corrupted/IncompatibleVersion, level/area/playtime/last-saved) thay
vì tự viết lại. Điểm khác duy nhất: `GameplaySessionController.ActiveSlotId` cho biết slot nào đang
active để UI hiển thị rõ (theo `UIAndInteractionFlows.md`: "Load Game hiển thị ba slot nhưng phân
biệt rõ active slot").

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/GameManagers/GameplaySessionController.cs`,
  `SessionDirtyTracker.cs`, `GameSessionManager.cs`, `SceneFlowService.cs` -- nếu cần API/field mới,
  báo lại Claude qua `CodexToClaude.md`.
- Không cần tự capture save data hay gọi `ISaveSlotRepository` trực tiếp -- `RequestSave()` đã làm
  toàn bộ, UI chỉ gọi và lắng nghe event.
- Không cần tự theo dõi dirty state bằng tay -- đọc `controller.IsDirty` hoặc lắng nghe
  `GameSessionManager.Instance.DirtyStateChanged` nếu muốn hiển thị icon "unsaved changes" trong
  Pause Menu.
- Không cần lo về việc rò dữ liệu giữa các slot khi Load -- `SceneFlowService` đã được sửa để luôn
  teardown session cũ trước khi load session mới, verify sống bằng scene reload thật.
- Không gọi `Application.Quit()` trực tiếp -- không cần, `RequestQuit()`/`Confirm*Quit` đã dùng
  `IApplicationQuitter` nội bộ đúng yêu cầu testability.
- Không chỉnh `QuestUIRoot`, `CommerceUIRoot`, Inventory UI, Tutorial UI, MainMenu UI, hay layout/
  hierarchy hiện có của `PauseMenuUI` ngoài phần thêm mới cho Save/Load/Return/Quit.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: mở Pause → Save Game → thông báo thành công, timestamp/metadata cập nhật (kiểm tra lại
  qua Load Game overlay hoặc MainMenu).
- Manual: mở Pause → Load Game → chọn slot khác → xác nhận scene load lại, state cũ (inventory/
  quest/world) đúng của slot mới, không dính state slot cũ.
- Manual: chọn slot Corrupted/Empty trong Load Game overlay → bị disable hoặc hiện lỗi rõ, không
  crash.
- Manual: thay đổi gì đó (nhặt item, giết enemy, mở chest...) → `IsDirty == true` → bấm Return Main
  Menu → popup 3 lựa chọn hiện đúng. Test cả ba nhánh (Save and Return, Return Without Saving,
  Cancel).
- Manual: tương tự cho Quit Desktop (không cần thật sự quit app khi test bằng Editor Play Mode --
  `Application.Quit()` không có tác dụng trong Editor, chỉ log; đây là hành vi Unity bình thường,
  không phải bug).
- Manual: spam Save/Load/Return/Quit trong lúc `IsBusy == true` → chỉ một operation chạy, các lần
  bấm thêm bị từ chối êm (không crash, không double transition).

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Save/Load/Return/Quit UI trong Pause Menu thuộc Codex.
Khi xong, cập nhật `CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía
backend trừ khi phát sinh gap mới).

---

# Phase 8 — World Persistence backend (không cần UI mới; cần scene/prefab visual)

Status: `READY_FOR_CODEX_SCENE_INTEGRATION` (đã `VERIFIED` bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-23

## Bối cảnh

Phase 8 World Persistence backend đã hoàn tất và tự vận hành đúng (48/48 EditMode, 85/85 PlayMode
PASS, Content Validation 0 error, verify sống trong DemoScene + minimal portability scene qua
`execute_code`). Chi tiết kiến trúc đầy đủ: [Phase8ImplementationReport.md](../Phase8ImplementationReport.md).

Khác các phase trước, Phase 8 **không cần Codex dựng UI mới** -- bốn entity persistent (chest, unique
pickup, boss, resource node) hiện chưa có visual gì (không sprite, không animation, không prompt).
Việc cần Codex làm (khi có capacity, không gấp): thêm visual/interaction prompt cho bốn loại entity
này trong DemoScene, theo đúng contract public bên dưới -- không cần logic mới, chỉ cần trình bày.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### Bốn component persistent (`Assets/Scripts/World/`), tất cả implement `IPersistentWorldObject`

```csharp
// ChestInteractable
public bool IsOpened { get; }
public bool TryOpen(out bool granted);          // false + granted=false nếu đã mở hoặc hết chỗ chứa

// UniquePickupInteractable
public bool IsCollected { get; }
public bool TryCollect(out bool granted);        // tự SetActive(false) khi granted=true

// ResourceNodeInteractable
public bool IsAvailable { get; }                 // tính on-demand từ DateTime.UtcNow, không polling
public bool TryHarvest(out bool granted);

// BossDefeatTracker (đặt trên GameObject RIÊNG, không phải trên chính EnemyUniversal --
// xem "Lưu ý quan trọng" bên dưới)
public bool IsDefeated { get; }
```

Cả bốn đều có `PersistentId`/`Kind` (từ `IPersistentWorldObject`) và các field Inspector đã author
sẵn (`_rewardItemId`, `_itemId`, `_resourceId`, v.v. -- xem asset thật bên dưới để đọc giá trị cụ
thể, đừng đoán).

- `_openedIndicator` (Chest) / `_depletedIndicator` (ResourceNode): `GameObject` optional, tự động
  `SetActive` theo state nếu Codex gán -- có thể dùng ngay làm hook hiển thị mà không cần sửa script,
  chỉ cần kéo một GameObject con (icon/sprite khác) vào field đó qua Inspector.
- Pickup ẩn bằng cách tự `SetActive(false)` cả GameObject khi collected -- nếu cần hiệu ứng
  biến mất mượt hơn (fade/particle) thay vì biến mất tức thì, báo lại qua `CodexToClaude.md` để
  Claude đổi thành một event/hook riêng thay vì tự sửa `UniquePickupInteractable.cs`.

## Lưu ý quan trọng: `BossDefeatTracker` không nằm trên chính boss

`EnemyUniversal` tự `Destroy()` GameObject của nó vài giây sau khi chết (corpse lifetime). Nếu Codex
gắn thêm bất kỳ component nào cần sống lâu hơn con boss đó (ví dụ để hiển thị "Boss Defeated" banner)
thì phải đặt trên `BossDefeatTracker`'s GameObject (`BossTracker_ForestGuardian` trong DemoScene) chứ
không phải trên `ForestGuardianBoss` -- GameObject đó sẽ biến mất sau khi chết đúng như enemy thường.

## Content thật đã có để test ngay (không cần tạo asset mới)

Trong DemoScene (`_World`):

- `Chest_TownGeneral` (`world.chest.town.general.01`) -- mở ra nhận 2× Iron Ore.
- `Pickup_AncientRelic` (`world.pickup.tutorial.relic.01`) -- nhặt nhận 1× Ancient Relic (item mới,
  non-stackable).
- `ResourceNode_WoodLog` (`world.resource.tutorial.wood_log.01`) -- harvest nhận 2× Wood, cooldown
  60s, đồng thời phát Quest `ResourceGathered` event thật (đóng nốt integration gap Gather còn lại
  từ Phase 6/7).
- `BossTracker_ForestGuardian` (`world.boss.forest.guardian.01`) -- theo dõi enemy mới
  `ForestGuardianBoss` (duplicate của Goblin, `enemyId = enemy.boss.forest_guardian`, vị trí `(10,
  5)`, tách biệt hoàn toàn khỏi Kill objective `enemy.goblin.green` của `quest.main.001` nên không
  xung đột content Phase 7).
- `Assets/Prefabs/World/Chest.prefab` -- prefab asset của Chest (dùng để test portability;
  `Chest_TownGeneral` trong DemoScene hiện là instance rời, chưa link prefab này, có thể re-link nếu
  Codex muốn thống nhất workflow prefab-based).

Tất cả bốn đã đăng ký sẵn trong `WorldObjectRegistry` (component trên `_SceneContext`) -- save/load
đã hoạt động đúng, không cần Codex đụng vào phần đó.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/World/`, `WorldObjectRegistry`, hay
  `PlayerSpawnReadinessSource` -- nếu cần field/API mới (ví dụ hiệu ứng ẩn khác cho pickup, prompt
  UI riêng cho từng loại), báo lại Claude qua `CodexToClaude.md`.
- Không cần lo về save/restore -- `WorldObjectRegistry`/`PlayerSpawnReadinessSource` đã đảm bảo
  world state đúng trước khi Playing, idempotent, không phát event giả khi restore.
- Không chỉnh `QuestUIRoot`/`CommerceUIRoot`, Inventory UI, Tutorial UI, MainMenu UI.
- Không cần sửa `MapManager`/`SoundFXManager` -- `MapManager` đã được Claude sửa xong trong phase
  này (không còn `DontDestroyOnLoad`, rebind đúng khi scene reload).

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ visual/prompt/hierarchy cho bốn entity persistent thuộc Codex khi cần. Khi xong (hoặc nếu
quyết định không cần visual ở bước này), cập nhật `CodexToClaude.md`.

---

# Phase 7 — Shop/Crafting backend (Shop/Crafting UI cần Codex dựng tiếp)

Status: `VERIFIED` (đã Codex xác nhận, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Bối cảnh

Phase 7 Shop/Crafting backend đã hoàn tất và tự vận hành đúng (46/46 EditMode, 64/64 PlayMode PASS,
Content Validation 0 error, verify sống trong DemoScene qua `execute_code`). Chi tiết kiến trúc đầy
đủ: [Phase7ImplementationReport.md](../Phase7ImplementationReport.md).

Việc cần Codex làm: dựng Shop UI (mua/bán) và Crafting UI (chọn recipe/craft), cùng thêm
`ShopInteraction`/`CraftingInteraction` capability vào `TownElderNPC` (prefab đã có từ Phase 6, tái
dùng làm chủ shop/recipe ở phase này -- không cần NPC mới). Đây thuần là UI/Canvas + component bind
vào service có sẵn — **không cần** dựng Dialogue UI hay hệ thống resource/gather (chưa tồn tại,
ngoài phạm vi Phase 7).

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### ShopManager (`Assets/Scripts/Shop/ShopManager.cs`), qua `ShopManager.Instance`

Persistent singleton, luôn tồn tại trong DemoScene sau khi scene load xong.

```csharp
public IShopResolver Catalog { get; }   // .AllShops để liệt kê shop content
public bool TryPurchase(string shopId, string itemId, int quantity, out ShopTransactionResult result);
public bool TrySell(string shopId, string itemId, int quantity, out ShopTransactionResult result);
```

`ShopDefinition` (đọc qua `Catalog.AllShops` hoặc `Catalog.TryResolve(shopId, out def)`):
`ShopId`, `DisplayName`, `NpcId`, `Stock` (mỗi `ShopStockEntry` có `ItemId`/`Price`),
`SellPriceMultiplier`.

`ShopTransactionResult` enum: `Success`, `ShopNotFound`, `ItemNotInStock`, `InsufficientGold`,
`InsufficientInventoryCapacity`, `InsufficientItemQuantity`, `GameplayNotAllowed` -- dùng để hiển
thị lý do fail cụ thể.

**Lưu ý bán lại**: `TrySell` chỉ bán được item nằm trong chính `Stock` của shop đó (giá = `Price *
SellPriceMultiplier`). Bán item không thuộc stock trả `ItemNotInStock`/`ShopNotFound` tuỳ trường hợp
-- không phải bug, là scope quyết định (xem Known limitations trong report).

### CraftingManager (`Assets/Scripts/Crafting/CraftingManager.cs`), qua `CraftingManager.Instance`

```csharp
public IRecipeResolver Catalog { get; }   // .AllRecipes
public bool TryCraft(string recipeId, string stationTag, out CraftingTransactionResult result);
```

`RecipeDefinition`: `RecipeId`, `DisplayName`, `Ingredients` (mỗi `RecipeIngredientEntry` có
`ItemId`/`Quantity`), `OutputItemId`/`OutputQuantity`, `RequiredStationTag` (rỗng = craft mọi nơi,
truyền `null`/`""` cho `stationTag`), `NpcId`.

`CraftingTransactionResult` enum: `Success`, `RecipeNotFound`, `WrongStation`,
`InsufficientIngredients`, `InsufficientOutputCapacity`, `GameplayNotAllowed`.

### ShopNpcInteractionService / CraftingNpcInteractionService (plain C#, không MonoBehaviour)

NPC component tạo instance (`new ShopNpcInteractionService(ShopManager.Instance)`,
`new CraftingNpcInteractionService(CraftingManager.Instance)`) và gọi qua đây, giống pattern
`QuestNpcInteractionService` đã dùng cho `TownElderNPC`:

```csharp
// Shop
public bool TryGetShop(string npcId, out ShopDefinition shop);
public bool TryPurchase(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result);
public bool TrySell(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result);

// Crafting
public IReadOnlyList<RecipeDefinition> GetOfferedRecipes(string npcId);
public bool TryCraft(string npcId, string recipeId, string stationTag, out CraftingTransactionResult result);
```

Cả hai validate đúng `npcId` sở hữu shop/recipe trước khi chạm Manager -- NPC component không cần tự
kiểm tra ownership.

## Content thật đã có để test UI ngay (không cần tạo asset mới)

- `Assets/Shops/ShopCatalog.asset` → `shop.town.general` (`npcId = npc.town.elder`, tái dùng
  `TownElderNPC`): bán `item.material.wood` (5 gold), `item.consumable.health_potion` (20 gold).
- `Assets/Crafting/RecipeCatalog.asset` → 2 recipe (cả hai `npcId = npc.town.elder`):
  `recipe.material.plank` (3× Wood → 1× Plank, không cần station) và
  `recipe.consumable.health_potion` (2× Wood + 1× Iron Ore → 1× Health Potion, cần
  `stationTag = "station.forge"`).
- 3 item mới có icon placeholder: `item.material.iron`, `item.material.plank`,
  `item.consumable.health_potion` (`Assets/Resources/Items/Shop/`).
- Player không có sẵn Iron Ore trong starting inventory -- để test recipe cần station, seed tạm qua
  `Resources.Load<ItemSO>("Items/Shop/IronOre")` + `InventoryManager.Instance.AddItem(...)`, hoặc
  đợi hệ thống Gather (chưa tồn tại) cấp Iron Ore thật ở phase sau.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/Shop/`, `Assets/Scripts/Crafting/` -- nếu
  cần field/API mới (ví dụ stock quantity giới hạn, base sell value chung), báo lại Claude qua
  `CodexToClaude.md`.
- Không cần lo về Quest integration -- `ShopManager.TryPurchase`/`CraftingManager.TryCraft` đã tự
  raise `QuestDomainEvents.ItemPurchased`/`ItemCrafted` thật, `QuestManager` (Phase 6) đã subscribe
  sẵn; UI chỉ cần gọi transaction, không cần biết gì về Quest.
- Không cần dựng Dialogue UI hay Resource/Gather UI (chưa tồn tại, ngoài phạm vi Phase 7).
- Không chỉnh `QuestUIRoot`, visual của `TownElderNPC`, Inventory UI, Tutorial UI hay layout/font
  hiện có -- chỉ thêm component/Canvas mới cho Shop/Crafting.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: mở Shop tại `TownElderNPC`, mua Health Potion → gold trừ đúng, item vào inventory; mua khi
  không đủ gold → từ chối, không trừ gì.
  bán lại Wood cho đúng shop đó → gold cộng đúng theo `sellPriceMultiplier`.
- Manual: mở Crafting tại `TownElderNPC`, craft Wood Plank (không cần station) → thành công, nguyên
  liệu bị trừ đúng. Craft Health Potion không đứng gần station (nếu UI có khái niệm station theo
  vị trí) → `WrongStation`; đứng đúng chỗ có `stationTag = "station.forge"` → thành công.
- Manual: một quest test tạm với objective Purchase/Craft (không có sẵn trong content hiện tại, có
  thể tạo asset tạm chỉ để verify rồi xoá) → xác nhận progress lên `ReadyToTurnIn` sau giao dịch
  thật, không cần click nào khác ngoài nút Buy/Craft.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Shop/Crafting UI và mọi thay đổi trên
`TownElderNPC` prefab thuộc Codex. Khi xong, cập nhật `CodexToClaude.md` để Claude biết UI đã sẵn
sàng (không cần thay đổi gì phía backend trừ khi phát sinh gap mới).

---

# Phase 6 — Quest backend (Quest Log/Tracker/NPC UI cần Codex dựng tiếp)

Status: `READY_FOR_CODEX_UI_BINDING` (đã `VERIFIED` bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Update 2026-08-22 — Trả lời 2 gap trong `CodexToClaude.md` (`BACKEND_GAP_FOUND`)

Đã xử lý cả hai gap Codex báo lại sau khi dựng `QuestUIRoot`/`TownElderNPC`. Không đổi
`QuestUIRoot`, `TownElderNPC` prefab, Tutorial UI, Inventory UI hay bất kỳ layout/font nào.

### 1. Presentation API cho objective progress

Thêm `QuestManager.TryGetProgress` (không đổi `ToSaveData`, không cần reflection):

```csharp
public bool TryGetProgress(string questId, out QuestProgressSnapshot snapshot);
```

`QuestProgressSnapshot` (`Assets/Scripts/Quest/QuestProgressSnapshot.cs`) là `readonly struct`:

```csharp
public QuestStatus Status { get; }
public int CurrentObjectiveIndex { get; }
public IReadOnlyList<int> ObjectiveCounters { get; }   // đồng bộ index với QuestDefinition.Objectives
```

- Trả `false` (snapshot mặc định) nếu quest chưa có runtime entry -- tức đang `Locked`/`Available`,
  chưa accept lần nào, không có gì để hiển thị progress.
- `ObjectiveCounters` là **bản copy tại thời điểm gọi** (`Clone()` trên array runtime), không phải
  live reference -- sửa mảng trả về không ảnh hưởng `QuestRuntimeState` thật, và gọi lại
  `TryGetProgress` sau khi có event mới sẽ ra snapshot mới đúng dữ liệu. Không expose mutable
  collection ra ngoài Definition/Runtime/Save boundary.
- Dùng cùng `CurrentObjectiveIndex` để index vào `QuestDefinition.Objectives[index].Description`/
  `.TargetCount` cho instruction text + target hiện tại; `ObjectiveCounters[index]` là progress số
  (`counters[i]` ứng với `Objectives[i]`, kể cả objective đã qua).
- Ví dụ hiển thị "1/2 killed" cho objective Kill đang active:
  `int current = snapshot.ObjectiveCounters[snapshot.CurrentObjectiveIndex]; int target =
  quest.Objectives[snapshot.CurrentObjectiveIndex].TargetCount;`

Verify sống trong DemoScene (Play Mode thật): accept `quest.tutorial.crafting.001` →
`TryGetProgress` = true, `counters = [0,0]`; sau 1 `RaiseEnemyKilled` khớp objective 0 →
`counters = [1,0]`. Trước khi accept, `TryGetProgress` = false đúng như spec.

### 2. Description rỗng cho objective + validator

Đã author `Description` cho toàn bộ objective của cả hai quest hiện có
(`Assets/Quests/Definitions/Quest_TutorialCrafting001.asset`,
`Assets/Quests/Definitions/Quest_Main001.asset`) -- không còn objective nào rỗng text.

`ContentValidationRunner.ValidateQuestObjective` giờ báo **Error** (không phải Warning) cho
objective có `Description` rỗng/whitespace -- coi đây là required presentation field, đúng nguyên
tắc "Handoff phải cung cấp read-model/contract public ổn định, không tự chế display data cho UI"
(Roadmap Phase 6 Boundary). Content Validation chạy lại: **0 error, 60 warning (không đổi), 69
asset checked**.

### Test

- EditMode: 42/42 PASS (không đổi số lượng file mới -- chỉ patch content).
- PlayMode: 48/48 PASS (+1: `QuestManagerPlayModeTests.TryGetProgress_ReflectsLiveStateAndReturnsADefensiveCopy`
  -- verify false khi chưa accept, đúng status/index/counters sau accept + progress, và mutate bản
  copy trả về không leak vào runtime state thật).
- Play Mode smoke test qua Unity MCP `execute_code` trên DemoScene thật (không chỉ test giả lập):
  xác nhận `TryGetProgress` hoạt động đúng trên `QuestManager.Instance` thật, console sạch.

Không có thay đổi nào khác tới contract Phase 6 gốc (event/status semantics/NPC service không đổi).

## Bối cảnh

Phase 6 Quest backend đã hoàn tất và tự vận hành đúng (42/42 EditMode, 47/47 PlayMode PASS, Content
Validation 0 error, verify sống trong DemoScene qua `execute_code`). Chi tiết kiến trúc đầy đủ:
[Phase6ImplementationReport.md](../Phase6ImplementationReport.md).

Việc cần Codex làm: dựng Quest Log/Tracker UI (danh sách quest Active/ReadyToTurnIn/Completed,
objective progress hiển thị được cho người chơi) và NPC marker/interaction UI (offer quest / turn-in
prompt) trong DemoScene. Đây thuần là UI/Canvas + một NPC prefab tối thiểu để test — **không cần**
Dialogue/Shop/Crafting UI thật (những hệ thống đó chưa tồn tại, xem "Integration gap" bên dưới).

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

### QuestManager (`Assets/Scripts/Quest/QuestManager.cs`), qua `QuestManager.Instance`

Persistent singleton, luôn tồn tại trong DemoScene sau khi scene load xong (giống
`TutorialManager.Instance`/`InventoryManager.Instance`).

```csharp
public IQuestResolver Catalog { get; }                 // .AllQuests để liệt kê toàn bộ quest content
public bool IsMainQuestUnlocked { get; }

public event Action<string> QuestAccepted;             // questId
public event Action<string> QuestProgressChanged;      // questId -- objective counter/status đổi
public event Action<string> QuestCompleted;             // questId -- fire đúng 1 lần khi TryTurnIn thành công
public event Action MainQuestUnlocked;                  // fire đúng 1 lần khi Tutorial Quest chain xong

public QuestStatus GetStatus(string questId);           // Locked/Available/Active/ReadyToTurnIn/Completed/Failed
public bool TryAcceptQuest(string questId);
public bool TryTurnIn(string questId, out QuestTurnInResult result);
```

`QuestDefinition` (đọc qua `Catalog.AllQuests` hoặc `Catalog.TryResolve(questId, out def)`) có field
đọc được: `QuestId`, `DisplayName`, `Objectives` (mỗi objective có `Type`, `TargetId`, `TargetCount`,
`Description` -- dùng `Description` để hiển thị text, đừng tự chế), `IsTutorialQuest`, `IsMainQuest`,
`GiverNpcId`, `TurnInNpcId`.

Objective progress hiện tại (counter/index) không có getter công khai trực tiếp trên
`QuestRuntimeState` qua `QuestManager` -- nếu UI cần hiển thị "2/3 killed", báo lại Claude qua
`CodexToClaude.md` để thêm getter (ví dụ `QuestManager.TryGetProgress(questId, out int index, out
int[] counters)`), đừng tự đọc reflection vào private field.

### QuestNpcInteractionService (`Assets/Scripts/Quest/QuestNpcInteractionService.cs`)

Plain C# (không phải MonoBehaviour) -- NPC component của Codex tạo một instance
(`new QuestNpcInteractionService(QuestManager.Instance)`) và gọi qua đây, **không** gọi thẳng
`QuestManager` cho logic liên quan tới NPC identity:

```csharp
public bool TryGetOfferedQuest(string npcId, out QuestDefinition quest);   // quest Available mà npcId này cho
public bool TryAcceptQuest(string npcId, string questId);
public bool TryGetTurnInQuest(string npcId, out QuestDefinition quest);    // quest ReadyToTurnIn tại npcId này
public bool TryTurnIn(string npcId, string questId, out QuestTurnInResult result);
public void ReportConversation(string npcId, string outcomeId);           // cho Talk objective (xem gap)
```

`QuestTurnInResult` enum: `Success`, `QuestNotFound`, `ObjectivesIncomplete`,
`InsufficientInventoryCapacity`, `AlreadyCompleted` -- dùng để hiển thị lý do fail cụ thể thay vì
generic "failed".

## Content thật đã có để test UI ngay (không cần tạo asset mới)

- `Assets/Quests/QuestCatalog.asset` chứa 2 quest:
  - `quest.tutorial.crafting.001` ("The Blacksmith's Request", Tutorial Quest): Kill
    `enemy.slime.green`×2 tại `area.tutorial` + Obtain `item.material.wood`×3. `giverNpcId` =
    `turnInNpcId` = `npc.town.elder`.
  - `quest.main.001` ("A Call to Adventure", Main Quest, prerequisite = quest trên): Kill
    `enemy.goblin.green`×1.
- 3 enemy có sẵn trong DemoScene đã gắn `enemyId` thật: `Slime1`/`Slime2` = `enemy.slime.green`,
  `Goblin` = `enemy.goblin.green` (tất cả `areaId = area.tutorial`) -- giết chúng bằng gameplay thật
  sẽ tiến quest thật, không cần giả lập.
- **Chưa có NPC GameObject/prefab nào trong scene** -- `npc.town.elder` chỉ là stable ID trong data,
  chưa có world object tương ứng. Codex cần tự tạo GameObject/prefab NPC tối thiểu (collider tương
  tác + component gọi `QuestNpcInteractionService`) để test luồng offer/accept/turn-in bằng gameplay
  thật; không có ràng buộc hierarchy/tên cụ thể nào từ phía Claude cho NPC này.

## Integration gap có chủ đích (đừng tự chế UI cho phần này)

4 objective type sau **chưa có hệ thống production thật** (không Dialogue/Crafting/Shop/Resource
system trong project) -- đây là quyết định đã ghi rõ trong
[Phase6ImplementationReport.md](../Phase6ImplementationReport.md):

- **Talk**: `QuestNpcInteractionService.ReportConversation(npcId, outcomeId)` là entry point, nhưng
  chưa có Dialogue UI/system thật gọi nó. Nếu Codex dựng NPC "nói chuyện" đơn giản (không phải full
  dialogue tree), có thể gọi `ReportConversation` trực tiếp khi player tương tác — đó là hợp lệ, không
  phải giả lập test.
- **Craft/Purchase**: cần `CraftingService`/`ShopService` (Phase 7, chưa tồn tại). Đừng dựng Shop/
  Crafting UI giả trong Phase 6 UI pass này.
- **Gather**: cần Resource/gather interaction script (chưa có phase cụ thể). Tương tự, không tự chế.

Nếu UI cần hiển thị các objective type này trước khi hệ thống thật tồn tại, hiển thị đúng
`Description`/`TargetCount` như objective khác — không cần logic tương tác thật cho tới khi Phase 7+.

## Việc Codex KHÔNG cần làm

- Không cần đổi bất kỳ script nào trong `Assets/Scripts/Quest/`, `InventoryManager.HasItemId`,
  `InventoryManager.AddItem` (chỗ raise `InventoryItemAdded`), hay `EnemyUniversal._enemyId`/`_areaId`
  -- nếu cần field/API mới (ví dụ progress getter ở trên), báo lại Claude qua `CodexToClaude.md`.
- Không cần lo về restore/save -- `QuestManager.RestoreState()` (backend, gọi từ
  `PlayerSpawnReadinessSource`) đã đảm bảo UI mở giữa chừng vẫn thấy đúng status/progress qua
  `GetStatus`/`Catalog`.
- Không cần dựng Shop/Crafting/Dialogue UI thật (xem Integration gap).
- Không chỉnh `TutorialOverlayRoot`/`TutorialOverlayUI` hay Inventory UI hiện có.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: NPC hiển thị đúng quest offer khi `quest.tutorial.crafting.001` = `Available`; sau accept,
  giết 2 slime + nhặt 3 wood → UI phản ánh `ReadyToTurnIn`; turn-in tại đúng NPC → reward vào
  inventory, quest biến mất khỏi active list, `quest.main.001` chuyển `Available`.
  Cách tạo Obtain event thật: `InventoryManager.Instance.AddItem` trên `item.material.wood`
  (`Resources.Load<ItemSO>("Items/Quest/WoodMaterial")`) -- không cần world pickup script mới nếu
  chưa có, chỉ cần test UI phản ứng đúng khi backend event fires.
- Manual: turn-in tại NPC sai (không phải `turnInNpcId`) bị từ chối, không hiện reward.
- Manual: Continue game giữa chừng quest -- UI hiện đúng status/progress ngay khi vào scene, không
  cần đợi event đầu tiên.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho Quest Log/Tracker/NPC UI thuộc Codex. Khi xong, cập nhật
`CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía backend trừ khi phát
sinh gap mới, ví dụ cần thêm getter progress).

---

# Phase 5 — UI hiển thị Input Tutorial (instruction prompt + skip)

Status: `READY_FOR_CODEX` (đã VERIFIED bởi Codex, xem `CodexToClaude.md`)

Ngày: 2026-08-22

## Bối cảnh

Phase 5 backend (`TutorialManager`, domain event, save/restore, `AreaTriggerZone`) đã hoàn tất, tự
vận hành đúng (28/28 EditMode, 32/32 PlayMode PASS) nhưng **chưa có UI nào hiển thị cho người chơi
thấy**. Chi tiết kiến trúc đầy đủ: [Phase5ImplementationReport.md](../Phase5ImplementationReport.md).

Việc cần Codex làm: dựng một overlay nhỏ trong DemoScene hiển thị `InstructionText` của step tutorial
hiện tại, và nút Skip có confirm. Đây thuần là UI/Canvas — không cần đổi gameplay logic.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

`TutorialManager` (`Assets/Scripts/Tutorial/TutorialManager.cs`), truy cập qua
`TutorialManager.Instance` (persistent singleton, luôn tồn tại trong gameplay scene sau khi
`PlayerSpawnReadinessSource` restore xong):

```csharp
public TutorialStepDefinition CurrentStep { get; }   // null nếu đã completed hoặc chưa có definition
public bool IsCompleted { get; }
public event Action<TutorialStepDefinition> OnStepChanged;   // fire khi qua step mới
public event Action OnTutorialCompleted;                     // fire đúng 1 lần khi xong step cuối
public void Skip();                                           // nhảy thẳng completed, không phát OnStepChanged
```

`TutorialStepDefinition` có field đọc được: `StepId` (string), `Type` (enum, không cần hiển thị),
`InstructionText` (string — đây là nội dung để show lên UI).

## UI cần dựng

1. **Panel instruction** (góc màn hình, ví dụ top-center hoặc top-left, không che HUD/inventory hiện
   có) — hiển thị `CurrentStep.InstructionText`.
   - Ẩn hoàn toàn nếu `TutorialManager.Instance == null` hoặc `CurrentStep == null` (đã completed
     hoặc chưa init xong).
   - Subscribe `OnStepChanged` để đổi text khi qua step mới.
   - Subscribe `OnTutorialCompleted` để ẩn panel (kèm hiệu ứng nhẹ nếu muốn, không bắt buộc).
   - Khi UI vừa `OnEnable`/mở game giữa chừng (ví dụ Continue), đọc luôn `CurrentStep` hiện tại để
     hiển thị đúng ngay lập tức — không đợi event đầu tiên.
2. **Nút Skip** trên panel đó, có **popup confirm** trước khi gọi (theo D-008 — skip tutorial phải có
   xác nhận, không skip ngay khi bấm 1 lần). Sau khi user xác nhận: gọi
   `TutorialManager.Instance.Skip()`.
3. Panel này là **gameplay overlay thuần túy** giống Inventory/Pause hiện có — không đi qua
   `GameStateManager` state machine (tutorial không pause game, không chặn input), chỉ là Canvas hiển
   thị/ẩn theo event ở trên.

## Việc Codex KHÔNG cần làm

- Không cần đổi `TutorialManager`, domain event, hay bất kỳ script nào trong
  `Assets/Scripts/Tutorial/`, `Assets/Scripts/GameManagers/AreaTriggerZone.cs` — nếu thấy cần đổi field
  gì ở đó (ví dụ thêm icon cho step, thêm field mới trong `TutorialStepDefinition`), báo lại Claude
  qua `CodexToClaude.md` thay vì tự sửa (đây là ScriptableObject data contract, đổi ẩu có thể vỡ save
  cũ hoặc content asset đã tạo).
- Không cần lo về restore/save — `TutorialManager.RestoreState()` (backend) đã đảm bảo UI mở lên giữa
  chừng vẫn thấy đúng step hiện tại qua `CurrentStep`.
- Chưa cần làm UI cho `AreaTrigger_Town`/`ReachArea` riêng — step đó cũng chỉ là một `InstructionText`
  bình thường như các step khác, panel dùng chung.

## Nội dung step hiện có (để tham khảo hiển thị, đọc thật từ asset, đừng hardcode text trong UI script)

6 step trong `Assets/Tutorial/Tutorial_TutorialArea.asset`: Move → Sprint → Attack → OpenInventory →
EquipItem → ReachArea (`area.town`, placeholder position `(10,0,0)` trong DemoScene, sẽ dời khi có Town
thật). `InstructionText` hiện tại là placeholder — nếu cần văn bản hiển thị đẹp hơn, có thể tự sửa nội
dung field đó trực tiếp trên asset qua Unity Editor (đây là content, không phải code, Codex có thể sửa
tự do), không cần hỏi lại Claude cho việc đổi text thuần túy.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: New Game → panel hiện đúng step Move → đi bộ → panel đổi sang Sprint → ... → sau step cuối
  panel ẩn.
- Manual: bấm Skip → confirm popup hiện → xác nhận → panel ẩn ngay, không đi qua step trung gian.
- Manual: Continue game đã có tutorial dở dang → panel hiện đúng step đã lưu ngay khi vào scene.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho panel này thuộc Codex. Khi xong, cập nhật
`CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía backend trừ khi phát sinh
gap mới).
