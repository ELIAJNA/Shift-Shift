#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace ShiftShift
{
    /// <summary>
    /// Whether sound can play. In Web builds the browser blocks sound until a user gesture, and Chrome does not count
    /// Shift as one, so the menu asks for a click (or Space / Enter) first (Assets/Plugins/WebGL/ShiftKeys.jslib).
    /// Always true in the editor and in desktop builds.
    /// </summary>
    public static class WebAudioUnlock
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AudioUnlock_Init();
        [DllImport("__Internal")] private static extern int AudioUnlock_IsRunning();

        private static bool _init, _ready;

        public static bool Ready
        {
            get
            {
                if (_ready) return true;
                if (!_init)
                {
                    AudioUnlock_Init();
                    _init = true;
                }
                _ready = AudioUnlock_IsRunning() != 0;
                return _ready;
            }
        }
#else
        public static bool Ready => true;
#endif
    }
}
