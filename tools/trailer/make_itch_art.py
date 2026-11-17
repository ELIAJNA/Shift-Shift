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


def runner_running(im, sc, x, z):
    """The boxy runner from behind, mid-stride (one leg planted, the other kicking back, arms swinging), with dust puffs
    at the heels as in the game."""
    smoke = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(smoke)
    rnd = random.Random(11)
    k = sc.focal / z / 250
    for i in range(40):                        # puffs trailing back toward the camera from the planted foot
        back = rnd.random() ** 1.2 * 1.6
        px, py = sc.p(x - 0.08 + rnd.gauss(0, 0.07) * (1 + back), 0.04 + back * 0.12 + rnd.uniform(0, 0.08), z - back)
        r = (9 + back * 16) * SS * k * rnd.uniform(0.7, 1.2)
        g = rnd.randint(170, 215)
        d.ellipse([px - r, py - r * 0.8, px + r, py + r * 0.8], fill=(g, g, g + 8, int(200 * (1 - back / 1.8))))
    im.alpha_composite(smoke.filter(ImageFilter.GaussianBlur(5 * SS)))
    body, shade = (240, 240, 236), (196, 198, 200)
    box(im, sc, x + 0.03, x + 0.12, 0.22, 0.46, z - 0.18, 0.1, shade, glow=False)   # right leg kicking back, foot up
    box(im, sc, x + 0.03, x + 0.12, 0.18, 0.26, z - 0.28, 0.12, shade, glow=False)  # its foot, sole toward us
    box(im, sc, x + 0.14, x + 0.2, 0.46, 0.74, z - 0.12, 0.08, shade, glow=False)   # right arm swinging back
    box(im, sc, x - 0.13, x + 0.13, 0.42, 0.85, z, 0.12, body, glow=False)            # torso
    box(im, sc, x - 0.09, x + 0.09, 0.88, 1.06, z, 0.12, body, glow=False)            # head
    box(im, sc, x - 0.12, x - 0.03, 0.0, 0.44, z + 0.05, 0.1, body, glow=False)       # left leg planted
    box(im, sc, x - 0.21, x - 0.15, 0.58, 0.86, z + 0.12, 0.08, body, glow=False)     # left arm swinging forward, up


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
    items = {  # (lane or None for all, kind, z)
        "full": [(None, "charge", 17), (-1, "lane", 7.5)],
        "slide_phase": [(None, "phase", 18), (None, "slide", 9)],   # the game frame (ELIA, day 27: not the same block and wall)
    }[set_]
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
            box(im, sc, -L - 0.32, -L + 0.32, 0, 1.0, z, 0.6, (255, 181, 217), alpha=230)  # a ghost block, left lane
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


def scene_image(w, h, horizon_frac, vx_frac, full=True, runner_on=True, scenery=True, runner_z=4.4,
                obstacle_set="full", running=False):
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
        obstacles(im, sc, obstacle_set)
    if runner_on:
        if running:
            runner_running(im, sc, 0.0, runner_z)
        else:
            runner(im, sc, 0.0, runner_z)
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


TILE_ORDER = ["lane", "slide", "sprint", "jump", "phase", "charge"]
TILE_COL = dict(COL, jump=(255, 79, 232))  # the title pink, lighter than the game's jump pink


def logo_geom(x, title_cy, k, bold, align="centre"):
    """Where the title block's parts sit (shared by the static art and the animated cover)."""
    f = ImageFont.truetype(bold, int(84 * k * SS))
    full, wS = f.getlength("SHIFT+SHIFT"), f.getlength("SHIFT")
    wP = full - 2 * wS
    left = x - full / 2 if align == "centre" else x
    ty, th = title_cy - 84 * k * SS, 14 * k * SS
    tiles = []
    for g in range(2):
        a = left + g * (wS + wP)
        tw = wS / (3 + 2 * 0.16)
        for i in range(3):
            tiles.append((a + i * tw * 1.16, a + i * tw * 1.16 + tw, TILE_COL[TILE_ORDER[g * 3 + i]]))
    x3, x4 = (tiles[2][0] + tiles[2][1]) / 2, (tiles[3][0] + tiles[3][1]) / 2
    return dict(f=f, left=left, x=x, cy=title_cy, k=k, align=align, bold=bold, ty=ty, th=th, tiles=tiles,
                x3=x3, x4=x4, top=ty - 4 * k * SS, apex=42 * k * SS)


def draw_tiles(im, g, flash=None):
    """The six tiles. flash = (tile index, 0..1) brightens one tile (the landing in the animated cover)."""
    k, ty, th = g["k"], g["ty"], g["th"]
    glow = Image.new("RGBA", im.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for i, (x0, x1, c) in enumerate(g["tiles"]):
        a = 200
        if flash and flash[0] == i:
            a = min(255, int(200 + 55 * flash[1]))
            e = 8 * k * SS * flash[1]
            gd.rounded_rectangle([x0 - e, ty - e, x1 + e, ty + th + e], radius=6 * k * SS, fill=c + (int(160 * flash[1]),))
        gd.rounded_rectangle([x0, ty, x1, ty + th], radius=4 * k * SS, fill=c + (a,))
    im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(10 * k * SS)))
    d = ImageDraw.Draw(im)
    for i, (x0, x1, c) in enumerate(g["tiles"]):
        lit = lerp(c, (255, 255, 255), 0.45 * flash[1]) if flash and flash[0] == i else c
        d.rounded_rectangle([x0 + 3 * k * SS, ty + 5 * k * SS, x1 + 3 * k * SS, ty + th + 5 * k * SS], radius=4 * k * SS,
                            fill=lerp(c, (0, 0, 0), 0.6))
        d.rounded_rectangle([x0, ty, x1, ty + th], radius=4 * k * SS, fill=lit)
        d.rounded_rectangle([x0, ty, x1, ty + th * 0.4], radius=4 * k * SS, fill=lerp(lit, (255, 255, 255), 0.35))


def draw_arc(im, g, upto=1.0):
    """Dotted jump arc from the 3rd tile to the 4th, up to fraction `upto` (the runner's trail); the top is left clear."""
    d = ImageDraw.Draw(im)
    for n in range(2, 30, 2):
        t = n / 30
        if abs(t - 0.5) <= 0.12 or t > upto:
            continue
        px, py = g["x3"] + (g["x4"] - g["x3"]) * t, g["top"] - 4 * g["apex"] * t * (1 - t)
        r = 2.2 * g["k"] * SS
        d.ellipse([px - r, py - r, px + r, py + r], fill=(190, 225, 232, 140))


def draw_runner(im, g, t=0.5, alpha=1.0, streaks=True):
    """The boxy runner, feet on the jump arc at fraction t (0 = on the 3rd tile, 0.5 = over the +, 1 = on the 4th).
    Near limbs white, far limbs grey and behind the body (ELIA, day 27)."""
    if alpha <= 0:
        return
    k = g["k"]
    L = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(L)
    u = 7.6 * k * SS
    fx = g["x3"] + (g["x4"] - g["x3"]) * t
    fy = g["top"] - 4 * g["apex"] * t * (1 - t) + 6 * k * SS
    hip = (fx - 0.6 * u, fy - 2.8 * u)
    if streaks:
        for dy, ln in [(-5.5, 5), (-3.6, 8), (-1.7, 4)]:
            d.rectangle([hip[0] - 3.6 * u - ln * u, hip[1] + dy * u - 0.28 * u, hip[0] - 3.0 * u, hip[1] + dy * u + 0.28 * u],
                        fill=(0, 250, 247, 130))

    def seg(p, ang, ln, wd, col):
        a = math.radians(ang)
        dx, dy = math.sin(a), math.cos(a)
        nx, ny = -dy, dx
        q = (p[0] + dx * ln, p[1] + dy * ln)
        hw = wd / 2
        d.polygon([(p[0] + nx * hw, p[1] + ny * hw), (q[0] + nx * hw, q[1] + ny * hw),
                   (q[0] - nx * hw, q[1] - ny * hw), (p[0] - nx * hw, p[1] - ny * hw)], fill=col)
        return q

    white, grey = (247, 247, 242), (150, 160, 172)
    torso = 160
    a = math.radians(torso)
    neck = (hip[0] + math.sin(a) * 4.4 * u, hip[1] + math.cos(a) * 4.4 * u)
    shoulder = (hip[0] + (neck[0] - hip[0]) * 0.85, hip[1] + (neck[1] - hip[1]) * 0.85)
    limbs = [(hip, -55, -110, 2.4, 2.2, 1.2, False), (shoulder, 60, 120, 2.5, 2.3, 1.0, False),
             (hip, 75, 20, 2.4, 2.2, 1.25, True), (shoulder, -50, -100, 2.5, 2.3, 1.0, True)]

    def limb(l, col):
        p, a1, a2, l1, l2, wd, _ = l
        kn = seg(p, a1, l1 * u, wd * u, col)
        seg(kn, a2, l2 * u, wd * 0.92 * u, col)

    for l in limbs:
        if l[6]:
            limb(l, grey)  # far side, behind the body
    seg(hip, torso, 4.4 * u, 2.6 * u, white)
    head = (neck[0] + math.sin(a) * 1.3 * u, neck[1] + math.cos(a) * 1.3 * u)
    seg((head[0] - math.sin(a) * 1.1 * u, head[1] - math.cos(a) * 1.1 * u), torso, 2.2 * u, 2.2 * u, white)
    for l in limbs:
        if not l[6]:
            limb(l, white)
    if alpha < 1:
        L.putalpha(L.getchannel("A").point(lambda v: int(v * alpha)))
    im.alpha_composite(L)


def draw_title(im, g, tagline):
    """The 3D title (white face, cyan sides, faint glow) and the tagline with a small dark shadow. Returns its bottom."""
    k, cy, f = g["k"], g["cy"], g["f"]
    anchor = "mm" if g["align"] == "centre" else "lm"
    tx = g["x"] if g["align"] == "centre" else g["left"]
    # The same depth as the menu (12 layers of 1.1 px at scale k), drawn in 1 px steps so large sizes show no stripes.
    depth, step = max(3, round(12 * 1.1 * k * SS)), 1.0

    def layer(dx, dy, col, font=f, xy=None):
        L = Image.new("RGBA", im.size, (0, 0, 0, 0))
        ImageDraw.Draw(L).text(xy or (tx + dx, cy + dy), "SHIFT+SHIFT" if xy is None else tagline, font=font, fill=col,
                               anchor=anchor)
        im.alpha_composite(L)
    for i in range(depth, 0, -1):
        layer(i * step * 0.6, i * step, lerp((0, 191, 199), (5, 31, 41), (i - 1) / max(1, depth - 1)) + (255,))
    for gx in (-2, 0, 2):
        for gy in (-2, 0, 2):
            if gx or gy:
                layer(gx * SS, gy * SS, (0, 250, 247, 46))
    layer(0, 0, (247, 247, 242, 255))
    tf = ImageFont.truetype(g["bold"], int(22 * k * SS))
    ty2 = cy + 70 * k * SS
    tx2 = g["x"] if g["align"] == "centre" else g["left"] + 4 * SS
    for i in (2, 1):
        layer(0, 0, (10, 14, 28, 255), tf, (tx2 + i * 0.6 * k * SS, ty2 + i * k * SS))
    layer(0, 0, (236, 240, 245, 255), tf, (tx2, ty2))
    return ty2 + 16 * k * SS


def logo_block(im, x, title_cy, k, bold, med, tagline, align="centre"):
    """The menu's title block (MainMenu.DrawLogo, day 27): six tiles over the two SHIFTs (none over the +), the boxy
    runner leaping from the 3rd tile to the 4th, the 3D title (white face, cyan sides, faint glow) and the tagline
    with a small dark shadow. Sizes as in the menu at scale k; x is the title's centre, or its left edge."""
    g = logo_geom(x, title_cy, k, bold, align)
    draw_tiles(im, g)
    draw_arc(im, g)
    draw_runner(im, g, 0.5)
    return draw_title(im, g, tagline)


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

    # Banner 960x360: road and obstacles on the right; the title block (as in the menu) and the two keys on the left.
    w, h = 960, 360
    im = scene_image(w, h, 0.42, 0.74)
    shade = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ds = ImageDraw.Draw(shade)
    for x in range(int(im.size[0] * 0.62)):
        ds.line([(x, 0), (x, im.size[1])], fill=(6, 8, 22, int(235 * (1 - x / (im.size[0] * 0.62)) ** 0.6)))
    im.alpha_composite(shade)
    logo_block(im, 40 * SS, 188 * SS, 0.9, bold, med, "A Rhythm Runner Played With Just Two Keys", align="left")
    keycap(im, (44 + 75) * SS, 314 * SS, "⇧  LEFT", COL["sprint"], bold, 1.0)
    text(im, "+", 34, ((44 + 175) * SS, 314 * SS), COL["white"], bold)
    keycap(im, (44 + 275) * SS, 314 * SS, "RIGHT  ⇧", COL["jump"], bold, 1.0)
    finish(im, w, h, os.path.join(a.out, "banner_960x360.png"))

    # Page background 1920x1080: the scene, darkened, so the page column in the middle stays readable.
    w, h = 1920, 1080
    im = scene_image(w, h, 0.62, 0.5, full=False, runner_on=False)
    im.alpha_composite(Image.new("RGBA", im.size, (6, 8, 22, 120)))
    vignette(im, 0.5)
    finish(im, w, h, os.path.join(a.out, "background_1920x1080.png"))

    # Embed background 960x600: shown behind the "Run game" button.
    w, h = 960, 600
    im = scene_image(w, h, 0.62, 0.5, runner_z=5.0, obstacle_set="slide_phase", running=True)  # scene low: the Run game button (frame centre) sits above the runner
    im.alpha_composite(Image.new("RGBA", im.size, (6, 8, 22, 70)))
    # No title here (ELIA, day 27: the banner's logo sits right above the game frame, a second title repeated it).
    # Instead the hook line and, right under it, the hint, above the Run game button, where the title was (fully
    # visible in itch's frame). itch.io crops this image to the embed's size, so both stay near the middle.
    band = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(band).rectangle([0, 122 * SS, w * SS, 220 * SS], fill=(6, 8, 22, 130))
    im.alpha_composite(band.filter(ImageFilter.GaussianBlur(24 * SS)))
    text(im, "You've pressed Shift a million times. Never like this.", 24, (w * SS / 2, 156 * SS), COL["white"], bold,
         glow=(92, 225, 230), stroke=1)
    text(im, "Click to play  ·  headphones on", 22, (w * SS / 2, 192 * SS), COL["white"], med, stroke=1)  # right under the hook (ELIA)
    finish(im, w, h, os.path.join(a.out, "embed_bg_960x600.png"))

    # Cover 630x500 (itch.io's recommended size): title over the scene, the two keys at the bottom.
    w, h = 630, 500
    im = scene_image(w, h, 0.6, 0.5)
    top = Image.new("RGBA", im.size, (0, 0, 0, 0))
    dt = ImageDraw.Draw(top)
    for y in range(int(im.size[1] * 0.3)):
        dt.line([(0, y), (im.size[0], y)], fill=(6, 8, 22, int(200 * (1 - y / (im.size[1] * 0.3)))))
    for y in range(int(im.size[1] * 0.7), im.size[1]):
        t = (y - im.size[1] * 0.7) / (im.size[1] * 0.3)
        dt.line([(0, y), (im.size[0], y)], fill=(6, 8, 22, int(210 * t)))
    im.alpha_composite(top)
    logo_block(im, w * SS / 2, 170 * SS, 0.8, bold, med, "Two Keys. Seven Moves. One Beat.")
    keycap(im, (w / 2 - 95) * SS, 452 * SS, "⇧  LEFT", COL["sprint"], bold, 0.95)
    text(im, "+", 32, (w / 2 * SS, 452 * SS), COL["white"], bold)
    keycap(im, (w / 2 + 95) * SS, 452 * SS, "RIGHT  ⇧", COL["jump"], bold, 0.95)
    finish(im, w, h, os.path.join(a.out, "cover_630x500.png"))
    cover_gif(os.path.join(a.out, "cover_630x500.gif"), bold, med)


def cover_gif(path, bold, med, fps=20):
    """Animated cover (ELIA, day 27), a 2.2 s loop: the runner waits on the 3rd tile, leaps over the + leaving a dotted
    trail, lands on the 4th tile (it flashes, pink sparks burst like a PERFECT), then fades back to the start.
    Everything else is the static cover."""
    w, h = 630, 500
    base = scene_image(w, h, 0.6, 0.5)
    top = Image.new("RGBA", base.size, (0, 0, 0, 0))
    dt = ImageDraw.Draw(top)
    for y in range(int(base.size[1] * 0.3)):
        dt.line([(0, y), (base.size[0], y)], fill=(6, 8, 22, int(200 * (1 - y / (base.size[1] * 0.3)))))
    for y in range(int(base.size[1] * 0.7), base.size[1]):
        t = (y - base.size[1] * 0.7) / (base.size[1] * 0.3)
        dt.line([(0, y), (base.size[0], y)], fill=(6, 8, 22, int(210 * t)))
    base.alpha_composite(top)
    g = logo_geom(w * SS / 2, 170 * SS, 0.8, bold)
    draw_title(base, g, "Two Keys. Seven Moves. One Beat.")
    keycap(base, (w / 2 - 95) * SS, 452 * SS, "⇧  LEFT", COL["sprint"], bold, 0.95)
    text(base, "+", 32, (w / 2 * SS, 452 * SS), COL["white"], bold)
    keycap(base, (w / 2 + 95) * SS, 452 * SS, "RIGHT  ⇧", COL["jump"], bold, 0.95)

    frames, n = [], int(2.2 * fps)
    wait, fly, hold = int(0.3 * fps), int(0.6 * fps), int(0.75 * fps)   # then the fade back
    land = wait + fly
    rnd = random.Random(4)
    tile4 = g["tiles"][3]
    sparks = [(rnd.uniform(-1, 1) * 260, -rnd.uniform(160, 420), rnd.uniform(3, 6)) for _ in range(22)]
    for f in range(n):
        im = base.copy()
        flash = None
        if land <= f < land + hold:
            flash = (3, 1 - (f - land) / hold)
        draw_tiles(im, g, flash)
        if f < wait:
            t, a_now, a_back = 0.0, 1.0, 0.0
        elif f < land:
            x = (f - wait) / fly
            t, a_now, a_back = x * x * (3 - 2 * x), 1.0, 0.0   # eased leap
        elif f < land + hold:
            t, a_now, a_back = 1.0, 1.0, 0.0
        else:
            x = (f - land - hold) / max(1, n - land - hold)
            t, a_now, a_back = 1.0, 1 - x, x                    # fade out on the 4th tile, back in on the 3rd
        if f < land + hold:
            draw_arc(im, g, t)
        draw_runner(im, g, t, a_now, streaks=wait <= f < land)
        if a_back > 0:
            draw_runner(im, g, 0.0, a_back, streaks=False)
        if land <= f < land + hold:                             # PERFECT sparks from the landing tile
            age = (f - land) / fps
            d = ImageDraw.Draw(im)
            cx, cy = (tile4[0] + tile4[1]) / 2, g["ty"]
            for vx, vy, size in sparks:
                px = cx + vx * age * SS * 0.8
                py = cy + (vy * age + 600 * age * age) * SS * 0.8
                sz = size * SS * (1 - age / (hold / fps) * 0.7)
                c = lerp(tile4[2], (255, 255, 255), 0.3)
                fade = 1 - (f - land) / hold
                d.rectangle([px - sz / 2, py - sz / 2, px + sz / 2, py + sz / 2], fill=c + (int(255 * fade),))
        frames.append(im.convert("RGB").resize((w, h), Image.LANCZOS))
    pal = frames[land + 2].quantize(colors=255, method=Image.MEDIANCUT)
    q = [fr.quantize(palette=pal, dither=Image.Dither.NONE) for fr in frames]
    q[0].save(path, save_all=True, append_images=q[1:], duration=int(1000 / fps), loop=0, optimize=True, disposal=1)
    print(path, len(frames), "frames", os.path.getsize(path) // 1024, "KB")


if __name__ == "__main__":
    main()
