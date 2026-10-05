"""Combat SFX: player attacks/hurt/dash, enemy families (slime, goblin), projectiles, stun, skill aiming."""
from sfxtypes import C, Mix, S

ENTRIES = {
    # ---- player
    "sfx.combat.player_attack": S("Combat", [
        C(60024, dur=0.45, peak=-4, hp=180, note="bamboo swosh"),
        C(367182, dur=0.5, peak=-4, hp=200, note="swing"),
        C(507466, dur=0.45, peak=-4, hp=220, note="clean fast swoosh"),
        C(485279, dur=0.42, peak=-4, hp=200, note="stick swing"),
    ], vol=0.8, jitter=0.07, desc="Weapon swing (animation event ActivatePlayerAttackHitbox)"),
    "sfx.combat.hit_impact": S("Combat", [
        C(434896, dur=0.5, peak=-3, note="sword impact"),
        Mix(C(244513, dur=0.35), C(326857, dur=0.45, gain=-7), peak=-3, note="punch + clash"),
        C(547038, start=0.0, dur=0.5, peak=-3, hp=120, note="hit swing sword"),
        C(616502, dur=0.4, peak=-3, note="strike"),
    ], vol=0.9, jitter=0.06, cooldown=0.04, desc="Player attack lands on a target"),
    "sfx.combat.hit_crit": S("Combat", [
        Mix(C(434896, dur=0.55, pitch=0.85), C(336011, dur=0.6, gain=-9, lp=3500), peak=-2, note="heavy hit"),
    ], vol=1.0, jitter=0.04, desc="Critical hit (heavier layer)"),
    "sfx.combat.player_hurt": S("Combat", [
        C(547209, dur=0.35, peak=-4, note="grunt 9"),
        C(413186, dur=0.4, peak=-4, note="male hurt 9"),
        C(416839, dur=0.55, peak=-4, note="grunt death-pain"),
    ], vol=0.85, jitter=0.05, cooldown=0.25, desc="Player takes damage"),
    "sfx.combat.player_dodge": S("Combat", [
        C(521999, dur=0.2, peak=-6, note="whoosh/dash"),
        C(803773, dur=0.22, peak=-6, note="whoosh short"),
    ], vol=0.7, jitter=0.08, cooldown=0.15, desc="Player dodged a hit"),
    "sfx.combat.player_death": S("Combat", [
        C(396801, dur=1.5, peak=-3, note="male death 4"),
        C(554443, dur=1.2, peak=-3, note="male death sound"),
    ], vol=0.95, desc="Player dies"),
    "sfx.player.dash": S("Combat", [
        Mix(C(59988, dur=0.3, pitch=0.85), C(521999, dur=0.2, gain=-4), peak=-4, note="swosh + dash"),
        Mix(C(60013, dur=0.3, pitch=0.9), C(864775, dur=0.2, gain=-5), peak=-4, note="whoosh + jump/dash"),
    ], vol=0.75, jitter=0.06, cooldown=0.2, desc="Player dash start"),
    "sfx.player.exhausted": S("Combat", [
        C(344407, dur=0.7, peak=-5, note="gasp 1"),
        C(271245, dur=0.6, peak=-5, note="gasp male"),
        C(437667, dur=0.9, peak=-5, note="gasp"),
    ], vol=0.7, jitter=0.04, cooldown=1.2, desc="Out of stamina when attacking/dashing"),
    "sfx.player.level_up": S("Combat", [
        C(442943, dur=1.7, peak=-3, note="level up"),
    ], vol=0.85, desc="Player gained a level"),

    # ---- generic enemy
    "sfx.combat.enemy_hit": S("Combat", [
        C(773737, dur=0.2, peak=-4, note="punch 1"),
        C(773739, dur=0.2, peak=-4, note="punch 3"),
        C(841274, dur=0.35, peak=-4, note="soft impact"),
    ], vol=0.7, jitter=0.08, cooldown=0.05, desc="Generic enemy takes damage (fallback)"),
    "sfx.combat.enemy_death": S("Combat", [
        C(754441, dur=0.9, peak=-4, pitch=1.0, note="zombie groan 3"),
        C(751340, dur=0.6, peak=-4, note="slime death"),
    ], vol=0.8, jitter=0.06, desc="Generic enemy dies (fallback)"),

    # ---- slime
    "sfx.enemy.slime_hurt": S("Combat", [
        C(442772, dur=0.45, peak=-4, note="slime squish"),
        C(515620, dur=0.5, peak=-4, note="splat/squish 3"),
        C(536765, dur=0.6, peak=-4, note="squish"),
    ], vol=0.8, jitter=0.1, cooldown=0.06, desc="Slime hit"),
    "sfx.enemy.slime_death": S("Combat", [
        C(445118, dur=0.8, peak=-3, note="cartoon splat"),
        C(433840, dur=0.85, peak=-3, note="slime 27"),
        C(447930, dur=0.9, peak=-3, note="slug splat"),
    ], vol=0.85, jitter=0.06, desc="Slime dies"),
    "sfx.enemy.slime_attack": S("Combat", [
        C(442772, dur=0.4, peak=-5, pitch=1.2, note="squish up"),
        C(411671, dur=0.35, peak=-5, note="squish"),
    ], vol=0.65, jitter=0.1, cooldown=0.2, desc="Slime lunge/attack"),

    # ---- goblin
    "sfx.enemy.goblin_hurt": S("Combat", [
        C(736274, dur=0.6, peak=-4, note="hit goblin"),
        C(482358, dur=0.5, peak=-4, note="goblin creature"),
    ], vol=0.85, jitter=0.07, cooldown=0.1, desc="Goblin hit"),
    "sfx.enemy.goblin_death": S("Combat", [
        C(482360, dur=1.2, peak=-3, note="screeching creature"),
        C(643655, dur=0.9, peak=-3, pitch=0.9, note="goblin yell"),
    ], vol=0.9, jitter=0.05, desc="Goblin dies"),
    "sfx.enemy.goblin_attack": S("Combat", [
        C(442817, dur=0.7, peak=-4, note="goblin growl"),
        C(643655, dur=0.8, peak=-4, note="goblin yell"),
    ], vol=0.75, jitter=0.07, cooldown=0.3, desc="Goblin swings"),

    # ---- projectiles / status / aiming
    "sfx.combat.projectile_launch": S("Combat", [
        C(391660, dur=0.5, peak=-4, note="projectile"),
        C(635125, dur=0.3, peak=-5, note="launcher"),
    ], vol=0.7, jitter=0.07, cooldown=0.08, desc="Enemy projectile fired"),
    "sfx.combat.projectile_impact": S("Combat", [
        C(193429, dur=0.5, peak=-4, note="projectile hit"),
        C(570855, dur=0.5, peak=-4, note="magic projectile impact"),
    ], vol=0.75, jitter=0.07, cooldown=0.06, desc="Enemy projectile hits"),
    "sfx.combat.stun_apply": S("Combat", [
        C(264779, dur=0.4, peak=-5, note="stun zap"),
    ], vol=0.7, jitter=0.05, cooldown=0.25, desc="Target stunned"),
    "sfx.combat.skill_aim_start": S("Combat", [
        C(495453, dur=0.45, peak=-9, lp=5000, note="magic spell 05 (soft)"),
    ], vol=0.55, cooldown=0.2, desc="Begin aiming a skill"),
    "sfx.combat.skill_aim_confirm": S("Combat", [
        C(786291, dur=0.45, peak=-7, note="magic spell effect"),
    ], vol=0.7, desc="Skill aim confirmed"),
    "sfx.combat.skill_aim_cancel": S("Combat", [
        C(495453, dur=0.4, peak=-10, lp=3500, rev=True, note="reversed soft magic"),
    ], vol=0.5, cooldown=0.2, desc="Skill aim cancelled"),
}
