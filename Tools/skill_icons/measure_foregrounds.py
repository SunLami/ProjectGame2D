"""Measure alpha bounds/hulls for Unity import without changing any image pixels."""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Assets/Resources/UI/Skills/Icons/V3Foreground"
OUTPUT = ROOT / "Assets/Documentation/DevelopmentPlan/SkillIconForegroundMeasurements.json"


def convex_hull(points):
    points = sorted(set(points))
    def cross(o, a, b):
        return (a[0]-o[0])*(b[1]-o[1]) - (a[1]-o[1])*(b[0]-o[0])
    lower, upper = [], []
    for point in points:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], point) <= 0:
            lower.pop()
        lower.append(point)
    for point in reversed(points):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], point) <= 0:
            upper.pop()
        upper.append(point)
    return lower[:-1] + upper[:-1]


def measure(path):
    image = Image.open(path).convert("RGBA")
    alpha = image.getchannel("A")
    if alpha.getextrema()[0] != 0:
        raise ValueError(f"Not transparent: {path}")
    # Ignore only near-invisible extraction fringe, then retain every pixel above this alpha.
    mask = alpha.point(lambda value: 255 if value > 16 else 0)
    left, top, right, bottom = mask.getbbox()
    points = []
    pixels = mask.load()
    for y in range(top, bottom):
        xs = [x for x in range(left, right) if pixels[x, y]]
        if not xs:
            continue
        for x in (xs[0], xs[-1]):
            points.extend(((x, y), (x+1, y), (x, y+1), (x+1, y+1)))
    width, height = right-left, bottom-top
    extent = max(width, height)
    cx, cy = (left+right)/2, (top+bottom)/2
    hull = [{"x": round((x-cx)/extent, 8), "y": round((cy-y)/extent, 8)}
            for x, y in convex_hull(points)]
    return {"name": path.stem,
            "rect": {"x": left, "y": image.height-bottom,
                     "width": width, "height": height}, "hull": hull}


if __name__ == "__main__":
    paths = sorted(FOLDER.glob("*.png"))
    if len(paths) != 13:
        raise ValueError(f"Expected 13 motifs, found {len(paths)}")
    OUTPUT.write_text(json.dumps({"icons": [measure(path) for path in paths]}, indent=2)+"\n")
    print(f"Measured 13 foregrounds: {OUTPUT}")
