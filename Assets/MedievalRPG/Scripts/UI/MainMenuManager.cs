using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MedievalRPG.Audio;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Điều khiển giao diện màn hình Menu chính với Nhạc nền, Âm lượng tách biệt (Music / SFX), Hướng dẫn và chuyển cảnh mượt.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Tên màn hình trận đấu")]
        public string battleSceneName = "MedievalBattleDemo";

        [Header("Bảng giao diện")]
        public GameObject instructionsPanel;
        public GameObject settingsPanel;

        [Header("Giao diện Canvas + TMP")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private UnityEngine.UI.Button instructionsButton;
        [SerializeField] private UnityEngine.UI.Button settingsButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;
        [SerializeField] private UnityEngine.UI.Button closeInstructionsButton;
        [SerializeField] private UnityEngine.UI.Button closeSettingsButton;

        [Header("Điều khiển Cài đặt")]
        [SerializeField] private UnityEngine.UI.Slider musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeValueTMP;
        [SerializeField] private UnityEngine.UI.Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI sfxVolumeValueTMP;
        [SerializeField] private UnityEngine.UI.Toggle quizModeToggle;
        [SerializeField] private UnityEngine.UI.Toggle fastSpeedToggle;

        private bool _isTransitioning;

        public bool HasCanvasUI => rootCanvas != null && startButton != null;

        public void ConfigureCanvasUI(
            Canvas canvas, CanvasGroup fade,
            UnityEngine.UI.Button btnStart, UnityEngine.UI.Button btnGuide,
            UnityEngine.UI.Button btnSettings, UnityEngine.UI.Button btnQuit,
            GameObject guidePanel, UnityEngine.UI.Button btnCloseGuide,
            GameObject settPanel, UnityEngine.UI.Button btnCloseSett,
            UnityEngine.UI.Slider musicSlider, TextMeshProUGUI musicValue,
            UnityEngine.UI.Slider sfxSlider, TextMeshProUGUI sfxValue,
            UnityEngine.UI.Toggle quizToggle, UnityEngine.UI.Toggle speedToggle)
        {
            rootCanvas = canvas;
            fadeOverlay = fade;
            startButton = btnStart;
            instructionsButton = btnGuide;
            settingsButton = btnSettings;
            quitButton = btnQuit;
            instructionsPanel = guidePanel;
            closeInstructionsButton = btnCloseGuide;
            settingsPanel = settPanel;
            closeSettingsButton = btnCloseSett;
            musicVolumeSlider = musicSlider;
            musicVolumeValueTMP = musicValue;
            sfxVolumeSlider = sfxSlider;
            sfxVolumeValueTMP = sfxValue;
            quizModeToggle = quizToggle;
            fastSpeedToggle = speedToggle;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            AudioManager audio = AudioManager.EnsureExists();
            audio.SetPausedDuck(false);
            audio.PlayMenuMusic();

            if (instructionsPanel != null) instructionsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            WireButtons();
            WireSettings();

            if (quitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                quitButton.gameObject.SetActive(false);
            }

            if (fadeOverlay != null)
            {
                StartCoroutine(FadeRoutine(1f, 0f, 0.30f));
            }
        }

        private void WireButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnNewGameClicked);
            }
            if (instructionsButton != null)
            {
                instructionsButton.onClick.RemoveAllListeners();
                instructionsButton.onClick.AddListener(() => ToggleInstructions(true));
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(() => ToggleSettings(true));
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }
            if (closeInstructionsButton != null)
            {
                closeInstructionsButton.onClick.RemoveAllListeners();
                closeInstructionsButton.onClick.AddListener(() => ToggleInstructions(false));
            }
            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.RemoveAllListeners();
                closeSettingsButton.onClick.AddListener(() => ToggleSettings(false));
            }
        }

        private void WireSettings()
        {
            AudioManager audio = AudioManager.EnsureExists();

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.value = audio.MusicVolume;
                UpdatePercentText(musicVolumeValueTMP, musicVolumeSlider.value);
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener(val =>
                {
                    AudioManager.EnsureExists().SetMusicVolume(val);
                    UpdatePercentText(musicVolumeValueTMP, val);
                });
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.value = audio.SFXVolume;
                UpdatePercentText(sfxVolumeValueTMP, sfxVolumeSlider.value);
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener(val =>
                {
                    AudioManager.EnsureExists().SetSFXVolume(val);
                    UpdatePercentText(sfxVolumeValueTMP, val);
                });
            }

            if (quizModeToggle != null)
            {
                quizModeToggle.isOn = PlayerPrefs.GetInt("QuizEnabled", 1) == 1;
                quizModeToggle.onValueChanged.RemoveAllListeners();
                quizModeToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetInt("QuizEnabled", isOn ? 1 : 0);
                    PlayerPrefs.Save();
                });
            }

            if (fastSpeedToggle != null)
            {
                fastSpeedToggle.isOn = PlayerPrefs.GetFloat("BattleSpeed", 1f) > 1.1f;
                fastSpeedToggle.onValueChanged.RemoveAllListeners();
                fastSpeedToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetFloat("BattleSpeed", isOn ? 1.5f : 1f);
                    PlayerPrefs.Save();
                });
            }
        }

        private static void UpdatePercentText(TextMeshProUGUI label, float val)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || _isTransitioning) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (instructionsPanel != null && instructionsPanel.activeSelf) ToggleInstructions(false);
                else if (settingsPanel != null && settingsPanel.activeSelf) ToggleSettings(false);
            }
        }

        public void OnNewGameClicked()
        {
            if (_isTransitioning) return;
            StartCoroutine(LoadBattleWithFade());
        }

        public void OnContinueClicked()
        {
            OnNewGameClicked();
        }

        public void ToggleInstructions(bool show)
        {
            if (show && settingsPanel != null) settingsPanel.SetActive(false);
            if (instructionsPanel != null) instructionsPanel.SetActive(show);
        }

        public void ToggleSettings(bool show)
        {
            if (show && instructionsPanel != null) instructionsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(show);
        }

        public void OnQuitClicked()
        {
            Application.Quit();
        }

        private IEnumerator LoadBattleWithFade()
        {
            _isTransitioning = true;
            if (fadeOverlay != null) yield return FadeRoutine(0f, 1f, 0.28f);
            SceneManager.LoadScene(battleSceneName);
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
        {
            if (fadeOverlay == null) yield break;
            fadeOverlay.blocksRaycasts = toAlpha > 0.05f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            fadeOverlay.alpha = toAlpha;
            fadeOverlay.blocksRaycasts = toAlpha > 0.05f;
        }
    }
}
