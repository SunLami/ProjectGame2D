# PlayerHUD consolidation — 2026-10-09

## Accepted contract (D-115)

Owner approved the generated no-vitals-text mockup, then requested asset generation and prefab build.
Reference target 1920x1080; bottom-left safe inset 32px. Four diamond sockets (1 top, 2 left,
3 right, 4 bottom) with upright item icons, quantity and key labels. Style matches approved Inventory:
warm walnut, antique gold stepped pixel bevels, one sapphire joint; no avatar or separate BottomHUD.
Four independent dynamic fill Images: EXP amber, stamina green, mana blue, health red. Only EXP has
current/required text; level is a separate TMP. No HP/STA/MP labels or numeric texts.

Owner polish revision: EXP label is part of the same right-aligned dynamic TMP as the ratio,
so it stays immediately adjacent for any number of digits. Empty tracks are dark walnut (0 EXP
must remain empty); positive EXP uses amber. Meter openings are convex UI stencil masks measured
from the frame alpha: fills cover the entire opening with 6 source-pixel under-border overlap,
including angled ends. Original PNGs and resource/progression values remain unchanged.

Size revision requested by owner: source authoring rectangle remains 340x113.333 Canvas units,
with uniform root presentation scale 0.72. At Canvas scale 2.4 this is 587.52x195.84 physical
pixels (30.6% screen width instead of 42.5%). Bottom-left pivot keeps the 32px inset unchanged.
All frame/fills/icons/text/drop targets scale together; each slot hit square remains about 44px.
Sizing pass uses karpathy-guidelines: only root presentation scale changes, not the art or gameplay.
Source prefab and builder both store scale 0.72; static checks confirm aspect/inset and approximately
44px slot hit squares. EditMode assertion covers the new physical width and uniform scale.
Final live screenshot and test rerun for this sizing pass remain pending Unity connection recovery.

## Ownership and compatibility

- Source `Resources/UI/Gameplay/HUD/PlayerHUD.prefab`, nested in `GameplayUIRoot`.
- Keep `UnifiedGameplayHUD` controller, MapPopup/CharacterPopup/Minimap and M/C navigation.
  Remove only its obsolete BottomHUD child; clear its obsolete vitals/EXP/level references.
- Reuse `PlayerHUDController`, `PlayerExperienceBarController`, `QuickBarSlotUI` and shared managers.
  Runtime installer must not generate duplicate overlays on authored slots.
- QuickBarSaveData.SlotCount = 4. V11→V12 migration keeps the first four assignments, pads missing
  ones, discards only legacy assignments 5–8, and resets out-of-range selection to 0. Inventory data
  never changes. File migration runs in memory, never rewrites personal saves during testing.
- Mana is a PlayerStat runtime resource (max 100, no automatic regeneration or save field yet).
  API consumes atomically and restores with clamp/events. Skill costs and per-skill cooldowns are
  explicitly deferred; existing skill mechanics are not changed by this UI task.

## Asset provenance

Built-in imagegen (imagegen skill), not API/CLI. Generated original PNGs are retained unmodified in
`Resources/UI/Gameplay/PlayerHUDApproved`. Frame prompt: approved cross-plus-four-meter HUD, empty
socket/key tabs, no text/icons/fills, antique gold/walnut/sapphire matching Inventory, transparent
outside and four track cutouts. Fill prompt: one neutral grayscale horizontal highlighted pixel-art
stripe on transparency, no frame/text, runtime tinted red/green/blue/amber. Unity sprite import may
crop transparent padding/neutral fill artwork without rewriting the original PNG.
Frame Simple Image preserves source aspect; fills/text/icons/hit targets remain separate editable
objects. Frame non-raycast, slot hit targets receive drop even empty. No baked text or item icons.

## Gates

1. Compile and EditMode: prefab references, bottom-left anchor, 4 slots, fill-only vitals; V11 migration
   preserves inventory and assignments 1–4, handles null/short inputs/idempotence.
2. PlayMode: manager refuses indices 4–7, legacy restore fallback, mirror drop; mana clamp/event and
   independent HUD binding/enable lifecycle; EXP update/level-up.
3. DemoScene then MapNhat at 1920x1080: screenshot inspect fill/track alignment, key/quantity positions,
   text overflow, layering; Inventory→HUD drag has no discard/item consumption, click has no attack;
   Map/Character still work, no duplicate HUD/slots, Console clean.
4. Independent Player build smoke remains required for full release acceptance.

Implementation/verification results are appended after execution. Do not treat generated mockup as
live acceptance. Karpathy-guidelines used to keep skill integration out of this presentation slice.

## Verification checkpoint (2026-10-09)

- Generated source art imported and PlayerHUD source prefab built; obsolete BottomHUD child removed.
- EditMode: 27/27 passed (job `a9cc24a1217a43aba56b7e9e6ea7eb92`).
- PlayMode: 6/6 passed (job `7c2773f71d094b0397d59a7ffb45cd58`). These results precede the final text-layout fix.
- Live DemoScene capture `PlayerHUDQA/DemoScene_PlayerHUD_Audit.png` at 1920x1080 exposed TMP
  rectangles shorter than preferred glyph height. This capture is diagnostic, NOT final acceptance.
- Builder corrected caption/key/quantity heights and adds the EXP label. PlayMode test now asserts
  rendered glyphs and no overflow for every HUD text (including a two-digit quantity).
- Unity process subsequently closed; connection attempts timed out, and no Unity process was present
  in the final read-only process check. Latest builder changes still require compilation, a rebuild,
  and rerun. Do not represent the current serialized prefab as visually accepted.
- Remaining: rebuild from `PlayerHUDApprovedBuilder.Build()`, rerun tests, DemoScene then MapNhat
  live visual/drag/navigation checks, independent Player build smoke. Personal saves were not written.

## Polish verification (2026-10-09, supersedes earlier rebuild-pending checkpoint)

- Rebuilt source prefab successfully with compact scale, combined EXP text and four convex stencil
  openings. PNGs untouched; no gameplay/save contract changes. Karpathy-guidelines limits this pass
  to the presentation defects reported by the owner.
- EditMode `PlayerHUDApprovedTests`: 4/4 passed, job `c3ebe5c0898d4305bb5f23c778158a7d`.
- Live DemoScene at 1920x1080: `PlayerHUDQA/DemoScene_PlayerHUD_Polish_HalfEXP.png` inspected;
  amber half-fill, full vitals, slanted ends and text alignment fit the frame with no world-color gaps.
- Live presentation probe of EXP 0/50/100 out of 100 produced fill 0/0.5/1 and adjacent label/ratio;
  TMP reported no overflow for all three cases. Console: 0 errors. Test fixture changes were in-memory;
  Play Mode stopped afterward. No personal save writes.
- MapNhat interaction audit and independent Player build smoke from the wider HUD consolidation
  remain release gates; this polish capture does not claim those scenarios passed.
- Additional PlayMode job `fbc80b6d191e4311840845a243b2d956` was dispatched, but polling timed
  out during Unity transport recovery. Its outcome is unverified; do not count it as a pass.
