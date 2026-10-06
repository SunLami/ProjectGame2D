"""Layered cut-out animation recipes (pixelart_workbench `draw`, no AI generations) for the Wind boss, the owl, base
Assets/Art/BossArena/Wind/Boss/OwlBase_256.png (256x256, wings raised in a V) on a 384x384 canvas (sprite at offset 64,64).
Usage: python Tools/owl_recipe.py <clip>            prints the minified recipe JSON
       python Tools/owl_recipe.py --preview <png> <out.png>   writes the part masks over the sprite (scratch check)
Angles are clockwise on screen. Left wing: + raises the tip (up), - lowers it. Right wing: the opposite sign.
W(a) raises both wings by `a` degrees (negative lowers both)."""
import json
import sys

OFF = 64
CANVAS = 384

LWING = [[0, 0], [62, 0], [62, 70], [86, 70], [96, 92], [90, 114], [84, 176], [60, 200], [56, 256], [0, 256]]
RWING = [[256 - x, y] for x, y in LWING]
HEAD = [[64, 0], [192, 0], [192, 122], [64, 122]]

PARTS = {
    "lwing": {"include": [LWING]},
    "rwing": {"include": [RWING]},
    "head": {"include": [HEAD], "exclude": [LWING, RWING]},
    # the body keeps the neck/ruff strip (y 110-122) that the head also contains, so a lifted head never opens a gap
    "rest": {"exclude": [LWING, RWING, [[64, 0], [192, 0], [192, 110], [64, 110]]]},
}

LAYERS = [("wings", "Body - wings"), ("body", "Body - base"), ("head", "Body - head")]
ALL_NODES = ("rest", "lwing", "rwing", "head")


def at_root(dx=0, dy=0):
    return {"node": "root", "point": [dx, dy]}


def nodes():
    anchor = at_root()

    def part(layer, name, pivot=None):
        n = {"layer": layer, "draw": [{"op": "copy", "part": name, "method": "scale2x8"}] if pivot else [{"op": "copy", "part": name}],
             "at": anchor}
        if pivot:
            n["pivot"] = pivot
        return n

    return {
        "root": {"layer": "body", "draw": [], "at": [OFF, OFF]},
        "rest": part("body", "rest"),
        "lwing": part("wings", "lwing", [84, 96]),
        "rwing": part("wings", "rwing", [172, 96]),
        "head": part("head", "head", [128, 100]),
    }


def P(lw=None, rw=None, head=None, head_at=None, root=None, opacity=255):
    pose = {}
    if lw is not None:
        pose["lwing"] = {"angle": lw}
    if rw is not None:
        pose["rwing"] = {"angle": rw}
    if head is not None:
        pose.setdefault("head", {})["angle"] = head
    if head_at is not None:
        pose.setdefault("head", {})["at"] = at_root(*head_at)
    if root is not None:
        pose["root"] = {"at": [OFF + root[0], OFF + root[1]]}
    if opacity != 255:
        for n in ALL_NODES:
            pose.setdefault(n, {})["opacity"] = opacity
    return pose


def W(a, **kw):
    """Both wings raised by `a` degrees (negative = lowered)."""
    return P(lw=a, rw=-a, **kw)


def clip(poses, order, durations):
    return poses, order, durations


CLIPS = {
    "idle": clip(
        {"rest": P(), "a": W(8, root=(0, -1)), "b": W(18, root=(0, -3), head_at=(0, -1)), "c": W(10, root=(0, -2)),
         "d": W(-8, root=(0, 1)), "e": W(-18, root=(0, 3), head_at=(0, 1)), "f": W(-9, root=(0, 2))},
        ["rest", "a", "b", "c", "rest", "d", "e", "f", "rest"],
        [120] * 9),
    "move": clip(
        {"rest": P(), "a": W(24, root=(0, -4), head_at=(0, -2)), "b": W(4, root=(0, -2)), "c": W(-30, root=(0, 4), head_at=(0, 2)),
         "d": W(-14, root=(0, 2)), "e": W(26, root=(0, -4), head_at=(0, -2)), "f": W(0, root=(0, -2))},
        ["rest", "a", "b", "c", "d", "e", "f", "a", "rest"],
        [70] * 9),
    "volley": clip(
        {"rest": P(), "w1": W(14, root=(0, -2)), "w2": W(26, root=(0, -4), head_at=(0, -1)), "w3": W(34, root=(0, -5), head_at=(0, -2)),
         "f1": W(-24, root=(0, 5), head_at=(0, 4)), "f2": W(-46, root=(0, 8), head_at=(0, 6)), "f3": W(-34, root=(0, 6), head_at=(0, 4)),
         "r1": W(-8, root=(0, 2), head_at=(0, 1))},
        ["rest", "w1", "w2", "w3", "w3", "f1", "f2", "f3", "r1"],
        [90, 80, 80, 80, 260, 50, 150, 110, 100]),
    "takeoff": clip(
        {"rest": P(), "c1": W(-14, root=(0, 6), head_at=(0, 4)), "c2": W(-22, root=(0, 10), head_at=(0, 7)),
         "u1": W(30, root=(0, -4), head_at=(0, -2)), "u2": W(-30, root=(0, -12)), "u3": W(36, root=(0, -22), opacity=220),
         "u4": W(-32, root=(0, -34), opacity=150), "u5": W(38, root=(0, -48), opacity=70), "u6": W(-30, root=(0, -64), opacity=0)},
        ["rest", "c1", "c2", "u1", "u2", "u3", "u4", "u5", "u6"],
        [90, 80, 90, 70, 70, 70, 70, 70, 70]),
    "dive": clip(
        {"rest": P(), "a": W(-44, root=(0, -2), head_at=(0, 3), opacity=235), "b": W(-52, root=(0, -3), head_at=(0, 4)),
         "c": W(-58, root=(0, -1), head_at=(0, 5)), "d": W(-60, root=(0, 1), head_at=(0, 6)), "e": W(-58, root=(1, 0), head_at=(0, 5)),
         "f": W(-60, root=(-1, 1), head_at=(0, 6)), "g": W(-24, root=(0, 4), head_at=(0, 3)), "h": W(8, root=(0, 5), head_at=(0, 4))},
        ["a", "b", "c", "d", "e", "f", "g", "h", "rest"],
        [80, 70, 70, 70, 70, 70, 70, 110, 120]),
    "perch": clip(
        {"rest": P(), "a": W(8, root=(0, 4), head_at=(0, 3)), "b": W(-16, root=(0, 7), head_at=(0, 6)), "c": W(-30, root=(0, 10), head_at=(0, 9)),
         "d": W(-42, root=(0, 12), head_at=(0, 11)), "e": W(-48, root=(0, 13), head_at=(0, 12)), "f": W(-44, root=(0, 12), head_at=(0, 11)),
         "g": W(-46, root=(0, 13), head_at=(0, 12))},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "e"],
        [100, 80, 90, 100, 120, 200, 300, 300, 200]),
    "flap": clip(
        {"rest": P(), "a": P(lw=26, rw=22, root=(0, -2), head_at=(0, -1)), "b": P(lw=-18, rw=-30, root=(0, -3)),
         "c": P(lw=-26, rw=-22, root=(0, -1), head_at=(0, 1)), "d": P(lw=22, rw=30, root=(0, -3)),
         "e": P(lw=28, rw=24, root=(0, -2), head_at=(0, -1)), "f": P(lw=-20, rw=-28, root=(0, -2))},
        ["rest", "a", "b", "c", "d", "e", "f", "a", "rest"],
        [60] * 9),
    "sweep": clip(
        {"rest": P(), "a": W(-6, root=(0, 1)), "b": W(-14, root=(-2, 2), head=-3),
         "c": P(lw=-30, rw=-30, root=(-4, 3), head=-5), "d": P(lw=-10, rw=-10, root=(2, 2), head=2),
         "e": P(lw=34, rw=34, root=(8, 0), head=7), "f": P(lw=26, rw=26, root=(5, 0), head=4),
         "g": P(lw=8, rw=8, root=(1, 0))},
        ["rest", "a", "b", "c", "c", "d", "e", "f", "g"],
        [90, 80, 80, 80, 230, 60, 120, 110, 100]),
    "slash": clip(
        {"rest": P(), "a": P(lw=18, rw=-8, root=(-2, -2)), "b": P(lw=32, rw=-14, root=(-3, -3), head=-3),
         "c": P(lw=-46, rw=-22, root=(3, 4), head=4), "d": P(lw=-30, rw=26, root=(4, 5), head=5),
         "e": P(lw=-12, rw=-34, root=(0, 2)), "f": P(lw=22, rw=44, root=(-3, 4), head=-4),
         "g": P(lw=6, rw=8, root=(0, 1))},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
        [80, 80, 100, 60, 110, 80, 120, 100, 90]),
    "circle": clip(
        {"rest": P(), "a": W(26, root=(0, -2), head=-3), "b": W(32, root=(-1, -3), head=-5), "c": W(26, root=(0, -3), head=-3),
         "d": W(18, root=(1, -1), head=0), "e": W(12, root=(2, 0), head=3), "f": W(20, root=(1, -1), head=5),
         "g": W(28, root=(0, -2), head=3)},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
        [80] * 9),
    "screech": clip(
        {"rest": P(), "a": W(18, root=(0, -2), head_at=(0, -3)), "b": W(30, root=(0, -4), head_at=(0, -5), head=0),
         "c": W(40, root=(-1, -5), head_at=(0, -6)), "d": W(36, root=(1, -5), head_at=(0, -6)),
         "e": W(42, root=(-1, -6), head_at=(0, -7)), "f": W(36, root=(1, -5), head_at=(0, -6)),
         "g": W(20, root=(0, -3), head_at=(0, -4))},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
        [90, 80, 80, 70, 70, 70, 70, 90, 100]),
    "death": clip(
        {"rest": P(), "a": W(-6, root=(0, 3), head_at=(0, 2)), "b": W(-18, root=(0, 7), head_at=(0, 5)), "c": W(-32, root=(0, 11), head_at=(0, 9)),
         "d": W(-46, root=(0, 15), head_at=(0, 12)), "e": W(-58, root=(0, 18), head_at=(0, 14), opacity=235),
         "f": W(-64, root=(0, 20), head_at=(0, 15), opacity=190), "g": W(-66, root=(0, 21), head_at=(0, 16), opacity=120),
         "h": W(-68, root=(0, 22), head_at=(0, 16), opacity=40)},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "h"],
        [90, 90, 90, 100, 110, 130, 150, 160, 160]),
}


def recipe(name):
    poses, frames, durations = CLIPS[name]
    return {
        "job": {"canvas": [CANVAS, CANVAS], "frame_count": len(frames), "durations_ms": durations,
                "views": [{"id": "south", "offset": [OFF, OFF]}], "source_bookends_exact": frames[0] == "rest"},
        "scene": {
            "palette": {"dark": "#112143"},
            "layers": [{"id": i, "name": n} for i, n in LAYERS],
            "views": {"south": {"parts": PARTS, "nodes": nodes(), "poses": poses, "frames": frames}},
        },
    }


if __name__ == "__main__":
    if sys.argv[1] == "--preview":
        from PIL import Image, ImageDraw
        src = Image.open(sys.argv[2]).convert("RGBA")
        big = Image.new("RGBA", (1024, 1024), (110, 110, 140, 255))
        big.alpha_composite(src.resize((1024, 1024), Image.NEAREST))
        d = ImageDraw.Draw(big, "RGBA")
        for name, poly, c in (("lwing", LWING, (255, 0, 0, 70)), ("rwing", RWING, (0, 0, 255, 70)), ("head", HEAD, (0, 255, 0, 40))):
            d.polygon([(x * 4, y * 4) for x, y in poly], fill=c, outline=c[:3] + (255,))
        big.save(sys.argv[3])
    else:
        print(json.dumps(recipe(sys.argv[1]), separators=(",", ":")))
