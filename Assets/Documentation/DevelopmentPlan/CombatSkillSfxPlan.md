# Kế hoạch SFX Combat Player + Bộ Skill Thủy/Địa/Phong (DemoScene-first)

Trạng thái: **PLAN — chờ user chốt** (2026-10-03). Mở rộng `AudioSfxSystem.md` (D-058) Phase C. Quyết định: D-082.

## 0. Phạm vi và nguyên tắc

- **Chỉ làm trong DemoScene trước.** DemoScene đã có `SoundFXManager` + `SoundFXLibrary` + `MusicManager` nên nạp group thẳng vào library của DemoScene.
  MapNhat / MainMenu / Bootstrap **không đụng** cho tới khi user chốt hoàn thiện, sau đó mới port (cùng danh sách ID, chỉ thêm group vào library của Bootstrap).
- SFX là **presentation-only**: không đổi logic sát thương, save, progression (đúng D-058). Mọi lệnh phát là một dòng `SoundFXManager.PlaySfx(id)` gọi từ chỗ đã có sự kiện.
- Quy ước ID `sfx.<category>.<action>`; file `Assets/Resources/Audio/SFX/<Category>/<id dấu chấm→gạch dưới>.wav`; mono, PCM 44.1kHz WAV, Decompress On Load, không loop
  (riêng "loop sound" xem §2.4), combat 0.2–0.8s, skill cast/impact ≤ 2s. Ưu tiên CC0; CC-BY phải ghi `CREDITS.md`.
- **Không tải file khi chưa được user cho phép** (Freesound cần đăng nhập). Danh sách dưới là *ứng viên* chưa nghe thử; user duyệt/tải hoặc cho phép tải từng file.
- Mỗi ID có `groupName` nhiều clip (2–4 biến thể) để `GetRandomClip` tránh lặp tai.

## 1. Hạ tầng cần mở rộng (cần user đồng ý — D-082)

`SoundFXManager` hiện chỉ có 1 `AudioSource` dùng chung → không đổi được pitch riêng, 20 thiên thạch rơi cùng lúc sẽ chồng/cắt tiếng. Đề xuất, **giữ nguyên API cũ**
(`PlaySfx(string, float)` không đổi; chỉ thêm):

| Thêm | Mục đích |
|---|---|
| Pool 8 `AudioSource` con (round-robin) | Phát chồng nhiều SFX, pitch riêng mỗi tiếng |
| `PlaySfx(id, volumeScale, pitchJitter)` (overload) | Biến thể pitch ±`pitchJitter` (mặc định 0.05 cho combat, 0.1 cho skill) |
| Cooldown + voice limit theo ID (`SoundFXLibrary` mỗi group thêm `minInterval`, `maxVoices`) | Mưa thiên thạch/lông không thành rác âm thanh; mặc định 0 = không giới hạn (không ảnh hưởng ID cũ) |
| `PlaySfxAt(id, Vector2 pos, volumeScale)` | Âm lượng suy giảm theo khoảng cách tới Player (impact xa nhỏ hơn); 2D, không cần spatial blend |
| `SfxLoopHandle StartLoop(id)` / `Stop()` | Tiếng lặp có điều khiển cho vòng xoáy/tụ lực/beam (clip loop-able, dừng khi skill kết thúc) |

Âm lượng vẫn qua `SettingsService.SfxVolume`. Mặc định mix: combat 0.8, skill cast 0.9, skill impact 0.7, ambient/loop 0.5.

## 2. PART 1 — SFX combat Player (còn thiếu)

### 2.1 Bảng ID + điểm gọi

| ID | Khi nào | Điểm gọi (code) | Ghi chú |
|---|---|---|---|
| `sfx.combat.player_attack` | Vung đòn | `ActivatePlayerAttackHitbox()` (animation event đã có; Idle@0.3, Walk@0.1, Run@0.3) | 3–4 biến thể whoosh, pitch jitter; Run attack nhỏ hơn |
| `sfx.combat.hit_impact` | Đòn trúng enemy | `Player.DamageTargetFromHitbox` (đúng lúc `TakeDamage` thành công) | Crit/ chết dùng thêm biến thể nặng nếu có |
| `sfx.combat.player_hurt` | Player nhận sát thương | `Player.TakeDamage`, nhánh outcome ≠ Ignored/Dodged/Killed | Cooldown 0.25s để không chồng khi nhiều đòn |
| `sfx.combat.player_dodge` | Né thành công | `TakeDamage` nhánh Dodged | Whoosh ngắn nhẹ |
| `sfx.combat.player_death` | Chết | `Player.Die()` | Không dùng Wilhelm-scream |
| `sfx.player.exhausted` | Hết stamina không đánh được | `TryConsumeAttackStamina` thất bại | Cooldown 1.0s |
| `sfx.player.level_up` | Lên cấp | `PlayerStat.OnLevelUp` | Ưu tiên 1 clip, không pitch jitter |
| `sfx.combat.enemy_hit` | Enemy bị đánh/ chịu sát thương | `Enemy.TakeDamage` / `EnemyUniversal` | Slime = squish, Goblin = grunt |
| `sfx.combat.enemy_death` | Enemy chết | hook chết của enemy (D-058 đã khai báo ID) | Slime/monster biến thể |
| `sfx.combat.skill_aim_start` / `skill_aim_confirm` / `skill_aim_cancel` | Ngắm skill | `BeginAimSkill` bắt đầu / xác nhận / hủy | UI-style nhẹ, dùng chung mọi skill |
| `sfx.combat.stun_apply` | Enemy bị choáng | `IStunnable.ApplyStun` | Có cooldown 0.2s |

Footstep: audit hiện có (`PlayFootSteps` + cooldown 0.2s) — chỉ kiểm tra clip có đủ nhóm, không làm lại. `Hurt*`/`Dead*` animation chưa có event nên phát từ code (không cần sửa file .anim).

### 2.2 Ứng viên Freesound (CC0, chưa nghe thử)

URL: `https://freesound.org/people/<author>/sounds/<id>/`

| ID gắn | Ứng viên (id · tác giả · tên · thời lượng) |
|---|---|
| player_attack | 60024 qubodup "swosh swinging sword 37" 0.56s · 733890 velcronator "Whoosh 03" 0.99s · 507466 Danjocross "Clean fast Swoosh" 0.92s · 367182 GaussTheWizard "swing" 0.70s · 614089 mateusboga "Woosh 6" 0.19s · 485279 Joao_Janz "Wooden Stick Swing 1_4" 0.56s |
| hit_impact | 426322 MTJohnson "Single Sword Hit" 0.77s · 442769 qubodup "Sword Hit" 0.76s · 326868 JohnBuhr "Sword_Clash (7)" 1.17s · 446016/446015/446014 SlavicMagic wpn_1/2/3 · 507131 Daleonfire "Punch2" 0.35s · 529942 grizzlymittz "Fighting Game Hit Sound" 0.40s · 255529 Opticreep "hit" 0.29s |
| player_hurt | 547209 MrFossy "PainGrunts_09" 0.32s · 413186 micahlg "male_hurt9" 0.38s · 839522 iampatrick "Video Game Character Grunt" 0.40s |
| player_death | 396801 scorpion67890 "Male Death 4" 1.37s · 554443 Blankened "Male Death Sound" 1.01s |
| exhausted | 344416 jawbutch "Male Gasp 3" 0.92s · 344415 "Male Gasp 4" 0.83s · 344407 "Male Gasp 1" 0.67s · 736458 9voltfan "Gasping" 0.58s |
| level_up | 609335 Kenneth_Cooney "LevelUp" 0.62s · 442943 qubodup "Level Up" 1.67s · 368651 Jofae "Game Powerup" 1.09s |
| enemy_hit (slime) | 442772 qubodup "Slime Squish" 0.45s · 433839 Archos "Slime 28" 0.59s · 589835 MrFossy "SQUELCH_slayer_214" 0.25s · 445109 Breviceps "Mud Splat" 0.36s |
| enemy_death | 751340 qubodup "Slime Death" 0.29s · 559621 Leadstarson "monster_sound_medium_death" 0.90s · 434129 89o "Die" 0.88s |
| stun_apply | 706380 agglow "shaking-hit-table-hard" 0.62s · 786323 Sadiquecat "Kung-Fu punch SFX" 0.45s |
| skill_aim_* | 420677 SypherZent "Spell Cast / Buff / Deep Tone" 0.86s (confirm) · 837459 Wavewire "SFX_Spell_WhisperedShort-04" 0.99s (start) |
| dodge | chưa tìm riêng — dùng chung whoosh ngắn (614089) pitch cao, hoặc tìm thêm "dash" khi thực hiện |

## 3. PART 2 — SFX bộ skill (Thủy / Địa / Phong)

Nguyên tắc: mỗi skill có 3 lớp — **cast** (lúc bấm/tụ lực), **travel/loop** (đang chạy), **impact** (trúng/nổ). Skill ultimate (S4) thêm **finale** + **aftermath**.
Điểm gọi bám đúng các script đã có (đã đọc: `SkillProjectile`, `SkillGroundImpact`, `SkillBeam`, `SkillTsunamiWave/Ultimate`, `SkillRockArena`, `SkillSpikeCone/Cage`,
`SkillMeteorStorm`, `SkillBoomerang`, `SkillWhirlwind`, `SkillGustFan`, `SkillWindRoc`); cách phát: thêm một dòng `PlaySfx` tại phase tương ứng, không đổi luồng.
Vị trí chính xác từng dòng sẽ xác nhận khi implement từng skill (mỗi skill kiểm tra Play Mode ở timeScale thấp để canh nhịp).

### 3.1 SFX dùng chung (skill)

| ID | Khi nào |
|---|---|
| `sfx.skill.cast_generic` | Mọi lần cast, nếu skill không có cast riêng (fallback) |
| `sfx.skill.hit_flesh` | Sát thương skill trúng enemy (nhẹ, cooldown 0.08s, chung mọi skill) |
| `sfx.skill.screen_slam` | Rung màn hình/hit-stop mạnh (`SkillScreenFX`) — bass thump |

### 3.2 Thủy (Water)

| Skill | ID | Thời điểm |
|---|---|---|
| S1 giọt/đạn nước (`SkillProjectile`) | `sfx.skill.water_s1_cast` · `water_s1_impact` | cast lúc bắn · impact khi trúng/hết tầm |
| S2 xoáy nước (`SkillGroundImpact`) | `water_s2_cast` · `water_s2_splash` | cast · lúc nổ bán kính |
| S3 tia nước (`SkillBeam`) | `water_s3_charge` · `water_s3_beam_loop` (StartLoop) · `water_s3_end` | tụ · suốt beam · tắt |
| S4 ultimate (`SkillTsunamiUltimate`) | `water_s4_charge` · `water_s4_wave` · `water_s4_crash` · `water_s4_aftermath_rain` | tụ lực · sóng chạy · sóng đập · mưa dư |
Ứng viên: 189499 VacekH "water splash 2" 0.18s · 737233 qubodup "Small Water Splash" 0.55s · 867498 qubodup "First Person Ball Water Drop Splash" 0.30s ·
646568 Ryanz-Official "WaterSplash" 1.27s · 867456 qubodup "Waboba Moon Ball Water Splash 1" 1.11s · 186748 rombart "Splash-eau-goudron1" 0.77s ·
791457 Sadiquecat "Vertical water sweep" 0.97s (beam/wave) · 862159/862160/862161 qubodup "Brewing a Magical Potion 2/3/4" ~1.0s (cast/bubbling) ·
S4 crash: ghép splash nặng + explosion (435416 V-ktor "explosion13" 1.06s, hạ tần số). **Thiếu:** loop beam/biển — cần tìm "water stream loop" hoặc dùng StartLoop với 791457.

### 3.3 Địa (Earth)

| Skill | ID | Thời điểm |
|---|---|---|
| S1 thạch trụ/đá ném (`SkillProjectile`) | `earth_s1_cast` · `earth_s1_impact` | bắn · trúng |
| S2 đấu trường đá (`SkillRockArena`) | `earth_s2_rise` · `earth_s2_spike` · `earth_s2_stun_loop` | sàn trồi · gai · vòng xoay stun |
| S3 hình nón gai / vòng gai (`SkillSpikeCone`/`SkillSpikeCage`) | `earth_s3_cone` · `earth_s3_cage` | gai lao theo hình nón · vòng khóa chân |
| S4 mưa thiên thạch (`SkillMeteorStorm`) | `earth_s4_charge` · `earth_s4_meteor_fall` · `earth_s4_meteor_impact` · `earth_s4_finale` · `earth_s4_aftermath` | tụ · mỗi thiên thạch rơi (limit 4 voice) · nổ (limit 6 voice, minInterval 0.06s) · cú kết · dư chấn |
Ứng viên: 843547 kevinklang "ROCK_IMPACT_GALETS" 0.51s · 385938 Pól "S020_Rock_Impact_Mono" 0.75s · 489924 falcospizaetus "MetalImpactAgainstRock07" 0.70s ·
319229 worthahep88 "Single Rock hit dirt 2" 1.51s · 504959 SieuAmThanh "Đánh Đá" 1.34s · 262118 rokenjocu "Kirkstall Rock Crumble" 1.80s (crumble/aftermath) ·
683179 NearTheAtmoshphere "Fireball" 1.63s (meteor fall) · 609588 unfa "Firecracker Explosion" 2.26s / 446624 IdkMrGarcia "explosion2" 1.72s / 336011 Rudmer_Rotteveel "Sharp Explosion 4" 1.05s (impact/finale).
**Thiếu:** "stone rise/ground rumble" (tìm không ra CC0) — thay bằng rock crumble hạ pitch + earthquake rumble sau khi nghe thử thêm.

### 3.4 Phong (Wind)

| Skill | ID | Thời điểm |
|---|---|---|
| S1 Đao Phong (`SkillBoomerang`) | `wind_s1_throw` · `wind_s1_loop` · `wind_s1_catch` | ném · đang bay (loop nhẹ) · bắt lại |
| S2 Cuồng Phong (`SkillWhirlwind`) | `wind_s2_cast` · `wind_s2_lift_loop` · `wind_s2_land` | cast · xoáy hất tung · enemy rơi |
| S3 Trận Gió (`SkillGustFan`) | `wind_s3_pulse` (×3 nhịp) · `wind_s3_wall_slam` | mỗi nhịp quạt · enemy đập tường |
| S4 Đại Bàng Gió (`SkillWindRoc`) | `wind_s4_charge` · `wind_s4_screech` · `wind_s4_flap` · `wind_s4_dive` · `wind_s4_shockwave` · `wind_s4_feather_rain` | tụ lực · đại bàng xuất hiện · vỗ cánh (loop) · lao xuống · sóng xung kích · mưa lông (limit voice) |
Ứng viên: 683096 florianreichelt "Woosh" 1.72s · 381853 sqeeeek "wind_gust_short" 2.08s · 711441 F3ather "gust of wind blowing some leaves" 2.49s ·
346237 helhel "blowing" 2.56s · 389634/389633 _stubb "Wing Flap 1/2" 0.93s/1.17s · 561364 GearArcanaNo37 "Wing Flap Heavy" 1.67s · 697363 GrayEpic "Eagle s" 2.06s (screech, nghe thử vì chỉ 1 kết quả) ·
620533 ertzsi "Pyorremyrsky" 1.72s (tornado, nghe thử) · 742907 Sadiquecat "Woosh - spatula" 0.46s (throw) · 519414 Iridiuss "Energy/magic shot" 1.97s (S4 charge) ·
420676 SypherZent "Spell Cast / Buff / High Tone" 1.69s (cast chung).

## 4. Lộ trình triển khai (từng phần, mỗi phần test Play Mode)

| Bước | Nội dung | Điều kiện qua |
|---|---|---|
| 0 | User duyệt plan + hạ tầng §1 + chọn clip/cho phép tải | Ghi vào `DecisionRegister.md` D-082 chuyển Accepted |
| 1 | Hạ tầng `SoundFXManager` (pool/pitch/cooldown/PlaySfxAt/Loop) + EditMode test nhỏ cho cooldown/voice limit | Compile sạch, ID cũ (UI) không đổi hành vi |
| 2 | Part 1 combat (§2.1) trong DemoScene: import clip, nạp group vào library DemoScene, gắn `PlaySfx` | Play Mode: nghe đủ attack/hit/hurt/death/level up/exhausted |
| 3 | Thủy S1→S4 | Mỗi skill nghe trong harness (Tab→Water, Q/E/R/T) |
| 4 | Địa S1→S4 | tương tự |
| 5 | Phong S1→S4 | tương tự |
| 6 | Cân bằng âm lượng/mix (không clipping, skill không át combat), cập nhật `CREDITS.md`, `AudioSfxSystem.md` | User chốt DemoScene |
| 7 (sau khi user chốt) | Port sang Bootstrap/MapNhat/Scene khác (chỉ thêm library group) | Ngoài phạm vi hiện tại |

## 5. Definition of Done

- Mọi ID trong bảng của phần đã làm có clip (CC0 hoặc có CREDITS), đúng import spec, nạp vào library DemoScene.
- Không đổi logic gameplay/save; `PlaySfx` thiếu clip chỉ cảnh báo một lần, không throw.
- Play Mode đã nghe từng skill/đòn đánh; không chồng tiếng gây rè; tắt SFX trong Settings thì im.
- Docs đồng bộ: `AudioSfxSystem.md` (trạng thái phase), `CREDITS.md`, `DecisionRegister.md`, README.
