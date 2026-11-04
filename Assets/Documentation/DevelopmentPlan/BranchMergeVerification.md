# SuperaAI / upstream main integration — 2026-11-04

## Scope and approved reconciliation

Merge parents: SuperaAI `41cafd599` and upstream/main `556503653` (Duy PR #145).
The working tree was clean before merging. main local still tracks the separate havindev fork;
this integration does not repoint or publish any remote branch.

Owner selected main configuration for nine overlapping content assets and their meta files:
Grape/Lemon crop definitions, produce and seeds, Meat, Carrot Juice and Grape Juice.
Keep main GUIDs and complete FarmingCatalog/Cuthbert shop lists. All original stable item/crop
IDs are unchanged. Superseded SuperaAI GUIDs were scanned across tracked source/serialized files:
none remain after resolving the overlapping assets/catalog/shop. No save migration is needed.

SuperaAI Act I quests/dialogues/tutorial wiring and skill/shield/mana behavior are preserved.
PlayerStat retains both skill catalog and consumable buff state, combines regeneration logic,
and preserves its existing living/gameplay guard. Both branches' DemoScene additions and
documentation remain. MapDuy is taken intact from main.

## Verification

- No merge conflict markers in tracked C#, serialized assets, metadata, scenes, prefabs or docs.
- Nine canonical GUIDs each resolve to one meta file; no retired conflicting GUID remains.
- FarmingCatalog and Cuthbert shop GUID references resolve to existing assets.
- Item stable IDs are unique across Resources/Items.
- Grape/Lemon definitions yield exactly three, matching main and its existing EditMode cases.
- DemoScene/MapNhat/MapDuy serialized document IDs have no duplicates.
- MapDuy is byte-identical to upstream/main.
- Git whitespace check passes with Unity's inherited blank-value trailing spaces excluded.
- Unity refresh and compilation were requested through MCP. The request was accepted, but the
  Editor then crashed during native QuickSearch/LMDB indexing around assembly reload.
  Logs/Editor.log records a native segv and LMDBIndexStorage frames; this is not a confirmed
  C# compile pass by itself. Reopened the project in Unity 6000.5.4f1 successfully.
- Post-restart live Editor verification: isCompiling=false, isUpdating=false,
  scriptCompilationFailed=false; active scene Assets/Scenes/MapDuy.unity.
  MCP Console query returned zero error entries. Initial asset refresh completed successfully.
  This confirms recovery/compile, not a gameplay or build acceptance pass.

No automated tests, Play Mode transition or Player build was run in this merge operation.
Historical tests in SheetContentExpansion.md are evidence for the original branch, not a fresh
pass of this combined revision. Existing content validation/production acceptance gates remain
open as documented there; no roadmap feature milestone is advanced.
