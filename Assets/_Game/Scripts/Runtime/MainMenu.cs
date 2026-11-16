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
            if (key == MenuKey.None) return;
            if (_page != Page.None)
            {
                _page = Page.None; // any key closes the page
                return;
            }
            if (key == MenuKey.Left) _index = (_index + _entries.Count - 1) % _entries.Count;
            else if (key == MenuKey.Right) _index = (_index + 1) % _entries.Count;
            else Open(_entries[_index]);
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

        private void DrawHowTo(float w, float h)
        {
            GUI.Label(new Rect(0, h * 0.05f, w, 70), "How to play", _big);
            GUI.Label(new Rect(w * 0.1f, h * 0.05f + 62, w * 0.8f, 34),
                "You run by yourself. Obstacles arrive on the beat: answer each one with the move its colour asks for.", _small);
            var left = new GUIStyle(_mid) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            var right = new GUIStyle(_mid) { alignment = TextAnchor.MiddleLeft, wordWrap = true, fontSize = 20 };
            float top = h * 0.05f + 110, row = Mathf.Min(64f, h * 0.085f), x0 = w * 0.08f, x1 = w * 0.45f;
            for (int i = 0; i < Moves.Length; i++)
            {
                var m = Moves[i];
                float y = top + i * row;
                GUI.Label(new Rect(x0, y, x1 - x0, row), $"<b>{m.keys}</b>", left);
                GUI.Label(new Rect(x1, y, w * 0.92f - x1, row),
                    $"<color={m.colour}><b>{m.action}</b></color>  through {m.obstacle}", right);
            }
            float tips = top + Moves.Length * row + 10;
            GUI.Label(new Rect(w * 0.1f, tips, w * 0.8f, 60),
                "Act on the click for more points: PERFECT and GOOD build your streak and multiplier.   Esc: pause", _small);
            GUI.Label(new Rect(0, h * 0.92f, w, 30), "Any Shift: back", _small);
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

            GUI.Label(new Rect(0, h * 0.08f, w, 110), "SHIFT+SHIFT", _title);
            GUI.Label(new Rect(0, h * 0.08f + 105, w, 30), "A Rhythm Runner Played With Just Two Keys", _small);

            Entry e = _entries[_index];
            GUI.Label(new Rect(0, h * 0.40f, w, 70), e.Title, _big);
            // Arrows are drawn as images: the Web build's font has no arrow characters (ELIA, day 20: missing in the browser).
            float half = _big.CalcSize(new GUIContent(e.Title)).x * 0.5f, size = 44f, gap = 50f, cy = h * 0.40f + 35f;
            Color old = GUI.color;
            GUI.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            GUI.DrawTexture(new Rect(w * 0.5f - half - gap - size, cy - size * 0.5f, size, size), Arrow(false));
            GUI.DrawTexture(new Rect(w * 0.5f + half + gap, cy - size * 0.5f, size, size), Arrow(true));
            GUI.color = old;
            GUI.Label(new Rect(w * 0.1f, h * 0.40f + 75, w * 0.8f, 40), e.Detail, _mid);

            string dots = "";
            for (int i = 0; i < _entries.Count; i++) dots += i == _index ? "● " : "○ ";
            GUI.Label(new Rect(0, h * 0.40f + 130, w, 30), dots.TrimEnd(), _mid);

            GUI.Label(new Rect(0, h * 0.74f, w, 34), "<b>Left Shift</b> / <b>Right Shift</b>: choose      <b>Both together</b>: open", _mid);
            GUI.Label(new Rect(w * 0.1f, h * 0.86f, w * 0.8f, 50),
                "Windows: pressing Shift five times in a row opens a Sticky Keys box. Choose No, or turn the shortcut off " +
                "in Settings > Accessibility > Keyboard. Keep this page in focus while you play.", _small);
        }
    }
}
