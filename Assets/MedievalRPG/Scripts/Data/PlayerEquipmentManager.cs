using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MedievalRPG.Data
{
    [Serializable]
    public class OwnedEquipmentInstance
    {
        public string instanceId;
        public string itemId;
        public int obtainedWave;
        public bool isNew;
        public int upgradeLevel;
    }

    [Serializable]
    public class EquippedSlotEntry
    {
        public CharacterClassType classType;
        public EquipmentSlot slot;
        public string instanceId;
    }

    [Serializable]
    public class EquipmentSavePayload
    {
        public int version = 1;
        public List<OwnedEquipmentInstance> ownedItems = new List<OwnedEquipmentInstance>();
        public List<EquippedSlotEntry> equippedSlots = new List<EquippedSlotEntry>();
    }

    /// <summary>
    /// Quản lý kho trang bị của người chơi và các món đang mặc cho cả 3 Class (Knight, Mage, Archer).
    /// Tự động lưu/tải JSON cục bộ tại persistentDataPath để giữ chiến lợi phẩm giữa các lượt chơi.
    /// </summary>
    public class PlayerEquipmentManager
    {
        private static PlayerEquipmentManager _instance;
        public static PlayerEquipmentManager Instance => _instance ??= new PlayerEquipmentManager();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        private const string SaveFileName = "medieval_rpg_equipment.json";

        private EquipmentCatalog _catalog;
        private readonly List<OwnedEquipmentInstance> _ownedItems = new List<OwnedEquipmentInstance>();
        private readonly Dictionary<(CharacterClassType, EquipmentSlot), string> _equippedMap =
            new Dictionary<(CharacterClassType, EquipmentSlot), string>();

        private bool _isLoaded;
        private bool _disableDiskPersistence;

        public EquipmentCatalog Catalog
        {
            get
            {
                EnsureCatalog();
                return _catalog;
            }
        }

        public int OwnedCount => _ownedItems.Count;

        public int UnseenNewCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _ownedItems.Count; i++)
                {
                    if (_ownedItems[i].isNew) count++;
                }
                return count;
            }
        }

        public event Action OnEquipmentChanged;

        public static PlayerEquipmentManager EnsureLoaded()
        {
            PlayerEquipmentManager mgr = Instance;
            mgr.LoadIfNeeded();
            return mgr;
        }

        public void InitializeForTests(EquipmentCatalog testCatalog)
        {
            _disableDiskPersistence = true;
            _catalog = testCatalog != null ? testCatalog : EquipmentCatalog.CreateRuntimeFallbackCatalog();
            _ownedItems.Clear();
            _equippedMap.Clear();
            _isLoaded = true;
        }

        private void EnsureCatalog()
        {
            if (_catalog != null) return;
            _catalog = Resources.Load<EquipmentCatalog>("MedievalEquipmentCatalog");
            if (_catalog == null || _catalog.allItems == null || _catalog.allItems.Count == 0)
            {
                _catalog = EquipmentCatalog.CreateRuntimeFallbackCatalog();
            }
            _catalog.RebuildLookup();
        }

        public void LoadIfNeeded()
        {
            if (_isLoaded) return;
            EnsureCatalog();
            _isLoaded = true;

            if (_disableDiskPersistence) return;
            string path = GetSavePath();
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                EquipmentSavePayload payload = JsonUtility.FromJson<EquipmentSavePayload>(json);
                if (payload == null) return;

                _ownedItems.Clear();
                _equippedMap.Clear();

                if (payload.ownedItems != null)
                {
                    foreach (OwnedEquipmentInstance inst in payload.ownedItems)
                    {
                        if (inst == null || string.IsNullOrEmpty(inst.itemId)) continue;
                        if (_catalog.GetById(inst.itemId) == null) continue;
                        if (string.IsNullOrEmpty(inst.instanceId)) inst.instanceId = Guid.NewGuid().ToString("N");
                        _ownedItems.Add(inst);
                    }
                }

                if (payload.equippedSlots != null)
                {
                    foreach (EquippedSlotEntry entry in payload.equippedSlots)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.instanceId)) continue;
                        if (FindOwnedByInstanceId(entry.instanceId) != null)
                        {
                            _equippedMap[(entry.classType, entry.slot)] = entry.instanceId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerEquipmentManager] Không thể đọc file kho đồ: {ex.Message}");
            }
        }

        public void SaveToDisk()
        {
            if (_disableDiskPersistence) return;
            try
            {
                EquipmentSavePayload payload = new EquipmentSavePayload();
                payload.ownedItems.AddRange(_ownedItems);
                foreach (KeyValuePair<(CharacterClassType, EquipmentSlot), string> kv in _equippedMap)
                {
                    if (string.IsNullOrEmpty(kv.Value)) continue;
                    payload.equippedSlots.Add(new EquippedSlotEntry
                    {
                        classType = kv.Key.Item1,
                        slot = kv.Key.Item2,
                        instanceId = kv.Value
                    });
                }

                string json = JsonUtility.ToJson(payload, true);
                File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerEquipmentManager] Không thể lưu kho đồ: {ex.Message}");
            }
        }

        private static string GetSavePath() => Path.Combine(Application.persistentDataPath, SaveFileName);

        public OwnedEquipmentInstance AddItem(EquipmentItemData item, int obtainedWave = 1)
        {
            LoadIfNeeded();
            if (item == null || string.IsNullOrEmpty(item.itemId)) return null;

            OwnedEquipmentInstance inst = new OwnedEquipmentInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                itemId = item.itemId,
                obtainedWave = obtainedWave,
                isNew = true
            };
            _ownedItems.Add(inst);
            SaveToDisk();
            OnEquipmentChanged?.Invoke();
            return inst;
        }

        public void MarkAllSeen()
        {
            LoadIfNeeded();
            bool changed = false;
            for (int i = 0; i < _ownedItems.Count; i++)
            {
                if (_ownedItems[i].isNew)
                {
                    _ownedItems[i].isNew = false;
                    changed = true;
                }
            }
            if (changed)
            {
                SaveToDisk();
                OnEquipmentChanged?.Invoke();
            }
        }

        public EquipmentItemData ResolveData(OwnedEquipmentInstance inst)
        {
            if (inst == null) return null;
            return Catalog.GetById(inst.itemId);
        }

        public OwnedEquipmentInstance FindOwnedByInstanceId(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            for (int i = 0; i < _ownedItems.Count; i++)
            {
                if (_ownedItems[i].instanceId == instanceId) return _ownedItems[i];
            }
            return null;
        }

        public bool IsEquipped(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return false;
            foreach (string eqId in _equippedMap.Values)
            {
                if (eqId == instanceId) return true;
            }
            return false;
        }

        public OwnedEquipmentInstance GetEquippedInstance(CharacterClassType cls, EquipmentSlot slot)
        {
            LoadIfNeeded();
            if (_equippedMap.TryGetValue((cls, slot), out string instId))
            {
                return FindOwnedByInstanceId(instId);
            }
            return null;
        }

        public EquipmentItemData GetEquippedItemData(CharacterClassType cls, EquipmentSlot slot)
        {
            return ResolveData(GetEquippedInstance(cls, slot));
        }

        public bool Equip(string instanceId)
        {
            LoadIfNeeded();
            OwnedEquipmentInstance inst = FindOwnedByInstanceId(instanceId);
            EquipmentItemData data = ResolveData(inst);
            if (inst == null || data == null) return false;

            _equippedMap[(data.requiredClass, data.slot)] = inst.instanceId;
            inst.isNew = false;
            SaveToDisk();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool Unequip(CharacterClassType cls, EquipmentSlot slot)
        {
            LoadIfNeeded();
            if (!_equippedMap.Remove((cls, slot))) return false;
            SaveToDisk();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        /// <summary>Tự động trang bị món có điểm sức mạnh cao nhất cho toàn bộ 3 Class x 3 Slot.</summary>
        public int AutoEquipBestForAllClasses()
        {
            LoadIfNeeded();
            int changedCount = 0;

            foreach (CharacterClassType cls in (CharacterClassType[])Enum.GetValues(typeof(CharacterClassType)))
            {
                foreach (EquipmentSlot slot in (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot)))
                {
                    OwnedEquipmentInstance best = null;
                    int bestScore = int.MinValue;

                    for (int i = 0; i < _ownedItems.Count; i++)
                    {
                        OwnedEquipmentInstance cand = _ownedItems[i];
                        EquipmentItemData data = ResolveData(cand);
                        if (data == null || data.requiredClass != cls || data.slot != slot) continue;

                        int score = data.GetPowerScore(cand.upgradeLevel);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = cand;
                        }
                    }

                    if (best != null)
                    {
                        _equippedMap.TryGetValue((cls, slot), out string currentId);
                        if (currentId != best.instanceId)
                        {
                            _equippedMap[(cls, slot)] = best.instanceId;
                            best.isNew = false;
                            changedCount++;
                        }
                    }
                }
            }

            if (changedCount > 0)
            {
                SaveToDisk();
                OnEquipmentChanged?.Invoke();
            }
            return changedCount;
        }

        public EquipmentStatBonus GetTotalBonusesForClass(CharacterClassType cls)
        {
            LoadIfNeeded();
            EquipmentStatBonus total = new EquipmentStatBonus();
            foreach (EquipmentSlot slot in (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot)))
            {
                OwnedEquipmentInstance inst = GetEquippedInstance(cls, slot);
                EquipmentItemData item = ResolveData(inst);
                if (item != null) total.Add(item.GetScaledStats(inst.upgradeLevel));
            }
            return total;
        }

        public bool SellItem(string instanceId, out int goldEarned)
        {
            goldEarned = 0;
            LoadIfNeeded();
            OwnedEquipmentInstance inst = FindOwnedByInstanceId(instanceId);
            EquipmentItemData data = ResolveData(inst);
            if (inst == null || data == null) return false;

            goldEarned = data.GetSellGoldValue(inst.upgradeLevel);
            if (IsEquipped(instanceId))
            {
                _equippedMap.Remove((data.requiredClass, data.slot));
            }
            _ownedItems.Remove(inst);
            SaveToDisk();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public int SellAllUnequippedByMaxRarity(ItemRarity maxRarity, out int totalGoldEarned)
        {
            totalGoldEarned = 0;
            LoadIfNeeded();
            int soldCount = 0;

            for (int i = _ownedItems.Count - 1; i >= 0; i--)
            {
                OwnedEquipmentInstance inst = _ownedItems[i];
                if (IsEquipped(inst.instanceId) || inst.upgradeLevel > 0) continue;
                EquipmentItemData data = ResolveData(inst);
                if (data == null || data.rarity > maxRarity) continue;

                totalGoldEarned += data.GetSellGoldValue(inst.upgradeLevel);
                _ownedItems.RemoveAt(i);
                soldCount++;
            }

            if (soldCount > 0)
            {
                SaveToDisk();
                OnEquipmentChanged?.Invoke();
            }
            return soldCount;
        }

        public bool TryUpgradeItem(string instanceId, int availableGold, out int goldSpent)
        {
            goldSpent = 0;
            LoadIfNeeded();
            OwnedEquipmentInstance inst = FindOwnedByInstanceId(instanceId);
            EquipmentItemData data = ResolveData(inst);
            if (inst == null || data == null || inst.upgradeLevel >= EquipmentItemData.MaxUpgradeLevel)
            {
                return false;
            }

            int cost = data.GetUpgradeCost(inst.upgradeLevel);
            if (availableGold < cost) return false;

            goldSpent = cost;
            inst.upgradeLevel++;
            inst.isNew = false;
            SaveToDisk();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public int CountUnequippedByRarity(ItemRarity rarity)
        {
            LoadIfNeeded();
            int count = 0;
            for (int i = 0; i < _ownedItems.Count; i++)
            {
                OwnedEquipmentInstance inst = _ownedItems[i];
                if (IsEquipped(inst.instanceId)) continue;
                EquipmentItemData data = ResolveData(inst);
                if (data != null && data.rarity == rarity) count++;
            }
            return count;
        }

        public bool TryForgeThreeIntoNextRarity(ItemRarity sourceRarity, out EquipmentItemData forgedItem, out OwnedEquipmentInstance createdInst)
        {
            forgedItem = null;
            createdInst = null;
            LoadIfNeeded();
            if (sourceRarity >= ItemRarity.Legendary) return false;

            List<OwnedEquipmentInstance> candidates = new List<OwnedEquipmentInstance>();
            for (int i = 0; i < _ownedItems.Count; i++)
            {
                OwnedEquipmentInstance inst = _ownedItems[i];
                if (IsEquipped(inst.instanceId)) continue;
                EquipmentItemData data = ResolveData(inst);
                if (data != null && data.rarity == sourceRarity)
                {
                    candidates.Add(inst);
                }
            }
            if (candidates.Count < 3) return false;

            // Ưu tiên ghép các món chưa cường hóa trước
            candidates.Sort((a, b) => a.upgradeLevel.CompareTo(b.upgradeLevel));

            ItemRarity nextRarity = sourceRarity + 1;
            List<EquipmentItemData> pool = new List<EquipmentItemData>();
            foreach (EquipmentItemData item in Catalog.allItems)
            {
                if (item != null && item.rarity == nextRarity) pool.Add(item);
            }
            if (pool.Count == 0) return false;

            for (int i = 0; i < 3; i++)
            {
                _ownedItems.Remove(candidates[i]);
            }

            forgedItem = pool[UnityEngine.Random.Range(0, pool.Count)];
            createdInst = new OwnedEquipmentInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                itemId = forgedItem.itemId,
                obtainedWave = 1,
                isNew = true,
                upgradeLevel = 0
            };
            _ownedItems.Add(createdInst);
            SaveToDisk();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public void ClearAllForReset()
        {
            _ownedItems.Clear();
            _equippedMap.Clear();
            _isLoaded = true;
            if (!_disableDiskPersistence)
            {
                string path = GetSavePath();
                if (File.Exists(path)) File.Delete(path);
            }
            OnEquipmentChanged?.Invoke();
        }

        public List<OwnedEquipmentInstance> GetOwnedItemsSorted(CharacterClassType? classFilter = null)
        {
            LoadIfNeeded();
            List<OwnedEquipmentInstance> list = new List<OwnedEquipmentInstance>();
            for (int i = 0; i < _ownedItems.Count; i++)
            {
                OwnedEquipmentInstance inst = _ownedItems[i];
                EquipmentItemData data = ResolveData(inst);
                if (data == null) continue;
                if (classFilter.HasValue && data.requiredClass != classFilter.Value) continue;
                list.Add(inst);
            }

            list.Sort((a, b) =>
            {
                bool eqA = IsEquipped(a.instanceId);
                bool eqB = IsEquipped(b.instanceId);
                if (eqA != eqB) return eqB.CompareTo(eqA);

                EquipmentItemData dA = ResolveData(a);
                EquipmentItemData dB = ResolveData(b);
                int rCmp = ((int)dB.rarity).CompareTo((int)dA.rarity);
                if (rCmp != 0) return rCmp;

                int pCmp = dB.GetPowerScore(b.upgradeLevel).CompareTo(dA.GetPowerScore(a.upgradeLevel));
                if (pCmp != 0) return pCmp;

                return string.CompareOrdinal(dA.itemName, dB.itemName);
            });

            return list;
        }
    }
}
