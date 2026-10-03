"""Download the layered crab animations rendered by pixelart_workbench (full.png sheets, 3 columns), drop isolated
specks left behind by the cut-out (static outline pixels outside a moving part), and write 9-frame strips.
Usage: python Tools/fetch_crab_clips.py [out_dir]"""
import io
import os
import subprocess
import sys
import numpy as np
from PIL import Image

OUT = "Assets/Art/BossArena/Water/Boss/Anim"
ONLY = sys.argv[1:]  # optional clip names; empty = all
DRAWINGS = {
    "Idle": "da7ad646-5960-4dd5-8d32-cfdbb9da96a6",
    "Move": "2ebed04c-b520-4df8-8add-bac24b514711",
    "Snap": "54866c6f-a80f-43cf-8c35-99a7b378407f",
    "Recovery": "0178766d-93e3-4c9c-bbf0-c0188b39fa68",
    "Burrow": "70ead6c0-6578-4e8c-a970-17e7f8f4b92a",
}
W = H = 320
SPECK = 40  # connected pieces smaller than this (px) that do not touch the main body are removed


def components(mask):
    h, w = mask.shape
    seen = np.zeros_like(mask, bool)
    out = []
    for y in range(h):
        for x in range(w):
            if mask[y, x] and not seen[y, x]:
                stack, pts = [(y, x)], []
                seen[y, x] = True
                while stack:
                    cy, cx = stack.pop()
                    pts.append((cy, cx))
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                                seen[ny, nx] = True
                                stack.append((ny, nx))
                out.append(pts)
    return out


def clean(frame):
    a = np.asarray(frame.convert("RGBA")).copy()
    # grow by 2 px so thin outlines that nearly touch their body count as connected
    solid = a[..., 3] > 0
    grown = solid.copy()
    for dy in (-2, -1, 0, 1, 2):
        for dx in (-2, -1, 0, 1, 2):
            grown |= np.roll(np.roll(solid, dy, 0), dx, 1)
    removed = 0
    for pts in components(grown):
        if len(pts) < SPECK * 6:  # in the grown mask
            for y, x in pts:
                if a[y, x, 3] > 0:
                    a[y, x, 3] = 0
                    removed += 1
    return Image.fromarray(a), removed


os.makedirs(OUT, exist_ok=True)
for name, drawing in DRAWINGS.items():
    if ONLY and name not in ONLY:
        continue
    data = subprocess.run(["curl", "-sf", f"https://api.pixellab.ai/mcp/pixel-tools/{drawing}/assets/south/full.png"], capture_output=True).stdout
    sheet = Image.open(io.BytesIO(data)).convert("RGBA")
    cols = sheet.width // W
    strip = Image.new("RGBA", (W * 9, H), (0, 0, 0, 0))
    total = 0
    for i in range(9):
        frame = sheet.crop(((i % cols) * W, (i // cols) * H, (i % cols) * W + W, (i // cols) * H + H))
        frame, n = clean(frame)
        total += n
        strip.paste(frame, (i * W, 0))
    path = f"{OUT}/Crab_{name}_9f.png"
    strip.save(path)
    print(path, strip.size, "specks removed", total)
