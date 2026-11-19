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
        private float _toGapCentre;

        /// <summary>
        /// A missed gap (ELIA, day 29): after the crash freeze the runner moves over the middle of the hole and drops
        /// straight down into it with both arms raised. The run can fail at either edge of a gap (a jump made too early
        /// lands before the far edge), so it moves toward the gap's centre, forward or back (the first version always
        /// went forward and from the far edge ended up on the road beyond, sinking through it). Game time, so it waits
        /// through the hit-stop and the pause. Look only.
        /// </summary>
        private void FallIntoGap()
        {
            if (_fallT < 0f)
            {
                _fallT = 0f;
                _basePos = transform.localPosition;
                // The runner is moving: it carries on a little into the hole as it falls (ELIA). From the near edge (no
                // jump) to about 1 m inside; from inside the gap (an early jump came down) 0.6 m on, never past the far
                // side, never back.
                Obstacle gap = runner.FailedOn;
                _toGapCentre = 0f;
                if (gap != null && gap.hasTrackPosition)
                {
                    float near = gap.trackDistance - gap.depth * 0.5f + 1.0f, far = gap.trackDistance + gap.depth * 0.5f - 0.45f;
                    float target = Mathf.Clamp(runner.Distance + 0.6f, near, Mathf.Max(near, far));
                    _toGapCentre = Mathf.Max(0f, target - runner.Distance);
                }
                if (flame != null) { flame.Stop(); _flaming = false; }
            }
            _fallT += Time.deltaTime;
            float t = _fallT;
            float over = 1f - (1f - Mathf.Clamp01(t / 0.3f)) * (1f - Mathf.Clamp01(t / 0.3f)); // momentum, easing out...
            float drop = Mathf.Min(1.6f, 0.5f * 9.8f * t * t);          // ...falling all the while; head, shoulders and arms stay in view (ELIA)
            transform.localPosition = _basePos + new Vector3(0f, -drop, _toGapCentre * over);
            transform.localRotation = Quaternion.Euler(6f * over, 0f, 0f);     // upright, a very slight lean
            if (armLeft != null) armLeft.localRotation = Quaternion.Euler(-172f, 0f, -12f);  // both arms straight up
            if (armRight != null) armRight.localRotation = Quaternion.Euler(-172f, 0f, 12f);
            if (legLeft != null) legLeft.localRotation = Quaternion.identity;
            if (legRight != null) legRight.localRotation = Quaternion.identity;
            if (upperBody != null) upperBody.localRotation = Quaternion.identity;
        }

    }
}
