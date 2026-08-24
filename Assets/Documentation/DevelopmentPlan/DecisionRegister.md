# Architecture and Game-Design Decision Register

File này ghi các quyết định ảnh hưởng kiến trúc. `Proposed` là mặc định khuyến nghị để team có thể
tiếp tục thiết kế; phải đổi thành `Accepted` trước phase implementation liên quan.

| ID | Quyết định | Trạng thái | Mặc định đề xuất | Cần chốt trước |
|---|---|---|---|---|
| D-001 | Vai trò DemoScene | Accepted từ yêu cầu | Integration playground; feature được đóng prefab/installer rồi kéo sang scene thật | Phase 0 |
| D-002 | Số save slot | Accepted từ yêu cầu | Chính xác 3 slot | Phase 2 |
| D-003 | Continue behavior | Proposed | Mở danh sách save hợp lệ; thêm Continue Last sau | Phase 3 |
| D-004 | New Game overwrite | Proposed | Slot có dữ liệu cần confirm rõ; không overwrite một click | Phase 2 |
| D-005 | Save khi combat | Proposed | Chỉ manual save ngoài combat/danger; UI giải thích lý do | Phase 9 |
| D-006 | Inventory pause world | Proposed | Pause world ở bản offline đầu | Phase 1 |
| D-007 | Dialogue pause world | Proposed | World chạy nhưng player input khóa; đánh giá nguy cơ bị tấn công | Phase 5/6 |
| D-008 | Tutorial skip | Proposed | Cho skip có confirm; vẫn cần Tutorial Quest để mở Main Quest | Phase 5 |
| D-009 | Tutorial Quest bắt buộc | Accepted từ yêu cầu | Không bắt buộc sandbox; bắt buộc để nhận Main Quest | Phase 6 |
| D-010 | Player death | Open | Load active slot, respawn checkpoint hay mất tài nguyên? | Phase 1/3 |
| D-011 | Initial save timing | Proposed | Ghi save sau New Game restore thành công | Phase 3 |
| D-012 | Autosave | Proposed | Chưa có ở foundation; manual save trước | Phase 9/10 |
| D-013 | Character creation scope | Open | Tối thiểu character name; appearance nếu hệ thống sẵn sàng | Phase 3 |
| D-014 | Obtain objective semantics | **Accepted — 2026-08-22** | `ObtainObjectiveMode` field trên từng `QuestObjectiveDefinition`: `CountAcquired` (counter cộng dồn, không giảm khi dùng) hoặc `RequirePossession` (kiểm tra sở hữu hiện tại >= targetCount, không phải counter). Không có rule ngầm định toàn cục. | Phase 6 |
| D-015 | Resource respawn clock | **Accepted — 2026-08-23** | `nextRespawnUtcTicks` lưu `DateTime.UtcNow.Ticks` tuyệt đối tại thời điểm harvest + respawn duration; `IsAvailable` so sánh trực tiếp với `DateTime.UtcNow.Ticks` hiện tại, không polling. Elapsed thời gian thật (kể cả lúc app đóng) tự nhiên được tính vì dùng UTC tuyệt đối, không phải in-game playtime tích lũy; chưa có catch-up/rate-limit/batch simulation đặc biệt cho khoảng offline dài. | Phase 8 |
| D-016 | Save format | Proposed | JSON versioned + backup; cân nhắc compression/encryption sau | Phase 2 |
| D-017 | Return Main Menu dirty state | **Accepted — 2026-08-23** | Đúng theo proposed default: `GameplaySessionController.OnConfirmationRequired` khi dirty → Save and Return / Return Without Saving / Cancel; clean session Return trực tiếp không hỏi. | Phase 9 |
| D-024 | Dirty-session event contract | **Accepted — 2026-08-23** | `SessionDirtyTracker` (scene service) đánh dấu dirty qua: `InventoryManager.OnInventoryChanged`, `EquipmentManager.OnEquipmentChanged`, `PlayerStat.OnLevelUp`/`OnExperienceChanged`, `TutorialManager.OnStepChanged`/`OnTutorialCompleted`, `QuestManager.QuestAccepted`/`QuestProgressChanged`/`QuestCompleted`/`MainQuestUnlocked`, `WorldDomainEvents.WorldObjectChanged` (mới). Player position/di chuyển đơn thuần **không** làm dirty. `GameSessionManager.MarkDirty()` tự no-op khi `IsRestoring == true` nên toàn bộ restore path (kể cả seed New Game) không bao giờ dirty giả. | Phase 9 |
| D-018 | Settings ownership | Accepted kiến trúc | Shared SettingsService, hai navigation UI riêng | Phase 1 |
| D-019 | Production world scene topology | Open | Chưa chốt một hay nhiều scene; save luôn dùng area/scene ID ổn định | Trước production world |
| D-020 | Data loading backend | **Accepted — 2026-08-22** | Domain phụ thuộc `IItemResolver`; `ResourcesItemResolver` là backend migration ban đầu | Phase 4 |
| D-021 | Definition authoring reference | **Accepted — 2026-08-22** | Typed asset reference trong Inspector (`ItemSO`/`EquipmentItemSO`), stable `itemId` tại save/runtime boundary | Phase 4–7 |
| D-022 | Legacy item ID convention | **Accepted — 2026-08-22** | Giữ nguyên 60 legacy underscore itemId hiện có (`sword_lvl1`, `body_lvl9`, ...) làm stable ID chính thức cho content hiện có; **không** bulk rename. Convention dot-namespace (`item.weapon.sword.001`) chỉ áp dụng cho item MỚI thêm sau Phase 4. Validator tiếp tục báo Warning (không phải Error) cho các legacy ID này. | Phase 4 |
| D-025 | Save migration strategy | **Accepted — 2026-08-23** | `SaveMigration`/`ISaveMigrationStep` chạy chuỗi N→N+1 additive-default (không parse raw JSON riêng từng version) tận dụng việc `JsonUtility` bỏ qua field lạ/thiếu. Chạy in-memory tại `FileSaveSlotRepository.TryLoadValid`, không bao giờ rewrite file trên đĩa; chỉ save thật (ghi mới) mới nâng version trên đĩa. Save cũ hơn `SaveMigration.MinimumSupportedVersion` (hiện = 1) hoặc mới hơn `CurrentSaveVersion` vẫn là `IncompatibleVersion`, không đoán shape. | Phase 10 |
| D-026 | Player build GUI verification trong môi trường này | **Accepted — 2026-08-23** | Windows Player build tự thân thành công (0 error/warning, `Player.log` init sạch), nhưng cửa sổ game không thể được điều khiển/chụp màn hình đáng tin cậy qua computer-use automation hiện có trong môi trường này (window rect hợp lệ từ Win32 API nhưng không khớp nội dung nhìn thấy được trong screenshot). Đây là giới hạn tooling môi trường, không phải lỗi code. Click-through smoke test (New Game→DemoScene→Save→Return→Continue→Quit) cần chạy thủ công bởi user/Codex trên máy thật. | Phase 10 |
| D-027 | Save Game slot-picker semantics | **Accepted — 2026-08-23** | Pause Menu "Save Game" mở slot picker thay vì ghi thẳng vào `ActiveSlotId`. Slot Empty ghi trực tiếp; slot Valid/Corrupted/IncompatibleVersion đều bắt buộc `OnSaveSlotConfirmationRequired` trước khi ghi (không có ngoại lệ im lặng cho bất kỳ status nào). Ghi thành công vào slot khác `ActiveSlotId` hiện tại ("Save As") tự động chuyển `ActiveSlotId` sang slot đó qua `GameSessionManager.SetActiveSlotId` — không reset `IsDirty`/`IsRestoring`/play-time base, chỉ đổi nhãn session. `DeleteSlot` không tự hỏi xác nhận (UI phải tự hỏi trước khi gọi, giống `MainMenuController.DeleteSlot`); xóa slot đang active không phá session đang chạy, save tiếp theo vào slot đó tự nhiên được coi là Empty vì không có autosave (D-012) nào có thể nhắm nhầm vào slot vừa xóa. | Phase 10 |

## Quy tắc cập nhật

Mỗi decision khi Accepted cần ghi:

- Ngày và người/nhóm chốt.
- Lý do.
- Hệ thống/tài liệu bị ảnh hưởng.
- Có cần migration data hoặc UI không.

Ví dụ:

```text
D-005 — Accepted — 2026-xx-xx
Manual save bị khóa khi player đang trong combat hoặc trong danger area.
Lý do: tránh restore enemy/projectile transient state phức tạp ở version đầu.
Ảnh hưởng: CombatState query, Pause save button disabled reason, QA save matrix.
```

## Chi tiết quyết định Phase 4 — 2026-08-22

```text
D-020 — Accepted — 2026-08-22 — Claude (Phase 4 baseline, người dùng xác nhận)
Domain code (InventoryManager/EquipmentManager/PlayerSpawnReadinessSource) chỉ biết
IItemResolver.TryResolve(itemId, out item); ResourcesItemResolver (Resources.LoadAll) là
implementation duy nhất hiện có. Lý do: tách domain khỏi cơ chế load cụ thể, cho phép đổi backend
(Addressables, catalog asset) sau này mà không sửa domain logic.
Ảnh hưởng: Assets/Scripts/Inventory/IItemResolver.cs, ResourcesItemResolver.cs;
InventoryManager.LoadFromSaveData(resolver overload); PlayerSpawnReadinessSource.

D-021 — Accepted — 2026-08-22 — Claude (Phase 4 baseline, người dùng xác nhận)
Authoring tiếp tục dùng typed asset reference (ItemSO/EquipmentItemSO) trong Inspector
(ItemDatabase.Entry.item, EquipmentCatalog arrays); ranh giới save/runtime chuyển sang stable
itemId (string) qua resolver. Không đổi gì ở authoring layer hiện có.
Ảnh hưởng: không có thay đổi asset/authoring; chỉ xác nhận pattern đã tồn tại là đúng hướng.

D-022 — Accepted — 2026-08-22 — Claude + người dùng (đã hỏi trực tiếp trước khi code)
Lý do: 60 itemId hiện tại (dạng sword_lvl1) chưa theo convention dot-namespace trong
DataAssetStableIdInventory.md, nhưng bulk-rename 60 asset là rủi ro cao (có thể vỡ
ItemDatabase/EquipmentCatalog reference) và không phải "thay đổi nhỏ nhất có thể kiểm chứng".
Chưa có save nào release nên không có migration cần thiết — chỉ cần chốt rằng dạng legacy này
CHÍNH THỨC là stable ID hợp lệ.
Ảnh hưởng: ContentValidation.md giữ nguyên legacy ID ở mức Warning; DataAssetStableIdInventory.md
migration gate "chốt mapping" coi như đã hoàn tất bằng quyết định này, không phải bằng rename.
```

## Chi tiết quyết định Phase 6 — 2026-08-22

```text
D-014 — Accepted — 2026-08-22 — Claude (Phase 6 baseline)
Obtain objective hỗ trợ hai semantics tách biệt qua field ObtainObjectiveMode trên
QuestObjectiveDefinition thay vì một rule ngầm định áp cho toàn hệ thống Quest:
- CountAcquired: counter tăng theo InventoryItemAdded, không giảm khi item bị dùng/bán/equip.
- RequirePossession: không dùng counter; kiểm tra InventoryManager.HasItemId(itemId, targetCount)
  mỗi khi có InventoryItemAdded khớp target, hoàn thành objective ngay khi đang sở hữu đủ.
Ly do: TutorialAndQuestProgression.md yeu cau ro "khong tron hai semantics trong cung type ma
khong co field cau hinh ro" -- field tuong minh de designer chon dung y muon tung quest thay vi
Claude tu quyet dinh mot rule chung.
Anh huong: Assets/Scripts/Quest/ObtainObjectiveMode.cs, QuestObjectiveDefinition.cs,
QuestManager.HandleObtain, InventoryManager.HasItemId (moi, additive).
```

## Chi tiết quyết định Phase 8 — 2026-08-23

```text
D-015 — Accepted — 2026-08-23 — Claude (Phase 8 baseline)
ResourceNodeInteractable luu nextRespawnUtcTicks = DateTime.UtcNow.Ticks + respawnDuration tai thoi
diem harvest. IsAvailable so sanh truc tiep saved ticks voi DateTime.UtcNow.Ticks hien tai -- khong
Update()/polling moi frame, khong tick nen tang. Vi dung UTC tuyet doi (khong phai playtime tich luy
trong game), thoi gian thuc troi qua ke ca luc ung dung dong deu tu nhien duoc tinh vao respawn --
day la lua chon don gian nhat thoa man "khong lam save phinh", khong phai gia dinh ngam ve balance.
Chua co catch-up/rate-limit/batch simulation cho truong hop offline rat dai (vi du hang tram node
respawn dong loat) -- de lai cho phase sau neu game design can gioi han.
Anh huong: Assets/Scripts/World/ResourceNodeInteractable.cs, WorldObjectState.NextRespawnUtcTicks,
WorldObjectSaveData.nextRespawnUtcTicks.
```

## Chi tiết quyết định Phase 9 — 2026-08-23

```text
D-017 — Accepted — 2026-08-23 — Claude (Phase 9 baseline)
Prompt xac nhan chi hien khi GameSessionManager.IsDirty == true. GameplaySessionController.
RequestReturnToMainMenu()/RequestQuit() fire OnConfirmationRequired(kind) va khong tu lam gi khac --
UI hien popup roi goi ConfirmSaveAndReturn/ConfirmReturnWithoutSaving/CancelReturnToMainMenu (hoac
ban Quit tuong ung). Cancel khong doi GameState (dung "popup xac nhan la UI navigation con" cua
UIAndInteractionFlows.md). Save-and-X chi thuc su chuyen scene/quit sau khi ghi file thanh cong;
that bai giu nguyen Paused va bao OnOperationFailed.
Anh huong: Assets/Scripts/GameManagers/GameplaySessionController.cs,
GameplaySessionConfirmationKind.cs, GameplaySessionOperationResult.cs.

D-024 — Accepted — 2026-08-23 — Claude (Phase 9 baseline)
Ly do: RuntimeArchitecture.md "Event rules" da de nghi pattern IsRestoring nhung chua co
implementation cu the cho dirty-tracking; task Phase 9 yeu cau de xuat contract toi thieu neu chua
chot. Field/gameplay progression that su moi lam dirty; di chuyen don thuan khong dirty vi muc dich
dirty-flag la canh bao "co the mat tien do chua luu khi roi gameplay", khong phai theo doi moi thay
doi vi tri.
Anh huong: Assets/Scripts/GameManagers/GameSessionManager.cs (IsDirty/IsRestoring/MarkDirty/
ClearDirty/BeginRestore/EndRestore), SessionDirtyTracker.cs (moi), Assets/Scripts/World/
WorldDomainEvents.cs (moi), PlayerSpawnReadinessSource.cs (boc RestoreAll trong BeginRestore/
EndRestore).
```

**D-005 (Save khi combat) và D-012 (Autosave) vẫn `Proposed`, KHÔNG triển khai ở Phase 9:**
project hiện chưa có khái niệm "combat state"/"danger area" nào trong `GameStateManager` hay domain
khác để D-005 có thể bám vào (không có state/flag nào đánh dấu "đang combat"); D-012 (autosave) nằm
ngoài phạm vi Phase 9 theo đúng ghi chú "Chưa có ở foundation; manual save trước" — Phase 9 chỉ làm
manual Save Game. Không tự chế một cơ chế combat-lock để "xong" D-005 vì sẽ là quyết định gameplay
chưa được xác nhận; để lại nguyên trạng cho phase sau khi có combat state thật.

## Những quyết định không được hard-code trước khi chốt

- Hình phạt khi chết.
- Save/load trong combat.
- Offline resource regeneration.
- Character appearance serialization.
- Daily Quest reset clock.

Code foundation nên cung cấp extension point nhưng không tự chọn gameplay rule thay designer.
