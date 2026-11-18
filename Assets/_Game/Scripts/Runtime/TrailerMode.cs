using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>
    /// Trailer mode (ELIA, day 23): the runner plays every level by itself, for recording footage with Unity Recorder.
    /// Editor only: switched with Shift+Shift > Trailer Mode, never on in a build.
    /// While on: pick the first level in the menu as usual (day 28: was always level 1; a Run scene opened directly still
    /// starts at level 1), the runner plays it and each level after it, going on after each results screen (a fail
    /// restarts it); debug overlays are hidden and nothing is saved (best scores, cleared levels).
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
