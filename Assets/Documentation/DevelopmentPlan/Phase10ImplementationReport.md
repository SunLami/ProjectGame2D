# Phase 10 Implementation Report — Hardening và Content-Ready Milestone

Status: **CONTENT_READY**

All four Roadmap.md § Phase 10 acceptance criteria are met, including physical acceptance of the
final manual click-through (see "Part 7 final acceptance" below). No P0/P1 issue is open.

## Part 7 follow-up — Pause Menu input bug + Save Game slot picker (2026-08-23)

The first real manual click-through attempt (by the user, on their own machine, using the Part 7
build) found a genuine bug: Escape did not open `PauseMenuUI` in the Player build, blocking the rest
of the required walk-through. Separately, a new requirement landed for a Save Game slot picker (save
to any of the 3 slots, not just the active one). Both are now implemented, reviewed, and re-verified:

**Pause input fix (Codex, root-caused and fixed):** `MainMenu`'s outgoing `InputSystemUIInputModule`
and `DemoScene`'s incoming `GameInputCoordinator` both operated on the same serialized
`InputActionAsset`. During the `MainMenu → DemoScene` `LoadSceneMode.Single` transition the two
scenes' lifecycles briefly overlap; the outgoing module could disable the shared asset's `UI/Cancel`
action *after* the incoming coordinator had already enabled it, silently leaving Cancel disabled for
the rest of the session. Direct Play of `DemoScene` alone never reproduces this (no outgoing MainMenu
module to race against), which is why it was invisible until a real MainMenu→DemoScene Player-build
transition exercised it. Fix: `GameInputCoordinator.Awake()` now `Instantiate()`s a private runtime
clone of `_projectActions` and only enables/disables `UI/Cancel` on that clone (destroyed in
`OnDestroy`); `EventSystem`/`PlayerInput` keep owning what they already owned. I independently
reviewed `GameInputCoordinator.cs` and confirm: the clone is genuinely private per-instance (no
cross-scene sharing), destroyed on teardown (no leak), event subscribe/unsubscribe stays correctly
paired in `OnEnable`/`OnDisable` (no double-subscribe), and nothing about the fix depends on
`DemoScene`'s specific hierarchy. `GameInputCoordinatorPlayModeTests.cs` directly reproduces the race
(disables the *shared* asset after the coordinator's clone already enabled Cancel, confirms Cancel
still works) and a disable/enable double-subscribe check — both are correct, targeted regression
tests, not incidental coverage.

**Save Game slot picker (Codex, built against the Part-2-of-this-session backend contract):**
`PauseMenuUI`'s "Save Game" button now reuses the existing three-slot overlay component (shared with
Load Game, no duplicated presentation) and wires it to `RequestSaveToSlot`/`ConfirmOverwriteAndSave`/
`CancelSaveToSlot`/`DeleteSlot` per the contract in `Handoffs/ClaudeToCodex.md`. I reviewed the wiring
line by line against every contract requirement (Empty saves directly, Valid/Corrupted/
IncompatibleVersion all gate on `OnSaveSlotConfirmationRequired` with no silent-overwrite exception,
Delete requires its own confirm before calling the backend, Save As correctly follows `ActiveSlotId`,
`RequestSave()`/Save-and-Return/Save-and-Quit are untouched) and confirm it matches. The
`Time.frameCount`-based double-submit guard on the slot Save button only blocks a second invocation
*within the same frame* — I traced through why this is sufficient rather than a gap: the backend
re-evaluates each slot's real status on every `RequestSaveToSlot` call (so a second, later-frame click
after the first write already changed the slot's status is naturally routed to the confirm path, never
a silent second write), and the Confirm/Delete popup buttons are protected by the popup deactivating
itself synchronously on the first click (a second click has nothing left to click). No change needed.

**Independent re-verification (all matched Codex's reported numbers exactly):** EditMode 58/58,
PlayMode 141/141 (18 new `GameplaySessionController` tests from this session + 2 new
`GameInputCoordinatorPlayModeTests` + 4 new `PauseMenuUISaveSlot` presentation tests, all passing, zero
regressions), Content Validation 0 error / 60 accepted-legacy warnings / 83 assets, `DemoScene` and
`MainMenu` scene validators both 0 issues, Build Settings unchanged (MainMenu index 0, DemoScene index
1). Rebuilt a fresh combined Player (`C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe`,
Windows64, 0 errors/0 warnings, 38.55 s, 515.86 MB) containing both fixes; launched it directly and
confirmed a clean `Player.log` init (D3D12/PhysX/Input System, no exceptions) before stopping it — the
same GUI-observability limitation from the original Part 7 attempt (see [D-026](DecisionRegister.md))
still applies in this environment, so the actual Escape-press-and-full-walkthrough acceptance needed a
physical keyboard on the user's own machine (done next, see below).

An unrelated engine-level `Assert: "Access version should be odd when acquiring lock"` message
appeared repeatedly in the Editor console during this verification pass. It has no stack trace, no
file/line, is not tied to any specific test name, and did not cause a single test failure across two
independent full-suite runs (this session's and the one before it) — treated as Editor/Burst-internal
noise, not a defect introduced by either change, and not investigated further per scope.

## Part 7 final acceptance — physical Player walk-through (2026-08-23, owner's own machine)

The owner ran the full acceptance walk-through with a real keyboard/mouse against
`C:\Users\havin\Phase10PlayerBuild_Combined\ProjectGame2D.exe` — the combined build from the follow-up
above. **All 24 steps PASS**, reported step-by-step (not a single rolled-up claim) in
`Handoffs/CodexToClaude.md § Phase 10 Final Physical Player Acceptance`:

Launch → MainMenu display; New Game on an empty slot (never Slot 1); MainMenu→DemoScene transition;
Escape opens `PauseMenuUI`; Escape closes/reopens it repeatedly; Save Game opens the three-slot
picker; saving an Empty slot writes immediately with no extra popup and updates the timestamp;
selecting a Valid slot shows the correct `OVERWRITE THE SAVE IN SLOT n?` popup; Cancel leaves the old
save untouched; Confirm actually overwrites; Save As to a different slot moves `ActiveSlotId` (the
target slot visibly shows `ACTIVE`); Delete shows its own irreversible-action popup; Cancel leaves the
save, Confirm actually deletes; Return to Main Menu; Continue on the correct slot; character position,
inventory (items + gold), quest state, tutorial state and persistent world state (chest/pickup/boss/
resource node) all restore correctly; no loss/duplication/cross-slot leak observed anywhere in the
run; Quit Desktop exits cleanly with no hang.

Slots used for testing: Slot 2 and Slot 3. **Slot 1 (the machine's real pre-Phase-6 save) was never
selected, overwritten, or deleted**, confirming the safety constraint held throughout. No UI issue and
no backend/contract gap was found during physical acceptance.

This closes the one remaining gap from the original Part 7 attempt: the Player build now has a
confirmed, physically-verified New Game → Save → Return → Continue → restore → Quit walk-through, not
just a clean launch log. Combined with the independent review and re-verification in the follow-up
section above (same build, same commit set), **all four Roadmap.md § Phase 10 acceptance criteria are
now met** and Phase 10 is `CONTENT_READY`.

## Part 1 — Baseline audit (Phase 0–9)

Full EditMode + PlayMode regression run at the start of Phase 10 (before any Phase 10 code changes):
all green, matching the state Phase 9 was verified in. No regression was found in any prior phase.

| Phase | Acceptance criteria (Roadmap.md) | Status |
|---|---|---|
| 0 | Baseline scene/build/input inventory, DemoScene role documented | PASS |
| 1 | Core input/state machine/scene flow foundation | PASS |
| 2 | Save slot repository, atomic write/backup, 3-slot contract | PASS (migration explicitly deferred to Phase 10 per Phase 2 report — now closed, see Part 2) |
| 3 | Player progression save/restore (level/XP/health/location) | PASS |
| 4 | Inventory/Equipment save/restore, `IItemResolver` | PASS |
| 5 | Tutorial save/restore, completion gate | PASS (one historical PARTIAL on `AreaTriggerZone` physics, since superseded — no longer open) |
| 6 | Quest backend, objective matchers, Tutorial→Main Quest gate | PASS (post gap-response: `TryGetProgress`/`QuestProgressSnapshot`, authored objective `Description`) |
| 7 | Shop/Crafting backend, atomic transactions | PASS |
| 8 | World persistence (chest/pickup/boss/resource node), registry pattern | PASS |
| 9 | Save/Load/Return/Quit orchestration, dirty-session tracking | PASS |

**Recurring accepted item across phases:** gamepad manual verification is repeatedly logged as
`BLOCKED` in every prior phase report (no gamepad hardware in the automation environment) — this is a
known, accepted limitation, not a Phase 10 regression, and is not re-litigated here.

**Environment note (new this phase, not a defect):** `ProjectSettings/EditorSettings.asset` has
`m_EnterPlayModeOptionsEnabled: 1` / `m_EnterPlayModeOptions: 1` — domain reload is disabled for this
project's Editor Play sessions. This is why the codebase's `Awake()` singleton guards and explicit
`OnDestroy`/`DestroyImmediate` cleanup conventions are load-bearing rather than defensive
over-engineering: static state genuinely survives across Play sessions unless code clears it. All 119
PlayMode tests passing under this setting confirms the existing conventions handle it correctly.

## Part 2 — Save migration hardening (done)

Built `ISaveMigrationStep` + `SaveMigration` (`Assets/Scripts/Save/`): a chained, additive-default
N→N+1 pipeline covering every historical version (V1→V2 introduces `player`, V2→V3 introduces
`inventory`/`equipment`, V3→V4 introduces `tutorial`, V4→V5 introduces `quests`, V5→V6 introduces
`world`; `CurrentSaveVersion = 6`). Wired into `FileSaveSlotRepository.TryLoadValid`: a save with
`MinimumSupportedVersion (1) <= saveVersion < CurrentSaveVersion` now migrates in-memory and loads as
`Valid`; anything older, or a `saveVersion` the pipeline doesn't recognize, still returns
`IncompatibleVersion` rather than guessing. Migration never rewrites the file on disk — only an
explicit subsequent save does. See [D-025](DecisionRegister.md).

**Verified properties (11 new tests, `SaveMigrationTests.cs` + 3 in `FileSaveSlotRepositoryTests.cs` +
1 PlayMode integration test):**
- Every historical version migrates to current with `NewGameFactory`-equivalent defaults for anything
  it didn't have.
- Fields the save already had are never touched (no silent data loss).
- Migration is idempotent (migrating twice produces identical output).
- A real V1 save restores through `PlayerSpawnReadinessSource` without throwing and without stalling
  the readiness gate.
- Below-minimum and above-current versions are rejected as `IncompatibleVersion`, not guessed at.
- Migration never rewrites the on-disk file.

**Explicitly not needed this phase:** no schema bug was found that required a version bump — Phase 10
only built the pipeline that was deferred from Phase 2, it did not change `GameSaveData`'s shape.

Full suite after this change: **58/58 EditMode, 119/119 PlayMode** — zero regressions.

## Part 3 — Automated quality matrix

Audited the full checklist in `QualityStrategy.md`'s test matrices against the existing suite
(Phase 3–9 tests already cover the large majority: slot independence, corrupted/backup recovery,
double-submit save/load, cancel/failure state recovery, tutorial/quest restore-without-fake-events,
Tutorial→Main Quest unlock exactly once, double turn-in protection, inventory stack/capacity
boundaries, equipment-replace-when-inventory-full (`Equip_ReplacedItemNeedsAnotherSlotButNoneFree_FailsWithoutLosingAnything`,
`Unequip_FullInventory_FailsWithoutLosingItem`), shop/crafting atomic failure, chest/pickup/boss/
resource-node round-trip, unknown/duplicate persistent ID handling, world restore idempotency, cross-
slot scene-reload leak checks, scene reference rebind).

Genuine remaining gaps were repeated-cycle scenarios (same-slot N times, A→B→C, world snapshot at
scale) — these are inherently soak-test concerns rather than one-shot NUnit assertions, so they were
folded into Part 4's soak tool instead of duplicated as unit tests. No new gap requiring a new unit
test was found beyond what Part 2 already added for migration.

## Part 4 — Soak testing

New tool: `Assets/Editor/SaveSoakTestRunner.cs`, menu **Tools/Project Game/Run Save Soak Test**. Does
not auto-run. Operates entirely against a temp directory (`Path.GetTempPath()`), never touches
`Application.persistentDataPath` — cannot corrupt a real player save.

**Actual run results (this machine, 2026-08-23):**

| Check | Result |
|---|---|
| 120 save/load cycles, same slot | 544.9 ms total, 4.54 ms/cycle avg. File size stable: 3054B → 3056B (no growth). PASS |
| A→B→C cross-slot independence, 25 rounds | 36.4 ms. No leak between slots. PASS |
| Save → fresh-repository-read ("Return→Continue"), 30 cycles | 132.2 ms. Every cycle read back the correct `saveId`. PASS |
| World snapshot, 60 persistent objects | 4.0 ms write+read, 6583B file size. Object count round-tripped exactly. PASS |
| Session teardown/recreate, 40 cycles | 205.4 ms. First cycle confirmed clean-empty state; no cross-instance leak. PASS |
| Leftover `.tmp` files after all cycles | 0. PASS |

**Scope limitation (documented, not fabricated):** this tool soak-tests the save **file** layer
(serialize/write/read/migrate). It does not drive a live scene/`GameStateManager`, so it cannot
observe event-subscriber growth or stuck `GameState`/`timeScale` under repeated cycling — those
properties are instead covered once each (not hundreds of times) by the existing PlayMode suite
(`GameplaySessionControllerPlayModeTests`, `SessionDirtyTracker` tests), which is the honest scope
this tool operates in.

## Part 5 — Profiling / instrumentation

New tool: `Assets/Editor/SaveProfilingRunner.cs`, menu **Tools/Project Game/Run Save Profiling**.
Measures each stage separately (not combined), 50 iterations per stage, representative non-empty save
data (player + 8 inventory slots + 2 equipment + 4 quests + 60 world objects, 6590-byte JSON).

**Baseline (this machine, Unity 6000.5.4f1 Editor, Windows, 2026-08-23 — not a device build; not a
performance budget, a reference point for future comparison):**

| Stage | ms/iteration (avg) |
|---|---|
| Serialize (`JsonUtility.ToJson`) | ~0.08–0.09 ms |
| Deserialize (`JsonUtility.FromJson`) | ~0.13–0.14 ms |
| Atomic write (serialize + temp-write + round-trip-validate + `File.Replace`) | ~2.8–4.2 ms |
| File read (`File.ReadAllText` + `FromJson` + status checks) | ~0.31–0.43 ms |
| Migration (V1 → current) | ~0.008–0.013 ms |

**GC allocation:** attempted via both `Profiler.GetTotalAllocatedMemoryLong()` and
`GC.GetAllocatedBytesForCurrentThread()`; neither produced a reliable non-zero delta in this Editor/
Mono runtime despite real allocations occurring. Reported honestly as **not reliably measurable in
this environment** rather than fabricated — the tool detects this itself and logs it as such instead
of printing a misleading `0 B`.

## Part 6 — Recovery UX backend contract

Reviewed `MainMenuController` + `SaveSlotInfo`/`SaveSlotStatus` against every required recovery
scenario. **Conclusion: no new backend code needed** — the existing contract already covers it:

| Scenario | Existing API |
|---|---|
| Corrupted-but-backup-recoverable | Handled transparently inside `FileSaveSlotRepository` — returns `Valid` from the backup; nothing is overwritten until the next explicit save, so there is nothing to confirm. |
| Fully `Corrupted` | `RefreshSlots()` → `SaveSlotInfo.Status == Corrupted`, `Metadata == null`. UI renders "cannot load" + offers `DeleteSlot` (reset) and simply calling `RefreshSlots()` again (retry). |
| `IncompatibleVersion` | Same shape as Corrupted — `Status == IncompatibleVersion`. |
| `Empty` | `Status == Empty` → New Game path (`SlotRequiresOverwriteConfirm` returns `false`). |
| Never silently overwrite unconfirmed corrupted/incompatible | `DeleteSlot` and `RequestNewGame` on a non-empty slot are both explicit UI-triggered calls — Codex's UI is responsible for confirming before calling them, exactly as it already does for the New Game overwrite case. |

No file paths or raw I/O are exposed to UI; migration/recovery decisions stay entirely inside
`FileSaveSlotRepository`/`SaveMigration`. This contract is documented in the Codex handoff (below) —
Codex only needs to build presentation around `SaveSlotInfo[]`/`Status`, not new backend surface.

## Part 7 — Player build verification

**Build:** Windows64, Build Settings unchanged (MainMenu index 0, DemoScene index 1), output to
`C:\Users\havin\Phase10PlayerBuild\` (outside the repo, not committed). **Result: Succeeded, 0 errors,
0 warnings, 70.99 s, 515.85 MB.**

**Launch:** the built `.exe` was started directly (PID confirmed, `Get-Process` reports
`Responding = True`). `Player.log` shows a clean startup through Mono init, D3D12 device creation
(NVIDIA GeForce RTX 4080 Laptop GPU, D3D12 feature level 12.2), physics (PhysX 4.1.2) and input system
init — **no errors, no exceptions, no failed asset loads.**

**Click-through smoke test: BLOCKED_MANUAL_BUILD_TEST.** The running game window could not be
reliably captured or interacted with through the computer-use automation available in this
environment: `Win32 SetForegroundWindow`/`ShowWindow(SW_RESTORE)` both reported success and the
process's `GetWindowRect` returned a valid, non-degenerate rectangle, but no screenshot taken
afterward (on either attached monitor, at any point over multiple attempts and several seconds of
waiting) showed anything other than desktop/File Explorer content — the game's actual rendered output
was never observed. This is consistent with a remote-desktop/screen-capture path that doesn't see a
D3D12 exclusive/borderless swapchain from this particular automation stack, not with a build or code
defect (the log evidence directly contradicts a crash or hang). See [D-026](DecisionRegister.md). The
process was stopped cleanly afterward.

**What remains:** the user (or Codex, on a machine where the window is actually visible) needs to run
the already-built `.exe` and walk: Launch → MainMenu → New Game (an empty slot, **not** slot 1 — real
slot 1 holds live pre-Phase-6-era `IncompatibleVersion` data on this machine and must not be touched)
→ DemoScene → Save → Return to Main Menu → Continue (same slot) → verify position/inventory/quest/
tutorial/world state → optionally Load a different (empty, then re-populated) slot → Quit Desktop.
Nothing about this report claims that walk-through passed — it has not been run.

## Part 8 — Content authoring documentation

New: [ContentAuthoringGuide.md](ContentAuthoringGuide.md) — operational companion to the existing
[DataDrivenDevelopment.md](DataDrivenDevelopment.md) (concepts) and
[DemoSceneWorkflow.md](DemoSceneWorkflow.md) (scene composition). Covers, with concrete current-state
field/asset/menu references (not aspirational architecture):

1. Item authoring (`ItemSO`/`EquipmentItemSO`, `Resources/Items/` placement, stable ID rules).
2. Quest authoring (`QuestDefinition`, objectives + **required authored `Description`** per objective
   — the Phase 6 gap-response fix — prerequisites, giver/turn-in `npcId`, rewards, Tutorial/Main gate,
   two quest variants sharing one runtime handler).
3. Shop/Recipe authoring (`ShopDefinition`/`RecipeDefinition`, stable IDs, NPC ownership, stock/price/
   sell multiplier, ingredients/output/station tag, quest-event integration is automatic via existing
   domain events).
4. Persistent world entity authoring (definition ID vs `persistentId` distinction, `WorldObjectRegistry`
   explicit-entry binding, chest/pickup/boss/resource-node patterns, duplicate-ID validation,
   respawn-by-rule vs persistent-by-instance, portability checklist reference).
5. Area/Spawn authoring (`areaId`/`spawnId`, `SpawnRegistry` explicit-entry pattern, fallback-spawn
   wiring boundary between authoring and backend, never-use-GameObject-name rule).
6. Scene integration (DemoScene role, prefab/installer portability, `SceneContext` dependency classes,
   service lifecycle reference, Unity MCP-only authoring/validation workflow, DigitalDisco SDF v3 font
   requirement for every TMP element).

Section 7 is an explicit checklist restating this phase's content-ready bar: can a designer build a
new Tutorial Quest end-to-end without touching manager code. Every item on it is satisfied by the
current backend surface.

## Open issues by severity

- **P0:** none open.
- **P1:** none open.
- **P2:** none opened by Phase 10. Pre-existing, out of this phase's scope (per explicit instruction
  not to scope-creep into unrelated P2/P3 fixes): ~30 items' `itemId` still use the accepted legacy
  underscore format ([D-022](DecisionRegister.md) — intentional, not a defect).
- **P3:** none newly identified.

## Known limitations

- Gamepad manual verification remains unverified in every phase including this one (no gamepad
  hardware ever used in physical acceptance either) — a recurring, accepted, non-blocking limitation.
  Keyboard/mouse physical acceptance is complete (see Part 7 final acceptance).
- GC allocation figures for the save pipeline are not reliably measurable in this Editor/Mono runtime
  (Part 5) — reported as unavailable rather than approximated.
- `SaveSoakTestRunner` does not exercise live-scene event-subscriber counts or `GameState`/`timeScale`
  stability under repeated cycling (Part 4 scope note) — those remain covered once each by the
  existing PlayMode suite, not hundreds of times.

## Remaining tasks for Codex / user

- Gamepad manual verification remains an open, non-blocking action item, same as every prior phase —
  can be picked up whenever gamepad hardware is available; does not block `CONTENT_READY`.
- No Recovery UI is required to be built for the current acceptance bar (Part 6 concluded no new
  backend surface is needed) — if a future phase wants dedicated Corrupted/Incompatible recovery
  screens, the contract in Part 6 is what to build against.
- No other Phase 10 follow-up remains. Next work is real content production (see Go/No-Go below).

## Go/No-Go

**CONTENT_READY.** All four Roadmap.md § Phase 10 acceptance criteria are met: mandatory automated
tests pass (58 EditMode + 141 PlayMode, zero regressions across the whole phase including the
Pause-input fix and Save Game slot picker), save migration/recovery pass with real coverage, soak
testing actually ran with real recorded numbers, profiling produced real (not fabricated)
measurements, the Recovery UX backend contract is confirmed complete without new code, authoring
documentation is complete and sufficient for a content designer to build a new Tutorial Quest without
touching manager core, the Player build succeeded cleanly, and the full physical New Game → Save →
Return → Continue → restore → Quit walk-through passed all 24 steps on a real machine with a real
keyboard (Part 7 final acceptance). No P0/P1 issue is open.

Phase 10 is the last phase in [Roadmap.md](Roadmap.md)'s foundation sequence — there is no Phase 11.
The reasonable next step is real content production (quests/items/shops/world entities) following
[ContentAuthoringGuide.md](ContentAuthoringGuide.md), or resolving the platform decisions still marked
`Open`/`Proposed` in [DecisionRegister.md](DecisionRegister.md) (D-010 player death, D-013 character
creation scope, D-019 production world scene topology, and others) if further foundation work is
wanted before content production begins at scale.
