using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>
    /// The menu (playbook 2.5): a row of entries (the levels, then Calibrate and Credits) chosen with the two Shift
    /// keys only: Left / Right Shift = previous / next, both together = open. Every level is open from the start
    /// (jam raters should be able to see any level). The first key press here is also the browser's audio unlock.
    /// IMGUI, like the rest of the UI.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private ShiftInput input;
        [Tooltip("In play order: level 1 first.")]
        [SerializeField] private List<TextAsset> levels = new List<TextAsset>();
        [SerializeField] private string runScene = "Run";
        [SerializeField] private string calibrateScene = "Calibrate";
        [SerializeField, TextArea(6, 20)] private string credits = "Shift+Shift";

        [Header("Sounds (day 23; the setup tool assigns them)")]
        [SerializeField] private AudioClip moveSound;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip backSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 0.5f;
        private static AudioSource _audio; // survives the scene change, so the "open" sound is not cut off

        private void PlayUi(AudioClip clip)
        {
            if (clip == null) return;
            if (_audio == null)
            {
                var go = new GameObject("UI Sound");
                DontDestroyOnLoad(go);
                _audio = go.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
            }
            _audio.PlayOneShot(clip, soundVolume);
        }

        private struct Entry
        {
            public string Title, Detail, LevelAsset;
            public bool Calibrate, Credits, HowTo;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly ShiftMenuKeys _keys = new ShiftMenuKeys();
        private int _index;
        private enum Page { None, Credits, HowTo }
        private Page _page;
        private GUIStyle _title, _big, _mid, _small;

        private void Start()
        {
            Time.timeScale = 1f;
            if (input == null) input = FindAnyObjectByType<ShiftInput>();

            for (int i = 0; i < levels.Count; i++)
            {
                TextAsset json = levels[i];
                if (json == null) continue;
                LevelData level = JsonUtility.FromJson<LevelData>(json.text);
                if (level == null) continue;
                int best = PlayerPrefs.GetInt(GameFlow.BestKeyPrefix + json.name, 0);
                double seconds = level.bpm > 0 ? new TempoMap(level.bpm, level.tempo).TimeAtBeat(level.lengthBeats) : 0;
                _entries.Add(new Entry
                {
                    Title = $"{_entries.Count + 1}   {level.name}",
                    Detail = $"{seconds:0} seconds   ·   " + Status(json.name, best),
                    LevelAsset = json.name,
                });
            }
            float offset = PlayerPrefs.GetFloat(Conductor.UserOffsetKey, 0f);
            _entries.Add(new Entry { Title = "How to play", Detail = "The seven moves, and which obstacle wants which", HowTo = true });
            _entries.Add(new Entry { Title = "Calibrate timing", Detail = $"Tap along to a click to line the game up with your sound (now {offset:+0;-0;0} ms)", Calibrate = true });
            _entries.Add(new Entry { Title = "Credits", Detail = "Who and what made this", Credits = true });

            // Come back to the level just played.
            if (!string.IsNullOrEmpty(GameFlow.LevelToLoad))
                for (int i = 0; i < _entries.Count; i++)
                    if (_entries[i].LevelAsset == GameFlow.LevelToLoad) _index = i;

            _keys.Reset(Down(Side.Left), Down(Side.Right));
        }

        private bool Down(Side side) => input != null && input.IsDown(side);

        private void Update()
        {
            MenuKey key = _keys.Poll(Down(Side.Left), Down(Side.Right));
            // Browser: nothing happens until sound is unlocked by a click (Shift does not count as a gesture there).
            if (!WebAudioUnlock.Ready) return;
            if (key == MenuKey.None) return;
            if (_page != Page.None)
            {
                _page = Page.None; // any key closes the page
                PlayUi(backSound);
                return;
            }
            if (key == MenuKey.Left) { _index = (_index + _entries.Count - 1) % _entries.Count; PlayUi(moveSound); }
            else if (key == MenuKey.Right) { _index = (_index + 1) % _entries.Count; PlayUi(moveSound); }
            else { PlayUi(openSound); Open(_entries[_index]); }
        }

        private void Open(Entry e)
        {
            if (e.Credits) _page = Page.Credits;
            else if (e.HowTo) _page = Page.HowTo;
            else if (e.Calibrate)
            {
                if (Application.CanStreamedLevelBeLoaded(calibrateScene)) SceneManager.LoadScene(calibrateScene);
                else Debug.LogWarning($"[Menu] Scene '{calibrateScene}' is not in the build list.");
            }
            else GameFlow.Play(e.LevelAsset, runScene);
        }

        /// <summary>Best score once cleared; "Failed - Try Again" once played but not cleared (ELIA, day 20).</summary>
        private static string Status(string level, int best)
        {
            bool cleared = best > 0 || PlayerPrefs.GetInt(GameFlow.ClearedKeyPrefix + level, 0) == 1;
            if (cleared) return $"best {best}";
            if (PlayerPrefs.GetInt(GameFlow.PlayedKeyPrefix + level, 0) == 1) return "<color=#FF8A8A>Failed - Try Again</color>";
            return "not played yet";
        }

        // Colour = what you press: the same action colours on every level (Docs/PALETTES.md).
        private static readonly (string keys, string action, string colour, string obstacle)[] Moves =
        {
            ("Tap Left Shift / Tap Right Shift", "Change lane", "#F27017", "orange blocks and wide bars"),
            ("Hold Left Shift", "Slide", "#FCC41E", "yellow low bars"),
            ("Hold Right Shift", "Sprint", "#00FCF9", "cyan gates"),
            ("Tap both together", "Jump", "#F40E9D", "pink gaps and hurdles"),
            ("Hold both", "Phase", "#0E79F1", "blue walls (but not the light pink blocks inside them)"),
            ("Left, Right, Left, Right quickly", "Charge, then smash", "#F21E3D", "red brick walls"),
        };

        private float _k = 1f; // text scale for the screen height
        private GUIStyle _tagline;

        /// <summary>
        /// The title and tagline as extruded 3D lettering (ELIA, day 27): the text is drawn many times, stepping down and
        /// to the right in darkening shades of the sprint cyan, then a soft glow, then the face on top.
        /// </summary>
        /// <summary>
        /// Title block (ELIA, day 27): six tiles in the action colours above the title, three over each SHIFT and none over
        /// the +, the boxy runner leaping from the 3rd tile to the 4th over the +; the first 3D title (white, cyan sides);
        /// the tagline clean, with a small dark shadow. Returns the bottom of the tagline.
        /// </summary>
        private float DrawLogo(float w, float h)
        {
            if (_tagline == null)
                _tagline = new GUIStyle(_small) { fontStyle = FontStyle.Bold, wordWrap = false };
            _tagline.fontSize = Mathf.RoundToInt(22 * _k);
            _title.normal.textColor = _tagline.normal.textColor = Color.white; // tinted per layer with GUI.color

            // The runner reaches about 205 px (x scale) above the title's centre: keep it on screen.
            float titleCY = Mathf.Max(h * 0.08f + 150 * _k, 214 * _k);
            var titleRect = new Rect(0, titleCY - 55 * _k, w, 110 * _k);
            DrawTilesAndRunner(w, titleCY);
            Extruded(titleRect, "SHIFT+SHIFT", _title, Mathf.RoundToInt(12 * _k), 1.1f * _k,
                new Color(0.97f, 0.97f, 0.95f), new Color(0f, 0.75f, 0.78f), new Color(0.02f, 0.12f, 0.16f),
                new Color(0f, 0.98f, 0.97f));
            var tagRect = new Rect(0, titleCY + 53 * _k, w, 34 * _k);
            var shadow = new Color(0.04f, 0.055f, 0.11f);
            Extruded(tagRect, "A Rhythm Runner Played With Just Two Keys", _tagline, 2, 1f * _k,
                new Color(0.93f, 0.94f, 0.96f), shadow, shadow, Color.clear);
            return tagRect.yMax;
        }

        private static readonly Color[] TileColours =
        {
            new Color(0.95f, 0.44f, 0.09f), new Color(0.99f, 0.77f, 0.12f), new Color(0f, 0.99f, 0.98f),     // lane, slide, sprint
            new Color(1f, 0.31f, 0.91f), new Color(0.24f, 0.55f, 1f), new Color(0.95f, 0.12f, 0.24f),        // jump, phase, charge
        };
        private static readonly Color RunnerWhite = new Color(0.97f, 0.97f, 0.95f);
        private static readonly Color RunnerGrey = new Color(0.59f, 0.63f, 0.67f);

        private void DrawTilesAndRunner(float w, float titleCY)
        {
            float k = _k;
            Texture2D tex = Texture2D.whiteTexture;
            Color old = GUI.color;

            // Tiles over the two SHIFTs, measured from the title as drawn.
            float full = _title.CalcSize(new GUIContent("SHIFT+SHIFT")).x;
            float wS = _title.CalcSize(new GUIContent("SHIFT")).x, wP = full - 2 * wS;
            float left = (w - full) * 0.5f, ty = titleCY - 84 * k, th = 14 * k;
            var tiles = new Rect[6];
            for (int g = 0; g < 2; g++)
            {
                float a = left + g * (wS + wP), tw = wS / (3 + 2 * 0.16f);
                for (int i = 0; i < 3; i++) tiles[g * 3 + i] = new Rect(a + i * tw * 1.16f, ty, tw, th);
            }
            for (int i = 0; i < 6; i++)
            {
                Rect r = tiles[i];
                Color c = TileColours[i];
                for (int gl = 3; gl >= 1; gl--) // soft glow
                {
                    float e = gl * 4 * k;
                    GUI.color = new Color(c.r, c.g, c.b, 0.07f);
                    GUI.DrawTexture(new Rect(r.x - e, r.y - e, r.width + 2 * e, r.height + 2 * e), tex);
                }
                GUI.color = Color.Lerp(c, Color.black, 0.6f); // thickness
                GUI.DrawTexture(new Rect(r.x + 3 * k, r.y + 5 * k, r.width, r.height), tex);
                GUI.color = c;
                GUI.DrawTexture(r, tex);
                GUI.color = Color.Lerp(c, Color.white, 0.35f); // satin top
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * 0.4f), tex);
            }

            // Dotted jump arc from the 3rd tile to the 4th; the runner at its top.
            float x3 = tiles[2].center.x, x4 = tiles[3].center.x, top = ty - 4 * k, apex = 42 * k, dotSize = 4.4f * k;
            GUI.color = new Color(0.75f, 0.88f, 0.91f, 0.55f);
            for (int n = 2; n < 30; n += 2)
            {
                float t = n / 30f;
                if (Mathf.Abs(t - 0.5f) <= 0.12f) continue;
                float x = Mathf.Lerp(x3, x4, t), y = top - 4 * apex * t * (1 - t);
                GUI.DrawTexture(new Rect(x - dotSize * 0.5f, y - dotSize * 0.5f, dotSize, dotSize), tex);
            }

            float u = 7.6f * k;
            var hip = new Vector2(w * 0.5f - 0.6f * u, top - apex + 6 * k - 2.8f * u);
            GUI.color = new Color(0f, 0.98f, 0.97f, 0.5f); // speed streaks
            float[,] streaks = { { -5.5f, 5f }, { -3.6f, 8f }, { -1.7f, 4f } };
            for (int i = 0; i < 3; i++)
            {
                float x1 = hip.x - 3.0f * u, x0 = hip.x - 3.6f * u - streaks[i, 1] * u;
                GUI.DrawTexture(new Rect(x0, hip.y + streaks[i, 0] * u - 0.28f * u, x1 - x0, 0.56f * u), tex);
            }

            const float torso = 160f; // degrees clockwise from straight down: up, leaning forward
            Vector2 neck = hip + Dir(torso) * 4.4f * u;
            Vector2 shoulder = hip + (neck - hip) * 0.85f;
            // (joint, upper angle, lower angle, upper length, lower length, width, far side). The far side is grey and drawn
            // behind the body (ELIA: a grey arm must not show across the torso); the near side is white, in front.
            var limbs = new (Vector2 p, float a1, float a2, float l1, float l2, float wd, bool far)[]
            {
                (hip, -55f, -110f, 2.4f * u, 2.2f * u, 1.2f * u, false),        // back leg trailing
                (shoulder, 60f, 120f, 2.5f * u, 2.3f * u, 1.0f * u, false),    // arm reaching forward
                (hip, 75f, 20f, 2.4f * u, 2.2f * u, 1.25f * u, true),          // front leg reaching ahead
                (shoulder, -50f, -100f, 2.5f * u, 2.3f * u, 1.0f * u, true),   // arm swinging back
            };
            foreach (var l in limbs) if (l.far) Limb(l.p, l.a1, l.a2, l.l1, l.l2, l.wd, RunnerGrey);
            Segment(hip, torso, 4.4f * u, 2.6f * u, RunnerWhite);
            Vector2 head = neck + Dir(torso) * 1.3f * u;
            Segment(head - Dir(torso) * 1.1f * u, torso, 2.2f * u, 2.2f * u, RunnerWhite);
            foreach (var l in limbs) if (!l.far) Limb(l.p, l.a1, l.a2, l.l1, l.l2, l.wd, RunnerWhite);
            GUI.color = old;
        }

        /// <summary>Direction for an angle measured clockwise from straight down (screen y runs down): 90 = right.</summary>
        private static Vector2 Dir(float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a));
        }

        private static void Limb(Vector2 p, float a1, float a2, float l1, float l2, float wd, Color c)
        {
            Vector2 knee = Segment(p, a1, l1, wd, c);
            Segment(knee, a2, l2, wd * 0.92f, c);
        }

        /// <summary>A box from joint p along the angle, as a rotated rectangle. Returns its far end.</summary>
        private static Vector2 Segment(Vector2 p, float degrees, float length, float width, Color c)
        {
            Vector2 q = p + Dir(degrees) * length, mid = (p + q) * 0.5f;
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(-degrees, mid); // a box pointing down, turned to the angle
            GUI.color = c;
            GUI.DrawTexture(new Rect(mid.x - width * 0.5f, mid.y - length * 0.5f, width, length), Texture2D.whiteTexture);
            GUI.matrix = m;
            return q;
        }

        private static void Extruded(Rect r, string text, GUIStyle style, int depth, float step, Color face, Color sideNear, Color sideFar,
            Color glow)
        {
            Color old = GUI.color;
            for (int i = depth; i >= 1; i--) // back to front
            {
                GUI.color = Color.Lerp(sideNear, sideFar, (i - 1f) / Mathf.Max(1, depth - 1));
                GUI.Label(new Rect(r.x + i * step * 0.6f, r.y + i * step, r.width, r.height), text, style);
            }
            // A faint glow lifts the face off the sides (none when glow is clear: small text turned hazy, ELIA).
            if (glow.a > 0f)
            {
                GUI.color = new Color(glow.r, glow.g, glow.b, 0.18f);
                for (int dx = -2; dx <= 2; dx += 2)
                    for (int dy = -2; dy <= 2; dy += 2)
                        if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx, r.y + dy, r.width, r.height), text, style);
            }
            GUI.color = face;
            GUI.Label(r, text, style);
            GUI.color = old;
        }

        /// <summary>Height a wrapped label needs at this width.</summary>
        private static float Fit(GUIStyle style, string text, float width) => style.CalcHeight(new GUIContent(text), width) + 4f;

        private void DrawHowTo(float w, float h)
        {
            // Everything in one box, sized to its content and centred (ELIA, day 29: the title and "Any Shift: back"
            // were outside the box, and at full screen the moves left a large empty gap). Top down: title, intro,
            // the six moves, the tips, the back hint; the move rows shrink on short screens so it always fits.
            const string intro = "You run by yourself. Obstacles arrive on the beat: answer each one with the move its colour asks for.";
            // Scoring spelled out (ELIA, day 28).
            const string tipText = "PERFECT 50, GOOD 20, OK 5 points. Hit on the beat in a row to build a streak: x2 at 10, x3 at 25, " +
                                   "x4 at 50. OK keeps your streak; a crash ends the run.   Esc: pause\n" +
                                   "Timing feels off (for example with Bluetooth headphones)? Use Calibrate timing in the menu.";
            float pad = 18 * _k, gap = 8 * _k, margin = 10 * _k;
            float titleH = 64 * _k, introH = Fit(_small, intro, w * 0.8f), tipsH = Fit(_small, tipText, w * 0.8f), backH = 28 * _k;
            float fixedH = pad * 2 + titleH + introH + tipsH + backH + gap * 4;
            float row = Mathf.Min(56f * _k, (h - 2 * margin - fixedH) / Moves.Length);
            float total = fixedH + row * Moves.Length;
            float y = Mathf.Max(margin, (h - total) * 0.5f);
            UIPanel.Draw(new Rect(w * 0.05f, y, w * 0.9f, total));

            y += pad;
            GUI.Label(new Rect(0, y, w, titleH), "How to play", _big);
            y += titleH + gap;
            GUI.Label(new Rect(w * 0.1f, y, w * 0.8f, introH), intro, _small);
            y += introH + gap;

            var left = new GUIStyle(_mid) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            var right = new GUIStyle(_mid) { alignment = TextAnchor.MiddleLeft, wordWrap = true, fontSize = Mathf.RoundToInt(20 * _k) };
            float x0 = w * 0.08f, x1 = w * 0.45f;
            for (int i = 0; i < Moves.Length; i++)
            {
                var m = Moves[i];
                GUI.Label(new Rect(x0, y, x1 - x0, row), $"<b>{m.keys}</b>", left);
                GUI.Label(new Rect(x1, y, w * 0.92f - x1, row),
                    $"<color={m.colour}><b>{m.action}</b></color>  through {m.obstacle}", right);
                y += row;
            }
            y += gap;
            GUI.Label(new Rect(w * 0.1f, y, w * 0.8f, tipsH), tipText, _small);
            y += tipsH + gap;
            GUI.Label(new Rect(0, y, w, backH), "Any Shift: back", _small);
        }


        private static Texture2D _arrowLeft, _arrowRight;

        /// <summary>A white triangle pointing right or left, made once (tinted with GUI.color when drawn).</summary>
        private static Texture2D Arrow(bool right)
        {
            Texture2D cached = right ? _arrowRight : _arrowLeft;
            if (cached != null) return cached;
            const int n = 64;
            var tex = new Texture2D(n, n) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float along = right ? u : 1f - u;                  // 0 at the flat side, 1 at the point
                float halfHeight = 0.5f * (1f - along);            // the triangle narrows towards the point
                float edge = halfHeight - Mathf.Abs(v - 0.5f);     // > 0 inside
                float a = Mathf.Clamp01(edge * n * 0.8f);          // soft edge
                if (along < 0.08f) a = 0f;                         // a little margin on the flat side
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            if (right) _arrowRight = tex; else _arrowLeft = tex;
            return tex;
        }

        private void OnGUI()
        {
            if (_entries.Count == 0) return;
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 84, alignment = TextAnchor.MiddleCenter, richText = true, fontStyle = FontStyle.Bold };
                _big = new GUIStyle(GUI.skin.label) { fontSize = 46, alignment = TextAnchor.MiddleCenter, richText = true };
                _mid = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true };
            }
            float w = Screen.width, h = Screen.height;
            // Day 25 (ELIA): in itch.io's small frame the text was cut off. Shrink the text on short screens (sizes
            // as before from 700 px high up), and let wrapped text measure its own height below.
            _k = Mathf.Clamp(h / 700f, 0.7f, 1f);
            _title.fontSize = Mathf.RoundToInt(84 * _k);
            _big.fontSize = Mathf.RoundToInt(46 * _k);
            _mid.fontSize = Mathf.RoundToInt(24 * _k);
            _small.fontSize = Mathf.RoundToInt(17 * _k);

            if (_page == Page.HowTo)
            {
                DrawHowTo(w, h);
                return;
            }
            if (_page == Page.Credits)
            {
                GUI.Label(new Rect(0, h * 0.08f, w, 70), "Credits", _big);
                float creditsH = Fit(_mid, credits, w * 0.7f);
                UIPanel.Draw(new Rect(w * 0.12f, h * 0.52f - creditsH * 0.5f - 24 * _k, w * 0.76f, creditsH + 48 * _k));
                GUI.Label(new Rect(w * 0.15f, h * 0.22f, w * 0.7f, h * 0.6f), credits, _mid);
                GUI.Label(new Rect(0, h * 0.88f, w, 30), "Any Shift: back", _small);
                return;
            }

            float logoBottom = DrawLogo(w, h);
            if (!WebAudioUnlock.Ready)
            {
                // Web build only: the browser needs one click before it plays sound (it does not count Shift).

                float startY = Mathf.Max(h * 0.42f, logoBottom + 30 * _k);
                const string why = "(or press Space). Your browser needs this once before it plays sound. After that, just the two Shift keys.";
                UIPanel.Draw(new Rect(w * 0.08f, startY - 10 * _k, w * 0.84f, 75 * _k + Fit(_mid, why, w * 0.8f) + 22 * _k));
                GUI.Label(new Rect(0, startY, w, 70), "Click to start", _big);
                GUI.Label(new Rect(w * 0.1f, startY + 75 * _k, w * 0.8f, Fit(_mid, why, w * 0.8f)), why, _mid);
                return;
            }


            Entry e = _entries[_index];
            float entryY = Mathf.Max(h * 0.40f, logoBottom + 17 * _k); // below the title block on short screens
            // The selector's panel (testers, day 29): from the entry's title to just under the dots.
            float panelBottom = entryY + 75 * _k + Fit(_mid, e.Detail, w * 0.8f) + 10f * _k + 9f * _k + 14f * _k;
            UIPanel.Draw(new Rect(w * 0.08f, entryY - 8 * _k, w * 0.84f, panelBottom - (entryY - 8 * _k)));
            GUI.Label(new Rect(0, entryY, w, 70 * _k), e.Title, _big);
            // Arrows are drawn as images: the Web build's font has no arrow characters (ELIA, day 20: missing in the browser).
            float half = _big.CalcSize(new GUIContent(e.Title)).x * 0.5f, size = 44f * _k, gap = 50f * _k, cy = entryY + 35f * _k;
            Color old = GUI.color;
            GUI.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            GUI.DrawTexture(new Rect(w * 0.5f - half - gap - size, cy - size * 0.5f, size, size), Arrow(false));
            GUI.DrawTexture(new Rect(w * 0.5f + half + gap, cy - size * 0.5f, size, size), Arrow(true));
            GUI.color = old;
            float detailY = entryY + 75 * _k;
            float detailH = Fit(_mid, e.Detail, w * 0.8f);
            GUI.Label(new Rect(w * 0.1f, detailY, w * 0.8f, detailH), e.Detail, _mid);

            // Which entry this is: small squares (the Web build's font has no dot characters either).
            float dot = 9f * _k, step = 18f * _k, dotsY = detailY + detailH + 10f * _k;
            float dotsX = w * 0.5f - (_entries.Count * step - (step - dot)) * 0.5f;
            for (int i = 0; i < _entries.Count; i++)
            {
                GUI.color = i == _index ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                GUI.DrawTexture(new Rect(dotsX + i * step, dotsY, dot, dot), Texture2D.whiteTexture);
            }
            GUI.color = old;

            // Bottom up, so nothing runs off the screen.
            const string sticky = "Windows: pressing Shift five times in a row opens a Sticky Keys box. Choose No, or turn the shortcut off " +
                                  "in Settings > Accessibility > Keyboard. Keep this page in focus while you play.";
            float stickyH = Fit(_small, sticky, w * 0.84f);
            float stickyY = h - stickyH - 8f * _k;
            GUI.Label(new Rect(w * 0.08f, stickyY, w * 0.84f, stickyH), sticky, _small);
            const string keys = "<b>Left Shift</b> / <b>Right Shift</b>: choose      <b>Both together</b>: open";
            float keysH = Fit(_mid, keys, w);
            GUI.Label(new Rect(0, Mathf.Max(dotsY + dot + 8f * _k, stickyY - keysH - 6f * _k), w, keysH), keys, _mid);
        }
    }
}
