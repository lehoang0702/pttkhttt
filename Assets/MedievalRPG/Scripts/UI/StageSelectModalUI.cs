using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Data;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Bảng chọn Chương chiến dịch & Tháp Vô Tận (V2) kèm bộ lọc trình độ Quiz B1/B2.
    /// </summary>
    public class StageSelectModalUI : MonoBehaviour
    {
        [Header("Tài nguyên giao diện")]
        [SerializeField] private Sprite panelInsetSprite;
        [SerializeField] private Sprite buttonFillSprite;
        [SerializeField] private Sprite badgePillSprite;
        [SerializeField] private TMP_FontAsset fontBold;
        [SerializeField] private TMP_FontAsset fontBody;

        public void OverrideFonts(TMP_FontAsset bold, TMP_FontAsset body)
        {
            if (bold != null) fontBold = bold;
            if (body != null) fontBody = body;
        }

        [Header("Thành phần hiển thị")]
        [SerializeField] private Transform stageListContent;
        [SerializeField] private TextMeshProUGUI selectedSummaryTMP;
        [SerializeField] private Button quizFilterButton;
        [SerializeField] private TextMeshProUGUI quizFilterLabelTMP;
        [SerializeField] private Button launchBattleButton;
        [SerializeField] private TextMeshProUGUI launchBattleLabelTMP;
        [SerializeField] private Button closeButton;

        private Action _onLaunchRequested;
        private Action _onCloseRequested;

        public bool IsOpen => gameObject.activeSelf;

        public void Configure(
            Sprite panelInset, Sprite btnFill, Sprite badgePill,
            TMP_FontAsset boldFont, TMP_FontAsset bodyFont,
            Transform listContent, TextMeshProUGUI selectedSummary,
            Button filterBtn, TextMeshProUGUI filterLbl,
            Button launchBtn, TextMeshProUGUI launchLbl,
            Button closeBtn)
        {
            panelInsetSprite = panelInset;
            buttonFillSprite = btnFill;
            badgePillSprite = badgePill;
            fontBold = boldFont;
            fontBody = bodyFont;
            stageListContent = listContent;
            selectedSummaryTMP = selectedSummary;
            quizFilterButton = filterBtn;
            quizFilterLabelTMP = filterLbl;
            launchBattleButton = launchBtn;
            launchBattleLabelTMP = launchLbl;
            closeButton = closeBtn;

            WireControls();
        }

        private void Awake()
        {
            WireControls();
        }

        private void OnEnable()
        {
            PlayerCampProgressManager.EnsureLoaded().OnCampProgressChanged += RefreshAll;
        }

        private void OnDisable()
        {
            PlayerCampProgressManager.Instance.OnCampProgressChanged -= RefreshAll;
        }

        private void WireControls()
        {
            if (quizFilterButton != null)
            {
                quizFilterButton.onClick.RemoveAllListeners();
                quizFilterButton.onClick.AddListener(() =>
                {
                    PlayerCampProgressManager.EnsureLoaded().CycleQuizFilter();
                    AudioManager.Instance?.PlayButtonClick();
                    RefreshAll();
                });
            }

            if (launchBattleButton != null)
            {
                launchBattleButton.onClick.RemoveAllListeners();
                launchBattleButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    gameObject.SetActive(false);
                    _onLaunchRequested?.Invoke();
                });
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

        public void Show(Action onLaunchRequested, Action onClose = null)
        {
            _onLaunchRequested = onLaunchRequested;
            _onCloseRequested = onClose;
            gameObject.SetActive(true);
            RefreshAll();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            _onCloseRequested?.Invoke();
        }

        public void RefreshAll()
        {
            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
            StageDefinition selectedDef = StageCatalog.GetStage(camp.SelectedStage);

            int avgLv = Mathf.RoundToInt((
                camp.GetHeroProgress(CharacterClassType.Knight).level +
                camp.GetHeroProgress(CharacterClassType.Mage).level +
                camp.GetHeroProgress(CharacterClassType.Archer).level) / 3f);

            if (selectedSummaryTMP != null)
            {
                string recHint = avgLv < selectedDef.recommendedLevel
                    ? $"<color=#F2A359>Khuyến nghị Lv.{selectedDef.recommendedLevel} (Đội TB Lv.{avgLv})</color>"
                    : $"<color=#6BE39B>Đủ sức chiến (Đội TB Lv.{avgLv} / Đề xuất Lv.{selectedDef.recommendedLevel})</color>";
                string waveInfo = selectedDef.isEndless ? "Chế độ Vô tận (Boss mỗi 5 tầng)" : $"{selectedDef.waveCount} đợt quái + Boss";
                selectedSummaryTMP.text = $"Đang chọn: <b>{selectedDef.title}</b>  |  {waveInfo}  |  {recHint}";
            }

            if (quizFilterLabelTMP != null)
            {
                quizFilterLabelTMP.text = $"LỌC QUIZ: {PlayerCampProgressManager.GetQuizFilterDisplayName(camp.SelectedQuizFilter)}";
            }

            if (launchBattleLabelTMP != null)
            {
                launchBattleLabelTMP.text = $"XUẤT TRẬN: {selectedDef.shortName.ToUpperInvariant()}";
            }

            RebuildStageRows(camp);
        }

        private void RebuildStageRows(PlayerCampProgressManager camp)
        {
            if (stageListContent == null) return;
            for (int i = stageListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(stageListContent.GetChild(i).gameObject);
            }

            StageDefinition[] stages = StageCatalog.Stages;
            for (int i = 0; i < stages.Length; i++)
            {
                BuildStageRow(stages[i], camp);
            }
        }

        private void BuildStageRow(StageDefinition def, PlayerCampProgressManager camp)
        {
            StageProgressEntry prog = camp.GetStageProgress(def.id);
            bool unlocked = prog.unlocked;
            bool selected = camp.SelectedStage == def.id;

            Color themeAccent = GetStageAccentColor(def.theme, def.isEndless);

            GameObject rowGo = new GameObject($"StageRow_{def.id}");
            rowGo.transform.SetParent(stageListContent, false);
            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = panelInsetSprite;
            rowBg.type = panelInsetSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            rowBg.pixelsPerUnitMultiplier = 0.65f;
            rowBg.color = !unlocked
                ? new Color(0.06f, 0.07f, 0.10f, 0.88f)
                : (selected ? new Color(0.14f, 0.21f, 0.28f, 0.98f) : new Color(0.09f, 0.11f, 0.16f, 0.96f));

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 106f;
            le.minHeight = 106f;

            // Thanh màu chủ đề bên trái
            GameObject stripeGo = new GameObject("ThemeStripe");
            stripeGo.transform.SetParent(rowGo.transform, false);
            Image stripeImg = stripeGo.AddComponent<Image>();
            stripeImg.color = unlocked ? themeAccent : MedievalUITheme.SlateBorder;
            stripeImg.raycastTarget = false;
            RectTransform stRt = stripeImg.rectTransform;
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(0f, 1f);
            stRt.pivot = new Vector2(0f, 0.5f);
            stRt.offsetMin = new Vector2(6f, 6f);
            stRt.offsetMax = new Vector2(14f, -6f);

            // Tiêu đề + Trạng thái sao/kỷ lục
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) titleTMP.font = fontBold;
            string progressTag;
            if (!unlocked)
            {
                progressTag = "<color=#9AA5B8>[CHƯA MỞ KHÓA]</color>";
            }
            else if (def.isEndless)
            {
                progressTag = prog.bestWaveReached > 0
                    ? $"<color=#FFD24A>[KỶ LỤC: TẦNG {prog.bestWaveReached}]</color>"
                    : "<color=#7AD7F0>[CHẾ ĐỘ THỬ THÁCH]</color>";
            }
            else
            {
                progressTag = prog.cleared
                    ? $"<color=#FFD24A>[HOÀN THÀNH: {prog.bestStars}/3 SAO]</color>"
                    : "<color=#6BE39B>[SẴN SÀNG]</color>";
            }
            titleTMP.text = $"{def.title}   {progressTag}";
            titleTMP.fontSize = 18f;
            titleTMP.enableAutoSizing = true;
            titleTMP.fontSizeMin = 13f;
            titleTMP.fontSizeMax = 18f;
            titleTMP.color = unlocked ? MedievalUITheme.IvoryText : MedievalUITheme.SlateMuted;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
            titleTMP.overflowMode = TextOverflowModes.Truncate;
            titleTMP.raycastTarget = false;
            RectTransform tRt = titleTMP.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0.54f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(26f, 2f);
            tRt.offsetMax = new Vector2(-180f, -8f);

            // Mô tả + Điều kiện mở khóa / Chỉ số phần thưởng
            GameObject descGo = new GameObject("Desc");
            descGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI descTMP = descGo.AddComponent<TextMeshProUGUI>();
            if (fontBody != null) descTMP.font = fontBody;
            if (!unlocked)
            {
                descTMP.text = $"Mở khóa: {def.unlockHint}  |  {def.subtitle}";
            }
            else
            {
                string bonusFirst = !def.isEndless && !prog.cleared && def.firstClearBonusGold > 0
                    ? $"  |  Thưởng đầu: +{def.firstClearBonusGold}V"
                    : "";
                descTMP.text = $"Đề xuất Lv.{def.recommendedLevel}  |  Trọng tâm: {def.quizFocusLabel}  |  Thưởng x{def.rewardMultiplier:0.0}{bonusFirst}";
            }
            descTMP.fontSize = 14.5f;
            descTMP.enableAutoSizing = true;
            descTMP.fontSizeMin = 11.5f;
            descTMP.fontSizeMax = 14.5f;
            descTMP.color = unlocked ? MedievalUITheme.GoldSoft : MedievalUITheme.SlateMuted;
            descTMP.alignment = TextAlignmentOptions.MidlineLeft;
            descTMP.textWrappingMode = TextWrappingModes.Normal;
            descTMP.overflowMode = TextOverflowModes.Ellipsis;
            descTMP.raycastTarget = false;
            RectTransform dRt = descTMP.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0.06f);
            dRt.anchorMax = new Vector2(1f, 0.54f);
            dRt.offsetMin = new Vector2(26f, 6f);
            dRt.offsetMax = new Vector2(-180f, -2f);

            // Nút chọn màn bên phải
            GameObject btnGo = new GameObject("SelectBtn");
            btnGo.transform.SetParent(rowGo.transform, false);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = buttonFillSprite;
            btnImg.type = buttonFillSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            btnImg.pixelsPerUnitMultiplier = 0.65f;
            btnImg.color = !unlocked
                ? MedievalUITheme.BtnDisabled
                : (selected ? MedievalUITheme.SuccessPanel : MedievalUITheme.BtnPrimaryGold);
            RectTransform bRt = btnImg.rectTransform;
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-14f, 0f);
            bRt.sizeDelta = new Vector2(150f, 46f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.interactable = unlocked;

            if (unlocked)
            {
                CampaignStageId capturedStage = def.id;
                btn.onClick.AddListener(() =>
                {
                    PlayerCampProgressManager mgr = PlayerCampProgressManager.EnsureLoaded();
                    if (mgr.SelectStage(capturedStage))
                    {
                        AudioManager.Instance?.PlayButtonClick();
                        RefreshAll();
                    }
                });
            }

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            TextMeshProUGUI lblTMP = lblGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) lblTMP.font = fontBold;
            lblTMP.text = !unlocked ? "ĐANG KHÓA" : (selected ? "ĐANG CHỌN" : "CHỌN MÀN");
            lblTMP.fontSize = 14f;
            lblTMP.enableAutoSizing = true;
            lblTMP.fontSizeMin = 11f;
            lblTMP.fontSizeMax = 14f;
            lblTMP.color = !unlocked
                ? MedievalUITheme.SlateMuted
                : (selected ? MedievalUITheme.SuccessGreen : MedievalUITheme.IvoryText);
            lblTMP.alignment = TextAlignmentOptions.Center;
            lblTMP.raycastTarget = false;
            RectTransform lRt = lblTMP.rectTransform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
        }

        private static Color GetStageAccentColor(EnvironmentThemeType theme, bool isEndless)
        {
            if (isEndless) return MedievalUITheme.GoldPrimary;
            switch (theme)
            {
                case EnvironmentThemeType.AncientCrypt: return new Color(0.58f, 0.42f, 0.90f, 1f);
                case EnvironmentThemeType.DarkCitadel: return new Color(0.88f, 0.28f, 0.28f, 1f);
                default: return MedievalUITheme.SuccessGreen;
            }
        }
    }
}
