"""Download a pixelart_workbench `draw` result (full.png sheet, 3 columns) and repack it as a horizontal strip.
Usage: python Tools/fetch_layered_clip.py <drawing_id> <ClipName> <frame_w> <frame_h> <frames> [out_dir]"""
import os, subprocess, sys, io
from PIL import Image

drawing, name = sys.argv[1], sys.argv[2]
fw, fh, count = int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5])
out_dir = sys.argv[6] if len(sys.argv) > 6 else "Assets/Art/BossArena/Earth/Boss/Anim"
data = subprocess.run(["curl", "-sf", f"https://api.pixellab.ai/mcp/pixel-tools/{drawing}/assets/south/full.png"], capture_output=True).stdout
sheet = Image.open(io.BytesIO(data)).convert("RGBA")
cols = sheet.width // fw
strip = Image.new("RGBA", (fw * count, fh), (0, 0, 0, 0))
for i in range(count):
    x, y = (i % cols) * fw, (i // cols) * fh
    strip.paste(sheet.crop((x, y, x + fw, y + fh)), (i * fw, 0))
os.makedirs(out_dir, exist_ok=True)
path = f"{out_dir}/{name}_{count}f.png"
strip.save(path)
print(path, strip.size)
