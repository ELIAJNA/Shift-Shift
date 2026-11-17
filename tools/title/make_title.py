"""Shift+Shift menu title: SHIFT+SHIFT in Black Ops One (SIL OFL), styled and baked into a PNG.

    python tools/title/make_title.py --out <file.png> [--skew 0.2] [--cap 150] [--depth 0.11]

Style (ELIA, day 27): mirrored letter pairs in the six action colours, outside in (lane orange, slide yellow, sprint
cyan, jump pink #FF4FE8, phase blue; + charge red); a dark navy 3D extrusion down-right; a dark outline; a satin face
(soft sheen at the top, a thin bright line, shade towards the bottom). Optional forward lean (--skew).
Needs Pillow. The font file and its licence sit next to this script.
"""
import argparse, math, os
from PIL import Image, ImageDraw, ImageFont, ImageChops, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
TEXT = "SHIFT+SHIFT"
PAIRS = ["#F27017", "#FCC41E", "#00FCF9", "#FF4FE8", "#3D8BFF", "#F21E3D"]


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))


def render(cap=150, skew=0.0, depth_frac=0.11, letter_gap=0.06):
    font_px = int(cap / 0.72)
    font = ImageFont.truetype(os.path.join(HERE, "BlackOpsOne-Regular.ttf"), font_px)
    cap_h = -font.getbbox("H", anchor="ls")[1]
    gap = int(cap_h * letter_gap)
    widths = [font.getlength(c) for c in TEXT]
    text_w = sum(widths) + gap * (len(TEXT) - 1)
    depth = max(3, int(cap_h * depth_frac))
    outline = max(2, int(cap_h * 0.025))
    pad = outline + 6
    lean = int(abs(skew) * cap_h) + 2
    W = int(text_w + 2 * pad + depth * 0.6 + lean + 4)
    H = int(cap_h + 2 * pad + depth + cap_h * 0.12)
    base = pad + cap_h  # baseline y

    # Face mask per letter colour, and the whole face.
    face = Image.new("L", (W, H), 0)
    colour = Image.new("RGB", (W, H), (0, 0, 0))
    x = pad
    for i, ch in enumerate(TEXT):
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).text((x, base), ch, font=font, fill=255, anchor="ls")
        face = ImageChops.lighter(face, m)
        colour.paste(Image.new("RGB", (W, H), hexc(PAIRS[min(i, len(TEXT) - 1 - i)])), (0, 0), m)
        x += widths[i] + gap

    def lean_it(img):  # forward lean: shift rows right the higher they are
        if skew == 0:
            return img
        return img.transform(img.size, Image.AFFINE, (1, skew, -skew * base, 0, 1, 0), resample=Image.BICUBIC)

    face, colour = lean_it(face), lean_it(colour)

    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    # Extrusion, back to front.
    for d in range(depth, 0, -1):
        t = (d - 1) / max(1, depth - 1)
        c = tuple(int(a + (b - a) * t) for a, b in zip((34, 44, 78), (8, 10, 20)))
        layer = Image.new("RGBA", (W, H), c + (0,))
        layer.putalpha(ImageChops.offset(face, int(round(d * 0.6)), d))
        out.alpha_composite(layer)
    # Outline.
    ring = face.filter(ImageFilter.MaxFilter(2 * outline + 1))
    layer = Image.new("RGBA", (W, H), (5, 8, 15, 0))
    layer.putalpha(ring.point(lambda v: int(v * 0.92)))
    out.alpha_composite(layer)
    # Satin face.
    top = base - cap_h
    shade = Image.new("RGB", (W, H))
    px = shade.load()
    cpx = colour.load()
    for yy in range(H):
        t = (yy - top + 0.5) / cap_h
        light = 0.55 * (1 - t / 0.5) ** 2 if 0 <= t < 0.5 else 0
        if 0.2 < t < 0.28:
            light += 0.25
        dark = 0.38 * (t - 0.55) / 0.45 if t > 0.55 else 0
        for xx in range(W):
            r, g, b = cpx[xx, yy]
            r += (255 - r) * light; g += (255 - g) * light; b += (255 - b) * light
            px[xx, yy] = (int(r * (1 - dark)), int(g * (1 - dark)), int(b * (1 - dark * 0.9)))
    layer = shade.convert("RGBA")
    layer.putalpha(face)
    out.alpha_composite(layer)
    return out.crop(out.getbbox())


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--cap", type=int, default=150)
    ap.add_argument("--skew", type=float, default=0.0)
    ap.add_argument("--depth", type=float, default=0.11)
    a = ap.parse_args()
    img = render(a.cap, a.skew, a.depth)
    img.save(a.out, optimize=True)
    print(a.out, img.size, os.path.getsize(a.out) // 1024, "KB")
