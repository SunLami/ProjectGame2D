# Enemy Pathfinding Plan

Trạng thái: **Đề xuất, chờ user duyệt trước khi implement** (D-109, 2026-10-05).

## 1. Bối cảnh

- `EnemyUniversal` (state `Idle/Patrol/Chase/Attack/Hurt/Dead/ReturnHome`) hiện đi thẳng bằng `MoveTowards`
  (`_desiredVelocity = direction * speed`). Không có tránh vật cản, nên enemy kẹt ở tường, nhà, nước.
- Enemy hiện chỉ có trong DemoScene (prefab `Goblin1`, `Slime1`, `Slime2`). Map thật (MapNhat, MapDuy) chưa có enemy.
- Boss **không** thuộc phạm vi: arena là phòng mở, đã dùng steering + `BossObstacle` (D-106, D-108).

## 2. Quyết định

Tự viết A* trên grid lấy từ Tilemap, không dùng plugin ngoài ở bản đầu (lý do: không phụ thuộc Unity 6/license,
hợp Data-Driven, phạm vi game vừa, thuật toán thuần dễ test).

## 3. Kiến trúc

| Thành phần | Trách nhiệm |
|---|---|
| `IPathfinder` | `bool TryFindPath(Vector2 from, Vector2 to, List<Vector2> result)`. Interface để sau này thay plugin mà không sửa enemy. |
| `GridPathfinder` | A* 8 hướng trên `PathGrid`, chặn cắt góc chéo qua ô bị chặn, heuristic octile, giới hạn số ô duyệt mỗi lần. Không phụ thuộc `MonoBehaviour` (EditMode test được). |
| `PathGrid` | Mảng ô walkable + cost, dựng từ Tilemap/Collider2D của scene. |
| `PathGridBuilder` (MonoBehaviour trong scene) | Bake lúc vào scene theo `PathGridSettings`; cung cấp `IPathfinder` cho enemy. Scene không có builder thì enemy rơi về hành vi đi thẳng hiện tại. |
| `PathGridSettings` (ScriptableObject) | Cell size, layer chặn, cost theo loại ô (nước, cỏ...), giới hạn duyệt, chu kỳ tính lại. Không bị mutate lúc runtime. |
| `PathFollower` (component trên enemy) | Giữ danh sách waypoint, tính lại đường tối đa mỗi 0.4 s và chỉ khi mục tiêu dịch chuyển đáng kể; trả hướng đi cho `EnemyUniversal`. Làm mượt bằng cùng kiểu steering với `SteerSmooth`. |

`EnemyUniversal` chỉ đổi ở `MoveTowards` (Chase, Patrol, ReturnHome): nếu có `PathFollower` thì dùng hướng của nó,
nếu không thì giữ nguyên logic cũ. Không xoá hay đổi tên member public.

## 4. Quy tắc an toàn

- Không lưu gì vào save: đường đi và grid là runtime state thuần (không ảnh hưởng save schema).
- Ngân sách: mỗi lần tìm đường giới hạn số ô duyệt; mỗi frame cho tối đa N yêu cầu (hàng đợi) để nhiều enemy không gây giật.
- Nếu không tìm được đường: enemy đứng yên rồi quay về home thay vì đi thẳng vào tường.
- Ô đích bị chặn: dùng ô walkable gần nhất.
- Chỉ bake lại grid khi vật cản đổi (sự kiện), không bake mỗi frame.

## 5. Phase

1. **P-A: lõi thuần.** `PathGrid`, `GridPathfinder`, `IPathfinder` + EditMode test (đường thẳng, vòng qua tường, ngõ cụt, không có đường, đích bị chặn, chéo góc, giới hạn duyệt).
2. **P-B: bake từ scene.** `PathGridSettings`, `PathGridBuilder`, gizmo debug; thử trên DemoScene.
3. **P-C: tích hợp enemy.** `PathFollower`, sửa `MoveTowards` của `EnemyUniversal`; thử Goblin/Slime (hai variant dùng chung code) trong DemoScene.
4. **P-D: promote.** Prefab/installer đưa sang map thật theo DemoScene workflow; cập nhật tài liệu.

## 6. Acceptance criteria

- EditMode test của lõi A* xanh; PlayMode test: enemy vòng qua tường để tới người chơi, không kẹt ở góc.
- Hai enemy variant dùng chung runtime code, chỉ khác data.
- 0 compile error, validator DemoScene/MainMenu 0 issue, PlayMode regression không đỏ thêm.
- Enemy hiện tại trong scene không có `PathGridBuilder` hành xử như trước.
- Đo thời gian: 20 enemy cùng truy đuổi không gây spike frame đáng kể (ghi số đo vào báo cáo).

## 7. Khi nào xét plugin

Nếu cần hàng chục enemy cùng lúc, map rất lớn hoặc tránh nhau phức tạp: thay lõi sau `IPathfinder`. Cần decision mới trước khi làm.
