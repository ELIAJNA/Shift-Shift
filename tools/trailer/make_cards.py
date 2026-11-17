"""Shift+Shift trailer: makes the caption cards and the end card (1920x1080 PNG) for Docs/TRAILER.md.

    python tools/trailer/make_cards.py --out Recordings/trailer_cards

Needs Pillow (pip install pillow). Font: Inter Display (SIL Open Font License) if found in --fonts, else Arial Bold
(Windows) or DejaVu Sans Bold (Linux). Writes cards.json: [file, start s, length s, what goes under it].
Captions are transparent PNGs to lay over the footage; black cards and the end card are opaque.
"""
import argparse, json, os, sys
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_itch_art  # the title block (tiles, leaping runner, 3D title), shared with the menu look and itch.io art

W, H = 1920, 1080
BAR = 4 * 60 / 190  # one bar of Celebration and Fear = 1.263 s
COL = {"lane": "#F27017", "slide": "#FCC41E", "sprint": "#00FCF9", "jump": "#F40E9D",
       "phase": "#3D8BFF", "charge": "#F21E3D", "white": "#F7F7F2"}

# (start bar, bars, caption lines [(text, colour, size)], placement, footage; None footage = black card,
#  None lines = end card). Bar 0 = 25.19 s into the full track.
SHOTS = [
    (0, 2, [("YOU HAVE TWO KEYS.", "white", 120)], "mid", None),
    (2, 2, [("LEFT SHIFT.  RIGHT SHIFT.", "white", 96)], "low", "Level 1: the runner at the start, count-in, nothing pressed"),
    (4, 2, [("THAT'S ALL YOU GET.", "white", 96)], "low", "Any level: the 4-3-2-1 count-in, close on the runner"),
    (6, 2, [("NOW KEEP UP.", "white", 140)], "mid", None),
    (8, 1, [("TAP", "lane", 130), ("DODGE", "white", 64)], "low", "Level 1: a lane change past an orange block"),
    (9, 1, [("HOLD LEFT", "slide", 110), ("SLIDE", "white", 64)], "low", "Level 2: slide under a yellow low bar"),
    (10, 1, [("HOLD RIGHT", "sprint", 110), ("SPRINT", "white", 64)], "low", "Level 2: sprint through a cyan gate"),
    (11, 1, [("BOTH", "jump", 130), ("JUMP", "white", 64)], "low", "Level 3: jump a gap, water below"),
    (12, 2, [("HOLD BOTH", "phase", 110), ("PHASE", "white", 64)], "low", "Level 4: phase through a blue wall past the pink ghost blocks"),
    (14, 2, [("L  R  L  R", "charge", 120), ("SMASH", "white", 64)], "low", "Level 5: four taps, flame, smash the brick wall at the end of the shot"),
    (16, 4, [("SIX LEVELS", "white", 110)], "low", "Montage, 1 bar each: level 3, level 4, level 5, level 6"),
    (20, 4, [("EVERY OBSTACLE.", "white", 96), ("ON THE BEAT.", "white", 96)], "low", "A clean run: PERFECT pop-ups, streak, multiplier (2 cuts of 2 bars)"),
    (24, 4, [], "low", "Level 6 at full speed, 1-bar cuts: bend, crest, tunnel, archways (no caption)"),
    (28, 4, [("SEVEN MOVES.", "white", 110), ("ONE BEAT.", "white", 110)], "low", "Level 6, 1-bar cuts: phase, flame and smash, a palette change"),
    (32, 3, None, "end", None),
]


def find_font(dirs, names):
    for d in dirs:
        for n in names:
            p = os.path.join(d, n)
            if os.path.isfile(p):
                return p
    raise SystemExit(f"No font found ({names[0]}...). Pass --fonts with Inter Display, or install Arial / DejaVu.")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--fonts", default="", help="folder with InterDisplay-Bold.otf / InterDisplay-Medium.otf")
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    dirs = [a.fonts] if a.fonts else []
    dirs += [r"C:\Windows\Fonts", "/usr/share/fonts/opentype/inter", "/usr/share/fonts/truetype/dejavu"]
    bold = find_font(dirs, ["InterDisplay-Bold.otf", "Inter-Bold.otf", "arialbd.ttf", "DejaVuSans-Bold.ttf"])
    med = find_font(dirs, ["InterDisplay-Medium.otf", "Inter-Medium.otf", "arial.ttf", "DejaVuSans.ttf"])
    font = lambda p, s: ImageFont.truetype(p, s)

    def card(lines, where, opaque):
        im = Image.new("RGBA", (W, H), (0, 0, 0, 255) if opaque else (0, 0, 0, 0))
        if not lines:
            return im
        hs = [font(bold, s).getbbox(t)[3] - font(bold, s).getbbox(t)[1] for t, c, s in lines]
        gap = 24
        total = sum(hs) + gap * (len(lines) - 1)
        y = (H - total) // 2 if where == "mid" else H - total - 120
        if where != "mid" and not opaque:  # soft shade behind lower captions
            sh = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            ImageDraw.Draw(sh).rectangle([0, y - 70, W, H], fill=(0, 0, 0, 110))
            im = Image.alpha_composite(im, sh)
        d = ImageDraw.Draw(im)
        for (t, c, s), h in zip(lines, hs):
            f = font(bold, s)
            bb = f.getbbox(t)
            d.text(((W - (bb[2] - bb[0])) // 2 - bb[0], y - bb[1]), t, font=f, fill=COL[c],
                   stroke_width=3, stroke_fill=(0, 0, 0, 200))
            y += h + gap
        return im

    def endcard():
        # The game's title block (day 27: tiles, the runner leaping over the +, 3D title, tagline), drawn at double
        # size and scaled down, then the call to action.
        ss = make_itch_art.SS
        big = Image.new("RGBA", (W * ss, H * ss), (16, 20, 24, 255))
        make_itch_art.logo_block(big, W * ss / 2, 520 * ss, 1.9, bold, med, "A Rhythm Runner Played With Just Two Keys")
        im = big.resize((W, H), Image.LANCZOS)
        d = ImageDraw.Draw(im)

        def ctext(t, f, y, fill):
            bb = f.getbbox(t)
            d.text(((W - (bb[2] - bb[0])) // 2 - bb[0], y), t, font=f, fill=fill)
        ctext("Play free in your browser  ·  itch.io", font(bold, 58), 760, "#00FCF9")
        ctext("GitHub Game Off 2026", font(med, 40), 860, "#8A939C")
        return im

    rows = []
    for i, (start, bars, lines, where, footage) in enumerate(SHOTS, 1):
        t0, dur = start * BAR, bars * BAR
        if lines is None:
            name, im, what = f"{i:02d}_end_card.png", endcard(), "end card (opaque)"
        elif footage is None:
            name, im, what = f"{i:02d}_black_card.png", card(lines, where, True), "black card (opaque)"
        else:
            name, im, what = f"{i:02d}_caption.png", card(lines, where, False), footage
            if not lines:
                name = None  # footage only
        if name:
            im.save(os.path.join(a.out, name))
        rows.append([name, round(t0, 4), round(dur, 4), what])
    json.dump(rows, open(os.path.join(a.out, "cards.json"), "w"), indent=1)
    print(f"{sum(1 for r in rows if r[0])} cards in {a.out}; font {os.path.basename(bold)}")


if __name__ == "__main__":
    main()
