# Skill Icon Asset Pack — V1

Owner-approved mockup, asset-only stage (D-116), 2026-10-10.

## Asset contract

12 transparent PNG motifs generated using built-in imagegen, matching Inventory/PlayerHUD dark walnut, antique gold and sapphire sockets. Water navy/cyan/white; Earth brown/tan angular rock with intentional orange meteor flames; Wind mint/jade/white. Frames, text, cost badges and learned overlays are separate editable UI, not baked into these motifs.

Assets: `Assets/Resources/UI/Skills/Icons/V1/`. All are 1254×1254 with no opaque pixels touching canvas edges. Original generated PNGs preserved without raster processing. Prompt requested 1024×1024; generator returned 1254×1254.

## English tooltip copy and learning costs

Names below are proposed display copy, not stable-ID or class renames. Descriptions follow SkillTestHarness and DemoScene assignments. Costs are Skill Points for learning, NOT mana. Each element uses 2/3/4/5. All future UI and tooltip text must be English. Labels: Skills Library, Skill Points, Skill Loadout, Learn, Learned, Not Enough Skill Points. Numeric combat values must come from gameplay definitions.

| Element / slot | Name | Cost | PNG | Description |
|---|---|---:|---|---|
| Water 1 | Spiral Water Bolt | 2 | water_01_spiral_bolt.png | Launch a spiraling water projectile. |
| Water 2 | Water Vortex | 3 | water_02_vortex.png | Create a damaging water vortex that slows enemies and pulls them toward its dark center. |
| Water 3 | Spiral Water Beam | 4 | water_03_spiral_beam.png | Fire a sustained spiral water beam while rooted in place. |
| Water 4 | Threefold Tsunami | 5 | water_04_threefold_tsunami.png | Unleash three increasingly powerful waves, culminating in a water dragon and final water blast. |
| Earth 1 | Rock Bolt | 2 | earth_01_rock_bolt.png | Launch an angular boulder. |
| Earth 2 | Rock Arena | 3 | earth_02_rock_arena.png | Raise a pillar arena that traps, slows and weakens enemies, with stunning spikes inside. |
| Earth 3 | Rock Spike Field | 4 | earth_03_spike_field.png | Erupt a forward fan of damaging stone spikes that stun and root enemies. |
| Earth 4 | Meteor Storm | 5 | earth_04_meteor_storm.png | Call down flaming rocky meteors with explosive impacts. |
| Wind 1 | Returning Wind Blade | 2 | wind_01_returning_blade.png | Throw a crescent wind blade that damages enemies on its outward and return paths. |
| Wind 2 | Whirlwind | 3 | wind_02_whirlwind.png | Summon a moving whirlwind that lifts enemies, then drops and stuns them. |
| Wind 3 | Gale Fan | 4 | wind_03_gale_fan.png | Release three pushing gusts in a forward fan. Wall collisions deal bonus damage and stun. |
| Wind 4 | Wind Roc | 5 | wind_04_wind_roc.png | Summon a diving wind roc that carries and throws enemies into a shockwave, leaving a slowing aftermath. |

## Verification and remaining work

Images individually visually reviewed; dimensions and transparent canvas edges checked. In-socket small-size preview and runtime UI acceptance remain pending. No scene, prefab, gameplay, mana, cooldown or save changes are part of this asset stage. Before implementation, define skill catalog/stable IDs, level-up/SP spending/save migration and four skill loadout slots separate from Inventory quick slots. Preserve DemoScene-first integration.

## Exact generation prompts

Unity import metadata checked for all 12 PNGs: Sprite texture type, Point filtering, mipmaps disabled,
default-platform uncompressed texture. Actual in-panel rendering remains a later integration check.
Unity Editor execution confirmed 12/12 load as Sprite after explicitly importing as Single sprites.

### Spiral Water Bolt

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-b0257d5c-feb3-4502-8b59-9915457969a1.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WATER SKILL 1: Spiral Water Bolt. A SINGLE compact teardrop-shaped turbulent water projectile flying diagonally up-right, bright cyan-white head, visibly twisting blue water bands around its body and short tapering splash trail behind. Jagged irregular watery edges broken by white foam/whitecap crests. Palette deep navy, royal blue, cyan, white. NOT a circular whirlpool, NOT a tidal wave, no ice, no bubbles ring.
```

### Water Vortex

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-415b2f63-2aa9-4fb1-a34e-1502dd268f41.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WATER SKILL 2: Water Vortex. Ground-target SUCTION whirlpool viewed three-quarter top-down: a churning flat blue water basin spiraling DOWN into a visibly DARK navy-black funnel hole at its center. Asymmetric jagged wave rims and white foam segments turn inward. Core must be dark, never bright or glowing. Deep navy, blue, cyan and white. Clearly a ground whirlpool, NOT an upright tornado, NOT a smooth geometric magic circle or portal.
```

### Spiral Water Beam

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-082f179f-6065-472f-aee5-c364f1e6e6cb.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WATER SKILL 3: Spiral Water Beam. One strong straight continuous pressurized water jet diagonally from lower-left to upper-right, long thick cyan-white central stream wrapped by churning spiraling blue water ribbons, jagged foam edges, forceful watery burst at the leading tip. Deep navy, royal blue, cyan, white. Read as a sustained long BEAM, not a compact projectile, not laser energy or ice. No caster.
```

### Threefold Tsunami

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-84a94dcf-4fbd-46e3-98dc-e03402b16ff7.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WATER SKILL 4 ULTIMATE: Threefold Tsunami. THREE distinct stacked advancing foaming tsunami crests, growing from a small rear crest to a large front crest in a fan-like diagonal formation. The largest crest has a recognizable roaring serpentine water-dragon head formed entirely of blue water and white foam emerging from it. Make three wave layers clearly readable. Deep navy, royal blue, cyan, white. Entire compact icon centered, no beach, no background, no fire, no ice.
```

### Rock Bolt

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-beaf660d-f8a5-4ba4-ae85-7dd0ff9a083d.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. EARTH SKILL 1: Rock Bolt (existing skill launches one rock, NOT a pillar rising from ground). ONE large jagged angular brown/tan flying boulder diagonally up-right, chipped fractured face with pale sand highlights, short tan dust trail and a few angular small debris behind lower-left. No spikes field, no pillar, no fire or lava, no magic glyph. Dark brown, earthy brown, tan/sand, pale yellow highlights.
```

### Rock Arena

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-28072e68-79ff-483f-b485-55d333613d15.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. EARTH SKILL 2: Rock Arena. A clearly CLOSED CIRCULAR ring of rough upright brown rock pillars enclosing a cracked earthen arena floor, viewed top-down three-quarter so the empty enclosed interior and full stone perimeter remain visible. A few shorter stone spikes INSIDE the ring, enclosing/trapping silhouette. Keep front/back rim clear, no human or enemy, no colosseum architecture, no brick walls, no fire. Dark brown, mid earthy brown, tan sand, pale-yellow highlights. NOT a single rock projectile, NOT fan formation.
```

### Rock Spike Field

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-964cbcb5-8531-49e1-ad0f-c0ca2af29345.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. EARTH SKILL 3: Rock Spike Field. A dense triangular FAN of sharp angular brown stone spikes erupting progressively outward from a narrow lower-left origin toward a broad upper-right edge, on a tiny cracked ground wedge. Distinct staggered rows of tapered jagged rock spikes, 7-10 readable primary spikes, tan dust chips. Clearly forward-spreading cone, not circular arena and not flying boulder. Dark brown, mid earthy brown, tan sand, pale yellow. No fire or lava.
```

### Meteor Storm

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-fd30173c-e9f2-4697-9950-627b7e9352d2.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. EARTH SKILL 4 ULTIMATE: Meteor Storm. THREE distinct heavy jagged brown rocky meteorites descending diagonally from upper-left toward a cracked ground impact at lower-right. Largest meteor is an unmistakable angular earthy-brown BOULDER with orange ember cracks, orange/gold flame tails behind the rocks, a compact ring-shaped dust shockwave under the largest impact. Rock bodies dominate over flames. Earth ultimate rather than pure fireball. Dark brown, tan, burnt orange, pale golden yellow; no blue, no volcano, no red planet.
```

### Returning Wind Blade

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-42cd8e17-e3b4-4ceb-9e86-82e663857f4e.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WIND SKILL 1: Returning Wind Blade. ONE curved CRESCENT boomerang blade made entirely of white and pale mint wind with sharp pointed ends, spinning on a clearly curved hooked return trail. A short second faint echo of the crescent along the curved trail suggests outward-and-return motion, not two separate blades. Two tiny jade leaves carried by the wind. Pure white, pale mint, jade green, deep teal dark outline. NOT a metal weapon, not a tornado, no blue water, no circular portal.
```

### Whirlwind

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-21b7265b-7b31-4a75-b602-cf2be0fe540d.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WIND SKILL 2: Whirlwind. One UPRIGHT tall conical tornado composed of layered sharp white/mint spiraling wind ribbons, tapering narrow at bottom and broad at top. A few small jade leaves and pebbles visibly lifted upward along the side to suggest airborne lift. Circular gust sweep under base only, transparent between ribbons. Pure white, pale mint, jade green, dark teal outline. NOT a flat ground whirlpool, no blue water, no lightning, no smoke cloud.
```

### Gale Fan

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-8cfe7449-b966-46ce-a0f5-baafd9519cb0.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WIND SKILL 3: Gale Fan. THREE successive sharp white-mint gust arcs advancing from narrow lower-left origin to broad upper-right in a forward-facing FAN, strong parallel wind streaks and a few jade leaves swept outward. Three crescent-like pressure fronts clearly distinct, nearest small, farthest large, open fan not a circle. Communicate forceful repeated PUSH, not suction or tornado. Pure white, pale mint, jade green, dark teal outline. No eagle, no water, no metal blades.
```

### Wind Roc

Original: `C:\Users\havin\.codex\generated_images\01a1161f-2b37-7d51-9d22-fc884ccbc558\exec-3318a02e-d648-4b48-9fce-8601b468ca3d.png`

```text
Use case: stylized-concept. Production bitmap UI SKILL ICON for a coherent set of twelve skills in a 2D pixel-art RPG. ONE icon per image, square canvas 1024x1024, genuine transparent background. Icon motif alone, NO frame, NO hexagon, NO badge, NO letters, NO numbers, NO UI, NO watermark, NO cast-cost text. Centered isolated subject occupying 72 percent of canvas, generous transparent safety margin on all four sides. Readable at 64-96 pixels. Crisp HARD-EDGED chunky pixel art, as if designed on a 128x128 logical pixel grid then integer nearest-neighbor enlarged. Deliberate stepped pixel contours, bold dark outline, 4-6 flat cel-shaded color bands, high contrast, no smooth gradients, no airbrush, no photographic texture, no diffuse glow. Matches warm walnut/antique gold/sapphire medieval Inventory UI and PlayerHUD by being a vivid elemental motif against their dark sockets, but do not bake the socket or gold border. Dynamic diagonal or three-quarter composition, clear single primary silhouette. WIND SKILL 4 ULTIMATE: Wind Roc. A majestic recognizable EAGLE formed entirely of white and pale mint WIND, hooked beak, fierce eye, broad feathered wings spread in a dynamic forward dive diagonally down-right, talons extended to snatch targets. Sharp layered feather shapes dissolve into curved mint wind trails behind and a few loose wind feathers. One eagle only, no enemy/human, no background. Pure white, pale mint, jade green, dark teal outline. NOT an owl, not a dragon, no blue water, no lightning.
```


## V2 continuation — 2026-10-16

Owner requested regenerating all twelve icons with backgrounds and a matching Shield. Current
consumers use V2; this document retains the original transparent V1 asset contract/history.
See [SkillIconV2.md](SkillIconV2.md) for complete prompts and integration/verification.
