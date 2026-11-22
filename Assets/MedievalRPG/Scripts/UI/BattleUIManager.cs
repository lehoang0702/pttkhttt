using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Quiz;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Điều khiển toàn bộ giao diện trận chiến (uGUI + TMP Tiếng Việt):
    /// - Thẻ 3 Anh hùng (bấm để chọn đồng đội), thẻ Quái vật đang nhắm mục tiêu & tab chọn đa quái.
    /// - Dải thứ tự lượt theo Speed (Turn Order Strip), WorldUnitHUD (máu, mũi tên lượt, vòng mục tiêu, Enemy Intent).
    /// - Bộ nút hành động [1-5] có hiển thị Cooldown, Túi Vật Phẩm (Popover) và Bảng Quiz / Tạm dừng (Music + SFX).
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        public static BattleUIManager Instance { get; private set; }

        [Header("Tham chiếu Hệ Thống Chiến Đấu")]
        public BattleSystem battleSystem;

        [Header("Giao diện uGUI cơ bản")]
        public GameObject actionPanel;
        public GameObject quizPanel;
        public Text questionCategoryText;
        public Text questionContentText;
        public Text[] answerButtonTexts;
        public UnityEngine.UI.Button[] answerButtons;
        public Text feedbackText;
        public Text dialogueBoxText;

        [Header("Màn hình Kết thúc")]
        public GameObject victoryScreen;
        public GameObject gameOverScreen;

        [Header("Giao diện Canvas + TMP")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private TextMeshProUGUI turnBannerTMP;
        [SerializeField] private TextMeshProUGUI turnSubTMP;
        [SerializeField] private TextMeshProUGUI quizStatsTMP;
        [SerializeField] private TextMeshProUGUI waveBadgeTMP;
        [SerializeField] private TextMeshProUGUI dialogueTMP;
        [SerializeField] private TextMeshProUGUI activeHeroBannerTMP;
        [SerializeField] private TextMeshProUGUI skillPreviewTMP;
        [SerializeField] private UnityEngine.UI.Image activeHeroPortrait;

        [Header("Dải Thứ Tự Lượt (Turn Order Strip)")]
        [SerializeField] private GameObject[] turnOrderSlotRoots = new GameObject[6];
        [SerializeField] private UnityEngine.UI.Image[] turnOrderSlotFrames = new UnityEngine.UI.Image[6];
        [SerializeField] private UnityEngine.UI.Image[] turnOrderSlotPortraits = new UnityEngine.UI.Image[6];

        [Header("Thẻ Nhân Vật, Tab Đa Quái & World HUD")]
        [SerializeField] private PartyMemberCardUI[] partyCards;
        [SerializeField] private EnemyStatusCardUI enemyCard;
        [SerializeField] private GameObject enemyTabsRow;
        [SerializeField] private UnityEngine.UI.Button[] enemyTabButtons = new UnityEngine.UI.Button[3];
        [SerializeField] private TextMeshProUGUI[] enemyTabLabels = new TextMeshProUGUI[3];
        [SerializeField] private UnityEngine.UI.Image[] enemyTabHpFills = new UnityEngine.UI.Image[3];
        [SerializeField] private UnityEngine.UI.Image[] enemyTabBorders = new UnityEngine.UI.Image[3];
        [SerializeField] private WorldUnitHUD[] worldUnitHUDs;
        [SerializeField] private RectTransform worldHudLayer;
        [SerializeField] private FloatingTextManager floatingTextManager;

        [Header("Bảng Câu Hỏi & Tạm Dừng")]
        [SerializeField] private QuizModalUI quizModal;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private UnityEngine.UI.Slider pauseMusicSlider;
        [SerializeField] private TextMeshProUGUI pauseMusicValueTMP;
        [SerializeField] private UnityEngine.UI.Slider pauseSfxSlider;
        [SerializeField] private TextMeshProUGUI pauseSfxValueTMP;
        [SerializeField] private UnityEngine.UI.Toggle pauseQuizToggle;
        [SerializeField] private UnityEngine.UI.Toggle pauseFastModeToggle;
        [SerializeField] private UnityEngine.UI.Button settingsOpenButton;

        [Header("5 Nút Hành Động (1: Đánh, 2: Skill1, 3: Skill2, 4: Túi Đồ, 5: Thủ)")]
        [SerializeField] private UnityEngine.UI.Button[] actionButtons = new UnityEngine.UI.Button[5];
        [SerializeField] private TextMeshProUGUI[] actionTitleTMPs = new TextMeshProUGUI[5];
        [SerializeField] private TextMeshProUGUI[] actionCostTMPs = new TextMeshProUGUI[5];
        [SerializeField] private UnityEngine.UI.Image[] actionIconImages = new UnityEngine.UI.Image[5];
        [SerializeField] private UIButtonFeedback[] actionFeedbacks = new UIButtonFeedback[5];
        [SerializeField] private GameObject actionLockedOverlay;
        [SerializeField] private TextMeshProUGUI actionLockedTMP;

        [Header("Túi Vật Phẩm (Popover)")]
        [SerializeField] private GameObject itemBagPanel;
        [SerializeField] private UnityEngine.UI.Button[] itemButtons = new UnityEngine.UI.Button[5];
        [SerializeField] private TextMeshProUGUI[] itemTitleTMPs = new TextMeshProUGUI[5];
        [SerializeField] private TextMeshProUGUI[] itemCountTMPs = new TextMeshProUGUI[5];
        [SerializeField] private UnityEngine.UI.Button closeItemBagButton;

        [Header("Vàng & Bùa Lợi Sảnh Chờ")]
        [SerializeField] private TextMeshProUGUI goldBadgeTMP;
        [SerializeField] private TextMeshProUGUI campBuffBadgeTMP;

        private static readonly ConsumableItemType[] BagItemOrder =
        {
            ConsumableItemType.HealthPotion,
            ConsumableItemType.ManaElixir,
            ConsumableItemType.PhoenixFeather,
            ConsumableItemType.WarElixir,
            ConsumableItemType.IronTonic
        };

        [Header("Tóm Tắt Kết Quả Trận Đấu")]
        [SerializeField] private TextMeshProUGUI victorySummaryTMP;
        [SerializeField] private TextMeshProUGUI gameOverSummaryTMP;

        [Header("Kho Trang Bị & Chiến Lợi Phẩm (Loot UI)")]
        [SerializeField] private UnityEngine.UI.Button equipmentOpenButton;
        [SerializeField] private TextMeshProUGUI equipmentOpenLabelTMP;
        [SerializeField] private EquipmentModalUI equipmentModal;
        [SerializeField] private UnityEngine.UI.Button collectLootButton;
        [SerializeField] private TextMeshProUGUI collectLootLabelTMP;
        [SerializeField] private GameObject lootToastRoot;
        [SerializeField] private UnityEngine.UI.Image lootToastBorder;
        [SerializeField] private TextMeshProUGUI lootToastTitleTMP;
        [SerializeField] private TextMeshProUGUI lootToastDetailTMP;

        private Coroutine _lootToastCoroutine;
        private bool _pausedByEquipmentModal;

        [Header("V2: Tutorial, Tooltip & Pause Mở Rộng")]
        [SerializeField] private GameObject tutorialOverlayRoot;
        [SerializeField] private TextMeshProUGUI tutorialTitleTMP;
        [SerializeField] private TextMeshProUGUI tutorialBodyTMP;
        [SerializeField] private TextMeshProUGUI tutorialStepTMP;
        [SerializeField] private UnityEngine.UI.Button tutorialNextButton;
        [SerializeField] private TextMeshProUGUI tutorialNextLabelTMP;
        [SerializeField] private UnityEngine.UI.Button tutorialSkipButton;

        [SerializeField] private GameObject statusTooltipRoot;
        [SerializeField] private TextMeshProUGUI statusTooltipTitleTMP;
        [SerializeField] private TextMeshProUGUI statusTooltipBodyTMP;

        [SerializeField] private UnityEngine.UI.Button retreatEndlessButton;
        [SerializeField] private UnityEngine.UI.Toggle pauseQuizTimerToggle;
        [SerializeField] private UnityEngine.UI.Toggle pauseCameraShakeToggle;
        [SerializeField] private UnityEngine.UI.Toggle pauseFullscreenToggle;
        [SerializeField] private UnityEngine.UI.Button pauseResetSaveButton;
        [SerializeField] private TextMeshProUGUI pauseResetSaveLabelTMP;

        private int _tutorialStepIndex;
        private bool _pausedByTutorial;

        private static readonly (string title, string body)[] TutorialSteps =
        {
            ("BƯỚC 1/4: CHIẾN ĐẤU THEO LƯỢT & TỐC ĐỘ",
             "Thứ tự hành động hiển thị ở dải chân dung góc trên bên trái.\nChọn [1] Đánh thường, [2]/[3] Kỹ năng, [4] Túi đồ hoặc [5] Phòng thủ để bảo toàn đội hình."),
            ("BƯỚC 2/4: QUIZ TIẾNG ANH B1/B2 & QUICK MIND",
             "Trả lời đúng câu B1/B2 để tung chiêu tối đa sức mạnh và nhận +10 Vàng + EXP.\nĐặc biệt: Trả lời đúng trong 5 giây đầu kích hoạt QUICK MIND (+15 Vàng, +18 EXP, +15% Chí mạng)!"),
            ("BƯỚC 3/4: NHẶT ĐỒ, CƯỜNG HÓA & LÒ RÈN",
             "Quái gục sẽ rớt Vàng và Trang bị Thường -> Hiếm -> Sử Thi -> Huyền Thoại.\nMở Kho Đồ [I] để xem So Sánh Chỉ Số, Cường Hóa (+1..+5), Bán hoặc Ghép 3 đồ thành phẩm chất cao hơn!"),
            ("BƯỚC 4/4: CHIẾN THUẬT BOSS & SỔ ÔN TẬP",
             "Boss kích hoạt Khiên Bóng Tối ở nửa máu: hãy trả lời đúng Quiz hoặc tung đòn phá giáp.\nNếu trả lời sai, câu hỏi tự động lưu vào Sổ Ôn Tập [N] tại Sảnh Chờ để bạn luyện lại (+15 Vàng/câu)!")
        };

        [Header("Sprites Chân Dung & Biểu Tượng")]
        [SerializeField] private Sprite knightPortrait;
        [SerializeField] private Sprite magePortrait;
        [SerializeField] private Sprite archerPortrait;
        [SerializeField] private Sprite goblinPortrait;
        [SerializeField] private Sprite skeletonPortrait;
        [SerializeField] private Sprite darkKnightPortrait;
        [SerializeField] private Sprite swordIcon;
        [SerializeField] private Sprite magicIcon;
        [SerializeField] private Sprite arrowIcon;
        [SerializeField] private Sprite heartIcon;
        [SerializeField] private Sprite shieldIcon;
        [SerializeField] private Sprite manaIcon;
        [SerializeField] private Sprite scrollIcon;

        private bool _isTransitioning;

        public bool HasCanvasUI => rootCanvas != null && (partyCards != null && partyCards.Length > 0);

        public void ConfigureCanvasUI(
            Canvas canvas, CanvasGroup fade,
            TextMeshProUGUI turnBanner, TextMeshProUGUI turnSub, TextMeshProUGUI quizStats, TextMeshProUGUI waveBadge,
            TextMeshProUGUI diagTMP, TextMeshProUGUI heroBanner, TextMeshProUGUI skillPreview,
            UnityEngine.UI.Image heroPortrait,
            GameObject[] turnRoots, UnityEngine.UI.Image[] turnFrames, UnityEngine.UI.Image[] turnPorts,
            PartyMemberCardUI[] pCards, EnemyStatusCardUI eCard,
            GameObject eTabsRow, UnityEngine.UI.Button[] eTabBtns, TextMeshProUGUI[] eTabLbls,
            UnityEngine.UI.Image[] eTabHp, UnityEngine.UI.Image[] eTabBorders,
            WorldUnitHUD[] wHuds, RectTransform wHudLayer, FloatingTextManager ftManager,
            QuizModalUI qModal, GameObject pPanel,
            UnityEngine.UI.Slider pMusicSlider, TextMeshProUGUI pMusicVal,
            UnityEngine.UI.Slider pSfxSlider, TextMeshProUGUI pSfxVal,
            UnityEngine.UI.Toggle pQuizToggle, UnityEngine.UI.Toggle pFastToggle,
            UnityEngine.UI.Button settingsBtn,
            UnityEngine.UI.Button[] actBtns, TextMeshProUGUI[] actTitles, TextMeshProUGUI[] actCosts,
            UnityEngine.UI.Image[] actIcons, UIButtonFeedback[] actFeedbacks,
            GameObject lockedOverlay, TextMeshProUGUI lockedText,
            GameObject bagPanel, UnityEngine.UI.Button[] bagItemBtns,
            TextMeshProUGUI[] bagItemTitles, TextMeshProUGUI[] bagItemCounts, UnityEngine.UI.Button closeBagBtn,
            TextMeshProUGUI winSummary, TextMeshProUGUI loseSummary,
            Sprite kPort, Sprite mPort, Sprite aPort, Sprite gPort, Sprite sPort, Sprite dkPort,
            Sprite icSword, Sprite icMagic, Sprite icArrow, Sprite icHeart, Sprite icShield, Sprite icMana, Sprite icScroll)
        {
            rootCanvas = canvas;
            fadeOverlay = fade;
            turnBannerTMP = turnBanner;
            turnSubTMP = turnSub;
            quizStatsTMP = quizStats;
            waveBadgeTMP = waveBadge;
            dialogueTMP = diagTMP;
            activeHeroBannerTMP = heroBanner;
            skillPreviewTMP = skillPreview;
            activeHeroPortrait = heroPortrait;
            turnOrderSlotRoots = turnRoots;
            turnOrderSlotFrames = turnFrames;
            turnOrderSlotPortraits = turnPorts;
            partyCards = pCards;
            enemyCard = eCard;
            enemyTabsRow = eTabsRow;
            enemyTabButtons = eTabBtns;
            enemyTabLabels = eTabLbls;
            enemyTabHpFills = eTabHp;
            enemyTabBorders = eTabBorders;
            worldUnitHUDs = wHuds;
            worldHudLayer = wHudLayer;
            floatingTextManager = ftManager;
            quizModal = qModal;
            pausePanel = pPanel;
            pauseMusicSlider = pMusicSlider;
            pauseMusicValueTMP = pMusicVal;
            pauseSfxSlider = pSfxSlider;
            pauseSfxValueTMP = pSfxVal;
            pauseQuizToggle = pQuizToggle;
            pauseFastModeToggle = pFastToggle;
            settingsOpenButton = settingsBtn;
            actionButtons = actBtns;
            actionTitleTMPs = actTitles;
            actionCostTMPs = actCosts;
            actionIconImages = actIcons;
            actionFeedbacks = actFeedbacks;
            actionLockedOverlay = lockedOverlay;
            actionLockedTMP = lockedText;
            itemBagPanel = bagPanel;
            itemButtons = bagItemBtns;
            itemTitleTMPs = bagItemTitles;
            itemCountTMPs = bagItemCounts;
            closeItemBagButton = closeBagBtn;
            victorySummaryTMP = winSummary;
            gameOverSummaryTMP = loseSummary;
            knightPortrait = kPort;
            magePortrait = mPort;
            archerPortrait = aPort;
            goblinPortrait = gPort;
            skeletonPortrait = sPort;
            darkKnightPortrait = dkPort;
            swordIcon = icSword;
            magicIcon = icMagic;
            arrowIcon = icArrow;
            heartIcon = icHeart;
            shieldIcon = icShield;
            manaIcon = icMana;
            scrollIcon = icScroll;
        }

        public void ConfigureLootAndEquipmentUI(
            UnityEngine.UI.Button equipBtn, TextMeshProUGUI equipLbl, EquipmentModalUI equipModal,
            UnityEngine.UI.Button collectBtn, TextMeshProUGUI collectLbl,
            GameObject toastRoot, UnityEngine.UI.Image toastBorder,
            TextMeshProUGUI toastTitle, TextMeshProUGUI toastDetail)
        {
            equipmentOpenButton = equipBtn;
            equipmentOpenLabelTMP = equipLbl;
            equipmentModal = equipModal;
            collectLootButton = collectBtn;
            collectLootLabelTMP = collectLbl;
            lootToastRoot = toastRoot;
            lootToastBorder = toastBorder;
            lootToastTitleTMP = toastTitle;
            lootToastDetailTMP = toastDetail;
        }

        public void ConfigureGoldAndCampUI(TextMeshProUGUI goldTMP, TextMeshProUGUI buffPillTMP)
        {
            goldBadgeTMP = goldTMP;
            campBuffBadgeTMP = buffPillTMP;
        }

        public void ConfigureV2Features(
            GameObject tutRoot, TextMeshProUGUI tutTitle, TextMeshProUGUI tutBody, TextMeshProUGUI tutStep,
            UnityEngine.UI.Button tutNextBtn, TextMeshProUGUI tutNextLbl, UnityEngine.UI.Button tutSkipBtn,
            GameObject tipRoot, TextMeshProUGUI tipTitle, TextMeshProUGUI tipBody,
            UnityEngine.UI.Button retreatBtn,
            UnityEngine.UI.Toggle timerToggle, UnityEngine.UI.Toggle shakeToggle, UnityEngine.UI.Toggle fullToggle,
            UnityEngine.UI.Button resetBtn, TextMeshProUGUI resetLbl)
        {
            tutorialOverlayRoot = tutRoot;
            tutorialTitleTMP = tutTitle;
            tutorialBodyTMP = tutBody;
            tutorialStepTMP = tutStep;
            tutorialNextButton = tutNextBtn;
            tutorialNextLabelTMP = tutNextLbl;
            tutorialSkipButton = tutSkipBtn;
            statusTooltipRoot = tipRoot;
            statusTooltipTitleTMP = tipTitle;
            statusTooltipBodyTMP = tipBody;
            retreatEndlessButton = retreatBtn;
            pauseQuizTimerToggle = timerToggle;
            pauseCameraShakeToggle = shakeToggle;
            pauseFullscreenToggle = fullToggle;
            pauseResetSaveButton = resetBtn;
            pauseResetSaveLabelTMP = resetLbl;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (battleSystem == null)
            {
                battleSystem = FindAnyObjectByType<BattleSystem>();
            }
        }

        private void OnEnable() => SubscribeEvents();
        private void OnDisable() => UnsubscribeEvents();

        private void Start()
        {
            if (answerButtons != null && answerButtons.Length >= 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (answerButtons[i] == null) continue;
                    int index = i;
                    answerButtons[i].onClick.AddListener(() => OnSubmitAnswerClicked(index));
                }
            }

            WireActionButtons();
            WireItemBagButtons();
            WireEnemyTabButtons();
            WirePauseControls();
            WireLootAndEquipmentControls();
            WireTutorialControls();

            if (victoryScreen != null) victoryScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (quizModal != null) quizModal.Hide();
            if (pausePanel != null) pausePanel.SetActive(false);
            if (itemBagPanel != null) itemBagPanel.SetActive(false);
            if (equipmentModal != null) equipmentModal.gameObject.SetActive(false);
            if (collectLootButton != null) collectLootButton.gameObject.SetActive(false);
            if (lootToastRoot != null) lootToastRoot.SetActive(false);
            if (statusTooltipRoot != null) statusTooltipRoot.SetActive(false);
            if (tutorialOverlayRoot != null) tutorialOverlayRoot.SetActive(false);

            RefreshEquipmentButtonBadge();
            RefreshGoldDisplay();
            RefreshCampBuffBadge();

            if (tutorialOverlayRoot != null && !PlayerCampProgressManager.EnsureLoaded().TutorialCompleted)
            {
                BeginFirstTimeTutorial();
            }

            if (fadeOverlay != null)
            {
                StartCoroutine(FadeRoutine(1f, 0f, 0.30f));
            }
        }

        private void SubscribeEvents()
        {
            if (battleSystem == null) return;
            battleSystem.OnBattleInitialized += HandleBattleInitialized;
            battleSystem.OnWaveChanged += HandleWaveChanged;
            battleSystem.OnStateChanged += HandleStateChanged;
            battleSystem.OnHeroTurnChanged += HandleHeroTurnChanged;
            battleSystem.OnTurnQueueChanged += HandleTurnQueueChanged;
            battleSystem.OnTargetsChanged += HandleTargetsChanged;
            battleSystem.OnDialogueChanged += HandleDialogueChanged;
            battleSystem.OnQuizStarted += HandleQuizStarted;
            battleSystem.OnQuizResultShown += HandleQuizResultShown;
            battleSystem.OnQuizEnded += HandleQuizEnded;
            battleSystem.OnDamageDealt += HandleDamageDealt;
            battleSystem.OnUnitHealed += HandleUnitHealed;
            battleSystem.OnUnitStatusPop += HandleUnitStatusPop;
            battleSystem.OnBattleEnded += HandleBattleEnded;
            battleSystem.OnLootPickedUp += HandleLootPickedUp;
            battleSystem.OnGroundLootCountChanged += HandleGroundLootCountChanged;
            battleSystem.OnGoldChanged += HandleGoldChanged;
            battleSystem.Inventory.OnInventoryChanged += RefreshItemBagUI;
            PlayerEquipmentManager.EnsureLoaded().OnEquipmentChanged += RefreshEquipmentButtonBadge;
        }

        private void UnsubscribeEvents()
        {
            PlayerEquipmentManager.Instance.OnEquipmentChanged -= RefreshEquipmentButtonBadge;
            if (battleSystem == null) return;
            battleSystem.OnBattleInitialized -= HandleBattleInitialized;
            battleSystem.OnWaveChanged -= HandleWaveChanged;
            battleSystem.OnStateChanged -= HandleStateChanged;
            battleSystem.OnHeroTurnChanged -= HandleHeroTurnChanged;
            battleSystem.OnTurnQueueChanged -= HandleTurnQueueChanged;
            battleSystem.OnTargetsChanged -= HandleTargetsChanged;
            battleSystem.OnDialogueChanged -= HandleDialogueChanged;
            battleSystem.OnQuizStarted -= HandleQuizStarted;
            battleSystem.OnQuizResultShown -= HandleQuizResultShown;
            battleSystem.OnQuizEnded -= HandleQuizEnded;
            battleSystem.OnDamageDealt -= HandleDamageDealt;
            battleSystem.OnUnitHealed -= HandleUnitHealed;
            battleSystem.OnUnitStatusPop -= HandleUnitStatusPop;
            battleSystem.OnBattleEnded -= HandleBattleEnded;
            battleSystem.OnLootPickedUp -= HandleLootPickedUp;
            battleSystem.OnGroundLootCountChanged -= HandleGroundLootCountChanged;
            battleSystem.OnGoldChanged -= HandleGoldChanged;
            battleSystem.Inventory.OnInventoryChanged -= RefreshItemBagUI;

            if (partyCards != null)
            {
                foreach (PartyMemberCardUI card in partyCards)
                {
                    if (card != null) card.OnCardClicked -= HandlePartyCardClicked;
                }
            }

            if (battleSystem.partyUnits != null)
            {
                foreach (Unit u in battleSystem.partyUnits)
                {
                    if (u != null) u.OnStatsChanged -= HandleUnitStatsChanged;
                }
            }
            if (battleSystem.enemyUnits != null)
            {
                foreach (Unit u in battleSystem.enemyUnits)
                {
                    if (u != null) u.OnStatsChanged -= HandleUnitStatsChanged;
                }
            }
        }

        private void WireActionButtons()
        {
            if (actionButtons == null) return;

            for (int i = 0; i < actionButtons.Length; i++)
            {
                if (actionButtons[i] != null)
                {
                    int idx = i;
                    actionButtons[i].onClick.RemoveAllListeners();
                    actionButtons[i].onClick.AddListener(() => TriggerActionByIndex(idx));
                }

                if (actionFeedbacks != null && i < actionFeedbacks.Length && actionFeedbacks[i] != null)
                {
                    actionFeedbacks[i].OnHoverChanged -= HandleActionHoverChanged;
                    actionFeedbacks[i].OnHoverChanged += HandleActionHoverChanged;
                }
            }
        }

        private void WireItemBagButtons()
        {
            if (itemButtons != null)
            {
                for (int i = 0; i < itemButtons.Length; i++)
                {
                    if (itemButtons[i] == null) continue;
                    int idx = i;
                    itemButtons[i].onClick.RemoveAllListeners();
                    itemButtons[i].onClick.AddListener(() => UseItemByIndex(idx));
                }
            }

            if (closeItemBagButton != null)
            {
                closeItemBagButton.onClick.RemoveAllListeners();
                closeItemBagButton.onClick.AddListener(() => SetItemBagOpen(false));
            }
        }

        private void WireEnemyTabButtons()
        {
            if (enemyTabButtons == null) return;
            for (int i = 0; i < enemyTabButtons.Length; i++)
            {
                if (enemyTabButtons[i] == null) continue;
                int idx = i;
                enemyTabButtons[i].onClick.RemoveAllListeners();
                enemyTabButtons[i].onClick.AddListener(() =>
                {
                    if (battleSystem != null && idx < battleSystem.enemyUnits.Count)
                    {
                        battleSystem.SelectEnemyTarget(battleSystem.enemyUnits[idx]);
                    }
                });
            }
        }

        private void WirePauseControls()
        {
            if (settingsOpenButton != null)
            {
                settingsOpenButton.onClick.RemoveAllListeners();
                settingsOpenButton.onClick.AddListener(TogglePauseMenu);
            }

            if (retreatEndlessButton != null)
            {
                retreatEndlessButton.onClick.RemoveAllListeners();
                retreatEndlessButton.onClick.AddListener(() =>
                {
                    if (pausePanel != null) pausePanel.SetActive(false);
                    AudioManager.Instance?.PlayButtonClick();
                    battleSystem?.FinishEndlessExpedition();
                });
            }

            GameSettingsUIHelper.BindSettingsControls(
                pauseMusicSlider, pauseMusicValueTMP,
                pauseSfxSlider, pauseSfxValueTMP,
                pauseQuizToggle, pauseFastModeToggle,
                pauseQuizTimerToggle, pauseCameraShakeToggle, pauseFullscreenToggle,
                () =>
                {
                    if (battleSystem != null) battleSystem.ApplySavedSettings();
                    RefreshActionButtons();
                });

            GameSettingsUIHelper.WireResetSaveButton(this, pauseResetSaveButton, pauseResetSaveLabelTMP);
        }

        private void WireTutorialControls()
        {
            if (tutorialNextButton != null)
            {
                tutorialNextButton.onClick.RemoveAllListeners();
                tutorialNextButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    if (_tutorialStepIndex + 1 < TutorialSteps.Length)
                    {
                        ShowTutorialStep(_tutorialStepIndex + 1);
                    }
                    else
                    {
                        CompleteFirstTimeTutorial();
                    }
                });
            }

            if (tutorialSkipButton != null)
            {
                tutorialSkipButton.onClick.RemoveAllListeners();
                tutorialSkipButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    CompleteFirstTimeTutorial();
                });
            }
        }

        private void BeginFirstTimeTutorial()
        {
            if (tutorialOverlayRoot == null) return;
            _pausedByTutorial = battleSystem != null && !battleSystem.IsPaused;
            if (_pausedByTutorial) battleSystem.SetPaused(true);
            ShowTutorialStep(0);
        }

        private void ShowTutorialStep(int index)
        {
            if (tutorialOverlayRoot == null) return;
            _tutorialStepIndex = Mathf.Clamp(index, 0, TutorialSteps.Length - 1);
            tutorialOverlayRoot.SetActive(true);

            (string title, string body) step = TutorialSteps[_tutorialStepIndex];
            if (tutorialTitleTMP != null) tutorialTitleTMP.text = step.title;
            if (tutorialBodyTMP != null) tutorialBodyTMP.text = step.body;
            if (tutorialStepTMP != null) tutorialStepTMP.text = $"{_tutorialStepIndex + 1} / {TutorialSteps.Length}";
            if (tutorialNextLabelTMP != null)
            {
                tutorialNextLabelTMP.text = _tutorialStepIndex + 1 >= TutorialSteps.Length ? "BẮT ĐẦU CHIẾN ĐẤU!" : "TIẾP THEO >>";
            }
        }

        private void CompleteFirstTimeTutorial()
        {
            if (tutorialOverlayRoot != null) tutorialOverlayRoot.SetActive(false);
            PlayerCampProgressManager.EnsureLoaded().MarkTutorialCompleted();
            if (_pausedByTutorial && battleSystem != null)
            {
                bool pauseOpen = pausePanel != null && pausePanel.activeSelf;
                bool eqOpen = equipmentModal != null && equipmentModal.IsOpen;
                if (!pauseOpen && !eqOpen) battleSystem.SetPaused(false);
            }
            _pausedByTutorial = false;
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================
        private void HandleBattleInitialized()
        {
            if (battleSystem == null) return;

            for (int i = 0; i < battleSystem.partyUnits.Count; i++)
            {
                Unit hero = battleSystem.partyUnits[i];
                if (hero != null)
                {
                    hero.OnStatsChanged -= HandleUnitStatsChanged;
                    hero.OnStatsChanged += HandleUnitStatsChanged;
                }

                if (partyCards != null && i < partyCards.Length && partyCards[i] != null)
                {
                    partyCards[i].Bind(hero, GetPortraitForUnit(hero), GetClassIconForUnit(hero));
                    partyCards[i].OnCardClicked -= HandlePartyCardClicked;
                    partyCards[i].OnCardClicked += HandlePartyCardClicked;
                    partyCards[i].OnPointerHoverChanged -= HandleUnitHoverChanged;
                    partyCards[i].OnPointerHoverChanged += HandleUnitHoverChanged;
                }

                if (worldUnitHUDs != null && i < worldUnitHUDs.Length && worldUnitHUDs[i] != null)
                {
                    worldUnitHUDs[i].Bind(hero, worldHudLayer, GetWorldOffsetForUnit(hero));
                }
            }

            BindCurrentEnemyWaveUI();
            RefreshAllStatusDisplays();
            RefreshQuizStatsBadge();
            RefreshItemBagUI();
            RefreshGoldDisplay();
            RefreshCampBuffBadge();
        }

        private void HandleWaveChanged()
        {
            BindCurrentEnemyWaveUI();
            RefreshAllStatusDisplays();
            RefreshWaveBadge();
        }

        private void BindCurrentEnemyWaveUI()
        {
            if (battleSystem == null) return;

            for (int i = 0; i < battleSystem.enemyUnits.Count; i++)
            {
                Unit e = battleSystem.enemyUnits[i];
                if (e != null)
                {
                    e.OnStatsChanged -= HandleUnitStatsChanged;
                    e.OnStatsChanged += HandleUnitStatsChanged;
                }

                int worldIdx = 3 + i;
                if (worldUnitHUDs != null && worldIdx < worldUnitHUDs.Length && worldUnitHUDs[worldIdx] != null)
                {
                    worldUnitHUDs[worldIdx].Bind(e, worldHudLayer, GetWorldOffsetForUnit(e));
                }
            }

            // Ẩn các slot WorldHUD quái dư thừa nếu wave ít quái hơn 3
            if (worldUnitHUDs != null)
            {
                for (int idx = 3 + battleSystem.enemyUnits.Count; idx < worldUnitHUDs.Length; idx++)
                {
                    if (worldUnitHUDs[idx] != null) worldUnitHUDs[idx].Bind(null, worldHudLayer, 0.8f);
                }
            }

            Unit selected = battleSystem.SelectedEnemyTarget ?? (battleSystem.enemyUnits.Count > 0 ? battleSystem.enemyUnits[0] : null);
            if (enemyCard != null)
            {
                enemyCard.Bind(selected, GetPortraitForUnit(selected));
                enemyCard.OnPointerHoverChanged -= HandleUnitHoverChanged;
                enemyCard.OnPointerHoverChanged += HandleUnitHoverChanged;
            }

            RefreshEnemyTargetTabs();
            RefreshWaveBadge();
        }

        private void HandlePartyCardClicked(Unit hero)
        {
            if (battleSystem == null || hero == null) return;
            battleSystem.SelectAllyTarget(hero);
        }

        private void HandleTargetsChanged(Unit selectedEnemy, Unit selectedAlly)
        {
            if (enemyCard != null)
            {
                enemyCard.Bind(selectedEnemy, GetPortraitForUnit(selectedEnemy));
            }
            RefreshEnemyTargetTabs();
            RefreshAllStatusDisplays();
        }

        private void HandleTurnQueueChanged(IReadOnlyList<Unit> queue, Unit active)
        {
            RefreshTurnOrderStrip(queue, active);
            RefreshAllStatusDisplays();
        }

        private void HandleUnitStatsChanged(Unit unit)
        {
            RefreshAllStatusDisplays();
            RefreshEnemyTargetTabs();
            RefreshActionButtons();
            RefreshItemBagUI();
        }

        private void HandleStateChanged(BattleState newState)
        {
            if (newState != BattleState.PLAYERTURN) SetItemBagOpen(false);
            UpdateTurnBanner(newState);
            RefreshAllStatusDisplays();
            RefreshActionButtons();
            RefreshQuizStatsBadge();
        }

        private void HandleHeroTurnChanged(Unit hero)
        {
            RefreshAllStatusDisplays();
            RefreshActionButtons();
        }

        private void HandleDialogueChanged(string message)
        {
            if (dialogueTMP != null) dialogueTMP.text = message;
            if (dialogueBoxText != null) dialogueBoxText.text = message;
        }

        private void HandleQuizStarted(QuizQuestion q, string actionContext)
        {
            SetItemBagOpen(false);
            if (statusTooltipRoot != null) statusTooltipRoot.SetActive(false);
            if (quizModal != null)
            {
                quizModal.ShowQuestion(
                    q,
                    actionContext,
                    OnSubmitAnswerClickedWithSpeed,
                    () => battleSystem?.SkipQuizFeedbackWait()
                );
            }
            else
            {
                ShowQuizPrompt(q);
            }
            RefreshActionButtons();
        }

        private void HandleQuizResultShown(bool isCorrect, int selectedIndex, int correctIndex, int bonusDamage, string explanation, float waitTime)
        {
            if (quizModal != null)
            {
                quizModal.ShowResult(isCorrect, selectedIndex, correctIndex, bonusDamage, explanation, waitTime);
            }
            RefreshQuizStatsBadge();
        }

        private void HandleQuizEnded()
        {
            if (quizModal != null) quizModal.Hide();
            else HideQuizPrompt();
        }

        private void HandleDamageDealt(Unit target, int amount, bool isCrit)
        {
            if (floatingTextManager == null || target == null) return;
            string text = isCrit ? $"CHÍ MẠNG -{amount}!" : $"-{amount}";
            Color color = isCrit ? MedievalUITheme.GoldSoft : new Color(1f, 0.42f, 0.42f, 1f);
            float fontSize = isCrit ? 34f : 28f;
            floatingTextManager.SpawnAtWorldPosition(target.transform.position, text, color, fontSize, isCrit ? 1.42f : 1.24f);
        }

        private void HandleUnitHealed(Unit target, int amount)
        {
            if (floatingTextManager == null || target == null) return;
            floatingTextManager.SpawnAtWorldPosition(target.transform.position, $"+{amount} HP", MedievalUITheme.SuccessGreen, 30f, 1.3f);
        }

        private void HandleUnitStatusPop(Unit target, string text, Color color)
        {
            if (floatingTextManager == null || target == null) return;
            floatingTextManager.SpawnAtWorldPosition(target.transform.position, text, color, 26f, 1.22f);
        }

        private void HandleBattleEnded(BattleState finalState)
        {
            SetItemBagOpen(false);
            if (collectLootButton != null) collectLootButton.gameObject.SetActive(false);
            if (finalState == BattleState.WON) ShowVictory();
            else if (finalState == BattleState.LOST) ShowGameOver();
        }

        private void WireLootAndEquipmentControls()
        {
            if (equipmentOpenButton != null)
            {
                equipmentOpenButton.onClick.RemoveAllListeners();
                equipmentOpenButton.onClick.AddListener(ToggleEquipmentModal);
            }

            if (collectLootButton != null)
            {
                collectLootButton.onClick.RemoveAllListeners();
                collectLootButton.onClick.AddListener(() =>
                {
                    battleSystem?.CollectAllGroundLoot();
                });
            }
        }

        public void ToggleEquipmentModal()
        {
            if (equipmentModal == null) return;
            if (battleSystem != null && battleSystem.state == BattleState.WAITING_FOR_QUIZ) return;

            if (equipmentModal.IsOpen)
            {
                equipmentModal.Hide();
            }
            else
            {
                SetItemBagOpen(false);
                if (pausePanel != null && pausePanel.activeSelf)
                {
                    pausePanel.SetActive(false);
                }

                bool wasPaused = battleSystem != null && battleSystem.IsPaused;
                _pausedByEquipmentModal = !wasPaused;
                if (battleSystem != null && _pausedByEquipmentModal)
                {
                    battleSystem.SetPaused(true);
                }

                CharacterClassType? focus = battleSystem != null && battleSystem.CurrentHero != null
                    ? battleSystem.CurrentHero.characterClass
                    : (CharacterClassType?)null;

                equipmentModal.Show(HandleEquipmentModalClosed, focus);
                RefreshActionButtons();
            }
        }

        private void HandleEquipmentModalClosed()
        {
            if (battleSystem != null && _pausedByEquipmentModal)
            {
                bool pauseStillOpen = pausePanel != null && pausePanel.activeSelf;
                if (!pauseStillOpen) battleSystem.SetPaused(false);
            }
            _pausedByEquipmentModal = false;
            RefreshEquipmentButtonBadge();
            RefreshGoldDisplay();
            RefreshCampBuffBadge();
            RefreshAllStatusDisplays();
            RefreshActionButtons();
        }

        private void RefreshEquipmentButtonBadge()
        {
            if (equipmentOpenLabelTMP == null) return;
            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            int unseen = mgr.UnseenNewCount;
            equipmentOpenLabelTMP.text = unseen > 0
                ? $"KHO ĐỒ [I] (+{unseen})"
                : $"KHO ĐỒ [I] ({mgr.OwnedCount})";
        }

        private void HandleGroundLootCountChanged(int count)
        {
            if (collectLootButton == null) return;
            collectLootButton.gameObject.SetActive(count > 0);
            if (count > 0 && collectLootLabelTMP != null)
            {
                collectLootLabelTMP.text = $"NHẶT TẤT CẢ [E] ({count})";
            }
        }

        private void HandleLootPickedUp(LootDropResult drop, OwnedEquipmentInstance inst)
        {
            RefreshEquipmentButtonBadge();
            RefreshGoldDisplay();
            RefreshCampBuffBadge();
            ShowLootToast(drop);
        }

        private void ShowLootToast(LootDropResult drop)
        {
            if (lootToastRoot == null) return;
            Color rColor = EquipmentItemData.GetRarityColor(drop.DisplayRarity);
            if (lootToastBorder != null) lootToastBorder.color = rColor;

            if (lootToastTitleTMP != null)
            {
                if (drop.equipment != null)
                {
                    string rName = EquipmentItemData.GetRarityDisplayName(drop.equipment.rarity).ToUpperInvariant();
                    string cName = EquipmentItemData.GetClassDisplayName(drop.equipment.requiredClass);
                    lootToastTitleTMP.text = $"NHẶT ĐƯỢC [{rName}] {drop.equipment.itemName} ({cName})";
                }
                else
                {
                    lootToastTitleTMP.text = $"NHẶT ĐƯỢC +1 {drop.DisplayName}";
                }
                lootToastTitleTMP.color = rColor;
            }

            if (lootToastDetailTMP != null)
            {
                lootToastDetailTMP.text = drop.equipment != null
                    ? $"{drop.equipment.GetStatsSummary()}  •  Bấm [I] để mở Kho Đồ"
                    : "Đã thêm vào Túi Vật Phẩm chiến đấu [4]";
            }

            lootToastRoot.SetActive(true);
            if (_lootToastCoroutine != null) StopCoroutine(_lootToastCoroutine);
            _lootToastCoroutine = StartCoroutine(HideLootToastAfterDelay(2.6f));
        }

        private IEnumerator HideLootToastAfterDelay(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (lootToastRoot != null) lootToastRoot.SetActive(false);
        }

        // =====================================================================
        // REFRESH UI HELPERS
        // =====================================================================
        private void UpdateTurnBanner(BattleState st)
        {
            if (turnBannerTMP == null) return;

            int round = battleSystem != null ? battleSystem.TurnCount : 1;
            int wave = battleSystem != null ? battleSystem.CurrentWaveNumber : 1;
            int totalWaves = battleSystem != null ? battleSystem.TotalWaves : 1;
            string heroName = (battleSystem != null && battleSystem.CurrentHero != null)
                ? MedievalUITheme.FormatHeroDisplayName(battleSystem.CurrentHero.unitName)
                : "Anh Hùng";

            switch (st)
            {
                case BattleState.START:
                    turnBannerTMP.text = "CHUẨN BỊ CHIẾN ĐẤU";
                    if (turnSubTMP != null) turnSubTMP.text = $"Đợt {wave}/{totalWaves} • Hiệp {round}";
                    break;
                case BattleState.PLAYERTURN:
                    turnBannerTMP.text = $"LƯỢT CỦA {heroName.ToUpperInvariant()}";
                    if (turnSubTMP != null) turnSubTMP.text = $"Đợt {wave}/{totalWaves} • Hiệp {round} • [1-5] & [Tab] đổi mục tiêu";
                    break;
                case BattleState.WAITING_FOR_QUIZ:
                    turnBannerTMP.text = "THỬ THÁCH TRI THỨC";
                    if (turnSubTMP != null) turnSubTMP.text = $"Hiệp {round} • Trả lời đúng để xuất chiêu";
                    break;
                case BattleState.ENEMYTURN:
                    string enemyLabel = battleSystem != null && battleSystem.ActiveUnit != null ? battleSystem.ActiveUnit.unitName : "QUÁI VẬT";
                    turnBannerTMP.text = $"LƯỢT {enemyLabel.ToUpperInvariant()}";
                    if (turnSubTMP != null) turnSubTMP.text = $"Đợt {wave}/{totalWaves} • Hiệp {round} • Kẻ địch phản công";
                    break;
                case BattleState.WON:
                    turnBannerTMP.text = "CHIẾN THẮNG!";
                    if (turnSubTMP != null) turnSubTMP.text = $"Vượt qua {totalWaves} đợt sau {round} hiệp";
                    break;
                case BattleState.LOST:
                    turnBannerTMP.text = "THẤT BẠI";
                    if (turnSubTMP != null) turnSubTMP.text = $"Gục ngã ở Đợt {wave}/{totalWaves} • Hiệp {round}";
                    break;
            }
        }

        private void RefreshWaveBadge()
        {
            if (waveBadgeTMP == null || battleSystem == null) return;
            int dropPct = Mathf.RoundToInt(battleSystem.CurrentWaveDropChance * 100f);
            if (battleSystem.ActiveStage.isEndless)
            {
                bool isBossFloor = battleSystem.CurrentWaveNumber % 5 == 0;
                waveBadgeTMP.text = isBossFloor
                    ? $"VÔ TẬN TẦNG {battleSystem.CurrentWaveNumber} - BOSS!"
                    : $"VÔ TẬN TẦNG {battleSystem.CurrentWaveNumber} ({dropPct}%)";
            }
            else
            {
                string tag = battleSystem.ActiveStage.id switch
                {
                    CampaignStageId.ForestChapter1 => "Ch.1",
                    CampaignStageId.CryptChapter2 => "Ch.2",
                    CampaignStageId.CitadelChapter3 => "Ch.3",
                    _ => "Màn"
                };
                bool isBoss = battleSystem.CurrentWaveNumber >= battleSystem.TotalWaves;
                waveBadgeTMP.text = isBoss
                    ? $"{tag} - ĐỢT {battleSystem.CurrentWaveNumber}/{battleSystem.TotalWaves} (BOSS)"
                    : $"{tag} - ĐỢT {battleSystem.CurrentWaveNumber}/{battleSystem.TotalWaves} ({dropPct}%)";
            }
        }

        private void HandleGoldChanged(int _) => RefreshGoldDisplay();

        private void RefreshGoldDisplay()
        {
            if (goldBadgeTMP == null) return;
            int gold = battleSystem != null
                ? battleSystem.TotalPlayerGold
                : PlayerCampProgressManager.EnsureLoaded().Gold;
            goldBadgeTMP.text = $"VÀNG: {gold}";
        }

        private void RefreshCampBuffBadge()
        {
            if (campBuffBadgeTMP == null) return;
            string summary = PlayerCampProgressManager.EnsureLoaded().GetActiveBuffsShortSummary();
            campBuffBadgeTMP.text = $"Bùa: {summary}";
        }

        private void RefreshQuizStatsBadge()
        {
            if (quizStatsTMP == null || battleSystem == null) return;
            string comboTag = battleSystem.CurrentQuizStreak >= 3 ? " (x1.2 ST!)" : "";
            quizStatsTMP.text = $"Đúng: {battleSystem.CorrectAnswersCount}/{battleSystem.TotalQuestionsAnswered} • Chuỗi: {battleSystem.CurrentQuizStreak}{comboTag}";
        }

        private void RefreshTurnOrderStrip(IReadOnlyList<Unit> queue, Unit active)
        {
            if (turnOrderSlotRoots == null || turnOrderSlotRoots.Length == 0) return;

            List<Unit> ordered = new List<Unit>();
            if (active != null && active.IsAlive) ordered.Add(active);
            if (queue != null)
            {
                for (int i = 0; i < queue.Count && ordered.Count < turnOrderSlotRoots.Length; i++)
                {
                    if (queue[i] != null && queue[i].IsAlive) ordered.Add(queue[i]);
                }
            }

            for (int i = 0; i < turnOrderSlotRoots.Length; i++)
            {
                if (turnOrderSlotRoots[i] == null) continue;
                bool visible = i < ordered.Count;
                turnOrderSlotRoots[i].SetActive(visible);
                if (!visible) continue;

                Unit u = ordered[i];
                if (turnOrderSlotPortraits != null && i < turnOrderSlotPortraits.Length && turnOrderSlotPortraits[i] != null)
                {
                    turnOrderSlotPortraits[i].sprite = GetPortraitForUnit(u);
                }
                if (turnOrderSlotFrames != null && i < turnOrderSlotFrames.Length && turnOrderSlotFrames[i] != null)
                {
                    turnOrderSlotFrames[i].color = i == 0
                        ? MedievalUITheme.GoldSoft
                        : (u.isEnemy ? MedievalUITheme.DangerRed : MedievalUITheme.SlateBorder);
                }
            }
        }

        private void RefreshEnemyTargetTabs()
        {
            if (enemyTabsRow == null || battleSystem == null || enemyTabButtons == null) return;

            int enemyCount = battleSystem.enemyUnits.Count;
            enemyTabsRow.SetActive(enemyCount > 1);
            if (enemyCount <= 1) return;

            for (int i = 0; i < enemyTabButtons.Length; i++)
            {
                if (enemyTabButtons[i] == null) continue;
                bool activeSlot = i < enemyCount && battleSystem.enemyUnits[i] != null;
                enemyTabButtons[i].gameObject.SetActive(activeSlot);
                if (!activeSlot) continue;

                Unit e = battleSystem.enemyUnits[i];
                bool alive = e.IsAlive;
                bool isSelected = e == battleSystem.SelectedEnemyTarget;
                enemyTabButtons[i].interactable = alive;

                if (enemyTabLabels != null && i < enemyTabLabels.Length && enemyTabLabels[i] != null)
                {
                    enemyTabLabels[i].text = alive ? $"{e.unitName}" : $"{e.unitName} (Hạ)";
                    enemyTabLabels[i].color = alive ? MedievalUITheme.IvoryText : MedievalUITheme.SlateMuted;
                }
                if (enemyTabHpFills != null && i < enemyTabHpFills.Length && enemyTabHpFills[i] != null)
                {
                    enemyTabHpFills[i].fillAmount = e.maxHP > 0 ? Mathf.Clamp01((float)e.currentHP / e.maxHP) : 0f;
                }
                if (enemyTabBorders != null && i < enemyTabBorders.Length && enemyTabBorders[i] != null)
                {
                    enemyTabBorders[i].color = isSelected ? MedievalUITheme.GoldSoft : MedievalUITheme.SlateBorder;
                }
            }
        }

        private void RefreshAllStatusDisplays()
        {
            if (battleSystem == null) return;
            Unit activeHero = (battleSystem.state == BattleState.PLAYERTURN || battleSystem.state == BattleState.WAITING_FOR_QUIZ)
                ? battleSystem.CurrentHero
                : null;

            for (int i = 0; i < battleSystem.partyUnits.Count; i++)
            {
                Unit u = battleSystem.partyUnits[i];
                bool isTurn = u != null && u == activeHero;
                bool isSelectedAlly = u != null && u == battleSystem.SelectedAllyTarget;

                if (partyCards != null && i < partyCards.Length && partyCards[i] != null)
                {
                    partyCards[i].RefreshImmediate(isTurn, isSelectedAlly);
                }
                if (worldUnitHUDs != null && i < worldUnitHUDs.Length && worldUnitHUDs[i] != null)
                {
                    worldUnitHUDs[i].Refresh(isTurn, false);
                }
            }

            bool isEnemyTurn = battleSystem.state == BattleState.ENEMYTURN;
            if (enemyCard != null) enemyCard.Refresh(isEnemyTurn);

            for (int i = 0; i < battleSystem.enemyUnits.Count; i++)
            {
                int worldIdx = 3 + i;
                Unit e = battleSystem.enemyUnits[i];
                if (worldUnitHUDs != null && worldIdx < worldUnitHUDs.Length && worldUnitHUDs[worldIdx] != null)
                {
                    bool enemyIsActing = isEnemyTurn && e == battleSystem.ActiveUnit;
                    bool isTargeted = e != null && e == battleSystem.SelectedEnemyTarget;
                    worldUnitHUDs[worldIdx].Refresh(enemyIsActing, isTargeted);
                }
            }

            if (activeHeroBannerTMP != null)
            {
                if (activeHero != null)
                {
                    activeHeroBannerTMP.text = $"{MedievalUITheme.FormatHeroDisplayName(activeHero.unitName)} • ATK {activeHero.damage} | MAG {activeHero.magicDamage} | DEF {activeHero.EffectiveDefense} | SPD {activeHero.speed}";
                }
                else if (battleSystem.state == BattleState.ENEMYTURN && battleSystem.ActiveUnit != null)
                {
                    activeHeroBannerTMP.text = $"Đang tới lượt {battleSystem.ActiveUnit.unitName}";
                }
                else
                {
                    activeHeroBannerTMP.text = "Đội hình Chiến Binh Tri Thức";
                }
            }

            if (activeHeroPortrait != null)
            {
                Unit portraitUnit = activeHero != null ? activeHero : (battleSystem.partyUnits.Count > 0 ? battleSystem.partyUnits[0] : null);
                Sprite p = GetPortraitForUnit(portraitUnit);
                if (p != null) activeHeroPortrait.sprite = p;
            }
        }

        private void RefreshActionButtons()
        {
            if (battleSystem == null || actionButtons == null || actionButtons.Length < 5) return;

            Unit hero = battleSystem.CurrentHero;
            bool canAct = battleSystem.state == BattleState.PLAYERTURN
                          && !battleSystem.IsActionInProgress
                          && !battleSystem.IsPaused
                          && hero != null
                          && hero.IsAlive;

            string quizTag = battleSystem.enableQuizSystem ? "Quiz" : "Tự do";

            if (actionLockedOverlay != null)
            {
                bool showLock = !canAct && battleSystem.state != BattleState.WON && battleSystem.state != BattleState.LOST;
                actionLockedOverlay.SetActive(showLock);
                if (showLock && actionLockedTMP != null)
                {
                    if (battleSystem.state == BattleState.ENEMYTURN) actionLockedTMP.text = "Đang đến lượt Quái Vật phản công...";
                    else if (battleSystem.state == BattleState.WAITING_FOR_QUIZ) actionLockedTMP.text = "Đang trả lời câu hỏi tri thức...";
                    else if (battleSystem.state == BattleState.START) actionLockedTMP.text = "Đang vào trận chiến...";
                    else actionLockedTMP.text = "Đang thi triển hành động...";
                }
            }

            if (hero == null)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (actionButtons[i] != null) actionButtons[i].interactable = false;
                }
                return;
            }

            // 0: Đánh thường (skills[0])
            SkillData basicSkill = (hero.skills != null && hero.skills.Count > 0) ? hero.skills[0] : null;
            string atkTitle = basicSkill != null ? basicSkill.skillName : "Tấn Công";
            int baseStat = (basicSkill != null && basicSkill.damageType == DamageType.Magical) ? hero.magicDamage : hero.damage;
            string atkDesc = basicSkill != null
                ? $"{basicSkill.description} (Sát thương cơ bản ~{baseStat} + thưởng {quizTag})"
                : $"Tấn công cơ bản gây ~{baseStat} sát thương.";
            SetActionButton(0, true, canAct, atkTitle, $"Miễn phí • {quizTag}", GetAttackIconForHero(hero), MedievalUITheme.BtnAttack, atkDesc);

            // 1: Skill 1
            ConfigureSkillButton(1, hero, canAct, quizTag);

            // 2: Skill 2
            ConfigureSkillButton(2, hero, canAct, quizTag);

            // 3: Túi Đồ (Consumables Bag)
            int itemCount = battleSystem.Inventory.TotalItems;
            SetActionButton(3, true, canAct && itemCount > 0, "Túi Đồ", $"Còn {itemCount} món • Nhanh", heartIcon, MedievalUITheme.BtnHeal,
                "Mở túi đồ sử dụng Bình Máu (+55 HP), Bình Mana (+30 MP) hoặc Lông Phượng Hoàng (Hồi sinh). Không mất Quiz!");

            // 4: Phòng Thủ
            string tauntHint = hero.characterClass == CharacterClassType.Knight ? " + Khiêu Khích" : "";
            SetActionButton(4, true, canAct, "Phòng Thủ", $"Miễn phí • +8 MP", shieldIcon, MedievalUITheme.BtnDefend,
                $"Giảm 50% sát thương nhận vào, hồi ngay +8 MP{tauntHint} (không cần trả lời Quiz).");
        }

        private void ConfigureSkillButton(int slotIndex, Unit hero, bool canAct, string quizTag)
        {
            bool hasSkill = hero.skills != null && hero.skills.Count > slotIndex && hero.skills[slotIndex] != null;
            SkillData s = hasSkill ? hero.skills[slotIndex] : null;
            int cd = hasSkill ? hero.GetSkillCooldown(s) : 0;
            bool enoughMana = hasSkill && hero.currentMP >= s.manaCost;
            bool canCast = canAct && hasSkill && cd == 0 && enoughMana;

            Sprite icon = magicIcon;
            if (hasSkill)
            {
                if (s.IsHealSkill) icon = heartIcon;
                else if (s.defenseBonus > 0 || s.IsSelfSupport) icon = shieldIcon;
                else if (hero.characterClass == CharacterClassType.Archer) icon = arrowIcon;
                else if (hero.characterClass == CharacterClassType.Knight) icon = swordIcon;
            }

            string costText;
            if (!hasSkill) costText = "Không có";
            else if (cd > 0) costText = $"Hồi: {cd} lượt";
            else costText = $"{s.manaCost} MP • {quizTag}";

            string desc = hasSkill
                ? $"{s.description} (Tiêu hao: {s.manaCost} MP{(s.cooldownTurns > 0 ? $", Hồi chiêu {s.cooldownTurns} lượt" : "")})"
                : "Kỹ năng chưa mở khóa.";

            SetActionButton(slotIndex, hasSkill, canCast, hasSkill ? s.skillName : $"Kỹ Năng {slotIndex}", costText, icon, MedievalUITheme.BtnSkill, desc);
        }

        private void SetActionButton(int idx, bool visible, bool interactable, string title, string cost, Sprite icon, Color baseColor, string previewDesc)
        {
            if (actionButtons[idx] != null)
            {
                actionButtons[idx].gameObject.SetActive(visible);
                actionButtons[idx].interactable = interactable;
            }
            if (actionTitleTMPs != null && idx < actionTitleTMPs.Length && actionTitleTMPs[idx] != null)
            {
                actionTitleTMPs[idx].text = title;
            }
            if (actionCostTMPs != null && idx < actionCostTMPs.Length && actionCostTMPs[idx] != null)
            {
                actionCostTMPs[idx].text = cost;
            }
            if (actionIconImages != null && idx < actionIconImages.Length && actionIconImages[idx] != null && icon != null)
            {
                actionIconImages[idx].sprite = icon;
            }
            if (actionFeedbacks != null && idx < actionFeedbacks.Length && actionFeedbacks[idx] != null)
            {
                actionFeedbacks[idx].SetBaseColor(baseColor);
                actionFeedbacks[idx].HoverTitle = title;
                actionFeedbacks[idx].HoverDescription = previewDesc;
            }
        }

        private void SetItemBagOpen(bool open)
        {
            if (itemBagPanel == null) return;
            itemBagPanel.SetActive(open);
            if (open) RefreshItemBagUI();
        }

        private void RefreshItemBagUI()
        {
            if (battleSystem == null || itemButtons == null) return;

            for (int i = 0; i < BagItemOrder.Length && i < itemButtons.Length; i++)
            {
                ConsumableItemType item = BagItemOrder[i];
                int count = battleSystem.Inventory.GetCount(item);
                bool canUse = battleSystem.CanUseConsumable(item);

                if (itemButtons[i] != null) itemButtons[i].interactable = canUse;
                if (itemTitleTMPs != null && i < itemTitleTMPs.Length && itemTitleTMPs[i] != null)
                {
                    itemTitleTMPs[i].text = $"[{i + 1}] {BattleInventory.GetDisplayName(item)} ({BattleInventory.GetShortEffectDescription(item)})";
                }
                if (itemCountTMPs != null && i < itemCountTMPs.Length && itemCountTMPs[i] != null)
                {
                    itemCountTMPs[i].text = $"x{count}";
                }
            }
        }

        private void UseItemByIndex(int index)
        {
            if (battleSystem == null || index < 0 || index >= BagItemOrder.Length) return;
            ConsumableItemType item = BagItemOrder[index];

            if (!battleSystem.CanUseConsumable(item)) return;
            SetItemBagOpen(false);
            battleSystem.UseConsumableItem(item);
        }

        private void HandleActionHoverChanged(UIButtonFeedback feedback, bool isHovering)
        {
            if (skillPreviewTMP == null) return;
            if (isHovering && !string.IsNullOrEmpty(feedback.HoverDescription))
            {
                skillPreviewTMP.text = $"<color=#F2CA6B><b>{feedback.HoverTitle}:</b></color> {feedback.HoverDescription}";
            }
            else
            {
                skillPreviewTMP.text = "Bấm vào quái hoặc [Tab] để đổi mục tiêu • Bấm thẻ anh hùng để chọn người hồi phục.";
            }
        }

        private void TriggerActionByIndex(int index)
        {
            if (battleSystem == null) return;
            switch (index)
            {
                case 0:
                    SetItemBagOpen(false);
                    battleSystem.OnAttackButton();
                    break;
                case 1:
                    SetItemBagOpen(false);
                    battleSystem.OnSkill1Button();
                    break;
                case 2:
                    SetItemBagOpen(false);
                    battleSystem.OnSkill2Button();
                    break;
                case 3:
                    if (itemBagPanel != null) SetItemBagOpen(!itemBagPanel.activeSelf);
                    else battleSystem.OnHealButton();
                    break;
                case 4:
                    SetItemBagOpen(false);
                    battleSystem.OnDefendButton();
                    break;
            }
        }

        private Sprite GetPortraitForUnit(Unit u)
        {
            if (u == null) return knightPortrait;
            string n = u.unitName ?? "";
            if (u.isBoss || n.Contains("Dark") || n.Contains("Boss")) return darkKnightPortrait != null ? darkKnightPortrait : knightPortrait;
            if (n.Contains("Shadow")) return darkKnightPortrait != null ? darkKnightPortrait : magePortrait;
            if (n.Contains("Skeleton")) return skeletonPortrait != null ? skeletonPortrait : goblinPortrait;
            if (u.isEnemy || n.Contains("Goblin")) return goblinPortrait;
            if (u.characterClass == CharacterClassType.Mage || n.Contains("Mage")) return magePortrait;
            if (u.characterClass == CharacterClassType.Archer || n.Contains("Archer")) return archerPortrait;
            return knightPortrait;
        }

        private Sprite GetClassIconForUnit(Unit u)
        {
            if (u == null) return swordIcon;
            if (u.characterClass == CharacterClassType.Mage) return magicIcon;
            if (u.characterClass == CharacterClassType.Archer) return arrowIcon;
            return swordIcon;
        }

        private Sprite GetAttackIconForHero(Unit u) => GetClassIconForUnit(u);

        private float GetWorldOffsetForUnit(Unit u)
        {
            if (u == null) return 0.9f;
            if (u.isBoss) return 1.15f;
            if (u.characterClass == CharacterClassType.Mage) return 0.95f;
            if (u.characterClass == CharacterClassType.Archer) return 0.92f;
            return 0.90f;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || _isTransitioning) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (equipmentModal != null && equipmentModal.IsOpen)
                {
                    equipmentModal.Hide();
                    return;
                }
                if (itemBagPanel != null && itemBagPanel.activeSelf)
                {
                    SetItemBagOpen(false);
                    return;
                }
                if (battleSystem != null && (battleSystem.state == BattleState.WON || battleSystem.state == BattleState.LOST)) return;
                if (battleSystem != null && battleSystem.state == BattleState.WAITING_FOR_QUIZ) return;
                TogglePauseMenu();
                return;
            }

            if (kb.iKey.wasPressedThisFrame && battleSystem != null && battleSystem.state != BattleState.WAITING_FOR_QUIZ)
            {
                ToggleEquipmentModal();
                return;
            }

            if (equipmentModal != null && equipmentModal.IsOpen) return;

            if (itemBagPanel != null && itemBagPanel.activeSelf)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) UseItemByIndex(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) UseItemByIndex(1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) UseItemByIndex(2);
                else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) UseItemByIndex(3);
                else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) UseItemByIndex(4);
                return;
            }

            if (battleSystem != null && battleSystem.state == BattleState.PLAYERTURN && !battleSystem.IsActionInProgress && !battleSystem.IsPaused)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) TriggerActionByIndex(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) TriggerActionByIndex(1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) TriggerActionByIndex(2);
                else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) TriggerActionByIndex(3);
                else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) TriggerActionByIndex(4);
            }
        }

        public void TogglePauseMenu()
        {
            if (pausePanel == null) return;
            bool nextState = !pausePanel.activeSelf;
            if (nextState)
            {
                if (statusTooltipRoot != null) statusTooltipRoot.SetActive(false);
                if (retreatEndlessButton != null)
                {
                    retreatEndlessButton.gameObject.SetActive(battleSystem != null && battleSystem.ActiveStage.isEndless);
                }
                GameSettingsUIHelper.BindSettingsControls(
                    pauseMusicSlider, pauseMusicValueTMP,
                    pauseSfxSlider, pauseSfxValueTMP,
                    pauseQuizToggle, pauseFastModeToggle,
                    pauseQuizTimerToggle, pauseCameraShakeToggle, pauseFullscreenToggle,
                    () =>
                    {
                        if (battleSystem != null) battleSystem.ApplySavedSettings();
                        RefreshActionButtons();
                    });
            }
            pausePanel.SetActive(nextState);
            if (battleSystem != null) battleSystem.SetPaused(nextState);
            RefreshActionButtons();
        }

        public void ResumeBattle()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (battleSystem != null) battleSystem.SetPaused(false);
            RefreshActionButtons();
        }

        public void ShowQuizPrompt(QuizQuestion q)
        {
            if (quizModal != null)
            {
                quizModal.ShowQuestion(q, "Hãy trả lời câu hỏi để kích hoạt sức mạnh!", OnSubmitAnswerClickedWithSpeed, () => battleSystem?.SkipQuizFeedbackWait());
                return;
            }

            if (quizPanel != null)
            {
                quizPanel.SetActive(true);
                if (questionCategoryText != null) questionCategoryText.text = $"[Chủ đề: {q.category}]";
                if (questionContentText != null) questionContentText.text = q.questionText;
            }
            if (actionPanel != null) actionPanel.SetActive(false);
        }

        public void HideQuizPrompt()
        {
            if (quizModal != null) quizModal.Hide();
            if (quizPanel != null) quizPanel.SetActive(false);
            if (actionPanel != null) actionPanel.SetActive(true);
        }

        public void OnSubmitAnswerClicked(int answerIndex) => OnSubmitAnswerClickedWithSpeed(answerIndex, false);

        public void OnSubmitAnswerClickedWithSpeed(int answerIndex, bool isQuickMind)
        {
            if (battleSystem != null) battleSystem.ProcessQuizAnswer(answerIndex, isQuickMind);
            if (quizModal == null) HideQuizPrompt();
        }

        private void HandleUnitHoverChanged(Unit unit, bool entered, Vector3 cardPos)
        {
            if (statusTooltipRoot == null) return;
            if (!entered || unit == null)
            {
                statusTooltipRoot.SetActive(false);
                return;
            }

            statusTooltipRoot.SetActive(true);
            statusTooltipRoot.transform.SetAsLastSibling();
            if (statusTooltipTitleTMP != null)
            {
                statusTooltipTitleTMP.text = $"{unit.unitName} (Lv.{unit.unitLevel})";
            }

            if (statusTooltipBodyTMP != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"HP: {unit.currentHP}/{unit.maxHP}  |  MP: {unit.currentMP}/{unit.maxMP}");
                sb.AppendLine($"ATK: {unit.damage}  |  MAG: {unit.magicDamage}  |  DEF: {unit.EffectiveDefense}  |  MDEF: {unit.EffectiveMagicDefense}");
                sb.AppendLine($"SPD: {unit.speed}  |  CRIT: {Mathf.RoundToInt(unit.critRate * 100f)}%");

                if (unit.isEnemy)
                {
                    string intent = unit.GetIntentDisplayText();
                    if (!string.IsNullOrEmpty(intent)) sb.AppendLine($"Ý định: {intent}");
                }
                if (unit.ShadowShieldStacks > 0) sb.AppendLine($"Khiên Bóng Tối: {unit.ShadowShieldStacks} tầng");
                if (unit.isDefending) sb.AppendLine("Đang Thủ Thế (-50% sát thương nhận)");
                if (unit.ActiveStatuses.Count > 0)
                {
                    for (int i = 0; i < unit.ActiveStatuses.Count; i++)
                    {
                        StatusEffectInstance st = unit.ActiveStatuses[i];
                        sb.AppendLine($"- {st.GetShortBadgeLabel()} ({st.remainingTurns} lượt)");
                    }
                }
                else
                {
                    sb.Append("Trạng thái: Bình thường");
                }
                statusTooltipBodyTMP.text = sb.ToString();
            }

            if (rootCanvas != null && statusTooltipRoot.TryGetComponent<RectTransform>(out var tipRt)
                && rootCanvas.transform is RectTransform canvasRt)
            {
                Vector3 localCard = canvasRt.InverseTransformPoint(cardPos);
                float halfCw = canvasRt.rect.width * 0.5f;
                float halfCh = canvasRt.rect.height * 0.5f;
                float halfTw = tipRt.rect.width * 0.5f + 16f;
                float halfTh = tipRt.rect.height * 0.5f + 16f;

                Vector3 desired = localCard + new Vector3(unit.isEnemy ? -50f : 35f, -158f, 0f);
                desired.x = Mathf.Clamp(desired.x, -halfCw + halfTw, halfCw - halfTw);
                desired.y = Mathf.Clamp(desired.y, -halfCh + halfTh, halfCh - halfTh);
                tipRt.localPosition = desired;
            }
        }

        public void ShowVictory()
        {
            if (victoryScreen != null) victoryScreen.SetActive(true);
            if (victorySummaryTMP != null) victorySummaryTMP.text = BuildEndSummaryText(true);
        }

        public void ShowGameOver()
        {
            if (gameOverScreen != null) gameOverScreen.SetActive(true);
            if (gameOverSummaryTMP != null) gameOverSummaryTMP.text = BuildEndSummaryText(false);
        }

        private string BuildEndSummaryText(bool won)
        {
            if (battleSystem == null) return "";
            int total = battleSystem.TotalQuestionsAnswered;
            int correct = battleSystem.CorrectAnswersCount;
            int pct = total > 0 ? Mathf.RoundToInt((float)correct / total * 100f) : 0;
            int aliveCount = 0;
            foreach (Unit u in battleSystem.partyUnits)
            {
                if (u != null && u.IsAlive) aliveCount++;
            }

            string headline = won
                ? "Đội hình anh hùng đã chinh phục toàn bộ thử thách!"
                : "Hãy ôn tập thêm kiến thức và quay lại phục thù!";

            int eqTotal = 0, cCount = 0, rCount = 0, eCount = 0, lCount = 0;
            List<EquipmentItemData> bestFound = new List<EquipmentItemData>();
            if (battleSystem.SessionLootCollected != null)
            {
                foreach (LootDropResult drop in battleSystem.SessionLootCollected)
                {
                    if (drop.equipment == null) continue;
                    eqTotal++;
                    bestFound.Add(drop.equipment);
                    switch (drop.equipment.rarity)
                    {
                        case ItemRarity.Common: cCount++; break;
                        case ItemRarity.Rare: rCount++; break;
                        case ItemRarity.Epic: eCount++; break;
                        case ItemRarity.Legendary: lCount++; break;
                    }
                }
            }
            bestFound.Sort((a, b) => b.GetPowerScore().CompareTo(a.GetPowerScore()));

            string lootLine = eqTotal > 0
                ? $"- Trang bị thu được: <b>{eqTotal} món</b> (<color=#FFB020>{lCount} Huyền Thoại</color>, <color=#B56BFF>{eCount} Sử Thi</color>, <color=#4AA3FF>{rCount} Hiếm</color>, {cCount} Thường)"
                : "- Trang bị thu được: <b>0 món</b> (Tiến xa hơn để tăng tỉ lệ rớt!)";

            string highlightLine = "";
            if (bestFound.Count > 0)
            {
                List<string> topNames = new List<string>();
                for (int i = 0; i < Mathf.Min(2, bestFound.Count); i++)
                {
                    string hex = EquipmentItemData.GetRarityHex(bestFound[i].rarity);
                    topNames.Add($"<color={hex}><b>{bestFound[i].itemName}</b></color>");
                }
                highlightLine = $"\n- Nổi bật: {string.Join(", ", topNames)}";
            }

            string stageWaveStr = battleSystem.ActiveStage.isEndless
                ? $"Tầng {battleSystem.CurrentWaveNumber} (Vô Tận)"
                : $"Đợt {battleSystem.CurrentWaveNumber}/{battleSystem.TotalWaves}";
            string starStr = won && !battleSystem.ActiveStage.isEndless
                ? $"  |  Đánh giá: <color=#FFD24A><b>{battleSystem.LastEarnedStars}/3 SAO</b></color>"
                : "";
            string firstGoldStr = battleSystem.LastFirstClearGoldBonus > 0
                ? $" (gồm +{battleSystem.LastFirstClearGoldBonus}V qua màn đầu)"
                : "";

            string levelUpStr = "";
            if (battleSystem.LeveledUpClasses != null && battleSystem.LeveledUpClasses.Count > 0)
            {
                PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();
                List<string> lvParts = new List<string>();
                for (int i = 0; i < battleSystem.LeveledUpClasses.Count; i++)
                {
                    CharacterClassType cls = battleSystem.LeveledUpClasses[i];
                    int newLv = camp.GetHeroProgress(cls).level;
                    lvParts.Add($"{EquipmentItemData.GetClassDisplayName(cls)} (Lv.{newLv})");
                }
                levelUpStr = $"\n- <color=#6ED2FF><b>LÊN CẤP: {string.Join(", ", lvParts)}!</b></color>";
            }

            string unlockStr = battleSystem.LastUnlockedNewStage
                ? "\n- <color=#FFD24A><b>ĐÃ MỞ KHÓA CHƯƠNG CHIẾN DỊCH TIẾP THEO!</b></color>"
                : "";

            return $"{headline}\n\n" +
                   $"- Chiến trường: <b>{battleSystem.ActiveStage.title}</b> ({stageWaveStr}){starStr}\n" +
                   $"- Tổng hiệp: <b>{battleSystem.TurnCount}</b>  |  Sống sót: <b>{aliveCount}/{battleSystem.partyUnits.Count}</b>\n" +
                   $"- Câu hỏi đúng: <b>{correct}/{total}</b> ({pct}%)  |  Chuỗi cao nhất: <b>{battleSystem.BestQuizStreak}</b>\n" +
                   $"- Thưởng trận: <b>+{battleSystem.SessionGoldEarned} Vàng</b>{firstGoldStr}  |  <b>+{battleSystem.SessionExpEarned} EXP</b> (Quỹ: {battleSystem.TotalPlayerGold}V)\n" +
                   $"{lootLine}{highlightLine}{levelUpStr}{unlockStr}";
        }

        public void RestartBattle()
        {
            if (_isTransitioning) return;
            if (battleSystem != null) battleSystem.SetPaused(false);
            StartCoroutine(TransitionAndLoadSceneIndex(SceneManager.GetActiveScene().buildIndex));
        }

        public void ReturnToCampHub()
        {
            if (_isTransitioning) return;
            if (battleSystem != null) battleSystem.SetPaused(false);
            StartCoroutine(TransitionAndLoadSceneName("CampHub"));
        }

        public void ReturnToMainMenu()
        {
            if (_isTransitioning) return;
            if (battleSystem != null) battleSystem.SetPaused(false);
            StartCoroutine(TransitionAndLoadSceneName("MainMenu"));
        }

        private IEnumerator TransitionAndLoadSceneIndex(int sceneIndex)
        {
            _isTransitioning = true;
            if (fadeOverlay != null) yield return FadeRoutine(0f, 1f, 0.28f);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneIndex);
        }

        private IEnumerator TransitionAndLoadSceneName(string sceneName)
        {
            _isTransitioning = true;
            if (fadeOverlay != null) yield return FadeRoutine(0f, 1f, 0.28f);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
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
    }
}
