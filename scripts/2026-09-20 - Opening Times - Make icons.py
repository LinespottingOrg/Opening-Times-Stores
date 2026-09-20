# Crop the WhatsApp bag mark and emit Windows ICO + missing mobile icons.
from __future__ import annotations

import io
import struct
from pathlib import Path

from PIL import Image

WA = Path(r"C:\Users\User\Downloads\WhatsApp Image 2026-09-14 at 11.32.10.jpeg")
ROOT = Path(r"C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores")
APPAR_LOGO = Path(r"C:\Users\User\Dropbox\aiprojekt\APPAR\Opening Times\logo")
MASTER_1024 = ROOT / "logo" / "icon-1024.png"
LANCZOS = Image.Resampling.LANCZOS


def fit_square(im: Image.Image, size: int, bg: tuple[int, int, int, int] | None = None) -> Image.Image:
    rgba = im.convert("RGBA")
    out = rgba.resize((size, size), LANCZOS)
    if bg is None:
        return out
    canvas = Image.new("RGBA", (size, size), bg)
    canvas.alpha_composite(out)
    return canvas


def padded(im: Image.Image, size: int, pad: float = 0.18, bg: tuple[int, int, int, int] = (0, 0, 0, 0)) -> Image.Image:
    inner = max(1, int(round(size * (1 - 2 * pad))))
    icon = im.convert("RGBA").resize((inner, inner), LANCZOS)
    canvas = Image.new("RGBA", (size, size), bg)
    xy = (size - inner) // 2
    canvas.paste(icon, (xy, xy), icon)
    return canvas


def is_bg(r: int, g: int, b: int) -> bool:
    mx, mn = max(r, g, b), min(r, g, b)
    return mn > 240 or ((mx - mn) < 18 and mn > 80)


def silhouette(im: Image.Image, size: int) -> Image.Image:
    src = im.convert("RGBA").resize((size, size), LANCZOS)
    px = src.load()
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    op = out.load()
    for y in range(size):
        for x in range(size):
            r, g, b, a = px[x, y]
            if a < 24 or is_bg(r, g, b):
                continue
            op[x, y] = (0, 0, 0, 255)
    return out


def save_ico(master: Image.Image, path: Path) -> None:
    sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
    white = (255, 255, 255, 255)
    images = [fit_square(master, s, white) for s in sizes]
    payloads = []
    for im in images:
        buf = io.BytesIO()
        im.save(buf, format="PNG")
        payloads.append(buf.getvalue())
    count = len(images)
    offset = 6 + 16 * count
    entries = []
    blob = b""
    for im, payload in zip(images, payloads):
        w, h = im.size
        entries.append(
            struct.pack(
                "<BBBBHHII",
                0 if w >= 256 else w,
                0 if h >= 256 else h,
                0,
                0,
                1,
                32,
                len(payload),
                offset,
            )
        )
        offset += len(payload)
        blob += payload
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(struct.pack("<HHH", 0, 1, count) + b"".join(entries) + blob)
    print(f"wrote {path} sizes={sizes}")


def save(im: Image.Image, path: Path, **kwargs) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path, **kwargs)
    print(f"wrote {path} {im.size} {im.mode}")


def main() -> None:
    wa = Image.open(WA).convert("RGB")
    px = wa.load()
    w, h = wa.size
    minx, miny, maxx, maxy = w, h, 0, 0
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if is_bg(r, g, b):
                continue
            minx, miny, maxx, maxy = min(minx, x), min(miny, y), max(maxx, x), max(maxy, y)
    pad = 18
    minx, miny = max(0, minx - pad), max(0, miny - pad)
    maxx, maxy = min(w - 1, maxx + pad), min(h - 1, maxy + pad)
    crop = wa.crop((minx, miny, maxx + 1, maxy + 1))
    side = max(crop.size)
    canvas = Image.new("RGB", (side, side), (255, 255, 255))
    canvas.paste(crop, ((side - crop.size[0]) // 2, (side - crop.size[1]) // 2))
    crop_path = ROOT / "logo" / "source" / "2026-09-20 - Opening Times - WhatsApp crop.png"
    save(canvas, crop_path, format="PNG")

    # Production master is the lossless 1024 bag (same mark, no JPEG artefacts).
    master = Image.open(MASTER_1024).convert("RGBA")
    white = (255, 255, 255, 255)

    # Windows widget
    app_icon = fit_square(master, 512, white)
    save(app_icon, ROOT / "OpeningTimesWidget" / "Assets" / "app-icon.png", format="PNG")
    save(app_icon, ROOT / "Package" / "layout" / "Assets" / "app-icon.png", format="PNG")
    save(app_icon, ROOT / "Package" / "Images" / "app-icon.png", format="PNG")

    ico_path = ROOT / "OpeningTimesWidget" / "Assets" / "app.ico"
    save_ico(master, ico_path)
    (ROOT / "Package" / "layout" / "Assets" / "app.ico").write_bytes(ico_path.read_bytes())

    # Store / qalogos copies
    save(fit_square(master, 1024, white).convert("RGB"), ROOT / "logo" / "icon-1024.png", format="PNG")
    save(fit_square(master, 512, white).convert("RGB"), ROOT / "logo" / "play-512.png", format="PNG")
    save(fit_square(master, 1024, white).convert("RGB"), APPAR_LOGO / "icon-1024.png", format="PNG")
    save(fit_square(master, 512, white).convert("RGB"), APPAR_LOGO / "play-512.png", format="PNG")
    save(fit_square(master, 512, white).convert("RGB"), ROOT / "store-assets" / "play-icon-512.png", format="PNG")
    save(fit_square(master, 1024, white).convert("RGB"), ROOT / "store-assets" / "asc-icon-1024.png", format="PNG")
    save(fit_square(master, 1080, white).convert("RGB"), ROOT / "store-assets" / "storelogo-1080x1080.png", format="PNG")
    save(
        fit_square(master, 1080, white).convert("RGB"),
        ROOT / "store-assets" / "ms-store-screenshots" / "storelogo-1080x1080.png",
        format="PNG",
    )

    # Missing Expo assets (app.json points at these; they were absent / Capacitor chevron)
    save(fit_square(master, 1024, white), ROOT / "mobile" / "assets" / "icon.png", format="PNG")
    save(padded(master, 1024, 0.12, (0, 0, 0, 0)), ROOT / "mobile" / "assets" / "android-icon-foreground.png", format="PNG")
    save(Image.new("RGBA", (1024, 1024), white), ROOT / "mobile" / "assets" / "android-icon-background.png", format="PNG")
    save(silhouette(master, 1024), ROOT / "mobile" / "assets" / "android-icon-monochrome.png", format="PNG")
    save(fit_square(master, 48, white), ROOT / "mobile" / "assets" / "favicon.png", format="PNG")
    save(padded(master, 1024, 0.12, (0, 0, 0, 0)), ROOT / "mobile" / "assets" / "splash-icon.png", format="PNG")
    save(fit_square(master, 1024, white), ROOT / "mobile" / "android" / "app" / "src" / "main" / "res" / "drawable" / "openingtimes_preview.png", format="PNG")

    splash = {
        "drawable-mdpi": 288,
        "drawable-hdpi": 432,
        "drawable-xhdpi": 576,
        "drawable-xxhdpi": 864,
        "drawable-xxxhdpi": 1152,
    }
    res = ROOT / "mobile" / "android" / "app" / "src" / "main" / "res"
    for folder, size in splash.items():
        save(padded(master, size, 0.12, (0, 0, 0, 0)), res / folder / "splashscreen_logo.png", format="PNG")

    mipmap = {
        "mipmap-mdpi": (48, 108),
        "mipmap-hdpi": (72, 162),
        "mipmap-xhdpi": (96, 216),
        "mipmap-xxhdpi": (144, 324),
        "mipmap-xxxhdpi": (192, 432),
    }
    bg_white = Image.new("RGBA", (1, 1), white)
    for folder, (legacy, adaptive) in mipmap.items():
        d = res / folder
        save(fit_square(master, legacy, white), d / "ic_launcher.webp", format="WEBP", lossless=True, quality=100)
        save(fit_square(master, legacy, white), d / "ic_launcher_round.webp", format="WEBP", lossless=True, quality=100)
        save(padded(master, adaptive, 0.12, (0, 0, 0, 0)), d / "ic_launcher_foreground.webp", format="WEBP", lossless=True, quality=100)
        save(bg_white.resize((adaptive, adaptive)), d / "ic_launcher_background.webp", format="WEBP", lossless=True, quality=100)
        save(silhouette(master, adaptive), d / "ic_launcher_monochrome.webp", format="WEBP", lossless=True, quality=100)

    # MSIX tiles (already bag; regenerate from master so they match)
    pkg = ROOT / "Package" / "Images"
    save(fit_square(master, 44, white), pkg / "Square44x44Logo.png", format="PNG")
    save(fit_square(master, 71, white), pkg / "Square71x71Logo.png", format="PNG")
    save(fit_square(master, 150, white), pkg / "Square150x150Logo.png", format="PNG")
    save(fit_square(master, 310, white), pkg / "Square310x310Logo.png", format="PNG")
    save(fit_square(master, 50, white), pkg / "StoreLogo.png", format="PNG")
    wide = Image.new("RGBA", (310, 150), white)
    tile = fit_square(master, 130, white)
    wide.paste(tile, ((310 - 130) // 2, (150 - 130) // 2), tile)
    save(wide, pkg / "Wide310x150Logo.png", format="PNG")
    splash_ms = Image.new("RGBA", (620, 300), white)
    tile2 = fit_square(master, 220, white)
    splash_ms.paste(tile2, ((620 - 220) // 2, (300 - 220) // 2), tile2)
    save(splash_ms, pkg / "SplashScreen.png", format="PNG")

    layout_img = ROOT / "Package" / "layout" / "Images"
    if layout_img.exists():
        for name in [
            "Square44x44Logo.png",
            "Square71x71Logo.png",
            "Square150x150Logo.png",
            "Square310x310Logo.png",
            "StoreLogo.png",
            "Wide310x150Logo.png",
            "SplashScreen.png",
        ]:
            src = pkg / name
            dst = layout_img / name
            dst.write_bytes(src.read_bytes())
            print(f"copied {dst}")


if __name__ == "__main__":
    main()
