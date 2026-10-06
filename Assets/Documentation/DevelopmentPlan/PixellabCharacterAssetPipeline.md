# Pixellab Character Asset Pipeline

Nguồn chuẩn duy nhất để gen animation nhân vật/VFX mới (Fishing, Farming, Skill VFX...) bằng
Pixellab và tích hợp đúng kiến trúc Sprite Library / Sprite Resolver hiện có của `Player`. Đọc file
này trước khi gen bất kỳ state animation mới nào — không suy đoán lại cấu trúc asset.

## 1. Cấu trúc asset gốc đã xác minh (Swordsman lvl1–9)

Ba bộ `Assets/Resources/Player level 1-3|4-6|7-9/PNG/Swordsman_lvl{N}/` đều có cùng layout:

```
Swordsman_lvlN/
  Parts/           # dùng để import Unity — nguồn duy nhất cho SpriteLibrary
  With_shadow/      # full composite preview (KHÔNG import, chỉ tham khảo bố cục)
  Without_shadow/   # full composite preview (KHÔNG import)
  Tiled_files/      # export cho Tiled map editor, không liên quan Unity animation
```

`Parts/` chứa PNG riêng cho từng **part × state**: `body`, `head`, `sword`, `sword_back`
(kiếm giắt sau lưng lúc không vung), `shadow` (bóng đổ theo frame), `swing` (FX vệt chém, chỉ có ở
trạng thái tấn công), `red` (máu, chỉ có ở `Death`).

### Quy tắc canvas — bắt buộc tuân thủ khi gen part mới

Mọi part của **cùng một level + cùng một state** nằm trên **cùng một canvas kích thước tuyệt đối**,
cùng offset, không crop riêng theo bounding box. Ví dụ xác minh trên lvl1/`Walk`: `body`, `head`,
`sword`, `shadow` đều là PNG `384×256`. Đây là lý do 4 layer ghép lại luôn khớp — Unity chỉ chồng
transform cố định, không "tự căn chỉnh" theo nội dung ảnh.

**Canvas KHÔNG bắt buộc giống nhau giữa các level** (nguồn art gốc không đồng nhất): lvl1
`Idle_body.png` = `384×256` nhưng lvl4 `Idle_body.png` = `768×256` (nhiều frame/hướng hơn). Sau khi
slice vào `SpriteLibrary`, cả hai chuẩn hóa về cùng scheme `Frame_0..N` nên animation clip dùng
chung vẫn chạy đúng — chỉ cần **trong cùng 1 lần gen, canvas khớp nhau giữa Head/Body/Weapon/Shadow
của chính level + state đó.**

FX có thể dùng canvas riêng lớn hơn nếu hiệu ứng vượt ra ngoài khung nhân vật (`swing` của lvl1 là
`512×256`, không phải `384×256`) — chỉ cần đúng 1 điểm anchor khớp với part nó bám theo (xem §3).

### Phát hiện quan trọng: `HeavyAttack` + `aoe` đã có sẵn, CHƯA tích hợp (lvl7–9)

`Player level 7-9/PNG/Swordsman_lvl{7,8,9}/Parts/` chứa đầy đủ state `HeavyAttack` (biến thể
`Idle` qua file không hướng `Swordsman_lvl{N}_heavyattack_*`, cộng `Walk_HeavyAttack`/
`Run_HeavyAttack`) với **part thứ 5 `aoe`** (hiệu ứng diện rộng) bên cạnh
`body/head/sword/sword_back` — cùng canvas `512×256`, cùng cấu trúc 8 frame/hướng như `RunAttack`.
Đã xác nhận: **không có category `HeavyAttack` nào trong `Body_lvl7/8/9.spriteLib`/
`Weapon_lvl7-9.spriteLib`, không có `AttackFXLib` category cho `aoe`, không có animation clip
`*HeavyAttack*.anim` nào trong `Assets/Animations/PlayerAnimations/`.**

→ Đây gần như là "đòn chém mạnh có AoE" làm sẵn bởi tác giả gốc, đúng convention pipeline hiện có,
chưa từng được wire vào game. **Trước khi dùng Pixellab gen skill VFX mới, hãy đánh giá tích hợp
HeavyAttack có sẵn này trước** — có thể tiết kiệm toàn bộ vòng gen cho ít nhất 1 skill, và dùng nó
làm mẫu tham chiếu style/canvas cho các skill VFX gen thêm sau.

### Bảng frame/hướng theo state (đo trên `Body_lvl1.spriteLib`, 4 hướng Down/Left/Right/Up)

| State | Tổng frame | Frame/hướng |
|---|---|---|
| Idle | 40 | 10 |
| Walk | 24 | 6 |
| Run | 32 | 8 |
| IdleAttack | 32 | 8 |
| WalkAttack | 24 | 6 |
| RunAttack | 32 | 8 |
| Hurt | 20 | 5 |
| Death | 28 | 7 |

Frame count **không cố định giữa các state** — mỗi state mới (Fishing, Farming, Skill) tự định
nghĩa số frame/hướng riêng, nhưng phải **giống nhau giữa Head/Body/Weapon/AttackFX của cùng một
state** để đồng bộ khi ghép.

### Pivot/PPU thật — đo trực tiếp từ `.meta`, không phải giả định

Đọc trực tiếp `Swordsman_lvl1_Walk_body.png.meta` (và đối chiếu lvl4 cùng state — khớp 100%):

| Part | `spritePixelsToUnits` | `spritePivot`/`pivot` | `alignment` |
|---|---|---|---|
| body / head / sword / sword_back | **16** | `{x: 0.5, y: 0.315}` | 9 (Custom) |
| shadow | **32** | `{x: 0.5, y: 0.315}` | 9 (Custom) |

Hai điểm quan trọng dễ gen sai nếu không tra bảng này:
- **Pivot không phải bottom-center chuẩn** (`0, 0.5`) — nó là `(0.5, 0.315)` tùy chỉnh, cùng giá trị
  cho mọi part và mọi level đã kiểm tra. Gen part mới phải giữ đúng vị trí chân nhân vật lệch lên
  một chút so với mép dưới canvas để pivot này vẫn đúng.
- **`shadow` dùng PPU khác** (32, gấp đôi các part còn lại) dù cùng kích thước cell pixel — đây là
  chủ ý gốc (bóng render nhỏ hơn theo world-unit), không phải lỗi. Giữ nguyên quy tắc này cho state
  mới: nếu gen shadow mới, set PPU = 32 trong khi Head/Body/Weapon vẫn PPU = 16.
- Cell size thực đo trên `Walk` (lvl1): `64×64` px, canvas `384×256` = 6 cột × 4 hàng = 24 sprite
  (khớp 24 frame tổng đã đếm ở §1). Trường `SpriteEditor.SliceSettings` cache trong `.meta` có thể
  ghi `gridCellCount` sai lệch (dữ liệu UI cũ) — luôn tin vào rect thật trong `spriteSheet.sprites`,
  không tin cache slice-settings khi hai bên lệch nhau.

## 2. Ánh xạ vào Unity (Sprite Library / Sprite Resolver)

- Mỗi part có `SpriteLibraryAsset` riêng theo level: `BodyLib/Body_lvl{N}`,
  `HeadLib/Head_lvl{N}`, `WeaponLib/Weapon_lvl{N}` — path
  `Assets/Sprites/SpriteLib/PlayerSpriteLib/`.
- Category bên trong đặt tên `<Part>_<State>` (ví dụ `Body_Run`, `Head_Run`, `Weapon_Run`), label
  `Frame_0..Frame_(N-1)` liên tục — index chạy hết 1 hướng rồi sang hướng kế tiếp theo đúng thứ tự
  Down → Left → Right → Up (đã xác nhận qua `Body_lvl1.Body_Run`).
- Animation clip (`Assets/Animations/PlayerAnimations/*.anim`) keyframe `SpriteResolver.category +
  label` trên 3 GameObject con `Player/Head`, `Player/Body`, `Player/Weapon` — clip dùng chung cho
  mọi level, chỉ đổi `SpriteLibraryAsset` để đổi outfit.
- **AttackFX KHÔNG nằm trong Head/Body/Weapon.** Nó có `SpriteLibraryAsset` riêng
  `PlayerSpriteLib/AttackFXLib.spriteLib`, hiện chỉ có 1 category `Attack_Swing` (16 entry = 4
  hướng × 4 frame, sprite gốc lấy từ file `Swordsman_lvl1_Run_Attack_swing_*.png`).
  **Đính chính so với bản trước của file này**: đọc trực tiếp `IdleAttackDown.anim` và
  `WalkAttackDown.anim` xác nhận cả hai **đều có track `path: AttackFX`** với cùng giá trị
  `m_SpriteHash` như `RunAttackDown.anim` — nghĩa là **1 category `Attack_Swing` duy nhất được
  TÁI SỬ DỤNG cho cả 3 state tấn công** (Idle/Walk/RunAttack), không phải chỉ dành riêng cho
  RunAttack như kết luận trước đó (kết luận cũ chỉ dựa vào tên file nguồn, chưa đọc animation clip
  thật — bài học: luôn xác minh qua clip, không suy đoán từ naming convention của source art).
  Cửa sổ hiện FX (`m_Enabled`) khác nhau đôi chút giữa các state (RunAttack ~0.3–0.8, Idle/WalkAttack
  ~0.3–0.7) nhưng dùng chung sprite. State tấn công mới (skill VFX sau này) vẫn cần tự quyết định có
  dùng chung `Attack_Swing` hay cần category FX riêng — không có rule bắt buộc, chỉ là tiền lệ.
- `Weapon` sorting-order theo hướng trái/phải **không đến từ animation clip** — bị
  [PlayerVisuals.cs](Assets/Scripts/Player/PlayerVisuals.cs) ép cứng mỗi `LateUpdate` (lý do: keyframe int
  trong Blend Tree từng gây flicker khi blend góc chéo). Asset Weapon mới không cần lo sorting order
  trong animation, chỉ cần vẽ đúng pose.
- Ghi chú serialize: `.spriteLib` là ScriptedImporter — sửa category/label phải qua
  `InternalEditorUtility.LoadSerializedFileAndForget` + field `m_Library[].m_OverrideEntries[].m_SpriteOverride`,
  KHÔNG dùng `SpriteLibraryAsset.AddCategoryLabel` (dead-end, không ghi vào file nguồn). Chi tiết kỹ
  thuật đầy đủ nằm trong memory phiên làm việc thứ 12, không lặp lại ở đây.

## 2b. Bảng ánh xạ đầy đủ: loại animation ↔ asset phục vụ nó

Đọc trực tiếp `Assets/Animations/PlayerAnimations/Player.controller` (Animator, `Base Layer`) —
8 state, mỗi state là 1 Blend Tree 4 hướng (Down/Left/Right/Up) chọn theo `InputX`/`InputY` (hoặc
`LastInputX/Y` khi đứng yên):

| Animator State | Điều kiện chuyển vào | Clip (4 hướng) | Category SpriteLib (`<Part>_<State>`) | AttackFX? |
|---|---|---|---|---|
| `Idle` | mặc định / `isMoving=false` | `Idle{Down,Left,Right,Up}` | `Body/Head/Weapon_Idle` | Không |
| `Walk` | `isMoving=true`, `isRunning=false` | `Walk{Down,Left,Right,Up}` | `..._Walk` | Không |
| `Run` | `isRunning=true` | `Run{Down,Left,Right,Up}` | `..._Run` | Không |
| `IdleAttack` | `Attack` trigger từ `Idle` | `IdleAttack{Down,Left,Right,Up}` | `..._IdleAttack` | **Có** (`Attack_Swing`, dùng chung) |
| `WalkAttack` | `Attack` trigger từ `Walk` | `WalkAttack{Down,Left,Right,Up}` | `..._WalkAttack` | **Có** (`Attack_Swing`, dùng chung) |
| `RunAttack` | `Attack` trigger từ `Run` | `RunAttack{Down,Left,Right,Up}` | `..._RunAttack` | **Có** (`Attack_Swing`, dùng chung) |
| `Hurt` | `isHit=true` | `Hurt{Down,Left,Right,Up}` | `..._Hurt` | Không |
| `Dead` | `isDead=true` | `Dead{Down,Left,Right,Up}` | `..._Death` (tên category lệch với tên clip `Dead`, đã xác nhận là quy ước cũ, không phải lỗi) | Không |

Animator Controller parameters đầy đủ: `LastInputX/Y`, `InputX/Y` (hướng nhìn hiện tại/gần nhất),
`isMoving`, `isRunning`, `Attack` (trigger), `isHit`, `isDead` — không có parameter riêng cho
Fishing/Farming/Skill, phải tự thêm khi triển khai (bool hoặc trigger mới + state mới trong cùng
Base Layer, theo đúng pattern Blend Tree 4 hướng ở trên).

**Nguồn PNG gốc ↔ category**, mỗi state ở bảng trên lấy dữ liệu từ
`Parts/Swordsman_lvl{N}_{State}_{body|head|sword|sword_back|shadow}.png` — tên `{State}` trong file
PNG dùng PascalCase khớp animator state (`Idle`, `Walk`, `Run`, `IdleAttack`, `WalkAttack`,
`RunAttack`, `Hurt`, `Death`) ngoại trừ file lowercase không-hướng `attack_*.png` (không map trực
tiếp vào state nào ở trên — nguồn dự phòng/không dùng, xem §1 phát hiện `HeavyAttack` để biết ví dụ
asset có sẵn nhưng chưa tích hợp tương tự).

## 3. Quy trình gen state mới bằng Pixellab MCP thật

Đã kết nối Pixellab MCP thật (xem [[reference_pixellab_mcp_connection]] trong memory) và hỏi thẳng
đội Pixellab (`agent_help`) về đúng bài toán 4-layer của project này. Câu trả lời xác nhận: **Pixellab
không có output per-layer** — model `create_character`/`animate_character` luôn ra 1 sprite đã ghép
phẳng mỗi hướng/frame, tách layer là việc mình tự làm sau. Quy trình dưới đây thay thế hoàn toàn
phiên bản "inpaint mask từng phần" ở các bản trước của file này — kỹ thuật color-tag mới tốt và rẻ
hơn nhiều.

### Bước 1 — Đưa nhân vật hiện có vào Pixellab làm "Character" gốc

Dùng `create_character(mode="v3", reference_image_url=<frame Idle-South hiện có>)` để Pixellab nhận
diện đúng nhân vật/outfit đang có làm character gốc 8 hướng. **Ảnh tham chiếu phải là hướng South
(quay thẳng về camera)** — Pixellab lấy chính hướng của ảnh này làm "south", lệch hướng tham chiếu
sẽ làm sai lệch toàn bộ 8 hướng còn lại.

### Bước 2 — Gen animation composite mới, giới hạn đúng 4 hướng cardinal

```
animate_character(
  character_id=<id bước 1>,
  action_description="fishing cast" / "hoeing the ground",
  directions=["south","north","east","west"],   # KHÔNG dùng mặc định 8 hướng — tốn gấp đôi
  mode="v3"                                       # rẻ; "pro" chỉ dùng khi v3 ra pose không đạt
)
```

`directions` nhận đúng danh sách 4 cardinal — xác nhận trực tiếp từ Pixellab, không phải suy đoán.
Map `south/north/east/west` → `Down/Up/Right/Left` của project. Không có template animation "fishing"/
"farming" có sẵn (`template_animation_id` chỉ có action combat/movement) nên bắt buộc dùng
`action_description` (mode `v3`, hoặc `skeleton-v3` nếu muốn khớp timing với animation có sẵn).

### Bước 3 — Tách layer: color-tag ĐÃ THỬ VÀ THẤT BẠI, dùng tách thủ công

**Cập nhật 2026-09-29 sau khi thử nghiệm thật**: kỹ thuật color-tag mô tả ở các bản trước của file
này (do `agent_help` của Pixellab đề xuất) **không dùng được** — đã test 2 lần trên chính animation
Fishing pilot (xem [[project_pixellab_character_pipeline]] trong memory):
1. `edit_image` batch cả 9 frame trong 1 lần gọi → chỉ 2/9 frame bị đổi màu, và chỉ ra viền rỗng chứ
   không phải fill đặc.
2. Gọi lại trên đúng 1 frame với chỉ dẫn cực rõ ("fill toàn bộ silhouette đầu bằng 1 màu đặc duy
   nhất, không viền không shading") → **không đổi màu gì cả**.

Tốn ~46 generations cho 2 lần thử, không ra được layer nào dùng được. **Không thử lại kỹ thuật này
trên sprite nhỏ (64×64) nữa** trừ khi có cách tiếp cận khác hẳn (canvas lớn hơn nhiều, tool khác).

**Hướng thay thế đã verify THÀNH CÔNG — `pixelart_workbench` (crop/paste), không dùng `edit_image`
AI nữa.** Đây là tool pixel-editing xác định (deterministic), **miễn phí** (không tốn generation),
khác hẳn `edit_image` (AI đoán, không đáng tin ở size nhỏ). Quy trình đã test thật trên frame
Fishing-cast South, ra kết quả sạch 100%:

1. `pixelart_workbench(argv="inspect <url>")` — import frame composite, nhận về `image_id` + bảng màu
   (palette) với số lượng pixel mỗi màu.
2. `pixelart_workbench(argv="clusters <image_id>")` — liệt kê mọi cụm màu liền kề (connected
   component, không phải toàn ảnh) kèm bounding box, giúp xác định pixel nào thuộc vùng nào.
3. `pixelart_workbench(argv="crop <image_id> --region x0,y0,x1,y1")` lấy đúng vùng chứa 1 part (ví
   dụ đầu) → trả về **lưới ASCII chính xác từng pixel + legend màu + fingerprint**.
4. Sửa tay (hoặc bằng script) lưới ASCII: đổi mọi ký tự thuộc phần muốn tách thành 1 ký tự đại diện
   cho màu tag (ví dụ `#ff00ffff`), giữ nguyên phần còn lại và toàn bộ ô `.` (trong suốt).
5. `pixelart_workbench(argv=["paste", "<image_id>", "--rows", "<json rows+legend>", "--node", "image",
   "--expect", "<fingerprint>"])` — ghi đè, có fingerprint bảo vệ chống ghi sai/ghi chồng.
6. Lặp lại bước 3-5 cho Body, Weapon với 2 màu tag khác nhau trên cùng `drawing_id` (chain tiếp từ
   kết quả bước trước) → 1 ảnh cuối có 3 vùng màu độc nhất, ranh giới chính xác tuyệt đối.
7. Tách 3 layer PNG trong suốt bằng script cắt theo màu tag (Python/PIL, không cần Pixellab) — vì
   ranh giới đã chính xác 100% ở bước 6, bước này thuần túy cơ học, không có sai số.

Không cần vẽ tay lại từ đầu trong Aseprite như phương án dự phòng trước đó — `pixelart_workbench` đã
đủ chính xác và miễn phí. Anchor tay/vũ khí vẫn có thể tham chiếu `estimate-skeleton` nếu cần, nhưng
với cách crop/paste này thường không cần thiết vì ranh giới màu đã đủ rõ để xác định vị trí.

**Hạn chế đã biết**: làm thủ công từng vùng/frame là chính xác nhưng tốn thao tác (mỗi frame mỗi
part cần 1 lần `crop`+sửa lưới+`paste`) — với 9 frame × 4 hướng × 3 part sẽ là hàng chục lệnh. Nên
cân nhắc viết script tự động hóa việc build lưới thay-thế (từ palette đã biết, tự động gán màu tag
theo cluster đã `clusters` liệt kê) thay vì sửa tay từng ô cho toàn bộ animation.

**Quan trọng — `cluster --at` chỉ đáng tin với vùng đặc, KHÔNG dùng cho chi tiết mảnh 1px**: đã bắt
lỗi thật (2026-09-30) khi tách Weapon (cán cần câu, đường chéo 1px) bằng cách click từng cluster —
anti-alias khiến đường chéo mảnh vỡ thành hàng chục cụm 1-2px riêng biệt, nhiều cụm trùng màu với chi
tiết khác trên nhân vật (ở đây trùng màu đế giày), khiến kết quả tách sót mất đoạn gần tay cầm (Body
vẫn dính 1 khúc cán, Weapon chỉ còn đoạn ngắn). **Sửa đúng: với chi tiết mảnh, luôn `crop` cả vùng,
nhìn kỹ lưới pixel bằng mắt, tô lại TOÀN BỘ vùng bằng `paste` trong 1 lần** (đúng kỹ thuật đã dùng
cho Đầu — vùng đặc), không dùng shortcut `cluster --at` cho bất kỳ chi tiết mảnh nào (vũ khí, dây,
tóc bay, vệt hiệu ứng...).

### Bước 4 — Áp dụng qua 9 level outfit, không re-animate từng level

Gen 1 lần cho 1 level (khuyến nghị lvl1, art đơn giản nhất để dễ kiểm), sau đó:
`edit_image` (Pro) trên **cả sheet animation cùng lúc**: "thêm giáp/kiếm lvl4 vào mọi frame, giữ
nguyên pose và tỉ lệ" — rẻ hơn hẳn so với gọi `create_character_state`/`animate_character` riêng cho
từng level. `create_character_state` (20-40 generations/lần, áp dụng đồng bộ cả 4-8 hướng) là lựa
chọn khi cần đổi hẳn outfit chứ không chỉ 1 vài vật phẩm.

### Skill VFX — không bake vào nhân vật, gen như object riêng

Khuyến nghị chính thức từ Pixellab (khớp với kiến trúc `AttackFX` hiện có của project — xem §2):
KHÔNG bake FX vào sheet nhân vật. Gen bằng `create_8_direction_object` (hoặc chỉ 1 hướng bằng
`create_1_direction_object` nếu FX không cần xoay theo hướng nhân vật) → `animate_object` → composite
trong Unity qua chính `AttackFXLib.spriteLib`/GameObject `AttackFX` đã có sẵn, không cần kiến trúc
mới.

### Cảnh báo chi phí (nguyên văn từ Pixellab, không phải suy đoán)

> "one outfit across 8 directions × several animations runs into thousands of generations" —
việc giới hạn còn 4 hướng (đã làm ở Bước 2) là bước tiết kiệm chi phí quan trọng nhất, không phải
tùy chọn. Luôn `get_balance` trước khi chạy batch lớn (nhiều level × nhiều state).

## 4. Quy tắc đặt tên file & import

- File part mới: `Swordsman_lvl{N}_{State}_{part}.png` — giữ đúng convention hiện có
  (`body/head/sword/sword_back/shadow`), đặt trong `Parts/` cùng cấp asset pack tương ứng.
- Weapon part của state phi-combat (câu cá, cày ruộng) vẫn dùng field `spriteLibraryAsset` có sẵn
  trên `EquipmentItemSO`/`WeaponLib` — không tạo slot part thứ 5, chỉ thêm category mới vào
  `Weapon_lvl{N}.spriteLib`.
- FX state mới thêm category mới vào `AttackFXLib.spriteLib` (hoặc `.spriteLib` FX riêng nếu số
  lượng FX tăng nhiều — quyết định khi cần, chưa cần tách ngay).
- Slice bằng provider API đã dùng cho lvl4-9 (không dùng `TextureImporter.spritesheet` legacy trên
  texture đã qua Sprite Editor).

## 5. QA checklist trước khi coi 1 state là xong

- [ ] Ghép Body+Head+Weapon(+Shadow+FX) trên cùng canvas trong Aseprite/Photoshop — không lệch
      pixel giữa các layer ở bất kỳ frame nào.
- [ ] Số frame/hướng bằng nhau giữa mọi part của state.
- [ ] Palette khớp level đang gen (so bằng crop+view, không suy đoán từ tên file).
- [ ] Play Mode: đổi `SpriteLibraryAsset`, animation clip mới chạy đúng, FX (nếu có) bắn đúng frame
      kích hoạt, không lệch 1 frame như bug Weapon flicker đã từng gặp.

## 5b. Cổng review bắt buộc trước khi tích hợp (rút kinh nghiệm từ D-055/D-057)

Hai lần trước đây (`Handoffs/`, xem D-055 Fishing UI và D-057 PlayerHUD trong `DecisionRegister.md`)
asset AI-gen đã bị wire thẳng vào prefab/animation rồi mới phát hiện lỗi khi owner review Play Mode
thật, dẫn tới phải `git checkout` revert toàn bộ. Với animation nhân vật (chi phí tích hợp cao hơn
UI vì đụng tới `.spriteLib` + animation clip + Animator), áp dụng cổng review bắt buộc:

1. Xuất ảnh ghép thử (composite 4 layer trên cùng canvas, xem §5 QA checklist) ra file PNG tĩnh —
   **chưa** import vào `.spriteLib`.
2. Gửi ảnh này cho owner duyệt bằng mắt (đúng pose, đúng palette, đúng pivot cảm quan) trước khi làm
   bước slice + provider-API wiring.
3. Chỉ sau khi owner xác nhận mới tiến hành wire vào `SpriteLibraryAsset`/animation clip.

Không tự ý bỏ qua bước 2 dù pipeline đã tự động hóa — chi phí revert sau khi wire cao hơn nhiều so
với việc duyệt trước 1 ảnh tĩnh.

## 6. Phạm vi chưa quyết định (Open)

- Fishing/Farming có cần bộ animation riêng cho từng level (lvl1-9) hay dùng chung 1 bộ pose bất kể
  outfit? (khác với combat, vốn có 9 bộ Body/Head/Weapon riêng theo level.)
- Skill VFX có cast animation riêng theo hướng (4 hướng) hay chỉ 1 hướng cố định để giảm khối lượng
  asset?
- Có tích hợp `HeavyAttack`/`aoe` có sẵn (lvl7-9, xem §1) làm skill đầu tiên trước khi gen skill VFX
  mới bằng Pixellab hay không, và nếu có thì gate theo level nào (chỉ lvl7-9, hay port ngược
  pose/FX này cho lvl1-6 bằng chính quy trình Pixellab ở §3)?

Không tự chọn thay cho hai mục trên khi triển khai — hỏi trước, ghi quyết định vào
[DecisionRegister.md](DecisionRegister.md) khi chốt.
