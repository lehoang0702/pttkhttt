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
using MedievalRPG.Visuals;

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
            ("BƯỚC 2/4: QUIZ B1/B2, PHẢN XẠ & NỘ HỢP KÍCH [Q]",
             "Trả lời đúng câu B1/B2 (nhất là 5s đầu: QUICK MIND) để tung chiêu mạnh, nhận Vàng & tích Nộ Tri Thức.\nKhi Nộ đạt 100/100, bấm [Q] để cả 3 anh hùng tung TUYỆT KỸ HỢP KÍCH! Boss đánh mạnh còn kích hoạt ĐỠ ĐÒN PHẢN XẠ (8s)!"),
            ("BƯỚC 3/4: NHẶT ĐỒ, CƯỜNG HÓA & LÒ RÈN",
             "Quái gục sẽ rớt Vàng và Trang bị Thường -> Hiếm -> Sử Thi -> Huyền Thoại.\nMở Kho Đồ [I] để xem So Sánh Chỉ Số, Cường Hóa (+1..+5), Bán hoặc Ghép 3 đồ thành phẩm chất cao hơn!"),
            ("BƯỚC 4/4: SỰ KIỆN DỌC ĐƯỜNG & SỔ ÔN TẬP",
             "Giữa các đợt hành quân có thể gặp Rương Cổ, Đền Thờ hoặc Học Giả dọc đường.\nBoss bật Khiên Bóng Tối ở nửa máu: hãy trả lời đúng Quiz hoặc tung Hợp Kích để phá giáp. Câu sai được lưu vào Sổ Ôn Tập [N]!")
        };

        [Header("V3: Nộ Tri Thức [Q], Sự Kiện Dọc Đường & Milestone Flash")]
        [SerializeField] private UnityEngine.UI.Button synergyUltimateButton;
        [SerializeField] private UnityEngine.UI.Image synergyButtonBackground;
        [SerializeField] private UnityEngine.UI.Image synergyGaugeFill;
        [SerializeField] private TextMeshProUGUI synergyLabelTMP;

        [SerializeField] private GameObject roadsideEventRoot;
        [SerializeField] private TextMeshProUGUI roadsideTitleTMP;
        [SerializeField] private TextMeshProUGUI roadsideDescTMP;
        [SerializeField] private UnityEngine.UI.Button[] roadsideOptionButtons = new UnityEngine.UI.Button[3];
        [SerializeField] private TextMeshProUGUI[] roadsideOptionLabels = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] roadsideOptionSubtitles = new TextMeshProUGUI[3];

        [SerializeField] private UnityEngine.UI.Image screenFlashOverlay;

        private float _displayedKnowledgeRatio;
        private float _targetKnowledgeRatio;
        private RoadsideEventOffer _activeRoadsideOffer;
        private Coroutine _flashCoroutine;
        private Coroutine _celebrationCoroutine;

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

        public void ConfigureSynergyAndEventUI(
            UnityEngine.UI.Button synBtn, UnityEngine.UI.Image synBg, UnityEngine.UI.Image synFill, TextMeshProUGUI synLbl,
            GameObject evRoot, TextMeshProUGUI evTitle, TextMeshProUGUI evDesc,
            UnityEngine.UI.Button[] evOptBtns, TextMeshProUGUI[] evOptLbls, TextMeshProUGUI[] evOptSubs,
            UnityEngine.UI.Image flashImg)
        {
            synergyUltimateButton = synBtn;
            synergyButtonBackground = synBg;
            synergyGaugeFill = synFill;
            synergyLabelTMP = synLbl;
            roadsideEventRoot = evRoot;
            roadsideTitleTMP = evTitle;
            roadsideDescTMP = evDesc;
            roadsideOptionButtons = evOptBtns;
            roadsideOptionLabels = evOptLbls;
            roadsideOptionSubtitles = evOptSubs;
            screenFlashOverlay = flashImg;
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
            EnsureSynergyAndEventUI();
            WireSynergyAndEventControls();

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
            if (roadsideEventRoot != null) roadsideEventRoot.SetActive(false);
            if (screenFlashOverlay != null) screenFlashOverlay.gameObject.SetActive(false);

            RefreshEquipmentButtonBadge();
            RefreshGoldDisplay();
            RefreshCampBuffBadge();
            RefreshKnowledgeGaugeUI(battleSystem != null ? battleSystem.KnowledgeGauge : 0, CombatCalculator.MaxKnowledgeGauge);

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
            battleSystem.OnQuizStartedWithMode += HandleQuizStartedWithMode;
            battleSystem.OnQuizResultShown += HandleQuizResultShown;
            battleSystem.OnQuizEnded += HandleQuizEnded;
            battleSystem.OnKnowledgeGaugeChanged += HandleKnowledgeGaugeChanged;
            battleSystem.OnRoadsideEventStarted += HandleRoadsideEventStarted;
            battleSystem.OnRoadsideEventEnded += HandleRoadsideEventEnded;
            battleSystem.OnSynergyUltimateExecuted += HandleSynergyUltimateExecuted;
            battleSystem.OnDamageDealt += HandleDamageDealt;
            battleSystem.OnUnitHealed += HandleUnitHealed;
            battleSystem.OnUnitStatusPop += HandleUnitStatusPop;
            battleSystem.OnBattleEnded += HandleBattleEnded;
            battleSystem.OnLootPickedUp += HandleLootPickedUp;
            battleSystem.OnGroundLootCountChanged += HandleGroundLootCountChanged;
            battleSystem.OnGoldChanged += HandleGoldChanged;
            battleSystem.Inventory.OnInventoryChanged += RefreshItemBagUI;
            PlayerEquipmentManager.EnsureLoaded().OnEquipmentChanged += RefreshEquipmentButtonBadge;
            PlayerCampProgressManager.EnsureLoaded().OnCampProgressChanged += RefreshGoldDisplay;
        }

        private void UnsubscribeEvents()
        {
            PlayerEquipmentManager.Instance.OnEquipmentChanged -= RefreshEquipmentButtonBadge;
            PlayerCampProgressManager.Instance.OnCampProgressChanged -= RefreshGoldDisplay;
            if (battleSystem == null) return;
            battleSystem.OnBattleInitialized -= HandleBattleInitialized;
            battleSystem.OnWaveChanged -= HandleWaveChanged;
            battleSystem.OnStateChanged -= HandleStateChanged;
            battleSystem.OnHeroTurnChanged -= HandleHeroTurnChanged;
            battleSystem.OnTurnQueueChanged -= HandleTurnQueueChanged;
            battleSystem.OnTargetsChanged -= HandleTargetsChanged;
            battleSystem.OnDialogueChanged -= HandleDialogueChanged;
            battleSystem.OnQuizStarted -= HandleQuizStarted;
            battleSystem.OnQuizStartedWithMode -= HandleQuizStartedWithMode;
            battleSystem.OnQuizResultShown -= HandleQuizResultShown;
            battleSystem.OnQuizEnded -= HandleQuizEnded;
            battleSystem.OnKnowledgeGaugeChanged -= HandleKnowledgeGaugeChanged;
            battleSystem.OnRoadsideEventStarted -= HandleRoadsideEventStarted;
            battleSystem.OnRoadsideEventEnded -= HandleRoadsideEventEnded;
            battleSystem.OnSynergyUltimateExecuted -= HandleSynergyUltimateExecuted;
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
                    if (card != null)
                    {
                        card.OnCardClicked -= HandlePartyCardClicked;
                        card.OnPointerHoverChanged -= HandleUnitHoverChanged;
                    }
                }
            }

            if (enemyCard != null)
            {
                enemyCard.OnPointerHoverChanged -= HandleUnitHoverChanged;
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

            GameSettingsUIHelper.WireResetSaveButton(this, pauseResetSaveButton, pauseResetSaveLabelTMP, () =>
            {
                if (battleSystem != null) battleSystem.ApplyPartyEquipment();
                RefreshGoldDisplay();
                RefreshCampBuffBadge();
                RefreshEquipmentButtonBadge();
                RefreshAllStatusDisplays();
                RefreshActionButtons();
            });
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
            if (statusTooltipRoot != null) statusTooltipRoot.SetActive(false);
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
            HandleQuizStartedWithMode(q, actionContext, QuizPromptMode.AttackSkill);
        }

        private void HandleQuizStartedWithMode(QuizQuestion q, string actionContext, QuizPromptMode mode)
        {
            SetItemBagOpen(false);
            if (statusTooltipRoot != null) statusTooltipRoot.SetActive(false);
            if (quizModal != null)
            {
                int streak = battleSystem != null ? battleSystem.CurrentQuizStreak : 0;
                int freeHints = battleSystem != null ? battleSystem.FreeHintsRemaining : 0;
                quizModal.ShowQuestionWithMode(
                    q,
                    actionContext,
                    mode,
                    streak,
                    freeHints,
                    battleSystem != null ? (System.Func<bool>)battleSystem.TryConsumeFreeHint : null,
                    OnSubmitAnswerWithModeFlags,
                    () => battleSystem?.SkipQuizFeedbackWait()
                );
            }
            else
            {
                ShowQuizPrompt(q);
            }
            RefreshActionButtons();
        }

        private void HandleKnowledgeGaugeChanged(int current, int max)
        {
            RefreshKnowledgeGaugeUI(current, max);
        }

        private void HandleSynergyUltimateExecuted()
        {
            TriggerScreenFlash(MedievalUITheme.GoldSoft, 0.34f, 0.22f);
        }

        private void HandleRoadsideEventStarted(RoadsideEventOffer offer)
        {
            EnsureSynergyAndEventUI();
            WireSynergyAndEventControls();
            _activeRoadsideOffer = offer;
            if (roadsideEventRoot == null || offer == null)
            {
                battleSystem?.SelectRoadsideEventChoice(null);
                return;
            }

            if (roadsideTitleTMP != null) roadsideTitleTMP.text = offer.title;
            if (roadsideDescTMP != null) roadsideDescTMP.text = offer.description;

            int gold = PlayerCampProgressManager.EnsureLoaded().Gold;
            for (int i = 0; i < 3; i++)
            {
                bool hasOpt = offer.options != null && i < offer.options.Count && offer.options[i] != null;
                if (roadsideOptionButtons != null && i < roadsideOptionButtons.Length && roadsideOptionButtons[i] != null)
                {
                    roadsideOptionButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt)
                    {
                        RoadsideChoiceOption opt = offer.options[i];
                        bool affordable = opt.goldCost <= 0 || gold >= opt.goldCost;
                        roadsideOptionButtons[i].interactable = affordable;
                        if (roadsideOptionLabels != null && i < roadsideOptionLabels.Length && roadsideOptionLabels[i] != null)
                        {
                            roadsideOptionLabels[i].text = $"[{i + 1}] {opt.label}";
                        }
                        if (roadsideOptionSubtitles != null && i < roadsideOptionSubtitles.Length && roadsideOptionSubtitles[i] != null)
                        {
                            roadsideOptionSubtitles[i].text = affordable ? opt.subtitle : $"{opt.subtitle} (Không đủ Vàng)";
                        }
                    }
                }
            }

            roadsideEventRoot.SetActive(true);
            roadsideEventRoot.transform.SetAsLastSibling();
        }

        private void HandleRoadsideEventEnded()
        {
            _activeRoadsideOffer = null;
            if (roadsideEventRoot != null) roadsideEventRoot.SetActive(false);
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
            if (statusTooltipRoot != null) statusTooltipRoot.SetActive(false);
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
                if (statusTooltipRoot != null) statusTooltipRoot.SetActive(false);
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
            string comboTag = battleSystem.CurrentQuizStreak >= 3 ? " <color=#5CE09A>[COMBO x1.2!]</color>" : "";
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

            RefreshKnowledgeGaugeUI(battleSystem.KnowledgeGauge, CombatCalculator.MaxKnowledgeGauge);
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

        public enum UnitPortraitKind
        {
            Knight,
            Mage,
            Archer,
            Goblin,
            Skeleton,
            ShadowMage,
            Boss
        }

        public static UnitPortraitKind ResolveUnitPortraitKind(Unit u)
        {
            if (u == null) return UnitPortraitKind.Knight;
            string n = u.unitName ?? "";
            if (u.isBoss || n.Contains("Dark") || n.Contains("Boss") || n.Contains("Ma Vương") || n.Contains("Thủ Lĩnh"))
                return UnitPortraitKind.Boss;
            if (n.Contains("Shadow") || n.Contains("Pháp Sư") || (u.isEnemy && u.characterClass == CharacterClassType.Mage))
                return UnitPortraitKind.ShadowMage;
            if (n.Contains("Skeleton") || n.Contains("Xương") || (u.isEnemy && u.characterClass == CharacterClassType.Archer))
                return UnitPortraitKind.Skeleton;
            if (u.isEnemy || n.Contains("Goblin")) return UnitPortraitKind.Goblin;
            if (u.characterClass == CharacterClassType.Mage || n.Contains("Mage")) return UnitPortraitKind.Mage;
            if (u.characterClass == CharacterClassType.Archer || n.Contains("Archer")) return UnitPortraitKind.Archer;
            return UnitPortraitKind.Knight;
        }

        private Sprite GetPortraitForUnit(Unit u)
        {
            switch (ResolveUnitPortraitKind(u))
            {
                case UnitPortraitKind.Boss:
                    return darkKnightPortrait != null ? darkKnightPortrait : knightPortrait;
                case UnitPortraitKind.ShadowMage:
                    return darkKnightPortrait != null ? darkKnightPortrait : magePortrait;
                case UnitPortraitKind.Skeleton:
                    return skeletonPortrait != null ? skeletonPortrait : (archerPortrait != null ? archerPortrait : goblinPortrait);
                case UnitPortraitKind.Goblin:
                    return goblinPortrait;
                case UnitPortraitKind.Mage:
                    return magePortrait;
                case UnitPortraitKind.Archer:
                    return archerPortrait;
                default:
                    return knightPortrait;
            }
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
            if (u == null) return 0.95f;
            float scaleY = Mathf.Abs(u.transform.lossyScale.y);
            if (scaleY < 0.05f) scaleY = 1f;

            Sprite refSprite = null;
            if (u.pixelAnimator != null && u.pixelAnimator.idleFrames != null && u.pixelAnimator.idleFrames.Count > 0)
            {
                refSprite = u.pixelAnimator.idleFrames[0];
            }
            if (refSprite == null && u.pixelAnimator != null && u.pixelAnimator.SpriteRendererComponent != null)
            {
                refSprite = u.pixelAnimator.SpriteRendererComponent.sprite;
            }

            if (refSprite != null)
            {
                float topWorldY = refSprite.bounds.max.y * scaleY;
                float margin = u.isBoss ? 0.24f : 0.18f;
                return Mathf.Max(0.85f, topWorldY + margin);
            }

            if (u.isBoss) return 1.35f;
            if (u.characterClass == CharacterClassType.Mage) return 1.02f;
            if (u.characterClass == CharacterClassType.Archer) return 1.08f;
            return 1.12f;
        }

        private void Update()
        {
            UpdateSynergyGaugeAnimation();

            Keyboard kb = Keyboard.current;
            if (kb == null || _isTransitioning) return;

            if (roadsideEventRoot != null && roadsideEventRoot.activeSelf && _activeRoadsideOffer != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) ChooseRoadsideOption(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) ChooseRoadsideOption(1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) ChooseRoadsideOption(2);
                return;
            }

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
                if (kb.qKey.wasPressedThisFrame && battleSystem.IsSynergyReady)
                {
                    SetItemBagOpen(false);
                    battleSystem.OnSynergyUltimateButton();
                    return;
                }
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

        public void OnSubmitAnswerWithModeFlags(int answerIndex, bool isQuickMind, bool wasSkipped)
        {
            if (battleSystem != null) battleSystem.ProcessQuizAnswer(answerIndex, isQuickMind, wasSkipped);
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
            PlayMilestoneCelebration();
        }

        private void PlayMilestoneCelebration()
        {
            TriggerScreenFlash(MedievalUITheme.GoldSoft, 0.36f, 0.24f);
            if (_celebrationCoroutine != null) StopCoroutine(_celebrationCoroutine);
            _celebrationCoroutine = StartCoroutine(VictoryCelebrationRoutine());
        }

        private void TriggerScreenFlash(Color color, float peakAlpha, float duration)
        {
            EnsureScreenFlashExists();
            if (screenFlashOverlay == null) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(ScreenFlashRoutine(color, peakAlpha, duration));
        }

        private IEnumerator ScreenFlashRoutine(Color color, float peakAlpha, float duration)
        {
            screenFlashOverlay.gameObject.SetActive(true);
            screenFlashOverlay.transform.SetAsLastSibling();
            yield return MedievalEase.Tween(duration, k =>
            {
                if (screenFlashOverlay == null) return;
                Color c = color;
                c.a = Mathf.Lerp(peakAlpha, 0f, k);
                screenFlashOverlay.color = c;
            }, MedievalEase.OutCubic, unscaled: true);
            if (screenFlashOverlay != null) screenFlashOverlay.gameObject.SetActive(false);
            _flashCoroutine = null;
        }

        private IEnumerator VictoryCelebrationRoutine()
        {
            if (victoryScreen != null)
            {
                Transform endCard = victoryScreen.transform.Find("EndCard");
                if (endCard != null)
                {
                    endCard.localScale = Vector3.one * 0.78f;
                    StartCoroutine(MedievalEase.Tween(0.35f, k =>
                    {
                        if (endCard != null)
                        {
                            float s = Mathf.LerpUnclamped(0.78f, 1.0f, k);
                            endCard.localScale = new Vector3(s, s, 1f);
                        }
                    }, MedievalEase.OutBack, unscaled: true));
                }

                // UI Confetti Burst trên VictoryScreen
                const int pieceCount = 32;
                Color[] palette =
                {
                    MedievalUITheme.GoldSoft,
                    MedievalUITheme.SuccessGreen,
                    MedievalUITheme.MpSapphire,
                    MedievalUITheme.RarityEpic
                };
                RectTransform[] rects = new RectTransform[pieceCount];
                Image[] imgs = new Image[pieceCount];
                Vector2[] vels = new Vector2[pieceCount];
                float[] spins = new float[pieceCount];

                for (int i = 0; i < pieceCount; i++)
                {
                    GameObject p = new GameObject($"UIConfetti_{i}", typeof(RectTransform), typeof(Image));
                    p.transform.SetParent(victoryScreen.transform, false);
                    RectTransform rt = p.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(12f, 8f);
                    rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), -60f);
                    Image im = p.GetComponent<Image>();
                    im.color = palette[i % palette.Length];
                    im.raycastTarget = false;
                    rects[i] = rt;
                    imgs[i] = im;
                    vels[i] = new Vector2(UnityEngine.Random.Range(-240f, 240f), UnityEngine.Random.Range(420f, 760f));
                    spins[i] = UnityEngine.Random.Range(-360f, 360f);
                }

                float elapsed = 0f;
                const float dur = 1.45f;
                while (elapsed < dur && victoryScreen != null && victoryScreen.activeSelf)
                {
                    float dt = Time.unscaledDeltaTime;
                    elapsed += dt;
                    float t = Mathf.Clamp01(elapsed / dur);
                    for (int i = 0; i < pieceCount; i++)
                    {
                        if (rects[i] == null) continue;
                        vels[i].y -= 920f * dt;
                        rects[i].anchoredPosition += vels[i] * dt;
                        rects[i].Rotate(0f, 0f, spins[i] * dt);
                        Color c = imgs[i].color;
                        c.a = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);
                        imgs[i].color = c;
                    }
                    yield return null;
                }

                for (int i = 0; i < pieceCount; i++)
                {
                    if (rects[i] != null) Destroy(rects[i].gameObject);
                }
            }
            _celebrationCoroutine = null;
        }

        private void EnsureScreenFlashExists()
        {
            if (screenFlashOverlay != null || rootCanvas == null) return;
            Transform existing = rootCanvas.transform.Find("ScreenFlashOverlay");
            if (existing != null && existing.TryGetComponent<Image>(out var exImg))
            {
                screenFlashOverlay = exImg;
                return;
            }
            GameObject go = new GameObject("ScreenFlashOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rootCanvas.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            screenFlashOverlay = go.GetComponent<Image>();
            screenFlashOverlay.color = new Color(1f, 0.85f, 0.35f, 0f);
            screenFlashOverlay.raycastTarget = false;
            go.SetActive(false);
        }

        private void WireSynergyAndEventControls()
        {
            if (synergyUltimateButton != null)
            {
                synergyUltimateButton.onClick.RemoveAllListeners();
                synergyUltimateButton.onClick.AddListener(() =>
                {
                    if (battleSystem == null) return;
                    SetItemBagOpen(false);
                    battleSystem.OnSynergyUltimateButton();
                });
            }

            if (roadsideOptionButtons != null)
            {
                for (int i = 0; i < roadsideOptionButtons.Length; i++)
                {
                    if (roadsideOptionButtons[i] == null) continue;
                    int idx = i;
                    roadsideOptionButtons[i].onClick.RemoveAllListeners();
                    roadsideOptionButtons[i].onClick.AddListener(() => ChooseRoadsideOption(idx));
                }
            }
        }

        private void ChooseRoadsideOption(int idx)
        {
            if (_activeRoadsideOffer == null || _activeRoadsideOffer.options == null || idx < 0 || idx >= _activeRoadsideOffer.options.Count) return;
            RoadsideChoiceOption opt = _activeRoadsideOffer.options[idx];
            if (opt.goldCost > 0 && PlayerCampProgressManager.EnsureLoaded().Gold < opt.goldCost) return;
            AudioManager.Instance?.PlayButtonClick();
            battleSystem?.SelectRoadsideEventChoice(opt);
        }

        private void RefreshKnowledgeGaugeUI(int current, int max)
        {
            _targetKnowledgeRatio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            bool ready = current >= max && max > 0;
            bool canPress = ready && battleSystem != null && battleSystem.state == BattleState.PLAYERTURN && !battleSystem.IsActionInProgress;

            if (synergyUltimateButton != null) synergyUltimateButton.interactable = canPress;
            if (synergyButtonBackground != null)
            {
                synergyButtonBackground.color = ready ? MedievalUITheme.BtnPrimaryGold : MedievalUITheme.ObsidianPanel;
            }
            if (synergyGaugeFill != null)
            {
                synergyGaugeFill.color = ready ? MedievalUITheme.GoldSoft : MedievalUITheme.MpSapphire;
            }
            if (synergyLabelTMP != null)
            {
                synergyLabelTMP.text = ready
                    ? "[Q] TUYỆT KỸ HỢP KÍCH! (SẴN SÀNG)"
                    : $"NỘ TRI THỨC: {current}/{max}";
                synergyLabelTMP.color = ready ? MedievalUITheme.DarkInkText : MedievalUITheme.IvoryText;
            }
        }

        private void UpdateSynergyGaugeAnimation()
        {
            if (synergyGaugeFill != null)
            {
                _displayedKnowledgeRatio = Mathf.Lerp(_displayedKnowledgeRatio, _targetKnowledgeRatio, Time.unscaledDeltaTime * 8f);
                synergyGaugeFill.fillAmount = _displayedKnowledgeRatio;
            }

            if (synergyUltimateButton != null && synergyUltimateButton.interactable && synergyButtonBackground != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                synergyButtonBackground.color = Color.Lerp(MedievalUITheme.BtnPrimaryGold, MedievalUITheme.GoldSoft, pulse * 0.45f);
            }
        }

        private void EnsureSynergyAndEventUI()
        {
            if (rootCanvas == null) return;
            TMP_FontAsset fontBold = dialogueTMP != null ? dialogueTMP.font : TMP_Settings.defaultFontAsset;
            Image samplePanel = actionPanel != null ? actionPanel.GetComponent<Image>() : null;
            Sprite panelSprite = samplePanel != null ? samplePanel.sprite : null;

            if (synergyUltimateButton == null)
            {
                Transform existing = rootCanvas.transform.Find("SynergyGaugeButton");
                if (existing != null)
                {
                    synergyUltimateButton = existing.GetComponent<Button>();
                    synergyButtonBackground = existing.GetComponent<Image>();
                    synergyGaugeFill = existing.Find("Fill")?.GetComponent<Image>();
                    synergyLabelTMP = existing.Find("Label")?.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    GameObject synGo = new GameObject("SynergyGaugeButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    synGo.transform.SetParent(rootCanvas.transform, false);
                    RectTransform sRt = synGo.GetComponent<RectTransform>();
                    sRt.anchorMin = new Vector2(0f, 0f);
                    sRt.anchorMax = new Vector2(0f, 0f);
                    sRt.pivot = new Vector2(0f, 0f);
                    sRt.anchoredPosition = new Vector2(64f, 234f);
                    sRt.sizeDelta = new Vector2(390f, 38f);

                    synergyButtonBackground = synGo.GetComponent<Image>();
                    synergyButtonBackground.sprite = panelSprite;
                    synergyButtonBackground.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                    synergyButtonBackground.pixelsPerUnitMultiplier = 0.7f;
                    synergyButtonBackground.color = MedievalUITheme.ObsidianPanel;

                    synergyUltimateButton = synGo.GetComponent<Button>();
                    synergyUltimateButton.targetGraphic = synergyButtonBackground;
                    synergyUltimateButton.interactable = false;

                    GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                    fillGo.transform.SetParent(synGo.transform, false);
                    RectTransform fRt = fillGo.GetComponent<RectTransform>();
                    fRt.anchorMin = Vector2.zero;
                    fRt.anchorMax = Vector2.one;
                    fRt.offsetMin = new Vector2(4f, 4f);
                    fRt.offsetMax = new Vector2(-4f, -4f);
                    synergyGaugeFill = fillGo.GetComponent<Image>();
                    synergyGaugeFill.sprite = GetWhiteBarSprite();
                    synergyGaugeFill.type = Image.Type.Filled;
                    synergyGaugeFill.fillMethod = Image.FillMethod.Horizontal;
                    synergyGaugeFill.fillAmount = 0f;
                    synergyGaugeFill.color = MedievalUITheme.MpSapphire;
                    synergyGaugeFill.raycastTarget = false;

                    GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lblGo.transform.SetParent(synGo.transform, false);
                    RectTransform lRt = lblGo.GetComponent<RectTransform>();
                    lRt.anchorMin = Vector2.zero;
                    lRt.anchorMax = Vector2.one;
                    lRt.offsetMin = new Vector2(8f, 2f);
                    lRt.offsetMax = new Vector2(-8f, -2f);
                    synergyLabelTMP = lblGo.GetComponent<TextMeshProUGUI>();
                    if (fontBold != null) synergyLabelTMP.font = fontBold;
                    synergyLabelTMP.fontSize = 14f;
                    synergyLabelTMP.fontStyle = FontStyles.Bold;
                    synergyLabelTMP.alignment = TextAlignmentOptions.Center;
                    synergyLabelTMP.color = MedievalUITheme.IvoryText;
                    synergyLabelTMP.raycastTarget = false;
                }
            }

            if (roadsideEventRoot == null)
            {
                Transform existingEv = rootCanvas.transform.Find("RoadsideEventOverlay");
                if (existingEv != null)
                {
                    roadsideEventRoot = existingEv.gameObject;
                    Transform card = existingEv.Find("EventCard");
                    if (card != null)
                    {
                        if (roadsideTitleTMP == null) roadsideTitleTMP = card.Find("EventTitle")?.GetComponent<TextMeshProUGUI>();
                        if (roadsideDescTMP == null) roadsideDescTMP = card.Find("EventDesc")?.GetComponent<TextMeshProUGUI>();
                        Transform optParent = card.Find("Options");
                        if (optParent != null && (roadsideOptionButtons == null || roadsideOptionButtons.Length < 3 || roadsideOptionButtons[0] == null))
                        {
                            roadsideOptionButtons = new Button[3];
                            roadsideOptionLabels = new TextMeshProUGUI[3];
                            roadsideOptionSubtitles = new TextMeshProUGUI[3];
                            for (int i = 0; i < 3; i++)
                            {
                                Transform opt = optParent.Find($"Option_{i}");
                                if (opt == null) continue;
                                roadsideOptionButtons[i] = opt.GetComponent<Button>();
                                roadsideOptionLabels[i] = opt.Find("Title")?.GetComponent<TextMeshProUGUI>();
                                roadsideOptionSubtitles[i] = opt.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
                            }
                        }
                    }
                }
                else
                {
                    GameObject evOverlay = new GameObject("RoadsideEventOverlay", typeof(RectTransform), typeof(Image));
                    evOverlay.transform.SetParent(rootCanvas.transform, false);
                    RectTransform ovRt = evOverlay.GetComponent<RectTransform>();
                    ovRt.anchorMin = Vector2.zero;
                    ovRt.anchorMax = Vector2.one;
                    ovRt.offsetMin = Vector2.zero;
                    ovRt.offsetMax = Vector2.zero;
                    Image scrim = evOverlay.GetComponent<Image>();
                    scrim.color = MedievalUITheme.ScrimDark;

                    GameObject cardGo = new GameObject("EventCard", typeof(RectTransform), typeof(Image));
                    cardGo.transform.SetParent(evOverlay.transform, false);
                    RectTransform cRt = cardGo.GetComponent<RectTransform>();
                    cRt.anchorMin = new Vector2(0.5f, 0.5f);
                    cRt.anchorMax = new Vector2(0.5f, 0.5f);
                    cRt.pivot = new Vector2(0.5f, 0.5f);
                    cRt.sizeDelta = new Vector2(780f, 440f);
                    Image cardImg = cardGo.GetComponent<Image>();
                    cardImg.sprite = panelSprite;
                    cardImg.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                    cardImg.pixelsPerUnitMultiplier = 0.65f;
                    cardImg.color = MedievalUITheme.ObsidianPanel;

                    GameObject titleGo = new GameObject("EventTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGo.transform.SetParent(cardGo.transform, false);
                    roadsideTitleTMP = titleGo.GetComponent<TextMeshProUGUI>();
                    if (fontBold != null) roadsideTitleTMP.font = fontBold;
                    roadsideTitleTMP.fontSize = 24f;
                    roadsideTitleTMP.fontStyle = FontStyles.Bold;
                    roadsideTitleTMP.color = MedievalUITheme.GoldSoft;
                    roadsideTitleTMP.alignment = TextAlignmentOptions.Center;
                    RectTransform tRt = roadsideTitleTMP.rectTransform;
                    tRt.anchorMin = new Vector2(0f, 1f);
                    tRt.anchorMax = new Vector2(1f, 1f);
                    tRt.offsetMin = new Vector2(24f, -64f);
                    tRt.offsetMax = new Vector2(-24f, -18f);

                    GameObject descGo = new GameObject("EventDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
                    descGo.transform.SetParent(cardGo.transform, false);
                    roadsideDescTMP = descGo.GetComponent<TextMeshProUGUI>();
                    if (fontBold != null) roadsideDescTMP.font = fontBold;
                    roadsideDescTMP.fontSize = 16.5f;
                    roadsideDescTMP.color = MedievalUITheme.IvoryText;
                    roadsideDescTMP.alignment = TextAlignmentOptions.Center;
                    roadsideDescTMP.textWrappingMode = TextWrappingModes.Normal;
                    RectTransform dRt = roadsideDescTMP.rectTransform;
                    dRt.anchorMin = new Vector2(0f, 1f);
                    dRt.anchorMax = new Vector2(1f, 1f);
                    dRt.offsetMin = new Vector2(28f, -124f);
                    dRt.offsetMax = new Vector2(-28f, -68f);

                    GameObject optList = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
                    optList.transform.SetParent(cardGo.transform, false);
                    RectTransform olRt = optList.GetComponent<RectTransform>();
                    olRt.anchorMin = Vector2.zero;
                    olRt.anchorMax = Vector2.one;
                    olRt.offsetMin = new Vector2(28f, 24f);
                    olRt.offsetMax = new Vector2(-28f, -138f);
                    VerticalLayoutGroup vlg = optList.GetComponent<VerticalLayoutGroup>();
                    vlg.spacing = 10f;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = true;

                    roadsideOptionButtons = new Button[3];
                    roadsideOptionLabels = new TextMeshProUGUI[3];
                    roadsideOptionSubtitles = new TextMeshProUGUI[3];
                    Color[] optColors = { MedievalUITheme.BtnPrimaryGold, MedievalUITheme.BtnDefend, MedievalUITheme.BtnNeutralSlate };

                    for (int i = 0; i < 3; i++)
                    {
                        GameObject bGo = new GameObject($"Option_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                        bGo.transform.SetParent(optList.transform, false);
                        Image bBg = bGo.GetComponent<Image>();
                        bBg.sprite = panelSprite;
                        bBg.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                        bBg.pixelsPerUnitMultiplier = 0.65f;
                        bBg.color = optColors[i];

                        Button btn = bGo.GetComponent<Button>();
                        btn.targetGraphic = bBg;

                        GameObject lGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                        lGo.transform.SetParent(bGo.transform, false);
                        TextMeshProUGUI lTmp = lGo.GetComponent<TextMeshProUGUI>();
                        if (fontBold != null) lTmp.font = fontBold;
                        lTmp.fontSize = 16.5f;
                        lTmp.fontStyle = FontStyles.Bold;
                        lTmp.color = MedievalUITheme.IvoryText;
                        lTmp.alignment = TextAlignmentOptions.MidlineLeft;
                        lTmp.raycastTarget = false;
                        lTmp.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                        lTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
                        lTmp.rectTransform.offsetMin = new Vector2(16f, 0f);
                        lTmp.rectTransform.offsetMax = new Vector2(-16f, -6f);

                        GameObject sGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                        sGo.transform.SetParent(bGo.transform, false);
                        TextMeshProUGUI sTmp = sGo.GetComponent<TextMeshProUGUI>();
                        if (fontBold != null) sTmp.font = fontBold;
                        sTmp.fontSize = 13.5f;
                        sTmp.color = MedievalUITheme.GoldSoft;
                        sTmp.alignment = TextAlignmentOptions.MidlineLeft;
                        sTmp.raycastTarget = false;
                        sTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
                        sTmp.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                        sTmp.rectTransform.offsetMin = new Vector2(16f, 6f);
                        sTmp.rectTransform.offsetMax = new Vector2(-16f, 0f);

                        roadsideOptionButtons[i] = btn;
                        roadsideOptionLabels[i] = lTmp;
                        roadsideOptionSubtitles[i] = sTmp;
                    }

                    evOverlay.SetActive(false);
                    roadsideEventRoot = evOverlay;
                }
            }
        }

        private static Sprite _fallbackBarSprite;
        private static Sprite GetWhiteBarSprite()
        {
            if (_fallbackBarSprite != null) return _fallbackBarSprite;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _fallbackBarSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
            return _fallbackBarSprite;
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

            string quizLine = total > 0
                ? $"- Câu hỏi đúng: <b>{correct}/{total}</b> ({pct}%)  |  Chuỗi cao nhất: <b>{battleSystem.BestQuizStreak}</b>"
                : "- Chế độ Quiz: <b>Tắt (Tự do)</b>  |  <i>Bật Quiz đạt ≥75% để mở khóa Sao thứ 3</i>";

            return $"{headline}\n\n" +
                   $"- Chiến trường: <b>{battleSystem.ActiveStage.title}</b> ({stageWaveStr}){starStr}\n" +
                   $"- Tổng hiệp: <b>{battleSystem.TurnCount}</b>  |  Sống sót: <b>{aliveCount}/{battleSystem.partyUnits.Count}</b>\n" +
                   $"{quizLine}\n" +
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
            TimeScaleController.Reset();
            SceneManager.LoadScene(sceneIndex);
        }

        private IEnumerator TransitionAndLoadSceneName(string sceneName)
        {
            _isTransitioning = true;
            if (fadeOverlay != null) yield return FadeRoutine(0f, 1f, 0.28f);
            TimeScaleController.Reset();
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
