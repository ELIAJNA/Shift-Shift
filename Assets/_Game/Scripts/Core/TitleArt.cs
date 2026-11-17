using System;
using System.Collections.Generic;

namespace ShiftShift.Core
{
    /// <summary>
    /// The SHIFT+SHIFT title lettering (ELIA, day 27: "like the Shift on a gaming keyboard, but bold"). An original
    /// squared design drawn from a few straight-edged shapes per letter, heavy strokes and diagonal cuts on the corners;
    /// not a copy of any brand's typeface, and no font file or licence needed.
    /// Rendered here into RGBA pixels with everything baked in: each mirrored letter pair in an action colour, a dark
    /// 3D extrusion, an outline, and a satin sheen. Engine-free so it can be previewed and tested outside Unity;
    /// MainMenu turns the pixels into a texture once.
    /// </summary>
    public static class TitleArt
    {
        private const double Cap = 12;     // letter height in grid units
        private const double Gap = 1.6;    // space between letters

        private struct Pt { public double X, Y; public Pt(double x, double y) { X = x; Y = y; } }

        // Convex shapes per glyph, in grid units, y up, counter-clockwise. Stroke 3, cap 12, corner cut 2.5.
        private static readonly Dictionary<char, (double width, Pt[][] shapes)> Glyphs = new Dictionary<char, (double, Pt[][])>
        {
            ['S'] = (8.5, new[]
            {
                P(0, 9, 8.5, 9, 8.5, 12, 2.5, 12, 0, 9.5),   // top bar, top-left corner cut
                P(0, 4.5, 3, 4.5, 3, 9, 0, 9),               // upper left stroke
                P(0, 4.5, 8.5, 4.5, 8.5, 7.5, 0, 7.5),       // middle bar
                P(5.5, 3, 8.5, 3, 8.5, 7.5, 5.5, 7.5),       // lower right stroke
                P(0, 0, 6, 0, 8.5, 2.5, 8.5, 3, 0, 3),       // bottom bar, bottom-right corner cut
            }),
            ['H'] = (8.5, new[]
            {
                P(0, 0, 3, 0, 3, 12, 1.5, 12, 0, 10.5),      // left stem, top cut
                P(5.5, 0, 7, 0, 8.5, 1.5, 8.5, 12, 5.5, 12), // right stem, bottom cut
                P(3, 4.5, 5.5, 4.5, 5.5, 7.5, 3, 7.5),       // crossbar
            }),
            ['I'] = (3, new[]
            {
                P(0, 0, 1.5, 0, 3, 1.5, 3, 12, 1.5, 12, 0, 10.5),
            }),
            ['F'] = (8, new[]
            {
                P(0, 9, 8, 9, 8, 12, 2.5, 12, 0, 9.5),       // top bar, top-left corner cut
                P(0, 0, 3, 0, 3, 9, 0, 9),                   // stem
                P(3, 4.5, 6.5, 4.5, 6.5, 7.5, 3, 7.5),       // middle bar
            }),
            ['T'] = (8.5, new[]
            {
                P(0, 9, 8.5, 9, 8.5, 12, 2.5, 12, 0, 9.5),   // top bar, top-left corner cut
                P(2.75, 0, 5.75, 0, 5.75, 9, 2.75, 9),       // stem
            }),
            ['+'] = (7, new[]
            {
                P(0, 4.5, 7, 4.5, 7, 7.5, 0, 7.5),
                P(2, 1, 5, 1, 5, 11, 2, 11),
            }),
        };

        private static Pt[] P(params double[] xy)
        {
            var pts = new Pt[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new Pt(xy[2 * i], xy[2 * i + 1]);
            return pts;
        }

        public const string Text = "SHIFT+SHIFT";

        /// <summary>Mirrored pairs, outside in: lane orange, slide yellow, sprint cyan, jump pink (lighter), phase blue; + charge red.</summary>
        private static readonly byte[][] PairColours =
        {
            new byte[] { 0xF2, 0x70, 0x17 }, new byte[] { 0xFC, 0xC4, 0x1E }, new byte[] { 0x00, 0xFC, 0xF9 },
            new byte[] { 0xFF, 0x4F, 0xE8 }, new byte[] { 0x3D, 0x8B, 0xFF }, new byte[] { 0xF2, 0x1E, 0x3D },
        };

        /// <summary>
        /// Renders the title. <paramref name="capPixels"/> = letter height in pixels. Returns RGBA, rows from the TOP
        /// (flip for Unity's bottom-up textures), transparent background.
        /// </summary>
        public static byte[] Render(int capPixels, out int width, out int height)
        {
            if (capPixels < 8) throw new ArgumentOutOfRangeException(nameof(capPixels));
            double unit = capPixels / Cap;
            int depth = Math.Max(3, (int)Math.Round(capPixels * 0.09));   // extrusion length in pixels
            int outline = Math.Max(1, (int)Math.Round(capPixels * 0.022));
            int pad = outline + 2;

            // Lay out the letters.
            var placed = new List<(double x, char c, int colour)>();
            double cursor = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                char c = Text[i];
                double extra = c == '+' || (i > 0 && Text[i - 1] == '+') ? 0.6 : 0;
                cursor += i > 0 ? Gap + extra : 0;
                placed.Add((cursor, c, Math.Min(i, Text.Length - 1 - i)));
                cursor += Glyphs[c].width;
            }
            int faceW = (int)Math.Ceiling(cursor * unit);
            width = faceW + 2 * pad + (int)Math.Ceiling(depth * 0.6);
            height = capPixels + 2 * pad + depth;
            int w = width, h = height;

            // Face coverage (4x4 samples per pixel) and which letter covers each pixel.
            var cover = new float[w * h];
            var owner = new int[w * h];
            for (int i = 0; i < owner.Length; i++) owner[i] = -1;
            for (int li = 0; li < placed.Count; li++)
            {
                var (gx, ch, _) = placed[li];
                var (gw, shapes) = Glyphs[ch];
                int x0 = pad + (int)Math.Floor(gx * unit) - 1, x1 = pad + (int)Math.Ceiling((gx + gw) * unit) + 1;
                for (int py = pad - 1; py <= pad + capPixels + 1; py++)
                for (int px = Math.Max(0, x0); px <= Math.Min(w - 1, x1); px++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 4; sy++)
                    for (int sx = 0; sx < 4; sx++)
                    {
                        double ux = (px - pad + (sx + 0.5) / 4) / unit - gx;
                        double uy = Cap - (py - pad + (sy + 0.5) / 4) / unit; // grid y runs up
                        foreach (Pt[] s in shapes)
                            if (Inside(s, ux, uy)) { hits++; break; }
                    }
                    if (hits == 0) continue;
                    int k = py * w + px;
                    float c = hits / 16f;
                    if (c > cover[k]) { cover[k] = c; owner[k] = placed[li].colour; }
                }
            }

            float At(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? 0f : cover[y * w + x];

            var rgba = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Back to front: extrusion, outline, face.
                double r = 0, g = 0, b = 0, a = 0;
                void Over(double cr, double cg, double cb, double ca)
                {
                    double na = ca + a * (1 - ca);
                    if (na <= 0) return;
                    r = (cr * ca + r * a * (1 - ca)) / na; g = (cg * ca + g * a * (1 - ca)) / na; b = (cb * ca + b * a * (1 - ca)) / na;
                    a = na;
                }
                for (int d = depth; d >= 1; d--)
                {
                    float c = At(x - (int)Math.Round(d * 0.6), y - d);
                    if (c <= 0) continue;
                    double t = (d - 1.0) / Math.Max(1, depth - 1);
                    Over(Lerp(34, 8, t), Lerp(44, 10, t), Lerp(78, 20, t), c);
                }
                float ring = 0;
                for (int oy = -outline; oy <= outline; oy++)
                for (int ox = -outline; ox <= outline; ox++)
                    ring = Math.Max(ring, At(x + ox, y + oy));
                if (ring > 0) Over(5, 8, 15, ring * 0.92);

                float face = cover[y * w + x];
                if (face > 0)
                {
                    byte[] col = PairColours[owner[y * w + x]];
                    double t = (y - pad + 0.5) / capPixels; // 0 top .. 1 bottom of the letters
                    double light = t < 0.5 ? 0.55 * (1 - t / 0.5) * (1 - t / 0.5) : 0;
                    if (t > 0.2 && t < 0.28) light += 0.25;
                    double dark = t > 0.55 ? 0.38 * (t - 0.55) / 0.45 : 0;
                    double cr = col[0], cg = col[1], cb = col[2];
                    cr += (255 - cr) * light; cg += (255 - cg) * light; cb += (255 - cb) * light;
                    cr *= 1 - dark; cg *= 1 - dark; cb *= 1 - dark * 0.9;
                    Over(cr, cg, cb, face);
                }
                int o = (y * w + x) * 4;
                rgba[o] = (byte)Math.Round(r); rgba[o + 1] = (byte)Math.Round(g); rgba[o + 2] = (byte)Math.Round(b);
                rgba[o + 3] = (byte)Math.Round(a * 255);
            }
            return rgba;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        /// <summary>Point inside a convex, counter-clockwise polygon.</summary>
        private static bool Inside(Pt[] s, double x, double y)
        {
            for (int i = 0; i < s.Length; i++)
            {
                Pt p = s[i], q = s[(i + 1) % s.Length];
                if ((q.X - p.X) * (y - p.Y) - (q.Y - p.Y) * (x - p.X) < 0) return false;
            }
            return true;
        }
    }
}
