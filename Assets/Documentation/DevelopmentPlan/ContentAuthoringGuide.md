# Content Authoring Guide

Phase 10 deliverable. This is the operational, current-state companion to
[Data-Driven Development Guide](DataDrivenDevelopment.md) (concepts/target architecture) and
[DemoScene Integration Workflow](DemoSceneWorkflow.md) (scene composition). This document tells a
content designer exactly which asset to create, which field to fill in, and which validator to run
to add real content **without touching manager core code** (`QuestManager`, `ShopManager`,
`CraftingManager`, `InventoryManager`, `WorldObjectRegistry`, `SaveMigration`, etc.).

If a task requires editing one of those managers, it is not content authoring — stop and treat it as
a backend change (new Claude phase), not something this guide should tell you to do.

## 0. Before you start

1. Open the scene you're authoring in via Unity MCP (`manage_scene` / `manage_gameobject` /
   `manage_prefabs` / `manage_scriptable_object`), never by hand-editing `.unity`/`.prefab` YAML.
2. Know your stable ID convention: lowercase, dot-hierarchical, e.g. `item.consumable.potion_health`,
   `quest.tutorial.crafting.001`, `shop.town.blacksmith`, `world.chest.forest.01`, `area.town`,
   `spawn.town.entrance`. See [Data-Driven Development Guide](DataDrivenDevelopment.md#stable-id-contract)
   for the full rule set — never use `gameObject.name`, asset filename, or a Unity GUID as an ID.
3. After any content change, run **Tools/Project Game/Validate Content** and read the Console. Errors
   block the content from being save-safe; the small number of `itemId` "accepted legacy format"
   warnings on existing items are pre-existing and known — don't chase them down as part of
   unrelated authoring work.

## 1. Item authoring

**Asset type:** `ItemSO` (`Assets/Scripts/Inventory/ItemSO.cs`) or `EquipmentItemSO` for equippable
items — menu `Scriptable Objects/Item` / `Scriptable Objects/Equipment Item`.

**Placement:** under `Assets/Resources/Items/<Category>/`, e.g. `Assets/Resources/Items/Consumables/`.
Items are loaded via `Resources`, so the folder must stay under a `Resources/` path — this is the one
place in the project where that's intentional (see `DataDrivenDevelopment.md`'s note on unifying
catalogs later; for now, Resources loading is the accepted mechanism).

**Fields to set:**

| Field | Rule |
|---|---|
| `itemId` | Stable ID, `domain.category.name` shape, e.g. `item.consumable.potion_health`. Must be globally unique — the content validator errors on duplicates. |
| `itemName` | Display text only, safe to change any time. |
| `description` | Display text only. |
| `icon` | A `Sprite`. Required for anything the player sees in inventory UI. |
| `type` | `Weapon` / `Armor` / `Material` / `Consumable`. |
| `isStackable` / `maxStackSize` | Stack policy — save DTO (`InventorySaveData.SlotData`) only stores `itemId` + `quantity`, so this can be tuned later without a migration. |

For equipment, also set `EquipmentItemSO`'s slot (`EquipSlot`: Head/Body/Weapon/Ring/Necklace/Foot/
Shield) and stat fields.

### Equipment stat modifier reference

Khi thiết kế equipment item, đối chiếu với growth mặc định của nhân vật để tránh cộng bonus mơ hồ
hoặc vô tình nhân đôi progression:

| Stat | Growth tự động mỗi level | Vai trò của item modifier |
|---|---:|---|
| Max Health | `+3` | Có thể cộng thêm Max Health qua equipment. |
| Attack Damage | `+0.5` | Có thể cộng thêm Attack Damage qua equipment. |
| Defense | `+0.15` | Có thể cộng thêm Defense qua equipment. |
| Critical Chance | `+0.1%` (`0.001`) | Modifier dùng dạng normalized, ví dụ `0.05 = 5%`. |
| Max Stamina | Không tăng theo level | Chưa có field trong `PlayerStatModifiers`; cần backend change nếu item phải tăng stat này. |
| Critical Multiplier | Không tăng theo level | Item có thể cộng modifier; runtime clamp trong khoảng `1–3`. |
| Move Speed | Không tăng theo level | Item có thể cộng modifier. |
| Sprint Multiplier | Không tăng theo level | Item có thể cộng modifier; giá trị cuối không thấp hơn `1`. |
| Dodge Chance | Không tăng theo level | Item có thể cộng modifier; runtime clamp tối đa `50%`. |
| Damage Reduction | Không tăng theo level | Item có thể cộng modifier; runtime clamp tối đa `75%`. |
| Health Regeneration | Không tăng theo level | Item có thể cộng modifier theo HP/giây. |

Các con số trên mô tả contract runtime hiện tại, không phải bảng balance bonus cho item. Không tự đặt
tier/rarity/budget bonus trước khi designer chốt. `CharacterPopup` phải chỉ hiển thị giá trị cuối cùng
sau khi cộng level growth và toàn bộ equipment modifiers; item không được trực tiếp sửa text UI.

**Validation:** `ContentValidationRunner.ValidateItems` checks empty/duplicate `itemId`. Run
**Tools/Project Game/Validate Content** after adding any item.

## Resource node authoring

Create one `ResourceNodeDefinition` per resource species. Author a stable `resourceId`, Mining/
Chopping/Gathering type, HP, independent `harvestDamage`, 1–1.5 second gathering duration, UTC respawn
cooldown and one or more loot entries. Each loot entry directly references an `ItemSO` and declares
chance plus inclusive min/max quantity. Keep `requiredToolType = None` while every valid attack is
allowed; the field is the accepted future tool gate.

The prefab root owns `ResourceNodeInteractable` and a 2D collider. Bind `_visualRoot` to a child—not
the prefab root—so hiding presentation does not stop its cooldown coroutine. Every scene instance needs
a unique `_persistentId`, an `_areaId`, and explicit registration in `WorldObjectRegistry`.

For the current sample content, run **Tools/Project Game 2D/Build Demo Resource Nodes**. It rebuilds
the three placeholder item assets, definitions, prefabs and DemoScene instances for Copper Ore, Wood
Log and Medicinal Leaf without changing their stable `itemId` values.

**Do not:**
- Reuse an `itemId` that was ever shipped for different content.
- Reference the item by `gameObject.name` or asset filename anywhere in code — only by `itemId`
  through `IItemResolver`.

## 2. Quest authoring

**Asset type:** `QuestDefinition` (`Assets/Scripts/Quest/QuestDefinition.cs`) — menu
`Game/Quest/Quest Definition`.

**Fields:**

| Field | Rule |
|---|---|
| `questId` | Stable ID, e.g. `quest.tutorial.crafting.002`. This is the save-file identity (`QuestProgressSaveData.questId`) — never rename it once a save could reference it. |
| `displayName` | Display text. |
| `prerequisiteQuestIds` | `questId`s that must be `Completed` before this quest becomes `Available`. Locked/Available are derived at runtime/restore, never saved directly. |
| `objectives` | Array of `QuestObjectiveDefinition` (see below). |
| `rewards` | `QuestRewardDefinition` — item entries (`itemId` + quantity), `gold`, `experience`. |
| `isTutorialQuest` / `isMainQuest` | The Tutorial Quest chain gates unlocking the Main Quest — see [TutorialAndQuestProgression.md](TutorialAndQuestProgression.md) for the exact gate contract. Two quest *variants* (e.g. two different Tutorial Quests) share the same `QuestManager` runtime handler purely through data — do not add a second manager class. |
| `giverNpcId` | Stable `npcId` this quest is offered by while `Available`. |
| `turnInNpcId` | Stable `npcId` this quest is turned in to while `ReadyToTurnIn`. Can equal `giverNpcId`. |

**Each objective (`QuestObjectiveDefinition`):**

| Field | Rule |
|---|---|
| `type` | `Talk` / `Obtain` / `Craft` / `Purchase` / `Gather` / `Kill` — must match an existing `QuestObjectiveMatchers` handler; adding a genuinely new objective *type* is a backend task, not authoring. |
| `targetId` | Meaning depends on `type`: `npcId` (Talk), `itemId` (Obtain/Craft/Purchase), `resourceId` (Gather), `enemyId` (Kill). |
| `targetAreaId` | Optional, only consulted by Gather/Kill. Empty = any area. |
| `targetCount` | How many before the objective completes. |
| `obtainMode` | Only meaningful for `Obtain` — see `ObtainObjectiveMode` for count-acquired vs currently-held semantics. |
| `description` | **Required, authored text** — this is what `QuestProgressSnapshot`/Quest UI shows the player; it is not auto-generated from `type`/`targetId` (Phase 6 gap-response fix). Leaving it empty produces a valid-but-useless UI string, not a validator error, so fill it in deliberately. |

**Placement:** wherever the project's quest asset folder convention lives for the owning system
(follow existing `QuestDefinition` assets' folder as precedent).

**Validation:** `ContentValidationRunner.ValidateQuestDefinitions` checks empty/duplicate `questId`,
prerequisite references, and (per `ContentValidation.md`) prerequisite cycles. Run content validation
after every quest change.

**Wiring for turn-in/offer, not code:** `QuestNpcInteractionService` reads `giverNpcId`/`turnInNpcId`
off the data — an NPC becomes a quest giver/turn-in target purely by an `npcId` string matching, no
per-quest code path.

**Making a new Tutorial Quest end-to-end (no manager code):**
1. Create the `QuestDefinition` asset, set `questId`, `isTutorialQuest = true`, objectives + authored
   `description` per objective, `rewards`, `giverNpcId`/`turnInNpcId` matching an existing NPC's
   `npcId`.
2. Add it to whatever quest catalog/registry the scene's `QuestManager` reads from.
3. Run Content Validation.
4. Enter Play in DemoScene, talk to the giver NPC, verify it becomes `Available` → `Active` →
   objectives progress on the matching domain event → `ReadyToTurnIn` → turn in grants reward exactly
   once.
5. Save, reload, confirm the quest's progress round-trips (`QuestManager.RestoreState`).

## 3. Shop / Recipe authoring

### Shop

**Asset type:** `ShopDefinition` (`Assets/Scripts/Shop/ShopDefinition.cs`) — menu
`Game/Shop/Shop Definition`.

| Field | Rule |
|---|---|
| `shopId` | Stable ID, e.g. `shop.town.blacksmith`. |
| `npcId` | Stable `npcId` that owns this shop — `ShopNpcInteractionService` matches on this, not a scene reference. |
| `stock` | Array of `ShopStockEntry` (item + price). Every stock `itemId` must resolve through the item catalog — the validator checks this against known item IDs. |
| `sellPriceMultiplier` | 0–1, applied to a stock entry's own price when the player sells that item back. Selling is only supported for items that are in this shop's own stock list (see `ShopManager.TrySell` remarks) — that's a deliberate scope limit, not a bug to author around. |

There is no separate shop runtime/save state in this phase — stock never depletes, so nothing beyond
the definition is needed for a new shop.

### Recipe

**Asset type:** `RecipeDefinition` (`Assets/Scripts/Crafting/RecipeDefinition.cs`) — menu
`Game/Crafting/Recipe Definition`.

| Field | Rule |
|---|---|
| `recipeId` | Stable ID, e.g. `recipe.weapon.sword.iron`. |
| `ingredients` | Array of `RecipeIngredientEntry` (`itemId` + quantity). All must resolve. |
| `outputItemId` / `outputQuantity` | Must resolve to a known item. |
| `requiredStationTag` | Empty = craftable anywhere. Otherwise must match the `stationTag` passed to `CraftingManager.TryCraft` (e.g. `station.forge`) — station gating is data (a string tag), not a new code path per station. |
| `npcId` | Optional — stable `npcId` that offers this recipe as a Crafting capability. |

`CraftingManager` is one shared transaction engine for every recipe — adding a recipe is purely
authoring a new `RecipeDefinition` asset and registering it in the catalog the scene's
`CraftingManager` reads from; it never means adding a method.

**Quest integration:** Purchase/Craft events already fire generic domain events
(`ItemPurchased`/`ItemCrafted`) that `QuestObjectiveMatchers` listens to — a Purchase/Craft quest
objective just needs its `targetId` to match the relevant `itemId`; no shop/recipe-side wiring is
needed to make it quest-trackable.

**Validation:** `ContentValidationRunner.ValidateShopDefinitions` / `ValidateRecipeDefinitions` check
empty/duplicate IDs and that every referenced `itemId` resolves against the known item set.

## 4. Persistent world entity authoring

Covers chests, unique pickups, bosses, resource nodes — anything implementing
`IPersistentWorldObject` (`WorldObjectKind`: `Chest` / `UniquePickup` / `Boss` / `ResourceNode`).
**Ordinary enemies are never given a `persistentId`** — they respawn by simply always being present
in the scene; do not add a persistent-object component to a regular enemy prefab instance.

**Definition ID vs persistent instance ID:** an enemy *type* (e.g. `enemy.slime.green`) is shared by
every instance of that enemy. A **persistent world object's ID is per placed instance** — e.g. two
chests in the same scene need two different `persistentId`s (`world.chest.forest.01`,
`world.chest.forest.02`), even though they might use the same prefab.

**Steps to place a new persistent chest (concretely, no code):**
1. Instantiate/drag the chest prefab into the target scene via Unity MCP (`manage_gameobject` /
   `manage_prefabs`), positioned where it should sit.
2. On its `ChestInteractable` component, set `_persistentId` to a new, scene-unique ID (e.g.
   `world.chest.<area>.<NN>`), `_rewardItemId` to a valid item, `_rewardQuantity`.
3. Add the new `ChestInteractable` GameObject to the scene's `WorldObjectRegistry`'s `_entries` array
   via the Inspector (Unity MCP `manage_components`) — the registry **only knows about objects
   explicitly listed here**; it never uses `Find`/`FindObjectsByType` to discover them, so a chest
   left off the list is invisible to save/restore even though it's present in the scene.
4. Run **Tools/Project Game/Validate Content** — `ValidatePersistentWorldObjects` (scene-scope, uses
   `FindObjectsByType`, unlike the registry itself) checks for duplicate `persistentId`s across the
   scene and flags any persistent object not registered in a `WorldObjectRegistry`.
5. Enter Play, open the chest, save, reload — confirm `opened` state round-trips
   (`WorldObjectRegistry.RestoreState` is idempotent and restore never re-grants the reward or fires
   `WorldDomainEvents.WorldObjectChanged` a second time).

The same pattern (component + `persistentId` + registry entry) applies to `UniquePickupInteractable`,
`BossDefeatTracker`, `ResourceNodeInteractable` — see their source files for their specific reward/
respawn fields. `ResourceNodeInteractable` additionally has a respawn cooldown
(`nextRespawnUtcTicks`), authored as a duration on the component, not a fixed timestamp.

**Respawn-by-rule vs persistent-by-instance:** ordinary enemies and resource nodes without a tracked
cooldown respawn "by rule" (always present when the scene loads, no save record). A `ResourceNode`
with a cooldown, or a `Boss`/unique `Chest`/`UniquePickup`, is "persistent-by-instance" (its exact
state is a save record, keyed by `persistentId`). Choose the simplest option that satisfies the
design — don't give an object a `persistentId` unless its individual state genuinely needs to survive
save/load.

**Portability checklist** before assuming a persistent-object prefab can move to another scene: see
[DemoSceneWorkflow.md § Scene portability checklist](DemoSceneWorkflow.md#scene-portability-checklist)
— in particular, confirm the new scene has its own `WorldObjectRegistry` with this instance listed;
registration does not carry over automatically with the prefab.

**Validation:** an unknown `persistentId` found in a save at restore time is skipped with a warning
(`missingIds`), never thrown — this lets old saves survive removed/renamed world content. A duplicate
`persistentId` within one scene keeps the first registered entry and logs an error
(`WorldObjectRegistry.Register`) — always fix this before shipping, don't rely on the fallback.

## 5. Area / Spawn authoring

**Area IDs** (`areaId`, e.g. `area.town`, `area.tutorial`) identify a logical zone for player
location save data (`PlayerLocationSaveData.areaId`) and quest/objective area gating
(`QuestObjectiveDefinition.targetAreaId`). They are plain strings authored wherever the zone's trigger
or spawn data lives — there is no separate `AreaDefinition` asset type in this phase; consistency of
the string across quest data, spawn data, and any area-trigger component is the designer's
responsibility (the content validator does not currently cross-check `areaId` strings against a
canonical area list — treat this as a manual-discipline requirement, not an automated one).

**Spawn IDs** (`spawnId`, e.g. `spawn.town.entrance`) are resolved to a world position by the scene's
`SpawnRegistry` (`Assets/Scripts/GameManagers/SpawnRegistry.cs`) — a scene service, bound explicitly
via Inspector `Entry[]` (`spawnId` + `Transform`), exactly like `WorldObjectRegistry`.

**To add a new spawn point:**
1. Create/position an empty `Transform` at the desired location in the scene.
2. Add an entry to the scene's `SpawnRegistry._entries` (Unity MCP `manage_components`): `spawnId`
   (new, scene-unique string) + the `Transform` reference.
3. If this spawn is meant to be a save's fallback spawn for an area (e.g. what `PlayerSpawnReadinessSource`
   uses when the saved position can't be resolved), reference this exact `spawnId` string from the
   relevant `PlayerLocationSaveData.fallbackSpawnId` / `NewGameFactory` constant — this is a backend
   constant (`NewGameFactory.TutorialStartSpawnId`), so wiring a *new* default fallback (not just an
   additional named spawn point) is a backend change, not authoring.

**Never use `GameObject` name or hierarchy position as save identity** for either an area or a spawn
— always the explicit `spawnId`/`areaId` string field. `SpawnRegistry.TryGetSpawn` returns false
(caller falls back, never throws) when a `spawnId` isn't found — this is the recovery path for typos;
don't rely on it silently, verify the exact string matches.

## 6. Scene integration

Full detail: [DemoSceneWorkflow.md](DemoSceneWorkflow.md). Summary for this guide's purpose:

- **`DemoScene` is the integration playground**, not a production scene — build and prove new content
  there first (happy path, failure/cancel path, save/load round-trip, portability) before promoting
  prefabs/catalogs to a production scene.
- **Prefab/installer portability**: a feature is only "done" when its prefab can be dragged into a
  fresh minimal scene, given the required scene-service dependencies (its own `SpawnRegistry`/
  `WorldObjectRegistry`/catalog references), and still function — see the full
  [portability checklist](DemoSceneWorkflow.md#scene-portability-checklist).
- **`SceneContext` dependencies**: distinguish application-scope services (`GameStateManager`,
  `GameSessionManager`, `SceneFlowService` — `DontDestroyOnLoad`, exist across every scene) from
  scene-scope services (`SpawnRegistry`, `WorldObjectRegistry`, `MapManager`'s Tilemap references —
  rebuilt per scene, bound by that scene's installer). A persistent (`DontDestroyOnLoad`) manager must
  never hold a direct reference to a scene-scope object past that scene's unload —
  `GameplaySceneLifetime.ReleaseForSceneExit()` runs before every scene load specifically to prevent
  this kind of leak (Phase 9 fix).
- **Service lifecycle**: see [ServiceOwnershipLifecycle.md](ServiceOwnershipLifecycle.md) for the
  application/scene/feature service classification and who is allowed to outlive a scene reload.
- **Unity MCP authoring/validation workflow**: all scene/prefab/ScriptableObject edits go through
  Unity MCP tools (`manage_scene`, `manage_gameobject`, `manage_prefabs`, `manage_scriptable_object`,
  `manage_components`), never hand-edited `.unity`/`.prefab` YAML and never a runtime script that
  builds scene content procedurally at startup. After edits, run `manage_scene(action: "validate")` on
  the touched scene and **Tools/Project Game/Validate Content** before considering the change done.
- **DigitalDisco SDF v3 font requirement**: every TMP (`TextMeshProUGUI`/`TextMeshPro`) element added
  to any scene or prefab in this project must use the `DigitalDisco SDF v3` font asset
  (`Assets/Fonts/DigitalDisco SDF v3.asset`) — this is a project-wide visual consistency requirement,
  not a per-feature choice; check the Font Asset field on any new TMP component before considering a
  UI addition complete.

## 7. Dialogue content

1. Tạo `DialogueDefinition` bằng menu `Project Game 2D/Dialogue/Dialogue Definition`.
2. Đặt `dialogueId` ổn định theo dạng `dialogue.<area>.<npc>.<context>`; display text có thể đổi nhưng
   ID không đổi sau khi content được phát hành.
3. Mỗi node cần `nodeId` duy nhất trong asset, speaker name, body text, portrait tùy chọn và một trong:
   `nextNodeId`, danh sách choices, hoặc node kết thúc có `outcomeId`.
4. Choice chỉ điều hướng đến node trong cùng definition. Quest/shop/crafting effect không được nhét
   trực tiếp vào node; `DialogueUI` trả outcome cho capability/service phù hợp.
5. Bind definition vào component tương tác NPC. UI dùng prefab
   `Assets/Prefabs/UI/DialogueUI.prefab`; không copy một Canvas riêng cho từng NPC.
6. Chạy validation/test graph trước khi test DemoScene; missing/duplicate node ID là lỗi content.

Demo hiện có `dialogue.town.elder.greeting` tại `Resources/Dialogue/TownElderGreeting.asset`, được
bind vào `TownElderNPC.prefab`. Hoàn tất node cuối phát `conversation.completed`, sau đó capability
quest hiện tại quyết định offer/turn-in; dialogue asset không sửa `QuestManager`.

## 8. Checklist for "can a content designer make a new Tutorial Quest without touching manager code?"

This is the Phase 10 content-ready bar. Concretely, all of the following must be true:

- [ ] A new `QuestDefinition` asset can be created, filled in, and registered in the quest catalog
      entirely through the Inspector/Unity MCP — no `QuestManager`/`QuestNpcInteractionService` edit.
- [ ] A new item/reward referenced by that quest can be created as an `ItemSO` under
      `Resources/Items/...` and resolves automatically through `IItemResolver` — no resolver code
      change.
- [ ] The quest's giver/turn-in NPC is wired purely by matching `npcId` strings — no NPC-specific
      code branch.
- [ ] Objective `description` is authored text, not derived — the designer must type it themselves
      per objective.
- [ ] Content Validation and the DemoScene validator both pass clean after the change.
- [ ] The quest can be completed end-to-end in DemoScene and its progress survives a save/reload.

If any of these requires a manager-core edit, that gap is a backend issue, not a documentation gap —
report it rather than routing around it in an authoring guide.
