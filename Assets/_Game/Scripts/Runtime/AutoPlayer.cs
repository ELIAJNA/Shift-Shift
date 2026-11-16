using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Trailer mode: presses the virtual Shift keys from <see cref="AutoPlan"/>, through ShiftInput, so every game rule
    /// and effect runs as for a player. Added at run time by <see cref="TrailerMode"/>; never in a scene or a build.
    /// </summary>
    public sealed class AutoPlayer : MonoBehaviour
    {
        private ShiftInput _input;
        private Conductor _conductor;
        private List<KeyHold> _plan;

        public void Init(ShiftInput input, Conductor conductor)
        {
            _input = input;
            _conductor = conductor;
        }

        private void Start()
        {
            if (_input == null || _conductor == null || _conductor.Level == null)
            {
                enabled = false;
                return;
            }
            _plan = AutoPlan.Build(_conductor.Level);
            _input.KeySource = IsDown;
            Debug.Log($"[Trailer] Autoplay: {_plan.Count} key presses for {_conductor.Level.name}.");
        }

        private bool IsDown(Side side, double songTime)
        {
            if (!_conductor.IsStarted || _conductor.IsPaused) return false;
            // Keys are read once per frame: look one frame ahead, so a press lands in the frame just before its time
            // (never after the obstacle is judged).
            double ahead = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.04f);
            return AutoPlan.IsDown(_plan, side, songTime + ahead);
        }

        private void OnDestroy()
        {
            if (_input != null && _input.KeySource == (System.Func<Side, double, bool>)IsDown) _input.KeySource = null;
        }
    }
}
