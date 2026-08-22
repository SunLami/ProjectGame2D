# Quality and Verification Strategy

## Definition of Done chung

Một feature chỉ hoàn tất khi:

- Compile không error/warning mới có ý nghĩa.
- Unit/EditMode tests cho logic thuần pass.
- PlayMode/integration tests cho Unity lifecycle pass.
- Manual acceptance scenario pass trong Editor.
- Player build smoke test pass.
- Save schema/docs được cập nhật nếu có data change.
- Không tạo duplicate ID/missing serialized reference.
- Definition/Runtime/Save boundary và catalog validator của domain được kiểm chứng.
- Console sạch exception trong luồng chính và failure path.

## Test pyramid

### Pure C# tests

Ưu tiên cho:

- State transition/history rules.
- Save validation và migration.
- Slot selection rules.
- Quest prerequisite graph.
- Objective counters.
- Reward transaction planning.
- New Game default factory.

Các class này không nên phụ thuộc MonoBehaviour để chạy test nhanh.

### EditMode tests

- ScriptableObject item/quest catalog validation.
- Definition asset không bị mutation trong Play Mode.
- Duplicate/rỗng itemId, questId, persistentId.
- Serialization round-trip DTO.
- Migration fixture từ version cũ.
- Spawn/area registry validation.

### PlayMode tests

- Bootstrap MainMenu/DemoScene và scene gameplay target được cấu hình.
- GameState policy thực sự khóa Player input.
- Scene load + restore order.
- Inventory/equipment restore và stat recalculation.
- Tutorial/quest event subscription lifecycle.
- World object restore.

### Manual/build tests

- Full UX bằng keyboard/mouse.
- Build độc lập Editor trên target platform.
- Persistent path permissions và file recovery.
- Performance/GC khi save world thật.

## Save test matrix

| Trường hợp | Kết quả bắt buộc |
|---|---|
| Ba slot trống | New Game enabled, Continue disabled |
| Một slot hợp lệ | Chỉ slot đó Continue được |
| Save rồi load cùng slot | Dữ liệu round-trip chính xác |
| Load slot A rồi slot B | Không rò runtime data A sang B |
| File hiện tại corrupt, backup hợp lệ | Recovery path rõ và không crash |
| Cả current/backup corrupt | Slot marked corrupted, slot khác dùng được |
| Unsupported future version | Không load/overwrite âm thầm |
| Disk write failure | Save cũ còn nguyên |
| Double-click Save/Load | Chỉ một operation |
| Quit trong lúc Saving | Policy ngăn corruption |

## Gameplay persistence matrix

Kiểm tra ít nhất:

- Player level/XP/health.
- Area và position fallback.
- Inventory stack/empty slots.
- Gold.
- Equipment và derived stats.
- Tutorial current step/completed.
- Active/completed quest và objective counters.
- Chest/unique pickup/boss/resource node.
- Total play time.

Mỗi domain cần test default, valid round-trip, missing reference và invalid numeric values.

## Quest test matrix

- Prerequisite locked/available.
- Accept đúng một lần.
- Mỗi objective type nhận đúng event và bỏ qua event sai ID/area.
- Save/load giữa objective.
- Turn-in thiếu item/reward capacity.
- Double turn-in không nhân reward.
- Tutorial chain unlock Main Quest đúng một lần.
- Player bỏ tutorial vẫn dùng sandbox systems được.
- Quest definition cycle/duplicate ID bị validator chặn.

## State/UI stress tests

- Spam Esc 20 lần.
- Spam Inventory key khi Pause/Dialogue/Loading.
- Settings mở từ Paused và từ MainMenu.
- Save failure khi state trước là Paused.
- Load failure từ MainMenu và active gameplay scene.
- Return Main Menu khi Inventory đang mở.
- Player chết khi Dialogue world không pause (nếu design cho phép).
- Domain reload disabled trong Editor nếu team dùng tùy chọn này.

## Soak và performance

Trước content-ready milestone:

- 100 vòng save/load cùng slot.
- Chuyển A→B→C nhiều vòng.
- Save world có số persistent records mục tiêu.
- Đo snapshot capture trên main thread.
- Đo serialize/write riêng.
- Theo dõi allocation và frame hitch.
- Xác nhận save size growth có giới hạn.

Performance budget cụ thể phải chốt khi có representative production world scene. DemoScene dùng để
đo integration sớm nhưng không được coi là tải production cuối. Không đặt con số giả trước khi
có dữ liệu, nhưng instrumentation phải tồn tại từ Phase 2.

## Portability tests bắt buộc

Mỗi feature được dựng trong DemoScene phải có ít nhất một kiểm tra ngoài DemoScene:

- Instantiate prefab trong minimal scene/context.
- Bind chỉ dependency đã document.
- Chạy happy path và missing-dependency validation.
- Xác nhận không có hard-coded DemoScene name/hierarchy lookup.
- Kéo sang một candidate world scene và chạy smoke test.

“Chạy trong DemoScene” là integration checkpoint, không phải Definition of Done cuối cho feature tái sử dụng.

## Data-driven content tests

Mỗi domain content cần kiểm tra:

- Hai definition variants khác nhau chạy trên cùng runtime handler/service.
- Duplicate/rỗng/format sai stable ID bị validator bắt.
- Missing cross-reference báo đúng asset path và field.
- Runtime mutation không làm thay đổi ScriptableObject asset.
- Save round-trip resolve đúng definition qua ID.
- Renamed/missing ID đi qua alias/recovery policy thay vì crash âm thầm.
- Handler bỏ qua domain event không match definition parameters.
- Restore không phát event làm tăng objective/grant reward.

## Bug severity

- **P0:** mất/corrupt save, load nhầm slot, không vào game/build.
- **P1:** duplicate/mất item/reward, progression gate sai, player spawn ngoài world.
- **P2:** UI state kẹt, objective update sai nhưng recover được, visual/settings issue đáng kể.
- **P3:** polish, copy, minor layout hoặc warning không ảnh hưởng dữ liệu.

Không release milestone với P0/P1 mở.

## Test fixtures và golden saves

Duy trì fixtures version-control:

- Empty/default new game DTO.
- Mid tutorial.
- Tutorial Quest đang làm.
- Tutorial chain completed/Main Quest unlocked.
- Rich inventory/equipment.
- Corrupted fixture.
- Mỗi saveVersion cũ cần migration.

Không dùng save cá nhân duy nhất của developer làm fixture chuẩn.

## Review checklist cho mỗi PR/đợt thay đổi

- Có thay đổi ownership/responsibility giữa manager không?
- Có serialized field rename cần `FormerlySerializedAs` không?
- Có stable ID mới và validator không?
- Có tách Definition, Runtime State và Save DTO không?
- Có branch `if (specificId)` đáng lẽ phải là definition/handler data không?
- Có save field mới và migration/default không?
- Restore có phát gameplay event/reward ngoài ý muốn không?
- Failure/cancel có trả GameState đúng không?
- UI có gọi trực tiếp file I/O/SceneManager/domain internals không?
- Test nào chứng minh acceptance criteria?
