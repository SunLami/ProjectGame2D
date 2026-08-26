# Runtime Architecture and Scene Flow

## Scene boundary

### MainMenu scene

Chỉ chứa presentation và controller cho:

- New Game.
- Continue.
- Chọn/xóa/overwrite ba save slot.
- Main Menu Settings.
- Quit Desktop.

Không chứa Player, Enemy, WorldManager hoặc gameplay inventory UI. `MainMenuController` yêu cầu
application services thực hiện hành động; nó không đọc/ghi file trực tiếp.

### DemoScene — integration playground

`DemoScene` là nơi dựng và test tích hợp player, NPC, enemy, gameplay camera và gameplay UI overlays:

- Pause.
- Inventory/Equipment/Character.
- Quest Log.
- Crafting/Shop.
- Gameplay Settings.
- Dialogue và notifications.

DemoScene không phải world scene production. Feature phải được đóng gói thành prefab/config/installer
để kéo sang scene khác. Xem [DemoScene Workflow](DemoSceneWorkflow.md).

### Production world scenes — chưa chốt topology

Về sau có thể có một hoặc nhiều world scene. Kế hoạch không ép tên `GameScene`. Mọi save position
phải lưu stable `sceneId`/`areaId` để cùng SaveManager phục vụ DemoScene khi test và world scene khi
production mà không đổi data contract.

## Service boundary

| Service | Sở hữu | Không được sở hữu |
|---|---|---|
| `GameStateManager` | Runtime mode, policy, overlay history | File I/O, quest, world object |
| `SceneFlowService` | Load/unload MainMenu và gameplay scene được session/config chọn | Save parsing, spawn rules |
| `GameSessionManager` | Active slot, session kind, dirty/play time | Serialize domain data |
| `SaveManager` | Orchestrate capture/write/read/restore | Gameplay UI navigation |
| `SaveSlotRepository` | File and metadata operations | Unity scene object references |
| `WorldManager` | Area, world registry, world restore | Menu state, disk writes |
| `SpawnRegistry` | Resolve stable spawn IDs | Decide new/load session |
| Domain `Catalog/Resolver` | Resolve immutable definitions theo stable ID | Runtime progress, file I/O |
| `TutorialManager` | Tutorial step progression | Main Quest logic |
| `QuestManager` | Quest availability/progress/reward | Direct shop/crafting mutation |

## Target GameState model

```csharp
public enum GameState
{
    Booting,
    MainMenu,
    Loading,
    Playing,
    Paused,
    GameplayMenu,
    Dialogue,
    Cutscene,
    Saving,
    PlayerDead
}
```

`GameplayMenu` chỉ tồn tại trong DemoScene/world gameplay scene. Main Menu scene dùng `MainMenu`, không dùng
`GameplayMenu(Settings)`.

Target page model:

```csharp
public enum GameplayMenuPage
{
    None,
    Inventory,
    Equipment,
    Character,
    Crafting,
    Shop,
    QuestLog,
    Map,
    Settings
}
```

Save-slot selection ở MainMenu là navigation nội bộ của `MainMenuController`, không phải
`GameplayMenuPage`.

## Application lifecycle

### Startup

```text
Process start
→ GameBootstrap creates persistent application services
→ GameState.Booting
→ load/validate settings and save metadata
→ load MainMenu scene if necessary
→ GameState.MainMenu
```

Không chuyển `Playing` trong `GameStateManager.Awake`. Chỉ flow coordinator được phép kết thúc
Loading bằng `ResetToPlaying` sau khi restore thành công.

### Editor direct-play trong DemoScene

Để iteration nhanh, khi developer mở DemoScene và bấm Play, `DemoSceneInstaller` có thể tạo một
development session từ test profile:

```text
Editor direct play in DemoScene
→ GameState.Booting
→ detect explicit Demo development context
→ create in-memory/test GameSession
→ initialize scene feature roots
→ optionally restore selected fixture
→ GameState.Playing
```

Đường tắt này chỉ dành cho Editor/development configuration. Player build vẫn bắt đầu MainMenu;
runtime production không suy luận “scene đang mở” để bỏ qua slot selection. Test profile không được
ghi đè ba save slot thật trừ khi developer chủ động chọn chế độ test file I/O.

### New Game

```text
MainMenu: choose empty slot
→ validate/confirm slot
→ create default snapshot in memory
→ establish GameSession(NewGame, slot)
→ GameState.Loading
→ load configured gameplay scene (DemoScene trong development)
→ register world/spawn/services
→ restore default player/inventory/tutorial data
→ resolve tutorial_start
→ position player and bind camera
→ optionally write initial save
→ GameState.Playing
```

### Continue

```text
MainMenu: choose valid slot
→ read and validate save
→ migrate if supported
→ establish GameSession(LoadedGame, slot)
→ GameState.Loading
→ resolve saved/configured gameplay scene
→ load scene đó
→ register world objects
→ restore domains in deterministic order
→ resolve saved area/position fallback
→ bind camera/UI
→ GameState.Playing
```

**Phase 3 implementation note (2026-08-22):** `MainMenuController` (non-visual, `_SceneContext` trong
`MainMenu.unity`) implement New Game/Continue bằng `NewGameFactory` + `GameSessionManager.SaveRepository`
(`ISaveSlotRepository`, mặc định `FileSaveSlotRepository`) + `SceneFlowService.TryLoadGameplay`.
`PlayerSpawnReadinessSource` (một `IGameplayReadinessSource` cắm vào `GameplayReadinessGate` có sẵn từ
Phase 1) restore `PlayerStat` progression và vị trí qua `SpawnRegistry`, rồi ghi initial save đúng một
lần cho New Game (D-011). Chưa có: inventory/tutorial restore, camera bind riêng (camera hiện tại đã
theo Player sẵn), migrate save version cũ (chưa có save cũ nào tồn tại). Camera/UI bind và
inventory/tutorial restore là extension point còn lại cho Phase 4/5 cắm thêm
`IGameplayReadinessSource` vào cùng Gate.

### Return to Main Menu

**Phase 9 implementation note (2026-08-23):** `GameplaySessionController.RequestReturnToMainMenu()`
đi thẳng vào Loading nếu `GameSessionManager.IsDirty == false`; nếu dirty, fire
`OnConfirmationRequired(ReturnToMainMenu)` và **không** đổi GameState (popup xác nhận là UI
navigation con, không phải confirm-then-auto-Loading). UI gọi `ConfirmSaveAndReturn()` (chỉ
Loading sau khi ghi save thành công), `ConfirmReturnWithoutSaving()` (Loading ngay) hoặc
`CancelReturnToMainMenu()` (không đổi gì). Không tự động ghi đè save trước khi người chơi xác nhận,
đúng D-017.

```text
Paused
→ confirm save/leave choice (chỉ hiện nếu session dirty; session clean bỏ qua bước này)
→ optional Saving (chỉ khi chọn Save and Return)
→ GameState.Loading
→ close gameplay overlays and clear state history
→ unload active gameplay scene
→ clear current session and domain runtime caches
→ load MainMenu
→ refresh slot metadata
→ GameState.MainMenu
```

`SceneFlowService` là owner của scene load. DemoScene đăng ký các root gameplay từng gọi
`DontDestroyOnLoad` trong `GameplaySceneLifetime`; trước `LoadSceneMode.Single`, component đưa các root
này trở lại gameplay scene để Unity unload chúng cùng scene. Application services như GameState,
GameSession, SceneFlow, Settings và Music không thuộc danh sách teardown.

`GameplayReadinessGate` trên `_SceneContext` của gameplay scene là restore-ready owner: nó chờ mọi
`IGameplayReadinessSource` đăng ký (ví dụ `SceneDependencyReadinessSource` ở Phase 1) báo sẵn sàng, có
timeout với diagnostic rõ, rồi mới gọi `SceneFlowService.CompleteGameplayRestore()` đúng một lần. Nếu
timeout hoặc thiếu dependency, Gate gọi `SceneFlowService.FailGameplayRestore()` để dọn session và quay
về MainMenu thay vì để game kẹt ở `Loading`. Đây là barrier có khả năng mở rộng: Phase 2/3 thêm
`IGameplayReadinessSource` cho save/world/quest/inventory restore mà không sửa Gate hay SceneFlowService.
Editor Direct-Play (`GameBootstrapMode.DevelopmentGameplay`) đã vào `Playing` trước khi Gate chạy nên Gate
không can thiệp vào luồng đó.

Khi scene activation hoàn tất, `Start` của readiness source/gate có thể chạy trước frame mà coroutine
`SceneFlowService.TrackSceneLoad` kịp hạ cờ transition. `CompleteGameplayRestore()` vì vậy chấp nhận
readiness trong cửa sổ activation này, kết thúc transition và vào `Playing` theo state/session guard;
không được từ chối chỉ vì `IsTransitioning` vẫn còn `true` trong cùng frame.

## Restore order

Thứ tự chuẩn tránh event chạy trên dữ liệu nửa hoàn chỉnh:

1. Validate and migrate DTO.
2. Create active session context.
3. Load selected gameplay scene and wait scene-ready signal.
4. Disable gameplay/event emission during restore.
5. Build item/quest/persistent-object registries.
6. Restore base player progression.
7. Restore inventory, equipment, then recalculate stats.
8. Restore world persistent state.
9. Restore tutorial and quests without granting rewards.
10. Resolve area/spawn position and camera target.
11. Refresh UI once from final domain state.
12. Enable events/input and enter `Playing`.

## Event rules

- Gameplay domains phát typed events sau transaction thành công.
- Trong restore, dùng `RestoreContext.IsRestoring` hoặc API restore riêng để không phát reward event.
- UI subscribe read models/events; UI không sửa DTO trực tiếp.
- Không dùng string event name cho quest objectives.
- Unsubscribe ở lifecycle đối xứng để tránh duplicate objective progression.

## Data ownership rules

- ScriptableObject Definition là immutable runtime input.
- Domain service sở hữu Runtime State.
- SaveManager chỉ phối hợp DTO capture/restore, không sở hữu Definition.
- Catalog/Resolver resolve stable ID; UI không tự load asset.
- Scene installer bind prefab instance với definition/catalog cần thiết.
- Cross-domain progression đi qua typed event/transaction result, không qua UI click hay specific-ID branch.

Chi tiết authoring, handler registry và migration nằm tại
[Data-Driven Development Guide](DataDrivenDevelopment.md).

## Failure rules

- New Game creation fail: ở lại MainMenu, slot không được đánh dấu hợp lệ.
- Continue read/migration fail: ở lại MainMenu, hiển thị recovery options.
- Gameplay scene load fail: clear partial session, trở về MainMenu.
- Restore thiếu optional content: báo recovery warnings; không crash nếu có fallback an toàn.
- Restore mất required player/area data: không vào Playing.

## Migration từ code hiện tại

1. Rename state/page và toàn bộ call site.
2. Thêm `MainMenu` policy.
3. Bỏ `ReplaceState(Playing)` khỏi manager `Awake`.
4. Thêm bootstrap/scene flow trước khi tạo MainMenu UI mới.
5. Giữ stack overlay hiện tại cho Pause/Inventory/Settings.
6. Chỉ tích hợp SaveManager khi Phase 2 repository tests đã pass.

## Scene composition và portability

Mỗi gameplay scene có một scene composition root/installer chịu trách nhiệm bind:

- Tilemap/ground query.
- Player spawn registry.
- World persistent registry.
- Gameplay camera.
- Scene-specific NPC/enemy/resource instances.
- Gameplay UI root nếu UI không được application bootstrap tạo.

Application service tồn tại xuyên scene không được serialize reference trực tiếp tới object thuộc
DemoScene. Khi unload scene, scene context phải unregister; khi load scene mới, installer bind context
mới. Feature prefab cần báo lỗi authoring rõ nếu thiếu dependency thay vì âm thầm tìm object theo tên.
