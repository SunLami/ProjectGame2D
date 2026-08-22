# Phase 2 Implementation Report

Ngày bắt đầu: 2026-08-22
Trạng thái: **In progress** (file/slot foundation xong; chưa có domain capture thật)

## Scope đã triển khai

Đúng phạm vi Roadmap Phase 2 — chỉ file/slot foundation, không capture domain gameplay thật
(player/inventory/quest — thuộc Phase 3/4):

- `Assets/Scripts/Save/GameSaveData.cs` — root save DTO, Phase 2 chỉ có `saveVersion`, `saveId`,
  `totalPlayTimeSeconds`. `CurrentSaveVersion = 1`.
- `Assets/Scripts/Save/SaveSlotMetadata.cs` — theo đúng contract đã spec trong
  [Save and World Persistence Plan](SaveAndWorldPersistence.md), cộng field mới `contentChecksum`
  (SHA-256 hex của `save.json`) để đáp ứng yêu cầu checksum của Roadmap.
- `Assets/Scripts/Save/SaveSlotStatus.cs` — `Empty | Valid | Corrupted | IncompatibleVersion`.
- `Assets/Scripts/Save/SaveSlotInfo.cs`, `SaveOperationResult.cs` — kết quả trả cho caller.
- `Assets/Scripts/Save/ISaveSlotRepository.cs` — contract chung.
- `Assets/Scripts/Save/FileSaveSlotRepository.cs` — implementation thật, root path inject qua
  constructor (mặc định `Application.persistentDataPath/Saves`). Atomic write: serialize ra temp
  file → round-trip validate → `File.Replace` (rotate current→backup + swap trong một lời gọi) →
  ghi lại metadata. Đọc: thử `save.json`, fallback `save.backup.json`; version khác
  `CurrentSaveVersion` (cũ hoặc mới hơn) đều là `IncompatibleVersion`; JSON hỏng ở cả hai file là
  `Corrupted`.
- `Assets/Scripts/Save/InMemorySaveSlotRepository.cs` — mock cho UI/test không chạm đĩa.

Toàn bộ là pure C# (không MonoBehaviour, không singleton mới) — chưa có Unity scene/prefab nào bị
thay đổi ở phase này. Ownership Unity-side (ai khởi tạo `ISaveSlotRepository` nào, khi nào) là quyết
định của Phase 3 khi MainMenu controller được dựng.

## Tests

`Assets/Scripts/Tests/EditMode/` (asmdef mới `ProjectGame2D.Tests.EditMode`, theo mẫu
`ProjectGame2D.Tests.PlayMode` đã có từ Phase 1):

- `FileSaveSlotRepositoryTests.cs` — 12 test: empty slot, round-trip write/read, overwrite đọc bản
  mới nhất, delete về Empty, ba slot độc lập, `GetAllSlotInfo` trả đúng 3 slot, current corrupted +
  backup hợp lệ → recover từ backup, cả hai corrupted → `Corrupted`, current corrupted không có
  backup → `Corrupted`, version tương lai (999) → `IncompatibleVersion` và không load, write thất
  bại (saveId rỗng) không phá save hợp lệ đang có, slot id ngoài phạm vi ném
  `ArgumentOutOfRangeException`.
- `InMemorySaveSlotRepositoryTests.cs` — 2 test: round-trip CRUD, từ chối `saveId` rỗng.

## Verification record — 2026-08-22

- Script validation: 0 diagnostics.
- Editor compile sau khi thêm `ProjectGame2D.Tests.EditMode.asmdef`: 0 Error/0 Warning.
- EditMode tests: **14/14 PASS** (0.39s).
- PlayMode tests (regression từ Phase 1, `GameplayReadinessGatePlayModeTests`): **4/4 PASS**
  (0.65s) — không bị ảnh hưởng bởi thay đổi Phase 2.
- Content Validation: 0 error, 60 warning (baseline không đổi), 63 asset checked.
- Không có Unity scene/prefab nào bị chỉnh sửa; không cần Player build lại cho phase này vì không
  có runtime/scene behavior mới cần smoke test ngoài Editor.

## Chưa hoàn thành trong Phase 2 / để lại cho phase sau

- Chưa có UI/MainMenu controller nào gọi `ISaveSlotRepository` — đó là Phase 3 (New Game/Continue
  slot UX). Contract đã sẵn sàng để Phase 3 dùng trực tiếp (`FileSaveSlotRepository` cho production,
  `InMemorySaveSlotRepository` cho UI iteration không ghi đĩa).
- Chưa có `NewGameFactory` tạo `GameSaveData` mặc định — Phase 3.
- Chưa có migration pipeline V1→V2→V3 — Phase 10; hiện tại version khác `CurrentSaveVersion` bị coi
  là `IncompatibleVersion` toàn bộ, không load một phần.
- Chưa tích hợp `InventorySaveData.cs` (DTO thử nghiệm hiện có) vào `GameSaveData` — đó là Phase 4.
- "Chỉ cho một write operation trên một slot tại một thời điểm" hiện được đảm bảo tự nhiên vì toàn
  bộ API đồng bộ (synchronous); chưa cần lock/queue riêng. Nếu Phase 9 đưa I/O sang async, cần thêm
  guard rõ khi đó.

Không đánh dấu Phase 2 hoàn tất trong Roadmap cho tới khi Phase 3 chứng minh được acceptance criteria
"Main Menu đọc metadata mà không load toàn bộ world data" bằng UI thật, và test matrix save đầy đủ
(bao gồm disk write failure thật, không chỉ input validation failure) được chạy trên target thật.
