"""Local renderer for the owl boss clips (no Pixellab calls). v2 (2026-10-05): real flapping instead of a rigid swing.

* Each wing is a 3-segment chain (inner / mid / tip) with forward kinematics: the inner segment follows the keyframe angle, the mid and
  tip segments LAG behind the motion (the tip whips after the beat), so the wing bends like a wing instead of turning like a door.
* Foreshortening: a wing that swings toward / away from the camera is narrower on screen (scale X about the shoulder), which is what
  sells a top-down wing beat.
* Follow-through: the tail trails the body's vertical motion, the head counter-moves, and the body squashes a little on the beats.
* Keyframes (owl_recipe.CLIPS) are interpolated with Catmull-Rom; every clip is FRAMES long (keyframes at even indices stay exact).
* Rotation / scaling is done at 4x with nearest sampling and sampled back down, which keeps the pixel-art edges hard.

Usage (project root): python Tools/owl_render.py [Clip ...]   ->   Assets/Art/BossArena/Wind/Boss/Anim/Owl_<Clip>_17f.png
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
import owl_recipe as base

SRC = "Assets/Art/BossArena/Wind/Boss/OwlBase_256.png"
OUT = "Assets/Art/BossArena/Wind/Boss/Anim"
OFF, CANVAS, S = base.OFF, base.CANVAS, 4
FRAMES = 17
NECK_CUT = [[64, 0], [192, 0], [192, 110], [64, 110]]
TAIL = [[70, 214], [186, 214], [186, 256], [70, 256]]
SHOULDER_L, JOINT2_L, JOINT3_L = (84, 96), (56, 108), (28, 112)
TAIL_PIVOT = (128, 206)


def mirror_pt(p):
    return (256 - p[0], p[1])


def poly_mask(include, exclude=()):
    m = Image.new("L", (256, 256), 0)
    d = ImageDraw.Draw(m)
    d.polygon([tuple(p) for p in include], fill=255)
    for ex in exclude:
        d.polygon([tuple(p) for p in ex], fill=0)
    return m


def cut(img, m, rect=None):
    arr = np.asarray(img).copy()
    mm = np.asarray(m).copy()
    if rect is not None:
        x0, x1 = rect
        keep = np.zeros_like(mm)
        keep[:, x0:x1] = 255
        mm = np.minimum(mm, keep)
    arr[..., 3] = np.minimum(arr[..., 3], mm)
    return Image.fromarray(arr, "RGBA")


def big_layer(part):
    """256 px part -> 4x layer on the 4x canvas (the part sits at OFF,OFF)."""
    layer = Image.new("RGBA", (CANVAS * S, CANVAS * S), (0, 0, 0, 0))
    layer.paste(part.resize((256 * S, 256 * S), Image.NEAREST), (OFF * S, OFF * S))
    return layer


def rot(layer, angle, pivot):
    if abs(angle) < 0.05:
        return layer
    return layer.rotate(-angle, resample=Image.NEAREST, center=((OFF + pivot[0]) * S, (OFF + pivot[1]) * S))


def scale_x(layer, sx, x_pivot):
    """Foreshortening: scales X about the vertical line through `x_pivot`."""
    if abs(sx - 1) < 0.005:
        return layer
    cx = (OFF + x_pivot) * S
    return layer.transform(layer.size, Image.AFFINE, (1 / sx, 0, cx - cx / sx, 0, 1, 0), resample=Image.NEAREST)


def down(layer):
    return Image.fromarray(np.asarray(layer)[S // 2::S, S // 2::S], "RGBA")


def shift(layer, dx, dy):
    out = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    out.paste(layer, (int(round(dx)) * S, int(round(dy)) * S))
    return out


def state_of(pose):
    rx = ry = 0
    if "root" in pose:
        rx, ry = pose["root"]["at"][0] - OFF, pose["root"]["at"][1] - OFF
    head = pose.get("head", {})
    hy = head["at"]["point"][1] if "at" in head else 0
    return np.array([pose.get("lwing", {}).get("angle", 0.0), pose.get("rwing", {}).get("angle", 0.0), head.get("angle", 0.0), hy, rx, ry,
                     pose.get("rest", {}).get("opacity", 255)], float)


def spline(keys, t):
    i = min(int(np.floor(t)), len(keys) - 2)
    u = t - i
    p0, p1, p2, p3 = keys[max(i - 1, 0)], keys[i], keys[i + 1], keys[min(i + 2, len(keys) - 1)]
    return 0.5 * ((2 * p1) + (-p0 + p2) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u + (-p0 + 3 * p1 - 3 * p2 + p3) * u ** 3)


def clip_states(name):
    poses, order, _ = base.CLIPS[name]
    keys = [state_of(poses[o]) for o in order]
    out = []
    for f in range(FRAMES):
        t = f * (len(keys) - 1) / (FRAMES - 1)
        out.append(keys[f // 2].copy() if f % 2 == 0 else spline(keys, t))
    return out


class Rig:
    def __init__(self, src):
        lw = poly_mask(base.LWING)
        rw = poly_mask(base.RWING)
        self.wings = {}
        for side, mask, sign in (("l", lw, 1), ("r", rw, -1)):
            # the segments overlap by ~7 px so a bend never opens a gap at a joint
            xs = [(47, 256), (21, 61), (0, 35)] if side == "l" else [(0, 209), (195, 235), (221, 256)]
            self.wings[side] = [big_layer(cut(src, mask, rect)) for rect in xs]
        self.head = big_layer(cut(src, poly_mask(base.HEAD, [base.LWING, base.RWING])))
        self.tail = big_layer(cut(src, poly_mask(TAIL, [base.LWING, base.RWING])))
        self.body = big_layer(cut(src, poly_mask([[0, 0], [256, 0], [256, 256], [0, 256]], [base.LWING, base.RWING, NECK_CUT, TAIL])))

    def wing(self, side, a1, a2, a3):
        j1 = SHOULDER_L if side == "l" else mirror_pt(SHOULDER_L)
        j2 = JOINT2_L if side == "l" else mirror_pt(JOINT2_L)
        j3 = JOINT3_L if side == "l" else mirror_pt(JOINT3_L)
        inner, mid, tip = self.wings[side]
        tip = rot(rot(rot(tip, a3, j3), a2, j2), a1, j1)
        mid = rot(rot(mid, a2, j2), a1, j1)
        inner = rot(inner, a1, j1)
        out = Image.alpha_composite(Image.alpha_composite(tip, mid), inner)
        sx = 1 - 0.0055 * min(abs(a1), 70)  # foreshortening: the wing is narrower while it swings off the rest pose
        return scale_x(out, sx, j1[0])


def render_clip(name, rig):
    states = clip_states(name)
    strip = Image.new("RGBA", (CANVAS * FRAMES, CANVAS), (0, 0, 0, 0))
    for f, st in enumerate(states):
        lw, rw, ha, hy, rx, ry, op = st
        prev = states[f - 1] if f > 0 else st
        vl, vr = lw - prev[0], rw - prev[1]
        bend = lambda v: (max(-7.0, min(7.0, -0.55 * v)), max(-13.0, min(13.0, -1.0 * v)))
        l2, l3 = bend(vl)
        r2, r3 = bend(vr)
        # a little squash on the downward beats, tail trailing the vertical motion, head counter-rotating
        vy = ry - prev[5]
        tail_angle = max(-9.0, min(9.0, 1.4 * vy))
        wl = rig.wing("l", lw, l2, l3)
        wr = rig.wing("r", rw, r2, r3)
        head = rot(rig.head, ha - 0.25 * (vl - vr) * 0.1, (128, 100))
        tail = rot(rig.tail, tail_angle, TAIL_PIVOT)
        frame = Image.new("RGBA", (CANVAS * S, CANVAS * S), (0, 0, 0, 0))
        for layer, (dx, dy) in ((wl, (rx, ry)), (wr, (rx, ry)), (tail, (rx, ry)), (rig.body, (rx, ry)), (head, (rx, ry + hy))):
            frame.alpha_composite(shift(layer, dx, dy))
        small = down(frame)
        if op < 254:
            arr = np.asarray(small).copy()
            arr[..., 3] = (arr[..., 3].astype(np.float32) * max(0.0, op) / 255).astype(np.uint8)
            small = Image.fromarray(arr, "RGBA")
        strip.paste(small, (f * CANVAS, 0))
    return strip


def main():
    src = Image.open(SRC).convert("RGBA")
    rig = Rig(src)
    names = {"idle": "Idle", "move": "Move", "volley": "Volley", "takeoff": "TakeOff", "dive": "Dive", "perch": "Perch", "flap": "Flap",
             "sweep": "Sweep", "slash": "Slash", "circle": "Circle", "screech": "Screech", "death": "Death"}
    only = {a.lower() for a in sys.argv[1:]}
    os.makedirs(OUT, exist_ok=True)
    for key, title in names.items():
        if only and key not in only:
            continue
        strip = render_clip(key, rig)
        path = f"{OUT}/Owl_{title}_17f.png"
        strip.save(path)
        print(path, strip.size)


if __name__ == "__main__":
    main()
