# Trailer (plan day 29: 30-45 s video)

44 seconds, cut to **Celebration and Fear** (Tricks & Traps, 190 BPM, the level 6 track). Use the full track in
`Assets/_Game/Audio/Source/6_celebration_and_fear.wav` from **25.19 s** (bar 19, eight bars before the drop) for
44.2 s, fading out over the last 2 s. One bar = 1.263 s. The drop lands at **10.1 s**: that is where the moves start.

Made in the cloud session (not in the repo, sent to ELIA): the 14 caption and end cards (1920x1080 PNG, captions
transparent) and an animatic MP4 with the music and every card in place, plus a labelled placeholder per shot.

## Script (day 28 rewrite, ELIA approved)

Action in the first second, then the hook, a challenge, and the reward. Captions are the narration (no voice).
Cards: `python tools/trailer/make_cards.py --out <folder>` (cards.json lists each card's start, length and footage).

| # | Time (s) | Bars | On screen | Footage |
|---|---|---|---|---|
| 1 | 0.0-1.3 | 1 | (none) | Cold open: level 6 at full speed, PERFECT sparks flying |
| 2 | 1.3-3.8 | 2 | **YOU'VE PRESSED SHIFT A MILLION TIMES.** | Black card |
| 3 | 3.8-6.3 | 2 | **NEVER LIKE THIS.** | The menu: the title with the runner leaping over the + |
| 4 | 6.3-8.8 | 2 | **TWO KEYS. THAT'S ALL YOU GET.** | The 4-3-2-1 count-in, close on the runner |
| 5 | 8.8-10.1 | 1 | **READY?** | Black card; cut on the drop |
| 6 | 10.1-11.4 | 1 | **TAP** / DODGE (orange) | Level 1: a lane change past an orange block |
| 7 | 11.4-12.6 | 1 | **HOLD LEFT** / SLIDE (yellow) | Level 2: slide under a low bar |
| 8 | 12.6-13.9 | 1 | **HOLD RIGHT** / SPRINT (cyan) | Level 2: sprint through a gate |
| 9 | 13.9-15.2 | 1 | **BOTH** / JUMP (pink) | Level 3: jump a gap, water below |
| 10 | 15.2-17.7 | 2 | **HOLD BOTH** / PHASE (blue) | Level 4: phase through a blue wall past the ghosts |
| 11 | 17.7-20.2 | 2 | **L R L R** / SMASH (red) | Level 5: four taps, flame, the smash at the end of the shot |
| 12 | 20.2-25.3 | 4 | **SIX LEVELS. 120 TO 190 BPM.** | 1 bar per level, getting faster |
| 13 | 25.3-27.8 | 2 | **MISS THE BEAT?** | A crash: the jolt, the dimmed screen, "Shift to retry" (recorded by hand) |
| 14 | 27.8-30.3 | 2 | **STAY ON IT.** | A clean run: PERFECT sparks, the tally climbing, x4 |
| 15 | 30.3-35.4 | 4 | (none) | Level 6 at full speed, 1-bar cuts: bend, crest, tunnel, archways |
| 16 | 35.4-40.4 | 4 | **CAN YOU KEEP UP?** | End of level 6, then the finale dance: spin, jump, arms up with the flame |
| 17 | 40.4-44.2 | 3 | End card: the title block, "Play free in your browser · itch.io", GitHub Game Off 2026 | End card as is; music fades |

## Recording
Two ways: trailer mode in the editor (below, cleanest), or play it yourself:
- Record the **browser release build** in fullscreen (F11 in Chrome), so there is no debug text and no editor frame.
- Windows: **Win+Alt+R** starts and stops Xbox Game Bar recording (clips go to Videos\Captures). OBS Studio works too.
- Record a full run of each level, played well; the shots are cut from those. Game sound on or off: the trailer uses
  the music track, so mute the clips in the editor.

### Trailer mode (autoplay, editor only)
The runner plays every level by itself, each answer on its beat, so the footage is clean. Editor only: it is never
on in a build.
1. **Shift+Shift > Trailer Mode (autoplay)** (a tick shows it is on).
2. Press Play (from the Menu or Run scene). Level 1 starts at once; each level goes on to the next after 4 s of
   results; level 6 stays on its results. A fail (should not happen) retries after 1 s. Debug text is hidden (F1
   still shows it). Best scores and cleared levels are not saved while it is on.
3. Untick the menu item to play normally again.

What the bot does: it answers every obstacle on the beat (PERFECT). A lane move just before a phase lands 0.1 s
early (GOOD), and a lane obstacle the runner is already clear of scores OK (the game's own rule). Checked in a
simulation of all six levels at 60, 20 and 8 fps (AutoPlanTests): each key press is given at its exact planned time, so a slow
editor while recording does not change the result.

### Unity Recorder (free package)
1. Window > Package Manager > Unity Registry > **Recorder** > Install.
2. Game view: set the size to **1920x1080**.
3. Window > General > Recorder > Recorder Window. Add Recorder > **Movie**. Format MP4 (H.264), output 1920x1080
   (Game View), quality High. Untick audio (the trailer uses the music track).
4. Recording Mode **Manual**; Playback **Variable**, max 60 fps. (Constant slows game time to the capture rate, but
   this game follows the audio clock, so constant-rate footage could skip.)
5. Start Recording (it enters Play mode), let the levels run, Stop Recording. The file is in the project's
   Recordings folder (outside Assets).

## Editing (Clipchamp, built into Windows 11)
1. New video, 16:9. Import the animatic, the cards, the track and the recordings.
2. Put the animatic on the timeline as the guide. Its audio is already the trimmed track; use it as the music.
3. Place each recording over its placeholder, trimmed to the shot's length; put the caption PNG above it.
4. Delete the animatic video layer when every shot is covered (keep its audio), export 1080p.
