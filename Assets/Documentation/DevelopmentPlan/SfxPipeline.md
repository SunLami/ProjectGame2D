# SFX Pipeline & Full-Game SFX (D-103)

Trạng thái: **IMPLEMENTED — chờ user test lại (2026-10-04)**. Mở rộng `AudioSfxSystem.md` (D-058) và `CombatSkillSfxPlan.md` (D-082).
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

## 2. Runtime (Assets/Scripts)

- `GameManagers/SoundFXManager.cs`: pool 12 voice, `PlaySfx(id, vol[, jitter])`, `PlaySfxAt(id, pos, vol)` (nhỏ dần theo khoảng cách + pan), `StartLoop(id, level, pitch, fadeIn)` → `SfxLoopHandle`, `HasSfx`, `FallbackFootstepId`, `RecentRequests` (phục vụ test). Id không có clip: cảnh báo một lần, không throw. Âm lượng chung vẫn là `SettingsService.SfxVolume`.
- `SoundFXLibrary`: mỗi group có `volume, pitchJitter, minInterval, maxVoices, loop` (mặc định 0 = hành vi cũ nên 7 id UI cũ không đổi).
- `Audio/SfxThrottle.cs`, `Audio/SfxLoopHandle.cs`, `Audio/SfxIds.cs` (sinh tự động), `Audio/EnemySfx.cs` (suy ra họ Slime/Goblin từ tên GameObject), `Audio/SceneAmbience.cs` (bed ambience + footstep fallback theo scene).
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
| Ambience | `SceneAmbience` trong MainMenu, DemoScene, MapNhat, MapDuy, BossArena_Earth (hang động + gió, step đá), BossArena_Water (bãi biển + gió, step cát) |

Id đã có clip nhưng **chưa gắn điểm gọi** (để dành): `sfx.inventory.open/close/drop/use_potion` (cửa sổ đã có `popup_open/close`), `sfx.ui.tab`, `sfx.craft.start`, `sfx.farm.water`, `sfx.skill.cast_generic`, `sfx.skill.hit_flesh`, `sfx.amb.night` ngoài MainMenu.

## 4. Kiểm chứng đã chạy (2026-10-04)

- EditMode: 114/114 (gồm `SfxDatabaseTests` 3, `SfxThrottleTests` 6, `SfxCatalogTests` 3: mọi `SfxIds`/literal có trong catalog, mọi clip mono và tồn tại).
- PlayMode `SoundFXManagerPlayModeTests` 5/5 (chạy sau thay đổi hạ tầng). Cả bộ PlayMode (182 test) **chưa chạy trọn**: runner MCP không attach được vào lần chạy chậm khởi động (MainMenu video) — nên chạy lại bằng Test Runner trong Editor trước khi merge.
- Play Mode thật (DemoScene + BossArena_Earth) qua `RecentRequests`: player hurt; cast + impact cả 12 skill Thủy/Địa/Phong (ultimate đủ charge → finale → aftermath); Boss Earth đủ ritual → awaken → 5 skill → hit/death/victory; footstep fallback đá; ambience loop đang phát; `SetVolume` thay đổi âm lượng pool và loop.
- QA số liệu từng file (đỉnh ≤ −1.5 dB, RMS cân theo category, không clip). **Chưa nghe bằng tai** — cần user nghe và báo id nào đổi clip.

## 5. Điều cần user kiểm tra bằng tai

Độ lớn tương đối (đặc biệt ambience, skill ultimate, boss slam), chất lượng clip chọn mù theo metadata/phổ (người nghe báo id → thay `fs_id` trong catalog, build lại 1 id, Apply Catalog).
