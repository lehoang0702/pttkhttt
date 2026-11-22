using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.Audio
{
    /// <summary>
    /// Quản lý âm thanh tập trung (BGM crossfade, Ambient loop, SFX pool, Ducking khi tạm dừng).
    /// Hỗ trợ AudioMixer (MusicVol / SFXVol) nếu đã gắn và fallback sang gain layer cho headless/WebGL.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string PrefMusicVolume = "MusicVolume";
        private const string PrefSfxVolume = "SFXVolume";
        private const string LegacyMasterVolume = "MasterVolume";

        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioBank audioBank;
        [SerializeField] private int sfxPoolSize = 8;
        [SerializeField] private float defaultMusicMaxVolume = 0.48f;
        [SerializeField] private float defaultAmbientMaxVolume = 0.22f;

        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private bool _usingSourceA = true;
        private AudioSource _ambientSource;
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private int _sfxCursor;

        private float _musicVolume = 0.85f;
        private float _sfxVolume = 0.90f;
        private bool _isPausedDuck;
        private float _duckMultiplier = 1f;
        private bool _mixerHandlesMusic;
        private bool _mixerHandlesSfx;
        private Coroutine _musicFadeCoroutine;

        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public AudioBank Bank => audioBank;

        public static AudioManager EnsureExists()
        {
            if (Instance != null) return Instance;

            AudioManager existing = FindAnyObjectByType<AudioManager>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject go = new GameObject("AudioManager");
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioBank == null)
            {
                audioBank = Resources.Load<AudioBank>("MedievalAudioBank");
            }

            BuildAudioSources();
            LoadSavedVolumes();
            ApplyVolumes();
        }

        private void BuildAudioSources()
        {
            _musicSourceA = CreateChildSource("MusicSource_A", true);
            _musicSourceB = CreateChildSource("MusicSource_B", true);
            _ambientSource = CreateChildSource("AmbientSource", true);

            for (int i = 0; i < Mathf.Max(4, sfxPoolSize); i++)
            {
                _sfxPool.Add(CreateChildSource($"SFXSource_{i}", false));
            }

            ApplyMixerRouting();
        }

        private AudioSource CreateChildSource(string childName, bool loop)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            AudioSource src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        private void ApplyMixerRouting()
        {
            if (audioBank == null) return;

            if (audioBank.musicGroup != null)
            {
                _musicSourceA.outputAudioMixerGroup = audioBank.musicGroup;
                _musicSourceB.outputAudioMixerGroup = audioBank.musicGroup;
            }
            if (audioBank.ambientGroup != null)
            {
                _ambientSource.outputAudioMixerGroup = audioBank.ambientGroup;
            }
            else if (audioBank.musicGroup != null)
            {
                _ambientSource.outputAudioMixerGroup = audioBank.musicGroup;
            }

            if (audioBank.sfxGroup != null)
            {
                foreach (AudioSource src in _sfxPool)
                {
                    src.outputAudioMixerGroup = audioBank.sfxGroup;
                }
            }

            if (audioBank.audioMixer != null)
            {
                _mixerHandlesMusic = audioBank.audioMixer.GetFloat("MusicVol", out _);
                _mixerHandlesSfx = audioBank.audioMixer.GetFloat("SFXVol", out _);
            }
        }

        private void LoadSavedVolumes()
        {
            float legacyFallback = PlayerPrefs.HasKey(LegacyMasterVolume)
                ? PlayerPrefs.GetFloat(LegacyMasterVolume, 0.85f)
                : 0.85f;

            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusicVolume, legacyFallback));
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVolume, legacyFallback));
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefMusicVolume, _musicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void SetSFXVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefSfxVolume, _sfxVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void SetPausedDuck(bool paused)
        {
            _isPausedDuck = paused;
            _duckMultiplier = paused ? 0.50f : 1.0f; // ~ -6 dB when paused
            ApplyVolumes();
        }

        public void ApplyVolumes()
        {
            AudioListener.volume = 1f;

            if (audioBank != null && audioBank.audioMixer != null)
            {
                if (_mixerHandlesMusic)
                {
                    audioBank.audioMixer.SetFloat("MusicVol", LinearToDecibels(_musicVolume * _duckMultiplier));
                }
                if (_mixerHandlesSfx)
                {
                    audioBank.audioMixer.SetFloat("SFXVol", LinearToDecibels(_sfxVolume));
                }
            }

            float musicGain = (_mixerHandlesMusic ? 1f : _musicVolume * _duckMultiplier);
            float ambientGain = musicGain * defaultAmbientMaxVolume;

            AudioSource activeMusic = _usingSourceA ? _musicSourceA : _musicSourceB;
            if (activeMusic != null && _musicFadeCoroutine == null && activeMusic.isPlaying)
            {
                activeMusic.volume = defaultMusicMaxVolume * musicGain;
            }
            if (_ambientSource != null && _ambientSource.isPlaying)
            {
                _ambientSource.volume = ambientGain;
            }
        }

        private static float LinearToDecibels(float linear)
        {
            return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        }

        public void PlayMenuMusic()
        {
            if (audioBank == null) return;
            PlayMusic(audioBank.bgmMainMenu, 0.75f);
            PlayAmbient(audioBank.ambientForest);
        }

        public void PlayBattleMusic(bool isBoss)
        {
            if (audioBank == null) return;
            AudioClip clip = isBoss && audioBank.bgmBoss != null ? audioBank.bgmBoss : audioBank.bgmBattle;
            PlayMusic(clip, 0.65f);
            PlayAmbient(audioBank.ambientForest);
        }

        public void PlayMusic(AudioClip clip, float fadeDuration = 0.6f)
        {
            if (clip == null) return;

            AudioSource current = _usingSourceA ? _musicSourceA : _musicSourceB;
            if (current != null && current.isPlaying && current.clip == clip)
            {
                ApplyVolumes();
                return;
            }

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            _musicFadeCoroutine = StartCoroutine(CrossfadeMusicRoutine(clip, Mathf.Max(0.05f, fadeDuration)));
        }

        public void StopMusic(float fadeDuration = 0.4f)
        {
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            _musicFadeCoroutine = StartCoroutine(FadeOutBothMusicRoutine(Mathf.Max(0.05f, fadeDuration)));
        }

        private IEnumerator CrossfadeMusicRoutine(AudioClip nextClip, float duration)
        {
            AudioSource from = _usingSourceA ? _musicSourceA : _musicSourceB;
            AudioSource to = _usingSourceA ? _musicSourceB : _musicSourceA;
            _usingSourceA = !_usingSourceA;

            to.clip = nextClip;
            to.volume = 0f;
            to.Play();

            float fromStartVol = from != null && from.isPlaying ? from.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float targetMax = defaultMusicMaxVolume * (_mixerHandlesMusic ? 1f : _musicVolume * _duckMultiplier);

                to.volume = Mathf.Lerp(0f, targetMax, t);
                if (from != null && from.isPlaying)
                {
                    from.volume = Mathf.Lerp(fromStartVol, 0f, t);
                }
                yield return null;
            }

            if (from != null && from.isPlaying)
            {
                from.Stop();
                from.volume = 0f;
            }

            _musicFadeCoroutine = null;
            ApplyVolumes();
        }

        private IEnumerator FadeOutBothMusicRoutine(float duration)
        {
            float startA = _musicSourceA != null ? _musicSourceA.volume : 0f;
            float startB = _musicSourceB != null ? _musicSourceB.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (_musicSourceA != null) _musicSourceA.volume = Mathf.Lerp(startA, 0f, t);
                if (_musicSourceB != null) _musicSourceB.volume = Mathf.Lerp(startB, 0f, t);
                yield return null;
            }

            if (_musicSourceA != null) _musicSourceA.Stop();
            if (_musicSourceB != null) _musicSourceB.Stop();
            _musicFadeCoroutine = null;
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null || _ambientSource == null) return;
            if (_ambientSource.isPlaying && _ambientSource.clip == clip) return;

            _ambientSource.clip = clip;
            _ambientSource.volume = defaultAmbientMaxVolume * (_mixerHandlesMusic ? 1f : _musicVolume * _duckMultiplier);
            _ambientSource.Play();
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchJitter = 0.06f)
        {
            if (clip == null || _sfxPool.Count == 0) return;

            AudioSource src = _sfxPool[_sfxCursor];
            _sfxCursor = (_sfxCursor + 1) % _sfxPool.Count;

            src.pitch = pitchJitter > 0f ? Random.Range(1f - pitchJitter, 1f + pitchJitter) : 1f;
            float gain = (_mixerHandlesSfx ? 1f : _sfxVolume) * Mathf.Clamp01(volumeScale);
            src.PlayOneShot(clip, gain);
        }

        public void PlayButtonClick()
        {
            if (audioBank != null) PlaySFX(audioBank.uiClick, 0.72f, 0.05f);
        }

        public void PlayButtonHover()
        {
            if (audioBank != null) PlaySFX(audioBank.uiHover, 0.35f, 0.04f);
        }

        public void PlayQuizOpen()
        {
            if (audioBank != null) PlaySFX(audioBank.quizOpen, 0.78f, 0.02f);
        }

        public void PlayQuizResult(bool isCorrect, int streak)
        {
            if (audioBank == null) return;
            if (isCorrect)
            {
                float pitchBoost = Mathf.Min(0.24f, Mathf.Max(0, streak - 1) * 0.04f);
                AudioSource src = _sfxPool[_sfxCursor];
                _sfxCursor = (_sfxCursor + 1) % _sfxPool.Count;
                src.pitch = 1f + pitchBoost;
                float gain = (_mixerHandlesSfx ? 1f : _sfxVolume) * 0.90f;
                src.PlayOneShot(audioBank.quizCorrect, gain);
            }
            else
            {
                PlaySFX(audioBank.quizWrong, 0.85f, 0.02f);
            }
        }

        public void PlayAttackForHero(CharacterClassType classType, SkillData skill, bool isCrit)
        {
            if (audioBank == null) return;

            if (skill != null && skill.IsHealSkill)
            {
                PlayHealSfx();
                return;
            }
            if (skill != null && skill.IsSelfSupport)
            {
                PlayDefendSfx();
                return;
            }

            if (skill != null && skill.skillName != null && skill.skillName.ToLowerInvariant().Contains("fireball"))
            {
                PlaySFX(audioBank.fireballHit != null ? audioBank.fireballHit : audioBank.magicCast, 0.92f, 0.05f);
            }
            else if (skill != null && skill.damageType == DamageType.Magical)
            {
                PlaySFX(audioBank.magicCast, 0.88f, 0.06f);
            }
            else if (classType == CharacterClassType.Archer)
            {
                PlaySFX(audioBank.bowShoot, 0.88f, 0.07f);
            }
            else if (classType == CharacterClassType.Mage)
            {
                PlaySFX(audioBank.magicCast, 0.84f, 0.06f);
            }
            else
            {
                PlaySFX(audioBank.swordSlash, 0.88f, 0.07f);
            }

            if (isCrit)
            {
                PlaySFX(audioBank.critHit, 0.95f, 0.04f);
            }
        }

        public void PlayEnemyAttackSfx(SkillData skill)
        {
            if (audioBank == null) return;
            if (skill != null && skill.IsSelfSupport)
            {
                PlayDefendSfx();
            }
            else if (skill != null && skill.damageType == DamageType.Magical)
            {
                PlaySFX(audioBank.magicCast, 0.82f, 0.06f);
            }
            else
            {
                PlaySFX(audioBank.swordSlash, 0.80f, 0.08f);
            }
        }

        public void PlayHitSfx(bool isCrit)
        {
            if (audioBank == null) return;
            PlaySFX(isCrit ? audioBank.critHit : audioBank.unitHit, isCrit ? 0.92f : 0.72f, 0.06f);
        }

        public void PlayHealSfx()
        {
            if (audioBank != null) PlaySFX(audioBank.healChime, 0.85f, 0.03f);
        }

        public void PlayDefendSfx()
        {
            if (audioBank != null) PlaySFX(audioBank.shieldDefend, 0.82f, 0.04f);
        }

        public void PlayDeathSfx()
        {
            if (audioBank != null) PlaySFX(audioBank.unitDeath, 0.86f, 0.04f);
        }

        public void PlayVictoryFanfare()
        {
            if (audioBank == null) return;
            StopMusic(0.35f);
            PlaySFX(audioBank.victoryFanfare, 0.95f, 0f);
        }

        public void PlayDefeatSting()
        {
            if (audioBank == null) return;
            StopMusic(0.35f);
            PlaySFX(audioBank.defeatSting, 0.90f, 0f);
        }

        public void PlayLootDropSfx(ItemRarity rarity)
        {
            if (audioBank == null) return;
            if (rarity >= ItemRarity.Epic && audioBank.lootLegendary != null)
            {
                PlaySFX(audioBank.lootLegendary, rarity == ItemRarity.Legendary ? 0.96f : 0.84f, 0.02f);
            }
            else if (audioBank.lootDrop != null)
            {
                PlaySFX(audioBank.lootDrop, rarity == ItemRarity.Rare ? 0.84f : 0.72f, 0.06f);
            }
            else if (audioBank.quizCorrect != null)
            {
                PlaySFX(audioBank.quizCorrect, 0.7f, 0.05f);
            }
        }

        public void PlayLootPickupSfx()
        {
            if (audioBank == null) return;
            AudioClip clip = audioBank.lootPickup != null ? audioBank.lootPickup : audioBank.uiClick;
            if (clip != null) PlaySFX(clip, 0.82f, 0.05f);
        }
    }
}
