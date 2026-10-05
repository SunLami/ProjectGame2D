"""Player elemental skill kits (Water / Earth / Wind S1-S4) + generic skill ids.
v3 (2026-10-04): synthesised with Tools/sfx/synth.py so every sound matches its skill (user: the Freesound picks did not
fit). Only sounds that ARE the real thing stay sampled: eagle cry, wing flaps, feathers, water splashes, rain."""
from sfxtypes import C, S, Synth

ENTRIES = {
    # ---- generic
    "sfx.skill.cast_generic": S("Skill", [
        Synth("magic_zap", seed=1, note="zap"), Synth("magic_zap", seed=2, f0=600, f1=2100, note="zap 2"),
    ], vol=0.75, jitter=0.03, cooldown=0.1, desc="Fallback cast"),
    "sfx.skill.hit_flesh": S("Skill", [
        C(773737, dur=0.22, peak=-5, note="Punch 1"),
        C(841274, dur=0.3, peak=-5, note="soft impact"),
    ], vol=0.6, jitter=0.05, cooldown=0.08, voices=3, desc="Skill damage lands on an enemy"),
    "sfx.skill.screen_slam": S("Skill", [Synth("slam", seed=1, note="bass thump")],
                               vol=0.8, cooldown=0.3, desc="Heavy screen shake"),

    # ---- water
    "sfx.skill.water_s1_cast": S("Skill", [
        Synth("water_cast", seed=1, dur=0.5, note="bubbly zap"),
        Synth("water_cast", seed=2, dur=0.45, bright=1.15, note="bubbly zap 2"),
        Synth("water_cast", seed=3, dur=0.55, bright=0.9, note="bubbly zap 3"),
    ], vol=0.75, jitter=0.03, cooldown=0.08, voices=3, desc="Water bolt fired"),
    "sfx.skill.water_s1_impact": S("Skill", [
        Synth("splash", seed=4, dur=0.4, size=0.6, droplets=4, note="small splash"),
        Synth("splash", seed=7, dur=0.35, size=0.5, droplets=3, note="small splash 2"),
        Synth("splash", seed=8, dur=0.45, size=0.7, droplets=5, note="small splash 3"),
    ], vol=0.7, jitter=0.05, cooldown=0.05, voices=4, desc="Water bolt impact"),
    "sfx.skill.water_s2_cast": S("Skill", [Synth("water_swirl_cast", seed=1, dur=1.2)],
                                 vol=0.75, desc="Water ground-burst cast"),
    "sfx.skill.water_s2_splash": S("Skill", [
        Synth("splash", seed=5, dur=1.0, size=1.6, droplets=10, note="big splash"),
        Synth("splash", seed=6, dur=0.9, size=1.3, droplets=8, note="big splash 2"),
    ], vol=0.85, jitter=0.03, cooldown=0.1, voices=3, desc="Water burst radius"),
    "sfx.skill.water_s3_charge": S("Skill", [Synth("water_charge", seed=1, dur=1.2, top=1500)],
                                   vol=0.7, desc="Water beam charge"),
    "sfx.skill.water_s3_beam_loop": S("Skill", [Synth("water_beam_loop", seed=1, dur=2.8, xfade=0.5)],
                                      vol=0.65, loop=True, desc="Water beam sustained"),
    "sfx.skill.water_s3_end": S("Skill", [Synth("water_end", seed=1, dur=0.8)],
                                vol=0.65, desc="Water beam ends"),
    "sfx.skill.water_s4_charge": S("Skill", [Synth("water_charge", seed=2, dur=1.9, top=2200)],
                                   vol=0.8, desc="Tsunami charge"),
    "sfx.skill.water_s4_wave": S("Skill", [Synth("water_wave", seed=1, dur=1.8)],
                                 vol=0.8, desc="Tsunami wave rolling"),
    "sfx.skill.water_s4_crash": S("Skill", [Synth("water_crash", seed=1, dur=1.6)],
                                  vol=0.95, cooldown=0.2, desc="Tsunami crash"),
    "sfx.skill.water_s4_aftermath_rain": S("Skill", [
        C(624645, start=1.0, dur=10.0, peak=-8, xfade=1.0, note="Heavy_Rain_Loop"),
    ], vol=0.45, loop=True, desc="Aftermath rain loop"),

    # ---- earth
    "sfx.skill.earth_s1_cast": S("Skill", [
        Synth("earth_cast", seed=1, dur=0.6, note="stone whoosh"),
        Synth("earth_cast", seed=2, dur=0.55, pitch=1.15, note="stone whoosh 2"),
    ], vol=0.75, jitter=0.03, cooldown=0.08, voices=3, desc="Rock thrown"),
    "sfx.skill.earth_s1_impact": S("Skill", [
        Synth("crack", seed=1, dur=0.5, note="rock crack"),
        Synth("crack", seed=2, dur=0.55, tau=0.09, bright=2600, note="rock crack 2"),
        Synth("crack", seed=3, dur=0.45, tau=0.06, bright=4000, note="rock crack 3"),
    ], vol=0.75, jitter=0.05, cooldown=0.05, voices=4, desc="Rock impact"),
    "sfx.skill.earth_s2_rise": S("Skill", [Synth("earth_rise", seed=1, dur=1.6)],
                                 vol=0.85, desc="Rock arena rises"),
    "sfx.skill.earth_s2_spike": S("Skill", [
        Synth("crack", seed=4, dur=0.45, tau=0.05, note="spike"),
        Synth("crack", seed=5, dur=0.45, tau=0.06, bright=2800, note="spike 2"),
    ], vol=0.7, jitter=0.05, cooldown=0.06, voices=4, desc="Arena spike"),
    "sfx.skill.earth_s2_stun_loop": S("Skill", [Synth("earth_stun_loop", seed=1, dur=3.4, xfade=0.7)],
                                      vol=0.5, loop=True, desc="Arena spinning ring"),
    "sfx.skill.earth_s3_cone": S("Skill", [Synth("earth_cone", seed=1, dur=0.9)],
                                 vol=0.8, jitter=0.03, cooldown=0.1, desc="Spike cone"),
    "sfx.skill.earth_s3_cage": S("Skill", [Synth("earth_cage", seed=1, dur=1.2)],
                                 vol=0.8, jitter=0.03, desc="Spike cage"),
    "sfx.skill.earth_s4_charge": S("Skill", [Synth("earth_charge", seed=1, dur=2.0)],
                                   vol=0.75, desc="Meteor storm charge"),
    "sfx.skill.earth_s4_meteor_fall": S("Skill", [Synth("meteor_fall", seed=1, dur=1.2)],
                                        vol=0.5, jitter=0.05, cooldown=0.1, voices=4, desc="Meteor falling"),
    "sfx.skill.earth_s4_meteor_impact": S("Skill", [
        Synth("explosion", seed=1, dur=1.2, size=0.8, note="boom"),
        Synth("explosion", seed=2, dur=1.0, size=0.6, note="boom 2"),
    ], vol=0.75, jitter=0.05, cooldown=0.06, voices=6, desc="Meteor impact"),
    "sfx.skill.earth_s4_finale": S("Skill", [Synth("explosion", seed=3, dur=2.0, size=1.8, note="finale")],
                                   vol=0.95, desc="Meteor storm finale"),
    "sfx.skill.earth_s4_aftermath": S("Skill", [
        Synth("earth_aftermath", seed=1, dur=1.6), Synth("earth_aftermath", seed=2, dur=1.2),
    ], vol=0.55, desc="Debris settling"),

    # ---- wind
    "sfx.skill.wind_s1_throw": S("Skill", [
        Synth("wind_throw", seed=1, dur=0.55), Synth("wind_throw", seed=2, dur=0.5, pitch=1.15),
    ], vol=0.75, jitter=0.03, cooldown=0.1, desc="Wind blade thrown"),
    "sfx.skill.wind_s1_loop": S("Skill", [Synth("wind_loop", seed=1, dur=2.4, xfade=0.5, lo=500, hi=2600)],
                                vol=0.4, loop=True, desc="Blade in flight"),
    "sfx.skill.wind_s1_catch": S("Skill", [Synth("wind_catch", seed=1, dur=0.45)],
                                 vol=0.65, jitter=0.03, desc="Blade caught"),
    "sfx.skill.wind_s2_cast": S("Skill", [Synth("wind_swirl_cast", seed=1, dur=1.2)],
                                vol=0.75, desc="Whirlwind cast"),
    "sfx.skill.wind_s2_lift_loop": S("Skill", [Synth("wind_loop", seed=2, dur=4.0, xfade=0.8, lo=300, hi=1600, speed=1.0)],
                                     vol=0.5, loop=True, desc="Whirlwind lifting"),
    "sfx.skill.wind_s2_land": S("Skill", [Synth("wind_land", seed=1, dur=0.5)],
                                vol=0.6, jitter=0.05, cooldown=0.06, voices=3, desc="Enemy lands"),
    "sfx.skill.wind_s3_pulse": S("Skill", [
        Synth("wind_pulse", seed=1, dur=0.9), Synth("wind_pulse", seed=2, dur=0.8, power=0.8),
    ], vol=0.75, jitter=0.03, cooldown=0.1, desc="Gust fan pulse"),
    "sfx.skill.wind_s3_wall_slam": S("Skill", [Synth("wind_slam", seed=1, dur=0.6)],
                                     vol=0.7, jitter=0.05, cooldown=0.08, voices=3, desc="Enemy slams the wall"),
    "sfx.skill.wind_s4_charge": S("Skill", [Synth("wind_charge", seed=1, dur=2.0)],
                                  vol=0.75, desc="Wind roc charge"),
    "sfx.skill.wind_s4_screech": S("Skill", [
        C(381200, dur=1.8, peak=-3, note="Eagle Cry"),
        C(697363, dur=1.8, peak=-3, note="Eagle"),
    ], vol=0.85, jitter=0.03, desc="Roc appears"),
    "sfx.skill.wind_s4_flap": S("Skill", [
        C(389634, dur=0.9, peak=-5, note="Wing Flap 1"),
        C(561364, dur=1.2, peak=-5, note="Wing Flap Heavy"),
    ], vol=0.65, jitter=0.04, cooldown=0.25, voices=2, desc="Wing flap"),
    "sfx.skill.wind_s4_dive": S("Skill", [Synth("wind_dive", seed=1, dur=0.9)],
                                vol=0.85, desc="Roc dives"),
    "sfx.skill.wind_s4_shockwave": S("Skill", [Synth("wind_shockwave", seed=1, dur=1.3)],
                                     vol=0.95, cooldown=0.2, desc="Dive shockwave"),
    "sfx.skill.wind_s4_feather_rain": S("Skill", [
        Synth("feather", seed=1, dur=0.7), Synth("feather", seed=2, dur=0.6),
    ], vol=0.4, jitter=0.06, cooldown=0.08, voices=3, desc="Feather rain tick"),
}
