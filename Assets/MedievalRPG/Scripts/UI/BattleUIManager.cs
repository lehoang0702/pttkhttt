using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Quiz;

namespace MedievalRPG.UI
{
    /// <summary>
    /// [Quản Lý Giao Diện Trận Đấu - BattleUIManager]
    /// Điều khiển toàn bộ giao diện uGUI + TextMeshPro hiện đại:
    /// 1. Thẻ trạng thái 3 Anh hùng (góc trên trái) & Thẻ Quái vật (góc trên phải).
    /// 2. Thanh máu mini & mũi tên lượt trực tiếp trên nhân vật (WorldUnitHUD) + Chữ nổi sát thương.
    /// 3. Bảng diễn biến trận đấu & xem trước kỹ năng + 5 nút hành động có phím tắt [1-5].
    /// 4. Bảng Câu hỏi Trắc nghiệm Giáo dục (QuizModalUI) hiển thị kết quả & giải thích đầy đủ.
    /// 5. Bảng Tạm dừng / Cài đặt (Esc) và Màn hình Chiến thắng / Thất bại kèm thống kê.
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        public static BattleUIManager Instance { get; private set; }

        [Header("Tham chiếu Hệ Thống Chiến Đấu")]
        public BattleSystem battleSystem;

        [Header("Giao diện uGUI cơ bản (Tương thích ngược)")]
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

        [Header("Cấu hình Giao Diện OnGUI Nhanh")]
        public bool enableInteractiveOnGUI = false;

        [Header("Giao diện uGUI Nâng Cao (Canvas + TMP)")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private TextMeshProUGUI turnBannerTMP;
        [SerializeField] private TextMeshProUGUI turnSubTMP;
        [SerializeField] private TextMeshProUGUI quizStatsTMP;
        [SerializeField] private TextMeshProUGUI dialogueTMP;
        [SerializeField] private TextMeshProUGUI activeHeroBannerTMP;
        [SerializeField] private TextMeshProUGUI skillPreviewTMP;
        [SerializeField] private UnityEngine.UI.Image activeHeroPortrait;

        [Header("Thẻ Nhân Vật & Trên Chiến Trường")]
        [SerializeField] private PartyMemberCardUI[] partyCards;
        [SerializeField] private EnemyStatusCardUI enemyCard;
        [SerializeField] private WorldUnitHUD[] worldUnitHUDs;
        [SerializeField] private RectTransform worldHudLayer;
        [SerializeField] private FloatingTextManager floatingTextManager;

        [Header("Bảng Câu Hỏi & Tạm Dừng")]
        [SerializeField] private QuizModalUI quizModal;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private UnityEngine.UI.Slider pauseVolumeSlider;
        [SerializeField] private TextMeshProUGUI pauseVolumeValueTMP;
        [SerializeField] private UnityEngine.UI.Toggle pauseQuizToggle;
        [SerializeField] private UnityEngine.UI.Toggle pauseFastModeToggle;
        [SerializeField] private UnityEngine.UI.Button settingsOpenButton;

        [Header("5 Nút Hành Động (1: Đánh, 2: Skill1, 3: Skill2, 4: Hồi, 5: Thủ)")]
        [SerializeField] private UnityEngine.UI.Button[] actionButtons = new UnityEngine.UI.Button[5];
        [SerializeField] private TextMeshProUGUI[] actionTitleTMPs = new TextMeshProUGUI[5];
        [SerializeField] private TextMeshProUGUI[] actionCostTMPs = new TextMeshProUGUI[5];
        [SerializeField] private UnityEngine.UI.Image[] actionIconImages = new UnityEngine.UI.Image[5];
        [SerializeField] private UIButtonFeedback[] actionFeedbacks = new UIButtonFeedback[5];
        [SerializeField] private GameObject actionLockedOverlay;
        [SerializeField] private TextMeshProUGUI actionLockedTMP;

        [Header("Tóm Tắt Kết Quả Trận Đấu")]
        [SerializeField] private TextMeshProUGUI victorySummaryTMP;
        [SerializeField] private TextMeshProUGUI gameOverSummaryTMP;

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

        private bool _isTransitioning;
        private string _lastDialogue = "";

        public bool HasCanvasUI => rootCanvas != null && (partyCards != null && partyCards.Length > 0);

        public void ConfigureCanvasUI(
            Canvas canvas, CanvasGroup fade,
            TextMeshProUGUI turnBanner, TextMeshProUGUI turnSub, TextMeshProUGUI quizStats,
            TextMeshProUGUI diagTMP, TextMeshProUGUI heroBanner, TextMeshProUGUI skillPreview,
            UnityEngine.UI.Image heroPortrait,
            PartyMemberCardUI[] pCards, EnemyStatusCardUI eCard,
            WorldUnitHUD[] wHuds, RectTransform wHudLayer, FloatingTextManager ftManager,
            QuizModalUI qModal, GameObject pPanel,
            UnityEngine.UI.Slider pVolSlider, TextMeshProUGUI pVolVal,
            UnityEngine.UI.Toggle pQuizToggle, UnityEngine.UI.Toggle pFastToggle,
            UnityEngine.UI.Button settingsBtn,
            UnityEngine.UI.Button[] actBtns, TextMeshProUGUI[] actTitles, TextMeshProUGUI[] actCosts,
            UnityEngine.UI.Image[] actIcons, UIButtonFeedback[] actFeedbacks,
            GameObject lockedOverlay, TextMeshProUGUI lockedText,
            TextMeshProUGUI winSummary, TextMeshProUGUI loseSummary,
            Sprite kPort, Sprite mPort, Sprite aPort, Sprite gPort, Sprite sPort, Sprite dkPort,
            Sprite icSword, Sprite icMagic, Sprite icArrow, Sprite icHeart, Sprite icShield)
        {
            rootCanvas = canvas;
            fadeOverlay = fade;
            turnBannerTMP = turnBanner;
            turnSubTMP = turnSub;
            quizStatsTMP = quizStats;
            dialogueTMP = diagTMP;
            activeHeroBannerTMP = heroBanner;
            skillPreviewTMP = skillPreview;
            activeHeroPortrait = heroPortrait;
            partyCards = pCards;
            enemyCard = eCard;
            worldUnitHUDs = wHuds;
            worldHudLayer = wHudLayer;
            floatingTextManager = ftManager;
            quizModal = qModal;
            pausePanel = pPanel;
            pauseVolumeSlider = pVolSlider;
            pauseVolumeValueTMP = pVolVal;
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

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

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
            WirePauseControls();

            if (victoryScreen != null) victoryScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (quizModal != null) quizModal.Hide();
            if (pausePanel != null) pausePanel.SetActive(false);

            if (fadeOverlay != null)
            {
                StartCoroutine(FadeRoutine(1f, 0f, 0.30f));
            }
        }

        private void SubscribeEvents()
        {
            if (battleSystem == null) return;
            battleSystem.OnBattleInitialized += HandleBattleInitialized;
            battleSystem.OnStateChanged += HandleStateChanged;
            battleSystem.OnHeroTurnChanged += HandleHeroTurnChanged;
            battleSystem.OnDialogueChanged += HandleDialogueChanged;
            battleSystem.OnQuizStarted += HandleQuizStarted;
            battleSystem.OnQuizResultShown += HandleQuizResultShown;
            battleSystem.OnQuizEnded += HandleQuizEnded;
            battleSystem.OnDamageDealt += HandleDamageDealt;
            battleSystem.OnUnitHealed += HandleUnitHealed;
            battleSystem.OnUnitStatusPop += HandleUnitStatusPop;
            battleSystem.OnBattleEnded += HandleBattleEnded;
        }

        private void UnsubscribeEvents()
        {
            if (battleSystem == null) return;
            battleSystem.OnBattleInitialized -= HandleBattleInitialized;
            battleSystem.OnStateChanged -= HandleStateChanged;
            battleSystem.OnHeroTurnChanged -= HandleHeroTurnChanged;
            battleSystem.OnDialogueChanged -= HandleDialogueChanged;
            battleSystem.OnQuizStarted -= HandleQuizStarted;
            battleSystem.OnQuizResultShown -= HandleQuizResultShown;
            battleSystem.OnQuizEnded -= HandleQuizEnded;
            battleSystem.OnDamageDealt -= HandleDamageDealt;
            battleSystem.OnUnitHealed -= HandleUnitHealed;
            battleSystem.OnUnitStatusPop -= HandleUnitStatusPop;
            battleSystem.OnBattleEnded -= HandleBattleEnded;

            if (battleSystem.partyUnits != null)
            {
                foreach (var u in battleSystem.partyUnits)
                {
                    if (u != null) u.OnStatsChanged -= HandleUnitStatsChanged;
                }
            }
            if (battleSystem.enemyUnit != null)
            {
                battleSystem.enemyUnit.OnStatsChanged -= HandleUnitStatsChanged;
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

        private void WirePauseControls()
        {
            if (settingsOpenButton != null)
            {
                settingsOpenButton.onClick.RemoveAllListeners();
                settingsOpenButton.onClick.AddListener(TogglePauseMenu);
            }

            if (pauseVolumeSlider != null)
            {
                pauseVolumeSlider.minValue = 0f;
                pauseVolumeSlider.maxValue = 1f;
                pauseVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
                UpdateVolumeLabel(pauseVolumeSlider.value);
                pauseVolumeSlider.onValueChanged.RemoveAllListeners();
                pauseVolumeSlider.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat("MasterVolume", val);
                    PlayerPrefs.Save();
                    AudioListener.volume = val;
                    UpdateVolumeLabel(val);
                });
            }

            if (pauseQuizToggle != null)
            {
                bool quizOn = PlayerPrefs.GetInt("QuizEnabled", 1) == 1;
                pauseQuizToggle.isOn = quizOn;
                pauseQuizToggle.onValueChanged.RemoveAllListeners();
                pauseQuizToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetInt("QuizEnabled", isOn ? 1 : 0);
                    PlayerPrefs.Save();
                    if (battleSystem != null) battleSystem.ApplySavedSettings();
                    RefreshActionButtons();
                });
            }

            if (pauseFastModeToggle != null)
            {
                bool fastOn = PlayerPrefs.GetFloat("BattleSpeed", 1f) > 1.1f;
                pauseFastModeToggle.isOn = fastOn;
                pauseFastModeToggle.onValueChanged.RemoveAllListeners();
                pauseFastModeToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetFloat("BattleSpeed", isOn ? 1.5f : 1f);
                    PlayerPrefs.Save();
                    if (battleSystem != null) battleSystem.ApplySavedSettings();
                });
            }
        }

        private void UpdateVolumeLabel(float vol)
        {
            if (pauseVolumeValueTMP != null)
            {
                pauseVolumeValueTMP.text = $"{Mathf.RoundToInt(vol * 100f)}%";
            }
        }

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
                }

                if (worldUnitHUDs != null && i < worldUnitHUDs.Length && worldUnitHUDs[i] != null)
                {
                    worldUnitHUDs[i].Bind(hero, worldHudLayer, GetWorldOffsetForUnit(hero));
                }
            }

            Unit enemy = battleSystem.enemyUnit;
            if (enemy != null)
            {
                enemy.OnStatsChanged -= HandleUnitStatsChanged;
                enemy.OnStatsChanged += HandleUnitStatsChanged;

                if (enemyCard != null)
                {
                    enemyCard.Bind(enemy, GetPortraitForUnit(enemy));
                }

                if (worldUnitHUDs != null && worldUnitHUDs.Length > 3 && worldUnitHUDs[3] != null)
                {
                    worldUnitHUDs[3].Bind(enemy, worldHudLayer, 0.72f);
                }
            }

            RefreshAllStatusDisplays();
            RefreshQuizStatsBadge();
        }

        private void HandleUnitStatsChanged(Unit unit)
        {
            RefreshAllStatusDisplays();
            RefreshActionButtons();
        }

        private void HandleStateChanged(BattleState newState)
        {
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
            _lastDialogue = message;
            if (dialogueTMP != null) dialogueTMP.text = message;
            if (dialogueBoxText != null) dialogueBoxText.text = message;
        }

        private void HandleQuizStarted(QuizQuestion q, string actionContext)
        {
            if (quizModal != null)
            {
                quizModal.ShowQuestion(
                    q,
                    actionContext,
                    OnSubmitAnswerClicked,
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
            if (quizModal != null)
            {
                quizModal.Hide();
            }
            else
            {
                HideQuizPrompt();
            }
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
            if (finalState == BattleState.WON) ShowVictory();
            else if (finalState == BattleState.LOST) ShowGameOver();
        }

        private void UpdateTurnBanner(BattleState st)
        {
            if (turnBannerTMP == null) return;

            int round = battleSystem != null ? battleSystem.TurnCount : 1;
            string heroName = (battleSystem != null && battleSystem.CurrentHero != null)
                ? MedievalUITheme.FormatHeroDisplayName(battleSystem.CurrentHero.unitName)
                : "Anh Hùng";

            switch (st)
            {
                case BattleState.START:
                    turnBannerTMP.text = "CHUẨN BỊ CHIẾN ĐẤU";
                    if (turnSubTMP != null) turnSubTMP.text = $"Hiệp {round}  •  Sắp xếp đội hình";
                    break;
                case BattleState.PLAYERTURN:
                    turnBannerTMP.text = $"LƯỢT CỦA {heroName.ToUpperInvariant()}";
                    if (turnSubTMP != null) turnSubTMP.text = $"Hiệp {round}  •  Chọn hành động [1-5]";
                    break;
                case BattleState.WAITING_FOR_QUIZ:
                    turnBannerTMP.text = "THỬ THÁCH TRI THỨC";
                    if (turnSubTMP != null) turnSubTMP.text = $"Hiệp {round}  •  Trả lời đúng để xuất chiêu";
                    break;
                case BattleState.ENEMYTURN:
                    turnBannerTMP.text = "LƯỢT QUÁI VẬT";
                    if (turnSubTMP != null) turnSubTMP.text = $"Hiệp {round}  •  Kẻ địch đang phản công";
                    break;
                case BattleState.WON:
                    turnBannerTMP.text = "CHIẾN THẮNG!";
                    if (turnSubTMP != null) turnSubTMP.text = $"Hoàn thành sau {round} hiệp";
                    break;
                case BattleState.LOST:
                    turnBannerTMP.text = "THẤT BẠI";
                    if (turnSubTMP != null) turnSubTMP.text = $"Gục ngã ở hiệp {round}";
                    break;
            }
        }

        private void RefreshQuizStatsBadge()
        {
            if (quizStatsTMP == null || battleSystem == null) return;
            quizStatsTMP.text = $"Đúng: {battleSystem.CorrectAnswersCount}/{battleSystem.TotalQuestionsAnswered}  •  Chuỗi: {battleSystem.CurrentQuizStreak}";
        }

        private void RefreshAllStatusDisplays()
        {
            if (battleSystem == null) return;
            Unit activeHero = battleSystem.state == BattleState.PLAYERTURN || battleSystem.state == BattleState.WAITING_FOR_QUIZ
                ? battleSystem.CurrentHero
                : null;

            for (int i = 0; i < battleSystem.partyUnits.Count; i++)
            {
                Unit u = battleSystem.partyUnits[i];
                bool isTurn = u != null && u == activeHero;

                if (partyCards != null && i < partyCards.Length && partyCards[i] != null)
                {
                    partyCards[i].RefreshImmediate(isTurn);
                }
                if (worldUnitHUDs != null && i < worldUnitHUDs.Length && worldUnitHUDs[i] != null)
                {
                    worldUnitHUDs[i].Refresh(isTurn);
                }
            }

            bool isEnemyTurn = battleSystem.state == BattleState.ENEMYTURN;
            if (enemyCard != null) enemyCard.Refresh(isEnemyTurn);
            if (worldUnitHUDs != null && worldUnitHUDs.Length > 3 && worldUnitHUDs[3] != null)
            {
                worldUnitHUDs[3].Refresh(isEnemyTurn);
            }

            if (activeHeroBannerTMP != null)
            {
                if (activeHero != null)
                {
                    activeHeroBannerTMP.text = $"{MedievalUITheme.FormatHeroDisplayName(activeHero.unitName)}  •  ATK {activeHero.damage}  |  MAG {activeHero.magicDamage}  |  DEF {activeHero.EffectiveDefense}";
                }
                else if (battleSystem.state == BattleState.ENEMYTURN && battleSystem.enemyUnit != null)
                {
                    activeHeroBannerTMP.text = $"Đang tới lượt {battleSystem.enemyUnit.unitName}";
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
                          && hero.currentHP > 0;

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

            // 0: Đánh thường (dùng skill[0] nếu có để lấy tên đẹp, mặc định Tấn Công)
            SkillData basicSkill = (hero.skills != null && hero.skills.Count > 0) ? hero.skills[0] : null;
            string atkTitle = basicSkill != null ? basicSkill.skillName : "Tấn Công";
            string atkDesc = basicSkill != null
                ? $"{basicSkill.description} (Sát thương cơ bản ~{hero.damage} + thưởng {quizTag})"
                : $"Tấn công vật lý cơ bản gây ~{hero.damage} sát thương.";
            SetActionButton(0, true, canAct, atkTitle, $"Miễn phí • {quizTag}", GetAttackIconForHero(hero), MedievalUITheme.BtnAttack, atkDesc);

            // 1: Skill 1
            bool hasS1 = hero.skills != null && hero.skills.Count > 1 && hero.skills[1] != null;
            SkillData s1 = hasS1 ? hero.skills[1] : null;
            bool s1CanCast = canAct && hasS1 && hero.currentMP >= s1.manaCost;
            string s1Cost = hasS1 ? $"{s1.manaCost} MP • {quizTag}" : "Không có";
            string s1Desc = hasS1 ? $"{s1.description} (Tiêu hao: {s1.manaCost} MP)" : "Kỹ năng chưa mở khóa.";
            SetActionButton(1, hasS1, s1CanCast, hasS1 ? s1.skillName : "Kỹ Năng 1", s1Cost, magicIcon, MedievalUITheme.BtnSkill, s1Desc);

            // 2: Skill 2
            bool hasS2 = hero.skills != null && hero.skills.Count > 2 && hero.skills[2] != null;
            SkillData s2 = hasS2 ? hero.skills[2] : null;
            bool s2CanCast = canAct && hasS2 && hero.currentMP >= s2.manaCost;
            Sprite s2Icon = hasS2 && s2.baseHealAmount > 0 ? heartIcon : (hasS2 && s2.defenseBonus > 0 ? shieldIcon : magicIcon);
            string s2Cost = hasS2 ? $"{s2.manaCost} MP • {quizTag}" : "Không có";
            string s2Desc = hasS2 ? $"{s2.description} (Tiêu hao: {s2.manaCost} MP)" : "Kỹ năng chưa mở khóa.";
            SetActionButton(2, hasS2, s2CanCast, hasS2 ? s2.skillName : "Kỹ Năng 2", s2Cost, s2Icon, MedievalUITheme.BtnSkill, s2Desc);

            // 3: Hồi phục cơ bản
            SetActionButton(3, true, canAct, "Hồi Phục", $"Thảo dược • {quizTag}", heartIcon, MedievalUITheme.BtnHeal,
                "Dùng thảo dược hồi +25 HP (+ thưởng Quiz) cho đồng đội mất nhiều máu nhất.");

            // 4: Phòng thủ
            SetActionButton(4, true, canAct, "Phòng Thủ", "Miễn phí • +8 MP", shieldIcon, MedievalUITheme.BtnDefend,
                "Giảm 50% sát thương nhận vào ở lượt quái vật và hồi ngay +8 MP (không cần trả lời Quiz).");
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

        private void HandleActionHoverChanged(UIButtonFeedback feedback, bool isHovering)
        {
            if (skillPreviewTMP == null) return;
            if (isHovering && !string.IsNullOrEmpty(feedback.HoverDescription))
            {
                skillPreviewTMP.text = $"<color=#F2CA6B><b>{feedback.HoverTitle}:</b></color> {feedback.HoverDescription}";
            }
            else
            {
                skillPreviewTMP.text = "Di chuột vào nút hành động để xem chi tiết hiệu ứng & chi phí MP.";
            }
        }

        private void TriggerActionByIndex(int index)
        {
            if (battleSystem == null) return;
            switch (index)
            {
                case 0: battleSystem.OnAttackButton(); break;
                case 1: battleSystem.OnSkill1Button(); break;
                case 2: battleSystem.OnSkill2Button(); break;
                case 3: battleSystem.OnHealButton(); break;
                case 4: battleSystem.OnDefendButton(); break;
            }
        }

        private Sprite GetPortraitForUnit(Unit u)
        {
            if (u == null) return knightPortrait;
            string n = u.unitName ?? "";
            if (n.Contains("Dark") || n.Contains("Boss")) return darkKnightPortrait != null ? darkKnightPortrait : knightPortrait;
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

        private Sprite GetAttackIconForHero(Unit u)
        {
            if (u == null) return swordIcon;
            if (u.characterClass == CharacterClassType.Mage) return magicIcon;
            if (u.characterClass == CharacterClassType.Archer) return arrowIcon;
            return swordIcon;
        }

        private float GetWorldOffsetForUnit(Unit u)
        {
            if (u == null) return 0.9f;
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
                if (battleSystem != null && (battleSystem.state == BattleState.WON || battleSystem.state == BattleState.LOST))
                {
                    return;
                }
                if (battleSystem != null && battleSystem.state == BattleState.WAITING_FOR_QUIZ)
                {
                    return;
                }
                TogglePauseMenu();
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

        /// <summary>
        /// Mở bảng câu hỏi trắc nghiệm khi nhân vật chuẩn bị tấn công (tương thích ngược)
        /// </summary>
        public void ShowQuizPrompt(QuizQuestion q)
        {
            if (quizModal != null)
            {
                quizModal.ShowQuestion(q, "Hãy trả lời câu hỏi để kích hoạt sức mạnh!", OnSubmitAnswerClicked, () => battleSystem?.SkipQuizFeedbackWait());
                return;
            }

            if (quizPanel != null)
            {
                quizPanel.SetActive(true);
                if (questionCategoryText != null) questionCategoryText.text = $"[Chủ đề: {q.category}]";
                if (questionContentText != null) questionContentText.text = q.questionText;

                for (int i = 0; i < 4; i++)
                {
                    if (answerButtonTexts != null && i < answerButtonTexts.Length && answerButtonTexts[i] != null)
                    {
                        char prefix = (char)('A' + i);
                        answerButtonTexts[i].text = $"{prefix}. {q.answers[i]}";
                    }
                }
            }

            if (actionPanel != null) actionPanel.SetActive(false);
        }

        /// <summary>
        /// Đóng bảng câu hỏi sau khi có kết quả
        /// </summary>
        public void HideQuizPrompt()
        {
            if (quizModal != null) quizModal.Hide();
            if (quizPanel != null) quizPanel.SetActive(false);
            if (actionPanel != null) actionPanel.SetActive(true);
        }

        /// <summary>
        /// Xử lý khi người chơi bấm chọn đáp án A, B, C hoặc D
        /// </summary>
        public void OnSubmitAnswerClicked(int answerIndex)
        {
            if (battleSystem != null)
            {
                battleSystem.ProcessQuizAnswer(answerIndex);
            }
            if (quizModal == null)
            {
                HideQuizPrompt();
            }
        }

        /// <summary>
        /// Hiển thị màn hình chiến thắng
        /// </summary>
        public void ShowVictory()
        {
            if (victoryScreen != null) victoryScreen.SetActive(true);
            if (victorySummaryTMP != null) victorySummaryTMP.text = BuildEndSummaryText(true);
        }

        /// <summary>
        /// Hiển thị màn hình thất bại
        /// </summary>
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
            foreach (var u in battleSystem.partyUnits)
            {
                if (u != null && u.currentHP > 0) aliveCount++;
            }

            string headline = won
                ? "Đội hình anh hùng đã đánh bại quái vật hầm ngục!"
                : "Hãy ôn tập thêm kiến thức và quay lại phục thù!";

            return $"{headline}\n\n" +
                   $"• Số hiệp chiến đấu: <b>{battleSystem.TurnCount}</b>\n" +
                   $"• Câu hỏi trả lời đúng: <b>{correct}/{total}</b> ({pct}%)\n" +
                   $"• Chuỗi trả lời đúng dài nhất: <b>{battleSystem.BestQuizStreak}</b>\n" +
                   $"• Thành viên còn sống: <b>{aliveCount}/{battleSystem.partyUnits.Count}</b>";
        }

        /// <summary>
        /// Chơi lại trận đấu từ đầu
        /// </summary>
        public void RestartBattle()
        {
            if (_isTransitioning) return;
            if (battleSystem != null) battleSystem.SetPaused(false);
            StartCoroutine(TransitionAndLoadSceneIndex(SceneManager.GetActiveScene().buildIndex));
        }

        /// <summary>
        /// Trở về Menu chính
        /// </summary>
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
