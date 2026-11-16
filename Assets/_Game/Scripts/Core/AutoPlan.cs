using System;
using System.Collections.Generic;

namespace ShiftShift.Core
{
    /// <summary>One key held from <see cref="Start"/> to <see cref="End"/> (song seconds).</summary>
    public readonly struct KeyHold
    {
        public readonly Side Side;
        public readonly double Start, End;

        public KeyHold(Side side, double start, double end)
        {
            Side = side;
            Start = start;
            End = end;
        }
    }

    /// <summary>
    /// Trailer mode (ELIA, day 23): the key presses that play a level perfectly, worked out from the level file.
    /// Every answer is pressed exactly on its obstacle's beat, so it grades PERFECT.
    /// - Lane obstacles (bar, wide, gate, ghost blocks): move to the nearest free lane, the last tap on the beat (or
    ///   just before, when a slide, sprint or phase must start on the beat).
    /// - Low bar: hold Left. Gate: hold Right. Gap, hurdle: both together. Barriers: hold both. Wall: four alternating
    ///   taps ending on the beat, starting on the side that leaves the runner in the same lane.
    /// Holds that follow each other closely are joined, so two barriers a beat apart are one long phase.
    /// Lanes here are runner lanes (-1, 0, 1), as in RunnerModel.
    /// </summary>
    public static class AutoPlan
    {
        public const double TapSeconds = 0.07;      // well under the tap/hold threshold
        public const double HoldSeconds = 0.35;     // well over it, and past the obstacle
        public const double TapGap = 0.15;          // between taps of one move or a charge (alternate gap is 0.4 s)
        // A lane move before a slide, sprint, phase or jump on the same beat: a short tap just before the beat, let go
        // before the hold presses (a GOOD, not a PERFECT: the tap cannot be on the beat and finished before it).
        public const double LaneLead = 0.09;
        public const double LeadTapSeconds = 0.04;
        public const double MinHoldSeconds = 0.30;  // a shortened hold, still well over the 250 ms tap/hold threshold
        public const double ReleaseGap = 0.06;      // keys up between two holds (a few frames)

        private enum Act { None, Slide, Sprint, Jump, Phase, Charge }

        public static List<KeyHold> Build(LevelData level)
        {
            var tempo = new TempoMap(level.bpm, level.tempo);
            var events = new List<LevelEvent>(level.events ?? Array.Empty<LevelEvent>());
            events.Sort((a, b) => a.beat.CompareTo(b.beat));

            // Group by beat: lanes blocked there, and the move needed there.
            var beats = new List<double>();
            var blocked = new List<bool[]>();
            var acts = new List<Act>();
            foreach (LevelEvent e in events)
            {
                if (!ObstacleTypes.TryGet(e.type, out ObstacleTypes.Spec spec)) continue;
                int g = beats.Count - 1;
                if (g < 0 || Math.Abs(beats[g] - e.beat) > 1e-6)
                {
                    beats.Add(e.beat);
                    blocked.Add(new bool[3]);
                    acts.Add(Act.None);
                    g++;
                }
                int lane = e.lane; // chart lane 0..2
                if (spec.GhostBlocks == ObstacleTypes.GhostBlocks.EventLane) blocked[g][lane] = true;
                else if (spec.GhostBlocks == ObstacleTypes.GhostBlocks.OtherLanes || spec.LaneIsOpening)
                {
                    for (int l = 0; l < 3; l++) if (l != lane) blocked[g][l] = true;
                }
                else if (!spec.AllLanes) blocked[g][lane] = true;

                Act a = spec.Kind switch
                {
                    ObstacleKind.Low => Act.Slide,
                    ObstacleKind.Gate => Act.Sprint,
                    ObstacleKind.Gap => Act.Jump,
                    ObstacleKind.Hurdle => Act.Jump,
                    ObstacleKind.Barrier => Act.Phase,
                    ObstacleKind.Wall => Act.Charge,
                    _ => Act.None,
                };
                if (a != Act.None) acts[g] = a;
            }

            var holds = new List<KeyHold>();
            int cur = 1; // chart lane; the runner starts in the middle
            for (int g = 0; g < beats.Count; g++)
            {
                double t = tempo.TimeAtBeat(beats[g]);
                Act act = acts[g];

                if (blocked[g][cur])
                {
                    int target = ChooseLane(cur, blocked[g], g + 1 < beats.Count ? blocked[g + 1] : null);
                    int n = Math.Abs(target - cur);
                    Side side = target < cur ? Side.Left : Side.Right;
                    double last = act == Act.None ? t : t - LaneLead;
                    double len = act == Act.None ? TapSeconds : LeadTapSeconds;
                    for (int i = 0; i < n; i++)
                    {
                        double at = last - (n - 1 - i) * TapGap;
                        holds.Add(new KeyHold(side, at, at + len));
                    }
                    cur = target;
                }

                switch (act)
                {
                    case Act.Slide: holds.Add(new KeyHold(Side.Left, t, t + HoldSeconds)); break;
                    case Act.Sprint: holds.Add(new KeyHold(Side.Right, t, t + HoldSeconds)); break;
                    case Act.Jump:
                        holds.Add(new KeyHold(Side.Left, t, t + TapSeconds));
                        holds.Add(new KeyHold(Side.Right, t, t + TapSeconds));
                        break;
                    case Act.Phase:
                        holds.Add(new KeyHold(Side.Left, t, t + HoldSeconds));
                        holds.Add(new KeyHold(Side.Right, t, t + HoldSeconds));
                        break;
                    case Act.Charge:
                    {
                        // Start on the side that brings the runner back to its lane (taps change lanes, clamped).
                        Side first = cur == 0 ? Side.Right : Side.Left;
                        for (int i = 0; i < 4; i++)
                        {
                            Side s = (i % 2 == 0) ? first : (first == Side.Left ? Side.Right : Side.Left);
                            double at = t - (3 - i) * TapGap;
                            holds.Add(new KeyHold(s, at, at + TapSeconds));
                            cur = Math.Max(0, Math.Min(2, cur + (s == Side.Left ? -1 : 1)));
                        }
                        break;
                    }
                }
            }
            return Join(holds);
        }

        /// <summary>The free lane nearest to the current one; on a tie, the one that suits the next beat (else the middle).</summary>
        private static int ChooseLane(int cur, bool[] blockedNow, bool[] blockedNext)
        {
            int best = -1;
            double bestScore = double.MaxValue;
            for (int l = 0; l < 3; l++)
            {
                if (blockedNow[l]) continue;
                double score = Math.Abs(l - cur) * 10;
                if (blockedNext != null && blockedNext[l]) score += 3;
                score += Math.Abs(l - 1); // prefer the middle
                if (score < bestScore) { bestScore = score; best = l; }
            }
            return best < 0 ? cur : best;
        }

        /// <summary>
        /// Two holds on one key that would touch: the first is cut short so the key is let go and pressed again on
        /// the next beat (each barrier answered on its own beat); if there is no room for that, they become one hold.
        /// </summary>
        private static List<KeyHold> Join(List<KeyHold> holds)
        {
            var result = new List<KeyHold>();
            foreach (Side side in new[] { Side.Left, Side.Right })
            {
                var list = holds.FindAll(h => h.Side == side);
                list.Sort((a, b) => a.Start.CompareTo(b.Start));
                int first = result.Count;
                foreach (KeyHold h in list)
                {
                    int last = result.Count - 1;
                    if (last >= first && h.Start < result[last].End + ReleaseGap && IsHold(result[last]) && IsHold(h))
                    {
                        KeyHold p = result[last];
                        if (h.Start - ReleaseGap - p.Start >= Math.Min(MinHoldSeconds, p.End - p.Start))
                        {
                            result[last] = new KeyHold(side, p.Start, h.Start - ReleaseGap);
                            result.Add(h);
                        }
                        else result[last] = new KeyHold(side, p.Start, Math.Max(p.End, h.End)); // no room: one long hold
                    }
                    else result.Add(h);
                }
            }
            result.Sort((a, b) => a.Start.CompareTo(b.Start));
            return result;
        }

        private static bool IsHold(KeyHold h) => h.End - h.Start > TapSeconds + 1e-9;

        public readonly struct KeyEdge
        {
            public readonly Side Side;
            public readonly bool Down;
            public readonly double Time;
            public KeyEdge(Side side, bool down, double time) { Side = side; Down = down; Time = time; }
        }

        /// <summary>The plan as key presses and releases in time order (releases first on a tie).</summary>
        public static List<KeyEdge> Edges(List<KeyHold> plan)
        {
            var edges = new List<KeyEdge>();
            foreach (KeyHold h in plan)
            {
                edges.Add(new KeyEdge(h.Side, true, h.Start));
                edges.Add(new KeyEdge(h.Side, false, h.End));
            }
            edges.Sort((x, y) => x.Time != y.Time ? x.Time.CompareTo(y.Time) : x.Down.CompareTo(y.Down));
            return edges;
        }

        /// <summary>Whether a key is down at a song time, given the plan.</summary>
        public static bool IsDown(List<KeyHold> plan, Side side, double time)
        {
            foreach (KeyHold h in plan)
            {
                if (h.Start > time) break;
                if (h.Side == side && time < h.End) return true;
            }
            return false;
        }
    }
}
