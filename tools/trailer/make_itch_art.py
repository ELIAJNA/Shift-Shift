"""Shift+Shift itch.io page art: banner (960x300), page background (1920x1080), embed background (960x600).

    python tools/trailer/make_itch_art.py --out <folder>

Drawn from the game's look: night palette sky, the three-lane road, obstacles in the action colours, the boxy runner.
Needs Pillow; font Inter Display if found (--fonts), else Arial Bold / DejaVu Sans Bold.
"""
import argparse, math, os, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SKY_TOP, SKY_BOTTOM = (6, 8, 22), (24, 30, 66)      # night palette background, darker at the top
ROAD, ROAD_EDGE, LINES = (40, 40, 46), (92, 225, 230), (190, 190, 190)
COL = {"lane": (242, 112, 23), "slide": (252, 196, 30), "sprint": (0, 252, 249), "jump": (244, 14, 157),
       "phase": (61, 139, 255), "charge": (242, 30, 61), "white": (247, 247, 242)}
SS = 2  # supersampling


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


class Scene:
    """A road running to a vanishing point. World: x sideways (lanes at -1, 0, 1 x lane width), z ahead."""

    def __init__(self, w, h, horizon, vx, cam_h=1.0, focal=None, lane_w=1.0):
        self.w, self.h, self.horizon, self.vx = w, h, horizon, vx
        self.cam_h, self.focal, self.lane_w = cam_h, focal or h * 0.9, lane_w

    def p(self, x, y, z):
        s = self.focal / z
        return (self.vx + x * s, self.horizon + (self.cam_h - y) * s)


def sky(im, horizon):
    d = ImageDraw.Draw(im)
    w, h = im.size
    for y in range(h):
        t = min(1.0, y / max(1, horizon))
        d.line([(0, y), (w, y)], fill=lerp(SKY_TOP, SKY_BOTTOM, t ** 1.6))
    rnd = random.Random(7)
    for _ in range(int(w * horizon / 9000)):
        x, y = rnd.randrange(w), rnd.randrange(int(horizon * 0.85))
        b = rnd.randint(90, 220)
        r = rnd.choice([1, 1, 1, 2]) * SS
        d.ellipse([x - r, y - r, x + r, y + r], fill=(b, b, min(255, b + 30)))


def skyline(im, horizon, seed=3, alpha=255):
    """Blocky buildings and conifers on the horizon (the game's roadside scenery)."""
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    w = im.size[0]
    rnd = random.Random(seed)
    for depth, col in ((0, (20, 26, 54)), (1, (13, 17, 38))):
        x = -rnd.randint(0, 40 * SS)
        while x < w:
            bw = rnd.randint(30, 90) * SS * (1 + depth * 0.3)
            if rnd.random() < 0.3:  # a conifer
                th = rnd.randint(30, 60) * SS
                d.polygon([(x, horizon), (x + bw * 0.25, horizon - th), (x + bw * 0.5, horizon)], fill=col + (alpha,))
                x += bw * 0.5
                continue
            bh = rnd.randint(20, 110) * SS * (1 - depth * 0.35)
            d.rectangle([x, horizon - bh, x + bw, horizon], fill=col + (alpha,))
            for wy in range(int(horizon - bh + 8 * SS), int(horizon - 6 * SS), 12 * SS):  # lit windows
                for wx in range(int(x + 6 * SS), int(x + bw - 6 * SS), 12 * SS):
                    if rnd.random() < 0.18:
                        d.rectangle([wx, wy, wx + 3 * SS, wy + 4 * SS], fill=(255, 214, 140, int(alpha * 0.8)))
            x += bw + rnd.randint(0, 12) * SS
    im.alpha_composite(layer)


def road(im, sc, z_near=0.8, z_far=400):
    d = ImageDraw.Draw(im)
    half = sc.lane_w * 1.5
    # ground either side
    d.rectangle([0, sc.horizon, sc.w, sc.h], fill=(10, 12, 26))
    a, b = sc.p(-half, 0, z_near), sc.p(half, 0, z_near)
    c, e = sc.p(half, 0, z_far), sc.p(-half, 0, z_far)
    d.polygon([a, b, c, e], fill=ROAD)
    # dashed lane lines and glowing edges
    for x in (-0.5, 0.5):
        z = z_near
        while z < 60:
            p1, p2 = sc.p(x * sc.lane_w, 0, z), sc.p(x * sc.lane_w, 0, z + 1.2)
            wd = max(1, int(0.05 * sc.focal / z))
            d.line([p1, p2], fill=LINES, width=wd)
            z += 3.0
    glow = Image.new("RGBA", im.size, (0, 0, 0, 0))
    g = ImageDraw.Draw(glow)
    for x in (-half, half):
        g.line([sc.p(x, 0, z_near), sc.p(x, 0, z_far)], fill=ROAD_EDGE + (255,), width=6 * SS)
    im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(6 * SS)))
    for x in (-half, half):
        d.line([sc.p(x, 0, z_near), sc.p(x, 0, z_far)], fill=ROAD_EDGE, width=2 * SS)


def box(im, sc, x0, x1, y0, y1, z, depth, col, alpha=255, glow=True):
    """An obstacle block: front face plus top face, with a soft glow."""
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    f = [sc.p(x0, y0, z), sc.p(x1, y0, z), sc.p(x1, y1, z), sc.p(x0, y1, z)]
    t = [sc.p(x0, y1, z), sc.p(x1, y1, z), sc.p(x1, y1, z + depth), sc.p(x0, y1, z + depth)]
    side_x = x1 if sc.p(x1, 0, z)[0] < sc.vx else x0
    s = [sc.p(side_x, y0, z), sc.p(side_x, y0, z + depth), sc.p(side_x, y1, z + depth), sc.p(side_x, y1, z)]
    d.polygon(s, fill=lerp(col, (0, 0, 0), 0.45) + (alpha,))
    d.polygon(t, fill=lerp(col, (255, 255, 255), 0.25) + (alpha,))
    d.polygon(f, fill=col + (alpha,))
    d.line(f + [f[0]], fill=lerp(col, (255, 255, 255), 0.5) + (min(255, alpha + 40),), width=SS)
    if glow:
        im.alpha_composite(layer.filter(ImageFilter.GaussianBlur(10 * SS)))
    im.alpha_composite(layer)


def runner(im, sc, x, z, flame=True):
    """The boxy runner, mid-stride, seen from behind, with the charge flame."""
    if flame:
        # Small flames streaming back from the body (yellow inside, then orange, then red), as in the game.
        fl = Image.new("RGBA", im.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(fl)
        rnd = random.Random(5)
        k = sc.focal / z / 250
        for i in range(320):
            back = rnd.random() ** 1.5 * 0.9              # how far behind the runner (toward the camera)
            px, py = sc.p(x + rnd.gauss(0, 0.12) * (1 + back), rnd.uniform(0.15, 1.0) + back * 0.25, z - back)
            r = rnd.uniform(1.4, 4.6) * SS * k * (1.2 - back * 0.6)
            t = min(1.0, back * 1.3 + rnd.uniform(-0.2, 0.2))
            col = lerp((255, 236, 120), (255, 130, 25), min(1, t * 2)) if t < 0.5 else lerp((255, 130, 25), (230, 30, 30), (t - 0.5) * 2)
            d.ellipse([px - r, py - r * 1.8, px + r, py + r * 0.6], fill=col + (int(220 * (1 - back * 0.7)),))
        im.alpha_composite(fl.filter(ImageFilter.GaussianBlur(1.5 * SS)))
        im.alpha_composite(fl.filter(ImageFilter.GaussianBlur(8 * SS)))
    body = (240, 240, 236)
    box(im, sc, x - 0.13, x + 0.13, 0.42, 0.85, z, 0.12, body, glow=False)          # torso
    box(im, sc, x - 0.09, x + 0.09, 0.88, 1.06, z, 0.12, body, glow=False)          # head
    box(im, sc, x - 0.12, x - 0.03, 0.05, 0.42, z + 0.15, 0.1, (200, 200, 196), glow=False)  # back leg
    box(im, sc, x + 0.03, x + 0.12, 0.12, 0.42, z - 0.05, 0.1, body, glow=False)      # front leg
    box(im, sc, x - 0.19, x - 0.13, 0.48, 0.82, z - 0.05, 0.08, body, glow=False)      # arms
    box(im, sc, x + 0.13, x + 0.19, 0.5, 0.84, z + 0.1, 0.08, (200, 200, 196), glow=False)


def obstacles(im, sc, set_="full"):
    L = sc.lane_w
    items = [  # (lane or None for all, kind, z)
        (None, "charge", 17), (-1, "lane", 7.5),
    ]
    for lane, kind, z in sorted(items, key=lambda t: -t[2]):
        if kind == "lane":
            box(im, sc, lane * L - 0.4, lane * L + 0.4, 0, 1.0, z, 0.8, COL["lane"])
        elif kind == "slide":
            box(im, sc, -1.5 * L, 1.5 * L, 0.75, 0.95, z, 0.3, COL["slide"])
            for x in (-1.45, 1.45):
                box(im, sc, x * L - 0.04, x * L + 0.04, 0, 0.75, z, 0.1, (90, 90, 96), glow=False)
        elif kind == "sprint_gate":
            for lx in (-1, 0):
                box(im, sc, lx * L - 0.5, lx * L + 0.5, 0, 1.6, z, 0.3, COL["sprint"], alpha=230)
            box(im, sc, L * 0.5, L * 1.5, 1.4, 1.6, z, 0.3, COL["sprint"])
        elif kind == "phase":
            box(im, sc, -1.5 * L, 1.5 * L, 0, 1.8, z, 0.2, COL["phase"], alpha=150)
            box(im, sc, -0.35, 0.35, 0, 1.0, z, 0.6, (255, 181, 217), alpha=230)  # a ghost block
        elif kind == "charge":
            box(im, sc, -1.5 * L, 1.5 * L, 0, 2.2, z, 0.5, COL["charge"])
            d = ImageDraw.Draw(im)  # brick lines
            for row in range(1, 8):
                y = row * 2.2 / 8
                d.line([sc.p(-1.5 * L, y, z), sc.p(1.5 * L, y, z)], fill=(150, 20, 40), width=SS)
                for i in range(7):
                    xx = -1.5 * L + (i + (0.5 if row % 2 else 0)) * 3 * L / 7
                    if -1.5 * L < xx < 1.5 * L:
                        d.line([sc.p(xx, y, z), sc.p(xx, y - 2.2 / 8, z)], fill=(150, 20, 40), width=SS)


def fog(im, horizon, strength=150, height=0.12):
    w, h = im.size
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    band = int(h * height)
    for i in range(band):
        a = int(strength * (1 - abs(i - band / 2) / (band / 2)) ** 2)
        d.line([(0, horizon - band / 2 + i), (w, horizon - band / 2 + i)], fill=SKY_BOTTOM + (a,))
    im.alpha_composite(layer)


def vignette(im, amount=0.6):
    """Darkens the edges: 0 at the centre, `amount` in the corners."""
    w, h = im.size
    sw, sh = 96, 54
    m = Image.new("L", (sw, sh))
    m.putdata([int(255 * min(1.0, (((x - sw / 2) / (sw / 2)) ** 2 + ((y - sh / 2) / (sh / 2)) ** 2) / 2) ** 1.2)
               for y in range(sh) for x in range(sw)])
    m = m.resize((w, h), Image.BILINEAR)
    dark = Image.new("RGBA", im.size, (0, 0, 0, int(255 * amount)))
    im.paste(Image.alpha_composite(im, dark), (0, 0), m)


def scene_image(w, h, horizon_frac, vx_frac, full=True, runner_on=True, scenery=True):
    W, H = w * SS, h * SS
    im = Image.new("RGBA", (W, H))
    horizon = int(H * horizon_frac)
    sky(im, horizon)
    if scenery:
        skyline(im, horizon)
    sc = Scene(W, H, horizon, W * vx_frac, cam_h=1.05, focal=H * 1.35, lane_w=1.0)
    road(im, sc)
    fog(im, horizon)
    if full:
        obstacles(im, sc)
    if runner_on:
        runner(im, sc, 0.0, 4.4)
    return im


def text(im, s, size, xy, fill, fontp, anchor="mm", glow=None, stroke=0):
    f = ImageFont.truetype(fontp, size * SS)
    if glow:
        layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).text(xy, s, font=f, fill=glow + (255,), anchor=anchor)
        im.alpha_composite(layer.filter(ImageFilter.GaussianBlur(10 * SS)))
    ImageDraw.Draw(im).text(xy, s, font=f, fill=fill, anchor=anchor, stroke_width=stroke * SS, stroke_fill=(0, 0, 0))


def keycap(im, cx, cy, label, col, fontp, scale=1.0):
    w, h = int(150 * SS * scale), int(66 * SS * scale)
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle([cx - w / 2, cy - h / 2 + 6 * SS, cx + w / 2, cy + h / 2 + 6 * SS], 12 * SS, fill=(0, 0, 0, 160))
    d.rounded_rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], 12 * SS, fill=(30, 34, 52, 255),
                        outline=col + (255,), width=3 * SS)
    im.alpha_composite(layer)
    text(im, label, int(26 * scale), (cx, cy), col, fontp)


def finish(im, w, h, path):
    im.convert("RGB").resize((w, h), Image.LANCZOS).save(path, optimize=True)
    print(path, os.path.getsize(path) // 1024, "KB")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--fonts", default="")
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    dirs = ([a.fonts] if a.fonts else []) + [r"C:\Windows\Fonts", "/usr/share/fonts/opentype/inter",
                                             "/usr/share/fonts/truetype/dejavu"]
    def ff(names):
        for d in dirs:
            for n in names:
                if os.path.isfile(os.path.join(d, n)):
                    return os.path.join(d, n)
        raise SystemExit("no font")
    bold = ff(["InterDisplay-Black.otf", "InterDisplay-Bold.otf", "ariblk.ttf", "arialbd.ttf", "DejaVuSans-Bold.ttf"])
    med = ff(["InterDisplay-Medium.otf", "arial.ttf", "DejaVuSans.ttf"])

    # Banner 960x300: road and obstacles on the right, title and the two keys on the left.
    w, h = 960, 300
    im = scene_image(w, h, 0.42, 0.72)
    shade = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ds = ImageDraw.Draw(shade)
    for x in range(int(im.size[0] * 0.62)):
        ds.line([(x, 0), (x, im.size[1])], fill=(6, 8, 22, int(235 * (1 - x / (im.size[0] * 0.62)) ** 0.6)))
    im.alpha_composite(shade)
    text(im, "SHIFT+SHIFT", 84, (40 * SS, 112 * SS), COL["white"], bold, anchor="lm", glow=(92, 225, 230))
    for i, c in enumerate(["lane", "slide", "sprint", "jump", "phase", "charge"]):
        ImageDraw.Draw(im).rectangle([(44 + i * 62) * SS, 52 * SS, (44 + i * 62 + 52) * SS, 58 * SS], fill=COL[c])
    text(im, "A rhythm runner played with just two keys", 24, (44 * SS, 172 * SS), (201, 209, 216), med, anchor="lm")
    keycap(im, (44 + 75) * SS, 236 * SS, "⇧  LEFT", COL["sprint"], bold, 1.0)
    text(im, "+", 34, ((44 + 175) * SS, 236 * SS), COL["white"], bold)
    keycap(im, (44 + 275) * SS, 236 * SS, "RIGHT  ⇧", COL["jump"], bold, 1.0)
    finish(im, w, h, os.path.join(a.out, "banner_960x300.png"))

    # Page background 1920x1080: the scene, darkened, so the page column in the middle stays readable.
    w, h = 1920, 1080
    im = scene_image(w, h, 0.62, 0.5, full=False, runner_on=False)
    im.alpha_composite(Image.new("RGBA", im.size, (6, 8, 22, 120)))
    vignette(im, 0.5)
    finish(im, w, h, os.path.join(a.out, "background_1920x1080.png"))

    # Embed background 960x600: shown behind the "Run game" button.
    w, h = 960, 600
    im = scene_image(w, h, 0.46, 0.5)
    im.alpha_composite(Image.new("RGBA", im.size, (6, 8, 22, 70)))
    # itch.io crops this image to the embed's size, so the text stays near the middle: the title above the
    # Run game button, the hint below it.
    band = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(band).rectangle([0, 140 * SS, w * SS, 230 * SS], fill=(6, 8, 22, 110))
    im.alpha_composite(band.filter(ImageFilter.GaussianBlur(30 * SS)))
    text(im, "SHIFT+SHIFT", 60, (w * SS / 2, 180 * SS), COL["white"], bold, glow=(92, 225, 230), stroke=1)
    text(im, "Click to play  ·  headphones on", 22, (w * SS / 2, 478 * SS), COL["white"], med, stroke=1)
    finish(im, w, h, os.path.join(a.out, "embed_bg_960x600.png"))

    # Cover 630x500 (itch.io's recommended size): title over the scene, the two keys at the bottom.
    w, h = 630, 500
    im = scene_image(w, h, 0.47, 0.5)
    top = Image.new("RGBA", im.size, (0, 0, 0, 0))
    dt = ImageDraw.Draw(top)
    for y in range(int(im.size[1] * 0.3)):
        dt.line([(0, y), (im.size[0], y)], fill=(6, 8, 22, int(200 * (1 - y / (im.size[1] * 0.3)))))
    for y in range(int(im.size[1] * 0.7), im.size[1]):
        t = (y - im.size[1] * 0.7) / (im.size[1] * 0.3)
        dt.line([(0, y), (im.size[0], y)], fill=(6, 8, 22, int(210 * t)))
    im.alpha_composite(top)
    for i, c in enumerate(["lane", "slide", "sprint", "jump", "phase", "charge"]):
        x0 = (w - 6 * 50 + 10) / 2 + i * 50
        ImageDraw.Draw(im).rectangle([x0 * SS, 26 * SS, (x0 + 40) * SS, 31 * SS], fill=COL[c])
    text(im, "SHIFT+SHIFT", 72, (w * SS / 2, 78 * SS), COL["white"], bold, glow=(92, 225, 230))
    text(im, "Two keys. Seven moves. One beat.", 22, (w * SS / 2, 128 * SS), (201, 209, 216), med)
    keycap(im, (w / 2 - 95) * SS, 452 * SS, "⇧  LEFT", COL["sprint"], bold, 0.95)
    text(im, "+", 32, (w / 2 * SS, 452 * SS), COL["white"], bold)
    keycap(im, (w / 2 + 95) * SS, 452 * SS, "RIGHT  ⇧", COL["jump"], bold, 0.95)
    finish(im, w, h, os.path.join(a.out, "cover_630x500.png"))


if __name__ == "__main__":
    main()
