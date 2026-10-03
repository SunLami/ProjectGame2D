"""Extend the Pixen-generated empty beach arena (512x424) to the Earth arena size (1320x804) without scaling pixels:
loop a plain segment horizontally (sea + shoreline + sand) and vertically (dry sand), join the copies along a
minimum-cost seam, then remove the small repeated sand marks and re-scatter pebbles/dashes irregularly.
Usage: python Tools/arena_extend_beach.py [source.png] [out.png]"""
import random
import sys
import numpy as np
from PIL import Image, ImageFilter

SRC = sys.argv[1] if len(sys.argv) > 1 else "Assets/Art/BossArena/Water/BaseMap_Water_Source_512x424.png"
OUT = sys.argv[2] if len(sys.argv) > 2 else "Assets/Art/BossArena/Water/BaseMap_Water_Empty_1320x804.png"
TARGET_W, TARGET_H = 1320, 804
random.seed(7)


def seam_path(A, B):
    d = np.sqrt(((A - B) ** 2).sum(2))
    lum = (A.mean(2) + B.mean(2)) / 2
    cost = d + 0.15 * lum
    h, w = cost.shape
    acc = cost.copy()
    back = np.zeros((h, w), int)
    for y in range(1, h):
        prev = acc[y - 1]
        for x in range(w):
            lo, hi = max(0, x - 1), min(w, x + 2)
            j = lo + int(np.argmin(prev[lo:hi]))
            back[y, x] = j
            acc[y, x] += prev[j]
    x = int(np.argmin(acc[-1]))
    path = [x]
    for y in range(h - 1, 0, -1):
        x = back[y, x]
        path.append(x)
    return path[::-1]


def join_h(L, R, ov):
    h = L.shape[0]
    A, B = L[:, -ov:], R[:, :ov]
    p = seam_path(A, B)
    out = np.zeros((h, L.shape[1] + R.shape[1] - ov, 3), L.dtype)
    out[:, : L.shape[1] - ov] = L[:, :-ov]
    out[:, L.shape[1]:] = R[:, ov:]
    for y in range(h):
        out[y, L.shape[1] - ov: L.shape[1]] = np.concatenate([A[y, : p[y]], B[y, p[y]:]], 0)
    return out


def best_loop(A, step, ov_range, lo0, hi0, limit):
    """Loop (x0, x1, ov) whose period minus overlap equals `step` and whose ends match best."""
    best = (1e18, None)
    for ov in ov_range:
        p = step + ov
        for x0 in range(lo0, hi0):
            x1 = x0 + p
            if x1 > limit:
                break
            d = np.abs(A[:, x1 - ov:x1] - A[:, x0:x0 + ov]).mean()
            if d < best[0]:
                best = (d, (x0, x1, ov))
    return best


def extend(img, target, step, ov_range, lo0, hi0, axis):
    A = img if axis == 1 else img.transpose(1, 0, 2)
    size = A.shape[1]
    assert (target - size) % step == 0, "step must divide the growth"
    loops = (target - size) // step
    d, (x0, x1, ov) = best_loop(A, step, ov_range, lo0, hi0, size - 14)
    print("axis", axis, "loop", x0, x1, "ov", ov, "loops", loops, "diff %.2f" % d)
    out = A[:, :x1]
    for _ in range(loops - 1):
        out = join_h(out, A[:, x0:x1], ov)
    out = join_h(out, A[:, x0:], ov)
    assert out.shape[1] == target, out.shape
    return out if axis == 1 else out.transpose(1, 0, 2)


def components(mask):
    h, w = mask.shape
    seen = np.zeros_like(mask, bool)
    comps = []
    for y in range(h):
        for x in range(w):
            if mask[y, x] and not seen[y, x]:
                stack = [(y, x)]
                seen[y, x] = True
                pts = []
                while stack:
                    cy, cx = stack.pop()
                    pts.append((cy, cx))
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                                seen[ny, nx] = True
                                stack.append((ny, nx))
                comps.append(pts)
    return comps


def marks(img, rect):
    """Small high-contrast specks on plain sand: returns (mask, median image)."""
    x0, y0, x1, y1 = rect
    sub = Image.fromarray(np.clip(img[y0:y1, x0:x1], 0, 255).astype(np.uint8))
    med = np.asarray(sub.filter(ImageFilter.MedianFilter(7))).astype(np.float32)
    diff = np.abs(img[y0:y1, x0:x1] - med).sum(2)
    return diff > 22, med


# ---- build
src = np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32)
# remove the single grass tuft on the left cliff so it is not repeated down the wall
src[282:304, 36:58] = src[258:280, 36:58]

def mirror_extend(img, a, b, n, axis):
    """Repeat the strip [a, b) n times, alternating normal / mirrored. The strip must start and end on a smooth
    extremum of the pattern (shoreline trough / crest) so every junction is seamless and kink-free."""
    A = img if axis == 1 else img.transpose(1, 0, 2)
    normal = A[:, a:b]
    flipped = A[:, a + 1:b + 1][:, ::-1]
    parts = [A[:, :a]]
    for i in range(n):
        parts.append(normal if i % 2 == 0 else flipped)
    parts.append(A[:, b:])
    out = np.concatenate(parts, 1)
    return out if axis == 1 else out.transpose(1, 0, 2)


def join_v(U, D, ov):
    return join_h(U.transpose(1, 0, 2), D.transpose(1, 0, 2), ov).transpose(1, 0, 2)


SPLIT = 236   # source row below the wet-sand trough: above = sea/shoreline, below = dry sand + dune rim
RIM = 90      # width of the cliff/rim strips kept apart from the plain sand
OV_V = 10

# 1) sea + shoreline: mirror about the smooth crest/trough => scalloped bay without kinks (width 512 + 4*202 = 1320)
top = mirror_extend(src[:SPLIT + OV_V], 74, 276, 5, 1)

# 2) dry sand: loop + optimal seam (no symmetry); rims on both sides are mirrored vertically instead
sand = src[SPLIT:]                                   # 188 rows
interior = sand[:, RIM:512 - RIM]                    # plain sand, 332 wide
interior = extend(interior, TARGET_W - 2 * RIM + 32, 210, range(24, 49), 6, 86, 1)  # +32: the two 16 px seam overlaps
interior = extend(interior, 188 + 380, 76, range(20, 31), 10, 30, 0)
rim_n = 188 + 4 * 95
left = mirror_extend(sand[:, :RIM], 26, 121, 5, 0)
right = mirror_extend(sand[:, 512 - RIM:], 26, 121, 5, 0)
assert left.shape[0] == interior.shape[0] == right.shape[0] == rim_n, (left.shape, interior.shape, right.shape)
bottom = join_h(join_h(left, interior, 16), right, 16)
bottom = np.pad(bottom, ((0, 0), (0, TARGET_W - bottom.shape[1]), (0, 0)), mode="edge") if bottom.shape[1] < TARGET_W else bottom[:, :TARGET_W]

tall = join_v(top, bottom, OV_V)
if tall.shape[0] != TARGET_H:
    tall = tall[:TARGET_H] if tall.shape[0] > TARGET_H else np.pad(tall, ((0, TARGET_H - tall.shape[0]), (0, 0), (0, 0)), mode="edge")
print("composed", tall.shape, "bottom", bottom.shape)

# ---- sand marks: collect from the source, wipe the repeated ones, scatter new ones
srect = (90, 235, 440, 360)
smask, _ = marks(src, srect)
stamps = []
for pts in components(smask):
    if 2 <= len(pts) <= 70:
        ys, xs = zip(*pts)
        y0, y1, x0, x1 = min(ys), max(ys), min(xs), max(xs)
        if (y1 - y0) < 14 and (x1 - x0) < 18:
            patch = np.zeros((y1 - y0 + 1, x1 - x0 + 1), bool)
            for py, px in pts:
                patch[py - y0, px - x0] = True
            gy, gx = y0 + srect[1], x0 + srect[0]
            colors = src[gy: gy + patch.shape[0], gx: gx + patch.shape[1]].copy()
            stamps.append((patch, colors))
print("stamps", len(stamps))

H, W, _ = tall.shape
sand = (110, 300, W - 110, H - 70)  # dry sand inside the dune rim
fmask, med = marks(tall, sand)
fy0, fx0 = sand[1], sand[0]
sub = tall[sand[1]:sand[3], sand[0]:sand[2]]
sub[fmask] = med[fmask]
tall[sand[1]:sand[3], sand[0]:sand[2]] = sub

placed = []
tries = 0
while len(placed) < 26 and stamps and tries < 2000:
    tries += 1
    patch, colors = random.choice(stamps)
    x = random.randint(sand[0] + 20, sand[2] - 40)
    y = random.randint(sand[1] + 20, sand[3] - 40)
    if any(abs(x - px) < 90 and abs(y - py) < 70 for px, py in placed):
        continue
    if random.random() < 0.5:
        patch, colors = patch[:, ::-1], colors[:, ::-1]
    h2, w2 = patch.shape
    region = tall[y:y + h2, x:x + w2]
    region[patch] = colors[patch]
    placed.append((x, y))

print("result", tall.shape, "marks placed", len(placed))
Image.fromarray(np.clip(tall, 0, 255).astype(np.uint8)).save(OUT)
