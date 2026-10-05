"""Extend the Pixen-generated floating sky platform (512x424, Candidates/Base_1101.png) to the Wind arena map (1500x900) without
scaling pixels: loop plain floor windows (left arm / centre / right arm horizontally, top arm / middle band / bottom arm vertically),
join the copies along a minimum-cost seam, then mirror-pad the cloud sea to the final size.
Usage (from the project root): python Tools/arena_extend_sky.py [source.png] [out.png]
Source-coordinate windows (see the grid overlay): columns left 166-230, centre 272-358, right 398-452; rows top 96-140, middle 178-242,
bottom 296-316. Walkable cross in the OUTPUT image: see WALK_POLYGON in Assets/Editor/BossArenaWindBuilder.cs."""
import sys
import numpy as np
from PIL import Image

SRC = sys.argv[1] if len(sys.argv) > 1 else "Assets/Art/BossArena/Wind/Candidates/Base_1101.png"
OUT = sys.argv[2] if len(sys.argv) > 2 else "Assets/Art/BossArena/Wind/BaseMap_Wind_Empty_1500x900.png"
FINAL_W, FINAL_H = 1500, 900


def seam_path(A, B):
    d = np.sqrt(((A - B) ** 2).sum(2))
    lum = (A.mean(2) + B.mean(2)) / 2
    cost = d + 0.1 * lum
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


def grow(img, axis, a, b, ov, loops):
    """Insert `loops` copies of the window [a, b) along `axis` (1 = columns, 0 = rows)."""
    A = img if axis == 1 else img.transpose(1, 0, 2)
    p = b - a
    # the best loop start inside a slightly wider search band that keeps the window length
    best = (1e18, a)
    for x0 in range(a - 6, a + 7):
        x1 = x0 + p
        if x0 < 0 or x1 > A.shape[1] - 2:
            continue
        d = np.abs(A[:, x1 - ov:x1] - A[:, x0:x0 + ov]).mean()
        if d < best[0]:
            best = (d, x0)
    x0 = best[1]
    x1 = x0 + p
    print("axis", axis, "window", x0, x1, "ov", ov, "loops", loops, "diff %.2f" % best[0])
    out = A[:, :x1]
    for _ in range(loops - 1):
        out = join_h(out, A[:, x0:x1], ov)
    out = join_h(out, A[:, x0:], ov)
    return out if axis == 1 else out.transpose(1, 0, 2)


PASTE = (38, 55)


def platform_mask(img):
    """The platform = everything enclosed by the black outline that is not the connected sky/cloud region."""
    from collections import deque
    lum = img.mean(2)
    dark0 = lum < 62
    h, w = dark0.shape
    dark = dark0.copy()
    for _ in range(2):  # seal 1-3 px gaps of the outline for the flood fill
        d = dark.copy()
        d[1:] |= dark[:-1]
        d[:-1] |= dark[1:]
        d[:, 1:] |= dark[:, :-1]
        d[:, :-1] |= dark[:, 1:]
        dark = d
    sky = np.zeros((h, w), bool)
    seed = (30, 560)
    assert not dark[seed], "sky seed is on an outline"
    dq = deque([seed])
    sky[seed] = True
    while dq:
        y, x = dq.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < h and 0 <= nx < w and not sky[ny, nx] and not dark[ny, nx]:
                sky[ny, nx] = True
                dq.append((ny, nx))
    for _ in range(2):  # give the sealed ring back to the sky, but never cross a real outline pixel
        g = sky.copy()
        g[1:] |= sky[:-1]
        g[:-1] |= sky[1:]
        g[:, 1:] |= sky[:, :-1]
        g[:, :-1] |= sky[:, 1:]
        sky = g & ~dark0
    rest = ~sky
    plat = np.zeros((h, w), bool)
    seed = (h // 2, w // 2)
    dq = deque([seed])
    plat[seed] = True
    while dq:
        y, x = dq.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < h and 0 <= nx < w and rest[ny, nx] and not plat[ny, nx]:
                plat[ny, nx] = True
                dq.append((ny, nx))
    # the cliff faces under the two horizontal arms leak into the sky flood: take them back by colour (grey-brown stone)
    r, g, b = img[..., 0], img[..., 1], img[..., 2]
    stone = ((b - r) < 22) | dark0
    for x0, x1 in ((128, 516), (1070, 1395)):
        plat[628:672, x0:x1] |= stone[628:672, x0:x1]
    return plat


def build_sea(w, h):
    src = np.array(Image.open("Assets/Art/BossArena/Wind/Candidates/Sea_404.png").convert("RGB")).astype(np.float32)
    sh, sw = src.shape[:2]
    out = np.zeros((h, w, 3), np.float32)
    for ty in range(0, h, sh):
        for tx in range(0, w, sw):
            tile = src
            if (tx // sw) % 2:
                tile = tile[:, ::-1]
            if (ty // sh) % 2:
                tile = tile[::-1]
            hh, ww = min(sh, h - ty), min(sw, w - tx)
            out[ty:ty + hh, tx:tx + ww] = tile[:hh, :ww]
    return out


def main():
    img = np.array(Image.open(SRC).convert("RGB")).astype(np.float32)
    # columns, right to left so earlier windows keep their source coordinates
    img = grow(img, 1, 398, 452, 10, 5)    # right arm  +220
    img = grow(img, 1, 272, 358, 14, 6)    # centre     +432
    img = grow(img, 1, 166, 230, 12, 5)    # left arm   +260
    # rows, bottom to top
    img = grow(img, 0, 178, 242, 12, 5)    # middle     +260
    img = grow(img, 0, 96, 140, 10, 3)     # top arm    +102
    h, w = img.shape[:2]
    print("grown", w, h)
    mask = platform_mask(img)
    print("platform pixels", int(mask.sum()))
    sea = build_sea(FINAL_W, FINAL_H)
    # platform placed so the walkable cross is centred: the grown image is 1424 x 790, pasted at (38, 55)
    ox, oy = PASTE
    canvas = sea.copy()
    region = canvas[oy:oy + h, ox:ox + w]
    region[mask] = img[mask]
    Image.fromarray(np.clip(canvas, 0, 255).astype(np.uint8)).save(OUT)
    # layered outputs: the platform cut-out (RGBA, same 1500x900 canvas) and a horizontally seamless sea tile that scrolls in Unity
    rgba = np.zeros((FINAL_H, FINAL_W, 4), np.uint8)
    rgba[oy:oy + h, ox:ox + w, :3] = np.clip(img, 0, 255).astype(np.uint8)
    rgba[oy:oy + h, ox:ox + w, 3] = (mask * 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(OUT.replace("BaseMap_Wind_Empty", "Platform_Wind"))
    tile = build_sea(1024, FINAL_H)
    Image.fromarray(np.clip(tile, 0, 255).astype(np.uint8)).save(OUT.replace("BaseMap_Wind_Empty_1500x900", "SeaTile_Wind_1024x900"))
    print("saved", OUT, canvas.shape)


main()
