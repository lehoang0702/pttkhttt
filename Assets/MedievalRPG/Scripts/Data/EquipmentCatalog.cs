using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    /// <summary>
    /// Danh mục trang bị tập trung (36 trang bị: 3 Class x 3 Slot x 4 Độ hiếm Thường/Hiếm/Sử Thi/Huyền Thoại).
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
                // ==================== KNIGHT (HIỆP SĨ) ====================
                // Vũ khí
                new EquipmentBlueprint("knight_weapon_common",    "Kiếm Sắt Rỉ",           "Thanh kiếm sắt đơn giản của lính gác cổng thành.",               CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Common,     10,  0,  5,  0,  1,  0, 0, 0.01f),
                new EquipmentBlueprint("knight_weapon_rare",      "Đại Kiếm Thép Rèn",     "Kiếm thép rèn kỹ giúp đòn chém uy lực và vững vàng hơn.",        CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Rare,       20,  0, 10,  0,  3,  0, 0, 0.03f),
                new EquipmentBlueprint("knight_weapon_epic",      "Thánh Kiếm Bình Minh",  "Lưỡi kiếm ánh vàng xua tan bóng tối, khắc chế quái hầm ngục.",   CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Epic,       35,  5, 17,  0,  5,  2, 1, 0.06f),
                new EquipmentBlueprint("knight_weapon_legendary", "Kiếm Rồng Huyền Thoại", "Thần kiếm rèn từ vảy rồng cổ đại, chém nát mọi phòng tuyến.",    CharacterClassType.Knight, EquipmentSlot.Weapon,    ItemRarity.Legendary,  55, 10, 26,  0,  8,  4, 2, 0.10f),
                // Giáp
                new EquipmentBlueprint("knight_armor_common",     "Giáp Da Chiến Binh",    "Bộ giáp da dày chống chịu tốt các vết cào cơ bản.",              CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Common,     20,  0,  0,  0,  4,  2, 0, 0.00f),
                new EquipmentBlueprint("knight_armor_rare",       "Giáp Sắt Hiệp Sĩ",      "Giáp tấm hợp kim bảo vệ toàn thân của kỵ sĩ hoàng gia.",         CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Rare,       40,  0,  2,  0,  8,  4, 0, 0.00f),
                new EquipmentBlueprint("knight_armor_epic",       "Giáp Chiến Hoàng Kim",  "Chiến giáp mạ vàng khắc cổ ngữ tăng sức bền vượt trội.",         CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Epic,       68,  5,  4,  0, 14,  8, 1, 0.00f),
                new EquipmentBlueprint("knight_armor_legendary",  "Thần Giáp Bất Diệt",    "Bộ giáp truyền thuyết giúp Hiệp Sĩ đứng vững trước bão đòn.",    CharacterClassType.Knight, EquipmentSlot.Armor,     ItemRarity.Legendary, 105, 10,  8,  0, 22, 14, 2, 0.02f),
                // Phụ kiện
                new EquipmentBlueprint("knight_acc_common",       "Khiên Gỗ Sồi",          "Tấm khiên gỗ cứng giúp đỡ đòn trực diện.",                       CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Common,     15,  5,  1,  0,  3,  1, 0, 0.00f),
                new EquipmentBlueprint("knight_acc_rare",         "Khiên Thép Hộ Vệ",      "Khiên thép kiên cố tăng giáp vật lý và sức sống.",               CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Rare,       30,  8,  3,  0,  6,  3, 1, 0.02f),
                new EquipmentBlueprint("knight_acc_epic",         "Huy Hiệu Kỵ Sĩ Đỏ",     "Huy hiệu danh dự tiếp thêm dũng khí và phản xạ chiến trường.",   CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Epic,       48, 12,  7,  0, 10,  6, 2, 0.04f),
                new EquipmentBlueprint("knight_acc_legendary",    "Trái Tim Phượng Hoàng", "Báu vật bất tử ban sinh lực dồi dào và uy thế áp đảo.",          CharacterClassType.Knight, EquipmentSlot.Accessory, ItemRarity.Legendary,  80, 18, 12,  0, 15, 10, 3, 0.06f),

                // ==================== MAGE (PHÁP SƯ) ====================
                // Vũ khí
                new EquipmentBlueprint("mage_weapon_common",      "Gậy Gỗ Cổ Thụ",         "Cây gậy tập sự dẫn truyền ma lực nguyên tố cơ bản.",             CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Common,      5,  8,  0,  6,  0,  1, 0, 0.01f),
                new EquipmentBlueprint("mage_weapon_rare",        "Trượng Ngọc Lam",       "Đầu trượng khảm lam ngọc khuếch đại sát thương phép.",           CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Rare,       10, 15,  0, 12,  0,  3, 1, 0.03f),
                new EquipmentBlueprint("mage_weapon_epic",        "Trượng Tinh Tú",        "Tập trung ánh sao trời, tăng mạnh sát thương phép và hồi máu.",  CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Epic,       18, 25,  0, 21,  2,  6, 2, 0.06f),
                new EquipmentBlueprint("mage_weapon_legendary",   "Quyền Trượng Hư Không", "Bảo vật tối thượng của Đại Pháp Sư, bùng nổ ma thuật hủy diệt.", CharacterClassType.Mage,   EquipmentSlot.Weapon,    ItemRarity.Legendary,  28, 40,  0, 32,  4, 10, 3, 0.10f),
                // Giáp
                new EquipmentBlueprint("mage_armor_common",       "Áo Choàng Vải Dệt",     "Áo choàng nhẹ dệt sợi ma thuật giúp giảm tổn thương phép.",      CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Common,     12, 10,  0,  2,  2,  4, 0, 0.00f),
                new EquipmentBlueprint("mage_armor_rare",         "Áo Pháp Sư Học Viện",   "Lễ phục tháp pháp sư tăng mạnh năng lượng và kháng phép.",       CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Rare,       24, 20,  0,  5,  4,  8, 1, 0.00f),
                new EquipmentBlueprint("mage_armor_epic",         "Bào Phục Ánh Trăng",    "Dệt từ tơ ánh trăng, bảo hộ sinh mệnh và khuếch đại ma lực.",    CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Epic,       42, 32,  0, 10,  8, 14, 2, 0.02f),
                new EquipmentBlueprint("mage_armor_legendary",    "Thánh Bào Nguyên Tố",   "Áo choàng hòa quyện tứ nguyên tố, kháng cự mọi ma lực hắc ám.",  CharacterClassType.Mage,   EquipmentSlot.Armor,     ItemRarity.Legendary,  68, 50,  0, 16, 13, 22, 3, 0.05f),
                // Phụ kiện
                new EquipmentBlueprint("mage_acc_common",         "Nhẫn Thủy Tinh",        "Chiếc nhẫn nhỏ tích trữ một lượng mana ổn định.",                CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Common,      8, 12,  0,  3,  0,  2, 0, 0.02f),
                new EquipmentBlueprint("mage_acc_rare",           "Ngọc Năng Lượng",       "Viên ngọc tỏa sáng xanh giúp thi triển phép nhanh hơn.",         CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Rare,       14, 22,  0,  7,  1,  5, 1, 0.04f),
                new EquipmentBlueprint("mage_acc_epic",           "Ấn Ký Cổ Ngữ",          "Cổ vật chạm khắc ký tự cổ gia tăng trí tuệ và chí mạng phép.",   CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Epic,       24, 35,  0, 13,  3,  9, 2, 0.07f),
                new EquipmentBlueprint("mage_acc_legendary",      "Mắt Rồng Ma Thuật",     "Viễn cổ bảo ngọc hội tụ tinh hoa tri thức, bứt phá ma pháp.",    CharacterClassType.Mage,   EquipmentSlot.Accessory, ItemRarity.Legendary,  40, 55,  0, 22,  6, 14, 4, 0.11f),

                // ==================== ARCHER (CUNG THỦ) ====================
                // Vũ khí
                new EquipmentBlueprint("archer_weapon_common",    "Cung Gỗ Rừng",          "Chiếc cung săn gọn nhẹ dễ kéo dây và bắn nhanh.",                CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Common,      6,  0,  6,  0,  0,  0, 1, 0.03f),
                new EquipmentBlueprint("archer_weapon_rare",      "Cung Sừng Hươu",        "Cung ghép sừng tăng lực căng dây và độ chuẩn xác.",              CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Rare,       14,  5, 12,  0,  1,  0, 2, 0.06f),
                new EquipmentBlueprint("archer_weapon_epic",      "Trường Cung Bão Tố",    "Mỗi mũi tên xé gió mang theo uy lực xuyên phá chí mạng.",        CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Epic,       24, 10, 20,  0,  3,  2, 3, 0.10f),
                new EquipmentBlueprint("archer_weapon_legendary", "Thần Cung Nhật Thực",   "Cung thần bắn ra ánh sáng mặt trời, bách phát bách trúng.",      CharacterClassType.Archer, EquipmentSlot.Weapon,    ItemRarity.Legendary,  38, 18, 30,  0,  5,  4, 5, 0.16f),
                // Giáp
                new EquipmentBlueprint("archer_armor_common",     "Áo Da Thợ Săn",         "Áo da mềm linh hoạt không làm giảm tốc độ di chuyển.",           CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Common,     15,  0,  1,  0,  3,  2, 1, 0.01f),
                new EquipmentBlueprint("archer_armor_rare",       "Áo Choàng Rừng Sâu",    "Trang phục tuần tra giúp lẩn tránh và né đòn hiệu quả.",         CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Rare,       30,  5,  4,  0,  6,  4, 2, 0.03f),
                new EquipmentBlueprint("archer_armor_epic",       "Giáp Bóng Đêm",         "Bộ giáp nhẹ màu mực tối ưu tốc độ ra đòn và chí mạng.",          CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Epic,       50, 10,  8,  0, 11,  8, 4, 0.06f),
                new EquipmentBlueprint("archer_armor_legendary",  "Áo Choàng Gió Thần",    "Phúc lành của thần gió giúp Cung Thủ nhanh tựa tia chớp.",       CharacterClassType.Archer, EquipmentSlot.Armor,     ItemRarity.Legendary,  78, 18, 14,  0, 16, 12, 6, 0.09f),
                // Phụ kiện
                new EquipmentBlueprint("archer_acc_common",       "Ống Tên Lông Vũ",       "Ống tên cân bằng giúp rút tên nhanh gọn trong giao tranh.",      CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Common,      8,  4,  3,  0,  1,  0, 1, 0.02f),
                new EquipmentBlueprint("archer_acc_rare",         "Bao Tay Ưng Nhãn",      "Bao tay da tinh xảo giúp ngắm bắn vào điểm yếu kẻ địch.",        CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Rare,       16,  8,  6,  0,  2,  2, 2, 0.05f),
                new EquipmentBlueprint("archer_acc_epic",         "Dây Chuyền Phong Linh", "Bùa hộ mệnh linh khí gió tăng tốc đánh và sát thương chuẩn.",    CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Epic,       28, 14, 11,  0,  5,  4, 4, 0.09f),
                new EquipmentBlueprint("archer_acc_legendary",    "Lông Đại Bàng Vàng",    "Thánh vật bầu trời đẩy tốc độ và chí mạng lên đỉnh cao.",        CharacterClassType.Archer, EquipmentSlot.Accessory, ItemRarity.Legendary,  45, 22, 18,  0,  8,  7, 6, 0.14f)
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
            EquipmentBlueprint[] blueprints = GetDefaultBlueprints();
            foreach (EquipmentBlueprint bp in blueprints)
            {
                EquipmentItemData item = CreateInstance<EquipmentItemData>();
                item.name = bp.id;
                ApplyBlueprint(item, bp);
                catalog.allItems.Add(item);
            }
            catalog.RebuildLookup();
            return catalog;
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
