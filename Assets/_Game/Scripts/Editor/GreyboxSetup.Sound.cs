using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Audio pass (day 23), run by Shift+Shift > Set Up Visuals: one Kenney sound (CC0, Assets/_Game/Audio/SFX) per
    /// action, with a volume set against its measured loudness so it sits under the music. Change a choice here and
    /// run Set Up Visuals again.
    /// </summary>
    public static partial class GreyboxSetup
    {
        private const string SfxDir = "Assets/_Game/Audio/SFX";

        // field on SoundFx, file under SfxDir, volume
        private static readonly (string field, string file, float volume)[] RunSounds =
        {
            ("laneChange", "InterfaceSounds/tick_002.ogg", 0.35f),        // 0.02 s: crisp, plays a lot
            ("slide", "DigitalAudio/lowDown.ogg", 0.3f),                  // falling tone: down
            ("sprint", "DigitalAudio/highUp.ogg", 0.3f),                  // rising tone: up
            ("jump", "DigitalAudio/phaseJump2.ogg", 0.45f),
            ("phase", "SciFiSounds/forceField_000.ogg", 0.3f),
            ("charged", "DigitalAudio/powerUp2.ogg", 0.6f),
            ("wallSmash", "SciFiSounds/explosionCrunch_000.ogg", 0.6f),
            ("hit", "ImpactSounds/impactPunch_heavy_000.ogg", 0.8f),
            ("levelComplete", "InterfaceSounds/confirmation_004.ogg", 0.5f),
            ("countInTick", "InterfaceSounds/click_002.ogg", 0.5f),
            ("pause", "InterfaceSounds/minimize_001.ogg", 0.4f),
            ("resume", "InterfaceSounds/maximize_001.ogg", 0.4f),
        };

        private static readonly (string field, string file)[] MenuSounds =
        {
            ("moveSound", "InterfaceSounds/select_001.ogg"),
            ("openSound", "InterfaceSounds/confirmation_001.ogg"),
            ("backSound", "InterfaceSounds/back_002.ogg"),
        };

        private static AudioClip Sfx(string file)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDir}/{file}");
            if (clip == null) Debug.LogError($"[Sound] Missing {SfxDir}/{file}");
            return clip;
        }

        /// <summary>Run and LevelTest scenes: a "Sound" object with SoundFx, wired to the scene. Call with the scene open.</summary>
        private static void WireRunSounds(List<string> log, string label)
        {
            var runner = Object.FindAnyObjectByType<Runner>();
            var conductor = Object.FindAnyObjectByType<Conductor>();
            if (runner == null || conductor == null) return; // Greybox has no song

            var fx = Object.FindAnyObjectByType<SoundFx>();
            if (fx == null) fx = new GameObject("Sound").AddComponent<SoundFx>();
            Set(fx, "input", Object.FindAnyObjectByType<ShiftInput>());
            Set(fx, "runner", runner);
            Set(fx, "conductor", conductor);
            var flow = Object.FindAnyObjectByType<GameFlow>();
            if (flow != null) Set(fx, "flow", flow);

            var so = new SerializedObject(fx);
            foreach (var (field, file, volume) in RunSounds)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null) { Debug.LogError($"[Sound] SoundFx has no field '{field}'."); continue; }
                p.FindPropertyRelative("clip").objectReferenceValue = Sfx(file);
                p.FindPropertyRelative("volume").floatValue = volume;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"{label}: {RunSounds.Length} sound effects wired");
        }

        private static void WireMenuSounds(MainMenu menu)
        {
            var so = new SerializedObject(menu);
            foreach (var (field, file) in MenuSounds)
                so.FindProperty(field).objectReferenceValue = Sfx(file);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
