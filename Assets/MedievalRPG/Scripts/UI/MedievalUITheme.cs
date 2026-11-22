using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Nguồn màu duy nhất (Art Bible Palette) cho toàn bộ giao diện Medieval RPG.
    /// Đồng bộ với cảnh rừng pixel-art: nền đá phiến tối (Obsidian Slate), viền vàng rèn (Forged Gold)
    /// và các màu phân biệt hành động rõ ràng.
    /// </summary>
    public static class MedievalUITheme
    {
        // --- Nền & Khung (Surfaces & Trim) ---
        public static readonly Color ObsidianPanel   = new Color(0.082f, 0.106f, 0.149f, 0.94f); // #151B26
        public static readonly Color DeepInset       = new Color(0.051f, 0.067f, 0.098f, 0.96f); // #0D1119
        public static readonly Color ScrimDark       = new Color(0.039f, 0.055f, 0.078f, 0.78f); // #0A0E14
        public static readonly Color GoldPrimary     = new Color(0.902f, 0.722f, 0.302f, 1.00f); // #E6B84D
        public static readonly Color GoldSoft        = new Color(0.949f, 0.792f, 0.420f, 1.00f); // #F2CA6B
        public static readonly Color BronzeBorder    = new Color(0.541f, 0.384f, 0.133f, 1.00f); // #8A6222
        public static readonly Color SlateBorder     = new Color(0.255f, 0.306f, 0.400f, 1.00f); // #414E66

        // --- Chữ (Typography) ---
        public static readonly Color IvoryText       = new Color(0.961f, 0.937f, 0.886f, 1.00f); // #F5EFE2
        public static readonly Color SlateMuted      = new Color(0.659f, 0.702f, 0.773f, 1.00f); // #A8B3C5
        public static readonly Color DarkInkText     = new Color(0.075f, 0.090f, 0.125f, 1.00f); // #131720

        // --- Thanh chỉ số (HP / MP Bars) ---
        public static readonly Color HpCrimson       = new Color(0.878f, 0.282f, 0.282f, 1.00f); // #E04848
        public static readonly Color HpWarning       = new Color(1.000f, 0.439f, 0.263f, 1.00f); // #FF7043
        public static readonly Color HpTrailGold     = new Color(0.960f, 0.780f, 0.350f, 0.90f); // Delayed drain bar
        public static readonly Color MpSapphire      = new Color(0.220f, 0.596f, 0.925f, 1.00f); // #3898EC

        // --- Nút hành động (Action Buttons) ---
        public static readonly Color BtnAttack       = new Color(0.745f, 0.235f, 0.220f, 1.00f); // #BE3C38
        public static readonly Color BtnSkill        = new Color(0.416f, 0.278f, 0.749f, 1.00f); // #6A47BF
        public static readonly Color BtnHeal         = new Color(0.173f, 0.576f, 0.369f, 1.00f); // #2C935E
        public static readonly Color BtnDefend       = new Color(0.216f, 0.416f, 0.592f, 1.00f); // #376A97
        public static readonly Color BtnPrimaryGold  = new Color(0.761f, 0.533f, 0.157f, 1.00f); // #C28828
        public static readonly Color BtnNeutralSlate = new Color(0.173f, 0.216f, 0.294f, 1.00f); // #2C374B
        public static readonly Color BtnDangerSlate  = new Color(0.478f, 0.176f, 0.196f, 1.00f); // #7A2D32
        public static readonly Color BtnDisabled     = new Color(0.145f, 0.165f, 0.208f, 0.72f);

        // --- Kết quả & Trạng thái (Status & Quiz) ---
        public static readonly Color SuccessGreen    = new Color(0.235f, 0.722f, 0.471f, 1.00f); // #3CB878
        public static readonly Color SuccessPanel    = new Color(0.102f, 0.298f, 0.204f, 0.96f);
        public static readonly Color DangerRed       = new Color(0.851f, 0.275f, 0.275f, 1.00f); // #D94646
        public static readonly Color DangerPanel     = new Color(0.329f, 0.114f, 0.129f, 0.96f);

        // --- Màu chủ đề câu hỏi (Quiz Categories) ---
        public static readonly Color CategoryHistory = new Color(0.741f, 0.467f, 0.180f, 1.00f); // Amber bronze
        public static readonly Color CategoryMath    = new Color(0.235f, 0.525f, 0.816f, 1.00f); // Cobalt blue
        public static readonly Color CategoryEnglish = new Color(0.514f, 0.341f, 0.780f, 1.00f); // Royal purple
        public static readonly Color CategoryScience = new Color(0.184f, 0.620f, 0.475f, 1.00f); // Teal emerald

        public static Color GetCategoryColor(string category)
        {
            if (string.IsNullOrEmpty(category)) return BtnNeutralSlate;
            string lower = category.ToLowerInvariant();
            if (lower.Contains("b2")) return CategoryEnglish;
            if (lower.Contains("b1")) return CategoryMath;
            if (lower.Contains("sử") || lower.Contains("history")) return CategoryHistory;
            if (lower.Contains("toán") || lower.Contains("math")) return CategoryMath;
            if (lower.Contains("anh") || lower.Contains("english")) return CategoryEnglish;
            if (lower.Contains("khoa") || lower.Contains("science")) return CategoryScience;
            return BtnSkill;
        }

        // --- Màu độ hiếm trang bị (Loot Rarity) ---
        public static readonly Color RarityCommon    = new Color(0.840f, 0.870f, 0.910f, 1.00f); // #D6DEE8 Thường
        public static readonly Color RarityRare      = new Color(0.290f, 0.640f, 1.000f, 1.00f); // #4AA3FF Hiếm
        public static readonly Color RarityEpic      = new Color(0.710f, 0.420f, 1.000f, 1.00f); // #B56BFF Sử Thi
        public static readonly Color RarityLegendary = new Color(1.000f, 0.690f, 0.130f, 1.00f); // #FFB020 Huyền Thoại

        public static Color GetRarityColor(ItemRarity rarity) => EquipmentItemData.GetRarityColor(rarity);
        public static string GetRarityHex(ItemRarity rarity) => EquipmentItemData.GetRarityHex(rarity);

        public static string FormatHeroDisplayName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Anh Hùng";
            if (rawName.Contains("Knight") && !rawName.Contains("Dark")) return "Hiệp Sĩ";
            if (rawName.Contains("Mage")) return "Pháp Sư";
            if (rawName.Contains("Archer")) return "Cung Thủ";
            return rawName;
        }
    }
}
