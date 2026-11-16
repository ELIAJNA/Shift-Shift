using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    /// <summary>
    /// Trailer mode: plays each real level with the AutoPlan key presses through the real gesture classifier and
    /// runner rules, the way Runner.Update judges (frame by frame at 60 fps, waiting for undecided presses), and
    /// checks the run is never hit. Mirrors the game's settings: GestureSettings asset, jumpSeconds 0.5, 5 m per beat,
    /// gap depth 2.5 m. The Unity side (ShiftInput, Runner, the spawner's positions) is not run here.
    /// </summary>
    public class AutoPlanTests
    {
        private const double JumpSeconds = 0.5;
        private const double MetresPerBeat = 5.0;
        private const double GapDepth = 2.5;
        private const double PerfectSeconds = 0.050, GoodSeconds = 0.120;

        private sealed class SimClock : IClock
        {
            public TempoMap Tempo;
            public double SongTime { get; set; }
            public double BeatAt(double songTime) => Tempo.BeatAtTime(songTime);
            public double SongTimeAtBeat(double beat) => Tempo.TimeAtBeat(beat);
        }

        private struct Check
        {
            public ObstacleInfo Info;
            public double Beat, TargetBeat;
            public bool Final;
            public string Name;
        }

        private static readonly string[] Levels = { "level01", "level02", "level03", "level04", "level05", "level06" };
        // 60 fps, a slow recording (20 fps) and an uneven, very slow one (8 fps): the result must not change.
        private static readonly double[] FrameRates = { 60, 20, 8 };

        [Test]
        public void Autoplay_ClearsLevel_NeverLate([ValueSource(nameof(Levels))] string file, [ValueSource(nameof(FrameRates))] double fps)
        {
            LevelData level = Load(file);
            var grades = Simulate(level, 1.0 / fps, out string failure);
            Assert.IsNull(failure, failure);
            TestContext.WriteLine($"{file}: {grades[Grade.Perfect]} perfect, {grades[Grade.Good]} good, {grades[Grade.Ok]} ok, {grades[Grade.None]} none");
            Assert.Greater(grades[Grade.Perfect], 0);
        }

        [Test]
        public void Plan_HoldsNeverOverlapOnOneKey()
        {
            foreach (string file in new[] { "level01", "level02", "level03", "level04", "level05", "level06" })
            {
                List<KeyHold> plan = AutoPlan.Build(Load(file));
                foreach (Side side in new[] { Side.Left, Side.Right })
                {
                    var list = plan.FindAll(h => h.Side == side);
                    for (int i = 1; i < list.Count; i++)
                        Assert.Greater(list[i].Start, list[i - 1].End, $"{file} {side} at {list[i].Start:0.000}");
                }
            }
        }

        private static Dictionary<Grade, int> Simulate(LevelData level, double dt, out string failure)
        {
            failure = null;
            var tempo = new TempoMap(level.bpm, level.tempo);
            var clock = new SimClock { Tempo = tempo };
            var classifier = new GestureClassifier(new GestureThresholds
            {
                TapHoldSeconds = 0.250, BothWindowSeconds = 0.080, AlternateMaxGapSeconds = 0.400,
            }, clock);
            var model = new RunnerModel(new RunnerRules { JumpSeconds = JumpSeconds, ChargeTaps = 4, ChargeGraceSeconds = GoodSeconds });
            double now = 0;
            classifier.GestureFired += (t, s) => model.OnGestureFired(t, s, classifier.AlternateStreak, now);
            classifier.HoldStarted += (t, s) => model.OnHoldStarted(t, s);
            classifier.HoldEnded += (t, s) => model.OnHoldEnded(t, s);

            List<Check> checks = Checks(level, tempo);
            List<AutoPlan.KeyEdge> edges = AutoPlan.Edges(AutoPlan.Build(level));
            int nextEdge = 0;
            var rng = new Random(1);
            var grades = new Dictionary<Grade, int> { { Grade.None, 0 }, { Grade.Perfect, 0 }, { Grade.Good, 0 }, { Grade.Ok, 0 }, { Grade.Miss, 0 } };
            int next = 0;
            double end = tempo.TimeAtBeat(level.lengthBeats);
            for (now = -1.0; now <= end + dt; now += dt * (0.5 + rng.NextDouble()))
            {
                clock.SongTime = now;
                // ShiftInput + AutoPlayer: every edge up to now, at its exact time.
                while (nextEdge < edges.Count && edges[nextEdge].Time <= now)
                {
                    AutoPlan.KeyEdge e = edges[nextEdge++];
                    if (e.Down) classifier.Press(e.Side, e.Time);
                    else classifier.Release(e.Side, e.Time);
                }
                classifier.Update(now);

                // Runner.Update
                while (next < checks.Count && tempo.TimeAtBeat(checks[next].Beat) <= now)
                {
                    Check c = checks[next];
                    c.Info.Time = tempo.TimeAtBeat(c.Beat);
                    if (classifier.TryGetUnresolvedPress(out double pd) && pd <= c.Info.Time) break;
                    if (c.Info.Kind == ObstacleKind.Wall)
                    {
                        bool pending = classifier.TryGetUnresolvedPress(out double pd2);
                        if (model.WallMayStillBreak(c.Info.Time, classifier.AlternateStreak, pending, pd2, now)) break;
                    }
                    ObstacleResolution r = model.ResolveWithAnswer(c.Info);
                    if (r.Outcome == ObstacleOutcome.Hit)
                    {
                        failure = $"hit {c.Name} (check at beat {c.Beat:0.###}, lane {model.LaneAt(c.Info.Time)})";
                        return grades;
                    }
                    if (c.Final)
                    {
                        Grade g = Judge.GradeOf(r, tempo.TimeAtBeat(c.TargetBeat), PerfectSeconds, GoodSeconds);
                        grades[g]++;
                        if (g == Grade.Good || g == Grade.Ok)
                        {
                            double off = r.AnswerTime - tempo.TimeAtBeat(c.TargetBeat);
                            TestContext.WriteLine($"  {g}: {c.Name}, answer {off:+0.000;-0.000} s");
                            // Allowed: a lane move just before a hold on the same beat (early GOOD), or an answer
                            // made long before (already in the right lane, or one long phase over close barriers).
                            if (off > 0 || (off < -GoodSeconds && off > -0.25))
                            {
                                failure = $"{c.Name}: answer {off:+0.000;-0.000} s off the beat";
                                return grades;
                            }
                        }
                    }
                    next++;
                }
            }
            if (next < checks.Count) failure = $"run ended with {checks.Count - next} checks left";
            return grades;
        }

        /// <summary>The checks the spawner and Runner.CollectObstacles make from a level.</summary>
        private static List<Check> Checks(LevelData level, TempoMap tempo)
        {
            var checks = new List<Check>();
            foreach (LevelEvent e in level.events)
            {
                if (!ObstacleTypes.TryGet(e.type, out ObstacleTypes.Spec spec)) continue;
                bool ghostBlocks = spec.GhostBlocks != ObstacleTypes.GhostBlocks.None;
                double z = e.beat * MetresPerBeat;
                if (spec.Kind == ObstacleKind.Gap || spec.Kind == ObstacleKind.Hurdle)
                    z += JumpSeconds * tempo.BpmAtBeat(e.beat) / 60.0 * MetresPerBeat * 0.5;
                var info = new ObstacleInfo
                {
                    Kind = spec.Kind, Layer = spec.Layer, AllLanes = spec.AllLanes || ghostBlocks,
                    LaneIsOpening = spec.LaneIsOpening,
                    Lane = spec.LaneIsOpening ? e.RunnerLane : (spec.AllLanes || ghostBlocks ? 0 : e.RunnerLane),
                };
                string name = $"{e.type} b{e.beat:0.##} lane {e.lane}";
                if (spec.Kind == ObstacleKind.Gap)
                {
                    checks.Add(new Check { Info = info, Beat = (z - GapDepth * 0.5) / MetresPerBeat, TargetBeat = e.beat, Final = false, Name = name });
                    checks.Add(new Check { Info = info, Beat = (z + GapDepth * 0.5) / MetresPerBeat, TargetBeat = e.beat, Final = true, Name = name });
                }
                else checks.Add(new Check { Info = info, Beat = z / MetresPerBeat, TargetBeat = e.beat, Final = true, Name = name });

                if (!ghostBlocks) continue;
                for (int lane = -1; lane <= 1; lane++)
                {
                    bool block = spec.GhostBlocks == ObstacleTypes.GhostBlocks.EventLane ? lane == e.RunnerLane : lane != e.RunnerLane;
                    if (!block) continue;
                    checks.Add(new Check
                    {
                        Info = new ObstacleInfo { Kind = ObstacleKind.Block, Layer = ObstacleLayer.Ghost, Lane = lane },
                        Beat = z / MetresPerBeat, TargetBeat = e.beat, Final = true, Name = name + $" ghost block {lane + 1}",
                    });
                }
            }
            // Stable order, like List.Sort on beat in the game (ties keep spawn order there only by chance).
            var sorted = new List<Check>(checks);
            sorted.Sort((a, b) => a.Beat.CompareTo(b.Beat));
            return sorted;
        }

        private static LevelData Load(string name)
        {
            string dir = Directory.GetCurrentDirectory();
            string path = null;
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string p = Path.Combine(dir, "Assets", "_Game", "Levels", name + ".json");
                if (File.Exists(p)) { path = p; break; }
                dir = Path.GetDirectoryName(dir);
            }
            if (path == null)
            {
                dir = AppContext.BaseDirectory;
                for (int i = 0; i < 10 && dir != null; i++)
                {
                    string p = Path.Combine(dir, "Assets", "_Game", "Levels", name + ".json");
                    if (File.Exists(p)) { path = p; break; }
                    dir = Path.GetDirectoryName(dir);
                }
            }
            Assert.IsNotNull(path, $"{name}.json not found above {Directory.GetCurrentDirectory()}");
            string json = File.ReadAllText(path);
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.FromJson<LevelData>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<LevelData>(json,
                new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }
    }
}
