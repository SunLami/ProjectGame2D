"""Download an animate_image result (frames 0..N, 176 px) and write a horizontal sprite sheet + a quick motion report.
Usage: python Tools/fetch_vfx_sheet.py <job_id> <SheetName> [out_dir] [frames=9]
Waits until the job's frames are downloadable."""
import io
import os
import subprocess
import sys
import time

import numpy as np
from PIL import Image

job, name = sys.argv[1], sys.argv[2]
out_dir = sys.argv[3] if len(sys.argv) > 3 else "Assets/Resources/VFX/Skills/Water"
count = int(sys.argv[4]) if len(sys.argv) > 4 else 9


def get(i):
    return subprocess.run(["curl", "-sf", f"https://api.pixellab.ai/mcp/images/{job}/download?index={i}"], capture_output=True).stdout


frames = []
for i in range(count):
    data = b""
    for _ in range(100):
        data = get(i)
        if data:
            break
        time.sleep(4)
    if not data:
        raise SystemExit(f"frame {i} never became available")
    frames.append(Image.open(io.BytesIO(data)).convert("RGBA"))

w, h = frames[0].size
sheet = Image.new("RGBA", (w * count, h), (0, 0, 0, 0))
for i, f in enumerate(frames):
    sheet.paste(f, (i * w, 0))
os.makedirs(out_dir, exist_ok=True)
path = f"{out_dir}/{name}.png"
sheet.save(path)
arrs = [np.array(f) for f in frames]
print(path, sheet.size)
print("opaque px per frame:", [int((a[..., 3] > 16).sum()) for a in arrs])
print("changed px vs previous:", [int((np.abs(arrs[i].astype(int) - arrs[i - 1].astype(int)).sum(-1) > 30).sum()) for i in range(1, count)])
