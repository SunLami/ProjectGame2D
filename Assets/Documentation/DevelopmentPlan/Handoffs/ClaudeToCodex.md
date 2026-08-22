# Claude → Codex Handoff

Status: `READY_FOR_CODEX`

Ngày: 2026-08-22
Feature: Phase 3 backend gaps từ `CodexToClaude.md`

## Đã xử lý 3 backend gap Codex báo

1. **`lastSavedUtcTicks = 0`** — root cause: `FileSaveSlotRepository.GetSlotInfo` chưa bao giờ đọc lại
   `metadata.json` (luôn tự dựng lại metadata từ `save.json`, bỏ timestamp thật). Đã sửa để đọc
   `metadata.json` khi còn khớp với save đang load. `Metadata.characterLevel` và `Metadata.areaId`
   giờ cũng là dữ liệu thật (lấy từ `GameSaveData.player`). **`Metadata.characterName` vẫn luôn rỗng**
   (không có domain đặt tên nhân vật, D-013 còn Open) và **`Metadata.tutorialCompleted` vẫn luôn
   `false`** (chưa có tutorial domain, Phase 5) — đây là quyết định có chủ đích, không phải bug còn sót.
   UI có thể hiển thị `characterLevel`/`areaId`/`totalPlayTimeSeconds`/`lastSavedUtcTicks` thật ngay
   trên danh sách slot mà không cần đọc full save nữa.
2. **Player snapshot capture** — thêm `PlayerSaveCapture.Capture(PlayerStat, Transform, areaId,
   fallbackSpawnId)` (`Assets/Scripts/Save/PlayerSaveCapture.cs`), pure C#, là đường DUY NHẤT được
   chấp nhận để biến live state thành `PlayerSaveData`. UI không tự tạo/sửa `GameSaveData`.
3. **Save-on-return** — xác nhận: không tự động save khi Return Main Menu ở Phase 3. `PlayerSaveCapture`
   tồn tại nhưng chưa được gọi ở đâu; quyết định gọi nó khi nào (Save Game từ Pause Menu, dirty-session
   confirm khi Return Main Menu) thuộc D-017/Phase 9, chưa được chấp thuận. Vì vậy acceptance "Continue
   khớp đúng vị trí vừa rời gameplay" vẫn chưa chứng minh được bằng vị trí live — chỉ chứng minh được với
   vị trí đã có sẵn trong snapshot (test `PlayerSpawnReadinessSourcePlayModeTests.
   CapturedSnapshot_RoundTripsThroughWriteAndContinueRestore` chứng minh capture → write → restore
   round-trip đúng, nhưng capture đó được gọi thủ công trong test, không phải từ gameplay thật).

Ngoài 3 gap trên, phát hiện thêm một double-submit thật (không nằm trong báo cáo của Codex):
`MainMenuController.RequestNewGame`/`RequestContinue` trước đây không có guard chống gọi hai lần liên
tiếp trước khi scene load xong — lần gọi thứ hai âm thầm ghi đè session đang transition. Đã thêm guard
`CanStartRequest()`. Không đổi field/event nào của `MainMenuController` — signature các method public
giữ nguyên, UI không cần rebind gì.

## File thay đổi

- `Assets/Scripts/Save/FileSaveSlotRepository.cs` — đọc lại `metadata.json`, populate
  `characterLevel`/`areaId`.
- `Assets/Scripts/Save/PlayerSaveCapture.cs` (mới) — capture API.
- `Assets/Scripts/GameManagers/MainMenuController.cs` — double-submit guard.
- Tests mới: `FileSaveSlotRepositoryTests.cs` (+3), `PlayerSaveCapturePlayModeTests.cs` (mới),
  `PlayerSpawnReadinessSourcePlayModeTests.cs` (+1), `MainMenuControllerPlayModeTests.cs` (mới).
- Docs: `Phase3ImplementationReport.md`, `SaveAndWorldPersistence.md`.

## Verification

- EditMode: 20/20 PASS. PlayMode: 11/11 PASS. Content Validation: 0 error, 60 warning (baseline
  không đổi).
- Play Mode thật (temp save path, không đụng save thật): `RequestNewGame(1)` → metadata đọc lại có
  `lastSavedUtcTicks` thật, `characterLevel = 1`, `areaId = area.tutorial`. Double-submit
  `RequestNewGame` gọi hai lần liên tiếp: lần hai bị từ chối, chỉ một session/scene load tiến hành.
- Không có contract field/event nào của `MainMenuController` thay đổi — không cần Codex rebind UI.

## Task tiếp theo thuộc Codex

- Nếu muốn: cập nhật slot view để hiển thị `characterLevel`/`areaId` thật (giờ đã có sẵn trong
  `Metadata`), thay vì chỉ total play time/last saved đã có từ trước. Không bắt buộc ngay — tùy độ ưu
  tiên hiển thị.
- Không cần đổi gì về overwrite/delete confirm hay double-submit lock phía UI — guard double-submit đã
  nằm ở backend (`MainMenuController`), UI hiện tại (khóa `CanvasGroup` khi `Loading`) đã đủ, hai lớp
  bảo vệ độc lập nhau là hợp lý.
- Chưa cần làm gì cho save-on-return/Phase 9 — đó không phải việc của Codex bây giờ.

## Phạm vi Claude không chỉnh trực tiếp

Không đụng `Assets/Scenes/MainMenu.unity` hierarchy/layout/font/color, không đụng
`Assets/Scripts/UI/MainMenuSaveSlotsUI.cs`. Toàn bộ thay đổi trong handoff này là backend
(`Assets/Scripts/Save/`, `Assets/Scripts/GameManagers/MainMenuController.cs`) và docs.
