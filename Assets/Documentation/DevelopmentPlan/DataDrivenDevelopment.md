# Data-Driven Development Guide

## Mục tiêu

Data-Driven Development trong project này có nghĩa là:

> Code định nghĩa những loại hành vi hệ thống hỗ trợ; data định nghĩa content cụ thể sử dụng các hành vi đó.

Ví dụ code Quest hỗ trợ objective `Talk`, `Craft`, `Purchase`, `Gather`, `Kill`; designer tạo nhiều
quest khác nhau bằng asset data mà không sửa `QuestManager` cho từng quest.

Mục tiêu không phải loại bỏ code hoặc biến Inspector thành một ngôn ngữ lập trình. Mục tiêu là:

- Thêm item, enemy variant, quest, recipe, shop hoặc tutorial step chủ yếu bằng asset/config.
- Dữ liệu content có ID ổn định để save/load và cross-reference.
- Definition không bị mutation trong runtime.
- Runtime state và save state tách khỏi asset thiết kế.
- Content sai được validator phát hiện trước khi vào gameplay/build.
- Feature đã dựng ở DemoScene có thể dùng lại trong scene khác với cùng data assets.

## Ba lớp dữ liệu bắt buộc

### 1. Definition data

Dữ liệu thiết kế, thường là ScriptableObject:

- Item stats, icon, stack size.
- Enemy base stats và attack definitions.
- Quest objectives/rewards/prerequisites.
- Recipe ingredients/outputs.
- Shop stock và pricing policy.
- Tutorial steps/prompts/completion conditions.
- NPC identity và capabilities.

Definition được author trong Editor và được coi là read-only khi runtime.

```csharp
public abstract class GameDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    public string Id => _id;
}
```

Không ghi health hiện tại, objective counter hoặc stock runtime trực tiếp vào ScriptableObject.

### 2. Runtime state

Object/class thay đổi trong phiên chơi:

```csharp
public sealed class QuestRuntimeState
{
    public QuestDefinition Definition { get; }
    public QuestStatus Status { get; private set; }
    public int CurrentObjectiveIndex { get; private set; }
}
```

Runtime state giữ reference definition đã resolve và trạng thái thay đổi. Nó không phải save DTO và
không phụ thuộc UI.

### 3. Save data

DTO thuần có thể serialize:

```csharp
[Serializable]
public sealed class QuestProgressSaveData
{
    public string questId;
    public QuestStatus status;
    public int currentObjectiveIndex;
    public int[] objectiveCounters;
}
```

Save lưu stable ID + phần thay đổi, không serialize ScriptableObject, GameObject, Transform hoặc Unity
instance ID. Khi load:

```text
questId từ save
→ QuestCatalog resolve QuestDefinition
→ tạo QuestRuntimeState
→ apply progress bằng restore API
```

## Stable ID contract

ID là xương sống của toàn bộ data-driven architecture.

Ví dụ convention:

```text
item.weapon.sword.iron.001
enemy.slime.green
quest.tutorial.crafting.001
npc.town.blacksmith
recipe.weapon.sword.iron
shop.town.blacksmith
tutorial.controls.move
area.tutorial
spawn.tutorial.start
```

Quy tắc:

- Lowercase ASCII.
- Dùng dấu chấm phân cấp; không chứa display/localized text.
- Unique trong domain tương ứng; khuyến nghị unique toàn project nếu chi phí thấp.
- Không tự đổi khi rename asset/file/GameObject.
- Không tái sử dụng ID đã phát hành cho content khác.
- Nếu buộc đổi ID, phải có alias/migration cho save cũ.

Không dùng làm persistent ID:

- `gameObject.name`.
- Asset filename.
- Display name.
- Array index.
- Enum numeric ordinal của content.
- `GetInstanceID()`.
- GUID Unity trực tiếp trong save public contract.

## Definition, instance và runtime instance ID

Phân biệt:

- `definitionId`: loại content, ví dụ `enemy.slime.green`.
- `persistentInstanceId`: instance độc nhất trong world, ví dụ chest tại vị trí cụ thể.
- `runtimeInstanceId`: object tạm trong phiên, không nhất thiết save.

Ví dụ 20 slime thường dùng cùng definition ID và không cần persistent instance ID. Một boss unique dùng
cả `enemy.boss.forest_guardian` và `world.boss.forest_guardian.01`.

Daily Quest tương lai có:

- Definition ID cho template.
- Quest instance ID cho lần generate cụ thể.

Không dùng một field ID cho cả ba ý nghĩa.

## Catalog và resolver

Mỗi domain cần một nguồn đăng ký rõ:

```csharp
[CreateAssetMenu(menuName = "Game/Items/Item Catalog")]
public sealed class ItemCatalog : ScriptableObject
{
    [SerializeField] private ItemDefinition[] _items;
}
```

Runtime tạo lookup một lần:

```csharp
Dictionary<string, ItemDefinition> _byId;
```

Catalog chịu trách nhiệm:

- Resolve ID nhanh và nhất quán.
- Kiểm tra ID rỗng/trùng.
- Cho SaveManager báo missing content.
- Là dependency explicit của service.
- Hỗ trợ editor search/authoring.

Không gọi `Resources.LoadAll` rải rác trong nhiều service. Project hiện có `ItemLookup.BuildFromResources`
và nhiều catalog mảng riêng; roadmap nên hợp nhất thành một item catalog/resolver contract. Có thể tiếp
tục dùng Resources ở bước migration, nhưng domain code chỉ biết `IItemResolver`, không biết cơ chế load.

## Cross-reference bằng ID hay asset reference

Trong definition asset, có hai lựa chọn:

### Asset reference

```csharp
public ItemDefinition rewardItem;
```

Ưu điểm: Inspector thân thiện, Unity giữ reference. Nhược điểm: validator/save/export cần đọc ID.

### Stable ID

```csharp
public string rewardItemId;
```

Ưu điểm: DTO/external data dễ dùng. Nhược điểm: dễ typo nếu Inspector chỉ là text field.

Khuyến nghị:

- Authoring asset dùng typed asset reference khi có thể.
- Runtime/save boundary chuyển thành stable ID.
- Custom inspector/property drawer hiển thị selector và validate ID.
- Không để designer nhập string ID tự do nếu có thể cung cấp picker.

## Data-driven behavior bằng handler registry

Data chọn loại hành vi; code handler thực thi:

```csharp
public enum QuestObjectiveType
{
    Talk,
    Obtain,
    Craft,
    Purchase,
    Gather,
    Kill
}
```

```text
Quest Objective Definition
→ ObjectiveType.Kill
→ KillObjectiveHandler
→ subscribe EnemyKilled event
→ validate enemyId/areaId
→ update runtime counter
```

Thêm quest Kill mới: tạo data. Thêm behavior Escort hoàn toàn mới: viết `EscortObjectiveHandler` một
lần, sau đó nhiều quest dùng handler đó bằng data.

Handler phải:

- Có input contract rõ.
- Không chứa ID quest cụ thể.
- Không gọi UI trực tiếp.
- Không tự save file.
- Có unit tests cho matching/progression.

## Domain events là cầu nối

Data-driven systems không nên poll state mỗi frame hoặc phụ thuộc UI click. Dùng typed domain event sau
transaction thành công:

```text
CraftingService completes transaction
→ ItemCrafted(itemId, quantity, stationId)
→ Quest objective handler matches definition
→ runtime quest progress changes
→ QuestProgressChanged event
→ Quest UI refreshes
```

Không phát `ItemCrafted` nếu transaction thiếu nguyên liệu/inventory full. Restore save không được phát
gameplay event làm tăng objective hoặc grant reward.

## Áp dụng theo domain

### Item và equipment

Definition nên chứa:

- `itemId`.
- Display/localization key.
- Icon.
- Type/tags/rarity.
- Stack policy.
- Buy/sell base value.
- Equipment slot và stat modifiers nếu trang bị.
- Optional visual definition.

Runtime InventorySlot chỉ giữ definition reference và quantity. Save giữ `itemId + quantity`.

Hiện project đã có `ItemSO`/`EquipmentItemSO`, đây là nền tảng tốt. Cần bổ sung encapsulation,
validation, catalog thống nhất và tách pricing/tags khi hệ thống cần.

### Enemy

Nên tách:

- `EnemyDefinition`: identity, stats, XP, drop table, high-level behavior config.
- Prefab: Rigidbody/Animator/hitbox/visual Transform references.
- `AttackDefinition`: damage/range/cooldown/type/projectile data.
- Runtime Enemy: health, state machine, cooldown runtime.

Không nên đặt reference scene Transform như hitbox instance trong shared ScriptableObject. Definition
chỉ tham chiếu prefab/asset hoặc logical socket key; prefab binder resolve component scene instance.

`EnemyUniversal.AttackProfile` hiện serialize trong prefab đã có tính data-driven. Migration nên làm
từng bước: trước tiên stable enemy ID/drop data/catalog; chỉ tách attack ra asset khi có reuse thực sự.

### Quest

QuestDefinition chứa:

- `questId`.
- Localization keys.
- Prerequisite definition references.
- Objective definitions.
- Reward definitions.
- Flags tutorial/main/daily-compatible.

Quest runtime/save tuân theo ba-layer model. Không viết `if (questId == "...")` trong QuestManager.

### Tutorial

TutorialDefinition chứa ordered/graph steps:

- Stable step ID.
- Instruction localization key.
- Completion condition type + parameters.
- Optional focus/highlight target key.
- Next step.

Completion dựa gameplay action/event, không hard-code key `W`, `Shift`, `I`; Input System remap vẫn
hoàn thành được tutorial.

### Crafting

RecipeDefinition:

- `recipeId`.
- Ingredient item references + quantity.
- Output item references + quantity.
- Required station/tag.
- Unlock prerequisites.
- Optional time/cost.

CraftingService là transaction engine dùng chung; không có method riêng `CraftIronSword()`.

### Shop

ShopDefinition:

- `shopId`.
- Stock entries/item references.
- Pricing multiplier/rules.
- Unlock requirements.
- Restock definition tương lai.

Shop runtime giữ stock thay đổi nếu thiết kế cần. Save chỉ lưu delta/runtime state, không copy toàn bộ
definition vào save.

### NPC

NpcDefinition chứa identity, presentation và capability references. Scene prefab giữ Animator,
collider và interaction anchors. NPC có thể kết hợp QuestGiver/Shop/Crafting capabilities bằng data.

### Loot và resource

DropTableDefinition gồm weighted entries và quantity range. ResourceDefinition gồm output, tool/tag
requirements và respawn rule. RNG execution là code; entry cụ thể là data.

### World persistence

Persistent behavior type được code hỗ trợ; từng chest/node/boss có definition/instance data. Save giữ
instance ID và state delta như `opened`, `collected`, `nextRespawnTime`.

## Authoring workflow trong DemoScene

```text
Define behavior contract
→ implement/reuse runtime handler
→ create Definition asset
→ add to Catalog
→ run validator
→ create/bind prefab instance
→ test variants in DemoScene
→ test save/load and failure paths
→ portability test in minimal scene
→ promote prefab + catalog/config to world scene
```

DemoScene nên chứa nhiều variant data để chứng minh hệ thống tổng quát, ví dụ:

- Item stackable/non-stackable/equipment.
- Enemy melee/area/projectile.
- Quest với từng objective type.
- Recipe thành công/thiếu nguyên liệu/inventory full.
- Shop locked/unlocked/insufficient gold.

Nếu feature chỉ chạy với một asset duy nhất được hard-code, chưa chứng minh data-driven.

## Validation pipeline

### OnValidate

Dùng cho clamp/local invariant nhỏ:

- Quantity > 0.
- Max stack >= 1.
- Damage/range không âm.

Không thực hiện scan toàn project hoặc sửa asset hàng loạt trong `OnValidate`.

### Catalog validator

Kiểm tra cross-asset:

- ID rỗng/trùng/format sai.
- Missing reference.
- Quest prerequisite cycle.
- Recipe ingredient/output invalid.
- Shop stock item không nằm trong catalog.
- Drop weight <= 0.
- Tutorial next step không tồn tại/cycle ngoài ý muốn.
- Stable persistent ID trùng trong scene.

### Build gate

Trước build/content milestone:

- Chạy toàn bộ validators.
- Error chặn build đối với broken required contract.
- Warning dành cho optional/missing recovery-compatible content.
- Report gồm asset path, field và suggested fix.

Validator không được chỉ log “invalid data” chung chung.

## Data versioning và migration

Hai version khác nhau:

- `saveVersion`: cấu trúc save DTO.
- `contentVersion`: tùy chọn, mô tả bộ definition/content.

Khi xóa/đổi definition ID:

- Giữ alias map ID cũ → ID mới nếu save đã phát hành.
- Migration resolve alias trước khi báo missing.
- Không tái dùng ID cũ cho content khác.
- Missing optional content tạo recovery report.
- Missing required player location/main progression có fallback hoặc chặn load rõ.

## Folder convention đề xuất

```text
Assets/Game/
├─ Core/
│  ├─ Runtime
│  └─ Data
├─ Items/
│  ├─ Definitions
│  ├─ Catalogs
│  ├─ Prefabs
│  └─ Editor
├─ Enemies/
├─ Quests/
├─ Tutorial/
├─ Crafting/
├─ Shops/
├─ NPCs/
└─ World/
```

Không bắt buộc di chuyển asset hiện tại ngay. Thực hiện migration theo domain để tránh phá GUID/prefab.

## Khi nào không nên data-drive

Giữ bằng code nếu:

- Là invariant/lifecycle lõi như atomic save transaction.
- Logic chỉ có một implementation và không phải content variation.
- Đưa ra Inspector làm khó hiểu hơn API code.
- Cần compile-time safety phức tạp.
- Data graph trở thành scripting language khó debug.

Ví dụ nên giữ code:

- GameState transition mechanics.
- Save file atomic replace/migration pipeline.
- Damage calculation algorithm lõi.
- Inventory transaction rules.
- Scene lifecycle orchestration.

Những thuật toán này có thể nhận tham số data, nhưng flow/invariant vẫn là code.

## Anti-patterns

- ScriptableObject bị sửa runtime và làm bẩn asset trong Editor.
- Mỗi quest/enemy/item có subclass riêng dù chỉ khác số liệu.
- String ID nhập tay khắp Inspector không picker/validator.
- Một `GameDatabase` khổng lồ chứa mọi domain.
- Service gọi `Resources.LoadAll` mỗi lần cần lookup.
- Save serialize trực tiếp asset/GameObject reference.
- Definition chứa scene instance Transform.
- Generic “condition/action graph” quá trừu tượng trước khi có use case.
- Dùng reflection/type name làm save contract mà không migration.
- Copy asset/config riêng cho từng scene rồi diverge.

## Migration từ project hiện tại

### Bước 1 — ID và validation foundation

- Chốt ID convention.
- Validate toàn bộ ItemSO hiện tại.
- Xây resolver/catalog interface.
- Không đổi behavior gameplay.

### Bước 2 — Encapsulate item data

- Chuyển public mutable fields sang serialized private + read-only properties theo từng đợt.
- Giữ `FormerlySerializedAs` khi rename field.
- Cập nhật Inventory/Equipment dùng resolver.

### Bước 3 — Enemy definitions ở mức cần thiết

- Stable enemy ID, XP/drop/stats definition.
- Giữ prefab hitbox/Animator bindings.
- Không tách mọi AttackProfile thành asset nếu chưa reuse.

### Bước 4 — Quest/Tutorial/Recipe/Shop xây data-driven ngay từ đầu

- Definition + runtime + save DTO.
- Catalog + validators.
- Handler/domain event architecture.

### Bước 5 — Content build gates

- One-click validation menu.
- Automated EditMode validation.
- CI/build preflight khi pipeline sẵn sàng.

## Definition of Done cho một data-driven domain

- [ ] Definition type có stable ID.
- [ ] Runtime state không mutation definition.
- [ ] Save DTO chỉ lưu ID + state delta.
- [ ] Catalog/resolver là dependency explicit.
- [ ] Duplicate/missing/cross-reference validator tồn tại.
- [ ] Ít nhất hai content variants chạy mà không sửa runtime code.
- [ ] Missing/renamed content có failure hoặc recovery policy.
- [ ] DemoScene integration test pass.
- [ ] Minimal-scene portability test pass.
- [ ] Authoring guide chỉ rõ cách thêm content mới.

## Success criteria toàn project

Kiến trúc được coi là data-driven hiệu quả khi designer có thể thực hiện các việc sau mà không sửa
manager core:

- Thêm item/equipment mới.
- Thêm enemy variant dùng behavior đã hỗ trợ.
- Tạo quest mới từ objective handlers có sẵn.
- Tạo recipe/shop stock mới.
- Thêm tutorial step dùng completion condition có sẵn.
- Kéo feature prefab/catalog sang world scene mới.

Nếu thêm content thường xuyên vẫn phải thêm `if (specificId)` trong manager, domain đó chưa đạt mục tiêu.
