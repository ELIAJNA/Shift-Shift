using System;
using System.Collections.Generic;
using System.IO;
using ShiftShift.Core;
using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Shift+Shift > Prepare Music (audio pass, day 20). The six tracks ELIA chose (Tricks &amp; Traps, CC0) are full
    /// songs of 2-7 minutes: everything in a Resources folder goes into the build, so as they are they would blow the
    /// 20 MB budget. This tool:
    /// 1. moves the source WAVs out of Resources to Assets/_Game/Audio/Source (kept for later edits, not built),
    /// 2. writes one trimmed clip per level into Resources (music_01 .. music_06): from 0.05 s before the track's
    ///    first downbeat (the level's offsetMs = 50) to 8 beats after the level's last beat, with a 10 ms fade-in and
    ///    a 2 s fade-out,
    /// 3. sets the clips to compressed (Vorbis) on import.
    /// Beat positions and tempos were measured from the files (Docs/LOG.md, 2026-11-16). Re-run it after changing a
    /// level's length: it reads lengthBeats from the level file. Editor only (System.IO is not used at runtime).
    /// </summary>
    public static partial class GreyboxSetup
    {
        private const string MusicSourceDir = "Assets/_Game/Audio/Source";
        private const string MusicOutDir = "Assets/_Game/Audio/Resources";
        private const double MusicLeadSeconds = 0.05; // = offsetMs 50 in the level files
        private const double MusicFadeOutSeconds = 2.0;

        private struct MusicSpec
        {
            public string Source, Clip, Level;
            public double Bpm, FirstBeatSeconds;
        }

        private static readonly MusicSpec[] MusicSpecs =
        {
            new MusicSpec { Source = "7_t.o.e.wav", Clip = "music_01", Level = "level01", Bpm = 120, FirstBeatSeconds = 0.4209 },
            new MusicSpec { Source = "5_flying_temple.wav", Clip = "music_02", Level = "level02", Bpm = 128, FirstBeatSeconds = 1.8047 },
            new MusicSpec { Source = "1_ascend.wav", Clip = "music_03", Level = "level03", Bpm = 130, FirstBeatSeconds = 1.7731 },
            new MusicSpec { Source = "4_the_lab.wav", Clip = "music_04", Level = "level04", Bpm = 144, FirstBeatSeconds = 1.5972 },
            new MusicSpec { Source = "9_vessel_of_eternity.wav", Clip = "music_05", Level = "level05", Bpm = 150, FirstBeatSeconds = 0.3267 },
            new MusicSpec { Source = "6_celebration_and_fear.wav", Clip = "music_06", Level = "level06", Bpm = 190, FirstBeatSeconds = 1.1921 },
        };

        [MenuItem("Shift+Shift/Prepare Music")]
        public static void PrepareMusic()
        {
            var log = new List<string>();
            EnsureFolder("Assets/_Game", "Audio");
            EnsureFolder("Assets/_Game/Audio", "Source");
            EnsureFolder("Assets/_Game/Audio", "Resources");

            foreach (MusicSpec m in MusicSpecs)
            {
                // 1. Source out of Resources (keeps its .meta).
                string inResources = $"{MusicOutDir}/{m.Source}", source = $"{MusicSourceDir}/{m.Source}";
                if (File.Exists(inResources) && !File.Exists(source))
                {
                    string err = AssetDatabase.MoveAsset(inResources, source);
                    if (!string.IsNullOrEmpty(err)) { Debug.LogError($"[Music] Could not move {m.Source}: {err}"); continue; }
                    log.Add($"Moved {m.Source} to {MusicSourceDir}");
                }
                if (!File.Exists(source)) { log.Add($"MISSING {source}: copy the track there and run this again"); continue; }

                // 2. The level decides how much music it needs.
                var levelJson = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/_Game/Levels/{m.Level}.json");
                LevelData level = levelJson != null ? JsonUtility.FromJson<LevelData>(levelJson.text) : null;
                if (level == null) { log.Add($"MISSING level {m.Level}"); continue; }
                if (Math.Abs(level.bpm - m.Bpm) > 0.01)
                    Debug.LogWarning($"[Music] {m.Level} is {level.bpm} BPM but its track is {m.Bpm} BPM.");
                double start = Math.Max(0, m.FirstBeatSeconds - MusicLeadSeconds);
                double length = MusicLeadSeconds + (level.lengthBeats + 8) * 60.0 / m.Bpm;

                string outPath = $"{MusicOutDir}/{m.Clip}.wav";
                try
                {
                    double got = TrimWav(source, outPath, start, length);
                    AssetDatabase.ImportAsset(outPath);
                    SetMusicImport(outPath);
                    log.Add($"{m.Clip}.wav for {m.Level}: {got:0.0} s from {m.Source} ({m.Bpm} BPM)");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Music] {m.Source}: {e.Message}");
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Music] Done.\n- " + string.Join("\n- ", log));
        }

        private static void SetMusicImport(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.5f;
            importer.defaultSampleSettings = s;
            importer.loadInBackground = false; // loaded before the level starts, so PlayScheduled is exact
            importer.SaveAndReimport();
        }

        /// <summary>Copies [start, start+length) seconds of a 16-bit PCM WAV, with a short fade-in and a fade-out. Returns the seconds written.</summary>
        private static double TrimWav(string source, string dest, double start, double length)
        {
            byte[] wav = File.ReadAllBytes(source);
            if (wav.Length < 12 || Ascii(wav, 0) != "RIFF" || Ascii(wav, 8) != "WAVE") throw new Exception("not a WAV file");
            int channels = 0, rate = 0, bits = 0, dataAt = -1, dataLen = 0;
            for (int at = 12; at + 8 <= wav.Length;)
            {
                string id = Ascii(wav, at);
                int size = BitConverter.ToInt32(wav, at + 4);
                if (id == "fmt ")
                {
                    if (BitConverter.ToInt16(wav, at + 8) != 1) throw new Exception("only plain PCM WAV is supported");
                    channels = BitConverter.ToInt16(wav, at + 10);
                    rate = BitConverter.ToInt32(wav, at + 12);
                    bits = BitConverter.ToInt16(wav, at + 22);
                }
                else if (id == "data") { dataAt = at + 8; dataLen = Math.Min(size, wav.Length - at - 8); break; }
                at += 8 + size + (size & 1);
            }
            if (dataAt < 0 || bits != 16 || channels < 1) throw new Exception("needs 16-bit PCM with a data chunk");

            int frame = channels * 2, totalFrames = dataLen / frame;
            int first = (int)Math.Round(start * rate);
            int count = Math.Min((int)Math.Round(length * rate), totalFrames - first);
            if (count <= 0) throw new Exception("the track is too short for this level");
            int fadeIn = (int)(0.01 * rate), fadeOut = Math.Min(count, (int)(MusicFadeOutSeconds * rate));

            var pcm = new byte[count * frame];
            for (int f = 0; f < count; f++)
            {
                double gain = 1.0;
                if (f < fadeIn) gain = (double)f / fadeIn;
                if (f >= count - fadeOut) gain = Math.Min(gain, (double)(count - f) / fadeOut);
                for (int c = 0; c < channels; c++)
                {
                    int src = dataAt + (first + f) * frame + c * 2;
                    short v = (short)Math.Round(BitConverter.ToInt16(wav, src) * gain);
                    pcm[f * frame + c * 2] = (byte)(v & 0xFF);
                    pcm[f * frame + c * 2 + 1] = (byte)((v >> 8) & 0xFF);
                }
            }

            using (var fs = new FileStream(dest, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                w.Write(36 + pcm.Length);
                w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)channels);
                w.Write(rate);
                w.Write(rate * frame);
                w.Write((short)frame);
                w.Write((short)16);
                w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                w.Write(pcm.Length);
                w.Write(pcm);
            }
            return (double)count / rate;
        }

        private static string Ascii(byte[] b, int at) => System.Text.Encoding.ASCII.GetString(b, at, 4);
    }
}
