#!/usr/bin/env python3
"""
Generate Google Play store listing graphics for Mergefall: Board Quest.

Outputs:
  Screenshots/store_assets/app_icon_512.png
  Screenshots/store_assets/app_icon_1024.png
  Screenshots/store_assets/feature_graphic_1024x500.png
  Screenshots/store_assets/phone/*.png

Uses project art (gems, backgrounds, enemies) + Lilita One (SIL OFL).
Re-run anytime after updating raw Screenshots/*.png captures.

Dependencies: Pillow (pip install pillow)
"""

from __future__ import annotations

import math
import os
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont
except ImportError:
    print("Pillow is required: pip install pillow", file=sys.stderr)
    sys.exit(1)

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets" / "_Project" / "Art"
FONT_PATH = ART / "Fonts" / "LilitaOne-Regular.ttf"
RAW_SHOTS = ROOT / "Screenshots"
OUT = RAW_SHOTS / "store_assets"
UI_ICON = ROOT / "Assets" / "_Project" / "UI" / "app_icon_mergefall.png"

# DesertTheme palette (0–255)
GOLD = (230, 184, 51)
CREAM = (255, 247, 230)
SAND = (199, 184, 158)
TURQ = (38, 191, 184)
MAHOGANY = (36, 23, 18)
CAMERA_BG = (15, 10, 8)
SHADOW = (77, 20, 15)
EMERALD = (31, 140, 97)

SCREENSHOT_JOBS: list[tuple[str, str, str, str]] = [
    (
        "03_board_run.png",
        "phone/01_build_squad.png",
        "BUILD YOUR SQUAD",
        "Spawn gems and merge on the desert board",
    ),
    (
        "04_merge_tiers.png",
        "phone/02_merge_tiers.png",
        "MERGE TO POWER UP",
        "Six crystal tiers — combine three of a kind",
    ),
    (
        "05_fight_race.png",
        "phone/03_battle.png",
        "FIGHT THE WAVE",
        "Tap Fight and clash with desert raiders",
    ),
    (
        "06_fight_result.png",
        "phone/04_victory.png",
        "CLAIM YOUR LOOT",
        "Win battles for gold and caravan resources",
    ),
    (
        "07_meta_hub.png",
        "phone/05_meta_hub.png",
        "UPGRADE THE CARAVAN",
        "Permanent meta upgrades between runs",
    ),
]


def c255(r: float, g: float, b: float) -> tuple[int, int, int]:
    return (int(r * 255), int(g * 255), int(b * 255))


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    if FONT_PATH.is_file():
        return ImageFont.truetype(str(FONT_PATH), size)
    return ImageFont.load_default()


def cover_crop(img: Image.Image, w: int, h: int) -> Image.Image:
    """Scale image to cover w×h, center-crop."""
    src_w, src_h = img.size
    scale = max(w / src_w, h / src_h)
    nw, nh = int(src_w * scale), int(src_h * scale)
    resized = img.resize((nw, nh), Image.Resampling.LANCZOS)
    left = (nw - w) // 2
    top = (nh - h) // 2
    return resized.crop((left, top, left + w, top + h))


def paste_center(base: Image.Image, sprite: Image.Image, cx: int, cy: int, scale: float = 1.0) -> None:
    if scale != 1.0:
        nw = max(1, int(sprite.width * scale))
        nh = max(1, int(sprite.height * scale))
        sprite = sprite.resize((nw, nh), Image.Resampling.LANCZOS)
    base.alpha_composite(sprite, (cx - sprite.width // 2, cy - sprite.height // 2))


def radial_gradient(size: int, inner: tuple[int, int, int], outer: tuple[int, int, int]) -> Image.Image:
    img = Image.new("RGBA", (size, size))
    px = img.load()
    half = size / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - half + 0.5, y - half + 0.5) / half
            t = min(1.0, d)
            r = int(inner[0] * (1 - t) + outer[0] * t)
            g = int(inner[1] * (1 - t) + outer[1] * t)
            b = int(inner[2] * (1 - t) + outer[2] * t)
            px[x, y] = (r, g, b, 255)
    return img


def draw_stroked_text(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    font: ImageFont.ImageFont,
    fill: tuple[int, int, int],
    stroke: tuple[int, int, int] = SHADOW,
    stroke_width: int = 3,
    anchor: str = "lt",
) -> None:
    draw.text(xy, text, font=font, fill=fill, anchor=anchor, stroke_width=stroke_width, stroke_fill=stroke)


def load_gem(tier: int) -> Image.Image:
    path = ART / "Items" / f"item_gem_t{tier}.png"
    if not path.is_file():
        raise FileNotFoundError(f"Missing gem art: {path}")
    return Image.open(path).convert("RGBA")


def load_bg(name: str) -> Image.Image:
    path = ART / "Backgrounds" / name
    if not path.is_file():
        raise FileNotFoundError(f"Missing background: {path}")
    return Image.open(path).convert("RGBA")


def make_app_icon(size: int) -> Image.Image:
    """Branded square icon — gem cluster on desert gradient."""
    inner = c255(0.28, 0.18, 0.12)
    outer = c255(0.08, 0.05, 0.04)
    icon = radial_gradient(size, inner, outer)

    # Warm sun glow behind hero gem
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(glow)
    cx, cy = size // 2, int(size * 0.44)
    for r, alpha in [(int(size * 0.38), 55), (int(size * 0.28), 90), (int(size * 0.18), 120)]:
        gdraw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(*GOLD, alpha))
    icon = Image.alpha_composite(icon, glow)

    t6 = load_gem(6)
    t4 = load_gem(4)
    t2 = load_gem(2)
    paste_center(icon, t2, int(size * 0.28), int(size * 0.52), scale=size / 512 * 0.55)
    paste_center(icon, t4, int(size * 0.72), int(size * 0.55), scale=size / 512 * 0.50)
    paste_center(icon, t6, size // 2, int(size * 0.46), scale=size / 512 * 1.05)

    # Gold ring (adaptive-icon safe zone ~66% center)
    ring = ImageDraw.Draw(icon)
    margin = int(size * 0.06)
    ring.ellipse(
        (margin, margin, size - margin, size - margin),
        outline=(*GOLD, 180),
        width=max(2, size // 64),
    )

    draw = ImageDraw.Draw(icon)
    title_font = load_font(max(18, size // 11))
    sub_font = load_font(max(12, size // 18))
    draw_stroked_text(draw, (size // 2, int(size * 0.82)), "Mergefall", title_font, GOLD, anchor="mm")
    draw.text((size // 2, int(size * 0.91)), "Board Quest", font=sub_font, fill=SAND, anchor="mm")

    return icon


def make_feature_graphic() -> Image.Image:
    """1024×500 Play Store banner."""
    w, h = 1024, 500
    bg = cover_crop(load_bg("bg_caravan.png"), w, h)

    # Darken left for text legibility
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    odraw = ImageDraw.Draw(overlay)
    for x in range(w):
        t = min(1.0, x / (w * 0.72))
        alpha = int(210 * (1 - t * 0.85))
        odraw.line([(x, 0), (x, h)], fill=(15, 10, 8, alpha))
    odraw.rectangle((0, 0, w, h), fill=(15, 10, 8, 40))
    banner = Image.alpha_composite(bg, overlay)

    # Gem cluster on the right
    gems = [load_gem(i) for i in (1, 3, 5, 6, 4, 2)]
    positions = [
        (780, 280, 0.75),
        (900, 220, 0.65),
        (680, 200, 0.60),
        (860, 360, 0.95),
        (720, 380, 0.70),
        (950, 320, 0.55),
    ]
    for gem, (gx, gy, sc) in zip(gems, positions):
        paste_center(banner, gem, gx, gy, scale=sc)

    # Enemy silhouette accent
    bandit_path = ART / "Enemies" / "bandit.png"
    if bandit_path.is_file():
        bandit = Image.open(bandit_path).convert("RGBA")
        paste_center(banner, bandit, 620, 380, scale=0.55)

    draw = ImageDraw.Draw(banner)
    title = load_font(72)
    subtitle = load_font(36)
    tagline = load_font(28)
    draw_stroked_text(draw, (48, 72), "Mergefall", title, GOLD, stroke_width=4)
    draw.text((48, 148), "Board Quest", font=subtitle, fill=CREAM)
    draw_stroked_text(draw, (48, 210), "Merge · Fight · Conquer the Board", tagline, TURQ, stroke_width=2)
    body = load_font(22)
    draw_stroked_text(
        draw,
        (48, 340),
        "Idle merge RPG with desert caravan meta progression",
        body,
        CREAM,
        stroke_width=2,
    )
    draw_stroked_text(
        draw,
        (48, 378),
        "Combine gems · Survive waves · Upgrade your camp",
        body,
        SAND,
        stroke_width=2,
    )

    # Bottom accent bar
    bar = Image.new("RGBA", (w, 8), (*GOLD, 200))
    banner.alpha_composite(bar, (0, h - 8))

    return banner.convert("RGB")


def patch_debug_hud(shot: Image.Image) -> Image.Image:
    """Cover the dev HUD line ('Wave N | Soft …') without removing the wave bar."""
    out = shot.convert("RGBA")
    w, h = out.size
    # Debug strip spans ~y 90–142 on 1080×1920 captures (below currency icons).
    y0, y1 = 88, 143
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    band = Image.new("RGBA", (w, y1 - y0), (*CAMERA_BG, 252))
    overlay.paste(band, (0, y0))
    # Feather top/bottom edges into the scene.
    feather = ImageDraw.Draw(overlay)
    for y in range(8):
        alpha = int(180 * (1 - y / 7))
        feather.line([(0, y0 + y), (w, y0 + y)], fill=(*CAMERA_BG, alpha))
        feather.line([(0, y1 - 1 - y), (w, y1 - 1 - y)], fill=(*CAMERA_BG, alpha))
    return Image.alpha_composite(out, overlay)


def add_caption_bar(shot: Image.Image, headline: str, subline: str) -> Image.Image:
    """Marketing caption gradient at the bottom."""
    w, h = shot.size
    if shot.mode != "RGBA":
        shot = shot.convert("RGBA")

    bar_h = 220
    grad = Image.new("RGBA", (w, bar_h), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(grad)
    for y in range(bar_h):
        t = y / bar_h
        alpha = int(230 * (t**1.6))
        gdraw.line([(0, y), (w, y)], fill=(*CAMERA_BG, alpha))

    shot.alpha_composite(grad, (0, h - bar_h))

    draw = ImageDraw.Draw(shot)
    head_font = load_font(52)
    sub_font = load_font(28)
    draw_stroked_text(draw, (w // 2, h - 130), headline, head_font, GOLD, stroke_width=4, anchor="mm")
    draw.text((w // 2, h - 68), subline, font=sub_font, fill=CREAM, anchor="mm")

    return shot


def polish_screenshot(path: Path, out_path: Path, headline: str, subline: str) -> None:
    img = Image.open(path).convert("RGB")
    img = patch_debug_hud(img)
    img = ImageEnhance.Contrast(img).enhance(1.06)
    img = ImageEnhance.Color(img).enhance(1.05)
    img = add_caption_bar(img, headline, subline)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    img.convert("RGB").save(out_path, optimize=True)
    print(f"  phone: {out_path.relative_to(ROOT)}")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    print("Generating app icons…")
    icon512 = make_app_icon(512)
    icon1024 = make_app_icon(1024)
    icon512.save(OUT / "app_icon_512.png", optimize=True)
    icon1024.save(OUT / "app_icon_1024.png", optimize=True)
    UI_ICON.parent.mkdir(parents=True, exist_ok=True)
    icon1024.save(UI_ICON, optimize=True)
    print(f"  icon:  {OUT / 'app_icon_512.png'}")
    print(f"  unity: {UI_ICON}")

    print("Generating feature graphic…")
    feature = make_feature_graphic()
    feature.save(OUT / "feature_graphic_1024x500.png", optimize=True)
    print(f"  banner: {OUT / 'feature_graphic_1024x500.png'}")

    print("Compositing phone screenshots…")
    for src_name, rel_out, headline, subline in SCREENSHOT_JOBS:
        src = RAW_SHOTS / src_name
        if not src.is_file():
            print(f"  SKIP missing {src_name}", file=sys.stderr)
            continue
        polish_screenshot(src, OUT / rel_out, headline, subline)

    print(f"\nDone — store assets in {OUT}")


if __name__ == "__main__":
    main()
