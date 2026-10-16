# Shared SkillHUD / SkillPanel icon presentation — 2026-10-17

Owner authorized resolving the two screenshots: HUD motifs too small; library/loadout exposing square art inside hexagonal frames. D-116/D-118 continuation, presentation only.

## Current composition

- Existing V2 sprites remain the catalog/panel source identity. Stable skill IDs, costs, casting, cooldown ownership and save schema are unchanged.
- 12 skills plus Shield have new transparent foreground layers at Assets/Resources/UI/Skills/Icons/V3Foreground/. Built-in image_gen edited each corresponding V2 PNG; originals V1/V2 are preserved.
- Existing Icon Image becomes the shaped background. SkillIconSocket shader samples V2 corner colors for a dark elemental palette and faint concentric engraving, and clips to a diamond/hexagon. It does not display a rectangular V2 picture.
- Foreground child Image draws the isolated motif using normal UI alpha. SkillIconView sizes it independently to 90% of the socket polygon based on measured alpha hull, keeping complete trails/feathers/flames within the safe region.
- Axes and slot grid remain fixed. Alpha measurements normalize artwork inside those fixed slots; they never reposition rows/columns.
- The same SkillIconVisualCatalog explicitly maps source Sprite to foreground Sprite/hull for all consumers. No runtime Resources scan, filename branches or gameplay catalog mutation.
- Existing Icon Image references, button/drag targets, slot parent relationships, costs and key labels are preserved. Learned/locked tint and visibility propagate to the foreground. Drag ghost uses that same transparent foreground.
- HUD frame, radial cooldown and Shield active highlight remain separate layers. Shield retains ivory/olive material-matched colors.
- Library interior: 112x76 centered in its existing 140x105 socket. Loadout interior: 80x50 at (0,7), above Q/E/R/T. HUD keeps corrected source centers and 210/260 diamond sizes.

The previous SkillHUDIcon 48%-square material remains as a historical asset but is no longer assigned to SkillHUD. Current source prefabs use SkillIconDiamond / SkillIconHexagon materials.

## Authoring and reproducibility

1. Preserve generated originals in V3Foreground; do not paint frames/text into them.
2. Run Tools/skill_icons/measure_foregrounds.py with Pillow. It reads alpha only (threshold >16), records tight Sprite metadata bounds plus a conservative convex hull; it does not rewrite pixels.
3. Refresh Unity, then Tools/ProjectGame2D/UI/Import Shared Skill Foregrounds.
4. Tools/ProjectGame2D/UI/Upgrade Shared Skill Icon Layout upgrades SkillPanel in place.
5. Tools/ProjectGame2D/UI/Build Skill HUD rebuilds the shared GameplayUIRoot HUD.
6. Both authoring builders now use SkillIconPresentationBuilder so later rebuilds retain the same composition.

Measurements: SkillIconForegroundMeasurements.json. Explicit presentation asset: Assets/Resources/UI/Skills/SkillIconVisuals.asset.
Foreground import: Multiple (one tight metadata Sprite), FullRect mesh, Bilinear, max 512, uncompressed, no mipmaps, Clamp, transparency preserved.
Importer validates all 13 sources/foregrounds, nonempty Sprite rects and finite hulls. UI material uses unpacked V2 sprites; no SpriteAtlas support claimed.

## Exact edit prompt

Used separately for each of the 13 matching V2 PNGs, with referenced_image_paths pointing at that file and transparent_background=true:

```text
Use case: background-extraction. Edit target: the attached RPG skill icon. Extract ONLY its main elemental skill motif (or protective bubble and center shield emblem). Preserve its exact recognizable silhouette, pose, colors, chunky pixel-art cel shading, highlights and complete trails/feathers/flames. Remove ALL background: stone surface, scenery, carved rings, decorative runes, circular diagrams and background haze. Output an isolated motif on genuine transparent alpha, no opaque dark rectangle or matte. Retain internal dark shading that belongs to the subject. Center the COMPLETE motif on a square canvas with 12 percent transparent margin all around, do not crop any tip or trail. No new border, frame, text, or objects. For Rock Arena retain the complete arena floor within its ring of pillars; for Shield retain the glass bubble rim and central shield, remove background sigils outside it. This is a foreground layer for compositing into diamond and hexagon UI sockets.
```

## Verification

- 13/13 extracted PNGs reviewed visually and confirmed to have genuine alpha.
- Foreground visual catalog validates; 16 SkillIconView bindings in SkillPanel (12 library + four loadout), five in HUD.
- Unity compilation succeeded and new shader compiler messages = 0.
- Isolated Editor renders reviewed for all twelve skills plus Shield; library, mixed loadout and keys checked at 1920x1080.
- Captures/SkillHUD/shared_layout_hud.png is an enlarged all-elements review, not an actual gameplay HUD screenshot.
- Captures/SkillHUD/shared_layout_panel.png is a standalone 1920x1080 prefab preview with temporary presentation fixtures and SP '--', not player progression.
- Generated previews are created in temporary preview scenes, then closed; no DemoScene state, learned/equipped data or save files are changed.
- Play Mode transitions, automated tests, physical drag/learning/cooldown/block interaction and Player build acceptance are not performed in this slice, following the existing owner workflow. Production scene gameplay acceptance remains manual.

