using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>
    /// Latency calibration screen (playbook 1.4), reached with F2 from the debug overlay. Plays clicks scheduled on
    /// the audio clock; the player taps either Shift on each one. The mean (tap - click) covers audio output delay,
    /// input delay and the player's own timing; it is saved and the Conductor delays the game by that much.
    /// Right Shift saves, Left Shift retries, both Shifts (or Esc, outside browser fullscreen) go back without saving.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class LatencyCalibration : MonoBehaviour
    {
        [SerializeField] private string clickClip = "click_100";
        [Tooltip("Must match the click clip.")]
        [SerializeField] private float bpm = 100f;
        [SerializeField, Min(8)] private int clicks = 20;
        [Tooltip("First clicks to let the player find the beat; not measured.")]
        [SerializeField, Min(0)] private int warmUp = 4;
        [SerializeField] private string returnScene = "Menu";

        private enum State { Ready, Running, Result }

        private AudioSource _source;
        private AudioClip _clip;
        private State _state;
        private double _start;
        private readonly List<double> _clickTimes = new List<double>();
        private readonly List<double> _taps = new List<double>();
        private Calibration.Result _result;
        private double Beat => 60.0 / bpm;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _clip = Resources.Load<AudioClip>(clickClip);
            if (_clip == null) Debug.LogError($"[Calibration] Click clip '{clickClip}' not found in a Resources folder.");

            // Same plain dark background as the menu (day 25, ELIA: the default sky made the text hard to read).
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.0627f, 0.0784f, 0.0941f); // menu #101418
            }
        }

        // Menu-style keys (testers, day 30): in browser fullscreen Esc only leaves fullscreen and never reaches the
        // game, so the screen also goes back with both Shifts together, like the menu. Esc still works in a window.
        private readonly ShiftMenuKeys _keys = new ShiftMenuKeys();
        private bool _keysReady;

        private void Update()
        {
            // Key states: the browser's own view in Web builds (ShiftKeys: Unity's can lose the second Shift), else the
            // Input System. "Hit" = pressed since the last frame, timed for the calibration taps.
            bool leftDown, rightDown, leftHit, rightHit, esc;
            Keyboard kb = Keyboard.current;
            if (ShiftKeys.FromBrowser)
            {
                ShiftKeys.Read(out leftDown, out rightDown, out bool lh, out bool rh, out bool la, out bool ra,
                    out bool lr, out bool rr);
                leftHit = lh || la || lr;
                rightHit = rh || ra || rr;
            }
            else
            {
                if (kb == null) return;
                leftDown = kb.leftShiftKey.isPressed;
                rightDown = kb.rightShiftKey.isPressed;
                leftHit = kb.leftShiftKey.wasPressedThisFrame;
                rightHit = kb.rightShiftKey.wasPressedThisFrame;
            }
            esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (!_keysReady)
            {
                _keys.Reset(leftDown, rightDown); // the keys that opened this screen from the menu do not count
                _keysReady = true;
            }
            MenuKey key = _keys.Poll(leftDown, rightDown);
            if (esc) { Back(); return; }

            switch (_state)
            {
                case State.Ready:
                    if (key == MenuKey.Both) Back();
                    else if ((key == MenuKey.Left || key == MenuKey.Right) && _clip != null) Begin();
                    break;
                case State.Running:
                    double now = AudioSettings.dspTime; // same clock the Conductor uses
                    if (leftHit || rightHit) _taps.Add(now);
                    if (now > _clickTimes[_clickTimes.Count - 1] + Beat)
                    {
                        Finish();
                        _keys.Reset(leftDown, rightDown); // the last tap must not pick a result option
                    }
                    break;
                case State.Result:
                    if (key == MenuKey.Both) Back();
                    else if (key == MenuKey.Right && _result.Count >= clicks / 2) Save();
                    else if (key == MenuKey.Left) Begin();
                    break;
            }
        }


        private void Begin()
        {
            _start = AudioSettings.dspTime + 0.5;
            _source.clip = _clip;
            _source.PlayScheduled(_start);
            _source.SetScheduledEndTime(_start + clicks * Beat);
            _clickTimes.Clear();
            for (int k = warmUp; k < clicks; k++) _clickTimes.Add(_start + k * Beat);
            _taps.Clear();
            _state = State.Running;
        }

        private void Finish()
        {
            _source.Stop();
            _result = Calibration.Compute(_clickTimes, _taps, Beat * 0.4);
            _state = State.Result;
            // Every tap against its nearest click, so lost taps (focus, Sticky Keys) and far-off taps can be told apart.
            var lines = new System.Text.StringBuilder();
            foreach (double tap in _taps)
            {
                double best = double.MaxValue;
                foreach (double c in _clickTimes)
                    if (System.Math.Abs(tap - c) < System.Math.Abs(best)) best = tap - c;
                lines.Append($"{best * 1000:+0;-0;0} ");
            }
            Debug.Log($"[Calibration] mean {_result.MeanMs:0.0} ms, spread {_result.SpreadMs:0.0} ms, " +
                      $"{_result.Count} of {_taps.Count} taps counted (cutoff ±{Beat * 0.4 * 1000:0} ms). Offsets: {lines}");
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(Conductor.UserOffsetKey, (float)_result.MeanMs);
            PlayerPrefs.Save();
            Back();
        }

        private void Back()
        {
            if (Application.CanStreamedLevelBeLoaded(returnScene)) SceneManager.LoadScene(returnScene);
            else Debug.LogWarning($"[Calibration] Scene '{returnScene}' is not in the build list.");
        }

        private GUIStyle _style;

        private void OnGUI()
        {
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, richText = true };
            float w = Screen.width, h = Screen.height;
            float saved = PlayerPrefs.GetFloat(Conductor.UserOffsetKey, 0f);

            string text;
            switch (_state)
            {
                case State.Ready:
                    text = "<b>Latency calibration</b>\n\nTap either Shift on every click.\n" +
                           $"The first {warmUp} clicks are practice.\n\nLeft or Right Shift: begin\n\n" +
                           $"<size=18>Current offset {saved:+0;-0;0} ms   ·   Both Shifts: back</size>";
                    break;
                case State.Running:
                    int heard = Mathf.Clamp((int)((AudioSettings.dspTime - _start) / Beat) + 1, 0, clicks);
                    text = $"<b>Tap on the click</b>\n\nclick {heard} / {clicks}\ntaps {_taps.Count}";
                    break;
                default:
                    bool enough = _result.Count >= clicks / 2;
                    text = $"<b>Result</b>\n\nOffset {_result.MeanMs:+0;-0;0} ms\nspread ±{_result.SpreadMs:0} ms   (counted {_result.Count} of {_taps.Count} taps)\n\n" +
                           (enough ? "Right Shift: save   ·   Left Shift: try again   ·   Both: back"
                                   : "Not enough taps counted.\nLeft Shift: try again   ·   Both: back") +
                           $"\n\n<size=18>A spread over ±40 ms means the taps were uneven: try again.</size>";
                    break;
            }
            // A panel behind the text (testers, day 29: the screen needed a highlight, like every screen now).
            float textH = _style.CalcHeight(new GUIContent(text), w * 0.8f);
            UIPanel.Draw(new Rect(w * 0.1f, h * 0.5f - textH * 0.5f - 28f, w * 0.8f, textH + 56f)); // wide enough for the longest line at 960 px
            GUI.Label(new Rect(0, 0, w, h), text, _style);
        }
    }
}
