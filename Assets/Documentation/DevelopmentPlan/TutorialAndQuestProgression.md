# Tutorial and Quest Progression Plan

Tutorial/Quest authoring tuân theo [Data-Driven Development Guide](DataDrivenDevelopment.md):
Definition asset bất biến, Runtime State riêng và Save DTO chỉ lưu stable ID/progress delta.

## Nguyên tắc thiết kế

Tutorial là hướng dẫn, không phải chế độ khóa sandbox. Người chơi có thể bỏ qua hoạt động hướng dẫn
và khám phá thế giới. Tutorial Quest chain là progression gate cho Main Quest, không phải gate cho
movement, shop, crafting hoặc exploration trừ khi game design chỉ rõ từng ngoại lệ.

## Hai lớp tutorial

### Input tutorial

Hướng dẫn thao tác cơ bản trong Tutorial Area:

```csharp
public enum TutorialStep
{
    None,
    Move,
    Sprint,
    Attack,
    OpenInventory,
    EquipItem,
    TravelToTown,
    Completed
}
```

Step progression đến từ typed event như `PlayerMoved`, `PlayerAttacked`, `InventoryOpened` và
`ItemEquipped`. Không đọc trực tiếp phím cụ thể; như vậy remap/controller vẫn hoàn thành tutorial.

### Tutorial Quest chain

Nhận từ Main NPC tại Town và có thể gồm:

- Nói chuyện NPC.
- Craft item.
- Mua item.
- Gather resource.
- Kill enemy theo type/area.
- Return to NPC.

Input tutorial completed không tự động đồng nghĩa Tutorial Quest completed.

## Quest definition

Quest authoring bằng ScriptableObject hoặc data asset:

```csharp
public sealed class QuestDefinition : ScriptableObject
{
    public string questId;
    public string displayName;
    public string[] prerequisiteQuestIds;
    public QuestObjectiveDefinition[] objectives;
    public QuestRewardDefinition rewards;
    public bool isTutorialQuest;
    public bool isMainQuest;
}
```

`questId` là contract save; đổi display name/localization không được đổi ID.

Runtime progress tách khỏi asset:

```csharp
public enum QuestStatus
{
    Locked,
    Available,
    Active,
    ReadyToTurnIn,
    Completed,
    Failed
}
```

## Objective types ban đầu

| Objective | Event nguồn | Điều kiện validation |
|---|---|---|
| Talk | `NpcConversationCompleted` | Đúng NPC ID/conversation outcome |
| Obtain | `InventoryItemAdded` | Đúng item ID; chốt consume hay possession |
| Craft | `ItemCrafted` | Craft transaction thành công |
| Purchase | `ItemPurchased` | Shop transaction thành công |
| Gather | `ResourceGathered` | Đúng resource/area nếu quest yêu cầu |
| Kill | `EnemyKilled` | Đúng enemy type/area/credit owner |

Mỗi objective phải định nghĩa semantics rõ. Ví dụ Obtain có hai lựa chọn:

- Counter tăng theo item từng nhặt được, không giảm khi dùng.
- Objective kiểm tra player đang sở hữu đủ item khi turn-in.

Không trộn hai semantics trong cùng type mà không có field cấu hình rõ.

## Main Quest gate

Khuyến nghị dùng prerequisite quest IDs làm nguồn sự thật:

```text
Main Quest 001 available
IFF all required Tutorial Quest IDs are Completed
```

Có thể cache `MainQuestUnlocked` để UI/NPC nhanh hơn, nhưng khi load phải reconciliation với completed
quest list. Không chỉ lưu một bool duy nhất vì content prerequisite có thể thay đổi.

## NPC roles

Một NPC có thể cung cấp nhiều capability qua component/service rõ ràng:

- `QuestGiver`.
- `ShopInteraction`.
- `CraftingInteraction`.
- `DialogueInteraction`.

NPC identity dùng stable `npcId`. UI tương tác chọn capability phù hợp; NPC MonoBehaviour không trực
tiếp sửa QuestManager internals.

Main NPC availability:

```text
Before Tutorial Quest accepted: offer Tutorial Quest
During chain: dialogue/progress/turn-in
After chain completed: offer Main Quest
```

## Reward transaction

Turn-in phải atomic:

1. Validate quest `ReadyToTurnIn`.
2. Validate inventory capacity nếu reward cần slot.
3. Consume required turn-in items nếu có.
4. Grant rewards.
5. Mark quest completed.
6. Persist dirty state / schedule save.
7. Emit `QuestCompleted` đúng một lần.

Nếu reward không thể nhận, không consume item và không mark completed. Có thể dùng reward inbox sau
này, nhưng không nằm trong phase đầu.

## Save/load rules

- Save tutorial step/completed.
- Save quest status, current objective index và counters.
- Restore bằng API không phát gameplay event.
- Rewards không được grant lại khi restore completed quest.
- Event subscription phải idempotent.
- Quest definition mất sau update tạo recovery warning; không crash toàn save.

## Daily Quest compatibility

Daily Quest chưa triển khai nhưng foundation cần hỗ trợ:

- Quest instance ID khác definition ID.
- Generated/accepted/expiry timestamp.
- Reset policy dựa trên game/server clock được định nghĩa sau.
- Daily Quest không nằm trong prerequisite mặc định của Main Quest.

Không thêm daily reset logic vào Tutorial/QuestManager core ở phase hiện tại.

## Authoring validation

Editor validation bắt buộc:

- Quest ID rỗng/trùng.
- Prerequisite không tồn tại hoặc cycle.
- Objective target ID không resolve.
- Target count <= 0.
- Reward item không tồn tại hoặc quantity invalid.
- Main Quest không có prerequisite mong đợi.

## Acceptance scenario end-to-end

```text
Create New Game
→ complete Move/Sprint/Attack tutorial
→ save and reload between tutorial steps
→ travel to Town
→ accept Tutorial Quest
→ craft, purchase, gather and kill objectives
→ save/reload between objectives
→ turn in without duplicate reward
→ Main Quest becomes available
→ load again and verify unlock persists
```

Song song, test một player bỏ Tutorial Quest vẫn có thể khám phá, gather, shop và craft nhưng không
thể nhận Main Quest.
