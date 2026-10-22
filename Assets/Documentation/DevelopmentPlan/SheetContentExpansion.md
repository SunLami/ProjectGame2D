# Items List expansion — 2026-10-21

Owner-approved continuation after merge 0183b80a7. Source: Items List Google Sheet, all four tabs.

## Accepted scope

- Small/Large Health and Mana potions restore 15%/35% of current maximum. Four variants share a 10 second cooldown, started only after successful use. Full resource, invalid data, missing stock and death reject without consumption/cooldown.
- Buff reuse refreshes its duration, never stacks. Health food: +20% MaxHealth and 1% current MaxHealth/sec for 90 seconds. Mana drink: analogous mana effect. Stamina drink: +25% MaxStamina and +25% stamina regeneration for 120 seconds.
- Buff pause/death/teleport/save/load special policies are deferred by owner. Use ordinary scaled runtime time and no buff/cooldown DTO; do not invent persistence/migration. Expiry clamps current resource to the unbuffed maximum. No immediate healing is granted by a cap increase.
- Harvest Stew: 2 Carrot + 2 Mushroom + 1 Cucumber + 1 Chillie.
- Orchard Elixir: 2 Grape + 1 Apple + 1 Lemon + 1 Cucumber.
- Trailrunner Tonic: 2 Apple + 1 Lemon + 1 Carrot + 1 Chillie.
- Seven sheet crops mature after 60 seconds and yield exactly 3 produce. Apple/Lemon harvest once, then plot becomes empty. Preserve Eggplant and its existing IDs.
- Owner explicitly permits new stable IDs using item.*, crop.*, recipe.*, resource.* convention. Unknown price/NPC/station stays unconfigured; zero price is unset, never an invented free stock entry. NPCs explicitly in sheet use existing stable identities.
- Keep legacy IDs, Clockmaker -> Copper Helm, metal -> Bar, Coal at Merric; ignore three incomplete joke fish rows.
- New world content is authored/tested in DemoScene first. Existing production layout is not a source of safe new spawn positions: promotion uses explicit reusable assets after gates pass.
- Art absent from sheet uses clearly documented provisional existing sprites, no claim of final art.

## Dependencies and verification

Fix fish purchase transaction before new commerce; validate IDs/references; expand non-equipment crafting without changing existing equipment categories; add definitions, recipes, crop catalog and node/animal loot; test consumables, capacity failures, fish metadata, harvest counts and V13 inventory resolution. No save schema change for definitions/runtime-only buffs.

## Current status — 2026-10-22

Code/data and DemoScene prototype implemented. Verified 15/15 focused EditMode cases, 40/40 official PlayMode regression cases, and an independent 9/9 direct live PlayMode smoke pass. The latest source compiled successfully in Unity. Production world placement, final art and independent Player build/runtime acceptance remain incomplete; see remaining gates below.

## Paused at owner request — 2026-10-21

Owner requested safe stop; no further implementation until resumed.

Saved work: D-119 and this contract; ItemSO percent/buff fields; transient ConsumableBuffState; PlayerStat buff integration; ConsumableUseHandler shared 10s cooldown; fish purchase path; non-equipment crafting groups; crop-builder catalog preservation; WildlifeDefinition/Actor, ActorLootDrop/DroppedLootPickup, legacy Enemy death event; SheetContentAuthoring editor script (not executed).

IMPORTANT: implementation is incomplete and unverified. Last MCP console contained CS0246 for ConsumableBuffState during import; its source and .meta exist, but no clean compile has been confirmed after forced refresh. Unity MCP subsequently reported no_unity_session. No BuildAssets invocation or new scene/prefab/content asset generation was performed. No PlayMode/tests/build/promotion, commit or push was performed.

Resume: inspect git diff and Unity instance/version, refresh/import all new scripts and resolve compilation; review transaction/event atomicity and wildlife movement bounds; complete idempotent authoring and content validation (unknown price/station/NPC remains unset); add focused tests, author in DemoScene and pass integration/portability before production promotion. Existing art references in authoring are provisional, not final art acceptance. Review new folders/metas and synchronize README/Roadmap/Farming/Resource/Fishing docs with actual delivered state.

User decisions: shared potion cooldown 10s; stable IDs for new content explicitly authorized; percentage restoration 15/35; buff refresh; special buff lifecycle deferred; all new sheet crops 60s/single harvest; select four-produce recipe names/ingredients autonomously; ignore three joke fish. No additional user answer is pending.

## Resumed — 2026-10-22

Unity 6000.5.4f1 reconnected and reported scriptCompilationFailed=false. BuildAssets and InstallDemoContent completed via MCP. Fifteen focused EditMode cases passed (job 5e24d1565946401fafd6387ca0535781). First PlayMode runner failed to initialize before any test (job 817a55671ed14a89974ff75e498b8990); this is not a test pass/failure of gameplay.

Initial full validator: 1141 errors / 55 warnings. Of these, 29 are intentionally missing prices on the newly expanded shop lists. Explicit commerceConfigurationPending metadata distinguishes owner-approved unfinished economy authoring (warning/unavailable) from accidental missing prices. The remaining 1112 are baseline defects: 1089 null RockTileData tiles, 9 old item icons missing, 13 duplicate Merric sell references and one pre-existing DemoScene WoodTree002 empty persistentId. RockTileData, old affected items and Merric shop were verified byte-equivalent to HEAD after newline normalization. No blanket validator disable or unrelated asset repair is authorized by this slice. Production world placement/promotion is gated by this baseline report.

## Final implementation and evidence — 2026-10-22

- 29 new item assets (31 configured including existing Carrot crop/seed), 6 new crop definitions, 12 new recipes, 2 herb node definitions, 7 node prefabs and 3 wildlife definitions/prefabs. Existing Eggplant, 55 equipment IDs/recipes, ore/bar/gem identities and three Orbs remain.
- Current FarmingCatalog has eight crops; all seven sheet crops are 60s/exactly-three, single harvest. New stable item IDs use item.crop.*, item.seed.*, item.material.*, item.consumable.* and item.drink.* conventions.
- Definition-only authoring stays separate from transient buff/cooldown state. Inventory/Shop purchase commits gold and fish metadata before events. V13 is unchanged; no personal save file was written by verification.
- Official EditMode job `5e24d1565946401fafd6387ca0535781`: 15/15 passed.
- Direct live PlayMode smoke: 9/9 passed; evidence `Verification/SheetContent_PlayMode_20261022.json`. Explicit Start method only; pending flag was observed false after completion. The verifier exited Play Mode itself.
- Official PlayMode job `98d183dda3e64010885d659f7b3b41fe`: 40/40 passed, 0 skipped/failed. Covers SheetContent, ShopManager (including real fish-shop transaction), InventoryManager, CraftingManager, FarmingSystem and QuickBarManager fixtures.
- First PlayMode runner initialization timed out before any test (`817a55671ed14a89974ff75e498b8990`); superseded by the successful 40-case run, not counted as a gameplay failure.
- Source and DemoScene assets were authored through Unity API/MCP. Scene View evidence: `Captures/SheetContent/DemoContent.png` (Editor prototype, not production art acceptance).
- Removed 13 duplicate Merric sell references without changing the unique offered set. Restored matching CopperOre/MedicinalLeaf/WoodLog placeholder icons; six old food/drink/buff items without icons received a provisional existing potion icon. Assigned previously-empty DemoScene WoodTree002 ID `world.resource.demo.wood.002` and registered it; no previously valid ID renamed.
- Final full validator: **1089 errors / 84 warnings / 278 checked assets**. All errors are the unchanged `Assets/Resources/TileDatas/RockTileData.asset`: all 1089 tile references resolve null. It has zero valid entries, so dropping nulls would leave an invalid empty surface definition. Correct source tiles must be identified before repairing that asset. Warnings: 55 accepted legacy IDs and 29 intentionally unset commerce prices. No new-content validation errors remain.
- Windows DemoScene smoke build (`build-a3b4d8ac4b`) subsequently completed: recheck on 2026-10-22 found `Logs/Editor.log` reporting `Build Finished, Result: Success` and `[MCP Build] Build succeeded: StandaloneWindows64`, with executable `Builds/SheetContentSmoke/ProjectGame2D.exe` present (667648 bytes, modified 11:03:27 local time). Build took 1662.2s. This confirms build completion, not a physical Player smoke pass. Additional EditMode rerun request timed out; retain the successful 15-case and 40-case results above. MCP recheck still returned zero Unity instances / no_unity_session, so no fresh live Editor tests were possible.

## Remaining gates and known limits

1. Rebind RockTileData to its correct tile assets, then re-run global validation. No guessed replacement list and no weakening of required tile validation.
2. Replace provisional art: new crop visuals reuse Carrot stages; new icons reuse a valid crop icon; ore prefabs reuse Copper visuals; wildlife currently uses Pig art for Rabbit/Deer/Boar. Gameplay definitions/IDs are distinct; these are functional prototypes, not final visuals. Existing resource sprites/atlas import GUIDs were preserved.
3. Configure owner-deferred economy/NPC/station data. Unknown-priced stock displays unavailable and rejects transactions. Unassigned recipes exist in catalog and can be exercised through CraftingManager, but are not offered by an invented NPC.
4. Test production world placement/portability and player UX before installing node/animal instances in MapNhat. Shared catalog/item data is available there; no new production world layout was authored in this slice.
5. Run physical Windows Player smoke. Windows build completion is confirmed by Editor log and executable; interactive Player behavior remains unverified.
6. Buff special lifecycle behavior for pause/death/teleport/save-load remains explicitly deferred. Runtime follows normal scaled updates, no buff/cooldown save DTO. No extra death or teleport rule was introduced.

No commit or push was made. This report distinguishes implemented prototype/data work from remaining production/release acceptance.
