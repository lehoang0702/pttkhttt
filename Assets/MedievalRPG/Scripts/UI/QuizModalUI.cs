using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MedievalRPG.Audio;
using MedievalRPG.Data;
using MedievalRPG.Quiz;
using MedievalRPG.Visuals;

namespace MedievalRPG.UI
{
    public enum QuizPromptMode
    {
        AttackSkill = 0,
        DefensiveGuard = 1,
        RoadsideEvent = 2
    }

    /// <summary>
    /// Bảng câu hỏi trắc nghiệm giáo dục (Quiz Modal).
    /// Hỗ trợ 3 chế độ: Tấn Công/Kỹ Năng, Phản Xạ Đỡ Đòn Boss (8s), và Sự Kiện Dọc Đường.
    /// Dùng unscaledDeltaTime cho thanh thời gian & tween để không bị Fast Mode làm trôi nhanh.
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

        [Header("Gợi ý 50:50 & Bỏ qua Đỡ đòn")]
        [SerializeField] private Button hint5050Button;
        [SerializeField] private Image hint5050Background;
        [SerializeField] private TextMeshProUGUI hint5050LabelTMP;
        [SerializeField] private Button skipGuardButton;
        [SerializeField] private TextMeshProUGUI headerTitleText;

        public const int Hint5050GoldCost = 20;
        private const float AttackTimeLimit = 20f;
        private const float GuardTimeLimit = 8f;
        private const float AttackQuickThreshold = 5f;
        private const float GuardQuickThreshold = 3.5f;

        private bool _isLocked;
        private float _feedbackDuration = 2.4f;
        private float _feedbackElapsed;
        private float _questionElapsed;
        private float _activeTimeLimit = AttackTimeLimit;
        private float _activeQuickThreshold = AttackQuickThreshold;
        private QuizPromptMode _mode = QuizPromptMode.AttackSkill;
        private int _currentStreak;
        private int _freeHintsRemaining;
        private bool _timerEnabledForCurrentQuestion;
        private bool _lastWasQuickMind;
        private bool _lastWasSkipped;
        private bool _isShowingFeedback;
        private QuizQuestion _currentQuestion;
        private bool _hint5050Used;
        private readonly bool[] _eliminatedOptions = new bool[4];
        private RectTransform _cardRect;
        private float _cardPopElapsed;
        private bool _isPopAnimating;
        private Func<bool> _onTryConsumeFreeHint;
        private Action<int> _onAnswerSubmitted;
        private Action<int, bool> _onAnswerSubmittedWithSpeed;
        private Action<int, bool, bool> _onAnswerWithModeFlags;
        private Action _onContinueClicked;

        public void ConfigureTimerUI(GameObject timerRoot, Image timerFill, TextMeshProUGUI timerText)
        {
            questionTimerRoot = timerRoot;
            questionTimerFill = timerFill;
            questionTimerText = timerText;
        }

        public void ConfigureHintButton(Button hintBtn, Image hintBg, TextMeshProUGUI hintLbl)
        {
            hint5050Button = hintBtn;
            hint5050Background = hintBg;
            hint5050LabelTMP = hintLbl;
            WireHintButton();
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
            EnsureExtraControlsExist();
            WireHintButton();
        }

        private void WireHintButton()
        {
            if (hint5050Button != null)
            {
                hint5050Button.onClick.RemoveAllListeners();
                hint5050Button.onClick.AddListener(UseHint5050);
            }
            if (skipGuardButton != null)
            {
                skipGuardButton.onClick.RemoveAllListeners();
                skipGuardButton.onClick.AddListener(SkipDefensiveQuiz);
            }
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
            ShowQuestionInternal(q, actionContext, QuizPromptMode.AttackSkill, 0, 0, null, onAnswerSubmitted, null, null, onContinueClicked);
        }

        public void ShowQuestion(QuizQuestion q, string actionContext, Action<int, bool> onAnswerSubmittedWithSpeed, Action onContinueClicked)
        {
            ShowQuestionInternal(q, actionContext, QuizPromptMode.AttackSkill, 0, 0, null, null, onAnswerSubmittedWithSpeed, null, onContinueClicked);
        }

        public void ShowQuestionWithMode(
            QuizQuestion q,
            string actionContext,
            QuizPromptMode mode,
            int currentStreak,
            int freeHintsRemaining,
            Func<bool> onTryConsumeFreeHint,
            Action<int, bool, bool> onAnswerWithModeFlags,
            Action onContinueClicked)
        {
            ShowQuestionInternal(
                q, actionContext, mode, currentStreak, freeHintsRemaining,
                onTryConsumeFreeHint, null, null, onAnswerWithModeFlags, onContinueClicked);
        }

        private void ShowQuestionInternal(
            QuizQuestion q,
            string actionContext,
            QuizPromptMode mode,
            int currentStreak,
            int freeHintsRemaining,
            Func<bool> onTryConsumeFreeHint,
            Action<int> onAnswerSubmitted,
            Action<int, bool> onAnswerSubmittedWithSpeed,
            Action<int, bool, bool> onAnswerWithModeFlags,
            Action onContinueClicked)
        {
            EnsureExtraControlsExist();
            WireHintButton();

            _currentQuestion = q;
            _mode = mode;
            _currentStreak = Mathf.Max(0, currentStreak);
            _freeHintsRemaining = Mathf.Max(0, freeHintsRemaining);
            _onTryConsumeFreeHint = onTryConsumeFreeHint;
            _activeTimeLimit = mode == QuizPromptMode.DefensiveGuard ? GuardTimeLimit : AttackTimeLimit;
            _activeQuickThreshold = mode == QuizPromptMode.DefensiveGuard ? GuardQuickThreshold : AttackQuickThreshold;
            _hint5050Used = false;
            Array.Clear(_eliminatedOptions, 0, _eliminatedOptions.Length);
            _onAnswerSubmitted = onAnswerSubmitted;
            _onAnswerSubmittedWithSpeed = onAnswerSubmittedWithSpeed;
            _onAnswerWithModeFlags = onAnswerWithModeFlags;
            _onContinueClicked = onContinueClicked;
            _isLocked = false;
            _isShowingFeedback = false;
            _feedbackElapsed = 0f;
            _questionElapsed = 0f;
            _lastWasQuickMind = false;
            _lastWasSkipped = false;
            _timerEnabledForCurrentQuestion = mode == QuizPromptMode.DefensiveGuard || Data.PlayerCampProgressManager.EnsureLoaded().QuizTimerEnabled;

            gameObject.SetActive(true);
            StartCardPopAnimation();
            if (feedbackContainer != null) feedbackContainer.SetActive(false);
            if (questionTimerRoot != null) questionTimerRoot.SetActive(true);
            if (skipGuardButton != null) skipGuardButton.gameObject.SetActive(mode == QuizPromptMode.DefensiveGuard);

            if (headerTitleText != null)
            {
                headerTitleText.text = mode switch
                {
                    QuizPromptMode.DefensiveGuard => "PHẢN XẠ ĐỠ ĐÒN!",
                    QuizPromptMode.RoadsideEvent  => "GIẢI MÃ SỰ KIỆN",
                    _                             => "QUIZ B1/B2"
                };
                headerTitleText.color = mode == QuizPromptMode.DefensiveGuard
                    ? MedievalUITheme.HpWarning
                    : (mode == QuizPromptMode.RoadsideEvent ? MedievalUITheme.SuccessGreen : MedievalUITheme.GoldSoft);
            }

            RefreshQuestionTimerUI();
            RefreshHint5050UI();

            if (q == null) return;

            if (categoryText != null) categoryText.text = (q.category ?? "ENGLISH QUIZ").ToUpperInvariant();
            if (categoryBadgeImage != null) categoryBadgeImage.color = MedievalUITheme.GetCategoryColor(q.category);
            if (bonusBadgeText != null)
            {
                if (mode == QuizPromptMode.DefensiveGuard)
                {
                    bonusBadgeText.text = "Đỡ -50% ST & Phản đòn";
                }
                else if (mode == QuizPromptMode.RoadsideEvent)
                {
                    bonusBadgeText.text = "Thưởng Sự Kiện";
                }
                else
                {
                    string streakTag = _currentStreak >= 3 ? $" • x1.2 ({_currentStreak})" : (_currentStreak >= 2 ? $" • Chuỗi {_currentStreak}" : "");
                    bonusBadgeText.text = $"+{q.bonusDamage} ST{streakTag}";
                }
            }
            if (pendingActionText != null)
            {
                pendingActionText.text = actionContext;
                pendingActionText.color = mode == QuizPromptMode.DefensiveGuard ? MedievalUITheme.HpWarning : MedievalUITheme.GoldSoft;
                RectTransform pRt = pendingActionText.rectTransform;
                pRt.sizeDelta = new Vector2(mode == QuizPromptMode.DefensiveGuard ? -404f : -246f, pRt.sizeDelta.y > 0f ? pRt.sizeDelta.y : 28f);
            }
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
                    optionTexts[i].color = MedievalUITheme.IvoryText;
                }
            }
        }

        public void ShowResult(bool isCorrect, int selectedIndex, int correctIndex, int bonusDamage, string explanation, float duration)
        {
            _isLocked = true;
            _isShowingFeedback = true;
            _feedbackDuration = Mathf.Max(0.5f, duration);
            _feedbackElapsed = 0f;
            if (hint5050Button != null) hint5050Button.interactable = false;
            if (skipGuardButton != null) skipGuardButton.gameObject.SetActive(false);

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
                if (_mode == QuizPromptMode.DefensiveGuard)
                {
                    if (isCorrect)
                    {
                        feedbackTitleText.text = _lastWasQuickMind
                            ? "PARRY HOÀN HẢO! Giảm 65% ST, Phản đòn & +8 Vàng!"
                            : "ĐỠ ĐÒN THÀNH CÔNG! Giảm 50% ST, Phản đòn & +5 Vàng!";
                    }
                    else
                    {
                        feedbackTitleText.text = _lastWasSkipped
                            ? "CHỌN NHẬN ĐÒN! Đang chuẩn bị chống đỡ đòn đánh..."
                            : (selectedIndex < 0 ? "HẾT GIỜ! Không kịp đỡ đòn tấn công." : "CHƯA ĐÚNG! Không đỡ được đòn tấn công.");
                    }
                }
                else if (_mode == QuizPromptMode.RoadsideEvent)
                {
                    feedbackTitleText.text = isCorrect
                        ? "GIẢI MÃ THÀNH CÔNG! Nhận trọn phần thưởng sự kiện!"
                        : "GIẢI MÃ THẤT BẠI! Sự kiện khép lại, tiếp tục hành quân.";
                }
                else if (isCorrect)
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

        public static int[] PickTwoWrongOptionIndices(int correctAnswerIndex, System.Random rng = null)
        {
            List<int> wrong = new List<int>(3);
            for (int i = 0; i < 4; i++)
            {
                if (i != correctAnswerIndex) wrong.Add(i);
            }
            for (int i = wrong.Count - 1; i > 0; i--)
            {
                int j = rng != null ? rng.Next(0, i + 1) : UnityEngine.Random.Range(0, i + 1);
                int tmp = wrong[i];
                wrong[i] = wrong[j];
                wrong[j] = tmp;
            }
            return new[] { wrong[0], wrong[1] };
        }

        public void UseHint5050()
        {
            if (_isLocked || _hint5050Used || _currentQuestion == null) return;

            bool usedFree = _freeHintsRemaining > 0 && _onTryConsumeFreeHint != null && _onTryConsumeFreeHint();
            if (usedFree)
            {
                _freeHintsRemaining = Mathf.Max(0, _freeHintsRemaining - 1);
            }
            else
            {
                PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
                if (!camp.TrySpendGold(Hint5050GoldCost))
                {
                    RefreshHint5050UI();
                    return;
                }
            }

            _hint5050Used = true;
            int[] toRemove = PickTwoWrongOptionIndices(_currentQuestion.correctAnswerIndex);
            for (int i = 0; i < toRemove.Length; i++)
            {
                int idx = toRemove[i];
                if (idx < 0 || idx >= 4) continue;
                _eliminatedOptions[idx] = true;
                if (optionButtons != null && idx < optionButtons.Length && optionButtons[idx] != null)
                {
                    optionButtons[idx].interactable = false;
                }
                if (optionFeedbacks != null && idx < optionFeedbacks.Length && optionFeedbacks[idx] != null)
                {
                    optionFeedbacks[idx].SetBaseColor(MedievalUITheme.BtnDisabled);
                }
                else if (optionBackgrounds != null && idx < optionBackgrounds.Length && optionBackgrounds[idx] != null)
                {
                    optionBackgrounds[idx].color = MedievalUITheme.BtnDisabled;
                }
                if (optionLetterBadges != null && idx < optionLetterBadges.Length && optionLetterBadges[idx] != null)
                {
                    optionLetterBadges[idx].color = MedievalUITheme.SlateBorder;
                }
                if (optionTexts != null && idx < optionTexts.Length && optionTexts[idx] != null)
                {
                    optionTexts[idx].color = MedievalUITheme.SlateMuted;
                    optionTexts[idx].text = $"<color=#6E7A8E>[Đã loại]</color> {optionTexts[idx].text}";
                }
            }

            AudioManager.Instance?.PlayButtonClick();
            RefreshHint5050UI();
        }

        private void RefreshHint5050UI()
        {
            if (hint5050Button == null) return;
            int gold = PlayerCampProgressManager.EnsureLoaded().Gold;
            bool hasFree = _freeHintsRemaining > 0 && _onTryConsumeFreeHint != null;
            bool canUse = !_isLocked && !_hint5050Used && _currentQuestion != null && (hasFree || gold >= Hint5050GoldCost);
            hint5050Button.interactable = canUse;
            if (hint5050Background != null)
            {
                hint5050Background.color = canUse
                    ? (hasFree ? MedievalUITheme.BtnHeal : MedievalUITheme.BtnPrimaryGold)
                    : MedievalUITheme.BtnDisabled;
            }
            if (hint5050LabelTMP != null)
            {
                if (_hint5050Used) hint5050LabelTMP.text = "ĐÃ DÙNG 50:50";
                else if (hasFree) hint5050LabelTMP.text = $"50:50 [H] (MIỄN PHÍ x{_freeHintsRemaining})";
                else if (gold < Hint5050GoldCost) hint5050LabelTMP.text = $"50:50 (Thiếu {Hint5050GoldCost}V)";
                else hint5050LabelTMP.text = $"50:50 [H] (-{Hint5050GoldCost}V)";
            }
        }

        private void EnsureExtraControlsExist()
        {
            EnsureHintButtonExists();
            if (pendingActionText == null) return;
            Transform card = pendingActionText.transform.parent;
            if (card == null) return;

            if (_cardRect == null) _cardRect = card as RectTransform;
            if (headerTitleText == null)
            {
                Transform hdr = card.Find("Header/Title");
                if (hdr != null) headerTitleText = hdr.GetComponent<TextMeshProUGUI>();
            }

            if (skipGuardButton == null)
            {
                Transform existingSkip = card.Find("SkipGuardButton");
                if (existingSkip != null)
                {
                    skipGuardButton = existingSkip.GetComponent<Button>();
                }
                else if (hint5050Button != null)
                {
                    GameObject skipGo = new GameObject("SkipGuardButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    skipGo.transform.SetParent(card, false);
                    Image skipBg = skipGo.GetComponent<Image>();
                    if (hint5050Background != null)
                    {
                        skipBg.sprite = hint5050Background.sprite;
                        skipBg.type = hint5050Background.type;
                        skipBg.pixelsPerUnitMultiplier = hint5050Background.pixelsPerUnitMultiplier;
                    }
                    skipBg.color = MedievalUITheme.BtnDangerSlate;

                    RectTransform sRt = skipBg.rectTransform;
                    sRt.anchorMin = new Vector2(1f, 1f);
                    sRt.anchorMax = new Vector2(1f, 1f);
                    sRt.pivot = new Vector2(1f, 1f);
                    sRt.anchoredPosition = new Vector2(-226f, -69f);
                    sRt.sizeDelta = new Vector2(148f, 32f);

                    skipGuardButton = skipGo.GetComponent<Button>();
                    skipGuardButton.targetGraphic = skipBg;

                    GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lblGo.transform.SetParent(skipGo.transform, false);
                    TextMeshProUGUI sLbl = lblGo.GetComponent<TextMeshProUGUI>();
                    if (hint5050LabelTMP != null) sLbl.font = hint5050LabelTMP.font;
                    sLbl.text = "NHẬN ĐÒN [Esc]";
                    sLbl.fontSize = 12.5f;
                    sLbl.fontStyle = FontStyles.Bold;
                    sLbl.color = MedievalUITheme.IvoryText;
                    sLbl.alignment = TextAlignmentOptions.Center;
                    sLbl.raycastTarget = false;
                    RectTransform lRt = sLbl.rectTransform;
                    lRt.anchorMin = Vector2.zero;
                    lRt.anchorMax = Vector2.one;
                    lRt.offsetMin = new Vector2(6f, 2f);
                    lRt.offsetMax = new Vector2(-6f, -2f);
                    skipGo.SetActive(false);
                }
            }
        }

        private void StartCardPopAnimation()
        {
            if (_cardRect == null && pendingActionText != null)
            {
                _cardRect = pendingActionText.transform.parent as RectTransform;
            }
            if (_cardRect != null)
            {
                _cardRect.localScale = Vector3.one * 0.88f;
                _cardPopElapsed = 0f;
                _isPopAnimating = true;
            }
        }

        private void EnsureHintButtonExists()
        {
            if (hint5050Button != null || pendingActionText == null) return;
            Transform card = pendingActionText.transform.parent;
            if (card == null) return;

            Transform existing = card.Find("Hint5050Button");
            if (existing != null)
            {
                hint5050Button = existing.GetComponent<Button>();
                hint5050Background = existing.GetComponent<Image>();
                hint5050LabelTMP = existing.GetComponentInChildren<TextMeshProUGUI>(true);
                return;
            }

            RectTransform pRt = pendingActionText.rectTransform;
            pRt.anchorMin = new Vector2(0f, 1f);
            pRt.anchorMax = new Vector2(1f, 1f);
            pRt.pivot = new Vector2(0f, 1f);
            pRt.anchoredPosition = new Vector2(24f, -72f);
            pRt.sizeDelta = new Vector2(-246f, 28f);

            GameObject btnGo = new GameObject("Hint5050Button", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(card, false);
            hint5050Background = btnGo.GetComponent<Image>();
            if (continueButton != null && continueButton.targetGraphic is Image contImg)
            {
                hint5050Background.sprite = contImg.sprite;
                hint5050Background.type = contImg.type;
                hint5050Background.pixelsPerUnitMultiplier = contImg.pixelsPerUnitMultiplier;
            }
            hint5050Background.color = MedievalUITheme.BtnPrimaryGold;

            RectTransform bRt = hint5050Background.rectTransform;
            bRt.anchorMin = new Vector2(1f, 1f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.pivot = new Vector2(1f, 1f);
            bRt.anchoredPosition = new Vector2(-24f, -69f);
            bRt.sizeDelta = new Vector2(194f, 32f);

            hint5050Button = btnGo.GetComponent<Button>();
            hint5050Button.targetGraphic = hint5050Background;

            GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(btnGo.transform, false);
            hint5050LabelTMP = lblGo.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI contLbl = continueButton != null ? continueButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (contLbl != null) hint5050LabelTMP.font = contLbl.font;
            else if (categoryText != null) hint5050LabelTMP.font = categoryText.font;
            hint5050LabelTMP.text = $"50:50 [H] (-{Hint5050GoldCost}V)";
            hint5050LabelTMP.fontSize = 13.5f;
            hint5050LabelTMP.enableAutoSizing = true;
            hint5050LabelTMP.fontSizeMin = 10.5f;
            hint5050LabelTMP.fontSizeMax = 13.5f;
            hint5050LabelTMP.fontStyle = FontStyles.Bold;
            hint5050LabelTMP.color = MedievalUITheme.IvoryText;
            hint5050LabelTMP.alignment = TextAlignmentOptions.Center;
            hint5050LabelTMP.raycastTarget = false;

            RectTransform lRt = hint5050LabelTMP.rectTransform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(8f, 2f);
            lRt.offsetMax = new Vector2(-8f, -2f);
        }

        private void SubmitOption(int index, bool skipped = false)
        {
            if (_isLocked) return;
            if (index >= 0 && index < 4 && _eliminatedOptions[index]) return;
            _isLocked = true;
            _lastWasSkipped = skipped;
            _lastWasQuickMind = !skipped && index >= 0 && _questionElapsed <= _activeQuickThreshold;
            _onAnswerWithModeFlags?.Invoke(index, _lastWasQuickMind, skipped);
            _onAnswerSubmittedWithSpeed?.Invoke(index, _lastWasQuickMind);
            _onAnswerSubmitted?.Invoke(index);
        }

        public void SkipDefensiveQuiz()
        {
            if (_isLocked || _mode != QuizPromptMode.DefensiveGuard) return;
            SubmitOption(-1, skipped: true);
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
                    questionTimerText.text = _questionElapsed <= _activeQuickThreshold
                        ? $"NHANH! ({Mathf.CeilToInt(_activeQuickThreshold - _questionElapsed)}s)"
                        : "KHÔNG GIỚI HẠN";
                }
                return;
            }

            float remaining = Mathf.Max(0f, _activeTimeLimit - _questionElapsed);
            float ratio = Mathf.Clamp01(remaining / _activeTimeLimit);
            bool inQuickWindow = _questionElapsed <= _activeQuickThreshold;

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
                string fastLabel = _mode == QuizPromptMode.DefensiveGuard ? $"PARRY! {sec}s" : $"NHANH! {sec}s";
                questionTimerText.text = inQuickWindow
                    ? fastLabel
                    : $"THỜI GIAN: {sec}s";
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            if (_isPopAnimating && _cardRect != null)
            {
                _cardPopElapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(_cardPopElapsed / 0.22f);
                _cardRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, MedievalEase.OutBack(p));
                if (p >= 1f) _isPopAnimating = false;
            }

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

                if (_timerEnabledForCurrentQuestion && _questionElapsed >= _activeTimeLimit)
                {
                    SubmitOption(-1, skipped: false);
                    return;
                }

                Keyboard kb = Keyboard.current;
                if (kb == null) return;

                if (kb.escapeKey.wasPressedThisFrame && _mode == QuizPromptMode.DefensiveGuard) SkipDefensiveQuiz();
                else if (kb.hKey.wasPressedThisFrame) UseHint5050();
                else if (kb.aKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SubmitOption(0);
                else if (kb.bKey.wasPressedThisFrame || kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SubmitOption(1);
                else if (kb.cKey.wasPressedThisFrame || kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SubmitOption(2);
                else if (kb.dKey.wasPressedThisFrame || kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SubmitOption(3);
            }
        }
    }
}
