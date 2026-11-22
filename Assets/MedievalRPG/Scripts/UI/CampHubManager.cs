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
        StartBattle = 2
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

        private readonly List<Unit> _previewHeroes = new List<Unit>();
        private bool _isTransitioning;

        public IReadOnlyList<Unit> PreviewHeroes => _previewHeroes;

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
            Time.timeScale = 1f;
            AudioManager audio = AudioManager.EnsureExists();
            audio.SetPausedDuck(false);
            audio.PlayMenuMusic();

            if (equipmentModal != null) equipmentModal.gameObject.SetActive(false);
            if (shopModal != null) shopModal.gameObject.SetActive(false);

            ForestRoadController road = FindAnyObjectByType<ForestRoadController>();
            if (road != null)
            {
                if (!road.HasRoadTiles) road.BuildOrRebuildRoadVisuals();
                road.SetMilestoneVisible(false);
            }

            SpawnHeroLineup();
            BuildCampWorldStations();
            WireButtons();

            PlayerEquipmentManager.EnsureLoaded().OnEquipmentChanged += RefreshCampSummary;
            PlayerCampProgressManager.EnsureLoaded().OnCampProgressChanged += RefreshCampSummary;
            RefreshCampSummary();

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
            SpawnCampHero(knightPrefab, new Vector3(-0.05f, -1.24f, 0f), 0);
            SpawnCampHero(magePrefab,   new Vector3(-1.35f, -1.04f, 0f), 1);
            SpawnCampHero(archerPrefab, new Vector3(-2.65f, -1.26f, 0f), 2);
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
            _previewHeroes.Add(u);

            if (partyCards != null && cardIndex < partyCards.Length && partyCards[cardIndex] != null)
            {
                // Giữ nguyên portrait đã dựng sẵn từ scene
                partyCards[cardIndex].Bind(u, null, null);
            }
        }

        private void BuildCampWorldStations()
        {
            CreateCampProp("CampChestStation",   CampStationAction.Equipment,   new Vector3(-4.25f, -0.92f, 0.05f),
                CreateChestPropSprite(),     "[I] KHO TRANG BỊ", MedievalUITheme.GoldSoft);
            CreateCampProp("CampShopStation",    CampStationAction.Shop,        new Vector3(1.65f,  -0.88f, 0.05f),
                CreateShopTentPropSprite(),  "[S] TIỆM THUỐC & BUFF", MedievalUITheme.SuccessGreen);
            CreateCampProp("CampBattlePortal",   CampStationAction.StartBattle, new Vector3(4.35f,  -0.96f, 0.05f),
                CreateBattleGateSprite(),    "RA CHIẾN TRƯỜNG >>", new Color(1f, 0.56f, 0.44f, 1f));
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
            cRt.sizeDelta = new Vector2(230f, 38f);

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
            tmp.fontSize = 15f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 11f;
            tmp.fontSizeMax = 15f;
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
                startBattleButton.onClick.AddListener(StartBattleExpedition);
            }
            if (backMenuButton != null)
            {
                backMenuButton.onClick.RemoveAllListeners();
                backMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
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
                EquipmentStatBonus total = equipMgr.GetTotalBonusesForClass(u.characterClass);
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
                activeBuffsBadgeTMP.text = $"Bùa trận tới: {campMgr.GetActiveBuffsShortSummary()}";
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
                openEquipmentLabelTMP.text = $"MẶC TRANG BỊ [I] ({equipMgr.OwnedCount})";
            }
        }

        public void OpenEquipment()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            if (shopModal != null && shopModal.IsOpen) shopModal.Hide();
            if (equipmentModal != null) equipmentModal.Show(RefreshCampSummary);
        }

        public void OpenShop()
        {
            if (_isTransitioning) return;
            AudioManager.Instance?.PlayButtonClick();
            if (equipmentModal != null && equipmentModal.IsOpen) equipmentModal.Hide();
            if (shopModal != null) shopModal.Show(RefreshCampSummary);
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
            if (_isTransitioning) return;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    if (equipmentModal != null && equipmentModal.IsOpen) { equipmentModal.Hide(); return; }
                    if (shopModal != null && shopModal.IsOpen) { shopModal.Hide(); return; }
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
                if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    && (equipmentModal == null || !equipmentModal.IsOpen)
                    && (shopModal == null || !shopModal.IsOpen))
                {
                    StartBattleExpedition();
                    return;
                }
            }

            if ((equipmentModal != null && equipmentModal.IsOpen) || (shopModal != null && shopModal.IsOpen))
                return;

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
                        if (st.stationAction == CampStationAction.Equipment) OpenEquipment();
                        else if (st.stationAction == CampStationAction.Shop) OpenShop();
                        else if (st.stationAction == CampStationAction.StartBattle) StartBattleExpedition();
                        break;
                    }
                }
            }
        }

        private IEnumerator LoadSceneWithFade(string targetScene)
        {
            _isTransitioning = true;
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
    }
}
