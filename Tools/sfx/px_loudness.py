"""Target perceived loudness per SFX (active LUFS, see loudness.py), D-122 volume pass 2026-11-27.

Principle: the more often a sound plays, the quieter it is; rare "event" sounds (boss roar, ultimate, quest complete)
sit on top. Every file is normalised to its tier at build time, so the SfxBank row volume starts at 1.0 for all ids
and stays free for fine-tuning by ear. First matching pattern wins (fnmatch on the id).

Reference ladder (dB LUFS, 0 dB = full scale):
  -30  footsteps, dialogue blip, hover               (constant / dozens per minute)
  -27  UI clicks, reel ticks, ambience beds (loops)
  -25  menus, inventory, bag, swings, dash, pickups, harvest hits
  -23  normal hits, enemy/animal voices, skill casts, fishing, farming
  -21  crits, player hurt, skill impacts, boss regular attacks, notifications
  -19  big moments: boss roar/slam/death, ultimates, level up, quest complete, victory
"""
import fnmatch

TIERS = [
    # constant / very frequent
    ("sfx.step.*", -30), ("sfx.ui.dialogue_blip", -30), ("sfx.ui.hover", -31),
    ("sfx.fishing.reel", -28),
    # ambience & long loops (bed under everything)
    ("sfx.amb.*", -29), ("sfx.bossw.tide_loop", -29),
    ("sfx.skill.water_s4_aftermath_rain", -28),
    ("sfx.*loop", -26),
    # UI
    ("sfx.ui.click_*", -27), ("sfx.ui.toggle", -27), ("sfx.ui.tab", -27),
    ("sfx.ui.popup_*", -26), ("sfx.ui.map_*", -25), ("sfx.ui.error", -25), ("sfx.ui.skill_cooldown", -26),
    ("sfx.ui.skill_assign", -25), ("sfx.ui.skill_learn", -22), ("sfx.ui.notification", -24), ("sfx.ui.save_success", -23),
    ("sfx.inventory.*", -25),
    ("sfx.quest.objective", -23), ("sfx.quest.accept", -23), ("sfx.quest.complete", -20),
    ("sfx.shop.*", -23), ("sfx.craft.start", -24), ("sfx.craft.complete", -22),
    ("sfx.player.level_up", -20),
    # player combat
    ("sfx.combat.player_attack", -25), ("sfx.combat.unarmed_swing", -26), ("sfx.player.dash", -25),
    ("sfx.combat.player_dodge", -25), ("sfx.combat.shield_raise", -26),
    ("sfx.combat.hit_impact", -23), ("sfx.combat.unarmed_hit", -23), ("sfx.combat.dummy_hit", -24),
    ("sfx.combat.hit_crit", -21), ("sfx.combat.unarmed_crit", -21), ("sfx.combat.shield_block", -21),
    ("sfx.combat.player_hurt", -22), ("sfx.combat.player_death", -20), ("sfx.player.exhausted", -25),
    ("sfx.combat.skill_aim_*", -26), ("sfx.combat.stun_apply", -23),
    ("sfx.combat.projectile_launch", -25), ("sfx.combat.projectile_impact", -23),
    # enemies and wildlife (heard many at a time)
    ("sfx.enemy.*_idle", -28), ("sfx.wildlife.*_idle", -28), ("sfx.wildlife.rabbit_alert", -27),
    ("sfx.enemy.*_alert", -24), ("sfx.wildlife.*_alert", -24),
    ("sfx.combat.enemy_hit", -24), ("sfx.combat.enemy_death", -23),
    ("sfx.enemy.*_attack", -25), ("sfx.enemy.*_hurt", -24), ("sfx.enemy.*_death", -23),
    ("sfx.wildlife.*_attack", -25), ("sfx.wildlife.*_hurt", -24), ("sfx.wildlife.*_death", -23),
    # player skills
    ("sfx.skill.*_s4_finale", -19), ("sfx.skill.*_s4_crash", -19), ("sfx.skill.screen_slam", -19),
    ("sfx.skill.*_s4_meteor_impact", -21), ("sfx.skill.*_s4_shockwave", -20), ("sfx.skill.*_s4_screech", -21),
    ("sfx.skill.*_s4_*", -22),
    ("sfx.skill.*impact*", -22), ("sfx.skill.*splash*", -22), ("sfx.skill.*spike*", -22), ("sfx.skill.*cage", -21),
    ("sfx.skill.*cone", -21), ("sfx.skill.*slam*", -22), ("sfx.skill.hit_flesh", -23),
    ("sfx.skill.*", -23),
    # bosses
    ("sfx.boss*.awaken", -19), ("sfx.boss*.phase_roar", -19), ("sfx.boss*.death", -19), ("sfx.boss.victory", -19),
    ("sfx.boss.slam", -19), ("sfx.boss.ritual_explosion", -19), ("sfx.boss.resonance_burst", -20),
    ("sfx.bossowl.dive_hit", -20), ("sfx.bossowl.floor_break", -21), ("sfx.bossw.wave_hit", -20),
    ("sfx.bossw.wave_rush", -21), ("sfx.bossw.emerge", -20),
    ("sfx.boss*.hit", -24), ("sfx.boss*.recovery", -24), ("sfx.boss.telegraph", -24), ("sfx.boss*.*warn*", -22),
    ("sfx.boss*.*windup", -23), ("sfx.boss*.*charge", -23), ("sfx.bossw.bubble_pop", -24), ("sfx.bossw.tide_rise", -25),
    ("sfx.boss.rain_drop", -25), ("sfx.boss.rain_impact", -23), ("sfx.boss.rock_bullet", -24),
    ("sfx.boss.shrine_orb_place", -22), ("sfx.boss.teleport", -22), ("sfx.boss.arena_lock", -21),
    ("sfx.boss*", -21),
    # world / fishing / farming
    ("sfx.world.harvest_*", -25), ("sfx.world.loot_drop", -25), ("sfx.world.resource_depleted", -22),
    ("sfx.world.chest_open", -22), ("sfx.world.unique_pickup", -21), ("sfx.world.gate_exit", -23),
    ("sfx.fishing.catch", -22), ("sfx.fishing.*", -24), ("sfx.farm.*", -25),
]


# Measured in game (SfxMixProbe, 2026-11-27): these sparse/bright clips play ~2 dB under their tier through the real
# mixer, so their files are built 2 dB hotter. Re-measure before adding entries; ±1 dB is noise.
IN_GAME_TRIM = {"sfx.boss.shrine_orb_place": 2.0, "sfx.quest.accept": 2.0}


def target_lufs(sfx_id):
    """Tier of the id (what the player should hear)."""
    for pattern, lufs in TIERS:
        if fnmatch.fnmatchcase(sfx_id, pattern):
            return lufs
    return -24


def file_lufs(sfx_id):
    """What the file is normalised to: the tier plus the measured in-game correction."""
    return target_lufs(sfx_id) + IN_GAME_TRIM.get(sfx_id, 0.0)
