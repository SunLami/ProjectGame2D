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
| Replant | `CropReplanted` | Đúng cropId/area nếu quest yêu cầu (plant lại sau harvest, không phân biệt lần trồng đầu) |
| Fish | `FishCaught` | Không phân biệt loài cá; `TargetId` chỉ là sentinel bắt buộc khác rỗng cho validation |

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

Production content in `MapNhat` ("Heorte" in-fiction) is Act I - "A Stop in Heorte", and follows
this concrete order (replaces the previous Wigheard/Cenhelm tutorial+main chain entirely; see
DecisionRegister.md 2026-11-01):

```text
Wigheard (npc.town.guildmaster)
  quest.act1.new_journey        Talk/train Wigheard           -> reward: Training Sword (sword_lvl1)
Cuthbert (npc.town.mixologist)
  quest.act1.drink_before_road  Talk + buy 5 Carrot/Grape Juice (autoTurnIn)
Oswin (npc.town.gastronome)
  quest.act1.stocking_up_oswin  Talk Oswin (2-1a, autoTurnIn)
Eadric (npc.town.chapman)
  quest.act1.stocking_up_eadric Talk Eadric (2-1b, autoTurnIn)
(no giver -- auto-accepted by TrainingAreaQuestFlow on stocking_up_eadric turn-in)
  quest.act1.back_to_fields     Harvest 15 + Replant 5 each of Carrot/Grape/Lemon (2-2, autoTurnIn)
Eadric (npc.town.chapman)
  quest.act1.back_to_eadric     Talk Eadric (2-3a) -> reward: 5 Carrot, 10 Meat (autoTurnIn)
(no giver -- auto-accepted by TrainingAreaQuestFlow on back_to_eadric turn-in)
Oswin (npc.town.gastronome)
  quest.act1.back_to_oswin      Talk Oswin (2-3b) -> reward: food/Bread (autoTurnIn)
  quest.act1.cast_a_line        Fish x5 -> return to Oswin -> reward: food/Bread (manual turn-in)
Merric (npc.town.artificer)
  quest.act1.ring_of_forge      Visit (Talk) Merric -> reward: 5 Gold Bar; IsSmithingStationUnlocked
Agnes (npc.town.hermetist)
  quest.act1.far_from_your_kind Visit (Talk) Agnes; IsBlackArtsStationUnlocked
```

No quest in this Act carries `isMainQuest`/`isTutorialQuest`; the Main Quest gate stays locked
until Act 2 defines its own Tutorial Quest chain. "Visit the smithing station" / "visit the
suspicious merchant" are authored as `Talk` against the NPC, not a new `VisitStation` type -- no
physical station GameObject exists yet. `QuestManager.IsSmithingStationUnlocked` /
`IsBlackArtsStationUnlocked` expose completion of Quest 4/5 for whatever station interactable is
built later.

Every quest in this chain -- except `quest.act1.back_to_fields`, which has no giver NPC -- is
accepted and turned in purely through that NPC's own dialogue (`QuestNpcInteractionUI`): talking to
its giver while the quest is `Available` opens that quest's `_questDialogues` entry, which closes
into a `QuestAcceptPopupUI` accept/decline card; accepting immediately reports the conversation,
which can satisfy a same-visit Talk objective (status becomes `ReadyToTurnIn` right there). All 8 of
these quests have `AutoTurnIn` off, and `QuestNpcInteractionUI` deliberately never turns in during
the same interaction that just accepted the quest (see its `offered == null` guard in
`CompleteDialogueInteraction`/`HandleQuestDecision`) -- turning in always needs a separate, later
visit to the giver, even one whose only objective is talking to that same NPC: the reward and
`Completed` status only land on that second conversation. `quest.act1.cast_a_line` is the same
pattern for a different reason (fishing happens away from Oswin, so a return visit is unavoidable
regardless). Only `quest.act1.back_to_fields` auto-completes itself with no NPC visit at all, since
it has no giver to return to. A giver NPC shows a "!" over its head while it has something
`Available` to offer and a "?" while a tracked quest is `ReadyToTurnIn` there
(`QuestNpcInteractionUI`'s marker, driven by `QuestNpcInteractionService`) -- this, plus the forced
second visit, is how the player is led NPC-to-NPC through the chain with a real "hand it in" beat
each time, not narration or a UI arrow. Do not auto-accept a quest that still has a giver (via
`TrainingAreaQuestFlow`, `TutorialManager`'s `WaitForQuest` step, or similar glue): doing so skips
the `Available` state the marker and dialogue resolution both depend on, so the giver falls back to
its generic greeting instead of that quest's line, and a same-NPC two-objective quest (e.g. "Talk A,
then talk B") cannot resolve the second NPC's dialogue at all, since `QuestNpcInteractionUI` only
resolves a quest it is the giver/turnIn for -- never a quest it is merely a Talk objective's
mid-quest target. For a quest whose narrative needs the player to visit two NPCs in order, split it
into two single-NPC quests chained by prerequisite instead (e.g. `quest.act1.stocking_up_oswin` ->
`quest.act1.stocking_up_eadric`, `quest.act1.back_to_eadric` -> `quest.act1.back_to_oswin`), each
resolving its own giver's dialogue on its own.

### Input Tutorial hand-off for "equip the sword" / "attack a dummy"

`quest.act1.new_journey`'s reward (the Training Sword) is not itself gated on equipping or
attacking anything -- per owner decision, that guidance lives entirely in the separate, non-gating
Input Tutorial layer (`TutorialManager` + `Tutorial_TutorialArea.asset`, see "Input tutorial"
above), never as quest objectives, so a player who ignores the banner can still go straight to
Cuthbert. The authored chain is Move -> Sprint -> WaitForTrainer (`WaitForQuest`, target
`quest.act1.new_journey`) -> OpenInventory -> EquipItem -> AttackDummy (new `Attack`-type step). The
WaitForTrainer step waits for Wigheard's quest to actually `Completed` through the real dialogue
flow above (no auto-accept -- see the anti-pattern note just above); `TutorialManager.Advance()`
skips a `WaitForQuest` step immediately if its target is already `Completed` by the time the chain
reaches it, since `QuestManager.QuestTurnedIn` only fires forward and the moment could otherwise
already be missed (e.g. the player never sprints before reaching Wigheard). AttackDummy only needs
`Player.PlayerAttacked` (a generic swing, no hit required) and has no quest/Kill-objective hook of
its own; the existing `Manequin_Training_1/2/3` dummies under `Forest_MainMap/TrainingArea` (near
Wigheard) are purely a visual target for it, nothing more is authored for them.

NPCs that also run a shop (Merric, Agnes, Eadric, Oswin, Cuthbert) use `QuestNpcInteractionUI` for
this, not `TraderNpcInteractionUI` -- `QuestNpcInteractionUI` now also opens Shop/Crafting/Storage
when a dialogue choice resolves to that commerce outcome (merged from `TraderNpcInteractionUI`), so
one NPC interaction component covers both quest and shop capability instead of two competing for
the same trigger/click.

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
- `isDailyQuest` hiện chỉ là metadata presentation để QuestTracker xếp Daily sau Main và Side; không
  ngụ ý quest đã có generation/reset/expiry runtime.
- `isDebugQuest` dành riêng cho content kiểm thử: chỉ nhận được trong Editor/Development Build, không
  được ghi vào save, bị bỏ qua khi restore và không làm session dirty.
- Generated/accepted/expiry timestamp.
- Reset policy dựa trên game/server clock được định nghĩa sau.
- Daily Quest không nằm trong prerequisite mặc định của Main Quest.

Không thêm daily reset logic vào Tutorial/QuestManager core ở phase hiện tại.

## Track, untrack và abandon

- Tối đa một quest `Active`/`ReadyToTurnIn` được track. Direction indicator và HUD tracker chỉ đọc
  quest đó; untrack không thay đổi tiến độ.
- Quest mới nhận tự track nếu chưa có quest nào đang track. Hoàn thành/turn-in quest đang track sẽ
  tự clear lựa chọn.
- Abandon chỉ hợp lệ cho quest Active/Ready có `giverNpcId`: runtime record và toàn bộ objective
  progress bị xóa. Quest trở lại `Available` nếu prerequisite vẫn thỏa, nhưng chỉ được nhận lại qua
  `QuestNpcInteractionService` tại đúng giver NPC; không có resume progress.
- Quest không có giver (auto/system/tutorial nội bộ) không được abandon vì không có đường nhận lại.

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
