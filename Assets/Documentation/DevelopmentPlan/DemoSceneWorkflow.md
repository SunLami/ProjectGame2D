# DemoScene Integration Workflow

## Vai trò chính thức

`Assets/Scenes/DemoScene.unity` là **integration playground** của project:

- Dựng và kiểm thử toàn bộ chức năng gameplay tại một nơi.
- Kết nối prefab, UI, input, save/load mock, player, NPC, enemy và world interactions.
- Tạo scenario tái hiện lỗi nhanh.
- Chứng minh một feature đã sẵn sàng trước khi đưa sang scene production.

`DemoScene` không phải MainMenu Scene và không mặc định là world scene production. Không rename nó
thành `GameScene`. Về sau các scene thật sẽ kéo thả những feature package đã được chứng minh tại đây.

## Nguyên tắc quan trọng

DemoScene được phép chứa **instance để test**, nhưng feature code không được phụ thuộc DemoScene.

Một chức năng chỉ được coi là tái sử dụng được khi:

- Root của chức năng là prefab hoặc prefab hierarchy rõ ràng.
- Config nằm trong ScriptableObject/prefab serialized fields, không nằm trong scene-only singleton.
- Không tìm GameObject bằng tên/hierarchy của DemoScene.
- Scene reference được bind qua installer/context khi scene load.
- Kéo prefab sang một empty test scene vẫn chạy sau khi cung cấp dependency bắt buộc.
- Xóa instance khỏi DemoScene không làm mất asset/config nguồn.

## Cấu trúc đề xuất trong DemoScene

```text
DemoScene
├─ _SceneContext
│  ├─ DemoSceneInstaller
│  ├─ SceneReferenceRegistry
│  └─ DemoSaveProfile/DebugControls
├─ _World
│  ├─ Ground/Tilemaps
│  ├─ TutorialTestArea
│  ├─ TownTestArea
│  └─ CombatTestArea
├─ _Actors
│  ├─ PlayerSpawn
│  ├─ NPC test instances
│  └─ Enemy test instances
├─ _Features
│  ├─ InventoryFeature
│  ├─ QuestFeature
│  ├─ ShopFeature
│  ├─ CraftingFeature
│  └─ TutorialFeature
└─ _UI
   └─ GameplayUIRoot prefab
```

Các tên hierarchy này là convention cho tác giả scene, không phải runtime lookup contract.

## Feature package contract

Mỗi chức năng kéo thả nên có một composition root rõ:

```text
FeatureName/
├─ Runtime scripts
├─ Data/ScriptableObjects
├─ Prefabs
├─ UI prefabs (nếu có)
├─ Tests
└─ Authoring notes
```

Ví dụ Quest feature:

```text
QuestSystemRoot.prefab
├─ QuestManager
├─ QuestEventBridge
└─ QuestRuntimeDatabase reference

QuestLogUI.prefab
└─ subscribe QuestManager/read model
```

Scene production kéo `QuestSystemRoot` và `GameplayUIRoot` hoặc dùng scene installer tạo/bind chúng.
Không copy-paste một GameObject đã unpack rồi sửa riêng ở từng scene nếu có thể giữ prefab connection.

Mọi feature có content variants phải theo [Data-Driven Development Guide](DataDrivenDevelopment.md):
prefab cung cấp runtime binding, Definition/Catalog cung cấp content, runtime state không sửa asset.

## Dependency classes

Phân biệt ba loại dependency:

1. **Application service:** tồn tại xuyên scene, ví dụ GameState/Save/Settings/Session.
2. **Scene service:** thuộc scene đang active, ví dụ Tilemap/SpawnRegistry/WorldRegistry.
3. **Feature service:** thuộc feature package, ví dụ QuestManager/ShopService.

Scene installer chịu trách nhiệm nối scene service vào application/feature service. Persistent manager
không được giữ reference trực tiếp tới Tilemap, Slider, NPC hoặc object thuộc scene cũ.

Điểm này đặc biệt áp dụng cho `MapManager` và `SoundFXManager` hiện tại: các reference Tilemap,
player foot transform và UI slider phải được rebind hoặc tách ownership trước khi feature được kéo sang
scene khác.

## Quy trình phát triển một feature

### 1. Define contract

- Mục tiêu và ngoài phạm vi.
- Required dependencies.
- Events/API public.
- Save data và stable IDs nếu có.
- Phân loại rõ Definition/Runtime/Save data và catalog/resolver owner.
- Acceptance tests.

### 2. Build asset source

- Runtime logic tách khỏi DemoScene.
- Tạo prefab/config asset nguồn.
- Thêm validation cho required references.
- Tạo ít nhất hai data variants để chứng minh runtime code không hard-code content đầu tiên.

### 3. Integrate in DemoScene

- Instantiate prefab.
- Bind scene context.
- Dựng test fixtures/NPC/item/enemy cần thiết.
- Không sửa source prefab chỉ bằng scene override không được ghi lại.

### 4. Verify in DemoScene

- Happy path.
- Failure/cancel path.
- Save/load round-trip.
- State/input lifecycle.
- Console sạch.

### 5. Portability test

- Tạo hoặc dùng MinimalFeatureTestScene.
- Kéo đúng prefab/config cần thiết.
- Bind dependency theo authoring guide.
- Xác nhận feature không dựa vào object ngầm của DemoScene.

### 6. Promote to production scene

- Kéo prefab giữ connection.
- Chỉ override dữ liệu scene-specific.
- Chạy scene contract validator và smoke test.

## Demo-only tools

Các công cụ sau được phép chỉ tồn tại trong DemoScene nhưng phải đánh dấu rõ:

- Give gold/items.
- Complete/reset tutorial step.
- Accept/complete/reset quest.
- Teleport giữa test area.
- Force save corruption/load fixture.
- Spawn enemy/resource node.
- Time scale/debug overlays.

Demo-only component nên nằm trong namespace/folder `Debug` hoặc có build guard phù hợp để không vô
tình xuất hiện trong production build.

## Direct Play profile

DemoScene cần hỗ trợ bấm Play trực tiếp để phát triển nhanh. `DemoSceneInstaller` chọn một test profile:

- Fresh New Game fixture.
- Mid-tutorial fixture.
- Existing-character fixture.
- Quest/shop/crafting-specific fixture.
- Empty/minimal fixture.

Mặc định dùng dữ liệu in-memory hoặc fixture copy, không dùng trực tiếp Slot 1–3. Nếu muốn test file
thật, debug UI phải hiển thị rõ đang dùng test path hay production save path.

## Scene portability checklist

Trước khi nói “chỉ cần kéo thả sang scene khác”, xác nhận:

- [ ] Prefab root và `.meta` ổn định.
- [ ] Không có missing script/reference.
- [ ] Required reference được validate với thông báo rõ.
- [ ] Không dùng `Find("Demo...")` hay hard-coded scene name trong feature.
- [ ] Persistent service không giữ scene object sau unload.
- [ ] Save IDs không phụ thuộc instance ID của Unity.
- [ ] Definition asset không bị mutation trong runtime.
- [ ] Catalog/validator nhận diện ID trùng và missing cross-reference.
- [ ] UI không phụ thuộc Canvas chỉ có trong DemoScene.
- [ ] Input action map được bind từ context chung.
- [ ] Minimal scene smoke test pass.
- [ ] Production scene smoke test pass sau khi kéo prefab.

## Definition of Done cho DemoScene integration

Feature chạy được trong DemoScene chưa đủ. Definition of Done là:

```text
Feature contract documented
+ prefab/config source created
+ DemoScene scenarios pass
+ save/state failure paths pass
+ portability test passes outside DemoScene
+ authoring/promotion instructions exist
```

## MapNhat integration status

`MapNhat` hiện có `_SceneContext` tối thiểu cho Editor direct-play gồm `GameBootstrap` ở chế độ
`DevelopmentGameplay` và `GameInputCoordinator` bind tới `PlayerInput` của Player trong scene. Vì
`GameStateManager` và `GameSessionManager` tự bootstrap trước scene load, cấu hình này đưa state từ
`Booting` sang `Playing` và kích hoạt action map `Gameplay` mà không sao chép manager singleton vào
scene. Đây mới là portability smoke integration cho Player; các feature UI/save/world đầy đủ vẫn phải
được promote từ DemoScene theo checklist phía trên.

Player ghép nhiều SpriteRenderer dùng cùng một sorting contract trong `DemoScene` và `MapNhat`:

- Mọi sprite được các Player SpriteLibrary tham chiếu dùng pivot `Custom (0.5, 0.315)` normalized.
- Head, Body, Weapon và AttackFX dùng Sprite Sort Point `Pivot`.
- Transform Head, Body, Weapon và AttackFX giữ ở local `(0, 0, 0)`; không bù dịch sprite sau khi đổi
  pivot, nên pivot trong Game Scene chính là pivot đã author trong Sprite Editor.
- Root Player có `SortingGroup`, `sortAtRoot` bật, Sorting Layer/Order ngoài group là `Default/0`.
- Order của Head/Weapon chỉ sắp xếp nội bộ group; Custom Axis Y so sánh toàn Player với world object.
- Tái áp dụng/kiểm tra pivot `(0.5, 0.315)` bằng menu `Tools > Project Game > Player`.

`MapNhat/Attacked_Manequin1` dùng `PolygonCollider2D` của object cha làm body collider theo Physics
Shape đã author trong Sprite Editor. Object con `Hurtbox` dùng `CapsuleCollider2D` trigger và
`MannequinHurtbox` để nhận hit qua `IDamageable`, sau đó chuyển tín hiệu cho `MannequinHitReaction`.
Reaction chỉ phát Animator trigger `Hit` và flash SpriteRenderer đỏ; không có health, death, movement,
AI hoặc knockback. Controller quay về Idle bằng exit time của clip Hit.
