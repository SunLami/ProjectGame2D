"""Boss Earth (Stone Colossus) SFX + summon ritual / shrine / teleport pillar."""
from sfxtypes import C, Mix, S

ENTRIES = {
    # ---- summon ritual + shrine
    "sfx.boss.shrine_orb_place": S("Boss", [
        Mix(C(432288, dur=0.8), C(843547, dur=0.4, gain=-4, pitch=0.8), peak=-4, note="orb placed"),
    ], vol=0.8, desc="Orb offered to a shrine statue"),
    "sfx.boss.ritual_charge": S("Boss", [
        Mix(C(592573, dur=2.5), C(719865, dur=2.5, gain=-6, lp=1200), peak=-4, note="laser charge + rumble"),
    ], vol=0.8, desc="Statues charge their eyes"),
    "sfx.boss.ritual_beam_loop": S("Boss", [
        C(245154, start=0.0, dur=3.0, peak=-6, xfade=0.6, note="laser loop"),
    ], vol=0.7, loop=True, desc="4 statue lasers"),
    "sfx.boss.ritual_explosion": S("Boss", [
        Mix(C(446624, dur=2.2, pitch=0.7), C(719865, dur=2.2, gain=-2, lp=1400), peak=-2, note="ritual blast"),
    ], vol=1.0, desc="Summoning statue explodes"),
    "sfx.boss.awaken": S("Boss", [
        Mix(C(489901, dur=2.4, pitch=0.8), C(718004, dur=1.2, gain=-3, pitch=0.8), peak=-2, note="awaken roar"),
    ], vol=1.0, desc="Boss appears / roars"),
    "sfx.boss.phase_roar": S("Boss", [
        C(132874, dur=2.0, peak=-2, pitch=0.85, note="roar"),
        C(489901, dur=2.2, peak=-2, pitch=0.9, note="roar 2"),
    ], vol=1.0, desc="Phase change roar"),
    "sfx.boss.telegraph": S("Boss", [
        C(701702, dur=0.6, peak=-8, lp=4000, note="soft warning"),
    ], vol=0.4, cooldown=0.15, voices=3, desc="Danger zone appears"),
    "sfx.boss.slam": S("Boss", [
        Mix(C(718004, dur=1.0, pitch=0.9), C(843547, dur=0.5, gain=-3, pitch=0.7), peak=-2, note="slam"),
        Mix(C(336011, dur=1.0, pitch=0.7, lp=2500), C(385938, dur=0.7, gain=-2), peak=-2, note="slam 2"),
    ], vol=1.0, jitter=0.05, cooldown=0.12, voices=3, desc="Slam impact"),
    "sfx.boss.slam_windup": S("Boss", [
        Mix(C(389653, dur=0.9, pitch=0.8), C(517877, dur=0.9, gain=-5), peak=-4, note="grunt+swoosh"),
    ], vol=0.8, jitter=0.05, cooldown=0.2, desc="Slam wind-up"),
    "sfx.boss.rain_drop": S("Boss", [
        C(683179, dur=1.0, peak=-5, pitch=0.9, note="falling rock"),
    ], vol=0.5, jitter=0.1, cooldown=0.06, voices=4, desc="Stone rain falling"),
    "sfx.boss.rain_impact": S("Boss", [
        C(332058, dur=0.6, peak=-3, pitch=0.9, note="rock impact"),
        C(385938, dur=0.7, peak=-3, pitch=0.85, note="rock impact 2"),
        C(666673, dur=0.6, peak=-3, note="rock drop"),
    ], vol=0.75, jitter=0.1, cooldown=0.05, voices=6, desc="Stone lands"),
    "sfx.boss.spike_lane": S("Boss", [
        Mix(C(743246, dur=0.7), C(669449, dur=0.8, gain=-3), peak=-3, note="spike eruption"),
        Mix(C(700537, dur=0.7), C(719865, dur=0.8, gain=-5, lp=1500), peak=-3, note="spike eruption 2"),
    ], vol=0.85, jitter=0.07, cooldown=0.08, voices=4, desc="Spike lane erupts"),
    "sfx.boss.fist_launch": S("Boss", [
        Mix(C(517877, dur=0.8), C(743236, dur=0.6, gain=-5), peak=-3, note="fist launch"),
    ], vol=0.9, jitter=0.05, desc="Rocket fist fired"),
    "sfx.boss.fist_impact": S("Boss", [
        Mix(C(718004, dur=1.0), C(336011, dur=0.8, gain=-4, lp=3000), peak=-2, note="fist impact"),
    ], vol=1.0, jitter=0.05, desc="Rocket fist hits"),
    "sfx.boss.resonance_charge": S("Boss", [
        Mix(C(588242, dur=2.5), C(719865, dur=2.5, gain=-5, lp=1200), peak=-4, note="resonance charge"),
    ], vol=0.85, desc="Core resonance charging"),
    "sfx.boss.resonance_burst": S("Boss", [
        Mix(C(446624, dur=1.2, pitch=0.8), C(843547, dur=0.6, gain=-3), peak=-3, note="burst"),
    ], vol=0.9, jitter=0.05, cooldown=0.1, voices=3, desc="Core resonance burst"),
    "sfx.boss.rock_bullet": S("Boss", [
        C(391660, dur=0.4, peak=-6, pitch=0.8, note="bolt"),
        C(635125, dur=0.3, peak=-6, pitch=0.8, note="bolt 2"),
    ], vol=0.5, jitter=0.1, cooldown=0.05, voices=4, desc="Rock bullet fired"),
    "sfx.boss.recovery": S("Boss", [
        Mix(C(743257, dur=1.5, pitch=0.8), C(389653, dur=1.2, gain=-5, pitch=0.7), peak=-4, note="grind + groan"),
    ], vol=0.8, desc="Boss slumps and recovers"),
    "sfx.boss.hit": S("Boss", [
        C(700537, dur=0.4, peak=-4, pitch=0.9, note="rock hit"),
        C(591152, dur=0.4, peak=-4, pitch=0.9, note="rock hit 2"),
    ], vol=0.6, jitter=0.1, cooldown=0.06, voices=3, desc="Boss takes damage"),
    "sfx.boss.death": S("Boss", [
        Mix(C(489901, dur=2.5, pitch=0.7), C(590096, dur=3.0, gain=-2), C(719865, dur=3.0, gain=-3, lp=1200), peak=-2, note="collapse"),
    ], vol=1.0, desc="Boss collapses"),
    "sfx.boss.victory": S("Boss", [
        C(538145, dur=2.5, peak=-4, note="complete jingle"),
    ], vol=0.8, desc="Boss defeated stinger"),
    "sfx.boss.teleport": S("Boss", [
        C(441225, dur=1.6, peak=-4, note="teleport"),
        C(241970, dur=1.6, peak=-4, note="teleport 2"),
    ], vol=0.85, desc="Teleport pillar used"),
    "sfx.boss.arena_lock": S("Boss", [
        Mix(C(743236, dur=1.2, pitch=0.8), C(718004, dur=0.8, gain=-3), peak=-3, note="gate seal"),
    ], vol=0.85, desc="Arena sealed"),
}
