using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Data;
using MedievalRPG.Quiz;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Sổ Tay Ôn Lỗi Sai B1/B2 (V2): liệt kê câu trả lời sai trong trận và cho phép luyện lại để nhận +15 Vàng.
    /// </summary>
    public class MistakeNotebookModalUI : MonoBehaviour
    {
        [Header("Tài nguyên giao diện")]
        [SerializeField] private Sprite panelInsetSprite;
        [SerializeField] private Sprite buttonFillSprite;
        [SerializeField] private TMP_FontAsset fontBold;
        [SerializeField] private TMP_FontAsset fontBody;

        [Header("Danh sách lỗi sai")]
        [SerializeField] private TextMeshProUGUI headerSummaryTMP;
        [SerializeField] private Transform mistakeListContent;
        [SerializeField] private GameObject emptyHintObject;

        [Header("Khung ôn luyện trực tiếp")]
        [SerializeField] private TextMeshProUGUI practiceCategoryTMP;
        [SerializeField] private TextMeshProUGUI practiceQuestionTMP;
        [SerializeField] private Button[] optionButtons = new Button[4];
        [SerializeField] private Image[] optionBackgrounds = new Image[4];
        [SerializeField] private TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI practiceFeedbackTMP;
        [SerializeField] private Button closeButton;

        private QuizQuestion _activeQuestion;
        private bool _isQuestionResolved;
        private Action _onCloseRequested;

        public bool IsOpen => gameObject.activeSelf;

        public void Configure(
            Sprite panelInset, Sprite btnFill,
            TMP_FontAsset boldFont, TMP_FontAsset bodyFont,
            TextMeshProUGUI summaryTMP, Transform listContent, GameObject emptyHint,
            TextMeshProUGUI pCatTMP, TextMeshProUGUI pQuestTMP,
            Button[] optBtns, Image[] optBgs, TextMeshProUGUI[] optTxts,
            TextMeshProUGUI pFeedbackTMP, Button closeBtn)
        {
            panelInsetSprite = panelInset;
            buttonFillSprite = btnFill;
            fontBold = boldFont;
            fontBody = bodyFont;
            headerSummaryTMP = summaryTMP;
            mistakeListContent = listContent;
            emptyHintObject = emptyHint;
            practiceCategoryTMP = pCatTMP;
            practiceQuestionTMP = pQuestTMP;
            optionButtons = optBtns;
            optionBackgrounds = optBgs;
            optionTexts = optTxts;
            practiceFeedbackTMP = pFeedbackTMP;
            closeButton = closeBtn;

            WireControls();
        }

        private void Awake()
        {
            WireControls();
        }

        private void OnEnable()
        {
            PlayerCampProgressManager.EnsureLoaded().OnCampProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            PlayerCampProgressManager.Instance.OnCampProgressChanged -= HandleProgressChanged;
        }

        private void HandleProgressChanged()
        {
            if (gameObject.activeInHierarchy) RefreshList();
        }

        private void WireControls()
        {
            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    if (optionButtons[i] == null) continue;
                    int idx = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SubmitPracticeOption(idx));
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    Hide();
                });
            }
        }

        public void Show(Action onClose = null)
        {
            _onCloseRequested = onClose;
            _activeQuestion = null;
            _isQuestionResolved = false;
            gameObject.SetActive(true);
            RefreshList();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            _onCloseRequested?.Invoke();
        }

        public void RefreshList()
        {
            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
            List<QuizQuestion> mistakes = QuizManager.ResolveMistakeQuestionsStatic(camp.MistakeQuestions);

            if (headerSummaryTMP != null)
            {
                headerSummaryTMP.text = $"Cần ôn: {mistakes.Count} câu  |  Đã sửa đúng: {camp.MistakesReviewedCount} câu  |  Thưởng ôn đúng: +15 Vàng/câu";
            }

            if (mistakeListContent != null)
            {
                for (int i = mistakeListContent.childCount - 1; i >= 0; i--)
                {
                    Destroy(mistakeListContent.GetChild(i).gameObject);
                }
            }

            if (emptyHintObject != null)
            {
                emptyHintObject.SetActive(mistakes.Count == 0);
            }

            if (mistakeListContent != null)
            {
                for (int i = 0; i < mistakes.Count; i++)
                {
                    BuildMistakeRow(mistakes[i]);
                }
            }

            if (_activeQuestion == null && mistakes.Count > 0)
            {
                LoadPracticeQuestion(mistakes[0]);
            }
            else if (mistakes.Count == 0 && _activeQuestion == null)
            {
                ShowEmptyPracticeState();
            }
        }

        private void BuildMistakeRow(QuizQuestion q)
        {
            bool isSelected = _activeQuestion != null && string.Equals(_activeQuestion.questionText, q.questionText, StringComparison.Ordinal);

            GameObject rowGo = new GameObject("MistakeRow");
            rowGo.transform.SetParent(mistakeListContent, false);
            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = panelInsetSprite;
            rowBg.type = panelInsetSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            rowBg.pixelsPerUnitMultiplier = 0.65f;
            rowBg.color = isSelected ? new Color(0.16f, 0.22f, 0.34f, 0.98f) : new Color(0.08f, 0.10f, 0.14f, 0.96f);

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 70f;
            le.minHeight = 70f;

            Button rowBtn = rowGo.AddComponent<Button>();
            rowBtn.targetGraphic = rowBg;
            QuizQuestion captured = q;
            rowBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButtonClick();
                LoadPracticeQuestion(captured);
                RefreshList();
            });

            GameObject catGo = new GameObject("Category");
            catGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI catTMP = catGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) catTMP.font = fontBold;
            catTMP.text = string.IsNullOrEmpty(q.category) ? "ENGLISH QUIZ" : q.category.ToUpperInvariant();
            catTMP.fontSize = 12f;
            catTMP.color = MedievalUITheme.GetCategoryColor(q.category);
            catTMP.alignment = TextAlignmentOptions.MidlineLeft;
            catTMP.raycastTarget = false;
            RectTransform cRt = catTMP.rectTransform;
            cRt.anchorMin = new Vector2(0f, 0.56f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.offsetMin = new Vector2(12f, 0f);
            cRt.offsetMax = new Vector2(-12f, -4f);

            GameObject qGo = new GameObject("Question");
            qGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI qTMP = qGo.AddComponent<TextMeshProUGUI>();
            if (fontBody != null) qTMP.font = fontBody;
            qTMP.text = q.questionText;
            qTMP.fontSize = 13f;
            qTMP.enableAutoSizing = true;
            qTMP.fontSizeMin = 10f;
            qTMP.fontSizeMax = 13f;
            qTMP.color = MedievalUITheme.IvoryText;
            qTMP.alignment = TextAlignmentOptions.MidlineLeft;
            qTMP.textWrappingMode = TextWrappingModes.NoWrap;
            qTMP.overflowMode = TextOverflowModes.Truncate;
            qTMP.raycastTarget = false;
            RectTransform qRt = qTMP.rectTransform;
            qRt.anchorMin = new Vector2(0f, 0.04f);
            qRt.anchorMax = new Vector2(1f, 0.56f);
            qRt.offsetMin = new Vector2(12f, 4f);
            qRt.offsetMax = new Vector2(-12f, 0f);
        }

        private void LoadPracticeQuestion(QuizQuestion q)
        {
            _activeQuestion = q;
            _isQuestionResolved = false;

            if (q == null)
            {
                ShowEmptyPracticeState();
                return;
            }

            if (practiceCategoryTMP != null)
            {
                practiceCategoryTMP.text = string.IsNullOrEmpty(q.category) ? "LUYỆN LẠI" : $"ÔN TẬP: {q.category.ToUpperInvariant()}";
                practiceCategoryTMP.color = MedievalUITheme.GetCategoryColor(q.category);
            }
            if (practiceQuestionTMP != null)
            {
                practiceQuestionTMP.text = q.questionText;
            }
            if (practiceFeedbackTMP != null)
            {
                practiceFeedbackTMP.text = "Chọn đáp án đúng để xóa câu này khỏi Sổ lỗi sai và nhận +15 Vàng.";
                practiceFeedbackTMP.color = MedievalUITheme.GoldSoft;
            }

            string[] letters = { "A", "B", "C", "D" };
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
                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                {
                    string ans = (q.answers != null && i < q.answers.Length) ? q.answers[i] : "";
                    optionTexts[i].text = $"{letters[i]}. {ans}";
                }
            }
        }

        private void ShowEmptyPracticeState()
        {
            if (practiceCategoryTMP != null)
            {
                practiceCategoryTMP.text = "HOÀN HẢO";
                practiceCategoryTMP.color = MedievalUITheme.SuccessGreen;
            }
            if (practiceQuestionTMP != null)
            {
                practiceQuestionTMP.text = "Hiện chưa có câu trả lời sai nào cần ôn tập. Hãy giữ vững phong độ trong các trận chiến!";
            }
            if (practiceFeedbackTMP != null)
            {
                practiceFeedbackTMP.text = "Khi trả lời sai câu B1/B2 trong trận, hệ thống sẽ tự động lưu vào Sổ Tay tại đây.";
                practiceFeedbackTMP.color = MedievalUITheme.SlateMuted;
            }
            for (int i = 0; i < 4; i++)
            {
                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null) optionButtons[i].interactable = false;
                if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null) optionBackgrounds[i].color = MedievalUITheme.BtnDisabled;
                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) optionTexts[i].text = "-";
            }
        }

        private void SubmitPracticeOption(int index)
        {
            if (_activeQuestion == null || _isQuestionResolved) return;

            bool isCorrect = _activeQuestion.IsCorrect(index);
            if (isCorrect)
            {
                _isQuestionResolved = true;
                for (int i = 0; i < 4; i++)
                {
                    if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null) optionButtons[i].interactable = false;
                    if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                    {
                        optionBackgrounds[i].color = i == _activeQuestion.correctAnswerIndex
                            ? MedievalUITheme.SuccessPanel
                            : MedievalUITheme.BtnDisabled;
                    }
                }

                PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
                bool removed = camp.ResolveQuizMistake(_activeQuestion.questionText);
                if (removed)
                {
                    camp.AddGold(15);
                    if (camp.MistakesReviewedCount >= 3)
                    {
                        camp.UnlockAchievement(AchievementId.MistakeReviewer);
                    }
                    AudioManager.Instance?.PlayCoinRewardSfx();
                }
                else
                {
                    AudioManager.Instance?.PlayQuizResult(true, 1);
                }

                if (practiceFeedbackTMP != null)
                {
                    practiceFeedbackTMP.text = removed
                        ? $"CHÍNH XÁC! Đã xóa khỏi Sổ lỗi sai (+15 Vàng)!\nGiải thích: {_activeQuestion.explanation}"
                        : $"CHÍNH XÁC!\nGiải thích: {_activeQuestion.explanation}";
                    practiceFeedbackTMP.color = MedievalUITheme.SuccessGreen;
                }

                _activeQuestion = null;
                RefreshList();
            }
            else
            {
                AudioManager.Instance?.PlayQuizResult(false, 0);
                if (optionBackgrounds != null && index >= 0 && index < optionBackgrounds.Length && optionBackgrounds[index] != null)
                {
                    optionBackgrounds[index].color = MedievalUITheme.DangerPanel;
                }
                if (practiceFeedbackTMP != null)
                {
                    practiceFeedbackTMP.text = $"Chưa đúng! Gợi ý ngữ pháp: {_activeQuestion.explanation}";
                    practiceFeedbackTMP.color = new Color(1f, 0.72f, 0.72f, 1f);
                }
            }
        }
    }
}
