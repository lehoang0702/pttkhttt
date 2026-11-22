using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MedievalRPG.Quiz;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Bảng câu hỏi trắc nghiệm giáo dục (Quiz Modal).
    /// Dùng unscaledDeltaTime cho thanh thời gian đọc giải thích để không bị Fast Mode làm trôi nhanh.
    /// </summary>
    public class QuizModalUI : MonoBehaviour
    {
        [Header("Thành phần hiển thị")]
        [SerializeField] private Image categoryBadgeImage;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI bonusBadgeText;
        [SerializeField] private TextMeshProUGUI pendingActionText;
        [SerializeField] private TextMeshProUGUI questionContentText;

        [Header("4 nút đáp án A, B, C, D")]
        [SerializeField] private Button[] optionButtons = new Button[4];
        [SerializeField] private Image[] optionBackgrounds = new Image[4];
        [SerializeField] private Image[] optionLetterBadges = new Image[4];
        [SerializeField] private TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];
        [SerializeField] private UIButtonFeedback[] optionFeedbacks = new UIButtonFeedback[4];

        [Header("Bộ đếm thời gian Quiz (V2)")]
        [SerializeField] private GameObject questionTimerRoot;
        [SerializeField] private Image questionTimerFill;
        [SerializeField] private TextMeshProUGUI questionTimerText;

        [Header("Khung kết quả & giải thích")]
        [SerializeField] private GameObject feedbackContainer;
        [SerializeField] private Image feedbackBackground;
        [SerializeField] private TextMeshProUGUI feedbackTitleText;
        [SerializeField] private TextMeshProUGUI feedbackExplanationText;
        [SerializeField] private Image feedbackTimerFill;
        [SerializeField] private Button continueButton;

        private const float QuestionTimeLimit = 20f;
        private const float QuickMindThreshold = 5f;

        private bool _isLocked;
        private float _feedbackDuration = 2.4f;
        private float _feedbackElapsed;
        private float _questionElapsed;
        private bool _timerEnabledForCurrentQuestion;
        private bool _lastWasQuickMind;
        private bool _isShowingFeedback;
        private Action<int> _onAnswerSubmitted;
        private Action<int, bool> _onAnswerSubmittedWithSpeed;
        private Action _onContinueClicked;

        public void ConfigureTimerUI(GameObject timerRoot, Image timerFill, TextMeshProUGUI timerText)
        {
            questionTimerRoot = timerRoot;
            questionTimerFill = timerFill;
            questionTimerText = timerText;
        }

        public void Configure(
            Image catBadge, TextMeshProUGUI catText, TextMeshProUGUI bonusText,
            TextMeshProUGUI pendingText, TextMeshProUGUI qText,
            Button[] buttons, Image[] bgs, Image[] letterBadges, TextMeshProUGUI[] texts, UIButtonFeedback[] feedbacks,
            GameObject fbContainer, Image fbBg, TextMeshProUGUI fbTitle, TextMeshProUGUI fbExpl,
            Image fbTimerFill, Button contBtn)
        {
            categoryBadgeImage = catBadge;
            categoryText = catText;
            bonusBadgeText = bonusText;
            pendingActionText = pendingText;
            questionContentText = qText;
            optionButtons = buttons;
            optionBackgrounds = bgs;
            optionLetterBadges = letterBadges;
            optionTexts = texts;
            optionFeedbacks = feedbacks;
            feedbackContainer = fbContainer;
            feedbackBackground = fbBg;
            feedbackTitleText = fbTitle;
            feedbackExplanationText = fbExpl;
            feedbackTimerFill = fbTimerFill;
            continueButton = contBtn;

            WireOptionButtons();
        }

        private void Awake()
        {
            WireOptionButtons();
        }

        private void WireOptionButtons()
        {
            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    if (optionButtons[i] == null) continue;
                    int idx = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SubmitOption(idx));
                }
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => _onContinueClicked?.Invoke());
            }
        }

        public void ShowQuestion(QuizQuestion q, string actionContext, Action<int> onAnswerSubmitted, Action onContinueClicked)
        {
            ShowQuestionInternal(q, actionContext, onAnswerSubmitted, null, onContinueClicked);
        }

        public void ShowQuestion(QuizQuestion q, string actionContext, Action<int, bool> onAnswerSubmittedWithSpeed, Action onContinueClicked)
        {
            ShowQuestionInternal(q, actionContext, null, onAnswerSubmittedWithSpeed, onContinueClicked);
        }

        private void ShowQuestionInternal(
            QuizQuestion q,
            string actionContext,
            Action<int> onAnswerSubmitted,
            Action<int, bool> onAnswerSubmittedWithSpeed,
            Action onContinueClicked)
        {
            _onAnswerSubmitted = onAnswerSubmitted;
            _onAnswerSubmittedWithSpeed = onAnswerSubmittedWithSpeed;
            _onContinueClicked = onContinueClicked;
            _isLocked = false;
            _isShowingFeedback = false;
            _feedbackElapsed = 0f;
            _questionElapsed = 0f;
            _lastWasQuickMind = false;
            _timerEnabledForCurrentQuestion = Data.PlayerCampProgressManager.EnsureLoaded().QuizTimerEnabled;

            gameObject.SetActive(true);
            if (feedbackContainer != null) feedbackContainer.SetActive(false);
            if (questionTimerRoot != null) questionTimerRoot.SetActive(true);
            RefreshQuestionTimerUI();

            if (q == null) return;

            if (categoryText != null) categoryText.text = q.category.ToUpperInvariant();
            if (categoryBadgeImage != null) categoryBadgeImage.color = MedievalUITheme.GetCategoryColor(q.category);
            if (bonusBadgeText != null) bonusBadgeText.text = $"Thưởng +{q.bonusDamage} ST";
            if (pendingActionText != null) pendingActionText.text = actionContext;
            if (questionContentText != null) questionContentText.text = q.questionText;

            for (int i = 0; i < 4; i++)
            {
                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                {
                    optionButtons[i].interactable = true;
                }
                if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                {
                    optionBackgrounds[i].color = MedievalUITheme.BtnNeutralSlate;
                }
                if (optionFeedbacks != null && i < optionFeedbacks.Length && optionFeedbacks[i] != null)
                {
                    optionFeedbacks[i].SetBaseColor(MedievalUITheme.BtnNeutralSlate);
                }
                if (optionLetterBadges != null && i < optionLetterBadges.Length && optionLetterBadges[i] != null)
                {
                    optionLetterBadges[i].color = MedievalUITheme.GoldPrimary;
                }
                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                {
                    string ans = (q.answers != null && i < q.answers.Length) ? q.answers[i] : "";
                    optionTexts[i].text = ans;
                }
            }
        }

        public void ShowResult(bool isCorrect, int selectedIndex, int correctIndex, int bonusDamage, string explanation, float duration)
        {
            _isLocked = true;
            _isShowingFeedback = true;
            _feedbackDuration = Mathf.Max(0.5f, duration);
            _feedbackElapsed = 0f;

            for (int i = 0; i < 4; i++)
            {
                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                {
                    optionButtons[i].interactable = false;
                }

                Color cardColor = MedievalUITheme.BtnDisabled;
                Color letterColor = MedievalUITheme.SlateBorder;

                if (i == correctIndex)
                {
                    cardColor = MedievalUITheme.SuccessPanel;
                    letterColor = MedievalUITheme.SuccessGreen;
                }
                else if (i == selectedIndex && !isCorrect)
                {
                    cardColor = MedievalUITheme.DangerPanel;
                    letterColor = MedievalUITheme.DangerRed;
                }

                if (optionFeedbacks != null && i < optionFeedbacks.Length && optionFeedbacks[i] != null)
                {
                    optionFeedbacks[i].SetBaseColor(cardColor);
                }
                else if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                {
                    optionBackgrounds[i].color = cardColor;
                }

                if (optionLetterBadges != null && i < optionLetterBadges.Length && optionLetterBadges[i] != null)
                {
                    optionLetterBadges[i].color = letterColor;
                }
            }

            if (feedbackContainer != null) feedbackContainer.SetActive(true);
            if (feedbackBackground != null)
            {
                feedbackBackground.color = isCorrect ? MedievalUITheme.SuccessPanel : MedievalUITheme.DangerPanel;
            }
            if (feedbackTitleText != null)
            {
                if (isCorrect)
                {
                    feedbackTitleText.text = _lastWasQuickMind
                        ? $"PHẢN XẠ NHANH!  +{bonusDamage} ST, +15 Vàng & +15% Chí mạng!"
                        : $"CHÍNH XÁC!  +{bonusDamage} sát thương chiêu thức & +10 Vàng!";
                }
                else
                {
                    feedbackTitleText.text = selectedIndex < 0
                        ? "HẾT GIỜ!  Anh hùng mất lượt hành động này."
                        : "CHƯA ĐÚNG!  Anh hùng mất lượt hành động này.";
                }
                feedbackTitleText.color = isCorrect ? MedievalUITheme.GoldSoft : new Color(1f, 0.68f, 0.68f, 1f);
            }
            if (feedbackExplanationText != null)
            {
                feedbackExplanationText.text = string.IsNullOrEmpty(explanation)
                    ? "Keep going on the next English challenge!"
                    : $"Grammar / Usage: {explanation}";
            }
            if (feedbackTimerFill != null)
            {
                feedbackTimerFill.fillAmount = 1f;
                feedbackTimerFill.color = isCorrect ? MedievalUITheme.SuccessGreen : MedievalUITheme.DangerRed;
            }
        }

        public void Hide()
        {
            _isShowingFeedback = false;
            _isLocked = false;
            gameObject.SetActive(false);
        }

        private void SubmitOption(int index)
        {
            if (_isLocked) return;
            _isLocked = true;
            _lastWasQuickMind = index >= 0 && _questionElapsed <= QuickMindThreshold;
            _onAnswerSubmittedWithSpeed?.Invoke(index, _lastWasQuickMind);
            _onAnswerSubmitted?.Invoke(index);
        }

        private void RefreshQuestionTimerUI()
        {
            if (questionTimerFill == null && questionTimerText == null) return;

            if (!_timerEnabledForCurrentQuestion)
            {
                if (questionTimerFill != null)
                {
                    questionTimerFill.fillAmount = 1f;
                    questionTimerFill.color = new Color(0.28f, 0.56f, 0.78f, 1f);
                }
                if (questionTimerText != null)
                {
                    questionTimerText.text = _questionElapsed <= QuickMindThreshold
                        ? $"NHANH! ({Mathf.CeilToInt(QuickMindThreshold - _questionElapsed)}s)"
                        : "KHÔNG GIỚI HẠN";
                }
                return;
            }

            float remaining = Mathf.Max(0f, QuestionTimeLimit - _questionElapsed);
            float ratio = Mathf.Clamp01(remaining / QuestionTimeLimit);
            bool inQuickWindow = _questionElapsed <= QuickMindThreshold;

            if (questionTimerFill != null)
            {
                questionTimerFill.fillAmount = ratio;
                if (inQuickWindow)
                {
                    questionTimerFill.color = MedievalUITheme.GoldPrimary;
                }
                else if (remaining <= 5f)
                {
                    questionTimerFill.color = MedievalUITheme.DangerRed;
                }
                else
                {
                    questionTimerFill.color = MedievalUITheme.SuccessGreen;
                }
            }

            if (questionTimerText != null)
            {
                int sec = Mathf.CeilToInt(remaining);
                questionTimerText.text = inQuickWindow
                    ? $"NHANH! {sec}s"
                    : $"THỜI GIAN: {sec}s";
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            bool isPaused = BattleUIManager.Instance != null
                            && BattleUIManager.Instance.battleSystem != null
                            && BattleUIManager.Instance.battleSystem.IsPaused;
            if (isPaused) return;

            if (_isShowingFeedback)
            {
                _feedbackElapsed += Time.unscaledDeltaTime;
                if (feedbackTimerFill != null)
                {
                    feedbackTimerFill.fillAmount = Mathf.Clamp01(1f - (_feedbackElapsed / _feedbackDuration));
                }

                Keyboard kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                {
                    _onContinueClicked?.Invoke();
                }
                return;
            }

            if (!_isLocked)
            {
                _questionElapsed += Time.unscaledDeltaTime;
                RefreshQuestionTimerUI();

                if (_timerEnabledForCurrentQuestion && _questionElapsed >= QuestionTimeLimit)
                {
                    SubmitOption(-1);
                    return;
                }

                Keyboard kb = Keyboard.current;
                if (kb == null) return;

                if (kb.aKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SubmitOption(0);
                else if (kb.bKey.wasPressedThisFrame || kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SubmitOption(1);
                else if (kb.cKey.wasPressedThisFrame || kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SubmitOption(2);
                else if (kb.dKey.wasPressedThisFrame || kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SubmitOption(3);
            }
        }
    }
}
