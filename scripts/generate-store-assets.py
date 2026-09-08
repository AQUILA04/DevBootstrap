from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
SOURCE_SIZE = 1024


def create_icon(size: int) -> Image.Image:
    scale = size / SOURCE_SIZE
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    def points(values: list[tuple[int, int]]) -> list[tuple[int, int]]:
        return [(round(x * scale), round(y * scale)) for x, y in values]

    radius = round(224 * scale)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill="#0B1F24")
    hexagon = points([(256, 640), (256, 384), (512, 256), (768, 384), (768, 640), (512, 768), (256, 640)])
    draw.line(hexagon, fill="#0F8F7B", width=max(2, round(48 * scale)), joint="curve")
    check = points([(384, 576), (480, 672), (672, 448)])
    draw.line(check, fill="#D97706", width=max(2, round(48 * scale)), joint="curve")
    return image


def main() -> None:
    app_assets = ROOT / "src" / "StackPilot" / "assets"
    store_assets = ROOT / "store-assets"
    app_assets.mkdir(parents=True, exist_ok=True)
    store_assets.mkdir(parents=True, exist_ok=True)

    source = create_icon(SOURCE_SIZE)
    source.save(store_assets / "StackPilot-512.png", optimize=True)
    for size in (300, 150, 44):
        source.resize((size, size), Image.Resampling.LANCZOS).save(
            store_assets / f"StackPilot-{size}.png", optimize=True
        )

    source.save(
        app_assets / "StackPilot.ico",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )


if __name__ == "__main__":
    main()
