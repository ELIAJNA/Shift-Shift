# Shift+Shift Mobile: kickoff brief (for the Claude session on ELIA's PC)

ELIA approved starting the mobile version now (18 Nov 2026) as a **separate track**. It is outside the jam scope in
Docs/SCOPE.md ("touch or mobile builds"), so: all mobile work happens on the `mobile` branch, in its own folder, and
never touches `main` before the jam is submitted (1 Dec 2026). The jam build on `main` must stay exactly as it is.
On the `mobile` branch, move "Mobile version" in Docs/SCOPE.md from Post-jam to In scope (with this note), so the
project rules allow the work there.

All project rules in CLAUDE.md still apply (dependency list before changing a script, say what was not run, dspTime for
timing, no unrequested features, commit after each step, Docs/LOG.md entries). Commit prefix on this branch: `M01:`,
`M02:` ... (mobile day number), e.g. `M01: mobile branch and touch plan`.

## 1. Setup (first step, before any code)

The jam project stays in `C:\dev\shift-shift` on `main`. Make a second folder for mobile with a git worktree (same
repository, different branch, so Unity never has to switch branches under an open project):

```
cd C:\dev\shift-shift
git pull --no-rebase --no-edit
git worktree add ..\shift-shift-mobile -b mobile
cd ..\shift-shift-mobile
git lfs pull
git push -u origin mobile
```

Open `C:\dev\shift-shift-mobile` as its own project in Unity Hub (first import takes a while). Add the Android Build
Support module (with OpenJDK and Android SDK & NDK) to Unity 6 in Unity Hub if it is not installed.

**After the jam signing step (30 Nov / 1 Dec):** tools/sign-history.sh rewrites `main` (every commit gets a new id).
Move the mobile commits onto the signed main afterwards, from `C:\dev\shift-shift-mobile`:

```
git fetch origin
git rebase --onto origin/main <the main commit mobile started from> mobile
git push --force-with-lease origin mobile
```

(`git merge-base mobile <old main tip>` gives the start point; note it in Docs/LOG.md when the branch is made.)
Sign the mobile commits with ELIA's key at that point too.

## 2. What the game is (read the code, these are pointers)

- Unity 6.6 (6000.6.8f1), URP, plain C#, no third-party frameworks. Primary target so far WebGL; secondary Windows.
- Assemblies: `Assets/_Game/Scripts/Core` (engine-free, tested with `dotnet test tools/CoreTests`, 175 tests),
  `Runtime`, `Editor`, `Tests/EditMode`.
- Two keys, seven gestures (Docs/INPUT.md): tap L/R = lane change, hold L = slide, hold R = sprint, both tap = jump,
  both hold = phase, alternate L R L R (4 taps) = charge. Thresholds in the GestureSettings asset (tap/hold 250 ms,
  both window 80 ms, alternate gap 400 ms).
- `Core/GestureClassifier` turns key-down/key-up events (with song times) into gestures. `Runtime/ShiftInput` feeds it
  from the keyboard (Input System), or in Web builds from `ShiftKeys.jslib` (browser key quirks), or from
  **`ShiftInput.KeySource`**: a hook that reports key edges with exact song times (used by trailer mode's AutoPlayer).
  **Touch input should use KeySource**: each on-screen button is one Shift key. The classifier and every game rule then
  work unchanged.
- Menus (MainMenu, GameFlow results/pause, Calibrate) are driven by the same two keys (ShiftMenuKeys: left, right,
  both), so the two touch buttons drive the menus too. LatencyCalibration reads the keyboard directly: it needs a touch
  path.
- Timing: Conductor on AudioSettings.dspTime and PlayScheduled; the player's latency offset from the Calibrate screen.
- All UI is IMGUI (OnGUI); text scales with screen height in MainMenu (`_k`).
- Levels: JSON in Assets/_Game/Levels (level01-06), music in Assets/_Game/Audio/Resources.

## 3. Mobile design (agreed so far)

- Landscape only. Two large on-screen buttons, one per thumb: **left half of the screen = Left Shift, right half =
  Right Shift** (whole halves, not small buttons: thumbs must not miss). Drawn as faint keycaps in the bottom corners
  (cyan LEFT, pink RIGHT, like the itch banner) that light up while touched.
- Multi-touch: each finger belongs to the half it started on until it lifts; a finger sliding across the middle stays
  on its side. Two fingers on one half count as one press.
- Timing: a touch's key-down time is the touch's start time converted to song time (Input System touch timestamps
  where available), not the frame time.
- No gameplay or level changes. Same seven gestures.

## 4. Things to check / decide early

1. **Target first:** Android APK (native) first, tested on ELIA's phone. Mobile browser (the WebGL build with touch)
   can follow; iOS needs a Mac (later).
2. **Audio latency on Android:** Project Settings > Audio > DSP Buffer Size = Best latency; measure with the Calibrate
   screen (touch screens add 30-80 ms; Bluetooth more). The Calibrate screen must work by touch.
3. **Performance:** phone GPUs. Check frame rate on the device on level 6 (190 BPM, most scenery). Levers: URP render
   scale, bloom off or cheaper, fewer scenery plots, shadow distance. Target a steady 60 fps.
4. **Text and screens:** hide the Windows Sticky Keys note and "Esc" hints on touch; How to play names "left/right side
   of the screen" instead of Shift keys; safe area (notches); minimum text size readable on a phone.
5. **Pause:** a small pause button (top corner) since there is no Esc; and pause when the app goes to the background.
6. **Keep the keyboard path working** in the mobile project (desktop testing in the editor): touch adds to it.

## 5. Suggested order of steps (one commit each, LOG entry each)

1. Branch, worktree, SCOPE.md note, LOG entry (M01).
2. Engine-free touch mapping in Core (finger -> side, half-screen rule, finger ownership) with tests in
   tools/CoreTests; then `TouchKeys` in Runtime feeding `ShiftInput.KeySource` (keyboard still works).
3. Touch buttons drawn (keycaps, lit while held); Calibrate by touch.
4. Android build settings: landscape lock, package name, minimum API level, IL2CPP + ARM64, icon; Build Android APK
   menu item next to Build WebGL. Install on the phone, play level 1.
5. Mobile text pass (How to play, hints, pause button, safe area).
6. Performance pass on the device.
7. Latency check with Calibrate; tune.
8. Then decide: mobile web build, Play Store (internal testing track first), iOS.

## 6. Not now

No store publishing, no ads, no accounts or leaderboards, no new levels, no gamepad. Ask ELIA before anything beyond
the steps above.
