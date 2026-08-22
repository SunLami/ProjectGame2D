# Claude → Codex Handoff

Status: `READY_FOR_CODEX`

Ngày: 2026-08-22
Feature: Phase 5 — UI hiển thị Input Tutorial (instruction prompt + skip)

## Bối cảnh

Phase 5 backend (`TutorialManager`, domain event, save/restore, `AreaTriggerZone`) đã hoàn tất, tự
vận hành đúng (28/28 EditMode, 32/32 PlayMode PASS) nhưng **chưa có UI nào hiển thị cho người chơi
thấy**. Chi tiết kiến trúc đầy đủ: [Phase5ImplementationReport.md](../Phase5ImplementationReport.md).

Việc cần Codex làm: dựng một overlay nhỏ trong DemoScene hiển thị `InstructionText` của step tutorial
hiện tại, và nút Skip có confirm. Đây thuần là UI/Canvas — không cần đổi gameplay logic.

## Contract phía Claude cung cấp (đã có sẵn, không cần đổi)

`TutorialManager` (`Assets/Scripts/Tutorial/TutorialManager.cs`), truy cập qua
`TutorialManager.Instance` (persistent singleton, luôn tồn tại trong gameplay scene sau khi
`PlayerSpawnReadinessSource` restore xong):

```csharp
public TutorialStepDefinition CurrentStep { get; }   // null nếu đã completed hoặc chưa có definition
public bool IsCompleted { get; }
public event Action<TutorialStepDefinition> OnStepChanged;   // fire khi qua step mới
public event Action OnTutorialCompleted;                     // fire đúng 1 lần khi xong step cuối
public void Skip();                                           // nhảy thẳng completed, không phát OnStepChanged
```

`TutorialStepDefinition` có field đọc được: `StepId` (string), `Type` (enum, không cần hiển thị),
`InstructionText` (string — đây là nội dung để show lên UI).

## UI cần dựng

1. **Panel instruction** (góc màn hình, ví dụ top-center hoặc top-left, không che HUD/inventory hiện
   có) — hiển thị `CurrentStep.InstructionText`.
   - Ẩn hoàn toàn nếu `TutorialManager.Instance == null` hoặc `CurrentStep == null` (đã completed
     hoặc chưa init xong).
   - Subscribe `OnStepChanged` để đổi text khi qua step mới.
   - Subscribe `OnTutorialCompleted` để ẩn panel (kèm hiệu ứng nhẹ nếu muốn, không bắt buộc).
   - Khi UI vừa `OnEnable`/mở game giữa chừng (ví dụ Continue), đọc luôn `CurrentStep` hiện tại để
     hiển thị đúng ngay lập tức — không đợi event đầu tiên.
2. **Nút Skip** trên panel đó, có **popup confirm** trước khi gọi (theo D-008 — skip tutorial phải có
   xác nhận, không skip ngay khi bấm 1 lần). Sau khi user xác nhận: gọi
   `TutorialManager.Instance.Skip()`.
3. Panel này là **gameplay overlay thuần túy** giống Inventory/Pause hiện có — không đi qua
   `GameStateManager` state machine (tutorial không pause game, không chặn input), chỉ là Canvas hiển
   thị/ẩn theo event ở trên.

## Việc Codex KHÔNG cần làm

- Không cần đổi `TutorialManager`, domain event, hay bất kỳ script nào trong
  `Assets/Scripts/Tutorial/`, `Assets/Scripts/GameManagers/AreaTriggerZone.cs` — nếu thấy cần đổi field
  gì ở đó (ví dụ thêm icon cho step, thêm field mới trong `TutorialStepDefinition`), báo lại Claude
  qua `CodexToClaude.md` thay vì tự sửa (đây là ScriptableObject data contract, đổi ẩu có thể vỡ save
  cũ hoặc content asset đã tạo).
- Không cần lo về restore/save — `TutorialManager.RestoreState()` (backend) đã đảm bảo UI mở lên giữa
  chừng vẫn thấy đúng step hiện tại qua `CurrentStep`.
- Chưa cần làm UI cho `AreaTrigger_Town`/`ReachArea` riêng — step đó cũng chỉ là một `InstructionText`
  bình thường như các step khác, panel dùng chung.

## Nội dung step hiện có (để tham khảo hiển thị, đọc thật từ asset, đừng hardcode text trong UI script)

6 step trong `Assets/Tutorial/Tutorial_TutorialArea.asset`: Move → Sprint → Attack → OpenInventory →
EquipItem → ReachArea (`area.town`, placeholder position `(10,0,0)` trong DemoScene, sẽ dời khi có Town
thật). `InstructionText` hiện tại là placeholder — nếu cần văn bản hiển thị đẹp hơn, có thể tự sửa nội
dung field đó trực tiếp trên asset qua Unity Editor (đây là content, không phải code, Codex có thể sửa
tự do), không cần hỏi lại Claude cho việc đổi text thuần túy.

## Test cần có phía Codex (nếu theo đúng quy trình Quality Strategy)

- Manual: New Game → panel hiện đúng step Move → đi bộ → panel đổi sang Sprint → ... → sau step cuối
  panel ẩn.
- Manual: bấm Skip → confirm popup hiện → xác nhận → panel ẩn ngay, không đi qua step trung gian.
- Manual: Continue game đã có tutorial dở dang → panel hiện đúng step đã lưu ngay khi vào scene.

## Phạm vi Claude không chỉnh trực tiếp

Toàn bộ Canvas/hierarchy/layout/font/màu cho panel này thuộc Codex. Khi xong, cập nhật
`CodexToClaude.md` để Claude biết UI đã sẵn sàng (không cần thay đổi gì phía backend trừ khi phát sinh
gap mới).
