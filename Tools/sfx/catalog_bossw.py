"""Boss Water (Tidal Crab) SFX - synthesised (Tools/sfx/synth.py) so each sound matches its move."""
from sfxtypes import S, Synth

ENTRIES = {
    # ---- Claw Clamp
    "sfx.bossw.claw_windup": S("Boss", [Synth("claw_windup", seed=1, dur=0.8)],
                               vol=0.7, cooldown=0.2, desc="Claws raise before the clamp"),
    "sfx.bossw.claw_snap": S("Boss", [
        Synth("clack", seed=1, dur=0.4), Synth("clack", seed=2, dur=0.38, pitch=0.9), Synth("clack", seed=3, dur=0.42, pitch=1.1),
    ], vol=0.9, jitter=0.03, cooldown=0.1, voices=3, desc="Claw snaps shut"),
    "sfx.bossw.claw_hold": S("Boss", [Synth("bubble_pop", seed=7, dur=0.35)],
                             vol=0.7, cooldown=0.3, desc="Both claws land: player held"),

    # ---- Bubble Trap
    "sfx.bossw.bubble_spit": S("Boss", [
        Synth("bubble_blow", seed=1, dur=0.55), Synth("bubble_blow", seed=2, dur=0.5), Synth("bubble_blow", seed=3, dur=0.6),
    ], vol=0.7, jitter=0.04, cooldown=0.1, voices=3, desc="Crab spits a bubble"),
    "sfx.bossw.bubble_pop": S("Boss", [
        Synth("bubble_pop", seed=1, dur=0.3), Synth("bubble_pop", seed=2, dur=0.28), Synth("bubble_pop", seed=3, dur=0.32),
    ], vol=0.75, jitter=0.05, cooldown=0.05, voices=4, desc="Bubble bursts"),

    # ---- Sand Ambush
    "sfx.bossw.burrow_dive": S("Boss", [Synth("sand_burrow", seed=1, dur=1.1)],
                               vol=0.85, cooldown=0.3, desc="Crab sinks into the sand"),
    "sfx.bossw.mound_loop": S("Boss", [Synth("mound_loop", seed=1, dur=2.4, xfade=0.5)],
                              vol=0.6, loop=True, desc="Sand mound chasing the player"),
    "sfx.bossw.emerge": S("Boss", [Synth("emerge", seed=1, dur=0.9), Synth("emerge", seed=2, dur=0.85)],
                          vol=1.0, jitter=0.03, cooldown=0.3, desc="Crab bursts out of the sand"),

    # ---- Tidal Wave
    "sfx.bossw.wave_warning": S("Boss", [Synth("conch_horn", seed=1, dur=1.4)],
                                vol=0.8, cooldown=0.4, desc="Conch horn before the wave"),
    "sfx.bossw.wave_rush": S("Boss", [Synth("water_wave", seed=3, dur=1.7)],
                             vol=0.85, cooldown=0.4, desc="Tidal wave sweeps the arena"),
    "sfx.bossw.wave_hit": S("Boss", [Synth("water_crash", seed=3, dur=1.0)],
                            vol=0.9, cooldown=0.3, desc="Wave hits the player"),

    # ---- Whirlpool
    "sfx.bossw.whirl_charge": S("Boss", [Synth("water_charge", seed=5, dur=2.0, top=1300)],
                                vol=0.8, cooldown=0.5, desc="Whirlpool opens"),
    "sfx.bossw.whirl_loop": S("Boss", [Synth("whirl_loop", seed=1, dur=3.0, xfade=0.6)],
                              vol=0.7, loop=True, desc="Whirlpool pulling"),
    "sfx.bossw.whirl_jet": S("Boss", [Synth("tentacle", seed=1, dur=0.7), Synth("tentacle", seed=2, dur=0.65)],
                             vol=0.8, jitter=0.04, cooldown=0.1, voices=3, desc="Water jets start spinning"),

    # ---- the crab itself
    "sfx.bossw.awaken": S("Boss", [Synth("crab_roar", seed=1, dur=1.8)],
                          vol=1.0, desc="Crab emerges and roars"),
    "sfx.bossw.phase_roar": S("Boss", [Synth("crab_roar", seed=2, dur=1.6), Synth("crab_roar", seed=3, dur=1.7)],
                              vol=1.0, desc="Phase change roar"),
    "sfx.bossw.hit": S("Boss", [
        Synth("crab_hit", seed=1, dur=0.35), Synth("crab_hit", seed=2, dur=0.32), Synth("crab_hit", seed=3, dur=0.38),
    ], vol=0.65, jitter=0.05, cooldown=0.06, voices=3, desc="Crab takes damage"),
    "sfx.bossw.recovery": S("Boss", [Synth("water_end", seed=4, dur=1.0)],
                            vol=0.7, desc="Crab slumps and recovers"),
    "sfx.bossw.death": S("Boss", [Synth("crab_death", seed=1, dur=2.6)],
                         vol=1.0, desc="Crab collapses"),
}
