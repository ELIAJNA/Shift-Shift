using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// The shared background panel for every screen (testers, day 29): a dark, slightly see-through card with rounded
    /// corners, a thin cyan edge and a soft cyan glow around it. One texture made in code (no image file), drawn
    /// 9-sliced so any size keeps the same corners and edge.
    /// </summary>
    public static class UIPanel
    {
        private const int Size = 96, Inset = 14, Radius = 16, Slice = 34;
        private static GUIStyle _style;

        /// <summary>Draws the panel so its card fills <paramref name="r"/> (the glow reaches a little outside).</summary>
        public static void Draw(Rect r)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_style == null) Build();
            var outer = new Rect(r.x - Inset, r.y - Inset, r.width + 2 * Inset, r.height + 2 * Inset);
            Color old = GUI.color;
            GUI.color = Color.white;
            _style.Draw(outer, GUIContent.none, false, false, false, false);
            GUI.color = old;
        }

        private static void Build()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            var fill = new Color(0.035f, 0.05f, 0.1f, 0.86f);
            var edge = new Color(0f, 0.98f, 0.97f, 0.75f);
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float d = RoundedRectDistance(x + 0.5f, y + 0.5f); // < 0 inside the card
                Color c;
                if (d < -1.5f) c = fill;
                else if (d < 0.5f)                                      // the edge line, anti-aliased on both sides
                {
                    float inner = Mathf.Clamp01(-d - 0.5f);             // 1 deep inside the line's inner side
                    c = Color.Lerp(edge, fill, inner);
                    c.a = Mathf.Lerp(edge.a, fill.a, inner) * Mathf.Clamp01(0.5f - d + 0.5f);
                }
                else
                {
                    float g = Mathf.Clamp01(1f - d / Inset);           // the soft glow outside
                    c = new Color(0f, 0.98f, 0.97f, 0.2f * g * g);
                }
                px[y * Size + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            _style = new GUIStyle { border = new RectOffset(Slice, Slice, Slice, Slice) };
            _style.normal.background = tex;
        }

        // Signed distance to the card: a rounded rectangle inset from the texture's edge.
        private static float RoundedRectDistance(float x, float y)
        {
            float half = Size * 0.5f - Inset;
            float qx = Mathf.Abs(x - Size * 0.5f) - (half - Radius);
            float qy = Mathf.Abs(y - Size * 0.5f) - (half - Radius);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - Radius;
        }
    }
}
