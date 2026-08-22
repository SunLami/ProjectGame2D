# Codex → Claude Handoff

Status: `READY_FOR_CLAUDE`

Ngày: 2026-08-22  
Feature: Phase 3 MainMenu New Game/Continue UI

## UI đã triển khai

- Dựng scene-authored UI trong `Assets/Scenes/MainMenu.unity/_UI/MainMenuCanvas/MainMenuRoot` bằng
  Unity MCP; không tạo hierarchy lúc runtime.
- Landing có `New Game`, `Continue`, `Settings`, `Quit`.
- New Game/Continue dùng chung selector đúng ba slot.
- Slot view hiển thị trạng thái `Empty`, `Valid`, `Corrupted`, `IncompatibleVersion` bằng chữ; không chỉ
  dùng màu.
- Slot hợp lệ chỉ hiển thị dữ liệu backend hiện có thật: total play time và last-saved local time.
  Không tự chế character name, level, area hoặc tutorial state.
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

## Backend gaps phát hiện

1. `SaveSlotMetadata.characterName`, `characterLevel`, `areaId`, `tutorialCompleted` vẫn là giá trị mặc
   định, đúng như Phase 3 report. UI hiện không hiển thị dữ liệu giả. Nếu muốn list slot hiển thị level,
   area hoặc tutorial thật, Claude cần populate metadata từ snapshot thật.
2. Initial New Game save được đọc lại thành công nhưng `lastSavedUtcTicks` hiện là `0`, nên UI hiển thị
   `Unknown`. Claude cần xác nhận repository/factory owner nào phải cập nhật timestamp trước atomic write.
3. Phase 3 chưa có in-game save/capture current Player position khi rời. Vì vậy acceptance “Continue
   khớp vị trí ngay trước khi rời” chỉ chứng minh được với vị trí đã tồn tại trong snapshot, không phải
   transform vừa thay đổi trong gameplay. Đây thuộc save capture/Phase 9 hoặc một scope backend được
   chấp thuận riêng; UI không tự ghi DTO.

## Task tiếp theo thuộc Claude

- Quyết định và triển khai metadata population thật (ít nhất `lastSavedUtcTicks`; level/area nếu muốn
  hiển thị trong Phase 3).
- Xác nhận scope save-current-position trước Return Main Menu; không để UI tự sở hữu capture/write.
- Thêm automated PlayMode coverage cho `MainMenuSaveSlotsUI` contract nếu Phase 3 yêu cầu toàn bộ UI
  integration được gate tự động.

## Phạm vi Claude không nên chỉnh trực tiếp

- Không chỉnh hierarchy/layout/colors/font của `MainMenuRoot`.
- Nếu contract field/event thay đổi, cập nhật handoff để Codex rebind bằng Unity MCP.

