# QuestTracker top-right, no background — 2026-10-22

Owner requests removing Minimap, moving QuestTracker to top-right and refreshing art. Owner explicitly
clarifies that QuestTracker must have icons and NO background. This supersedes prior minimap-preserving
HUD slices, but does not remove M/FullMap or the underlying map snapshot assets.

## Implementation

- Remove Minimap object from shared UnifiedGameplayHUD.prefab and clear its serialized controller reference.
- MapUIAuthoring.Rebuild now rebuilds FullMap only and removes any legacy Minimap instead of recreating it.
- QuestTracker source prefab, nested GameplayUIRoot transform and runtime initialization use top-right
  anchors/pivot (1,1), offset (-16,-16), size 155x190 Canvas units. Source Canvas scale converts this to
  about 372x456 at 1920x1080. UI remains owned by Bootstrap.
- Root Image remains transparent; no panel/background is added. Header and objective/category icons
  stay separate real sprites. Dropdown, scroll, category/order, quest selection and progress logic retained.
- New built-in ImageGen asset: Assets/Resources/UI/Quest/Tracker1920/quest_tracker_divider_pixel_v2.png.
  Thin pixel-art gold/walnut separator with sapphire accent, entirely transparent outside the ornament.
  Generated PNG preserved; alpha bounds are cropped through Sprite metadata only. Point filtering,
  uncompressed texture, no mipmaps. Header underline leaves room before first quest row.
- Runtime fallback, prefab builder, legacy quest skin authoring and asset importer synchronized.

## Generation prompt

Create ONLY a thin horizontal decorative separator sprite for a BACKGROUND-FREE QuestTracker HUD.
Match reference crisp pixel-art beveled antique gold, walnut shadows, tiny sapphire blue diamond centered.
One very slender gold rail left to right with compact angular finials, a single tiny blue diamond in middle.
Overall ornament at most 20 pixels thick relative to ~900 pixels long. No panel, no board, no enclosing
frame, NO BACKGROUND OR OPAQUE FILL anywhere around the thin ornament; genuine alpha transparency
everywhere else. No text, no icons other than tiny centered gem. The game world must be visible through
all empty pixels. This is a small header underline, not an interface panel. Flat orthographic, hard stepped
pixel clusters, entire ornament visible.

Style reference: Assets/Resources/UI/Skills/Panel/V1/skill_panel_board_v1.png. Built-in tool used,
not CLI. Source generated file exec-bc3c6963-ea7a-4c01-a9c9-91687dd41a3b.png. Initial panel generation
was cancelled after owner clarified no background and is not integrated.

## Verification

Editor source inspection: shared HUD has zero MinimapControllers, QuestTracker anchors (1,1), generated
separator sprite assigned. Isolated 1920x1080 UI render with sample Main/Talk and Side/Kill quests reviewed:
Captures/QuestTracker/top_right_no_background.png. Shows category/objective icons, progress, no backdrop;
render background is a plain camera clear color, not gameplay acceptance. Preview sample is not saved
into actual quest data. No Play Mode transition or automated tests. Owner manual map gameplay, collapse,
scroll, scene/session changes and M/FullMap checks remain pending.
