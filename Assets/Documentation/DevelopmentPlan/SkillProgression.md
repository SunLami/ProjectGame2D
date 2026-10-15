# Skill progression and loadout — 2026-10-13

## Cursor hover tooltip — 2026-10-14

Tooltip is no longer opened by icon click. Hover a library/equipped icon to show its skill immediately
to the right of cursor (18 Canvas units), clamped inside Canvas with 12-unit inset. Follow only while
over the icon; freeze while pointer is inside tooltip so Learn remains clickable. Owner reverted the
fixed-on-entry revision to this behavior. On exit allow 0.2s
unscaled time to cross the gap, then hide when neither region is hovered. Hide on drag/close/disable;
hidden tooltips cannot submit Learn. Existing prefab references are reused, no asset/layout rebuild.
Owner performs manual testing; no tests or Play Mode changes for this presentation change.

## DemoScene casting continuation — 2026-10-14

Owner requests Q/E/R/T on the four loadout sockets, including when equipped. Catalog `castKind`
selects one of twelve existing behavior handlers; save continues storing stable skill IDs, not this enum.
SkillTestHarness now resolves slots through PlayerStat.Skills and explicit catalog before starting aim.
Requires learned/equipped/known skill, gameplay input allowed, living player and no current aim/ground
target. Mixed elements and any skill order are allowed. Tab kit switching is removed. Direct debug
Test Skill button is removed from DemoScene; its legacy callback is also routed through Q's gated slot.
Q/E/R/T start the existing aim/ground-target flow; click confirms as before. No mana cost, VFX tuning
or schema migration in this continuation. Icons are inset above permanently visible key labels.

2026-10-14 (D-118): per-skill cooldown added (`SkillCatalog.Definition.cooldownSeconds`, enforced in
`SkillTestHarness.TryCastLoadoutSlot`, keyed by loadout slot). Session-only, no save-schema change.
See D-118 for SkillHUD (bottom-right, visual cooldown) and Shield (hold RMB, unrelated to this
loadout/SkillCatalog system).
Connect DemoScene Loadout Casting editor menu wires the existing bridge and upgrades source prefab,
without rebuilding the panel or changing Play Mode. Owner performs tests; no automatic tests were run
for this change. Manual checks: empty slots inert, learned-but-not-equipped inert, mixed-element
assignments on each key, P/menu blocks casts, Tab no effect, key labels remain visible after drag.

Owner-authorized continuation of D-116. Read-only SkillCatalog asset contains stable IDs, English name,
description, cost and icon. PlayerStat owns runtime SkillProgressionState independently of panel lifetime.
No point rewards are tied to UI subscription. Actual AddExperience level-up advances an award watermark
and grants one point per level gained. Restore does not grant; New Game resets via player.skills DTO.

Save schema V13 adds PlayerSaveData.skills; capture uses PlayerSaveCapture, restore uses
PlayerSpawnReadinessSource after base level restore. V12 migration gives max(0,level-1) points once;
it does not rewrite disk until a real explicit save. Snapshots copy collections; negative points clamp,
duplicate/empty learned IDs normalize, assignments require learned IDs and are exactly four. Unknown
learned IDs remain preserved for recovery, but catalog-unresolved skills cannot be learned/equipped
and display empty/unavailable rather than inventing content. No refund or file overwrite on load.

Learn validates catalog identity, positive cost, sufficient points and not already learned atomically.
Equip validates slot/known/learned; moving the same skill clears its prior slot, replacement keeps
the old skill learned. These four skill slots never write QuickBarManager or Inventory items.
Drag ghost follows pointer in Canvas coordinates and never intercepts drop raycasts; unlearned skills
cannot begin drag. Closing/disabling/outside drop cancels without changing state. Slot-to-slot dragging
uses the same validation. UI labels and tooltip remain English.

Authoring: Connect Skill Progression editor menu upgrades existing prefab without rebuilding geometry.
Catalog asset: Assets/Resources/Skills/PlayerSkillCatalog.asset, referenced explicitly by prefab.
Add/change content through catalog and prefab library entry, not manager conditionals. IDs are explicit
constants when initially authored, never derived from filename/index at runtime or written as ordinals.
Catalog.Validate rejects duplicates/nonpositive costs/missing icons. Definition fields are read-only by
runtime convention; state never mutates them. Skill casting, mana costs and cooldowns are not altered.

Verification results are recorded below. Production promotion/build and skill casting integration remain separate.

## Verified 2026-10-13

- EditMode 46/46 passed (`3b173aa7ae4045c891e3f20b96000270`): SkillProgression, SkillPanel presentation,
  SaveMigration, PlayerHUD, FileSaveSlotRepository and NewGameFactory tests.
- DemoScene actual AddExperience next-level delta = +1; displayed points update through state event.
- Real Learn Button invocation: Water 1 costs 2, repeat learn does not spend; learned icon white and label Learned.
- BeginDrag/Drag/Drop/EndDrag handlers tested: unlearned blocked, screen ghost tracks pointer within 1px
  at 1920x1080, non-raycast ghost, library→slot and slot→slot move succeed. Outside cancel leaves
  loadout untouched; closing clears ghost. PlayerSaveCapture JSON round-trip retains learned/equipped state.
- Dedicated file repository fixtures under Captures/SkillPanel/Progression: V13 skill payload round-trip,
  raw V12 JSON backfill, untouched legacy file on read, explicit V13 write/reload without regrant passed.
  Unity JsonUtility synthesizes missing nested objects; V12 migration therefore initializes skills
  unconditionally (that field never existed in V12), not merely when null. Added raw JSON regression test.
- Screenshots: skill_learned_equipped_v13.png (1920x1080), skill_drag_ghost_v13.png (1280px preview)
  under Captures/SkillPanel/Progression. Final console error query: zero. No production save slot touched.

## Regression completed 2026-10-14

- PlayMode capture/restore/dirty regression 21/21 passed, job `106a096377bd43eabcd0a23701a49355`.
- Continue test now asserts learned skill and loadout survive restore without another point grant.
- Added dirty-tracking test: restore is suppressed; learning and slot assignment mark the session dirty.
- Initial combined run exposed Inventory fixture leakage (expected 2 starter items, captured 4).
  The same test passed in isolation. BuildFixtureWithInventory now explicitly resets the live singleton's
  inventory before each fixture; unchanged gameplay seeding and unchanged expected quantity. Combined
  regression then passed. Runner initialization timeout was retried, not reported as a code test failure.
- Owner takes over manual Unity testing from this point. No further automatic tests or Play Mode changes
  unless requested. Physical mouse/gamepad acceptance, Player build, production promotion and
  loadout-to-casting integration are not claimed.

Development SceneTravel also carries a copied skill DTO in PlayerTravelSnapshot, restoring without
rewards at arrival so unsaved learned/loadout state is not reset by per-scene PlayerStat recreation.
Dev panel Level Up uses AddExperience and grants points; Set Level is a silent restore/debug override,
not an award operation. Lowering a level never refunds/re-grants previously earned points.
