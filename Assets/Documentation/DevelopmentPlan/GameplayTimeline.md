# Gameplay Timeline

## Trạng thái

- Accepted từ yêu cầu — 2026-09-08.
- Triển khai theo từng scene; hiện chỉ có Scene 01 qua cầu.

## Scene 01 — Cross the Bridge

- Bắt đầu sau sự kiện `IntroCutsceneController.Completed` trong New Game.
- Player bắt đầu tại marker `Intro` ở đầu trái cầu (`-27.88571, -5.71325`), đi sang phải trong bốn
  giây và dừng tại đầu phải cầu (`-24.25, -6.42`).
- Chuyển động và thời lượng phải nhìn thấy, chỉnh được bằng `Animation Track` chuẩn trong GameObject
  `Intro` ở Hierarchy, dùng `Assets/Timeline/IntroTimeline.playable`. Mỗi phân cảnh là một Group Track
  rõ ràng trong cùng Timeline.
- Scene 01 dùng `Scene01_PlayerCrossBridge.anim`: Position X/Y và sprite animation `WalkRight` đều là
  keyframe native, chỉnh được bằng Timeline/Animation window; không dùng custom movement track.
- Trong cảnh, `GameState.Cutscene` khóa gameplay input. Timeline dùng unscaled time vì Cutscene pause world.
- Kết thúc Timeline trả state về `Playing`.
- Chưa thêm NPC, dialogue, camera shot hoặc save progress; các scene sau được author tuần tự theo yêu cầu.
