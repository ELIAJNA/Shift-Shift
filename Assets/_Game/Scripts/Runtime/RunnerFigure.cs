using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// The runner as a blocky running figure (ELIA, day 17: instead of the capsule), with dust puffs from the heels.
    /// Built from boxes by Shift+Shift > Set Up Visuals; this component only animates it.
    /// The stride follows the runner's distance along the track, which follows the song: one full stride (two
    /// footfalls) per beat, so the feet land in time with the music. No frame time involved.
    /// The Runner still squashes the whole figure for a slide; this adds the tuck for a jump, the lean for a sprint
    /// and the phase material on every part.
    /// </summary>
    public sealed class RunnerFigure : MonoBehaviour
    {
        [SerializeField] private Runner runner;
        [Tooltip("Pivots at the hips and shoulders; they swing around their local X axis.")]
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;
        [Tooltip("Pivot at the hips for the upper body: leans forward when sprinting.")]
        [SerializeField] private Transform upperBody;
        [SerializeField] private Renderer[] parts = new Renderer[0];
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material phaseMaterial;

        [Header("Stride")]
        [Tooltip("Metres per full stride (left + right step). 5 = one stride per beat at the Runner's 5 m per beat.")]
        [SerializeField, Min(0.5f)] private float strideMetres = 5f;
        [SerializeField, Range(0f, 80f)] private float legSwingDegrees = 42f;
        [SerializeField, Range(0f, 80f)] private float armSwingDegrees = 50f;
        [SerializeField, Range(0f, 45f)] private float runLeanDegrees = 8f;
        [SerializeField, Range(0f, 45f)] private float sprintLeanDegrees = 22f;

        [Tooltip("Lean into bends (ELIA, day 17): degrees of lean per degree the track turns over the next few metres.")]
        [SerializeField, Range(0f, 8f)] private float bendLeanFactor = 1f;
        [Tooltip("Kept slight (ELIA, day 17): a big lean reads as a lane change.")]
        [SerializeField, Range(0f, 30f)] private float bendLeanMaxDegrees = 5f;

        [Header("Charged (ELIA, day 20): glowing body and flames while a charge is ready for the next red wall")]
        [SerializeField] private Material chargedMaterial;
        [SerializeField] private ParticleSystem flame;

        [Header("Dust from the heels")]
        [SerializeField] private ParticleSystem dust;
        [SerializeField, Range(0, 12)] private int puffsPerStep = 4;
        [Tooltip("Where the feet touch down, relative to this object.")]
        [SerializeField] private Vector3 footOffset = new Vector3(0.14f, 0.05f, -0.25f);

        private int _lastStep = int.MinValue;
        private Material _applied;
        private bool _flaming;
        private float _lean;

        // Celebration dance at the end of a level (ELIA, day 27): one per level, timed to the level's tempo.
        private Conductor _conductor;
        private double _danceStart = -1; // audio clock (dspTime): the dance follows the beat
        private Vector3 _basePos;
        private int _danceLevel;
        private float _danceBpm = 120f;

        private void Start()
        {
            if (runner == null) runner = GetComponentInParent<Runner>();
            if (runner == null) enabled = false;
            _conductor = FindAnyObjectByType<Conductor>();
        }

        private void LateUpdate()
        {
            if (runner.State == Runner.RunState.Completed)
            {
                Dance();
                return;
            }
            if (runner.State == Runner.RunState.Failed && runner.FailedOn != null &&
                runner.FailedOn.kind == ShiftShift.Core.ObstacleKind.Gap)
            {
                FallIntoGap();
                return;
            }
            // Half-strides since the start: the whole part counts footfalls, the fraction drives the swing.
            float half = runner.Distance / strideMetres * 2f;
            float swing = Mathf.Sin(half * Mathf.PI);
            bool inAir = runner.Height > 0.05f;
            bool running = runner.State == Runner.RunState.Running;

            float leg = running ? (inAir ? 0f : swing * legSwingDegrees) : 0f;
            float tuck = inAir ? -55f : 0f; // both knees up in a jump
            if (legLeft != null) legLeft.localRotation = Quaternion.Euler(leg + tuck, 0f, 0f);
            if (legRight != null) legRight.localRotation = Quaternion.Euler(-leg + tuck, 0f, 0f);
            float arm = running ? swing * armSwingDegrees : 0f;
            if (armLeft != null) armLeft.localRotation = Quaternion.Euler(-arm, 0f, 0f);
            if (armRight != null) armRight.localRotation = Quaternion.Euler(arm, 0f, 0f);

            float wantedLean = !running ? 0f : runner.IsSprinting ? sprintLeanDegrees : runLeanDegrees;
            _lean = Mathf.MoveTowards(_lean, wantedLean, 120f * Time.deltaTime); // look only, not rhythm timing
            if (upperBody != null) upperBody.localRotation = Quaternion.Euler(_lean, 0f, 0f);

            // Lean into the bend: how much the track's heading changes from 3 m behind to 3 m ahead.
            if (runner.Track != null)
            {
                double s = runner.Distance;
                float turn = (float)((runner.Track.Path.YawAt(s + 3) - runner.Track.Path.YawAt(s - 3)) * 180.0 / System.Math.PI);
                float lean = running ? Mathf.Clamp(turn * bendLeanFactor, -bendLeanMaxDegrees, bendLeanMaxDegrees) : 0f;
                transform.localRotation = Quaternion.Euler(0f, 0f, -lean); // right bend: lean right
            }

            // A puff each time a foot lands (every half stride), on the ground only.
            int step = Mathf.FloorToInt(half);
            if (step != _lastStep)
            {
                bool first = _lastStep == int.MinValue;
                _lastStep = step;
                if (!first && running && !inAir && dust != null && puffsPerStep > 0)
                {
                    float side = (step & 1) == 0 ? -1f : 1f;
                    dust.transform.localPosition = new Vector3(footOffset.x * side, footOffset.y, footOffset.z);
                    dust.Emit(puffsPerStep);
                }
            }

            // Look: phased beats charged beats normal. Charged = a charge is ready and the next obstacle is a red wall.
            bool charged = running && runner.HasCharge && runner.NextObstacleKind == ShiftShift.Core.ObstacleKind.Wall;
            Material wanted = runner.IsPhased && phaseMaterial != null ? phaseMaterial
                : charged && chargedMaterial != null ? chargedMaterial : normalMaterial;
            if (wanted != null && wanted != _applied)
            {
                _applied = wanted;
                foreach (Renderer r in parts)
                    if (r != null) r.sharedMaterial = wanted;
            }
            if (flame != null && charged != _flaming)
            {
                _flaming = charged;
                if (charged) flame.Play();
                else flame.Stop(); // flames already in the air burn out
            }
        }

        /// <summary>
        /// The celebration: the runner turns to face the camera, then dances the level's dance on the beat until the scene
        /// ends (the results box appears over it after a few seconds; GameFlow). Look only: rules and timing are done.
        /// 1 victory pump, 2 disco point, 3 jumping jacks, 4 spin and pose, 5 running man, 6 finale (spin, big jump,
        /// arms up with the flame lit).
        /// </summary>
        private void Dance()
        {
            if (_danceStart < 0f)
            {
                _danceStart = AudioSettings.dspTime;
                _basePos = transform.localPosition;
                _danceLevel = _conductor != null ? GameFlow.LevelNumber(_conductor.LevelAssetName) : 1;
                if (_danceLevel < 1 || _danceLevel > 6) _danceLevel = 1;
                if (_conductor != null && _conductor.Level != null)
                    _danceBpm = Mathf.Clamp((float)_conductor.BpmAtBeat(_conductor.Level.lengthBeats), 60f, 200f);
                if (normalMaterial != null)
                    foreach (Renderer r in parts)
                        if (r != null) r.sharedMaterial = normalMaterial;
                _applied = normalMaterial;
                if (flame != null) { flame.Stop(); _flaming = false; }
            }
            float t = (float)(AudioSettings.dspTime - _danceStart);
            float p = t * _danceBpm / 60f;                 // beats since the end of the level
            float f = p - Mathf.Floor(p);
            float pulse = Mathf.Sin(f * Mathf.PI);          // 0 -> 1 -> 0 within each beat
            float sw = Mathf.Sin(p * Mathf.PI);             // swings one way on even beats, the other on odd
            bool even = ((int)Mathf.Floor(p) & 1) == 0;
            float turn = Smooth(Mathf.Clamp01(t / 0.35f)) * 180f; // face the camera

            // Pose (limbs hang from their pivots: X negative swings forward then up; Z swings sideways, + to the right).
            Quaternion aL = Quaternion.identity, aR = Quaternion.identity, lL = Quaternion.identity, lR = Quaternion.identity;
            float hop = 0f, spin = 0f, roll = 0f;
            bool fire = false;
            switch (_danceLevel)
            {
                case 1: // victory pump
                    aL = aR = Quaternion.Euler(-150f - 25f * pulse, 0f, 0f);
                    hop = 0.12f * pulse;
                    break;
                case 2: // disco point
                    aR = even ? Quaternion.Euler(-155f, 0f, 25f) : Quaternion.Euler(-35f, 0f, -40f);
                    aL = Quaternion.Euler(0f, 0f, -35f);
                    lL = Quaternion.Euler(-15f * pulse, 0f, 0f);
                    roll = 6f * sw;
                    break;
                case 3: // jumping jacks
                    aL = Quaternion.Euler(0f, 0f, -160f * pulse);
                    aR = Quaternion.Euler(0f, 0f, 160f * pulse);
                    lL = Quaternion.Euler(0f, 0f, -22f * pulse);
                    lR = Quaternion.Euler(0f, 0f, 22f * pulse);
                    hop = 0.15f * pulse;
                    break;
                case 4: // spin and pose, every 4 beats
                {
                    float c = p % 4f;
                    if (c < 2f)
                    {
                        spin = 360f * Smooth(c / 2f);
                        aL = Quaternion.Euler(0f, 0f, -90f);
                        aR = Quaternion.Euler(0f, 0f, 90f);
                    }
                    else
                    {
                        aR = Quaternion.Euler(-160f, 0f, 20f);
                        aL = Quaternion.Euler(0f, 0f, -30f);
                        hop = 0.06f * pulse;
                    }
                    break;
                }
                case 5: // running man
                    lL = Quaternion.Euler(50f * sw, 0f, 0f);
                    lR = Quaternion.Euler(-50f * sw, 0f, 0f);
                    aL = Quaternion.Euler(-60f * sw, 0f, 0f);
                    aR = Quaternion.Euler(60f * sw, 0f, 0f);
                    hop = 0.06f * pulse;
                    break;
                default: // 6: finale - spin, big jump, then arms up with the flame
                    if (p < 2f)
                    {
                        spin = 360f * Smooth(p / 2f);
                        aL = Quaternion.Euler(0f, 0f, -90f);
                        aR = Quaternion.Euler(0f, 0f, 90f);
                    }
                    else if (p < 3f)
                    {
                        hop = 0.7f * Mathf.Sin((p - 2f) * Mathf.PI);
                        lL = lR = Quaternion.Euler(-55f, 0f, 0f);
                        aL = Quaternion.Euler(-170f, 0f, -10f);
                        aR = Quaternion.Euler(-170f, 0f, 10f);
                    }
                    else
                    {
                        aL = Quaternion.Euler(-160f - 15f * pulse, 0f, -15f);
                        aR = Quaternion.Euler(-160f - 15f * pulse, 0f, 15f);
                        hop = 0.08f * pulse;
                        fire = true;
                    }
                    break;
            }
            if (armLeft != null) armLeft.localRotation = aL;
            if (armRight != null) armRight.localRotation = aR;
            if (legLeft != null) legLeft.localRotation = lL;
            if (legRight != null) legRight.localRotation = lR;
            if (upperBody != null) upperBody.localRotation = Quaternion.identity;
            transform.localRotation = Quaternion.Euler(0f, turn + spin, roll);
            transform.localPosition = _basePos + Vector3.up * hop;
            if (flame != null && fire != _flaming)
            {
                _flaming = fire;
                if (fire) flame.Play();
                else flame.Stop();
            }
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        private float _fallT = -1f;
        private float _fallForward, _fallDepth;
        private const float FallLean = 30f;        // degrees forward at the end of the fall
        private const float ShoulderHeight = 1.55f; // the figure's shoulders above its feet (m)

        /// <summary>
        /// A missed gap (ELIA, day 29). After the crash freeze the runner arcs on into the hole with its momentum:
        /// forward motion easing out while it falls faster and faster, leaning forward to 30 degrees, both arms raised.
        /// It sinks just far enough for the gap's near edge to hide it below the shoulders from where the camera is
        /// (ELIA: head and arms in view, no torso or legs). How much shows depends on how far past the edge it is (the
        /// camera looks down into the pit), so the depth is worked out from the camera's position, not fixed.
        /// Game time, so it waits through the hit-stop and the pause. Look only.
        /// </summary>
        private void FallIntoGap()
        {
            if (_fallT < 0f) StartFall();
            _fallT += Time.deltaTime;
            float t = _fallT;
            float u = Mathf.Clamp01(t / 0.5f);
            float forward = _fallForward * (1f - (1f - u) * (1f - u));         // momentum, easing out
            float drop = Mathf.Min(_fallDepth, 0.5f * 8f * t * t);               // falling faster and faster
            float lean = FallLean * (1f - (1f - u) * (1f - u));
            transform.localPosition = _basePos + new Vector3(0f, -drop, forward);
            transform.localRotation = Quaternion.Euler(lean, 0f, 0f);
            if (armLeft != null) armLeft.localRotation = Quaternion.Euler(-172f, 0f, -12f);  // both arms straight up
            if (armRight != null) armRight.localRotation = Quaternion.Euler(-172f, 0f, 12f);
            if (legLeft != null) legLeft.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            if (legRight != null) legRight.localRotation = Quaternion.Euler(8f, 0f, 0f);
            if (upperBody != null) upperBody.localRotation = Quaternion.identity;
        }

        private void StartFall()
        {
            _fallT = 0f;
            _basePos = transform.localPosition;
            if (flame != null) { flame.Stop(); _flaming = false; }
            _fallForward = 0f;
            _fallDepth = 1.6f;
            Obstacle gap = runner.FailedOn;
            if (gap == null || !gap.hasTrackPosition) return;

            // How far on: from the near edge (no jump) to about 1.2 m into the hole; from inside it (an early jump came
            // down) 0.8 m on; never closer than 0.5 m to the far side, never back.
            float nearEdge = gap.trackDistance - gap.depth * 0.5f, farEdge = gap.trackDistance + gap.depth * 0.5f;
            float target = Mathf.Clamp(runner.Distance + 0.8f, nearEdge + 1.2f, Mathf.Max(nearEdge + 1.2f, farEdge - 0.5f));
            _fallForward = Mathf.Max(0f, target - runner.Distance);

            // Depth: the line from the camera over the gap's near edge, at the figure's final place, is where the edge
            // starts hiding it; put the shoulders just below that line. Runner-local space: z along the track, y up,
            // the road at the figure's starting height.
            Camera cam = Camera.main;
            Transform parent = transform.parent;
            if (cam == null || parent == null) return;
            Vector3 c = parent.InverseTransformPoint(cam.transform.position);
            float roadY = _basePos.y;
            float rimZ = nearEdge - runner.Distance + _basePos.z;
            float figZ = _basePos.z + _fallForward;
            float camY = c.y - roadY;
            float hidden = 0f; // how far below the road the edge hides things at the figure's place
            if (figZ > rimZ && rimZ > c.z && camY > 0f)
                hidden = camY * (figZ - rimZ) / (rimZ - c.z);
            float shoulders = ShoulderHeight * Mathf.Cos(FallLean * Mathf.Deg2Rad);
            _fallDepth = Mathf.Clamp(shoulders + 0.05f + hidden, 1.0f, 3.2f); // shoulders just below the hiding line
        }


    }
}
