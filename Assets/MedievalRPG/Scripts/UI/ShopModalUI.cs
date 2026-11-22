using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Data;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Bảng Cửa Hàng Tiền Đồn tại Sảnh Chờ (CampHub):
    /// Cho phép dùng Vàng để mua 5 loại Thuốc / Vật phẩm mang vào trận
    /// và kích hoạt 3 loại Bùa Lợi (+Máu, +Dame, +Giáp/Tốc) cho cả đội.
    /// </summary>
    public class ShopModalUI : MonoBehaviour
    {
        [Header("Cấu hình Tài Nguyên UI")]
        [SerializeField] private Sprite panelInsetSprite;
        [SerializeField] private Sprite buttonFillSprite;
        [SerializeField] private Sprite badgePillSprite;
        [SerializeField] private TMP_FontAsset fontBold;
        [SerializeField] private TMP_FontAsset fontBody;
        [SerializeField] private Sprite iconHeart;
        [SerializeField] private Sprite iconMana;
        [SerializeField] private Sprite iconSword;
        [SerializeField] private Sprite iconShield;
        [SerializeField] private Sprite iconCrown;

        [Header("Tham chiếu Giao Diện")]
        [SerializeField] private TextMeshProUGUI goldBalanceTMP;
        [SerializeField] private TextMeshProUGUI activeBuffsSummaryTMP;
        [SerializeField] private Transform consumablesContent;
        [SerializeField] private Transform buffsContent;
        [SerializeField] private Button closeButton;

        private Action _onCloseRequested;

        public bool IsOpen => gameObject.activeSelf;

        public void Configure(
            Sprite panelInset, Sprite btnFill, Sprite badgePill,
            TMP_FontAsset boldFont, TMP_FontAsset bodyFont,
            Sprite heartSp, Sprite manaSp, Sprite swordSp, Sprite shieldSp, Sprite crownSp,
            TextMeshProUGUI goldTMP, TextMeshProUGUI buffsSummary,
            Transform consContainer, Transform buffContainer, Button closeBtn)
        {
            panelInsetSprite = panelInset;
            buttonFillSprite = btnFill;
            badgePillSprite = badgePill;
            fontBold = boldFont;
            fontBody = bodyFont;
            iconHeart = heartSp;
            iconMana = manaSp;
            iconSword = swordSp;
            iconShield = shieldSp;
            iconCrown = crownSp;
            goldBalanceTMP = goldTMP;
            activeBuffsSummaryTMP = buffsSummary;
            consumablesContent = consContainer;
            buffsContent = buffContainer;
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
            PlayerCampProgressManager mgr = PlayerCampProgressManager.EnsureLoaded();
            if (goldBalanceTMP != null)
            {
                goldBalanceTMP.text = $"VÀNG HIỆN CÓ: {mgr.Gold}";
            }
            if (activeBuffsSummaryTMP != null)
            {
                activeBuffsSummaryTMP.text = $"Bùa trận kế tiếp: {mgr.GetActiveBuffsShortSummary()}";
            }

            RebuildConsumableRows(mgr);
            RebuildBuffRows(mgr);
        }

        private void RebuildConsumableRows(PlayerCampProgressManager mgr)
        {
            if (consumablesContent == null) return;
            for (int i = consumablesContent.childCount - 1; i >= 0; i--)
            {
                Destroy(consumablesContent.GetChild(i).gameObject);
            }

            ShopConsumableOffer[] offers = PlayerCampProgressManager.ConsumableOffers;
            for (int i = 0; i < offers.Length; i++)
            {
                ShopConsumableOffer offer = offers[i];
                int owned = mgr.GetConsumableCount(offer.itemType);
                bool atCap = owned >= offer.maxStock;
                bool canAfford = !atCap && mgr.Gold >= offer.price;

                string title = BattleInventory.GetDisplayName(offer.itemType);
                string stockTag = $"Đang có: x{owned}/{offer.maxStock}";
                string desc = BattleInventory.GetEffectDescription(offer.itemType);
                string btnLabel = atCap ? "ĐÃ ĐẦY" : $"MUA • {offer.price}V";
                Color accent = GetConsumableColor(offer.itemType);
                Sprite icon = GetConsumableIcon(offer.itemType);

                CreateOfferRow(
                    consumablesContent,
                    $"ShopCons_{offer.itemType}",
                    icon,
                    accent,
                    $"{title}  <color=#A8B3C5><size=12>({stockTag})</size></color>",
                    desc,
                    btnLabel,
                    canAfford,
                    canAfford ? MedievalUITheme.BtnPrimaryGold : MedievalUITheme.BtnDisabled,
                    () =>
                    {
                        if (PlayerCampProgressManager.EnsureLoaded().TryBuyConsumable(offer.itemType))
                        {
                            AudioManager.Instance?.PlayLootPickupSfx();
                            RefreshAll();
                        }
                    });
            }
        }

        private void RebuildBuffRows(PlayerCampProgressManager mgr)
        {
            if (buffsContent == null) return;
            for (int i = buffsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(buffsContent.GetChild(i).gameObject);
            }

            ShopCampBuffOffer[] offers = PlayerCampProgressManager.CampBuffOffers;
            for (int i = 0; i < offers.Length; i++)
            {
                ShopCampBuffOffer offer = offers[i];
                bool active = mgr.IsCampBuffActive(offer.buffType);
                bool canAfford = !active && mgr.Gold >= offer.price;

                string btnLabel = active ? "ĐANG BẬT" : $"MUA • {offer.price}V";
                Color btnColor = active
                    ? MedievalUITheme.SuccessPanel
                    : (canAfford ? MedievalUITheme.BtnSkill : MedievalUITheme.BtnDisabled);
                Color accent = offer.buffType == CampBuffType.VitalityBlessing
                    ? MedievalUITheme.SuccessGreen
                    : (offer.buffType == CampBuffType.FuryBlessing ? MedievalUITheme.BtnAttack : MedievalUITheme.BtnDefend);
                Sprite icon = offer.buffType == CampBuffType.VitalityBlessing
                    ? iconHeart
                    : (offer.buffType == CampBuffType.FuryBlessing ? iconSword : iconShield);

                CreateOfferRow(
                    buffsContent,
                    $"ShopBuff_{offer.buffType}",
                    icon,
                    accent,
                    offer.title,
                    offer.description,
                    btnLabel,
                    canAfford,
                    btnColor,
                    () =>
                    {
                        if (PlayerCampProgressManager.EnsureLoaded().TryBuyCampBuff(offer.buffType))
                        {
                            AudioManager.Instance?.PlayLootPickupSfx();
                            RefreshAll();
                        }
                    });
            }
        }

        private void CreateOfferRow(
            Transform parent, string rowName, Sprite icon, Color accent,
            string titleText, string descText, string buttonText,
            bool interactable, Color buttonColor, Action onClick)
        {
            GameObject rowGo = new GameObject(rowName);
            rowGo.transform.SetParent(parent, false);

            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = panelInsetSprite;
            rowBg.type = panelInsetSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            rowBg.pixelsPerUnitMultiplier = 0.65f;
            rowBg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 74f;
            le.minHeight = 74f;

            // Thanh màu phân loại bên trái
            GameObject stripeGo = new GameObject("Stripe");
            stripeGo.transform.SetParent(rowGo.transform, false);
            Image stripe = stripeGo.AddComponent<Image>();
            stripe.color = accent;
            stripe.raycastTarget = false;
            RectTransform stRt = stripe.rectTransform;
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(0f, 1f);
            stRt.pivot = new Vector2(0f, 0.5f);
            stRt.offsetMin = new Vector2(4f, 5f);
            stRt.offsetMax = new Vector2(10f, -5f);

            // Icon hộp tròn
            GameObject iconBoxGo = new GameObject("IconBox");
            iconBoxGo.transform.SetParent(rowGo.transform, false);
            Image iconBoxImg = iconBoxGo.AddComponent<Image>();
            iconBoxImg.sprite = badgePillSprite;
            iconBoxImg.type = badgePillSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            iconBoxImg.color = new Color(accent.r * 0.30f, accent.g * 0.30f, accent.b * 0.30f, 0.92f);
            iconBoxImg.raycastTarget = false;
            RectTransform ibRt = iconBoxImg.rectTransform;
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(16f, 0f);
            ibRt.sizeDelta = new Vector2(48f, 48f);

            GameObject icGo = new GameObject("Icon");
            icGo.transform.SetParent(iconBoxGo.transform, false);
            Image icImg = icGo.AddComponent<Image>();
            icImg.sprite = icon != null ? icon : iconCrown;
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;
            RectTransform icRt = icImg.rectTransform;
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.offsetMin = new Vector2(8f, 8f);
            icRt.offsetMax = new Vector2(-8f, -8f);

            // Tên vật phẩm
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) titleTMP.font = fontBold;
            titleTMP.text = titleText;
            titleTMP.fontSize = 15f;
            titleTMP.enableAutoSizing = true;
            titleTMP.fontSizeMin = 11.5f;
            titleTMP.fontSizeMax = 15f;
            titleTMP.color = MedievalUITheme.GoldSoft;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
            titleTMP.overflowMode = TextOverflowModes.Truncate;
            titleTMP.raycastTarget = false;
            RectTransform ttRt = titleTMP.rectTransform;
            ttRt.anchorMin = new Vector2(0f, 0.52f);
            ttRt.anchorMax = new Vector2(1f, 1f);
            ttRt.offsetMin = new Vector2(74f, 0f);
            ttRt.offsetMax = new Vector2(-132f, -6f);

            // Mô tả công dụng
            GameObject descGo = new GameObject("Desc");
            descGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI descTMP = descGo.AddComponent<TextMeshProUGUI>();
            if (fontBody != null) descTMP.font = fontBody;
            descTMP.text = descText;
            descTMP.fontSize = 12.5f;
            descTMP.enableAutoSizing = true;
            descTMP.fontSizeMin = 9.5f;
            descTMP.fontSizeMax = 12f;
            descTMP.color = MedievalUITheme.IvoryText;
            descTMP.alignment = TextAlignmentOptions.TopLeft;
            descTMP.textWrappingMode = TextWrappingModes.Normal;
            descTMP.overflowMode = TextOverflowModes.Truncate;
            descTMP.raycastTarget = false;
            RectTransform dsRt = descTMP.rectTransform;
            dsRt.anchorMin = new Vector2(0f, 0f);
            dsRt.anchorMax = new Vector2(1f, 0.56f);
            dsRt.offsetMin = new Vector2(74f, 6f);
            dsRt.offsetMax = new Vector2(-132f, -2f);

            // Nút MUA / ĐANG BẬT
            GameObject btnGo = new GameObject("BuyBtn");
            btnGo.transform.SetParent(rowGo.transform, false);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = buttonFillSprite;
            btnImg.type = buttonFillSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            btnImg.pixelsPerUnitMultiplier = 0.65f;
            btnImg.color = buttonColor;
            RectTransform bRt = btnImg.rectTransform;
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-10f, 0f);
            bRt.sizeDelta = new Vector2(114f, 42f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.interactable = interactable;
            if (interactable)
            {
                UIButtonFeedback fb = btnGo.AddComponent<UIButtonFeedback>();
                fb.SetBaseColor(buttonColor);
                btn.onClick.AddListener(() => onClick?.Invoke());
            }

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            TextMeshProUGUI lbl = lblGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) lbl.font = fontBold;
            lbl.text = buttonText;
            lbl.fontSize = 13f;
            lbl.enableAutoSizing = true;
            lbl.fontSizeMin = 10f;
            lbl.fontSizeMax = 13f;
            lbl.color = MedievalUITheme.IvoryText;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
            RectTransform lRt = lbl.rectTransform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(4f, 2f);
            lRt.offsetMax = new Vector2(-4f, -2f);
        }

        private Sprite GetConsumableIcon(ConsumableItemType type)
        {
            switch (type)
            {
                case ConsumableItemType.HealthPotion:   return iconHeart;
                case ConsumableItemType.ManaElixir:     return iconMana;
                case ConsumableItemType.PhoenixFeather: return iconCrown;
                case ConsumableItemType.WarElixir:      return iconSword;
                case ConsumableItemType.IronTonic:      return iconShield;
                default:                                return iconHeart;
            }
        }

        private static Color GetConsumableColor(ConsumableItemType type)
        {
            switch (type)
            {
                case ConsumableItemType.HealthPotion:   return MedievalUITheme.BtnHeal;
                case ConsumableItemType.ManaElixir:     return MedievalUITheme.MpSapphire;
                case ConsumableItemType.PhoenixFeather: return MedievalUITheme.GoldPrimary;
                case ConsumableItemType.WarElixir:      return MedievalUITheme.BtnAttack;
                case ConsumableItemType.IronTonic:      return MedievalUITheme.BtnDefend;
                default:                                return MedievalUITheme.GoldPrimary;
            }
        }
    }
}
