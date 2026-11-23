using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Data;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Bảng Thành Tựu V2: hiển thị 7 mốc thành tựu chiến đấu & học tập, cho phép nhận thưởng Vàng.
    /// </summary>
    public class AchievementsModalUI : MonoBehaviour
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

        [Header("Thành phần hiển thị")]
        [SerializeField] private TextMeshProUGUI summaryTMP;
        [SerializeField] private Transform listContent;
        [SerializeField] private Button closeButton;

        private Action _onCloseRequested;

        public bool IsOpen => gameObject.activeSelf;

        public void Configure(
            Sprite panelInset, Sprite btnFill,
            TMP_FontAsset boldFont, TMP_FontAsset bodyFont,
            TextMeshProUGUI summaryText, Transform contentTransform,
            Button closeBtn)
        {
            panelInsetSprite = panelInset;
            buttonFillSprite = btnFill;
            fontBold = boldFont;
            fontBody = bodyFont;
            summaryTMP = summaryText;
            listContent = contentTransform;
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
            AchievementDefinition[] all = AchievementCatalog.All;

            int unlockedCount = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (camp.IsAchievementUnlocked(all[i].id)) unlockedCount++;
            }

            if (summaryTMP != null)
            {
                summaryTMP.text = $"ĐÃ MỞ: {unlockedCount}/{all.Length}  |  CHỜ NHẬN: {camp.ClaimableAchievementCount}  |  VÀNG HIỆN CÓ: {camp.Gold} V";
            }

            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }

            for (int i = 0; i < all.Length; i++)
            {
                BuildRow(all[i], camp);
            }
        }

        private void BuildRow(AchievementDefinition def, PlayerCampProgressManager camp)
        {
            bool unlocked = camp.IsAchievementUnlocked(def.id);
            bool claimed = camp.IsAchievementClaimed(def.id);
            bool canClaim = unlocked && !claimed;

            GameObject rowGo = new GameObject($"AchRow_{def.id}");
            rowGo.transform.SetParent(listContent, false);
            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = panelInsetSprite;
            rowBg.type = panelInsetSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            rowBg.pixelsPerUnitMultiplier = 0.65f;
            rowBg.color = canClaim
                ? new Color(0.20f, 0.16f, 0.08f, 0.98f)
                : (claimed ? new Color(0.09f, 0.16f, 0.13f, 0.94f) : new Color(0.08f, 0.10f, 0.14f, 0.94f));

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;
            le.minHeight = 84f;

            // Thanh trạng thái bên trái
            GameObject stripeGo = new GameObject("Stripe");
            stripeGo.transform.SetParent(rowGo.transform, false);
            Image stripeImg = stripeGo.AddComponent<Image>();
            stripeImg.color = canClaim
                ? MedievalUITheme.GoldPrimary
                : (claimed ? MedievalUITheme.SuccessGreen : MedievalUITheme.SlateBorder);
            stripeImg.raycastTarget = false;
            RectTransform stRt = stripeImg.rectTransform;
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(0f, 1f);
            stRt.pivot = new Vector2(0f, 0.5f);
            stRt.offsetMin = new Vector2(5f, 5f);
            stRt.offsetMax = new Vector2(12f, -5f);

            // Tiêu đề
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) titleTMP.font = fontBold;
            string statusPrefix = claimed ? "[ĐÃ NHẬN] " : (canClaim ? "[HOÀN THÀNH] " : "[ĐANG KHÓA] ");
            titleTMP.text = statusPrefix + def.title;
            titleTMP.fontSize = 16f;
            titleTMP.enableAutoSizing = true;
            titleTMP.fontSizeMin = 12f;
            titleTMP.fontSizeMax = 16f;
            titleTMP.color = canClaim
                ? MedievalUITheme.GoldPrimary
                : (claimed ? MedievalUITheme.SuccessGreen : MedievalUITheme.IvoryText);
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
            titleTMP.overflowMode = TextOverflowModes.Truncate;
            titleTMP.raycastTarget = false;
            RectTransform tRt = titleTMP.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0.52f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(22f, 2f);
            tRt.offsetMax = new Vector2(-168f, -6f);

            // Mô tả điều kiện
            GameObject descGo = new GameObject("Desc");
            descGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI descTMP = descGo.AddComponent<TextMeshProUGUI>();
            if (fontBody != null) descTMP.font = fontBody;
            descTMP.text = def.description;
            descTMP.fontSize = 13.5f;
            descTMP.enableAutoSizing = true;
            descTMP.fontSizeMin = 11.5f;
            descTMP.fontSizeMax = 13.5f;
            descTMP.color = unlocked ? MedievalUITheme.IvoryText : MedievalUITheme.SlateMuted;
            descTMP.alignment = TextAlignmentOptions.MidlineLeft;
            descTMP.textWrappingMode = TextWrappingModes.Normal;
            descTMP.overflowMode = TextOverflowModes.Ellipsis;
            descTMP.raycastTarget = false;
            RectTransform dRt = descTMP.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0.04f);
            dRt.anchorMax = new Vector2(1f, 0.52f);
            dRt.offsetMin = new Vector2(22f, 4f);
            dRt.offsetMax = new Vector2(-168f, -2f);

            // Nút Nhận thưởng
            GameObject btnGo = new GameObject("ClaimBtn");
            btnGo.transform.SetParent(rowGo.transform, false);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = buttonFillSprite;
            btnImg.type = buttonFillSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            btnImg.pixelsPerUnitMultiplier = 0.65f;
            btnImg.color = canClaim
                ? MedievalUITheme.BtnPrimaryGold
                : (claimed ? MedievalUITheme.SuccessPanel : MedievalUITheme.BtnDisabled);
            RectTransform bRt = btnImg.rectTransform;
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-14f, 0f);
            bRt.sizeDelta = new Vector2(142f, 42f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.interactable = canClaim;

            if (canClaim)
            {
                AchievementId capturedId = def.id;
                btn.onClick.AddListener(() =>
                {
                    if (PlayerCampProgressManager.EnsureLoaded().TryClaimAchievement(capturedId, out int gold))
                    {
                        AudioManager.Instance?.PlayCoinRewardSfx();
                        RefreshAll();
                    }
                });
            }

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            TextMeshProUGUI lblTMP = lblGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) lblTMP.font = fontBold;
            lblTMP.text = claimed ? "ĐÃ NHẬN" : (canClaim ? $"NHẬN +{def.goldReward}V" : $"+{def.goldReward}V");
            lblTMP.fontSize = 14f;
            lblTMP.enableAutoSizing = true;
            lblTMP.fontSizeMin = 11f;
            lblTMP.fontSizeMax = 14f;
            lblTMP.color = claimed ? MedievalUITheme.SuccessGreen : (canClaim ? MedievalUITheme.IvoryText : MedievalUITheme.SlateMuted);
            lblTMP.alignment = TextAlignmentOptions.Center;
            lblTMP.raycastTarget = false;
            RectTransform lRt = lblTMP.rectTransform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
        }
    }
}
