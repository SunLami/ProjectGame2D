# Twelve-skill MapNhat sorting audit — 2026-10-22

Owner reported Earth 4 missing its rune after production promotion. Cause verified: MeteorStorm
CastRune/TargetRing use Default/-95 and Default/-97; MapNhat terrain was also Default, with orders
-15…0, so opaque terrain rendered over the rune. Frame references were present.

## Fix and layer contract

Terrain-only TilemapRenderers use existing Ground1 for both water and ground, both below Default.
Preserve each renderer's original sortingOrder. Ground VFX remain Default negative order (typically
-100…-49), below actors/props in Default/0 and Player. No global VFX order increase or new layer.
Tree/building/fence/collision/column/railing renderers retain existing sorting. No physics, skill costs,
damage, timing, catalog, save or asset-frame changes.

Ground1 (water): Water, Water_Filler_Under, Water_Filler_Top, Water_surface.
Ground1: Forest_Elevated_ground, Forest_Decor_ground, Forest_Stair, Forest_Grass_ground,
Forest_Dirt_ground, Grass_Filler, Detail, Forest_Bridges, Bridge_01, TA_Decorations_ground,
TA_Sand_ground, Farm_Tiled_Dirt, Farm_Road, CowBarn_GroundGrass, PigBarn_GroundDirt,
ChickenBarn_GroundGrass, Coasts, Wodden_Ground.

## Static audit (all twelve catalog handlers)

| Skill | Renderer path / result |
|---|---|
| Spiral Water Bolt | Projectile prefab Player/5; frames resolve |
| Water Vortex | GroundImpact Default/-100; terrain fix exposes ground effect |
| Spiral Water Beam | Beam Default/-50; terrain fix exposes beam |
| Threefold Tsunami | Wave Default/-50; ultimate Default negative gather/ripple/puddle/flood; references resolve |
| Rock Bolt | Projectile prefab Player/5; frames resolve |
| Rock Arena | Floor Default/-100; pillars Default/0; ground no longer masks floor |
| Rock Spike Field | Runtime spikes Default/0; frames resolve |
| Meteor Storm | Runtime rune -95/-97, shadows -80, shockwave -60 in Default; airborne meteor Player/100 |
| Returning Wind Blade | Launch sets Player/5; trail Player/4 |
| Whirlwind | Ground ring Default/-95, tornado Default/3; terrain no longer masks ring |
| Gale Fan | Runtime gust/puff override Player; references resolve |
| Wind Roc | Default negative gather/path/landing/shadow/shockwave, airborne Player; terrain fix exposes ground components |

Prefab/config inspection includes twelve handler prefab references plus the optional Tsunami ultimate;
no null frame entries or empty *Frames arrays were found. Runtime factory code reviewed as well as
serialized renderers: several VFX roots do not have a baked renderer because factories create children.

## Verification and limits

Read-only layer inspection before fix confirmed the overlap. After fix, isolated Editor camera render
uses the real Meteor prefab rune frame, radius-derived scale, Default/-97 and actual MapNhat terrain:
Captures/Skills/mapnhat_meteor_sorting_editor.png. Rune visibly renders on ground and behind tree/player.
Temporary sample/camera/RT destroyed; no cast, mana, progression or save mutation. This is a sorting
render check, not a real skill cast or animation acceptance. Owner workflow retains manual Play Mode
checks for all twelve casts, canopy occlusion, bridges/water/farm terrain and Player build. No automatic
unit/PlayMode suite or Play Mode transition performed. Existing RuleTile Editor errors are a separate
issue and no clean whole-project Console claim is made.

### Water regression correction — 2026-10-22

The initial split of water into Ground0 was incorrect: it forced all water below all grass,
regardless of the existing per-renderer orders. Owner screenshot exposed hidden lake/river regions.
All four water renderers now share Ground1 with the other migrated terrain renderers; original
orders -6/-5/-4/-1 retained. This restores the original terrain/water interleaving while keeping
all ground below Default negative-order VFX. Trees/buildings/actors untouched.
Full-map Editor render verified: Captures/Skills/mapnhat_water_restored_editor.png.
No Play Mode transition or runtime cast acceptance claimed.
