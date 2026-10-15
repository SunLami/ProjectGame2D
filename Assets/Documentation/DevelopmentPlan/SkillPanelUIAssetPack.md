# SkillPanel UI Asset Pack — V1

2026-10-10, D-116. Generation-only continuation after the approved mockup and twelve skill icons.

## Files and composition

Directory: `Assets/Resources/UI/Skills/Panel/V1/`.

- `skill_panel_board_v1.png`: coherent outer board and structural loadout divider only.
- `skill_hex_socket_v1.png`: reusable library/loadout socket; instantiate 12 + 4.
- `skill_cost_badge_v1.png`: separate circular badge; runtime cost text 2/3/4/5 per element.
- `skill_points_panel_v1.png`: separate blank counter plaque.
- `skill_tooltip_panel_v1.png`: blank tooltip; English TMP content and Learn button separate.
- `skill_learn_button_v1.png`: blank sapphire action button; hover/pressed/disabled via tint, identical geometry.
- `skill_close_button_v1.png`: exact binary reuse of Inventory Processed/inventory_close_v2.png, not newly generated.

Built-in imagegen used for six new components with the approved Inventory board as style reference. Original PNG alpha retained. No baked text or skill motifs. Images visually inspected; exact source pixel ratios differ from requested prompt ratios, so use actual sprite bounds/aspect, never force-stretch ornaments. Keep one coherent outer board, independently position content.

## Planned assembly and remaining verification

Design reference 1920×1080. Center a roughly 1280×800 visual board; preserve its aspect and shrink uniformly when necessary. Library remains above the divider, four loadout slots below; counter top-right, close at safe top-right inset. Reserve at least 24px internal padding. Library 12 sockets can follow approved 6×2 layout; overlays remain separate RectTransforms. Use Digital Disco SDF v3; all labels/tooltips English. Do not bake numbers or current SP into PNGs. Text and icon positioning still need actual Prefab/Game View QA, including alpha padding and fit at 1920×1080. Generated assets alone are not a working SkillPanel.

No prefab, scene, runtime skill learning, input P, progression or save changes in this stage. Data-driven learning/loadout/save gates in SkillIconAssetPack.md still apply. Existing Inventory/PlayerHUD unchanged.

Later UI assembly stage: see SkillPanelImplementationReport.md. UI component import now uses one
tight-cropped sprite per PNG (Multiple mode with one rect), not the initial full-canvas Single sprite;
original PNG pixels remain untouched. Runtime learning/save gates still apply.

## Exact prompts

Import verification: Unity loaded all 7/7 files as Single Sprite, Point filtering, no mipmaps,
uncompressed default platform. All seven PNG corner-alpha checks returned zero (transparent).

Reference for every generated component: `Assets/Resources/UI/Inventory/DarkInventoryTabbed/inventory_complete_frame_approved.png` (style reference only).

### skill_panel_board_v1

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-29741065-f791-4683-bbf6-1dcf0dea6d31.png`

```text
Use case: stylized-concept. Production UI bitmap asset for a 2D pixel-art fantasy RPG, one isolated component on genuine transparent background. Match the reference Inventory: dark walnut and charcoal, warm taupe interior, beveled antique golden thin borders, crisp stepped pixel edges, sparse medieval geometric ornament, subtle sapphire only when requested. Front-facing perfectly orthographic, symmetrical, no perspective, no cast shadow outside asset. No text, letters, numbers, icons, watermark or gameplay imagery. Transparent margin at least 6 percent all sides. Preserve blank surfaces for editable Unity TMP and skill icon overlays. A single wide rectangular main SkillPanel board, aspect ratio 1.6:1. Thick dark walnut outer frame, thin antique gold inner bevel, clipped corners, ONE small sapphire diamond centered on top rim. Broad uninterrupted dark charcoal interior. A single thin straight gold horizontal divider near bottom 22 percent separates empty bottom loadout area from larger upper library area. NO slots, no buttons, no points box, no inner panels, no title plaque, no extra objects. Elegant restrained frame, not a collage.
```

### skill_hex_socket_v1

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-36d86d1a-1cbe-4b62-aaae-3082c5377334.png`

```text
Use case: stylized-concept. Production UI bitmap asset for a 2D pixel-art fantasy RPG, one isolated component on genuine transparent background. Match the reference Inventory: dark walnut and charcoal, warm taupe interior, beveled antique golden thin borders, crisp stepped pixel edges, sparse medieval geometric ornament, subtle sapphire only when requested. Front-facing perfectly orthographic, symmetrical, no perspective, no cast shadow outside asset. No text, letters, numbers, icons, watermark or gameplay imagery. Transparent margin at least 6 percent all sides. Preserve blank surfaces for editable Unity TMP and skill icon overlays. ONE horizontal flat-top regular hexagonal empty skill socket, width to height 1.15:1. Thin double antique gold bevel, dark warm taupe opaque interior, clean large interior safe area 76 percent. No gem, no badge, no markings. Reusable for all 12 library skills and four loadout slots. Simple crisp silhouette.
```

### skill_cost_badge_v1

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-3b65c264-4d67-44eb-8f41-bc32be76636a.png`

```text
Use case: stylized-concept. Production UI bitmap asset for a 2D pixel-art fantasy RPG, one isolated component on genuine transparent background. Match the reference Inventory: dark walnut and charcoal, warm taupe interior, beveled antique golden thin borders, crisp stepped pixel edges, sparse medieval geometric ornament, subtle sapphire only when requested. Front-facing perfectly orthographic, symmetrical, no perspective, no cast shadow outside asset. No text, letters, numbers, icons, watermark or gameplay imagery. Transparent margin at least 6 percent all sides. Preserve blank surfaces for editable Unity TMP and skill icon overlays. ONE small perfectly circular empty Skill Point cost medallion, thin antique gold ring with subtle beveled rim, opaque charcoal center with broad blank center for a dynamic single digit. No gem, no hanging loop, no numbers. Square canvas.
```

### skill_points_panel_v1

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-7610a064-ff77-4f90-b1b7-4bfba1c54f71.png`

```text
Use case: stylized-concept. Production UI bitmap asset for a 2D pixel-art fantasy RPG, one isolated component on genuine transparent background. Match the reference Inventory: dark walnut and charcoal, warm taupe interior, beveled antique golden thin borders, crisp stepped pixel edges, sparse medieval geometric ornament, subtle sapphire only when requested. Front-facing perfectly orthographic, symmetrical, no perspective, no cast shadow outside asset. No text, letters, numbers, icons, watermark or gameplay imagery. Transparent margin at least 6 percent all sides. Preserve blank surfaces for editable Unity TMP and skill icon overlays. ONE compact horizontal rectangular counter plaque, aspect 2.2:1. Clipped corners, thin antique gold border, dark walnut outer edge, charcoal opaque blank interior with room for an editable heading and numeric value. No gem. No text. Restrained ornament.
```

### skill_tooltip_panel_v1

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-4fb6ca6f-410b-4ba9-85ef-d51c159d0523.png`

```text
Use case: stylized-concept. Production UI bitmap asset for a 2D pixel-art fantasy RPG, one isolated component on genuine transparent background. Match the reference Inventory: dark walnut and charcoal, warm taupe interior, beveled antique golden thin borders, crisp stepped pixel edges, sparse medieval geometric ornament, subtle sapphire only when requested. Front-facing perfectly orthographic, symmetrical, no perspective, no cast shadow outside asset. No text, letters, numbers, icons, watermark or gameplay imagery. Transparent margin at least 6 percent all sides. Preserve blank surfaces for editable Unity TMP and skill icon overlays. ONE vertical rectangular skill tooltip panel, aspect 0.8:1. Thin antique gold border, clipped corners, charcoal opaque flat interior, narrow walnut outside rim. Large completely uninterrupted empty interior to lay out a skill name, description and separate Learn button in Unity. No decorative sections, no button baked in, no icon, no gem. Minimal subtle corner bevels.
```

### skill_learn_button_v1

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-4c8773e8-84f7-4ddc-ad58-1161d0bc735a.png`

```text
Use case: stylized-concept. Production UI bitmap asset for a 2D pixel-art fantasy RPG, one isolated component on genuine transparent background. Match the reference Inventory: dark walnut and charcoal, warm taupe interior, beveled antique golden thin borders, crisp stepped pixel edges, sparse medieval geometric ornament, subtle sapphire only when requested. Front-facing perfectly orthographic, symmetrical, no perspective, no cast shadow outside asset. No text, letters, numbers, icons, watermark or gameplay imagery. Transparent margin at least 6 percent all sides. Preserve blank surfaces for editable Unity TMP and skill icon overlays. ONE horizontal empty primary action button, aspect 3.4:1. Sapphire dark-blue interior, thin antique gold double beveled outline, clipped corners, crisp pixel art. Broad blank center for editable Learn or Learned text. No gem, no symbols, no letters. Normal state only; future tint states keep identical geometry.
```

