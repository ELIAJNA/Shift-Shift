using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    /// <summary>
    /// Day 29: the runner now fails the moment it comes down inside a gap (Runner.Update), instead of at the far edge.
    /// This plays one gap with a jump pressed at every offset from 450 ms early to 450 ms late (5 ms steps), at every
    /// level's tempo, through the real GestureClassifier and RunnerModel, the way Runner.Update judges (60 fps, gap
    /// checks at both edges, half a jump later than the beat), once with the old rules and once with the new one, and
    /// checks every jump gets the same outcome and grade. The Unity side is mirrored here, not run.
    /// </summary>
    public class GapRuleTests
    {
        private const double JumpSeconds = 0.5, MetresPerBeat = 5, GapDepth = 2.5, Dt = 1.0 / 60.0;
        private const double PerfectSeconds = 0.050, GoodSeconds = 0.120;

        private sealed class Clock : IClock
        {
            public double Bpm;
            public double SongTime { get; set; }
            public double BeatAt(double t) => t * Bpm / 60.0;
            public double SongTimeAtBeat(double b) => b * 60.0 / Bpm;
        }

        private struct Result { public bool Passed; public Grade Grade; public double FailedAt; }

        private static Result Play(double bpm, double offset, bool landingFails)
        {
            var clock = new Clock { Bpm = bpm };
            var classifier = new GestureClassifier(new GestureThresholds
            {
                TapHoldSeconds = 0.250, BothWindowSeconds = 0.080, AlternateMaxGapSeconds = 0.400,
            }, clock);
            var model = new RunnerModel(new RunnerRules { JumpSeconds = JumpSeconds, ChargeTaps = 4, ChargeGraceSeconds = GoodSeconds });
            double now = 0;
            classifier.GestureFired += (t, s) => model.OnGestureFired(t, s, classifier.AlternateStreak, now);
            classifier.HoldStarted += (t, s) => model.OnHoldStarted(t, s);
            classifier.HoldEnded += (t, s) => model.OnHoldEnded(t, s);

            const double gapBeat = 8;
            double target = clock.SongTimeAtBeat(gapBeat);
            // ObstacleSpawner: the gap's centre is half a jump after its beat; Runner: checks at both edges.
            double centre = gapBeat * MetresPerBeat + JumpSeconds * bpm / 60.0 * MetresPerBeat * 0.5;
            double nearBeat = (centre - GapDepth / 2) / MetresPerBeat, farBeat = (centre + GapDepth / 2) / MetresPerBeat;
            var info = new ObstacleInfo { Kind = ObstacleKind.Gap, Layer = ObstacleLayer.Solid, AllLanes = true };

            double press = target + offset, release = press + 0.07; // both keys together, a quick tap
            bool pressed = false, released = false;
            int next = 0; // 0 = near edge check, 1 = far edge check, 2 = done
            for (now = target - 1.0; now < target + 2.0; now += Dt)
            {
                clock.SongTime = now;
                if (!pressed && press <= now) { classifier.Press(Side.Left, press); classifier.Press(Side.Right, press); pressed = true; }
                if (pressed && !released && release <= now) { classifier.Release(Side.Left, release); classifier.Release(Side.Right, release); released = true; }
                classifier.Update(now);

                // The new rule (Runner.Update, day 29): over the gap and down = the miss, there and then.
                if (landingFails && next == 1)
                {
                    bool pending = classifier.TryGetUnresolvedPress(out double at) && at <= now;
                    if (!pending && !model.IsAirborne(now)) return new Result { Passed = false, Grade = Grade.Miss, FailedAt = now };
                }
                while (next < 2)
                {
                    double checkTime = clock.SongTimeAtBeat(next == 0 ? nearBeat : farBeat);
                    if (checkTime > now) break;
                    if (classifier.TryGetUnresolvedPress(out double down) && down <= checkTime) break;
                    info.Time = checkTime;
                    ObstacleResolution r = model.ResolveWithAnswer(info);
                    if (r.Outcome == ObstacleOutcome.Hit) return new Result { Passed = false, Grade = Grade.Miss, FailedAt = now };
                    if (next == 1) return new Result { Passed = true, Grade = Judge.GradeOf(r, target, PerfectSeconds, GoodSeconds) };
                    next++;
                }
            }
            return new Result { Passed = false, Grade = Grade.None };
        }

        [TestCase(120)]
        [TestCase(128)]
        [TestCase(130)]
        [TestCase(144)]
        [TestCase(150)]
        [TestCase(190)]
        public void LandingRule_SameOutcomeAndGrade_AsBefore(double bpm)
        {
            var bands = new List<string>();
            Grade lastGrade = (Grade)(-1);
            double bandStart = 0;
            for (int ms = -450; ms <= 450; ms += 5)
            {
                Result before = Play(bpm, ms / 1000.0, false), after = Play(bpm, ms / 1000.0, true);
                Assert.AreEqual(before.Passed, after.Passed, $"{bpm} BPM, jump {ms:+0;-0} ms: pass/fail changed");
                Assert.AreEqual(before.Grade, after.Grade, $"{bpm} BPM, jump {ms:+0;-0} ms: grade changed");
                if (!after.Passed) Assert.LessOrEqual(after.FailedAt, before.FailedAt + 1e-9, "a miss is called no later than before");
                if (after.Grade != lastGrade)
                {
                    if (lastGrade != (Grade)(-1)) bands.Add($"{bandStart:+0;-0}..{ms - 5:+0;-0} ms {lastGrade}");
                    lastGrade = after.Grade;
                    bandStart = ms;
                }
            }
            bands.Add($"{bandStart:+0;-0}..+450 ms {lastGrade}");
            TestContext.WriteLine($"{bpm} BPM: " + string.Join(", ", bands));
            // The grades themselves, unchanged from the rules: on the beat PERFECT, within 120 ms GOOD.
            Assert.AreEqual(Grade.Perfect, Play(bpm, 0, true).Grade);
            Assert.AreEqual(Grade.Good, Play(bpm, -0.1, true).Grade);
        }
    }
}
