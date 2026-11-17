# Scope lock (9 Nov – 1 Dec 2026)

This is the contract for the jam. Anything not under "In scope" goes to "Post-jam" and is not
discussed again before 2 Dec.

## In scope
- Two-key input with the 7 gestures
- 3-lane auto-runner with beat-synced obstacles
- 6 hand-authored levels (4 mandatory, 2 cuttable)
- Menu, level select, results screen, pause, restart
- Local best scores
- 6 palettes, one modular track kit, post-processing
- Music, gesture SFX, UI SFX
- WebGL build on itch.io, Windows zip as backup
- itch page with screenshots, GIF and a 30–45 s video

## Out of scope
- Multiplayer of any kind, online leaderboards, accounts
- Procedural or endless mode
- Story, dialogue, characters with faces
- Gamepad, touch or mobile builds
- Custom level sharing
- Localisation
- Achievements, unlockables, cosmetics

## Cut order when behind (cut the top item first)
1. Level 6
2. Level 5 (the Alternate gesture goes with it)
3. Juice list beyond hit-stop and particles
4. Per-level music variety (reuse 3 tracks across 4 levels)
5. Windows build (WebGL only)
6. Level 4's ghost layer (phase becomes a simple invulnerability dash)

Below item 6 the game no longer interprets the theme well; if that line is reached, submit what exists.

Rule: any task more than two days late gets cut in this order rather than caught up.

## Decisions
- Day 17: roadside scenery (marker posts, distant blocks) added to the track kit at ELIA's request, for depth.
- Day 13 (M1, early): keep all 7 gestures (ELIA). Revisit only if the all-gestures test level shows a real problem.

## Post-jam
- Global leaderboard
- Mobile version (ELIA, day 27): two on-screen buttons, one per thumb, landscape. Out of scope for the jam ("touch or
  mobile builds"). Notes: input maps cleanly (each button = one Shift key, the classifier is unchanged); needs touch
  input, a landscape lock, UI sized for phones, a performance pass for phone GPUs, and timing calibration (touch
  screens add 30-80 ms). Browser on phones first, app stores later.
