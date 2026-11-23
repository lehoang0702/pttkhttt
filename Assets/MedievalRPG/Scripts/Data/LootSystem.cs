using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    [Serializable]
    public struct LootDropResult
    {
        public EquipmentItemData equipment;
        public bool isConsumableOnly;
        public ConsumableItemType consumableType;
        public int waveNumber;
        public int totalWaves;
        public float dropChanceAtWave;

        public ItemRarity DisplayRarity => equipment != null ? equipment.rarity : (consumableType == ConsumableItemType.PhoenixFeather ? ItemRarity.Epic : ItemRarity.Common);

        public string DisplayName
        {
            get
            {
                if (equipment != null) return equipment.itemName;
                return BattleInventory.GetDisplayName(consumableType);
            }
        }
    }

    /// <summary>
    /// Công thức tính tỉ lệ rớt đồ và phân bổ độ hiếm (Thường / Hiếm / Sử Thi / Huyền Thoại).
    /// Càng đi xa theo số đợt trong bản đồ thì tỉ lệ drop và tỉ lệ ra đồ hiếm/sử thi/huyền thoại càng tăng rõ rệt.
    /// Boss cuối map luôn chắc chắn 100% rớt 2 trang bị cấp cao (tối thiểu từ Hiếm trở lên).
    /// </summary>
    public static class LootSystem
    {
        public static float GetWaveProgress(int waveIndex, int totalWaves)
        {
            if (totalWaves <= 1) return 1f;
            return Mathf.Clamp01((float)waveIndex / (totalWaves - 1));
        }

        /// <summary>
        /// Tỉ lệ rớt trang bị trên mỗi quái bị hạ gục (tăng tuyến tính từ 35% ở đợt đầu lên 90% ở đợt cuối, Boss = 100%).
        /// </summary>
        public static float CalculateEquipmentDropChance(int waveIndex, int totalWaves, bool isBoss, float waveDropBonus = 0f)
        {
            if (isBoss) return 1f;
            float progress = GetWaveProgress(waveIndex, totalWaves);
            return Mathf.Clamp01(0.35f + 0.55f * progress + Mathf.Max(0f, waveDropBonus));
        }

        /// <summary>
        /// Trả về 4 trọng số [Common, Rare, Epic, Legendary] có tổng = 100.
        /// Càng đi sâu (waveIndex lớn), xác suất Thường giảm mạnh còn Hiếm / Sử Thi / Huyền Thoại tăng vọt.
        /// </summary>
        public static float[] GetRarityWeights(int waveIndex, int totalWaves, bool isBoss, float extraLuck = 0f)
        {
            if (isBoss)
            {
                float bossLuck = Mathf.Clamp(extraLuck, 0f, 0.25f);
                float rare = Mathf.Max(8f, 22f - bossLuck * 40f);
                float epic = 46f;
                float legendary = 100f - rare - epic;
                return new[] { 0f, rare, epic, legendary };
            }

            float progress = GetWaveProgress(waveIndex, totalWaves);
            float t = Mathf.Clamp01(progress + Mathf.Clamp(extraLuck, 0f, 0.35f));

            // Nội suy mượt từ Wave đầu (76 / 20 / 3.5 / 0.5) đến cuối map (10 / 36 / 34 / 20)
            float wCommon = Mathf.Lerp(76f, 10f, t);
            float wRare = Mathf.Lerp(20f, 36f, t);
            float wEpic = Mathf.Lerp(3.5f, 34f, t);
            float wLegendary = Mathf.Lerp(0.5f, 20f, t);

            return new[] { wCommon, wRare, wEpic, wLegendary };
        }

        public static ItemRarity RollRarity(
            int waveIndex,
            int totalWaves,
            bool isBoss,
            float extraLuck = 0f,
            float? fixedRoll01 = null)
        {
            float[] weights = GetRarityWeights(waveIndex, totalWaves, isBoss, extraLuck);
            float total = weights[0] + weights[1] + weights[2] + weights[3];
            float roll = (fixedRoll01.HasValue ? Mathf.Clamp01(fixedRoll01.Value) : UnityEngine.Random.value) * total;

            float cumulative = weights[0];
            if (roll < cumulative) return ItemRarity.Common;
            cumulative += weights[1];
            if (roll < cumulative) return ItemRarity.Rare;
            cumulative += weights[2];
            if (roll < cumulative) return ItemRarity.Epic;
            return ItemRarity.Legendary;
        }

        /// <summary>
        /// Quay phần thưởng rớt ra khi hạ gục một quái vật hoặc Boss.
        /// </summary>
        public static List<LootDropResult> RollDropsForDefeatedEnemy(
            EquipmentCatalog catalog,
            int waveIndex,
            int totalWaves,
            bool isBoss,
            float waveDropBonus = 0f,
            float waveRarityLuck = 0f,
            int quizStreak = 0,
            System.Random rng = null)
        {
            List<LootDropResult> drops = new List<LootDropResult>();
            if (catalog == null || catalog.allItems == null || catalog.allItems.Count == 0)
            {
                return drops;
            }

            float streakLuck = quizStreak >= 3 ? 0.08f : 0f;
            float totalLuck = Mathf.Max(0f, waveRarityLuck) + streakLuck;
            float chance = CalculateEquipmentDropChance(waveIndex, totalWaves, isBoss, waveDropBonus);

            float NextRoll() => rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value;

            if (isBoss)
            {
                // Boss cuối map chắc chắn rớt 2 món trang bị cao cấp (1 món tối thiểu Sử Thi + 1 món tối thiểu Hiếm)
                ItemRarity r1 = RollRarity(waveIndex, totalWaves, true, totalLuck + 0.12f, NextRoll());
                if (r1 < ItemRarity.Epic) r1 = ItemRarity.Epic;
                EquipmentItemData item1 = catalog.PickRandomItem(r1, rng);
                if (item1 != null)
                {
                    drops.Add(new LootDropResult
                    {
                        equipment = item1,
                        isConsumableOnly = false,
                        waveNumber = waveIndex + 1,
                        totalWaves = totalWaves,
                        dropChanceAtWave = 1f
                    });
                }

                ItemRarity r2 = RollRarity(waveIndex, totalWaves, true, totalLuck, NextRoll());
                EquipmentItemData item2 = catalog.PickRandomItem(r2, rng);
                if (item2 != null)
                {
                    drops.Add(new LootDropResult
                    {
                        equipment = item2,
                        isConsumableOnly = false,
                        waveNumber = waveIndex + 1,
                        totalWaves = totalWaves,
                        dropChanceAtWave = 1f
                    });
                }

                // Tặng kèm 1 Lông Phượng Hoàng hoặc Thảo Dược Máu
                drops.Add(new LootDropResult
                {
                    equipment = null,
                    isConsumableOnly = true,
                    consumableType = ConsumableItemType.PhoenixFeather,
                    waveNumber = waveIndex + 1,
                    totalWaves = totalWaves,
                    dropChanceAtWave = 1f
                });
                return drops;
            }

            // Quái thường / Tinh anh: kiểm tra xác suất rớt trang bị
            if (NextRoll() <= chance)
            {
                ItemRarity rarity = RollRarity(waveIndex, totalWaves, false, totalLuck, NextRoll());
                EquipmentItemData item = catalog.PickRandomItem(rarity, rng);
                if (item != null)
                {
                    drops.Add(new LootDropResult
                    {
                        equipment = item,
                        isConsumableOnly = false,
                        waveNumber = waveIndex + 1,
                        totalWaves = totalWaves,
                        dropChanceAtWave = chance
                    });
                }
            }

            // Xác suất phụ rớt vật phẩm tiêu hao giúp đội hình trụ vững qua bản đồ nhiều đợt
            float progress = GetWaveProgress(waveIndex, totalWaves);
            float consumableChance = 0.22f + 0.18f * progress;
            if (NextRoll() <= consumableChance)
            {
                float cRoll = NextRoll();
                ConsumableItemType cType;
                if (progress >= 0.6f && cRoll < 0.18f) cType = ConsumableItemType.PhoenixFeather;
                else if (cRoll < 0.62f) cType = ConsumableItemType.HealthPotion;
                else cType = ConsumableItemType.ManaElixir;

                drops.Add(new LootDropResult
                {
                    equipment = null,
                    isConsumableOnly = true,
                    consumableType = cType,
                    waveNumber = waveIndex + 1,
                    totalWaves = totalWaves,
                    dropChanceAtWave = chance
                });
            }

            return drops;
        }

        /// <summary>
        /// Sinh chắc chắn 1 món trang bị (tối thiểu Hiếm) cho phần thưởng Rương Cổ / sự kiện đặc biệt.
        /// </summary>
        public static LootDropResult RollGuaranteedEquipmentDrop(
            EquipmentCatalog catalog,
            int waveIndex,
            int totalWaves,
            float extraLuck = 0.15f,
            int quizStreak = 0,
            ItemRarity minRarity = ItemRarity.Rare,
            System.Random rng = null)
        {
            if (catalog == null || catalog.allItems == null || catalog.allItems.Count == 0)
            {
                return default;
            }

            float streakLuck = quizStreak >= 3 ? 0.08f : 0f;
            float totalLuck = Mathf.Max(0f, extraLuck) + streakLuck;
            float? fixedRoll = rng != null ? (float?)rng.NextDouble() : null;
            ItemRarity rarity = RollRarity(waveIndex, totalWaves, false, totalLuck, fixedRoll);
            if (rarity < minRarity) rarity = minRarity;

            EquipmentItemData item = catalog.PickRandomItem(rarity, rng);
            return new LootDropResult
            {
                equipment = item,
                isConsumableOnly = false,
                waveNumber = waveIndex + 1,
                totalWaves = Mathf.Max(1, totalWaves),
                dropChanceAtWave = 1f
            };
        }
    }
}
