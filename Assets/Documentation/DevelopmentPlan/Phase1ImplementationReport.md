# Phase 1 Implementation Report

Ngày bắt đầu: 2026-08-22  
Trạng thái: **In progress**

## Scope đã triển khai

- Migration `GameState.Menu` → `GameState.GameplayMenu`.
- Migration `GameMenuPage` → `GameplayMenuPage`; bổ sung page `Shop` theo target model.
- Thêm `GameState.MainMenu` và policy khóa gameplay input.
- `GameStateManager` không còn tự vào `Playing` trong `Awake`.
- Thêm scene-level `GameBootstrap` với hai mode explicit:
  - `MainMenu`.
  - `DevelopmentGameplay`.
- Thêm `GameSessionManager` giữ session kind, slot 1–3 và gameplay scene target.
- Thêm `SceneFlowService` làm owner duy nhất của scene load foundation.
- Tạo `Assets/Scenes/MainMenu.unity` bằng Unity MCP.
- Build Settings: `MainMenu` index 0, `DemoScene` index 1.
- Migration Input System `Player` → `Gameplay` giữ nguyên map/action ID; thêm `Inventory` với binding I
  và gamepad Select.
- Thêm scene-level `GameInputCoordinator` quản lý `PlayerInput` theo `GameStatePolicy`, thay polling Esc/I
  bằng `Gameplay/Inventory` và `UI/Cancel`.
- EventSystem của MainMenu và DemoScene cùng dùng project `UI` action map, không còn dùng package
  `DefaultInputActions` song song.
- Thêm application `SettingsService` sở hữu PlayerPrefs, preview/save/restore và áp SFX/Music/fullscreen.
- `SettingsUI` chỉ còn presentation; không đọc PlayerPrefs hoặc gọi trực tiếp audio manager/Screen API.
- `SoundFXManager` không còn giữ gameplay Slider/listener; hai audio manager nhận volume từ SettingsService.
- Pause Menu `MainMenuBtn` gọi `SceneFlowService.TryReturnToMainMenu`; UI không gọi SceneManager trực tiếp.
- Thêm `GameplaySceneLifetime` trên DemoScene `_SceneContext` để đưa các gameplay root từng gọi
  `DontDestroyOnLoad` trở lại gameplay scene trước khi load MainMenu bằng `LoadSceneMode.Single`.
- Thêm `IGameplayReadinessSource` (extension point), `SceneDependencyReadinessSource` (Phase 1
  implementation kiểm tra scene dependency tồn tại) và `GameplayReadinessGate` (restore-ready owner thật
  trên `_SceneContext`) để thay thế API `CompleteGameplayRestore()` treo không có caller.
- Thêm `SceneFlowService.FailGameplayRestore(reason)` để Gate có đường trả game về MainMenu có diagnostic
  khi timeout hoặc thiếu dependency, tái dùng đúng cơ chế teardown/scene-load đã có.
- Wiring Unity: `DemoScene/_SceneContext` có `SceneDependencyReadinessSource` (yêu cầu `Player` và
  `MapManager`) và `GameplayReadinessGate` (đăng ký source đó, timeout 10s).
- Thêm `Assets/Scripts/ProjectGame2D.Runtime.asmdef` (bọc toàn bộ `Assets/Scripts`, không di chuyển file)
  và `Assets/Scripts/Tests/PlayMode/ProjectGame2D.Tests.PlayMode.asmdef` để có thể chạy PlayMode
  `[UnityTest]` — `UnityEngine.TestTools` không tự động có sẵn cho assembly ngầm định, và bọc asmdef là
  cách duy nhất kiểm chứng được hành vi bất đồng bộ của readiness gate bằng Unity Test Framework.

## Contract hiện tại

### Startup

```text
Runtime services → GameState.Booting
MainMenu/GameBootstrap → GameState.MainMenu
```

### Editor direct-play

```text
Runtime services → GameState.Booting
DemoScene/GameBootstrap → Development session → GameState.Playing
```

### Gameplay scene load

```text
Active session required
→ SceneFlowService.TryLoadGameplay(scene target)
→ GameState.Loading
→ load scene
→ giữ Loading
→ GameplayReadinessGate chờ mọi IGameplayReadinessSource sẵn sàng
→ Gate gọi CompleteGameplayRestore() đúng một lần
→ GameState.Playing
```

`GameBootstrap` của DemoScene chỉ tạo development session khi state vẫn là `Booting`; nó không bỏ qua
restore khi scene được load từ `Loading`. `GameplayReadinessGate` chỉ hoạt động khi state đang là
`Loading` với active session lúc `Start()`; Editor Direct-Play đã vào `Playing` từ `GameBootstrap` trước
đó nên Gate bỏ qua, không can thiệp.

### Failure path

Scene load thất bại:

- Dừng transition.
- Clear active session.
- Trở về `GameState.MainMenu`.
- Phát `TransitionFailed` và ghi lỗi có scene target.

Restore-ready timeout hoặc thiếu dependency bắt buộc (Gate):

- `GameplayReadinessGate` ghi lỗi liệt kê source chưa sẵn sàng.
- Gọi `SceneFlowService.FailGameplayRestore(reason)`.
- Clear active session, teardown gameplay roots qua `GameplaySceneLifetime`, load lại MainMenu.
- Phát `TransitionFailed` với message chứa lý do.
- Không vào `Playing` nếu chưa đủ điều kiện readiness.

## Verification record

- Script validation cho state/bootstrap/session/scene flow: 0 diagnostics.
- Editor compile: 0 Error/0 Warning sau migration.
- DemoScene direct-play:
  - state `Playing`;
  - session kind `Development`;
  - gameplay target `DemoScene`;
  - `SceneFlowService` tồn tại;
  - Console sạch.
- Overlay stack regression: `Paused → GameplayMenu(Settings) → Paused`, `Time.timeScale = 0`.
- MainMenu direct-play:
  - state `MainMenu`;
  - gameplay input `false`;
  - active session `false`;
  - Player count `0`;
  - project `Gameplay` map disabled, project `UI` map enabled;
  - Console sạch với `DigitalDisco SDF v3`.
- Input migration Play Mode verification:
  - DemoScene khởi động với PlayerInput active và current map `Gameplay`;
  - mô phỏng phím I: `Playing → GameplayMenu(Inventory)` và PlayerInput bị deactivate;
  - mô phỏng Esc: Inventory → Playing; Playing → Paused → Playing;
  - UI Cancel vẫn enabled trong overlay; Console 0 Error/0 Warning;
  - MainMenu có 0 PlayerInput và project `Gameplay` map disabled;
  - MainMenu và DemoScene validator đều 0 issue.
- Settings Play Mode verification:
  - preview SFX 0,21/Music 0,63 áp ngay lên hai AudioSource;
  - Decline hoàn nguyên snapshot và trở về Playing;
  - Save SFX 0,37/Music 0,74 rồi recreate service đọc lại đúng và áp đúng AudioSource;
  - dữ liệu PlayerPrefs trước test được phục hồi sau verification;
  - chuyển DemoScene → MainMenu giữ đúng preview values với đúng một SettingsService;
  - MainMenu không chứa SettingsUI gameplay; service không giữ scene UI reference;
  - bốn script settings/audio validation 0 diagnostics, DemoScene validator 0 issue, Console sạch.
- Return Main Menu Play Mode verification:
  - click callback thật từ trạng thái Paused: `Paused → Loading → MainMenu`;
  - active development session được clear ngay khi bắt đầu transition;
  - MainMenu sau load có 0 PlayerInput/PlayerStat, InventoryManager, EquipmentManager, MapManager,
    SoundFXManager, PauseMenuUI và SettingsUI;
  - MusicManager và SettingsService application scope còn đúng một instance;
  - vòng MainMenu → DemoScene → MainMenu lần hai hoàn tất, gameplay binding được tạo lại và leak audit
    sau lần hai vẫn sạch;
  - ba script liên quan validation 0 diagnostics, DemoScene validator 0 issue, Console sạch;
  - Content Validation: 0 error, 60 warning legacy-ID đã được chấp nhận ở baseline, 63 asset được kiểm tra.
- Missing-scene failure injection:
  - transition kết thúc;
  - session được clear;
  - state trở về `MainMenu`;
  - error message chứa scene target.
- Unity scene validator cho MainMenu: 0 missing script, 0 broken prefab, 0 issue.
- Windows x64 Development Build đầu tiên tạo thành công trong 84,32 giây nhưng TMP pre-build phát
  `UnassignedReferenceException` do `DigitalDisco SDF v3` mất atlas/material.
- `DigitalDisco SDF v3` sau đó đã được regenerate từ `DigitalDisco.ttf`, giữ nguyên GUID và có material
  cùng atlas sub-assets hợp lệ. TMP default, MainMenu, DemoScene và hai Inventory prefab hiện đều trỏ
  DigitalDisco theo [Typography Standard](Typography.md).
- Windows x64 Development Build sau repair: succeeded trong 43,22 giây, 581,89 MB,
  **0 Error/0 Warning**; scene order là MainMenu index 0 và DemoScene index 1.

## Restore-ready transition — verification record (2026-08-22)

- Script validation: 0 diagnostics cho `IGameplayReadinessSource`, `SceneDependencyReadinessSource`,
  `GameplayReadinessGate`, `SceneFlowService.FailGameplayRestore`.
- Editor compile sau khi thêm `ProjectGame2D.Runtime.asmdef` và
  `ProjectGame2D.Tests.PlayMode.asmdef`: 0 Error/0 Warning.
- PlayMode automated tests (`GameplayReadinessGatePlayModeTests`, Unity Test Framework): **4/4 PASS**
  (0.59s) —
  - `NotReadySource_DoesNotTransitionToPlaying`: state ở lại `Loading` khi source chưa sẵn sàng.
  - `SourceBecomesReady_CompletesRestoreExactlyOnce`: đúng một lần chuyển sang `Playing` dù
    `ReadyChanged` có thể bắn lại.
  - `DirectPlay_AlreadyPlaying_GateTakesNoAction`: Gate không can thiệp khi state đã là `Playing`
    trước khi nó chạy (mô phỏng Editor Direct-Play).
  - `TimeoutWithoutReadySource_FailsRestoreAndReturnsToMainMenu`: hết timeout gọi
    `FailGameplayRestore`, có log lỗi liệt kê source, quay về `MainMenu`, session được clear.
- Content Validation (`Tools/Project Game/Validate Content`): 0 error, 60 warning (legacy-ID baseline
  không đổi), 63 asset checked.
- DemoScene Direct-Play thủ công qua Unity Editor (Play Mode thật, không phải test):
  `GameState=Playing`, `Session=Development`, `GameplayScene=DemoScene`, Gate tồn tại nhưng không hành
  động; Console sạch.
- Kịch bản MainMenu → gameplay thật (mô phỏng bằng cách gọi trực tiếp
  `GameSessionManager.TryStartLoadedGame` + `SceneFlowService.TryLoadGameplay` từ MainMenu, vì Phase 2/3
  chưa có New Game/Continue UI để bấm):
  - Gọi xong: state chuyển `Loading` ngay lập tức.
  - Scene `DemoScene` load xong nhưng state **giữ nguyên `Loading`** cho tới khi Gate xác nhận
    `Player`/`MapManager` sẵn sàng — chứng minh acceptance criteria "không chuyển Playing chỉ vì
    callback scene load vừa chạy".
  - Sau khi Gate hoàn tất: state chuyển `Playing` đúng một lần, `SceneFlowService.IsTransitioning=false`.
  - Lặp lại vòng thứ hai với slot khác: kết quả giống hệt, không duplicate `GameStateManager`/
    `GameSessionManager`/`SceneFlowService` (mỗi loại đúng 1 instance).
  - `TryReturnToMainMenu` sau mỗi vòng: về `MainMenu`, `HasActiveSession=false`, `PlayerCount=0`,
    `GateCount=0` (Gate là scene-local, bị unload cùng DemoScene, không leak).
  - Console sạch trong toàn bộ kịch bản hai vòng.
- Windows x64 Development Build (bản cho session này): succeeded trong 27,64 giây, 581,89 MB,
  **0 Error/0 Warning**. Output: `Builds/Phase1RestoreReady/ProjectGame2D.exe` (thư mục build, đã
  `.gitignore`).
- Runtime executable launch độc lập Editor: khởi chạy qua CLI (`-logFile`), process
  `ProjectGame2D` sống và `Responding=True` trong suốt smoke window (~20 giây quan sát thêm sau khi log
  ổn định), sau đó dừng process chủ động (`Stop-Process`).
  - `Player.log`: không có NullReference/MissingReference/UnassignedReference/exception/assert/crash.
  - Log chỉ có initialization message chuẩn (Input System, Physics, D3D12 device info) và một
    `Curl error 35` từ development player connection — cùng loại artifact non-gameplay đã ghi nhận ở
    Phase 0 baseline, không phải lỗi runtime.
  - **Giới hạn đã biết:** không xác nhận được bằng mắt rằng màn hình hiển thị đúng MainMenu, vì exe build
    này không phải app đã cài đặt nên công cụ computer-use không thể cấp quyền chụp màn hình cửa sổ đó.
    Bằng chứng hiện có chỉ ở mức process/log; xác nhận trực quan MainMenu trên Player build cần chạy thủ
    công bởi người dùng hoặc một cơ chế screenshot khác.

## Chưa hoàn thành trong Phase 1

- MainMenu navigation/controller; New Game/Continue slot UX thuộc Phase 2–3.
- Xác nhận trực quan (screenshot/quan sát người dùng) rằng Player build khởi động đúng màn hình MainMenu;
  hiện chỉ có bằng chứng process/log sạch, chưa có xác nhận hình ảnh độc lập Editor.
- Manual gamepad verification cho gameplay và UI Navigate/Submit/Cancel — xem mục kiểm kê gamepad bên dưới.

Restore-ready signal thật (mục tiêu chính của session 2026-08-22) đã triển khai và kiểm chứng bằng
automated PlayMode test và Play Mode thủ công như trên. Đây vẫn là readiness barrier ở mức Phase 1 —
chỉ xác nhận scene runtime dependency sẵn sàng, chưa phải full save/world restoration của Phase 2/3.

## Gamepad — kiểm kê thiết bị (2026-08-22)

Session này không có gamepad vật lý được thao tác. Không thể xác nhận Input System có nhận diện thiết bị
gamepad thật hay không trong lần kiểm tra này. Checklist manual gamepad (MainMenu/Loading/Gameplay/
Settings theo mục 12 của brief) chưa được thực hiện.

**Trạng thái: `BLOCKED – requires physical user input`.** Không ghi PASS.

Không đánh dấu Phase 1 hoàn tất cho đến khi các acceptance criteria tương ứng trong Roadmap được kiểm chứng.
