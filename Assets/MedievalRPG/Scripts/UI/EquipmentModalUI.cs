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

        public void OverrideFonts(TMP_FontAsset bold, TMP_FontAsset body)
        {
            if (bold != null) fontBold = bold;
            if (body != null) fontBody = body;
        }
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

        private static readonly EquipmentSlot[] SlotDisplayOrder =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.Helmet,
            EquipmentSlot.Armor,
            EquipmentSlot.Accessory
        };

        [Header("4 Ô Trang Bị Đang Mặc (Vũ Khí, Mũ, Giáp, Phụ Kiện)")]
        [SerializeField] private Image[] slotBorders = new Image[4];
        [SerializeField] private Image[] slotIcons = new Image[4];
        [SerializeField] private TextMeshProUGUI[] slotItemNames = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] slotItemStats = new TextMeshProUGUI[4];
        [SerializeField] private Button[] slotUnequipButtons = new Button[4];

        [Header("Cột Phải: Bộ Lọc & Danh Sách Kho Đồ")]
        [SerializeField] private Button[] filterButtons = new Button[4]; // 0=All, 1=Knight, 2=Mage, 3=Archer
        [SerializeField] private Image[] filterBackgrounds = new Image[4];
        [SerializeField] private TextMeshProUGUI inventoryCountTMP;
        [SerializeField] private Transform inventoryListContent;
        [SerializeField] private GameObject emptyHintObject;

        [Header("Nút Điều Khiển")]
        [SerializeField] private Button autoEquipAllButton;
        [SerializeField] private Button closeButton;

        [Header("V2: Chi Tiết, Cường Hóa, Bán & Rèn Đồ")]
        [SerializeField] private GameObject detailPanelRoot;
        [SerializeField] private TextMeshProUGUI detailTitleTMP;
        [SerializeField] private TextMeshProUGUI detailStatsTMP;
        [SerializeField] private TextMeshProUGUI detailDiffTMP;
        [SerializeField] private Button detailEquipButton;
        [SerializeField] private TextMeshProUGUI detailEquipLabelTMP;
        [SerializeField] private Button detailUpgradeButton;
        [SerializeField] private TextMeshProUGUI detailUpgradeLabelTMP;
        [SerializeField] private Button detailSellButton;
        [SerializeField] private TextMeshProUGUI detailSellLabelTMP;
        [SerializeField] private Button bulkSellCommonButton;
        [SerializeField] private TextMeshProUGUI bulkSellCommonLabelTMP;
        [SerializeField] private Button forgeThreeButton;
        [SerializeField] private TextMeshProUGUI forgeThreeLabelTMP;
        [SerializeField] private TextMeshProUGUI goldBalanceTMP;

        private CharacterClassType _selectedClass = CharacterClassType.Knight;
        private CharacterClassType? _inventoryFilter = null;
        private string _selectedInstanceId;
        private bool _suppressRefresh;
        private Action _onCloseRequested;

        public bool IsOpen => gameObject.activeSelf;

        public void ConfigureUpgradeAndForgeUI(
            GameObject detailRoot,
            TextMeshProUGUI detailTitle,
            TextMeshProUGUI detailStats,
            TextMeshProUGUI detailDiff,
            Button detailEquipBtn, TextMeshProUGUI detailEquipLbl,
            Button detailUpgradeBtn, TextMeshProUGUI detailUpgradeLbl,
            Button detailSellBtn, TextMeshProUGUI detailSellLbl,
            Button bulkSellBtn, TextMeshProUGUI bulkSellLbl,
            Button forgeBtn, TextMeshProUGUI forgeLbl,
            TextMeshProUGUI goldBalTMP)
        {
            detailPanelRoot = detailRoot;
            detailTitleTMP = detailTitle;
            detailStatsTMP = detailStats;
            detailDiffTMP = detailDiff;
            detailEquipButton = detailEquipBtn;
            detailEquipLabelTMP = detailEquipLbl;
            detailUpgradeButton = detailUpgradeBtn;
            detailUpgradeLabelTMP = detailUpgradeLbl;
            detailSellButton = detailSellBtn;
            detailSellLabelTMP = detailSellLbl;
            bulkSellCommonButton = bulkSellBtn;
            bulkSellCommonLabelTMP = bulkSellLbl;
            forgeThreeButton = forgeBtn;
            forgeThreeLabelTMP = forgeLbl;
            goldBalanceTMP = goldBalTMP;

            WireV2Controls();
        }

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
            PlayerEquipmentManager.EnsureLoaded().OnEquipmentChanged += HandleExternalEquipmentChanged;
            PlayerCampProgressManager.EnsureLoaded().OnCampProgressChanged += HandleExternalCampChanged;
        }

        private void OnDisable()
        {
            PlayerEquipmentManager.Instance.OnEquipmentChanged -= HandleExternalEquipmentChanged;
            PlayerCampProgressManager.Instance.OnCampProgressChanged -= HandleExternalCampChanged;
        }

        private void HandleExternalEquipmentChanged()
        {
            if (!_suppressRefresh && gameObject.activeInHierarchy) RefreshAll();
        }

        private void HandleExternalCampChanged()
        {
            if (!_suppressRefresh && gameObject.activeInHierarchy) RefreshDetailAndToolbar();
        }

        private void WireControls()
        {
            EnsureFourSlotCards();

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
                int count = Mathf.Min(slotUnequipButtons.Length, SlotDisplayOrder.Length);
                for (int i = 0; i < count; i++)
                {
                    if (slotUnequipButtons[i] == null) continue;
                    EquipmentSlot slot = SlotDisplayOrder[i];
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
                        RefreshAll();
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

            WireV2Controls();
        }

        private void WireV2Controls()
        {
            if (detailEquipButton != null)
            {
                detailEquipButton.onClick.RemoveAllListeners();
                detailEquipButton.onClick.AddListener(HandleDetailEquipToggle);
            }

            if (detailUpgradeButton != null)
            {
                detailUpgradeButton.onClick.RemoveAllListeners();
                detailUpgradeButton.onClick.AddListener(HandleDetailUpgrade);
            }

            if (detailSellButton != null)
            {
                detailSellButton.onClick.RemoveAllListeners();
                detailSellButton.onClick.AddListener(HandleDetailSell);
            }

            if (bulkSellCommonButton != null)
            {
                bulkSellCommonButton.onClick.RemoveAllListeners();
                bulkSellCommonButton.onClick.AddListener(HandleBulkSellCommon);
            }

            if (forgeThreeButton != null)
            {
                forgeThreeButton.onClick.RemoveAllListeners();
                forgeThreeButton.onClick.AddListener(HandleForgeThree);
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
            _suppressRefresh = true;
            try
            {
                PlayerEquipmentManager.EnsureLoaded().MarkAllSeen();
            }
            finally
            {
                _suppressRefresh = false;
            }
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
            RefreshDetailAndToolbar();
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

        /// <summary>
        /// Nâng cấp runtime cho các scene dựng sẵn chỉ có 3 ô (Vũ khí/Giáp/Phụ kiện) lên đủ 4 ô có thêm ô Mũ.
        /// </summary>
        private void EnsureFourSlotCards()
        {
            if (slotBorders == null || slotBorders.Length != 3 || slotBorders[0] == null || slotBorders[1] == null || slotBorders[2] == null)
            {
                return;
            }

            Transform card0 = slotBorders[0].transform.parent;
            Transform slotsBox = card0 != null ? card0.parent : null;
            if (slotsBox == null) return;

            Transform existingHelm = slotsBox.Find("EquipSlotCard_Helmet");
            GameObject helmCard = existingHelm != null
                ? existingHelm.gameObject
                : Instantiate(slotBorders[2].transform.parent.gameObject, slotsBox, false);
            helmCard.name = "EquipSlotCard_Helmet";
            helmCard.transform.SetSiblingIndex(1);

            RectTransform sbRt = slotsBox as RectTransform;
            if (sbRt != null)
            {
                sbRt.anchorMin = new Vector2(0f, 0f);
                sbRt.anchorMax = new Vector2(1f, 1f);
                sbRt.pivot = new Vector2(0.5f, 0.5f);
                sbRt.offsetMin = new Vector2(14f, 158f);
                sbRt.offsetMax = new Vector2(-14f, -98f);
            }
            VerticalLayoutGroup vlg = slotsBox.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.spacing = 6f;

            for (int c = 0; c < slotsBox.childCount; c++)
            {
                Transform iconBox = slotsBox.GetChild(c).Find("IconBox");
                if (iconBox is RectTransform ibRt) ibRt.sizeDelta = new Vector2(46f, 46f);
            }

            slotBorders = new[]
            {
                slotBorders[0],
                helmCard.transform.Find("Border")?.GetComponent<Image>(),
                slotBorders[1],
                slotBorders[2]
            };
            slotIcons = new[]
            {
                slotIcons != null && slotIcons.Length > 0 ? slotIcons[0] : null,
                helmCard.transform.Find("IconBox/Icon")?.GetComponent<Image>(),
                slotIcons != null && slotIcons.Length > 1 ? slotIcons[1] : null,
                slotIcons != null && slotIcons.Length > 2 ? slotIcons[2] : null
            };
            slotItemNames = new[]
            {
                slotItemNames != null && slotItemNames.Length > 0 ? slotItemNames[0] : null,
                helmCard.transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>(),
                slotItemNames != null && slotItemNames.Length > 1 ? slotItemNames[1] : null,
                slotItemNames != null && slotItemNames.Length > 2 ? slotItemNames[2] : null
            };
            slotItemStats = new[]
            {
                slotItemStats != null && slotItemStats.Length > 0 ? slotItemStats[0] : null,
                helmCard.transform.Find("ItemStats")?.GetComponent<TextMeshProUGUI>(),
                slotItemStats != null && slotItemStats.Length > 1 ? slotItemStats[1] : null,
                slotItemStats != null && slotItemStats.Length > 2 ? slotItemStats[2] : null
            };
            slotUnequipButtons = new[]
            {
                slotUnequipButtons != null && slotUnequipButtons.Length > 0 ? slotUnequipButtons[0] : null,
                helmCard.GetComponentInChildren<Button>(true),
                slotUnequipButtons != null && slotUnequipButtons.Length > 1 ? slotUnequipButtons[1] : null,
                slotUnequipButtons != null && slotUnequipButtons.Length > 2 ? slotUnequipButtons[2] : null
            };
        }

        private void RefreshEquippedSlots()
        {
            EnsureFourSlotCards();
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            for (int i = 0; i < SlotDisplayOrder.Length; i++)
            {
                EquipmentSlot slot = SlotDisplayOrder[i];
                OwnedEquipmentInstance eqInst = mgr.GetEquippedInstance(_selectedClass, slot);
                EquipmentItemData equipped = mgr.ResolveData(eqInst);
                int upLv = eqInst != null ? eqInst.upgradeLevel : 0;

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
                        string upTag = upLv > 0 ? $" +{upLv}" : "";
                        slotItemNames[i].text = $"{EquipmentItemData.GetSlotDisplayName(slot)}: [{rTag}] {equipped.itemName}{upTag}";
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
                    slotItemStats[i].text = equipped != null ? equipped.GetStatsSummary(upLv) : "Nhặt đồ trên bản đồ để trang bị";
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

            if (string.IsNullOrEmpty(_selectedInstanceId) || mgr.FindOwnedByInstanceId(_selectedInstanceId) == null)
            {
                _selectedInstanceId = items.Count > 0 ? items[0].instanceId : null;
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
            bool isSelected = inst.instanceId == _selectedInstanceId;

            GameObject rowGo = new GameObject($"EquipRow_{data.itemId}");
            rowGo.transform.SetParent(inventoryListContent, false);

            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.sprite = panelInsetSprite;
            rowBg.type = panelInsetSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            rowBg.pixelsPerUnitMultiplier = 0.65f;
            rowBg.color = isSelected
                ? new Color(0.18f, 0.23f, 0.34f, 0.98f)
                : (isEquipped
                    ? new Color(0.11f, 0.22f, 0.19f, 0.96f)
                    : new Color(0.08f, 0.10f, 0.14f, 0.96f));

            Button rowSelectBtn = rowGo.AddComponent<Button>();
            rowSelectBtn.targetGraphic = rowBg;
            string selectedCapture = inst.instanceId;
            CharacterClassType selectClassCapture = data.requiredClass;
            rowSelectBtn.onClick.AddListener(() =>
            {
                _selectedInstanceId = selectedCapture;
                _selectedClass = selectClassCapture;
                AudioManager.Instance?.PlayButtonHover();
                RefreshAll();
            });

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 80f;
            le.minHeight = 80f;

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
            string upTag = inst.upgradeLevel > 0 ? $" +{inst.upgradeLevel}" : "";
            string selMark = isSelected ? "> " : "";
            titleTMP.text = $"{selMark}[{rName}] {data.itemName}{upTag}  <color=#A8B3C5><size=12>({cName} - {sName})</size></color>";
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
            statsTMP.text = data.GetStatsSummary(inst.upgradeLevel);
            statsTMP.fontSize = 13.5f;
            statsTMP.enableAutoSizing = true;
            statsTMP.fontSizeMin = 11f;
            statsTMP.fontSizeMax = 13.5f;
            statsTMP.color = MedievalUITheme.IvoryText;
            statsTMP.alignment = TextAlignmentOptions.MidlineLeft;
            statsTMP.textWrappingMode = TextWrappingModes.Normal;
            statsTMP.overflowMode = TextOverflowModes.Ellipsis;
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
                    _selectedInstanceId = capturedId;
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

        private void RefreshDetailAndToolbar()
        {
            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();

            if (goldBalanceTMP != null)
            {
                goldBalanceTMP.text = $"VÀNG: {camp.Gold} V";
            }

            OwnedEquipmentInstance selInst = !string.IsNullOrEmpty(_selectedInstanceId)
                ? mgr.FindOwnedByInstanceId(_selectedInstanceId)
                : null;
            EquipmentItemData selData = mgr.ResolveData(selInst);

            if (detailPanelRoot != null)
            {
                detailPanelRoot.SetActive(true);
            }

            if (selInst != null && selData != null)
            {
                bool selEquipped = mgr.IsEquipped(selInst.instanceId);
                string upStr = selInst.upgradeLevel > 0 ? $" +{selInst.upgradeLevel}" : "";
                string rName = EquipmentItemData.GetRarityDisplayName(selData.rarity);
                if (detailTitleTMP != null)
                {
                    detailTitleTMP.text = $"[<color={EquipmentItemData.GetRarityHex(selData.rarity)}>{rName}</color>] {selData.itemName}{upStr}  ({EquipmentItemData.GetClassDisplayName(selData.requiredClass)})";
                }
                if (detailStatsTMP != null)
                {
                    detailStatsTMP.text = $"Chỉ số: {selData.GetStatsSummary(selInst.upgradeLevel)}";
                }
                if (detailDiffTMP != null)
                {
                    if (selEquipped)
                    {
                        detailDiffTMP.text = "<color=#6BE39B>Đang trang bị trên ô này</color>";
                    }
                    else
                    {
                        OwnedEquipmentInstance curEqInst = mgr.GetEquippedInstance(selData.requiredClass, selData.slot);
                        EquipmentItemData curEqData = mgr.ResolveData(curEqInst);
                        EquipmentStatBonus curBonus = curEqData != null
                            ? curEqData.GetScaledStats(curEqInst != null ? curEqInst.upgradeLevel : 0)
                            : default;
                        EquipmentStatBonus selBonus = selData.GetScaledStats(selInst.upgradeLevel);
                        detailDiffTMP.text = $"So với đang mặc: {selBonus.FormatDiffAgainst(curBonus)}";
                    }
                }

                if (detailEquipButton != null) detailEquipButton.interactable = true;
                if (detailEquipLabelTMP != null) detailEquipLabelTMP.text = selEquipped ? "THÁO RA" : "TRANG BỊ";

                if (detailUpgradeButton != null && detailUpgradeLabelTMP != null)
                {
                    if (selInst.upgradeLevel >= EquipmentItemData.MaxUpgradeLevel)
                    {
                        detailUpgradeButton.interactable = false;
                        detailUpgradeLabelTMP.text = "TỐI ĐA (+5)";
                    }
                    else
                    {
                        int upCost = selData.GetUpgradeCost(selInst.upgradeLevel);
                        detailUpgradeButton.interactable = camp.Gold >= upCost;
                        detailUpgradeLabelTMP.text = $"CƯỜNG HÓA +{selInst.upgradeLevel + 1} ({upCost}V)";
                    }
                }

                if (detailSellButton != null && detailSellLabelTMP != null)
                {
                    int sellVal = selData.GetSellGoldValue(selInst.upgradeLevel);
                    detailSellButton.interactable = true;
                    detailSellLabelTMP.text = $"BÁN (+{sellVal}V)";
                }
            }
            else
            {
                if (detailTitleTMP != null) detailTitleTMP.text = "CHI TIẾT & LÒ RÈN TRANG BỊ";
                if (detailStatsTMP != null) detailStatsTMP.text = "Chưa chọn trang bị trong kho.";
                if (detailDiffTMP != null) detailDiffTMP.text = "Chọn 1 món ở danh sách bên phải để xem so sánh chỉ số, cường hóa hoặc bán.";
                if (detailEquipButton != null) detailEquipButton.interactable = false;
                if (detailEquipLabelTMP != null) detailEquipLabelTMP.text = "TRANG BỊ";
                if (detailUpgradeButton != null) detailUpgradeButton.interactable = false;
                if (detailUpgradeLabelTMP != null) detailUpgradeLabelTMP.text = "CƯỜNG HÓA";
                if (detailSellButton != null) detailSellButton.interactable = false;
                if (detailSellLabelTMP != null) detailSellLabelTMP.text = "BÁN";
            }

            RefreshBulkAndForgeButtons(mgr);
        }

        private void RefreshBulkAndForgeButtons(PlayerEquipmentManager mgr)
        {
            if (bulkSellCommonButton != null && bulkSellCommonLabelTMP != null)
            {
                int count = 0;
                int totalGold = 0;
                List<OwnedEquipmentInstance> all = mgr.GetOwnedItemsSorted(null);
                for (int i = 0; i < all.Count; i++)
                {
                    OwnedEquipmentInstance inst = all[i];
                    if (mgr.IsEquipped(inst.instanceId) || inst.upgradeLevel > 0) continue;
                    EquipmentItemData data = mgr.ResolveData(inst);
                    if (data != null && data.rarity == ItemRarity.Common)
                    {
                        count++;
                        totalGold += data.GetSellGoldValue(inst.upgradeLevel);
                    }
                }
                bulkSellCommonButton.interactable = count > 0;
                bulkSellCommonLabelTMP.text = count > 0
                    ? $"BÁN {count} ĐỒ THƯỜNG (+{totalGold}V)"
                    : "BÁN ĐỒ THƯỜNG (0)";
            }

            if (forgeThreeButton != null && forgeThreeLabelTMP != null)
            {
                ItemRarity? source = GetForgeSourceRarity(mgr);
                if (source.HasValue)
                {
                    string srcName = EquipmentItemData.GetRarityDisplayName(source.Value).ToUpperInvariant();
                    string dstName = EquipmentItemData.GetRarityDisplayName(source.Value + 1).ToUpperInvariant();
                    forgeThreeButton.interactable = true;
                    forgeThreeLabelTMP.text = $"GHÉP 3 {srcName} -> {dstName}";
                }
                else
                {
                    forgeThreeButton.interactable = false;
                    forgeThreeLabelTMP.text = "GHÉP 3 ĐỒ CÙNG PHẨM CHẤT";
                }
            }
        }

        private ItemRarity? GetForgeSourceRarity(PlayerEquipmentManager mgr)
        {
            if (!string.IsNullOrEmpty(_selectedInstanceId))
            {
                OwnedEquipmentInstance selInst = mgr.FindOwnedByInstanceId(_selectedInstanceId);
                EquipmentItemData selData = mgr.ResolveData(selInst);
                if (selData != null && selData.rarity < ItemRarity.Legendary && mgr.CountUnequippedByRarity(selData.rarity) >= 3)
                {
                    return selData.rarity;
                }
            }

            ItemRarity[] order = { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic };
            for (int i = 0; i < order.Length; i++)
            {
                if (mgr.CountUnequippedByRarity(order[i]) >= 3) return order[i];
            }
            return null;
        }

        private void HandleDetailEquipToggle()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId)) return;
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            OwnedEquipmentInstance inst = mgr.FindOwnedByInstanceId(_selectedInstanceId);
            EquipmentItemData data = mgr.ResolveData(inst);
            if (inst == null || data == null) return;

            if (mgr.IsEquipped(inst.instanceId))
            {
                if (mgr.Unequip(data.requiredClass, data.slot))
                {
                    AudioManager.Instance?.PlayButtonClick();
                    RefreshAll();
                }
            }
            else if (mgr.Equip(inst.instanceId))
            {
                _selectedClass = data.requiredClass;
                AudioManager.Instance?.PlayLootPickupSfx();
                RefreshAll();
            }
        }

        private void HandleDetailUpgrade()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId)) return;
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();

            bool upgraded;
            int goldSpent;
            _suppressRefresh = true;
            try
            {
                upgraded = mgr.TryUpgradeItem(_selectedInstanceId, camp.Gold, out goldSpent);
                if (upgraded)
                {
                    camp.TrySpendGold(goldSpent);
                    OwnedEquipmentInstance checkInst = mgr.FindOwnedByInstanceId(_selectedInstanceId);
                    if (checkInst != null && checkInst.upgradeLevel >= 3)
                    {
                        camp.UnlockAchievement(AchievementId.ForgePlusThree);
                    }
                }
            }
            finally
            {
                _suppressRefresh = false;
            }

            if (upgraded)
            {
                AudioManager.Instance?.PlayForgeUpgradeSfx();
                OwnedEquipmentInstance inst = mgr.FindOwnedByInstanceId(_selectedInstanceId);
                RefreshAll();
                if (detailDiffTMP != null && inst != null)
                {
                    detailDiffTMP.text = $"<color=#FFD24A>CƯỜNG HÓA THÀNH CÔNG +{inst.upgradeLevel} (-{goldSpent} Vàng)!</color>";
                }
            }
            else
            {
                AudioManager.Instance?.PlayQuizResult(false, 0);
            }
        }

        private void HandleDetailSell()
        {
            if (string.IsNullOrEmpty(_selectedInstanceId)) return;
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();

            bool sold;
            int goldEarned;
            _suppressRefresh = true;
            try
            {
                sold = mgr.SellItem(_selectedInstanceId, out goldEarned);
                if (sold)
                {
                    PlayerCampProgressManager.EnsureLoaded().AddGold(goldEarned);
                    _selectedInstanceId = null;
                }
            }
            finally
            {
                _suppressRefresh = false;
            }

            if (sold)
            {
                AudioManager.Instance?.PlayCoinRewardSfx();
                RefreshAll();
                if (detailDiffTMP != null)
                {
                    detailDiffTMP.text = $"<color=#5CE09A>Đã bán trang bị, nhận +{goldEarned} Vàng!</color>";
                }
            }
        }

        private void HandleBulkSellCommon()
        {
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();

            int count;
            int goldEarned;
            _suppressRefresh = true;
            try
            {
                count = mgr.SellAllUnequippedByMaxRarity(ItemRarity.Common, out goldEarned);
                if (count > 0)
                {
                    PlayerCampProgressManager.EnsureLoaded().AddGold(goldEarned);
                    if (!string.IsNullOrEmpty(_selectedInstanceId) && mgr.FindOwnedByInstanceId(_selectedInstanceId) == null)
                    {
                        _selectedInstanceId = null;
                    }
                }
            }
            finally
            {
                _suppressRefresh = false;
            }

            if (count > 0)
            {
                AudioManager.Instance?.PlayCoinRewardSfx();
                RefreshAll();
                if (detailDiffTMP != null)
                {
                    detailDiffTMP.text = $"<color=#5CE09A>Đã bán {count} đồ Thường rảnh, thu +{goldEarned} Vàng!</color>";
                }
            }
        }

        private void HandleForgeThree()
        {
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            ItemRarity? source = GetForgeSourceRarity(mgr);
            if (!source.HasValue) return;

            bool forged;
            EquipmentItemData forgedItem;
            OwnedEquipmentInstance createdInst;
            _suppressRefresh = true;
            try
            {
                forged = mgr.TryForgeThreeIntoNextRarity(source.Value, out forgedItem, out createdInst);
                if (forged)
                {
                    _selectedInstanceId = createdInst.instanceId;
                    _selectedClass = forgedItem.requiredClass;
                    if (forgedItem.rarity == ItemRarity.Legendary)
                    {
                        PlayerCampProgressManager.EnsureLoaded().UnlockAchievement(AchievementId.FirstLegendary);
                    }
                }
            }
            finally
            {
                _suppressRefresh = false;
            }

            if (forged)
            {
                AudioManager.Instance?.PlayForgeUpgradeSfx();
                RefreshAll();
                if (detailDiffTMP != null)
                {
                    string rName = EquipmentItemData.GetRarityDisplayName(forgedItem.rarity);
                    detailDiffTMP.text = $"<color=#FFD24A>RÈN THÀNH CÔNG:</color> [<color={EquipmentItemData.GetRarityHex(forgedItem.rarity)}>{rName}</color>] {forgedItem.itemName}!";
                }
            }
        }

        private Sprite GetSpriteForItem(CharacterClassType cls, EquipmentSlot slot, EquipmentItemData data)
        {
            if (data != null)
            {
                Sprite customSprite = EquipmentVisualCatalog.GetSpriteForItem(data.itemId);
                if (customSprite != null) return customSprite;
                if (data.icon != null) return data.icon;
            }
            if (slot == EquipmentSlot.Armor) return iconShield;
            if (slot == EquipmentSlot.Accessory || slot == EquipmentSlot.Helmet) return iconCrown;

            switch (cls)
            {
                case CharacterClassType.Mage: return iconMagic;
                case CharacterClassType.Archer: return iconArrow;
                default: return iconSword;
            }
        }
    }
}
