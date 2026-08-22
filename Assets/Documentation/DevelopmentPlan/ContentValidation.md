# Content Validation Foundation

Editor entry point: `Tools/Project Game/Validate Content`  
Implementation: `Assets/Editor/ContentValidationRunner.cs`

Validator chỉ đọc asset và ghi kết quả vào Console. Nó không rename ID, sửa reference hoặc làm dirty asset.

Baseline chạy ngày 2026-08-21: **0 Error, 60 Warning, 63 assets checked**. Toàn bộ Warning hiện tại
là 60 legacy underscore item IDs đã được lên kế hoạch migration ở Phase 4.

Phase 6 (2026-08-22): sau khi thêm QuestDefinition/QuestCatalog validator và hai quest content asset
(`quest.tutorial.crafting.001`, `quest.main.001`) cùng hai item mới (`item.quest.tutorial_badge`,
`item.material.wood`): **0 Error, 60 Warning (không đổi), 69 assets checked**.

Phase 7 (2026-08-22): sau khi thêm ShopDefinition/ShopCatalog và RecipeDefinition/RecipeCatalog
validator, một shop (`shop.town.general`, 2 stock entry) và hai recipe
(`recipe.material.plank`, `recipe.consumable.health_potion`) cùng ba item mới
(`item.material.iron`, `item.material.plank`, `item.consumable.health_potion`):
**0 Error, 60 Warning (không đổi), 77 assets checked**.

## Phạm vi hiện tại

- Item ID rỗng, trùng và format stable ID.
- Legacy underscore item ID được báo Warning cho tới migration Phase 4.
- Item name, icon và stack invariants.
- Equipment stack policy và required Head/Body/Weapon SpriteLibraryAsset.
- EquipmentCatalog null, duplicate, sai slot và thiếu item.
- ItemDatabase null item, amount không hợp lệ và duplicate loadout entry.
- TileData required tile/audio, null entry và tile thuộc nhiều surface definitions.
- TutorialDefinition/TutorialStepDefinition ID rỗng/trùng, ReachArea thiếu targetAreaId.
- QuestDefinition ID rỗng/trùng/format, objective target ID rỗng, targetCount <= 0, objective
  description rỗng (required presentation field), reward item ID rỗng/quantity invalid,
  prerequisite tham chiếu ID không tồn tại, prerequisite cycle (DFS trên toàn bộ đồ thị quest),
  isMainQuest thiếu prerequisiteQuestIds (Warning), QuestCatalog thiếu/duplicate quest, quest
  không nằm trong catalog nào.
- ShopDefinition ID rỗng/trùng/format, stock rỗng, stock itemId rỗng/trùng/không tồn tại trong bất
  kỳ ItemSO nào, stock price âm, ShopCatalog thiếu/duplicate shop.
- RecipeDefinition ID rỗng/trùng/format, ingredients rỗng, ingredient itemId rỗng/trùng/không tồn
  tại, ingredient quantity <= 0, outputItemId rỗng/không tồn tại, outputQuantity <= 0,
  RecipeCatalog thiếu/duplicate recipe.

## Severity

- **Error:** broken required contract, duplicate identity/reference hoặc data có thể làm runtime sai/crash.
- **Warning:** dữ liệu còn chạy được nhưng cần migration/policy cleanup, ví dụ legacy item ID.

Mỗi message gồm asset path và context object để có thể chọn asset từ Unity Console.

## Cách chạy

1. Chờ Unity compile xong.
2. Chọn `Tools > Project Game > Validate Content`.
3. Sửa Error trước; Warning phải được review nhưng legacy ID chưa chặn Phase 0.
4. Chạy lại cho tới khi summary phù hợp acceptance của phase.

## Giới hạn có chủ đích

- Chưa có Editor Window/dashboard hoặc auto-fix.
- Chưa hook build/CI.
- Chưa validate Recipe/Shop/NPC/World vì các definition chưa tồn tại (Phase 7+).
- Chưa đổi 60 legacy IDs hoặc tạo alias map.

Khi domain mới được thêm, validator của domain đó phải được bổ sung cùng Definition/Catalog, không gom
mọi rule gameplay vào một reflection framework tổng quát.
