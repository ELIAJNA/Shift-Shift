using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Trailer mode: presses the virtual Shift keys from <see cref="AutoPlan"/>, through ShiftInput, so every game rule
    /// and effect runs as for a player. Each press and release is given at its exact planned song time (decision A:
    /// answers count from key-down), so slow frames while recording cannot lose a tap or make a press late.
    /// Added at run time by <see cref="TrailerMode"/>; never in a scene or a build.
    /// </summary>
    public sealed class AutoPlayer : MonoBehaviour
    {
        private ShiftInput _input;
        private Conductor _conductor;
        private List<AutoPlan.KeyEdge> _edges;
        private int _next;

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
            _edges = AutoPlan.Edges(AutoPlan.Build(_conductor.Level));
            _input.KeySource = Feed;
            Debug.Log($"[Trailer] Autoplay: {_edges.Count / 2} key presses for {_conductor.Level.name}.");
        }

        private void Feed(double now, ShiftInput.KeyEdge edge)
        {
            if (!_conductor.IsStarted || _conductor.IsPaused) return;
            while (_next < _edges.Count && _edges[_next].Time <= now)
            {
                AutoPlan.KeyEdge e = _edges[_next++];
                edge(e.Side, e.Down, e.Time);
            }
        }

        private void OnDestroy()
        {
            if (_input != null) _input.KeySource = null;
        }
    }
}
