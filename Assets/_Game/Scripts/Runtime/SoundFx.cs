using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Sound effects for the run (audio pass, day 23): every action makes a sound (plan: "no silent action").
    /// Lane change, slide, sprint, jump, phase, charge ready, wall smashed, hit, level complete, pause and resume,
    /// and a tick on each of the last four count-in beats. Kenney packs (CC0), Assets/_Game/Audio/SFX; the setup
    /// tool assigns the clips and volumes (Shift+Shift > Set Up Visuals).
    /// The count-in ticks are scheduled on the audio clock (PlayScheduled), so they are exactly on the beat like the
    /// music. Gesture sounds play when the gesture is recognised (a tap on key release, a hold after the tap/hold
    /// threshold): feedback, not timing.
    /// </summary>
    public sealed class SoundFx : MonoBehaviour
    {
        [System.Serializable]
        public struct Sound
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private ShiftInput input;
        [SerializeField] private Runner runner;
        [SerializeField] private Conductor conductor;
        [Tooltip("Optional: pause and resume sounds.")]
        [SerializeField] private GameFlow flow;

        [Header("Moves")]
        [SerializeField] private Sound laneChange;
        [SerializeField] private Sound slide;
        [SerializeField] private Sound sprint;
        [SerializeField] private Sound jump;
        [SerializeField] private Sound phase;
        [SerializeField] private Sound charged;

        [Header("Outcomes")]
        [SerializeField] private Sound wallSmash;
        [SerializeField] private Sound hit;
        [SerializeField] private Sound levelComplete;

        [Header("Flow")]
        [SerializeField] private Sound countInTick;
        [SerializeField] private Sound pause;
        [SerializeField] private Sound resume;

        private const int CountInTicks = 4; // the "4-3-2-1" bar

        private AudioSource _oneShots;
        private AudioSource[] _ticks;
        private bool _wasStarted, _wasPaused, _wasCharged;

        private void Awake()
        {
            _oneShots = gameObject.AddComponent<AudioSource>();
            _oneShots.playOnAwake = false;
            _ticks = new AudioSource[CountInTicks];
            for (int i = 0; i < CountInTicks; i++)
            {
                _ticks[i] = gameObject.AddComponent<AudioSource>();
                _ticks[i].playOnAwake = false;
            }
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.GestureFired += OnGesture;
                input.HoldStarted += OnHold;
            }
            if (runner != null)
            {
                runner.Judged += OnJudged;
                runner.RunFailed += OnFailed;
                runner.RunCompleted += OnCompleted;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.GestureFired -= OnGesture;
                input.HoldStarted -= OnHold;
            }
            if (runner != null)
            {
                runner.Judged -= OnJudged;
                runner.RunFailed -= OnFailed;
                runner.RunCompleted -= OnCompleted;
            }
        }

        private bool Live => conductor != null && conductor.IsStarted && runner != null &&
                             runner.State == Runner.RunState.Running && (flow == null || !flow.IsPaused);

        private void Play(Sound s)
        {
            if (s.clip != null && s.volume > 0f) _oneShots.PlayOneShot(s.clip, s.volume);
        }

        private void OnGesture(GestureType type, double time)
        {
            if (!Live) return;
            if (type == GestureType.TapLeft || type == GestureType.TapRight) Play(laneChange);
            else if (type == GestureType.BothTap) Play(jump);
        }

        private void OnHold(GestureType type, double time)
        {
            if (!Live) return;
            if (type == GestureType.HoldLeft) Play(slide);
            else if (type == GestureType.HoldRight) Play(sprint);
            else if (type == GestureType.BothHold) Play(phase);
        }

        private void OnJudged(Obstacle o, Grade g)
        {
            if (o != null && o.kind == ObstacleKind.Wall && g != Grade.Miss) Play(wallSmash);
        }

        private void OnFailed(Obstacle o) => Play(hit);
        private void OnCompleted() => Play(levelComplete);

        private void Update()
        {
            if (conductor == null) return;

            // Count-in ticks, scheduled once the song clock exists; taken back and rescheduled around a pause.
            if (conductor.IsStarted && !_wasStarted) ScheduleTicks();
            _wasStarted = conductor.IsStarted;

            bool paused = flow != null && flow.IsPaused;
            if (paused != _wasPaused)
            {
                if (paused)
                {
                    foreach (AudioSource t in _ticks) t.Stop();
                    Play(pause);
                }
                else
                {
                    Play(resume);
                    ScheduleTicks();
                }
                _wasPaused = paused;
            }

            // Charge ready (only before a red wall, like the glow).
            bool chargedNow = Live && runner.HasCharge && runner.NextObstacleKind == ObstacleKind.Wall;
            if (chargedNow && !_wasCharged) Play(charged);
            _wasCharged = chargedNow;
        }

        private void ScheduleTicks()
        {
            if (countInTick.clip == null || !conductor.IsStarted) return;
            double now = AudioSettings.dspTime;
            for (int i = 0; i < CountInTicks; i++)
            {
                double at = conductor.DspTimeAtBeat(-CountInTicks + i); // beats -4, -3, -2, -1
                AudioSource src = _ticks[i];
                src.Stop();
                if (at <= now + 0.02) continue; // already past
                src.clip = countInTick.clip;
                src.volume = countInTick.volume;
                src.PlayScheduled(at);
            }
        }
    }
}
