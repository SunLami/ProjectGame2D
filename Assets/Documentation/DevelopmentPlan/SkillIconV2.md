# Skill Icons V2 — generation and integration

Owner request 2026-10-16: smaller complete motifs inside diamond HUD sockets, richer generated backgrounds, and Shield matching current shader. Built-in image_gen used; V1 preserved.

Generated originals copied unchanged into `Assets/Resources/UI/Skills/Icons/V2/` (12 skills + shield_block). Catalog stable IDs, learning costs, cast kinds and save schema unchanged. SkillPanel builder uses V2; runtime resolves catalog icons.

HUD retains corrected centers and separate diamond mask/cooldown/frame. SkillHUDIcon UI material insets the complete image to 48% of diamond width, mathematically inside the inscribed square, and blends the outer image margins into a dark corner-sampled backdrop over the remaining diamond. Mask stencil is supported. No foreground cropping or raster editing. This shader is for full-rect unpacked V2 sprites only (no atlas).

Shield matches actual front/back material: rim RGB (1,.98,.9), glass tint (.6,.6,.47), warm ivory/olive; active highlight follows ivory.

## Exact prompts

Common prompt:

```
Use case: stylized-concept. Production RPG skill icon V2, single square bitmap. Cohesive premium dark fantasy pixel art with bold chunky stepped contours, 5-7 cel shaded color bands, readable at 48px. An ornate magical motif with a distinctive silhouette, no frame or border, no text or numbers or letters, no watermark. Opaque textured dark elemental background with subtle carved runic arcs, restrained depth, dark vignette; no transparent cutout. Subject fully visible, centered, occupies central 62 percent of square, generous dark breathing space, no cropped trails. Not photoreal, no blurry airbrush. 
```

### water1

WATER 1 Spiral Water Bolt: one tightly coiled surging cyan water projectile spiraling diagonally upward, frothy white crest and dark navy core; deep ocean teal backdrop with subtle ripple rings. No ice, no fire.

### water2

WATER 2 Water Vortex: one circular turbulent cyan maelstrom, clearly visible dark open eye, curling white foam teeth; navy ocean backdrop with subtle ripple arcs. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### water3

WATER 3 Spiral Water Beam: a focused diagonal lance of water wrapped in two helical cyan streams, white froth; navy ocean backdrop with subtle ripple arcs. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### water4

WATER 4 Threefold Tsunami: three staggered curling tidal crests forming one powerful layered wave silhouette, larger rear crest hints at a water dragon head; navy ocean backdrop. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### earth1

EARTH 1 Rock Bolt: one airborne jagged tan brown boulder with fractured bright facets, short angular dust trail; dark umber mineral stone backdrop with faint amber fissures. No lava. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### earth2

EARTH 2 Rock Arena: complete closed ring of angular upright stone pillars surrounding a small cracked earthen arena, three quarter overhead view; dark umber mineral stone backdrop, faint amber fissures. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### earth3

EARTH 3 Rock Spike Field: fan of five distinct jagged tan stone spikes bursting outward from fractured ground; dark umber mineral stone backdrop, faint amber fissures. No fire. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### earth4

EARTH 4 Meteor Storm: three falling angular rocky meteors, one dominant foreground meteor with fiery amber cracks and orange flame tails; dark umber mineral stone backdrop. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### wind1

WIND 1 Returning Wind Blade: one sharp crescent mint jade wind blade, curved circular returning trail and a few pale leaves; deep desaturated jade storm backdrop with subtle swirling carved arcs. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### wind2

WIND 2 Whirlwind: vertical twisting jade mint tornado with visibly hollow dark eye, two bright spiral ribbons and a few leaves; deep desaturated jade storm backdrop. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### wind3

WIND 3 Gale Fan: three distinct curved gust blades spreading in a fan, pale mint-white cutting edges, a few jade leaves; deep desaturated jade storm backdrop. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### wind4

WIND 4 Wind Roc: one majestic spectral owl-roc formed of jade winds, spread sweeping wings, hooked beak and pale feather edges in diving pose; deep desaturated jade storm backdrop. Keep all essential motif including trails inside the central 60 percent square; surround it with a quiet darker backdrop. Same cohesive dark fantasy jewel-like skill-icon collection.

### shield

Use case: stylized-concept. Production RPG skill icon V2, single square bitmap. Cohesive premium dark fantasy pixel art with bold chunky stepped contours, 5-7 cel shaded color bands, readable at 48px. An ornate magical motif with a distinctive silhouette, no frame or border, no text or numbers or letters, no watermark. Opaque textured dark elemental background with subtle carved runic arcs, restrained depth, dark vignette; no transparent cutout. Subject fully visible, centered, occupies central 62 percent of square, generous dark breathing space, no cropped trails. Not photoreal, no blurry airbrush. SHIELD BLOCK, match actual game shader palette: luminous warm ivory rim RGB 1,.98,.9 and muted olive glass interior RGB .6,.6,.47. One magical spherical protective glass bubble with bright ivory fresnel rim, very clear dark transparent-looking center, small sweeping upper-left ivory highlight and subtle curved shimmer. A tiny simple ivory shield sigil at its heart makes defense readable. No metallic blue shield, no cyan. Opaque charcoal olive background with subdued concentric protection arcs. Essential bubble and motif fit central 60 percent of canvas; no frame or text. Cohesive premium chunky pixel art matching elemental skill icon set.


## Verification

13/13 generated originals visually reviewed in Captures/SkillHUD/v2_all_review.png.
Catalog Validate succeeded with 12 V2 sprite references. SkillPanel source prefab upgraded without
rebuilding layout, learning state or callbacks. SkillHUD source prefab rebuilt with V2 Shield and
ivory active tint. All twelve skill variants plus Shield rendered in an isolated Editor preview:
Captures/SkillHUD/v2_diamond_review.png. Unity Console error query: zero.
Texture import is FullRect/Single, uncompressed, no mipmaps, Clamp, 256 max texture size and Bilinear
filtering for small HUD readability. Full-resolution generated originals remain unchanged on disk.
No Play Mode transition, automated tests, progression mutation or save migration performed.
Physical gameplay at 1920x1080, cooldown/block input and Player build acceptance remain manual.

## Current consumer composition — 2026-10-17

The 48%-square HUD inset described above is historical. Current consumers retain V2 source
identity/palette and use separate transparent motif derivatives with shape-aware sizing; see
[SkillIconSharedPresentation.md](SkillIconSharedPresentation.md). V2 originals remain intact.
