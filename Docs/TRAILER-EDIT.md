# Trailer edit: brief for the Claude session on ELIA's PC

Read this file, Docs/TRAILER.md (the script) and CLAUDE.md first. Make the 44 s trailer from ELIA's trailer-mode
recordings. Work only inside `Recordings/` (ignored by git: never commit videos). Output:
`Recordings/trailer/shiftshift_trailer.mp4`, 1920x1080, 30 fps, H.264 (yuv420p, CRF 18), AAC 192k.

## Inputs
- Footage: the Unity Recorder MP4s in `Recordings/` (levels 1-6 played by trailer mode; debug text hidden).
  Each file starts with the count-in. Look at them before cutting (ffprobe for length; extract frames to check).
- Music: `Assets/_Game/Audio/Source/6_celebration_and_fear.wav`, from **25.19 s** for **44.21 s**, 2 s fade-out at the
  end. One bar = 1.263 s (190 BPM, 4/4); the drop is at 10.1 s into the trailer.
- Cards: `python tools/trailer/make_cards.py --out Recordings/trailer/cards` (needs `pip install pillow`; uses Arial
  Bold on Windows unless Inter Display is passed with `--fonts`). `cards.json` lists each card's file, start, length
  and the footage it goes over. Captions are transparent overlays; black cards and the end card replace the picture.
- Game audio from the recordings: not used (the music track replaces it).

## Shot timing (all from cards.json; cut exactly on these times, they are bar lines)
Shots 1 and 4 are black cards; shot 15 is the end card. For every other shot pick footage that matches the
description, so the key moment (the dodge, the slide under, the gap, the phase, the smash) lands on or just after a
beat in the shot. The game obstacles are on the level's own beat, not the trailer music's, so line the moment up
near the start of each 1-bar shot or the end of the smash shot rather than stretching anything. No speed changes.
Shots 11, 13, 14: 1-bar cuts (4 per shot). Shot 12: two 2-bar cuts showing PERFECT pop-ups and the multiplier.

## Finding moments
Level charts are in `Assets/_Game/Levels/level0N.json` (events by beat; types: bar, wide, low, gate, gap, hurdle,
ghost, ghostbar, ghostwide, wall). In a recording, beat 0 is about 4 beats after the count-in starts: at BPM b, beat n
is roughly at (count-in start) + (n + 4) * 60 / b seconds. Confirm by looking at frames.
BPM: level01 120, level02 128, level03 130, level04 144, level05 150, level06 190.

## Steps
1. Install what is missing: ffmpeg (`winget install Gyan.FFmpeg`), Pillow.
2. Make the cards. List the recordings and their lengths.
3. Write a cut list (shot, source file, in-point, length) to `Recordings/trailer/cutlist.md`; show ELIA frames of
   each pick (a contact sheet) before rendering.
4. Render each shot at 1920x1080 30 fps, overlay its caption card, concatenate, add the music, fade out.
5. Check: length 44.2 s, cuts on bar lines, no debug text, captions readable. Send ELIA the file path.
