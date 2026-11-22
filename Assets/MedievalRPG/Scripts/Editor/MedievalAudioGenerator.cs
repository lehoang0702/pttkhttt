using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MedievalRPG.Audio;

namespace MedievalRPG.EditorTools
{
    /// <summary>
    /// Sinh bộ âm thanh WAV 16-bit phong cách Trung Cổ / Pixel RPG (BGM, Ambient, UI, Quiz, Combat)
    /// và tự động đóng gói thành Resources/MedievalAudioBank.asset để mọi Scene chạy có âm thanh ngay.
    /// </summary>
    public static class MedievalAudioGenerator
    {
        private const int SampleRate = 44100;
        private const string AudioRoot = "Assets/MedievalRPG/Audio";
        private const string MusicDir = AudioRoot + "/Music";
        private const string SfxDir = AudioRoot + "/SFX";
        private const string ResourcesDir = "Assets/MedievalRPG/Resources";
        private const string BankAssetPath = ResourcesDir + "/MedievalAudioBank.asset";

        [MenuItem("MedievalRPG/Generate Procedural Audio Clips")]
        public static AudioBank GenerateAllAudioAndBank()
        {
            EnsureFolder("Assets/MedievalRPG", "Audio");
            EnsureFolder(AudioRoot, "Music");
            EnsureFolder(AudioRoot, "SFX");
            EnsureFolder("Assets/MedievalRPG", "Resources");

            // 1. BGM & Ambient
            WriteWav($"{MusicDir}/BGM_MainMenu.wav", SynthMenuMusic());
            WriteWav($"{MusicDir}/BGM_Battle.wav", SynthBattleMusic());
            WriteWav($"{MusicDir}/BGM_Boss.wav", SynthBossMusic());
            WriteWav($"{MusicDir}/AMB_ForestWind.wav", SynthForestAmbient());

            // 2. UI & Quiz SFX
            WriteWav($"{SfxDir}/SFX_UIClick.wav", SynthUIClick());
            WriteWav($"{SfxDir}/SFX_UIHover.wav", SynthUIHover());
            WriteWav($"{SfxDir}/SFX_QuizOpen.wav", SynthQuizOpen());
            WriteWav($"{SfxDir}/SFX_QuizCorrect.wav", SynthQuizCorrect());
            WriteWav($"{SfxDir}/SFX_QuizWrong.wav", SynthQuizWrong());

            // 3. Combat SFX
            WriteWav($"{SfxDir}/SFX_SwordSlash.wav", SynthSwordSlash());
            WriteWav($"{SfxDir}/SFX_BowShoot.wav", SynthBowShoot());
            WriteWav($"{SfxDir}/SFX_MagicCast.wav", SynthMagicCast());
            WriteWav($"{SfxDir}/SFX_FireballHit.wav", SynthFireballHit());
            WriteWav($"{SfxDir}/SFX_HealChime.wav", SynthHealChime());
            WriteWav($"{SfxDir}/SFX_ShieldDefend.wav", SynthShieldDefend());
            WriteWav($"{SfxDir}/SFX_CritHit.wav", SynthCritHit());
            WriteWav($"{SfxDir}/SFX_UnitHit.wav", SynthUnitHit());
            WriteWav($"{SfxDir}/SFX_UnitDeath.wav", SynthUnitDeath());

            // 4. Victory / Defeat
            WriteWav($"{SfxDir}/SFX_VictoryFanfare.wav", SynthVictoryFanfare());
            WriteWav($"{SfxDir}/SFX_DefeatSting.wav", SynthDefeatSting());

            // 5. Loot & Equipment SFX
            WriteWav($"{SfxDir}/SFX_LootDrop.wav", SynthLootDrop());
            WriteWav($"{SfxDir}/SFX_LootLegendary.wav", SynthLootLegendary());
            WriteWav($"{SfxDir}/SFX_LootPickup.wav", SynthLootPickup());

            // 6. Economy & Progression SFX
            WriteWav($"{SfxDir}/SFX_CoinReward.wav", SynthCoinReward());
            WriteWav($"{SfxDir}/SFX_ForgeUpgrade.wav", SynthForgeUpgrade());
            WriteWav($"{SfxDir}/SFX_LevelUp.wav", SynthLevelUp());

            AssetDatabase.Refresh();

            ConfigureImporter($"{MusicDir}/BGM_MainMenu.wav", AudioClipLoadType.Streaming, true);
            ConfigureImporter($"{MusicDir}/BGM_Battle.wav", AudioClipLoadType.Streaming, true);
            ConfigureImporter($"{MusicDir}/BGM_Boss.wav", AudioClipLoadType.Streaming, true);
            ConfigureImporter($"{MusicDir}/AMB_ForestWind.wav", AudioClipLoadType.Streaming, true);

            string[] sfxFiles = Directory.GetFiles(SfxDir, "*.wav");
            foreach (string path in sfxFiles)
            {
                ConfigureImporter(path.Replace('\\', '/'), AudioClipLoadType.DecompressOnLoad, false);
            }

            AudioBank bank = AssetDatabase.LoadAssetAtPath<AudioBank>(BankAssetPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<AudioBank>();
                AssetDatabase.CreateAsset(bank, BankAssetPath);
            }

            bank.bgmMainMenu = LoadClip($"{MusicDir}/BGM_MainMenu.wav");
            bank.bgmBattle = LoadClip($"{MusicDir}/BGM_Battle.wav");
            bank.bgmBoss = LoadClip($"{MusicDir}/BGM_Boss.wav");
            bank.ambientForest = LoadClip($"{MusicDir}/AMB_ForestWind.wav");

            bank.uiClick = LoadClip($"{SfxDir}/SFX_UIClick.wav");
            bank.uiHover = LoadClip($"{SfxDir}/SFX_UIHover.wav");
            bank.quizOpen = LoadClip($"{SfxDir}/SFX_QuizOpen.wav");
            bank.quizCorrect = LoadClip($"{SfxDir}/SFX_QuizCorrect.wav");
            bank.quizWrong = LoadClip($"{SfxDir}/SFX_QuizWrong.wav");

            bank.swordSlash = LoadClip($"{SfxDir}/SFX_SwordSlash.wav");
            bank.bowShoot = LoadClip($"{SfxDir}/SFX_BowShoot.wav");
            bank.magicCast = LoadClip($"{SfxDir}/SFX_MagicCast.wav");
            bank.fireballHit = LoadClip($"{SfxDir}/SFX_FireballHit.wav");
            bank.healChime = LoadClip($"{SfxDir}/SFX_HealChime.wav");
            bank.shieldDefend = LoadClip($"{SfxDir}/SFX_ShieldDefend.wav");
            bank.critHit = LoadClip($"{SfxDir}/SFX_CritHit.wav");
            bank.unitHit = LoadClip($"{SfxDir}/SFX_UnitHit.wav");
            bank.unitDeath = LoadClip($"{SfxDir}/SFX_UnitDeath.wav");

            bank.victoryFanfare = LoadClip($"{SfxDir}/SFX_VictoryFanfare.wav");
            bank.defeatSting = LoadClip($"{SfxDir}/SFX_DefeatSting.wav");

            bank.lootDrop = LoadClip($"{SfxDir}/SFX_LootDrop.wav");
            bank.lootLegendary = LoadClip($"{SfxDir}/SFX_LootLegendary.wav");
            bank.lootPickup = LoadClip($"{SfxDir}/SFX_LootPickup.wav");

            bank.coinReward = LoadClip($"{SfxDir}/SFX_CoinReward.wav");
            bank.forgeUpgrade = LoadClip($"{SfxDir}/SFX_ForgeUpgrade.wav");
            bank.levelUp = LoadClip($"{SfxDir}/SFX_LevelUp.wav");

            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MedievalAudioGenerator] Đã sinh 26 tệp âm thanh WAV và cấu hình Resources/MedievalAudioBank.asset!");
            return bank;
        }

        private static AudioClip LoadClip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        private static void EnsureFolder(string parent, string folderName)
        {
            string combined = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(combined))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void ConfigureImporter(string path, AudioClipLoadType loadType, bool isLongMusic)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                importer.forceToMono = true;
                importer.loadInBackground = isLongMusic;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = loadType;
                settings.compressionFormat = isLongMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                settings.quality = isLongMusic ? 0.65f : 0.85f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        // =====================================================================
        // SYNTHESIS RECIPES
        // =====================================================================
        private static float[] SynthMenuMusic()
        {
            // D Dorian gentle medieval lute loop (~8.0s at 120 BPM = 16 quarter-note beats, 32 eighth notes)
            float stepSec = 0.25f;
            int[] notes =
            {
                62, 69, 65, 69,  64, 69, 67, 69,
                60, 67, 64, 67,  62, 65, 69, 72,
                62, 69, 65, 74,  72, 69, 67, 65,
                64, 67, 71, 67,  62, 65, 69, 62
            };
            int[] bass =
            {
                38, 0, 0, 0,  45, 0, 0, 0,
                36, 0, 0, 0,  38, 0, 41, 0,
                38, 0, 0, 0,  43, 0, 0, 0,
                40, 0, 43, 0, 38, 0, 0, 0
            };

            float totalDur = notes.Length * stepSec;
            float[] buf = new float[Mathf.RoundToInt(totalDur * SampleRate)];
            for (int i = 0; i < notes.Length; i++)
            {
                AddPluck(buf, i * stepSec, MidiToHz(notes[i]), 0.42f, 0.26f);
                if (bass[i] > 0) AddWarmPad(buf, i * stepSec, MidiToHz(bass[i]), 0.85f, 0.22f);
            }
            ApplyLoopFade(buf, 0.02f);
            NormalizePeak(buf, 0.72f);
            return buf;
        }

        private static float[] SynthBattleMusic()
        {
            // Energetic D minor RPG battle loop (~7.2s)
            float stepSec = 0.20f;
            int[] melody =
            {
                62, 0, 65, 69,  67, 65, 64, 62,
                60, 64, 67, 0,  65, 64, 62, 57,
                62, 65, 69, 74, 72, 69, 67, 69,
                64, 67, 70, 69, 67, 65, 64, 62,
                57, 62, 64, 65
            };
            int[] bass =
            {
                38, 38, 45, 38,  43, 43, 41, 40,
                36, 36, 43, 36,  41, 40, 38, 33,
                38, 38, 45, 38,  48, 45, 43, 45,
                40, 40, 46, 45,  43, 41, 40, 38,
                33, 38, 40, 41
            };

            float totalDur = melody.Length * stepSec;
            float[] buf = new float[Mathf.RoundToInt(totalDur * SampleRate)];
            for (int i = 0; i < melody.Length; i++)
            {
                if (melody[i] > 0) AddPluck(buf, i * stepSec, MidiToHz(melody[i]), 0.28f, 0.24f);
                if (bass[i] > 0) AddBassPulse(buf, i * stepSec, MidiToHz(bass[i]), 0.18f, 0.22f);
                if (i % 2 == 0) AddSoftDrum(buf, i * stepSec, i % 4 == 0 ? 110f : 180f, 0.08f, i % 4 == 0 ? 0.16f : 0.08f);
            }
            ApplyLoopFade(buf, 0.015f);
            NormalizePeak(buf, 0.74f);
            return buf;
        }

        private static float[] SynthBossMusic()
        {
            // Intense E minor / Phrygian boss loop (~6.4s)
            float stepSec = 0.16f;
            int[] melody =
            {
                64, 65, 64, 67,  64, 71, 69, 67,
                65, 64, 62, 64,  60, 62, 64, 59,
                64, 67, 71, 72,  71, 69, 67, 65,
                64, 67, 65, 64,  62, 60, 59, 64,
                64, 71, 69, 67,  65, 64, 62, 59
            };
            float totalDur = melody.Length * stepSec;
            float[] buf = new float[Mathf.RoundToInt(totalDur * SampleRate)];
            for (int i = 0; i < melody.Length; i++)
            {
                AddPluck(buf, i * stepSec, MidiToHz(melody[i]), 0.24f, 0.26f);
                int bassNote = (i % 8 < 4) ? 40 : ((i % 16 < 12) ? 41 : 38);
                AddBassPulse(buf, i * stepSec, MidiToHz(bassNote), 0.15f, 0.26f);
                AddSoftDrum(buf, i * stepSec, (i % 4 == 0) ? 95f : 165f, 0.08f, (i % 2 == 0) ? 0.18f : 0.08f);
            }
            ApplyLoopFade(buf, 0.015f);
            NormalizePeak(buf, 0.76f);
            return buf;
        }

        private static float[] SynthForestAmbient()
        {
            // 5.0s gentle brown noise forest breeze loop
            float dur = 5.0f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            System.Random rng = new System.Random(1024);
            float brown = 0f;
            for (int i = 0; i < count; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                brown = Mathf.Clamp((brown + 0.02f * white) / 1.02f, -1f, 1f);
                float t = (float)i / SampleRate;
                float windMod = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t / dur);
                buf[i] = brown * windMod * 0.65f;
            }
            // Crossfade wrap edges for seamless loop
            int fadeSamples = SampleRate / 4;
            for (int i = 0; i < fadeSamples; i++)
            {
                float k = (float)i / fadeSamples;
                int tail = count - fadeSamples + i;
                float blended = Mathf.Lerp(buf[tail], buf[i], k);
                buf[i] = blended;
                buf[tail] = Mathf.Lerp(buf[tail], buf[i], 1f - k);
            }
            NormalizePeak(buf, 0.35f);
            return buf;
        }

        private static float[] SynthUIClick()
        {
            float dur = 0.055f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            System.Random rng = new System.Random(42);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float env = Mathf.Exp(-u * 9f);
                float freq = Mathf.Lerp(780f, 340f, u);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-u * 18f);
                buf[i] = (tone * 0.65f + noise * 0.35f) * env;
            }
            ApplyEdgeFades(buf, 0.003f);
            NormalizePeak(buf, 0.65f);
            return buf;
        }

        private static float[] SynthUIHover()
        {
            float dur = 0.028f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float env = Mathf.Sin(u * Mathf.PI);
                buf[i] = Mathf.Sin(2f * Mathf.PI * 980f * t) * env * 0.4f;
            }
            ApplyEdgeFades(buf, 0.003f);
            NormalizePeak(buf, 0.38f);
            return buf;
        }

        private static float[] SynthQuizOpen()
        {
            float dur = 0.28f;
            float[] buf = new float[Mathf.RoundToInt(dur * SampleRate)];
            AddPluck(buf, 0f, 523.25f, 0.22f, 0.55f);    // C5
            AddPluck(buf, 0.07f, 659.25f, 0.21f, 0.60f); // E5
            ApplyEdgeFades(buf, 0.005f);
            NormalizePeak(buf, 0.68f);
            return buf;
        }

        private static float[] SynthQuizCorrect()
        {
            float dur = 0.48f;
            float[] buf = new float[Mathf.RoundToInt(dur * SampleRate)];
            AddPluck(buf, 0.00f, 523.25f, 0.22f, 0.50f); // C5
            AddPluck(buf, 0.07f, 659.25f, 0.24f, 0.55f); // E5
            AddPluck(buf, 0.14f, 783.99f, 0.28f, 0.60f); // G5
            AddPluck(buf, 0.21f, 1046.5f, 0.30f, 0.70f); // C6
            ApplyEdgeFades(buf, 0.005f);
            NormalizePeak(buf, 0.80f);
            return buf;
        }

        private static float[] SynthQuizWrong()
        {
            float dur = 0.38f;
            float[] buf = new float[Mathf.RoundToInt(dur * SampleRate)];
            AddWarmSaw(buf, 0.00f, 220f, 0.16f, 0.50f);
            AddWarmSaw(buf, 0.13f, 174.6f, 0.24f, 0.55f);
            ApplyEdgeFades(buf, 0.005f);
            NormalizePeak(buf, 0.72f);
            return buf;
        }

        private static float[] SynthSwordSlash()
        {
            float dur = 0.24f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            System.Random rng = new System.Random(13);
            float prev = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                float highpass = white - prev * 0.82f;
                prev = white;
                float whooshEnv = Mathf.Sin(u * Mathf.PI) * Mathf.Exp(-u * 3.5f);
                float impact = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(320f, 95f, u) * t) * Mathf.Exp(-u * 8f);
                buf[i] = highpass * whooshEnv * 0.55f + impact * 0.65f;
            }
            ApplyEdgeFades(buf, 0.004f);
            NormalizePeak(buf, 0.82f);
            return buf;
        }

        private static float[] SynthBowShoot()
        {
            float dur = 0.22f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            System.Random rng = new System.Random(77);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float twang = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(290f, 180f, u) * t) * Mathf.Exp(-u * 11f);
                float whistle = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1650f, 740f, u) * t) * Mathf.Exp(-u * 7f) * 0.45f;
                float air = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-u * 9f) * 0.25f;
                buf[i] = twang * 0.7f + whistle + air;
            }
            ApplyEdgeFades(buf, 0.004f);
            NormalizePeak(buf, 0.78f);
            return buf;
        }

        private static float[] SynthMagicCast()
        {
            float dur = 0.34f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float env = Mathf.Sin(u * Mathf.PI) * Mathf.Exp(-u * 1.8f);
                float f1 = Mathf.Lerp(420f, 960f, u);
                float f2 = Mathf.Lerp(630f, 1440f, u);
                float vib = 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 22f * t);
                buf[i] = (Mathf.Sin(2f * Mathf.PI * f1 * vib * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * f2 * t)) * env;
            }
            ApplyEdgeFades(buf, 0.005f);
            NormalizePeak(buf, 0.76f);
            return buf;
        }

        private static float[] SynthFireballHit()
        {
            float dur = 0.44f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            System.Random rng = new System.Random(99);
            float lp = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp = lp * 0.88f + white * 0.12f;
                float boom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 45f, u) * t) * Mathf.Exp(-u * 6f);
                float roar = lp * Mathf.Exp(-u * 3.5f) * 1.8f;
                buf[i] = boom * 0.65f + roar * 0.6f;
            }
            ApplyEdgeFades(buf, 0.005f);
            NormalizePeak(buf, 0.86f);
            return buf;
        }

        private static float[] SynthHealChime()
        {
            float dur = 0.55f;
            float[] buf = new float[Mathf.RoundToInt(dur * SampleRate)];
            AddPluck(buf, 0.00f, 587.33f, 0.34f, 0.5f); // D5
            AddPluck(buf, 0.09f, 739.99f, 0.34f, 0.5f); // F#5
            AddPluck(buf, 0.18f, 880.00f, 0.36f, 0.6f); // A5
            AddPluck(buf, 0.27f, 1174.66f, 0.34f, 0.6f); // D6
            ApplyEdgeFades(buf, 0.005f);
            NormalizePeak(buf, 0.75f);
            return buf;
        }

        private static float[] SynthShieldDefend()
        {
            float dur = 0.32f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float env = Mathf.Exp(-u * 7.5f);
                float clang =
                    Mathf.Sin(2f * Mathf.PI * 460f * t) * 0.55f +
                    Mathf.Sin(2f * Mathf.PI * 735f * t) * 0.35f +
                    Mathf.Sin(2f * Mathf.PI * 1180f * t) * 0.20f;
                buf[i] = clang * env;
            }
            ApplyEdgeFades(buf, 0.003f);
            NormalizePeak(buf, 0.78f);
            return buf;
        }

        private static float[] SynthCritHit()
        {
            float dur = 0.30f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            System.Random rng = new System.Random(555);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float punch = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 55f, u) * t) * Mathf.Exp(-u * 7f);
                float ring = Mathf.Sin(2f * Mathf.PI * 1120f * t) * Mathf.Exp(-u * 10f) * 0.4f;
                float crunch = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-u * 14f) * 0.5f;
                buf[i] = punch * 0.8f + ring + crunch;
            }
            ApplyEdgeFades(buf, 0.003f);
            NormalizePeak(buf, 0.88f);
            return buf;
        }

        private static float[] SynthUnitHit()
        {
            float dur = 0.16f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            System.Random rng = new System.Random(31);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 70f, u) * t) * Mathf.Exp(-u * 9f);
                float hitNoise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-u * 16f) * 0.35f;
                buf[i] = thud * 0.75f + hitNoise;
            }
            ApplyEdgeFades(buf, 0.003f);
            NormalizePeak(buf, 0.76f);
            return buf;
        }

        private static float[] SynthUnitDeath()
        {
            float dur = 0.45f;
            int count = Mathf.RoundToInt(dur * SampleRate);
            float[] buf = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float freq = Mathf.Lerp(240f, 58f, u);
                float env = Mathf.Exp(-u * 4.5f);
                buf[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t)) * env;
            }
            ApplyEdgeFades(buf, 0.005f);
            NormalizePeak(buf, 0.78f);
            return buf;
        }

        private static float[] SynthVictoryFanfare()
        {
            float dur = 1.35f;
            float[] buf = new float[Mathf.RoundToInt(dur * SampleRate)];
            AddPluck(buf, 0.00f, 587.33f, 0.25f, 0.55f); // D5
            AddPluck(buf, 0.16f, 587.33f, 0.20f, 0.50f); // D5
            AddPluck(buf, 0.30f, 739.99f, 0.25f, 0.58f); // F#5
            AddPluck(buf, 0.46f, 880.00f, 0.40f, 0.65f); // A5
            AddPluck(buf, 0.70f, 1174.66f, 0.62f, 0.72f); // D6
            AddWarmPad(buf, 0.70f, 293.66f, 0.62f, 0.32f); // D4 harmony
            AddWarmPad(buf, 0.70f, 440.00f, 0.62f, 0.26f); // A4 harmony
            ApplyEdgeFades(buf, 0.006f);
            NormalizePeak(buf, 0.82f);
            return buf;
        }

        private static float[] SynthDefeatSting()
        {
            float dur = 1.25f;
            float[] buf = new float[Mathf.RoundToInt(dur * SampleRate)];
            AddWarmSaw(buf, 0.00f, 329.63f, 0.28f, 0.40f); // E4
            AddWarmSaw(buf, 0.28f, 293.66f, 0.28f, 0.40f); // D4
            AddWarmSaw(buf, 0.56f, 261.63f, 0.30f, 0.40f); // C4
            AddWarmSaw(buf, 0.84f, 220.00f, 0.40f, 0.45f); // A3
            ApplyEdgeFades(buf, 0.006f);
            NormalizePeak(buf, 0.72f);
            return buf;
        }

        // =====================================================================
        // DSP HELPERS
        // =====================================================================
        private static void AddPluck(float[] buf, float startSec, float freq, float durSec, float amp)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / len;
                float attack = Mathf.Min(1f, t / 0.008f);
                float decay = Mathf.Exp(-u * 4.5f);
                float sample =
                    0.65f * Mathf.Sin(2f * Mathf.PI * freq * t) +
                    0.25f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * Mathf.Exp(-u * 7f) +
                    0.10f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t) * Mathf.Exp(-u * 10f);
                buf[start + i] += sample * attack * decay * amp;
            }
        }

        private static void AddWarmPad(float[] buf, float startSec, float freq, float durSec, float amp)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / len;
                float env = Mathf.Sin(u * Mathf.PI);
                float sample = 0.7f * Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                buf[start + i] += sample * env * amp;
            }
        }

        private static void AddBassPulse(float[] buf, float startSec, float freq, float durSec, float amp)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / len;
                float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-u * 4.2f);
                float sample = 0.75f * Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                buf[start + i] += sample * env * amp;
            }
        }

        private static void AddWarmSaw(float[] buf, float startSec, float freq, float durSec, float amp)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / len;
                float env = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-u * 3.0f);
                float sample =
                    Mathf.Sin(2f * Mathf.PI * freq * t) +
                    0.5f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t) +
                    0.25f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t);
                buf[start + i] += sample * 0.55f * env * amp;
            }
        }

        private static void AddSoftDrum(float[] buf, float startSec, float startFreq, float durSec, float amp)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / len;
                float f = Mathf.Lerp(startFreq, 42f, u);
                buf[start + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-u * 8f) * amp;
            }
        }

        private static float MidiToHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        private static void ApplyEdgeFades(float[] buf, float fadeSec)
        {
            int n = Mathf.Min(buf.Length / 2, Mathf.RoundToInt(fadeSec * SampleRate));
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / Mathf.Max(1, n);
                buf[i] *= k;
                buf[buf.Length - 1 - i] *= k;
            }
        }

        private static void ApplyLoopFade(float[] buf, float fadeSec)
        {
            ApplyEdgeFades(buf, fadeSec);
        }

        private static float[] SynthLootDrop()
        {
            float duration = 0.34f;
            float[] buf = new float[(int)(SampleRate * duration)];
            float[] freqs = { 784f, 1174.66f };
            float[] starts = { 0f, 0.08f };
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;
                for (int n = 0; n < freqs.Length; n++)
                {
                    float lt = t - starts[n];
                    if (lt < 0f) continue;
                    float env = Mathf.Exp(-lt * 13f) * Mathf.Clamp01(lt / 0.005f);
                    s += (Mathf.Sin(2f * Mathf.PI * freqs[n] * lt) * 0.65f +
                          Mathf.Sin(2f * Mathf.PI * freqs[n] * 2f * lt) * 0.25f) * env;
                }
                buf[i] = s;
            }
            NormalizePeak(buf, 0.85f);
            return buf;
        }

        private static float[] SynthLootLegendary()
        {
            float duration = 0.95f;
            float[] buf = new float[(int)(SampleRate * duration)];
            float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f };
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;
                for (int n = 0; n < freqs.Length; n++)
                {
                    float lt = t - n * 0.075f;
                    if (lt < 0f) continue;
                    float env = Mathf.Exp(-lt * 4.5f) * Mathf.Clamp01(lt / 0.008f);
                    float shimmer = 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 14f * lt);
                    s += (Mathf.Sin(2f * Mathf.PI * freqs[n] * lt) * 0.6f +
                          Mathf.Sin(2f * Mathf.PI * freqs[n] * 2f * lt) * 0.28f +
                          Mathf.Sin(2f * Mathf.PI * freqs[n] * 3f * lt) * 0.12f) * env * shimmer;
                }
                buf[i] = s;
            }
            NormalizePeak(buf, 0.90f);
            return buf;
        }

        private static float[] SynthLootPickup()
        {
            float duration = 0.22f;
            float[] buf = new float[(int)(SampleRate * duration)];
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float f = Mathf.Lerp(587.33f, 1174.66f, t / duration);
                float env = Mathf.Exp(-t * 14f) * Mathf.Clamp01(t / 0.004f);
                buf[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.3f) * env;
            }
            NormalizePeak(buf, 0.82f);
            return buf;
        }

        private static float[] SynthCoinReward()
        {
            float duration = 0.28f;
            float[] buf = new float[(int)(SampleRate * duration)];
            float[] notes = { 987.77f, 1318.51f }; // B5 -> E6
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float lt = t - n * 0.065f;
                    if (lt < 0f) continue;
                    float env = Mathf.Exp(-lt * 16f) * Mathf.Clamp01(lt / 0.004f);
                    s += (Mathf.Sin(2f * Mathf.PI * notes[n] * lt) * 0.7f +
                          Mathf.Sin(2f * Mathf.PI * notes[n] * 2f * lt) * 0.3f) * env;
                }
                buf[i] = s;
            }
            NormalizePeak(buf, 0.84f);
            return buf;
        }

        private static float[] SynthForgeUpgrade()
        {
            float duration = 0.42f;
            float[] buf = new float[(int)(SampleRate * duration)];
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                // Tiếng búa đe kim loại sáng + ngân vang
                float env = Mathf.Exp(-t * 8.5f) * Mathf.Clamp01(t / 0.003f);
                float clang = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.55f +
                              Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * 0.30f +
                              Mathf.Sin(2f * Mathf.PI * 2489.0f * t) * 0.15f;
                buf[i] = clang * env;
            }
            NormalizePeak(buf, 0.86f);
            return buf;
        }

        private static float[] SynthLevelUp()
        {
            float duration = 0.75f;
            float[] buf = new float[(int)(SampleRate * duration)];
            float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5-E5-G5-C6
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;
                for (int n = 0; n < freqs.Length; n++)
                {
                    float lt = t - n * 0.085f;
                    if (lt < 0f) continue;
                    float env = Mathf.Exp(-lt * 5.2f) * Mathf.Clamp01(lt / 0.006f);
                    s += (Mathf.Sin(2f * Mathf.PI * freqs[n] * lt) * 0.65f +
                          Mathf.Sin(2f * Mathf.PI * freqs[n] * 2f * lt) * 0.35f) * env;
                }
                buf[i] = s;
            }
            NormalizePeak(buf, 0.88f);
            return buf;
        }

        private static void NormalizePeak(float[] buf, float targetPeak)
        {
            float maxAbs = 0.0001f;
            for (int i = 0; i < buf.Length; i++)
            {
                float a = Mathf.Abs(buf[i]);
                if (a > maxAbs) maxAbs = a;
            }
            float scale = targetPeak / maxAbs;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = Mathf.Clamp(buf[i] * scale, -1f, 1f);
            }
        }

        private static void WriteWav(string assetPath, float[] samples)
        {
            string fullPath = Path.GetFullPath(assetPath);
            using (FileStream fs = new FileStream(fullPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int dataBytes = samples.Length * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataBytes);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(SampleRate);
                bw.Write(SampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataBytes);

                for (int i = 0; i < samples.Length; i++)
                {
                    short pcm = (short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                    bw.Write(pcm);
                }
            }
        }
    }
}
