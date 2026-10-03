"""Download all frames of a Pixellab animation job (via curl: python's SSL store is unreliable here) and pack
them into one horizontal strip.
Usage: python Tools/fetch_boss_clip.py <job_id> <ClipName> [out_dir]"""
import io, os, subprocess, sys
from PIL import Image

job, name = sys.argv[1], sys.argv[2]
out_dir = sys.argv[3] if len(sys.argv) > 3 else "Assets/Art/BossArena/Earth/Boss/Anim"
frames = []
for i in range(32):
    data = subprocess.run(["curl", "-sf", f"https://api.pixellab.ai/mcp/images/{job}/download?index={i}"], capture_output=True).stdout
    if not data:
        break
    frames.append(Image.open(io.BytesIO(data)).convert("RGBA"))
w, h = frames[0].size
strip = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
for i, f in enumerate(frames):
    strip.paste(f, (i * w, 0))
os.makedirs(out_dir, exist_ok=True)
path = f"{out_dir}/{name}_{len(frames)}f.png"
strip.save(path)
print(path, len(frames), (w, h))
