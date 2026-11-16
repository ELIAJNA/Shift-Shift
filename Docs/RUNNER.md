# Runner rules (Runner / RunnerModel)

Code: `Assets/_Game/Scripts/Core/RunnerModel.cs` (rules, engine-free, tested), `Assets/_Game/Scripts/Runtime/Runner.cs`
(feeds gestures, moves visuals, judges obstacles). Test scene: `Shift+Shift > Create Greybox Scene`.

## How obstacles are judged

- **By rules, not physics.** When the runner reaches an obstacle (beat = obstacle Z / metres per beat, converted to
  song time by the clock), the state at that
  moment decides: Pass, Hit (run fails) or Break (wall). Colliders are not used.
- **Decision A (ELIA, day 10): actions count from key-down.** A slide that was pressed before a low bar passes it,
  even though the slide is only recognised 250 ms after the press. If a press made before the obstacle is still
  undecided, the Runner waits for it (at most one tap/hold threshold), so a fail can show up to ~250 ms late.
- **Visuals start on recognition** (lane ease 120 ms, jump arc 0.5 s, slide pose), so they can lag the rules slightly.
- **A lane change is "in between" from key-down to key-up** (day 11 fix): during that time a one-lane obstacle is
  passed if either the lane being left or the lane being entered is clear. Dodging out late still works (key-down
  counts) and moving back early works if the key-up comes after the obstacle, which matches what the screen shows.

| Obstacle | Passes when | Gesture |
|---|---|---|
| Block (one lane) | runner in another lane | Tap L / Tap R |
| Low bar (all lanes) | sliding | Hold L |
| Gate (one open lane, day 13) | in the opening **and** sprinting | lane change + Hold R |
| Wide bar (two lanes, day 13) | in its free lane | lane change |
| Hurdle (short pink wall, day 13) | in the air | Both tap |
| Gap (all lanes) | in the air at both edges | Both tap (airborne from key-down until the visual landing: recognised + 0.5 s) |
| Wall (all lanes) | a charge is available (used up); the last tap may land up to 120 ms after the wall | L, R, L, R quickly, any rhythm (each tap within 400 ms) |
| Phase barrier (all lanes) | phased | Both hold |
| Ghost layer (any kind) | only hits while phased | (let go of phase, or change lane) |

Phase passes **only** phase barriers. Every other obstacle needs its own answer whether phased or not (day 13 fix:
phase used to pass every solid obstacle, so holding both skipped the level).

Lane comes from the obstacle's X (lanes at -2, 0, 2 m), timing from its Z. Forward position = metres per beat (5) x
current beat: 10 m/s at the stub's 120 BPM, 8.3 m/s at 100 BPM. With a Conductor the runner waits for the start and
ignores gestures until then.

## Decisions taken while building (flag if you disagree)

1. **Sprint does not change forward speed.** The playbook says "speed x1.5", but obstacle timing comes from song
   time; actually speeding up would make every later obstacle arrive off the beat. Sprint = state for gates + FOV
   kick (60 → 72). Confirmed by ELIA, day 11: keep it.
2. **A charge is good for the next obstacle only** (changed by ELIA, day 20; was "until the next wall uses it").
   Every 4th quick alternating tap earns one; passing any obstacle uses it up, wall or not, and the counter above
   the runner goes out. A charge completed in a wall's grace window, after the obstacle before it, is kept.
   The counter above the runner shows only when the next obstacle is a red wall (ELIA, day 20).
3. ~~Walls are in the solid layer, so a phased runner also passes them.~~ Changed day 13: only a charge breaks a wall.
4. **Gaps are checked at both edges**, other obstacles at their centre.
5. **Charge taps also change lanes** (open question 1 in INPUT.md). Walls span all lanes, so it only looks odd.
6. Camera smoothing uses frame time (visual only, not rhythm timing).

## Wall wait and charge counter (day 20)

- A tap is recognised when its key is let go but counts from the key press. At a wall, the runner now also waits for a
  last tap that was **pressed** within the grace window (wall click + Good window, 120 ms) but not yet released. Before
  this, a fourth tap pressed in time but released after the window failed. Rule: `RunnerModel.WallMayStillBreak`
  (tested). Waiting is bounded: a pressed key is decided within the tap/hold threshold (250 ms).
- On a fail the runner stops just in front of the obstacle (`FailStop.Distance`, tested), even if it moved on
  while the judgement waited (ELIA, day 20). A gap fail stops at the gap's edge.
- Charge counter (GameFlow): dots above the runner, one per alternating tap towards the next charge, "CHARGED" once a
  charge is ready. Hidden when no charge is under way.
