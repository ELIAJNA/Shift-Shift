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

        private bool _trailerStarted;

        private bool Down(Side side) => input != null && input.IsDown(side);

        private void Update()
        {
            MenuKey key = _keys.Poll(Down(Side.Left), Down(Side.Right));
            // Browser: nothing happens until sound is unlocked by a click (Shift does not count as a gesture there).
            if (!WebAudioUnlock.Ready) return;
            if (TrailerMode.Enabled && !_trailerStarted)
            {
                _trailerStarted = true; // editor only: straight into level 1
                GameFlow.Play(TrailerMode.FirstLevel, runScene);
                return;
            }
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
        private void DrawLogo(float w, float h)
        {
            if (_tagline == null)
                _tagline = new GUIStyle(_small) { fontStyle = FontStyle.Bold, wordWrap = false };
            _tagline.fontSize = Mathf.RoundToInt(22 * _k);
            _title.normal.textColor = _tagline.normal.textColor = Color.white; // tinted per layer with GUI.color
            var titleRect = new Rect(0, h * 0.08f, w, 110 * _k);
            // Each letter in its own action colour (rich text); the layers are tinted grey, so every letter's sides are
            // darker shades of its own colour.
            Extruded(titleRect, TitleColoured, _title, Mathf.RoundToInt(12 * _k), 1.1f * _k,
                Color.white, new Color(0.5f, 0.5f, 0.5f), new Color(0.12f, 0.12f, 0.12f), Color.white);
            var tagRect = new Rect(0, h * 0.08f + 108 * _k, w, 34 * _k);
            Extruded(tagRect, "A Rhythm Runner Played With Just Two Keys", _tagline, Mathf.RoundToInt(4 * _k), 1f * _k,
                new Color(0.79f, 0.82f, 0.85f), new Color(0.75f, 0.06f, 0.48f), new Color(0.12f, 0.02f, 0.08f),
                new Color(0f, 0.98f, 0.97f));
        }

        /// <summary>
        /// SHIFT+SHIFT with mirrored letter pairs in the six action colours, outside in (ELIA, day 27): S..T lane
        /// orange, H..F slide yellow, I..I sprint cyan, F..H jump pink, T..S phase blue, and the + charge red.
        /// </summary>
        private static readonly string TitleColoured = BuildTitle();

        private static string BuildTitle()
        {
            const string text = "SHIFT+SHIFT";
            string[] colours = { "#F27017", "#FCC41E", "#00FCF9", "#F40E9D", "#3D8BFF", "#F21E3D" };
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                int fromOutside = Mathf.Min(i, text.Length - 1 - i); // 0 for the outer pair .. 5 for the +
                sb.Append("<color=").Append(colours[fromOutside]).Append('>').Append(text[i]).Append("</color>");
            }
            return sb.ToString();
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
            // A thin dark outline and a cyan glow lift the face off the sides.
            GUI.color = new Color(glow.r, glow.g, glow.b, 0.18f);
            for (int dx = -2; dx <= 2; dx += 2)
                for (int dy = -2; dy <= 2; dy += 2)
                    if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx, r.y + dy, r.width, r.height), text, style);
            GUI.color = face;
            GUI.Label(r, text, style);
            GUI.color = old;
        }

        /// <summary>Height a wrapped label needs at this width.</summary>
        private static float Fit(GUIStyle style, string text, float width) => style.CalcHeight(new GUIContent(text), width) + 4f;

        private void DrawHowTo(float w, float h)
        {
            GUI.Label(new Rect(0, h * 0.04f, w, 70 * _k), "How to play", _big);
            const string intro = "You run by yourself. Obstacles arrive on the beat: answer each one with the move its colour asks for.";
            float introY = h * 0.04f + 62 * _k, introH = Fit(_small, intro, w * 0.8f);
            GUI.Label(new Rect(w * 0.1f, introY, w * 0.8f, introH), intro, _small);

            // Bottom up (day 25: text was cut off in itch.io's small frame): back hint, tips, then the rows share the rest.
            float backH = 30 * _k, backY = h - backH - 6 * _k;
            GUI.Label(new Rect(0, backY, w, backH), "Any Shift: back", _small);
            const string tipText = "Act on the click for more points: PERFECT and GOOD build your streak and multiplier.   Esc: pause\n" +
                                   "Timing feels off (for example with Bluetooth headphones)? Use Calibrate timing in the menu.";
            float tipsH = Fit(_small, tipText, w * 0.8f), tipsY = backY - tipsH - 4 * _k;
            GUI.Label(new Rect(w * 0.1f, tipsY, w * 0.8f, tipsH), tipText, _small);

            var left = new GUIStyle(_mid) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            var right = new GUIStyle(_mid) { alignment = TextAnchor.MiddleLeft, wordWrap = true, fontSize = Mathf.RoundToInt(20 * _k) };
            float top = introY + introH + 8 * _k, x0 = w * 0.08f, x1 = w * 0.45f;
            float row = Mathf.Min(64f * _k, (tipsY - 6 * _k - top) / Moves.Length);
            for (int i = 0; i < Moves.Length; i++)
            {
                var m = Moves[i];
                float y = top + i * row;
                GUI.Label(new Rect(x0, y, x1 - x0, row), $"<b>{m.keys}</b>", left);
                GUI.Label(new Rect(x1, y, w * 0.92f - x1, row),
                    $"<color={m.colour}><b>{m.action}</b></color>  through {m.obstacle}", right);
            }
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
                GUI.Label(new Rect(w * 0.15f, h * 0.22f, w * 0.7f, h * 0.6f), credits, _mid);
                GUI.Label(new Rect(0, h * 0.88f, w, 30), "Any Shift: back", _small);
                return;
            }

            DrawLogo(w, h);
            if (!WebAudioUnlock.Ready)
            {
                // Web build only: the browser needs one click before it plays sound (it does not count Shift).

                GUI.Label(new Rect(0, h * 0.42f, w, 70), "Click to start", _big);
                const string why = "(or press Space). Your browser needs this once before it plays sound. After that, just the two Shift keys.";
                GUI.Label(new Rect(w * 0.1f, h * 0.42f + 75 * _k, w * 0.8f, Fit(_mid, why, w * 0.8f)), why, _mid);
                return;
            }


            Entry e = _entries[_index];
            GUI.Label(new Rect(0, h * 0.40f, w, 70 * _k), e.Title, _big);
            // Arrows are drawn as images: the Web build's font has no arrow characters (ELIA, day 20: missing in the browser).
            float half = _big.CalcSize(new GUIContent(e.Title)).x * 0.5f, size = 44f * _k, gap = 50f * _k, cy = h * 0.40f + 35f * _k;
            Color old = GUI.color;
            GUI.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            GUI.DrawTexture(new Rect(w * 0.5f - half - gap - size, cy - size * 0.5f, size, size), Arrow(false));
            GUI.DrawTexture(new Rect(w * 0.5f + half + gap, cy - size * 0.5f, size, size), Arrow(true));
            GUI.color = old;
            float detailY = h * 0.40f + 75 * _k;
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
