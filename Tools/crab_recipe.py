"""Builds pixelart_workbench `draw` recipes (layered cut-out animation, no AI generations) for the Water crab boss D.
Usage: python Tools/crab_recipe.py <clip>   (prints the minified recipe JSON; paste it as `--recipe`)
Clips: idle, move, snap, recovery. 9 frames each, canvas 320x320 (the 256 sprite sits at offset 32,32, centre preserved)."""
import json
import sys

OFF = 32
HEAD_POLY = None  # selections use rectangles (first match wins)

def rect(x0, y0, x1, y1):
    return [[x0, y0], [x1, y0], [x1, y1], [x0, y1]]


LARM = [[0, 153], [15, 151], [35, 152], [52, 161], [62, 173], [75, 183], [83, 190], [102, 211], [99, 226], [90, 243], [104, 255],
        [75, 255], [50, 252], [36, 240], [22, 252], [17, 255], [8, 252], [6, 231], [4, 218], [2, 211], [4, 196], [0, 181]]
RARM = [[172, 12], [188, 0], [215, 0], [242, 8], [256, 24], [256, 50], [256, 88], [256, 112], [252, 130], [238, 144],
        [222, 150], [197, 142], [195, 117], [193, 100], [183, 93], [172, 80], [174, 46], [197, 40]]
ANTL = [[88, 26], [116, 26], [116, 68], [104, 68], [104, 77], [88, 77]]  # antenna strands only: the dome of the head starts at y~67
ANTR = [[124, 26], [152, 26], [152, 81], [136, 81], [136, 68], [124, 68]]
HEAD = rect(86, 33, 154, 108)
LEGL, LEGR = rect(60, 176, 127, 254), rect(158, 168, 246, 254)

# polygon masks (follow the claw silhouettes; rectangles dragged leg/body pixels along and left flat cut edges)
PARTS = {
    "antl": {"include": [ANTL]},
    "antr": {"include": [ANTR]},
    "larm": {"include": [LARM]},
    "rarm": {"include": [RARM]},
    "head": {"include": [HEAD], "exclude": [ANTL, ANTR]},
    "legl": {"include": [LEGL], "exclude": [LARM]},
    "legr": {"include": [LEGR]},
    "rest": {"exclude": [ANTL, ANTR, LARM, RARM, HEAD]},  # legs stay in the base too, so a shifted leg block never opens a seam
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
        "larm": part("arms", "larm", [50, 146]),
        "rarm": part("arms", "rarm", [186, 125]),
        "head": part("head", "head", [120, 100]),
        "antl": part("ant", "antl", [101, 76]),
        "antr": part("ant", "antr", [138, 79]),
    }


def P(**kw):
    """pose helper: angles for larm/rarm/antl/antr/head, root=(dx,dy), legl/legr/head_at=(dx,dy)."""
    pose = {}
    for k in ("larm", "rarm", "antl", "antr"):
        if k in kw:
            pose[k] = {"angle": kw[k]}
    if "head" in kw:
        pose.setdefault("head", {})["angle"] = kw["head"]
    if "head_at" in kw:
        pose.setdefault("head", {})["at"] = at_root(*kw["head_at"])
        # the antennae grow out of the head: they follow it
        pose.setdefault("antl", {})["at"] = at_root(*kw["head_at"])
        pose.setdefault("antr", {})["at"] = at_root(*kw["head_at"])
    if "root" in kw:
        pose["root"] = {"at": [OFF + kw["root"][0], OFF + kw["root"][1]]}
    for k in ("legl", "legr"):
        if k in kw:
            pose[k] = {"at": at_root(*kw[k])}
    return pose


CLIPS = {
    "idle": (
        {"rest": P(), "a": P(larm=2, rarm=-2, antl=4, antr=-4, root=(0, -1)),
         "b": P(larm=3, rarm=-3, antl=7, antr=-7, root=(0, -2),),
         "c": P(larm=2, rarm=-2, antl=4, antr=-4, root=(0, -1)),
         "d": P(larm=-2, rarm=2, antl=-4, antr=4, root=(0, 1)),
         "e": P(larm=-3, rarm=3, antl=-7, antr=7, root=(0, 2),),
         "f": P(larm=-2, rarm=2, antl=-4, antr=4, root=(0, 1))},
        ["rest", "a", "b", "c", "rest", "d", "e", "f", "rest"],
        [110] * 9,
    ),
    "move": (
        {"rest": P(),
         "a": P(larm=6, rarm=6, antl=5, antr=5, root=(0, -2), legl=(2, 0), legr=(-2, 0), head_at=(0, -1)),
         "b": P(larm=3, rarm=3, antl=2, antr=2, root=(0, -3), legl=(0, -1), legr=(0, -1)),
         "c": P(larm=-4, rarm=-4, antl=-3, antr=-3, root=(0, -2), legl=(-2, 0), legr=(2, 0)),
         "d": P(larm=-6, rarm=-6, antl=-5, antr=-5, root=(0, 0), legl=(-2, 0), legr=(2, 0), head_at=(0, 1)),
         "e": P(larm=-3, rarm=-3, antl=-2, antr=-2, root=(0, 1), legl=(0, 1), legr=(0, 1)),
         "f": P(larm=4, rarm=4, antl=3, antr=3, root=(0, 0), legl=(2, 0), legr=(-2, 0))},
        ["rest", "a", "b", "c", "d", "e", "f", "a", "rest"],
        [80] * 9,
    ),
    "snap": (
        {"rest": P(),
         "w1": P(larm=10, rarm=8, root=(0, 1), head_at=(0, 1)),
         "w2": P(larm=22, rarm=20, root=(0, 2), head_at=(0, 2), antl=6, antr=-6),
         "w3": P(larm=34, rarm=30, root=(0, 3), head_at=(0, 2), antl=8, antr=-8),
         "w4": P(larm=40, rarm=38, root=(0, 3), head_at=(0, 3), antl=10, antr=-10),
         "s1": P(larm=-24, rarm=-30, root=(0, 7), head_at=(0, 5), antl=-8, antr=8),
         "s2": P(larm=-30, rarm=-40, root=(0, 8), head_at=(0, 6), antl=-10, antr=10),
         "s3": P(larm=-12, rarm=-20, root=(0, 4), head_at=(0, 2))},
        ["rest", "w1", "w2", "w3", "w4", "s1", "s2", "s3", "rest"],
        [90, 80, 80, 80, 260, 60, 110, 110, 100],
    ),
    "recovery": (
        {"rest": P(),
         "a": P(larm=4, rarm=-4, root=(0, 4), head_at=(0, 3), antl=-6, antr=6),
         "b": P(larm=8, rarm=-8, root=(0, 8), head_at=(0, 6), antl=-12, antr=12),
         "c": P(larm=12, rarm=-12, root=(0, 12), head_at=(0, 9), antl=-18, antr=18),
         "d": P(larm=14, rarm=-14, root=(0, 14), head_at=(0, 11), antl=-22, antr=22),
         "e": P(larm=15, rarm=-15, root=(0, 15), head_at=(0, 12), antl=-24, antr=24),
         "f": P(larm=16, rarm=-16, root=(0, 16), head_at=(0, 12), antl=-26, antr=26),
         "u1": P(larm=8, rarm=-8, root=(0, 9), head_at=(0, 6), antl=-12, antr=12),
         "u2": P(larm=3, rarm=-3, root=(0, 3), head_at=(0, 2), antl=-4, antr=4)},
        ["rest", "a", "b", "c", "d", "e", "f", "u1", "u2"],
        [90, 80, 80, 80, 90, 120, 300, 120, 100],
    ),
}


ALL_NODES = ("rest", "legl", "legr", "larm", "rarm", "head", "antl", "antr")


def Q(sink=0, opacity=255, **kw):
    """Burrow pose: the whole crab sinks `sink` px into the sand and fades to `opacity` (0-255)."""
    pose = P(**kw)
    pose["root"] = {"at": [OFF, OFF + sink]}
    if opacity != 255:
        for n in ALL_NODES:
            pose.setdefault(n, {})["opacity"] = opacity
    return pose


CLIPS["burrow"] = (
    {"rest": P(),
     "a": Q(4, 255, larm=8, rarm=-8, antl=-6, antr=6),
     "b": Q(14, 255, larm=14, rarm=-14, antl=-10, antr=10),
     "c": Q(28, 235, larm=20, rarm=-20, antl=-14, antr=14),
     "d": Q(44, 190, larm=24, rarm=-24, antl=-16, antr=16),
     "e": Q(60, 120, larm=26, rarm=-26, antl=-18, antr=18),
     "f": Q(76, 60, larm=26, rarm=-26, antl=-18, antr=18),
     "g": Q(92, 20, larm=26, rarm=-26, antl=-18, antr=18),
     "h": Q(110, 0, larm=26, rarm=-26, antl=-18, antr=18)},
    ["rest", "a", "b", "c", "d", "e", "f", "g", "h"],
    [70, 70, 70, 70, 70, 70, 70, 70, 70],
)
CLIPS["emerge"] = (
    {"rest": P(),
     "a": Q(96, 40, larm=24, rarm=-24, antl=-16, antr=16),
     "b": Q(70, 130, larm=22, rarm=-22, antl=-14, antr=14),
     "c": Q(46, 210, larm=14, rarm=-14, antl=-8, antr=8),
     "d": Q(24, 255, larm=-10, rarm=10, antl=6, antr=-6),
     "e": Q(8, 255, larm=-18, rarm=18, antl=10, antr=-10),
     "f": Q(-6, 255, larm=-8, rarm=8, antl=6, antr=-6),
     "g": Q(2, 255, larm=2, rarm=-2),
     "h": Q(0, 255)},
    ["a", "b", "c", "d", "e", "f", "g", "h", "rest"],
    [60, 60, 60, 70, 80, 80, 80, 80, 90],
)
CLIPS["spit"] = (
    {"rest": P(),
     "a": P(larm=-6, rarm=6, root=(0, 1), head_at=(0, 2)),
     "b": P(larm=-14, rarm=14, root=(0, 2), head_at=(0, 4), antl=6, antr=-6),
     "c": P(larm=-22, rarm=22, root=(0, 1), head_at=(0, 6), antl=10, antr=-10),
     "d": P(larm=-26, rarm=26, root=(0, 0), head_at=(0, 7), antl=12, antr=-12),
     "e": P(larm=-8, rarm=8, root=(0, 4), head_at=(0, -2), antl=-8, antr=8),
     "f": P(larm=-4, rarm=4, root=(0, 3), head_at=(0, -3), antl=-10, antr=10),
     "g": P(larm=-2, rarm=2, root=(0, 1), head_at=(0, -1))},
    ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
    [70, 70, 80, 100, 220, 70, 90, 90, 90],
)
CLIPS["whirl"] = (
    {"rest": P(),
     "a": P(larm=12, rarm=10, root=(0, -1), antl=4, antr=4),
     "b": P(larm=22, rarm=20, root=(-2, -2), antl=8, antr=8),
     "c": P(larm=30, rarm=28, root=(2, -3), antl=10, antr=10),
     "d": P(larm=38, rarm=34, root=(-2, -4), antl=12, antr=12),
     "e": P(larm=42, rarm=38, root=(2, -4), antl=14, antr=14),
     "f": P(larm=38, rarm=34, root=(-1, -3), antl=10, antr=10),
     "g": P(larm=22, rarm=20, root=(1, -1), antl=4, antr=4)},
    ["rest", "a", "b", "c", "d", "e", "f", "g", "rest"],
    [80, 80, 80, 80, 80, 80, 80, 80, 90],
)


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
    print(json.dumps(recipe(sys.argv[1]), separators=(",", ":")))
