# LightFantasy Dialogue UI

Original project-local dialogue UI kit generated for `ProjectGame2D` on 2026-08-26.

## Runtime assets

- `dialogue_frame_hd.png`: bottom-screen dialogue shell with an integrated portrait aperture and transparent text aperture.
- `dialogue_nameplate_hd.png`: empty NPC name plaque; render the name with TMP/Digital Disco.
- `dialogue_choice_button_hd.png`: empty choice button. Use Unity `Button.colors` for hover, pressed and disabled states so dynamic choices share one stable sprite.
- `dialogue_continue_indicator_hd.png`: small continue marker; animate with a subtle vertical bob or alpha pulse.

All assets use alpha transparency and match the existing LightFantasy language: warm oak, antique gold, cream parchment, restrained green leaves and royal-blue sapphire accents. Dynamic text must use `Assets/Fonts/DigitalDisco SDF v3.asset`.

## Recommended reference layout (800x450)

- Dialogue root: stretch to full Canvas; anchor the panel to bottom-center.
- Frame presentation footprint: approximately `700 x 190`, anchored 42 px above the bottom edge.
- Portrait: place behind the circular aperture, preserve aspect, and mask to the opening.
- NPC nameplate: approximately `230 x 42` visible footprint, single-line 18 pt TMP, overlapping the upper-left of the text region.
- Body text safe area: begin after the portrait divider; keep at least 34 px from parchment edges.
- The parchment backing intentionally overlaps beneath the decorative frame; do not fit it exactly to
  the irregular aperture or the world background will leak through its antialiased inner edge.
- Choice list: up to four buttons, stacked above the frame or within the right text region depending on line count.
- Continue indicator: lower-right of the text safe area; pulse only when the current page is fully revealed.

The UI is presentation only. Dialogue state, quest outcomes, shop/crafting routing and save data remain owned by their domain/application services.

## Generation notes

Built-in image generation was used with the existing Unified HUD, Quest panel and Tutorial panel as visual references. Prompts required isolated, text-free UI assets and either genuine alpha or a solid `#FF00FF` chroma background. The generated sources are preserved under `Assets/ArtSource/UI/Dialogue/LightFantasy`; final files were cleaned with the `generate2dsprite` deterministic processor.
