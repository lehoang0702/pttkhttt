using System;
using System.Collections;
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

        public void OverrideFonts(TMP_FontAsset bold, TMP_FontAsset body)
        {
            if (bold != null) fontBold = bold;
            if (body != null) fontBody = body;
        }

        [Header("Danh sách lỗi sai & Chủ đề")]
        [SerializeField] private TextMeshProUGUI headerSummaryTMP;
        [SerializeField] private TextMeshProUGUI weaknessSummaryTMP;
        [SerializeField] private Transform mistakeListContent;
        [SerializeField] private GameObject emptyHintObject;
        [SerializeField] private Button mistakesTabButton;
        [SerializeField] private Image mistakesTabBg;
        [SerializeField] private TextMeshProUGUI mistakesTabLabel;
        [SerializeField] private Button topicsTabButton;
        [SerializeField] private Image topicsTabBg;
        [SerializeField] private TextMeshProUGUI topicsTabLabel;
        [SerializeField] private Button weakestTopicButton;

        [Header("Khung ôn luyện trực tiếp")]
        [SerializeField] private TextMeshProUGUI practiceCategoryTMP;
        [SerializeField] private TextMeshProUGUI practiceQuestionTMP;
        [SerializeField] private Button[] optionButtons = new Button[4];
        [SerializeField] private Image[] optionBackgrounds = new Image[4];
        [SerializeField] private TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI practiceFeedbackTMP;
        [SerializeField] private Button closeButton;

        private enum NotebookTabMode
        {
            Mistakes = 0,
            Topics = 1
        }

        private NotebookTabMode _tabMode = NotebookTabMode.Mistakes;
        private string _selectedPracticeTopic;
        private bool _isTopicPracticeMode;
        private bool _hasRecordedCurrentAttempt;
        private QuizQuestion _activeQuestion;
        private bool _isQuestionResolved;
        private bool _suppressProgressRefresh;
        private Coroutine _advanceCoroutine;
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

        public void ConfigureWeaknessSummary(TextMeshProUGUI weaknessTMP)
        {
            weaknessSummaryTMP = weaknessTMP;
        }

        public void ConfigureTopicTabs(
            Button mTabBtn, Image mTabBg, TextMeshProUGUI mTabLbl,
            Button tTabBtn, Image tTabBg, TextMeshProUGUI tTabLbl,
            Button weakestBtn)
        {
            mistakesTabButton = mTabBtn;
            mistakesTabBg = mTabBg;
            mistakesTabLabel = mTabLbl;
            topicsTabButton = tTabBtn;
            topicsTabBg = tTabBg;
            topicsTabLabel = tTabLbl;
            weakestTopicButton = weakestBtn;
            WireTabButtons();
        }

        private void Awake()
        {
            WireControls();
            EnsureWeaknessSummaryUI();
            EnsureTopicTabsUI();
        }

        private void OnEnable()
        {
            PlayerCampProgressManager.EnsureLoaded().OnCampProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            CancelPendingAdvance();
            PlayerCampProgressManager.Instance.OnCampProgressChanged -= HandleProgressChanged;
        }

        private void HandleProgressChanged()
        {
            if (!_suppressProgressRefresh && gameObject.activeInHierarchy) RefreshList();
        }

        private void CancelPendingAdvance()
        {
            if (_advanceCoroutine != null)
            {
                StopCoroutine(_advanceCoroutine);
                _advanceCoroutine = null;
            }
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

            WireTabButtons();
        }

        private void WireTabButtons()
        {
            if (mistakesTabButton != null)
            {
                mistakesTabButton.onClick.RemoveAllListeners();
                mistakesTabButton.onClick.AddListener(() => SwitchTabMode(NotebookTabMode.Mistakes));
            }
            if (topicsTabButton != null)
            {
                topicsTabButton.onClick.RemoveAllListeners();
                topicsTabButton.onClick.AddListener(() => SwitchTabMode(NotebookTabMode.Topics));
            }
            if (weakestTopicButton != null)
            {
                weakestTopicButton.onClick.RemoveAllListeners();
                weakestTopicButton.onClick.AddListener(StartWeakestTopicPractice);
            }
        }

        private void SwitchTabMode(NotebookTabMode mode)
        {
            AudioManager.Instance?.PlayButtonClick();
            CancelPendingAdvance();
            _tabMode = mode;
            if (mode == NotebookTabMode.Topics)
            {
                if (string.IsNullOrEmpty(_selectedPracticeTopic))
                {
                    StartWeakestTopicPractice();
                    return;
                }
                StartTopicPractice(_selectedPracticeTopic);
                return;
            }

            _isTopicPracticeMode = false;
            _isQuestionResolved = false;
            _activeQuestion = null;
            RefreshList();
        }

        public void Show(Action onClose = null)
        {
            CancelPendingAdvance();
            EnsureWeaknessSummaryUI();
            EnsureTopicTabsUI();
            _onCloseRequested = onClose;
            _activeQuestion = null;
            _isQuestionResolved = false;
            _isTopicPracticeMode = _tabMode == NotebookTabMode.Topics;
            gameObject.SetActive(true);
            RefreshList();
        }

        public void Hide()
        {
            CancelPendingAdvance();
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            _onCloseRequested?.Invoke();
        }

        public void RefreshList()
        {
            EnsureWeaknessSummaryUI();
            EnsureTopicTabsUI();

            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
            List<QuizQuestion> mistakes = QuizManager.ResolveMistakeQuestionsStatic(camp.MistakeQuestions);
            mistakes.Sort((a, b) => camp.GetMistakeWrongCount(b.questionText).CompareTo(camp.GetMistakeWrongCount(a.questionText)));

            if (headerSummaryTMP != null)
            {
                camp.GetOverallQuizAccuracy(out int totalAns, out int totalCor);
                string accPart = totalAns > 0
                    ? $"  |  Đúng: {Mathf.RoundToInt((float)totalCor / totalAns * 100f)}% ({totalCor}/{totalAns})"
                    : "";
                headerSummaryTMP.text = $"Lỗi cần ôn: {mistakes.Count} câu (+15V)  |  Đã sửa: {camp.MistakesReviewedCount}  |  Luyện chủ đề (+5V){accPart}";
            }
            if (weaknessSummaryTMP != null)
            {
                weaknessSummaryTMP.text = camp.GetWeakestTopicsSummary(4);
            }

            UpdateTabVisualState(mistakes.Count);

            if (mistakeListContent != null)
            {
                for (int i = mistakeListContent.childCount - 1; i >= 0; i--)
                {
                    Destroy(mistakeListContent.GetChild(i).gameObject);
                }
            }

            if (_tabMode == NotebookTabMode.Topics)
            {
                if (emptyHintObject != null) emptyHintObject.SetActive(false);
                BuildTopicMasteryRows(camp);
                if (!_isQuestionResolved && _activeQuestion == null)
                {
                    if (!string.IsNullOrEmpty(_selectedPracticeTopic))
                    {
                        LoadPracticeQuestion(
                            QuizManager.GetQuestionForTopicStatic(_selectedPracticeTopic, camp.SelectedQuizFilter),
                            isTopicPractice: true);
                    }
                    else
                    {
                        ShowEmptyPracticeState();
                    }
                }
                return;
            }

            if (emptyHintObject != null)
            {
                emptyHintObject.SetActive(mistakes.Count == 0);
            }

            if (mistakeListContent != null)
            {
                for (int i = 0; i < mistakes.Count; i++)
                {
                    BuildMistakeRow(mistakes[i], camp.GetMistakeWrongCount(mistakes[i].questionText));
                }
            }

            // Giữ nguyên lời giải thích xanh lá trên khung luyện tập cho tới khi chuyển câu mới
            if (_isQuestionResolved) return;

            if (_activeQuestion == null && mistakes.Count > 0)
            {
                LoadPracticeQuestion(mistakes[0], isTopicPractice: false);
            }
            else if (mistakes.Count == 0 && _activeQuestion == null)
            {
                ShowEmptyPracticeState();
            }
        }

        private void UpdateTabVisualState(int mistakeCount)
        {
            if (mistakesTabLabel != null)
            {
                mistakesTabLabel.text = mistakeCount > 0 ? $"Lỗi Sai ({mistakeCount})" : "Lỗi Sai";
                mistakesTabLabel.color = _tabMode == NotebookTabMode.Mistakes ? MedievalUITheme.GoldSoft : MedievalUITheme.SlateMuted;
            }
            if (mistakesTabBg != null)
            {
                mistakesTabBg.color = _tabMode == NotebookTabMode.Mistakes
                    ? new Color(0.24f, 0.28f, 0.42f, 0.98f)
                    : MedievalUITheme.BtnNeutralSlate;
            }
            if (topicsTabLabel != null)
            {
                topicsTabLabel.color = _tabMode == NotebookTabMode.Topics ? MedievalUITheme.GoldSoft : MedievalUITheme.SlateMuted;
            }
            if (topicsTabBg != null)
            {
                topicsTabBg.color = _tabMode == NotebookTabMode.Topics
                    ? new Color(0.24f, 0.28f, 0.42f, 0.98f)
                    : MedievalUITheme.BtnNeutralSlate;
            }
        }

        private void EnsureTopicTabsUI()
        {
            if (mistakesTabButton != null && topicsTabButton != null && weakestTopicButton != null) return;
            if (mistakeListContent == null) return;

            Transform scroll = mistakeListContent.parent != null ? mistakeListContent.parent.parent : null;
            if (scroll == null) return;
            Transform leftCol = scroll.parent;
            if (leftCol == null) return;

            Transform existingBar = leftCol.Find("NotebookTabsBar");
            if (existingBar != null)
            {
                mistakesTabButton = existingBar.Find("TabMistakesBtn")?.GetComponent<Button>();
                mistakesTabBg = existingBar.Find("TabMistakesBtn")?.GetComponent<Image>();
                mistakesTabLabel = existingBar.Find("TabMistakesBtn/Label")?.GetComponent<TextMeshProUGUI>();
                topicsTabButton = existingBar.Find("TabTopicsBtn")?.GetComponent<Button>();
                topicsTabBg = existingBar.Find("TabTopicsBtn")?.GetComponent<Image>();
                topicsTabLabel = existingBar.Find("TabTopicsBtn/Label")?.GetComponent<TextMeshProUGUI>();
                weakestTopicButton = existingBar.Find("WeakestBtn")?.GetComponent<Button>();
                WireTabButtons();
                return;
            }

            if (scroll is RectTransform scrollRt)
            {
                scrollRt.offsetMax = new Vector2(scrollRt.offsetMax.x, -54f);
            }

            GameObject bar = new GameObject("NotebookTabsBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(leftCol, false);
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.offsetMin = new Vector2(8f, -48f);
            barRt.offsetMax = new Vector2(-8f, -8f);

            HorizontalLayoutGroup hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            (mistakesTabButton, mistakesTabBg, mistakesTabLabel) = CreateCompactTabButton(
                "TabMistakesBtn", bar.transform, "Lỗi Sai", MedievalUITheme.BtnNeutralSlate, MedievalUITheme.GoldSoft);
            (topicsTabButton, topicsTabBg, topicsTabLabel) = CreateCompactTabButton(
                "TabTopicsBtn", bar.transform, "Chủ Đề B1/B2", MedievalUITheme.BtnNeutralSlate, MedievalUITheme.SlateMuted);
            (weakestTopicButton, _, _) = CreateCompactTabButton(
                "WeakestBtn", bar.transform, "Luyện Yếu", new Color(0.42f, 0.24f, 0.14f, 0.98f), new Color(1f, 0.88f, 0.58f, 1f));

            WireTabButtons();
        }

        private (Button btn, Image bg, TextMeshProUGUI lbl) CreateCompactTabButton(
            string name, Transform parent, string text, Color bgColor, Color textColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image bg = go.GetComponent<Image>();
            bg.sprite = buttonFillSprite != null ? buttonFillSprite : panelInsetSprite;
            bg.type = bg.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bg.pixelsPerUnitMultiplier = 0.65f;
            bg.color = bgColor;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;

            GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI lbl = lblGo.GetComponent<TextMeshProUGUI>();
            if (fontBold != null) lbl.font = fontBold;
            lbl.text = text;
            lbl.fontSize = 13f;
            lbl.enableAutoSizing = true;
            lbl.fontSizeMin = 10f;
            lbl.fontSizeMax = 13.5f;
            lbl.color = textColor;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
            RectTransform rt = lbl.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);

            return (btn, bg, lbl);
        }

        private void EnsureWeaknessSummaryUI()
        {
            if (weaknessSummaryTMP != null || headerSummaryTMP == null) return;
            Transform strip = headerSummaryTMP.transform.parent;
            if (strip == null) return;

            Transform existing = strip.Find("WeaknessSummary");
            if (existing != null)
            {
                weaknessSummaryTMP = existing.GetComponent<TextMeshProUGUI>();
                if (weaknessSummaryTMP != null) return;
            }

            if (strip is RectTransform stripRt && stripRt.rect.height < 48f)
            {
                stripRt.offsetMin = new Vector2(stripRt.offsetMin.x, stripRt.offsetMin.y - 18f);
                Transform card = strip.parent;
                if (card != null)
                {
                    if (card.Find("MistakesListColumn") is RectTransform leftRt && leftRt.offsetMax.y > -120f)
                    {
                        leftRt.offsetMax = new Vector2(leftRt.offsetMax.x, -126f);
                    }
                    if (card.Find("PracticeColumn") is RectTransform rightRt && rightRt.offsetMax.y > -120f)
                    {
                        rightRt.offsetMax = new Vector2(rightRt.offsetMax.x, -126f);
                    }
                }
            }

            RectTransform hdrRt = headerSummaryTMP.rectTransform;
            hdrRt.anchorMin = new Vector2(0f, 0.50f);
            hdrRt.anchorMax = new Vector2(1f, 1f);
            hdrRt.offsetMin = new Vector2(14f, 0f);
            hdrRt.offsetMax = new Vector2(-14f, -3f);

            GameObject weakGo = new GameObject("WeaknessSummary", typeof(RectTransform), typeof(TextMeshProUGUI));
            weakGo.transform.SetParent(strip, false);
            weaknessSummaryTMP = weakGo.GetComponent<TextMeshProUGUI>();
            weaknessSummaryTMP.font = fontBody != null ? fontBody : headerSummaryTMP.font;
            weaknessSummaryTMP.fontSize = 12.5f;
            weaknessSummaryTMP.enableAutoSizing = true;
            weaknessSummaryTMP.fontSizeMin = 10f;
            weaknessSummaryTMP.fontSizeMax = 12.5f;
            weaknessSummaryTMP.color = MedievalUITheme.IvoryText;
            weaknessSummaryTMP.alignment = TextAlignmentOptions.MidlineLeft;
            weaknessSummaryTMP.textWrappingMode = TextWrappingModes.NoWrap;
            weaknessSummaryTMP.overflowMode = TextOverflowModes.Truncate;
            weaknessSummaryTMP.raycastTarget = false;

            RectTransform wRt = weaknessSummaryTMP.rectTransform;
            wRt.anchorMin = new Vector2(0f, 0f);
            wRt.anchorMax = new Vector2(1f, 0.50f);
            wRt.offsetMin = new Vector2(14f, 3f);
            wRt.offsetMax = new Vector2(-14f, 0f);
        }

        private void BuildMistakeRow(QuizQuestion q, int wrongCount)
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
            le.preferredHeight = 82f;
            le.minHeight = 82f;

            Button rowBtn = rowGo.AddComponent<Button>();
            rowBtn.targetGraphic = rowBg;
            QuizQuestion captured = q;
            rowBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButtonClick();
                _tabMode = NotebookTabMode.Mistakes;
                LoadPracticeQuestion(captured, isTopicPractice: false);
                RefreshList();
            });

            GameObject catGo = new GameObject("Category");
            catGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI catTMP = catGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) catTMP.font = fontBold;
            string baseCat = string.IsNullOrEmpty(q.category) ? "ENGLISH QUIZ" : q.category.ToUpperInvariant();
            catTMP.text = wrongCount > 1 ? $"{baseCat}  •  SAI {wrongCount} LẦN" : baseCat;
            catTMP.fontSize = 12f;
            catTMP.color = MedievalUITheme.GetCategoryColor(q.category);
            catTMP.alignment = TextAlignmentOptions.MidlineLeft;
            catTMP.raycastTarget = false;
            RectTransform cRt = catTMP.rectTransform;
            cRt.anchorMin = new Vector2(0f, 0.62f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.offsetMin = new Vector2(12f, 0f);
            cRt.offsetMax = new Vector2(-12f, -4f);

            GameObject qGo = new GameObject("Question");
            qGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI qTMP = qGo.AddComponent<TextMeshProUGUI>();
            if (fontBody != null) qTMP.font = fontBody;
            qTMP.text = q.questionText;
            qTMP.fontSize = 13.5f;
            qTMP.enableAutoSizing = true;
            qTMP.fontSizeMin = 11f;
            qTMP.fontSizeMax = 13.5f;
            qTMP.color = MedievalUITheme.IvoryText;
            qTMP.alignment = TextAlignmentOptions.TopLeft;
            qTMP.textWrappingMode = TextWrappingModes.Normal;
            qTMP.overflowMode = TextOverflowModes.Ellipsis;
            qTMP.raycastTarget = false;
            RectTransform qRt = qTMP.rectTransform;
            qRt.anchorMin = new Vector2(0f, 0.04f);
            qRt.anchorMax = new Vector2(1f, 0.62f);
            qRt.offsetMin = new Vector2(12f, 4f);
            qRt.offsetMax = new Vector2(-12f, -2f);
        }

        private void BuildTopicMasteryRows(PlayerCampProgressManager camp)
        {
            if (mistakeListContent == null) return;

            List<string> allTopics = QuizManager.GetAllTopicKeysStatic();
            List<TopicQuizStatEntry> recorded = camp.GetTopicStatsSortedByWeakness();
            Dictionary<string, TopicQuizStatEntry> lookup = new Dictionary<string, TopicQuizStatEntry>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < recorded.Count; i++)
            {
                if (recorded[i] != null && !string.IsNullOrEmpty(recorded[i].topicKey))
                {
                    lookup[recorded[i].topicKey] = recorded[i];
                }
            }

            List<TopicQuizStatEntry> rows = new List<TopicQuizStatEntry>(recorded.Count + allTopics.Count);
            for (int i = 0; i < recorded.Count; i++)
            {
                rows.Add(recorded[i]);
            }

            List<string> unpracticed = new List<string>();
            for (int i = 0; i < allTopics.Count; i++)
            {
                string key = allTopics[i];
                if (!lookup.ContainsKey(key))
                {
                    unpracticed.Add(key);
                }
            }
            unpracticed.Sort(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < unpracticed.Count; i++)
            {
                rows.Add(new TopicQuizStatEntry { topicKey = unpracticed[i], answeredCount = 0, correctCount = 0 });
            }

            for (int i = 0; i < rows.Count; i++)
            {
                BuildTopicRow(rows[i]);
            }
        }

        private void BuildTopicRow(TopicQuizStatEntry entry)
        {
            if (entry == null) return;
            bool isSelected = _tabMode == NotebookTabMode.Topics &&
                !string.IsNullOrEmpty(_selectedPracticeTopic) &&
                string.Equals(_selectedPracticeTopic, entry.topicKey, StringComparison.OrdinalIgnoreCase);

            GameObject rowGo = new GameObject($"TopicRow_{entry.topicKey}");
            rowGo.transform.SetParent(mistakeListContent, false);
            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = panelInsetSprite;
            rowBg.type = panelInsetSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            rowBg.pixelsPerUnitMultiplier = 0.65f;
            rowBg.color = isSelected ? new Color(0.16f, 0.24f, 0.36f, 0.98f) : new Color(0.08f, 0.10f, 0.14f, 0.96f);

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 76f;
            le.minHeight = 76f;

            Button rowBtn = rowGo.AddComponent<Button>();
            rowBtn.targetGraphic = rowBg;
            string capturedTopic = entry.topicKey;
            rowBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButtonClick();
                StartTopicPractice(capturedTopic);
            });

            GameObject titleGo = new GameObject("TopicName", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI titleTMP = titleGo.GetComponent<TextMeshProUGUI>();
            if (fontBold != null) titleTMP.font = fontBold;
            titleTMP.text = entry.topicKey.ToUpperInvariant();
            titleTMP.fontSize = 13.5f;
            titleTMP.color = MedievalUITheme.GoldSoft;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.raycastTarget = false;
            RectTransform tRt = titleTMP.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0.50f);
            tRt.anchorMax = new Vector2(0.58f, 1f);
            tRt.offsetMin = new Vector2(12f, 0f);
            tRt.offsetMax = new Vector2(0f, -6f);

            float ratio = entry.answeredCount > 0 ? Mathf.Clamp01((float)entry.correctCount / entry.answeredCount) : 0f;
            int pct = Mathf.RoundToInt(ratio * 100f);
            Color barColor = entry.answeredCount == 0
                ? MedievalUITheme.SlateMuted
                : (pct < 60 ? new Color(0.92f, 0.36f, 0.34f, 1f)
                    : (pct < 80 ? MedievalUITheme.GoldPrimary : MedievalUITheme.SuccessGreen));

            GameObject statGo = new GameObject("TopicStat", typeof(RectTransform), typeof(TextMeshProUGUI));
            statGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI statTMP = statGo.GetComponent<TextMeshProUGUI>();
            if (fontBold != null) statTMP.font = fontBold;
            statTMP.text = entry.answeredCount > 0 ? $"{pct}% ({entry.correctCount}/{entry.answeredCount})" : "Chưa luyện";
            statTMP.fontSize = 12.5f;
            statTMP.color = barColor;
            statTMP.alignment = TextAlignmentOptions.MidlineRight;
            statTMP.raycastTarget = false;
            RectTransform sRt = statTMP.rectTransform;
            sRt.anchorMin = new Vector2(0.56f, 0.50f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = new Vector2(-12f, -6f);

            GameObject barBgGo = new GameObject("MasteryBarBg", typeof(RectTransform), typeof(Image));
            barBgGo.transform.SetParent(rowGo.transform, false);
            Image barBg = barBgGo.GetComponent<Image>();
            barBg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            barBg.raycastTarget = false;
            RectTransform bgRt = barBg.rectTransform;
            bgRt.anchorMin = new Vector2(0f, 0.20f);
            bgRt.anchorMax = new Vector2(1f, 0.42f);
            bgRt.offsetMin = new Vector2(12f, 0f);
            bgRt.offsetMax = new Vector2(-12f, 0f);

            GameObject fillGo = new GameObject("MasteryFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barBgGo.transform, false);
            Image fill = fillGo.GetComponent<Image>();
            fill.sprite = buttonFillSprite != null ? buttonFillSprite : panelInsetSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = entry.answeredCount > 0 ? Mathf.Max(0.04f, ratio) : 0f;
            fill.color = barColor;
            fill.raycastTarget = false;
            RectTransform fRt = fill.rectTransform;
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(2f, 2f);
            fRt.offsetMax = new Vector2(-2f, -2f);
        }

        public void StartWeakestTopicPractice()
        {
            AudioManager.Instance?.PlayButtonClick();
            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
            List<TopicQuizStatEntry> sorted = camp.GetTopicStatsSortedByWeakness();
            string targetTopic = null;
            if (sorted.Count > 0 && !string.IsNullOrEmpty(sorted[0].topicKey))
            {
                targetTopic = sorted[0].topicKey;
            }
            else
            {
                List<string> allTopics = QuizManager.GetAllTopicKeysStatic();
                if (allTopics.Count > 0) targetTopic = allTopics[0];
            }

            StartTopicPractice(targetTopic);
        }

        public void StartTopicPractice(string topicKey)
        {
            _tabMode = NotebookTabMode.Topics;
            _selectedPracticeTopic = topicKey;
            _isTopicPracticeMode = true;
            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
            QuizQuestion q = QuizManager.GetQuestionForTopicStatic(topicKey, camp.SelectedQuizFilter);
            LoadPracticeQuestion(q, isTopicPractice: true);
            RefreshList();
        }

        private void LoadPracticeQuestion(QuizQuestion q, bool isTopicPractice = false)
        {
            CancelPendingAdvance();
            _isTopicPracticeMode = isTopicPractice;
            _hasRecordedCurrentAttempt = false;
            if (q == null)
            {
                _activeQuestion = null;
                _isQuestionResolved = false;
                ShowEmptyPracticeState();
                return;
            }

            _activeQuestion = q.CreateShuffledCopy();
            _isQuestionResolved = false;

            if (practiceCategoryTMP != null)
            {
                string prefix = isTopicPractice ? "LUYỆN CHỦ ĐỀ" : "ÔN LỖI SAI";
                practiceCategoryTMP.text = string.IsNullOrEmpty(_activeQuestion.category)
                    ? prefix
                    : $"{prefix}: {_activeQuestion.category.ToUpperInvariant()}";
                practiceCategoryTMP.color = MedievalUITheme.GetCategoryColor(_activeQuestion.category);
            }
            if (practiceQuestionTMP != null)
            {
                practiceQuestionTMP.text = _activeQuestion.questionText;
            }
            if (practiceFeedbackTMP != null)
            {
                practiceFeedbackTMP.text = isTopicPractice
                    ? "Trả lời đúng để tăng điểm thành thạo chủ đề và nhận +5 Vàng."
                    : "Chọn đáp án đúng để xóa câu này khỏi Sổ lỗi sai và nhận +15 Vàng.";
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
                    string ans = (_activeQuestion.answers != null && i < _activeQuestion.answers.Length) ? _activeQuestion.answers[i] : "";
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
                practiceQuestionTMP.text = "Hiện chưa có câu trả lời sai nào cần ôn tập. Bạn có thể bấm tab [Chủ Đề B1/B2] hoặc [Luyện Yếu] để rèn thêm!";
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

            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
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

                bool removed = false;
                int goldEarned = 0;
                _suppressProgressRefresh = true;
                try
                {
                    if (_isTopicPracticeMode && !_hasRecordedCurrentAttempt)
                    {
                        _hasRecordedCurrentAttempt = true;
                        camp.RecordQuizAttemptStats(_activeQuestion.category, true, saveImmediate: false);
                    }

                    removed = camp.ResolveQuizMistake(_activeQuestion.questionText);
                    if (removed)
                    {
                        goldEarned = 15;
                        camp.AddGold(goldEarned);
                    }
                    else if (_isTopicPracticeMode)
                    {
                        goldEarned = 5;
                        camp.AddGold(goldEarned);
                    }
                }
                finally
                {
                    _suppressProgressRefresh = false;
                }

                if (goldEarned > 0)
                {
                    AudioManager.Instance?.PlayCoinRewardSfx();
                }
                else
                {
                    AudioManager.Instance?.PlayQuizResult(true, 1);
                }

                if (practiceFeedbackTMP != null)
                {
                    if (removed)
                    {
                        practiceFeedbackTMP.text = $"CHÍNH XÁC! Đã xóa khỏi Sổ lỗi sai (+15 Vàng)!\nGiải thích: {_activeQuestion.explanation}";
                    }
                    else if (goldEarned > 0)
                    {
                        practiceFeedbackTMP.text = $"CHÍNH XÁC! (+{goldEarned} Vàng)\nGiải thích: {_activeQuestion.explanation}";
                    }
                    else
                    {
                        practiceFeedbackTMP.text = $"CHÍNH XÁC!\nGiải thích: {_activeQuestion.explanation}";
                    }
                    practiceFeedbackTMP.color = MedievalUITheme.SuccessGreen;
                }

                RefreshList();
                CancelPendingAdvance();
                if (gameObject.activeInHierarchy)
                {
                    _advanceCoroutine = StartCoroutine(AutoAdvanceAfterResolvedRoutine());
                }
            }
            else
            {
                AudioManager.Instance?.PlayQuizResult(false, 0);
                if (optionButtons != null && index >= 0 && index < optionButtons.Length && optionButtons[index] != null)
                {
                    optionButtons[index].interactable = false;
                }
                if (optionBackgrounds != null && index >= 0 && index < optionBackgrounds.Length && optionBackgrounds[index] != null)
                {
                    optionBackgrounds[index].color = MedievalUITheme.DangerPanel;
                }

                if (_isTopicPracticeMode && !_hasRecordedCurrentAttempt)
                {
                    _hasRecordedCurrentAttempt = true;
                    _suppressProgressRefresh = true;
                    try
                    {
                        camp.RecordQuizAttemptStats(_activeQuestion.category, false, saveImmediate: false);
                        camp.RecordQuizMistake(_activeQuestion.questionText);
                    }
                    finally
                    {
                        _suppressProgressRefresh = false;
                    }
                    RefreshList();
                }

                if (practiceFeedbackTMP != null)
                {
                    practiceFeedbackTMP.text = $"Chưa đúng! Gợi ý ngữ pháp: {_activeQuestion.explanation}";
                    practiceFeedbackTMP.color = new Color(1f, 0.72f, 0.72f, 1f);
                }
            }
        }

        private IEnumerator AutoAdvanceAfterResolvedRoutine()
        {
            yield return new WaitForSecondsRealtime(1.8f);
            _advanceCoroutine = null;
            if (!gameObject.activeInHierarchy || !_isQuestionResolved) yield break;
            _isQuestionResolved = false;

            if (_isTopicPracticeMode && !string.IsNullOrEmpty(_selectedPracticeTopic))
            {
                PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
                QuizQuestion next = QuizManager.GetQuestionForTopicStatic(_selectedPracticeTopic, camp.SelectedQuizFilter);
                LoadPracticeQuestion(next, isTopicPractice: true);
                RefreshList();
                yield break;
            }

            _activeQuestion = null;
            RefreshList();
        }
    }
}
