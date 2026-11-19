# Build log

## 2026-11-09 — D09–D10: repo, project, input classifier

**Done**
- Public repo JNA1980/Shift-Shift: Unity .gitignore, Git LFS, README, MIT LICENSE, Docs/ (PLAN, SCOPE, CREDITS, INPUT, SETUP).
- Unity 6000.6.8f1 URP project (Universal 3D template), Input System active, Web build profile (Desktop - Development).
- GestureClassifier (engine-free, Assets/_Game/Scripts/Core) + 24 edit-mode tests; ShiftInput + GestureDebugHud (Runtime); BuildScript with WebGL/Windows menu items; tools/butler-push.sh.
- InputTest scene (only scene in the build list).
- Fixed setup issues: Unity project had been created nested (Shift-Shift/Shift-Shift); moved up, stray copy deleted. Build intermediates in /Data/ git-ignored. Commit author emails rewritten to the GitHub no-reply address.

**Tested**
- 24/24 edit-mode tests pass in Unity and via `dotnet test tools/CoreTests`.
- All 7 gestures register in the editor and in the Web build (Edge, local Build And Run).
- Lane taps resolve on key-up (~60–107 ms after key-down in the overlay); ELIA: feels fine. Keep as is.

**Uncertain / not yet tested**
- Shift+Shift > Build WebGL menu item not yet run (Build And Run was used instead).
- Firefox and Chrome not yet tested (only Edge).
- Sticky Keys behaviour with the Windows shortcut on: not recorded.
- Input timestamps use realtimeSinceStartup, not dspTime yet; frame-quantised (~16 ms).
- Playbook alignment pending: see next entry.

## 2026-11-09 — D10: input aligned with playbook prompt 1.1

**Done**
- Thresholds moved to a `GestureSettings` ScriptableObject (`Assets/_Game/Settings/GestureSettings.asset`), wired into InputTest. Live edits in Play mode rebuild the classifier.
- `IClock` interface (Core) + `DspClock` stub (dspTime, fixed BPM, default 120 on ShiftInput).
- Events now `GestureFired(type, songTime)`, `HoldStarted`, `HoldEnded`; GestureType is the seven gestures.
- Alternate is now beat-based (Option 1, ELIA's decision): within ±0.25 beats of a beat line, alternating sides, consecutive beats; streak expires when the next beat passes.
- Overlay shows key states, clock + beat, thresholds, alternate streak and the last five gestures.
- Kept `Scripts/Core` + `Scripts/Runtime` instead of `Scripts/Input/` (Core split lets tests run without Unity; moving files risks breaking scene references).

**Tested**
- 27/27 classifier tests pass via `dotnet test tools/CoreTests` (tap/hold boundary, both-tap in/out of window, alternate on/off beat, expiry, lockout).
- Runtime scripts compile against stand-in Unity types (catches C# errors only).
- Found and fixed while testing: the alternate streak expired at key-up even when the tap's key-down was on the beat.

**Not yet run**
- Nothing above has been compiled by Unity or played. Needs: compile, Test Runner, Play InputTest, Web build.
- dspTime in WebGL: does the clock advance before the first click/key? Does it move smoothly enough for 120 ms windows?

## 2026-11-09 — D10: project setup per playbook 0.3

**Done**
- `Shift+Shift > Apply Project Setup` (Assets/_Game/Scripts/Editor/ProjectSetup.cs): creates missing folders (Materials, UI, Settings, …), Boot/Menu/Run scenes, build list Boot → Menu → Run (InputTest kept after), a neutral URP Volume profile at Assets/_Game/Materials/PostProcessProfile.asset, and Web player settings (1280x720, Brotli + fallback, data caching, explicit exceptions, strip engine code, IL2CPP optimise for size). Re-runnable; never overwrites scenes or the profile.
- Already correct before this: Active Input Handling = Input System only.

**Not yet run**
- Not compiled by Unity. Least certain line: `PlayerSettings.SetIl2CppCodeGeneration(...)` in Unity 6000.6.
- Left for the editor: Windows x64 build profile; Web build profile "Code Optimization".

## 2026-11-09 — D10: upload scripts per playbook 0.4

**Done**
- `tools/upload.ps1` (Windows) and `tools/upload.sh` push Builds/WebGL to channel `html` and a zipped Builds/Windows to `windows`; itch user/project read from `tools/itch.env` (git-ignored; template `tools/itch.env.example`). Replaces tools/butler-push.sh. Folder kept lowercase `tools/` (case-only renames are unreliable on Windows).

**Tested**
- Both scripts run against a fake butler (PowerShell 7 on Linux): correct targets and version, Windows zip contents, and clear errors for missing env file, placeholder user, missing builds, bad channel, butler failure.

**Not yet run**
- Windows PowerShell 5.1 on ELIA's PC; real butler upload (needs itch draft project + `butler login`).

## 2026-11-09 — D10: Input System errors after script reload

- After pulling the input changes, Play mode logged InputActionState errors ("Map/Control/Binding index out of range", NullReferenceException on Key:/Keyboard/leftShift). Gestures still worked.
- None of our code uses InputActions (ShiftInput polls Keyboard directly). After restarting Unity: 0 errors. Conclusion: stale action state after the domain reload (template's project-wide actions, or an Input System 1.20.1 reload bug).
- If it recurs after pulls: restart Unity. If it becomes a nuisance: Project Settings > Input System Package > Project-wide Actions = None (nothing in the game uses them).

## 2026-11-09 — D10: dspTime clock verified in the Web build (Edge)

**Tested (ELIA, Web build, Edge, InputTest)**
- DspClock advances before any click/key (no frozen clock before audio unlock).
- Holds decide at +255 ms against a 250 ms threshold (~5 ms frame delay); hold ends at +0 ms.
- Tap timestamps look smooth (gaps 279–328 ms, no fixed step), so no audio-buffer quantisation visible at this level.
- Alternate streak counts up. Alt+Tab while holding Shift: key not stuck.
- All 7 gestures register; no errors in Unity's console after the build.

**Uncertain**
- Chrome and Firefox not yet tested (day 28 browser pass). Browser F12 console not checked on this build.
- Input-to-audio latency not measured yet (day 13, needs Conductor + clicks).
- Info message from the template's experimental `com.unity.pipeline` package on every build: harmless; removal is out of scope for now.

## 2026-11-09 — D10: project setup applied (ELIA)

**Done**
- Ran Shift+Shift > Apply Project Setup: Materials/UI folders, Boot/Menu/Run scenes, build list Boot → Menu → Run → InputTest, neutral Volume profile. Committed (29af827).
- Found: the player-settings changes were not on disk until File > Save Project, and in Unity 6.6 they land in the active Web build profile's own Player Settings copy (Assets/Settings/Build Profiles/Web - Desktop - Development.asset), not ProjectSettings.asset. Confirmed in the profile UI: 1280x720, IL2CPP optimise for code size, strip engine code, Input System only. Decision: keep the profile copy (it is what Web builds use).
- ProjectSetup now runs File > Save Project itself.

**Uncertain**
- Whether Shift+Shift > Build WebGL (BuildPipeline.BuildPlayer) uses the active profile's player settings or the global ones. Check the first time the menu build is used (by day 24).
- The Web profile is a Development preset (C++ config Debug, Development Build). A release profile is needed before the itch upload (day 24/28).
- Web "Code Optimization" option is not offered in 6000.6; skipped.

## 2026-11-09 — D10: closed

- Web profile player settings verified in the committed profile asset (d630369): 1280x720, Brotli, decompression fallback on, data caching on, explicit exceptions only, strip engine code, IL2CPP code generation = size, Input System only. C++ compiler configuration is Debug (Development profile); Release profile still needed before upload.
- Playbook Day 0 (0.1–0.4) and 1.1 complete. Plan "done when" for days 9–10 met: WebGL build runs in a browser and all 7 gestures log correctly. About one day ahead of plan.
- Still ELIA's (not blocking): itch.io draft project, butler install + login, tools/itch.env.
- Next: playbook 1.2 Runner + TrackBuilder (day 11).

## 2026-11-09 — D11: Runner, TrackBuilder, chase camera (playbook 1.2)

**Done**
- RunnerModel (Core): lanes, slide/sprint/phase intervals, jump window, charges, obstacle judging. Decision A: all actions count from key-down; Runner waits on undecided presses before judging.
- Runner (forward position from song time, visuals, RunFailed/RunCompleted, debug panel top-right), Obstacle, TrackPiece, TrackBuilder, ChaseCamera (lag + sprint FOV kick).
- GestureClassifier/ShiftInput: TryGetUnresolvedPress (additive). GestureSettings: chargeTaps = 4.
- Shift+Shift > Create Greybox Scene: materials, track pieces (Straight/Tunnel/Bridge/Gap 2.5 m), obstacle prefabs, 200 m Greybox scene with one obstacle of each behaviour.
- Docs/RUNNER.md: rules and six decisions taken while building (notably: sprint does not change speed).

**Tested**
- 47/47 core tests (`dotnet test tools/CoreTests`): 17 runner rule tests + 3 unresolved-press tests added.
- Runtime and greybox editor scripts compile against stand-in Unity types.

**Cannot verify without Play mode (playbook 1.2 asks for this list)**
- Lane ease feel (120 ms) and whether the visual lag behind key-down feels responsive.
- Jump arc height/duration vs. the 2.5 m gap; slide pose; phase material swap.
- Chase camera lag, look-ahead, sprint FOV kick.
- Fail timing: whether a fail judged up to ~250 ms late (decision A wait) is noticeable.
- That obstacle positions match the rules (lane from X, time from Z) in the generated scene.
- The generated scene, prefabs and URP materials themselves (editor script not run by Unity yet).

## 2026-11-09 — D11: fix editor assembly reference

- Unity compile errors in GreyboxSetup.cs: `ShiftShift.Core` not found. Cause: ShiftShift.Editor.asmdef did not reference ShiftShift.Core (the stand-in compile builds everything as one assembly, so it cannot catch missing asmdef references). Added the reference.
- Warning CS0618 in Runner.cs: `FindObjectsByType(FindObjectsSortMode)` is deprecated in 6000.6. Harmless; left as is until the replacement overload is confirmed from the full warning text.
- ELIA confirmed: sprint does not change forward speed.

## 2026-11-09 — D11: greybox scene, first Play

- 47/47 tests pass in Unity. Greybox scene generated (track, runner, obstacles, camera all wired) but Play failed: ShiftInput.settings was empty in the generated scene, so input disabled itself and the Runner refused to start. All other references set by the tool were fine.
- Likely cause (not confirmed): the settings asset was loaded at the start of the tool, before ~20 prefab/material assets were created; an asset reload in between left the reference stale. Fix: load it just before assigning, and every assignment is now verified and logs an error if it did not stick. ELIA assigns it by hand in the existing scene.

## 2026-11-09 — D11: beat dot on the debug panel (ELIA asked)

- Wall charge kept failing: with no beat cue, on-beat taps were effectively random. With Alternate Beat Tolerance temporarily at 0.45 (not committed), the charge and wall work.
- Added a beat dot to GestureDebugHud: lit for the first 20% of each beat (yellow on beat 1 of the bar, green otherwise) with the beat number 1-4. Debug aid until the Conductor's click track (day 12).
- Not run in Unity yet. Compiles against stand-in types.

## 2026-11-09 — D11: greybox played (ELIA)

**Tested (editor, Greybox scene)**
- Day 11 question answered: moving the capsule with two keys feels responsive.
- Wall charge works with a beat reference (tolerance back at 0.25; beat dot added).
- Greybox scene with settings assigned committed (bf4d5c4).

**Open bug (not yet diagnosed)**
- "Sometimes after an obstacle is passed I have to wait before changing lane, or it fails."
  Hypotheses: (1) blocks judged at their centre while they look passed ~80 ms earlier, so a tap back counts as inside the lane (decision A uses key-down); (2) phase lockout ignores taps (would not cause a fail); (3) ordering bug. Waiting on the `[Run] Failed` line plus the preceding `[Gesture]` taps to compare times.

## 2026-11-09 — D11: fix "fails after passing a block"

**Diagnosis (from ELIA's run)**
- TapLeft key-down 2.880 s, recognised 3.051 s; Block @30m (middle) at 3.000 s. Rules (decision A, key-down) put the runner back in the middle before the block, while the screen only moves the capsule after key-up (3.05 s + 120 ms ease). The fail was then shown at 30.5 m because judging waited for the tap to resolve, so it looked like a fail after passing.

**Fix (ELIA: go)**
- A lane change is "in between" from key-down to key-up: a one-lane obstacle passes if either lane is clear. RunnerModel records each tap's recognition time; Runner passes it in. Holds, jumps, phase unchanged.

**Tested**
- 52/52 core tests (5 new, including ELIA's exact timings → Pass, and released-before-block → Hit).
- New check: Core, Runtime and Editor compiled as separate assemblies with only their asmdef references (non-transitive, like Unity). Verified it reproduces yesterday's missing-reference error.

**Not yet run**
- In Unity / Play mode: re-run the 30 m block the same way (tap back just before it, release after).

## 2026-11-09 — D11: wall feel note (for M1 review, day 15)

- ELIA: after L R L R the red wall "looked like I failed but it went through". Rules are correct (charge breaks the next wall; no extra input needed). The look comes from: wall hidden only when the runner reaches its centre (capsule already touching the face), no break effect yet (day 27 juice), and a possible up-to-250 ms wait when a key was down just before the wall (decision A).
- Not changed. Option if needed: break walls at their front face. Decide at M1 review.

## 2026-11-09 — D11: 30 m block fix confirmed (ELIA)

## 2026-11-09 — D12: Conductor and level JSON (playbook 1.3)

**Done**
- Conductor: waits for the first Shift press (browser audio unlock), schedules audio with PlayScheduled at dspTime + 1 s, SongTime = dspTime - start - offset, BPM, CurrentBeat, Beat event; becomes ShiftInput's clock. Start detected in LateUpdate so the start press is not also a gesture.
- IClock.SongTimeAtBeat. Runner: forward position = 5 m per beat x current beat; obstacle timing from beats via the clock; waits for the Conductor; run ends at the level's last beat; music stops on fail.
- LevelData + validation + type mapping (Core), LevelLoader (JsonUtility, WebGL-safe), ObstacleSpawner (beat → Z, lane → X, prefab per type).
- level00.json: 100 BPM, 32 beats, a bar every 4 beats from beat 8, alternating left/right.
- Shift+Shift > Create Level Test Scene: generates click_100.wav (if missing) and LevelTest.unity.
- Docs/LEVELS.md (format), CREDITS (generated click track).

**Deviations from the playbook (flagged to ELIA)**
1. Click track generated by an editor menu (C#) instead of a Python script in Tools/: no Python needed on ELIA's PC, and the WAV must be committed from his machine (LFS uploads are blocked from the cloud session).
2. Obstacle "tags" are the Obstacle component's kind/layer, not Unity tags (Unity tags need TagManager edits; the rules never read tags).
3. Chart type "ghost" = phase barrier (playbook 1.4: ghost -> phase held). Ghost-layer obstacles are not in the format yet (needed for level 4).

**Tested**
- 62/62 core tests (level validation, type mapping, lane mapping, click WAV header/length/click positions).
- Per-assembly compile check (Core / Runtime / Editor with asmdef references) passes.

**Not yet run**
- Anything in Unity: PlayScheduled timing, whether obstacles pass exactly on the click, generated scene and WAV import.
- WebGL: PlayScheduled and audio start after the key press (day 13).

## 2026-11-09 — D12: fix empty references in generated scenes (root cause)

- LevelTest generated without a track ("TrackBuilder: no fill piece assigned"). Root cause, also behind day 11's empty Settings field: the tools loaded existing assets and then called EditorSceneManager.NewScene(Single), which unloads assets nothing references yet, leaving the references empty. Freshly created prefabs survived, which is why only some fields failed.
- Fix: both tools create the new scene first, then load/create assets. Track fill piece now assigned through the verified Set(); the spawner logs any missing type prefab.
- Runner: FindObjectsByType without the deprecated sort parameter (6000.6 warning).
- Greybox scene from day 11 is unaffected. LevelTest.unity must be deleted and regenerated.

## 2026-11-09 — D12: clearer level-file errors in the level test tool

- Regenerating LevelTest failed with "level00.json is missing or invalid" (it had loaded fine on the previous run; file unchanged in git). Waiting on `git status` from ELIA to see whether it was deleted. The tool now says which: not found (and whether the file exists on disk) vs. the list of validation errors.

## 2026-11-09 — D12: level00 sync check (ELIA)

- Obstacles reach the runner exactly as the click sounds ("the click happens just as the obstacle passes by"): sync is correct as built. It feels early because the obstacle visibly approaches before it is level with the runner. offsetMs experiments did not help (it is not latency). ELIA: keep A (click = obstacle level with runner); revisit at M1 review if pressing on the click feels wrong.
- level00.json had been deleted while deleting the broken LevelTest scene; restored. Laptop speakers.

## 2026-11-09 — D13: judge, scoring, run loop, calibration (playbook 1.4)

**Done**
- Rules: RunnerModel.ResolveWithAnswer (which gesture answered), Judge (Perfect 50 / Good 120 / OK / none / Miss), ScoreKeeper (100/50, x2/x3/x4 at 10/25/50), Calibration maths. Windows in GestureSettings.
- Decisions (flagged): OK grade for answers outside the window (0 points, streak reset); bars needing no action are ungraded; a gap's beat is the take-off (hole placed half a jump later).
- Runtime: Runner emits Judged per obstacle; Conductor: two-bar count-in, saved user offset, level set from code, StartNow; GameFlow (level by name, count-in pulses, flash + 60 ms hit-stop, instant restart by scene reload, results, local best); LatencyCalibration scene (F2 from the debug overlay); Boot loads Run.
- Shift+Shift > Set Up Run Scenes: fills Run, creates Calibrate, Boot → Run, build list Boot, Menu, Run, Calibrate, …
- Docs/SCORING.md.

**Tested**
- 80/80 core tests (18 new: grades per obstacle type, scoring, multiplier steps, calibration maths).
- Per-assembly compile check passes.

**Not yet run**
- Everything in Unity and in WebGL: count-in, restart time (< 1 s target), hit-stop, results, calibration, F2 in the browser.
- F1 is the browser's help key in Chrome; the debug overlay toggle may not work in WebGL (F2 should).

## 2026-11-09 — D13: fix compile error (Safe Mode)

- Unity opened in Safe Mode: `Resolution` (Core) was ambiguous with `UnityEngine.Resolution` in Runner.cs. Renamed ours to `ObstacleResolution`. The stand-in compile check did not know Unity's `Resolution`; it now includes common UnityEngine type names and reproduces the exact error (Runner.cs(200,17) CS0104) when the old name is put back.
- 80/80 tests, per-assembly check passes.

## 2026-11-09 — D13: first calibration runs (editor)

- Single tap on the last click: +111 ms. Tapping every click: only 4 of ~16 measured clicks counted (mean +7, spread 29).
- Hypotheses: (1) Windows Sticky Keys popup after 5 Shift presses (start + 4 practice taps = exactly 5) stealing focus; (2) taps beyond the ±240 ms cutoff; (3) Game view focus. Result screen now shows "counted X of Y taps" and the Console logs every tap's offset to tell them apart.

## 2026-11-09 — D13: first Web builds of the run loop (ELIA)

**Tested**
- Build And Run failed until the Web profile's Client Browser Type was set (it pointed at a non-default browser with no executable). Then ran in **Chrome and Firefox** (same localhost server).
- Both browsers: count-in, play, fail screen ("Shift to retry"), level complete with results panel.
- Firefox fail "bar b16 lane 0" was legitimate (tapped into the bar's lane at beat 13.3).
- Score 0 in completed runs is expected: level00's bars sit in the side lanes and the runner starts in the middle, so most need no action (ungraded). Grades only appear when dodging a bar in your own lane.

**Pending from ELIA**
- Calibration offset/spread/counted-of-taps for Editor, Chrome, Firefox (Sticky Keys shortcut checked off).
- Restart time after a fail (< 1 s target). Grades seen when dodging on the click.

## 2026-11-09 — D13: browser calibration results and M1 gesture decision

**Measured (WebGL, laptop speakers, builds without the counted-of-taps display)**

| Browser | Offset | Spread | Taps counted |
|---|---|---|---|
| Chrome | +74 ms | ±29 ms | 16 |
| Firefox | −27 ms | ±35 ms | 15 |
| Edge | −36 ms | ±63 ms | 8 (too few; redo) |

- Kill criterion 2 (beat sync off by > 60 ms in WebGL with no fix): **passes**. The offset is compensated by calibration; the jitter (spread ~±30 ms) is within budget.
- Chrome and Firefox differ by ~100 ms, so calibration has to be per browser. Flagged (not built): prompt calibration on first launch, since itch players will not find F2.
- **Decision (ELIA): keep all 7 gestures.** Recorded in SCOPE.md.

**Done**
- Assets/_Game/Levels/test_gestures.json: one of each gesture, 4 beats apart, 100 BPM, 40 beats (fits click_100). Validated against the level rules.
- Restart after a fail feels under a second (ELIA). Playbook 1.4 targets met: count-in, flash + hit-stop, instant restart, results, calibration; Chrome and Firefox tested.

## 2026-11-10 — D13: one answer per obstacle (phase exploit fix, all-lane low bars)

**Found (ELIA, test level):** holding both Shift keys passed the low bar (beat 16), the gate (20) and the wall (36). Cause: the playbook 1.2 rule "phased runners ignore Solid obstacles" made phase pass everything; it also broke 1.4's "exactly one correct gesture per obstacle".

**Changed**
- New ObstacleKind.Barrier (added last; kinds are saved as numbers). Only a Barrier is passed by phasing; low/gate/gap/wall/bar need their own answer, phased or not. Ghost-layer obstacles still only hit while phased.
- Chart "ghost" → Barrier. PhaseBarrier prefab kind set to Barrier (prefab file edited).
- Low bars span all lanes (ELIA): chart "low" is all-lane; LowBar prefab all-lane and full track width (prefab file edited).
- Wall: only a charge breaks it. A charge still lasts until the next wall (early charge = OK grade); unchanged unless ELIA asks.
- Docs RUNNER, LEVELS, SCORING updated.

**Tested**
- 86/86 core tests (new: phase does not pass low/gate/wall/gap/lane block; barrier without phase hits; low maps to all lanes). Per-assembly compile check passes.

**Not yet run** in Unity: prefab edits (kind 5 = Barrier, LowBar scale 7), test level replay.

## 2026-11-10 — D13: gap failed while the capsule looked airborne

- ELIA's run: jump key-down 14.113 s (half a beat early), recognised 14.348 s; gap far edge reached 14.800 s. Rules had the runner airborne 14.113–14.613 (from key-down), the screen 14.348–14.848 (arc starts on recognition), so the fail showed mid-air.
- Fix (same principle as day 11's lane rule): a jump is airborne from key-down until the visual landing (recognised + 0.5 s). Grading unchanged: that jump now passes as OK.
- 89/89 core tests (3 new, including ELIA's exact timings). Per-assembly compile check passes. Not yet run in Unity.
- Also from the screenshot: slide (Hold L) and sprint (Hold R) through beats 16 and 20 passed on this run.

## 2026-11-10 — D13: charge shortened to two taps (tuning)

- ELIA asked for an easier charge ("two presses of L R"); no preference among the options offered. Set GestureSettings Charge Taps 4 → 2 (setting only, no code): L, R on consecutive clicks charges; the second tap on the wall's click = Perfect. Side effect: dodging left then right on back-to-back clicks also charges (harmless; only walls use it). Revisit at M1 (2, 3 or 4).
- The plan's "four on-beat alternations" is now a tunable value, not a rule.

## 2026-11-10 — D13: charge redesign (ELIA: option A, 4 taps)

- ELIA: "L R L R doesn't seem to work". Alternatives offered (no-beat alternate, double jump, roll, double tap, mash); ELIA chose **A: quick alternating taps, no beat, 4 taps**. The 2-tap tuning from earlier today is reverted (Charge Taps back to 4).
- Classifier: alternate streak continues when sides alternate and key-downs are within Alternate Max Gap (400 ms, GestureSettings; replaces Alternate Beat Tolerance). Expires 400 ms after the last tap.
- Bug fixed: a charge had to complete strictly before the wall's moment, so a last tap aimed at the click failed about half the time. Now a charge completed up to the Good window (120 ms) after the wall still breaks it; the Runner waits that long while a streak is in progress.
- Project rules file, INPUT, RUNNER, LEVELS, SCORING updated.
- 92/92 core tests (alternate tests rewritten for gaps; late-charge tests; two "not a jump" tests updated because a quick L then R is now also alternate step 2). Per-assembly check passes. Not yet run in Unity.

## 2026-11-10 — D13: gate opening, wide bar, hurdle (ELIA)

**Decisions (ELIA)**
- Gate: one-lane opening whose position varies; you must change into it and sprint through.
- Short wall ("hurdle", **pink**) cleared by jumping; the tall **red** wall stays charge-only, so all 7 gestures keep a purpose.
- Two-lane obstacles: a new **wide bar** (one free lane) alongside the normal one-lane bars ("simple and complex"). Orange stays the colour for bars.

**Done**
- Rules: ObstacleInfo.LaneIsOpening (blocks every lane except Lane; in-between rule applies: either lane of a change in progress counts); new ObstacleKind.Hurdle (jump only). Wide bar answer = the move into its free lane.
- Chart types: `gate` now has an opening (`lane` = opening), new `wide` (`lane` = free lane) and `hurdle`. Hurdles are placed half a jump after their beat, like gaps.
- Prefabs (tool): GateOpening (yellow, Panel0-2), WideBar (orange, Panel0-2), Hurdle (pink); the spawner hides the open lane's panel. Shift+Shift > Set Up Run Scenes now refreshes the Run and LevelTest scenes' obstacle lists. Greybox scene untouched (old full-width gate still works).
- test_gestures.json: bar, wide (free right), low, gate (opening left), gap, ghost barrier, hurdle, wall.
- Docs LEVELS, RUNNER, SCORING.

**Tested**
- 102/102 core tests (10 new). Per-assembly compile check passes.

**Not yet run** in Unity: new prefabs, panel hiding, scene refresh, the updated test level.

## 2026-11-10 — D13: new obstacles verified in the editor (ELIA)

- After Shift+Shift > Set Up Run Scenes refreshed the obstacle lists, all 8 types appear and the test level completes ("works good"). The first attempt showed "No prefab for type 'wide'/'hurdle'" and "Gate has no Panel0" because the Run scene still had its old list (the refresh had not been run after the pull).
- **For the art pass (day 17, playbook 2.2/2.3):** the wide bar and the gate read as separate bars (one block per lane with gaps). Make the wide bar one solid piece and give the gate a frame with a clearly visible opening.

## 2026-11-10 — D16: chart editor (playbook 2.1)

**Done**
- Core.Chart: place (replaces overlaps; gate/wide keep the clicked lane as the opening; all-lane types stored in lane 1), move, remove, snap 1/4 or 1/8, type counts, length in seconds, checks (errors block save; warnings: first 8 beats, different types under 1 beat apart when the spacing toggle is on), JSON writer (2-space indent, sorted by beat then lane, invariant numbers, escaped strings; same format as the existing level files).
- ChartEditorWindow (Shift+Shift > Chart Editor): level dropdown, waveform strip, beat grid with bar numbers, three lane rows, click/drag/right-click editing, play/pause/stop/scrub/loop with the playhead taken from the audio's sample position, Save/Revert, Ctrl+S, Space, 1-8 type keys, problems panel.
- Docs/CHART_EDITOR.md (usage, checks, editor features relied on and Unity 6 risks).

**Tested**
- 116/116 core tests (14 new chart tests, including exact JSON output). Per-assembly compile check now includes the window (stand-ins extended with the editor UI types it uses).

**Not yet run** in Unity: the window, edit-mode audio playback (highest risk), waveform, save/reimport round trip.

## 2026-11-10 — D16: chart editor verified (ELIA)

**Tested (editor)**
- 116/116 tests pass in Unity. Window opens, level00 loads, obstacles drawn in lane rows, edit-mode audio playback works (the highest risk), waveform shows the click spikes exactly on the beat lines.
- Moved obstacles, saved, played: they appear where placed (spawned names like `bar b7 lane 0`).
- Playbook 2.1 question answered: **faster than editing JSON by hand; nothing clumsy.** No simplification needed.
- Day 16 "done when" met. Next per plan: 2.2 palette and post-processing (day 17), including the art-pass note on the wide bar and gate.

## 2026-11-10 — D16: palettes decided; visuals step 1 (playbook 2.2)

**Decisions (ELIA)** — see Docs/PALETTES.md: colour = what you press (six action colours identical on every level), Option 3 skies (pale dawn/overcast, dark others), charcoal road #333333 with light edge and lane lines. Four action colours sit at 3.0-4.3:1 on the road, below the plan's 4.5:1; agreed floor 3:1.

**Done**
- Palette ScriptableObject (warns in the Inspector if an action colour drops below 3:1 on the road); PaletteApplier (camera background, linear fog, recolours runtime copies of mapped materials so assets are never modified by playing; palette picked from the level JSON's "palette" field).
- Core ColorContrast (WCAG) + tests, including the chosen colours on the charcoal road.
- Shift+Shift > Set Up Visuals: six palette assets, edge/lane lines on Straight/Tunnel/Bridge, applier wired into Run, LevelTest and Greybox (Greybox shows "night"), tracks rebuilt, fog enabled in each scene's lighting so builds keep the fog shader variants.

**Tested**
- 120/120 core tests. Per-assembly compile check passes (stand-ins extended: RenderSettings, fog, camera clear, prefab contents).

**Not yet run** in Unity: the menu, the applier, fog, how it looks. Steps 2-5 (bloom/emissive edges, camera FOV/roll + trail, phase look, solid wide bar + gate frame) not started.

## 2026-11-11 — D17: first look at the palettes; camera lowered

**Seen (ELIA)**
- Run scene, dawn: pale sky, charcoal road, edge/lane lines; loop, grades and results work.
- The "black" run without Shift prompt, count-in or scores was the **Greybox** scene (night fallback palette, no conductor/game loop, by design), not the Run scene; the neon edit to test_gestures.json only shows in Run.
- Unity closed abnormally once (scene backup prompt). Cause unknown; kept backups. `/Assets/_Recovery/` is now git-ignored.

**Changed (ELIA: camera too high, the overhang hides the view)**
- The camera sat at 4 m, inside the tunnel ceiling slab (3.5-4.0 m). Lowered to 3.3 m (ChaseCamera default and the three scenes' serialized offset).
- To keep every obstacle below eye level (the palette decision relies on it), both gate prefabs lowered from 3.5-3.7 m to 3.2 m (prefab files and the tool). Tunnel ceiling now passes overhead.
- Per-assembly compile check passes. Not yet seen in Unity.
- Note: a jump raises the camera by half the jump height (0.75 m); inside a tunnel it would clip the ceiling. Avoid gaps/hurdles in tunnels.

## 2026-11-11 — D17: tunnel question, count-in, gap flicker (ELIA)

- "Tunnel gone": the Run scene's track has always been straight pieces only (tunnel exists in the Greybox track). Per-level track pieces come with playbook 2.3 (modular kit). Flagged, not built: automatic tunnel around low bars.
- Count-in read as "counting twice" (two bars of 1-2-3-4). Now: first bar "Get ready" with a pulse per beat, last bar 4-3-2-1, "GO" fading over beat 0.
- Gap "fidgety": z-fighting. The gap's black patch and the new lane lines were at the same height (0-0.02 m). Patch lifted to 0.02-0.04 m and pink (jump colour) rims added at its front and back edges. GapMarker prefab file edited for the height; rims are added by Shift+Shift > Set Up Visuals (re-runnable) and by the tool for new projects.
- Per-assembly compile check passes. Not yet seen in Unity.
- Gap rims and the new count-in confirmed in the editor (ELIA: "all good"). "Get ready" pulsed on each beat and read as appearing 4 times: now it stays steady with four dots lighting one per beat; 4-3-2-1 still pulse.

## 2026-11-11 — D17: glowing edges and bloom (playbook 2.2 step 2)

**Done**
- Six glow materials (LaneGlow … ChargeGlow), emission keyword on in the assets so Web builds keep the emission shader variant.
- Shift+Shift > Set Up Visuals now also: adds a thin glowing top-edge strip to every obstacle block (all obstacle prefabs; slightly wider than the block so no faces coincide), makes the gap rims glow, turns bloom on in PostProcessProfile (0.8 / threshold 0.9 / scatter 0.7, only if still 0), adds a global Volume and enables camera post-processing in Run, LevelTest and Greybox.
- Palette: Glow Intensity (default 1.5) per palette; PaletteApplier sets glow copies' emission = action colour x glow.

**Tested**
- Per-assembly compile check passes (stand-ins extended with Volume, Bloom, camera data, emission).

**Uncertain / not yet run**
- The look in Unity. The Web build uses the Mobile URP asset: if HDR is off there, glow is capped and bloom weaker than in the editor. Check in a Web build.
- Bloom on the light skies (dawn, overcast): threshold 0.9 should keep the pale sky from glowing; verify.

## 2026-11-11 — D17: camera feel and runner trail (playbook 2.2 step 3)

**Done**
- CameraFeel asset (Assets/_Game/Settings/CameraFeel.asset): offset (3.3 m kept), look-ahead, lag, FOV 60 / 72 sprint / 78 phase eased over 150 ms, 2 deg roll into lane changes (proportional to sideways speed, eased 120 ms). On an asset so Play-mode tuning is kept (playbook: visual settings on ScriptableObjects).
- ChaseCamera reads CameraFeel (its own fields moved into the asset; default instance + warning if unassigned).
- Runner trail: TrailRenderer (0.25 s, tapering) with a Sprites/Default material; PaletteApplier colours it accent -> transparent.
- Set Up Visuals now also creates CameraFeel and wires it plus the trail into Run, LevelTest and Greybox.

**Tested**: per-assembly compile check passes.

**Not yet run / uncertain**
- Step 2 (glow, bloom) and step 3 not yet seen in Unity.
- Sprites/Default for the trail under URP: expected to render (unlit, vertex colours); if it shows pink, switch the Trail material to URP Particles/Unlit.

## 2026-11-11 — D17: steps 2 and 3 verified (ELIA)
- Glowing edges, bloom, runner trail, FOV widening (sprint/phase) and lane-change roll seen in the editor: "looks good". No tuning changes requested; defaults kept (glow 1.5, bloom 0.8/0.9/0.7, FOV 60/72/78, roll 2 deg).
- Remaining in playbook 2.2: step 4 phase look (palette inversion + scanlines), step 5 solid wide bar + gate frame.

## 2026-11-11 — D17: steps 4 and 5 (phase look, obstacle shapes)

Done:
- Step 4: PaletteApplier now keeps each material copy with its role and blends every frame toward the phase look while the runner is phased (0.1 s, unscaled time): sky/fog to phase colour x phaseSkyDarken, Ghost role (new, added at the end of the enum) to near-white, other obstacle/glow roles x phaseDim, road/lines unchanged, IMGUI scanline overlay (1x4 repeating texture). New Palette fields: phaseSkyDarken 0.6, phaseDim 0.45, scanlineStrength 0.25. Set Up Visuals maps the Ghost material to the Ghost role and wires the applier's runner.
- Step 5: Set Up Visuals rebuilds WideBar and GateOpening prefabs (once; skipped if Open0 exists) into three layouts Open0/1/2. Wide bar: blocked lanes = solid 2 m slabs to the road edge. Gate: 3 m walls plus a framed doorway (posts 0.25 m, beam with top 3.1 m). Middle opening is necessarily two pieces. ObstacleSpawner activates Open{lane} and hides the others; falls back to the old Panel{lane} hiding for old prefabs. Gameplay rules unchanged.

Tested: per-assembly compile check against stand-ins (stand-in gap fixed: Texture now derives from Object); 120 Core tests pass.
Not run: nothing in Unity yet — the prefab rebuild, the edge strips on the new pieces, the phase blend, the scanlines and their draw order against the HUD are all unverified until ELIA runs Set Up Visuals and plays.
Uncertain: whether scanlines draw over or under the GameFlow text (GUI.depth across components); whether the near-white ghost colour reads well on the dusk/night skies.

## 2026-11-11 — D17: steps 4 and 5 verified

ELIA ran Set Up Visuals and played the Run scene: the phase look and the new wide bar and gate shapes work. Committed on his side. Still open: the scanline draw order against the HUD text and how the ghost colour reads on dusk/night (not tested; the test level is dawn).

## 2026-11-11 — D17: modular track kit (playbook 2.3)

ELIA chose the full kit (bends, rises, drops) over a cut-down straight kit.

Done:
- Core/TrackPath.cs: 12 pieces (straight, bendLeft/Right, sLeft/Right, rise, drop, crest, dip, tunnel, bridge, pillars), each 8 beats. The path is the track's centre line; distance along it is measured on the ground plan, so distance = beat x 5 m everywhere and timing and rules are untouched. Turns and slopes ease in and out.
- Level format: optional `track` list of {beat, piece}; validated (unknown piece, overlap, out of range). Chart keeps it on save and writes it one piece per line; new warnings (gap on a slope, more than 120 degrees one way).
- TrackBuilder rewritten: builds at play time (order -150) from the playing level; road and line meshes generated along the path in 40 m chunks; TunnelShell / BridgeRails / Pillars prefabs repeated every 10 m along their piece. Point() and Rotation() turn (distance, sideways, height) into world space.
- Runner, ChaseCamera, ObstacleSpawner use the track for positions; Obstacle carries its track position (hand-placed Greybox obstacles still use world X/Z on a straight track).
- Tools: Set Up Visuals removes the pieces saved in scenes, wires the builders, creates the dressing prefabs, generates click_100_long.wav, puts test_track.json first in the Run scene's level list. Greybox keeps a gap at 100 m as a GapMarker obstacle (its tunnel is now 60-100 m, bridge 105-145 m).

Tested: 133 Core tests pass (13 new: path geometry, ground distance equals s, flat joins, validation, chart save). Per-assembly compile check passes (stand-in gaps filled: Mesh, MeshFilter, LookRotation, SetPositionAndRotation and others). test_track.json validates and round-trips through the chart writer.
Not run: nothing in Unity. Unverified: the generated road mesh (facing, shading, seams between chunks), lines flicker, camera through bends and over crests, obstacles on slopes, tunnel clearance, restart time with the track built at start, WebGL.
Uncertain: obstacles on a rise may poke above the horizon (the 3.2 m rule assumed a flat road); the camera may see under the road on a crest; fog distance on bends.
Unused now, left in place: TrackPiece.cs and the Straight/Tunnel/Bridge/Gap prefabs (old scenes still reference the script until Set Up Visuals is run).
Flagged, not built: editing track pieces in the chart editor.

## 2026-11-11 — D17: track kit played by ELIA (Run scene, test_track, dusk)

Works: the level plays to the end on the bent and sloped track ("overall the game is good"); no underside of the road seen at the crest; the road and lines render.
Found:
- The blue phase barrier (beat 60, on the crest) reads as "the end of the game": a full-width opaque wall with nothing visible behind or beside it. ELIA's guess: no surroundings to give depth.
- Obstacles poke above the horizon on slopes (expected; the 3.2 m rule assumed a flat road).
Open, waiting for ELIA's choice: see-through barrier, roadside scenery (not in SCOPE.md as written), what to do about the horizon on slopes.

## 2026-11-11 — D17: scenery, see-through barrier, camera on slopes, brick wall

ELIA's choices after playing the track kit: roadside scenery and a see-through barrier ("like car racing games"), camera adjusts on slopes, red wall made of bricks. Scenery recorded in SCOPE.md decisions.

Done:
- Scenery (TrackBuilder): RoadsidePost every 10 m on both sides at 5 m (not in tunnels); BackdropBlock every 25 m on alternating sides, 14-30 m out, width and top height varied by a fixed hash (same every run). Both use the road-detail and line materials, so palettes colour them.
- Barrier: material set to URP transparent, 30% solid; PhaseBarrier prefab gets glowing side posts and sill. PaletteApplier keeps each material's own alpha when recolouring.
- Camera: lifts by slopeLift (0.6) x the height the road gains between the camera and 30 m ahead of the runner; off inside tunnels. Both values in CameraFeel.
- Wall: the tool generates Materials/Greybox/Brick.png (64x32, running bond) and puts it on the Wall material, tiled 7 x 8; the palette's charge red tints it.

Tested: compile check and 133 Core tests.
Not run in Unity: all four. Unverified: transparent material set from script (keywords, sorting against the glow frame and the trail), brick scale, scenery density and frame rate in WebGL, how the lift feels.
Note: Brick.png is created on ELIA's machine and committed from there (LFS does not work from the cloud session).

## 2026-11-11 — D17: scenery second pass (ELIA's feedback after playing)

ELIA: posts looked like floating lights; buildings repetitive; wants trees or shrubs at times; barrier a little less see-through; bricks only visible up close; camera a tad raised throughout, put it back.

Done:
- Ground: a 45 m band each side of the road, 5 cm below the surface, in the road-detail colour; left out under bridges. Posts, plants and buildings now stand on it. No posts on bridges.
- Buildings: six shapes (tower, slab, stepped, twin, spire, arch), picked per spot by a fixed hash, with varied distance (18-34 m), size, sink and quarter-turn. They and the plants stand upright on slopes (yaw only).
- Plants: two blocky trees and a shrub, at about 45% of the 12 m spots, 6.5-11 m from the centre line, random turn and size. New Foliage material, new PaletteApplier role Scenery (accent mixed 55% toward road-detail; unchanged while phased).
- Barrier 50% solid (was 30%). Bricks 2 m x 0.5 m (were 1 m x 0.19 m).
- Camera slope lift off by default (field renamed slopeLiftAmount so the saved 0.6 is dropped). So obstacles on rises poke above the horizon again; ELIA will say if it needs a small value.

Tested: compile check, 133 Core tests. Not run in Unity. Unverified: ground meeting the road edge, the scenery mix by eye, frame rate in WebGL with the extra objects.
Unused now: BackdropBlock prefab.

## 2026-11-11 — D17: scenery third pass

ELIA after playing: posts look planted; buildings varied but not realistic (asked if cosmetics come later); barrier to 65%; red wall "kinda jittery"; camera should lower slightly under the first bridge (read as: the tunnel, the first structure passed under; the pillars archway is fine).

Done: barrier 65% solid. Brick texture regenerated at 256 x 128 with mipmaps, trilinear and anisotropic 8 (guess at the cause: shimmer from thin high-contrast joints). Camera drops CameraFeel.tunnelDrop (0.5 m) while camera or runner is in a tunnel.
Tested: compile check only. Not run in Unity.
Uncertain: whether the jitter was texture shimmer at all (could be the edge strip against the wall face); whether "first bridge" meant the tunnel. In a tunnel the camera (2.8 m) is now below gate tops (3.1 m).
Flagged, not built: realistic buildings and trees would need imported 3D models (not in SCOPE.md; the plan's art direction is abstract shapes).

## 2026-11-11 — D17: scenery fourth pass

ELIA: the camera rises at the pink wall (hurdle, beat 44, just after the tunnel) and should not; buildings still look alike, make 10 more; more trees, conifers.

Done:
- Camera: jump-follow is now a CameraFeel setting, 0 by default (was a fixed 0.5), so the camera stays level on every jump. The tunnel drop eases in over 12 m before a tunnel and out over 12 m after it (it used to snap back at the exit, about 13 m before the hurdle). Not known which of the two ELIA saw; both changed.
- Buildings: 10 more shapes (L, pyramid, needle, cluster, frame, T, wall, stairs, crown, linked towers): 16 in total.
- Plants: four conifers (tiers narrowing to a point, alternate tiers turned 45 degrees); conifers are 6 of the 10 entries the builder picks from. A plant spot every 7 m with a 70% chance (was every 12 m, 45%).

Tested: compile check only. Not run in Unity.
Uncertain: wide buildings at the near distance can reach the plant band (6.5-11 m), so a tree may stand inside a building now and then; frame rate with roughly twice the plants.

## 2026-11-11 — D17: plots and compound walls

ELIA: fourth pass "looks good". Asked for trees beside the buildings or in empty spaces, so it reads as plots (building + trees, or trees only), and a compound wall to tell plots apart.

Done (TrackBuilder scenery placement rewritten; posts unchanged):
- Both sides of the road are divided into plots 30 m long with a 4 m lane between them, from 7.5 m to 40 m out. None where a plot would touch a bridge.
- 55% of plots get one building (same 16 shapes, slightly smaller: 0.75-1.15) set back from the front wall, plus 2-5 plants placed only where the building is not. The rest get 5-9 plants and no building.
- Compound wall per plot: 1.1 m high, generated as a mesh. Front wall (with a 4 m gateway in the middle) and back wall follow the road through bends and slopes; the two end walls run straight out. Road-grey material, one shade lighter than the ground. Faces drawn from both sides.
- The loose band of trees between posts and buildings is gone (all plants are inside plots now).

Tested: compile check, 133 Core tests. Not run in Unity.
Uncertain: wall colour against the ground (#333 on #2A2A2A may be too close); walls on bends; object count (about 20 buildings and 200 plants on the test level) in WebGL.

## 2026-11-11 — D17: plot walls second pass

ELIA: plots look good. Walls only along the road, not around the plot; lit top edge; closed gates instead of openings; no marker posts in front of gates.

Done (TrackBuilder): back and end walls removed. Each plot's road-side wall has a lit strip on top (line material), a closed gate in the middle (4 m, thinner, ground-dark, lit bar on top) between two gate posts 0.5 m taller than the wall, and closed ends. Marker posts are skipped within 3.5 m of a gate's centre.
Tested: compile check only. Not run in Unity.
Uncertain: the lit wall edges run parallel to the road's edge lines 4 m away; they may compete with the lane lines for attention. Both use the same line material, so shared-face seams between wall and end faces rely on RecalculateNormals (possible soft shading at wall ends).

## 2026-11-11 — D17: white screen fixed

ELIA: after the wall change the Game view was white with a few black marks. Nothing lost: his commit holds the code as pushed.
Cause (from reading the code, not reproduced here): the wall end faces added in the last change shared four corners between a front-facing and a back-facing pair of triangles. RecalculateNormals averages the two opposite directions to zero; the lit shader normalises that to an invalid value (NaN); bloom spreads one invalid pixel over the whole screen.
Fix: each side of an end face has its own four corners. The Strip() calls were already safe (every strip adds its own vertices).
Tested: compile check only. Not run in Unity.
Lesson for the mesh code: never let one vertex belong to triangles facing opposite ways.

## 2026-11-11 — D17: walls, gates and white-screen fix verified in the editor

ELIA: "now it looks good" (Run scene, test_track): walls along the road with lit edges, closed gates with taller posts, plots with buildings and trees; the white screen is gone. Next: browser Build And Run to check frame rate, glow and the see-through barrier in WebGL (not done yet).

## 2026-11-11 — D17: first browser build with scenery; gap pit, gate fix, running figure

ELIA ran Build And Run (Chrome, development build): the track test level plays in the browser with scenery, bricks and the tunnel. No frame-rate complaint; build size not reported yet. Feedback:
- Screen "shakes" on quick left-right: that is the step-3 camera roll (2 degrees) and sideways lag; explained, not changed (CameraFeel > Roll Degrees).
- Gap needs depth, maybe water. Gate ("green wall") looks jittery. Wants a running character instead of the capsule, with white smoke from the heels.

Done:
- Gap: TrackBuilder cuts the road (surface, sides, underside, lines) at each gap, with cross-sections exactly at the edges and closed slab ends; the cut matches the spawner through one shared JumpShiftMetres(). GapMarker prefab gets a "Pit" (four dark walls 3 m deep, water surface 1.2 m down, fixed dark blue-green "Water" material). The spawner hides the flat black "Hole" where the road is cut and hides the pit on a bridge (open air). Greybox (no level, no cut) still shows the flat hole.
- Gate: layout v2 (prefabs rebuilt once, marker child "LayoutV2"): walls 2.8 m, posts 3.0 m, beam top 3.15 m, each deeper than the last, so no faces or glow strips share a plane. Guess at the cause of the jitter (coplanar glow strips), not confirmed.
- Runner: RunnerFigure, a figure of boxes (legs, arms on pivots, torso, head) built by the tool. Stride from distance along the track: one stride per beat, feet land with the music. Knees tuck in a jump, upper body leans more in a sprint, all parts take the phase material. Dust: a ParticleSystem set up by the tool, a puff of white squares per footfall. The capsule stays in the scene, switched off; Runner.body points at the figure (slide still squashes it).

Tested: compile check (stand-ins added for ParticleSystem and its modules), 133 Core tests. Not run in Unity.
Uncertain: the cut road and pit by eye, especially on slopes and where a gap crosses a 40 m chunk boundary; the particle setup from script (module properties) and its look in WebGL; limb swing directions; how the squashed figure reads as a slide.
Scope note: the figure and dust were asked for by ELIA; particles were already on the day-27 juice list.

## 2026-11-11 — D17: stuck Shift key in the browser, bend lean, trail off

ELIA (Chrome build): figure and gate look good. Asked if the yellow trail is needed; thought he pressed on time at the gap (beat 68) but failed; wants the runner to lean into bends; sometimes left taps do nothing, "especially in the open stretch".

Finding (from his screenshots, not reproduced here): at the gap failure the debug log's newest entry is the phase hold ending at beat 61: no gesture at all was registered near beat 68. An earlier screenshot shows "R-Shift: DOWN" at beat 54.9 with no gesture logged since the jump at beat 43.8, i.e. a key shown as held for 6 s after a both-press. Reading: on Windows, when both Shift keys are down and one is released, the browser gets no key-up for it; Unity's keyboard state keeps that key down, so its next press has no down edge and is lost. This follows every jump and phase, which matches both complaints. The editor reads the keyboard differently and was not affected.

Done:
- Assets/Plugins/WebGL/ShiftKeys.jslib + Runtime/ShiftKeys.cs: in Web builds ShiftInput reads the two Shift keys from the browser's own key events. On any Shift key-up with no Shift left down, both are cleared. A fresh key-down for a key still marked down counts as release + press. Presses are latched between frames (a tap shorter than a frame still counts). Editor and Windows builds keep the Input System path.
- RunnerFigure leans into bends: 3 degrees of lean per degree the track turns over 6 m, at most 16.
- Runner trail switched off by the tool (object kept).

Tested: compile check for both variants (normal and with UNITY_WEBGL defined); the key script simulated in Node for the lost key-up, re-press, quick tap and blur cases; 133 Core tests. Not run in Unity or a browser.
Uncertain: whether the stuck key is the whole cause; the .jslib meta is minimal (Unity should fill in the importer and include it for Web only). Conductor start, GameFlow restart and the calibration scene still read the Input System directly; a stuck key there only costs one press.
Remaining limit: releasing one Shift while the other stays held is not seen until both are up or that key is pressed again (e.g. going from phase straight into a slide).

## 2026-11-11 — D17: both-press lost in the Web build (my regression), lean reduced

ELIA (Chrome): pressing both Shifts did not jump, three tries. His screenshot at the hurdle (beat 44) logs only "TapRight": the left key's press was never registered. Before the browser key reader, the same spot logged BothTap, so the reader introduced this.
Likely cause (not confirmed on his machine): the reader skipped key-down events flagged "repeat"; with one Shift already down, Windows flags the first press of the other Shift as a repeat. Fix: a repeat is ignored only when that same key is already marked down.
Also: bend lean reduced to 1 degree per degree of turn, at most 5 (was 3 and 16): ELIA found the big lean read like a lane change. Fields renamed so the saved values are dropped.
Added tools/shift-key-test.html: a page that lists the browser's raw Shift key events (code, repeat, shiftKey) for four short tests, to replace guesses about the Windows behaviour with facts.
Tested: compile check; the key script simulated in Node (second Shift flagged repeat counts, true auto-repeat ignored). Not run in a browser.
Uncertain: everything about what Windows and Chrome really send for two Shift keys until ELIA's test page results are in. In particular, tapping one Shift while holding the other (lane change during a slide) may leave the tapped key marked down.

## 2026-11-11 — D17: Web input verified; what Chrome on Windows really reports for two Shift keys

ELIA (Chrome build): "works well now"; test_track completed in the browser with jumps, phase and taps registering.

Facts from tools/shift-key-test.html on his machine (Chrome, Windows):
1. Both together: keydown ShiftRight (repeat false), keydown ShiftLeft 4 ms later with repeat TRUE, then auto-repeat of ShiftLeft, then ONE keyup (ShiftRight, shiftKey false). No keyup for ShiftLeft.
2. Hold Left, tap Right, release Left: keydown ShiftLeft, its auto-repeats, keydown ShiftRight (repeat true), then nothing until ONE keyup (ShiftLeft, shiftKey false) when Left is released. The Right tap's release is never reported.
4. Hold both, release Left, wait, release Right: no event at Left's release; ONE keyup (ShiftRight, shiftKey false) at the end.
(Screenshot 3 was a copy of 2.)

So: the second Shift's key-down is always flagged repeat (confirms the last fix); only the LAST Shift released produces a key-up; the first release is silent. Auto-repeat moves to the most recently pressed key.

Consequence still open: tapping one Shift while holding the other (e.g. changing lane during a slide) cannot be seen as a tap in the browser: the tapped key stays marked down until the held key is released, so it is read as a hold (and both down). Proposed to ELIA, not built: in Web builds, a second Shift pressed more than the both-window (80 ms) after the first counts as a tap straight away. Windows desktop builds are not affected.

## 2026-11-11 — D17: Web tap-during-hold workaround

ELIA agreed to the workaround. Browser felt smooth with all scenery (his words); build size still not known. The red Console error (UnityConnectWebRequestException: Token Exchange failed) is the editor's own Unity account service, not project code.
Done: in the Web path of ShiftInput, a Shift pressed while the other has been held longer than bothWindowMs is fed to the classifier as press + release at once (a tap) and ignored until the browser shows it up. Documented in Docs/INPUT.md with the limits.
Tested: compile check for both variants; new Core test (zero-length tap during a hold fires the tap, no both gesture, the hold goes on); 134 Core tests pass. Not run in a browser.

## 2026-11-11 — D17: Web tap-during-hold verified; art block closed; level 1 drafted

ELIA (Chrome build): "everything works" (tap during a hold, jump, phase); asked to move on. Days 16-17 block done: palettes, glow, camera feel, phase look, shapes, track kit, scenery, running figure, Web input.
Still not known: the Web build's size (limit 20 MB compressed). Not done: Edge calibration redo; Release Web profile build.

Level 1 "Warm-up" drafted a day early (plan: day 18): Assets/_Game/Levels/level01.json, see Docs/LEVELS.md. Lane changes only, quarter notes, first 8 bars one tap each, 2-bar breather before the fastest section (plan's authoring rules). Set Up Visuals now orders the Run scene's levels: level01, test_track, test_gestures, level00.
Tested: level validates, strict chart check has no warnings, a scripted walk-through needs at most two taps per obstacle with at least one beat per tap, file round-trips through the chart writer. Compile check passes.
Not run in Unity. Whether it is fun, readable and the right difficulty is ELIA's call after playing (rule of thumb: clear within 5 attempts).

## 2026-11-11 — D17: level 1 played; level 2 drafted

ELIA: cleared level 1 on the first attempt; visuals "look good, realistic with lighting". Asked if level 1 should have only one type of obstacle: yes by the plan (level 1 = Tap L and Tap R only; bar and wide are both lane changes, same orange).
Build folder size (development Web build): 59.4 MB uncompressed on disk. Not comparable with the 20 MB compressed limit: needs a Release profile build to measure. Flagged as a risk to check before day 24, not day 28.
Noticed, not changed: his clean run scored 50 with best streak 1 and 0% perfect. By design a dodge made away from the obstacle's beat grades "Ok" (no points, streak reset); only moves within 120 ms of the beat score. On a lane-change level that makes a clean run look like a bad one. This is the open M1 question "when should the click land"; needs ELIA's decision.

Level 2 "Low ceiling" drafted: Assets/_Game/Levels/level02.json (Docs/LEVELS.md). Set Up Visuals generates click_110.wav and puts level02 first in the Run scene's list.
Tested: validates, strict chart check clean, scripted walk (gate openings always in the player's lane, holds at least 4 beats apart, nothing under 2 beats), round-trips through the chart writer. Compile check passes. Not played.

## 2026-11-11 — D17: scoring: every obstacle passed scores

ELIA's decision: points for every obstacle passed, Ok 5, Good 20, Perfect 50, the on-beat grades being the bonus.
Done: ScoreKeeper values 50 / 20 / 5 (were 100 / 50 / 0), all times the streak multiplier. An OK no longer resets the streak; it neither adds to it nor breaks it (my choice, so the streak and multiplier stay a reward for on-beat play; one line to change if ELIA wants passes to build the streak too). Best-score keys moved to ShiftShift.BestV2.* so bests under the old values are not compared with new ones. Docs/SCORING.md updated.
Tested: 136 Core tests pass (3 scoring tests rewritten, 2 added: a clean off-beat run of 27 scores 135; an OK uses the multiplier). Compile check passes. Not run in Unity.

## 2026-11-11 — D17: camera lowered to 2.8 m

ELIA (level 2, editor): the gate's top beam (2.95-3.15 m) was level with the camera (3.3 m), looked like it hit the camera and blocked the view of the runner. Asked for the camera to come down so the beam passes overhead. His screenshot also shows the new scoring counting up (115 points at beat 41).
Done: CameraFeel offset y 3.3 -> 2.8 (asset and code default); tunnelDrop default 0.5 -> 0 (2.8 m is the height he already approved inside the tunnel). Docs/PALETTES.md notes that the day-16 rule "every obstacle top below the camera" no longer holds for the 3 m obstacles.
Tested: compile check only. Not run in Unity.

## 2026-11-11 — D17: camera closer after the height change

ELIA (level 2, editor, camera at 2.8 m): the gate beam now passes overhead, but the runner "looks a little far ahead", so judging the distance to an obstacle is tricky. (The earlier ArgumentNullException "clock" was scripts reloading during Play mode after a pull, not a game bug; advised Recompile After Finished Playing.)
Done: CameraFeel offset z -7 -> -5.5 (closer behind the runner) and lookAhead 8 -> 6 (aims nearer, so the view tilts down about 3 degrees more and shows more road around the runner). Asset and code defaults.
Tested: compile check only. These are first guesses by geometry, not by eye; ELIA can tune Offset Z and Look Ahead in the CameraFeel asset while playing.

## 2026-11-11 — D17: camera settled; level 3 drafted; release build requested

ELIA: "camera feels right now" (2.8 m up, 5.5 m behind, aiming 6 m ahead). Asked for level 3 or the build; doing both: he runs Shift+Shift > Build WebGL (release, Brotli, prints the size against the 20 MB budget) while level 3 is drafted. Level 2 attempts not reported yet.
Level 3 "Gaps" drafted: Assets/_Game/Levels/level03.json (Docs/LEVELS.md). Set Up Visuals generates click_120.wav and puts level03 first in the Run scene's list.
Tested: validates, strict chart check clean (no gap-on-slope warning), scripted walk (jumps at least 4 beats apart, nothing under 2 beats, two-tap moves have 4 beats), round-trips through the chart writer. Compile check passes. Not played.

## 2026-11-11 — D17: release Web build size measured

ELIA ran Shift+Shift > Build WebGL (release, Brotli + decompression fallback): "[Build] WebGL OK in 03:54 -> Builds/WebGL, 9.6 MB on disk (plan budget: under 20 MB)". About 10 MB of headroom. Contents at the time: levels 1-2 with placeholder clicks, all scenery, no music yet. The earlier 59.4 MB was the development build, uncompressed.
Budget note for day 23: six music tracks have to fit in roughly 10 MB together (about 1.5 MB each after Unity's compression), or fewer tracks get reused (cut order item 4).
Not yet done by ELIA: Set Up Visuals + play for level 3 (his pull shows the level arrived; nothing to commit yet).

## 2026-11-11 — D17: levels 2 and 3 played; ghost-layer chart types; level 4 drafted

ELIA: level 2 cleared on attempt 4, level 3 on attempt 4 (plan's limit: 5). Left as they are; tester feedback on days 25-26 decides.

The plan's level 4 needs "two overlapping obstacle layers"; the chart had no way to place a ghost-layer block. Added two chart types (ObstacleTypes.Cores): ghostbar (barrier + core in the event's lane) and ghostwide (barrier + cores in the other two lanes). They fill their beat in the chart like gate and wide; the spawner places the barrier and one GhostBlock per core, each judged on its own; the chart editor draws core lanes lighter. Ghost-layer blocks are now a lighter tint of the phase colour (were identical to the barrier), so cores can be seen before phasing. Set Up Visuals now also refreshes the spawner's type list.
Level 4 "Ghost layer" drafted: Assets/_Game/Levels/level04.json (Docs/LEVELS.md), click_125 generated by the tool, level04 first in the Run scene's list.

Tested: 137 Core tests (new: type specs, chart placement of a barrier with cores). Level validates, strict check clean, scripted walk over every lane the player could be in (gate lanes certain, at most two taps, a lane change before a phase always has 4 beats), round-trips. Compile check passes. Not played.
Uncertain: how a light core reads inside the 65%-solid barrier, in the night palette, before and during phase; each core scoring as its own obstacle (extra points per barrier); difficulty.

## 2026-11-11 — D17: "ghost block", light pink

ELIA asked what "cores" were; chose the name "ghost block" and light pink for them.
Done: renamed in code (ObstacleTypes.GhostBlocks, spawner field ghostBlockPrefab), comments, chart editor and Docs/LEVELS.md. Ghost blocks are light pink (#FFB5D9) on every level, not palette-driven; brighter (half-way to white) while phased; the chart editor draws their lanes in the same pink.
Flagged to ELIA: pink is the jump colour (hurdle, gap rims) under the "colour = what you press" rule; the ghost block is a paler pink inside a blue wall. To watch when playing.
Tested: compile check, 137 Core tests. Not run in Unity. The spawner's prefab field was renamed, so Set Up Visuals must be run before level 4 plays.

## 2026-11-11 — D17: level 4 played; level 5 drafted

ELIA: level 4 cleared on attempt 3 (ghost blocks light pink). Asked for level 5.
Level 5 "Drumline" drafted: Assets/_Game/Levels/level05.json (Docs/LEVELS.md), click_135 generated by the tool, level05 first in the Run scene's list.
Tested: validates, strict check clean, scripted walk (3 free beats before each wall, lane unknown after a wall, gates only with a certain lane, at most two taps, lane change + phase gets 4 beats), round-trips. Compile check passes. Not played.
Uncertain: whether four taps before every wall at 135 BPM is tiring over 92 s (21 walls); the open M1 question that charge taps move the runner sideways is now felt in a real level.
Schedule: five of six levels exist as first drafts on day 17 (plan: levels 1-4 by day 20, 5-6 on days 21-22). All use placeholder clicks.

## 2026-11-11 — D17: level 5 played; tempo and palette changes; level 6 drafted

ELIA: level 5 cleared on attempt 3. Asked for level 6.
Level 6 needs a tempo rise and a palette cycle (plan). Done:
- Core/TempoMap.cs: beats <-> seconds with tempo changes; LevelData gets optional `tempo` and `paletteChanges` lists (validated). With no changes the maths is the old beat x 60 / bpm.
- Conductor converts through the tempo map (BeatAt, SongTimeAtBeat, new BpmAtBeat); the count-in uses the starting tempo. Spawner and TrackBuilder place gaps and hurdles using the tempo at their own beat.
- ClickTrack can follow a tempo map; the tool generates click_130_150.wav for level 6.
- PaletteApplier cuts to the listed palette on its beat (sky, fog, trail, scenery tint).
- Chart keeps and writes both lists; the chart editor converts time through the tempo map.
- Level 6 "Full shift": Assets/_Game/Levels/level06.json (Docs/LEVELS.md); first in the Run scene's list.

Tested: 145 Core tests (8 new: tempo map continuity and round trip, click length, chart save, validation). Level validates, strict check clean, scripted walk with all earlier rules, round-trips; length 89.1 s. Compile check passes. Not played.
Uncertain (all need ELIA's eyes and ears): that the click and the obstacles stay together across the tempo rise at beat 96 in the editor and in WebGL; the feel of the speed-up (the runner goes from 10.8 to 12.5 m/s); hard palette cuts on the beat; a gap's hole position right after the rise.
Schedule: all six levels exist as first drafts on day 17, on placeholder clicks.

## 2026-11-12 — D18: menu, level select, results choices, pause

ELIA: level 6 cleared on attempt 3 (so the tempo rise and palette cuts work in the editor). Asked for the menu.
Done (Docs/MENU.md):
- Core/ShiftMenuKeys: Left / Right / Both from the two Shift keys, engine-free, 5 tests.
- MainMenu (Menu scene): levels 1-6 with length and best score, Calibrate timing, Credits; Sticky Keys tip. Left/Right choose, Both opens.
- GameFlow: static Play(level) used by the menu (starts at once); results: Left retry, Right next level, Both menu; fail: any Shift retries as before, Esc menu; Esc pauses during count-in and play (Left/Esc resume, Right restart, Both menu). Keys now come through ShiftInput, not the keyboard directly, so the Web key fix applies to retry too.
- Conductor.Pause/Resume: freezes SongTime, pauses audio, shifts the song start by the paused time.
- DebugOverlay: gesture log and runner box are off by default in builds, on in the editor; F1 toggles.
- Boot opens Menu; Calibrate returns to Menu; Set Up Visuals fills the Menu scene, gives GameFlow its input, orders the Run scene's levels 1-6, and puts Boot, Menu, Run, Calibrate first in the build list.

Tested: 150 Core tests; compile check (stand-in gaps filled: TextArea, AudioSource.Pause/UnPause, Application.isEditor, GUIStyle.wordWrap/fontStyle). Not run in Unity.
Uncertain: audio and beat staying together after a pause (UnPause is not sample-exact; may drift a few ms per pause), pause during the count-in, Esc in a browser (leaves fullscreen first), menu text sizes at small window sizes, a key held across a scene change.
Still placeholder: all UI is IMGUI text; no UI sounds until the audio pass.

## 2026-11-13 — D19: commit attribution removed

ELIA asked for the co-author attribution to be removed from the commit history and for it not to recur.
Done: the 73 "Co-Authored-By" lines were stripped from the commit messages on main (history rewritten; every commit has a new hash). Trees, authors, committers, dates and the rest of each message are unchanged, checked commit by commit. The project settings file now sets attribution.commit and attribution.pr to "", so no attribution is added to commits or pull requests.
Also removed, at ELIA's request: the session link line the same 73 commit messages ended with; the settings file sets attribution.sessionUrl to false so it is not added again. Signing: ELIA re-signs the commits on their own machine with their own key, so GitHub shows them as verified under their account.
After the forced push, ELIA's clone must be reset to the new history (not pulled), or the old commits come back as a merge.

## 2026-11-13 — D19: licences

ELIA re-signed the whole history with their own SSH signing key and force-pushed (110 commits, all signed; this session's copy reset to it).
Dual licensing at ELIA's request: LICENSE (MIT, source code only, note added at the top), new LICENSE-ASSETS (All Rights Reserved for all original non-code content), README "License" section, and the click-track row in Docs/CREDITS.md changed from "MIT, as code" to All Rights Reserved so it does not contradict the new notice.
Decided by ELIA: the copyright holder in both files is "ELIA" (git config user.name returns "ELIA"); level designs, scenes, prefabs, materials and palettes are assets (All Rights Reserved); the Docs/ folder is also under LICENSE-ASSETS.
Signing: commits made from the cloud session are unsigned from here on (repo-local commit.gpgsign false there), so GitHub shows no badge on them. ELIA batch-signs everything on their PC with their own SSH key right before the final Game Off submission (added to the submission checklist in Docs/PLAN.md).
How to batch-sign (tested on 13 Nov; it never touches the working files): close Unity, `git fetch origin`, `git reset --hard origin/main`, then `git filter-branch -f --commit-filter 'git commit-tree -S $@' -- main`, check `git cat-file commit HEAD | Select-String gpgsig`, then `git push --force-with-lease origin main`. Do not use `git rebase -S --root` for this: it stops at the commit that moved the PNGs into LFS.

## 2026-11-13 — D19: names in credits and docs; README charge row

ELIA's instructions: the credits name is ELIA, and no other personal or tool name is to appear anywhere in the project's files.
Done: the earlier first name replaced by ELIA in 32 files (docs, code comments, test comments: 166 places); the tool name removed from the plan, credits, log and code comments (references to the rules file now read "project rules"). In-game credits: "Design, code, levels and testing: ELIA". Docs/CREDITS.md tools list: "Design, code and levels by ELIA". README controls: charge is "Alternate L, R, L, R quickly" (no beat needed since day 13).
Tested: compile check, 150 Core tests (only comments, docs and one credits string changed).
Left as they are, for ELIA to decide: the names of the rules file and the settings folder at the repository root (the tool only finds them under those names; cloud sessions clone the repository fresh, so they must stay in it to work); the author name and three commit subjects in the existing commit history (changing them means another history rewrite, which can be folded into the final batch-signing).
Note: the Menu scene already in the repository has no credits text yet; it is filled in when Set Up Visuals first creates the Menu object.

## 2026-11-13 — D19: final history step prepared

ELIA's decisions: the name on commits is ELIA (git name set on the PC and in the cloud session); the rules file and the settings folder at the repository root stay, so cloud sessions keep working.
Added tools/sign-history.sh for the last step before submission. It sets the author and committer name of every commit to the current git name, rewords the three old commit subjects that contain a name, and signs every commit with the owner's key. It rewrites commit records only and never touches the working files (the earlier `git rebase -S --root` attempt stopped at the commit that moved the PNGs into LFS).
Rehearsed here on a throwaway branch: 112 commits, 112 signed, one name, files, emails and dates identical, three subjects changed, no name left in any message.
Final step, on the PC, with Unity closed (this replaces the filter-branch line in the earlier entry):
1. `git fetch origin` then `git reset --hard origin/main` (or `git pull --no-edit` if nothing was rewritten since the last pull)
2. `git -c alias.signhistory='!sh tools/sign-history.sh' signhistory`
3. Check the printed totals, then `git push --force-with-lease origin main`
4. Tell the cloud session, so its copy is reset before any further commit.
Until then: commits already on GitHub keep the old name and their Verified badges; new commits carry the name ELIA and, when made from the cloud session, no signature.

## 2026-11-16 — D20: charge: last tap counts from its key press; charge counter

ELIA (level 5, editor) failed on the wall at beat 56 and asked if a second wall was behind it. No: the on-screen log shows only three alternating taps (L 54.24, R 54.86, L 55.48); a charge needs four. While checking, found that a fourth tap pressed in time but released after the grace window would also fail (taps are recognised on key-up). ELIA asked for both fixes.
Done:
- RunnerModel.WallMayStillBreak: wait while a charge is under way and the window is open, or while a key pressed within the window is still undecided; stop waiting once a charge counting up to the end of the window exists. Runner's wall wait uses it.
- Runner.HasCharge (public). GameFlow draws a charge counter above the runner during play: one dot per alternating tap towards the next charge, "CHARGED" when ready.
Tested: 152 Core tests (2 new: last tap pressed in the window and released after it breaks the wall; wait rules). Compile check passes.
Not run in Unity: the counter's position and size on screen, and how the wall feels with the longer wait. Docs/RUNNER.md updated.

## 2026-11-16 — D20: runner stops in front of the obstacle on a fail

ELIA did not want the runner to stop past a wall (side effect of waiting for a late key press before judging).
Done: FailStop.Distance (Core, tested) gives the stopping point: the obstacle's front face minus 0.35 m, never ahead of where the runner already is; a gap stops at the failing edge. Runner.Fail uses it and places the runner at once (Update stops after a fail, so before this the runner simply stayed wherever the last frame left it). Applies to every obstacle kind, not only walls.
Tested: 153 Core tests (1 new), compile check. Not run in Unity.
Uncertain: when the wait was long, the runner (and camera) jump back up to about 3 m at the fail; the white flash and hit-stop should hide it, but it needs a look.

## 2026-11-16 — D20: a charge is good for the next obstacle only; counter only before a red wall

ELIA (level 1, which has no walls) saw "CHARGED" stay on screen: a charge earned earlier from quick alternating lane changes was kept until a wall used it (the day-13 rule). Asked: a charge must not carry past an obstacle, and the counter should show only when the next obstacle is a red wall.
Done: RunnerModel.ResolveWithAnswer uses up every charge earned at or before the obstacle being judged, wall or not; a charge completed after it (in a wall's grace window) is kept for the next one. So charges no longer bank up (eight taps = one wall, not two). Runner.NextObstacleKind; GameFlow draws the counter only when it is Wall. Docs/RUNNER.md rule 2 updated.
Tested: 155 Core tests (2 new; the old "eight taps break two walls" test rewritten for the new rule). Compile check passes. Not run in Unity.
Checked against the levels: every wall has at least 3 free beats before it, so no level relied on carrying a charge past an obstacle. Level 5's back-to-back walls (176, 180) each need their own four taps.

## 2026-11-16 — D20: charged look on the runner

ELIA asked for a glow or flame on the runner instead of the "CHARGED" text.
Done: RunnerFigure switches the figure to a new emissive material (Materials/Greybox/RunnerCharged, wall red #F21E3D, emission x2.2 so bloom picks it up) and plays a flame particle system around the torso while a charge is ready and the next obstacle is a red wall. Phase look still wins over charged. Flames are left behind in the world as the runner moves, so they stream back like a trail; they burn out when the charge is used. GameFlow keeps the tap dots (1-3) and no longer draws "CHARGED". Set Up Visuals adds the flame and material to existing figures (WireChargeLook).
Tested: compile check, 155 Core tests (no rule changed). Not run in Unity: how it looks, whether the flames read as fire in WebGL (Sprites/Default, alpha-blended), and frame rate with up to 300 extra particles.

## 2026-11-16 — D20: flame in three colours

ELIA: the flame should not be all red: yellow inside, then orange, then red.
Done (setup tool only): the flame is rebuilt as three particle layers: a tight yellow core (#FFF6B0 to #FFD23F), an orange middle (#FFA41E to #FF6A1E) and a wider red outside (#FF5A2A to #F21E3D), drawn core on top (sorting fudge). The charged body glow is now warm yellow-orange (#FFB547, emission #FF9A1F x1.8) instead of red. The old single-layer flame is replaced on the next Set Up Visuals; the material's colours are reset on every run.
Tested: compile check only (no runtime code changed). Not seen in Unity: whether the layers read as one fire, and the particle count (up to 450) in WebGL.

## 2026-11-16 — D20: the Run scene opens the menu when started directly

ELIA: no menu on the first screen; the level choice only appeared after clearing level 1. Cause: ELIA presses Play in the Run scene, which starts level 1 by itself; the menu is reached through Boot (in a build the list starts Boot, Menu, Run, so a build opens on the menu).
Done: GameFlow loads the Menu scene when the Run scene is opened directly (no level chosen, no auto-start) and the Menu scene is in the build list. Tick box "Open Menu When Started Here" (on by default) to switch it off for quick tests. Docs/MENU.md updated.
Tested: compile check only. Not run in Unity.
Noticed: the Menu scene in the repository is still the empty one (its menu is added by Set Up Visuals on ELIA's PC); it reaches GitHub with ELIA's next commit.

## 2026-11-16 — D20: menu tagline, How to play, level status, fewer flames

ELIA tested the menu (works: Left/Right choose, Both opens) and the charged flame. Asked: tagline "A Rhythm Runner Played With Just Two Keys"; where is How to play; a level played but not cleared should say "Failed - Try Again"; the flames hid the runner.
Done:
- MainMenu: new tagline; "How to play" entry and page (six moves with their obstacle colours, scoring line, Esc); status per level from PlayerPrefs.
- GameFlow: records Played (on fail and finish) and Cleared (on finish) per level.
- Flame rebuilt by Set Up Visuals on every run: three layers at 12 flames per second each (were 55-60), smaller (0.12-0.2), simulated in local space so they stay on the body instead of streaming back at the camera; at most 20 per layer.
Tested: compile check, 155 Core tests. Not run in Unity: page layout at different window sizes, the new flame by eye.
Noticed: in ELIA's screenshot the runner was charged at the start of level 5 because L R L R during the count-in earns a charge for the first wall (beat 8). Allowed by the rules (the first obstacle is the wall); left as is.

## 2026-11-16 — D20: flame flows back again, shorter trail

ELIA: too few flames now, and flames stuck to the body look wrong; they should flow back like real fire.
Done (setup tool only): flames are left in the world again (they trail behind), 30 per second per layer (first try 55-60, second 12), sizes 0.16-0.24, lifetimes 0.18-0.26 s. At 12.5 m/s the trail is about 3 m, short of the camera 5.5 m behind, which is what hid the runner the first time.
Tested: compile check. Not seen in Unity.

## 2026-11-16 — D20: exactly four charge taps; fuller flame with a 4 m trail

ELIA: slightly more flames, a mix of big and small, a 4 m trail; and a fifth Shift press should reset the charge, so it takes strictly four presses.
Done:
- RunnerModel: any press after a charge (tap, jump, or the start of a hold) cancels charges earned before it; the fifth tap counts as tap 1 of a fresh set, so taps 5-8 charge again. The fourth tap's own lane tap (fired just before its Alternate, at the same key-down time) does not cancel. Docs/RUNNER.md rule 2 updated.
- Flame (setup tool): 40 per second per layer (was 30), random sizes between small and big per layer (outer 0.12-0.38, middle 0.1-0.3, core 0.08-0.22), lifetimes 0.24-0.34 s for a trail of about 4 m at full speed (camera at 5.5 m).
Tested: 156 Core tests (2 new for the four-tap rule; the old eight-taps test replaced), compile check. Not run in Unity.

## 2026-11-16 — D20: charge and flame verified in the editor

ELIA: the flame "looks good now"; the four-press charge rule is in. Next: a browser build to check the menu, the flame, the four-press rule and the frame rate in WebGL.

## 2026-11-16 — D20: browser build checked; menu arrows drawn as images

ELIA ran a browser build: menu, How to play, level status, pause, charge rule and flame all look good in the browser. Only the arrows either side of the level name were missing: the Web build's font has no arrow characters (the editor falls back to a Windows font).
Done: MainMenu draws the two arrows as small triangle textures made at run time (no font needed), placed from the measured width of the level name.
Tested: compile check. Not seen in the browser yet.
Note: the round dots (menu position, count-in, charge counter) did show in the browser, so they were left as text.

## 2026-11-16 — D20: music for all six levels

ELIA picked six CC0 tracks by Tricks & Traps (Free Rhythm Game Music Packs 1 and 2, OpenGameArt), copied them into Audio/Resources and pushed them (2 to 7 minutes each, 275 MB of WAV, no .meta files yet). Agreed: level 6 at 190 with wider spacing and no midpoint tempo rise.
Measured here (downloaded from GitHub LFS, analysed with an onset-strength beat grid): T.O.E. 120, Flying Temple 128, Ascend 130, The Lab 144, Vessel of Eternity 150 (not listed on its page), Celebration and Fear 190; first downbeat at 0.42 / 1.80 / 1.77 / 1.60 / 0.33 / 1.19 s; grid within 15 ms over each level's span. Vessel measured faster than The Lab, so The Lab went to level 4 and Vessel to level 5 (tempo rises level by level).
Done:
- Levels 1-6: bpm, audioClip music_01..06, offsetMs 50. Level 6 re-timed x1.5 for 190 (312 beats, 98.5 s), tempo list removed, palette changes and track pieces scaled. All six validate with no chart warnings; level 6 passes the scripted playability walk.
- Shift+Shift > Prepare Music (editor tool): moves the full tracks to Audio/Source (outside Resources, so not built), writes trimmed clips music_01..06 to Resources (0.05 s before the first downbeat to 8 beats after the level's end, 10 ms fade-in, 2 s fade-out), Vorbis quality 0.5, not loaded in the background.
- Removed the five click tracks no level uses (110, 120, 125, 135, 130-150) and stopped generating them; level00, test_gestures and test_track keep theirs.
- Credits (menu text, now regenerated on every Set Up Visuals; Docs/CREDITS.md rows).
Rehearsed here with the same trim maths: clip lengths 56-101 s; in each clip the first beat lands at 47-62 ms (target 50) and is a downbeat; estimated 7.5 MB compressed for all six (Vorbis q4). Compile check passes.
Not done here: the actual clips (made on ELIA's PC; audio cannot be pushed from the cloud session), playing to check sync by ear, the build size with the music.
Uncertain: every level is faster than when it was tested (level 1: 100 to 120 BPM, level 6: 15.8 m/s); the WebGL build may encode audio differently (AAC), so the size estimate is rough.

## 2026-11-16 — D20: no music in Chrome; wait for the music to decode

ELIA: the music plays in the editor but not in the browser (Chrome).
Likely cause (not confirmed in a browser): Web builds decode compressed audio in the browser after it loads, asynchronously; the Conductor scheduled the clip at once, and a clip scheduled before it is decoded stays silent. The old click tracks were small and decoded in time; the editor loads audio synchronously.
Done: Conductor starts loading the clip when the level is set; StartNow becomes a request that begins playback once the clip is Loaded (or Failed, or after 15 s: then it runs silent with a warning). IsStarted only turns true when playback is scheduled, so GameFlow, Runner and PaletteApplier wait as before. GameFlow shows "Loading music..." meanwhile.
Tested: compile check, Core tests. Not run in a browser.
If it is still silent: the browser's console (F12) will say why; the Calibrate scene (clips made at run time, never compressed) tells an audio-unlock problem apart from a decoding one.

## 2026-11-16 — D20: real cause of the silent browser build: Shift is not a user gesture

ELIA's Chrome console showed "The AudioContext was not allowed to start. It must be resumed (or created) after a user gesture on the page" (166 times). Chrome does not count modifier keys (Shift, Ctrl, Alt, Meta) as a user gesture, so a game played only with Shift never unlocks sound. Earlier browser tests had sound because the page had been clicked first. The decode wait from the previous entry stays (harmless, and right for long clips) but was not the cause.
Done: ShiftKeys.jslib resumes Unity's audio context on a click, tap or non-modifier key and reports whether it is running; WebAudioUnlock.Ready wraps it (always true outside Web builds). MainMenu shows "Click to start (or press Space)" in Web builds until sound is unlocked and ignores Shift until then.
Tested: compile check for both variants; the browser script simulated in Node (Shift does not unlock, Space and a click do, fallback when Unity's audio context is not found). Not run in a browser.

## 2026-11-17 — D20: browser sound confirmed; release build with music 16.1 MB

ELIA: the music plays in Chrome after the "Click to start" screen. Release Web build (Shift+Shift > Build WebGL, Brotli): "[Build] WebGL OK in 03:16 -> Builds/WebGL, 16.1 MB on disk (plan budget: under 20 MB)". Was 9.6 MB before the music; matches the 15-17 MB estimate. About 4 MB of headroom left: UI and gesture sounds (day 23) must stay small. If space runs short, the first lever is the music quality in Prepare Music (Vorbis 0.5).

## 2026-11-17 — D20: music verified and committed

ELIA played all six levels at the new tempos ("it's all good", level 6 at 190 included) and pushed the trimmed clips (Audio/Resources/music_01..06.wav) and the moved full tracks (Audio/Source, 6 files, not built).
Audio pass status (plan day 23): music done. Still to do: gesture sound effects and UI sounds (plan: Kenney audio packs, CC0), CREDITS.md entries for them, and "no silent action anywhere".

## 2026-11-17 — D23: sound effects

ELIA pushed four Kenney packs (Interface Sounds, Digital Audio, Impact Sounds, Sci-fi Sounds; 365 .ogg files; License.txt files not copied in) and asked for the credits to name only the artists (written "Tricks & Traps" and "Kenney", the artists' spellings).
Done: SoundFx (Run and LevelTest scenes) plays a sound for every move and outcome, count-in ticks on beats -4..-1 scheduled on the audio clock (Conductor.DspTimeAtBeat, new), pause/resume sounds; MainMenu plays move/open/back sounds (the player object survives the scene change so "open" is not cut off). Choices and volumes in GreyboxSetup.Sound.cs, wired by Set Up Visuals; list in Docs/AUDIO.md. Lengths and loudness measured here (ffmpeg); volumes set against them.
Credits: menu text "Music: Tricks & Traps" / "Sound effects: Kenney"; Docs/CREDITS.md rows for the four packs.
Tested: compile check, Core tests. Not heard: every choice and volume is a first guess for ELIA to approve by ear. Size: the used files are about 20 small clips.

## 2026-11-17 — D23: sound effects approved

ELIA played the menu and levels with the sound effects: "all is good" (choices and volumes kept as in Docs/AUDIO.md). Audio pass done (plan day 23: music, gesture sounds, UI sounds, credits). Next in the plan: day 24 content lock, draft itch.io page, build to 3-5 testers.

## 2026-11-17 — D23: trailer script and animatic

ELIA asked for a gameplay video / trailer with an enticing script. Footage cannot be recorded from the cloud session (no Unity), so: script and shot list timed to Celebration and Fear (the drop found by measuring loudness per bar: quiet intro to bar 26, drop at bar 27; the trailer starts at bar 19 = 25.19 s), 14 caption and end cards (Inter Display, the action colours), and an animatic MP4 (44.2 s, music and cards in place, a labelled placeholder per shot) sent to ELIA. Docs/TRAILER.md has the script, recording and Clipchamp steps. Cards and video are not in the repo (PNG/MP4 go through LFS, which cannot be pushed from the cloud session).
Font: Inter (SIL Open Font License) used in the trailer images only, not in the game.

## 2026-11-17 — D23: trailer mode (autoplay) for recording footage

ELIA chose option 2: the runner plays every level by itself so clean footage can be recorded with Unity Recorder.
Done: Core/AutoPlan.cs works out the key presses from a level file (lane moves, holds, jumps, phases, 4-tap charges
ending on the beat; close holds released and pressed again, or joined if there is no room). Runtime: TrailerMode
(editor only, PlayerPrefs switch, menu Shift+Shift > Trailer Mode (autoplay)) adds an AutoPlayer to the Input object
when a scene loads; ShiftInput.KeySource feeds its virtual keys to the classifier, looking one frame ahead. GameFlow:
direct Play starts level 1, results go on to the next level after 4 s (stops after level 6), a fail retries after 1 s,
nothing saved; MainMenu starts level 1 at once. No scene changes; nothing to re-run in Set Up Visuals.
Depends on / could break: ShiftInput (Runner, GameFlow, MainMenu, SoundFx, GestureDebugHud, LatencyCalibration read it;
unchanged when KeySource is null), GameFlow (Run and LevelTest scenes; the Results/Failed branch was reordered so
trailer mode is checked first), MainMenu (Menu scene).
Tested: AutoPlanTests simulates levels 1-6 through the real GestureClassifier and RunnerModel at 60 fps, mirroring
Runner.Update: no hits on any level; answers PERFECT except lane moves before a phase (GOOD, 0.1 s early) and lanes
already clear (OK). Compile check (normal and Web), Core tests 163 passing.
Not run: anything in Unity. Unknown: how it looks and sounds, Recorder
settings (Variable playback suggested, not tried), and frame rate while recording at 1080p.

## 2026-11-17 — D23: trailer mode fix (hits while recording)

ELIA: recording with trailer mode only showed level 1, and the runner hit obstacles. The video is on ELIA's PC and
was not seen here. Likely cause: the AutoPlayer sampled "is the key down now" once per frame, so with slow frames
while recording at 1080p a 70 ms tap could fall between two frames (lost lane move) or a press land late; each fail
retried level 1. Fix: ShiftInput.KeySource now reports key edges with their exact planned song times
(AutoPlan.Edges), and the classifier and runner judge from key-down times, so frame rate no longer matters.
Depends on / could break: ShiftInput (keyboard path unchanged when KeySource is null), AutoPlayer.
Tested: AutoPlanTests now run every level at 60, 20 and uneven 8 fps: no hits. Compile checks, Core tests.
Not run: Unity. Not confirmed that this was the cause seen in the video.

## 2026-11-17 — D23: trailer edit handed to a session on ELIA's PC

ELIA recorded all six levels in trailer mode and chose to have the edit done by a Claude session on the PC (the
videos cannot reach the cloud session). Added Docs/TRAILER-EDIT.md (brief: inputs, timing, steps, output) and
tools/trailer/make_cards.py (the 14 caption/end cards, portable: Arial Bold on Windows if Inter is not there).
/Recordings/ added to .gitignore so the videos are never committed.
Tested: make_cards.py runs here (Inter). Not run: on Windows (Arial fallback), the edit itself.


## 2026-11-17 — D23: trailer rendered (Recordings/trailer/shiftshift_trailer.mp4)

Cut from Recordings/Movie_003.mp4 only (all six levels in trailer mode). Movie_001 (level 1 with fails) and
Movie_002 (menu, then a failed run) not used. Cut list with source in-points: Recordings/trailer/cutlist.md
(+ cuts.json, contact sheets); ELIA saw the frames before the render. Cards from make_cards.py (Arial Bold fallback
worked on Windows). Each shot rendered at exact frame counts on the bar grid (30 fps), captions overlaid,
concatenated, music 25.19 s + 44.21 s with 2 s fade, H.264 CRF 18 yuv420p, AAC 192k, 21.2 MB. Shot 3 is a 1.4x
zoom of the level 3 count-in ("close on the runner"). No speed changes. Videos are not committed (Recordings/ ignored).
Tested: ffprobe 1920x1080, 30 fps, 1326 frames, 44.21 s; frames extracted by index at cuts (black card -> TAP at
frame 303 = 10.10 s, the drop; 14d -> end card at 1213); mid-shot frames show captions readable, no debug text
(game HUD score/streak/multiplier is visible, as intended for shot 12); audio level drops over the last 2 s.
Found: trailer mode failed once in level 5 (phase, about 312 s into Movie_003) and retried; the bot is supposed
never to fail. Not investigated. Uncertain: obstacle moments were placed from chart times read off frames
(about +-0.1 s); not watched in real time with sound by a person here.

## 2026-11-17 — D23: itch.io page art

ELIA set up the draft page (jna1980.itch.io/shift-shift: browser embed, description, AI disclosure) and asked for
images. tools/trailer/make_itch_art.py draws them from the game's look (night palette, three-lane road with cyan
edges, orange lane block, red brick wall, the boxy runner with the charge flame, skyline with lit windows):
banner 960x300 (title, tagline, the two Shift keys), page background 1920x1080 (sky, skyline, empty road, darkened
for the page column), embed background 960x600 (scene plus "Click to play · headphones on"), cover 630x500 (title, "Two keys. Seven moves. One beat.", scene, the two keys). Images go to itch-art/
(ignored by git: PNGs are LFS and cannot be pushed from the cloud session); sent to ELIA.
Not checked: how they look on the itch page.

## 2026-11-17 — D23: trailer done

ELIA: the trailer came out fine (edited in a session on the PC from the trailer-mode recordings) and is linked on the
itch.io page (YouTube). Plan day 29 "screenshots and video": video done; screenshots still to take.

## 2026-11-17 — D25: tester fixes (lane move during sprint, fullscreen)

Tester 1: holding Right Shift and tapping Left twice moved only one lane. Cause (browser build): Windows sends no key-up
for one Shift while the other is held and flags its next press as a repeat; ShiftKeys.jslib discarded it as auto-repeat.
Fix: jslib reports such a press as a re-press (gap over 100 ms since that key's last key-down, other Shift held);
ShiftInput counts it as a tap only if the key's last press was already a tap (Docs/INPUT.md).
Tester 2: itch.io's fullscreen button froze the game until a click inside, and fullscreen did not fill the screen.
Causes: Run In Background was off (Unity stops when the page loses focus; the music kept going), and Unity's Default
page draws a fixed 960x600 canvas. Fix: new page template Assets/WebGLTemplates/ShiftShift (fills the frame and the
screen, gives focus back to the game on click, fullscreen and resize, resolution capped at 1.5x); Build WebGL now sets
that template and Run In Background on; GameFlow pauses the run when the game loses focus (not in trailer mode), and
the pause screen says to click the game if the keys do nothing.
Depends on / could break: ShiftKeys/ShiftInput (web input only; Runner, GameFlow, MainMenu, SoundFx read ShiftInput),
GameFlow (Run, LevelTest), BuildScript and the template (Web build only: loading, compression fallback).
Tested: compile checks (normal and Web), Core tests 175, the jslib run in Node against simulated Chrome events (first and
second Left press during a held Right both register; auto-repeat 30 ms later ignored).
Not run: a Web build with the new template (written by hand, not copied from Unity's; if the page fails to load, the
first suspect is it), real Chrome on Windows, the editor focus pause (clicking outside the Game view now pauses a run).


## 2026-11-17 — D25: calibration screen background

ELIA: the Calibrate scene showed Unity's default sky, so the white text was hard to read. LatencyCalibration now sets
the camera to the menu's plain #101418 on Awake (no scene change). Depends on: Calibrate scene only.
Tested: compile check. Not run: in Unity.

## 2026-11-17 — D25: calibration hint on How to play

ELIA asked for a pointer to calibration. How to play (MainMenu.DrawHowTo) now has a second tip line: "Timing feels off
(for example with Bluetooth headphones)? Use Calibrate timing in the menu." Fits in the existing 60 px tip area (two
lines at 17 px); checked by arithmetic for 600 and 1080 px high screens, not seen. Depends on: Menu scene only.
Tested: compile check. Not run: in Unity.

## 2026-11-17 — D25: Kenney licence files; log merges

ELIA added the four Kenney packs' License.txt files (Audio/SFX/*). Two pulls stopped on LOG.md conflicts (entries
added on both machines); ELIA resolved them by hand. .gitattributes now merges Docs/LOG.md with the union driver,
so appended entries from both sides are kept automatically (order of the two blocks may need a look).

## 2026-11-17 — D25: new build on itch.io

ELIA built Web (Build/*.unityweb, 16.05 MB; a measured 32 MB was the old shift-shift.zip left inside Builds/WebGL),
zipped it outside the folder, uploaded it to itch.io in place of the old zip; testers are re-checking the sprint lane
move and fullscreen. Not confirmed: that index.html is the new ShiftShift template (TemplateData/profiler.js in the
folder may be left over from an older build).

## 2026-11-17 — D25: itch build did not load (zip paths)

Testers: the page stayed on the loading screen. Console: 404 for Build/WebGL.loader.js. Cause: the zip was made with
PowerShell 5.1 Compress-Archive (my instruction), which writes paths with backslashes ("Build\WebGL.loader.js");
itch.io's server treats that as a file name, not a folder. Fix: zip the contents of Builds/WebGL with File Explorer
(Compress to ZIP file). Never use Compress-Archive for the itch zip. The template itself loaded fine.
Also: the YouTube trailer on the page is Private (shows "This video is private"): set it to Unlisted or Public.

## 2026-11-17 — D25: itch build loads; menu text fitted to small screens

ELIA re-zipped with File Explorer: the game loads on itch.io. In its small frame the menu text was cut off (the entry
description and the Sticky Keys note wrapped onto lines their boxes had no room for). MainMenu: text shrinks on screens
under 700 px high (to 70% at most; unchanged from 700 px up); wrapped labels measure their own height (CalcHeight);
the bottom lines are placed from the bottom edge up; How to play's rows share the space left between the intro and the
tips. The page dots were never visible in the browser (the Web font has no dot characters, like the arrows): drawn as
small squares now. Depends on: Menu scene only.
Tested: compile check (normal and Web); layout worked out by hand for 450 and 1080 px high. Not run: in Unity or the
browser.

## 2026-11-17 — D25: credits text

ELIA: music credit should read only "Tricks & Traps (CC0)". The Menu scene still held the older long credits (track
list, no Kenney line): Set Up Visuals had not been re-run and saved since day 23. Updated both the generator
(GreyboxSetup.Menu CreditsText) and the saved Menu scene's credits field: "Music: Tricks & Traps (CC0)", "Sound effects:
Kenney". Not run: in Unity (scene text edited in the file).

## 2026-11-17 — D25: confirmed

ELIA: credits, menu text fit and the itch build all good.

## 2026-11-17 — D27: juice; 3D title; mobile noted post-jam

ELIA: go ahead with the juice list; make the logo and tagline look 3D; asked whether a mobile version (two thumb
buttons, landscape) is possible.
Done (visual only, no rules or timing touched):
- Camera shake on a crash: ChaseCamera listens to Runner.RunFailed; Perlin jolt, fading over crashShakeSeconds in real
  time so it plays through the hit-stop. Settings added to CameraFeel (the asset gets the defaults: 0.25 m, 0.35 s).
- JuiceOverlay (new, added by ChaseCamera.Awake, no scene change): spark burst at the runner on every PERFECT, in the
  obstacle's action colour (22 sparks, 0.3-0.55 s); speed lines streaming out at the screen edges while sprinting
  (not while phased), fading in and out over 0.12 s. Drawn with OnGUI and the white texture: no particle shaders that
  the Web build could strip. Game time: freezes with the pause. CameraFeel: perfectSparks, speedLines (0 = off).
- MainMenu: title and tagline drawn as extruded 3D lettering (layers stepping down-right in darkening cyan / magenta,
  a faint cyan glow, white face).
- Mobile: out of scope (SCOPE.md: no touch or mobile builds); noted under Post-jam with what it would take.
Depends on / could break: ChaseCamera (Run, LevelTest, Greybox scenes), CameraFeel asset, MainMenu (Menu scene).
Tested: compile check (normal and Web), Core tests. Not run: anything in Unity; how the sparks, lines, shake and the 3D
title look is untested.

## 2026-11-17 — D27: HUD readable on light levels; HUD dots

ELIA: sparks, speed lines and crash shake are good. "Shift to retry" was hard to read over the overcast (light) level.
GameFlow: every HUD label now has a soft dark shadow (Label helper; follows the GO fade); the fail (once retry is
possible), results and pause screens darken the game behind them (45% black). The count-in "Get ready" beat dots and
the charge counter above the runner used the characters ● and ○, which the Web font does not have (as with the menu):
now drawn as squares (DrawDots). Depends on: GameFlow (Run, LevelTest).
Tested: compile check (normal and Web). Not run: in Unity or the browser.

## 2026-11-17 — D27: HUD shadows checked

ELIA's screenshots: "Shift to retry" readable on the overcast level with the dimmed background. The big GO's shadow
(8 px away at 140 px text) read as a grey double: shadow offset now capped at 4 px.

## 2026-11-17 — D27: title in the six action colours

ELIA saw the 3D title (the Game view was zoomed to 1.4x, hiding the top and bottom of the screen) and asked for the
letters in the six obstacle colours, mirrored pairs from the outside in. MainMenu: SHIFT+SHIFT as rich text, S..T
orange (lane), H..F yellow (slide), I..I cyan (sprint), F..H pink (jump), T..S blue (phase), + red (charge); extrusion
layers tinted grey so each letter's sides are darker shades of its own colour; glow takes the letter colours.
Tested: compile check (normal and Web). Not run: in Unity.

## 2026-11-17 — D27: satin title

ELIA: the coloured title "looks horrible": the coloured extrusion merged the letters into blobs; wanted a 3D satin
fill, not solid. MainMenu.SatinTitle: dark navy extrusion (7 layers) and a dark outline, the same for every letter;
the face in the letter's action colour; a soft light sheen over the top half with a thin bright line near the top, and
shading towards the bottom, drawn as 1 px clipped slices (GUI.BeginGroup) of the text, each tinted (IMGUI cannot fill
text with a gradient). Previewed here with the same steps in Pillow (DejaVu Sans Bold standing in for Unity's font):
first version showed stripes from overlapping slices, fixed with whole-pixel 1 px slices.
Tested: compile check (normal and Web), the preview. Not run: in Unity.

## 2026-11-17 — D27: title pink

ELIA: pink and red looked similar in the title. Previewed two lighter pinks; ELIA chose #FF4FE8 (magenta-pink). Title
only: the game keeps #F40E9D, since a lighter jump pink would come close to the light pink ghost blocks (#FFB5D9).

## 2026-11-17 — D27: custom title lettering

ELIA asked for title lettering like the Shift legend on a gaming keyboard (ROG Strix), but bold. ROG's typeface is
ASUS's own (not licensable for a public game, and it would borrow their brand look), so the title is an original
design instead: Core/TitleArt draws S, H, I, F, T and + from a few straight-edged shapes each (stroke 3, cap 12,
diagonal corner cuts), anti-aliased, and bakes in the pair colours (title pink #FF4FE8), a dark navy extrusion, an
outline and the satin sheen. MainMenu turns the pixels into a texture once per size (redrawn only if the screen height
changes) and draws it in place of the text title; the earlier IMGUI satin and rich-text title code is removed. The
tagline keeps its text look. No font file or licence needed.
ELIA then asked for more options: eight variants (current, keycap = all corners cut, italic, italic keycap, split,
italic split, wide, wide italic keycap) rendered with a preview copy of the same code; waiting for a choice.
Tested: Core tests 178 (TitleArtTests: size, coverage, the outer S orange, the + red), compile check (normal and Web).
Not run: in Unity.

## 2026-11-17 — D27: Black Ops One title previews

ELIA asked to stylize Black Ops One (SIL OFL, stencil) for the title. tools/title/make_title.py bakes SHIFT+SHIFT in
that font with the pair colours, dark navy extrusion, outline and satin face, optional lean (--skew) and depth
(--depth); the font file and its OFL licence sit beside it (tools only, not in the game yet). Three previews sent:
A straight, B italic, C italic with deeper 3D. Waiting for ELIA's choice before it replaces the TitleArt lettering.

## 2026-11-17 — D27: white-letter title option

ELIA asked about white letters with each pair's colour as a shadow. make_title.py --white: white satin face, the 3D
sides in each letter's colour darkening with depth. All styles now drawn at double size and scaled down (the stepped
extrusion showed fine diagonal stripes). Previews sent: C colour letters, D white letters, E white with deeper sides.

## 2026-11-17 — D27: Russo One title previews

ELIA asked to try Russo One (SIL OFL). make_title.py takes --font (font files beside it: Black Ops One, Russo One,
each with its OFL text) and --gap (Russo One sits tight: 0.14 of the letter height between letters). Previews sent:
C colour letters, D white letters, E white with deeper sides, F upright E.

## 2026-11-17 — D27: title back to the first 3D version

ELIA chose the first 3D title (commit 47f2824): white face, cyan extrusion fading to dark, faint cyan glow, Unity's
default font. MainMenu.DrawLogo draws it again with Extruded; Core/TitleArt and its tests are removed (unused; in git
history at ae0b8c8). tools/title (Black Ops One / Russo One generator with fonts and OFL texts) is kept as a tool; the
game uses no extra font, so the credits need no font line.
Tested: compile check (normal and Web), Core tests 175. Not run: in Unity (same drawing ELIA saw on day 27).

## 2026-11-17 — D27: title block with tiles and the leaping runner (menu, banner, cover)

ELIA approved (after previews): six tiles in the action colours above the title, three over each SHIFT and none over
the +; the boxy runner leaping from the 3rd tile to the 4th over the + on a dotted arc, with speed streaks; near limbs
white, far limbs grey and drawn behind the body; the first 3D title; the tagline clean (no glow, 2 px dark shadow).
MainMenu.DrawLogo draws it all with IMGUI boxes (rotated with GUIUtility.RotateAroundPivot; no image files). The title
block moved down to make room (title centre at max(8% + 150 px, 214 px) x scale) and the level selector and "Click to
start" follow it on short screens (worked out for 450, 600 and 1080 px high). Extruded skips the glow when it is clear.
itch.io art (tools/trailer/make_itch_art.py, logo_block with the same measurements): banner now 960x360 (title block
on the left, the keys below it), cover 630x500 with the title block at the top. Images in itch-art/ (not in git).
Tested: compile check (normal and Web), previews of the itch art. Not run: the menu in Unity.

## 2026-11-17 — D27: title block approved

ELIA's screenshot of the menu in Unity (no console errors): tiles, leaping runner, 3D title and tagline drawn as in the
preview; layout intact. "Looks really good." Juice step (plan day 27) done, plus the title work.

## 2026-11-17 — D27: trailer retake prepared

ELIA asked whether to retake the trailer with the new look and logo: yes (sparks on every PERFECT, speed lines,
readable HUD, new title). make_cards.py's end card now uses make_itch_art.logo_block (tiles, leaping runner, 3D title,
tagline) above the call to action. logo_block draws the title sides in 1 px steps (at end-card size the 2-4 px steps
showed stripes); banner and cover re-rendered. Docs/TRAILER-EDIT.md: "Retake (day 27)" steps for the PC session.

## 2026-11-17 — D27: trailer retake rendered (Recordings/trailer/shiftshift_trailer.mp4)

Re-cut from Recordings/Movie_005.mp4 (new trailer-mode recording of all six levels; no fails this time, level 5
included). Same shot list and timings as the first trailer (first version kept in Recordings/trailer/v1/). Beat 0 of
each level found from the count-in; levels 2 and 4 line up with the old recording exactly, the others by a fixed
offset, and every pick was moved to the same moment (checked on contact sheets, approved by ELIA before render).
Cards regenerated (new end card with the title block). Movie_004 (7 s of the new menu) not used: no slot in the
fixed shot list. Output: 1920x1080, 30 fps, 1326 frames, 44.21 s, H.264 CRF 18, AAC 192k, 21.7 MB.
Tested: frames by index at cuts (black card -> TAP at frame 303 = the drop; 14d -> end card at 1213), a mid frame
of every shot (captions readable, HUD visible, no debug text), audio drops over the last 2 s.
Uncertain: the PERFECT sparks are small in the footage (a few dots for a few frames); not watched in real time with
sound here.
## 2026-11-17 — D27: trailer retaken

ELIA: retake done and on YouTube with the new description (challenge tone). Next: browser pass (day 28), screenshots,
README / PLAN checklist, final signing on 30 Nov, submission 1 Dec.

## 2026-11-17 — D27: animated GIF cover

ELIA wanted the animated cover from the checklist. make_itch_art.py: the title block split into logo_geom /
draw_tiles / draw_arc / draw_runner / draw_title (static art unchanged); cover_gif makes cover_630x500.gif, a 2.2 s
loop at 20 fps: the runner waits on the 3rd tile, leaps over the + leaving the dotted trail, lands on the 4th tile
(the tile flashes, pink sparks burst), then fades back to the start. 39 frames (duplicates merged), 133 KB, one
shared palette, no dithering. Checked frame by frame here; not yet seen on itch.io.

## 2026-11-17 — D27: embed background without the title

ELIA: the banner's logo and a second plain SHIFT+SHIFT on the game frame's background right below it looked doubled.
embed_bg_960x600.png now carries only the scene and "Click to play · headphones on". Also noted: the itch.io page is
now at jna24.itch.io/shift-shift (username changed); use that in the README and the YouTube description.

## 2026-11-17 — D27: hook line on the game frame

ELIA chose the hook "You've pressed Shift a million times. Never like this." followed by the click hint.
embed_bg_960x600.png: the hook above the Run game button (where the old title sat, fully visible in itch's frame,
740 px wide so it survives a side crop to 800), "Click to play · headphones on" below the runner as before.

## 2026-11-17 — D27: click hint under the hook

ELIA: move "Click to play · headphones on" up too. Now right under the hook line, both above the Run game button.

## 2026-11-17 — D27: hook fits the small frame

ELIA's screenshot: the hook line was cut at both ends. The itch embed is still 640x360 (the default; 960x600 was
suggested on day 23 but not set), and itch shows the background at 1:1, centred, so only x 160-800, y 120-480 of the
960x600 image show. Hook font 30 -> 24 px: fits the middle 640 px. Asked ELIA to set the viewport to 960x600 (the
game's canvas) so the game plays larger too.

## 2026-11-17 — D27: hook and hint clear of the wall

ELIA: the click hint sat on the red wall. Hook at y 156 and hint at y 192 (were 182 / 222): the hint now ends above
the wall's top (y 220); both still inside the 640x360 centre crop.

## 2026-11-17 — D27: GitHub username ELIAJNA

ELIA renamed the GitHub account: the repo is now github.com/ELIAJNA/Shift-Shift (the old JNA1980 address redirects).
The cloud session keeps the old address for its remote: its GitHub access is tied to it (a push to the new address
was refused, 403); GitHub forwards the old address, so pushes still arrive. Older log entries keep the old name as history.
To update on ELIA's side: the PC's git remote, the "Source code" link in the itch.io description, the YouTube
description if it links the repo.

## 2026-11-17 — D27: results heading names the level

ELIA: the results screen should say which level was completed, with a cheer ("Hurray! Level 1 complete", "Awesome!
Level 2 complete", ...). GameFlow.CompleteHeading: Hurray / Awesome / Brilliant / Fantastic / Unstoppable / Legendary
for levels 1-6 (number read from the level's asset name); "Level complete" for test levels. Results box widened from
40% to 50% of the screen so the longest heading fits at 960 px wide.
Tested: the heading function run on every level name (level01-06, level00, test_track, none); compile check.
Not run: in Unity.

## 2026-11-17 — D27: celebration dances

ELIA asked for a short celebratory dance per level at the end of the level. RunnerFigure.Dance (when the run is
Completed): the runner turns to face the camera (0.35 s), then dances on the beat of the level's tempo, timed on the
audio clock (dspTime): 1 victory pump, 2 disco point, 3 jumping jacks, 4 spin and pose, 5 running man, 6 finale
(spin, big jump, arms up with the flame lit). Posed from the existing limb pivots (shoulders, hips); no new art.
GameFlow: the results box waits celebrateSeconds (3 s, Inspector) after the level ends; meanwhile only the cheer
heading (big, top of the screen) and the score show; results keys start when the box appears (keys reset then, so a
held key cannot pick an option). Trailer mode waits 3 s longer on each level's results. GameFlow.LevelNumber added
(shared by the heading and the dance).
Depends on / could break: RunnerFigure (Run, LevelTest, Greybox scenes; only acts once a run is Completed), GameFlow
results flow (Run, LevelTest).
Tested: compile check (normal and Web), Core tests 175. Not run: in Unity. The limb angles are worked out from the
figure's pivots, not seen: a dance may need its angles flipped (an arm going into the body instead of out).

## 2026-11-17 — D27: dances approved; music check

ELIA: "The dances are really good." ELIA then felt the music tracks had changed; checked: no audio, level or timing
change since day 20, and the six music files on ELIA's PC match the committed SHA-256 fingerprints with no local
changes. ELIA compared again: "it sounds same". Nothing changed.

## 2026-11-17 — D27: game frame at 960x600; runner clear of the Run button

ELIA set the itch viewport to 960x600: the whole background shows, but the Run game button (frame centre) covered the
runner. embed_bg: scene moved down (horizon 46% -> 62%): the button now sits on the wall top, the runner fully below
it. Checked with a stand-in button drawn at the centre.

## 2026-11-17 — D27: game frame scene - other obstacles, running runner

ELIA: not the same orange block and red wall as the banner and cover; the runner running with smoke.
embed_bg: a yellow low bar (slide) across the road and a translucent blue phase wall behind it with a pink ghost block
in the left lane; the runner seen from behind mid-stride (left leg planted, right leg kicking back with the sole
showing, arms swinging) with dust puffs trailing from the heels, no flame. The banner and cover keep the block, wall
and flame. Still checked with a stand-in Run button: clear of the runner.

## 2026-11-17 — D27: ghost blocks shaped like ghosts

ELIA: the ghost block should not look like the orange lane block. Game: GreyboxSetup.RebuildGhostBlock (run by Set Up
Visuals, before the glow edges, once: marker GhostV1) rebuilds GhostBlock.prefab as a ghost from boxes: body, a rounded
top of three narrowing layers, three tails with gaps (it floats 0.19 m up), two dark eyes (Hole material) on the -z face
the runner sees. Same footprint (1.6 x 2 x 0.6 m) and lane: rules unchanged, look only. Itch art: the same ghost at half
size in the game frame's phase wall (left lane); the phase wall now comes first (z 9) and the slide bar behind it
(z 17) so the ghost is not hidden.
Depends on / could break: GhostBlock.prefab (level 4's ghostbar/ghostwide and level 6; Greybox scene), its glow edges
(added per box: the ghost's steps get edge strips).
Tested: compile check (Editor assembly). Not run: Set Up Visuals in Unity; how the glow edges look on the stepped shape.

## 2026-11-17 — D27: ghost blocks see-through

ELIA: make the ghost slightly transparent like the wall. GreyboxSetup.Track: the barrier's transparent-material setup
moved into SeeThrough(material, alpha, queue); the Ghost material gets it too at the wall's 65%, render queue 3001
(drawn after the wall's 3000: the ghost sits inside the wall's glass and two see-through surfaces at the same place
would otherwise swap order and flicker). PaletteApplier keeps a material's own alpha when recolouring (day 17), so the
pink stays see-through. Art: the ghost at 65% too.
Tested: compile check (Editor). Not run: Set Up Visuals; how the overlapping see-through boxes look in the game.

## 2026-11-18 — D28: score label on the HUD

ELIA: the top line showed "20   streak 0" without saying 20 is the score. GameFlow.DrawScore: "Score 20 · Streak 0"
(the multiplier still follows in yellow when above x1). Run and LevelTest scenes.
Tested: compile check. Not run: in Unity.

## 2026-11-18 — D28: grade tally on the HUD; scoring explained

ELIA asked whether the streak is in the rules (only named on How to play) and agreed to both changes:
- GameFlow HUD: "PERFECT n   GOOD n   OK n" (cyan, green, grey), small, left side, just under the score line (no
  overlap with the centred score at any score or at 960 px).
- How to play: the first tip now reads "PERFECT 50, GOOD 20, OK 5 points. Hit on the beat in a row to build a streak:
  x2 at 10, x3 at 25, x4 at 50. OK keeps your streak; a crash ends the run." The tips area measures its own height;
  worked out to fit at 450 and 600 px high (rows shrink to fit).
Tested: compile check (normal and Web). Not run: in Unity.

## 2026-11-18 — D28: HUD shadows on coloured words fixed

ELIA: the tally looked blurry. Cause: GameFlow.Label drew its shadow by tinting a copy of the text dark with GUI.color,
but a <color> tag's colour is not darkened by GUI.color, so coloured words got a second coloured copy 2-4 px away (the
day 27 comment said the opposite; wrong). Same on the yellow multiplier and NEW BEST. The shadow copy now has its
<color> tags removed. Menu text does not use this shadow.
Tested: compile check (normal and Web). Not run: in Unity.

## 2026-11-18 — D28: trailer script rewrite

ELIA approved a new trailer script: action in the first second (cold open), the hook "You've pressed Shift a million
times. Never like this." with the new title, "Two keys. That's all you get.", "Ready?" on the drop; the six moves
(phase shot with the new ghosts); "Six levels. 120 to 190 BPM."; "Miss the beat?" over a crash, "Stay on it." over a
clean run with the tally; "Can you keep up?" over the finale dance; end card. make_cards.py SHOTS rewritten (17 shots,
same bar timings), Docs/TRAILER.md script table, Docs/TRAILER-EDIT.md "Re-cut with the new script". Cards rendered
and checked here. The re-cut runs in the session on ELIA's PC; one new clip needed (a crash, recorded by hand).

## 2026-11-18 — D28: lighter caption shade; shot 3 as a rendered leap

ELIA: the caption shade was a bit dark; use the cover animation for 3.8-6.3 s. make_cards.py: the shade behind
captions 110 -> 56 alpha. make_itch_art.py: the leap animation shared (leap_frames) by the cover GIF and a new
title_clip (--title-clip DIR): 1920x1080 PNG frames, 30 fps, 2.53 s (2 bars at 190 BPM), the title block on the end
card's dark background with a few stars, the runner waiting on the 3rd tile, leaping, landing on the 4th with sparks,
staying landed. Rendered here (49 s for all art) and previewed with the caption over it; the cover GIF unchanged.

## 2026-11-18 — D28: trailer mode starts from the level picked in the menu

ELIA asked how to record only level 6. Trailer mode always started at level 1 from the menu. MainMenu no longer jumps
to level 1 in trailer mode: pick any level as usual; GameFlow then autoplays it and the levels after it. A Run scene
opened directly in trailer mode still starts at level 1. Docs/TRAILER.md updated.
Tested: compile check (normal and Web). Not run: in Unity.

## 2026-11-18 — D28: trailer done; mobile kickoff brief

ELIA: the new trailer is done and on YouTube and itch.io. ELIA wants to start the mobile version now in a session on
the PC. Docs/MOBILE-KICKOFF.md: ELIA's approval as a separate track on a `mobile` branch in its own worktree folder
(main stays the jam build until submission); how to move the branch onto main after the 30 Nov history signing
(rebase --onto); pointers into the code (touch should feed ShiftInput.KeySource, so the classifier and rules are
unchanged; menus already run on two keys; Calibrate reads the keyboard directly); the agreed touch design (left/right
half of the screen, one finger per side); early checks (Android first, audio latency, performance, text, pause); order
of steps (M01...). SCOPE.md on main unchanged (mobile stays post-jam there).

## 2026-11-19 — D29: jam deadline checked

Official (GitHub's Game Off 2026 post and the itch.io jam page, via web search; itch.io and github.blog are not
reachable from this cloud session): submissions open 1 Nov, close **1 Dec 2026 13:37 PST = 21:37 UTC = 2 Dec 03:07 IST**,
for both the itch.io submission and the GitHub push; the source must be in a public GitHub repo; pushing code alone
does not enter the jam; voting closes 1 Jan 2027 13:37 PST. Our plan's "build by 12:00 IST, submit by 18:00 IST on
1 Dec" is our own buffer, about 9 hours before the real close. Post-deadline changes: the FAQ (per forum replies) allows
critical bug fixes only. Plan: submit early (once the browser pass and screenshots are done), keep the 30 Nov signing.

## 2026-11-19 — D29: tester feedback - fullscreen Esc, panels, grade colours, pop animation

1. Esc in fullscreen zoomed out instead of pausing: browsers keep Esc to leave fullscreen and never pass it to the page
   (cannot be changed). GameFlow.PauseOnResize: any change of the frame's size during the count-in or a run pauses
   (so the Esc that leaves fullscreen also pauses; entering fullscreen mid-run pauses too). Not in trailer mode.
2. Background panels: new UIPanel (Runtime): a dark see-through card, rounded corners, thin cyan edge, soft glow, one
   texture made in code and drawn 9-sliced. Used on: the menu's level selector, "Click to start", How to play (behind
   intro, moves and tips), Credits, Calibrate, Pause, Results, and behind "Shift to retry". Texture logic previewed here.
3. Grade pop-ups were white over the white runner: now coloured (PERFECT cyan, GOOD green, OK amber, MISS red, dark
   shadow) and above the runner (36% of the height, was 62%). The tally uses the same colours (OK was grey).
4. Pop animation: grades appear at 1.7x, snap to 0.92x in 0.08 s, settle at 1x by 0.16 s, float up and fade over 0.6 s
   (GUIUtility.ScaleAroundPivot). The level-complete cheer pops in the same way.
Depends on / could break: GameFlow (Run, LevelTest), MainMenu (Menu), LatencyCalibration (Calibrate).
Tested: compile check (normal and Web), Core tests 175; panel and pop previews rendered here. Not run: in Unity.

## 2026-11-19 — D29: pause box layout

ELIA's browser screenshot: in itch's frame the hint "(keys do nothing? click the game first)" overlapped "Right Shift:
restart" (lines at fixed fractions of the height). Pause lines are now stacked one after another (title 64 px, options
38 px, hint 26 px, 6 px gaps) and the panel is sized around them (~270 px), centred at 47% of the height.
Also confirmed from the screenshot: panel, coloured tally and the pause on the fullscreen change work in the browser.
Tested: compile check (normal and Web). Not run: in Unity.

## 2026-11-19 — D29: How to play in one box; runner falls into a missed gap

ELIA's screenshots: How to play's title and "Any Shift: back" sat outside the panel, and at full screen the moves left
a large empty gap. MainMenu.DrawHowTo laid out top down: title, intro, six move rows, tips, back hint, all inside one
panel sized to its content and centred; rows are at most 56 px (x scale) and shrink to fit short screens (worked out:
~560 px tall at 1080, ~500 at 600, fits at 450).
ELIA: on a missed gap the runner stood at the edge. Runner.FailedOn (new, read-only); RunnerFigure.FallIntoGap: after
the crash freeze (game time, so it waits through the hit-stop and pause) the figure drifts 1.2 m forward over the hole,
tips forward up to 75 degrees and falls (9.8 m/s2) down to 2.6 m below the road, arms up and flailing. Look only; the
camera stays.
Depends on / could break: Runner (one read-only property), RunnerFigure (only acts on a fail at a Gap), MainMenu.
Tested: compile check (normal and Web), Core tests 175. Not run: in Unity (the fall's direction, forward = the figure's
+z, is worked out, not seen).

## 2026-11-19 — D29: gap fall fixed

ELIA's screenshot: the runner fell onto the road beyond the gap and sank through it. Cause: the run failed at the
gap's far edge (an early jump landed before it; FailStop puts the runner at the failed edge), and the fall always
moved 1.2 m forward. Now the fall moves to the gap's centre (Obstacle.trackDistance - Runner.Distance, forward or back)
in 0.25 s while dropping, and, as ELIA asked, straight down with both arms raised: no forward tumble, no flailing,
a 6 degree lean.
Tested: compile check. Not run: in Unity.

## 2026-11-19 — D29: gap miss where the runner lands; head stays in view

ELIA's screenshots: after the fall the head sank out of view; on an early jump the runner went on to the far edge and
then fell back. Runner.Update: when the runner is over a gap (near edge passed in the air, far edge ahead), no key
press undecided, and it is no longer airborne, the run fails there and then (Miss, Fail at the current distance:
FailStop keeps it before the far edge). Same outcome as before (landing before the far edge always failed at the far
edge check), only the moment and place change. A jump that covers the far edge is unaffected (autoplay's jumps stay
airborne past it: far edge at beat + 0.25 s + depth/2, airborne until key-up + 0.5 s). RunnerFigure.FallIntoGap: steps
into the hole only from the near edge (to 0.5 m inside); a runner already over the hole drops where it is; drop 1.3 m
(was 2.6) so the head and raised arms stay in view.
Depends on / could break: Runner.Update (gap judging moment; Run, LevelTest), RunnerFigure.
Tested: compile check (normal and Web), Core tests 175. Not run: in Unity.

## 2026-11-19 — D29: gap fall - momentum and depth

ELIA: a running figure would not drop right at the edge; and too much torso showed after an early jump. The fall now
carries on forward (easing out over 0.3 s): from the near edge to about 1 m inside the gap; from inside, 0.6 m on;
never past 0.45 m before the far side, never back. Drop 1.6 m (was 1.3): head, shoulders and raised arms show.
Tested: compile check. Not run: in Unity.

## 2026-11-19 — D29: gap fall - a forward arc, a lean, depth from the camera

ELIA: the fall still dropped straight down; it should lean forward and fall with its momentum; and after an early jump
the legs showed (ELIA had asked for less body, I had made it deeper but it also drifted further from the edge).
Why depth alone did not work: the camera looks down into the pit, so the further past the near edge the runner is, the
more of it shows. RunnerFigure.FallIntoGap now: forward motion easing out over 0.5 s while falling faster and faster
(8 m/s2), so the path arcs; a lean forward to 30 degrees; arms up. Forward: from the near edge to 1.2 m into the
hole, from inside 0.8 m on, at most to 0.5 m before the far side. Depth: worked out at the start of the fall from the
camera's position (runner-local): the line from the camera over the near edge gives how far below the road the edge
hides things at the figure's final place; the shoulders go 5 cm below it (clamped 1-3.2 m). Worked numbers: no jump,
drop 2.0 m; early jump landing 1.5 m in, drop 3.0 m; head and raised arms show in both.
Tested: compile check (normal and Web), the numbers above. Not run: in Unity.

## 2026-11-19 — D29: gap rule checked (no change to outcomes or grades)

ELIA asked to check the gap mechanics and grades still work as before after the "fail where you land" change.
New GapRuleTests: one gap, a jump pressed at every offset from -450 to +450 ms (5 ms steps), at each level's tempo
(120-190 BPM), through the real GestureClassifier and RunnerModel the way Runner.Update judges (60 fps, checks at both
edges); once with the old rules, once with the new one. Every jump: same pass/fail and same grade; a miss is never
called later than before. Windows at 120 BPM: PERFECT within 50 ms, GOOD 55-115 ms early or late, OK 120-200 ms early
or 120-125 ms late, a miss beyond (190 BPM: OK to 250 ms early and 170 ms late). AutoPlanTests' level simulation now
mirrors the new rule too: all six levels still clear with the same grades at 60, 20 and 8 fps. Core tests 181.

## 2026-11-19 — D29: README link, checklist

README: the itch.io link (jna24.itch.io/shift-shift, public from the submission). PLAN.md checklist: README ticked;
butler items ticked as not used (zips uploaded on the itch.io site); the submit item now carries the official close
(2 Dec 03:07 IST) and the 25-27 Nov target. Still open for ELIA: joined the jam, browser pass, screenshots, GIF cover
upload, theme sentence and GitHub link on the page, final build, signing, submission, tag, community post, voting.
