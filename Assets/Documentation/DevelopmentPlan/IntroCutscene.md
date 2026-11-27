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

Dialogue presentation dùng chung visual language với Dialogue gameplay: frame
`Assets/Resources/UI/Dialogue/DarkInventoryStyle/dialogue_frame_v4.png`, nền tối bán trong suốt,
viền vàng pixel-art và sapphire tiết chế. `Storyteller`/speaker nằm trong nameplate phải dưới; body
text nằm trong safe area và auto-size/ellipsis để không tràn. Ba action `NEXT`, `SKIP SCENE`,
`SKIP INTRO` dùng chung sprite nền độc lập
`dialogue_action_button_v1.png`; label vẫn là TMP runtime, không bake vào ảnh.
`PresentationRoot` mặc định inactive trong prefab/scene để Game tab ở Edit Mode không hiện nền video
trắng hoặc khung thoại; `IntroCutsceneController` chỉ bật presentation khi intro thực sự chạy.
Body text dùng safe area theo viền artwork (không dùng toàn bộ bounds của panel), với lề ngang 120 px
ở reference resolution để glyph không đè lên đường viền trang trí.

## Bong bóng thoại Gameplay Timeline (D-123, 2026-11-27)

Sau Outro, thoại của Timeline `Intro` (`Assets/Timeline/IntroTimeline.playable`) hiện bằng bong bóng
world-space `SpeechBubble` (component `ActorSpeechBubble`) trên hai actor double `Player_Actor_Scene1`
(Frawand) và `TruongLang_Actor_Scene1` (Cenhelm). Chỉ scene `IntroCutscene` dùng bong bóng này
(MapNhat không có), nên quy chuẩn kích thước dưới đây là quy chuẩn duy nhất.

- **Quy đổi tham chiếu**: camera ortho size 8 ở 1920×1080 ⇒ **67,5 px màn hình / 1 world unit**. Bong
  bóng vẽ sprite `dialogbox_2` (507×235 px) ở tỉ lệ **1 texel = 1 px màn hình** ⇒ rộng cố định
  507 px = 7,511 unit; đây cũng là độ rộng dòng tối đa.
- **Chữ**: Digital Disco (`DigitalDisco SDF v3`), speaker và body đều **32 px** (0,474 unit) — font vẽ trên
  lưới 16 px/em nên 32 px = đúng 2 px màn hình cho mỗi pixel font, nét sắc. Trước đây body 0,26 unit
  ≈ 17,5 px (≈1,1 px/pixel font, nhòe), speaker 0,3 unit ≈ 20 px; body hiện to **≈1,8×**. Màu chữ giữ nguyên.
- **Layout (px tham chiếu)**: lề ngang 30; speaker cao 36, cách mép trên 22; body giãn giữa
  top 62 và bottom 63 (vùng đuôi + lề dưới).
- **Tự co giãn theo chữ**: `ActorSpeechBubble.FitHeightToText` đo chiều cao body đã wrap
  (`GetPreferredValues` ở độ rộng body) rồi đặt chiều cao `_root` = phần chrome cố định + body, tối thiểu
  `_minHeight` = 144 px (2,133 unit, bằng tổng slice trên + dưới). Sprite 9-slice border
  L/B/R/T = 52/92/52/52 px: chỉ dải thẳng giữa giãn dọc, góc bo và đuôi giữ nguyên art. Chữ không bao giờ bị
  cắt; 1 dòng ≈ 160 px, 2 dòng 193 px, 3 dòng 227 px.
- **Đuôi cố định**: pivot `_root` = (0.5, 0) ở mép dưới, nên bong bóng lớn lên phía trên còn đầu đuôi
  đứng yên. Khi đổi kích thước, vị trí đã được tính lại để đầu đuôi giữ đúng điểm cũ trên đầu actor
  (vẫn tuân thủ `CutsceneTimeline.md` cạm bẫy #8: không cộng offset runtime).
- **Đã kiểm tra trong Play Mode (2026-11-27, render camera 1920×1080)**: mọi dòng đã duyệt ≤3 dòng, không
  overflow; dòng dài nhất *"This is our Farm Area. On the left we raise livestock, and on the right we grow
  our crops."* = 3 dòng, bong bóng ở viewport x 0,48–0,74; bong bóng Frawand ở x 0,20–0,55. Không có lỗi Console.
- Thêm thoại mới dài hơn ~3 dòng ở 447 px vẫn hiển thị đủ (bong bóng cao lên), nhưng cần kiểm tra lại còn
  nằm trong màn hình.

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
- Trong 2 giây cuối của `OutroTransition`, overlay đen fade dần trên chính video. Khi clip kết thúc,
  màn hình giữ đen 0,15 giây rồi controller phát `Completed` để Gameplay Timeline bắt đầu. Fade che
  frame chuyển giao và ngăn flash frame world.
- `MusicManager` bị suppress từ lúc intro bắt đầu; track nền của gameplay chỉ bắt đầu khi Outro hoàn
  tất và controller bàn giao về gameplay (`Finish`).
- `EventSystem` dùng instance persistent do `GameBootstrap` sở hữu. Scene `IntroCutscene` không giữ
  một `EventSystem` cục bộ để tránh hai hệ input cùng xử lý hover/click ở runtime.
- `GameplayTimelineController` trong scene phải giữ hoặc tự khôi phục reference tới
  `IntroCutsceneController`; sự kiện `Completed` bàn giao sang Timeline sáu scene trước khi Timeline
  kết thúc và load `MapNhat`. Fallback chỉ dùng khi scene thật sự không có Gameplay Timeline.
- Gameplay Timeline có button pixel-art `SKIP SCENE` kích thước 300×120 ở góc phải dưới. Button mặc định ẩn,
  chỉ bật trong lúc Timeline chạy và gọi `SkipToNextScene()` để nhảy tới camera cut kế tiếp.

## Authoring và kiểm tra

- Chạy **Tools > Project Game 2D > Cinematics > Create Or Update Intro Cutscene** để đồng bộ definition,
  Timeline và prefab sau khi thay clip hoặc thoại.
- Mở `DemoScene`, chạy **Install Intro Cutscene In Active Scene**, sau đó lưu scene. `DemoScene` là nơi
  integration chính thức; không author trực tiếp vào `MapNhat`.
- Kiểm tra đủ bảy VideoClip, thứ tự Timeline, nút UI, phát NewGame/Development, Continue không phát,
  skip không làm thay đổi save/progression và không có error Console.
