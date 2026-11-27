# SFX Pipeline & Full-Game SFX (D-103)

Trạng thái: **IMPLEMENTED — chờ user test lại (2026-10-04)**. **2026-11-27 (D-122): toàn bộ SFX đổi nguồn sang Pixabay** + 27 id mới cho chỗ đang im (xem §1b, §3) — chờ owner nghe trong game. Mở rộng `AudioSfxSystem.md` (D-058) và `CombatSkillSfxPlan.md` (D-082).
Phạm vi đã làm: SFX Combat, bộ 3 skill nguyên tố Player (Thủy/Địa/Phong), Boss Earth + ritual/shrine/teleport, UI bổ sung,
Inventory/Quest/Commerce, World interaction, Fishing, Farming, footstep fallback, Ambience theo scene.
**Boss Water (Cua) đã có SFX** (D-104, `sfx.bossw.*`, bank "Boss Water (Crab)"; sự kiện chung của boss chọn tiếng theo `bossId`). **Boss Wind (Cú) đã có SFX** (D-111, `sfx.bossowl.*`, 24 id, bank "Boss Wind (Owl)", `synth_owl.py`: vỗ cánh, tiếng hót/kêu, lông vũ, lốc loop, tường gió, lưỡi gió, bão, sàn vỡ; tổng hợp bằng code, chưa nghe thử).

## 1. Quy trình sản xuất (Tools/sfx, Python)

| File | Vai trò |
|---|---|
| `fs.py` / `scout.py` | Tìm Freesound công khai (không cần đăng nhập): `scout.py "nhãn|từ khóa|maxGiây[|by]"`; ưu tiên CC0, cho phép CC BY (ghi credit), **không bao giờ** NC/Sampling+/không rõ |
| `sfxtypes.py` | `C(freesound_id, start, dur, peak, gain, lp, hp, pitch, fade, tempo, rev, xfade, at)`, `Mix(...)` (nhiều lớp), `S(category, clips, vol, jitter, cooldown, voices, loop, desc, rms)` |
| `catalog_combat.py`, `catalog_skills.py`, `catalog_boss.py`, `catalog_world.py` | Công thức từng id (nguồn + cắt + EQ + mix) |
| `build_sfx.py` | Tải preview, xử lý → WAV mono 44.1 kHz 16-bit (loop → OGG crossfade), **cân bằng độ lớn theo RMS từng category**, QA (clip/lead/ngắn), ghi `Tools/sfx/out/report.json`, `Assets/Editor/Sfx/sfx_catalog.json`, phần sinh tự động của `CREDITS.md` |
| `inspect_sfx.py` | Bảng số đo + contact sheet (waveform/spectrogram) để rà không cần nghe |
| `gen_ids.py` | Sinh `Assets/Scripts/Audio/SfxIds.cs` từ catalog |

Chạy lại: `python Tools/sfx/build_sfx.py [pattern…] [--force]` → `python Tools/sfx/gen_ids.py` → Unity `Tools/SFX/Apply Catalog (import settings + add missing bank rows)`.

## 1b. Nguồn Pixabay (D-122, 2026-11-27)

- Catalog duy nhất: `Tools/sfx/catalog_pixabay.py` — mỗi clip là `P(pixabay_id, cdn_path, tên, dur=, start=, pitch=, align=, …)`;
  `cdn_path` lấy từ JSON kết quả tìm kiếm Pixabay (trường `src`), không đoán. `align=s` = bắt đầu trước đỉnh âm `s` giây
  (cho impact/whoosh có đoạn lặng đầu). Cài đặt mixer theo id (category, volume, jitter, cooldown, voices, loop) ở `px_settings.py`.
- `px.py` tải file từ `cdn.pixabay.com` (pixabay.com chặn request bằng script → tìm kiếm làm trong browser: trang
  `pixabay.com/sound-effects/search/<từ khóa>/` đọc `window.__BOOTSTRAP_URL__` → JSON `page.results[]` có `id, name,
  duration, downloadCount, hasYoutubeContentId, sources.src`). Bỏ kết quả có `hasYoutubeContentId`.
- Giấy phép: Pixabay Content License — dùng trong game, không bắt buộc ghi credit (vẫn ghi nguồn trong `CREDITS.md`);
  không phân phối lại file gốc như một bộ sound.
- Build trên macOS: `brew install ffmpeg vorbis-tools`, venv có `numpy pillow`; `SFX_WORK=<thư mục tạm> python
  Tools/sfx/build_sfx.py [id…] [--force]` → `python Tools/sfx/prune_stale.py` → `python Tools/sfx/gen_ids.py` → Unity
  `Tools/SFX/Apply Catalog` (id mới) hoặc `Reset ALL bank rows` (khi số clip của id đổi). Build in ra `PEAK CUT` khi cửa sổ
  cắt bỏ mất đỉnh âm của nguồn.
- 7 id UI cũ (`sfx.ui.hover/click_primary/click_secondary/toggle/error/popup_open/popup_close`) được build đè đúng file
  cũ (giữ GUID) và **không** vào bank: chúng vẫn là group của `SoundFXLibrary` trong Bootstrap (AudioSource chung).
- Đổi một sound: tìm lại trên Pixabay, thay dòng `P(...)`, `build_sfx.py <id> --force`, Reset bank rows hoặc kéo clip trong Inspector.
- **Cân bằng âm lượng (2026-11-27):** build chuẩn hóa từng file theo **độ to cảm nhận** (`Tools/sfx/loudness.py`: K-weighting
  BS.1770, trung bình các cửa sổ 100 ms trong 20 dB của cửa sổ to nhất) về mức của nhóm trong `Tools/sfx/px_loudness.py`
  (thang: bước chân/blip/hover −30…−31, click UI/ambience −27…−29, menu/túi/vung vũ khí −25…−26, đòn trúng/tiếng quái/cast
  −23…−24, chí mạng/bị đánh/impact skill/đòn boss −21…−22, khoảnh khắc lớn: boss gầm/slam/chết, ultimate, lên cấp, xong
  quest −19…−20 LUFS). Âm càng hay lặp lại càng nhỏ. Transient ngắn qua soft-limiter (knee 6 dB) trước trần −1 dBFS.
  Report ghi `lufs` từng file. Volume dòng SfxBank đặt **1.0** cho mọi id (độ to nằm trong file) — chỉ dùng để tinh chỉnh tai;
  ambience nền vẫn trộn bằng `bedLevel` của `SfxSceneProfile`.
- **Đo trong game (2026-11-27):** ghi đầu ra thật của AudioListener (MapDuy, thế giới đóng băng, mỗi id phát qua
  `SoundFXManager`) rồi đo LUFS từng tiếng: 210/210 phát được, không tiếng nào clip, mọi id lệch ≤ 2 dB so với nhóm sau khi
  trừ mức Settings chung (−3,4 dB). Ba id bright/thưa thấp hơn ~2 dB được bù trong file (`IN_GAME_TRIM` ở `px_loudness.py`).
  Tiếng chết của quái/thú không có cooldown nên 6 con chết cùng frame to hơn 1 con +6 dB → mọi tiếng đám đông có
  `maxVoices` 2–3 và tiếng chết có cooldown 60 ms (`px_settings.py`, `CROWD_VOICES`); đo lại: 6 con = 1 con. Burst nặng nhất
  (finale + screen slam + meteor) đỉnh −1,4 dBFS, không clip. Bộ ghi tạm (`SfxMixProbe`) đã gỡ sau khi đo.

## 2. Runtime (Assets/Scripts)

- `GameManagers/SoundFXManager.cs`: pool 12 voice, `PlaySfx(id, vol[, jitter])`, `PlaySfxAt(id, pos, vol)` (nhỏ dần theo khoảng cách + pan), `StartLoop(id, level, pitch, fadeIn)` → `SfxLoopHandle`, `HasSfx`, `FallbackFootstepId`, `RecentRequests` (phục vụ test). Id không có clip: cảnh báo một lần, không throw. Âm lượng chung vẫn là `SettingsService.SfxVolume`.
- `SoundFXLibrary`: mỗi group có `volume, pitchJitter, minInterval, maxVoices, loop` (mặc định 0 = hành vi cũ nên 7 id UI cũ không đổi).
- `Audio/SfxThrottle.cs`, `Audio/SfxLoopHandle.cs`, `Audio/SfxIds.cs` (sinh tự động), `Audio/EnemySfx.cs` (suy ra họ Slime/Goblin/Ent từ tên GameObject), `Audio/MobVoiceTimer.cs` (nhịp alert/idle của quái/thú), `Audio/SceneAmbience.cs` (bed ambience + footstep fallback theo scene).
- Editor: `Assets/Editor/Sfx/SfxCatalogApplier.cs` (import mono/Decompress On Load, loop = Vorbis; đồng bộ `SfxBank`; xem mục 2b), `SceneAmbienceInstaller.cs` (menu `Tools/SFX/Install Scene Ambience`).


## 2b. Chỉnh SFX trong Unity (ScriptableObject, không cần Python)

Mọi cài đặt là ScriptableObject, mở lên là sửa; không cần chạy script nào trong công việc hằng ngày.

- **`SfxBank`** (`Assets/Resources/Audio/SfxBanks/`, **12 asset** gộp theo nhóm: Skill Thủy / Địa / Phong / Common, Combat Player, Enemy, Boss Earth, UI and Inventory, Quest and Shop, World, Fishing and Farming, Ambience; tổng 130 sound). Mở asset thấy **một bảng**: mỗi dòng = tên dễ đọc (ví dụ "Thủy · Skill 1 · Cast"), nút ▶ nghe thử, thanh volume, các clip bên dưới (kéo thả thêm/thay, ✕ xóa, "Import file…" nhập wav/ogg/mp3 từ máy). Ô tìm kiếm lọc dòng; nút "Show advanced" mới hiện id, loại, mô tả, pitch jitter, min interval, max voices, loop. Mọi `SfxBank` trong thư mục đó được `SoundFXLibrary` nạp tự động (đè lên group cũ trong scene).
- **`SfxSceneProfile`** (`Assets/Resources/Audio/SfxScenes/<Scene>.asset`, 6 asset): tiếng nền chính/phụ, mức, fade, bước chân dự phòng — mỗi ô là **dropdown** liệt kê sound theo bank (`[SfxId]`) — cùng volume/Mute theo loại SFX và override từng sound. `SceneAmbience` của scene đọc profile theo tên scene khi nạp.
- **Tools > SFX > SFX Manager**: cửa sổ mỏng, trái là danh sách bank (hoặc scene), phải nhúng đúng inspector của asset đó.
- Menu còn lại (chỉ dùng khi chạy Python hoặc thêm scene): `Apply Catalog (import settings + add missing bank rows)` chỉ **thêm dòng còn thiếu**, không đụng dòng đã có (chỉnh tay an toàn); `Reset ALL bank rows from catalog (overwrites edits)` đặt lại từ catalog; `Install Scene Ambience` thêm object `SceneAmbience` + profile mặc định cho scene.
- Test: `SfxDatabaseTests` (nhân/mute của profile, bank đủ clip + nhãn cho mọi `SfxIds`, profile tham chiếu sound tồn tại), Play Mode: mute Combat trong profile thì `hit_impact` không phát.

## 3. Điểm gọi (hook points)

| Nhóm | Điểm gọi |
|---|---|
| Player | `Player.TakeDamage` (hurt/dodge), `Player.Die`, `PlayerCombat` (attack ở `ActivatePlayerAttackHitbox`, hit/crit ở `DamageTargetFromHitbox`, exhausted), `PlayerDash`, `PlayerStat` level up, `PlayerSkillCast` aim start/confirm/cancel |
| Enemy | `EnemyUniversal` + `Enemy.cs` (hurt/death/attack theo họ), `FireProjectile`, `ApplyStun`, `UniversalEnemyProjectile` impact |
| Skill Thủy | `SkillTestHarness` (cast S1/S2/S3), `SkillProjectile` impact, `SkillGroundImpact`, `SkillBeam` (loop + end), `SkillTsunamiUltimate` (charge/wave/crash/rain loop) |
| Skill Địa | harness (S1 cast, impact id qua `SetImpactSfx`), `SkillRockArena` (rise/spike/stun loop), `SkillSpikeCone`, `SkillSpikeCage`, `SkillMeteorStorm` (charge/fall/impact/finale/aftermath) |
| Skill Phong | `SkillBoomerang` (throw/loop/catch), `SkillWhirlwind` (cast/loop/land), `SkillGustFan` (pulse/wall slam), `SkillWindRoc` (charge/screech/flap/dive/shockwave/feather rain) |
| Boss Earth | `BossController` (awaken/phase/hit/recovery/death/victory), `BossSkills` (slam, stone rain, spike lanes, rocket fist, core resonance, telegraph), `BossArenaController` (arena lock), `SummonRitualFX` (charge/beam loop/explosion — dùng chung Earth & Water), `BossShrineUI` (orb), `BossTeleportSelectUI` (teleport) |
| UI | `PauseMenuUI` save success, `DialogueUI` blip, `InventoryActionFeedbackUI` notification |
| Inventory/Quest/Commerce | `EquipmentManager` equip/unequip, `QuestManager` accept/objective/complete, `ShopManager` buy/sell, `CraftingManager` craft |
| World | `ChestInteractable`, `UniquePickupInteractable`, `ResourceNodeInteractable` (wood/stone/plant/depleted/pickup), `SceneTravel` gate |
| Fishing/Farming | `FishingMinigameController` (cast/splash/bite/reel/catch/fail), `FarmPlot` (plant/harvest) |
| D-122 (mới) | `PlayerCombat` (tay không `combat.unarmed_*` vs kiếm `player_attack/hit_impact/hit_crit` theo ô Weapon; hình nộm `combat.dummy_hit`), `PlayerShieldBlock`/`Player.TakeDamage` (`shield_raise/block`), `EnemySfx` (Ent theo tên `Ent*`), `WildlifeSfx` (`wildlife.deer/boar/rabbit_*`), `ConsumableUseHandler` (`inventory.eat/drink/use_potion`; potion đang hồi chiêu chỉ dùng tiếng toast có sẵn), `SkillPanelUI` (`ui.skill_learn/assign`), `SkillTestHarness` (`ui.skill_cooldown` khi hồi chiêu/thiếu mana), `GameStateManager` (trang Inventory → `inventory.open/close`, Map → `ui.map_open/close`), `InventoryDiscardConfirmUI` (`inventory.drop`), `ActorLootDrop` (`world.loot_drop`), `TideController` (`bossw.tide_loop` theo mực nước + `bossw.tide_rise` một lần mỗi chu kỳ triều) |
| D-122 (bổ sung 2) | `EnemyUniversal` (`enemy.<slime|goblin|ent>_alert` khi chuyển Idle/Patrol/ReturnHome → Chase; `_idle` hiếm khi Idle/Patrol), `WildlifeActor` (`wildlife.<boar|deer|rabbit>_alert` khi người chơi vào tầm phản ứng; `boar/deer_idle` khi lang thang), nhịp do `Audio/MobVoiceTimer.cs` (alert tối đa 1 lần/4 s mỗi con; idle 8–16 s, chỉ trong 8 ô quanh người chơi) |
| Ambience | `SceneAmbience` trong MainMenu, DemoScene, MapNhat, MapDuy, BossArena_Earth (hang động + gió, step đá), BossArena_Water (bãi biển + gió, step cát) |

Id đã có clip nhưng **chưa gắn điểm gọi** (để dành): `sfx.ui.tab`, `sfx.craft.start`, `sfx.farm.water` (Farming chưa có tưới), `sfx.skill.cast_generic`, `sfx.skill.hit_flesh`, `sfx.amb.night` ngoài MainMenu. (`inventory.open/close/drop/use_potion` đã gắn từ D-122.)

## 4. Kiểm chứng đã chạy (2026-10-04)

- EditMode: 114/114 (gồm `SfxDatabaseTests` 3, `SfxThrottleTests` 6, `SfxCatalogTests` 3: mọi `SfxIds`/literal có trong catalog, mọi clip mono và tồn tại).
- PlayMode `SoundFXManagerPlayModeTests` 5/5 (chạy sau thay đổi hạ tầng). Cả bộ PlayMode (182 test) **chưa chạy trọn**: runner MCP không attach được vào lần chạy chậm khởi động (MainMenu video) — nên chạy lại bằng Test Runner trong Editor trước khi merge.
- Play Mode thật (DemoScene + BossArena_Earth) qua `RecentRequests`: player hurt; cast + impact cả 12 skill Thủy/Địa/Phong (ultimate đủ charge → finale → aftermath); Boss Earth đủ ritual → awaken → 5 skill → hit/death/victory; footstep fallback đá; ambience loop đang phát; `SetVolume` thay đổi âm lượng pool và loop.
- QA số liệu từng file (đỉnh ≤ −1.5 dB, RMS cân theo category, không clip). **Chưa nghe bằng tai** — cần user nghe và báo id nào đổi clip.

## 5. Điều cần user kiểm tra bằng tai

Độ lớn tương đối (đặc biệt ambience, skill ultimate, boss slam), chất lượng clip chọn mù theo metadata/phổ (người nghe báo id → thay `fs_id` trong catalog, build lại 1 id, Apply Catalog).
