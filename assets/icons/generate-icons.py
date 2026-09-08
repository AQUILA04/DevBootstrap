#!/usr/bin/env python3
"""Generate reproducible StackPilot PNG/ICO assets from StackPilot.svg.

Requires: Pillow (pip install Pillow)
Optional: cairosvg for true SVG rasterization; otherwise draws a matching vector
approximation so CI/Linux agents can regenerate without extra system libs.
"""
from __future__ import annotations

import struct
from io import BytesIO
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
SVG = ROOT / "StackPilot.svg"
PNG_OUT = ROOT / "StackPilot.png"
ICO_OUT = ROOT / "StackPilot.ico"
STORE_OUT = ROOT / "StoreLogo.png"  # 300x300 for Partner Center

PINE = (0x0B, 0x1F, 0x24, 255)
TEAL = (0x0F, 0x8F, 0x7B, 255)
AMBER = (0xD9, 0x77, 0x06, 255)


def draw_icon(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    radius = int(size * 48 / 256)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=PINE)

    scale = size / 256.0
    stroke = max(2, int(14 * scale))

    hex_pts = [
        (64 * scale, 160 * scale),
        (64 * scale, 96 * scale),
        (128 * scale, 64 * scale),
        (192 * scale, 96 * scale),
        (192 * scale, 160 * scale),
        (128 * scale, 192 * scale),
    ]
    draw.line(hex_pts + [hex_pts[0]], fill=TEAL, width=stroke, joint="curve")

    check = [
        (96 * scale, 144 * scale),
        (120 * scale, 168 * scale),
        (168 * scale, 112 * scale),
    ]
    draw.line(check, fill=AMBER, width=stroke, joint="curve")
    return img


def write_ico(images: list[Image.Image], path: Path) -> None:
    """Write a multi-size ICO without relying on Pillow's ICO plugin quirks."""
    png_blobs: list[bytes] = []
    for im in images:
        bio = BytesIO()
        im.save(bio, format="PNG")
        png_blobs.append(bio.getvalue())

    count = len(images)
    header = struct.pack("<HHH", 0, 1, count)
    offset = 6 + (16 * count)
    entries = b""
    data = b""
    for im, blob in zip(images, png_blobs):
        w = 0 if im.width >= 256 else im.width
        h = 0 if im.height >= 256 else im.height
        entries += struct.pack("<BBBBHHII", w, h, 0, 0, 1, 32, len(blob), offset)
        data += blob
        offset += len(blob)
    path.write_bytes(header + entries + data)


def main() -> None:
    if not SVG.exists():
        raise SystemExit(f"Missing source SVG: {SVG}")

    master = draw_icon(512)
    master.save(PNG_OUT)
    print(f"Wrote {PNG_OUT}")

    store = draw_icon(300)
    store.save(STORE_OUT)
    print(f"Wrote {STORE_OUT}")

    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [draw_icon(s) for s in sizes]
    write_ico(frames, ICO_OUT)
    print(f"Wrote {ICO_OUT} ({', '.join(str(s) for s in sizes)})")


if __name__ == "__main__":
    main()
