using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    /// <summary>
    /// Dữ liệu một món trang bị trung cổ thuộc một trong 3 class (Knight/Mage/Archer),
    /// 3 ô trang bị (Vũ khí/Giáp/Phụ kiện) và 4 cấp độ hiếm (Thường/Hiếm/Sử Thi/Huyền Thoại).
    /// </summary>
    [CreateAssetMenu(fileName = "NewEquipment", menuName = "Medieval RPG/Equipment Item")]
    public class EquipmentItemData : ScriptableObject
    {
        [Header("Định Danh & Phân Loại")]
        public string itemId = "";
        public string itemName = "Trang Bị";
        [TextArea(2, 3)]
        public string description = "";
        public CharacterClassType requiredClass = CharacterClassType.Knight;
        public EquipmentSlot slot = EquipmentSlot.Weapon;
        public ItemRarity rarity = ItemRarity.Common;
        public Sprite icon;

        [Header("Chỉ Số Cộng Thêm")]
        public int bonusMaxHP;
        public int bonusMaxMP;
        public int bonusPhysicalAttack;
        public int bonusMagicAttack;
        public int bonusPhysicalDefense;
        public int bonusMagicDefense;
        public int bonusSpeed;
        [Range(0f, 0.5f)]
        public float bonusCritRate;

        public const int MaxUpgradeLevel = 5;

        /// <summary>Điểm sức mạnh ước lượng dùng để sắp xếp và tự động mặc đồ mạnh nhất.</summary>
        public int GetPowerScore() => GetPowerScore(0);

        public int GetPowerScore(int upgradeLevel)
        {
            EquipmentStatBonus s = GetScaledStats(upgradeLevel);
            int rarityWeight = ((int)rarity + 1) * 100;
            int statsScore =
                s.maxHP
                + s.maxMP * 2
                + s.physicalAttack * 4
                + s.magicAttack * 4
                + s.physicalDefense * 3
                + s.magicDefense * 3
                + s.speed * 5
                + Mathf.RoundToInt(s.critRate * 300f);
            return rarityWeight + statsScore;
        }

        public EquipmentStatBonus GetScaledStats(int upgradeLevel)
        {
            int lv = Mathf.Clamp(upgradeLevel, 0, MaxUpgradeLevel);
            float mult = 1f + lv * 0.15f;

            int ScaleStat(int baseVal)
            {
                if (baseVal <= 0) return 0;
                if (lv == 0) return baseVal;
                return Mathf.Max(baseVal + lv, Mathf.RoundToInt(baseVal * mult));
            }

            return new EquipmentStatBonus
            {
                maxHP = ScaleStat(bonusMaxHP),
                maxMP = ScaleStat(bonusMaxMP),
                physicalAttack = ScaleStat(bonusPhysicalAttack),
                magicAttack = ScaleStat(bonusMagicAttack),
                physicalDefense = ScaleStat(bonusPhysicalDefense),
                magicDefense = ScaleStat(bonusMagicDefense),
                speed = lv == 0 ? bonusSpeed : (bonusSpeed > 0 ? bonusSpeed + Mathf.Max(1, lv / 2) : 0),
                critRate = bonusCritRate + (bonusCritRate > 0.001f ? lv * 0.01f : 0f)
            };
        }

        public int GetSellGoldValue(int upgradeLevel)
        {
            int basePrice = rarity switch
            {
                ItemRarity.Common => 20,
                ItemRarity.Rare => 45,
                ItemRarity.Epic => 95,
                ItemRarity.Legendary => 200,
                _ => 20
            };
            return basePrice + Mathf.Clamp(upgradeLevel, 0, MaxUpgradeLevel) * 25;
        }

        public int GetUpgradeCost(int currentUpgradeLevel)
        {
            if (currentUpgradeLevel >= MaxUpgradeLevel) return 0;
            return (int)rarity * 25 + (currentUpgradeLevel + 1) * 35;
        }

        public string GetStatsSummary() => GetStatsSummary(0);

        public string GetStatsSummary(int upgradeLevel)
        {
            return GetScaledStats(upgradeLevel).FormatCompact();
        }

        public static string GetRarityDisplayName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "Thường";
                case ItemRarity.Rare: return "Hiếm";
                case ItemRarity.Epic: return "Sử Thi";
                case ItemRarity.Legendary: return "Huyền Thoại";
                default: return "Thường";
            }
        }

        public static Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:    return new Color(0.84f, 0.87f, 0.91f, 1f); // Xám sáng #D6DEE8
                case ItemRarity.Rare:      return new Color(0.29f, 0.64f, 1.00f, 1f); // Xanh lam #4AA3FF
                case ItemRarity.Epic:      return new Color(0.71f, 0.42f, 1.00f, 1f); // Tím sử thi #B56BFF
                case ItemRarity.Legendary: return new Color(1.00f, 0.69f, 0.13f, 1f); // Vàng cam #FFB020
                default:                   return Color.white;
            }
        }

        public static string GetRarityHex(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:    return "#D6DEE8";
                case ItemRarity.Rare:      return "#4AA3FF";
                case ItemRarity.Epic:      return "#B56BFF";
                case ItemRarity.Legendary: return "#FFB020";
                default:                   return "#F5EFE2";
            }
        }

        public static string GetSlotDisplayName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return "Vũ Khí";
                case EquipmentSlot.Armor: return "Giáp";
                case EquipmentSlot.Accessory: return "Phụ Kiện";
                default: return "Trang Bị";
            }
        }

        public static string GetClassDisplayName(CharacterClassType cls)
        {
            switch (cls)
            {
                case CharacterClassType.Knight: return "Hiệp Sĩ";
                case CharacterClassType.Mage: return "Pháp Sư";
                case CharacterClassType.Archer: return "Cung Thủ";
                default: return "Anh Hùng";
            }
        }
    }

    public struct EquipmentStatBonus
    {
        public int maxHP;
        public int maxMP;
        public int physicalAttack;
        public int magicAttack;
        public int physicalDefense;
        public int magicDefense;
        public int speed;
        public float critRate;

        public void Add(EquipmentItemData item)
        {
            if (item == null) return;
            maxHP += item.bonusMaxHP;
            maxMP += item.bonusMaxMP;
            physicalAttack += item.bonusPhysicalAttack;
            magicAttack += item.bonusMagicAttack;
            physicalDefense += item.bonusPhysicalDefense;
            magicDefense += item.bonusMagicDefense;
            speed += item.bonusSpeed;
            critRate += item.bonusCritRate;
        }

        public void Add(EquipmentStatBonus other)
        {
            maxHP += other.maxHP;
            maxMP += other.maxMP;
            physicalAttack += other.physicalAttack;
            magicAttack += other.magicAttack;
            physicalDefense += other.physicalDefense;
            magicDefense += other.magicDefense;
            speed += other.speed;
            critRate += other.critRate;
        }

        public string FormatCompact()
        {
            List<string> parts = new List<string>(6);
            if (physicalAttack > 0) parts.Add($"+{physicalAttack} ATK");
            if (magicAttack > 0) parts.Add($"+{magicAttack} MAG");
            if (maxHP > 0) parts.Add($"+{maxHP} HP");
            if (maxMP > 0) parts.Add($"+{maxMP} MP");
            if (physicalDefense > 0) parts.Add($"+{physicalDefense} DEF");
            if (magicDefense > 0) parts.Add($"+{magicDefense} MDEF");
            if (speed > 0) parts.Add($"+{speed} SPD");
            if (critRate > 0.001f) parts.Add($"+{Mathf.RoundToInt(critRate * 100f)}% CRIT");
            return parts.Count > 0 ? string.Join("  ", parts) : "Không cộng chỉ số";
        }

        public string FormatDiffAgainst(EquipmentStatBonus currentEquipped)
        {
            List<string> parts = new List<string>(8);
            void AppendDelta(int delta, string label)
            {
                if (delta == 0) return;
                string col = delta > 0 ? "#5CE09A" : "#FF6B6B";
                string sign = delta > 0 ? "+" : "";
                parts.Add($"<color={col}>{sign}{delta} {label}</color>");
            }

            AppendDelta(maxHP - currentEquipped.maxHP, "HP");
            AppendDelta(maxMP - currentEquipped.maxMP, "MP");
            AppendDelta(physicalAttack - currentEquipped.physicalAttack, "ATK");
            AppendDelta(magicAttack - currentEquipped.magicAttack, "MAG");
            AppendDelta(physicalDefense - currentEquipped.physicalDefense, "DEF");
            AppendDelta(magicDefense - currentEquipped.magicDefense, "MDEF");
            AppendDelta(speed - currentEquipped.speed, "SPD");

            int critDelta = Mathf.RoundToInt((critRate - currentEquipped.critRate) * 100f);
            AppendDelta(critDelta, "% CRIT");

            return parts.Count > 0 ? string.Join("  ", parts) : "<color=#A8B3C5>Chỉ số tương đương món đang mặc</color>";
        }

        public string FormatSummary()
        {
            List<string> parts = new List<string>(6);
            if (maxHP > 0) parts.Add($"+{maxHP} HP");
            if (maxMP > 0) parts.Add($"+{maxMP} MP");
            if (physicalAttack > 0) parts.Add($"+{physicalAttack} ATK");
            if (magicAttack > 0) parts.Add($"+{magicAttack} MAG");
            if (physicalDefense > 0) parts.Add($"+{physicalDefense} DEF");
            if (magicDefense > 0) parts.Add($"+{magicDefense} MDEF");
            if (speed > 0) parts.Add($"+{speed} SPD");
            if (critRate > 0.001f) parts.Add($"+{Mathf.RoundToInt(critRate * 100f)}% CRIT");
            return parts.Count > 0 ? string.Join("  •  ", parts) : "Chưa mặc trang bị nào";
        }
    }
}
