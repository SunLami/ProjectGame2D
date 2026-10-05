"""Boss Wind (Sky Owl) recipes (D-111). Imported by synth.py so they join RECIPES. Everything is synthesised from noise / tones."""
import numpy as np

from synth import (SR, at, band_noise, clicks, crack, curve, expdecay, feather, lowpass_noise, mix, pings, reverb, rumble, swell, thump, tone,
                        tt, whoosh, wind_catch, wind_charge, wind_dive, wind_land, wind_loop, wind_pulse, wind_throw)


def wing_flap(seed, dur=0.5, power=1.0):
    """One heavy wing beat: a low air thump + a short feathery whoosh."""
    thud = thump(dur, 110 * power, 45, 0.09)
    air = whoosh(seed, dur, 500, 2400, 900, 1.2, 0.3, 1.0) * expdecay(dur, 0.18, 0.01)
    flutter = band_noise(dur, 5200, 0.9, seed + 1, amp=expdecay(dur, 0.06, 0.004)) * 0.5
    return mix((thud, 0.9 * power), (air, 0.9), (flutter, 0.4))


def wing_flaps(seed, dur=1.0, beats=4):
    """A short flurry of wing beats (take-off, hovering, cyclone cast)."""
    out = np.zeros(int(dur * SR))
    step = dur / (beats + 0.6)
    for i in range(beats):
        b = wing_flap(seed + i, step * 1.1, 0.8 + 0.2 * i / max(1, beats - 1))
        out = out + at(b, i * step * (1 - 0.1 * i / beats), dur)
    return out


def owl_screech(seed, dur=1.5):
    """Raptor screech, synthesised: a harsh falling tone with a noisy edge and a rasp (no real recording)."""
    t = tt(dur)
    f = curve([(0, 1900), (dur * 0.12, 3100), (dur * 0.5, 2300), (dur, 1100)], dur)
    harsh = tone(dur, f * (1 + 0.012 * np.sin(2 * np.pi * 38 * t)), (1.0, 0.6, 0.45, 0.3), amp=swell(dur, 0.06, dur * 0.55))
    rasp = band_noise(dur, lambda tm: np.interp(tm, [0, dur], [3800, 1800]), 0.8, seed,
                      amp=swell(dur, 0.08, dur * 0.5) * (0.5 + 0.5 * np.sin(2 * np.pi * 34 * t)))
    low = lowpass_noise(dur, 500, seed + 1, amp=swell(dur, 0.1, dur * 0.5)) * 0.5
    return reverb(mix((harsh, 0.7), (rasp, 0.7), (low, 0.4)), 0.35, 0.18)


def owl_hoot(seed, dur=1.2):
    """Deep two-note hoot for the awakening."""
    n1 = tone(0.5, curve([(0, 300), (0.5, 260)], 0.5), (1.0, 0.5, 0.2), amp=swell(0.5, 0.06, 0.35))
    n2 = tone(0.6, curve([(0, 250), (0.6, 190)], 0.6), (1.0, 0.5, 0.2), amp=swell(0.6, 0.05, 0.45))
    return reverb(mix((at(n1, 0.0, dur), 0.9), (at(n2, 0.5, dur), 1.0)), 0.5, 0.25)


def owl_hurt(seed, dur=0.4):
    return mix((tone(dur, curve([(0, 2800), (dur, 1400)], dur), (1.0, 0.6, 0.3), amp=expdecay(dur, 0.1, 0.004)), 0.7),
               (band_noise(dur, 3500, 0.9, seed, amp=expdecay(dur, 0.07, 0.003)), 0.6),
               (feather(seed + 1, dur), 0.5))


def _fit(a, n):
    a = np.asarray(a)[:n]
    return np.pad(a, (0, n - len(a)))


def owl_death(seed, dur=2.4):
    n = int(dur * SR)
    cry = _fit(owl_screech(seed, 1.4), n)
    fall = _fit(at(wind_land(seed + 1, 0.8), 1.2, dur), n)
    flutter = _fit(at(wing_flaps(seed + 2, 1.2, 5), 0.2, dur), n) * expdecay(dur, 0.8)[:n] if len(expdecay(dur, 0.8)) >= n else _fit(at(wing_flaps(seed + 2, 1.2, 5), 0.2, dur), n)
    return mix((cry, 0.9), (fall, 0.8), (flutter, 0.5))


def feather_volley(seed, dur=0.9):
    """Wings fling forward and a spray of feathers whistles out."""
    return mix((wing_flap(seed, dur, 1.3), 1.0), (wind_throw(seed + 1, dur, 1.2), 0.6), (feather(seed + 2, dur), 0.7))


def cyclone_spin(seed, dur=2.4):
    return mix((wind_loop(seed, dur, 250, 1800, 1.4), 1.0), (clicks(dur, 18, seed + 1, decay=0.02, lp=5000), 0.15))


def gale_wall(seed, dur=1.8):
    return mix((band_noise(dur, lambda t: 300 + 1800 * np.sin(np.pi * t / dur), 1.1, seed, amp=swell(dur, dur * 0.4, dur * 0.6)), 1.0),
               (rumble(seed + 1, dur, 50, 260, 0.3, 14) * swell(dur, dur * 0.4, dur * 0.6), 0.6))


def blade_cut(seed, dur=0.6):
    """Wind blade: a thin, fast, high whoosh with a metallic ring."""
    cut = whoosh(seed, dur, 2000, 7000, 3500, 0.7, 0.3, 1.0)
    ring = tone(dur, curve([(0, 3400), (dur, 2400)], dur), (1.0, 0.3), amp=expdecay(dur, 0.18, 0.004)) * 0.5
    return mix((cut, 0.9), (ring, 0.35), (pings(dur, 5, 5000, 9000, seed + 1, 0.04), 0.3))


def storm_charge(seed, dur=2.4):
    return mix((wind_charge(seed, dur), 1.0), (wing_flaps(seed + 1, dur, 7), 0.5),
               (rumble(seed + 2, dur, 40, 200, 0.4, 12) * swell(dur, dur * 0.9, 0.1), 0.5))


def floor_break(seed, dur=1.0):
    return mix((crack(seed, dur, 0.1, 2400), 1.0), (rumble(seed + 1, dur, 30, 160, 0.7, 18) * expdecay(dur, 0.45), 0.9),
               (wind_pulse(seed + 2, dur, 1.0), 0.5))


def floor_warn(seed, dur=0.9):
    """Flickering floor: a rapid tick-tick-tick of high pings that gets denser."""
    return mix((pings(dur, 14, 2500, 6000, seed, 0.06) * np.linspace(0.4, 1, int(dur * SR)), 1.0),
               (clicks(dur, 22, seed + 1, decay=0.01, lp=3500) * np.linspace(0.3, 1, int(dur * SR)), 0.4))
