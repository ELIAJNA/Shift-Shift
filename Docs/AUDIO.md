# Audio (day 23)

Music: one Tricks & Traps track per level (Docs/LEVELS.md, "Music"), made by Shift+Shift > Prepare Music.
Sound effects: Kenney packs (CC0) in `Assets/_Game/Audio/SFX`; only the files the game uses go into the build.
The choices and volumes live in `Assets/_Game/Scripts/Editor/GreyboxSetup.Sound.cs`; Set Up Visuals applies them.

| Action | Sound | Volume |
|---|---|---|
| Lane change (each tap, also the charge taps) | InterfaceSounds/tick_002 | 0.35 |
| Slide starts | DigitalAudio/lowDown | 0.3 |
| Sprint starts | DigitalAudio/highUp | 0.3 |
| Jump | DigitalAudio/phaseJump2 | 0.45 |
| Phase starts | SciFiSounds/forceField_000 | 0.3 |
| Charge ready (next obstacle a red wall) | DigitalAudio/powerUp2 | 0.6 |
| Red wall smashed | SciFiSounds/explosionCrunch_000 | 0.6 |
| Hit (run over) | ImpactSounds/impactPunch_heavy_000 | 0.8 |
| Level complete | InterfaceSounds/confirmation_004 | 0.5 |
| Count-in, beats -4 to -1 | InterfaceSounds/click_002 (scheduled on the audio clock) | 0.5 |
| Pause / resume | InterfaceSounds/minimize_001 / maximize_001 | 0.4 |
| Menu: move / open / back | InterfaceSounds/select_001 / confirmation_001 / back_002 | 0.5 |

Gesture sounds play when the gesture is recognised (a tap on key release, a hold after 250 ms): they confirm the
move, they are not part of the timing. The count-in ticks are scheduled with PlayScheduled on the song's audio
clock, so they are exactly on the beat; a pause takes them back and reschedules the ones still to come.
