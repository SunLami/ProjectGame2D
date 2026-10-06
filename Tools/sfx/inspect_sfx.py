"""Quick look at generated SFX: metrics table + a contact sheet PNG (waveform on top, spectrogram below per file).
  python Tools/sfx/inspect_sfx.py "Combat/sfx_combat_player_attack*" [--out sheet.png]
Pattern is relative to Assets/Resources/Audio/SFX; accepts wav and ogg (ogg needs ffmpeg)."""
import glob
import io
import os
import subprocess
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Resources", "Audio", "SFX"))
SR = 44100


def load(path):
    if path.endswith(".wav"):
        with wave.open(path) as w:
            raw = w.readframes(w.getnframes())
            a = np.frombuffer(raw, dtype="<i2").astype(np.float32) / 32768
            return a, w.getframerate()
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"], capture_output=True).stdout
    return np.frombuffer(raw, dtype=np.float32), SR


def spectrogram(a, width, height):
    n = 1024
    hop = max(1, (len(a) - n) // width)
    cols = []
    win = np.hanning(n)
    for i in range(width):
        seg = a[i * hop: i * hop + n]
        if len(seg) < n:
            seg = np.pad(seg, (0, n - len(seg)))
        cols.append(np.abs(np.fft.rfft(seg * win)))
    mag = np.array(cols).T[: n // 2]  # (freq, time)
    db = 20 * np.log10(mag + 1e-6)
    db = np.clip((db + 80) / 80, 0, 1)
    # log-ish frequency compression to `height` rows
    idx = np.unique(np.clip((np.logspace(0, np.log10(n // 2 - 1), height)).astype(int), 0, n // 2 - 1))
    img = np.zeros((height, width))
    img[: len(idx)] = db[idx]
    img = img[::-1]
    rgb = np.stack([img * 255, np.power(img, 1.6) * 220, np.power(img, 3) * 120], -1).astype(np.uint8)
    return Image.fromarray(rgb)


def main():
    pattern = sys.argv[1]
    out = sys.argv[sys.argv.index("--out") + 1] if "--out" in sys.argv else None
    files = sorted(glob.glob(os.path.join(ROOT, pattern + ("" if "." in os.path.basename(pattern) else ".*"))))
    files = [f for f in files if f.endswith((".wav", ".ogg"))]
    rows = []
    for f in files:
        a, sr = load(f)
        peak = float(np.max(np.abs(a))) if len(a) else 0
        act = a[np.abs(a) > 0.01]
        rms = float(np.sqrt(np.mean(act ** 2))) if len(act) else 0
        spec = np.abs(np.fft.rfft(a[: SR * 2] * np.hanning(min(len(a), SR * 2))))
        fr = np.fft.rfftfreq(min(len(a), SR * 2), 1 / sr)
        cen = int(np.sum(fr * spec) / max(np.sum(spec), 1e-9))
        print(f"{os.path.relpath(f, ROOT):<58} {len(a) / sr:5.2f}s peak {20 * np.log10(max(peak, 1e-9)):6.1f}dB rms {20 * np.log10(max(rms, 1e-9)):6.1f}dB centroid {cen:5d}Hz")
        rows.append((os.path.basename(f), a, sr))
    if out and rows:
        w, h1, h2 = 360, 50, 80
        sheet = Image.new("RGB", (w * 3, (h1 + h2 + 16) * ((len(rows) + 2) // 3)), (20, 22, 28))
        d = ImageDraw.Draw(sheet)
        for i, (name, a, sr) in enumerate(rows):
            x, y = (i % 3) * w, (i // 3) * (h1 + h2 + 16)
            d.text((x + 4, y + 2), name[:48], fill=(220, 220, 220))
            wave_img = Image.new("RGB", (w - 8, h1), (30, 34, 44))
            wd = ImageDraw.Draw(wave_img)
            step = max(1, len(a) // (w - 8))
            for px in range(w - 8):
                seg = a[px * step: (px + 1) * step]
                if len(seg):
                    amp = float(np.max(np.abs(seg)))
                    wd.line([(px, h1 // 2 - amp * h1 / 2), (px, h1 // 2 + amp * h1 / 2)], fill=(120, 200, 255))
            sheet.paste(wave_img, (x + 4, y + 14))
            sheet.paste(spectrogram(a, w - 8, h2), (x + 4, y + 14 + h1))
        sheet.save(out)
        print("sheet ->", out)


if __name__ == "__main__":
    main()
