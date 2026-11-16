using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>
    /// Trailer mode (ELIA, day 23): the runner plays every level by itself, for recording footage with Unity Recorder.
    /// Editor only: switched with Shift+Shift > Trailer Mode, never on in a build.
    /// While on: the menu starts level 1 at once, debug overlays are hidden, each level goes on to the next after its
    /// results (a fail restarts it), and nothing is saved (best scores, cleared levels).
    /// </summary>
    public static class TrailerMode
    {
        public const string PrefKey = "ShiftShift.TrailerMode";
        public const string FirstLevel = "level01";
        public const string LastLevel = "level06";

        public static bool Enabled => Application.isEditor && PlayerPrefs.GetInt(PrefKey, 0) == 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (!Application.isEditor) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // After the scene's Awake (GameFlow has set the Conductor's level), before its Start.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Enabled) return;
            DebugOverlay.Visible = false;
            ShiftInput input = Object.FindAnyObjectByType<ShiftInput>();
            Conductor conductor = Object.FindAnyObjectByType<Conductor>();
            if (input == null || conductor == null || input.GetComponent<AutoPlayer>() != null) return;
            input.gameObject.AddComponent<AutoPlayer>().Init(input, conductor);
        }
    }
}
