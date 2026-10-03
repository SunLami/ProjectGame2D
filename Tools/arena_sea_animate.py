"""Compose the Pixellab sea-wave tile animation (200x236 x 8 frames, SeaWave_Loop_9f.png) onto the whole beach arena.
- colours are snapped back to the base map palette (the generation input was palette-reduced),
- only sea / foam / wet-sand pixels animate (sand stays still), tile edges are feathered to the static map,
- the 5 half-wave strips of the map (normal / mirrored alternately) get phase-shifted copies of the tile.
Writes: Anim/SeaWave_Tile_8f.png (cleaned frames), Anim/WaterMap_Anim_Preview.gif, Anim/WaterMap_Anim_f0..7.png (top 1320x250 crops).
Usage: python Tools/arena_sea_animate.py"""
import numpy as np
from PIL import Image

BASE = "Assets/Art/BossArena/Water/BaseMap_Water_Empty_1320x804.png"
LOOP = "Assets/Art/BossArena/Water/Anim/SeaWave_Loop_9f.png"
OUT = "Assets/Art/BossArena/Water/Anim/"
W, H = 200, 236
X0, STRIP, N_STRIPS, HEAD = 75, 202, 5, 74
PHASES = [0, 3, 6, 2, 5]
FEATHER = 14

base = np.asarray(Image.open(BASE).convert("RGB")).astype(np.int32)
static = base[:H, X0:X0 + W].copy()                      # static tile region (src cols 75..274)
sheet = np.asarray(Image.open(LOOP).convert("RGB")).astype(np.int32)
frames = [sheet[:, i * W:(i + 1) * W] for i in range(8)]  # 9th frame == 1st (pinned), dropped

palette = np.unique(base[:260].reshape(-1, 3), axis=0)
print("palette", len(palette))


def snap(img):
    flat = img.reshape(-1, 3)
    out = np.empty_like(flat)
    for s in range(0, len(flat), 8192):
        chunk = flat[s:s + 8192]
        d = ((chunk[:, None, :] - palette[None, :, :]) ** 2).sum(2)
        out[s:s + 8192] = palette[d.argmin(1)]
    return out.reshape(img.shape)


r, g, b = static[..., 0], static[..., 1], static[..., 2]
sand = (r > 150) & (r > b + 25)                          # sand / orange wet-sand band stays static
water = ~sand
# keep the mask solid: drop 1-2 px specks
from PIL import ImageFilter
m = Image.fromarray((water * 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(5))
water = np.asarray(m) > 127
ramp = np.minimum(np.arange(W), np.arange(W)[::-1]).astype(np.float32)
ramp = np.clip(ramp / FEATHER, 0, 1)[None, :]
alpha = water.astype(np.float32) * ramp


def tile_frame(i):
    snapped = snap(frames[i])
    out = static * (1 - alpha[..., None]) + snapped * alpha[..., None]
    return np.clip(np.rint(out), 0, 255).astype(np.uint8)


tiles = [tile_frame(i) for i in range(8)]
print("tile0 vs static changed px:", (np.abs(tiles[0].astype(int) - static).sum(2) > 30).sum())
strip = np.concatenate(tiles, 1)
Image.fromarray(strip).save(OUT + "SeaWave_Tile_8f.png")


# ---- tail: the original right-hand part of the bay (map x 1084..1267) is not one of the mirrored strips and its
# shoreline sits a few px higher, so reuse the mirrored tile and shift every column vertically to the tail's shoreline.
TAIL_X, TAIL_W = 1084, 184
static_tail = base[:H, TAIL_X:TAIL_X + TAIL_W].copy()


def shore_y(img):
    rr, gg, bb = img[..., 0], img[..., 1], img[..., 2]
    sandlike = (rr > 150) & (rr > bb + 25)
    ys = np.full(img.shape[1], H - 1)
    for x in range(img.shape[1]):
        hit = np.where(sandlike[60:, x])[0]
        if len(hit):
            ys[x] = 60 + hit[0]
    return ys


static_flip = static[:, ::-1][:, :TAIL_W]
dy = np.clip(shore_y(static_tail) - shore_y(static_flip), -24, 24)
# smooth the column offsets so the shifted pattern does not shear
dy = np.rint(np.convolve(np.pad(dy, 6, mode="edge"), np.ones(13) / 13, mode="valid")).astype(int)
rr, gg, bb = static_tail[..., 0], static_tail[..., 1], static_tail[..., 2]
tail_water = ~((rr > 150) & (rr > bb + 25))
tail_water = np.asarray(Image.fromarray((tail_water * 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(5))) > 127
tail_ramp = np.clip(np.minimum(np.arange(TAIL_W), np.arange(TAIL_W)[::-1]).astype(np.float32) / FEATHER, 0, 1)[None, :]
tail_alpha = tail_water.astype(np.float32) * tail_ramp
rows = np.arange(H)


def tail_frame(i):
    flipped = snap(frames[i])[:, ::-1][:, :TAIL_W]
    shifted = np.empty_like(flipped)
    for k in range(TAIL_W):
        shifted[:, k] = flipped[np.clip(rows - dy[k], 0, H - 1), k]
    out = static_tail * (1 - tail_alpha[..., None]) + shifted * tail_alpha[..., None]
    return np.clip(np.rint(out), 0, 255).astype(np.uint8)


tail_tiles = [tail_frame(i) for i in range(8)]
Image.fromarray(np.concatenate(tail_tiles, 1)).save(OUT + "SeaWave_TailTile_8f.png")
TAIL_PHASE = 4


def compose(f):
    out = base.copy().astype(np.uint8)
    for i in range(N_STRIPS):
        tile = tiles[(f + PHASES[i]) % 8]
        x0 = HEAD + STRIP * i
        if i % 2 == 0:
            out[:H, x0 + 1:x0 + 1 + W] = tile
        else:
            out[:H, x0 + 2:x0 + 2 + W] = tile[:, ::-1]
    out[:H, TAIL_X:TAIL_X + TAIL_W] = tail_tiles[(f + TAIL_PHASE) % 8]
    return out


frames_full = [compose(f) for f in range(8)]
# crops for the review: top band of the map
crops = [Image.fromarray(fr[:260]) for fr in frames_full]
for i, c in enumerate(crops):
    c.save(OUT + f"WaterMap_Anim_f{i}.png")
crops[0].save(OUT + "WaterMap_Anim_Preview.gif", save_all=True, append_images=crops[1:], duration=130, loop=0, disposal=2)
print("done")
