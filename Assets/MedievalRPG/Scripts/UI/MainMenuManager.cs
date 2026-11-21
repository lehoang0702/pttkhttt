using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MedievalRPG.UI
{
    /// <summary>
    /// [Quản Lý Màn Hình Menu Chính - MainMenuManager]
    /// Điều khiển giao diện uGUI hiện đại tại màn hình khởi động game:
    /// - Bắt đầu trận chiến mới (New Game) với chuyển cảnh mượt
    /// - Bảng Hướng dẫn luật chơi giáo dục (Instructions)
    /// - Bảng Cài đặt âm lượng & chế độ Quiz / tốc độ (Settings lưu PlayerPrefs)
    /// - Thoát game (Quit)
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
        [SerializeField] private UnityEngine.UI.Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeValueTMP;
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
            UnityEngine.UI.Slider volSlider, TextMeshProUGUI volValue,
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
            volumeSlider = volSlider;
            volumeValueTMP = volValue;
            quizModeToggle = quizToggle;
            fastSpeedToggle = speedToggle;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);

            if (instructionsPanel != null) instructionsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            WireButtons();
            WireSettings();

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
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
                UpdateVolumeText(volumeSlider.value);
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat("MasterVolume", val);
                    PlayerPrefs.Save();
                    AudioListener.volume = val;
                    UpdateVolumeText(val);
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

        private void UpdateVolumeText(float val)
        {
            if (volumeValueTMP != null)
            {
                volumeValueTMP.text = $"{Mathf.RoundToInt(val * 100f)}%";
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

        /// <summary>
        /// Bắt đầu một ván chơi mới
        /// </summary>
        public void OnNewGameClicked()
        {
            if (_isTransitioning) return;
            Debug.Log("[MainMenu] Khởi tạo trận chiến mới...");
            StartCoroutine(LoadBattleWithFade());
        }

        /// <summary>
        /// Tiếp tục màn chơi hiện tại
        /// </summary>
        public void OnContinueClicked()
        {
            OnNewGameClicked();
        }

        /// <summary>
        /// Mở / Đóng bảng hướng dẫn chơi
        /// </summary>
        public void ToggleInstructions(bool show)
        {
            if (show && settingsPanel != null) settingsPanel.SetActive(false);
            if (instructionsPanel != null) instructionsPanel.SetActive(show);
        }

        /// <summary>
        /// Mở / Đóng bảng cài đặt
        /// </summary>
        public void ToggleSettings(bool show)
        {
            if (show && instructionsPanel != null) instructionsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(show);
        }

        /// <summary>
        /// Thoát trò chơi ra màn hình Desktop
        /// </summary>
        public void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Đang thoát trò chơi...");
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

        // Chỉ vẽ OnGUI dự phòng nếu chưa cấu hình Canvas uGUI
        private void OnGUI()
        {
            if (HasCanvasUI) return;
            if (SceneManager.GetActiveScene().name == battleSceneName) return;

            GUI.skin.box.fontSize = 14;
            GUI.skin.button.fontSize = 14;

            float menuWidth = 320;
            float menuHeight = 320;
            Rect rect = new Rect((Screen.width - menuWidth) / 2, (Screen.height - menuHeight) / 2, menuWidth, menuHeight);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Space(10);
            GUILayout.Label("<size=18><b>MEDIEVAL RPG</b></size>", GUI.skin.label);
            GUILayout.Label("<size=12><i>Chiến Binh Tri Thức - Game Giáo Dục</i></size>");
            GUILayout.Space(20);

            if (GUILayout.Button("BẮT ĐẦU CHƠI (NEW GAME)", GUILayout.Height(40))) OnNewGameClicked();
            GUILayout.Space(8);
            if (GUILayout.Button("HƯỚNG DẪN CÁCH CHƠI", GUILayout.Height(36))) ToggleInstructions(true);
            GUILayout.Space(8);
            if (GUILayout.Button("CÀI ĐẶT (SETTINGS)", GUILayout.Height(36))) ToggleSettings(true);
            GUILayout.Space(8);
            if (GUILayout.Button("THOÁT GAME (QUIT)", GUILayout.Height(36))) OnQuitClicked();

            GUILayout.EndArea();
        }
    }
}
