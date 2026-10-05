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
DRAWINGS = {  # layered clips of the NEW base CrabUp_256 (both claws raised), 2026-10-05
    "Idle": "634257d5-05a8-4e92-b993-df727c959ecf",
    "Move": "d80ecb05-f7c8-446b-8cf4-55d1b7fabd3d",
    "Snap": "d6470680-f37a-471a-85f9-68a043dfcbe4",
    "Recovery": "ea04d857-8732-4ae2-be07-c9a7b3ac957f",
    "Burrow": "83d02d84-3964-4661-9f4e-f4917777001c",
    "Spit": "7510c9a6-e7b2-4197-92a3-911b9500d7b8",
    "Whirl": "b0dd7769-f106-4d51-aede-5baabbc11501",
    "Slam": "d4572900-c9fa-4219-bc01-90c04bc98f27",
    "Resonance": "79237daa-4489-4323-90d4-8aa2f1665489",
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
