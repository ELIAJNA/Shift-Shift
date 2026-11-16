using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>Shift+Shift > Trailer Mode: ticks on and off the autoplay used to record footage (Docs/TRAILER.md).</summary>
    public static class TrailerModeMenu
    {
        private const string Item = "Shift+Shift/Trailer Mode (autoplay)";

        [MenuItem(Item)]
        private static void Toggle()
        {
            bool on = !TrailerMode.Enabled;
            PlayerPrefs.SetInt(TrailerMode.PrefKey, on ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log(on
                ? "[Trailer] Trailer mode ON: Play starts level 1 and the runner plays every level by itself. Untick to play normally."
                : "[Trailer] Trailer mode OFF.");
        }

        [MenuItem(Item, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(Item, TrailerMode.Enabled);
            return true;
        }
    }
}
