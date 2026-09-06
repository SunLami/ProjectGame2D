# Logo Intro and Outro Transition keyframes

These are 16:9 reference images for Google Flow. Upload the image named in each section as the first-frame reference, then use its matching prompt.

## Logo Intro

- First-frame image: `logo-intro-first-frame.jpg` (`.png` is the lossless backup)
- Duration: 6 seconds.
- The ORYNTHALS logo is composited from the approved project PNG. Do not ask Flow to redraw or edit the logo; the purpose of this keyframe is to preserve its lettering and ornamentation.

```text
Use the supplied image as a strict first-frame reference for a 6-second 16:9 pixel-art fantasy game logo intro.

Preserve the ORYNTHALS logo exactly: the exact word, gold stone lettering, dark navy outline, green leaves, white flowers, blue banners, and the sunrise road within the O. The logo stays centered, sharp, full-size, and fully readable. Do not alter any letter, crop it, translate it, add text, or create a second logo.

Start from this exact dawn village composition. In the first two seconds, animate only the environment: subtle grass and distant tree movement, an almost imperceptible drift of cloud, and sparse warm golden motes rising slowly. From seconds two to four, give the logo a very subtle gold edge shimmer and a restrained breathing scale, maximum two percent. From seconds four to six, let the motes travel gently toward the viewer while the title fades softly to transparent, revealing the meadow behind it.

Locked camera. No characters, no weapons, no UI, no subtitles, no watermark, no new text, no camera motion, no cuts, no morphing, no blurry logo, no photorealism.
```

## Outro Transition

- First-frame image: `outro-transition-first-frame.jpg` (`.png` is the lossless backup)
- Duration: 5 seconds.
- This frame follows the arrival at the training yard and ends ready to crossfade into the Unity Timeline camera.

```text
Use the supplied image as a strict first-frame reference for a 5-second 16:9 pixel-art fantasy RPG transition.

Preserve the exact elevated top-down composition: the young hero at lower left, the trainer at upper center, the sandy training yard, targets, fences, blue-roof homes, trees, flowers, and warm morning palette. Both characters keep their position, scale, clothing, empty hands, and identity.

During seconds zero to two, animate only small natural motions: the trainer gives one gentle acknowledging nod, the hero responds with a small hopeful nod, leaves and grass sway, the banner moves lightly, and the distant windmill sails rotate very slowly. From seconds two to four, settle the characters and let the environment quiet down. In the final second, gradually soften the peripheral brightness while keeping the two characters and the yard readable, creating a stable frame for a crossfade into the in-game Timeline.

Locked camera. No text, logo, UI, weapons, walking, new characters, large gestures, lip-sync, camera pan, zoom, cuts, morphing, pixel flicker, or photorealism.
```

## Unity handoff

End the outro on a stable frame. Crossfade for 0.35-0.5 seconds to the first Timeline shot, matching player and trainer placement. The Timeline should have player input locked before the crossfade begins.
