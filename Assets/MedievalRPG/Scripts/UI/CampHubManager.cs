using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Visuals;

namespace MedievalRPG.UI
{
    public enum CampStationAction
    {
        Equipment = 0,
        Shop = 1,
        StartBattle = 2,
        MistakeNotebook = 3
    }

    public class CampWorldStation : MonoBehaviour
    {
        public CampStationAction stationAction;
    }

    /// <summary>
    /// Quản lý Sảnh Chờ Tiền Đồn (CampHub.unity):
    /// Nơi người chơi ngắm 3 anh hùng đứng trên con đường rừng, xem & mặc trang bị ([I]),
    /// mua thuốc + bùa hỗ trợ bằng Vàng tại quầy Thương Nhân ([S]), và xuất phát vào trận chiến 6 đợt!
    /// </summary>
    public class CampHubManager : MonoBehaviour
    {
        [Header("Scene Chuyển Tiếp")]
        public string battleSceneName = "MedievalBattleDemo";
        public string mainMenuSceneName = "MainMenu";

        [Header("Prefabs Anh Hùng & Trạm Đứng")]
        public GameObject knightPrefab;
        public GameObject magePrefab;
        public GameObject archerPrefab;
        public TMP_FontAsset worldLabelFont;

        [Header("UI Tham Chiếu")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private PartyMemberCardUI[] partyCards = new PartyMemberCardUI[3];
        [SerializeField] private TextMeshProUGUI goldHeaderTMP;
        [SerializeField] private TextMeshProUGUI activeBuffsBadgeTMP;
        [SerializeField] private TextMeshProUGUI stockSummaryTMP;
        [SerializeField] private TextMeshProUGUI campDialogueTMP;
        [SerializeField] private Button openEquipmentButton;
        [SerializeField] private TextMeshProUGUI openEquipmentLabelTMP;
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button autoEquipButton;
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Button backMenuButton;
        [SerializeField] private EquipmentModalUI equipmentModal;
        [SerializeField] private ShopModalUI shopModal;

        [Header("V2: Modals & Điều Khiển Mở Rộng")]
        [SerializeField] private StageSelectModalUI stageSelectModal;
        [SerializeField] private MistakeNotebookModalUI mistakeNotebookModal;
        [SerializeField] private AchievementsModalUI achievementsModal;
        [SerializeField] private GameObject settingsModal;
        [SerializeField] private Button openNotebookButton;
        [SerializeField] private TextMeshProUGUI notebookLabelTMP;
        [SerializeField] private Button openAchievementsButton;
        [SerializeField] private TextMeshProUGUI achievementsLabelTMP;
        [SerializeField] private Button quizFilterButton;
        [SerializeField] private TextMeshProUGUI quizFilterLabelTMP;
        [SerializeField] private Button openSettingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button resetSaveButton;
        [SerializeField] private TextMeshProUGUI resetSaveLabelTMP;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TextMeshProUGUI musicValueTMP;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI sfxValueTMP;
        [SerializeField] private Toggle quizToggle;
        [SerializeField] private Toggle fastCombatToggle;
        [SerializeField] private Toggle quizTimerToggle;
        [SerializeField] private Toggle cameraShakeToggle;
        [SerializeField] private Toggle fullscreenToggle;

        private struct StationSpot
        {
            public CampStationAction action;
            public float worldX;
            public string displayLabel;
        }

        private readonly List<Unit> _previewHeroes = new List<Unit>();
        private readonly List<StationSpot> _stationSpots = new List<StationSpot>();
        private readonly float[] _partyFormationOffsets = { 0f, -1.25f, -2.45f };
        private readonly float[] _partyLaneY = { -1.36f, -1.08f, -1.27f };
        private float _campPartyX = -0.1f;
        private bool _isWalkingOnRoad;
        private CampStationAction? _nearestStation;
        private Transform _merchantNpcTransform;
        private Vector3 _merchantBasePos;
        private SpriteRenderer _campfireRenderer;
        private SpriteRenderer _campfireGlowRenderer;
        private Sprite[] _campfireFrames;
        private bool _isTransitioning;

        public IReadOnlyList<Unit> PreviewHeroes => _previewHeroes;
        public float CampPartyX => _campPartyX;

        public void ConfigureV2Features(
            StageSelectModalUI stageModal,
            MistakeNotebookModalUI notebookModal,
            AchievementsModalUI achModal,
            GameObject settModal,
            Button notebookBtn, TextMeshProUGUI notebookLbl,
            Button achBtn, TextMeshProUGUI achLbl,
            Button filterBtn, TextMeshProUGUI filterLbl,
            Button settBtn, Button closeSettBtn,
            Button resetBtn, TextMeshProUGUI resetLbl,
            Slider musicSl, TextMeshProUGUI musicVal,
            Slider sfxSl, TextMeshProUGUI sfxVal,
            Toggle qToggle, Toggle fastToggle,
            Toggle timerToggle, Toggle shakeToggle, Toggle fullToggle)
        {
            stageSelectModal = stageModal;
            mistakeNotebookModal = notebookModal;
            achievementsModal = achModal;
            settingsModal = settModal;
            openNotebookButton = notebookBtn;
            notebookLabelTMP = notebookLbl;
            openAchievementsButton = achBtn;
            achievementsLabelTMP = achLbl;
            quizFilterButton = filterBtn;
            quizFilterLabelTMP = filterLbl;
            openSettingsButton = settBtn;
            closeSettingsButton = closeSettBtn;
            resetSaveButton = resetBtn;
            resetSaveLabelTMP = resetLbl;
            musicSlider = musicSl;
            musicValueTMP = musicVal;
            sfxSlider = sfxSl;
            sfxValueTMP = sfxVal;
            quizToggle = qToggle;
            fastCombatToggle = fastToggle;
            quizTimerToggle = timerToggle;
            cameraShakeToggle = shakeToggle;
            fullscreenToggle = fullToggle;
        }

        public void Configure(
            GameObject kPrefab, GameObject mPrefab, GameObject aPrefab, TMP_FontAsset lblFont,
            CanvasGroup fade, PartyMemberCardUI[] cards,
            TextMeshProUGUI goldTxt, TextMeshProUGUI buffsTxt, TextMeshProUGUI stockTxt, TextMeshProUGUI diagTxt,
            Button eqBtn, TextMeshProUGUI eqLbl, Button shBtn, Button autoEqBtn, Button battleBtn, Button menuBtn,
            EquipmentModalUI eqModal, ShopModalUI shModal)
        {
            knightPrefab = kPrefab;
            magePrefab = mPrefab;
            archerPrefab = aPrefab;
            worldLabelFont = lblFont;
            fadeOverlay = fade;
            partyCards = cards;
            goldHeaderTMP = goldTxt;
            activeBuffsBadgeTMP = buffsTxt;
            stockSummaryTMP = stockTxt;
            campDialogueTMP = diagTxt;
            openEquipmentButton = eqBtn;
            openEquipmentLabelTMP = eqLbl;
            openShopButton = shBtn;
            autoEquipButton = autoEqBtn;
            startBattleButton = battleBtn;
            backMenuButton = menuBtn;
            equipmentModal = eqModal;
            shopModal = shModal;
        }

        private void Start()
        {
            TimeScaleController.Reset();
            AudioManager audio = AudioManager.EnsureExists();
            audio.SetPausedDuck(false);
            audio.PlayMenuMusic();

            if (equipmentModal != null) equipmentModal.gameObject.SetActive(false);
            if (shopModal != null) shopModal.gameObject.SetActive(false);
            if (stageSelectModal != null) stageSelectModal.gameObject.SetActive(false);
            if (mistakeNotebookModal != null) mistakeNotebookModal.gameObject.SetActive(false);
            if (achievementsModal != null) achievementsModal.gameObject.SetActive(false);
            if (settingsModal != null) settingsModal.SetActive(false);

            ForestRoadController road = FindAnyObjectByType<ForestRoadController>();
            if (road != null)
            {
                if (!road.HasRoadTiles) road.BuildOrRebuildRoadVisuals();
                road.SetMilestoneVisible(false);
                road.SetEnvironmentTheme(EnvironmentThemeType.MistyForest);
            }

            SpawnHeroLineup();
            BuildCampWorldStations();
            WireButtons();
            WireV2Controls();

            PlayerEquipmentManager.EnsureLoaded().OnEquipmentChanged += RefreshCampSummary;
            PlayerCampProgressManager.EnsureLoaded().OnCampProgressChanged += RefreshCampSummary;
            RefreshCampSummary();
            UpdateNearestStationPrompt(true);

            if (fadeOverlay != null)
            {
                StartCoroutine(FadeRoutine(1f, 0f, 0.28f));
            }
        }

        private void OnDestroy()
        {
            PlayerEquipmentManager.Instance.OnEquipmentChanged -= RefreshCampSummary;
            PlayerCampProgressManager.Instance.OnCampProgressChanged -= RefreshCampSummary;
        }

        private void SpawnHeroLineup()
        {
            _previewHeroes.Clear();
            _campPartyX = -0.1f;
            SpawnCampHero(knightPrefab, new Vector3(_campPartyX + _partyFormationOffsets[0], _partyLaneY[0], 0f), 0);
            SpawnCampHero(magePrefab,   new Vector3(_campPartyX + _partyFormationOffsets[1], _partyLaneY[1], 0f), 1);
            SpawnCampHero(archerPrefab, new Vector3(_campPartyX + _partyFormationOffsets[2], _partyLaneY[2], 0f), 2);
        }

        private void SpawnCampHero(GameObject prefab, Vector3 pos, int cardIndex)
        {
            if (prefab == null) return;
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            Unit u = go.GetComponent<Unit>();
            if (u == null) return;
            u.isEnemy = false;
            u.pixelAnimator?.SetHomePosition(pos);
            u.pixelAnimator?.SetSortingOrder(5);
            u.pixelAnimator?.SetFacingRight(true);
            _previewHeroes.Add(u);

            if (partyCards != null && cardIndex < partyCards.Length && partyCards[cardIndex] != null)
            {
                partyCards[cardIndex].Bind(u, null, null);
                partyCards[cardIndex].OnCardClicked -= HandlePartyCardClicked;
                partyCards[cardIndex].OnCardClicked += HandlePartyCardClicked;
            }
        }

        private void HandlePartyCardClicked(Unit hero)
        {
            if (_isTransitioning || hero == null) return;
            CloseAllModals();
            AudioManager.Instance?.PlayButtonClick();
            if (equipmentModal != null) equipmentModal.Show(RefreshCampSummary, hero.characterClass);
        }

        private void BuildCampWorldStations()
        {
            _stationSpots.Clear();

            RegisterCampStation("CampChestStation", CampStationAction.Equipment, new Vector3(-4.50f, -0.92f, 0.05f),
                CreateChestPropSprite(), "[I] KHO TRANG BỊ", MedievalUITheme.GoldSoft);
            RegisterCampStation("CampNotebookStation", CampStationAction.MistakeNotebook, new Vector3(-2.20f, -0.90f, 0.05f),
                CreateNotebookDeskSprite(), "[N] SỔ ÔN TẬP", new Color(0.55f, 0.85f, 1f, 1f));
            RegisterCampStation("CampShopStation", CampStationAction.Shop, new Vector3(1.80f, -0.88f, 0.05f),
                CreateShopTentPropSprite(), "[S] TIỆM THUỐC & BUFF", MedievalUITheme.SuccessGreen);
            RegisterCampStation("CampBattlePortal", CampStationAction.StartBattle, new Vector3(4.45f, -0.96f, 0.05f),
                CreateBattleGateSprite(), "CHỌN MÀN CHIẾN ĐẤU >>", new Color(1f, 0.56f, 0.44f, 1f));

            SpawnCampMerchantAndFire();
        }

        private void RegisterCampStation(string objName, CampStationAction action, Vector3 pos, Sprite sprite, string labelText, Color accent)
        {
            CreateCampProp(objName, action, pos, sprite, labelText, accent);
            _stationSpots.Add(new StationSpot
            {
                action = action,
                worldX = pos.x,
                displayLabel = labelText
            });
        }

        private void SpawnCampMerchantAndFire()
        {
            // Thương nhân đứng cạnh lều thuốc
            GameObject merchantGo = new GameObject("CampMerchantNPC");
            _merchantBasePos = new Vector3(1.12f, -0.94f, 0.04f);
            merchantGo.transform.position = _merchantBasePos;
            _merchantNpcTransform = merchantGo.transform;
            SpriteRenderer mSr = merchantGo.AddComponent<SpriteRenderer>();
            mSr.sprite = CreateMerchantNpcSprite();
            mSr.sortingOrder = 3;

            // Lửa trại bập bùng ở trung tâm sảnh chờ
            _campfireFrames = new[] { CreateCampfireSprite(false), CreateCampfireSprite(true) };
            GameObject fireGo = new GameObject("CampfireProp");
            fireGo.transform.position = new Vector3(-0.78f, -0.78f, 0.04f);
            _campfireRenderer = fireGo.AddComponent<SpriteRenderer>();
            _campfireRenderer.sprite = _campfireFrames[0];
            _campfireRenderer.sortingOrder = 3;

            GameObject glowGo = new GameObject("CampfireGlow");
            glowGo.transform.SetParent(fireGo.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.25f, 0.01f);
            _campfireGlowRenderer = glowGo.AddComponent<SpriteRenderer>();
            _campfireGlowRenderer.sprite = CreateWarmGlowSprite();
            _campfireGlowRenderer.sortingOrder = 1;
            _campfireGlowRenderer.color = new Color(1f, 0.62f, 0.22f, 0.36f);
        }

        private void CreateCampProp(string objName, CampStationAction action, Vector3 pos, Sprite sprite, string labelText, Color accent)
        {
            GameObject go = new GameObject(objName);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.45f, 1.45f);
            col.offset = new Vector2(0f, 0.35f);

            CampWorldStation st = go.AddComponent<CampWorldStation>();
            st.stationAction = action;

            // Nhãn chữ nổi phía trên công trình sảnh chờ
            GameObject lblGo = new GameObject("WorldLabel");
            lblGo.transform.SetParent(go.transform, false);
            lblGo.transform.localPosition = new Vector3(0f, 1.05f, -0.05f);
            lblGo.transform.localScale = Vector3.one * 0.0085f;

            Canvas c = lblGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 15;
            RectTransform cRt = lblGo.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(248f, 40f);

            Image bg = lblGo.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.92f);
            bg.raycastTarget = false;
            Outline outline = lblGo.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(lblGo.transform, false);
            TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
            if (worldLabelFont != null) tmp.font = worldLabelFont;
            else if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = labelText;
            tmp.fontSize = 16f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12.5f;
            tmp.fontSizeMax = 16.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = accent;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;

            RectTransform tRt = tmp.rectTransform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(6f, 2f);
            tRt.offsetMax = new Vector2(-6f, -2f);
        }

        private void WireButtons()
        {
            if (openEquipmentButton != null)
            {
                openEquipmentButton.onClick.RemoveAllListeners();
                openEquipmentButton.onClick.AddListener(OpenEquipment);
            }
            if (openShopButton != null)
            {
                openShopButton.onClick.RemoveAllListeners();
                openShopButton.onClick.AddListener(OpenShop);
            }
            if (autoEquipButton != null)
            {
                autoEquipButton.onClick.RemoveAllListeners();
                autoEquipButton.onClick.AddListener(() =>
                {
                    int changed = PlayerEquipmentManager.EnsureLoaded().AutoEquipBestForAllClasses();
                    if (changed > 0)
                    {
                        AudioManager.Instance?.PlayLootPickupSfx();
                        if (campDialogueTMP != null) campDialogueTMP.text = $"Đã tự động trang bị <b>{changed}</b> món mạnh nhất cho cả 3 Class!";
                    }
                    else
                    {
                        AudioManager.Instance?.PlayButtonClick();
                        if (campDialogueTMP != null) campDialogueTMP.text = "Đội hình hiện đang mặc các trang bị tối ưu nhất trong Kho!";
                    }
                    RefreshCampSummary();
                });
            }
            if (startBattleButton != null)
            {
                startBattleButton.onClick.RemoveAllListeners();
                startBattleButton.onClick.AddListener(OpenStageSelect);
            }
            if (backMenuButton != null)
            {
                backMenuButton.onClick.RemoveAllListeners();
                backMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        private void WireV2Controls()
        {
            if (openNotebookButton != null)
            {
                openNotebookButton.onClick.RemoveAllListeners();
                openNotebookButton.onClick.AddListener(OpenMistakeNotebook);
            }

            if (openAchievementsButton != null)
            {
                openAchievementsButton.onClick.RemoveAllListeners();
                openAchievementsButton.onClick.AddListener(OpenAchievements);
            }

            if (quizFilterButton != null)
            {
                quizFilterButton.onClick.RemoveAllListeners();
                quizFilterButton.onClick.AddListener(() =>
                {
                    PlayerCampProgressManager.EnsureLoaded().CycleQuizFilter();
                    AudioManager.Instance?.PlayButtonClick();
                    RefreshCampSummary();
                });
            }

            if (openSettingsButton != null)
            {
                openSettingsButton.onClick.RemoveAllListeners();
                openSettingsButton.onClick.AddListener(OpenSettings);
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.RemoveAllListeners();
                closeSettingsButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    if (settingsModal != null) settingsModal.SetActive(false);
                });
            }

            GameSettingsUIHelper.BindSettingsControls(
                musicSlider, musicValueTMP,
                sfxSlider, sfxValueTMP,
                quizToggle, fastCombatToggle,
                quizTimerToggle, cameraShakeToggle, fullscreenToggle);

            GameSettingsUIHelper.WireResetSaveButton(
                this, resetSaveButton, resetSaveLabelTMP, () =>
                {
                    RefreshCampSummary();
                    if (campDialogueTMP != null) campDialogueTMP.text = "Dữ liệu lưu đã được đặt lại về mặc định!";
                });
        }

        public void RefreshCampSummary()
        {
            PlayerEquipmentManager equipMgr = PlayerEquipmentManager.EnsureLoaded();
            PlayerCampProgressManager campMgr = PlayerCampProgressManager.EnsureLoaded();
            EquipmentStatBonus campBuff = campMgr.GetActiveCampBonusTotal();

            for (int i = 0; i < _previewHeroes.Count; i++)
            {
                Unit u = _previewHeroes[i];
                if (u == null) continue;
                u.unitLevel = campMgr.GetHeroProgress(u.characterClass).level;
                EquipmentStatBonus total = equipMgr.GetTotalBonusesForClass(u.characterClass);
                total.Add(campMgr.GetLevelBonusForClass(u.characterClass));
                total.Add(campBuff);
                u.ApplyEquipmentBonuses(total);
            }

            if (partyCards != null)
            {
                for (int i = 0; i < partyCards.Length; i++)
                {
                    if (partyCards[i] != null) partyCards[i].RefreshImmediate(false, false);
                }
            }

            if (goldHeaderTMP != null)
            {
                goldHeaderTMP.text = $"VÀNG: {campMgr.Gold}";
            }
            if (activeBuffsBadgeTMP != null)
            {
                StageDefinition stDef = StageCatalog.GetStage(campMgr.SelectedStage);
                activeBuffsBadgeTMP.text = $"Màn: {stDef.shortName} | Bùa: {campMgr.GetActiveBuffsShortSummary()}";
            }
            if (stockSummaryTMP != null)
            {
                int hp = campMgr.GetConsumableCount(ConsumableItemType.HealthPotion);
                int mp = campMgr.GetConsumableCount(ConsumableItemType.ManaElixir);
                int ph = campMgr.GetConsumableCount(ConsumableItemType.PhoenixFeather);
                int wr = campMgr.GetConsumableCount(ConsumableItemType.WarElixir);
                int ir = campMgr.GetConsumableCount(ConsumableItemType.IronTonic);
                stockSummaryTMP.text = $"Túi: Máu x{hp} | MP x{mp} | Phượng x{ph} | Dame x{wr} | Giáp x{ir}";
            }
            if (openEquipmentLabelTMP != null)
            {
                openEquipmentLabelTMP.text = $"KHO & RÈN [I] ({equipMgr.OwnedCount})";
            }
            if (notebookLabelTMP != null)
            {
                notebookLabelTMP.text = $"SỔ ÔN TẬP [N] ({campMgr.MistakeCount})";
            }
            if (achievementsLabelTMP != null)
            {
                int claimable = campMgr.ClaimableAchievementCount;
                achievementsLabelTMP.text = claimable > 0 ? $"THÀNH TỰU ({claimable} MỚI)" : "THÀNH TỰU";
            }
            if (quizFilterLabelTMP != null)
            {
                quizFilterLabelTMP.text = $"QUIZ: {PlayerCampProgressManager.GetQuizFilterDisplayName(campMgr.SelectedQuizFilter)}";
            }
        }

        private void CloseAllModals()
        {
            if (equipmentModal != null && equipmentModal.IsOpen) equipmentModal.Hide();
            if (shopModal != null && shopModal.IsOpen) shopModal.Hide();
            if (stageSelectModal != null && stageSelectModal.IsOpen) stageSelectModal.Hide();
            if (mistakeNotebookModal != null && mistakeNotebookModal.IsOpen) mistakeNotebookModal.Hide();
            if (achievementsModal != null && achievementsModal.IsOpen) achievementsModal.Hide();
            if (settingsModal != null && settingsModal.activeSelf) settingsModal.SetActive(false);
        }

        private bool IsAnyModalOpen()
        {
            return (equipmentModal != null && equipmentModal.IsOpen)
                   || (shopModal != null && shopModal.IsOpen)
                   || (stageSelectModal != null && stageSelectModal.IsOpen)
                   || (mistakeNotebookModal != null && mistakeNotebookModal.IsOpen)
                   || (achievementsModal != null && achievementsModal.IsOpen)
                   || (settingsModal != null && settingsModal.activeSelf);
        }

        public void OpenEquipment()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            CloseAllModals();
            if (equipmentModal != null) equipmentModal.Show(RefreshCampSummary);
        }

        public void OpenShop()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            CloseAllModals();
            if (shopModal != null) shopModal.Show(RefreshCampSummary);
        }

        public void OpenStageSelect()
        {
            if (_isTransitioning) return;
            if (stageSelectModal == null)
            {
                StartBattleExpedition();
                return;
            }
            AudioManager.Instance?.PlayButtonClick();
            CloseAllModals();
            stageSelectModal.Show(StartBattleExpedition, RefreshCampSummary);
        }

        public void OpenMistakeNotebook()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            CloseAllModals();
            if (mistakeNotebookModal != null) mistakeNotebookModal.Show(RefreshCampSummary);
        }

        public void OpenAchievements()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            CloseAllModals();
            if (achievementsModal != null) achievementsModal.Show(RefreshCampSummary);
        }

        public void OpenSettings()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            CloseAllModals();
            if (settingsModal != null)
            {
                GameSettingsUIHelper.BindSettingsControls(
                    musicSlider, musicValueTMP,
                    sfxSlider, sfxValueTMP,
                    quizToggle, fastCombatToggle,
                    quizTimerToggle, cameraShakeToggle, fullscreenToggle);
                settingsModal.SetActive(true);
            }
        }

        public void StartBattleExpedition()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            StartCoroutine(LoadSceneWithFade(battleSceneName));
        }

        public void ReturnToMainMenu()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            StartCoroutine(LoadSceneWithFade(mainMenuSceneName));
        }

        private void Update()
        {
            UpdateCampAmbientProps();

            if (_isTransitioning) return;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    if (IsAnyModalOpen()) { CloseAllModals(); return; }
                    ReturnToMainMenu();
                    return;
                }
                if (kb.iKey.wasPressedThisFrame)
                {
                    if (equipmentModal != null && equipmentModal.IsOpen) equipmentModal.Hide();
                    else OpenEquipment();
                    return;
                }
                if (kb.sKey.wasPressedThisFrame)
                {
                    if (shopModal != null && shopModal.IsOpen) shopModal.Hide();
                    else OpenShop();
                    return;
                }
                if (kb.nKey.wasPressedThisFrame)
                {
                    if (mistakeNotebookModal != null && mistakeNotebookModal.IsOpen) mistakeNotebookModal.Hide();
                    else OpenMistakeNotebook();
                    return;
                }
                if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) && !IsAnyModalOpen())
                {
                    OpenStageSelect();
                    return;
                }
                if (kb.eKey.wasPressedThisFrame && !IsAnyModalOpen() && _nearestStation.HasValue)
                {
                    TriggerStationAction(_nearestStation.Value);
                    return;
                }
            }

            if (IsAnyModalOpen())
            {
                StopCampWalking();
                return;
            }

            UpdatePartyRoadWalking(kb);

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Camera.main != null)
            {
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!overUI)
                {
                    Vector2 screenPos = mouse.position.ReadValue();
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
                    Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(worldPos.x, worldPos.y));
                    for (int i = 0; i < hits.Length; i++)
                    {
                        CampWorldStation st = hits[i].GetComponent<CampWorldStation>();
                        if (st == null) continue;
                        TriggerStationAction(st.stationAction);
                        break;
                    }
                }
            }
        }

        private void TriggerStationAction(CampStationAction action)
        {
            switch (action)
            {
                case CampStationAction.Equipment: OpenEquipment(); break;
                case CampStationAction.Shop: OpenShop(); break;
                case CampStationAction.MistakeNotebook: OpenMistakeNotebook(); break;
                case CampStationAction.StartBattle: OpenStageSelect(); break;
            }
        }

        private void UpdateCampAmbientProps()
        {
            if (_merchantNpcTransform != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 2.2f) * 0.03f;
                _merchantNpcTransform.position = _merchantBasePos + new Vector3(0f, bob, 0f);
            }

            if (_campfireRenderer != null && _campfireFrames != null && _campfireFrames.Length == 2)
            {
                int frame = Mathf.FloorToInt(Time.unscaledTime * 7f) % 2;
                _campfireRenderer.sprite = _campfireFrames[frame];
            }

            if (_campfireGlowRenderer != null)
            {
                float pulse = 0.28f + 0.11f * Mathf.Sin(Time.unscaledTime * 6.5f);
                _campfireGlowRenderer.color = new Color(1f, 0.62f, 0.22f, pulse);
            }
        }

        private void UpdatePartyRoadWalking(Keyboard kb)
        {
            float dir = 0f;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir += 1f;
            }

            if (Mathf.Abs(dir) > 0.01f)
            {
                const float walkSpeed = 3.6f;
                _campPartyX = Mathf.Clamp(_campPartyX + dir * walkSpeed * Time.deltaTime, -4.45f, 4.45f);

                for (int i = 0; i < _previewHeroes.Count; i++)
                {
                    Unit u = _previewHeroes[i];
                    if (u == null) continue;
                    Vector3 pos = new Vector3(_campPartyX + _partyFormationOffsets[i], _partyLaneY[i], 0f);
                    u.transform.position = pos;
                    if (u.pixelAnimator != null)
                    {
                        u.pixelAnimator.SetHomePosition(pos);
                        u.pixelAnimator.SetFacingRight(dir > 0f);
                        if (!_isWalkingOnRoad) u.pixelAnimator.StartMarchRun(1.0f);
                    }
                }
                _isWalkingOnRoad = true;
                UpdateNearestStationPrompt(false);
            }
            else if (_isWalkingOnRoad)
            {
                StopCampWalking();
            }
        }

        private void StopCampWalking()
        {
            if (!_isWalkingOnRoad) return;
            _isWalkingOnRoad = false;
            for (int i = 0; i < _previewHeroes.Count; i++)
            {
                Unit u = _previewHeroes[i];
                if (u == null || u.pixelAnimator == null) continue;
                u.pixelAnimator.StopMarchRun();
                u.pixelAnimator.SetFacingRight(true);
            }
        }

        private void UpdateNearestStationPrompt(bool forceUpdate)
        {
            CampStationAction? found = null;
            string foundLabel = "";
            float bestDist = 1.25f;

            for (int i = 0; i < _stationSpots.Count; i++)
            {
                float d = Mathf.Abs(_campPartyX - _stationSpots[i].worldX);
                if (d <= bestDist)
                {
                    bestDist = d;
                    found = _stationSpots[i].action;
                    foundLabel = _stationSpots[i].displayLabel;
                }
            }

            if (!forceUpdate && found == _nearestStation) return;
            _nearestStation = found;

            if (campDialogueTMP == null) return;
            campDialogueTMP.text = found.HasValue
                ? $"Gần trạm <b>{foundLabel}</b>  -  Bấm <b>[E]</b> hoặc Click để tương tác!"
                : "Dùng <b>[A/D]</b> dạo sảnh chờ, <b>[E]</b> mở trạm gần nhất, <b>[I]</b> Kho, <b>[S]</b> Tiệm, <b>[N]</b> Sổ ôn tập.";
        }

        private IEnumerator LoadSceneWithFade(string targetScene)
        {
            _isTransitioning = true;
            StopCampWalking();
            if (fadeOverlay != null) yield return FadeRoutine(0f, 1f, 0.25f);
            SceneManager.LoadScene(targetScene);
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

        // =====================================================================
        // SPRITE CÔNG TRÌNH SẢNH CHỜ VẼ THỦ TỤC (PIXEL ART)
        // =====================================================================
        private static Sprite CreateChestPropSprite()
        {
            const int w = 28, h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(0.08f, 0.08f, 0.11f, 1f);
            Color woodDark = new Color(0.42f, 0.25f, 0.14f, 1f);
            Color woodLight = new Color(0.60f, 0.38f, 0.21f, 1f);
            Color gold = new Color(0.95f, 0.77f, 0.28f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < 3 || x > 24 || y < 2 || y > 18) { tex.SetPixel(x, y, clear); continue; }
                    bool border = x == 3 || x == 24 || y == 2 || y == 18;
                    if (border) tex.SetPixel(x, y, outline);
                    else if (y == 11 || x == 7 || x == 20 || (x >= 12 && x <= 15 && y >= 9 && y <= 13)) tex.SetPixel(x, y, gold);
                    else tex.SetPixel(x, y, y > 11 ? woodLight : woodDark);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 28f);
        }

        private static Sprite CreateShopTentPropSprite()
        {
            const int w = 44, h = 40;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color wood = new Color(0.38f, 0.23f, 0.13f, 1f);
            Color counter = new Color(0.54f, 0.36f, 0.20f, 1f);
            Color roofA = new Color(0.16f, 0.56f, 0.36f, 1f);
            Color roofB = new Color(0.94f, 0.85f, 0.58f, 1f);
            Color potionRed = new Color(0.88f, 0.28f, 0.28f, 1f);
            Color potionBlue = new Color(0.26f, 0.62f, 0.96f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                    // Quầy gỗ
                    if (x >= 5 && x <= 38 && y >= 2 && y <= 13)
                    {
                        tex.SetPixel(x, y, y >= 11 ? counter : wood);
                    }
                    // Trụ lều
                    else if ((x == 6 || x == 7 || x == 36 || x == 37) && y >= 13 && y <= 26)
                    {
                        tex.SetPixel(x, y, wood);
                    }
                    // Mái bạt kẻ sọc
                    else if (y >= 26 && y <= 36 && x >= 3 + (y - 26) / 2 && x <= 40 - (y - 26) / 2)
                    {
                        tex.SetPixel(x, y, ((x / 5) % 2 == 0) ? roofA : roofB);
                    }
                    // Hai lọ thuốc trên quầy
                    if (y >= 14 && y <= 18 && x >= 13 && x <= 16) tex.SetPixel(x, y, potionRed);
                    if (y >= 14 && y <= 18 && x >= 27 && x <= 30) tex.SetPixel(x, y, potionBlue);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 28f);
        }

        private static Sprite CreateBattleGateSprite()
        {
            const int w = 32, h = 42;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color wood = new Color(0.35f, 0.22f, 0.12f, 1f);
            Color stone = new Color(0.45f, 0.48f, 0.54f, 1f);
            Color gold = new Color(0.92f, 0.72f, 0.28f, 1f);
            Color banner = new Color(0.76f, 0.22f, 0.22f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                    if ((x >= 4 && x <= 8 && y >= 2 && y <= 34) || (x >= 23 && x <= 27 && y >= 2 && y <= 34))
                    {
                        tex.SetPixel(x, y, stone);
                    }
                    else if (y >= 32 && y <= 37 && x >= 2 && x <= 29)
                    {
                        tex.SetPixel(x, y, y == 37 ? gold : wood);
                    }
                    else if (y >= 18 && y <= 31 && x >= 11 && x <= 20)
                    {
                        tex.SetPixel(x, y, banner);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 28f);
        }

        private static Sprite CreateNotebookDeskSprite()
        {
            const int w = 28, h = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color wood = new Color(0.42f, 0.26f, 0.14f, 1f);
            Color paper = new Color(0.95f, 0.90f, 0.76f, 1f);
            Color ink = new Color(0.24f, 0.45f, 0.76f, 1f);
            Color ribbon = new Color(0.84f, 0.24f, 0.24f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                    if ((x == 6 || x == 7 || x == 20 || x == 21) && y >= 2 && y <= 14) tex.SetPixel(x, y, wood);
                    else if (x >= 4 && x <= 23 && y >= 13 && y <= 16) tex.SetPixel(x, y, wood);
                    else if (x >= 6 && x <= 21 && y >= 17 && y <= 25)
                    {
                        if (x == 13 || x == 14) tex.SetPixel(x, y, ribbon);
                        else if (y == 20 || y == 22 || y == 24) tex.SetPixel(x, y, ink);
                        else tex.SetPixel(x, y, paper);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 28f);
        }

        private static Sprite CreateMerchantNpcSprite()
        {
            const int w = 24, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color robe = new Color(0.18f, 0.46f, 0.34f, 1f);
            Color hood = new Color(0.12f, 0.34f, 0.25f, 1f);
            Color skin = new Color(0.92f, 0.76f, 0.62f, 1f);
            Color gold = new Color(0.94f, 0.78f, 0.28f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                    if (x >= 7 && x <= 16 && y >= 2 && y <= 16) tex.SetPixel(x, y, robe);
                    if (x >= 7 && x <= 16 && y == 10) tex.SetPixel(x, y, gold);
                    if (x >= 8 && x <= 15 && y >= 17 && y <= 23) tex.SetPixel(x, y, skin);
                    if (x >= 7 && x <= 16 && y >= 22 && y <= 27) tex.SetPixel(x, y, hood);
                    if ((x == 10 || x == 13) && y == 20) tex.SetPixel(x, y, new Color(0.10f, 0.10f, 0.14f, 1f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.06f), 28f);
        }

        private static Sprite CreateCampfireSprite(bool altFrame)
        {
            const int w = 22, h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color log = new Color(0.38f, 0.21f, 0.11f, 1f);
            Color flameOuter = new Color(0.96f, 0.44f, 0.12f, 1f);
            Color flameInner = new Color(1.00f, 0.86f, 0.28f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);
                    if (y >= 2 && y <= 6 && x >= 4 && x <= 17) tex.SetPixel(x, y, log);
                    int shift = altFrame ? 1 : 0;
                    if (y >= 6 && y <= 16 + shift && Mathf.Abs(x - 10) <= Mathf.Max(0, (17 - y) / 2))
                    {
                        tex.SetPixel(x, y, flameOuter);
                    }
                    if (y >= 7 && y <= 13 + shift && Mathf.Abs(x - 10) <= Mathf.Max(0, (14 - y) / 2))
                    {
                        tex.SetPixel(x, y, flameInner);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 28f);
        }

        private static Sprite CreateWarmGlowSprite()
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2(15.5f, 15.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / 15.5f;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 0.72f, 0.28f, a * a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
