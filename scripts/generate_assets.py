#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "src" / "PixelHomestead.Game" / "Assets" / "Generated"
OUT.mkdir(parents=True, exist_ok=True)


def img(size: tuple[int, int]) -> Image.Image:
    return Image.new("RGBA", size, (0, 0, 0, 0))


def rect(draw: ImageDraw.ImageDraw, xy: tuple[int, int, int, int], color: str) -> None:
    draw.rectangle(xy, fill=color)


def px(draw: ImageDraw.ImageDraw, x: int, y: int, color: str) -> None:
    draw.point((x, y), fill=color)


def paste_cell(atlas: Image.Image, index: int, sprite: Image.Image, cell: int = 16) -> None:
    columns = atlas.width // cell
    atlas.alpha_composite(sprite, ((index % columns) * cell, (index // columns) * cell))


def terrain_sprite(kind: str, variant: int = 0) -> Image.Image:
    s = img((16, 16))
    d = ImageDraw.Draw(s)
    if kind == "grass":
        base = ["#55b94a", "#66c94e", "#4eaa45", "#72d45b"][variant % 4]
        rect(d, (0, 0, 15, 15), base)
        for i in range(10):
            x = (i * 5 + variant * 3) % 16
            y = (i * 7 + variant * 2) % 16
            px(d, x, y, "#a4e86b")
            if y + 1 < 16:
                px(d, x, y + 1, "#2f8d45")
        for x, y in [(3, 11), (11, 4), (7, 13)]:
            rect(d, (x, y, x, min(15, y + 2)), "#267a3e")
    elif kind == "tall_grass":
        rect(d, (0, 0, 15, 15), "#54b749")
        for x in [1, 3, 4, 7, 10, 13, 14]:
            h = 4 + ((x + variant) % 5)
            rect(d, (x, 15 - h, x, 15), "#236f3d")
            if x + 1 < 16:
                rect(d, (x + 1, 16 - h, x + 1, 15), "#48b84f")
    elif kind == "flower":
        s = terrain_sprite("grass", variant)
        d = ImageDraw.Draw(s)
        for x, y, c in [(5, 6, "#ffb3d7"), (11, 9, "#fff2de"), (3, 12, "#f8cbff")]:
            rect(d, (x, y + 2, x, y + 5), "#247a3e")
            px(d, x, y, c)
            px(d, x - 1, y + 1, c)
            px(d, x + 1, y + 1, c)
            px(d, x, y + 1, "#ffe063")
    elif kind == "path":
        rect(d, (0, 0, 15, 15), "#e9ba55")
        for i in range(22):
            x = (i * 7 + variant) % 16
            y = (i * 5 + variant * 3) % 16
            px(d, x, y, "#f6d273")
            if (i + variant) % 3 == 0:
                px(d, min(15, x + 1), y, "#b87935")
        for x, y, w, h in [(2, 4, 5, 3), (9, 9, 5, 4), (5, 13, 4, 2)]:
            rect(d, (x, y, x + w - 1, y + h - 1), "#b7a978")
            rect(d, (x + 1, y, x + w - 2, y), "#d1c390")
    elif kind == "dirt":
        rect(d, (0, 0, 15, 15), "#b87d37")
        for i in range(18):
            x = (i * 3 + variant) % 16
            y = (i * 7 + variant * 2) % 16
            px(d, x, y, "#d79747")
            px(d, (x + 5) % 16, (y + 2) % 16, "#7e512b")
    elif kind == "soil":
        rect(d, (0, 0, 15, 15), "#6e4527")
        for y in [3, 8, 13]:
            rect(d, (1, y, 14, y + 1), "#8d562c")
            rect(d, (2, y + 1, 13, y + 1), "#4e301f")
    elif kind == "water":
        base = ["#3298cc", "#2f8dc4", "#3aa9d5", "#287ab5"][variant % 4]
        rect(d, (0, 0, 15, 15), base)
        for i in range(9):
            x = (i * 6 + variant * 3) % 16
            y = (i * 4 + variant * 2) % 16
            rect(d, (x, y, min(15, x + 4), y), "#8fe3ee")
        for x, y in [(3, 10), (12, 12), (8, 3)]:
            rect(d, (x, y, x + 1, y + 1), "#2474ac")
    elif kind == "shore":
        rect(d, (0, 0, 15, 15), "#d79b42")
        rect(d, (0, 6, 15, 15), "#8d9b98")
        rect(d, (0, 8, 15, 15), "#3298cc")
        rect(d, (1, 5, 5, 6), "#eac160")
        rect(d, (8, 6, 14, 7), "#c0c5b6")
    return s


def make_terrain() -> None:
    atlas = img((128, 64))
    kinds = ["grass", "grass", "grass", "grass", "tall_grass", "flower", "path", "dirt", "soil", "water", "water", "water", "water", "shore"]
    for index, kind in enumerate(kinds):
        paste_cell(atlas, index, terrain_sprite(kind, index))
    atlas.save(OUT / "terrain.png")


def make_props() -> None:
    atlas = img((256, 160))
    d = ImageDraw.Draw(atlas)
    # tree 32x48
    x, y = 0, 0
    rect(d, (x + 13, y + 28, x + 19, y + 47), "#5a301d")
    rect(d, (x + 15, y + 29, x + 16, y + 45), "#c67d33")
    for box, color in [
        ((x + 3, y + 17, x + 28, y + 35), "#17683e"),
        ((x + 6, y + 9, x + 25, y + 26), "#24864a"),
        ((x + 11, y + 1, x + 22, y + 16), "#43b455"),
        ((x + 1, y + 24, x + 14, y + 39), "#0e5836"),
        ((x + 17, y + 22, x + 31, y + 38), "#0f5c39"),
    ]:
        rect(d, box, color)
    rect(d, (x + 8, y + 15, x + 13, y + 17), "#6ad45c")
    rect(d, (x + 22, y + 25, x + 25, y + 27), "#083f2b")

    # bush 24x20
    x, y = 40, 10
    rect(d, (x + 2, y + 8, x + 21, y + 17), "#167146")
    rect(d, (x + 6, y + 3, x + 17, y + 13), "#2daa54")
    rect(d, (x + 3, y + 10, x + 7, y + 13), "#57cf58")
    rect(d, (x + 16, y + 8, x + 17, y + 9), "#ffb3d7")

    # mushroom 32x36
    x, y = 72, 8
    rect(d, (x + 13, y + 16, x + 21, y + 35), "#e8e0cf")
    rect(d, (x + 3, y + 7, x + 30, y + 19), "#d55367")
    rect(d, (x + 8, y + 3, x + 25, y + 12), "#e36e7d")
    for sx, sy in [(10, 9), (20, 7), (25, 13), (15, 15)]:
        rect(d, (x + sx, y + sy, x + sx + 3, y + sy + 2), "#fff2de")
    rect(d, (x + 26, y + 18, x + 30, y + 19), "#9b3249")

    # barrel/crate 16x20 each
    for x, y in [(112, 12), (136, 12)]:
        rect(d, (x + 3, y + 2, x + 12, y + 18), "#5a301d")
        rect(d, (x + 4, y + 3, x + 11, y + 17), "#9e5c27")
        rect(d, (x + 3, y + 6, x + 12, y + 7), "#d5903b")
        rect(d, (x + 3, y + 14, x + 12, y + 15), "#d5903b")
    rect(d, (139, 15, 148, 27), "#b86e2b")
    rect(d, (139, 15, 148, 16), "#e6a44a")
    rect(d, (143, 15, 144, 27), "#6a3a22")

    # fence 24x18
    x, y = 160, 14
    rect(d, (x + 2, y + 1, x + 6, y + 17), "#5a301d")
    rect(d, (x + 17, y + 1, x + 21, y + 17), "#5a301d")
    rect(d, (x + 3, y + 2, x + 4, y + 13), "#d5903b")
    rect(d, (x + 18, y + 2, x + 19, y + 13), "#d5903b")
    rect(d, (x, y + 6, x + 23, y + 9), "#9e5c27")
    rect(d, (x, y + 12, x + 23, y + 14), "#d5903b")

    # shipping box 20x20
    x, y = 192, 12
    rect(d, (x + 2, y + 3, x + 17, y + 18), "#5a301d")
    rect(d, (x + 4, y + 5, x + 15, y + 17), "#9e5c27")
    rect(d, (x + 6, y + 7, x + 13, y + 9), "#ffe063")
    rect(d, (x + 6, y + 13, x + 14, y + 14), "#d5903b")

    # house 112x96 at 0,48
    x, y = 0, 48
    rect(d, (x + 4, y + 33, x + 107, y + 94), "#9e5c27")
    rect(d, (x + 12, y + 39, x + 98, y + 42), "#d5903b")
    rect(d, (x + 16, y + 47, x + 20, y + 91), "#d5903b")
    rect(d, (x + 48, y + 55, x + 66, y + 94), "#5a301d")
    rect(d, (x + 13, y + 51, x + 34, y + 68), "#78cde8")
    rect(d, (x + 77, y + 51, x + 98, y + 68), "#78cde8")
    rect(d, (x - 4, y + 15, x + 115, y + 43), "#c94a25")
    for row in range(4):
        yy = y + 17 + row * 6
        rect(d, (x + row * 5, yy, x + 110 - row * 4, yy + 2), "#e46a2d")
        rect(d, (x + row * 5 + 2, yy + 3, x + 108 - row * 4, yy + 4), "#8c3021")
    rect(d, (x + 44, y + 80, x + 47, y + 83), "#ffe063")

    atlas.save(OUT / "props.png")


def make_player() -> None:
    atlas = img((128, 64))
    d = ImageDraw.Draw(atlas)
    body = {
        "skin": "#f6be8e",
        "hat": "#e0aa3a",
        "shirt": "#4570c8",
        "shirt_hi": "#6c99df",
        "pants": "#26346f",
        "boot": "#5a301d",
        "hair": "#5c2e27",
    }
    directions = ["down", "up", "left", "right"]
    for row, direction in enumerate(directions):
        for frame in range(4):
            x = frame * 24 + 4
            y = row * 16
            step = -1 if frame == 1 else 1 if frame == 3 else 0
            rect(d, (x + 5, y + 1, x + 14, y + 4), body["hat"])
            rect(d, (x + 7, y, x + 12, y + 2), "#ffe063")
            rect(d, (x + 7, y + 4, x + 12, y + 8), body["skin"])
            rect(d, (x + 6, y + 8, x + 13, y + 14), body["shirt"])
            rect(d, (x + 7, y + 9, x + 12, y + 10), body["shirt_hi"])
            rect(d, (x + 5, y + 14 + step, x + 8, y + 15), body["boot"])
            rect(d, (x + 11, y + 14 - step, x + 14, y + 15), body["boot"])
            if direction == "up":
                rect(d, (x + 6, y + 4, x + 13, y + 8), body["hair"])
            elif direction == "left":
                rect(d, (x + 6, y + 5, x + 7, y + 6), body["hair"])
                rect(d, (x + 6, y + 6, x + 7, y + 7), body["skin"])
            elif direction == "right":
                rect(d, (x + 12, y + 5, x + 13, y + 6), body["hair"])
                rect(d, (x + 12, y + 6, x + 13, y + 7), body["skin"])
            else:
                rect(d, (x + 7, y + 5, x + 8, y + 6), "#3a211b")
                rect(d, (x + 11, y + 5, x + 12, y + 6), "#3a211b")
    atlas.save(OUT / "player.png")


def make_icons() -> None:
    atlas = img((320, 64))
    d = ImageDraw.Draw(atlas)
    for index in range(16):
        x = index * 16
        rect(d, (x + 1, 1, x + 14, 14), "#fff1be")
    for index in range(16, 20):
        x = index * 16
        rect(d, (x + 1, 1, x + 14, 14), "#fff1be")
    paste_ai_tool_icons(atlas)
    # seeds
    for index, leaf in [(5, "#55b94a"), (6, "#72d45b"), (7, "#43b455"), (8, "#66c94e")]:
        x = index * 16
        rect(d, (x + 5, 7, x + 10, 12), "#7e512b")
        rect(d, (x + 7, 3, x + 10, 7), leaf)
        rect(d, (x + 10, 5, x + 13, 6), leaf)
    # crops
    for index, color in [(9, "#fff2de"), (10, "#ee7e2d"), (11, "#e64943"), (12, "#b57e40")]:
        x = index * 16
        rect(d, (x + 5, 5, x + 11, 12), color)
        rect(d, (x + 6, 2, x + 10, 5), "#43b455")
        rect(d, (x + 6, 6, x + 7, 6), "#fff1be")
    # fish
    for index, color in [(13, "#8fe3ee"), (14, "#3298cc"), (15, "#ffe063")]:
        x = index * 16
        rect(d, (x + 2, 7, x + 11, 11), color)
        rect(d, (x + 10, 5, x + 14, 13), "#287ab5")
        px(d, x + 4, 8, "#37231b")
    if not (OUT / "tool_icons_ai_source.png").exists():
        # scythe, hammer, shovel fallback
        x = 16 * 16
        rect(d, (x + 7, 3, x + 8, 14), "#d5903b")
        rect(d, (x + 4, 2, x + 12, 3), "#a6aba0")
        rect(d, (x + 11, 3, x + 13, 5), "#a6aba0")
        x = 17 * 16
        rect(d, (x + 7, 4, x + 8, 14), "#d5903b")
        rect(d, (x + 4, 2, x + 12, 6), "#909790")
        x = 18 * 16
        rect(d, (x + 7, 3, x + 8, 14), "#d5903b")
        rect(d, (x + 5, 2, x + 10, 5), "#909790")
    atlas.save(OUT / "icons.png")


def paste_ai_tool_icons(atlas: Image.Image) -> None:
    source_path = OUT / "tool_icons_ai_source.png"
    if not source_path.exists():
        return

    source = Image.open(source_path).convert("RGBA")
    cells = extract_ai_icon_cells(source)
    if len(cells) < 9:
        return

    atlas_indices = [0, 1, 2, 3, 4, 5, 16, 17, 18]
    for cell, atlas_index in zip(cells[:9], atlas_indices):
        icon = cell.resize((16, 16), Image.Resampling.LANCZOS)
        atlas.alpha_composite(icon, (atlas_index * 16, 0))


def extract_ai_icon_cells(source: Image.Image) -> list[Image.Image]:
    width, height = source.size

    def is_background(pixel: tuple[int, int, int, int]) -> bool:
        red, green, blue, alpha = pixel
        return alpha > 0 and red > 220 and green < 80 and blue > 220

    column_has_icon = []
    for x in range(width):
        non_background = 0
        for y in range(height):
            if not is_background(source.getpixel((x, y))):
                non_background += 1
        column_has_icon.append(non_background > 20)

    ranges: list[tuple[int, int]] = []
    start: int | None = None
    for x, has_icon in enumerate(column_has_icon):
        if has_icon and start is None:
            start = x
        elif not has_icon and start is not None:
            if x - start > 20:
                ranges.append((start, x - 1))
            start = None

    if start is not None and width - start > 20:
        ranges.append((start, width - 1))

    cells: list[Image.Image] = []
    for left, right in ranges[:9]:
        top = height
        bottom = 0
        for x in range(left, right + 1):
            for y in range(height):
                if not is_background(source.getpixel((x, y))):
                    top = min(top, y)
                    bottom = max(bottom, y)

        if top >= bottom:
            continue

        pad = 10
        left = max(0, left - pad)
        right = min(width - 1, right + pad)
        top = max(0, top - pad)
        bottom = min(height - 1, bottom + pad)
        crop = source.crop((left, top, right + 1, bottom + 1))
        square_size = max(crop.width, crop.height)
        square = img((square_size, square_size))
        square.alpha_composite(crop, ((square_size - crop.width) // 2, (square_size - crop.height) // 2))
        cells.append(square)

    return cells


def make_ui() -> None:
    atlas = img((128, 64))
    d = ImageDraw.Draw(atlas)
    rect(d, (0, 0, 31, 31), "#55301f")
    rect(d, (2, 2, 29, 29), "#9e5c27")
    rect(d, (5, 5, 26, 26), "#b8713b")
    rect(d, (8, 8, 23, 23), "#ffe1a3")
    rect(d, (10, 10, 21, 10), "#fff1be")
    rect(d, (40, 5, 103, 25), "#55301f")
    rect(d, (42, 7, 101, 23), "#9e5c27")
    rect(d, (46, 10, 97, 20), "#d5903b")
    rect(d, (48, 11, 95, 11), "#fff1be")
    atlas.save(OUT / "ui.png")


if __name__ == "__main__":
    make_terrain()
    make_props()
    make_player()
    make_icons()
    make_ui()
    print(f"Generated pixel assets in {OUT}")
