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

## SkillHUD presentation repair — 2026-10-16

Shared GameplayUIRoot prefab rebuilt by SkillHUDBuilder. Slots now use a hidden diamond Mask;
icons increase from 130 to 250 source units (Shield 170 to 300). Cooldown has a real diamond
sprite and disables at zero/empty rather than relying on fillAmount on a sprite-less Image.
Block highlight uses the diamond silhouette with 0.18 alpha. Key font sizes use the same 0.25
source-to-layout conversion as their rectangles; labels are raised within the baked key plaques.
Generated slot_diamond.png is reproducible via Build Skill HUD; no manually authored scene copies.
Verification: Unity compilation and prefab rebuild succeeded, Console error query returned zero.
Isolated Editor preview rendered populated slots, half cooldown and active Shield without square
spill. Capture: Captures/SkillHUD/presentation_review.png (enlarged review, not Game View acceptance).
Play Mode, 1920x1080 gameplay, production scene and Player build acceptance remain owner/manual
checks; no automated tests or Play Mode transitions performed, per the existing owner workflow.

### Interior alignment follow-up — 2026-10-16

Owner screenshot exposed that the previous repair still positioned icons below the frame center.
Correct loadout centers in source pixels: (183,230), (457.5,230), (732,230), (1007,230);
previous y=253.5 was incorrect, as were Q/T x positions. Interior mask/icon diameter is now
210 source units, Shield 260, superseding the 250/300 sizing above. Key labels follow corrected
x centers. Isolated preview with Water bolt/vortex/beam, Earth meteor and Shield visually checked:
icons centered with visible dark inset before ornaments. Capture: Captures/SkillHUD/interior_alignment.png.
Prefab rebuilt successfully; no Play Mode transition or progression/save mutation. Gameplay acceptance pending.

### V2 art and complete-icon inset — 2026-10-16

Owner requested smaller complete motifs and a regenerated distinctive set with backgrounds, plus
shader-matched Shield. 12 catalog icons now reference V2; panel source and builder synchronized.
SkillHUDIcon material fits the entire generated image in 48% of mask diameter, leaving foreground
inside the inscribed square; dark backdrop continues to the diamond edges. Overlay/mask geometry
remains independent. V1 retained. See SkillIconV2.md for prompts, imports and verified Editor renders.

### Shared icon layout — 2026-10-17

The 48%-square inset is superseded by independent foreground/background layers shared across
HUD, library and loadout. Foreground sizing follows alpha hull inside fixed diamond/hexagon axes;
drag ghost uses the same transparent sprite. Catalog identity/costs/learning/save unchanged.
Source prefabs and authoring builders synchronized. Isolated Editor render verification and
remaining owner acceptance: SkillIconSharedPresentation.md.

## Skill Point budget — owner request 2026-10-22 (D-116 continuation)

Owner replaces unlimited level awards with a catalog-derived learning budget: current twelve skills
cost 3 × (2 + 3 + 4 + 5) = 42 points total. Keep +1 per gained level, but available points never
exceed the total cost of known unlearned skills. Spending does not reopen award capacity beyond
that remaining cost. At all skills learned, points remain zero even on subsequent level-ups.
PlayerStat binds the serialized catalog (or the existing Resources catalog once at composition);
missing catalog fails closed with zero award budget. UI continues reading player-owned state.
Restore clamps legacy surplus in runtime only, preserves learned/unknown IDs and loadout, never
grants points and never writes the source DTO/file. Save V13 fields and migration remain unchanged;
normal explicit saves capture the normalized balance. No automatic test runs or Play Mode transitions
per existing owner workflow. Regression source added for cap, spending, legacy surplus and all learned;
manual gameplay/build acceptance pending.

## D-118 mana and cooldown continuation — Accepted, owner request 2026-10-22

Supersedes the earlier no-mana scope and aim-start cooldown timing. All twelve skills use catalog
manaCost and cooldownSeconds. Initial tier 1/2/3/4 tuning for Water/Earth/Wind: mana 8/16/24/35,
cooldown 2/6/10/18 seconds. Builder defaults and live catalog match; tooltip displays both values.
Aim entry checks mana and cooldown. Confirmation rechecks gameplay/alive/learned/equipped state,
consumes mana through PlayerStat.TryConsumeMana once and starts cooldown before spawning effects.
Cancel or missing prefab (no aim started) spends nothing; mana lost during aim prevents effects.
Existing slot-keyed cooldown and scaled gameplay clock retained per D-118; HUD reads same gate.
No new regen policy, save field, schema change or Shield change. Mana remains the existing runtime
resource; cooldown remains transient. Automatic tests/Play Mode remain owner-controlled.
Manual acceptance pending: each of twelve casts, insufficient mana, exact cost, cancel, pause,
missing prefab, loadout changes during aim, HUD overlay and PlayerHUD mana refresh.

### SkillHUD countdown text — owner request 2026-10-22, D-118 continuation

Four Q/E/R/T icons show centered white Digital Disco cooldown seconds, rounded up (18…1),
above the radial overlay with dark outline. Ready/empty slots hide numbers. Remaining time comes
from the same SkillTestHarness deadline as the cast gate and radial overlay, using scaled time.
Shared GameplayUIRoot prefab receives a targeted text-only upgrade; full builder also creates texts.
Shield, cooldown tuning and save remain unchanged. Prefab wiring statically checked; Unity MCP stopped answering after refresh, so compile/editor verification remains pending. Owner manual
gameplay and visual acceptance pending; no automatic tests or Play Mode transitions.

### Mana recovery and Shield guard — owner request 2026-10-22, D-118 continuation

Mana still spends only on confirmed casts. PlayerStat now regenerates 3 mana/second after
a 2-second delay from positive spending, only during living gameplay. T remains loadout slot 3;
key polling is independent and failed learned/mana/cooldown gates log a readable reason.
DemoScene ultimate prefab references are present; exact owner T failure still requires reproduction.
Shield supersedes the old 70% mitigation: full block, no HP loss/hurt animation/knockback/hurt sound
or hurt invulnerability on blocked hits. Holding drains existing 20 stamina/sec without passive
stamina regeneration; each blocked hit additionally drains 15 stamina. Last exhausting hit is blocked
in full, clamps stamina to zero and breaks guard; subsequent hits damage normally. Exhaustion locks
reactivation for 1 scaled second, including while RMB remains held; stamina can recover during lockout.
All resource/break rules live in PlayerStat; Player owns input, VFX and hurt routing. No save changes.
Initial tuning is serialized on PlayerStat. Owner manual acceptance remains pending; no automated
test run or Play Mode transition. Compile checks recorded separately after implementation.

Verification for mana/shield continuation: Unity compile completed with zero Console errors.
Read-only Editor inspection resolves Player, SkillCatalog and all three ultimate prefab references
on the active DemoScene bridge. No missing T ultimate reference found. All cast rejection gates now
log their reason when the key is pressed. No live T reproduction or gameplay acceptance is claimed.
Shield state/VFX are cleared when Player disables; cooldown deadline remains in PlayerStat.

### Shield single-hit break — owner request 2026-10-22 (supersedes stamina guard above)

Shield costs no stamina to activate, hold or absorb a hit, and can activate at zero stamina.
A blocked hit deals zero HP damage, immediately clears guard and starts a 5-second scaled
cooldown in PlayerStat; Player hides VFX immediately through the Blocked result path.
No hurt/knockback on that hit; following hits use normal damage. Holding RMB cannot bypass
the lockout; after expiry it may raise the shield again. Manual release has no hit cooldown.
Passive stamina recovery continues while holding. No save or schema changes.

HP investigation (read-only Editor inspection): active DemoScene Slime1/Slime2/Goblin/ForestGuardianBoss
attack profiles each author 20 raw damage. Player base HP 100, Defense 2, growth +3 HP/+0.15 Defense
per level. At level 101 without equipment this gives 400 max HP, 17 Defense, and 3 damage per hit
(0.75% HP). HUD fill correctly uses current/max HP. Equipment Defense/DamageReduction may lower
this further; 0.45-second hurt invulnerability can also suppress overlapping hits. No live high-level
loadout/HP measurement was performed. Damage formula/tuning unchanged; balance revision requires
a separate owner choice. Owner manual single-hit shield/gameplay/build acceptance pending.

### Shield HUD countdown — owner request 2026-10-22

ShieldSlot now includes the same centered white outlined Digital Disco countdown as Q/E/R/T.
SkillHUDController reads PlayerStat.ShieldCooldownRemaining directly (5…1, hidden at ready);
shared prefab and builder/targeted upgrade include the Shield text. No guard/resource/save changes.
Owner manual visual/gameplay acceptance pending; no automated tests or Play Mode transitions.

### Shield radial overlay — owner request 2026-10-22

Shield now mirrors the skill slots' 72%-black diamond Radial360 cooldown overlay. Controller uses
remaining / PlayerStat.ShieldCooldownSeconds from the same five-second guard deadline; countdown
text is last sibling above overlay for readability. Builder and targeted prefab upgrade synchronized.
No gameplay timing/save changes. Owner manual visual acceptance pending, no Play Mode/test run.

## MapNhat skill UI/casting and dash promotion — owner approved 2026-10-22

Owner accepted DemoScene presentation and requested production promotion (D-085/D-116/D-118).
Bootstrap owns the connected SkillPanel prefab; MapNhat contains one PlayerSkillCasting prefab instance,
with explicit local Frawand Player binding and shared SkillCatalog. New reusable casting prefab
Assets/Prefabs/Skills/PlayerSkillCasting.prefab carries all twelve behavior prefab configurations
from DemoScene, with no demo controls or scene Player reference baked into the source.
Shared SkillHUD already comes from Bootstrap's GameplayUIRoot prefab, including Shield radial/text
cooldowns; no duplicate HUD is added to MapNhat. SkillFX child promoted to local Player and
SkillFX/Shield renderers bound. Existing shared Player Animator and PlayerInput supply dash.
MapNhat Player dash allow-list and new-component default include DemoScene/MapNhat; tuning remains
3.2 units / 0.2s, 0.8s cooldown, 20 stamina and 0.12s initial invulnerability.
Learning, mana, shield, cooldown and save V13 use existing shared runtime contracts, no regrant/reset.
Editor validation: one Bootstrap panel, one MapNhat bridge, local Player/catalog and every skill prefab resolved, Dash action
exists, shared HUD count one. MapNhat stays active, source DemoScene read additively then closed
without saving it. Owner manual New Game/Continue, P, Q/E/R/T, Shield and Space/gamepad smoke
acceptance and Player build remain pending; no automated tests or Play Mode transitions performed.

### UI ownership correction — owner clarified 2026-10-22

SkillPanel and SkillHUD belong to persistent Bootstrap. SkillPanel prefab is instantiated once
in Bootstrap; MapNhat and DemoScene local copies are removed to avoid duplicate P input handlers.
MapNhat retains PlayerSkillCasting, local Player/FX binding and dash configuration. SkillPanel
already rebinds PlayerStat.Instance as sessions change and gates P to Playing/Skills state;
scene unloading does not own the shared UI lifetime. No save/gameplay tuning changes.
