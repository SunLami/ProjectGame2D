# Inventory modular prefab integration — 2026-10-07

## Owner revision: complete background frame

Owner rejected the assembled component frames and requested the supplied complete frame unchanged.
`inventory_complete_frame_approved.png` is now the authoritative background, using Simple Image
at its original 1672:941 aspect ratio. Separate overlay text, icons, Close X, category hit targets,
equipment drag targets and Quick Assign controls remain editable. This supersedes the independent
decorative frame arrangement below. Board size is 680 x 382.70 reference units (about 1306 x 735
screen pixels at 1920x1080). The previous 7/7 visual-prefab test result predates this revision;
new frame assertion and live layout verification have now passed in Editor.
Complete background saved to source prefab; isolated 1920x1080 preview regenerated and inspected.
Duplicate decorative PreviewFrame Images disabled, bookmark labels centered on baked tabs.
Live DemoScene inspection exposed stale scene geometry overrides; these were removed and the live
layout rechecked. Current Console: zero errors. See latest verification below, not only isolated preview.

## Implemented

- Source: `Assets/Prefabs/InventoryUIController.prefab`.
- Builder: `Tools > ProjectGame2D > UI > Build Modular Inventory`.
- Component sprites: `Assets/Resources/UI/Inventory/DarkInventoryTabbed/Processed`.
- Originals retained outside Processed; builder tight-crops alpha and imports Single Sprite,
  Point filtering, no mipmaps, uncompressed textures and explicit slice borders.
- Independent outer border, character well, stats panel, grid panel, bookmark controls, square
  equipment/item slots, diamond Quick Assign and Gold well; Close X retains existing callback.
- Five categories use definition metadata, authored on existing content without changing item IDs.
  Food includes Bread/Cheese, drinks and crop produce; seed/bait use Resources; armor remains in All.
- Filtering binds original InventorySlot references. Empty filler cells cannot mutate hidden slots.
- Quick Assign mirrors indices 0–3. Same-index drag between Inventory and HUD does not clear assignment.
- Grid remains six columns and five visible rows; unused cells beyond capacity/filter display are hidden.
- Builder synchronizes RectTransform/Image overrides on the Inventory instance in GameplayUIRoot.

## 1920x1080 layout

Gameplay Canvas continues using its existing reference units 800x600 with width scaling. At 1920x1080,
the effective Canvas viewport is 800x450. Inventory window is 680x382.70 with the existing 0.8 root scale,
so the visible board is approximately 1306x735 screen pixels, centered with margins.

All dynamic text uses the existing TMP font. Item icons stay inside their own slot safe area.
Gold icon/value, numeric quick-slot labels and bookmark labels are separate objects. Each module can
be moved/resized in Prefab Mode. Keep outer/frame Images non-raycast and slot/Button Images raycast.

`InventoryTabbedPrefabBuilder.RenderPreview()` renders a 1920x1080 test image in an isolated preview
scene using sample item icons, stacks, Gold and long stats. It leaves gameplay scenes untouched.
Character preview requires the live Player; the isolated layout image does not claim to verify it.

## Verification status

### Live audit after owner feedback — 2026-10-07

- Removed exactly 827 Inventory RectTransform overrides from Bootstrap's GameplayUIRoot instance;
  retained 780 other property modifications. Live Game View confirmed 1920x1080 and centered geometry.
- Fixed full-board transparent EquipmentPanel blocking pointer events; empty inventory/equipment
  backgrounds are raycast targets, child text/icon and decorative surfaces are not.
- RaycastAll at occupied/empty slot, HeadSlot, QuickSlot1, All and Close resolves the intended target.
- EditMode source prefab gate: 8/8 PASS (job 5465f8abc2484617b5e0f513b3d99402).
- Live in-memory callbacks: all five category counts/original references, unchanged inventory on
  filter, filler drop rejection, all four quick assignments without consumption, quick move and
  right-click clear PASS. Empty-slot swap, double-click equip, drag unequip, tooltip viewport/exit,
  outside discard, cancel and exact single-unit discard PASS. Scroll reaches Slot_131; 20 open/close
  cycles return to Playing. Console error query: zero entries at that checkpoint.
- Reopening from bottom retained scroll; fixed reset after next-frame layout rebuild.
- Player preview used Sprite-Lit on a camera-only layer without 2D lighting. Added temporary unlit
  presentation with original-material restoration on close. Live colored preview PASS; all Player
  SpriteRenderer shaders/layers returned to original Sprite-Lit/layer 0 after closing. Reopen scroll=1.
- Added InventoryPresentationPlayModeTests using the source prefab for filter/reference/filler,
  actual empty-target raycast and reopen-scroll regression.
- PlayMode job 11c010499c144edd8d80279ce5a425f9 failed to initialize with zero tests started;
  this is not a test assertion failure. Subsequent Editor reconnect/build/test attempt interrupted:
  Unity PID 31832 reported Responding=False, bridge had zero active instances. Do not count the
  pending new PlayMode suite, final preview or MapNhat smoke as passed. No user save file was written.

Unity recovered. Initial new fixture failed because it created a duplicate of Bootstrap's InventoryManager;
fixture now reuses/snapshots/restores in-memory inventory when an owner already exists. Final targeted
PlayMode run: 20/20 PASS (job 39987e83ff0442c084e305423804471e), including source-prefab regression.
MapNhat production smoke: loaded Bootstrap + MapNhat, Game View 1920x1080, board corners
(307.2,172.6) to (1612.8,907.4), full begin/drop/end Quick gesture PASS, no discard modal/item consumption,
Console zero errors. Image: `InventoryRedesignQA/MapNhat_Inventory1920x1080_Final.png`.
Editor play-start scene restored to Bootstrap. No user save file written. Independent Player build
was not run in this UI audit; it remains a release gate. Prior modular-layout results below are historical.

Unity script compilation succeeded. Source prefab saved through PrefabUtility.
Initial screenshot identified slice distortion on slots/bookmarks and edit-mode tooltip visibility;
the geometry and preview setup were corrected. Final isolated layout image
`InventoryRedesignQA/Inventory1920x1080.png` inspected: square slots, tab labels, Close X, Gold,
long stats, quick-slot numbers and frame margins fit with no panel overlap.

- `InventoryTabbedPrefabTests`: 7/7 EditMode PASS.
- `QuickBarManagerPlayModeTests`: 4/4 PlayMode PASS, including same-index mirror drop regression.
- Focused test Console: no error entries.
- GameplayUIRoot: removed 1134 stale Inventory RectTransform/Image override lines; preserved other
  component/data/callback overrides and prefab connection.
- Some existing Food/BuffPotion definitions have null item icons. Rendering now hides the missing
  Image instead of presenting a white square; new item icon content is outside this UI asset pass.

Do not mark the redesign fully verified until final image inspection and gameplay drag/filter smoke
checks are recorded here. Existing unrelated Boss Water/scene changes in the worktree are preserved.
