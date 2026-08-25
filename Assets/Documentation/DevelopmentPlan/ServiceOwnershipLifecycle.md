# Service Ownership and Lifecycle Inventory

Ngày kiểm kê: 2026-08-21  
Phạm vi: toàn bộ `Assets/Scripts/**/*.cs` và các instance tương ứng trong `DemoScene`.

Tài liệu này là baseline audit cho Phase 0. Cột **đích đề xuất** phải được áp dụng theo phase ghi trong
bảng, không phải giấy phép refactor đồng loạt. Những boundary đã có trong `RuntimeArchitecture.md` là
source of truth; những chi tiết tách class mới bên dưới vẫn là đề xuất cho tới khi phase tương ứng triển khai.

## Định nghĩa lifecycle

| Scope | Bắt đầu | Kết thúc | Được giữ scene reference? |
|---|---|---|---|
| Application | Process/bootstrap | Thoát ứng dụng | Không |
| Session | New Game/Continue thành công | Return Main Menu/đổi session | Không giữ trực tiếp object của scene đã unload |
| Scene | Scene load/scene-ready | Trước scene unload | Có, nhưng phải unregister khi unload |
| Feature | Feature root được khởi tạo | Feature root bị dispose/unload | Chỉ dependency đã khai báo trong feature/context |
| Presentation | UI/actor instance enable | Disable/destroy | Có reference cùng scene; không sở hữu domain/save state |
| Definition/Resolver | Catalog/config được load | Application hoặc session teardown theo backend | Chỉ immutable asset/reference catalog |

## Inventory service và state holder hiện tại

| Thành phần | Hiện tại | Dependency/reference đang giữ | Đích đề xuất | Rủi ro chính | Migration |
|---|---|---|---|---|---|
| `GameStateManager` | Auto-bootstrap, static singleton, `DontDestroyOnLoad`; tự vào `Playing` | Policy, history; điều khiển `Time.timeScale` và cursor; không giữ scene object | **Application service** | Khởi tạo thẳng `Playing`; static fallback cho phép input nếu manager chưa tồn tại | Phase 1: bootstrap explicit, thêm MainMenu, reset history khi đổi session |
| `InventoryManager` | Scene instance, static singleton, `DontDestroyOnLoad` | Runtime slots/gold, `ItemSO`, event cho gameplay UI | **Session/domain service** | Data slot A có thể rò sang slot B/MainMenu; duplicate scene instance bị destroy; static không được clear ở `OnDestroy` | Phase 3–4: session tạo/clear state, restore bằng DTO/catalog; UI chỉ subscribe active session |
| `EquipmentManager` | Scene instance, static singleton, `DontDestroyOnLoad` | Inventory, PlayerStat, ba `SpriteLibrary` của Player, equipment definitions/default visuals | **Session equipment state + scene presentation binder** | Persistent manager giữ Player scene components; actor mới không được bind; equip transaction có thể lệch inventory | Phase 4: tách equipped state khỏi visual binder, explicit bind Player khi scene-ready |
| `PlayerStat` trên `Player` | Static singleton; gọi `DontDestroyOnLoad` cho toàn Player root | Progression/health/runtime modifiers; events; combat/enemy truy cập static | **Scene actor + session/save data** | Player GameObject đi vào MainMenu hoặc nhân đôi khi load scene; `Awake` reset health; static không clear khi destroy | Phase 3–4: spawn Player theo scene/session, restore DTO; không persist GameObject |
| `MapManager` | Scene instance nhưng static singleton và `DontDestroyOnLoad` | `Tilemap`, Player, `TileDataSO`; `FindAnyObjectByType`; `Resources.LoadAll` | **Scene service** | Giữ Tilemap/Player đã unload; scene mới tạo duplicate rồi bị destroy; lookup tile thiếu key có thể lỗi | Phase 1 chốt registration/rebind; Phase 8 hoàn thiện world/ground service và portability |
| `SettingsService` | Auto-bootstrap, static singleton, `DontDestroyOnLoad` | Giá trị SFX/Music/fullscreen; `PlayerPrefs`; runtime audio/screen API; không giữ scene UI | **Application service** | MainMenu presentation chưa được dựng; cần giữ schema key tương thích | Phase 1 foundation đã triển khai; mọi settings presentation dùng chung service |
| `SoundFXManager` | Static singleton, static AudioSource, `DontDestroyOnLoad`; volume do SettingsService áp | Player foot Transform và MapManager/Tilemap audio; không còn giữ gameplay Slider | **Application audio service + scene footstep presenter** | Persistent service vẫn giữ actor reference và cần rebind/tách emitter khi chuyển world scene | Phase 1 đã cắt settings UI ownership; feature audio migration tiếp theo khi đóng gói Player/world |
| `MusicManager` | Static singleton, `DontDestroyOnLoad`; volume tổng do SettingsService áp | AudioSource cùng root; scene MusicManager cung cấp clip và track volume khi chuyển scene | **Application audio service** | API static; chưa có area-to-track resolver | Volume thực tế = track volume trên AudioSource của scene × Music Volume trong Settings; khu vực production sau này đổi clip/track volume qua `SetClip` hoặc resolver data-driven |
| `SettingsUI` | Gameplay presentation trong DemoScene | Slider/toggle/image; preview/save/restore qua SettingsService | **Presentation**, dùng shared **Application SettingsService** | MainMenu Settings presentation chưa được dựng | Phase 1 theo D-018: hai navigation UI, chung SettingsService; UI không sở hữu persistence |
| `InventorySeeder` | Component cùng persistent InventoryManager; seed ở `Start` | `ItemDatabase`, InventoryManager singleton | **NewGame factory hoặc explicit demo fixture** | Starter items có thể phụ thuộc scene startup và trùng với save restore | Phase 3–4: chỉ chạy khi tạo New Game/test profile, không chạy khi Continue/reload |
| `ItemLookup` | Static helper gọi `Resources.LoadAll<ItemSO>` | Resources path và dictionary itemId → asset | **Definition resolver backend tạm thời** | Duplicate ID ghi đè âm thầm; lookup rải rác nếu call site tăng | Phase 4: interface catalog/resolver + duplicate/missing-ID validator; Resources chỉ là backend migration |

## Thành phần không phải persistent service

| Nhóm | Lifecycle đúng | Ghi chú |
|---|---|---|
| `PauseMenuUI`, `InventoryWindowUI`, `InventoryUI`, slot/equipment/gold UI | Presentation cùng gameplay scene | Subscribe/unsubscribe domain/state events; không đưa vào `DontDestroyOnLoad` |
| Enemy và hitbox/projectile/health bar | Scene actor/temporary scene object | Có thể resolve Player qua scene context; không mang runtime instance qua scene |
| Camera, Light, Grid/Tilemap, EventSystem, UICanvas | Scene/presentation | Nằm dưới `_World`/`_UI`; scene installer chịu trách nhiệm bind |
| `ItemSO`, `EquipmentItemSO`, `TileDataSO`, catalogs/libraries | Immutable Definition | Không mutation để lưu runtime progress; save chỉ giữ stable ID/state |

Các `static readonly` Animator hash và pure static calculation/helper không phải service state và không
cần lifecycle migration.

## Dependency hotspots đã xác nhận

```text
SettingsUI (scene UI)
  → SettingsService (application persistence/runtime settings)

SettingsService (application)
  → PlayerPrefs
  → SoundFXManager/MusicManager volume API

SoundFXManager (persistent)
  → Player FootPos (scene actor)
  → MapManager (persistent nhưng giữ scene Tilemap/Player)

EquipmentManager (persistent)
  → InventoryManager (session state)
  → PlayerStat (scene actor/session data)
  → Player SpriteLibrary components (scene actor)
```

Settings/UI ownership đã được cắt trong Phase 1; hai đường actor/world còn lại phải được cắt trước khi
flow production `MainMenu → gameplay → MainMenu` được xem là portable hoàn chỉnh. Persistent
application service tuyệt đối không được giữ `Slider`, `Tilemap`, Player Transform hay SpriteLibrary
của scene đã unload.

## Quyết định ownership áp dụng từ đây

Các điểm sau đã phù hợp kiến trúc nguồn chuẩn và được dùng làm constraint triển khai:

1. `GameStateManager`, Settings, Save, SceneFlow và GameSession thuộc application scope.
2. Inventory/equipment runtime state thuộc active session; không tồn tại như dữ liệu gameplay active khi ở MainMenu.
3. Player là scene actor; save/session giữ dữ liệu Player, không giữ Player GameObject.
4. Map/Tilemap query là scene service và phải register/unregister qua scene context.
5. Gameplay UI là presentation; không sở hữu persistence, scene flow hoặc domain state.
6. Audio có thể sống application scope, nhưng emitter/binding tới actor/world/UI phải scene-local hoặc rebind explicit.
7. ScriptableObject definition là immutable input; catalog/resolver không sở hữu runtime progress.

## Thứ tự migration an toàn

Không chuyển tất cả manager trong một lần. Thứ tự bắt buộc đề xuất:

1. Phase 1 tạo `GameBootstrap`, `SceneFlowService`, `GameSessionManager` và shared SettingsService.
2. Phase 1 tạo scene registration contract tối thiểu để Map/Player/UI không bị giữ bởi application service.
3. Phase 3 chuyển Player thành scene-spawned actor và restore progression/position từ active session.
4. Phase 4 chuyển Inventory/Equipment sang session state; tách equipment visuals và New Game seeding.
5. Phase 8 hoàn thiện Map/World ownership, persistent world registry và multi-scene rebind.

Mỗi bước phải giữ Direct Play của DemoScene, test load/unload và bảo đảm Console sạch trước khi sang bước kế.

## Verification checklist cho migration

- [x] MainMenu sau return không có Player, gameplay UI, Inventory/Equipment runtime instance hoặc MapManager.
- [ ] Vào gameplay hai lần không sinh duplicate singleton hoặc mất binding.
- [x] Return MainMenu giải phóng gameplay roots đã đăng ký trong `GameplaySceneLifetime` và clear active session.
- [ ] Load slot A rồi B không rò inventory/equipment/stat.
- [ ] Persistent application service không serialize/giữ scene object.
- [ ] Domain reload disabled không để static `Instance` trỏ object đã destroy.
- [x] Một vòng MainMenu → DemoScene → MainMenu lại tạo đúng Player/Inventory/Map/SFX và teardown sạch.
- [ ] New Game seed đúng một lần; Continue không seed.
- [ ] Event subscription đối xứng và không tăng callback sau mỗi scene load.
