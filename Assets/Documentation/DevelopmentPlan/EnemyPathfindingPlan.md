# Enemy Pathfinding Plan

Trạng thái: **Đã implement P-A → P-D cho MapDuy (2026-11-26)**; thiết kế gốc D-109 (2026-10-05). Xem §8.

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

## 8. Implementation 2026-11-26

Owner request: enemies and animals in MapDuy kept running into objects and getting stuck. Scope now covers
`WildlifeActor` (Boar/Deer/Hare) as well as `EnemyUniversal`.

| File | Role |
|---|---|
| `Assets/Scripts/Navigation/GridPathfinder.cs` | `IWalkableGrid` + pure A* (8-dir, no corner cut, octile, node budget → `Partial`), nearest-walkable snap, thick-line LOS, string-pull smoothing. |
| `Assets/Scripts/Navigation/PathGridSettings.cs` | SO: cell 0.5, obstacle layers, clearance 0.2, cache lifetime 8 s, 1500 nodes/search, 4000 nodes/frame, repath/LOS/stuck timings. Asset: `Assets/Game/Navigation/DefaultPathGridSettings.asset` (Default + Water + SkillWall). |
| `Assets/Scripts/Navigation/PathGridBuilder.cs` | Scene grid. **Deviation from §3:** cells are probed lazily with `Physics2D.OverlapBox` and cached (no load bake, so no spike on a 300×140 map, and harvested trees/skill walls are picked up after cache expiry). Static or Rigidbody-less non-trigger colliders block; dynamic/kinematic bodies never do. Frame budget returns `Deferred`. Also `HasClearPath` (circle cast) and `SteerAround` (local avoidance ±135°). |
| `Assets/Scripts/Navigation/PathFollower.cs` | Plain C# object per actor (not a component, so prefabs are unchanged). Goes straight while the circle cast to the target is clear. Otherwise it follows waypoints and repaths on target move (>0.75 after 0.4 s, or >2 immediately) or when stuck (<0.08 moved in 0.6 s). After 3 stuck repaths it returns `NoPath`. If the actor is stuck while the line is clear, a moving body is in the way, so it sidesteps instead of giving up. |
| Prefab `Assets/Prefabs/World/Navigation/PathGrid.prefab` | Installer; placed in MapDuy. Not yet in DemoScene/MapNhat. |

Actor fallbacks: Patrol NoPath or >8 s → Idle (new point, sampled walkable). Chase NoPath → ReturnHome. ReturnHome NoPath → home re-anchored to the current position (runtime only). Wildlife wander NoPath → new wander point; flee uses `SteerAround`; leash/aggressive chase use the follower.

Verification: 7 EditMode tests in `GridPathfinderTests` pass, and the code compiles with 0 errors. One Play Mode sample in MapDuy: an Ent rounded an obstacle to reach the player. That sample exposed a false stuck while pressed against the player, which is now fixed. **The owner is running full Play Mode acceptance** (the 20-enemy timing in §6 has not been measured yet).

### 8.1 Footprint fix — 2026-11-26 (owner report: scaled Ent still stuck behind a tree)

Root cause: the scaled Ent has a tall full-body box collider (about 1.12 × 1.94). Navigation had
modelled every actor as a small circle (radius = shortest half-extent), so paths hugged tree trunks
that the Ent's body could not clear. The Ent then hit the same stuck → repath → give up → return home loop.

- New `NavFootprint` (`Assets/Scripts/Navigation/NavFootprint.cs`): the union of the actor's solid collider bounds
  relative to `Rigidbody2D.position`, resolved on first use.
- `PathGridBuilder` keeps one lazy cell cache per footprint shape. A cell is walkable only if a probe box the size of
  the actor's collider plus a half cell plus clearance, placed at the cell centre plus the collider offset, touches no
  static obstacle. The straight-line check uses `BoxCast` with the real box instead of `CircleCast`. Default `Clearance`
  is now 0.08, since the real box replaces the old 0.2 margin.
- `EnemyUniversal` melee/projectile attacks now require a raycast line of sight to the player. An enemy no longer
  swings through a tree; it walks around it instead.

Verification: compiles with 0 errors and the EditMode `GridPathfinderTests` pass. Play Mode acceptance is pending the owner's test.

### 8.2 Enemy and Boar chase range (2026-11-26)

The owner asked for longer ranges ("Tăng tầm đuổi của enemies lên vì map rất to"), then for the Boar too, and then for slightly less ("Giảm nhẹ tầm xuống xíu").

- Ent, Goblin1, Slime1 and Slime2 prefabs:
  - `_detectionRange` changes from 3 to **5**.
  - `_chaseRange` (distance from home) changes from 5 to **12**.
  - No scene overrides existed, so all MapDuy and DemoScene instances inherit these values.
- `WildlifeDefinition` gains data fields `_reactionRange` (default 4) and `_leashRange` (default 8, never below the reaction range). These replace the constants 4 and 8 that were hard-coded in `WildlifeActor`.
  - `Boar.asset` uses **5 / 12**.
  - Deer and Rabbit keep 4 / 8.
- `DefaultPathGridSettings` raises `MaxNodesPerSearch` from 1500 to 3000 and `MaxNodesPerFrame` from 4000 to 6000, so longer chases stay routable.

### 8.3 Movement-feel pass (2026-11-26)

The owner said the Ent "tìm đường đuổi đánh hơi kì… không thông minh" and approved four fixes modelled on common
top-down practice. The owner asked why we don't use NavMesh. Answer: Unity's NavMesh is 3D-only, and 2D needs the
third-party NavMeshPlus. The agent also fights with `Rigidbody2D`/knockback/pull, harvested nodes need carving, and
actor sizes need separate bakes. Above all, NavMesh would not fix the collider and steering issues. Revisit NavMeshPlus
with a new decision if this pass is still not good enough (see §7).

1. **Feet collider + hurtbox (Ent prefab).** The solid body box (0.7×1.21, offset −0.01/−0.13) becomes a feet box
   (0.6×0.28, offset −0.01/−0.48). A new child `Hurtbox` trigger keeps the old full-body box. `PlayerAttackHitbox`
   detects trigger colliders through `GetComponentsInParent<IDamageable>`, so melee still hits the whole body.
   Area skills that skip triggers (`SkillGustFan`, `SkillTsunamiWave`, `SkillWhirlwind`, `SkillMeteorStorm`) now hit
   the Ent only where their area covers its feet. Other enemies keep their colliders.
2. **Steering.** Enemy and wildlife velocity ramps toward the target (`EnemyUniversal._acceleration`, default 14;
   wildlife 12) instead of snapping. Attacks, pulls, launches and paused input still set velocity instantly.
   `PathFollower` skips up to two waypoints ahead as soon as a box cast to them is clear.
   `CrowdSeparation` (`Assets/Scripts/Navigation/CrowdSeparation.cs`) blends a push away from overlapping actors into
   the move direction while a grid is active.
3. **Facing hysteresis.** `FacingFilter`: a new 4-way facing must lead by ×1.25 and hold for 0.15 s. Attack
   wind-ups turn immediately. Enemy `InputX/InputY` now receive the filtered cardinal direction.
4. **Engagement.**
   - Detection and continued chase require a raycast line of sight past static obstacles
     (`_requireLineOfSight`, default on).
   - When the player is out of sight, the enemy goes to the last seen position and waits there. After
     `_loseSightMemory` (default 3 s) without sight, it returns home.
   - Entering Chase (including being hit from behind cover) records the player's position.
   - When an attack is in range (×0.9) with line of sight but still on cooldown, the enemy holds position facing the
     player instead of pushing into them.

All new fields have code defaults, so the existing prefabs and scenes pick them up without edits. Nothing changes in
save data. Verification: compiles with 0 errors, and EditMode passes 11/11 (`GridPathfinderTests` plus 4 new
`FacingFilterTests`). Play Mode acceptance is pending the owner's test.

### 8.4 Ent beside a tree: attack reach and animator fix (2026-11-26)

The owner reported the Ent "đứng im" beside a tree. A Play Mode reproduction (Ent and player on opposite sides of a
`WoodTree002` trunk) found three causes:

1. **The swing whiffed.** The Ent's directional hitboxes, copied from the Goblin, sit at waist height relative to its
   pivot, while the player's only body collider is at the feet. At pivot distance 1.31 (inside activation range 1.5)
   the swing could not overlap the player.
   Fix: melee attacks that have directional hitboxes now read the hitbox shapes once
   (`AttackProfile.meleeReach`, relative to the pivot; works while the colliders are disabled). They swing only when
   the hitbox facing the player overlaps the player's body collider. Otherwise the enemy walks to the nearest walkable
   **attack slot**, the pivot that centres that hitbox on the player, and holds there during cooldown.
   This applies to Goblin and Ent. Slime attacks without directional hitboxes keep the range check.
2. **The trunk hid the player point-blank.** Fix: a new field `_awarenessRadius` (default 2.5) lets the enemy notice
   the player without line of sight at that range. Beyond it, sight uses two parallel rays 0.6 apart, so only
   obstacles wider than a trunk hide the player.
3. **The animator stuck in RunAttack.** This regression came from §8.3's hold-at-range: in `Goblin.controller`
   (shared by Goblin and the Ent override), attack could start only from Run and leave only while running. When the
   enemy held in place, the second attack never started its clip, so `FinishAttackAnimation` never fired.
   Fix, per the owner's suggestion to match the Slime `Enemy.controller`:
   - Attack now enters through **Any State → RunAttack** (`Attack`, `!isDead`, no self-transition), replacing the
     Run → RunAttack transition.
   - Added RunAttack → Idle (exit time 0.95, `!isWalking`).
   - Code safety net: an Attack state longer than 2.5 s finishes itself, and leaving Attack resets the attack trigger.

Verification (same reproduction after the fix): the Ent noticed the player behind the trunk and walked around the
tree to an attack slot. All 4 attacks in 8 s connected (player HP 100 → 76 → 52 → 28 → 4). Each Attack state lasted
at most 0.7 s, and the Ent returned to Idle between attacks. Compiles with 0 errors, and EditMode passes 11/11.
