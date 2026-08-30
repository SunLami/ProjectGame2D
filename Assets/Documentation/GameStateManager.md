# GameStateManager Architecture

> **Trạng thái tài liệu:** migration state/page và bootstrap foundation của Phase 1 đã được triển khai
> ngày 2026-08-22. MainMenu Scene hiện là build index 0; New Game/Continue và save-slot UI thuộc các
> phase sau. Thứ tự triển khai tiếp theo nằm trong
> [Development Roadmap](DevelopmentPlan/Roadmap.md) và
> [Runtime Architecture](DevelopmentPlan/RuntimeArchitecture.md).

## Mục tiêu

`GameStateManager` điều phối **chế độ vận hành toàn game** của sandbox RPG. Nó không quản lý
tiến trình qua màn, dữ liệu save, entity trong thế giới, inventory hoặc state machine của Player/Enemy.

Kiến trúc này giải quyết ba vấn đề:

- Chỉ một nơi quyết định world có pause và gameplay input có được nhận hay không.
- Pause, menu RPG, dialogue, cutscene và load/save không tranh chấp `Time.timeScale`.
- Có thể thêm hệ thống mới bằng state policy và event mà không tạo phụ thuộc trực tiếp giữa các UI.

## Thành phần

### `GameState`

Đại diện cho trạng thái cấp toàn game:

| State | Ý nghĩa |
|---|---|
| `Booting` | Khởi tạo dịch vụ trước khi gameplay sẵn sàng. |
| `MainMenu` | MainMenu Scene; gameplay input bị khóa. |
| `Loading` | Đọc save và khôi phục world snapshot. |
| `Playing` | Gameplay sandbox bình thường. |
| `Paused` | Pause menu đang mở. |
| `GameplayMenu` | Gameplay overlay như Inventory, Equipment hoặc Settings. |
| `Dialogue` | Player đang hội thoại, world vẫn có thể chạy. |
| `Cutscene` | Cutscene đang nắm quyền điều khiển. |
| `Saving` | Đóng băng world trong lúc chụp/ghi snapshot an toàn. |
| `PlayerDead` | Player chết, chờ load save hoặc hồi sinh. |

Không đưa `EnemyState`, `PlayerState` hoặc từng loại UI vào enum này. Chúng là state cục bộ.

### `GameplayMenuPage`

Các màn Inventory, Equipment, Character, Crafting, Quest Log và Settings đều dùng
`GameState.GameplayMenu`. Trang đang hiển thị được mô tả riêng bởi `GameplayMenuPage`.

Cách tách này giúp bổ sung menu RPG mà không làm `GameState` tăng vô hạn.

`GameplayMenuPage` không đại diện cho navigation của MainMenu Scene; save-slot selection của Main
Menu thuộc `MainMenuController`.

### `GameStatePolicy`

Mỗi state có bốn thuộc tính hành vi:

- `PausesWorld`: manager đặt `Time.timeScale` bằng `0`.
- `AllowsGameplayInput`: Player/combat có được nhận input hay không.
- `AllowsUIInput`: có được mở một menu UI mới hay không.
- `ShowsCursor`: trạng thái hiển thị con trỏ chuột.

Policy nằm tập trung trong `GameStateManager`. Nếu thiết kế thay đổi, ví dụ inventory không còn
pause thế giới, chỉ cần đổi policy của `GameplayMenu` thay vì sửa mọi UI và gameplay script.

### `GameStateSnapshot`

Một snapshot gồm `GameState` và `GameplayMenuPage`. Snapshot được lưu trong stack để quay lại đúng
ngữ cảnh trước đó.

Ví dụ:

```text
Playing
  -> Paused
  -> GameplayMenu(Settings)
  -> đóng Settings: Paused
  -> Resume: Playing
```

### `GameStateManager`

Manager được tạo tự động bằng `RuntimeInitializeOnLoadMethod`, có `DontDestroyOnLoad` và bắt đầu ở
`Booting`. `GameBootstrap` đặt explicit trong scene quyết định `MainMenu` hoặc development `Playing`;
manager không còn tự động vào gameplay trong `Awake`.

API chính:

```csharp
GameStateManager.Instance.Pause();
GameStateManager.Instance.Resume();
GameStateManager.Instance.OpenMenu(GameplayMenuPage.Inventory);
GameStateManager.Instance.ReturnToPreviousState();

GameStateManager.Instance.PushState(GameState.Dialogue);
GameStateManager.Instance.ReplaceState(GameState.Loading);
GameStateManager.Instance.ResetToPlaying();
```

- `PushState`: lưu state hiện tại rồi chuyển sang state mới.
- `ReturnToPreviousState`: pop stack và quay lại đúng state trước.
- `ReplaceState`: thay state hiện tại mà không thay đổi history.
- `ResetToPlaying`: xóa toàn bộ history; dùng sau khi load xong hoặc khi cần phục hồi về gameplay.

## Cách các hệ thống giao tiếp

Các hệ thống phản ứng qua event thay vì được manager gọi trực tiếp:

```csharp
private void OnEnable()
{
    GameStateManager.Instance.StateChanged += HandleStateChanged;
}

private void OnDisable()
{
    if (GameStateManager.Instance != null)
        GameStateManager.Instance.StateChanged -= HandleStateChanged;
}

private void HandleStateChanged(GameStateChange change)
{
    bool isPaused = change.Current.State == GameState.Paused;
}
```

Player dùng cổng đọc đơn giản:

```csharp
if (!GameStateManager.AllowsGameplayInput)
    return;
```

`PauseMenuUI`, `InventoryWindowUI` và `SettingsUI` hiện chỉ yêu cầu đổi state và đồng bộ visibility
theo state. Chúng không còn sở hữu `Time.timeScale`.

## Tích hợp Save/Load trong tương lai

`SaveManager` phải là service riêng. GameStateManager chỉ điều phối lifecycle:

```csharp
public async Task SaveAsync()
{
    GameStateManager states = GameStateManager.Instance;
    states.PushState(GameState.Saving);

    SaveSnapshot snapshot = CaptureSnapshotOnMainThread();
    await WriteSnapshotAsync(snapshot);

    states.ReturnToPreviousState();
}
```

Luồng load đề xuất:

```text
Booting/Paused/PlayerDead
  -> ReplaceState(Loading)
  -> đọc file save
  -> khôi phục PlayerStat, inventory, equipment
  -> khôi phục player position và WorldState
  -> ResetToPlaying()
```

Không serialize `CurrentState`, menu đang mở hoặc state history vào save. Sau khi load thành công,
game luôn trở về `Playing`. Quest/dialogue progression phải được lưu trong dữ liệu domain riêng.

Nếu thao tác save thất bại, `SaveManager` vẫn phải gọi `ReturnToPreviousState` trong `finally` và
phát lỗi cho UI. Manager không xử lý file I/O hoặc quyết định retry.

GameState là lifecycle code, không cần biến thành ScriptableObject graph. Data-driven content systems
chỉ đọc policy/state hoặc subscribe event; xem
[Data-Driven Development Guide](DevelopmentPlan/DataDrivenDevelopment.md) để phân biệt core flow và content data.

Luồng đầy đủ ba save slot, New Game/Continue, atomic write và restore order được quy định tại
[Save and World Persistence Plan](DevelopmentPlan/SaveAndWorldPersistence.md).

## Bootstrap và MainMenu Scene

Implementation Phase 1 foundation hiện tại:

1. `GameState.MainMenu`, `GameState.GameplayMenu` và `GameplayMenuPage` đã được migration.
2. `GameStateManager.Awake` giữ `Booting`.
3. `MainMenu/_SceneContext/GameBootstrap` chọn `MainMenu`.
4. `DemoScene/_SceneContext/GameBootstrap` chỉ tạo development session khi state vẫn là `Booting`.
5. Khi `SceneFlowService` load DemoScene từ session thật, scene bootstrap không được tự vào `Playing`;
   `GameplayReadinessGate` trên `_SceneContext` là restore owner thật, chờ mọi
   `IGameplayReadinessSource` báo sẵn sàng rồi mới gọi `CompleteGameplayRestore()` đúng một lần. Hết
   timeout hoặc thiếu dependency bắt buộc, Gate gọi `SceneFlowService.FailGameplayRestore()` để quay về
   MainMenu thay vì kẹt ở `Loading`. Phase 2/3 cắm thêm `IGameplayReadinessSource` (save/world/quest/
   inventory restore) vào danh sách của Gate mà không cần sửa `GameplayReadinessGate` hay `SceneFlowService`.
6. Pause Menu gọi `SceneFlowService.TryReturnToMainMenu()`; service chuyển sang Loading, clear session,
   yêu cầu `GameplaySceneLifetime` release các persistent gameplay roots rồi load MainMenu bằng Single.

Không tạo `GameplayMenuPage.SaveSelection` cho MainMenu Scene. Main Menu và gameplay overlays có
navigation controller độc lập, chỉ dùng chung application services cần thiết như Settings/Save metadata.

## Mở rộng state mới

Ví dụ bổ sung Photo Mode:

1. Thêm `PhotoMode` vào `GameState`.
2. Khai báo policy trong dictionary `Policies`.
3. Hệ thống camera subscribe `StateChanged` để bật/tắt photo controls.
4. UI gọi `PushState(GameState.PhotoMode)` và đóng bằng `ReturnToPreviousState()`.

Không sửa Player nếu policy đã mô tả đúng `AllowsGameplayInput`. Không thêm serialized reference
của camera hoặc UI vào GameStateManager.

Ví dụ bổ sung Shop không cần state mới nếu shop chỉ là một trang menu:

1. Dùng `Shop` trong `GameplayMenuPage`.
2. UI gọi `OpenMenu(GameplayMenuPage.Shop)`.
3. Shop controller hiển thị khi snapshot hiện tại là `GameplayMenu(Shop)`.

Chỉ tạo state mới nếu hành vi toàn game khác biệt về world simulation, input hoặc lifecycle.

## Quy tắc kiến trúc

- Chỉ GameStateManager được phép thay đổi `Time.timeScale` cho game flow.
- Gameplay code không tự mở/đóng UI; nó phát event hoặc yêu cầu chuyển state.
- UI không trực tiếp enable/disable Player hoặc Enemy.
- SaveManager, WorldManager và GameStateManager là ba trách nhiệm độc lập.
- State machine của Player/Enemy không được nhập vào GameStateManager.
- Mọi `PushState` phải có đường `ReturnToPreviousState`, hoặc được kết thúc bằng `ResetToPlaying`.
- Popup con như xác nhận bỏ item nên thuộc UI navigation stack, không phải global game state.

## Các file liên quan

- `Assets/Scripts/GameManagers/GameStateManager.cs`
- `Assets/Scripts/GameManagers/GameStateTypes.cs`
- `Assets/Scripts/GameManagers/GameBootstrap.cs`
- `Assets/Scripts/GameManagers/GameSessionManager.cs`
- `Assets/Scripts/GameManagers/SceneFlowService.cs`
- `Assets/Scripts/GameManagers/GameplaySceneLifetime.cs`
- `Assets/Scripts/GameManagers/GameplayReadinessGate.cs`
- `Assets/Scripts/GameManagers/IGameplayReadinessSource.cs`
- `Assets/Scripts/GameManagers/SceneDependencyReadinessSource.cs`
- `Assets/Scripts/UI/PauseMenuUI.cs`
- `Assets/Scripts/Inventory/UI/InventoryWindowUI.cs`
- `Assets/Scripts/UI/SettingsUI.cs`
- `Assets/Scripts/Player/PlayerMovement.cs`
- `Assets/Scripts/Player/PlayerCombat.cs`
