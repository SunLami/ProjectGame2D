# Skill VFX Pipeline

Nguồn chuẩn cho việc thêm skill (projectile + skill định hướng) vào Player, dùng lại animation tấn
công có sẵn và gen VFX mới gắn vào layer `SkillFX`. Thay thế hướng gen animation nhân vật mới bằng
Pixellab (Fishing/Farming) đã thử và dừng lại — xem §5 để biết lý do và bài học giữ lại được.

## 1. Phạm vi đã chốt

- Skill gồm 2 loại: **projectile** (bắn vật thể bay đi) và **một số skill định hướng** (hiệu ứng tại
  chỗ/theo hướng nhân vật, không phải vật bay).
- **Không tạo animation nhân vật mới cho skill.** Lúc cast skill, nhân vật dùng lại nguyên
  `IdleAttack`/`WalkAttack`/`RunAttack` đã có trong Sprite Library — không cần Pixellab đụng vào
  Head/Body ở bước này.
- Lúc cast skill: **ẩn `Weapon`** (nhân vật không cầm kiếm khi tung chiêu), VFX skill hiển thị qua
  layer `SkillFX` mới (sibling với `Head`/`Body`/`Weapon`/`AttackFX`).
- Sau khi cast xong: `Weapon` hiện lại **đúng theo trạng thái trang bị thật** (không tự bật nếu người
  chơi vốn không cầm vũ khí gì).

## 2. Kiến trúc đã dựng và verify (2026-09-30, DemoScene)

- **`Assets/Scripts/Player/PlayerSkillFX.cs`** (partial class `Player`, file mới, không sửa file cũ
  ngoài 1 dòng gọi cache trong `Player.cs Awake()`):
  - `_skillFxRenderer` (SpriteRenderer) — cache qua `transform.Find("SkillFX")`.
  - `BeginSkillCast()` — set `_weaponRenderer.enabled = false`.
  - `EndSkillCast()` — set `_weaponRenderer.enabled = EquipmentManager.Instance.GetEquipped(EquipSlot.Weapon) != null`
    (không unconditionally show — tôn trọng trạng thái trang bị thật).
- **GameObject `SkillFX`** trong `DemoScene` (con của `Player`, cùng cấp `AttackFX`): `SpriteRenderer`
  (`sortingLayerName="Player"`, `enabled=false` mặc định). **Chưa gắn `SpriteLibrary`/`SpriteResolver`**
  — sẽ thêm khi có VFX asset thật (xem §6, việc chưa làm).
- **Verify thật trong Play Mode** (không chỉ đọc code):
  - Trang bị vũ khí qua đúng API data-driven (`EquipmentManager.Equip(item, slot)`, không phải
    `EquipSwordByIndex` — hàm đó là **nút test cũ chỉ đổi hiển thị, không cập nhật `GetEquipped()`**,
    đừng dùng để verify state trang bị thật) → `Weapon.enabled=True`.
  - `BeginSkillCast()` → `Weapon.enabled=False`.
  - `EndSkillCast()` → `Weapon.enabled=True` (đúng vì có vũ khí trang bị thật).
  - Trường hợp không trang bị gì → sau cast vẫn `False`, không tự bật nhầm.

### Xác nhận kiến trúc AttackFX không phụ thuộc trạng thái Weapon

Đọc trực tiếp code xác nhận (không suy đoán): `AttackFX` là `SpriteRenderer`/`SpriteResolver` hoàn
toàn tách biệt, keyframe trực tiếp trong `IdleAttackDown/WalkAttackDown/RunAttackDown.anim` qua
`m_Enabled` + `SpriteResolver` hash — **không có đoạn code nào kiểm tra `Weapon.enabled` trước khi
chạy AttackFX**. Vì vậy ẩn Weapon lúc cast skill **không ảnh hưởng** đến AttackFX của đòn đánh
thường — hai hệ thống độc lập, đúng như thiết kế `SkillFX` mới (cùng pattern với `AttackFX`, chạy
song song, không tranh chấp).

## 3. Câu hỏi mở — cần chốt trước khi implement animation/logic cast

Không tự chọn thay, hỏi trước khi code:

1. **Animator**: thêm parameter/state mới nào để trigger cast skill? Dùng lại đúng `Attack` trigger
   hiện có (skill = 1 biến thể của combo tấn công) hay thêm trigger riêng (`CastSkill`) + state mới
   trong cùng Base Layer (theo đúng pattern Blend Tree 4 hướng đã có)?
2. **Đồng bộ VFX với đòn đánh**: `BeginSkillCast()`/`EndSkillCast()` gọi từ đâu — **tạm giải qua debug
   harness** (`SkillTestHarness.CastTestProjectile()` → `Player.BeginAimSkill()`, hẹn giờ cố định
   `_castHideDuration` thay vì Animation Event thật) — xem §8.4. Còn mở: khi có Animator trigger thật
   (mục 1 chưa chốt), có đổi sang Animation Event (giống `OnAttackEnd`/`OnHitEnd`) hay giữ hẹn giờ?
3. ~~**Số hướng cho skill định hướng**~~ — **ĐÃ CHỐT (2026-09-30), rồi ĐỔI LẠI (2026-09-30, D-071):**
   ban đầu chốt 8 hướng bake sẵn, sau đổi sang **1 hướng + xoay `Transform` tự do theo góc aim thật**
   (không snap nữa) — xem §8 mục 2 và §8.2-8.4.
4. **Danh sách skill đầu tiên** cần gen VFX — đã chốt khung skill-shape tái dùng được cho mọi nguyên tố
   và danh sách skill Nước/Địa/Phong đầu tiên, xem §7.

## 7. Khung skill-shape dùng chung cho mọi nguyên tố + phạm vi bộ skill (2026-09-30)

### 7.1 Sáu khuôn skill (skill shape) — element-agnostic

Mọi skill trong game, bất kể nguyên tố, thuộc 1 trong 6 khuôn cơ chế sau. Nguyên tố chỉ đổi VFX/tên/
loại sát thương, code cơ chế dùng lại 100% giữa các nguyên tố:

1. **Projectile định hướng** — mũi tên chỉ hướng lúc cast (hiện rõ tầm bắn tối đa), bắn 1 vật thể bay
   thẳng, nổ/biến mất khi chạm địch hoặc hết tầm.
2. **Vòng tròn mục tiêu** — người chơi định vị 1 vòng tròn trong bán kính giới hạn quanh nhân vật (chặn
   cast từ quá xa), hiệu ứng xảy ra tại vị trí đó (tức thời hoặc có delay cảnh báo trước khi nổ).
3. **Quạt/Nón định hướng (Cone)** — không cần ngắm, tác động tức thời theo hướng nhân vật đang quay
   mặt (`FacingDirection`), hit tất cả mục tiêu trong 1 góc quạt trước mặt.
4. **Tia liên tục (Beam/Channel)** — giữ nút để duy trì dòng tia theo hướng nhân vật, gây sát thương
   liên tục theo thời gian giữ.
5. **Hào quang tự thân (Self Aura/Buff)** — không nhắm mục tiêu, VFX gắn trực tiếp lên `SkillFX` của
   Player (không phải object rời), tạo buff tạm thời (dmg/tốc độ/phòng thủ).
6. **Vật triệu hồi đứng yên (Totem/Trap)** — đặt 1 vật thể tĩnh tại vị trí chọn (như #2 nhưng vật tồn
   tại lâu, tick sát thương định kỳ thay vì nổ 1 lần).

### 7.2 Hỏa (Pyro) — ĐÃ BỎ, không còn trong tài liệu làm việc (2026-09-30)

Bộ 4 skill Hỏa (Cầu Lửa/Mưa Thiên Thạch Lửa/Phun Lửa/Thiêu Đốt) từng dùng để xác nhận khung 6 khuôn ở
§7.1 hoạt động tốt, nhưng theo yêu cầu đã **bỏ hoàn toàn** — không giữ làm reference nữa, không nằm
trong bất kỳ phạm vi production nào. Lịch sử quyết định xem D-070 trong `DecisionRegister.md`.

### 7.3 Phạm vi production — 3 nguyên tố khớp Boss, làm Thủy trước (2026-09-30)

Thay vì mở rộng tuần tự cả 7 nguyên tố kiểu Genshin, phạm vi production được thu hẹp lại **đúng 3
nguyên tố khớp với 3 Slime Boss** sẽ làm (tham khảo ảnh Slime Boss: Thủy — slime xanh dương, tấn công
tinh thể/tia chớp xanh + giọt nước; Địa/Nham — slime nâu gỗ có lá, tấn công gai đá/gai gỗ trồi lên;
Phong — slime trắng mây, tấn công lốc xoáy):

- **Thủy (Hydro) 💧**
- **Địa/Nham (Geo) 🗿**
- **Phong (Anemo) 💨**

Mỗi nguyên tố **ban đầu** dùng lại đúng 4 khuôn skill đã xác nhận ở bộ Hỏa (Projectile / Vòng tròn mục
tiêu / Quạt định hướng / Hào quang tự thân) — chỉ đổi tên và ý tưởng VFX. **Bộ Thủy đã đổi khuôn Skill 3
và Skill 4 theo yêu cầu 2026-10-02 (xem §7.6, §7.7, D-073, D-074)** — Địa/Nham và Phong vẫn giữ nguyên 4
khuôn gốc cho tới khi có yêu cầu khác:

| Slot | Khuôn | Thủy | Địa/Nham | Phong |
|---|---|---|---|---|
| Skill 1 | Projectile | Giọt Nước Xoáy — tia nước xoáy bắn thẳng | Thạch Trụ — khối đá bắn thẳng | **Đao Phong** (§7.11) — lưỡi gió trăng khuyết bay ra rồi quay lại như boomerang, trúng 2 lần |
| Skill 2 | Vòng tròn mục tiêu | Lốc Xoáy Nước — vùng xoáy nước tại điểm chọn, 2 tầng sát thương (rìa ngoài: chậm + DoT nhỏ; tâm: DoT lớn hơn), hút địch vào tâm thay vì đẩy lùi (đổi 2026-10-01, xem §7.3 cũ đã lỗi thời — chi tiết triển khai thật nằm ở `SkillGroundImpact.cs`) | **Đấu Trường Đá** (đổi 2026-10-02, §7.8) — vành cột đá dựng quanh vòng tròn nhốt địch + làm chậm + giảm phòng thủ; gai đá trong vòng nhô lên gây choáng | **Cuồng Phong** (§7.11) — lốc xoáy trôi trong vòng tròn mục tiêu, hất tung enemy lên không rồi thả xuống |
| Skill 3 | **Tia liên tục** (đổi từ Quạt định hướng, §7.6) | Tia Nước Xoáy — laser nước ngắm thẳng, tự duy trì 2-3s, root thay vì đẩy lùi | **Bãi Gai Đá** (2026-10-02, §7.9) — bãi gai đá mọc lao thẳng ra phía trước phủ kín hình nón (vẫn là khuôn Quạt định hướng) | **Trận Gió** (§7.11) — quạt hẹp 3 nhịp đẩy lùi rất xa, enemy đập tường nhận thêm sát thương (khuôn Quạt định hướng) |
| Skill 4 | **Quạt định hướng** (đổi từ Hào quang tự thân, §7.7) — **skill mạnh nhất bộ, ultimate** | Sóng Thần Tam Trùng — 3 đợt sóng thần liên tiếp bay về phía trước theo hình quạt | **Thiên Thạch Giáng Thế** (đổi 2026-10-02, §7.10) — ultimate: mưa thiên thạch rơi trong vòng tròn mục tiêu, nhiều giai đoạn hiệu ứng (khuôn Vòng tròn mục tiêu) | **Đại Bàng Gió** (đổi 2026-10-03, §7.11) — ultimate: đại bàng gió khổng lồ bay lướt theo đường ngắm, quắp enemy lên rồi thả xuống (khuôn Tia/đường thẳng) |

**Thứ tự làm: Thủy trước, làm xong và verify hết mới sang Địa rồi Phong.** Thủy là nguyên tố pilot —
xác nhận toàn bộ quy trình gen Pixellab + tích hợp `SkillFX`/`SpriteLibrary` một lần cho đúng, rồi lặp
lại y hệt quy trình đó cho Địa và Phong (không cần dò lại từ đầu). **Lưu ý:** vì Thủy đã đổi khuôn Skill
3/4, bộ Thủy không còn minh hoạ đủ cả 4 khuôn gốc (thiếu Hào quang tự thân) — nếu muốn giữ đủ 4 khuôn
làm template cho Địa/Phong, dùng chính bộ Hỏa cũ (đã bỏ, §7.2) hoặc bộ Địa/Phong khi tới lượt làm.

### 7.4 Tam giác khắc chế — ĐỀ XUẤT, CHƯA XÁC NHẬN

Đề xuất tam giác khắc chế 1 chiều (kiểu rock-paper-scissors) để mỗi Boss có đúng 1 nguyên tố khắc chế
bắt buộc dùng, không phải chọn tùy ý:

- **Phong khắc Thủy** (gió cuốn/thổi bay nước) → dùng bộ skill Phong khi đánh Boss Thủy.
- **Thủy khắc Địa/Nham** (nước xói mòn/cuốn trôi đất) → dùng bộ skill Thủy khi đánh Boss Địa/Nham.
- **Địa/Nham khắc Phong** (đất/đá chặn gió) → dùng bộ skill Địa/Nham khi đánh Boss Phong.

Cơ chế khắc chế cụ thể (vd +% dmg, hoặc CC/shield-break khi đúng nguyên tố khắc chế) **chưa thiết kế**
— cần xác nhận trước khi đụng tới code combat/damage type. Đây thuần là đề xuất ý tưởng, chưa Accepted.

### 7.6 Skill 3 Thủy — đổi từ Quạt định hướng sang Tia liên tục (2026-10-02, xem D-073)

Theo yêu cầu, Skill 3 Thủy đổi từ "Đợt Sóng" (khuôn Quạt định hướng #3, không ngắm) sang **"Tia Nước
Xoáy"** dùng khuôn **Tia liên tục #4** (có ngắm hướng thẳng, giống cơ chế aim của Skill 1 — tái dùng
`Player.BeginAimSkill`/`PlayerSkillCast.cs`). Chỉ đổi cho Thủy; Địa và Phong giữ nguyên Quạt định hướng
như bảng ở §7.3 cho tới khi có yêu cầu khác.

**Cơ chế đã chốt** (khác với mô tả gốc "giữ nút để duy trì" của khuôn #4 ở §7.1):

- **Tự động duy trì khoảng 2-3 giây sau khi xác nhận hướng bắn** — không phải giữ chuột/phím để duy trì.
  Người chơi ngắm hướng (giống Skill 1), xác nhận 1 lần, tia bắn ra và tồn tại hết thời lượng đó rồi tắt
  — không cần giữ input liên tục trong lúc tia đang bắn.
- **Không đẩy lùi (không knockback).** Thay vào đó, mọi enemy đang dính tia bị **đứng yên tại chỗ**
  (root/immobilize) trong lúc còn trong vùng tia — tái dùng hệ thống `ISlowable` đã có (D-072 hệ quả),
  gọi `ApplySlow(0f, duration)` thay vì cần interface/field mới (multiplier 0 = không di chuyển được).
  Áp dụng lại liên tục mỗi tick trong khi mục tiêu còn nằm trong vùng tia (giống cơ chế hút của Skill 2 —
  "deadline chỉ kéo dài thêm", không phải áp 1 lần).
- Gây sát thương liên tục theo tick (giống mọi vùng hiệu ứng khác trong dự án — xem `SkillGroundImpact.cs`
  làm mẫu), không phải 1 cú đánh duy nhất.
- VFX: 1 dải sprite nước turbulent (đúng công thức `ThuySkillVfxArtStyleGuide.md`) kéo dài/lặp theo chiều
  dài tia bằng `SpriteRenderer.drawMode = Tiled` (không stretch méo hình) — canvas gen vẫn vuông 176×176
  như quy định, chỉ 1 đoạn dải nước được lặp lại theo trục dài của tia.

### 7.7 Skill 4 Thủy — đổi từ Hào quang tự thân sang Quạt định hướng, ultimate (2026-10-02, xem D-074)

Theo yêu cầu, Skill 4 Thủy đổi từ "Lá Chắn Thủy Triều" (khuôn Hào quang tự thân #5, buff tạm thời,
không gây sát thương) sang **"Sóng Thần Tam Trùng"** dùng khuôn **Quạt định hướng #3** (không ngắm, bắn
theo `FacingDirection` như Mảnh Đá Văng/Trận Gió) — và trở thành **skill mạnh nhất, vai trò ultimate**
của bộ Thủy. Chỉ đổi cho Thủy; Địa và Phong giữ nguyên Hào quang tự thân.

**Cơ chế đã chốt:**

- **3 đợt sóng thần liên tiếp** bay về phía trước theo hình quạt (không phải 1 đợt duy nhất) — mỗi đợt
  là 1 lần quét vùng quạt riêng, phát liên tiếp cách nhau 1 khoảng delay ngắn (giống 3 "nhịp" đánh), tái
  dùng cùng 1 VFX quạt lặp lại 3 lần thay vì cần 3 asset khác nhau.
  - Delay/cooldown giữa skill chung và sát thương từng đợt **chưa chốt số liệu cụ thể** — tạm để làm mẫu
    thử nghiệm trong DemoScene trước (vd mỗi đợt cách nhau 0.3-0.5s, tổng 3 đợt ~1-1.5s), chỉnh theo cảm
    giác test thật, không coi số liệu này là final tới khi verify Play Mode.
  - Vì là ultimate, cooldown của skill này nên dài hơn hẳn Skill 1-3 (số liệu cụ thể để sau, không chặn
    việc dựng VFX/cơ chế trước).
- **Mỗi đợt sóng lao tới và lớn dần theo hình quạt** (chốt 2026-10-02): sóng xuất phát hẹp sát người
  chơi rồi nở rộng dần theo quãng đường bay, tạo hình quạt (không phải 1 mảng rộng cố định trượt thẳng).
  Khi gen VFX bằng Pixellab phải mô tả đúng ý này (mép sóng nở ra, nhỏ ở gốc/lớn ở đầu); phần nở rộng khi
  chạy do code scale `Transform` theo quãng đường đã bay, hitbox quạt nở theo cùng tỉ lệ để khớp hình.
  Cơ chế đã đề xuất: sóng bay thẳng theo hướng ngắm, đẩy lùi enemy theo hướng từ tâm quạt ra, mỗi enemy
  chỉ dính 1 hit/đợt. **Đã chốt (2026-10-02): sóng bị chặn bởi mọi vật có collider** giống `SkillBeam`
  (enemy, pickup, cây/địa hình sau này) — đầu sóng chạm collider không phải Player thì đợt sóng đó dừng,
  chỉ vật có `IDamageable` mới nhận sát thương + đẩy lùi. **Góc quạt và tầm ~7 đơn vị (chỉnh được
  trong inspector). Ban đầu chốt 70°, đổi xuống 50° ngày 2026-10-02 vì sóng ở cuối tầm quá to; sprite sóng
  chỉ scale đều (không bóp 1 trục, sẽ vỡ pixel art) và bị giới hạn độ rộng `_maxWidth`, nên độ rộng quạt ở
  cuối tầm phải gần bằng giá trị đó để indicator khớp với sóng thật.** Ngắm tự do bằng chuột (indicator hình quạt), không dùng
  `FacingDirection`; 3 đợt cách nhau ~0.4s, đợt sau to/mạnh hơn đợt trước.
- VFX: 3 đợt sóng dùng đúng công thức `ThuySkillVfxArtStyleGuide.md` (turbulent, răng cưa + bọt trắng),
  quy mô lớn hơn rõ rệt so với Skill 1-3 (đúng vai trò ultimate — hình ảnh phải "hoành tráng" hơn).
- **AoE xuyên enemy (2026-10-02):** đợt sóng gây damage + đẩy lùi MỌI enemy nó quét qua (mỗi enemy
  1 lần/đợt, dedupe theo component `IDamageable`, không theo `transform.root` vì nhiều enemy có thể chung
  parent). Enemy KHÔNG chặn sóng; chỉ collider rắn không phải `IDamageable` (tường, cây, pickup) ở dải
  giữa mới chặn. Skill 2 cũng dùng cách dedupe này.

#### 7.7.1 Nâng cấp ultimate cho Skill 4 Thủy — hiệu ứng cast (2026-10-03, xem D-079)

Giữ nguyên hình dạng/sát thương 3 đợt sóng của D-074 (không to thêm: đã từng bị chê sóng quá to/tràn indicator); thêm
hiệu ứng để giống ultimate, dùng chung hệ thống màn hình đã làm cho Skill 4 Địa (`SkillScreenFX`: camera rung, tối/chớp
màn hình, hit-stop + slow-mo). Code: `SkillTsunamiUltimate.cs` (bọc `SkillTsunamiWave`, thêm sự kiện `TargetHit`/`Ended`
vào sóng), `SkillStatusLoop.cs` (icon trạng thái animation bám theo enemy). Prefab `TsunamiUltimate_TamTrung`; harness
dùng nếu `_tsunamiUltimatePrefab` được gán (để trống = bản 3 đợt thuần như cũ). User chọn đủ 4 nhóm ý tưởng:

1. **Tụ lực (1s):** vòng xoáy nước dưới chân (animation), 6 quả cầu nước xoay quanh rồi bắn thẳng ra phía trước, màn
   hình tối xanh nhẹ, camera rung; **gợn sóng cong animation chạy dọc quạt** (rộng theo bề rộng quạt, mờ dần ở tầm cuối)
   làm cảnh báo hướng sóng.
2. **3 đợt sóng:** mỗi đợt có **nước phun + bọt ở chân** (animation), rung camera tăng dần; sóng để lại **vệt nước ướt**
   (vũng nhỏ animation rải ngang đỉnh sóng, mờ dần ~2.5s); **đợt 3 có rồng nước** (animation) trồi ra ở đỉnh sóng, to theo
   bề rộng sóng, rung mạnh hơn. Enemy trúng sóng bị **ướt**: làm chậm x0.75 trong 3s + icon giọt nước animation bám theo.
3. **Cú kết:** chỗ đợt 3 kết thúc (hết tầm hoặc bị chặn) bung **vụ nổ nước lớn** (tái dùng burst Skill 1 phóng 3.5x) +
   **sóng xung kích** (tái dùng vòng bụi Địa, nhuộm xanh), chớp xanh-trắng, rung mạnh, hit-stop + slow-mo, AoE 40 dmg + đẩy lùi
   8 bán kính 2.2 (cũng làm ướt); sau đó **vùng ngập** (vũng lớn animation, 4s, làm chậm x0.5) và **mưa rào 3s** (vệt mưa rơi + gợn
   bắn animation).

Asset (`Resources/VFX/Skills/Water/`, tất cả gen Pixellab có animation, loop ghép ping-pong, loại frame hỏng):
`WaterGatherRing_Swirl`, `WaterOrb_Wobble`, `WaterRipple_Shimmer` (gen ra hình "C" nên lật ngang), `WaterSpray_Settle`,
`WaterDragon_Roar`, `WaterPuddle_Ripple` (bỏ frame 7-8 lệch pattern), `WaterWetDrops_Drip`, `WaterRainSplash_Hit` (7 frame,
frame 7-8 trống).

### 7.8 Skill 2 Địa/Nham — Đấu Trường Đá (2026-10-02, xem D-076)

Đổi từ "Gai Đá" (chông trồi sau delay) sang **Đấu Trường Đá**, khuôn Vòng tròn mục tiêu (dùng lại
`Player.BeginGroundTargetSkill`; indicator 2 vòng = mép ngoài/mép trong của vành đá). Code: `SkillRockArena.cs`.

1. **Vành đá:** nền đất nứt có animation xuất hiện **gen bằng Pixellab** (`RockArena_FloorAppear.png`, 7 frame:
   gen "co đều về tâm" rồi đảo ngược, bỏ 2 frame thủng trong suốt; phát 1 lần trong 0.6s, nền loe ra từ vết
   nứt nhỏ; sprite nền nằm ở GameObject con `Floor` để không làm méo collider tường; scale/xoay bằng code chỉ
   còn là fallback khi không gán frame). Không vẽ cột đá riêng quanh vành (`_pillarCount = 0`, code cột đá
   còn đó nếu muốn bật lại; đã thử bật 18 cột mọc quanh mép 2026-10-02 nhưng user chọn quay lại animation nền).
2. **Nhốt thật:** `EdgeCollider2D` vòng kín trên layer riêng `SkillWall` (layer 8, thêm vào TagManager).
   Layer riêng để beam/projectile/sóng (mask Default+Enemy) bay xuyên qua, không bị tường chắn; Player bị
   `Physics2D.IgnoreCollision` từng collider nên đi xuyên tường của chính mình. Enemy bên trong bị kẹt, enemy
   bên ngoài không vào được.
3. **Debuff liên tục (mỗi 0.3s, enemy trong vòng):** làm chậm (`ISlowable`, x0.5) + **giảm phòng thủ**
   (`IVulnerable`, nhận sát thương x1.3, còn hiệu lực ~1s sau khi ra/hết skill). Vành đá không gây sát thương.
   `IVulnerable` là hook cho combo: skill khác đánh enemy dính debuff sẽ mạnh hơn (đã cài ở `Enemy` và
   `EnemyUniversal`; hệ số/cơ chế hiển thị sẽ chốt khi thiết kế hệ combo).
4. **Bãi gai đá bên trong:** tâm vòng tròn là điểm triệu hồi. Sau khi vành dựng xong (~0.9s), gai được đặt
   trên các vòng đồng tâm (cách nhau 0.4), mỗi vòng chia đều 360° (~1 gai/0.45 cung, vòng kề nhau lệch nửa
   bước) và nhô lên từ tâm lan ra ngoài như sóng chấn (độ trễ = khoảng cách/4 đơn vị/s), kín tới ~72% bán
   kính vành (không đè lên vành). 2 đợt, đợt sau xoay lệch để lấp khe. Mỗi gai gây **choáng 1.5s**
   (`IStunnable`: đứng yên, không AI/không tấn công) + sát thương nhỏ cho enemy trong bán kính 0.4; enemy bị
   choáng hiện **icon 3 ngôi sao xoay quanh đầu** (`SkillStunIndicator`, prefab `StunIndicator_Stars`, một
   icon/enemy, gia hạn nếu bị gai khác choáng tiếp). Vành cột đá thu nhỏ (16 cột, scale 0.2) và nền phóng
   1.1x để cột đứng trên mép nền thay vì tràn ra ngoài.
   Gai phóng to (scale 0.2, bán kính trúng 0.5), vòng gai chia đều tới sát 72% bán kính vành, bắt đầu nhô
   sau 0.7s.
5. **Asset** (`Resources/VFX/Skills/Earth/`): `RockPillar_Rise.png` (9 frame, gen "chìm xuống" rồi đảo ngược
   — kỹ thuật reverse-frame), `RockArena_Floor.png`, `StunStar.png`.

> **Đồng bộ phạm vi Skill 2 (2026-10-02):** vòng tròn Skill 2 Thủy (Lốc Xoáy Nước) tăng từ bán kính 1.5 lên **2.0**
> (lõi 0.7 → 0.93, hình xoáy phóng 0.82 → 1.093) để bằng vòng Skill 2 Địa/Nham (mép ngoài vành 2.0); chỉ đổi số,
> không đổi cơ chế.

### 7.9 Skill 3 Địa/Nham — Bãi Gai Đá (2026-10-02, xem D-077)

Thay ý tưởng gốc "Mảnh Đá Văng" bằng **bãi gai đá mọc lao về phía trước, phủ kín hình nón**; vẫn là khuôn Quạt
định hướng (dùng lại `Player.BeginAimSkill` với `fanAngleDegrees`, indicator hình quạt). Code: `SkillSpikeCone.cs`,
prefab `RockSpikeCone_DiaThich`, phím R ở kit Earth (Tab chuyển kit).

1. **Bố cục:** gai đặt trên các cung cách nhau 0.55 từ 0.8 tới tầm 6, mỗi cung chia đều toàn bộ góc nón 60°
   (khoảng cách gai 0.5, cung kề nhau lệch nửa bước) → phủ đặc cả hình nón, không để trống.
2. **Lao tới:** mỗi cung nhô lên trễ hơn cung trước (độ trễ = khoảng cách/9 đơn vị/s) nên bãi gai trồi lan ra xa
   như đợt sóng gai (~0.7s tới tầm cuối).
3. **Sát thương + choáng + giữ chân:** AoE — mọi enemy chạm gai đều nhận sát thương 12 (1 lần/lần cast, dedupe theo
   component `IDamageable`), **choáng 1.2s** (`IStunnable`, hiện icon sao xoay như Skill 2) và **bị giữ chân 2s**
   (`ApplySlow(0, 2s)`, hết 2s enemy di chuyển lại bình thường); không đẩy lùi (`_knockbackForce = 0`, chỉnh được).
   Burst đá của Skill 1 hiện tại vị trí enemy trúng.
   **Vòng gai khóa chân (`SkillSpikeCage`, prefab `RockSpikeCage_Lock`):** mỗi enemy trúng có 1 vòng 10 gai nhỏ mọc
   quanh chân, bám theo enemy, giữ đúng thời gian giữ chân rồi chìm xuống (phát ngược frame mọc). Vì là
   top-down nên vòng có trước/sau: gai ở nửa trên (xa) vẽ **sau** enemy (`sortingOrder` của enemy −1), gai nửa dưới
   (gần) vẽ **trước** enemy (+1) → enemy trông như đứng giữa vòng gai. Asset: `RockSpike_Rise.png` (1 gai đơn, 9
   frame, gen "chìm xuống đất" rồi đảo ngược; lần gen đầu có 2 gai/sprite nên nhìn thành từng cặp → gen lại 1 gai).
4. **Bị chặn bởi vật cản:** gai nào có đường thẳng từ người chơi bị collider rắn không phải `IDamageable` chắn
   (tường/cây) thì không mọc — bãi gai không mọc xuyên vật cản (cùng quy tắc `SkillBeam`/sóng thần). Collider
   `SkillWall` của Skill 2 không chặn.
5. **Asset:** tái dùng `RockPillar_Rise.png` (Skill 2) và burst `RockImpactVfx_ThachTru` (Skill 1) cho đồng bộ style;
   chưa gen asset riêng. Có thể gen gai dài/nhọn hơn nếu muốn khác biệt hình ảnh.

### 7.10 Skill 4 Địa/Nham — Thiên Thạch Giáng Thế, ultimate (2026-10-02, xem D-078)

Đổi từ "Da Đá" (buff phòng thủ) sang **ultimate mưa thiên thạch**, khuôn Vòng tròn mục tiêu (dùng lại
`Player.BeginGroundTargetSkill`). Skill mạnh nhất bộ Địa, đầu tư nhiều hiệu ứng trong lúc cast hơn các skill
khác (yêu cầu user 2026-10-02). Bốn giai đoạn (user đã chọn đủ cả 4, làm bản đầy đủ):

1. **Tụ lực (~1s):** player niệm; nền nứt dưới chân + vòng rune đất xoay; đá nhỏ lơ lửng quay quanh người; màn hình
   tối dần ở rìa, camera rung tăng dần.
2. **Cảnh báo:** vòng indicator rực cam; bóng đổ nhỏ trên mặt đất phình to dần ở từng điểm thiên thạch sắp rơi
   (báo trước để người chơi đọc được vùng nguy hiểm).
3. **Mưa thiên thạch (~4s):** ~20 thiên thạch có đuôi lửa rơi từ trên xuống ngẫu nhiên trong vòng; mỗi cái nổ AoE nhỏ
   (sát thương + đẩy lùi + bụi + mảnh vỡ + hố va chạm lưu lại). Cuối cùng 1 thiên thạch khổng lồ ở tâm: sóng
   xung kích, chớp trắng, camera rung mạnh, choáng enemy.
4. **Dư chấn:** hố va chạm còn khói/tàn lửa vài giây; vùng đất nứt làm chậm + giảm phòng thủ (`IVulnerable`, hook combo
   Skill 2).

**Ý tưởng thêm user đã chọn (2026-10-02):** (1) bóng đen khổng lồ phủ vòng + slow-mo + hit-stop ở thiên thạch cuối;
(2) gai trồi quanh mỗi hố + thiên thạch vỡ mảnh lăn ra; (3) thiên thạch tinh thể (choáng) + nổ dây chuyền. Không chọn:
cắt cảnh ultimate / Da Đá lúc niệm.

**Quy tắc hiệu ứng (user, 2026-10-02): mọi hiệu ứng trong lúc cast phải là animation Pixellab, không phải asset tĩnh.**
Asset Skill 4 (`Resources/VFX/Skills/Earth/`): `Meteor_Fall.png` (16 frame ping-pong, lửa chập chờn),
`MeteorCrater_Smolder.png` (9 frame: bùng lửa rồi nguội, phát 1 lần), `EarthRune_Pulse.png` (16 frame ping-pong, rune sáng
tối dạng sóng; dùng dưới chân người cast và làm vòng đánh dấu mục tiêu), `RockChunk_Hover.png` (16 frame ping-pong; đá
quay quanh người + mảnh vỡ lăn), `Shockwave_Expand.png` (7 frame: gen "co về tâm" rồi đảo ngược, bỏ frame ngả xanh),
dùng lại `RockSpike_Rise` (gai quanh hố), `RockProjectile_Impact` (vụ nổ khi chạm đất), `StunStar` (choáng).
Bóng đổ thiên thạch là đĩa 3 dải sinh bằng code (to dần, đậm dần) — không có frame.

**Code:** `SkillMeteorStorm.cs` (điều phối 4 giai đoạn, thiên thạch/bóng đổ/hố/gai/mảnh vỡ/nổ dây chuyền/thiên thạch cuối/vùng dư
chấn), `SkillScreenFX.cs` (camera rung, tối/chớp màn hình, hit-stop + slow-mo; singleton tự tạo, dùng sprite overlay gắn
camera, không cần UI), `SkillFrameAnimator.cs` (phát frame cho sprite sinh từ code). Chỉ số khởi đầu: vòng bán kính 3.5,
vùng thiên thạch cuối 2.2 (= vòng indicator trong), 20 thiên thạch trong 4s, thiên thạch thường 18 dmg + đẩy lùi 5, 20% là
tinh thể (8 dmg + choáng 1.2s), nổ dây chuyền khi 2 vụ rơi cách ≤1.4 trong 0.8s, thiên thạch cuối 70 dmg + đẩy lùi 9 + choáng
1.5s, dư chấn 5s làm chậm x0.6 + `IVulnerable` x1.3. Phím T ở kit Earth.

### 7.11 Bộ Phong (Wind) — 4 skill, chốt ý tưởng 2026-10-03 (xem D-080)

Bản sắc: tốc độ, xuyên qua, hất enemy lên không/đẩy bay (Water thiên về chặn/ngập, Earth thiên về giữ chân/nhốt). Gió vô hình
nên vẽ bằng vệt gió cong trắng-mint, lá, lông, bụi; bảng màu trắng / mint nhạt / xanh lá ngọc / trắng sáng (style guide
`WindSkillVfxArtStyleGuide.md`). Mọi hiệu ứng là animation Pixellab (quy tắc user 2026-10-02). Wind bị Địa khắc (§7.3).

**Cơ chế mới cần thêm trước/cùng lúc làm skill:** `IAirborne.ApplyAirborne(duration, height)` (bay lên: không hành động, sprite nhấc
lên + bóng ở đất, hạ cánh gây sát thương rơi + choáng ngắn) cài ở `Enemy`/`EnemyUniversal`; logic "đập tường" cho Skill 3. Tái dùng
`ISlowable/IPullable/IStunnable/IVulnerable`, `SkillScreenFX`, `SkillFrameAnimator`, `SkillStatusLoop`.

1. **Skill 1 — Đao Phong (Projectile, boomerang):** lưỡi gió trăng khuyết xoay bay thẳng theo hướng ngắm tới tầm cuối (hoặc tới
   vật cản rắn → quay đầu ngay), rồi **quay lại về người chơi**; mỗi lượt đi/về trúng mỗi enemy 1 lần (tối đa 2 hit/enemy, lượt
   về hút nhẹ enemy về phía người chơi); để lại vệt lá/gió; người chơi "bắt" lưỡi gió khi nó về, có hiệu ứng bắt nhỏ.
   **Đã làm (2026-10-03):** `SkillBoomerang.cs`, prefab `WindBlade_DaoPhong`, phím Q ở kit Wind (Tab: Thủy → Địa → Phong). Tốc độ ra 10 / về 13,
   tầm 7, sát thương 14 (lượt đi, đẩy lùi 3) + 10 (lượt về, hút nhẹ 3 u/s trong 0.35s), bán kính trúng 0.55; gặp collider rắn không phải
   `IDamageable` thì quay đầu (collider `SkillWall` của Skill 2 Địa không chặn); vệt luồng gió animation mỗi 0.07s; bắt lưỡi gió có vụ nổ nhỏ.
   Asset (`Resources/VFX/Skills/Wind/`): `WindBlade_Spin` (16 frame ping-pong), `WindImpact_Burst` (7 frame, bỏ 2 frame trống),
   `WindGustPuff_Dissipate` (9 frame, vệt gió).
2. **Skill 2 — Cuồng Phong (Vòng tròn mục tiêu, hất tung):** vòng tròn bán kính 2.0 (bằng Skill 2 Thủy/Địa); một lốc xoáy trôi
   chậm vòng quanh trong vòng ~6s, enemy chạm lõi lốc bị **hất tung ~1.2s** (không hành động được, sprite bay lên + bóng dưới đất),
   rơi xuống chịu sát thương rơi + choáng ngắn; vùng ngoài lõi **hút** enemy về phía lốc xoáy (không đẩy ra xa; sửa theo user 2026-10-03). Lá/bụi bay quanh.
   **Đã làm (2026-10-03):** `SkillWhirlwind.cs`, prefab `WindWhirlwind_CuongPhong`, phím E ở kit Wind. Vòng bán kính 2.0 (lõi 0.9, indicator hai vòng),
   lốc xoáy trôi theo quỹ đạo bán kính 0.7 trong 6s; enemy chạm lõi được **hất tung** 1.2s (cao 1.0) qua `IAirborne` mới (cài ở `Enemy`/
   `EnemyUniversal`, = choáng + hình nhấc lên), hạ cánh chịu 14 sát thương + choáng 0.6s + sao choáng + vụ nổ gió; mỗi enemy chỉ bị hất lại sau 2.5s;
   enemy ngoài lõi (trong vòng) bị **hút** về phía lốc xoáy với tốc độ 2.6 (sửa 2026-10-03 theo user: ban đầu ghi "đẩy ra xa", user muốn hút vào tâm), rồi chạm lõi thì bị hất tung. **Hình nhấc lên:** sprite enemy nằm cùng GameObject với collider nên không nhấc transform được —
   `EnemyAirborneVisual` ẩn sprite thật, vẽ bản sao nhấc lên theo đường cong (lên 25% / treo 50% / xuống 25%) và để bóng dưới đất, trả
   sprite lại khi hạ cánh hoặc khi enemy chết. Asset: `WindTornado_Spin` (16 frame ping-pong), `WindRing_Flow` (16 frame ping-pong), tái dùng
   `WindGustPuff_Dissipate` và burst Skill 1.
   **Gen bằng Pixen + `animate_image`** (≈5 generation/asset thay vì ≈24): chất lượng dùng được, hơi mềm hơn công cụ object nhưng vẫn đúng chủ đề.
3. **Skill 3 — Trận Gió (Quạt định hướng, 3 nhịp + đập tường):** quạt hẹp (~35°, tầm ~6), thổi 3 nhịp liên tiếp, mỗi nhịp đẩy
   lùi rất xa; enemy bị đẩy tới tường/vật cản rắn (kể cả vành đá Skill 2 Địa) bị **đập tường**: sát thương thêm + choáng ngắn + hiệu
   ứng va. Ngắm bằng chỉ báo quạt như Skill 4 Thủy.
   **Đã làm (2026-10-03):** `SkillGustFan.cs`, prefab `WindGustFan_TranGio`, phím R ở kit Wind. Quạt 35° tầm 6 (chỉ báo quạt), 3 nhịp cách 0.35s;
   mỗi nhịp gây 8 sát thương và **đẩy rất xa** (dùng `IPullable` về điểm xa phía sau enemy, tốc độ 10 trong 0.25s ≈ 2.5 đơn vị/nhịp, không phụ
   thuộc khối lượng hay trạng thái Hit-stagger). **Đập tường:** dự đoán bằng CircleCast dọc hướng đẩy; nếu có collider rắn không phải `IDamageable`
   (tường, cây, vành đá `SkillWall` của Skill 2 Địa) trong tầm đẩy thì tới lúc đó enemy nhận 18 sát thương + choáng 0.8s + sao choáng + vụ nổ gió,
   mỗi enemy 1 lần/lần cast. Hiệu ứng: vòng cung gió animation chạy dọc quạt theo từng nhịp (rộng theo bề rộng quạt), luồng gió trôi xuôi chiều
   gió, camera rung nhẹ. Test: enemy cạnh tường tạm mất đúng 3×8 + 18 = 42 máu. Asset: `WindGust_Arc` (16 frame ping-pong; Pixen gen ra 2 cung "( )" nên
   cắt lấy cung phải ")" ở bước xử lý frame, tốn 6 gen gồm 1 lần gen lại prompt).
4. **Skill 4 — Đại Bàng Gió (ultimate, đổi từ "Cuồng Phong Bao Bọc"):** khuôn Tia/đường thẳng (chỉ báo hình chữ nhật dài ~12, rộng
   ~2.5). 4 giai đoạn:
   - *Tụ lực (~1s):* vòng gió xoắn animation dưới chân, lông gió xoay quanh người, trời tối nhẹ, camera rung.
   - *Cảnh báo:* vệt gió animation chạy dọc đường bay; **bóng đại bàng** (silhouette) lướt trên mặt đất báo trước.
   - *Lướt:* đại bàng gió khổng lồ (vỗ cánh animation, ở trên cao) bay dọc đường ngắm, để lại lốc xoáy nhỏ và lông trắng; enemy dưới
     đường bay bị **quắp lên** (`IAirborne` + bị kéo theo), chịu sát thương liên tục, tới cuối đường bị **thả/quăng** xuống chịu sát thương rơi.
   - *Kết:* đại bàng rít + vỗ cánh một cú tại cuối đường: sóng gió đẩy lùi AoE, chớp trắng, camera rung, hit-stop ngắn, mưa lông rơi,
     vùng gió dư chấn vài giây làm chậm.
   Asset cần gen (đều animation): đại bàng vỗ cánh, vòng gió xoắn dưới chân, lông gió, vệt gió chỉ báo, lốc xoáy nhỏ để lại, mưa lông,
   trạng thái "bị quắp" trên enemy; tái dùng vòng sóng xung kích Địa (nhuộm trắng) và `SkillScreenFX`.

   **Đã làm (2026-10-03):** `SkillWindRoc.cs`, prefab `WindRoc_DaiBang`, phím T ở kit Wind. Đường bay dài 12, rộng 2.5 (chỉ báo chữ nhật). Giai đoạn:
   *Tụ lực 1s* (vòng gió xoắn animation dưới chân + 6 lông gió xoay quanh rồi bắn ra phía trước, tối xanh nhẹ, rung camera; cung gió animation chạy dọc
   đường + vòng gió thu nhỏ báo điểm hạ cánh ở cuối đường) → *Bay* (đại bàng gió animation vỗ cánh bay ở độ cao 1.6 với bóng đen trên mặt đất, tốc độ 12; đàn 4
   chim con bay chữ V theo sau; lốc xoáy nhỏ để lại dọc đường; bắn lông hai bên đường gây 7 sát thương; lá/bụi bay xuyên màn hình; enemy dưới đường bay
   bị **quắp** — `IAirborne` + di chuyển theo đại bàng, 5 sát thương mỗi 0.25s, biểu tượng xoáy gió) → *Kết* (đại bàng **lao xuống** ở cuối đường: ném enemy bị quắp
   về phía trước 2.5, rơi chịu 25 sát thương + choáng 1s; rít: chớp trắng-mint, rung camera, hit-stop + slow-mo, sóng xung kích + nổ gió bán kính 3 gây 20 sát thương + đẩy lùi 8
   cho enemy chưa bị quắp; đại bàng bay vút đi) → *Dư chấn* (mưa lông 2s, vùng gió làm chậm x0.6 trong 3s). Ý tưởng thêm user đã chọn: đàn chim con + lao xuống,
   điểm hạ cánh báo trước + lá bay xuyên màn hình, mưa lông bắn hai bên (không làm camera lướt theo và dải mây). Asset mới: `WindRoc_Flap` (9 frame, vỗ cánh),
   `WindFeather_Flutter` (9 frame); còn lại tái dùng (`WindRing_Flow`, `WindGust_Arc`, `WindTornado_Spin`, `WindGustPuff_Dissipate`, burst Skill 1, vòng bụi Địa nhuộm mint).
   Gen bằng Pixen + `animate_image`: tổng ~12 generation cho 2 asset.
   **Rà soát & sửa sau góp ý user (2026-10-03):** (1) đường bay rút từ 12 xuống **8** — màn hình chỉ thấy ~10 đơn vị bên phải người chơi nên cú kết ở 12 nằm
   NGOÀI màn hình (nguyên nhân chính khiến skill "không đẹp/to"); (2) kích thước chuẩn hóa: đại bàng scale 0.75 (sải cánh ~2.4), vùng nổ bán kính 2.2, burst cuối x2.2,
   sóng xung kích x2.2 (phóng quá lớn làm vỡ pixel), lốc xoáy nhỏ để lại 0.3, lá 0.14; (3) vòng lông xoay quanh người: tâm hạ xuống ngang **hông** (cao +0.25), vòng dẹt (ry 0.22),
   nửa sau vẽ sau lưng, nửa trước ở chân — trước đó tâm ở ngực làm nửa sau bay lên ngang mặt (đã sửa cùng cách cho quả cầu nước Skill 4 Thủy và đá bay Skill 4 Địa);
   (4) gen thêm **lá** animation riêng (`WindLeaf_Tumble`) thay cho xoáy nhỏ đang dùng làm lá, và **vòng đánh dấu vùng hạ cánh** sắc nét (`WindMarker_Pulse`, loại 2 frame bị tô đặc) thay vòng gió mờ đục;
   (5) biểu tượng "bị quắp" tự tắt ngay sau khi thả. Bài học: luôn test hiệu ứng trong đúng khung nhìn camera (độ dài/vị trí) trước khi chốt.
   **Rà lại luồng cast (2026-10-03, lần 2, theo góp ý "chưa chuyên nghiệp / effect thừa / hướng sai"):**
   (1) **Bỏ hẳn lốc xoáy nhỏ rải dọc đường bay** — thừa và sai logic (chim không để lại lốc xoáy đứng yên), chỉ gây rối; (2) **đại bàng bay vào từ ngoài khung hình**: bắt đầu bay NGAY TRONG lúc tụ lực
   (xuất phát cách người chơi 10, mờ dần hiện ra trong 3.5 đơn vị đầu) để tới trên đầu người chơi đúng lúc tụ lực kết thúc, thay vì bật ra giữa màn hình;
   (3) **hướng bay đúng theo đường đi:** sprite lật khi bay sang trái và nghiêng tối đa ±45° theo thành phần dọc của đường bay (đại bàng, bóng, đàn chim con);
   (4) **đàn chim con giữ đội hình chữ V** (bám chặt, trước đó bám mềm nên rải rác ở tốc độ bay), mờ dần cùng đại bàng;
   (5) **sóng xung kích riêng cho gió** `WindShockwave_Expand` (gen Pixen: vòng vệt gió trắng-mint; Pixen tô đĩa xám ở giữa nên lọc màu xám trung tính thành trong suốt ở bước xử lý frame; gen "co về tâm"
   rồi đảo ngược, loại 2 frame gần trống; code phóng thêm từ 0.5x lên 1x vì frame chỉ lớn ~35%) thay cho vòng bụi Địa nhuộm mint (ra màu nâu-xanh, pixel vỡ khối).
   **Sát thương ultimate + dọn effect (2026-10-03, lần 3):** (1) **các cung gió lao tới trong lúc tụ lực gây sát thương thật** (8 + đẩy 2, mỗi cung 1 lần/enemy);
   thêm sát thương cho các effect khác: 6 lông bắn ra khi hết tụ lực (6), đàn chim con khi bay qua enemy (6/lần/chim), vùng gió dư chấn (4 mỗi 0.4s), lông rơi (3 mỗi lông);
   tăng mức mặc định: lông bắn hai bên 10, quắp 8/0.25s, hạ cánh 40, nổ 35. Toàn bộ qua **một hệ số `_damageMultiplier`** (mặc định 1) để chỉnh một chỗ khi làm boss — user: "sát thương đợi
   làm boss sẽ config lại sau"; (2) **sửa lá/lông còn sót sau khi skill kết thúc:** coroutine điều khiển effect chạy trên chính đối tượng skill nên khi `Destroy(gameObject)` coroutine dừng và
   effect đang sống bị bỏ lại; nay mọi effect là CON của đối tượng skill nên hủy skill là hủy hết (đo sau khi chạy xong: 0 effect sót; 3 enemy trên đường mất 83-132 máu).
   **Chỉ báo ngắm có vòng tròn vùng sát thương (2026-10-03, theo yêu cầu user):** `Player.BeginAimSkill` có thêm tham số tùy chọn cuối `endCircleRadius`
   (mặc định 0 = không vẽ, các skill cũ không đổi); khi > 0 vẽ thêm một vòng tròn quanh điểm cuối của đường ngắm. Skill 4 Gió truyền bán kính vùng nổ 2.2 (`_windRocBlastRadius` trong harness,
   phải bằng `SkillWindRoc._blastRadius` trên prefab) nên người chơi thấy trước chính xác vùng nổ/lao xuống sẽ gây sát thương.
   **Quy tắc chung:** effect sinh bằng code phải là con của đối tượng điều phối (hoặc tự hủy bằng `Destroy(go, thời gian)`), không phụ thuộc coroutine của cha.
   Còn lại sau rà: gợn sóng cảnh báo (cung gió) trong lúc tụ lực, lông xoay quanh hông, vòng đánh dấu, lá bay thưa, bắn lông hai bên, quắp/ném, mưa lông, vùng gió dư chấn.

Thứ tự làm (user duyệt): Skill 1 → 2 → 3 → 4, test từng skill; kit Wind chuyển bằng Tab (thêm kit thứ 3 vào harness).

### 7.5 Việc chưa làm cho rescope này

- Chưa gen VFX Pixellab cho bất kỳ skill nào trong 3 bộ Thủy/Địa/Phong ở trên.
- Chưa thiết kế cơ chế damage-type/khắc chế trong code (elemental status trên enemy, % bonus dmg...).
- Chưa chốt §3 mục 1-2 (Animator trigger, nơi gọi Begin/EndSkillCast) — vẫn mở, cần hỏi trước khi bắt
  đầu code logic cast. Mục 3 (số hướng) đã chốt = 8, xem §8.

## 8. Cast-aim indicator (8 hướng) — chốt 2026-09-30, tham khảo skillshot LoL

Người dùng gửi ảnh tham khảo League of Legends: lúc bấm phím skill, hiện **đường/mũi tên định hướng
dưới chân nhân vật** cho biết tầm cast tối đa; đường này **xoay tự do quanh nhân vật theo hướng
chuột/joystick** trong lúc người chơi đang chọn hướng (aim phase), xác nhận (click/thả phím) mới thực
sự bắn.

**Phân biệt 2 lớp, không nhầm lẫn:**

1. **Aim indicator (lúc đang ngắm)** — xoay **mượt, tự do 360°**, không snap theo 8 hướng. Đây gợi ý
   là 1 line/sprite đơn giản (LineRenderer hoặc sprite kéo dài theo trục X, scale theo range), **không
   cần gen bằng Pixellab** vì không phải animation nhân vật/VFX skill — là UI/gameplay indicator, tách
   biệt hoàn toàn khỏi `SkillFX`. Loại indicator cụ thể (LineRenderer vs sprite) là quyết định code,
   chưa chốt, không chặn việc gen VFX projectile ở §7.3.
2. **Projectile VFX (lúc bắn thật) — ĐÃ ĐỔI KIẾN TRÚC (2026-09-30, override quyết định gốc ở trên):**
   ban đầu chốt 8 hướng bake sẵn qua Pixellab + không xoay `Transform` (sợ vỡ pixel-art ở góc lệch).
   Sau khi xem asset mẫu thật (craftpix magic-effects pack, `PNG/water`) — xem §8.2 — xác nhận VFX dạng
   blob/giọt nước đơn giản (không có tay/chân/chi tiết bất đối xứng mạnh) **xoay `Transform` không vỡ
   hình**, và đây chính là cách asset thương mại thật dùng. Đổi sang: **gen đúng 1 hướng** qua
   `create_1_direction_object`, code **xoay `Transform` theo góc aim thật** (không snap 8 hướng nữa,
   xoay mượt tự do đúng góc bắn) khi spawn projectile.

Áp dụng cho mọi skill dùng khuôn "Projectile định hướng" (§7.1 khuôn #1) ở cả 3 nguyên tố — không chỉ
riêng Thủy. Khuôn "Quạt định hướng" (#3) tạm giữ cùng cách tiếp cận (1 hướng + xoay `Transform`), review
lại nếu sau này phát sinh vấn đề vỡ hình cho hiệu ứng có chi tiết bất đối xứng mạnh.

### 8.2 Học đúng cấu trúc animation của asset VFX tham khảo (craftpix, 11 frame)

Asset mẫu `C:\Users\havin\Downloads\craftpix-781160-10-magic-effects-pixel-art-pack\PNG\water` (11 PNG,
128×128, dùng riêng làm tham khảo style — không nhúng file gốc vào project) là **1 sequence animation
đầy đủ vòng đời skill**, không phải 1 pose lặp lại đơn thuần:

1. Frame 1-2: khối nước nhỏ, mới hình thành (cast/spawn).
2. Frame 3-6: khối nước lớn dần, bay có đuôi giọt nước rơi rớt theo sau (fly/travel).
3. Frame 7-10: nổ bung thành các tia nước hình sao tỏa ra (impact/burst — kích hoạt khi va chạm).
4. Frame 11: các hạt nước thưa dần, tan biến (fade out).

Style màu: **chỉ 2-3 tông phẳng** (navy đậm cho viền/bóng, xanh dương giữa cho khối chính, xanh nhạt
cho highlight/tia sáng) — không gradient nhiều lớp, không dithering mịn, silhouette rõ và đơn giản.

**Áp dụng cho việc gen lại VFX Giọt Nước Xoáy:** gen 1 sequence animation theo đúng cấu trúc 4 giai
đoạn trên (không phải xoay tròn tại chỗ như v1-v2 đã thử, cũng không phải chỉ bay thẳng không đổi hình
dạng như v3), với đúng style phẳng 2-3 tông đã quan sát được — xem thực thi ở §8.3.

### 8.3 Đã gen + tích hợp — phần Travel (2026-09-30)

**Phạm vi đã làm**: chỉ phần **Travel** (tương ứng frame 3-6 của asset mẫu — khối nước bay có đuôi giọt
rơi), khớp đúng nhu cầu thật của `SkillProjectile.cs` (VFX gắn theo object đang bay). Phần
**Cast/Spawn** (frame 1-2, khối nhỏ mới hình thành) và **Impact/Burst + Fade** (frame 7-11, nổ tia nước
+ tan biến) **CHƯA gen** — đây là 1 VFX riêng, one-shot, spawn tại điểm va chạm khi projectile hết tầm
hoặc trúng địch, không phải một phần của sprite đang bay. Để làm sau, không chặn việc dùng thử skill.

- Gen bằng `create_1_direction_object` (176×176, `view="top-down"`) + `animate_object` (v3, 8 frame →
  lưu 9 frame gồm cả frame gốc) — KHÔNG dùng `create_8_direction_object` nữa (đổi kiến trúc, xem §8
  mục 2 ở trên).
  - Object base: `730f5ea6-a00e-4192-bbce-5f3270e8a767` ("Water Projectile Travel - Giot Nuoc Xoay").
  - Prompt nhấn mạnh style phẳng 2-3 tông, viền cứng, KHÔNG gradient — pass thử 2 lần trước khi đạt
    (v1/v2 xoáy tròn quá mượt/vector-hoá, v3 hình cầu đối xứng không có hướng, v4 giống giọt mưa tĩnh —
    tất cả đã bỏ, không giữ trong project). Bản cuối (v5, "flying blob... nose pointing right... tail
    of droplets trailing left") mới đúng vừa phong cách vừa đọc được là "đang bay".
- Import Unity: `Assets/Resources/VFX/Skills/Thuy/GiotNuocXoay_Fly.png` (9 frame, cắt 1 hàng ngang qua
  `TextureImporter.spritesheet`, PPU=48, Point filter, `alphaIsTransparency=true`). **Cập nhật
  2026-10-02 (D-072/D-073):** file này đã được re-gen đồng bộ style "turbulent water" và đổi tên —
  đường dẫn thật hiện tại là `Assets/Resources/VFX/Skills/Water/WaterDropletProjectile_Fly.png` (thư
  mục `Thuy` đã đổi tên thành `Water`, dùng tiếng Anh cho asset). Xem bảng asset tham khảo đầy đủ ở
  `ThuySkillVfxArtStyleGuide.md` §4.
- `SkillProjectile.cs` đổi từ mảng `DirectionFrames[]` (8 hướng) sang **`Sprite[] _frames` đơn hướng**:
  sprite vẽ với "mũi" chỉ về +X (phải); `Launch()` set `transform.rotation` theo đúng góc aim thật
  (`Mathf.Atan2`), không snap 8 hướng nữa. Verify Play Mode: bay đúng hướng, animate đúng frame, xoay
  mượt tự nhiên không vỡ hình — xem screenshot trong lịch sử phiên làm việc.
- Prefab `Assets/Prefabs/Skills/WaterProjectile_GiotNuocXoay.prefab` đã cập nhật khớp script mới.

### 8.1 Player facing 4 hướng vs skill cast 8 hướng — KHÔNG gen lại Player asset

Player (`Head`/`Body`/`Weapon`) chỉ có 4 hướng (Down/Left/Right/Up) trong Sprite Library — giữ nguyên,
không mở rộng lên 8. Giải pháp: **tách rời hoàn toàn 2 khái niệm góc**, dùng chung 1 giá trị góc ngắm
(aim angle, độ, tính từ input chuột/joystick lúc aim) nhưng snap về 2 độ phân giải khác nhau cho 2 mục
đích khác nhau:

- **`FacingDirection` của Player (4 hướng, dùng cho Animator/Sprite Library như hiện tại)** — snap góc
  ngắm về hướng **có trục lệch nhỏ nhất trong 4 hướng** (so `|dx|` với `|dy|`: `|dx| > |dy|` → Left/Right
  theo dấu `dx`, ngược lại → Up/Down theo dấu `dy`). Với góc chéo đúng 45°, ưu tiên trục ngang
  (Left/Right) — chọn 1 quy ước cố định để không "rung" facing khi ngắm gần đường biên.
- **Hướng VFX skill (8 hướng, dùng riêng cho `SkillFX`/projectile)** — snap cùng góc ngắm đó về bội số
  45° gần nhất (8 hướng) như đã chốt ở §8, độc lập hoàn toàn với `FacingDirection`.

Kết quả: Player **xoay người theo 1 trong 4 hướng gần nhất** với hướng ngắm (visual gần đúng, không
lệch nhiều so với hướng bắn thật), còn projectile/VFX **bắn đúng chính xác theo 1 trong 8 hướng** —
không cần Player có sprite chéo thật, không xoay `Transform` sprite (không vỡ pixel-art). Animator vẫn
dùng nguyên state `IdleAttack`/`WalkAttack`/`RunAttack` 4-hướng đã có, không đổi Blend Tree.

## 4. Quy trình gen VFX bằng Pixellab — khác hẳn animation nhân vật, ĐƠN GIẢN HƠN NHIỀU

Đây là khác biệt quan trọng nhất so với nhánh Fishing/Farming đã bỏ: **Skill VFX không cần tách layer
khỏi composite nhân vật** — VFX là **object độc lập**, không dính Head/Body/Weapon, nên toàn bộ vấn đề
silhouette/neckline/rod-đi-xuyên-tay đã vật lộn ở Fishing **không tồn tại** ở đây.

> **QUY TẮC BẮT BUỘC (chốt 2026-10-03, D-081): LUÔN gen asset Pixellab bằng Pixen + `animate_image`, không dùng
> `create_1_direction_object` + `animate_object` nữa.** Lý do: tiết kiệm generation (gói Tier 1 chỉ 2000 gen/chu kỳ).
> Chi phí đo thực tế: `create_image_pixen` = **1 gen**, `animate_image` 176px × 8 frame = **4 gen** → ≈ **5 gen/asset**;
> còn `create_1_direction_object` 176px = 20 gen + `animate_object` v3 ≈ 4 gen → ≈ 24 gen/asset (đắt gấp ~5 lần).
> Quy trình bắt buộc:
> 1. `create_image_pixen` (width/height 176, `no_background=true`, `view="high top-down"`, `outline="single color black outline"`),
>    prompt bám style guide của nguyên tố (HARD-EDGED chunky pixel art, flat cel-shaded color bands, bảng màu của nguyên tố).
> 2. `animate_image` với `first_frame_url` = link `https://api.pixellab.ai/mcp/images/<job_id>/download` của ảnh vừa gen,
>    `frame_count` 8 (cho 9 frame gồm frame gốc), `action` chỉ mô tả CHUYỂN ĐỘNG ("looping", "keeping the same size and position").
> 3. Tải từng frame bằng `curl "https://api.pixellab.ai/mcp/images/<job_id>/download?index=N"` (N = 0..8) — KHÔNG gọi `get_image`
>    lấy ảnh inline (tốn token); Python `urllib` thiếu chứng chỉ SSL nên dùng `curl`.
> 4. Kiểm tra frame bằng số liệu (PIL): số điểm ảnh alpha > 16 mỗi frame (loại frame trống/thủng) và số điểm ảnh khác nhau giữa frame
>    (xác nhận có chuyển động) thay vì xem contact sheet; chỉ xem ảnh khi nghi ngờ. Loop ghép ping-pong (0..8..1); animation "co/chìm" thì đảo ngược.
> 5. Nếu chất lượng chưa đạt (thiếu nét cứng, lệch style): chỉnh prompt và gen lại Pixen (chỉ 1 gen/lần thử). Chỉ khi Pixen không thể
>    làm được một hình đặc thù mới được dùng công cụ object (phải ghi lý do), và vẫn không dùng chế độ Pro.
> Chi tiết, bài học và ví dụ: `WindSkillVfxArtStyleGuide.md` mục 5.

Các bước dưới đây là quy trình ban đầu (2026-09-30), **đã bị quy tắc ở trên thay thế phần công cụ gen**; phần còn lại (VFX là object độc lập,
không tách layer, xuất PNG vào SpriteLibrary, di chuyển bằng code) vẫn đúng.

Theo đúng khuyến nghị trực tiếp từ Pixellab (`agent_help`, đã hỏi ở phiên trước — xem
[[reference_pixellab_mcp_connection]] trong memory):

1. Gen VFX bằng `create_1_direction_object` (skill không cần xoay theo hướng nhân vật, vd hào quang
   tại chỗ) hoặc `create_8_direction_object` (nếu VFX cần xoay theo hướng bắn, vd projectile) — KHÔNG
   dùng `create_character`/`animate_character` (đó là pipeline nhân vật, không phải VFX).
2. Animate bằng `animate_object` (mode `v3`, rẻ) cho chuỗi frame hiệu ứng (phóng ra/nổ/tan biến).
3. Xuất PNG, đưa thẳng vào `SkillFX`/`AttackFXLib`-style `SpriteLibraryAsset` mới — **không cần bước
   tách màu/silhouette nào cả**, vì Pixellab xuất ra đúng 1 layer VFX sẵn, nền trong suốt.
4. Với projectile bay: cần thêm logic di chuyển (không phải chỉ animation tại chỗ) — object VFX di
   chuyển bằng code (`Rigidbody2D`/`Transform` lerp), animation chỉ lo phần hình ảnh xoay/nhấp nháy.

## 5. Vì sao bỏ nhánh gen animation nhân vật Fishing/Farming bằng Pixellab

Đã thử nghiệm sâu (xem `PixellabCharacterAssetPipeline.md` và memory `project_pixellab_character_pipeline`)
và xác nhận:
- Tách Head/Body/Weapon từ 1 composite nhân vật luôn có rủi ro cao — mọi kỹ thuật tự động (hình chữ
  nhật, flood-fill theo y, theo y+x, theo màu) đều có lỗ hổng khác nhau xuất hiện mỗi lần thử; chỉ
  cách tỉ mỉ đọc từng pixel bằng mắt mới ra kết quả đúng, nhưng rất chậm (nhiều frame × nhiều lần sửa).
- Chính bản thân animation composite từ Pixellab cũng không ổn định 100% qua các lần gen (từng gặp:
  vũ khí không rõ hình dạng, vũ khí đè lên mặt, tay thừa dính vào mặt) — phải gen lại nhiều lần.
- Không liên quan tới kỹ thuật tách layer — đây là giới hạn thật của multi-part character animation
  gen hiện tại, đã ghi nhận và không đáng để tiếp tục đầu tư thêm cho Fishing/Farming ở giai đoạn này.

**Bài học giữ lại được cho Skill VFX** (không lặp lại các lỗi này):
- `pixelart_workbench` (crop + tô tay theo silhouette đọc bằng mắt) là kỹ thuật đáng tin nhất khi
  thật sự cần tách layer — nhưng Skill VFX không cần dùng tới nó.
- Luôn quét connected-component (mảnh rời rạc) sau khi cắt bất kỳ layer nào — chỉ tiêu "0 pixel lệch
  khi ghép lại" không đủ, không bắt được lỗi gán sai layer.
- Prompt mô tả hành động cho Pixellab cần liệt kê rõ ràng, kèm ràng buộc phủ định (âm bản) khi cần
  chặn lỗi cụ thể đã gặp — ví dụ "tay còn lại giữ ở hông, không đưa gần mặt" mới sửa được lỗi tay dính
  mặt, chỉ mô tả thêm chi tiết tích cực không đủ.

## 6. Việc chưa làm (không tự triển khai thêm khi chưa hỏi)

- Chưa có `SpriteLibrary`/`SpriteResolver` trên `SkillFX` (chờ VFX asset thật).
- Chưa có script điều khiển luồng cast skill (input, cooldown, gọi `BeginSkillCast`/`EndSkillCast`,
  spawn projectile).
- Chưa gen VFX nào bằng Pixellab cho skill cụ thể — chờ chốt §3 mục 4 (danh sách skill) trước.
- Chưa đưa `SkillFX` từ DemoScene sang `MapNhat` — theo đúng `DemoSceneWorkflow.md`, chỉ promote sau
  khi test đầy đủ trong DemoScene.
