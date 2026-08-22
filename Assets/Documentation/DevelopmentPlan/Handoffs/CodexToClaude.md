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
