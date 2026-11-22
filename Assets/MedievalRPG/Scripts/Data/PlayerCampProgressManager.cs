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
    public class CampProgressSavePayload
    {
        public int version = 1;
        public int gold = 180;
        public List<ConsumableStockEntry> consumables = new List<ConsumableStockEntry>();
        public List<CampBuffType> activeBuffs = new List<CampBuffType>();
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
    /// Quản lý Vàng (Gold), kho thuốc/vật phẩm tiêu hao dự trữ và Bùa Lợi chuẩn bị trận
    /// mua tại Sảnh Chờ Tiền Đồn (CampHub). Lưu tự động ra JSON cục bộ.
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
            new ShopCampBuffOffer(CampBuffType.VitalityBlessing, "Bùa Sinh Lực",  "Tăng +35 Máu Tối Đa cho cả 3 anh hùng suốt trận kế tiếp.",             60),
            new ShopCampBuffOffer(CampBuffType.FuryBlessing,     "Bùa Chiến Ý",  "Tăng +8 Sát Thương Vật Lý & Phép cho cả 3 anh hùng suốt trận kế tiếp.",   75),
            new ShopCampBuffOffer(CampBuffType.AegisBlessing,    "Bùa Hộ Mệnh",  "Tăng +6 Giáp, +6 Kháng Phép & +2 Tốc Độ cho cả đội suốt trận kế tiếp.",     65)
        };

        private int _gold = DefaultStartingGold;
        private readonly Dictionary<ConsumableItemType, int> _consumableStock = new Dictionary<ConsumableItemType, int>();
        private readonly HashSet<CampBuffType> _activeBuffs = new HashSet<CampBuffType>();
        private bool _isLoaded;
        private bool _disableDiskPersistence;

        public int Gold
        {
            get
            {
                LoadIfNeeded();
                return _gold;
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
            _gold = Mathf.Max(0, startingGold);
            ResetDefaultStocks();
            _activeBuffs.Clear();
            _isLoaded = true;
            OnCampProgressChanged?.Invoke();
        }

        private void ResetDefaultStocks()
        {
            _consumableStock[ConsumableItemType.HealthPotion] = 3;
            _consumableStock[ConsumableItemType.ManaElixir] = 2;
            _consumableStock[ConsumableItemType.PhoenixFeather] = 1;
            _consumableStock[ConsumableItemType.WarElixir] = 0;
            _consumableStock[ConsumableItemType.IronTonic] = 0;
        }

        public void LoadIfNeeded()
        {
            if (_isLoaded) return;
            _isLoaded = true;
            _gold = DefaultStartingGold;
            ResetDefaultStocks();
            _activeBuffs.Clear();

            if (_disableDiskPersistence) return;
            string path = GetSavePath();
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                CampProgressSavePayload payload = JsonUtility.FromJson<CampProgressSavePayload>(json);
                if (payload == null) return;

                _gold = Mathf.Max(0, payload.gold);
                if (payload.consumables != null && payload.consumables.Count > 0)
                {
                    foreach (ConsumableStockEntry entry in payload.consumables)
                    {
                        _consumableStock[entry.itemType] = Mathf.Max(0, entry.count);
                    }
                }
                if (payload.activeBuffs != null)
                {
                    foreach (CampBuffType buff in payload.activeBuffs)
                    {
                        _activeBuffs.Add(buff);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerCampProgressManager] Không thể đọc file sảnh chờ: {ex.Message}");
            }
        }

        public void SaveToDisk()
        {
            if (_disableDiskPersistence) return;
            try
            {
                CampProgressSavePayload payload = new CampProgressSavePayload
                {
                    gold = _gold
                };
                foreach (KeyValuePair<ConsumableItemType, int> kv in _consumableStock)
                {
                    payload.consumables.Add(new ConsumableStockEntry { itemType = kv.Key, count = kv.Value });
                }
                foreach (CampBuffType b in _activeBuffs)
                {
                    payload.activeBuffs.Add(b);
                }
                File.WriteAllText(GetSavePath(), JsonUtility.ToJson(payload, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerCampProgressManager] Không thể lưu file sảnh chờ: {ex.Message}");
            }
        }

        private static string GetSavePath() => Path.Combine(Application.persistentDataPath, SaveFileName);

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

        public int ActiveBuffCount
        {
            get
            {
                LoadIfNeeded();
                return _activeBuffs.Count;
            }
        }

        public void AddGold(int amount)
        {
            LoadIfNeeded();
            if (amount <= 0) return;
            _gold += amount;
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }

        public bool TryBuyConsumable(ConsumableItemType type)
        {
            LoadIfNeeded();
            int price = 0;
            int maxStock = 9;
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
            if (_activeBuffs.Contains(CampBuffType.VitalityBlessing))
            {
                bonus.maxHP += 35;
            }
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

        /// <summary>Đồng bộ số vật phẩm còn lại sau trận, đồng thời đảm bảo đội luôn có tối thiểu thuốc cơ bản để không bị kẹt ở lần sau.</summary>
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
            if (consumeActiveBuffs)
            {
                _activeBuffs.Clear();
            }
            SaveToDisk();
            OnCampProgressChanged?.Invoke();
        }
    }
}
