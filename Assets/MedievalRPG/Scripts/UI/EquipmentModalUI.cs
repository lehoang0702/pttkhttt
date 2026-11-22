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
    /// Bảng Kho Đồ & Trang Bị (dùng chung cho cả MainMenu và BattleScene).
    /// Cho phép xem/mặc/gỡ 3 ô trang bị (Vũ Khí, Giáp, Phụ Kiện) cho cả 3 Class
    /// (Hiệp Sĩ, Pháp Sư, Cung Thủ) và lọc danh sách đồ nhặt được theo 4 cấp độ hiếm.
    /// </summary>
    public class EquipmentModalUI : MonoBehaviour
    {
        [Header("Cấu hình Tài Nguyên UI")]
        [SerializeField] private Sprite panelInsetSprite;
        [SerializeField] private Sprite buttonFillSprite;
        [SerializeField] private Sprite buttonFrameSprite;
        [SerializeField] private Sprite badgePillSprite;
        [SerializeField] private TMP_FontAsset fontBold;
        [SerializeField] private TMP_FontAsset fontBody;
        [SerializeField] private Sprite iconSword;
        [SerializeField] private Sprite iconMagic;
        [SerializeField] private Sprite iconArrow;
        [SerializeField] private Sprite iconShield;
        [SerializeField] private Sprite iconCrown;

        [Header("Tab Chọn Class (Cột Trái: 0=Knight, 1=Mage, 2=Archer)")]
        [SerializeField] private Button[] classTabButtons = new Button[3];
        [SerializeField] private Image[] classTabBackgrounds = new Image[3];
        [SerializeField] private TextMeshProUGUI selectedClassHeaderTMP;
        [SerializeField] private TextMeshProUGUI totalBonusesTMP;

        [Header("3 Ô Trang Bị Đang Mặc (0=Weapon, 1=Armor, 2=Accessory)")]
        [SerializeField] private Image[] slotBorders = new Image[3];
        [SerializeField] private Image[] slotIcons = new Image[3];
        [SerializeField] private TextMeshProUGUI[] slotItemNames = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] slotItemStats = new TextMeshProUGUI[3];
        [SerializeField] private Button[] slotUnequipButtons = new Button[3];

        [Header("Cột Phải: Bộ Lọc & Danh Sách Kho Đồ")]
        [SerializeField] private Button[] filterButtons = new Button[4]; // 0=All, 1=Knight, 2=Mage, 3=Archer
        [SerializeField] private Image[] filterBackgrounds = new Image[4];
        [SerializeField] private TextMeshProUGUI inventoryCountTMP;
        [SerializeField] private Transform inventoryListContent;
        [SerializeField] private GameObject emptyHintObject;

        [Header("Nút Điều Khiển")]
        [SerializeField] private Button autoEquipAllButton;
        [SerializeField] private Button closeButton;

        private CharacterClassType _selectedClass = CharacterClassType.Knight;
        private CharacterClassType? _inventoryFilter = null;
        private Action _onCloseRequested;

        public bool IsOpen => gameObject.activeSelf;

        public void Configure(
            Sprite panelInset, Sprite btnFill, Sprite btnFrame, Sprite badgePill,
            TMP_FontAsset boldFont, TMP_FontAsset bodyFont,
            Sprite swordSp, Sprite magicSp, Sprite arrowSp, Sprite shieldSp, Sprite crownSp,
            Button[] clsTabs, Image[] clsTabBgs, TextMeshProUGUI clsHeader, TextMeshProUGUI bonusSummary,
            Image[] sBorders, Image[] sIcons, TextMeshProUGUI[] sNames, TextMeshProUGUI[] sStats, Button[] sUnequipBtns,
            Button[] filtBtns, Image[] filtBgs, TextMeshProUGUI invCount, Transform invContent, GameObject emptyHint,
            Button autoEquipBtn, Button closeBtn)
        {
            panelInsetSprite = panelInset;
            buttonFillSprite = btnFill;
            buttonFrameSprite = btnFrame;
            badgePillSprite = badgePill;
            fontBold = boldFont;
            fontBody = bodyFont;
            iconSword = swordSp;
            iconMagic = magicSp;
            iconArrow = arrowSp;
            iconShield = shieldSp;
            iconCrown = crownSp;

            classTabButtons = clsTabs;
            classTabBackgrounds = clsTabBgs;
            selectedClassHeaderTMP = clsHeader;
            totalBonusesTMP = bonusSummary;

            slotBorders = sBorders;
            slotIcons = sIcons;
            slotItemNames = sNames;
            slotItemStats = sStats;
            slotUnequipButtons = sUnequipBtns;

            filterButtons = filtBtns;
            filterBackgrounds = filtBgs;
            inventoryCountTMP = invCount;
            inventoryListContent = invContent;
            emptyHintObject = emptyHint;

            autoEquipAllButton = autoEquipBtn;
            closeButton = closeBtn;

            WireControls();
        }

        private void Awake()
        {
            WireControls();
        }

        private void OnEnable()
        {
            PlayerEquipmentManager.EnsureLoaded().OnEquipmentChanged += RefreshAll;
        }

        private void OnDisable()
        {
            PlayerEquipmentManager.Instance.OnEquipmentChanged -= RefreshAll;
        }

        private void WireControls()
        {
            if (classTabButtons != null)
            {
                for (int i = 0; i < classTabButtons.Length; i++)
                {
                    if (classTabButtons[i] == null) continue;
                    CharacterClassType cls = (CharacterClassType)i;
                    classTabButtons[i].onClick.RemoveAllListeners();
                    classTabButtons[i].onClick.AddListener(() =>
                    {
                        _selectedClass = cls;
                        AudioManager.Instance?.PlayButtonClick();
                        RefreshAll();
                    });
                }
            }

            if (slotUnequipButtons != null)
            {
                for (int i = 0; i < slotUnequipButtons.Length; i++)
                {
                    if (slotUnequipButtons[i] == null) continue;
                    EquipmentSlot slot = (EquipmentSlot)i;
                    slotUnequipButtons[i].onClick.RemoveAllListeners();
                    slotUnequipButtons[i].onClick.AddListener(() =>
                    {
                        if (PlayerEquipmentManager.EnsureLoaded().Unequip(_selectedClass, slot))
                        {
                            AudioManager.Instance?.PlayButtonClick();
                            RefreshAll();
                        }
                    });
                }
            }

            if (filterButtons != null)
            {
                for (int i = 0; i < filterButtons.Length; i++)
                {
                    if (filterButtons[i] == null) continue;
                    int idx = i;
                    filterButtons[i].onClick.RemoveAllListeners();
                    filterButtons[i].onClick.AddListener(() =>
                    {
                        _inventoryFilter = idx == 0 ? (CharacterClassType?)null : (CharacterClassType)(idx - 1);
                        AudioManager.Instance?.PlayButtonClick();
                        RefreshInventoryList();
                        RefreshFilterVisuals();
                    });
                }
            }

            if (autoEquipAllButton != null)
            {
                autoEquipAllButton.onClick.RemoveAllListeners();
                autoEquipAllButton.onClick.AddListener(() =>
                {
                    int changed = PlayerEquipmentManager.EnsureLoaded().AutoEquipBestForAllClasses();
                    if (changed > 0) AudioManager.Instance?.PlayLootPickupSfx();
                    else AudioManager.Instance?.PlayButtonClick();
                    RefreshAll();
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

        public void Show(Action onClose = null, CharacterClassType? focusClass = null)
        {
            _onCloseRequested = onClose;
            if (focusClass.HasValue)
            {
                _selectedClass = focusClass.Value;
            }
            gameObject.SetActive(true);
            PlayerEquipmentManager.EnsureLoaded().MarkAllSeen();
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
            RefreshClassTabs();
            RefreshEquippedSlots();
            RefreshFilterVisuals();
            RefreshInventoryList();
        }

        private void RefreshClassTabs()
        {
            if (classTabBackgrounds != null)
            {
                for (int i = 0; i < classTabBackgrounds.Length; i++)
                {
                    if (classTabBackgrounds[i] == null) continue;
                    bool selected = (int)_selectedClass == i;
                    classTabBackgrounds[i].color = selected ? MedievalUITheme.BtnPrimaryGold : MedievalUITheme.BtnNeutralSlate;
                }
            }

            if (selectedClassHeaderTMP != null)
            {
                selectedClassHeaderTMP.text = $"TRANG BỊ CỦA {EquipmentItemData.GetClassDisplayName(_selectedClass).ToUpperInvariant()}";
            }

            if (totalBonusesTMP != null)
            {
                EquipmentStatBonus bonus = PlayerEquipmentManager.EnsureLoaded().GetTotalBonusesForClass(_selectedClass);
                totalBonusesTMP.text = $"Tổng cộng thêm: {bonus.FormatSummary()}";
            }
        }

        private void RefreshEquippedSlots()
        {
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            for (int i = 0; i < 3; i++)
            {
                EquipmentSlot slot = (EquipmentSlot)i;
                EquipmentItemData equipped = mgr.GetEquippedItemData(_selectedClass, slot);

                if (slotBorders != null && i < slotBorders.Length && slotBorders[i] != null)
                {
                    slotBorders[i].color = equipped != null
                        ? EquipmentItemData.GetRarityColor(equipped.rarity)
                        : MedievalUITheme.SlateBorder;
                }

                if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                {
                    slotIcons[i].sprite = GetSpriteForItem(_selectedClass, slot, equipped);
                    slotIcons[i].color = equipped != null ? Color.white : new Color(0.45f, 0.50f, 0.58f, 0.55f);
                }

                if (slotItemNames != null && i < slotItemNames.Length && slotItemNames[i] != null)
                {
                    if (equipped != null)
                    {
                        string rTag = EquipmentItemData.GetRarityDisplayName(equipped.rarity);
                        slotItemNames[i].text = $"{EquipmentItemData.GetSlotDisplayName(slot)}: [{rTag}] {equipped.itemName}";
                        slotItemNames[i].color = EquipmentItemData.GetRarityColor(equipped.rarity);
                    }
                    else
                    {
                        slotItemNames[i].text = $"{EquipmentItemData.GetSlotDisplayName(slot)}: Trống";
                        slotItemNames[i].color = MedievalUITheme.SlateMuted;
                    }
                }

                if (slotItemStats != null && i < slotItemStats.Length && slotItemStats[i] != null)
                {
                    slotItemStats[i].text = equipped != null ? equipped.GetStatsSummary() : "Nhặt đồ trên bản đồ để trang bị";
                }

                if (slotUnequipButtons != null && i < slotUnequipButtons.Length && slotUnequipButtons[i] != null)
                {
                    slotUnequipButtons[i].gameObject.SetActive(equipped != null);
                }
            }
        }

        private void RefreshFilterVisuals()
        {
            if (filterBackgrounds == null) return;
            for (int i = 0; i < filterBackgrounds.Length; i++)
            {
                if (filterBackgrounds[i] == null) continue;
                bool active = (i == 0 && !_inventoryFilter.HasValue)
                              || (i > 0 && _inventoryFilter.HasValue && (int)_inventoryFilter.Value == i - 1);
                filterBackgrounds[i].color = active ? MedievalUITheme.BtnPrimaryGold : MedievalUITheme.BtnNeutralSlate;
            }
        }

        private void RefreshInventoryList()
        {
            if (inventoryListContent == null) return;

            for (int i = inventoryListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(inventoryListContent.GetChild(i).gameObject);
            }

            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            List<OwnedEquipmentInstance> items = mgr.GetOwnedItemsSorted(_inventoryFilter);

            if (inventoryCountTMP != null)
            {
                inventoryCountTMP.text = $"KHO TRANG BỊ ({items.Count}/{mgr.OwnedCount} món)";
            }

            if (emptyHintObject != null)
            {
                emptyHintObject.SetActive(items.Count == 0);
            }

            for (int i = 0; i < items.Count; i++)
            {
                OwnedEquipmentInstance inst = items[i];
                EquipmentItemData data = mgr.ResolveData(inst);
                if (data == null) continue;
                BuildInventoryRowCard(inst, data, mgr.IsEquipped(inst.instanceId));
            }
        }

        private void BuildInventoryRowCard(OwnedEquipmentInstance inst, EquipmentItemData data, bool isEquipped)
        {
            Color rarityColor = EquipmentItemData.GetRarityColor(data.rarity);

            GameObject rowGo = new GameObject($"EquipRow_{data.itemId}");
            rowGo.transform.SetParent(inventoryListContent, false);

            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = panelInsetSprite;
            rowBg.type = panelInsetSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            rowBg.pixelsPerUnitMultiplier = 0.65f;
            rowBg.color = isEquipped
                ? new Color(0.11f, 0.22f, 0.19f, 0.96f)
                : new Color(0.08f, 0.10f, 0.14f, 0.96f);

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            le.minHeight = 72f;

            // Thanh viền màu độ hiếm bên trái thẻ
            GameObject stripeGo = new GameObject("RarityStripe");
            stripeGo.transform.SetParent(rowGo.transform, false);
            Image stripeImg = stripeGo.AddComponent<Image>();
            stripeImg.color = rarityColor;
            stripeImg.raycastTarget = false;
            RectTransform stRt = stripeImg.rectTransform;
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(0f, 1f);
            stRt.pivot = new Vector2(0f, 0.5f);
            stRt.offsetMin = new Vector2(4f, 5f);
            stRt.offsetMax = new Vector2(10f, -5f);

            // Icon món đồ
            GameObject iconBoxGo = new GameObject("IconBox");
            iconBoxGo.transform.SetParent(rowGo.transform, false);
            Image iconBoxImg = iconBoxGo.AddComponent<Image>();
            iconBoxImg.sprite = badgePillSprite;
            iconBoxImg.type = badgePillSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            iconBoxImg.color = new Color(rarityColor.r * 0.28f, rarityColor.g * 0.28f, rarityColor.b * 0.28f, 0.92f);
            iconBoxImg.raycastTarget = false;
            RectTransform ibRt = iconBoxImg.rectTransform;
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(18f, 0f);
            ibRt.sizeDelta = new Vector2(48f, 48f);

            GameObject icGo = new GameObject("Icon");
            icGo.transform.SetParent(iconBoxGo.transform, false);
            Image icImg = icGo.AddComponent<Image>();
            icImg.sprite = GetSpriteForItem(data.requiredClass, data.slot, data);
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;
            RectTransform icRt = icImg.rectTransform;
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.offsetMin = new Vector2(8f, 8f);
            icRt.offsetMax = new Vector2(-8f, -8f);

            // Tên món đồ + Độ hiếm + Class + Slot
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) titleTMP.font = fontBold;
            string rName = EquipmentItemData.GetRarityDisplayName(data.rarity).ToUpperInvariant();
            string cName = EquipmentItemData.GetClassDisplayName(data.requiredClass);
            string sName = EquipmentItemData.GetSlotDisplayName(data.slot);
            titleTMP.text = $"[{rName}] {data.itemName}  <color=#A8B3C5><size=12>({cName} • {sName})</size></color>";
            titleTMP.fontSize = 15f;
            titleTMP.enableAutoSizing = true;
            titleTMP.fontSizeMin = 11f;
            titleTMP.fontSizeMax = 15f;
            titleTMP.color = rarityColor;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
            titleTMP.overflowMode = TextOverflowModes.Truncate;
            titleTMP.raycastTarget = false;
            RectTransform ttRt = titleTMP.rectTransform;
            ttRt.anchorMin = new Vector2(0f, 0.5f);
            ttRt.anchorMax = new Vector2(1f, 1f);
            ttRt.offsetMin = new Vector2(76f, 2f);
            ttRt.offsetMax = new Vector2(-128f, -6f);

            // Dòng chỉ số cộng thêm
            GameObject statsGo = new GameObject("Stats");
            statsGo.transform.SetParent(rowGo.transform, false);
            TextMeshProUGUI statsTMP = statsGo.AddComponent<TextMeshProUGUI>();
            if (fontBody != null) statsTMP.font = fontBody;
            statsTMP.text = data.GetStatsSummary();
            statsTMP.fontSize = 13f;
            statsTMP.enableAutoSizing = true;
            statsTMP.fontSizeMin = 10f;
            statsTMP.fontSizeMax = 13f;
            statsTMP.color = MedievalUITheme.IvoryText;
            statsTMP.alignment = TextAlignmentOptions.MidlineLeft;
            statsTMP.textWrappingMode = TextWrappingModes.NoWrap;
            statsTMP.overflowMode = TextOverflowModes.Truncate;
            statsTMP.raycastTarget = false;
            RectTransform ssRt = statsTMP.rectTransform;
            ssRt.anchorMin = new Vector2(0f, 0f);
            ssRt.anchorMax = new Vector2(1f, 0.5f);
            ssRt.offsetMin = new Vector2(76f, 6f);
            ssRt.offsetMax = new Vector2(-128f, -2f);

            // Nút TRANG BỊ / ĐANG MẶC
            GameObject btnGo = new GameObject("EquipBtn");
            btnGo.transform.SetParent(rowGo.transform, false);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = buttonFillSprite;
            btnImg.type = buttonFillSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            btnImg.pixelsPerUnitMultiplier = 0.65f;
            btnImg.color = isEquipped ? MedievalUITheme.SuccessPanel : MedievalUITheme.BtnPrimaryGold;
            RectTransform bRt = btnImg.rectTransform;
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-12f, 0f);
            bRt.sizeDelta = new Vector2(106f, 40f);

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.interactable = !isEquipped;

            if (!isEquipped)
            {
                string capturedId = inst.instanceId;
                CharacterClassType capturedClass = data.requiredClass;
                btn.onClick.AddListener(() =>
                {
                    _selectedClass = capturedClass;
                    if (PlayerEquipmentManager.EnsureLoaded().Equip(capturedId))
                    {
                        AudioManager.Instance?.PlayLootPickupSfx();
                        RefreshAll();
                    }
                });
            }

            GameObject bLblGo = new GameObject("Label");
            bLblGo.transform.SetParent(btnGo.transform, false);
            TextMeshProUGUI bLbl = bLblGo.AddComponent<TextMeshProUGUI>();
            if (fontBold != null) bLbl.font = fontBold;
            bLbl.text = isEquipped ? "ĐANG MẶC" : "TRANG BỊ";
            bLbl.fontSize = 13f;
            bLbl.color = isEquipped ? MedievalUITheme.SuccessGreen : MedievalUITheme.IvoryText;
            bLbl.alignment = TextAlignmentOptions.Center;
            bLbl.raycastTarget = false;
            RectTransform blRt = bLbl.rectTransform;
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.offsetMin = Vector2.zero;
            blRt.offsetMax = Vector2.zero;
        }

        private Sprite GetSpriteForItem(CharacterClassType cls, EquipmentSlot slot, EquipmentItemData data)
        {
            if (data != null && data.icon != null) return data.icon;
            if (slot == EquipmentSlot.Armor) return iconShield;
            if (slot == EquipmentSlot.Accessory) return iconCrown;

            switch (cls)
            {
                case CharacterClassType.Mage: return iconMagic;
                case CharacterClassType.Archer: return iconArrow;
                default: return iconSword;
            }
        }
    }
}
