# Cutscene Timeline Architecture

> **Trạng thái tài liệu:** ghi lại kiến trúc Timeline đã triển khai và kiểm chứng cho cutscene mở đầu
> Scene 1 "Forest Bridge" (`Assets/Scenes/IntroCutscene.unity`, `Assets/Timeline/IntroTimeline.playable`)
> tính đến 2026-09-12. Dùng tài liệu này làm **base flow** khi dựng Timeline cho các cảnh cutscene tiếp
> theo — mục tiêu là tái sử dụng đúng track/script đã có, không viết lại từ đầu, và tránh lặp lại các
> bug đã từng gặp (liệt kê ở phần "Cạm bẫy đã gặp").

## Mục tiêu

Dựng cutscene 2D bằng Unity Timeline với:

- Actor đi vào/ra cảnh theo đường thẳng trục X/Y (không đi chéo), đúng sprite theo hướng đi.
- Camera cắt cảnh qua Cinemachine.
- Thoại hiện trong bong bóng world-space phía trên đầu actor đang nói, dừng chờ input người chơi,
  không dùng `DialogueUI_v2`.
- Ẩn HUD gameplay và Player/NPC thật trong lúc cutscene chạy, khôi phục khi xong.
- Tự động chuyển sang scene gameplay tiếp theo khi Timeline kết thúc.

## Sơ đồ thành phần

```text
Scene "IntroCutscene"
└── Intro (GameObject)
    ├── PlayableDirector          -> playableAsset = IntroTimeline.playable
    └── GameplayTimelineController -> orchestration, hide/restore HUD & actor doubles

IntroTimeline.playable (TimelineAsset)
├── Camera - Scene1        [CinemachineTrack]      bound -> CinemachineBrain (Main Camera)
├── <Actor> - Walk Sprite  [AnimationTrack] x N     bound -> Animator (actor double)
├── Fade To/From Black     [AnimationTrack]         bound -> CanvasGroup (GameplayTimelineFade)
├── <Actor> Dialogue       [ActorDialogueTrack] x N bound -> Transform (actor double)

Actor double GameObject (vd Player_Actor_Scene1)
├── Animator, SortingGroup, SpriteRenderer   -> hiển thị + sprite-swap animation
├── TimelineActorMotion [ExecuteAlways]      -> di chuyển theo director.time
├── ActorSpeechBubble                        -> bong bóng thoại world-space
```

Mỗi actor nói chuyện trong cảnh là một **actor double** (bản sao chỉ để hiển thị, KHÔNG phải
Player/NPC thật) đặt trong chính scene cutscene, dưới một `*_Anchor` cố định (điểm spawn/tổ chức
Hierarchy, actor double di chuyển tương đối so với Anchor này qua `TimelineActorMotion`).

## Quy trình dựng một cảnh cutscene mới (theo base Scene 1)

1. **Tạo actor double** cho mỗi nhân vật xuất hiện: copy từ actor double đã có (vd
   `Player_Actor_Scene1`) sang `Player_Actor_SceneN`, đặt dưới một `*_Anchor` riêng ở vị trí spawn của
   cảnh mới.
   - **Chỉ giữ các component hiển thị thuần túy**: `Animator`, `SortingGroup`/`SpriteRenderer`,
     `TimelineActorMotion`, `ActorSpeechBubble`. **Không copy** `Player`, `PlayerStat`,
     `PlayerAnimationEvent`, `Rigidbody2D` hay bất kỳ component gameplay logic nào khác — xem
     "Cạm bẫy đã gặp #1".
2. **Tạo `IntroTimeline.playable` mới** (hoặc TimelineAsset riêng cho cảnh) gắn vào `PlayableDirector`
   của GameObject điều phối cảnh (tương đương `Intro`).
3. **Camera**: thêm `CinemachineTrack`, tạo `CinemachineShot` clip trỏ tới một `CinemachineCamera`
   dựng riêng cho góc quay cảnh này. **Bind track vào `CinemachineBrain` của Main Camera**
   (`director.SetGenericBinding(cameraTrack, brain)`), không bind vào camera — nếu quên bước này,
   cắt cảnh sẽ không có tác dụng (xem "Cạm bẫy #6").
4. **Animation Track sprite theo actor**: một `AnimationTrack` mỗi actor, bind vào `Animator` của actor
   double đó. Xếp các clip Walk/Idle theo hướng di chuyển thật (không dùng clip "đi chéo"); mỗi đoạn rẽ
   hướng là một clip Walk khác nhau (WalkRight/WalkUp/WalkLeft/WalkDown). Tất cả clip để
   `pre = Hold`, `post = Hold` (giữ đúng pose ở đầu/cuối clip, tránh actor "giật" về pose sai khi
   Timeline dừng ở biên clip).
5. **Di chuyển actor**: dùng `TimelineActorMotion` (component, không phải Animation Track gốc — xem
   "Cạm bẫy #4"), điền mảng `PositionKey[] { time, localPosition }` khớp đúng thời điểm bắt đầu/kết
   thúc mỗi đoạn Walk ở bước 4. `localPosition` là tọa độ tương đối so với `*_Anchor` (actor luôn bắt
   đầu ở `(0,0)`).
6. **Fade**: một `AnimationTrack` bind vào `CanvasGroup` của `GameplayTimelineFade` (Canvas dùng
   chung, đã có sẵn trong `_UI` của scene), animate `alpha` 0↔1 ở đầu/cuối Timeline.
7. **Thoại**: một `ActorDialogueTrack` mỗi actor có thoại, bind vào `Transform` của actor double đó
   (không phải Animator). Mỗi dòng thoại là một `ActorDialogueClip` với `SpeakerName`, `Text`,
   `FlipHorizontal`. `FlipHorizontal = true` khi actor đang đứng **bên phải** người nghe,
   `false` khi đứng bên trái — xem "Quy tắc Flip" bên dưới.
8. **Bong bóng thoại**: mỗi actor double cần một `SpeechBubble` (Canvas world-space con của chính actor
   double, không phải của `*_Anchor` — xem "Cạm bẫy #7") gắn với component `ActorSpeechBubble`.
   Căn vị trí bằng cách **tự tay bật `_root` GameObject lên trong Editor, kéo tới vị trí đẹp** (đuôi
   bong bóng chạm đúng đầu actor), rồi tắt lại — vị trí đó được dùng trực tiếp, không có phép cộng nào
   khác ở runtime (xem "Cạm bẫy #8").
9. **Wire `GameplayTimelineController`**: gán `_actorDoublesToHide` (Player/NPC **thật**, không phải
   actor double) để ẩn trong lúc chạy Timeline; `_hudRootName = "GameplayUIRoot"`; `_nextSceneName`
   là scene gameplay kế tiếp.
10. **Test bằng Play mode thật** theo checklist ở cuối tài liệu — không chỉ preview scrub trong Editor.

## Quy tắc Flip cho bong bóng thoại

Actor đứng **bên trái** đối phương trong không gian world → `FlipHorizontal = false` (dùng art gốc).
Actor đứng **bên phải** đối phương → `FlipHorizontal = true` (mirror art, chỉ mirror phần nền bong
bóng — `_background`, không mirror text). Quy tắc này chỉ đúng khi **actor không đổi bên trong suốt
cảnh** (đúng cho các cảnh 2 nhân vật đối thoại cố định vị trí như Scene 1); nếu cảnh sau có nhân vật
đổi bên giữa các dòng thoại, cần thiết kế lại cơ chế offset (xem "Cạm bẫy #8" trước khi động vào).

## Component tham chiếu

| Script | Vai trò | Đường dẫn |
|---|---|---|
| `GameplayTimelineController` | Orchestration: play khi vào scene, ẩn/khôi phục HUD + actor thật, chuyển scene khi Timeline dừng. | `Assets/Scripts/Cinematics/Gameplay/GameplayTimelineController.cs` |
| `TimelineActorMotion` | Di chuyển actor double theo `director.time`, piecewise-linear giữa các `PositionKey`. `[ExecuteAlways]` để preview đúng khi scrub Timeline trong Editor. | `Assets/Scripts/Cinematics/Gameplay/TimelineActorMotion.cs` |
| `ActorDialogueTrack` / `ActorDialogueClip` / `ActorDialogueBehaviour` / `ActorDialogueMixerBehaviour` | Custom Timeline track hiện bong bóng thoại world-space, dừng `PlayableDirector` chờ input. | `Assets/Scripts/Cinematics/Timeline/` |
| `ActorSpeechBubble` | Component trên actor double, hiện/ẩn bong bóng, typewriter, chờ input để đóng và resume director. | `Assets/Scripts/Cinematics/Timeline/ActorSpeechBubble.cs` |

## Cạm bẫy đã gặp (đọc trước khi sửa hệ thống này)

### 1. Actor double không được mang component gameplay logic
Nếu actor double có `Player`/`PlayerStat`/`Rigidbody2D`, script này sẽ Awake và **chiếm mất singleton**
(`PlayerStat.Instance`) của Player thật (vì Player thật đang `inactive` trong scene cutscene, Awake
sau). Khi cutscene kết thúc, actor double bị hủy → `PlayerStat.Instance` thành tham chiếu treo → HUD
máu/mana/damage hỏng ở gameplay thật sau đó. **Actor double chỉ được có component hiển thị thuần túy.**

### 2. `PlayableDirector.Pause()` hủy và dựng lại toàn bộ PlayableGraph
Xác nhận qua `OnGraphStart`/`OnGraphStop` (tăng đúng lúc mỗi lần dialogue pause/resume): gọi
`director.Pause()` **KHÔNG** chỉ tạm dừng — nó khiến graph bị rebuild ngầm. Hệ quả: mọi state lưu
trong field của một `PlayableBehaviour` (mixer, v.v.) **bị xóa mỗi lần pause/resume**, không chỉ một
lần. `ActorDialogueMixerBehaviour` vì vậy lưu "clip nào đã hiện" trong một `Dictionary<TrackAsset,int>`
**static, khóa theo TrackAsset** (TrackAsset là asset bền vững, không bị rebuild theo graph) thay vì
field instance thường. `GameplayTimelineController.Play()` gọi `ActorDialogueMixerBehaviour.ResetAll()`
đúng một lần khi cutscene thật sự bắt đầu lại từ đầu.

### 3. Không bao giờ gọi `director.Evaluate()` bên trong `ProcessFrame`
`ProcessFrame` được gọi TỪ BÊN TRONG graph evaluation của chính director đó. Gọi `Evaluate()` đồng bộ
ở đây tái nhập (reentrant) vào evaluation đang chạy → **treo cứng Unity Editor** (tái hiện 2 lần trong
quá trình phát triển). Nếu cần force-refresh graph sau khi `Pause()`, phải làm từ một call stack khác
hẳn (vd `MonoBehaviour.Update()` một frame sau), không phải từ trong `ProcessFrame`.

### 4. Animation Track gốc cho vị trí actor không ổn định khi kết hợp với dialogue pause
Đã thử thay `TimelineActorMotion` bằng Animation Track position curve thuần Unity (để preview khớp
Play mode tự nhiên hơn). Preview bằng `Evaluate()` thủ công hoạt động hoàn hảo, nhưng khi chạy Play
thật, graph rebuild ở mục #2 khiến Animation Track chưa kịp "đẩy" giá trị đúng lên Transform sau mỗi
lần resume — actor kẹt ở keyframe đầu tiên (0,0) trong lúc dialogue đang hiện. Đã thử force thêm một
`Evaluate()` trễ một frame (an toàn, không nằm trong `ProcessFrame`) nhưng vẫn không ổn định. **Kết
luận: giữ `TimelineActorMotion` (Update()-driven) cho vị trí actor trong hệ thống dialogue-pause này.**

### 5. `TimelineActorMotion` cần `[ExecuteAlways]` để preview khớp Play mode
Không có `[ExecuteAlways]`, `Update()` chỉ chạy trong Play mode → tua/xem trước Timeline trong Editor
(không bấm Play) sẽ không di chuyển actor, dù sprite animation (do PlayableGraph điều khiển) vẫn xem
trước đúng. Thêm `[ExecuteAlways]` khiến `Update()` cũng chạy khi Editor scrub Timeline, khớp đúng
`director.time` ngay cả ngoài Play mode.

### 6. `CinemachineTrack` phải bind vào `CinemachineBrain`, không phải camera
`director.SetGenericBinding(cameraTrack, brain)` — nếu bind nhầm vào GameObject camera hoặc
`CinemachineCamera`, `Brain.ActiveVirtualCamera` không bao giờ đổi, cắt cảnh không có tác dụng.

### 7. `SpeechBubble` phải là con của actor double, không phải `*_Anchor`
Actor double di chuyển trong lúc cutscene; thoại phát ra khi actor **đã đi tới vị trí giữa cảnh**
(không phải điểm spawn). Nếu `SpeechBubble` là con của `*_Anchor` (điểm cố định), bong bóng sẽ đứng
yên tại spawn thay vì bám theo đầu actor lúc nói.

### 8. Vị trí `SpeechBubble` là giá trị cuối cùng, không cộng offset runtime
`ActorSpeechBubble.Awake()` lưu `_root.transform.localPosition` hiện tại làm vị trí gốc. Việc căn
chỉnh vị trí đúng cách là **bật `_root` lên trong Editor, kéo tới vị trí đẹp bằng mắt, rồi lưu** — giá
trị đó được `Show()` dùng **y nguyên**, không cộng/trừ gì thêm. (Từng có phép cộng offset đuôi bong
bóng theo `FlipHorizontal` chạy ở runtime — gây lệch kép vì giá trị lưu trong Editor đã bao gồm sẵn độ
lệch cần thiết. Đã bỏ phép cộng đó.) Nếu cảnh sau cần một actor đổi bên (flip) giữa các dòng thoại
trong cùng một cảnh, đây là chỗ cần thiết kế lại trước, không tự ý thêm lại offset runtime.

### 9. Actor double không được có `PlayerAnimationEvent`
Hệ quả trực tiếp của mục #1: bỏ `PlayerAnimationEvent` (dùng để phát âm thanh bước chân) khiến
Animation clip `WalkRight`/... log warning "no receiver" cho `AnimationEvent PlayFootSteps`. Vô hại
(không có exception, chỉ là thiếu tiếng bước chân trong cutscene) — chấp nhận đánh đổi này thay vì
kéo `Player` component (và bug #1) trở lại. Nếu cần tiếng bước chân, viết một event-receiver riêng,
nhẹ, không phụ thuộc `Player`.

### 10. `Enter Play Mode Options` của project tắt Domain Reload
Project này bật `DisableDomainReload` (Project Settings > Editor). Nghĩa là **static field không tự
reset giữa các lần bấm Play/Stop** trong cùng phiên Editor (chỉ scene mới reload). Đây là lý do
`ActorDialogueMixerBehaviour.LastShownIndex` phải được dọn thủ công (`ResetAll()`) ở đầu
`GameplayTimelineController.Play()` — bất kỳ static state mới nào thêm vào hệ thống dialogue/cutscene
sau này cũng phải tự chủ động reset đúng lúc cutscene thật sự bắt đầu, không được ỷ lại vào việc Unity
tự dọn.

### 11. Đừng nhóm (Group) track hoặc reparent qua script
`TrackAsset.parent` có setter nhưng là `internal`; gọi qua reflection **không** thực sự thêm track vào
danh sách con của `GroupTrack` (data không lỗi, nhưng cũng không nhóm được gì — no-op). Muốn nhóm
track, làm thủ công trong cửa sổ Timeline (kéo thả track lên nhau, hoặc chuột phải > **Group
Tracks**). Sau khi sửa Timeline nhiều bằng script trong một phiên Editor, cửa sổ Timeline có thể hiện
sai/thiếu track (bug hiển thị của Unity, không phải mất dữ liệu) — đóng tab Timeline và mở lại bằng
cách double-click asset `.playable` hoặc chọn lại GameObject director sẽ khắc phục; nếu không hết,
khởi động lại Editor.

## Checklist test trước khi commit một cutscene mới

1. Play mode thật (không chỉ Editor preview) từ đầu tới cuối, không skip bước nào.
2. Mỗi dòng thoại: đúng người nói, đúng text, bong bóng đúng vị trí trên đầu, đúng hướng flip.
3. Cố tình chờ vài giây ở mỗi điểm dừng thoại trước khi bấm tiếp — xác nhận Timeline **giữ nguyên**
   (không tự trôi tiếp, không tự đóng bong bóng).
4. Actor di chuyển đúng trục, đúng sprite theo hướng, không "giật" pose ở biên clip.
5. Camera cắt đúng lúc, đúng góc.
6. Sau khi Timeline kết thúc: HUD hiện lại, actor/NPC thật hiện lại đúng vị trí, `PlayerStat.Instance`
   trỏ vào Player thật (không phải actor double đã bị hủy), scene chuyển đúng sang gameplay tiếp theo.
7. Console không có warning/error mới phát sinh từ cutscene (trừ warning đã biết và chấp nhận ở mục
   #9 phía trên).
