# Intro Cutscene — Orynthals

## Trạng thái

- Accepted từ yêu cầu content — 2026-09-06.
- Scope hiện tại là cinematic mở đầu; Timeline gameplay sau Outro là hạng mục kế tiếp.

## Nội dung và asset

Intro gồm bảy đoạn theo đúng thứ tự trong `IntroCutsceneDefinition`:

1. `LogoIntro.mp4`
2. `IntroScene1.mp4` — The First Spark
3. `IntroScene2.mp4` — A Promise at Dawn
4. `IntroScene3.mp4` — The Road Calls
5. `IntroScene4.mp4` — The Village of Beginnings
6. `IntroScene5.mp4` — A Guide Appears
7. `OutroTransition.mp4`

Video nằm ở `Assets/Cinematics/Intro/Videos/`. Definition dùng stable ID
`cutscene.intro.orynthals`; các segment ID chỉ là content ID, không được dùng làm save progress.
Timeline biên tập nằm ở `Assets/Cinematics/Intro/Timelines/IntroCutsceneTimeline.playable` và prefab
presentation nằm ở `Assets/Prefabs/Cinematics/IntroCutscene.prefab`.

## Runtime contract

- `NewGame` luôn phát Intro. `Development` phát Intro khi prefab bật `Play In Development`.
- `LoadedGame` không phát Intro để Continue không lặp cinematic.
- Trong `GameState.Cutscene`, world dừng, gameplay input bị khóa, UI và con trỏ vẫn hoạt động để dùng
  nút **Tiếp**, **Bỏ qua cảnh**, và **Bỏ qua intro**.
- Bỏ qua chỉ kết thúc cinematic; không thay đổi tutorial, quest, world state, dirty session hay save schema.
- Mỗi scene có thoại tĩnh. Người chơi nhấn **Tiếp** hoặc Space/Enter để qua từng đoạn thoại; Escape bỏ
  qua scene hiện tại. Scene không có thoại tự chuyển khi video hết.
- Outro kết thúc ở trạng thái `Playing`. Gameplay Timeline sẽ được nối ở phase kế tiếp và tự quản lý
  `GameState.Cutscene` của nó.
- `MusicManager` bị suppress từ lúc intro bắt đầu; track nền của gameplay chỉ bắt đầu khi Outro hoàn
  tất và controller bàn giao về gameplay (`Finish`).

## Authoring và kiểm tra

- Chạy **Tools > Project Game 2D > Cinematics > Create Or Update Intro Cutscene** để đồng bộ definition,
  Timeline và prefab sau khi thay clip hoặc thoại.
- Mở `DemoScene`, chạy **Install Intro Cutscene In Active Scene**, sau đó lưu scene. `DemoScene` là nơi
  integration chính thức; không author trực tiếp vào `MapNhat`.
- Kiểm tra đủ bảy VideoClip, thứ tự Timeline, nút UI, phát NewGame/Development, Continue không phát,
  skip không làm thay đổi save/progression và không có error Console.
