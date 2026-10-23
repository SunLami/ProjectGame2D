# Development Roadmap

2026-10-14 D-117: Inventory categories and Used Items/Active/Inactive configuration implemented,
with eligibility checks and active-use event for teammate integration. Effects remain teammate-owned.
Owner manual verification pending; no new automated or Play Mode test run claimed.

2026-10-14 owner-requested next slice: DemoScene Q/E/R/T cast the learned skills assigned in four
loadout slots; Tab kit switching/debug Test Skill button removed. Existing aim/confirm mechanics retained.
Manual verification belongs to owner; no automated test/Play Mode changes for this slice. See SkillProgression.md.

2026-10-13: owner authorized D-116 runtime Skill Points/learning and ghost drag-loadout, with save V13.
Implementation/verification status: SkillProgression.md. Production promotion and skill casting remain separate.

2026-10-14: Skill Points/learning/drag-loadout verification: EditMode 46/46 (2026-10-13),
PlayMode capture/restore/dirty 21/21. Owner now handles manual Unity tests; no production/build acceptance claimed.

SkillPanel asset stage (D-116): 12 Water/Earth/Wind icons and English tooltip copy authored in
SkillIconAssetPack.md; per-element learning costs 2/3/4/5. This historical asset-only milestone is
superseded by the runtime implementation and verification above; asset completion alone is not feature completion.

2026-10-10 UI asset continuation: six new frame/component PNGs plus reused Inventory Close X,
documented in SkillPanelUIAssetPack.md. Prefab presentation and 1920x1080 checks are recorded in the implementation report below.

UI presentation assembly now created in SkillPanel.prefab and DemoScene; see
SkillPanelImplementationReport.md for verification and remaining backend work. No learning/save milestone
is marked complete by this UI slice.

## Nguyên tắc triển khai

Mỗi phase tạo ra một lát cắt có thể kiểm chứng. Không xây đồng thời Save, Quest và World persistence
khi data contract chưa ổn định. Mọi thay đổi serialized asset phải được kiểm tra trong Unity, không chỉ
bằng static analysis.

## Data-Driven Gate áp dụng cho mọi phase

Mọi domain content mới phải tuân theo [Data-Driven Development Guide](DataDrivenDevelopment.md):

- Tách Definition asset, Runtime State và Save DTO.
- Có stable ID convention và không dùng GameObject/filename/array index làm save identity.
- Catalog/resolver là dependency explicit; không lookup Resources rải rác.
- Có validator cho ID, reference và cross-domain contract.
- Code handler định nghĩa behavior type; data định nghĩa content cụ thể.
- Chứng minh ít nhất hai content variants dùng cùng runtime code khi domain có tính biến thể.
- Không mutation ScriptableObject definition trong runtime.

Không phase nào được đánh dấu hoàn tất nếu content data mới chưa đạt các gate phù hợp với domain đó.

## Phase 0 — Ổn định project baseline

### Mục tiêu

Tạo một baseline build/play đáng tin trước khi thêm hệ thống nền tảng.

### Công việc

- Giữ `DemoScene.unity` làm integration playground chính thức; không rename thành GameScene.
- Chốt tên và đường dẫn `MainMenu.unity`.
- Chưa bắt buộc chốt topology world scene production; định nghĩa `sceneId`/`areaId` từ đầu.
- Tạo convention `_SceneContext`, feature prefab root và demo-only debug tools.
- Chốt stable ID naming convention và danh sách domain ID ban đầu.
- Inventory toàn bộ ScriptableObject/catalog hiện có; chưa refactor hàng loạt.
- Sửa Build Settings đang tham chiếu `SampleScene.unity` đã bị xóa.
- Lưu snapshot git sạch hoặc commit baseline có thể quay lại.
- Sửa compiler/runtime error chặn test, ưu tiên TMP Font Atlas và missing reference.
- Lập danh sách prefab/service hiện dùng `DontDestroyOnLoad`.
- Xác nhận Input System action map dành riêng cho Main Menu và Gameplay.

### Acceptance criteria

- Mở project không có compiler error.
- Có thể chạy trực tiếp DemoScene để test integration.
- Có thể chạy từ MainMenu qua New/Continue mock vào DemoScene trong giai đoạn development.
- Demo-only tool được nhận diện rõ và không vô tình phụ thuộc production code.
- Play mode 5 phút không có exception lặp lại trong Console.
- Scene/prefab thay đổi được lưu và version-control nhận đúng `.meta`.
- ID convention và data-driven boundaries được ghi nhận trước khi tạo save schema.

## Phase 1 — Scene bootstrap, GameState và GameSession

### Mục tiêu

Triển khai luồng `Booting → MainMenu → Loading → Playing` và phân tách rõ scene/menu overlay.

### Công việc

- Đổi `GameState.Menu` thành `GameState.GameplayMenu`.
- Đổi `GameMenuPage` thành `GameplayMenuPage`.
- Thêm `GameState.MainMenu`.
- Không tự động vào `Playing` trong `GameStateManager.Awake`.
- Thêm `GameBootstrap` xác định initial state dựa trên scene khởi động.
- Thêm `SceneFlowService` chịu trách nhiệm load scene; UI không gọi `SceneManager` trực tiếp.
- Thêm `GameSessionManager` giữ slot đang active và loại session `NewGame`/`LoadedGame`.
- Xác định transition failure path: load thất bại phải trở về Main Menu và hiển thị lỗi.
- Giữ gameplay overlay stack: `Playing → Paused → GameplayMenu(Settings) → Paused`.
- Cho phép development session chọn DemoScene làm gameplay target mà không hard-code nó vào save schema.

### Acceptance criteria

- Player build luôn bắt đầu ở Main Menu.
- Chạy trực tiếp DemoScene trong Editor có development bootstrap rõ ràng và không cần đi qua MainMenu.
- Không có gameplay input trong Main Menu/Loading.
- Không có frame gameplay hiển thị trước khi restore save xong.
- Trở về Main Menu giải phóng scene gameplay hiện tại (DemoScene hoặc world scene) và reset active session.
- Pause/Inventory/Settings hiện tại vẫn hoạt động sau rename.

## Phase 2 — Save slot metadata và file foundation

### Mục tiêu

Có ba save slot đáng tin cậy trước khi lưu gameplay domain.

### Công việc

- Xây `SaveSlotRepository` quản lý Slot 1–3.
- Định nghĩa `SaveSlotMetadata` và `GameSaveData` có `saveVersion`.
- Save DTO chỉ lưu stable ID/state delta, không serialize ScriptableObject/GameObject reference.
- Tách capture snapshot khỏi serialize/write file.
- Ghi file atomic bằng temp file, backup và replace.
- Thêm checksum hoặc validation tối thiểu cho JSON.
- Hỗ trợ slot trống, corrupted, compatible và incompatible version.
- Thêm delete slot có confirm; không overwrite slot ngoài lựa chọn người chơi.
- Thêm mock/in-memory repository để test UI không cần ghi thật.

### Acceptance criteria

- Tạo, đọc, overwrite và xóa độc lập cả ba slot.
- Crash/failure trong lúc ghi không phá save cuối cùng hợp lệ.
- Main Menu đọc metadata mà không load toàn bộ world data.
- Corrupted slot không làm crash Main Menu và không ảnh hưởng slot khác.

## Phase 3 — New Game/Continue và player restore trong DemoScene

### Mục tiêu

Hoàn tất hai đường vào gameplay bằng DemoScene làm integration target đầu tiên. Scene production dùng
cùng flow sau khi được tạo.

### Công việc

- Main Menu New Game mở slot selector và chỉ cho chọn slot trống hoặc overwrite có confirm.
- Tạo default save với stable `saveId`, `areaId = tutorial_area`, spawn ID `tutorial_start`.
- Continue chỉ hiển thị slot hợp lệ có metadata.
- Xây `SpawnRegistry` ánh xạ stable spawn ID thành Transform.
- Lưu `areaId` cùng position; không chỉ lưu raw transform.
- Restore PlayerStat, health, position và session play time theo thứ tự xác định.
- Chỉ chuyển sang `Playing` sau khi player đã spawn và camera đã bind.
- Scene target được resolve từ development config/session, không ghi chuỗi `DemoScene` vào domain save.

### Acceptance criteria

- New Game luôn spawn đúng Tutorial Area và không nhận item hai lần khi reload.
- Continue spawn sai số trong tolerance đã định tại đúng vị trí cũ.
- Load một slot không rò dữ liệu inventory/stat từ slot trước.
- Save mới và save cũ dùng chung gameplay path trong DemoScene sau bước restore.
- Cùng feature root có thể chạy trong một minimal test scene mà không mang toàn DemoScene theo.

## Phase 4 — Inventory, equipment và stat persistence

### Mục tiêu

Lưu/restore đầy đủ dữ liệu nhân vật hiện có.

### Công việc

- Chốt unique `itemId` và editor validation cho trùng/rỗng.
- Xây item catalog/resolver contract thống nhất; giữ Resources như migration implementation nếu cần.
- Tách item definition read-only khỏi runtime slot/save state.
- Bổ sung gold vào save.
- Bổ sung equipment slots vào save bằng item ID.
- Clamp quantity, stack size và item compatibility khi load.
- Sửa equip/unequip thành transaction để inventory đầy không mất item.
- Seed starting inventory chỉ khi tạo New Game, không chạy mỗi lần load scene.
- Recalculate stats từ base progression + equipment sau restore.

### Acceptance criteria

- Round-trip inventory/gold/equipment không thay đổi dữ liệu.
- Load thiếu item catalog không crash; báo warning và giữ report phục hồi.
- Không duplicate starter item khi load/reload.
- Không thể mất item khi inventory đầy trong equip/unequip.

## Phase 5 — Tutorial system

### Mục tiêu

Hướng dẫn thao tác cho nhân vật mới nhưng không biến tutorial thành game mode cứng.

### Công việc

- Xây `TutorialManager` và `TutorialStep` data-driven.
- Định nghĩa Tutorial Definition/Step Definition, completion condition handlers và catalog validation.
- Theo dõi movement, sprint, attack, inventory và equipment bằng domain event.
- Lưu current step/completed trong save.
- Cho phép rời khu tutorial; thiết kế prompt nhắc thay vì khóa sandbox.
- Xác định skip/replay policy (khuyến nghị: skip có confirm, replay không đổi progression).
- Hướng dẫn đi Town sau input tutorial.
- Dựng Tutorial/Town test area trong DemoScene bằng prefab/config có thể promote sang world scene.

### Acceptance criteria

- New Game bắt đầu đúng step đầu.
- Save/load giữa một step tiếp tục đúng step, không phát thưởng lần hai.
- Save cũ đã hoàn thành không bật tutorial lại.
- Tutorial không subscribe event trùng sau load hoặc scene lifecycle.

## Phase 6 — Quest foundation và Tutorial Quest chain

### Mục tiêu

Có quest data-driven, objective tracking và prerequisite mở Main Quest.

### Công việc

- Định nghĩa Quest Definition asset với stable `questId`.
- Định nghĩa runtime `QuestProgress` độc lập asset.
- Định nghĩa Quest Save DTO chỉ chứa ID, status và counters.
- Hỗ trợ objective cơ bản: talk, obtain, craft, purchase, gather và kill.
- Dùng gameplay event bus typed event; không poll toàn thế giới mỗi frame.
- Xây NPC quest interaction và turn-in validation.
- Xây chuỗi Tutorial Quest; hoàn thành chain phát `MainQuestUnlocked`.
- Lưu active/completed quest và objective counters.
- Chống phát thưởng lặp lại sau load hoặc double-click turn-in.

### Acceptance criteria

- Objective chỉ tăng từ event hợp lệ và không vượt target ngoài thiết kế.
- Save/load giữ đúng active step/counter.
- Main Quest NPC không cấp Main Quest trước prerequisite.
- Người chơi vẫn khám phá/craft/shop khi chưa làm Tutorial Quest.
- Hoàn thành Tutorial Quest mở Main Quest đúng một lần.

## Phase 7 — NPC Shop/Crafting và quest integration

### Mục tiêu

Hoàn chỉnh các tương tác cần cho Tutorial Quest.

### Công việc

- Tách `ShopService`, `CraftingService` khỏi UI.
- Transaction mua/bán/craft phải atomic đối với gold, nguyên liệu và inventory capacity.
- Phát typed events `ItemPurchased`, `ItemCrafted` cho QuestManager.
- NPC chỉ mở đúng interaction khi player trong range và gameplay state cho phép.
- Chốt recipe/item catalog validation.
- Shop/Recipe definition tách runtime transaction state; event chỉ phát sau transaction thành công.

### Acceptance criteria

- Giao dịch thất bại không trừ tiền/nguyên liệu một phần.
- Quest objective không phụ thuộc click UI; chỉ phụ thuộc transaction thành công.
- Save/load giữ kết quả giao dịch thông qua inventory/gold persistence.

## Phase 8 — World persistence

### Mục tiêu

Lưu các thay đổi lâu dài của sandbox mà không cố serialize mọi GameObject.

### Công việc

- Gán stable `persistentId` cho chest, unique pickup, boss và resource node cần lưu.
- Phân biệt definition ID với persistent instance ID.
- Xây registry phát hiện ID trùng trong editor.
- Chia entity thành persistent và respawn-by-rule.
- Lưu chest opened, unique pickup collected, boss defeated và resource respawn timestamp.
- Khôi phục world trước khi cho phép gameplay.
- Sửa MapManager/service scene reference để rebind sau load.
- Chứng minh persistent feature chạy cả DemoScene và minimal portability scene.

### Acceptance criteria

- Persistent entity giữ trạng thái qua save/load.
- Enemy thường respawn theo rule, không làm save phình không cần thiết.
- ID trùng bị phát hiện trước build.
- Object bị xóa/đổi giữa version không làm hỏng toàn bộ save.

## Phase 9 — Save/load UX trong gameplay scene

### Mục tiêu

Người chơi save/load/return Main Menu an toàn từ Pause Menu.

### Công việc

- Save Game ghi vào active slot; confirm trước overwrite nếu policy yêu cầu.
- Load Game hiển thị ba slot nhưng phân biệt rõ active slot.
- Block double-submit trong Saving/Loading.
- Hiển thị success/error và timestamp sau save.
- Return Main Menu hỏi save trước khi rời nếu có dirty session.
- Quit Desktop có lựa chọn save, quit without saving, cancel.

### Acceptance criteria

- Không thể tương tác gameplay trong Loading/Saving theo policy.
- Lỗi ghi file trả UI về state trước, không kẹt timeScale 0.
- Load slot khác reset sạch session cũ.
- Quit/return không tự ghi đè save nếu người chơi chưa xác nhận.

## Phase 10 — Hardening và content-ready milestone

### Mục tiêu

Đưa nền tảng thành trạng thái đủ an toàn để sản xuất content quest/world dài hạn.

### Công việc

- Save migration từ version N sang N+1.
- Automated tests theo Quality Strategy.
- Soak test save/load nhiều vòng và đổi slot liên tục.
- Profiling snapshot size, serialization time và allocations.
- Build test độc lập Editor.
- Recovery UX cho backup/corrupted/incompatible save.
- Tài liệu authoring quest, item, persistent entity và area/spawn ID.

### Acceptance criteria

- Test matrix bắt buộc đều pass.
- Không có P0/P1 known issue trong save, progression hoặc item transaction.
- Content designer có thể tạo Tutorial Quest mới mà không sửa manager core.
- Build player chạy đúng New Game và Continue trên máy sạch.

### Trạng thái: `CONTENT_READY` — 2026-08-23

Cả bốn acceptance criteria trên đã đạt: EditMode 58/58, PlayMode 141/141, Content Validation 0
error, DemoScene/MainMenu validator 0 issue; không có P0/P1 mở; `ContentAuthoringGuide.md` đủ để
designer tạo Tutorial Quest mới không sửa manager core; physical acceptance 24/24 bước PASS trên
Player build Windows64 thật (`New Game → DemoScene → Save (Empty/Overwrite/Save As/Delete) → Return
→ Continue → verify restore → Quit`). Chi tiết đầy đủ:
[Phase10ImplementationReport.md](Phase10ImplementationReport.md). Đây là phase cuối cùng của roadmap
nền tảng này — không có Phase 11 kế tiếp trong tài liệu; bước tiếp theo là sản xuất content thật
(quest/world) theo [ContentAuthoringGuide.md](ContentAuthoringGuide.md), hoặc chốt các decision còn
`Open`/`Proposed` trong `DecisionRegister.md` (D-010, D-013, D-019, ...) nếu muốn mở rộng nền tảng
thêm trước khi sản xuất content quy mô lớn.

### Content slice sau `CONTENT_READY`: Quick bar và Farming — 2026-09-22

- Quick bar 8 slot đã chuyển từ presentation-only sang stable `itemId` assignment, chọn bằng click/
  phím 1–8 và persist ở save schema V8.
- Farming ô cố định đã có hai crop variants, UTC growth, transactional harvest/loot fly, hover feedback,
  non-combat click và persist ở save schema V9.
- Đã promote prefab/catalog từ DemoScene sang MapNhat (8 plot demo, 21 plot production candidate),
  bổ sung validator và tài liệu authoring/acceptance tại [FarmingSystem.md](FarmingSystem.md).
- Gate đã chạy: EditMode 73/73 và Quick bar/Farming PlayMode 5/5. Full PlayMode regression được giữ
  như release gate chung; test runner MCP đã discover 169 test nhưng bị kẹt trước test đầu trong lần
  chạy này, không ghi nhận failure và đã được clear an toàn.

## Trình tự bắt buộc

### PlayerHUD consolidation — 2026-10-09 (in progress)

D-115 supersedes the historical eight-slot HUD contract: four slots shared with Inventory, PlayerHUD
bottom-left at 1920x1080, EXP label and fill-only vitals including runtime mana. Remove BottomHUD
without removing Map/Character/Minimap. V11→V12 migration and verification: `PlayerHUDRedesign.md`.

### Inventory presentation slice — 2026-10-07

D-114: năm category bookmarks và Quick Assign 1–4, giữ nguyên tám quick slot/save contract.
Owner yêu cầu dùng nguyên ảnh khung approved; source prefab giữ overlay text/icon/hit target riêng.
Audit live DemoScene tại 1920x1080 phát hiện và gỡ 827 Bootstrap geometry overrides thuộc Inventory;
sửa raycast blocker, empty-slot targets và reset scroll khi mở. Các luồng filter, quick assignment,
swap, equip/unequip, tooltip, discard/cancel và 20 vòng open/close đã qua kiểm tra live in-memory.
Trạng thái regression/production smoke mới nhất ghi tại `InventoryTabbedImplementationReport.md`;
không coi screenshot preview là thay thế cho acceptance trong scene/build.

```text
Phase 0
  → Phase 1
  → Phase 2
  → Phase 3
  → Phase 4
  → Phase 5
  → Phase 6
  → Phase 7
  → Phase 8
  → Phase 9
  → Phase 10
```

Phase 5 và thiết kế Quest Definition của Phase 6 có thể nghiên cứu song song sau Phase 3, nhưng không
merge runtime progression trước khi save schema và domain event contract của Phase 4 ổn định.

2026-10-16: D-118 SkillHUD presentation correction rebuilt shared source prefab (diamond masks,
explicit overlay sprites, larger icons, corrected key typography). Editor render/compile checked;
manual gameplay/resolution/build acceptance pending. See SkillProgression.md.

2026-10-16 follow-up: SkillHUD source-space center correction and inset sizing verified with an
isolated Editor render; supersedes prior oversized 250/300 icon layout. Gameplay acceptance pending.

2026-10-16: owner-requested 12 skill + Shield V2 generation complete; catalog, panel source and
HUD source updated. Isolated Editor render checked; manual gameplay/build acceptance pending.
See SkillIconV2.md.

2026-10-17: owner-authorized shared SkillHUD/SkillPanel composition implemented with 13 transparent
motif derivatives, independent shaped backgrounds and alpha-hull sizing. Source prefabs/builders
synchronized; isolated Editor renders reviewed. Gameplay/build acceptance pending.
See SkillIconSharedPresentation.md.

## Skill Point budget — owner request 2026-10-22 (D-116 continuation)

Owner replaces unlimited level awards with a catalog-derived learning budget: current twelve skills
cost 3 × (2 + 3 + 4 + 5) = 42 points total. Keep +1 per gained level, but available points never
exceed the total cost of known unlearned skills. Spending does not reopen award capacity beyond
that remaining cost. At all skills learned, points remain zero even on subsequent level-ups.
PlayerStat binds the serialized catalog (or the existing Resources catalog once at composition);
missing catalog fails closed with zero award budget. UI continues reading player-owned state.
Restore clamps legacy surplus in runtime only, preserves learned/unknown IDs and loadout, never
grants points and never writes the source DTO/file. Save V13 fields and migration remain unchanged;
normal explicit saves capture the normalized balance. No automatic test runs or Play Mode transitions
per existing owner workflow. Regression source added for cap, spending, legacy surplus and all learned;
manual gameplay/build acceptance pending.

## D-118 mana and cooldown continuation — Accepted, owner request 2026-10-22

Supersedes the earlier no-mana scope and aim-start cooldown timing. All twelve skills use catalog
manaCost and cooldownSeconds. Initial tier 1/2/3/4 tuning for Water/Earth/Wind: mana 8/16/24/35,
cooldown 2/6/10/18 seconds. Builder defaults and live catalog match; tooltip displays both values.
Aim entry checks mana and cooldown. Confirmation rechecks gameplay/alive/learned/equipped state,
consumes mana through PlayerStat.TryConsumeMana once and starts cooldown before spawning effects.
Cancel or missing prefab (no aim started) spends nothing; mana lost during aim prevents effects.
Existing slot-keyed cooldown and scaled gameplay clock retained per D-118; HUD reads same gate.
No new regen policy, save field, schema change or Shield change. Mana remains the existing runtime
resource; cooldown remains transient. Automatic tests/Play Mode remain owner-controlled.
Manual acceptance pending: each of twelve casts, insufficient mana, exact cost, cancel, pause,
missing prefab, loadout changes during aim, HUD overlay and PlayerHUD mana refresh.

### SkillHUD countdown text — owner request 2026-10-22, D-118 continuation

Four Q/E/R/T icons show centered white Digital Disco cooldown seconds, rounded up (18…1),
above the radial overlay with dark outline. Ready/empty slots hide numbers. Remaining time comes
from the same SkillTestHarness deadline as the cast gate and radial overlay, using scaled time.
Shared GameplayUIRoot prefab receives a targeted text-only upgrade; full builder also creates texts.
Shield, cooldown tuning and save remain unchanged. Prefab wiring statically checked; Unity MCP stopped answering after refresh, so compile/editor verification remains pending. Owner manual
gameplay and visual acceptance pending; no automatic tests or Play Mode transitions.

### Mana recovery and Shield guard — owner request 2026-10-22, D-118 continuation

Mana still spends only on confirmed casts. PlayerStat now regenerates 3 mana/second after
a 2-second delay from positive spending, only during living gameplay. T remains loadout slot 3;
key polling is independent and failed learned/mana/cooldown gates log a readable reason.
DemoScene ultimate prefab references are present; exact owner T failure still requires reproduction.
Shield supersedes the old 70% mitigation: full block, no HP loss/hurt animation/knockback/hurt sound
or hurt invulnerability on blocked hits. Holding drains existing 20 stamina/sec without passive
stamina regeneration; each blocked hit additionally drains 15 stamina. Last exhausting hit is blocked
in full, clamps stamina to zero and breaks guard; subsequent hits damage normally. Exhaustion locks
reactivation for 1 scaled second, including while RMB remains held; stamina can recover during lockout.
All resource/break rules live in PlayerStat; Player owns input, VFX and hurt routing. No save changes.
Initial tuning is serialized on PlayerStat. Owner manual acceptance remains pending; no automated
test run or Play Mode transition. Compile checks recorded separately after implementation.

Verification for mana/shield continuation: Unity compile completed with zero Console errors.
Read-only Editor inspection resolves Player, SkillCatalog and all three ultimate prefab references
on the active DemoScene bridge. No missing T ultimate reference found. All cast rejection gates now
log their reason when the key is pressed. No live T reproduction or gameplay acceptance is claimed.
Shield state/VFX are cleared when Player disables; cooldown deadline remains in PlayerStat.

### Shield single-hit break — owner request 2026-10-22 (supersedes stamina guard above)

Shield costs no stamina to activate, hold or absorb a hit, and can activate at zero stamina.
A blocked hit deals zero HP damage, immediately clears guard and starts a 5-second scaled
cooldown in PlayerStat; Player hides VFX immediately through the Blocked result path.
No hurt/knockback on that hit; following hits use normal damage. Holding RMB cannot bypass
the lockout; after expiry it may raise the shield again. Manual release has no hit cooldown.
Passive stamina recovery continues while holding. No save or schema changes.

HP investigation (read-only Editor inspection): active DemoScene Slime1/Slime2/Goblin/ForestGuardianBoss
attack profiles each author 20 raw damage. Player base HP 100, Defense 2, growth +3 HP/+0.15 Defense
per level. At level 101 without equipment this gives 400 max HP, 17 Defense, and 3 damage per hit
(0.75% HP). HUD fill correctly uses current/max HP. Equipment Defense/DamageReduction may lower
this further; 0.45-second hurt invulnerability can also suppress overlapping hits. No live high-level
loadout/HP measurement was performed. Damage formula/tuning unchanged; balance revision requires
a separate owner choice. Owner manual single-hit shield/gameplay/build acceptance pending.

### Shield HUD countdown — owner request 2026-10-22

ShieldSlot now includes the same centered white outlined Digital Disco countdown as Q/E/R/T.
SkillHUDController reads PlayerStat.ShieldCooldownRemaining directly (5…1, hidden at ready);
shared prefab and builder/targeted upgrade include the Shield text. No guard/resource/save changes.
Owner manual visual/gameplay acceptance pending; no automated tests or Play Mode transitions.

### Shield radial overlay — owner request 2026-10-22

Shield now mirrors the skill slots' 72%-black diamond Radial360 cooldown overlay. Controller uses
remaining / PlayerStat.ShieldCooldownSeconds from the same five-second guard deadline; countdown
text is last sibling above overlay for readability. Builder and targeted prefab upgrade synchronized.
No gameplay timing/save changes. Owner manual visual acceptance pending, no Play Mode/test run.

## MapNhat skill UI/casting and dash promotion — owner approved 2026-10-22

Owner accepted DemoScene presentation and requested production promotion (D-085/D-116/D-118).
Bootstrap owns the connected SkillPanel prefab; MapNhat contains one PlayerSkillCasting prefab instance,
with explicit local Frawand Player binding and shared SkillCatalog. New reusable casting prefab
Assets/Prefabs/Skills/PlayerSkillCasting.prefab carries all twelve behavior prefab configurations
from DemoScene, with no demo controls or scene Player reference baked into the source.
Shared SkillHUD already comes from Bootstrap's GameplayUIRoot prefab, including Shield radial/text
cooldowns; no duplicate HUD is added to MapNhat. SkillFX child promoted to local Player and
SkillFX/Shield renderers bound. Existing shared Player Animator and PlayerInput supply dash.
MapNhat Player dash allow-list and new-component default include DemoScene/MapNhat; tuning remains
3.2 units / 0.2s, 0.8s cooldown, 20 stamina and 0.12s initial invulnerability.
Learning, mana, shield, cooldown and save V13 use existing shared runtime contracts, no regrant/reset.
Editor validation: one Bootstrap panel, one MapNhat bridge, local Player/catalog and every skill prefab resolved, Dash action
exists, shared HUD count one. MapNhat stays active, source DemoScene read additively then closed
without saving it. Owner manual New Game/Continue, P, Q/E/R/T, Shield and Space/gamepad smoke
acceptance and Player build remain pending; no automated tests or Play Mode transitions performed.

### UI ownership correction — owner clarified 2026-10-22

SkillPanel and SkillHUD belong to persistent Bootstrap. SkillPanel prefab is instantiated once
in Bootstrap; MapNhat and DemoScene local copies are removed to avoid duplicate P input handlers.
MapNhat retains PlayerSkillCasting, local Player/FX binding and dash configuration. SkillPanel
already rebinds PlayerStat.Instance as sessions change and gates P to Playing/Skills state;
scene unloading does not own the shared UI lifetime. No save/gameplay tuning changes.

2026-10-22 owner-approved Quest Offer art refresh: three built-in ImageGen pixel sprites, shared prefab/builder synchronized and isolated Editor render reviewed. Details/prompts/remaining gameplay acceptance: [Quest Offer visual refresh](QuestOfferVisualRefresh.md).

2026-10-22 owner-requested twelve-skill MapNhat VFX audit: ground/water terrain renderers promoted
to Ground1 (all terrain and water) below Default. Ground VFX retain documented negative orders below actors;
MapNhat terrain must not use Default above these effects. Earth 4 rune rendering verified with an
isolated Editor sample. Details, affected skills, changed map layers and manual acceptance limits:
[MapNhat skill sorting audit](SkillMapNhatSortingAudit.md).

2026-10-22 owner request: remove Minimap from Bootstrap shared HUD; QuestTracker moves top-right with no background, separate category/objective icons and generated pixel-art header separator. FullMap (M) retained. Implementation/prompt/acceptance: [QuestTracker refresh](QuestTrackerTopRightRefresh.md).

### Water regression correction — 2026-10-22

The initial split of water into Ground0 was incorrect: it forced all water below all grass,
regardless of the existing per-renderer orders. Owner screenshot exposed hidden lake/river regions.
All four water renderers now share Ground1 with the other migrated terrain renderers; original
orders -6/-5/-4/-1 retained. This restores the original terrain/water interleaving while keeping
all ground below Default negative-order VFX. Trees/buildings/actors untouched.
Full-map Editor render verified: Captures/Skills/mapnhat_water_restored_editor.png.
No Play Mode transition or runtime cast acceptance claimed.
