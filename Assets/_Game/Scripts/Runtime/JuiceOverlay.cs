using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Day 27 juice, drawn on screen (no particle shaders, so nothing can go missing in the Web build):
    /// - a spark burst at the runner, in the obstacle's colour, when an obstacle is answered PERFECT;
    /// - speed lines streaming out at the screen edges while sprinting.
    /// Visual only: reads Runner events and state, changes nothing. Added by ChaseCamera; settings in CameraFeel.
    /// Uses game time, so it freezes with the pause.
    /// </summary>
    public sealed class JuiceOverlay : MonoBehaviour
    {
        private Runner _runner;
        private ChaseCamera _chase;
        private Camera _cam;

        private struct Spark { public Vector2 Pos, Vel; public float Age, Life, Size; public Color Colour; }
        private readonly List<Spark> _sparks = new List<Spark>();

        private struct Line { public float Angle, Dist, Speed, Length; }
        private readonly Line[] _lines = new Line[28];
        private float _linesAlpha; // fades in and out with the sprint

        public void Init(Runner runner, ChaseCamera chase)
        {
            _runner = runner;
            _chase = chase;
            _cam = chase.GetComponent<Camera>();
            _runner.Judged += OnJudged;
            for (int i = 0; i < _lines.Length; i++) _lines[i] = NewLine(Random.value);
        }

        private void OnDestroy()
        {
            if (_runner != null) _runner.Judged -= OnJudged;
        }

        // The action colours, as on the How to play page.
        private static Color ColourOf(Obstacle o)
        {
            if (o == null) return Color.white;
            if (o.layer == ObstacleLayer.Ghost) return new Color(1f, 0.71f, 0.85f); // ghost block pink
            switch (o.kind)
            {
                case ObstacleKind.Low: return new Color(0.99f, 0.77f, 0.12f);
                case ObstacleKind.Gate: return new Color(0f, 0.99f, 0.98f);
                case ObstacleKind.Gap:
                case ObstacleKind.Hurdle: return new Color(0.96f, 0.05f, 0.62f);
                case ObstacleKind.Barrier: return new Color(0.24f, 0.55f, 1f);
                case ObstacleKind.Wall: return new Color(0.95f, 0.12f, 0.24f);
                default: return new Color(0.95f, 0.44f, 0.09f); // lane block orange
            }
        }

        private void OnJudged(Obstacle o, Grade g)
        {
            int n = _chase.Feel.perfectSparks;
            if (g != Grade.Perfect || n <= 0 || _runner.Track == null) return;
            Vector3 world = _runner.Track.Point(_runner.Distance, _runner.Sideways, _runner.Height + 0.9f);
            Vector3 sp = _cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            var centre = new Vector2(sp.x, Screen.height - sp.y); // GUI space: y down
            float k = Screen.height / 1080f;
            Color c = ColourOf(o);
            for (int i = 0; i < n; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(260f, 620f) * k;
                _sparks.Add(new Spark
                {
                    Pos = centre,
                    Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.75f - 0.35f) * speed,
                    Life = Random.Range(0.3f, 0.55f),
                    Size = Random.Range(6f, 13f) * k,
                    Colour = Color.Lerp(c, Color.white, Random.Range(0f, 0.45f)),
                });
            }
        }

        private static Line NewLine(float startDist) => new Line
        {
            Angle = Random.Range(0f, 360f),
            Dist = Mathf.Lerp(0.55f, 1.05f, startDist), // fraction of the half-diagonal
            Speed = Random.Range(1.6f, 2.8f),
            Length = Random.Range(0.08f, 0.2f),
        };

        private void Update()
        {
            float dt = Time.deltaTime;
            float k = Screen.height / 1080f;
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                Spark s = _sparks[i];
                s.Age += dt;
                if (s.Age >= s.Life) { _sparks.RemoveAt(i); continue; }
                s.Vel.y += 900f * k * dt; // fall
                s.Vel *= 1f - 2.5f * dt;   // drag
                s.Pos += s.Vel * dt;
                _sparks[i] = s;
            }

            bool sprint = _runner != null && _runner.IsSprinting && !_runner.IsPhased && _runner.State == Runner.RunState.Running;
            _linesAlpha = Mathf.MoveTowards(_linesAlpha, sprint ? 1f : 0f, dt / 0.12f);
            if (_linesAlpha <= 0f) return;
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i].Dist += _lines[i].Speed * dt;
                if (_lines[i].Dist > 1.15f) _lines[i] = NewLine(0f);
            }
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.depth = 5; // behind the HUD text
            Color old = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            Texture2D tex = Texture2D.whiteTexture;

            float strength = _chase != null ? _chase.Feel.speedLines : 0f;
            if (_linesAlpha > 0f && strength > 0f)
            {
                var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
                float half = new Vector2(Screen.width, Screen.height).magnitude * 0.5f;
                float thick = Mathf.Max(1.5f, 2.2f * Screen.height / 1080f);
                foreach (Line l in _lines)
                {
                    GUI.matrix = oldMatrix;
                    GUIUtility.RotateAroundPivot(l.Angle, centre);
                    float fade = Mathf.Clamp01((l.Dist - 0.55f) / 0.15f); // appear at the edge, not in the middle
                    GUI.color = new Color(0.85f, 1f, 1f, strength * _linesAlpha * fade);
                    GUI.DrawTexture(new Rect(centre.x + l.Dist * half, centre.y - thick * 0.5f, l.Length * half, thick), tex);
                }
                GUI.matrix = oldMatrix;
            }

            foreach (Spark s in _sparks)
            {
                float t = s.Age / s.Life;
                float size = s.Size * (1f - t * 0.7f);
                GUI.color = new Color(s.Colour.r, s.Colour.g, s.Colour.b, 1f - t * t);
                GUI.DrawTexture(new Rect(s.Pos.x - size * 0.5f, s.Pos.y - size * 0.5f, size, size), tex);
            }
            GUI.color = old;
        }
    }
}
