using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using MedievalRPG.Audio;
using MedievalRPG.Data;
using MedievalRPG.Quiz;
using MedievalRPG.UI;
using MedievalRPG.Visuals;

namespace MedievalRPG.Combat
{
    public enum BattleState
    {
        START,
        PLAYERTURN,
        WAITING_FOR_QUIZ,
        ENEMYTURN,
        WON,
        LOST
    }

    public enum PendingActionType
    {
        Attack,
        Skill,
        Heal
    }

    /// <summary>
    /// Bộ điều khiển chiến đấu theo lượt trung tâm:
    /// - Hỗ trợ nhiều quái vật đồng thời & nhiều Wave chiến đấu (Goblin -> Skeleton -> Dark Knight Boss).
    /// - Hàng đợi lượt đánh theo Speed, Cooldown kỹ năng, Kháng Vật Lý / Phép, Hiệu ứng trạng thái (Stun/Burn/Taunt).
    /// - Cơ chế Boss Phase 2 (Khiên Tri Thức yêu cầu trả lời đúng Quiz để phá khiên) & Túi Vật Phẩm giới hạn.
    /// - Kết hợp chặt chẽ với QuizManager & AudioManager.
    /// </summary>
    public class BattleSystem : MonoBehaviour
    {
        [Header("--- Cấu hình Giáo Dục ---")]
        public bool enableQuizSystem = true;

        [Header("--- Prefabs Nhân Vật & Quái Vật ---")]
        public GameObject knightPrefab;
        public GameObject magePrefab;
        public GameObject archerPrefab;
        public GameObject enemyPrefab;
        public GameObject skeletonPrefab;
        public GameObject skeletonArcherPrefab;
        public GameObject shadowMagePrefab;
        public GameObject bossPrefab;

        [Header("--- Vị Trí Đứng Trong Trận (Battle Stations) ---")]
        public Transform knightBattleStation;
        public Transform mageBattleStation;
        public Transform archerBattleStation;
        public Transform enemyBattleStation;
        public List<Transform> enemyBattleStations = new List<Transform>();

        [Header("--- Cấu hình Đợt Quái (Waves) ---")]
        public EncounterData encounterData;
        public List<BattleWave> defaultWaves = new List<BattleWave>();

        [Header("--- Danh Sách Đơn Vị Chiến Đấu ---")]
        public List<Unit> partyUnits = new List<Unit>();
        public List<Unit> enemyUnits = new List<Unit>();
        public Unit enemyUnit; // Giữ tham chiếu quái đang được chọn mục tiêu để tương thích UI

        [Header("--- Trạng Thái Trận Đấu ---")]
        public BattleState state;
        public bool enableLegacyOnGUI = false;
        public TMP_FontAsset lootLabelFont;
        public ForestRoadController forestRoad;

        private readonly List<Unit> _turnQueue = new List<Unit>();
        private readonly List<WorldLootDrop> _groundLootDrops = new List<WorldLootDrop>();
        private readonly List<LootDropResult> _sessionLootCollected = new List<LootDropResult>();
        private readonly HashSet<Unit> _lootDroppedUnits = new HashSet<Unit>();
        private Unit _activeUnit;
        private Unit _selectedEnemyTarget;
        private Unit _selectedAllyTarget;
        private bool _isActionInProgress;
        private int _currentWaveIndex;

        private PendingActionType _pendingAction = PendingActionType.Attack;
        private int _pendingSkillIndex;
        private QuizQuestion _activeQuestion;
        private bool _isQuizAnswerCorrect;
        private bool _quizAnswerSubmitted;
        private bool _skipQuizFeedback;
        private bool _isQuickMindActive;
        private float _battleSpeed = 1f;
        private List<BattleWave> _runtimeStageWaves;
        private readonly List<CharacterClassType> _leveledUpClasses = new List<CharacterClassType>();

        public BattleInventory Inventory { get; private set; } = new BattleInventory(3, 2, 1);

        // Thống kê trận đấu
        public int TurnCount { get; private set; } = 1;
        public int CurrentWaveNumber => _currentWaveIndex + 1;
        public int TotalWaves => ActiveStage.isEndless ? Mathf.Max(CurrentWaveNumber, 5) : GetActiveWaveList().Count;
        public int TotalQuestionsAnswered { get; private set; }
        public int CorrectAnswersCount { get; private set; }
        public int CurrentQuizStreak { get; private set; }
        public int BestQuizStreak { get; private set; }
        public int SessionGoldEarned { get; private set; }
        public int SessionExpEarned { get; private set; }
        public CampaignStageId ActiveStageId { get; private set; }
        public StageDefinition ActiveStage { get; private set; }
        public int LastEarnedStars { get; private set; }
        public int LastFirstClearGoldBonus { get; private set; }
        public bool LastUnlockedNewStage { get; private set; }
        public IReadOnlyList<CharacterClassType> LeveledUpClasses => _leveledUpClasses;
        public int TotalPlayerGold => PlayerCampProgressManager.EnsureLoaded().Gold;
        public bool IsActionInProgress => _isActionInProgress;
        public bool IsPaused { get; private set; }
        public string CurrentDialogue { get; private set; } = "";

        public Unit ActiveUnit => _activeUnit;
        public Unit CurrentHero => (_activeUnit != null && !_activeUnit.isEnemy) ? _activeUnit : null;
        public Unit SelectedEnemyTarget => _selectedEnemyTarget;
        public Unit SelectedAllyTarget => _selectedAllyTarget;
        public IReadOnlyList<Unit> TurnQueue => _turnQueue;
        public IReadOnlyList<LootDropResult> SessionLootCollected => _sessionLootCollected;
        public int PendingGroundLootCount => _groundLootDrops.Count;

        public float CurrentWaveDropChance
        {
            get
            {
                List<BattleWave> waves = GetActiveWaveList();
                float bonus = (_currentWaveIndex >= 0 && _currentWaveIndex < waves.Count) ? waves[_currentWaveIndex].dropChanceBonus : 0f;
                return LootSystem.CalculateEquipmentDropChance(_currentWaveIndex, Mathf.Max(1, waves.Count), IsCurrentWaveBoss(), bonus);
            }
        }

        public string CurrentWaveSubtitle
        {
            get
            {
                List<BattleWave> waves = GetActiveWaveList();
                if (_currentWaveIndex >= 0 && _currentWaveIndex < waves.Count && !string.IsNullOrEmpty(waves[_currentWaveIndex].waveSubtitle))
                {
                    return waves[_currentWaveIndex].waveSubtitle;
                }
                return "";
            }
        }

        // Sự kiện đồng bộ giao diện
        public event Action OnBattleInitialized;
        public event Action OnWaveChanged;
        public event Action<BattleState> OnStateChanged;
        public event Action<Unit> OnHeroTurnChanged;
        public event Action<IReadOnlyList<Unit>, Unit> OnTurnQueueChanged;
        public event Action<Unit, Unit> OnTargetsChanged;
        public event Action<string> OnDialogueChanged;
        public event Action<QuizQuestion, string> OnQuizStarted;
        public event Action<bool, int, int, int, string, float> OnQuizResultShown;
        public event Action OnQuizEnded;
        public event Action<Unit, int, bool> OnDamageDealt;
        public event Action<Unit, int> OnUnitHealed;
        public event Action<Unit, string, Color> OnUnitStatusPop;
        public event Action<BattleState> OnBattleEnded;
        public event Action<LootDropResult, WorldLootDrop> OnLootDropped;
        public event Action<LootDropResult, OwnedEquipmentInstance> OnLootPickedUp;
        public event Action<int> OnGroundLootCountChanged;
        public event Action<int> OnGoldChanged;

        private void Start()
        {
            AudioManager.EnsureExists();
            ApplySavedSettings();
            SetState(BattleState.START);
            StartCoroutine(SetupBattle());
        }

        private void OnDestroy()
        {
            PlayerEquipmentManager.Instance.OnEquipmentChanged -= ApplyPartyEquipment;
            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            if (IsPaused) return;

            Mouse mouse = Mouse.current;
            Keyboard kb = Keyboard.current;

            // Cho phép nhặt đồ dưới đất bất cứ lúc nào (trừ khi đang trả lời Quiz hoặc tạm dừng)
            if (state != BattleState.WAITING_FOR_QUIZ)
            {
                if (kb != null && kb.eKey.wasPressedThisFrame && _groundLootDrops.Count > 0)
                {
                    CollectAllGroundLoot();
                    return;
                }

                if (mouse != null && mouse.leftButton.wasPressedThisFrame && Camera.main != null)
                {
                    bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                    if (!pointerOverUI)
                    {
                        Vector2 screenPos = mouse.position.ReadValue();
                        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
                        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(worldPos.x, worldPos.y));

                        // Ưu tiên nhặt vật phẩm rơi trước khi chọn mục tiêu quái
                        for (int i = 0; i < hits.Length; i++)
                        {
                            WorldLootDrop drop = hits[i].GetComponent<WorldLootDrop>();
                            if (drop != null && !drop.IsCollected)
                            {
                                CollectLootDrop(drop);
                                return;
                            }
                        }

                        if (state == BattleState.PLAYERTURN && !_isActionInProgress)
                        {
                            for (int i = 0; i < hits.Length; i++)
                            {
                                Unit clicked = hits[i].GetComponent<Unit>();
                                if (clicked == null) continue;
                                if (clicked.isEnemy && clicked.IsAlive)
                                {
                                    SelectEnemyTarget(clicked);
                                    AudioManager.Instance?.PlayButtonHover();
                                    break;
                                }
                                if (!clicked.isEnemy)
                                {
                                    SelectAllyTarget(clicked);
                                    AudioManager.Instance?.PlayButtonHover();
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;

            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                CycleEnemyTarget();
                AudioManager.Instance?.PlayButtonHover();
            }
        }

        public void ApplySavedSettings()
        {
            AudioManager.EnsureExists().ApplyVolumes();
            enableQuizSystem = PlayerPrefs.GetInt("QuizEnabled", enableQuizSystem ? 1 : 0) == 1;
            _battleSpeed = PlayerPrefs.GetFloat("BattleSpeed", 1f);
            if (_battleSpeed < 0.5f) _battleSpeed = 1f;
            if (!IsPaused)
            {
                Time.timeScale = _battleSpeed;
            }
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : _battleSpeed;
            AudioManager.Instance?.SetPausedDuck(paused);
        }

        public void SkipQuizFeedbackWait()
        {
            _skipQuizFeedback = true;
        }

        private void SetState(BattleState newState)
        {
            state = newState;
            OnStateChanged?.Invoke(newState);
        }

        // =====================================================================
        // KHỞI TẠO ĐỘI HÌNH & ĐỢT QUÁI VẬT (WAVES)
        // =====================================================================
        private IEnumerator SetupBattle()
        {
            PlayerCampProgressManager campMgr = PlayerCampProgressManager.EnsureLoaded();
            campMgr.ApplyToBattleInventory(Inventory);
            SessionGoldEarned = 0;
            SessionExpEarned = 0;
            LastEarnedStars = 0;
            LastFirstClearGoldBonus = 0;
            LastUnlockedNewStage = false;
            _leveledUpClasses.Clear();

            ActiveStageId = campMgr.SelectedStage;
            ActiveStage = StageCatalog.GetStage(ActiveStageId);
            if (enemyPrefab != null)
            {
                _runtimeStageWaves = StageCatalog.BuildWavesForStage(
                    ActiveStageId, enemyPrefab, skeletonPrefab, skeletonArcherPrefab, shadowMagePrefab, bossPrefab);
            }
            else
            {
                _runtimeStageWaves = null;
            }

            TurnCount = 1;
            _currentWaveIndex = 0;
            _sessionLootCollected.Clear();
            _lootDroppedUnits.Clear();
            _groundLootDrops.Clear();

            PlayerEquipmentManager equipMgr = PlayerEquipmentManager.EnsureLoaded();
            equipMgr.OnEquipmentChanged -= ApplyPartyEquipment;
            equipMgr.OnEquipmentChanged += ApplyPartyEquipment;

            PixelCombatVFXManager.EnsureExists();

            if (forestRoad == null)
            {
                forestRoad = FindAnyObjectByType<ForestRoadController>();
            }
            if (forestRoad != null)
            {
                if (!forestRoad.HasRoadTiles) forestRoad.BuildOrRebuildRoadVisuals();
                forestRoad.SetEnvironmentTheme(ActiveStage.theme);
            }

            EnsureEnemyStationsExist();
            SpawnPartyIfEmpty();

            if (partyUnits.Count == 0)
            {
                Unit[] sceneUnits = FindObjectsByType<Unit>();
                foreach (Unit u in sceneUnits)
                {
                    if (u == null) continue;
                    if (u.isEnemy) enemyUnits.Add(u);
                    else partyUnits.Add(u);
                }
            }

            ApplyPartyEquipment();

            yield return SpawnWaveRoutine(_currentWaveIndex, animateEntrance: false);

            EnsureValidTargets();
            OnBattleInitialized?.Invoke();
            OnWaveChanged?.Invoke();
            OnGroundLootCountChanged?.Invoke(0);
            OnGoldChanged?.Invoke(campMgr.Gold);

            bool isBossWave = IsCurrentWaveBoss();
            forestRoad?.SetMilestone(1, Mathf.Max(1, TotalWaves), isBossWave);
            AudioManager.Instance?.PlayBattleMusic(isBossWave);
            int dropPct = Mathf.RoundToInt(CurrentWaveDropChance * 100f);
            string waveTag = ActiveStage.isEndless ? "Tầng 1 (Tháp Vô Tận)" : $"{ActiveStage.shortName} • Đợt 1/{TotalWaves}";
            SetDialogue($"{waveTag}: Tiến quân săn kho báu & luyện tiếng Anh {ActiveStage.quizFocusLabel} (Rớt đồ: <b>{dropPct}%</b>)!");

            yield return new WaitForSeconds(0.9f);
            StartNewRound();
        }

        public void ApplyPartyEquipment()
        {
            PlayerEquipmentManager equipMgr = PlayerEquipmentManager.EnsureLoaded();
            PlayerCampProgressManager campMgr = PlayerCampProgressManager.EnsureLoaded();
            EquipmentStatBonus campBuff = campMgr.GetActiveCampBonusTotal();
            for (int i = 0; i < partyUnits.Count; i++)
            {
                Unit hero = partyUnits[i];
                if (hero == null) continue;
                hero.unitLevel = campMgr.GetHeroProgress(hero.characterClass).level;
                EquipmentStatBonus bonus = equipMgr.GetTotalBonusesForClass(hero.characterClass);
                bonus.Add(campMgr.GetLevelBonusForClass(hero.characterClass));
                bonus.Add(campBuff);
                hero.ApplyEquipmentBonuses(bonus);
            }
        }

        private void EnsureEnemyStationsExist()
        {
            if (enemyBattleStations == null) enemyBattleStations = new List<Transform>();
            enemyBattleStations.RemoveAll(t => t == null);

            if (enemyBattleStations.Count == 0 && enemyBattleStation != null)
            {
                enemyBattleStations.Add(enemyBattleStation);
            }
        }

        private void SpawnPartyIfEmpty()
        {
            if (partyUnits.Count > 0) return;

            SpawnHero(knightPrefab, knightBattleStation);
            SpawnHero(magePrefab, mageBattleStation);
            SpawnHero(archerPrefab, archerBattleStation);
        }

        private void SpawnHero(GameObject prefab, Transform station)
        {
            if (prefab == null || station == null) return;
            GameObject go = Instantiate(prefab, station.position, Quaternion.identity);
            Unit unit = go.GetComponent<Unit>();
            if (unit != null)
            {
                unit.pixelAnimator?.SetHomePosition(station.position);
                unit.pixelAnimator?.SetSortingOrder(5);
                partyUnits.Add(unit);
            }
        }

        private List<BattleWave> GetActiveWaveList()
        {
            if (_runtimeStageWaves != null && _runtimeStageWaves.Count > 0)
            {
                return _runtimeStageWaves;
            }
            if (encounterData != null && encounterData.waves != null && encounterData.waves.Count > 0)
            {
                return encounterData.waves;
            }
            if (defaultWaves != null && defaultWaves.Count > 0)
            {
                return defaultWaves;
            }

            // Tự tạo bản đồ 6 đợt leo thang (Yếu -> Thành đàn -> Mạnh hơn -> Đàn mạnh -> Tinh Anh -> Boss)
            List<BattleWave> fallback = new List<BattleWave>();
            if (enemyPrefab != null)
            {
                fallback.Add(new BattleWave
                {
                    waveTitle = "Đợt 1: Trinh Sát Rừng Sâu",
                    waveSubtitle = "Quái yếu đơn lẻ",
                    isBossWave = false,
                    enemyStatMultiplier = 0.80f,
                    dropChanceBonus = 0f,
                    rarityLuckBonus = 0f,
                    enemyPrefabs = new List<GameObject> { enemyPrefab }
                });
                fallback.Add(new BattleWave
                {
                    waveTitle = "Đợt 2: Đàn Goblin Phục Kích",
                    waveSubtitle = "Quái kéo đến thành đàn (3 con)",
                    isBossWave = false,
                    enemyStatMultiplier = 0.90f,
                    dropChanceBonus = 0.02f,
                    rarityLuckBonus = 0.03f,
                    enemyPrefabs = new List<GameObject> { enemyPrefab, enemyPrefab, enemyPrefab }
                });
            }
            if (skeletonPrefab != null && enemyPrefab != null)
            {
                fallback.Add(new BattleWave
                {
                    waveTitle = "Đợt 3: Vệ Binh Xương Khô",
                    waveSubtitle = "Quái mạnh hơn • Giáp dày",
                    isBossWave = false,
                    enemyStatMultiplier = 1.00f,
                    dropChanceBonus = 0.05f,
                    rarityLuckBonus = 0.06f,
                    enemyPrefabs = new List<GameObject> { enemyPrefab, skeletonPrefab }
                });
                fallback.Add(new BattleWave
                {
                    waveTitle = "Đợt 4: Tiểu Đội Hầm Ngục",
                    waveSubtitle = "Đàn quái hạng nặng (3 con)",
                    isBossWave = false,
                    enemyStatMultiplier = 1.08f,
                    dropChanceBonus = 0.08f,
                    rarityLuckBonus = 0.10f,
                    enemyPrefabs = new List<GameObject> { skeletonPrefab, enemyPrefab, skeletonPrefab }
                });
                fallback.Add(new BattleWave
                {
                    waveTitle = "Đợt 5: Cận Vệ Tinh Anh",
                    waveSubtitle = "Tinh Anh áp sát Boss • Rớt đồ Sử Thi cao",
                    isBossWave = false,
                    enemyStatMultiplier = 1.25f,
                    dropChanceBonus = 0.12f,
                    rarityLuckBonus = 0.16f,
                    enemyPrefabs = new List<GameObject> { skeletonPrefab, skeletonPrefab }
                });
            }
            if (bossPrefab != null)
            {
                fallback.Add(new BattleWave
                {
                    waveTitle = "Đợt 6: Thủ Lĩnh Bóng Đêm",
                    waveSubtitle = "BOSS CUỐI MAP • Chắc chắn rớt 2 trang bị cao cấp",
                    isBossWave = true,
                    enemyStatMultiplier = 1.05f,
                    dropChanceBonus = 0f,
                    rarityLuckBonus = 0.15f,
                    enemyPrefabs = new List<GameObject> { bossPrefab }
                });
            }

            if (fallback.Count == 0 && enemyPrefab != null)
            {
                fallback.Add(new BattleWave
                {
                    waveTitle = "Đợt 1: Giao Tranh",
                    isBossWave = false,
                    enemyPrefabs = new List<GameObject> { enemyPrefab }
                });
            }

            defaultWaves = fallback;
            return defaultWaves;
        }

        private bool IsCurrentWaveBoss()
        {
            List<BattleWave> waves = GetActiveWaveList();
            if (_currentWaveIndex < 0 || _currentWaveIndex >= waves.Count) return false;
            if (waves[_currentWaveIndex].isBossWave) return true;
            foreach (Unit e in enemyUnits)
            {
                if (e != null && e.isBoss) return true;
            }
            return false;
        }

        private IEnumerator SpawnWaveRoutine(int waveIndex, bool animateEntrance)
        {
            // Dọn xác quái đợt trước
            for (int i = 0; i < enemyUnits.Count; i++)
            {
                if (enemyUnits[i] != null) Destroy(enemyUnits[i].gameObject);
            }
            enemyUnits.Clear();
            _lootDroppedUnits.Clear();

            List<BattleWave> waves = GetActiveWaveList();
            if (waveIndex < 0 || waveIndex >= waves.Count) yield break;

            BattleWave wave = waves[waveIndex];
            int count = wave.enemyPrefabs != null ? wave.enemyPrefabs.Count : 0;
            float statMult = wave.enemyStatMultiplier > 0.05f ? wave.enemyStatMultiplier : 1f;
            bool isEliteWave = !wave.isBossWave && statMult >= 1.20f;

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = wave.enemyPrefabs[i];
                if (prefab == null) continue;

                Vector3 stationPos = GetEnemyStationPosition(i, count);
                Vector3 spawnPos = animateEntrance ? stationPos + new Vector3(3.2f, 0f, 0f) : stationPos;

                GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
                Unit eUnit = go.GetComponent<Unit>();
                if (eUnit != null)
                {
                    eUnit.isEnemy = true;
                    if (Mathf.Abs(statMult - 1f) > 0.01f || isEliteWave)
                    {
                        eUnit.ApplyWaveScale(statMult, isEliteWave);
                    }
                    eUnit.PlanNextIntent(TurnCount);
                    eUnit.pixelAnimator?.SetSortingOrder(5);
                    if (animateEntrance && eUnit.pixelAnimator != null)
                    {
                        eUnit.pixelAnimator.PlayEntranceStep(spawnPos, stationPos);
                    }
                    else
                    {
                        eUnit.pixelAnimator?.SetHomePosition(stationPos);
                    }
                    enemyUnits.Add(eUnit);
                }
            }

            enemyUnit = enemyUnits.Count > 0 ? enemyUnits[0] : null;
            EnsureValidTargets();

            if (animateEntrance)
            {
                yield return new WaitForSeconds(0.55f);
            }
        }

        private Vector3 GetEnemyStationPosition(int index, int totalCount)
        {
            if (enemyBattleStations != null && index < enemyBattleStations.Count && enemyBattleStations[index] != null)
            {
                return enemyBattleStations[index].position;
            }

            Vector3 basePos = enemyBattleStation != null ? enemyBattleStation.position : new Vector3(1.85f, -1.24f, 0f);
            if (totalCount <= 1) return basePos;

            switch (index)
            {
                case 0:  return basePos + new Vector3(-0.35f, -0.14f, 0f);
                case 1:  return basePos + new Vector3(0.90f, 0.22f, 0f);
                default: return basePos + new Vector3(2.15f, -0.18f, 0f);
            }
        }

        // =====================================================================
        // TARGETING (CHỌN MỤC TIÊU)
        // =====================================================================
        public void SelectEnemyTarget(Unit target)
        {
            if (target == null || !target.isEnemy || !target.IsAlive) return;
            _selectedEnemyTarget = target;
            enemyUnit = target;
            RefreshTargetHighlights();
            OnTargetsChanged?.Invoke(_selectedEnemyTarget, _selectedAllyTarget);
        }

        public void CycleEnemyTarget()
        {
            List<Unit> alive = GetAliveEnemies();
            if (alive.Count == 0) return;
            int cur = alive.IndexOf(_selectedEnemyTarget);
            Unit next = alive[(cur + 1) % alive.Count];
            SelectEnemyTarget(next);
        }

        public void SelectAllyTarget(Unit ally)
        {
            if (ally == null || ally.isEnemy) return;
            _selectedAllyTarget = ally;
            RefreshTargetHighlights();
            OnTargetsChanged?.Invoke(_selectedEnemyTarget, _selectedAllyTarget);
        }

        private void EnsureValidTargets()
        {
            if (_selectedEnemyTarget == null || !_selectedEnemyTarget.IsAlive)
            {
                _selectedEnemyTarget = GetFirstAliveEnemy();
                enemyUnit = _selectedEnemyTarget;
            }

            if (_selectedAllyTarget == null || !partyUnits.Contains(_selectedAllyTarget))
            {
                _selectedAllyTarget = GetMostWoundedAliveAlly();
            }

            RefreshTargetHighlights();
            OnTargetsChanged?.Invoke(_selectedEnemyTarget, _selectedAllyTarget);
        }

        private void RefreshTargetHighlights()
        {
            foreach (Unit e in enemyUnits)
            {
                if (e != null && e.pixelAnimator != null)
                {
                    e.pixelAnimator.SetTargetHighlight(e == _selectedEnemyTarget && e.IsAlive);
                }
            }
        }

        private List<Unit> GetAliveEnemies() => enemyUnits.FindAll(e => e != null && e.IsAlive);
        private List<Unit> GetAliveHeroes() => partyUnits.FindAll(h => h != null && h.IsAlive);
        private Unit GetFirstAliveEnemy() => enemyUnits.Find(e => e != null && e.IsAlive);

        private Unit GetMostWoundedAliveAlly()
        {
            Unit best = null;
            float lowestPct = float.MaxValue;

            foreach (Unit ally in partyUnits)
            {
                if (ally == null || !ally.IsAlive) continue;
                float pct = (float)ally.currentHP / Mathf.Max(1, ally.maxHP);
                if (pct < lowestPct)
                {
                    lowestPct = pct;
                    best = ally;
                }
            }
            return best;
        }

        private Unit GetFirstDeadAlly() => partyUnits.Find(h => h != null && !h.IsAlive);

        // =====================================================================
        // HÀNG ĐỢI LƯỢT ĐÁNH THEO SPEED
        // =====================================================================
        private void StartNewRound()
        {
            _turnQueue.Clear();
            foreach (Unit h in partyUnits) if (h != null && h.IsAlive) _turnQueue.Add(h);
            foreach (Unit e in enemyUnits) if (e != null && e.IsAlive) _turnQueue.Add(e);

            _turnQueue.Sort(CombatCalculator.CompareTurnOrder);

            // Quái vật lên kế hoạch Intent cho vòng này
            foreach (Unit e in enemyUnits)
            {
                if (e != null && e.IsAlive)
                {
                    e.PlanNextIntent(TurnCount);
                }
            }

            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            if (CheckWaveOrBattleEnd()) return;

            while (_turnQueue.Count > 0 && (_turnQueue[0] == null || !_turnQueue[0].IsAlive))
            {
                _turnQueue.RemoveAt(0);
            }

            if (_turnQueue.Count == 0)
            {
                TurnCount++;
                StartNewRound();
                return;
            }

            _activeUnit = _turnQueue[0];
            _turnQueue.RemoveAt(0);

            StartCoroutine(BeginUnitTurnRoutine(_activeUnit));
        }

        private IEnumerator BeginUnitTurnRoutine(Unit unit)
        {
            _isActionInProgress = true;
            EnsureValidTargets();

            unit.OnTurnStart(out int burnDamage, out bool wasStunned, out bool diedFromBurn);

            if (burnDamage > 0)
            {
                OnDamageDealt?.Invoke(unit, burnDamage, false);
                OnUnitStatusPop?.Invoke(unit, $"THIÊU ĐỐT -{burnDamage}", new Color(1f, 0.45f, 0.2f, 1f));
                AudioManager.Instance?.PlayHitSfx(false);
                SetDialogue($"<b>{MedievalUITheme.FormatHeroDisplayName(unit.unitName)}</b> chịu <b>{burnDamage}</b> sát thương thiêu đốt!");
                yield return new WaitForSeconds(0.65f);

                if (diedFromBurn)
                {
                    AudioManager.Instance?.PlayDeathSfx();
                    if (unit.isEnemy) TriggerEnemyLootDrop(unit);
                    yield return new WaitForSeconds(0.55f);
                    _isActionInProgress = false;
                    AdvanceTurn();
                    yield break;
                }
            }

            if (wasStunned)
            {
                OnUnitStatusPop?.Invoke(unit, "BỊ CHOÁNG!", MedievalUITheme.GoldSoft);
                SetDialogue($"<b>{MedievalUITheme.FormatHeroDisplayName(unit.unitName)}</b> đang bị choáng, bỏ qua lượt này!");
                OnHeroTurnChanged?.Invoke(unit.isEnemy ? null : unit);
                OnTurnQueueChanged?.Invoke(_turnQueue, _activeUnit);
                yield return new WaitForSeconds(0.95f);
                _isActionInProgress = false;
                AdvanceTurn();
                yield break;
            }

            _isActionInProgress = false;
            EnsureValidTargets();
            OnTurnQueueChanged?.Invoke(_turnQueue, _activeUnit);

            if (unit.isEnemy)
            {
                SetState(BattleState.ENEMYTURN);
                OnHeroTurnChanged?.Invoke(null);
                StartCoroutine(EnemyTurnRoutine(unit));
            }
            else
            {
                SetState(BattleState.PLAYERTURN);
                OnHeroTurnChanged?.Invoke(unit);
                SetDialogue($"Đến lượt của <b>{MedievalUITheme.FormatHeroDisplayName(unit.unitName)}</b> (SPD {unit.speed})! Chọn mục tiêu & hành động:");
            }
        }

        // =====================================================================
        // CƠ CHẾ CÂU HỎI TRẮC NGHIỆM GIÁO DỤC (QUIZ SYSTEM)
        // =====================================================================
        private void TriggerQuizForAction(PendingActionType actionType, int skillIndex = 0)
        {
            if (!enableQuizSystem)
            {
                ExecutePendingAction(actionType, skillIndex, 0);
                return;
            }

            _pendingAction = actionType;
            _pendingSkillIndex = skillIndex;
            _quizAnswerSubmitted = false;
            _skipQuizFeedback = false;

            if (QuizManager.Instance != null)
            {
                _activeQuestion = QuizManager.Instance.GetRandomQuestion();
            }
            else
            {
                _activeQuestion = new QuizQuestion(
                    "B1 • Grammar",
                    "By the time we reached the castle, the gates _____ already.",
                    new[] { "closed", "had closed", "have closed", "close" },
                    1,
                    "Past Perfect ('had closed') describes an event completed before another past action.",
                    7
                );
            }

            SetState(BattleState.WAITING_FOR_QUIZ);
            string heroLabel = CurrentHero != null ? MedievalUITheme.FormatHeroDisplayName(CurrentHero.unitName) : "Anh Hùng";
            string actionLabel = GetPendingActionDisplayName(actionType, skillIndex);
            string actionContext = $"{heroLabel} đang tụ lực [{actionLabel}] — Trả lời đúng để xuất chiêu!";

            AudioManager.Instance?.PlayQuizOpen();
            SetDialogue($"<b>{heroLabel}</b> đang tập trung! Trả lời câu hỏi để thi triển <b>{actionLabel}</b>.");

            if (OnQuizStarted != null)
            {
                OnQuizStarted.Invoke(_activeQuestion, actionContext);
            }
            else if (BattleUIManager.Instance != null)
            {
                BattleUIManager.Instance.ShowQuizPrompt(_activeQuestion);
            }
        }

        private string GetPendingActionDisplayName(PendingActionType actionType, int skillIndex)
        {
            if (actionType == PendingActionType.Heal) return "Hồi Phục";
            if (CurrentHero != null && CurrentHero.skills != null && skillIndex >= 0 && skillIndex < CurrentHero.skills.Count && CurrentHero.skills[skillIndex] != null)
            {
                return CurrentHero.skills[skillIndex].skillName;
            }
            return actionType == PendingActionType.Attack ? "Tấn Công" : "Kỹ Năng";
        }

        public void ProcessQuizAnswer(int answerIndex) => ProcessQuizAnswer(answerIndex, false);

        public void ProcessQuizAnswer(int answerIndex, bool isQuickMind)
        {
            if (state != BattleState.WAITING_FOR_QUIZ || _activeQuestion == null || _quizAnswerSubmitted) return;
            _quizAnswerSubmitted = true;

            int bonusDamage = 0;
            string feedback = "";
            bool timedOut = answerIndex < 0;

            if (QuizManager.Instance != null)
            {
                _isQuizAnswerCorrect = QuizManager.Instance.SubmitAnswer(answerIndex, out feedback, out bonusDamage);
            }
            else
            {
                _isQuizAnswerCorrect = _activeQuestion.IsCorrect(answerIndex);
                bonusDamage = _isQuizAnswerCorrect ? _activeQuestion.bonusDamage : 0;
                feedback = _isQuizAnswerCorrect ? "CORRECT!" : "INCORRECT!";
            }

            if (timedOut)
            {
                feedback = $"<b>TIME'S UP!</b> Correct answer: <b>{_activeQuestion.GetCorrectAnswerString()}</b>.\n<i>{_activeQuestion.explanation}</i>";
            }

            PlayerCampProgressManager campMgr = PlayerCampProgressManager.EnsureLoaded();
            TotalQuestionsAnswered++;
            _isQuickMindActive = _isQuizAnswerCorrect && isQuickMind;

            if (_isQuizAnswerCorrect)
            {
                CorrectAnswersCount++;
                CurrentQuizStreak++;
                if (CurrentQuizStreak > BestQuizStreak) BestQuizStreak = CurrentQuizStreak;

                int goldGain = 10 + (_isQuickMindActive ? 5 : 0);
                int expGain = 12 + (_isQuickMindActive ? 6 : 0);
                AwardSessionGold(goldGain);
                SessionExpEarned += expGain;

                if (CurrentHero != null)
                {
                    string popLabel = _isQuickMindActive ? $"QUICK MIND! +{goldGain}V" : $"+{goldGain} VÀNG!";
                    OnUnitStatusPop?.Invoke(CurrentHero, popLabel, MedievalUITheme.GoldSoft);
                }

                if (CurrentQuizStreak >= 5 && !string.IsNullOrEmpty(_activeQuestion.category)
                    && _activeQuestion.category.StartsWith("B2", StringComparison.OrdinalIgnoreCase))
                {
                    campMgr.UnlockAchievement(AchievementId.StreakFiveB2);
                }
            }
            else
            {
                CurrentQuizStreak = 0;
                campMgr.RecordQuizMistake(_activeQuestion.questionText);
            }

            AudioManager.Instance?.PlayQuizResult(_isQuizAnswerCorrect, CurrentQuizStreak);
            SetDialogue(feedback);

            float waitTime = 2.4f;
            OnQuizResultShown?.Invoke(
                _isQuizAnswerCorrect,
                answerIndex,
                _activeQuestion.correctAnswerIndex,
                bonusDamage,
                _activeQuestion.explanation,
                waitTime
            );

            StartCoroutine(HandleQuizResultRoutine(_isQuizAnswerCorrect, bonusDamage, waitTime));
        }

        private IEnumerator HandleQuizResultRoutine(bool isCorrect, int bonusDamage, float waitTime)
        {
            _isActionInProgress = true;

            float elapsed = 0f;
            while (elapsed < waitTime && !_skipQuizFeedback)
            {
                if (!IsPaused)
                {
                    elapsed += Time.unscaledDeltaTime;
                }
                yield return null;
            }

            OnQuizEnded?.Invoke();

            if (isCorrect)
            {
                SetState(BattleState.PLAYERTURN);
                ExecutePendingAction(_pendingAction, _pendingSkillIndex, bonusDamage);
            }
            else
            {
                Unit hero = CurrentHero;
                if (hero != null)
                {
                    OnUnitStatusPop?.Invoke(hero, "MẤT LƯỢT!", MedievalUITheme.DangerRed);
                    SetDialogue($"<color=#FF6B6B><b>BỊ MẤT LƯỢT!</b></color> Đòn của <b>{MedievalUITheme.FormatHeroDisplayName(hero.unitName)}</b> đã trượt!");
                }
                yield return new WaitForSeconds(0.95f);

                _isActionInProgress = false;
                AdvanceTurn();
            }
        }

        private void ExecutePendingAction(PendingActionType actionType, int skillIndex, int bonusDamage)
        {
            switch (actionType)
            {
                case PendingActionType.Attack:
                    StartCoroutine(ExecuteHeroSkillRoutine(0, bonusDamage));
                    break;
                case PendingActionType.Skill:
                    StartCoroutine(ExecuteHeroSkillRoutine(skillIndex, bonusDamage));
                    break;
                case PendingActionType.Heal:
                    StartCoroutine(PlayerHealRoutine(bonusDamage));
                    break;
            }
        }

        // =====================================================================
        // HÀNH ĐỘNG CỦA ANH HÙNG (BASIC ATTACK, SKILLS, ITEMS, DEFEND)
        // =====================================================================
        private IEnumerator ExecuteHeroSkillRoutine(int skillIndex, int bonusDamage)
        {
            _isActionInProgress = true;
            Unit hero = CurrentHero;
            if (hero == null)
            {
                _isActionInProgress = false;
                AdvanceTurn();
                yield break;
            }

            SkillData skill = (hero.skills != null && skillIndex >= 0 && skillIndex < hero.skills.Count)
                ? hero.skills[skillIndex]
                : null;

            string heroDisplay = MedievalUITheme.FormatHeroDisplayName(hero.unitName);

            if (skill != null)
            {
                hero.UseMana(skill.manaCost);
                hero.StartSkillCooldown(skill);
                SetDialogue($"<b>{heroDisplay}</b> thi triển <color=#6ED2FF><b>[{skill.skillName}]</b></color>!");
            }
            else
            {
                SetDialogue($"<b>{heroDisplay}</b> lao lên tấn công!");
            }

            // 1. Kỹ năng tự hỗ trợ (VD: Knight Iron Bulwark)
            if (skill != null && skill.IsSelfSupport)
            {
                yield return new WaitForSeconds(0.25f);
                AudioManager.Instance?.PlayDefendSfx();

                if (skill.defenseBonus > 0)
                {
                    int totalDef = skill.defenseBonus + Mathf.Max(0, bonusDamage / 2);
                    int turns = Mathf.Max(1, skill.effectDurationTurns);
                    hero.ApplyDefenseBuff(totalDef, turns);
                    OnUnitStatusPop?.Invoke(hero, $"+{totalDef} GIÁP!", MedievalUITheme.GoldSoft);
                }

                if (skill.statusEffect != StatusEffectType.None && skill.statusDurationTurns > 0)
                {
                    hero.ApplyStatus(skill.statusEffect, skill.statusDurationTurns, skill.statusPotency);
                    if (skill.statusEffect == StatusEffectType.Taunt)
                    {
                        OnUnitStatusPop?.Invoke(hero, "KHIÊU KHÍCH!", MedievalUITheme.GoldPrimary);
                    }
                }

                SetDialogue($"<b>{heroDisplay}</b> kích hoạt <b>{skill.skillName}</b> kiên cố phòng tuyến!");
                yield return new WaitForSeconds(0.85f);
                _isActionInProgress = false;
                AdvanceTurn();
                yield break;
            }

            // 2. Kỹ năng hồi máu đồng đội (VD: Mage Healing Light)
            if (skill != null && skill.IsHealSkill)
            {
                List<Unit> healTargets = new List<Unit>();
                if (skill.targetType == SkillTargetType.AllAllies)
                {
                    healTargets.AddRange(GetAliveHeroes());
                }
                else
                {
                    Unit chosenAlly = (_selectedAllyTarget != null && _selectedAllyTarget.IsAlive)
                        ? _selectedAllyTarget
                        : GetMostWoundedAliveAlly();
                    if (chosenAlly != null) healTargets.Add(chosenAlly);
                }

                bool animDone = false;
                Vector3 focusPos = healTargets.Count > 0 ? healTargets[0].transform.position : hero.transform.position + Vector3.right;

                void ApplyHeals()
                {
                    AudioManager.Instance?.PlayHealSfx();
                    int healValue = CombatCalculator.CalculateHealAmount(skill.baseHealAmount, hero.magicDamage, bonusDamage);
                    foreach (Unit ally in healTargets)
                    {
                        int gained = ally.Heal(healValue);
                        OnUnitHealed?.Invoke(ally, gained);
                        PixelCombatVFXManager.Instance?.PlayHealColumn(ally.transform.position);
                    }
                    string targetLabel = healTargets.Count > 1
                        ? "toàn đội hình"
                        : $"<b>{MedievalUITheme.FormatHeroDisplayName(healTargets[0].unitName)}</b>";
                    SetDialogue($"Ánh sáng thần thánh bao phủ {targetLabel}, hồi <b>+{healValue} HP</b>!");
                }

                if (hero.pixelAnimator != null)
                {
                    hero.pixelAnimator.PlayAttack(focusPos, ApplyHeals, () => animDone = true, customStepDistance: 0.16f);
                    while (!animDone) yield return null;
                }
                else
                {
                    ApplyHeals();
                }

                yield return new WaitForSeconds(0.6f);
                _isActionInProgress = false;
                AdvanceTurn();
                yield break;
            }

            // 3. Kỹ năng tấn công (SingleEnemy hoặc AllEnemies)
            EnsureValidTargets();
            List<Unit> targets = new List<Unit>();
            if (skill != null && skill.targetType == SkillTargetType.AllEnemies)
            {
                targets.AddRange(GetAliveEnemies());
            }
            else if (_selectedEnemyTarget != null && _selectedEnemyTarget.IsAlive)
            {
                targets.Add(_selectedEnemyTarget);
            }

            if (targets.Count == 0)
            {
                _isActionInProgress = false;
                AdvanceTurn();
                yield break;
            }

            bool isAttackDone = false;
            Vector3 attackPos = targets[0].transform.position;
            float quickMindCritBonus = _isQuickMindActive ? 0.15f : 0f;
            _isQuickMindActive = false;

            string skillLower = skill != null && skill.skillName != null ? skill.skillName.ToLowerInvariant() : "";
            bool isRainOfArrows = skillLower.Contains("rain");
            bool isFireball = skillLower.Contains("fireball");
            bool isRangedAttack = hero.characterClass == CharacterClassType.Mage || hero.characterClass == CharacterClassType.Archer;

            void ApplyDamageImpact()
            {
                DamageType dmgType = skill != null ? skill.damageType : DamageType.Physical;
                int basePower = dmgType == DamageType.Magical ? hero.magicDamage : hero.damage;
                float powerMult = skill != null ? Mathf.Max(0.1f, skill.powerMultiplier) : 1f;
                int flatBonus = skill != null ? skill.flatBonusDamage : 0;
                float bonusCrit = (skill != null ? skill.bonusCritRate : 0f) + quickMindCritBonus;

                bool anyCrit = false;
                int totalDamageShown = 0;

                foreach (Unit target in targets)
                {
                    if (target == null || !target.IsAlive) continue;

                    bool isCrit = UnityEngine.Random.value < (hero.critRate + bonusCrit);
                    if (isCrit) anyCrit = true;

                    int rawDmg = CombatCalculator.CalculateSkillRawDamage(
                        basePower,
                        powerMult,
                        flatBonus,
                        hero.critMultiplier,
                        isCrit,
                        bonusDamage,
                        enableQuizSystem ? CurrentQuizStreak : 0,
                        hero.AttackBonusPercent);

                    int actualDmg = target.TakeDamage(rawDmg, dmgType, out bool targetDied);
                    totalDamageShown += actualDmg;
                    OnDamageDealt?.Invoke(target, actualDmg, isCrit);

                    if (!isRangedAttack)
                    {
                        Color slashCol = isCrit ? MedievalUITheme.GoldSoft : new Color(0.95f, 0.96f, 1f, 1f);
                        PixelCombatVFXManager.Instance?.PlaySlashArc(target.transform.position, slashCol, false, isCrit ? 1.45f : 1.15f);
                    }
                    else if (isFireball)
                    {
                        PixelCombatVFXManager.Instance?.PlayExplosionBurst(target.transform.position, new Color(1f, 0.52f, 0.18f, 1f), 1.15f);
                    }

                    // Hiệu ứng phụ (Stun / Burn)
                    if (!targetDied && skill != null && skill.statusEffect != StatusEffectType.None && !skill.applyStatusToSelf && skill.statusDurationTurns > 0)
                    {
                        target.ApplyStatus(skill.statusEffect, skill.statusDurationTurns, skill.statusPotency);
                        OnUnitStatusPop?.Invoke(target, skill.statusEffect == StatusEffectType.Stun ? "CHOÁNG!" : "THIÊU ĐỐT!", MedievalUITheme.GoldSoft);
                    }

                    // Boss Phase 2 Trigger & Quiz Shield Break + Boss Phase 2 Music
                    if (!targetDied)
                    {
                        if (target.TryTriggerPhase2())
                        {
                            OnUnitStatusPop?.Invoke(target, "KHIÊN BÓNG TỐI x2!", MedievalUITheme.BtnSkill);
                            PixelCombatVFXManager.Instance?.PlayExplosionBurst(target.transform.position, new Color(0.65f, 0.25f, 1f, 1f), 1.35f);
                            AudioManager.Instance?.PlayBattleMusic(true);
                        }
                        else if (target.ShadowShieldStacks > 0 && (!enableQuizSystem || _isQuizAnswerCorrect))
                        {
                            if (target.StripShadowShield(out bool broken))
                            {
                                if (broken)
                                {
                                    OnUnitStatusPop?.Invoke(target, "PHÁ KHIÊN • CHOÁNG!", MedievalUITheme.GoldSoft);
                                    PixelCombatVFXManager.Instance?.PlayShieldShatter(target.transform.position);
                                    PixelCombatVFXManager.Instance?.TriggerCameraShake(0.18f, 0.24f);
                                    int breakDmg = target.TakeDamage(25, DamageType.TrueDamage, out targetDied);
                                    OnDamageDealt?.Invoke(target, breakDmg, true);
                                }
                                else
                                {
                                    OnUnitStatusPop?.Invoke(target, $"KHIÊN CÒN {target.ShadowShieldStacks}", MedievalUITheme.BtnSkill);
                                }
                            }
                        }
                    }

                    if (targetDied)
                    {
                        AudioManager.Instance?.PlayDeathSfx();
                        if (target.isEnemy) TriggerEnemyLootDrop(target);
                    }
                }

                if (anyCrit)
                {
                    PixelCombatVFXManager.Instance?.TriggerHitstop(0.045f);
                    PixelCombatVFXManager.Instance?.TriggerCameraShake(0.14f, 0.20f);
                }

                AudioManager.Instance?.PlayAttackForHero(hero.characterClass, skill, anyCrit);

                string critTag = anyCrit ? "<color=#F2CA6B><b>CHÍ MẠNG!</b> </color>" : "";
                string streakTag = (enableQuizSystem && CurrentQuizStreak >= 3) ? " <color=#5CE09A>[Combo x1.2]</color>" : "";
                string targetStr = targets.Count > 1 ? $"{targets.Count} kẻ địch" : targets[0].unitName;
                SetDialogue($"{critTag}Gây tổng <b>{totalDamageShown}</b> sát thương lên <b>{targetStr}</b>!{streakTag}");
            }

            if (hero.pixelAnimator != null)
            {
                if (isRainOfArrows)
                {
                    bool arrowHitDone = false;
                    hero.pixelAnimator.PlayAttack(attackPos, () =>
                    {
                        PixelCombatVFXManager.EnsureExists().PlayRainOfArrows(attackPos, () =>
                        {
                            ApplyDamageImpact();
                            arrowHitDone = true;
                        });
                    }, () => isAttackDone = true, customStepDistance: 0.14f);
                    while (!isAttackDone || !arrowHitDone) yield return null;
                }
                else if (isRangedAttack)
                {
                    bool projHitDone = false;
                    Color projColor = isFireball
                        ? new Color(1f, 0.52f, 0.18f, 1f)
                        : (hero.characterClass == CharacterClassType.Mage ? new Color(0.45f, 0.85f, 1f, 1f) : new Color(0.72f, 0.96f, 0.58f, 1f));
                    bool isArrowProj = hero.characterClass == CharacterClassType.Archer;

                    hero.pixelAnimator.PlayAttack(attackPos, () =>
                    {
                        StartCoroutine(PixelCombatVFXManager.EnsureExists().PlayProjectileRoutine(
                            hero.transform.position, attackPos, projColor, isArrowProj, 0.22f, () =>
                            {
                                ApplyDamageImpact();
                                projHitDone = true;
                            }));
                    }, () => isAttackDone = true, customStepDistance: 0.16f);
                    while (!isAttackDone || !projHitDone) yield return null;
                }
                else
                {
                    hero.pixelAnimator.PlayAttack(attackPos, ApplyDamageImpact, () => isAttackDone = true);
                    while (!isAttackDone) yield return null;
                }
            }
            else
            {
                ApplyDamageImpact();
            }

            yield return new WaitForSeconds(0.55f);
            _isActionInProgress = false;
            EnsureValidTargets();
            AdvanceTurn();
        }

        private IEnumerator PlayerHealRoutine(int bonusAmount)
        {
            _isActionInProgress = true;
            Unit hero = CurrentHero;
            Unit target = (_selectedAllyTarget != null && _selectedAllyTarget.IsAlive)
                ? _selectedAllyTarget
                : GetMostWoundedAliveAlly();

            if (target != null && hero != null)
            {
                AudioManager.Instance?.PlayHealSfx();
                int healValue = 25 + bonusAmount;
                int gained = target.Heal(healValue);
                OnUnitHealed?.Invoke(target, gained);
                SetDialogue($"<b>{MedievalUITheme.FormatHeroDisplayName(hero.unitName)}</b> hồi <b>+{gained} HP</b> cho <b>{MedievalUITheme.FormatHeroDisplayName(target.unitName)}</b>!");
            }

            yield return new WaitForSeconds(0.8f);
            _isActionInProgress = false;
            AdvanceTurn();
        }

        public bool CanUseConsumable(ConsumableItemType item)
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress || CurrentHero == null) return false;
            if (Inventory.GetCount(item) <= 0) return false;

            if (item == ConsumableItemType.PhoenixFeather)
            {
                return GetFirstDeadAlly() != null;
            }
            return GetAliveHeroes().Count > 0;
        }

        public void UseConsumableItem(ConsumableItemType item)
        {
            if (!CanUseConsumable(item)) return;
            StartCoroutine(UseConsumableRoutine(item));
        }

        private IEnumerator UseConsumableRoutine(ConsumableItemType item)
        {
            _isActionInProgress = true;
            Unit hero = CurrentHero;
            string heroDisplay = MedievalUITheme.FormatHeroDisplayName(hero.unitName);

            if (!Inventory.TryConsume(item))
            {
                _isActionInProgress = false;
                yield break;
            }

            if (item == ConsumableItemType.HealthPotion)
            {
                Unit ally = (_selectedAllyTarget != null && _selectedAllyTarget.IsAlive)
                    ? _selectedAllyTarget
                    : GetMostWoundedAliveAlly();
                int gained = ally != null ? ally.Heal(55) : 0;
                if (ally != null)
                {
                    OnUnitHealed?.Invoke(ally, gained);
                    AudioManager.Instance?.PlayHealSfx();
                    SetDialogue($"<b>{heroDisplay}</b> dùng <b>Thảo Dược Máu</b> hồi <b>+{gained} HP</b> cho <b>{MedievalUITheme.FormatHeroDisplayName(ally.unitName)}</b>!");
                }
            }
            else if (item == ConsumableItemType.ManaElixir)
            {
                Unit ally = (_selectedAllyTarget != null && _selectedAllyTarget.IsAlive)
                    ? _selectedAllyTarget
                    : hero;
                ally.RestoreMana(30);
                OnUnitStatusPop?.Invoke(ally, "+30 MP", MedievalUITheme.MpSapphire);
                AudioManager.Instance?.PlayHealSfx();
                SetDialogue($"<b>{heroDisplay}</b> dùng <b>Bình Mana</b> hồi <b>+30 MP</b> cho <b>{MedievalUITheme.FormatHeroDisplayName(ally.unitName)}</b>!");
            }
            else if (item == ConsumableItemType.PhoenixFeather)
            {
                Unit fallen = (_selectedAllyTarget != null && !_selectedAllyTarget.IsAlive)
                    ? _selectedAllyTarget
                    : GetFirstDeadAlly();
                if (fallen != null)
                {
                    int hp = fallen.Revive(Mathf.RoundToInt(fallen.maxHP * 0.5f));
                    OnUnitHealed?.Invoke(fallen, hp);
                    OnUnitStatusPop?.Invoke(fallen, "HỒI SINH!", MedievalUITheme.GoldSoft);
                    AudioManager.Instance?.PlayHealSfx();
                    SetDialogue($"<b>{heroDisplay}</b> dùng <b>Lông Phượng Hoàng</b> hồi sinh <b>{MedievalUITheme.FormatHeroDisplayName(fallen.unitName)}</b> ({hp} HP)!");
                }
            }
            else if (item == ConsumableItemType.WarElixir)
            {
                List<Unit> alive = GetAliveHeroes();
                for (int i = 0; i < alive.Count; i++)
                {
                    alive[i].ApplyStatus(StatusEffectType.AttackUp, 3, 30);
                    OnUnitStatusPop?.Invoke(alive[i], "+30% DAME!", MedievalUITheme.BtnAttack);
                }
                AudioManager.Instance?.PlayDefendSfx();
                SetDialogue($"<b>{heroDisplay}</b> kích hoạt <b>Thuốc Cuồng Chiến</b>! Toàn đội tăng <b>+30% Sát Thương</b> trong 3 lượt!");
            }
            else if (item == ConsumableItemType.IronTonic)
            {
                List<Unit> alive = GetAliveHeroes();
                for (int i = 0; i < alive.Count; i++)
                {
                    int gained = alive[i].Heal(25);
                    alive[i].ApplyStatus(StatusEffectType.DefenseUp, 3, 12);
                    if (gained > 0) OnUnitHealed?.Invoke(alive[i], gained);
                    OnUnitStatusPop?.Invoke(alive[i], "+12 GIÁP!", MedievalUITheme.BtnDefend);
                }
                AudioManager.Instance?.PlayDefendSfx();
                SetDialogue($"<b>{heroDisplay}</b> kích hoạt <b>Thuốc Thiết Giáp</b>! Cả đội hồi +25 HP & tăng <b>+12 Giáp</b> trong 3 lượt!");
            }

            yield return new WaitForSeconds(0.85f);
            _isActionInProgress = false;
            EnsureValidTargets();
            AdvanceTurn();
        }

        public IEnumerator PlayerDefend()
        {
            if (_isActionInProgress || CurrentHero == null) yield break;
            _isActionInProgress = true;

            Unit hero = CurrentHero;
            hero.isDefending = true;
            hero.RestoreMana(8);

            // Hiệp Sĩ khi Phòng Thủ thu hút 1 lượt đòn đánh
            if (hero.characterClass == CharacterClassType.Knight)
            {
                hero.ApplyStatus(StatusEffectType.Taunt, 1, 0);
            }

            AudioManager.Instance?.PlayDefendSfx();
            OnUnitStatusPop?.Invoke(hero, "THỦ +8 MP", MedievalUITheme.MpSapphire);
            SetDialogue($"<b>{MedievalUITheme.FormatHeroDisplayName(hero.unitName)}</b> vào thế phòng thủ (-50% sát thương) và hồi <b>+8 MP</b>!");

            yield return new WaitForSeconds(0.8f);
            _isActionInProgress = false;
            AdvanceTurn();
        }

        // =====================================================================
        // LƯỢT CỦA KẺ ĐỊCH (ENEMY TURN)
        // =====================================================================
        private IEnumerator EnemyTurnRoutine(Unit enemy)
        {
            _isActionInProgress = true;
            List<Unit> aliveHeroes = GetAliveHeroes();
            if (aliveHeroes.Count == 0)
            {
                _isActionInProgress = false;
                SetState(BattleState.LOST);
                EndBattle();
                yield break;
            }

            SkillData skill = enemy.PlannedSkill;
            SetDialogue($"<b>{enemy.unitName}</b> chuẩn bị hành động!");
            yield return new WaitForSeconds(0.45f);

            // Nếu quái tự buff giáp
            if (skill != null && skill.IsSelfSupport)
            {
                if (skill.manaCost > 0) enemy.UseMana(skill.manaCost);
                enemy.StartSkillCooldown(skill);
                if (skill.defenseBonus > 0)
                {
                    enemy.ApplyDefenseBuff(skill.defenseBonus, Mathf.Max(1, skill.effectDurationTurns));
                    OnUnitStatusPop?.Invoke(enemy, $"+{skill.defenseBonus} GIÁP!", MedievalUITheme.GoldSoft);
                }
                AudioManager.Instance?.PlayEnemyAttackSfx(skill);
                SetDialogue($"<b>{enemy.unitName}</b> kích hoạt <b>{skill.skillName}</b>!");
                enemy.PlanNextIntent(TurnCount + 1);
                yield return new WaitForSeconds(0.75f);
                _isActionInProgress = false;
                AdvanceTurn();
                yield break;
            }

            // Chọn mục tiêu (ưu tiên Taunt hoặc AoE)
            List<Unit> targets = new List<Unit>();
            if (skill != null && skill.targetType == SkillTargetType.AllEnemies)
            {
                targets.AddRange(aliveHeroes);
            }
            else
            {
                Unit tauntTarget = aliveHeroes.Find(h => h.HasTaunt);
                Unit picked = tauntTarget != null ? tauntTarget : aliveHeroes[UnityEngine.Random.Range(0, aliveHeroes.Count)];
                targets.Add(picked);
            }

            if (skill != null)
            {
                if (skill.manaCost > 0) enemy.UseMana(skill.manaCost);
                enemy.StartSkillCooldown(skill);
            }

            bool animDone = false;
            Vector3 strikePos = targets[0].transform.position;
            string enemySkillLower = skill != null && skill.skillName != null ? skill.skillName.ToLowerInvariant() : "";
            bool isEnemyRanged = skill != null && (skill.damageType == DamageType.Magical || enemySkillLower.Contains("arrow") || enemySkillLower.Contains("volley") || enemySkillLower.Contains("dagger"));

            void ApplyEnemyStrike()
            {
                AudioManager.Instance?.PlayEnemyAttackSfx(skill);
                DamageType dmgType = skill != null ? skill.damageType : DamageType.Physical;
                int baseAtk = dmgType == DamageType.Magical ? enemy.magicDamage : enemy.damage;
                float mult = skill != null ? Mathf.Max(0.5f, skill.powerMultiplier) : 1f;
                int flat = skill != null ? skill.flatBonusDamage : 0;

                int totalDmg = 0;
                bool anyCrit = false;
                foreach (Unit h in targets)
                {
                    if (h == null || !h.IsAlive) continue;

                    bool isCrit = UnityEngine.Random.value < (enemy.critRate + (skill != null ? skill.bonusCritRate : 0f));
                    if (isCrit) anyCrit = true;
                    int raw = CombatCalculator.CalculateSkillRawDamage(
                        baseAtk,
                        mult,
                        flat,
                        enemy.critMultiplier,
                        isCrit,
                        0,
                        0,
                        enemy.AttackBonusPercent);

                    int actual = h.TakeDamage(raw, dmgType, out bool heroDied);
                    totalDmg += actual;
                    OnDamageDealt?.Invoke(h, actual, isCrit);

                    if (!isEnemyRanged)
                    {
                        PixelCombatVFXManager.Instance?.PlaySlashArc(h.transform.position, new Color(1f, 0.35f, 0.32f, 1f), true, 1.15f);
                    }
                    else
                    {
                        Color hitCol = dmgType == DamageType.Magical ? new Color(0.75f, 0.35f, 1f, 1f) : new Color(0.95f, 0.48f, 0.35f, 1f);
                        PixelCombatVFXManager.Instance?.PlayExplosionBurst(h.transform.position, hitCol, 0.75f);
                    }

                    if (!heroDied && skill != null && skill.statusEffect != StatusEffectType.None && !skill.applyStatusToSelf && skill.statusDurationTurns > 0)
                    {
                        h.ApplyStatus(skill.statusEffect, skill.statusDurationTurns, skill.statusPotency);
                        OnUnitStatusPop?.Invoke(h, skill.statusEffect == StatusEffectType.Burn ? "THIÊU ĐỐT!" : "CHOÁNG!", MedievalUITheme.DangerRed);
                    }

                    if (heroDied)
                    {
                        AudioManager.Instance?.PlayDeathSfx();
                    }
                }

                if (anyCrit) PixelCombatVFXManager.Instance?.TriggerCameraShake(0.11f, 0.16f);

                string skillLabel = skill != null ? $" dùng <b>{skill.skillName}</b>" : " tấn công";
                string targetLabel = targets.Count > 1
                    ? "toàn bộ đội hình"
                    : $"<b>{MedievalUITheme.FormatHeroDisplayName(targets[0].unitName)}</b>";
                SetDialogue($"<b>{enemy.unitName}</b>{skillLabel} trúng {targetLabel}, gây <b>{totalDmg}</b> sát thương!");
            }

            if (enemy.pixelAnimator != null)
            {
                if (isEnemyRanged)
                {
                    bool projDone = false;
                    bool arrow = enemySkillLower.Contains("arrow") || enemySkillLower.Contains("volley") || enemySkillLower.Contains("dagger");
                    Color projColor = arrow ? new Color(0.85f, 0.95f, 0.65f, 1f) : new Color(0.78f, 0.32f, 1f, 1f);
                    enemy.pixelAnimator.PlayAttack(strikePos, () =>
                    {
                        StartCoroutine(PixelCombatVFXManager.EnsureExists().PlayProjectileRoutine(
                            enemy.transform.position, strikePos, projColor, arrow, 0.22f, () =>
                            {
                                ApplyEnemyStrike();
                                projDone = true;
                            }));
                    }, () => animDone = true, customStepDistance: 0.15f);
                    while (!animDone || !projDone) yield return null;
                }
                else
                {
                    enemy.pixelAnimator.PlayAttack(strikePos, ApplyEnemyStrike, () => animDone = true);
                    while (!animDone) yield return null;
                }
            }
            else
            {
                ApplyEnemyStrike();
            }

            enemy.PlanNextIntent(TurnCount + 1);
            yield return new WaitForSeconds(0.55f);

            _isActionInProgress = false;
            AdvanceTurn();
        }

        // =====================================================================
        // KIỂM TRA CHUYỂN ĐỢT / KẾT THÚC TRẬN
        // =====================================================================
        private bool CheckWaveOrBattleEnd()
        {
            if (GetAliveHeroes().Count == 0)
            {
                SetState(BattleState.LOST);
                EndBattle();
                return true;
            }

            if (GetAliveEnemies().Count == 0)
            {
                List<BattleWave> waves = GetActiveWaveList();
                if (ActiveStage.isEndless && _currentWaveIndex + 1 >= waves.Count)
                {
                    waves.Add(StageCatalog.CreateEndlessWave(
                        _currentWaveIndex + 1, enemyPrefab, skeletonPrefab, skeletonArcherPrefab, shadowMagePrefab, bossPrefab));
                }

                if (_currentWaveIndex + 1 < waves.Count)
                {
                    StartCoroutine(TransitionToNextWaveRoutine());
                    return true;
                }

                SetState(BattleState.WON);
                EndBattle();
                return true;
            }

            return false;
        }

        private IEnumerator TransitionToNextWaveRoutine()
        {
            _isActionInProgress = true;

            // Tự động gom các vật phẩm còn nằm trên mặt đất trước khi sang đợt mới để không bao giờ mất đồ
            if (_groundLootDrops.Count > 0)
            {
                CollectAllGroundLoot();
                yield return new WaitForSeconds(0.35f);
            }

            _currentWaveIndex++;

            // Hồi phục nhẹ giữa các Wave (18% HP & 18 MP cho người còn sống)
            foreach (Unit h in partyUnits)
            {
                if (h == null || !h.IsAlive) continue;
                int hpHeal = Mathf.Max(12, Mathf.RoundToInt(h.maxHP * 0.18f));
                int gained = h.Heal(hpHeal);
                h.RestoreMana(18);
                if (gained > 0) OnUnitHealed?.Invoke(h, gained);
            }

            List<BattleWave> waves = GetActiveWaveList();
            BattleWave nextWave = waves[_currentWaveIndex];
            string title = nextWave.waveTitle;
            SetDialogue($"<color=#F2CA6B><b>VƯỢT QUA ĐỢT {_currentWaveIndex}!</b></color> Đội hình hành quân dọc đường tiến tới <b>{title}</b>...");

            for (int i = 0; i < partyUnits.Count; i++)
            {
                if (partyUnits[i] != null && partyUnits[i].IsAlive)
                {
                    partyUnits[i].pixelAnimator?.StartMarchRun(1.22f);
                }
            }

            bool isBossWave = nextWave.isBossWave;
            if (forestRoad != null)
            {
                yield return forestRoad.ScrollForwardRoutine(1.25f, 5.8f, _currentWaveIndex + 1, waves.Count, isBossWave);
            }
            else
            {
                yield return new WaitForSeconds(0.95f);
            }

            for (int i = 0; i < partyUnits.Count; i++)
            {
                if (partyUnits[i] != null && partyUnits[i].IsAlive)
                {
                    partyUnits[i].pixelAnimator?.StopMarchRun();
                }
            }

            yield return SpawnWaveRoutine(_currentWaveIndex, animateEntrance: true);

            isBossWave = IsCurrentWaveBoss();
            AudioManager.Instance?.PlayBattleMusic(isBossWave);
            OnWaveChanged?.Invoke();

            int dropPct = Mathf.RoundToInt(CurrentWaveDropChance * 100f);
            SetDialogue(isBossWave
                ? "<color=#FFB020><b>CẢNH BÁO BOSS CUỐI MAP!</b></color> Thủ lĩnh bóng đêm xuất hiện — Chắc chắn rớt <b>2 trang bị cao cấp</b>!"
                : $"<b>{title}</b> đã áp sát! Tỉ lệ rớt đồ tăng lên <color=#F2CA6B><b>{dropPct}%</b></color>!");

            yield return new WaitForSeconds(0.75f);
            _isActionInProgress = false;
            StartNewRound();
        }

        // =====================================================================
        // HỆ THỐNG RỚT ĐỒ & NHẶT ĐỒ TRÊN CHIẾN TRƯỜNG (LOOT DROP & PICKUP)
        // =====================================================================
        public void TriggerEnemyLootDrop(Unit deadEnemy)
        {
            if (deadEnemy == null || !deadEnemy.isEnemy) return;
            if (!_lootDroppedUnits.Add(deadEnemy)) return;

            int baseGold = deadEnemy.isBoss ? 120 : (15 + _currentWaveIndex * 4);
            int enemyGold = Mathf.RoundToInt(baseGold * Mathf.Max(1f, ActiveStage.rewardMultiplier));
            AwardSessionGold(enemyGold);
            SessionExpEarned += deadEnemy.isBoss ? 85 : (22 + _currentWaveIndex * 5);

            List<BattleWave> waves = GetActiveWaveList();
            BattleWave wave = (_currentWaveIndex >= 0 && _currentWaveIndex < waves.Count) ? waves[_currentWaveIndex] : null;
            float dropBonus = wave != null ? wave.dropChanceBonus : 0f;
            float luckBonus = wave != null ? wave.rarityLuckBonus : 0f;

            PlayerEquipmentManager equipMgr = PlayerEquipmentManager.EnsureLoaded();
            List<LootDropResult> drops = LootSystem.RollDropsForDefeatedEnemy(
                equipMgr.Catalog,
                _currentWaveIndex,
                Mathf.Max(1, waves.Count),
                deadEnemy.isBoss,
                dropBonus,
                luckBonus,
                enableQuizSystem ? CurrentQuizStreak : 0);

            if (drops.Count == 0)
            {
                OnUnitStatusPop?.Invoke(deadEnemy, $"+{enemyGold} VÀNG!", MedievalUITheme.GoldSoft);
                return;
            }

            ItemRarity highestRarity = ItemRarity.Common;
            Vector3 origin = deadEnemy.transform.position + new Vector3(0f, 0.25f, 0f);

            for (int i = 0; i < drops.Count; i++)
            {
                LootDropResult drop = drops[i];
                if (drop.DisplayRarity > highestRarity) highestRarity = drop.DisplayRarity;

                // Phân bố vị trí rơi trên nền cỏ rừng xung quanh vị trí quái chết (không chồng lên nhau)
                float spreadX = (i - (drops.Count - 1) * 0.5f) * 1.15f + UnityEngine.Random.Range(-0.18f, 0.18f);
                float groundX = Mathf.Clamp(deadEnemy.transform.position.x + spreadX - 0.35f, -0.4f, 4.35f);
                float groundY = Mathf.Clamp(-1.46f + (i % 2) * 0.24f + UnityEngine.Random.Range(-0.08f, 0.08f), -1.64f, -1.12f);
                Vector3 groundPos = new Vector3(groundX, groundY, -0.05f);

                WorldLootDrop worldDrop = WorldLootDrop.Spawn(drop, origin, groundPos, lootLabelFont);
                _groundLootDrops.Add(worldDrop);
                OnLootDropped?.Invoke(drop, worldDrop);
            }

            AudioManager.Instance?.PlayLootDropSfx(highestRarity);

            string rarityTag = highestRarity switch
            {
                ItemRarity.Legendary => "RỚT HUYỀN THOẠI!",
                ItemRarity.Epic => "RỚT SỬ THI!",
                ItemRarity.Rare => "RỚT ĐỒ HIẾM!",
                _ => "RỚT CHIẾN LỢI PHẨM!"
            };
            OnUnitStatusPop?.Invoke(deadEnemy, rarityTag, EquipmentItemData.GetRarityColor(highestRarity));
            OnGroundLootCountChanged?.Invoke(_groundLootDrops.Count);
        }

        public void CollectLootDrop(WorldLootDrop drop)
        {
            if (drop == null || drop.IsCollected) return;

            _groundLootDrops.Remove(drop);
            LootDropResult data = drop.DropData;
            _sessionLootCollected.Add(data);

            OwnedEquipmentInstance addedEquip = null;
            if (!data.isConsumableOnly && data.equipment != null)
            {
                addedEquip = PlayerEquipmentManager.EnsureLoaded().AddItem(data.equipment, data.waveNumber);
                if (data.equipment.rarity == ItemRarity.Legendary)
                {
                    PlayerCampProgressManager.EnsureLoaded().UnlockAchievement(AchievementId.FirstLegendary);
                }
            }
            else if (data.isConsumableOnly)
            {
                Inventory.AddItem(data.consumableType, 1);
            }

            AudioManager.Instance?.PlayLootPickupSfx();

            Vector3 flyTarget = Camera.main != null
                ? Camera.main.ViewportToWorldPoint(new Vector3(0.92f, 0.90f, -Camera.main.transform.position.z))
                : new Vector3(4.5f, 2.6f, 0f);
            flyTarget.z = -0.1f;

            drop.PlayCollectAndDestroy(flyTarget);
            OnLootPickedUp?.Invoke(data, addedEquip);
            OnGroundLootCountChanged?.Invoke(_groundLootDrops.Count);
        }

        public int CollectAllGroundLoot()
        {
            if (_groundLootDrops.Count == 0) return 0;
            List<WorldLootDrop> snapshot = new List<WorldLootDrop>(_groundLootDrops);
            int collected = 0;
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] != null && !snapshot[i].IsCollected)
                {
                    CollectLootDrop(snapshot[i]);
                    collected++;
                }
            }
            return collected;
        }

        private void AwardSessionGold(int amount)
        {
            if (amount <= 0) return;
            SessionGoldEarned += amount;
            PlayerCampProgressManager campMgr = PlayerCampProgressManager.EnsureLoaded();
            campMgr.AddGold(amount);
            OnGoldChanged?.Invoke(campMgr.Gold);
        }

        private void EndBattle()
        {
            // Gom hết vật phẩm còn trên chiến trường trước khi hiện tổng kết
            CollectAllGroundLoot();

            PlayerCampProgressManager campMgr = PlayerCampProgressManager.EnsureLoaded();
            int finalExp = SessionExpEarned;

            if (state == BattleState.WON)
            {
                int winGold = Mathf.RoundToInt(80f * Mathf.Max(1f, ActiveStage.rewardMultiplier));
                AwardSessionGold(winGold);
                finalExp += 65;

                int aliveCount = GetAliveHeroes().Count;
                float quizRatio = TotalQuestionsAnswered > 0 ? (float)CorrectAnswersCount / TotalQuestionsAnswered : 1f;
                int stars = 1;
                if (quizRatio >= 0.75f) stars++;
                if (aliveCount >= partyUnits.Count && partyUnits.Count > 0) stars++;
                LastEarnedStars = stars;

                campMgr.RecordStageResult(ActiveStageId, true, CurrentWaveNumber, stars, out int firstGold, out bool unlocked);
                LastFirstClearGoldBonus = firstGold;
                LastUnlockedNewStage = unlocked;
                if (firstGold > 0) SessionGoldEarned += firstGold;

                campMgr.AwardPartyExp(finalExp, out List<CharacterClassType> ups);
                _leveledUpClasses.Clear();
                _leveledUpClasses.AddRange(ups);

                if (_leveledUpClasses.Count > 0) AudioManager.Instance?.PlayLevelUpSfx();
                else AudioManager.Instance?.PlayVictoryFanfare();

                SetDialogue($"<color=#5CE09A><b>CHIẾN THẮNG VANG DỘI!</b></color> Nhận <color=#F2CA6B><b>+{SessionGoldEarned} Vàng</b></color> & <color=#6ED2FF><b>+{finalExp} EXP</b></color>!");
            }
            else if (state == BattleState.LOST)
            {
                finalExp = Mathf.Max(SessionExpEarned > 0 ? 15 : 0, Mathf.RoundToInt(SessionExpEarned * 0.45f));
                LastEarnedStars = 0;
                campMgr.RecordStageResult(ActiveStageId, false, CurrentWaveNumber, 0, out _, out _);
                campMgr.AwardPartyExp(finalExp, out List<CharacterClassType> ups);
                _leveledUpClasses.Clear();
                _leveledUpClasses.AddRange(ups);

                AudioManager.Instance?.PlayDefeatSting();
                SetDialogue($"<color=#FF6B6B><b>THẤT BẠI!</b></color> Mang về <color=#F2CA6B><b>+{SessionGoldEarned} Vàng</b></color> & <b>+{finalExp} EXP</b> để rèn luyện lại!");
            }

            SessionExpEarned = finalExp;
            campMgr.SyncAfterBattle(Inventory, consumeActiveBuffs: true);
            OnBattleEnded?.Invoke(state);
        }

        private void SetDialogue(string message)
        {
            CurrentDialogue = message;
            OnDialogueChanged?.Invoke(message);
        }

        // =====================================================================
        // SỰ KIỆN GỌI TỪ NÚT BẤM (BUTTON HANDLERS)
        // =====================================================================
        public void OnAttackButton()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            TriggerQuizForAction(PendingActionType.Attack, 0);
        }

        public void OnSkill1Button()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            if (!ValidateSkillUse(1)) return;
            TriggerQuizForAction(PendingActionType.Skill, 1);
        }

        public void OnSkill2Button()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            if (!ValidateSkillUse(2)) return;
            TriggerQuizForAction(PendingActionType.Skill, 2);
        }

        private bool ValidateSkillUse(int index)
        {
            if (CurrentHero == null || CurrentHero.skills == null || index >= CurrentHero.skills.Count) return false;
            SkillData skill = CurrentHero.skills[index];
            if (skill == null) return false;
            if (CurrentHero.IsSkillOnCooldown(skill))
            {
                SetDialogue($"Kỹ năng <b>{skill.skillName}</b> đang hồi chiêu (còn {CurrentHero.GetSkillCooldown(skill)} lượt)!");
                return false;
            }
            if (CurrentHero.currentMP < skill.manaCost)
            {
                SetDialogue($"Không đủ Mana để dùng <b>{skill.skillName}</b>! Cần {skill.manaCost} MP.");
                return false;
            }
            return true;
        }

        public void OnHealButton()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            TriggerQuizForAction(PendingActionType.Heal);
        }

        public void OnDefendButton()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            StartCoroutine(PlayerDefend());
        }

        public void FinishEndlessExpedition()
        {
            if (!ActiveStage.isEndless || state == BattleState.WON || state == BattleState.LOST) return;
            StopAllCoroutines();
            SetPaused(false);
            _isActionInProgress = false;
            SetState(BattleState.WON);
            EndBattle();
        }
    }
}
