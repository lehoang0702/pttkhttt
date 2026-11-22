using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MedievalRPG.Data
{
    [Serializable]
    public struct ConsumableStockEntry
    {
        public ConsumableItemType itemType;
        public int count;
    }

    [Serializable]
    public class HeroProgressEntry
    {
        public CharacterClassType classType;
        public int level = 1;
        public int exp = 0;
    }

    [Serializable]
    public class StageProgressEntry
    {
        public CampaignStageId stageId;
        public bool unlocked;
        public bool cleared;
        public int bestStars;
        public int bestWaveReached;
    }

    [Serializable]
    public class CampProgressSavePayload
    {
        public int version = 2;
        public int gold = 180;
        public List<ConsumableStockEntry> consumables = new List<ConsumableStockEntry>();
        public List<CampBuffType> activeBuffs = new List<CampBuffType>();

        public CampaignStageId selectedStage = CampaignStageId.ForestChapter1;
        public QuizDifficultyFilter quizFilter = QuizDifficultyFilter.AllB1B2;
        public bool quizTimerEnabled = true;
        public bool cameraShakeEnabled = true;
        public bool tutorialCompleted = false;
        public int mistakesReviewedCount = 0;

        public List<HeroProgressEntry> heroProgress = new List<HeroProgressEntry>();
        public List<StageProgressEntry> stageProgress = new List<StageProgressEntry>();
        public List<string> mistakeQuestions = new List<string>();
        public List<AchievementId> unlockedAchievements = new List<AchievementId>();
        public List<AchievementId> claimedAchievements = new List<AchievementId>();
    }

    public readonly struct ShopConsumableOffer
    {
        public readonly ConsumableItemType itemType;
        public readonly int price;
        public readonly int maxStock;

        public ShopConsumableOffer(ConsumableItemType itemType, int price, int maxStock = 9)
        {
            this.itemType = itemType;
            this.price = price;
            this.maxStock = maxStock;
        }
    }

    public readonly struct ShopCampBuffOffer
    {
        public readonly CampBuffType buffType;
        public readonly string title;
        public readonly string description;
        public readonly int price;

        public ShopCampBuffOffer(CampBuffType buffType, string title, string description, int price)
        {
            this.buffType = buffType;
            this.title = title;
            this.description = description;
            this.price = price;
        }
    }

    /// <summary>
    /// Quản lý toàn bộ tiến trình người chơi: Vàng, Túi thuốc dự trữ, Bùa 1 trận,
    /// Cấp độ & EXP của 3 anh hùng, Mở khóa & Số sao 4 bản đồ, Sổ tay câu sai Tiếng Anh B1/B2,
    /// Thành tựu và Tùy chọn lối chơi. Tự lưu/tải JSON cục bộ tại persistentDataPath.
    /// </summary>
    public class PlayerCampProgressManager
    {
        private static PlayerCampProgressManager _instance;
        public static PlayerCampProgressManager Instance => _instance ??= new PlayerCampProgressManager();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        private const string SaveFileName = "medieval_rpg_camp.json";
        public const int DefaultStartingGold = 180;
        public const int MaxHeroLevel = 20;

        public static readonly ShopConsumableOffer[] ConsumableOffers =
        {
            new ShopConsumableOffer(ConsumableItemType.HealthPotion,   40, 9),
            new ShopConsumableOffer(ConsumableItemType.ManaElixir,     35, 9),
            new ShopConsumableOffer(ConsumableItemType.PhoenixFeather, 90, 5),
            new ShopConsumableOffer(ConsumableItemType.WarElixir,      65, 5),
            new ShopConsumableOffer(ConsumableItemType.IronTonic,      55, 5)
        };

        public static readonly ShopCampBuffOffer[] CampBuffOffers =
        {
            new ShopCampBuffOffer(CampBuffType.VitalityBlessing, "Bùa Sinh Lực",  "Tăng +35 Máu Tối Đa cho cả 3 anh hùng suốt trận kế tiếp.",           60),
            new ShopCampBuffOffer(CampBuffType.FuryBlessing,     "Bùa Chiến Ý",  "Tăng +8 Sát Thương Vật Lý & Phép cho cả 3 anh hùng suốt trận kế tiếp.", 75),
            new ShopCampBuffOffer(CampBuffType.AegisBlessing,    "Bùa Hộ Mệnh",  "Tăng +6 Giáp, +6 Kháng Phép & +2 Tốc Độ cho cả đội suốt trận kế tiếp.",   65)
        };

        private int _gold = DefaultStartingGold;
        private readonly Dictionary<ConsumableItemType, int> _consumableStock = new Dictionary<ConsumableItemType, int>();
        private readonly HashSet<CampBuffType> _activeBuffs = new HashSet<CampBuffType>();
        private readonly Dictionary<CharacterClassType, HeroProgressEntry> _heroProgress = new Dictionary<CharacterClassType, HeroProgressEntry>();
        private readonly Dictionary<CampaignStageId, StageProgressEntry> _stageProgress = new Dictionary<CampaignStageId, StageProgressEntry>();
        private readonly List<string> _mistakeQuestions = new List<string>();
        private readonly HashSet<AchievementId> _unlockedAchievements = new HashSet<AchievementId>();
        private readonly HashSet<AchievementId> _claimedAchievements = new HashSet<AchievementId>();

        private CampaignStageId _selectedStage = CampaignStageId.ForestChapter1;
        private QuizDifficultyFilter _quizFilter = QuizDifficultyFilter.AllB1B2;
        private bool _quizTimerEnabled = true;
        private bool _cameraShakeEnabled = true;
        private bool _tutorialCompleted;
        private int _mistakesReviewedCount;
        private bool _isLoaded;
        private bool _disableDiskPersistence;

        public int Gold { get { LoadIfNeeded(); return _gold; } }
        public CampaignStageId SelectedStage { get { LoadIfNeeded(); return _selectedStage; } }
        public QuizDifficultyFilter SelectedQuizFilter { get { LoadIfNeeded(); return _quizFilter; } }
        public bool QuizTimerEnabled { get { LoadIfNeeded(); return _quizTimerEnabled; } }
        public bool CameraShakeEnabled { get { LoadIfNeeded(); return _cameraShakeEnabled; } }
        public bool TutorialCompleted { get { LoadIfNeeded(); return _tutorialCompleted; } }
        public int MistakesReviewedCount { get { LoadIfNeeded(); return _mistakesReviewedCount; } }

        public IReadOnlyList<string> MistakeQuestions
        {
            get { LoadIfNeeded(); return _mistakeQuestions; }
        }

        public int MistakeCount { get { LoadIfNeeded(); return _mistakeQuestions.Count; } }

        public int ActiveBuffCount { get { LoadIfNeeded(); return _activeBuffs.Count; } }

        public int ClaimableAchievementCount
        {
            get
            {
                LoadIfNeeded();
                int count = 0;
                foreach (AchievementId id in _unlockedAchievements)
                {
                    if (!_claimedAchievements.Contains(id)) count++;
                }
                return count;
            }
        }

        public event Action OnCampProgressChanged;

        public static PlayerCampProgressManager EnsureLoaded()
        {
            PlayerCampProgressManager mgr = Instance;
            mgr.LoadIfNeeded();
            return mgr;
        }

        public void InitializeForTests(int startingGold = DefaultStartingGold)
        {
            _disableDiskPersistence = true;
            ResetStateInMemory(startingGold);
            _isLoaded = true;
            OnCampProgressChanged?.Invoke();
        }

        private void ResetStateInMemory(int startingGold)
        {
            _gold = Mathf.Max(0, startingGold);
            ResetDefaultStocks();
            _activeBuffs.Clear();
            ResetHeroProgressDefaults();
            ResetStageProgressDefaults();
            _mistakeQuestions.Clear();
            _unlockedAchievements.Clear();
            _claimedAchievements.Clear();
            _selectedStage = CampaignStageId.ForestChapter1;
            _quizFilter = QuizDifficultyFilter.AllB1B2;
            _quizTimerEnabled = true;
            _cameraShakeEnabled = true;
            _tutorialCompleted = false;
            _mistakesReviewedCount = 0;
        }

        private void ResetDefaultStocks()
        {
            _consumableStock[ConsumableItemType.HealthPotion] = 3;
            _consumableStock[ConsumableItemType.ManaElixir] = 2;
            _consumableStock[ConsumableItemType.PhoenixFeather] = 1;
            _consumableStock[ConsumableItemType.WarElixir] = 0;
            _consumableStock[ConsumableItemType.IronTonic] = 0;
        }

        private void ResetHeroProgressDefaults()
        {
            _heroProgress.Clear();
            foreach (CharacterClassType cls in (CharacterClassType[])Enum.GetValues(typeof(CharacterClassType)))
            {
                _heroProgress[cls] = new HeroProgressEntry { classType = cls, level = 1, exp = 0 };
            }
        }

        private void ResetStageProgressDefaults()
        {
            _stageProgress.Clear();
            foreach (CampaignStageId st in (CampaignStageId[])Enum.GetValues(typeof(CampaignStageId)))
            {
                _stageProgress[st] = new StageProgressEntry
                {
                    stageId = st,
                    unlocked = st == CampaignStageId.ForestChapter1,
                    cleared = false,
                    bestStars = 0,
                    bestWaveReached = 0
                };
            }
        }

        public void LoadIfNeeded()
        {
            if (_isLoaded) return;
            _isLoaded = true;
            ResetStateInMemory(DefaultStartingGold);

            if (_disableDiskPersistence) return;
            string path = GetSavePath();
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                CampProgressSavePayload payload = JsonUtility.FromJson<CampProgressSavePayload>(json);
                if (payload == null) return;

                _gold = Mathf.Max(0, payload.gold);
                _selectedStage = payload.selectedStage;
                _quizFilter = payload.quizFilter;
                _quizTimerEnabled = payload.version < 2 ? true : payload.quizTimerEnabled;
                _cameraShakeEnabled = payload.version < 2 ? true : payload.cameraShakeEnabled;
                _tutorialCompleted = payload.tutorialCompleted;
                _mistakesReviewedCount = Mathf.Max(0, payload.mistakesReviewedCount);

                if (payload.consumables != null && payload.consumables.Count > 0)
                {
                    foreach (ConsumableStockEntry entry in payload.consumables)
                    {
                        _consumableStock[entry.itemType] = Mathf.Max(0, entry.count);
                    }
                }
                if (payload.activeBuffs != null)
                {
                    foreach (CampBuffType buff in payload.activeBuffs) _activeBuffs.Add(buff);
                }
                if (payload.heroProgress != null)
                {
                    foreach (HeroProgressEntry hp in payload.heroProgress)
                    {
                        if (hp == null) continue;
                        _heroProgress[hp.classType] = new HeroProgressEntry
                        {
                            classType = hp.classType,
                            level = Mathf.Clamp(hp.level, 1, MaxHeroLevel),
                            exp = Mathf.Max(0, hp.exp)
                        };
                    }
                }
                if (payload.stageProgress != null)
                {
                    foreach (StageProgressEntry sp in payload.stageProgress)
                    {
                        if (sp == null) continue;
                        _stageProgress[sp.stageId] = new StageProgressEntry
                        {
                            stageId = sp.stageId,
                            unlocked = sp.unlocked || sp.stageId == CampaignStageId.ForestChapter1,
                            cleared = sp.cleared,
                            bestStars = Mathf.Clamp(sp.bestStars, 0, 3),
                            bestWaveReached = Mathf.Max(0, sp.bestWaveReached)
                        };
                    }
                }
                if (!_stageProgress.TryGetValue(_selectedStage, out StageProgressEntry sel) || !sel.unlocked)
                {
                    _selectedStage = CampaignStageId.ForestChapter1;
                }
                if (payload.mistakeQuestions != null)
                {
                    foreach (string q in payload.mistakeQuestions)
                    {
                        if (!string.IsNullOrWhiteSpace(q) && !_mistakeQuestions.Contains(q))
                            _mistakeQuestions.Add(q);
                    }
                }
                if (payload.unlockedAchievements != null)
                {
                    foreach (AchievementId a in payload.unlockedAchievements) _unlockedAchievements.Add(a);
                }
                if (payload.claimedAchievements != null)
                {
                    foreach (AchievementId a in payload.claimedAchievements) _claimedAchievements.Add(a);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerCampProgressManager] Không thể đọc file tiến trình: {ex.Message}");
            }
        }

        public void SaveToDisk()
        {
            if (_disableDiskPersistence) return;
            try
            {
                CampProgressSavePayload payload = new CampProgressSavePayload
                {
                    version = 2,
                    gold = _gold,
                    selectedStage = _selectedStage,
                    quizFilter = _quizFilter,
                    quizTimerEnabled = _quizTimerEnabled,
                    cameraShakeEnabled = _cameraShakeEnabled,
                    tutorialCompleted = _tutorialCompleted,
                    mistakesReviewedCount = _mistakesReviewedCount
                };
                foreach (var kv in _consumableStock)
                    payload.consumables.Add(new ConsumableStockEntry { itemType = kv.Key, count = kv.Value });
                foreach (CampBuffType b in _activeBuffs) payload.activeBuffs.Add(b);
                foreach (var kv in _heroProgress) payload.heroProgress.Add(kv.Value);
                foreach (var kv in _stageProgress) payload.stageProgress.Add(kv.Value);
                payload.mistakeQuestions.AddRange(_mistakeQuestions);
                foreach (AchievementId a in _unlockedAchievements) payload.unlockedAchievements.Add(a);
                foreach (AchievementId a in _claimedAchievements) payload.claimedAchievements.Add(a);

                File.WriteAllText(GetSavePath(), JsonUtility.ToJson(payload, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerCampProgressManager] Không thể lưu tiến trình: {ex.Message}");
            }
        }

        private static string GetSavePath() => Path.Combine(Application.persistentDataPath, SaveFileName);

        // =====================================================================
        // VÀNG, SHOP & BÙA LỢI
        // =====================================================================
        public int GetConsumableCount(ConsumableItemType type)
        {
            LoadIfNeeded();
            return _consumableStock.TryGetValue(type, out int c) ? c : 0;
        }

        public bool IsCampBuffActive(CampBuffType buff)
        {
            LoadIfNeeded();
            return _activeBuffs.Contains(buff);
        }

        public void AddGold(int amount)
        {
            LoadIfNeeded();
            if (amount <= 0) return;
            _gold += amount;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        public bool TrySpendGold(int amount)
        {
            LoadIfNeeded();
            if (amount < 0 || _gold < amount) return false;
            _gold -= amount;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
            return true;
        }

        public bool TryBuyConsumable(ConsumableItemType type)
        {
            LoadIfNeeded();
            int price = 0, maxStock = 9;
            bool found = false;
            for (int i = 0; i < ConsumableOffers.Length; i++)
            {
                if (ConsumableOffers[i].itemType == type)
                {
                    price = ConsumableOffers[i].price;
                    maxStock = ConsumableOffers[i].maxStock;
                    found = true;
                    break;
                }
            }
            if (!found) return false;

            int cur = GetConsumableCount(type);
            if (_gold < price || cur >= maxStock) return false;

            _gold -= price;
            _consumableStock[type] = cur + 1;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
            return true;
        }

        public bool TryBuyCampBuff(CampBuffType buff)
        {
            LoadIfNeeded();
            if (_activeBuffs.Contains(buff)) return false;

            int price = 0;
            bool found = false;
            for (int i = 0; i < CampBuffOffers.Length; i++)
            {
                if (CampBuffOffers[i].buffType == buff)
                {
                    price = CampBuffOffers[i].price;
                    found = true;
                    break;
                }
            }
            if (!found || _gold < price) return false;

            _gold -= price;
            _activeBuffs.Add(buff);
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
            return true;
        }

        public EquipmentStatBonus GetActiveCampBonusTotal()
        {
            LoadIfNeeded();
            EquipmentStatBonus bonus = new EquipmentStatBonus();
            if (_activeBuffs.Contains(CampBuffType.VitalityBlessing)) bonus.maxHP += 35;
            if (_activeBuffs.Contains(CampBuffType.FuryBlessing))
            {
                bonus.physicalAttack += 8;
                bonus.magicAttack += 8;
            }
            if (_activeBuffs.Contains(CampBuffType.AegisBlessing))
            {
                bonus.physicalDefense += 6;
                bonus.magicDefense += 6;
                bonus.speed += 2;
            }
            return bonus;
        }

        public string GetActiveBuffsShortSummary()
        {
            LoadIfNeeded();
            if (_activeBuffs.Count == 0) return "Chưa bật bùa hỗ trợ";
            List<string> labels = new List<string>(3);
            if (_activeBuffs.Contains(CampBuffType.VitalityBlessing)) labels.Add("+35 HP");
            if (_activeBuffs.Contains(CampBuffType.FuryBlessing)) labels.Add("+8 ATK/MAG");
            if (_activeBuffs.Contains(CampBuffType.AegisBlessing)) labels.Add("+6 DEF/+2 SPD");
            return string.Join(" • ", labels);
        }

        public void ApplyToBattleInventory(BattleInventory inventory)
        {
            if (inventory == null) return;
            LoadIfNeeded();
            inventory.Reset(
                GetConsumableCount(ConsumableItemType.HealthPotion),
                GetConsumableCount(ConsumableItemType.ManaElixir),
                GetConsumableCount(ConsumableItemType.PhoenixFeather),
                GetConsumableCount(ConsumableItemType.WarElixir),
                GetConsumableCount(ConsumableItemType.IronTonic));
        }

        public void SyncAfterBattle(BattleInventory battleInventory, bool consumeActiveBuffs)
        {
            LoadIfNeeded();
            if (battleInventory != null)
            {
                _consumableStock[ConsumableItemType.HealthPotion] = Mathf.Max(2, battleInventory.GetCount(ConsumableItemType.HealthPotion));
                _consumableStock[ConsumableItemType.ManaElixir] = Mathf.Max(1, battleInventory.GetCount(ConsumableItemType.ManaElixir));
                _consumableStock[ConsumableItemType.PhoenixFeather] = Mathf.Max(1, battleInventory.GetCount(ConsumableItemType.PhoenixFeather));
                _consumableStock[ConsumableItemType.WarElixir] = Mathf.Max(0, battleInventory.GetCount(ConsumableItemType.WarElixir));
                _consumableStock[ConsumableItemType.IronTonic] = Mathf.Max(0, battleInventory.GetCount(ConsumableItemType.IronTonic));
            }
            if (consumeActiveBuffs) _activeBuffs.Clear();
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        // =====================================================================
        // CẤP ĐỘ & KINH NGHIỆM 3 ANH HÙNG (LEVEL & EXP)
        // =====================================================================
        public static int GetExpRequiredForNextLevel(int currentLevel)
        {
            return 100 + Mathf.Max(0, currentLevel - 1) * 65;
        }

        public HeroProgressEntry GetHeroProgress(CharacterClassType cls)
        {
            LoadIfNeeded();
            if (!_heroProgress.TryGetValue(cls, out HeroProgressEntry entry))
            {
                entry = new HeroProgressEntry { classType = cls, level = 1, exp = 0 };
                _heroProgress[cls] = entry;
            }
            return entry;
        }

        public float GetHeroExpProgress01(CharacterClassType cls)
        {
            HeroProgressEntry entry = GetHeroProgress(cls);
            if (entry.level >= MaxHeroLevel) return 1f;
            int req = GetExpRequiredForNextLevel(entry.level);
            return req > 0 ? Mathf.Clamp01((float)entry.exp / req) : 0f;
        }

        public EquipmentStatBonus GetLevelBonusForClass(CharacterClassType cls)
        {
            int lv = Mathf.Max(1, GetHeroProgress(cls).level);
            int ups = lv - 1;
            if (ups <= 0) return new EquipmentStatBonus();

            return cls switch
            {
                CharacterClassType.Knight => new EquipmentStatBonus
                {
                    maxHP = ups * 18,
                    maxMP = ups * 3,
                    physicalAttack = ups * 3,
                    physicalDefense = ups * 2,
                    magicDefense = ups * 1
                },
                CharacterClassType.Mage => new EquipmentStatBonus
                {
                    maxHP = ups * 10,
                    maxMP = ups * 12,
                    magicAttack = ups * 4,
                    magicDefense = ups * 2
                },
                CharacterClassType.Archer => new EquipmentStatBonus
                {
                    maxHP = ups * 12,
                    maxMP = ups * 5,
                    physicalAttack = ups * 4,
                    speed = ups / 2,
                    critRate = ups * 0.01f
                },
                _ => new EquipmentStatBonus()
            };
        }

        public void AwardPartyExp(int expAmount, out List<CharacterClassType> leveledUpClasses)
        {
            leveledUpClasses = new List<CharacterClassType>();
            LoadIfNeeded();
            if (expAmount <= 0) return;

            foreach (CharacterClassType cls in (CharacterClassType[])Enum.GetValues(typeof(CharacterClassType)))
            {
                HeroProgressEntry entry = GetHeroProgress(cls);
                if (entry.level >= MaxHeroLevel) continue;

                entry.exp += expAmount;
                bool gainedLevel = false;
                while (entry.level < MaxHeroLevel)
                {
                    int req = GetExpRequiredForNextLevel(entry.level);
                    if (entry.exp < req) break;
                    entry.exp -= req;
                    entry.level++;
                    gainedLevel = true;
                }
                if (entry.level >= MaxHeroLevel) entry.exp = 0;
                if (gainedLevel) leveledUpClasses.Add(cls);
            }

            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        // =====================================================================
        // BẢN ĐỒ CHIẾN DỊCH, SAO & MỞ KHÓA
        // =====================================================================
        public StageProgressEntry GetStageProgress(CampaignStageId id)
        {
            LoadIfNeeded();
            if (!_stageProgress.TryGetValue(id, out StageProgressEntry entry))
            {
                entry = new StageProgressEntry
                {
                    stageId = id,
                    unlocked = id == CampaignStageId.ForestChapter1
                };
                _stageProgress[id] = entry;
            }
            return entry;
        }

        public bool SelectStage(CampaignStageId id)
        {
            LoadIfNeeded();
            StageProgressEntry entry = GetStageProgress(id);
            if (!entry.unlocked) return false;
            _selectedStage = id;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
            return true;
        }

        public void RecordStageResult(CampaignStageId id, bool won, int waveReached, int stars, out int firstClearGoldBonus, out bool unlockedNewStage)
        {
            firstClearGoldBonus = 0;
            unlockedNewStage = false;
            LoadIfNeeded();

            StageProgressEntry entry = GetStageProgress(id);
            entry.bestWaveReached = Mathf.Max(entry.bestWaveReached, waveReached);

            if (won && !StageCatalog.GetStage(id).isEndless)
            {
                bool firstTime = !entry.cleared;
                entry.cleared = true;
                entry.bestStars = Mathf.Max(entry.bestStars, Mathf.Clamp(stars, 1, 3));

                if (firstTime)
                {
                    firstClearGoldBonus = StageCatalog.GetStage(id).firstClearBonusGold;
                    if (firstClearGoldBonus > 0) _gold += firstClearGoldBonus;
                }

                if (id == CampaignStageId.ForestChapter1)
                {
                    UnlockAchievement(AchievementId.FirstChapterClear, saveImmediate: false);
                    StageProgressEntry ch2 = GetStageProgress(CampaignStageId.CryptChapter2);
                    if (!ch2.unlocked) { ch2.unlocked = true; unlockedNewStage = true; }
                }
                else if (id == CampaignStageId.CryptChapter2)
                {
                    StageProgressEntry ch3 = GetStageProgress(CampaignStageId.CitadelChapter3);
                    StageProgressEntry endless = GetStageProgress(CampaignStageId.EndlessTower);
                    if (!ch3.unlocked) { ch3.unlocked = true; unlockedNewStage = true; }
                    if (!endless.unlocked) { endless.unlocked = true; unlockedNewStage = true; }
                }
                else if (id == CampaignStageId.CitadelChapter3)
                {
                    UnlockAchievement(AchievementId.ChapterThreeClear, saveImmediate: false);
                }
            }

            if (id == CampaignStageId.EndlessTower && waveReached >= 10)
            {
                UnlockAchievement(AchievementId.EndlessWaveTen, saveImmediate: false);
            }

            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        // =====================================================================
        // QUIZ TIẾNG ANH B1/B2, SỔ TAY LỖI SAI & THÀNH TỰU
        // =====================================================================
        public void SetQuizFilter(QuizDifficultyFilter filter)
        {
            LoadIfNeeded();
            _quizFilter = filter;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        public QuizDifficultyFilter CycleQuizFilter()
        {
            LoadIfNeeded();
            _quizFilter = (QuizDifficultyFilter)(((int)_quizFilter + 1) % 3);
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
            return _quizFilter;
        }

        public static string GetQuizFilterDisplayName(QuizDifficultyFilter f) => f switch
        {
            QuizDifficultyFilter.OnlyB1 => "Chỉ B1",
            QuizDifficultyFilter.OnlyB2 => "Chỉ B2",
            _ => "B1 + B2"
        };

        public void SetGameplayPreferences(bool timerEnabled, bool cameraShakeEnabled)
        {
            LoadIfNeeded();
            _quizTimerEnabled = timerEnabled;
            _cameraShakeEnabled = cameraShakeEnabled;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        public void MarkTutorialCompleted()
        {
            LoadIfNeeded();
            if (_tutorialCompleted) return;
            _tutorialCompleted = true;
            SaveToDisk();
        }

        public void RecordQuizMistake(string questionText)
        {
            LoadIfNeeded();
            if (string.IsNullOrWhiteSpace(questionText)) return;
            if (_mistakeQuestions.Contains(questionText)) return;
            _mistakeQuestions.Add(questionText);
            if (_mistakeQuestions.Count > 40) _mistakeQuestions.RemoveAt(0);
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        public bool ResolveQuizMistake(string questionText)
        {
            LoadIfNeeded();
            if (!_mistakeQuestions.Remove(questionText)) return false;
            _mistakesReviewedCount++;
            if (_mistakesReviewedCount >= 3)
            {
                UnlockAchievement(AchievementId.MistakeReviewer, saveImmediate: false);
            }
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
            return true;
        }

        public bool IsAchievementUnlocked(AchievementId id)
        {
            LoadIfNeeded();
            return _unlockedAchievements.Contains(id);
        }

        public bool IsAchievementClaimed(AchievementId id)
        {
            LoadIfNeeded();
            return _claimedAchievements.Contains(id);
        }

        public bool UnlockAchievement(AchievementId id, bool saveImmediate = true)
        {
            LoadIfNeeded();
            if (!_unlockedAchievements.Add(id)) return false;
            if (saveImmediate)
            {
                SaveToDisk();
                OnCampProgressChanged?.Invoke();
            }
            return true;
        }

        public bool TryClaimAchievement(AchievementId id, out int goldReward)
        {
            goldReward = 0;
            LoadIfNeeded();
            if (!_unlockedAchievements.Contains(id) || _claimedAchievements.Contains(id)) return false;

            goldReward = AchievementCatalog.Get(id).goldReward;
            _claimedAchievements.Add(id);
            _gold += goldReward;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
            return true;
        }

        public void ResetAllProgress(bool alsoClearEquipment = true)
        {
            ResetStateInMemory(DefaultStartingGold);
            _isLoaded = true;
            if (!_disableDiskPersistence)
            {
                string path = GetSavePath();
                if (File.Exists(path)) File.Delete(path);
            }
            if (alsoClearEquipment)
            {
                PlayerEquipmentManager.EnsureLoaded().ClearAllForReset();
            }
            OnCampProgressChanged?.Invoke();
        }
    }
}
