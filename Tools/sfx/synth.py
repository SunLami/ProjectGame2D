"""Procedural SFX synthesiser (numpy only): stylised fantasy / pixel-RPG effects built from filtered noise, tones, chirps
and impacts, so the *meaning* of a sound is under our control (a water beam is a bright watery hum, not a river).

Every recipe is a function `name(seed, **params) -> float array (mono, 44.1 kHz)`; catalogs reference them with
`Synth("name", seed=1, ...)` (see sfxtypes.Synth). Loop recipes are written so build_sfx's crossfade makes them seamless."""
import numpy as np

SR = 44100


# ---------------------------------------------------------------------------------------------------- primitives
def rng(seed):
    return np.random.default_rng(seed)


def tt(dur):
    return np.arange(int(dur * SR)) / SR


def curve(points, dur):
    """Piecewise-linear control curve over `dur` seconds: points = [(time, value), ...]."""
    ts, vs = zip(*points)
    return np.interp(tt(dur), ts, vs)


def expdecay(dur, tau, attack=0.003):
    t = tt(dur)
    env = np.exp(-t / max(tau, 1e-4))
    a = int(attack * SR)
    if a > 0:
        env[:a] *= np.linspace(0, 1, a)
    return env


def swell(dur, rise, fall):
    """Smooth attack/release envelope: rises over `rise` s, falls over `fall` s."""
    t = tt(dur)
    up = np.clip(t / max(rise, 1e-4), 0, 1) ** 2
    down = np.clip((dur - t) / max(fall, 1e-4), 0, 1) ** 2
    return up * down


def stft_filter(sig, env_fn, n=1024, hop=256):
    """Time-varying spectral shaping: env_fn(t[:,None], f[None,:]) -> gain matrix (frames x bins)."""
    win = np.hanning(n)
    pad = np.concatenate([np.zeros(n), sig, np.zeros(n)])
    frames = (len(pad) - n) // hop + 1
    out = np.zeros(len(pad))
    norm = np.zeros(len(pad))
    f = np.fft.rfftfreq(n, 1 / SR)[None, :]
    centres = np.maximum((np.arange(frames) * hop + n / 2 - n) / SR, 0.0)
    gains = env_fn(centres[:, None], f)
    for i in range(frames):
        seg = pad[i * hop: i * hop + n] * win
        spec = np.fft.rfft(seg) * gains[i]
        out[i * hop: i * hop + n] += np.fft.irfft(spec, n) * win
        norm[i * hop: i * hop + n] += win ** 2
    out = out / np.maximum(norm, 1e-6)
    return out[n: n + len(sig)]


def band_noise(dur, center, width, seed=0, amp=None, slope=0.0):
    """Noise whose band moves: `center`/`width` are callables of time (Hz) or numbers. Gaussian-in-log-frequency band."""
    noise = rng(seed).standard_normal(int(dur * SR))
    cf = center if callable(center) else (lambda t, c=center: c + 0 * t)
    wf = width if callable(width) else (lambda t, w=width: w + 0 * t)

    def env(t, f):
        c = np.maximum(cf(t), 20.0)
        w = np.maximum(wf(t), 0.05)
        return np.exp(-0.5 * (np.log2(np.maximum(f, 1.0) / c) / w) ** 2) * (np.maximum(f, 1.0) / 1000.0) ** slope

    out = stft_filter(noise, env)
    if amp is not None:
        out = out * amp
    return out


def lowpass_noise(dur, cutoff, seed=0, amp=None):
    noise = rng(seed).standard_normal(int(dur * SR))
    cf = cutoff if callable(cutoff) else (lambda t, c=cutoff: c + 0 * t)

    def env(t, f):
        return 1.0 / (1.0 + (f / np.maximum(cf(t), 20.0)) ** 4)

    out = stft_filter(noise, env)
    return out * amp if amp is not None else out


def tone(dur, freq, harmonics=(1.0,), amp=None, vib=0.0, vib_rate=6.0, phase0=0.0):
    """Additive tone; `freq` number or array (Hz). Harmonic k has gain harmonics[k-1]."""
    f = freq if isinstance(freq, np.ndarray) else np.full(int(dur * SR), float(freq))
    t = tt(dur)
    if vib:
        f = f * (1 + vib * np.sin(2 * np.pi * vib_rate * t))
    ph = 2 * np.pi * np.cumsum(f) / SR + phase0
    out = np.zeros(len(ph))
    for k, g in enumerate(harmonics, start=1):
        if g:
            out += g * np.sin(k * ph)
    return out * amp if amp is not None else out


def thump(dur, f0, f1, tau, click=0.0):
    """Impact body: sine sweeping f0 -> f1 with an exponential decay."""
    t = tt(dur)
    f = f1 + (f0 - f1) * np.exp(-t / (tau * 0.5))
    ph = 2 * np.pi * np.cumsum(f) / SR
    out = np.sin(ph) * expdecay(dur, tau, 0.001)
    if click:
        out += click * lowpass_noise(dur, 4000, seed=3, amp=expdecay(dur, 0.01, 0.0005))
    return out


def bubbles(dur, density, fmin=500, fmax=1800, seed=0, decay=0.045, rising=True):
    """Water droplets: short rising sine chirps at random times (`density` per second)."""
    r = rng(seed)
    out = np.zeros(int(dur * SR))
    for _ in range(max(1, int(dur * density))):
        start = r.uniform(0, max(dur - 0.08, 0.01))
        f = r.uniform(fmin, fmax)
        length = int(r.uniform(0.04, 0.09) * SR)
        t = np.arange(length) / SR
        sweep = f * (1 + (2.2 if rising else -0.3) * t / 0.08)
        ph = 2 * np.pi * np.cumsum(sweep) / SR
        chirp = np.sin(ph) * np.exp(-t / decay) * r.uniform(0.4, 1.0)
        i = int(start * SR)
        out[i: i + length] += chirp[: len(out) - i]
    return out


def pings(dur, density, fmin=2500, fmax=7500, seed=0, decay=0.08):
    """Magic sparkle: short decaying high sine pings."""
    r = rng(seed)
    out = np.zeros(int(dur * SR))
    for _ in range(max(1, int(dur * density))):
        start = r.uniform(0, max(dur - 0.15, 0.01))
        f = r.uniform(fmin, fmax)
        length = int(0.25 * SR)
        t = np.arange(length) / SR
        p = (np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * f * 2.01 * t)) * np.exp(-t / decay) * r.uniform(0.3, 1.0)
        i = int(start * SR)
        out[i: i + length] += p[: len(out) - i]
    return out


def clicks(dur, density, seed=0, lp=4000, decay=0.012, start_bias=0.0):
    """Debris: random short noise ticks (pebbles). `start_bias` > 0 front-loads them."""
    r = rng(seed)
    out = np.zeros(int(dur * SR))
    for _ in range(max(1, int(dur * density))):
        pos = (r.random() ** (1 + start_bias)) * max(dur - 0.05, 0.01)
        length = int(0.04 * SR)
        n = r.standard_normal(length) * np.exp(-np.arange(length) / SR / decay) * r.uniform(0.2, 1.0)
        i = int(pos * SR)
        out[i: i + length] += n[: len(out) - i]
    if lp and len(out) > 2048:
        out = stft_filter(out, lambda t, f: np.broadcast_to(1.0 / (1.0 + (f / lp) ** 4), (t.shape[0], f.shape[1])))
    return out


def reverb(sig, tail=0.5, mix=0.25, seed=5):
    n = int(tail * SR)
    ir = rng(seed).standard_normal(n) * np.exp(-np.arange(n) / SR / (tail * 0.3))
    ir[0] = 0
    wet = np.fft.irfft(np.fft.rfft(sig, len(sig) + n) * np.fft.rfft(ir, len(sig) + n), len(sig) + n)
    wet = wet / max(np.max(np.abs(wet)), 1e-6) * max(np.max(np.abs(sig)), 1e-6)
    out = np.concatenate([sig, np.zeros(n)]) * (1 - mix) + wet * mix
    return out


def norm(a, peak=1.0):
    m = float(np.max(np.abs(a))) if len(a) else 0.0
    return a / m * peak if m > 1e-9 else a


def mix(*layers):
    n = max(len(a) for a, _ in layers)
    out = np.zeros(n)
    for a, g in layers:
        out[: len(a)] += norm(a) * g
    return out


def at(a, delay, total=None):
    pad = np.zeros(int(delay * SR))
    out = np.concatenate([pad, a])
    if total is not None and len(out) < int(total * SR):
        out = np.concatenate([out, np.zeros(int(total * SR) - len(out))])
    return out


def fade_out(a, seconds):
    f = min(int(seconds * SR), len(a))
    a = a.copy()
    a[-f:] *= np.linspace(1, 0, f)
    return a


# ---------------------------------------------------------------------------------------------------- shared shapes
def whoosh(seed, dur=0.5, f0=800, f1=3500, f2=1500, width=1.2, peak_at=0.4, body=1.0):
    """Air whoosh: a noise band that glides f0 -> f1 -> f2 with a swelling envelope (stylised, no real-world sample)."""
    t_peak = dur * peak_at
    centre = lambda t: np.interp(t, [0, t_peak, dur], [f0, f1, f2])
    env = swell(dur, dur * peak_at, dur * (1 - peak_at))
    return band_noise(dur, centre, width, seed, amp=env ** 0.8) * body


def splash(seed, dur=0.5, size=1.0, droplets=6):
    """Water splash: bright bandpassed noise burst + droplet bubbles + soft low body."""
    burst = band_noise(dur, lambda t: 3500 - 1800 * t / dur, 1.6, seed, amp=expdecay(dur, 0.09 * size + 0.03, 0.002))
    body = lowpass_noise(dur, 900, seed + 1, amp=expdecay(dur, 0.12 * size, 0.004))
    drops = bubbles(dur, droplets / dur * 0.6, 700, 2400, seed + 2, decay=0.04)
    return mix((burst, 1.0), (body, 0.45 * size), (drops, 0.55))


def rumble(seed, dur=1.5, lo=40, hi=220, grind=0.5, rate=22.0):
    """Stone rumble: low noise with an amplitude-modulated grinding layer."""
    low = band_noise(dur, lambda t: lo + (hi - lo) * 0.3 + 0 * t, 1.0, seed)
    gr = band_noise(dur, 350, 1.4, seed + 1)
    t = tt(dur)
    mod = 0.55 + 0.45 * np.sin(2 * np.pi * rate * t + rng(seed).uniform(0, 6))
    return mix((low, 1.0), (gr * mod, grind))


def crack(seed, dur=0.5, tau=0.07, bright=3500):
    """Rock crack/impact: noise burst + low thump + a few pebbles."""
    burst = lowpass_noise(dur, bright, seed, amp=expdecay(dur, tau, 0.001))
    mid = band_noise(dur, 700, 1.2, seed + 1, amp=expdecay(dur, tau * 1.6, 0.001))
    th = thump(dur, 120, 48, tau * 2.2)
    deb = clicks(dur, 18, seed + 2, decay=0.01, start_bias=1.5)
    return mix((burst, 0.8), (mid, 0.6), (th, 1.0), (deb, 0.35))


# ---------------------------------------------------------------------------------------------------- recipes: generic
def magic_zap(seed, dur=0.8, f0=500, f1=1800):
    glide = curve([(0, f0), (dur * 0.5, f1), (dur, f1 * 0.7)], dur)
    t1 = tone(dur, glide, (1.0, 0.4, 0.2), amp=swell(dur, 0.05, dur * 0.6))
    sh = pings(dur, 14, 2500, 7000, seed, decay=0.07)
    air = whoosh(seed + 1, dur, 1200, 4500, 2000, 1.0, 0.35, 0.5)
    return mix((t1, 0.6), (sh, 0.5), (air, 0.5))


def slam(seed, dur=0.9):
    return mix((thump(dur, 90, 32, 0.30, click=0.1), 1.0), (lowpass_noise(dur, 400, seed, amp=expdecay(dur, 0.25)), 0.7))


# ---------------------------------------------------------------------------------------------------- water
# v2: softer and rounder - fewer cartoon bubbles, a "bloop" tone as the signature of water magic, band-limited noise.
def soft(a, cutoff=6500):
    """Gentle low-pass so synthetic noise does not hiss."""
    f = lambda t, f_: 1.0 / (1.0 + (f_ / cutoff) ** 4)
    return stft_filter(a, lambda t, f_: np.broadcast_to(f(t, f_), (t.shape[0], f_.shape[1])))


def bloop(dur, f0, f1, tau=0.12, amp=1.0):
    """Water-drop magic tone: sine gliding f0 -> f1 with a soft decay and a faint octave."""
    t = tt(dur)
    f = f1 + (f0 - f1) * np.exp(-t / (dur * 0.35))
    ph = 2 * np.pi * np.cumsum(f) / SR
    return (np.sin(ph) + 0.18 * np.sin(2 * ph)) * expdecay(dur, tau, 0.004) * amp


def water_cast(seed, dur=0.5, bright=1.0):
    air = band_noise(dur, lambda t: (500 + 1900 * np.sin(np.pi * np.minimum(t / dur, 1.0) * 0.9)) * bright, 1.0, seed,
                     amp=swell(dur, dur * 0.35, dur * 0.65) ** 0.9)
    lead = bloop(dur, 260 * bright, 900 * bright, 0.16)
    d1 = at(bloop(0.2, 620 * bright, 1200 * bright, 0.05), dur * 0.5, dur)[: int(dur * SR)]
    return soft(mix((air, 0.7), (lead, 0.9), (d1, 0.35)), 6000)


def water_swirl_cast(seed, dur=1.2):
    sweep = band_noise(dur, lambda t: 250 + 1800 * (t / dur) ** 1.4, 1.0, seed, amp=swell(dur, dur * 0.7, dur * 0.3))
    t = tt(dur)
    wob = 0.75 + 0.25 * np.sin(2 * np.pi * 6 * t)
    rise = tone(dur, curve([(0, 220), (dur, 760)], dur), (1.0, 0.25), amp=swell(dur, dur * 0.8, dur * 0.2) * wob)
    drops = bubbles(dur, 7, 400, 1100, seed + 1, decay=0.06)
    return soft(mix((sweep, 0.8), (rise, 0.6), (drops, 0.35)), 5500)


def water_charge(seed, dur=1.2, top=1500):
    t = tt(dur)
    rise = curve([(0, 180), (dur, top * 0.55)], dur)
    hum = tone(dur, rise, (1.0, 0.35, 0.12), amp=swell(dur, dur * 0.9, 0.12) * (0.8 + 0.2 * np.sin(2 * np.pi * 8 * t)))
    air = band_noise(dur, lambda tm: 300 + 1800 * tm / dur, 1.0, seed + 1, amp=swell(dur, dur * 0.9, 0.1))
    drops = bubbles(dur, 6, 450, 1300, seed, decay=0.05) * np.linspace(0.3, 1, int(dur * SR))
    return soft(mix((hum, 0.8), (air, 0.45), (drops, 0.3)), 5500)


def water_beam_loop(seed, dur=2.8):
    """Sustained watery energy hum: round low harmonics + soft flowing mid noise."""
    t = tt(dur)
    wob = 0.88 + 0.12 * np.sin(2 * np.pi * 5 * t)
    hum = tone(dur, 140.0, (1.0, 0.6, 0.35, 0.2), amp=wob, vib=0.003, vib_rate=4)
    flow = band_noise(dur, lambda tm: 900 + 350 * np.sin(2 * np.pi * 0.9 * tm), 0.9, seed)
    drops = bubbles(dur, 5, 450, 1300, seed + 2, decay=0.05) * 0.8
    return soft(mix((hum, 0.8), (flow, 0.5), (drops, 0.25)), 4500)


def splash(seed, dur=0.5, size=1.0, droplets=6):
    """Water splash: band-limited noise burst + a few droplets + soft low body."""
    burst = band_noise(dur, lambda t: 2400 - 1200 * np.minimum(t / dur, 1.0), 1.3, seed, amp=expdecay(dur, 0.08 * size + 0.03, 0.002))
    body = lowpass_noise(dur, 700, seed + 1, amp=expdecay(dur, 0.12 * size, 0.004))
    drops = bubbles(dur, max(2, droplets * 0.5 / dur * 0.6), 500, 1500, seed + 2, decay=0.05)
    return soft(mix((burst, 1.0), (body, 0.5 * size), (drops, 0.35)), 6000)


def water_end(seed, dur=0.8):
    down = bloop(dur, 900, 220, 0.25)
    return mix((down, 0.6), (splash(seed, dur, 0.7, 4), 0.8))


def water_wave(seed, dur=1.8):
    rush = band_noise(dur, lambda t: 350 + 1500 * np.sin(np.pi * np.minimum(t / dur, 1.0)), 1.2, seed, amp=swell(dur, dur * 0.45, dur * 0.4))
    low = lowpass_noise(dur, 400, seed + 1, amp=swell(dur, dur * 0.4, dur * 0.4))
    return soft(mix((rush, 1.0), (low, 0.8)), 4500)


def water_crash(seed, dur=1.6):
    return soft(mix((splash(seed, dur, 2.2, 10), 1.0), (thump(dur, 100, 34, 0.45), 0.9),
                    (lowpass_noise(dur, 600, seed + 3, amp=expdecay(dur, 0.45)), 0.6)), 5500)


# ---------------------------------------------------------------------------------------------------- earth
def earth_cast(seed, dur=0.6, pitch=1.0):
    scr = band_noise(dur, lambda t: (300 + 900 * t / dur) * pitch, 1.2, seed, amp=swell(dur, dur * 0.35, dur * 0.65))
    gr = clicks(dur, 40, seed + 1, decay=0.008)
    th = thump(dur, 110 * pitch, 55, 0.18) * np.linspace(0.2, 1.0, int(dur * SR))
    return mix((scr, 0.8), (gr, 0.35), (th, 0.6))


def earth_rise(seed, dur=1.6):
    r = rumble(seed, dur, 40, 200, 0.7, 24)
    env = swell(dur, dur * 0.55, dur * 0.3)
    th = at(thump(0.6, 80, 35, 0.22), dur * 0.5, dur)[: int(dur * SR)]
    return mix((r * env, 1.0), (th, 0.8), (clicks(dur, 30, seed + 4, decay=0.01) * env, 0.3))


def earth_stun_loop(seed, dur=3.4):
    r = rumble(seed, dur, 40, 160, 0.6, 14)
    return r * (0.8 + 0.2 * np.sin(2 * np.pi * 0.9 * tt(dur)))


def earth_cone(seed, dur=0.9):
    layers = []
    for i in range(5):
        layers.append((at(crack(seed + i, 0.35, 0.05 + 0.01 * i, 3000), 0.07 * i, dur), 0.9 - 0.1 * i))
    return mix(*layers)


def earth_cage(seed, dur=1.2):
    gr = rumble(seed, dur, 50, 250, 0.9, 30) * swell(dur, 0.25, 0.5)
    th = at(crack(seed + 1, 0.5, 0.09, 2500), 0.55, dur)
    return mix((gr, 0.9), (th, 1.0))


def earth_charge(seed, dur=2.0):
    r = rumble(seed, dur, 35, 180, 0.4, 16) * swell(dur, dur * 0.9, 0.1)
    rise = tone(dur, curve([(0, 55), (dur, 190)], dur), (1.0, 0.6, 0.3), amp=swell(dur, dur * 0.9, 0.1))
    sh = pings(dur, 10, 900, 2600, seed, 0.12) * np.linspace(0.2, 1, int(dur * SR))
    return mix((r, 1.0), (rise, 0.6), (sh, 0.25))


def meteor_fall(seed, dur=1.2):
    sweep = curve([(0, 2200), (dur, 180)], dur)
    t1 = tone(dur, sweep, (1.0, 0.5), amp=swell(dur, 0.1, 0.05) * np.linspace(0.3, 1, int(dur * SR)))
    air = band_noise(dur, lambda t: 2600 - 2200 * t / dur, 1.2, seed, amp=swell(dur, 0.2, 0.05))
    return mix((t1, 0.5), (air, 0.9))


def explosion(seed, dur=1.2, size=1.0):
    boom = lowpass_noise(dur, 900, seed, amp=expdecay(dur, 0.28 * size, 0.002))
    crack_ = band_noise(dur, 2500, 1.5, seed + 1, amp=expdecay(dur, 0.06, 0.001))
    th = thump(dur, 90, 28, 0.4 * size, click=0.0)
    deb = clicks(dur, 40, seed + 2, decay=0.015, start_bias=1.0)
    return mix((boom, 0.9), (crack_, 0.6), (th, 1.0), (deb, 0.35))


def earth_aftermath(seed, dur=1.6):
    deb = clicks(dur, 55, seed, decay=0.012, start_bias=1.2)
    r = rumble(seed + 1, dur, 40, 150, 0.3, 10) * expdecay(dur, 0.6)
    return mix((deb, 0.8), (r, 0.7))


# ---------------------------------------------------------------------------------------------------- wind
def wind_throw(seed, dur=0.55, pitch=1.0):
    return whoosh(seed, dur, 900 * pitch, 4200 * pitch, 1600 * pitch, 1.1, 0.35, 1.0)


def wind_loop(seed, dur=2.4, lo=350, hi=2200, speed=1.0):
    t = tt(dur)
    m = lambda tm: lo + (hi - lo) * (0.5 + 0.5 * np.sin(2 * np.pi * speed * tm / dur * 2))
    body = band_noise(dur, m, 1.0, seed)
    whistle = tone(dur, 900 + 250 * np.sin(2 * np.pi * speed * t / dur * 3), (1.0, 0.2), amp=0.25 + 0.15 * np.sin(2 * np.pi * 2 * t / dur))
    return mix((body, 1.0), (whistle, 0.12))


def wind_catch(seed, dur=0.45):
    return mix((whoosh(seed, dur, 2500, 5000, 3500, 1.0, 0.25), 0.8), (pings(dur, 8, 3000, 6500, seed + 1, 0.07), 0.5))


def wind_swirl_cast(seed, dur=1.2):
    sweep = band_noise(dur, lambda t: 300 + 3000 * (t / dur) ** 1.4, 1.0, seed, amp=swell(dur, dur * 0.7, dur * 0.3))
    t = tt(dur)
    tr = 0.65 + 0.35 * np.sin(2 * np.pi * 8 * t)
    return mix((sweep * tr, 1.0), (pings(dur, 10, 3000, 7000, seed + 1, 0.09), 0.3))


def wind_land(seed, dur=0.5):
    return mix((thump(dur, 140, 60, 0.12), 0.9), (lowpass_noise(dur, 1500, seed, amp=expdecay(dur, 0.1)), 0.6),
               (band_noise(dur, 1500, 1.2, seed + 1, amp=expdecay(dur, 0.18)), 0.4))


def wind_pulse(seed, dur=0.9, power=1.0):
    gust = band_noise(dur, lambda t: 500 + 2800 * np.exp(-t / 0.25), 1.2, seed, amp=expdecay(dur, 0.3 * power, 0.02))
    low = lowpass_noise(dur, 600, seed + 1, amp=expdecay(dur, 0.2))
    return mix((gust, 1.0), (low, 0.5))


def wind_slam(seed, dur=0.6):
    return mix((crack(seed, dur, 0.06, 2800), 0.9), (wind_pulse(seed + 1, dur, 0.6), 0.7))


def wind_charge(seed, dur=2.0):
    sweep = band_noise(dur, lambda t: 250 + 3500 * (t / dur) ** 1.6, 1.1, seed, amp=swell(dur, dur * 0.9, 0.1))
    ring = tone(dur, curve([(0, 400), (dur, 1500)], dur), (1.0, 0.4, 0.2), amp=swell(dur, dur * 0.9, 0.1) * 0.5)
    return mix((sweep, 1.0), (ring, 0.35), (pings(dur, 14, 3000, 7500, seed + 1, 0.1) * np.linspace(0.2, 1, int(dur * SR)), 0.4))


def wind_dive(seed, dur=0.9):
    return mix((whoosh(seed, dur, 4500, 2500, 400, 1.1, 0.2), 1.0), (tone(dur, curve([(0, 1800), (dur, 300)], dur), (1.0,), amp=swell(dur, 0.1, 0.3)), 0.25))


def wind_shockwave(seed, dur=1.3):
    return mix((wind_pulse(seed, dur, 1.6), 1.0), (thump(dur, 80, 30, 0.35), 0.9), (explosion(seed + 1, dur, 0.6), 0.4))


def feather(seed, dur=0.7):
    air = band_noise(dur, 6000, 0.8, seed, amp=swell(dur, 0.06, 0.4) * 0.5)
    return mix((air, 0.6), (pings(dur, 4, 4000, 8000, seed + 1, 0.05), 0.2))


# ---------------------------------------------------------------------------------------------------- Boss Water (crab)
def clack(seed, dur=0.4, pitch=1.0):
    """Hard shell clack: bright noise tick + a short ringing knock + body thump."""
    tick = band_noise(dur, 3200 * pitch, 1.0, seed, amp=expdecay(dur, 0.025, 0.0005))
    ring = tone(dur, 760 * pitch, (1.0, 0.5, 0.25), amp=expdecay(dur, 0.09, 0.0005))
    th = thump(dur, 160 * pitch, 70, 0.1)
    return mix((tick, 1.0), (ring, 0.45), (th, 0.8))


def claw_windup(seed, dur=0.8):
    scrape = band_noise(dur, lambda t: 500 + 2200 * (np.minimum(t / dur, 1.0)) ** 1.3, 1.0, seed, amp=swell(dur, dur * 0.8, dur * 0.2))
    tk = clicks(dur, 22, seed + 1, decay=0.006) * np.linspace(0.3, 1.0, int(dur * SR))
    return mix((scrape, 0.8), (tk, 0.4))


def bubble_blow(seed, dur=0.55):
    puff = band_noise(dur, lambda t: 700 + 600 * np.sin(np.pi * np.minimum(t / dur, 1.0)), 1.0, seed, amp=swell(dur, dur * 0.3, dur * 0.5))
    drops = bubbles(dur, 18, 350, 1100, seed + 1, decay=0.05)
    pop = bloop(dur, 300, 800, 0.2)
    return soft(mix((puff, 0.7), (drops, 0.6), (pop, 0.7)), 4500)


def bubble_pop(seed, dur=0.3):
    snap = band_noise(dur, 2600, 0.9, seed, amp=expdecay(dur, 0.02, 0.0004))
    chirp = bubbles(dur, 4, 900, 1600, seed + 1, decay=0.03)
    water = splash(seed + 2, dur, 0.4, 3)
    return soft(mix((snap, 0.8), (chirp, 0.5), (water, 0.6)), 6500)


def sand_burrow(seed, dur=1.1):
    grain = band_noise(dur, lambda t: 2800 - 2000 * np.minimum(t / dur, 1.0), 1.1, seed, amp=expdecay(dur, 0.45, 0.01))
    gr = clicks(dur, 70, seed + 1, decay=0.01, start_bias=0.6)
    low = lowpass_noise(dur, 350, seed + 2, amp=swell(dur, 0.1, dur * 0.7))
    th = at(thump(0.5, 90, 40, 0.18), dur * 0.5, dur)[: int(dur * SR)]
    return mix((grain, 0.7), (gr, 0.45), (low, 0.6), (th, 0.8))


def mound_loop(seed, dur=2.4):
    t = tt(dur)
    drag = band_noise(dur, lambda tm: 450 + 250 * np.sin(2 * np.pi * 2.2 * tm), 1.0, seed)
    low = lowpass_noise(dur, 260, seed + 1, amp=0.7 + 0.3 * np.sin(2 * np.pi * 4.4 * t))
    gr = clicks(dur, 45, seed + 2, decay=0.008)
    return mix((drag, 0.8), (low, 0.7), (gr, 0.3))


def emerge(seed, dur=0.9):
    burst = lowpass_noise(dur, 2500, seed, amp=expdecay(dur, 0.12, 0.002))
    grit = band_noise(dur, 2200, 1.3, seed + 1, amp=expdecay(dur, 0.2, 0.002))
    deb = clicks(dur, 60, seed + 2, decay=0.012, start_bias=1.0)
    th = thump(dur, 110, 42, 0.3)
    return mix((burst, 0.8), (grit, 0.6), (deb, 0.45), (th, 1.0))


def conch_horn(seed, dur=1.4):
    """Warning horn (shell): swelling tone with soft harmonics, slightly wobbling."""
    t = tt(dur)
    f = curve([(0, 150), (dur * 0.35, 190), (dur, 175)], dur)
    horn = tone(dur, f, (1.0, 0.6, 0.45, 0.25, 0.12), amp=swell(dur, 0.25, 0.5) * (0.9 + 0.1 * np.sin(2 * np.pi * 5 * t)), vib=0.01, vib_rate=5)
    air = band_noise(dur, 900, 1.2, seed, amp=swell(dur, 0.25, 0.5) * 0.4)
    return mix((horn, 1.0), (air, 0.35))


def whirl_loop(seed, dur=3.0):
    t = tt(dur)
    swirl = band_noise(dur, lambda tm: 500 + 600 * np.sin(2 * np.pi * 1.3 * tm), 1.0, seed)
    hum = tone(dur, 110.0, (1.0, 0.5, 0.25), amp=0.8 + 0.2 * np.sin(2 * np.pi * 3 * t))
    gur = bubbles(dur, 7, 300, 900, seed + 1, decay=0.06)
    return soft(mix((swirl, 0.8), (hum, 0.5), (gur, 0.3)), 4200)


def tentacle(seed, dur=0.7):
    whip = whoosh(seed, dur, 900, 3200, 1200, 1.0, 0.3, 0.9)
    return soft(mix((whip, 0.9), (splash(seed + 1, dur, 0.8, 4), 0.7)), 5500)


def crab_roar(seed, dur=1.6):
    """Chittering hiss: wobbling noise band + rapid clacks, no voice."""
    t = tt(dur)
    wob = 0.6 + 0.4 * np.sin(2 * np.pi * 14 * t)
    hiss = band_noise(dur, lambda tm: 2600 + 700 * np.sin(2 * np.pi * 3 * tm), 0.9, seed, amp=swell(dur, 0.12, dur * 0.5) * wob)
    low = lowpass_noise(dur, 500, seed + 1, amp=swell(dur, 0.1, dur * 0.6))
    clk = clicks(dur, 38, seed + 2, decay=0.01, lp=6000) * swell(dur, 0.1, dur * 0.5)
    return mix((hiss, 0.9), (low, 0.6), (clk, 0.5))


def crab_hit(seed, dur=0.35):
    return mix((crack(seed, dur, 0.04, 5200), 0.8), (clack(seed + 1, dur, 1.2), 0.7))


def crab_death(seed, dur=2.6):
    roar = crab_roar(seed, dur * 0.7)
    fall = at(splash(seed + 1, 1.2, 2.0, 8), 0.8, dur)
    rum = rumble(seed + 2, dur, 40, 180, 0.4, 12) * expdecay(dur, 1.0)
    return mix((at(roar, 0, dur), 0.9), (fall[: int(dur * SR)], 0.8), (rum, 0.6))


from synth_owl import *  # noqa: E402,F401,F403  (Boss Wind owl recipes, D-111)

RECIPES = {n: f for n, f in globals().items() if callable(f) and not n.startswith("_") and n not in (
    "rng", "tt", "curve", "expdecay", "swell", "stft_filter", "band_noise", "lowpass_noise", "tone", "thump", "bubbles",
    "pings", "clicks", "reverb", "norm", "mix", "at", "fade_out", "soft", "bloop")}

if __name__ == "__main__":
    import sys
    import wave
    name = sys.argv[1]
    a = norm(RECIPES[name](int(sys.argv[2]) if len(sys.argv) > 2 else 1)) * 0.8
    with wave.open(name + ".wav", "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((a * 32767).astype("<i2").tobytes())
