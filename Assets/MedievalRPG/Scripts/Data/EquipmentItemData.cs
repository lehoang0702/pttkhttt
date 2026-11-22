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

        /// <summary>Điểm sức mạnh ước lượng dùng để sắp xếp và tự động mặc đồ mạnh nhất.</summary>
        public int GetPowerScore()
        {
            int rarityWeight = ((int)rarity + 1) * 100;
            int statsScore =
                bonusMaxHP
                + bonusMaxMP * 2
                + bonusPhysicalAttack * 4
                + bonusMagicAttack * 4
                + bonusPhysicalDefense * 3
                + bonusMagicDefense * 3
                + bonusSpeed * 5
                + Mathf.RoundToInt(bonusCritRate * 300f);
            return rarityWeight + statsScore;
        }

        public string GetStatsSummary()
        {
            List<string> parts = new List<string>(6);
            if (bonusPhysicalAttack > 0) parts.Add($"+{bonusPhysicalAttack} ATK");
            if (bonusMagicAttack > 0) parts.Add($"+{bonusMagicAttack} MAG");
            if (bonusMaxHP > 0) parts.Add($"+{bonusMaxHP} HP");
            if (bonusMaxMP > 0) parts.Add($"+{bonusMaxMP} MP");
            if (bonusPhysicalDefense > 0) parts.Add($"+{bonusPhysicalDefense} DEF");
            if (bonusMagicDefense > 0) parts.Add($"+{bonusMagicDefense} MDEF");
            if (bonusSpeed > 0) parts.Add($"+{bonusSpeed} SPD");
            if (bonusCritRate > 0.001f) parts.Add($"+{Mathf.RoundToInt(bonusCritRate * 100f)}% CRIT");
            return parts.Count > 0 ? string.Join("  ", parts) : "Không cộng chỉ số";
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
