"""Layered cut-out animation recipes (pixelart_workbench `draw`, no AI generations) for the NEW Water crab base: both claws raised
(Assets/Art/BossArena/Water/Boss/CrabUp_256.png, 256x256 -> 320x320 canvas with the sprite at offset 32,32).
Usage: python Tools/crab_up_recipe.py <clip>      prints the minified recipe JSON
       python Tools/crab_up_recipe.py --preview   writes the part masks over the sprite (scratch check)
Angles: clockwise positive. The left claw rotates TOWARD the centre with +, the right claw with - ; S(a) moves both toward the centre
by `a` degrees (negative = both spread outward)."""
import json
import sys

OFF = 32


def rect(x0, y0, x1, y1):
    return [[x0, y0], [x1, y0], [x1, y1], [x0, y1]]


LARM = [[20, 6], [114, 4], [114, 56], [100, 64], [100, 100], [70, 104], [68, 130], [40, 130], [38, 112], [24, 102], [20, 72]]
RARM = [[236, 6], [142, 4], [142, 56], [156, 64], [156, 100], [186, 104], [188, 130], [216, 130], [218, 112], [232, 102], [236, 72]]
ANTL = [[102, 62], [126, 62], [126, 98], [112, 100], [102, 98]]
ANTR = [[130, 62], [154, 62], [154, 98], [144, 100], [130, 98]]
HEAD = rect(98, 90, 158, 144)
LEGL = rect(30, 160, 98, 254)
LEGR = rect(158, 160, 226, 254)

PARTS = {
    "antl": {"include": [ANTL]},
    "antr": {"include": [ANTR]},
    "larm": {"include": [LARM]},
    "rarm": {"include": [RARM]},
    "head": {"include": [HEAD], "exclude": [ANTL, ANTR, LARM, RARM]},
    "legl": {"include": [LEGL]},
    "legr": {"include": [LEGR]},
    "rest": {"exclude": [ANTL, ANTR, LARM, RARM, HEAD]},  # legs also stay in the base so a shifted leg never opens a seam
}

LAYERS = [("fill", "Body - fill"), ("ant", "Body - antennae"), ("legs", "Body - legs"), ("body", "Body - base"),
          ("head", "Body - head"), ("arms", "Body - claws")]


def at_root(dx=0, dy=0):
    return {"node": "root", "point": [dx, dy]}


def nodes():
    anchor = at_root()

    def part(layer, name, pivot=None):
        n = {"layer": layer, "draw": [{"op": "copy", "part": name}], "at": anchor}
        if pivot:
            n["pivot"] = pivot
        return n

    return {
        "root": {"layer": "body", "draw": [], "at": [OFF, OFF]},
        "rest": part("body", "rest"),
        "legl": part("legs", "legl"),
        "legr": part("legs", "legr"),
        "larm": part("arms", "larm", [54, 124]),
        "rarm": part("arms", "rarm", [202, 124]),
        "head": part("head", "head", [128, 128]),
        "antl": part("ant", "antl", [114, 98]),
        "antr": part("ant", "antr", [142, 98]),
    }


def P(**kw):
    pose = {}
    for k in ("larm", "rarm", "antl", "antr"):
        if k in kw:
            pose[k] = {"angle": kw[k]}
    if "head" in kw:
        pose.setdefault("head", {})["angle"] = kw["head"]
    if "head_at" in kw:
        pose.setdefault("head", {})["at"] = at_root(*kw["head_at"])
        pose.setdefault("antl", {})["at"] = at_root(*kw["head_at"])
        pose.setdefault("antr", {})["at"] = at_root(*kw["head_at"])
    if "root" in kw:
        pose["root"] = {"at": [OFF + kw["root"][0], OFF + kw["root"][1]]}
    for k in ("legl", "legr"):
        if k in kw:
            pose[k] = {"at": at_root(*kw[k])}
    return pose


def S(a, **kw):
    """Both claws toward the centre by `a` degrees (negative = spread outward)."""
    return P(larm=a, rarm=-a, **kw)


ALL_NODES = ("rest", "legl", "legr", "larm", "rarm", "head", "antl", "antr")


def Q(sink=0, opacity=255, a=-20, **kw):
    pose = S(a, **kw)
    pose["root"] = {"at": [OFF, OFF + sink]}
    if opacity != 255:
        for n in ALL_NODES:
            pose.setdefault(n, {})["opacity"] = opacity
    return pose


CLIPS = {
    "idle": (
        {"rest": P(), "a": S(-3, root=(0, -1)), "b": S(-8, root=(0, -2), head_at=(0, -1)), "c": S(-3, root=(0, -1)),
         "d": S(4, root=(0, 1)), "e": S(9, root=(0, 2), head_at=(0, 1)), "f": S(4, root=(0, 1))},
        ["rest", "a", "b", "c", "rest", "d", "e", "f", "rest"],
        [110] * 9,
    ),
    "move": (
        {"rest": P(),
         "a": P(larm=-6, rarm=6, root=(0, -2), legl=(2, 0), legr=(-2, 0), head_at=(0, -1)),
         "b": P(larm=-3, rarm=3, root=(0, -3), legl=(0, -1), legr=(0, -1)),
         "c": P(larm=4, rarm=-4, root=(0, -2), legl=(-2, 0), legr=(2, 0)),
         "d": P(larm=6, rarm=-6, root=(0, 0), legl=(-2, 0), legr=(2, 0), head_at=(0, 1)),
         "e": P(larm=3, rarm=-3, root=(0, 1), legl=(0, 1), legr=(0, 1)),
         "f": P(larm=-4, rarm=4, root=(0, 0), legl=(2, 0), legr=(-2, 0))},
        ["rest", "a", "b", "c", "d", "e", "f", "a", "rest"],
        [80] * 9,
    ),
    "snap": (
        {"rest": P(), "w1": S(-6, root=(0, 1)), "w2": S(-12, root=(0, 2), head_at=(0, 1)),
         "w3": S(-18, root=(0, 3), head_at=(0, 2)), "w4": S(-22, root=(0, 3), head_at=(0, 3)),
         "s1": S(26, root=(0, 7), head_at=(0, 5)), "s2": S(40, root=(0, 8), head_at=(0, 6)), "s3": S(14, root=(0, 4), head_at=(0, 2))},
        ["rest", "w1", "w2", "w3", "w4", "s1", "s2", "s3", "rest"],
        [90, 80, 80, 80, 260, 60, 110, 110, 100],
    ),
    "spit": (
        {"rest": P(), "a": S(6, root=(0, 1), head_at=(0, 2)), "b": S(16, root=(0, 2), head_at=(0, 5)),
         "c": S(28, root=(0, 2), head_at=(0, 7)), "d": S(32, root=(0, 1), head_at=(0, 8)),
         "e": S(10, root=(0, 5), head_at=(0, -3)), "f": S(4, root=(0, 4), head_at=(0, -3)), "g": S(0, root=(0, 1), head_at=(0, -1))},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
        [70, 70, 80, 100, 220, 70, 90, 90, 90],
    ),
    "whirl": (
        {"rest": P(), "a": P(larm=8, rarm=8, root=(0, -1)), "b": P(larm=22, rarm=22, root=(-2, -2)),
         "c": P(larm=-22, rarm=-22, root=(2, -3)), "d": P(larm=32, rarm=32, root=(-2, -4)),
         "e": P(larm=-32, rarm=-32, root=(2, -4)), "f": P(larm=18, rarm=18, root=(-1, -3)), "g": P(larm=-10, rarm=-10, root=(1, -1))},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
        [80, 80, 80, 80, 80, 80, 80, 80, 90],
    ),
    "slam": (
        {"rest": P(), "w1": S(-8, root=(0, -2)), "w2": S(-16, root=(0, -4), head_at=(0, -1)),
         "w3": S(-20, root=(0, -5), head_at=(0, -2)), "s1": S(30, root=(0, 6), head_at=(0, 4)),
         "s2": S(50, root=(0, 12), head_at=(0, 8)), "s3": S(40, root=(0, 10), head_at=(0, 7)), "r1": S(14, root=(0, 4), head_at=(0, 2))},
        ["rest", "w1", "w2", "w3", "w3", "s1", "s2", "s3", "r1"],
        [90, 80, 80, 80, 200, 60, 200, 110, 100],
    ),
    "resonance": (
        {"rest": P(), "a": S(-10, root=(0, -2), head_at=(0, -2)), "b": S(-18, root=(-1, -4), head_at=(0, -3)),
         "c": S(-20, root=(1, -4), head_at=(0, -3)), "d": S(-18, root=(-1, -4), head_at=(0, -3)),
         "e": S(-20, root=(1, -5), head_at=(0, -4)), "f": S(-14, root=(0, -3), head_at=(0, -2)), "g": S(-6, root=(0, -1))},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
        [90, 80, 70, 70, 70, 70, 80, 90, 100],
    ),
    "recovery": (
        {"rest": P(), "a": S(-6, root=(0, 4), head_at=(0, 3)), "b": S(-14, root=(0, 8), head_at=(0, 6)),
         "c": S(-24, root=(0, 12), head_at=(0, 9)), "d": S(-34, root=(0, 14), head_at=(0, 11)),
         "e": S(-42, root=(0, 15), head_at=(0, 12)), "f": S(-46, root=(0, 16), head_at=(0, 12)),
         "u1": S(-26, root=(0, 9), head_at=(0, 6)), "u2": S(-8, root=(0, 3), head_at=(0, 2))},
        ["rest", "a", "b", "c", "d", "e", "f", "u1", "u2"],
        [90, 80, 80, 80, 90, 120, 300, 120, 100],
    ),
    "burrow": (
        {"rest": P(), "a": Q(4, 255, -6), "b": Q(14, 255, -12), "c": Q(28, 235, -18), "d": Q(44, 190, -24),
         "e": Q(60, 120, -28), "f": Q(76, 60, -30), "g": Q(92, 20, -30), "h": Q(110, 0, -30)},
        ["rest", "a", "b", "c", "d", "e", "f", "g", "h"],
        [70] * 9,
    ),
}


def recipe(clip):
    poses, frames, durations = CLIPS[clip]
    return {
        "job": {"canvas": [320, 320], "frame_count": len(frames), "durations_ms": durations,
                "views": [{"id": "south", "offset": [OFF, OFF]}], "source_bookends_exact": True},
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
        big = Image.new("RGBA", (1024, 1024), (214, 190, 140, 255))
        big.alpha_composite(src.resize((1024, 1024), Image.NEAREST))
        d = ImageDraw.Draw(big, "RGBA")
        colors = {"larm": (255, 0, 0, 70), "rarm": (0, 0, 255, 70), "head": (0, 255, 0, 70), "antl": (255, 255, 0, 120),
                  "antr": (255, 0, 255, 120), "legl": (0, 255, 255, 60), "legr": (255, 128, 0, 60)}
        for name, poly in (("larm", LARM), ("rarm", RARM), ("head", HEAD), ("antl", ANTL), ("antr", ANTR), ("legl", LEGL), ("legr", LEGR)):
            d.polygon([(x * 4, y * 4) for x, y in poly], fill=colors[name], outline=colors[name][:3] + (255,))
        big.save(sys.argv[3])
    else:
        print(json.dumps(recipe(sys.argv[1]), separators=(",", ":")))
