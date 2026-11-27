"""Perceived loudness for game SFX (D-122 volume pass).

`active_lufs(a)` = ITU-R BS.1770 K-weighting, then the mean power of the 100 ms windows that are within 20 dB of the
loudest window ("how loud it sounds while it is sounding"). Unlike plain RMS this ranks a bright click, a bass rumble
and a 30 s ambience on the same scale, and unlike integrated LUFS it also works for 50 ms clips.
"""
import numpy as np
from scipy.signal import lfilter

SR = 44100


def _k_weighting(sr=SR):
    # BS.1770 stage 1 (high shelf, +4 dB above ~1.5 kHz) and stage 2 (RLB high-pass ~38 Hz), designed for any rate
    # with the same formulas pyloudnorm uses.
    G, Q, fc = 3.999843853973347, 0.7071752369554196, 1681.974450955533
    A = 10 ** (G / 40.0)
    w0 = 2.0 * np.pi * fc / sr
    alpha = np.sin(w0) / (2.0 * Q)
    cw = np.cos(w0)
    b1 = np.array([A * ((A + 1) + (A - 1) * cw + 2 * np.sqrt(A) * alpha),
                   -2 * A * ((A - 1) + (A + 1) * cw),
                   A * ((A + 1) + (A - 1) * cw - 2 * np.sqrt(A) * alpha)])
    a1 = np.array([(A + 1) - (A - 1) * cw + 2 * np.sqrt(A) * alpha,
                   2 * ((A - 1) - (A + 1) * cw),
                   (A + 1) - (A - 1) * cw - 2 * np.sqrt(A) * alpha])
    Q2, fc2 = 0.5003270373238773, 38.13547087602444
    w0 = 2.0 * np.pi * fc2 / sr
    alpha = np.sin(w0) / (2.0 * Q2)
    cw = np.cos(w0)
    b2 = np.array([(1 + cw) / 2, -(1 + cw), (1 + cw) / 2])
    a2 = np.array([1 + alpha, -2 * cw, 1 - alpha])
    return (b1 / a1[0], a1 / a1[0]), (b2 / a2[0], a2 / a2[0])


_K = _k_weighting()


def active_lufs(a, sr=SR):
    a = np.asarray(a, dtype=np.float64)
    if len(a) == 0:
        return -90.0
    y = lfilter(_K[0][0], _K[0][1], a)
    y = lfilter(_K[1][0], _K[1][1], y)
    win = max(1, int(0.1 * sr))
    n = max(1, len(y) // win)
    blocks = y[: n * win].reshape(n, win) if len(y) >= win else y.reshape(1, -1)
    power = np.mean(blocks ** 2, axis=1)
    top = float(np.max(power))
    if top <= 1e-12:
        return -90.0
    kept = power[power >= top * 10 ** (-20 / 10)]
    return -0.691 + 10 * np.log10(float(np.mean(kept)))


def soft_limit(a, ceiling_db=-1.0, knee_db=6.0):
    """Peaks above (ceiling - knee) are bent smoothly towards the ceiling (tanh), so short loud transients (punches,
    clicks) can reach their loudness target without hard clipping. Returns a new array."""
    ceiling = 10 ** (ceiling_db / 20)
    start = 10 ** ((ceiling_db - knee_db) / 20)
    out = np.array(a, dtype=np.float64)
    mag = np.abs(out)
    over = mag > start
    if np.any(over):
        span = ceiling - start
        out[over] = np.sign(out[over]) * (start + span * np.tanh((mag[over] - start) / span))
    return out
