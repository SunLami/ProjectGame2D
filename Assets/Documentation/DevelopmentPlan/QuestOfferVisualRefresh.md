# Quest Offer pixel asset refresh — 2026-10-22

Owner requested regenerating Quest Offer to match existing UI. Built-in image_gen used for three transparent PNG assets; previous artwork retained. Style reference: Skills/Panel/V1/skill_panel_board_v1.png. Geometry reference: QuestAccept1920/quest_accept_board_dynamic_rewards_v3.png.

## Deliverables

Under Assets/Resources/UI/Quest/QuestAccept1920/:
- quest_offer_board_pixel_v4.png
- quest_offer_accept_pixel_v2.png
- quest_offer_decline_pixel_v2.png

Gold/walnut stepped pixel borders, dark charcoal brown interior, sapphire title accent; blue Accept and walnut Decline. Text, objective icons, rewards and button labels remain Unity objects. Sprites use Point filtering, uncompressed RGBA, no mipmaps; button transparent margins cropped through Sprite metadata only. PNG pixels and alpha are untouched after generation.

QuestAcceptPopup.prefab and QuestAcceptPopupPrefabBuilder reference new assets. Objectives title/rows moved below the new divider to prevent overlap. Existing quest logic, callbacks, save/progression and Bootstrap ownership retained.

## Prompt set

Board: Create a replacement Unity 2D RPG Quest Offer panel sprite. First reference is STYLE source: crisp chunky pixel-art antique golden beveled trim, warm walnut brown and dark charcoal inset, hard stepped highlights, no smooth painterly noisy texture. Second reference is GEOMETRY source: portrait 4:5 canvas, panel bounding box with transparent exterior margin, top blank title plaque, large content area. Two thin golden horizontal structural dividers around 26% and 60% canvas height. Small sapphire accent above title. Clean empty safe areas. No baked buttons, text, letters, numbers, icons or reward slots. Front orthographic, genuine transparent exterior.

Accept: One primary RPG button sprite, wide chamfered rectangle; reference chunky crisp pixel-art antique gold double bevel and warm walnut shadows, stepped corners. Dark sapphire blue inner surface with subtle pixel texture. No text/icons/gems. Transparent exterior, complete silhouette.

Decline edit: Preserve exact Accept frame geometry, canvas/padding and corner highlights; change only blue fill to dark walnut charcoal brown (#282019) with subtle pixel texture. No lettering/symbols. Preserve alpha.

Generation source files remain under Codex generated_images/01a16188-f42e-7381-b968-7465257dd86a (exec-698bbe0b, exec-e7549fb8, exec-2e1d6d92).

## Verification and limits

Three generated images visually reviewed. Isolated 1920x1080 Editor render with temporary Training Dummy sample reviewed after spacing correction: Captures/QuestOffer/quest_offer_v4_preview.png. Sample labels/icons are preview-only, not runtime acceptance or authored quest data. No Play Mode transition, automated tests, quest acceptance transaction or Player build performed. Console includes Grass_No_Outline RuleTile KeyNotFoundException during Editor work; not treated as a Quest UI pass or silently fixed in this scope.
