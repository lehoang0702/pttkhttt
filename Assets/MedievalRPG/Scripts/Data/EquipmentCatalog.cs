using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    /// <summary>
    /// Danh mục trang bị tập trung (156 trang bị: 3 Class x 4 Slot [Vũ Khí, Mũ, Giáp, Phụ Kiện] x 13 món [4 Thường, 4 Hiếm, 3 Sử Thi, 2 Huyền Thoại]).
    /// Được nạp từ Resources/MedievalEquipmentCatalog.asset hoặc khởi tạo dự phòng tự động khi chạy test.
    /// </summary>
    [CreateAssetMenu(fileName = "MedievalEquipmentCatalog", menuName = "Medieval RPG/Equipment Catalog")]
    public class EquipmentCatalog : ScriptableObject
    {
        public List<EquipmentItemData> allItems = new List<EquipmentItemData>();

        private Dictionary<string, EquipmentItemData> _lookupById;

        public struct EquipmentBlueprint
        {
            public string id;
            public string name;
            public string desc;
            public CharacterClassType cls;
            public EquipmentSlot slot;
            public ItemRarity rarity;
            public int hp, mp, atk, mag, def, mdef, spd;
            public float crit;

            public EquipmentBlueprint(
                string id, string name, string desc,
                CharacterClassType cls, EquipmentSlot slot, ItemRarity rarity,
                int hp, int mp, int atk, int mag, int def, int mdef, int spd, float crit)
            {
                this.id = id;
                this.name = name;
                this.desc = desc;
                this.cls = cls;
                this.slot = slot;
                this.rarity = rarity;
                this.hp = hp;
                this.mp = mp;
                this.atk = atk;
                this.mag = mag;
                this.def = def;
                this.mdef = mdef;
                this.spd = spd;
                this.crit = crit;
            }
        }

        public static EquipmentBlueprint[] GetDefaultBlueprints()
        {
            return new[]
            {
                // ================================================================
                // 1. KNIGHT (HIỆP SĨ) - 52 MÓN (13 Vũ Khí, 13 Mũ, 13 Giáp, 13 PK)
                // ================================================================
                // --- Vũ Khí Knight (13) ---
                new EquipmentBlueprint("knight_weapon_common",      "Kiếm Sắt Rỉ",           "Thanh kiếm sắt đơn giản của lính gác cổng thành.",               CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Common,     10,  0,  5,  0,  1,  0, 0, 0.01f),
                new EquipmentBlueprint("knight_weapon_common_2",    "Rìu Gỗ Chiến Binh",     "Lưỡi rìu sắt cán gỗ nặng tay, bổ mạnh vào giáp địch.",           CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Common,      8,  0,  6,  0,  1,  0, 0, 0.02f),
                new EquipmentBlueprint("knight_weapon_common_3",    "Chùy Gai Sắt",          "Chùy tròn gắn đinh sắt gây chấn động qua lớp khiên.",           CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Common,     12,  0,  5,  0,  2,  0, 0, 0.01f),
                new EquipmentBlueprint("knight_weapon_common_4",    "Giáo Tập Sự",           "Ngọn giáo cán dài giữ cự ly an toàn nơi tiền tuyến.",           CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Common,      9,  0,  5,  0,  1,  1, 1, 0.02f),
                new EquipmentBlueprint("knight_weapon_rare",        "Đại Kiếm Thép Rèn",     "Kiếm thép rèn kỹ giúp đòn chém uy lực và vững vàng hơn.",        CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Rare,       20,  0, 10,  0,  3,  0, 0, 0.03f),
                new EquipmentBlueprint("knight_weapon_rare_2",      "Chiến Phủ Hai Lưỡi",    "Đại phủ hai lưỡi xoay chuyển liên hoàn cắt nát đội hình.",       CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Rare,       18,  0, 12,  0,  2,  1, 0, 0.04f),
                new EquipmentBlueprint("knight_weapon_rare_3",      "Búa Chiến Bạc",         "Đầu búa nạm thép bạc đập tan khiên chắn hạng nặng.",            CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Rare,       24,  2, 10,  0,  4,  1, 0, 0.03f),
                new EquipmentBlueprint("knight_weapon_rare_4",      "Trường Thương Kỵ Sĩ",   "Thương thép dài đính cờ xanh xuyên thủng hàng tiên phong.",      CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Rare,       19,  2, 11,  0,  3,  1, 1, 0.04f),
                new EquipmentBlueprint("knight_weapon_epic",        "Thánh Kiếm Bình Minh",  "Lưỡi kiếm ánh vàng xua tan bóng tối, khắc chế quái hầm ngục.",   CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Epic,       35,  5, 17,  0,  5,  2, 1, 0.06f),
                new EquipmentBlueprint("knight_weapon_epic_2",      "Huyết Phủ Cuồng Nộ",    "Chiến phủ lưỡi trăng khuyết rực lửa chiến ý bất khuất.",         CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Epic,       32,  4, 20,  0,  4,  2, 1, 0.08f),
                new EquipmentBlueprint("knight_weapon_epic_3",      "Thánh Chùy Quang Minh", "Bảo chùy hoàng kim tỏa hào quang chấn áp ma vật bóng tối.",      CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Epic,       40,  8, 17,  0,  6,  4, 1, 0.06f),
                new EquipmentBlueprint("knight_weapon_legendary",   "Kiếm Rồng Huyền Thoại", "Thần kiếm rèn từ vảy rồng cổ đại, chém nát mọi phòng tuyến.",    CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Legendary,  55, 10, 26,  0,  8,  4, 2, 0.10f),
                new EquipmentBlueprint("knight_weapon_legendary_2", "Thần Kích Lôi Đình",    "Kích thần kết hợp thương và rìu phóng ra lôi điện xé trời.",     CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Legendary,  50, 12, 27,  0,  7,  5, 3, 0.11f),

                // --- Mũ Knight (13) ---
                new EquipmentBlueprint("knight_helm_common",      "Mũ Sắt Lính Gác",       "Mũ trụ sắt thô chống đỡ những cú đánh trực diện vào đầu.",       CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Common,     14,  0,  0,  0,  3,  1, 0, 0.00f),
                new EquipmentBlueprint("knight_helm_common_2",    "Mũ Da Chiến Binh",      "Mũ da thuộc ôm gọn đầu, nhẹ và dễ xoay chuyển tầm nhìn.",        CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Common,     12,  0,  1,  0,  2,  1, 1, 0.00f),
                new EquipmentBlueprint("knight_helm_common_3",    "Mũ Đồng Cũ Kỵ Sĩ",      "Mũ đồng dày dặn truyền lại từ các đội vệ binh biên ải.",        CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Common,     16,  0,  0,  0,  3,  2, 0, 0.00f),
                new EquipmentBlueprint("knight_helm_common_4",    "Băng Đô Thép Nhẹ",      "Băng trán gắn bản thép bảo vệ mi tâm và giữ vững tinh thần.",   CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Common,     11,  2,  1,  0,  2,  1, 1, 0.01f),
                new EquipmentBlueprint("knight_helm_rare",        "Mũ Trụ Hiệp Sĩ Bạc",    "Mũ có tấm che mặt thép bạc sáng loáng chuẩn kỵ sĩ đoàn.",       CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Rare,       28,  2,  1,  0,  6,  3, 0, 0.01f),
                new EquipmentBlueprint("knight_helm_rare_2",      "Mũ Đồng Hoàng Gia",     "Mũ trụ đồng mạ vàng tinh xảo của đội cận vệ lâu đài.",           CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Rare,       30,  4,  1,  0,  6,  4, 0, 0.01f),
                new EquipmentBlueprint("knight_helm_rare_3",      "Mũ Trụ Cận Vệ Xanh",    "Khảm lam ngọc hộ mệnh giúp giảm sát thương phép và choáng.",     CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Rare,       26,  6,  1,  0,  5,  5, 1, 0.01f),
                new EquipmentBlueprint("knight_helm_rare_4",      "Mũ Sừng Bò Chiến",      "Đôi sừng nhọn tăng uy thế áp đảo khi lao vào giữa trận.",       CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Rare,       27,  0,  3,  0,  5,  3, 1, 0.02f),
                new EquipmentBlueprint("knight_helm_epic",        "Mũ Chiến Cánh Vàng",    "Đôi cánh vàng hai bên tượng trưng cho lòng quả cảm bất diệt.",   CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Epic,       48,  6,  3,  0, 10,  6, 2, 0.03f),
                new EquipmentBlueprint("knight_helm_epic_2",      "Mũ Sừng Quỷ Đỏ",        "Mũ thép đỏ thẫm rèn theo sừng ác quỷ khơi dậy sức chiến đấu.",  CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Epic,       44,  4,  5,  0,  9,  5, 2, 0.04f),
                new EquipmentBlueprint("knight_helm_epic_3",      "Mũ Thánh Quang Bạch",   "Mũ kỵ sĩ ánh sáng thuần khiết, bảo hộ tâm trí trước ma thuật.",  CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Epic,       50,  8,  2,  0, 11,  8, 1, 0.02f),
                new EquipmentBlueprint("knight_helm_legendary",   "Vương Miện Rồng Lửa",   "Vương miện chiến tướng đính vảy rồng rực cháy giữa chiến trận.", CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Legendary,  74, 12,  6,  0, 16, 11, 2, 0.05f),
                new EquipmentBlueprint("knight_helm_legendary_2", "Thánh Quan Thiên Sứ",   "Thánh quan ánh bạc có vòng hào quang bảo vệ chủ nhân bất bại.",  CharacterClassType.Knight, EquipmentSlot.Helmet,    ItemRarity.Legendary,  78, 14,  5,  0, 17, 13, 3, 0.04f),

                // --- Giáp Knight (13) ---
                new EquipmentBlueprint("knight_armor_common",      "Giáp Da Chiến Binh",   "Bộ giáp da dày chống chịu tốt các vết cào cơ bản.",              CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Common,     20,  0,  0,  0,  4,  2, 0, 0.00f),
                new EquipmentBlueprint("knight_armor_common_2",    "Áo Khâu Sắt Nhẹ",      "Áo giáp lưới sắt đan khít, linh hoạt và chắc chắn hơn vải.",     CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Common,     18,  0,  1,  0,  4,  2, 1, 0.00f),
                new EquipmentBlueprint("knight_armor_common_3",    "Giáp Đồng Tân Binh",   "Tấm ngực đồng rèn giản dị dành cho kỵ sĩ mới nhập ngũ.",        CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Common,     22,  0,  0,  0,  5,  1, 0, 0.00f),
                new EquipmentBlueprint("knight_armor_common_4",    "Áo Chiến Cơ Bản",      "Áo chiến độn bông lót thép nhẹ giữ ấm và giảm chấn thương.",   CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Common,     19,  3,  1,  0,  3,  3, 0, 0.00f),
                new EquipmentBlueprint("knight_armor_rare",        "Giáp Sắt Hiệp Sĩ",     "Giáp tấm hợp kim bảo vệ toàn thân của kỵ sĩ hoàng gia.",         CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Rare,       40,  0,  2,  0,  8,  4, 0, 0.00f),
                new EquipmentBlueprint("knight_armor_rare_2",      "Giáp Tấm Cận Vệ",      "Chiến giáp lam bạc cân bằng hoàn hảo giữa giáp và kháng phép.",   CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Rare,       38,  5,  2,  0,  8,  5, 1, 0.01f),
                new EquipmentBlueprint("knight_armor_rare_3",      "Giáp Vảy Rồng Bạc",    "Ghép từ vảy thép chồng lên nhau giúp hóa giải đòn chém mạnh.",    CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Rare,       42,  2,  3,  0,  9,  4, 0, 0.01f),
                new EquipmentBlueprint("knight_armor_rare_4",      "Giáp Đồng Hoàng Gia",  "Tấm ngực đồng chạm khắc hoa văn sư tử uy nghiêm.",              CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Rare,       44,  4,  2,  0,  8,  5, 0, 0.01f),
                new EquipmentBlueprint("knight_armor_epic",        "Giáp Chiến Hoàng Kim", "Chiến giáp mạ vàng khắc cổ ngữ tăng sức bền vượt trội.",         CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Epic,       68,  5,  4,  0, 14,  8, 1, 0.00f),
                new EquipmentBlueprint("knight_armor_epic_2",      "Chiến Giáp Huyết Thệ", "Áo giáp đỏ rực nhuốm lời thề kỵ sĩ, tăng công lẫn thủ.",         CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Epic,       64,  6,  7,  0, 13,  8, 2, 0.02f),
                new EquipmentBlueprint("knight_armor_epic_3",      "Giáp Thánh Quang",     "Giáp bạch kim điểm cánh thiên sứ kháng cự mạnh mọi tà thuật.",   CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Epic,       72, 10,  4,  0, 15, 11, 1, 0.01f),
                new EquipmentBlueprint("knight_armor_legendary",   "Thần Giáp Bất Diệt",   "Bộ giáp truyền thuyết giúp Hiệp Sĩ đứng vững trước bão đòn.",    CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Legendary, 105, 10,  8,  0, 22, 14, 2, 0.02f),
                new EquipmentBlueprint("knight_armor_legendary_2", "Giáp Lôi Thần Hoàng",  "Chiến giáp lam sấm sét kết tinh giáp thiên giới và uy lực sấm.",  CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Legendary, 102, 14,  9,  0, 21, 16, 3, 0.03f),

                // --- Phụ Kiện Knight (13) ---
                new EquipmentBlueprint("knight_acc_common",      "Khiên Gỗ Sồi",           "Tấm khiên gỗ cứng giúp đỡ đòn trực diện.",                       CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Common,     15,  5,  1,  0,  3,  1, 0, 0.00f),
                new EquipmentBlueprint("knight_acc_common_2",    "Khiên Tròn Đồng",        "Khiên tròn viền đồng gọn nhẹ, dễ xoay đỡ tên và chùy.",         CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Common,     14,  4,  1,  0,  3,  2, 0, 0.00f),
                new EquipmentBlueprint("knight_acc_common_3",    "Bao Tay Sắt Gỉ",         "Bao tay gia cố giúp siết chặt cán kiếm và ra đòn chuẩn xác.",   CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Common,     12,  3,  3,  0,  2,  1, 0, 0.01f),
                new EquipmentBlueprint("knight_acc_common_4",    "Khăn Choàng Tân Binh",   "Khăn choàng đỏ bảo vệ vai gáy và giữ nhiệt giữa đèo tuyết.",    CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Common,     13,  6,  1,  0,  2,  2, 1, 0.00f),
                new EquipmentBlueprint("knight_acc_rare",        "Khiên Thép Hộ Vệ",       "Khiên thép kiên cố tăng giáp vật lý và sức sống.",               CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Rare,       30,  8,  3,  0,  6,  3, 1, 0.02f),
                new EquipmentBlueprint("knight_acc_rare_2",      "Khiên Tháp Kỵ Sĩ",       "Khiên lớn hình chữ nhật che chắn toàn thân trước loạt tên.",     CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Rare,       34,  6,  2,  0,  7,  4, 0, 0.01f),
                new EquipmentBlueprint("knight_acc_rare_3",      "Huy Chương Dũng Sĩ",     "Huy chương bạc vinh danh chiến công, khích lệ tinh thần.",      CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Rare,       26, 10,  4,  0,  5,  4, 1, 0.03f),
                new EquipmentBlueprint("knight_acc_rare_4",      "Thắt Lưng Chiến Binh",   "Khóa thép chắc chắn giữ vững trọng tâm khi tung trọng kích.",    CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Rare,       28,  8,  4,  0,  6,  3, 1, 0.02f),
                new EquipmentBlueprint("knight_acc_epic",        "Huy Hiệu Kỵ Sĩ Đỏ",      "Huy hiệu danh dự tiếp thêm dũng khí và phản xạ chiến trường.",   CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Epic,       48, 12,  7,  0, 10,  6, 2, 0.04f),
                new EquipmentBlueprint("knight_acc_epic_2",      "Khiên Ánh Dương",        "Khiên vàng rực phản chiếu ánh mặt trời làm chói mắt kẻ thù.",    CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Epic,       52, 10,  6,  0, 11,  7, 2, 0.03f),
                new EquipmentBlueprint("knight_acc_epic_3",      "Thánh Tích Hộ Mệnh",     "Bùa thánh tử tinh hộ thân giảm mạnh tổn thương từ ma pháp.",     CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Epic,       46, 18,  6,  0,  9,  9, 2, 0.04f),
                new EquipmentBlueprint("knight_acc_legendary",   "Trái Tim Phượng Hoàng",  "Báu vật bất tử ban sinh lực dồi dào và uy thế áp đảo.",          CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Legendary,  80, 18, 12,  0, 15, 10, 3, 0.06f),
                new EquipmentBlueprint("knight_acc_legendary_2", "Thánh Thuẫn Bất Hoại",   "Đại thuẫn bất khả xâm phạm của thần hộ mệnh thành cổ.",         CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Legendary,  88, 16, 10,  0, 18, 12, 3, 0.05f),

                // ================================================================
                // 2. MAGE (PHÁP SƯ) - 52 MÓN (13 Vũ Khí, 13 Mũ, 13 Giáp, 13 PK)
                // ================================================================
                // --- Vũ Khí Mage (13) ---
                new EquipmentBlueprint("mage_weapon_common",      "Gậy Gỗ Cổ Thụ",           "Cây gậy tập sự dẫn truyền ma lực nguyên tố cơ bản.",             CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Common,      5,  8,  0,  6,  0,  1, 0, 0.01f),
                new EquipmentBlueprint("mage_weapon_common_2",    "Đũa Phép Tập Sự",         "Đũa phép mảnh nhẹ giúp học viên niệm chú ổn định hơn.",          CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Common,      4,  9,  0,  6,  0,  1, 1, 0.02f),
                new EquipmentBlueprint("mage_weapon_common_3",    "Trượng Đóng Nến",         "Ngọn nến ma pháp dẫn lửa nhỏ sưởi ấm và phóng tia lửa.",        CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Common,      6,  8,  0,  7,  0,  1, 0, 0.01f),
                new EquipmentBlueprint("mage_weapon_common_4",    "Gậy Xương Thô",           "Cây gậy cổ gắn tinh thể tím yếu, khuếch đại ma lực hắc ám.",    CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Common,      5,  7,  0,  7,  0,  2, 0, 0.02f),
                new EquipmentBlueprint("mage_weapon_rare",        "Trượng Ngọc Lam",         "Đầu trượng khảm lam ngọc khuếch đại sát thương phép.",           CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Rare,       10, 15,  0, 12,  0,  3, 1, 0.03f),
                new EquipmentBlueprint("mage_weapon_rare_2",      "Gậy Hồng Hỏa",            "Tinh thạch đỏ rực đốt cháy ma lực thành những quả cầu lửa.",     CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Rare,       10, 14,  0, 13,  0,  2, 1, 0.04f),
                new EquipmentBlueprint("mage_weapon_rare_3",      "Đũa Băng Tuyết",          "Đũa pha lê băng tinh khiết giúp tung chiêu băng giá cực nhanh.", CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Rare,        9, 16,  0, 12,  1,  3, 2, 0.03f),
                new EquipmentBlueprint("mage_weapon_rare_4",      "Trượng Sinh Mệnh",        "Gắn ngọc lục bảo tự nhiên trợ lực hồi phục và ma lực dồi dào.", CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Rare,       14, 17,  0, 11,  1,  4, 1, 0.03f),
                new EquipmentBlueprint("mage_weapon_epic",        "Trượng Tinh Tú",          "Tập trung ánh sao trời, tăng mạnh sát thương phép và hồi máu.",  CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Epic,       18, 25,  0, 21,  2,  6, 2, 0.06f),
                new EquipmentBlueprint("mage_weapon_epic_2",      "Pháp Trượng Phượng Hoàng", "Đầu trượng chạm hình phượng hoàng thiêu rụi đội hình địch.",      CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Epic,       18, 24,  0, 23,  2,  5, 2, 0.07f),
                new EquipmentBlueprint("mage_weapon_epic_3",      "Quyền Trượng Bão Sét",    "Lõi điện từ xoáy tròn phóng ra tia sét xé toạc bầu trời.",      CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Epic,       16, 26,  0, 22,  2,  6, 3, 0.07f),
                new EquipmentBlueprint("mage_weapon_legendary",   "Quyền Trượng Hư Không",   "Bảo vật tối thượng của Đại Pháp Sư, bùng nổ ma thuật hủy diệt.", CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Legendary,  28, 40,  0, 32,  4, 10, 3, 0.10f),
                new EquipmentBlueprint("mage_weapon_legendary_2", "Thánh Trượng Thái Dương", "Ngọn lửa mặt trời cô đọng thiêu đốt bóng tối và hồi sinh lực.",  CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Legendary,  32, 38,  0, 31,  5, 11, 4, 0.10f),

                // --- Mũ Mage (13) ---
                new EquipmentBlueprint("mage_helm_common",      "Mũ Vải Học Trò",          "Mũ chóp vải thô quen thuộc của mọi pháp sư học viện.",           CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Common,      7, 10,  0,  2,  1,  3, 0, 0.00f),
                new EquipmentBlueprint("mage_helm_common_2",    "Khăn Trùm Du Mục",        "Khăn trùm tím chắn gió bụi và giữ tập trung khi đọc thần chú.",  CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Common,      8,  9,  0,  2,  1,  3, 0, 0.01f),
                new EquipmentBlueprint("mage_helm_common_3",    "Mũ Nồi Phép Thuật",       "Chiếc mũ gọn nhẹ giúp trí óc minh mẫn và tiết kiệm mana.",      CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Common,      6, 11,  0,  2,  1,  3, 1, 0.00f),
                new EquipmentBlueprint("mage_helm_common_4",    "Băng Đô Cổ Ngữ",          "Vòng trán dệt chỉ vàng trợ giúp cảm nhận dòng chảy ma lực.",     CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Common,      6, 10,  0,  3,  0,  3, 1, 0.01f),
                new EquipmentBlueprint("mage_helm_rare",        "Mũ Nhọn Pháp Sư Lam",     "Mũ pháp sư xanh lam tăng cường kết giới và độ sắc bén phép.",   CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Rare,       15, 18,  0,  5,  2,  6, 1, 0.02f),
                new EquipmentBlueprint("mage_helm_rare_2",      "Mũ Băng Tinh Tuyết",       "Mũ lạnh giá kết tinh băng vĩnh cửu, kháng hỏa và tăng trí tuệ.", CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Rare,       14, 20,  0,  6,  2,  6, 1, 0.02f),
                new EquipmentBlueprint("mage_helm_rare_3",      "Mũ Trùm Hồng Hỏa",        "Mũ trùm đỏ thẫm rực lửa khuếch đại sức nóng của hỏa cầu.",      CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Rare,       16, 17,  0,  6,  2,  5, 1, 0.03f),
                new EquipmentBlueprint("mage_helm_rare_4",      "Vòng Nguyệt Quế Tri Thức", "Vương miện lá ngọc lục bảo giúp niệm phép liên tục không mệt.", CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Rare,       15, 22,  0,  5,  2,  6, 2, 0.02f),
                new EquipmentBlueprint("mage_helm_epic",        "Mũ Tinh Tú Trăng Khuyết", "Mũ chóp tím gắn biểu tượng trăng sao dẫn truyền ma lực vũ trụ.", CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Epic,       25, 30,  0, 11,  5, 10, 2, 0.04f),
                new EquipmentBlueprint("mage_helm_epic_2",      "Mũ Trùm Ánh Trăng",       "Khăn trùm bạc tinh khiết phản xạ lời nguyền và tà chú.",         CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Epic,       28, 28,  0, 10,  6, 12, 2, 0.03f),
                new EquipmentBlueprint("mage_helm_epic_3",      "Vương Miện Bão Sét",      "Vương miện sấm sét giúp pháp sư tung chiêu với tốc độ chớp.",   CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Epic,       24, 32,  0, 12,  4, 10, 3, 0.05f),
                new EquipmentBlueprint("mage_helm_legendary",   "Thánh Quan Nguyên Tố",    "Mũ thần ánh lửa nguyên tố đẩy giới hạn ma lực lên tầm huyền thoại.",CharacterClassType.Mage,  EquipmentSlot.Helmet,    ItemRarity.Legendary,  42, 48,  0, 18,  8, 16, 4, 0.07f),
                new EquipmentBlueprint("mage_helm_legendary_2", "Vương Miện Hư Không",     "Vương miện tím ma quái kết nối với cõi hư vô vô tận.",          CharacterClassType.Mage,   EquipmentSlot.Helmet,    ItemRarity.Legendary,  38, 52,  0, 20,  7, 17, 4, 0.08f),

                // --- Giáp Mage (13) ---
                new EquipmentBlueprint("mage_armor_common",      "Áo Choàng Vải Dệt",      "Áo choàng nhẹ dệt sợi ma thuật giúp giảm tổn thương phép.",      CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Common,     12, 10,  0,  2,  2,  4, 0, 0.00f),
                new EquipmentBlueprint("mage_armor_common_2",    "Áo Tập Sự Màu Tím",      "Lễ phục tím của tháp học viên giúp điều hòa dòng mana.",        CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Common,     11, 11,  0,  2,  2,  4, 0, 0.01f),
                new EquipmentBlueprint("mage_armor_common_3",    "Áo Vải Rừng Xanh",       "Áo choàng thảo mộc nhẹ nhàng tăng khả năng hồi phục năng lượng.",CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Common,     14, 10,  0,  1,  3,  4, 0, 0.00f),
                new EquipmentBlueprint("mage_armor_common_4",    "Lễ Phục Cũ Kỳ Thư",      "Áo cũ nhưng còn nguyên bùa chỉ bảo vệ khỏi ma thuật nhẹ.",     CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Common,     12, 12,  0,  2,  2,  4, 0, 0.00f),
                new EquipmentBlueprint("mage_armor_rare",        "Áo Pháp Sư Học Viện",    "Lễ phục tháp pháp sư tăng mạnh năng lượng và kháng phép.",       CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Rare,       24, 20,  0,  5,  4,  8, 1, 0.00f),
                new EquipmentBlueprint("mage_armor_rare_2",      "Bào Phục Băng Giá",      "Sợi băng dệt kết thành lớp khiên lạnh bảo vệ pháp sư.",         CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Rare,       26, 18,  0,  5,  5,  9, 1, 0.01f),
                new EquipmentBlueprint("mage_armor_rare_3",      "Áo Choàng Hồng Hỏa",     "Áo đỏ rực chịu nhiệt cao, trợ lực cho ma pháp hệ Hỏa.",         CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Rare,       22, 20,  0,  7,  4,  7, 1, 0.02f),
                new EquipmentBlueprint("mage_armor_rare_4",      "Bào Sinh Mệnh Xanh",     "Áo lục bảo tràn đầy sinh khí giúp pháp sư trụ vững lâu dài.",   CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Rare,       28, 21,  0,  4,  5,  9, 1, 0.01f),
                new EquipmentBlueprint("mage_armor_epic",        "Bào Phục Ánh Trăng",     "Dệt từ tơ ánh trăng, bảo hộ sinh mệnh và khuếch đại ma lực.",    CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Epic,       42, 32,  0, 10,  8, 14, 2, 0.02f),
                new EquipmentBlueprint("mage_armor_epic_2",      "Bào Phục Tinh Tú",       "Áo tím dệt bụi sao trời, tăng sâu công phép và trí tuệ.",        CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Epic,       40, 34,  0, 12,  7, 14, 2, 0.03f),
                new EquipmentBlueprint("mage_armor_epic_3",      "Bào Phục Bão Sét",       "Lớp áo dẫn điện giúp thân pháp linh hoạt và tung chiêu chớp.",  CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Epic,       41, 32,  0, 11,  8, 13, 3, 0.03f),
                new EquipmentBlueprint("mage_armor_legendary",   "Thánh Bào Nguyên Tố",    "Áo choàng hòa quyện tứ nguyên tố, kháng cự mọi ma lực hắc ám.",  CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Legendary,  68, 50,  0, 16, 13, 22, 3, 0.05f),
                new EquipmentBlueprint("mage_armor_legendary_2", "Thánh Bào Hư Không",     "Áo bào từ hư vô hấp thụ ma công địch hóa thành nội lực.",       CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Legendary,  64, 54,  0, 18, 12, 23, 3, 0.06f),

                // --- Phụ Kiện Mage (13) ---
                new EquipmentBlueprint("mage_acc_common",      "Nhẫn Thủy Tinh",         "Chiếc nhẫn nhỏ tích trữ một lượng mana ổn định.",                CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Common,      8, 12,  0,  3,  0,  2, 0, 0.02f),
                new EquipmentBlueprint("mage_acc_common_2",    "Sách Phép Cũ",           "Cuốn sách phép cơ bản ghi chép các câu chú hộ thân.",           CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Common,      7, 13,  0,  3,  1,  2, 0, 0.01f),
                new EquipmentBlueprint("mage_acc_common_3",    "Chuỗi Hạt Mana",         "Chuỗi hạt lam ngọc điều hòa hơi thở khi niệm phép liên tiếp.",   CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Common,      8, 14,  0,  2,  0,  3, 0, 0.01f),
                new EquipmentBlueprint("mage_acc_common_4",    "Bùa Giấy Cổ",            "Lá bùa giấy vàng dán ấn chú cổ bảo vệ trước tà khí.",           CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Common,      6, 11,  0,  4,  0,  2, 1, 0.02f),
                new EquipmentBlueprint("mage_acc_rare",        "Ngọc Năng Lượng",        "Viên ngọc tỏa sáng xanh giúp thi triển phép nhanh hơn.",         CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Rare,       14, 22,  0,  7,  1,  5, 1, 0.04f),
                new EquipmentBlueprint("mage_acc_rare_2",      "Nhẫn Lam Ngọc",          "Nhẫn vàng nạm lam ngọc tăng độ chuẩn xác của chú thuật.",       CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Rare,       13, 20,  0,  8,  1,  4, 2, 0.05f),
                new EquipmentBlueprint("mage_acc_rare_3",      "Cổ Thư Băng Giá",        "Sách phép băng tinh chứa nhiều công thức kết giới lạnh.",       CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Rare,       16, 24,  0,  6,  2,  6, 1, 0.03f),
                new EquipmentBlueprint("mage_acc_rare_4",      "Đèn Ma Thuật",           "Chiếc đèn lồng phát sáng dẫn đường và nâng cao ma công.",       CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Rare,       14, 23,  0,  7,  1,  5, 2, 0.04f),
                new EquipmentBlueprint("mage_acc_epic",        "Ấn Ký Cổ Ngữ",           "Cổ vật chạm khắc ký tự cổ gia tăng trí tuệ và chí mạng phép.",   CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Epic,       24, 35,  0, 13,  3,  9, 2, 0.07f),
                new EquipmentBlueprint("mage_acc_epic_2",      "Quả Cầu Tinh Tú",        "Quả cầu xoay vần tinh vân cung cấp nguồn mana gần như vô tận.",  CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Epic,       26, 38,  0, 13,  3, 10, 3, 0.06f),
                new EquipmentBlueprint("mage_acc_epic_3",      "Ma Điển Hỏa Long",       "Cuốn ma điển bọc da rồng chứa đựng bí thuật thiêu đốt.",        CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Epic,       23, 34,  0, 15,  3,  8, 2, 0.08f),
                new EquipmentBlueprint("mage_acc_legendary",   "Mắt Rồng Ma Thuật",      "Viễn cổ bảo ngọc hội tụ tinh hoa tri thức, bứt phá ma pháp.",    CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Legendary,  40, 55,  0, 22,  6, 14, 4, 0.11f),
                new EquipmentBlueprint("mage_acc_legendary_2", "Thánh Thư Hư Không",     "Cuốn thiên thư bị phong ấn giúp thi triển đại pháp thuật tức thì.",CharacterClassType.Mage,  EquipmentSlot.Accessory, ItemRarity.Legendary,  42, 58,  0, 23,  6, 15, 4, 0.12f),

                // ================================================================
                // 3. ARCHER (CUNG THỦ) - 52 MÓN (13 Vũ Khí, 13 Mũ, 13 Giáp, 13 PK)
                // ================================================================
                // --- Vũ Khí Archer (13) ---
                new EquipmentBlueprint("archer_weapon_common",      "Cung Gỗ Rừng",           "Chiếc cung săn gọn nhẹ dễ kéo dây và bắn nhanh.",                CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Common,      6,  0,  6,  0,  0,  0, 1, 0.03f),
                new EquipmentBlueprint("archer_weapon_common_2",    "Cung Tre Nhẹ",           "Cung uốn từ tre già đàn hồi tốt, ra tên cực kỳ thanh thoát.",   CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Common,      5,  0,  5,  0,  0,  0, 2, 0.04f),
                new EquipmentBlueprint("archer_weapon_common_3",    "Nỏ Gỗ Thợ Săn",          "Nỏ gỗ săn thú nhỏ gọn có chốt khóa giúp giữ độ ổn định.",       CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Common,      7,  0,  7,  0,  1,  0, 0, 0.03f),
                new EquipmentBlueprint("archer_weapon_common_4",    "Cung Săn Gai",           "Cung rừng gia cố dây gân thú tăng lực xuyên tên ở tầm trung.",   CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Common,      8,  2,  6,  0,  1,  0, 1, 0.03f),
                new EquipmentBlueprint("archer_weapon_rare",        "Cung Sừng Hươu",         "Cung ghép sừng tăng lực căng dây và độ chuẩn xác.",              CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Rare,       14,  5, 12,  0,  1,  0, 2, 0.06f),
                new EquipmentBlueprint("archer_weapon_rare_2",      "Cung Thép Bạc",          "Cánh cung rèn thép đàn hồi cao phóng mũi tên bay cực xa.",      CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Rare,       14,  4, 13,  0,  2,  1, 2, 0.06f),
                new EquipmentBlueprint("archer_weapon_rare_3",      "Nỏ Sắt Xuyên Giáp",      "Nỏ cơ khí lực căng mạnh xuyên thủng giáp sắt cự ly gần.",       CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Rare,       16,  5, 14,  0,  2,  1, 1, 0.07f),
                new EquipmentBlueprint("archer_weapon_rare_4",      "Cung Lông Vũ",           "Cung khảm lông chim ưng giúp đường tên lướt gió êm ái.",        CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Rare,       13,  6, 12,  0,  1,  1, 3, 0.07f),
                new EquipmentBlueprint("archer_weapon_epic",        "Trường Cung Bão Tố",     "Mỗi mũi tên xé gió mang theo uy lực xuyên phá chí mạng.",        CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Epic,       24, 10, 20,  0,  3,  2, 3, 0.10f),
                new EquipmentBlueprint("archer_weapon_epic_2",      "Cung Nguyệt Ảnh",        "Lấp lánh ánh trăng tím giúp mũi tên bám đuổi mục tiêu.",         CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Epic,       23, 12, 20,  0,  3,  3, 4, 0.11f),
                new EquipmentBlueprint("archer_weapon_epic_3",      "Nỏ Liên Hoàn Hoàng Kim", "Bảo nỏ mạ vàng có ổ đạn phép bắn liên tiếp chuẩn xác.",        CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Epic,       26, 10, 22,  0,  4,  2, 3, 0.10f),
                new EquipmentBlueprint("archer_weapon_legendary",   "Thần Cung Nhật Thực",    "Cung thần bắn ra ánh sáng mặt trời, bách phát bách trúng.",      CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Legendary,  38, 18, 30,  0,  5,  4, 5, 0.16f),
                new EquipmentBlueprint("archer_weapon_legendary_2", "Thần Cung Thiên Phong",  "Thần cung kết tinh gió trời cho phép bắn nhanh như chớp giật.",  CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Legendary,  36, 20, 29,  0,  5,  5, 6, 0.17f),

                // --- Mũ Archer (13) ---
                new EquipmentBlueprint("archer_helm_common",      "Mũ Da Trùm Đầu",         "Mũ trùm da giúp xạ thủ ẩn mình giữa tán rừng rậm.",             CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Common,      9,  2,  2,  0,  2,  1, 1, 0.01f),
                new EquipmentBlueprint("archer_helm_common_2",    "Mũ Vải Rừng Xanh",       "Mũ vải xanh che nắng mưa mà vẫn gọn gàng khi ngắm bắn.",        CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Common,     10,  2,  1,  0,  2,  1, 1, 0.02f),
                new EquipmentBlueprint("archer_helm_common_3",    "Băng Đô Trinh Sát",      "Băng trán gọn nhẹ giữ tóc không che tầm nhìn chiến trường.",     CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Common,      8,  3,  2,  0,  1,  1, 2, 0.02f),
                new EquipmentBlueprint("archer_helm_common_4",    "Mũ Lá Cắm Ngang",        "Mũ thợ săn gắn lông trắng biểu tượng của đội tuần rừng.",       CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Common,      9,  2,  2,  0,  2,  1, 1, 0.02f),
                new EquipmentBlueprint("archer_helm_rare",        "Mũ Lông Vũ Thợ Săn",     "Gắn lông chim sơn ca tăng phản xạ và độ chuẩn khi giương cung.", CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Rare,       18,  5,  4,  0,  4,  3, 2, 0.04f),
                new EquipmentBlueprint("archer_helm_rare_2",      "Mũ Trùm Rừng Sâu",       "Mũ trùm màu ngọc lục ngụy trang tuyệt đối trong khu rừng cổ.",  CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Rare,       20,  6,  4,  0,  5,  3, 2, 0.03f),
                new EquipmentBlueprint("archer_helm_rare_3",      "Mũ Bạc Ưng Nhãn",        "Gia cố chỏm thép bạc mỏng bảo vệ đầu và trợ giúp thị lực.",     CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Rare,       19,  4,  5,  0,  5,  3, 2, 0.04f),
                new EquipmentBlueprint("archer_helm_rare_4",      "Băng Đô Phong Linh",     "Băng trán thêu bùa gió tăng mạnh tốc độ phản ứng.",              CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Rare,       17,  6,  4,  0,  4,  4, 3, 0.04f),
                new EquipmentBlueprint("archer_helm_epic",        "Mũ Trùm Bóng Đêm",       "Mũ trùm tím thẫm giúp xạ thủ ẩn bóng và tung đòn chí mạng.",    CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Epic,       30, 10,  7,  0,  7,  6, 4, 0.06f),
                new EquipmentBlueprint("archer_helm_epic_2",      "Mũ Lông Cánh Bão Tố",    "Mũ xanh gắn lông vũ sấm gió tăng uy lực mọi phát bắn.",        CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Epic,       32, 10,  8,  0,  8,  6, 4, 0.06f),
                new EquipmentBlueprint("archer_helm_epic_3",      "Mũ Cánh Bạc Nguyệt",     "Mũ gắn đôi cánh ánh trăng giúp né tránh nhẹ tựa lông hồng.",     CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Epic,       31, 12,  7,  0,  7,  7, 5, 0.07f),
                new EquipmentBlueprint("archer_helm_legendary",   "Mũ Gió Thần Đại Bàng",   "Đôi cánh đại bàng gió thiêng ban tầm nhìn thấu suốt chiến địa.", CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Legendary,  48, 16, 12,  0, 11, 10, 6, 0.10f),
                new EquipmentBlueprint("archer_helm_legendary_2", "Vương Miện Nhật Thực",   "Vương miện ánh dương huyền thoại tăng tối đa uy lực xạ kích.",   CharacterClassType.Archer, EquipmentSlot.Helmet,    ItemRarity.Legendary,  50, 18, 13,  0, 12, 10, 6, 0.11f),

                // --- Giáp Archer (13) ---
                new EquipmentBlueprint("archer_armor_common",      "Áo Da Thợ Săn",         "Áo da mềm linh hoạt không làm giảm tốc độ di chuyển.",           CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Common,     15,  0,  1,  0,  3,  2, 1, 0.01f),
                new EquipmentBlueprint("archer_armor_common_2",    "Áo Xanh Trinh Sát",     "Áo vải xanh thêu dây đeo tên nhẹ nhàng, tiện lợi di chuyển.",    CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Common,     14,  2,  1,  0,  3,  2, 1, 0.02f),
                new EquipmentBlueprint("archer_armor_common_3",    "Giáp Vảy Gỗ Rừng",      "Giáp ghép vỏ cây cứng gia công tinh tế chống trầy xước tốt.",    CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Common,     17,  0,  1,  0,  4,  2, 1, 0.01f),
                new EquipmentBlueprint("archer_armor_common_4",    "Áo Choàng Du Kích",     "Áo choàng ngắn cản gió bụi trên đường hành quân xa.",           CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Common,     15,  2,  2,  0,  3,  2, 1, 0.01f),
                new EquipmentBlueprint("archer_armor_rare",        "Áo Choàng Rừng Sâu",    "Trang phục tuần tra giúp lẩn tránh và né đòn hiệu quả.",         CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Rare,       30,  5,  4,  0,  6,  4, 2, 0.03f),
                new EquipmentBlueprint("archer_armor_rare_2",      "Giáp Bạc Tuần Tra",     "Áo da lót các phiến bạc nhẹ chống tên lạc và tăng sức bền.",    CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Rare,       32,  4,  4,  0,  7,  4, 2, 0.03f),
                new EquipmentBlueprint("archer_armor_rare_3",      "Giáp Vảy Ngọc Lục",     "Lớp vảy ngọc lục bảo lấp lánh tăng mạnh kháng phép và né đòn.",  CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Rare,       30,  6,  4,  0,  6,  5, 3, 0.03f),
                new EquipmentBlueprint("archer_armor_rare_4",      "Áo Choàng Ưng Nhãn",    "Áo choàng lam thanh lịch giúp cử động cánh tay cực kỳ linh hoạt.",CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Rare,       29,  6,  5,  0,  6,  4, 3, 0.04f),
                new EquipmentBlueprint("archer_armor_epic",        "Giáp Bóng Đêm",         "Bộ giáp nhẹ màu mực tối ưu tốc độ ra đòn và chí mạng.",          CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Epic,       50, 10,  8,  0, 11,  8, 4, 0.06f),
                new EquipmentBlueprint("archer_armor_epic_2",      "Giáp Bão Tố Xanh",      "Lớp vảy lam bão tố giúp xạ thủ lướt qua làn tên đạn dễ dàng.",   CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Epic,       52, 10,  8,  0, 12,  8, 4, 0.06f),
                new EquipmentBlueprint("archer_armor_epic_3",      "Bào Nguyệt Ảnh",        "Trang phục ánh trăng nhẹ tựa sương đêm, tăng công và né tránh.",  CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Epic,       48, 12,  9,  0, 10, 10, 5, 0.07f),
                new EquipmentBlueprint("archer_armor_legendary",   "Áo Choàng Gió Thần",    "Phúc lành của thần gió giúp Cung Thủ nhanh tựa tia chớp.",       CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Legendary,  78, 18, 14,  0, 16, 12, 6, 0.09f),
                new EquipmentBlueprint("archer_armor_legendary_2", "Giáp Nhật Thực",        "Thánh giáp ánh dương vàng rực công thủ toàn diện ngoài chiến địa.",CharacterClassType.Archer, EquipmentSlot.Armor,    ItemRarity.Legendary,  82, 16, 15,  0, 17, 13, 6, 0.10f),

                // --- Phụ Kiện Archer (13) ---
                new EquipmentBlueprint("archer_acc_common",      "Ống Tên Lông Vũ",        "Ống tên cân bằng giúp rút tên nhanh gọn trong giao tranh.",      CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Common,      8,  4,  3,  0,  1,  0, 1, 0.02f),
                new EquipmentBlueprint("archer_acc_common_2",    "Găng Da Săn Bắn",        "Găng tay hở ngón bảo vệ khớp tay khi kéo dây cung liên tục.",    CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Common,      7,  2,  3,  0,  1,  1, 1, 0.03f),
                new EquipmentBlueprint("archer_acc_common_3",    "Móc Dây Cung Gỗ",        "Móc nhỏ bằng gỗ sồi giúp thả dây mượt mà và ổn định hơn.",       CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Common,      6,  3,  3,  0,  1,  0, 2, 0.03f),
                new EquipmentBlueprint("archer_acc_common_4",    "Túi Tên Cơ Bản",         "Túi tên da nhẹ chứa được nhiều mũi tên dự phòng.",              CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Common,      9,  4,  3,  0,  1,  1, 1, 0.02f),
                new EquipmentBlueprint("archer_acc_rare",        "Bao Tay Ưng Nhãn",       "Bao tay da tinh xảo giúp ngắm bắn vào điểm yếu kẻ địch.",        CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Rare,       16,  8,  6,  0,  2,  2, 2, 0.05f),
                new EquipmentBlueprint("archer_acc_rare_2",      "Ống Tên Thép Bạc",       "Ống tên nạm bạc chứa những mũi tên cân bằng khí động học.",     CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Rare,       18,  7,  7,  0,  3,  2, 2, 0.05f),
                new EquipmentBlueprint("archer_acc_rare_3",      "Nhẫn Thợ Săn",           "Nhẫn kéo cung ngọc lục bảo tăng lực bật dây và sát thương.",     CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Rare,       15,  8,  7,  0,  2,  2, 3, 0.06f),
                new EquipmentBlueprint("archer_acc_rare_4",      "Bùa Gió Rừng",           "Chiếc bùa lông xanh tăng nhịp rút tên và né tránh nhẹ.",         CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Rare,       16,  9,  6,  0,  2,  3, 3, 0.05f),
                new EquipmentBlueprint("archer_acc_epic",        "Dây Chuyền Phong Linh",  "Bùa hộ mệnh linh khí gió tăng tốc đánh và sát thương chuẩn.",    CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Epic,       28, 14, 11,  0,  5,  4, 4, 0.09f),
                new EquipmentBlueprint("archer_acc_epic_2",      "Ống Tên Bão Tố",         "Ống tên tụ điện nạp uy lực sấm sét vào từng mũi tên.",          CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Epic,       30, 14, 12,  0,  5,  5, 4, 0.09f),
                new EquipmentBlueprint("archer_acc_epic_3",      "Kính Ngắm Tinh Anh",     "Thấu kính thủy tinh phép thuật giúp ngắm trúng điểm yếu chí mạng.",CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Epic,       26, 16, 11,  0,  4,  5, 4, 0.11f),
                new EquipmentBlueprint("archer_acc_legendary",   "Lông Đại Bàng Vàng",     "Thánh vật bầu trời đẩy tốc độ và chí mạng lên đỉnh cao.",        CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Legendary,  45, 22, 18,  0,  8,  7, 6, 0.14f),
                new EquipmentBlueprint("archer_acc_legendary_2", "Cánh Gió Thần Tốc",      "Đôi cánh gió thần đeo sau vai giúp xạ thủ bắn nhanh như chớp.",  CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Legendary,  44, 24, 17,  0,  8,  8, 7, 0.15f)
            };
        }

        public static void ApplyBlueprint(EquipmentItemData item, EquipmentBlueprint bp)
        {
            if (item == null) return;
            item.itemId = bp.id;
            item.itemName = bp.name;
            item.description = bp.desc;
            item.requiredClass = bp.cls;
            item.slot = bp.slot;
            item.rarity = bp.rarity;
            item.bonusMaxHP = bp.hp;
            item.bonusMaxMP = bp.mp;
            item.bonusPhysicalAttack = bp.atk;
            item.bonusMagicAttack = bp.mag;
            item.bonusPhysicalDefense = bp.def;
            item.bonusMagicDefense = bp.mdef;
            item.bonusSpeed = bp.spd;
            item.bonusCritRate = bp.crit;
        }

        public static EquipmentCatalog CreateRuntimeFallbackCatalog()
        {
            EquipmentCatalog catalog = CreateInstance<EquipmentCatalog>();
            catalog.EnsureAllDefaultBlueprintsPresent();
            return catalog;
        }

        /// <summary>
        /// Bổ sung tự động mọi trang bị còn thiếu từ danh sách mặc định (hữu ích khi asset cũ chưa rebuild qua Editor).
        /// </summary>
        public void EnsureAllDefaultBlueprintsPresent()
        {
            if (allItems == null) allItems = new List<EquipmentItemData>();
            RebuildLookup();
            EquipmentBlueprint[] blueprints = GetDefaultBlueprints();
            for (int i = 0; i < blueprints.Length; i++)
            {
                EquipmentBlueprint bp = blueprints[i];
                if (GetById(bp.id) != null) continue;
                EquipmentItemData item = CreateInstance<EquipmentItemData>();
                item.name = bp.id;
                ApplyBlueprint(item, bp);
                allItems.Add(item);
            }
            RebuildLookup();
        }

        public void RebuildLookup()
        {
            _lookupById = new Dictionary<string, EquipmentItemData>();
            if (allItems == null) return;
            foreach (EquipmentItemData item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemId))
                {
                    _lookupById[item.itemId] = item;
                }
            }
        }

        public EquipmentItemData GetById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (_lookupById == null || _lookupById.Count != allItems.Count)
            {
                RebuildLookup();
            }
            return _lookupById != null && _lookupById.TryGetValue(itemId, out EquipmentItemData found) ? found : null;
        }

        public List<EquipmentItemData> GetItemsByRarity(ItemRarity rarity)
        {
            List<EquipmentItemData> matches = new List<EquipmentItemData>();
            if (allItems == null) return matches;
            for (int i = 0; i < allItems.Count; i++)
            {
                if (allItems[i] != null && allItems[i].rarity == rarity)
                {
                    matches.Add(allItems[i]);
                }
            }
            return matches;
        }

        public EquipmentItemData PickRandomItem(ItemRarity rarity, System.Random rng = null)
        {
            List<EquipmentItemData> pool = GetItemsByRarity(rarity);
            if (pool.Count == 0 && allItems != null && allItems.Count > 0)
            {
                pool = allItems;
            }
            if (pool.Count == 0) return null;

            int idx = rng != null ? rng.Next(0, pool.Count) : UnityEngine.Random.Range(0, pool.Count);
            return pool[idx];
        }
    }
}
