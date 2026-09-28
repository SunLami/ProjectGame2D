from pathlib import Path
from PIL import Image, ImageDraw, ImageStat

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Resources/UI/Gameplay/UnifiedHUD/DarkInventoryStyle"
GEN = Path(r"C:/Users/havin/.codex/generated_images/01a0e123-d0a1-7822-869d-def07db08c1e")


def fit_icon(source: Path, target: Path, size: tuple[int, int], coverage: float = 0.90) -> None:
    image = Image.open(source).convert("RGBA")
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError(f"No alpha content in {source}")
    content = image.crop(bbox)
    max_w, max_h = int(size[0] * coverage), int(size[1] * coverage)
    scale = min(max_w / content.width, max_h / content.height)
    logical = content.resize((max(1, round(content.width * scale / 4)), max(1, round(content.height * scale / 4))), Image.Resampling.LANCZOS)
    content = logical.resize((logical.width * 4, logical.height * 4), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(content, ((size[0] - content.width) // 2, (size[1] - content.height) // 2))
    canvas.save(target)


def add_connector_branches() -> None:
    path = OUT / "unified_hud_frame_v1.png"
    source = Image.open(GEN / "exec-d2060d01-bfb4-4da4-bfc3-8c343b62756e.png").convert("RGBA")
    size = (2172, 424)
    original = Image.open(ROOT / "Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/unified_hud_frame.png").convert("RGBA")
    bbox = original.getchannel("A").getbbox()
    content_size = (round(size[0] * 0.96), size[1])
    position = ((size[0] - content_size[0]) // 2, 0)
    # Ignore sparse ImageGen alpha speckles outside the actual HUD silhouette.
    source_crop = source.crop((16, 92, 2156, 580))
    base = Image.new("RGBA", size, (0, 0, 0, 0))
    base.alpha_composite(source_crop.resize(content_size, Image.Resampling.NEAREST), position)
    mask = Image.new("L", size, 0)
    mask.paste(original.getchannel("A").crop(bbox).resize(content_size, Image.Resampling.NEAREST), position)
    base.putalpha(mask)
    alpha = base.getchannel("A")
    ImageDraw.Draw(alpha).rounded_rectangle((524, 56, 1648, 108), radius=26, fill=0)
    base.putalpha(alpha)
    additions = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(additions)

    # Compact solid struts bridge only the two outer shoulders of the EXP frame.
    left_outer = [(398, 92), (458, 74), (548, 118), (604, 138), (564, 146), (480, 120)]
    right_outer = [(1774, 92), (1714, 74), (1624, 118), (1568, 138), (1608, 146), (1692, 120)]
    left_inner = [(420, 94), (460, 84), (538, 122), (574, 136), (554, 138), (484, 114)]
    right_inner = [(1752, 94), (1712, 84), (1634, 122), (1598, 136), (1618, 138), (1688, 114)]
    for poly in (left_outer, right_outer):
        draw.polygon(poly, fill=(177, 116, 43, 255))
        draw.line(poly + [poly[0]], fill=(236, 184, 83, 255), width=6, joint="curve")
    for poly in (left_inner, right_inner):
        draw.polygon(poly, fill=(75, 43, 28, 255))
        draw.line(poly[:2], fill=(250, 204, 104, 255), width=4)

    # Only fill pixels that were transparent: every pre-existing frame pixel remains byte-identical.
    empty = base.getchannel("A").point(lambda a: 255 if a == 0 else 0)
    additions.putalpha(Image.composite(additions.getchannel("A"), Image.new("L", base.size, 0), empty))
    merged = Image.alpha_composite(additions, base)
    merged.save(path)


def opaque_mean(path: Path) -> tuple[float, float, float]:
    im = Image.open(path).convert("RGBA")
    rgb = Image.new("RGB", im.size)
    rgb.paste(im.convert("RGB"), mask=im.getchannel("A"))
    return tuple(round(v, 2) for v in ImageStat.Stat(rgb, mask=im.getchannel("A")).mean)


fit_icon(GEN / "exec-be91d086-08a7-49f2-bdc7-dc6da1756e63.png", OUT / "map_icon_v1.png", (1017, 915))
fit_icon(GEN / "exec-e5515b67-2400-444b-b56b-ed837bb0bff1.png", OUT / "stat_icon_v1.png", (1117, 1168))
add_connector_branches()

for name in ("map_icon_v1.png", "stat_icon_v1.png"):
    path = OUT / name
    im = Image.open(path).convert("RGBA")
    bbox = im.getchannel("A").getbbox()
    print(name, im.size, bbox, opaque_mean(path))

frame = Image.open(OUT / "unified_hud_frame_v1.png").convert("RGBA")
print("frame", frame.size, [frame.getpixel((1086, y))[3] for y in (6, 39, 43)], frame.getpixel((1086, 80))[3])
