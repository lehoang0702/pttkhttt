using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    /// <summary>
    /// Quản lý tài nguyên hình ảnh của toàn bộ 156 trang bị (icon trong Resources/EquipmentSprites),
    /// bảng màu chuyển sắc (Palette Swap cho 39 bộ giáp), màu áo choàng và luật tách sạch vũ khí/mũ/phụ kiện mặc định trên frame nhân vật.
    /// </summary>
    public static class EquipmentVisualCatalog
    {
        public readonly struct ColorReplacement
        {
            public readonly Color32 source;
            public readonly Color32 target;

            public ColorReplacement(Color32 source, Color32 target)
            {
                this.source = source;
                this.target = target;
            }
        }

        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _spriteCache.Clear();
            _cutoutHoodCache.Clear();
        }

        public static Sprite GetSpriteForItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (_spriteCache.TryGetValue(itemId, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite loaded = Resources.Load<Sprite>($"EquipmentSprites/eq_{itemId}");
            if (loaded != null && loaded.texture != null && loaded.texture.filterMode != FilterMode.Point)
            {
                loaded.texture.filterMode = FilterMode.Point;
            }

            _spriteCache[itemId] = loaded;
            return loaded;
        }

        private static Color32 Hex(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);

        // Màu gốc trên frame Knight (Giáp tím-xám + áo ngực đỏ)
        private static readonly Color32[] KnightArmorSrc =
        {
            Hex(0x44, 0x2B, 0x61), // Giáp tối
            Hex(0x62, 0x50, 0x7E), // Giáp vừa
            Hex(0x81, 0x70, 0x9A), // Giáp sáng
            Hex(0xA0, 0x96, 0xD1), // Giáp highlight
            Hex(0x8A, 0x0B, 0x41), // Áo ngực tối
            Hex(0xCE, 0x20, 0x38), // Áo ngực sáng
        };

        // Màu gốc trên frame Mage (Áo choàng + mũ trùm tím)
        private static readonly Color32[] MageRobeSrc =
        {
            Hex(0x25, 0x1E, 0x53),
            Hex(0x3F, 0x36, 0x7A),
            Hex(0x3B, 0x33, 0x77),
            Hex(0x47, 0x28, 0x99),
            Hex(0x6E, 0x49, 0xCD),
            Hex(0x97, 0x6E, 0xFF),
            Hex(0x6F, 0x1F, 0x73),
            Hex(0x99, 0x36, 0x9E),
        };

        // Màu gốc trên frame Archer (Áo da xanh rừng + găng + ủng)
        private static readonly Color32[] ArcherArmorSrc =
        {
            Hex(0x1F, 0x55, 0x1A),
            Hex(0x20, 0x3E, 0x26),
            Hex(0x21, 0x64, 0x1A),
            Hex(0x2C, 0x78, 0x24),
            Hex(0x3A, 0x6A, 0x43),
            Hex(0x5C, 0x98, 0x67),
        };

        private static Color32[] MakeKnightPalette(Color32 dark, Color32 mid, Color32 light, Color32 hi, Color32 tabDark, Color32 tabLight)
            => new[] { dark, mid, light, hi, tabDark, tabLight };

        private static Color32[] MakeMagePalette(Color32 r0, Color32 r1, Color32 r2, Color32 r3, Color32 r4, Color32 r5, Color32 trim0, Color32 trim1)
            => new[] { r0, r1, r2, r3, r4, r5, trim0, trim1 };

        private static Color32[] MakeArcherPalette(Color32 d0, Color32 d1, Color32 d2, Color32 m0, Color32 l0, Color32 hi)
            => new[] { d0, d1, d2, m0, l0, hi };

        private static readonly Dictionary<string, Color32[]> ArmorPalettes = new Dictionary<string, Color32[]>(StringComparer.Ordinal)
        {
            // ==================== KNIGHT: 13 BỘ GIÁP ====================
            ["knight_armor_common"]      = MakeKnightPalette(Hex(82, 50, 30),  Hex(116, 74, 44), Hex(152, 100, 58), Hex(188, 134, 82),  Hex(54, 82, 46),  Hex(78, 122, 66)),
            ["knight_armor_common_2"]    = MakeKnightPalette(Hex(64, 68, 76),  Hex(94, 100, 110),Hex(132, 138, 148),Hex(172, 180, 192), Hex(112, 88, 52), Hex(162, 132, 82)),
            ["knight_armor_common_3"]    = MakeKnightPalette(Hex(84, 54, 24),  Hex(124, 82, 38), Hex(168, 118, 64), Hex(208, 168, 86),  Hex(72, 46, 32),  Hex(114, 72, 44)),
            ["knight_armor_common_4"]    = MakeKnightPalette(Hex(68, 28, 34),  Hex(102, 42, 50), Hex(142, 62, 72),  Hex(176, 178, 188), Hex(112, 34, 42), Hex(158, 56, 66)),
            ["knight_armor_rare"]        = MakeKnightPalette(Hex(72, 86, 112), Hex(118, 136, 168),Hex(172, 192, 222),Hex(224, 236, 250),Hex(28, 74, 152), Hex(48, 124, 218)),
            ["knight_armor_rare_2"]      = MakeKnightPalette(Hex(32, 62, 118), Hex(52, 96, 168), Hex(82, 142, 222), Hex(210, 232, 252), Hex(36, 84, 164), Hex(88, 168, 244)),
            ["knight_armor_rare_3"]      = MakeKnightPalette(Hex(68, 84, 104), Hex(104, 126, 154),Hex(156, 178, 206),Hex(232, 196, 82), Hex(128, 92, 34), Hex(198, 154, 56)),
            ["knight_armor_rare_4"]      = MakeKnightPalette(Hex(96, 62, 22),  Hex(146, 98, 34), Hex(204, 154, 62), Hex(246, 208, 98),  Hex(38, 92, 164), Hex(68, 148, 232)),
            ["knight_armor_epic"]        = MakeKnightPalette(Hex(138, 90, 22), Hex(194, 138, 34),Hex(242, 190, 62), Hex(255, 232, 128), Hex(108, 32, 152),Hex(158, 56, 212)),
            ["knight_armor_epic_2"]      = MakeKnightPalette(Hex(74, 16, 24),  Hex(118, 24, 38), Hex(178, 42, 58),  Hex(238, 188, 72),  Hex(148, 22, 32), Hex(212, 48, 52)),
            ["knight_armor_epic_3"]      = MakeKnightPalette(Hex(96, 122, 164),Hex(148, 174, 214),Hex(206, 224, 248),Hex(252, 214, 86), Hex(162, 122, 34),Hex(245, 196, 68)),
            ["knight_armor_legendary"]   = MakeKnightPalette(Hex(52, 20, 28),  Hex(96, 32, 42),  Hex(154, 48, 48),  Hex(240, 184, 58),  Hex(185, 28, 24), Hex(245, 76, 44)),
            ["knight_armor_legendary_2"] = MakeKnightPalette(Hex(28, 36, 78),  Hex(48, 60, 124), Hex(82, 102, 186), Hex(162, 224, 255), Hex(156, 118, 24),Hex(248, 206, 72)),

            // ==================== MAGE: 13 BỘ ÁO CHOÀNG ====================
            ["mage_armor_common"]      = MakeMagePalette(Hex(58, 44, 34),  Hex(78, 60, 46),  Hex(76, 58, 44),  Hex(98, 76, 56),  Hex(130, 102, 74),Hex(164, 134, 98),Hex(86, 108, 68), Hex(120, 148, 92)),
            ["mage_armor_common_2"]    = MakeMagePalette(Hex(54, 32, 74),  Hex(78, 46, 104), Hex(74, 44, 98),  Hex(104, 64, 132),Hex(138, 92, 168),Hex(196, 168, 106),Hex(126, 94, 52),Hex(188, 152, 88)),
            ["mage_armor_common_3"]    = MakeMagePalette(Hex(34, 56, 40),  Hex(48, 78, 56),  Hex(46, 74, 54),  Hex(68, 104, 78), Hex(98, 142, 106),Hex(168, 188, 128),Hex(122, 98, 54),Hex(182, 154, 94)),
            ["mage_armor_common_4"]    = MakeMagePalette(Hex(42, 48, 68),  Hex(58, 68, 96),  Hex(56, 64, 92),  Hex(82, 96, 132), Hex(116, 132, 170),Hex(178, 174, 162),Hex(96, 94, 88),Hex(156, 152, 140)),
            ["mage_armor_rare"]        = MakeMagePalette(Hex(22, 42, 92),  Hex(34, 64, 132), Hex(32, 62, 128), Hex(46, 92, 184), Hex(72, 138, 234),Hex(132, 188, 255),Hex(42, 106, 178),Hex(76, 158, 228)),
            ["mage_armor_rare_2"]      = MakeMagePalette(Hex(48, 92, 142), Hex(74, 136, 198),Hex(70, 128, 190),Hex(108, 174, 234),Hex(154, 214, 252),Hex(232, 248, 255),Hex(88, 156, 212),Hex(164, 224, 255)),
            ["mage_armor_rare_3"]      = MakeMagePalette(Hex(78, 18, 22),  Hex(118, 28, 32), Hex(112, 26, 30), Hex(162, 42, 40), Hex(208, 66, 54), Hex(250, 192, 74), Hex(156, 112, 32),Hex(238, 184, 62)),
            ["mage_armor_rare_4"]      = MakeMagePalette(Hex(24, 66, 46),  Hex(38, 98, 68),  Hex(36, 92, 64),  Hex(54, 138, 96), Hex(82, 184, 128),Hex(218, 206, 96), Hex(144, 124, 42),Hex(212, 192, 78)),
            ["mage_armor_epic"]        = MakeMagePalette(Hex(32, 48, 86),  Hex(56, 82, 134), Hex(54, 78, 128), Hex(78, 114, 178),Hex(146, 184, 236),Hex(202, 228, 255),Hex(92, 118, 172),Hex(168, 200, 246)),
            ["mage_armor_epic_2"]      = MakeMagePalette(Hex(46, 22, 82),  Hex(74, 36, 126), Hex(68, 34, 118), Hex(106, 54, 168),Hex(148, 88, 216),Hex(248, 204, 76), Hex(154, 114, 32),Hex(236, 186, 62)),
            ["mage_armor_epic_3"]      = MakeMagePalette(Hex(22, 34, 84),  Hex(34, 52, 122), Hex(32, 48, 118), Hex(52, 82, 172), Hex(86, 128, 224),Hex(162, 240, 255),Hex(54, 136, 212),Hex(138, 228, 255)),
            ["mage_armor_legendary"]   = MakeMagePalette(Hex(72, 18, 34),  Hex(112, 28, 46), Hex(108, 26, 44), Hex(162, 44, 42), Hex(236, 96, 46), Hex(255, 198, 72), Hex(148, 36, 88), Hex(220, 74, 118)),
            ["mage_armor_legendary_2"] = MakeMagePalette(Hex(38, 12, 68),  Hex(58, 18, 96),  Hex(54, 16, 92),  Hex(94, 32, 146), Hex(144, 54, 208),Hex(255, 208, 74), Hex(132, 34, 172),Hex(224, 96, 248)),

            // ==================== ARCHER: 13 BỘ GIÁP ====================
            ["archer_armor_common"]      = MakeArcherPalette(Hex(76, 48, 28),  Hex(56, 34, 20),  Hex(96, 62, 36),  Hex(128, 84, 48), Hex(152, 104, 62), Hex(184, 136, 84)),
            ["archer_armor_common_2"]    = MakeArcherPalette(Hex(44, 82, 46),  Hex(30, 58, 32),  Hex(56, 104, 58), Hex(72, 128, 74), Hex(128, 104, 62), Hex(172, 140, 82)),
            ["archer_armor_common_3"]    = MakeArcherPalette(Hex(68, 52, 30),  Hex(48, 36, 20),  Hex(88, 66, 38),  Hex(122, 92, 52), Hex(138, 164, 92), Hex(176, 198, 116)),
            ["archer_armor_common_4"]    = MakeArcherPalette(Hex(58, 68, 42),  Hex(40, 48, 28),  Hex(72, 86, 52),  Hex(94, 112, 68), Hex(132, 102, 58), Hex(168, 132, 78)),
            ["archer_armor_rare"]        = MakeArcherPalette(Hex(24, 68, 56),  Hex(18, 52, 44),  Hex(34, 92, 72),  Hex(52, 132, 96), Hex(76, 164, 118), Hex(138, 208, 162)),
            ["archer_armor_rare_2"]      = MakeArcherPalette(Hex(92, 108, 124),Hex(64, 78, 92),  Hex(126, 146, 166),Hex(176, 196, 212),Hex(64, 162, 112),Hex(214, 232, 244)),
            ["archer_armor_rare_3"]      = MakeArcherPalette(Hex(24, 78, 64),  Hex(18, 56, 46),  Hex(34, 104, 86), Hex(54, 156, 126),Hex(182, 152, 62), Hex(232, 198, 86)),
            ["archer_armor_rare_4"]      = MakeArcherPalette(Hex(36, 62, 98),  Hex(24, 42, 68),  Hex(52, 86, 132), Hex(84, 126, 178),Hex(178, 146, 62), Hex(234, 200, 88)),
            ["archer_armor_epic"]        = MakeArcherPalette(Hex(28, 24, 52),  Hex(20, 16, 38),  Hex(42, 36, 76),  Hex(64, 56, 110), Hex(96, 84, 152),  Hex(172, 120, 245)),
            ["archer_armor_epic_2"]      = MakeArcherPalette(Hex(26, 66, 118), Hex(18, 44, 82),  Hex(38, 94, 158), Hex(58, 146, 214),Hex(188, 152, 58), Hex(246, 208, 82)),
            ["archer_armor_epic_3"]      = MakeArcherPalette(Hex(68, 52, 122), Hex(46, 34, 86),  Hex(96, 74, 162), Hex(156, 136, 222),Hex(212, 196, 128),Hex(246, 236, 174)),
            ["archer_armor_legendary"]   = MakeArcherPalette(Hex(48, 112, 98), Hex(38, 88, 78),  Hex(62, 138, 116),Hex(98, 178, 148),Hex(186, 228, 212),Hex(246, 214, 108)),
            ["archer_armor_legendary_2"] = MakeArcherPalette(Hex(82, 26, 36),  Hex(54, 16, 24),  Hex(126, 46, 36), Hex(214, 96, 36), Hex(246, 176, 56), Hex(255, 226, 98)),
        };

        /// <summary>
        /// Tạo danh sách quy tắc đổi màu (Palette Swap giáp/vũ khí) cho khung hình nhân vật.
        /// </summary>
        public static List<ColorReplacement> BuildColorReplacements(
            CharacterClassType cls,
            string armorItemId,
            string weaponItemId)
        {
            List<ColorReplacement> rules = new List<ColorReplacement>(16);

            if (!string.IsNullOrEmpty(armorItemId) && ArmorPalettes.TryGetValue(armorItemId, out Color32[] dst))
            {
                Color32[] src = cls switch
                {
                    CharacterClassType.Knight => KnightArmorSrc,
                    CharacterClassType.Mage => MageRobeSrc,
                    CharacterClassType.Archer => ArcherArmorSrc,
                    _ => null
                };

                if (src != null && dst != null)
                {
                    int len = Mathf.Min(src.Length, dst.Length);
                    for (int i = 0; i < len; i++)
                    {
                        rules.Add(new ColorReplacement(src[i], dst[i]));
                    }
                }
            }

            if (!string.IsNullOrEmpty(weaponItemId))
            {
                if (cls == CharacterClassType.Archer)
                {
                    // Ẩn cây cung gỗ gốc ở mọi animation để cây cung mới khớp tay không bị chồng
                    Color32 clear = Hex(0, 0, 0, 0);
                    rules.Add(new ColorReplacement(Hex(0x61, 0x2A, 0x1D), clear));
                    rules.Add(new ColorReplacement(Hex(0x43, 0x1C, 0x13), clear));
                    rules.Add(new ColorReplacement(Hex(0x57, 0x78, 0x33), clear));
                }
                else if (cls == CharacterClassType.Knight)
                {
                    // Khi Run/Attack, tô màu lưỡi kiếm gốc đồng bộ với vũ khí đang cầm
                    Color32 bladeTint = GetWeaponElementTint(weaponItemId, Hex(212, 218, 228));
                    rules.Add(new ColorReplacement(Hex(0xFC, 0xFC, 0xFC), bladeTint));
                }
                else if (cls == CharacterClassType.Mage)
                {
                    Color32 orbTint = GetWeaponElementTint(weaponItemId, Hex(0x33, 0xDA, 0xFF));
                    rules.Add(new ColorReplacement(Hex(0x33, 0xDA, 0xFF), orbTint));
                }
            }

            return rules;
        }

        private static Color32 GetWeaponElementTint(string itemId, Color32 fallback)
        {
            if (string.IsNullOrEmpty(itemId)) return fallback;
            if (itemId.Contains("_legendary_2")) return itemId.StartsWith("knight") ? Hex(125, 225, 255) : itemId.StartsWith("mage") ? Hex(255, 198, 68) : Hex(145, 255, 215);
            if (itemId.Contains("_legendary"))   return itemId.StartsWith("knight") ? Hex(255, 128, 52)  : itemId.StartsWith("mage") ? Hex(232, 96, 255)  : Hex(255, 208, 68);
            if (itemId.Contains("_epic_2"))      return itemId.StartsWith("knight") ? Hex(248, 76, 86)   : itemId.StartsWith("mage") ? Hex(255, 145, 58)  : Hex(205, 138, 255);
            if (itemId.Contains("_epic_3"))      return itemId.StartsWith("knight") ? Hex(255, 232, 124) : itemId.StartsWith("mage") ? Hex(135, 225, 255) : Hex(255, 204, 78);
            if (itemId.Contains("_epic"))        return Hex(255, 222, 108);
            if (itemId.Contains("_rare_2"))      return Hex(175, 235, 255);
            if (itemId.Contains("_rare_3"))      return itemId.StartsWith("mage") ? Hex(175, 240, 255) : Hex(205, 230, 255);
            if (itemId.Contains("_rare_4"))      return Hex(128, 240, 152);
            if (itemId.Contains("_rare"))        return Hex(168, 218, 255);
            if (itemId.Contains("_common"))      return itemId.StartsWith("mage") ? Hex(118, 228, 136) : fallback;
            return fallback;
        }

        public enum VisualAnimState
        {
            Idle = 0,
            Run = 1,
            Attack = 2,
            Hit = 3
        }

        public enum HelmetVisualMode
        {
            FullReplace = 0,
            BandOverlay = 1,
            WizardHat = 2,
            CutoutHood = 3,
            OpenCap = 4,
            FeatherCap = 5,
            Crown = 6
        }

        public enum AccessoryVisualCategory
        {
            Shield = 0,
            Gauntlet = 1,
            Collar = 2,
            Belt = 3,
            Badge = 4,
            MageFloating = 5,
            ArcherQuiver = 6,
            ArcherWing = 7,
            ArcherFeather = 8,
            ArcherGlove = 9,
            ArcherScope = 10,
            ArcherNecklace = 11
        }

        public readonly struct ItemVisualProfile
        {
            public readonly float anchorX;
            public readonly float anchorY;
            public readonly float charPixelSize;
            public readonly float angleOffset;
            public readonly float yOffsetPx;
            public readonly HelmetVisualMode helmMode;
            public readonly AccessoryVisualCategory accCategory;

            public ItemVisualProfile(
                float anchorX,
                float anchorY,
                float charPixelSize,
                float angleOffset = 0f,
                float yOffsetPx = 0f,
                HelmetVisualMode helmMode = HelmetVisualMode.FullReplace,
                AccessoryVisualCategory accCategory = AccessoryVisualCategory.Shield)
            {
                this.anchorX = anchorX;
                this.anchorY = anchorY;
                this.charPixelSize = charPixelSize;
                this.angleOffset = angleOffset;
                this.yOffsetPx = yOffsetPx;
                this.helmMode = helmMode;
                this.accCategory = accCategory;
            }
        }

        public readonly struct CharacterFrameAnchor
        {
            public readonly float headX, headY, headRotZ;
            public readonly float wpnX, wpnY, wpnRotZ;
            public readonly int wpnOrderOff;
            public readonly bool wpnVisible;
            public readonly float accX, accY, accRotZ;
            public readonly int accOrderOff;
            public readonly float chestX, chestY;
            public readonly float collarX, collarY;
            public readonly float beltX, beltY;
            public readonly float eyeX, eyeY;
            public readonly float gloveX, gloveY;
            public readonly float cloakX, cloakY, cloakRotZ;
            public readonly float staffSegX1, staffSegY1, staffSegX2, staffSegY2;

            public CharacterFrameAnchor(
                float headX, float headY, float headRotZ,
                float wpnX, float wpnY, float wpnRotZ, int wpnOrderOff, bool wpnVisible,
                float accX, float accY, float accRotZ, int accOrderOff,
                float chestX, float chestY,
                float collarX, float collarY,
                float beltX, float beltY,
                float eyeX, float eyeY,
                float gloveX, float gloveY,
                float cloakX, float cloakY, float cloakRotZ,
                float staffSegX1 = 0f, float staffSegY1 = 0f, float staffSegX2 = 0f, float staffSegY2 = 0f)
            {
                this.headX = headX;
                this.headY = headY;
                this.headRotZ = headRotZ;
                this.wpnX = wpnX;
                this.wpnY = wpnY;
                this.wpnRotZ = wpnRotZ;
                this.wpnOrderOff = wpnOrderOff;
                this.wpnVisible = wpnVisible;
                this.accX = accX;
                this.accY = accY;
                this.accRotZ = accRotZ;
                this.accOrderOff = accOrderOff;
                this.chestX = chestX;
                this.chestY = chestY;
                this.collarX = collarX;
                this.collarY = collarY;
                this.beltX = beltX;
                this.beltY = beltY;
                this.eyeX = eyeX;
                this.eyeY = eyeY;
                this.gloveX = gloveX;
                this.gloveY = gloveY;
                this.cloakX = cloakX;
                this.cloakY = cloakY;
                this.cloakRotZ = cloakRotZ;
                this.staffSegX1 = staffSegX1;
                this.staffSegY1 = staffSegY1;
                this.staffSegX2 = staffSegX2;
                this.staffSegY2 = staffSegY2;
            }
        }

        private static readonly Dictionary<string, Sprite> _cutoutHoodCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static Dictionary<string, CharacterFrameAnchor> _frameAnchorLookup;

        public static Sprite GetEquippedSpriteForItem(string itemId, CharacterClassType cls, EquipmentSlot slot)
        {
            Sprite raw = GetSpriteForItem(itemId);
            if (raw == null || slot != EquipmentSlot.Helmet) return raw;

            ItemVisualProfile profile = GetItemVisualProfile(cls, slot, itemId);
            if (profile.helmMode != HelmetVisualMode.CutoutHood) return raw;

            bool sideProfile = cls == CharacterClassType.Archer;
            string cacheKey = $"{itemId}_{(sideProfile ? "S" : "F")}";
            if (_cutoutHoodCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite cutout = CreateCutoutHoodSprite(raw, sideProfile);
            _cutoutHoodCache[cacheKey] = cutout;
            return cutout != null ? cutout : raw;
        }

        private static Sprite CreateCutoutHoodSprite(Sprite raw, bool sideProfile)
        {
            if (raw == null || raw.texture == null) return raw;
            Rect fullRect = raw.rect;
            int w = Mathf.RoundToInt(fullRect.width);
            int h = Mathf.RoundToInt(fullRect.height);
            if (w < 24 || h < 24) return raw;

            Color32[] pixels = ReadSpritePixelsSafe(raw, w, h);
            if (pixels == null) return raw;

            int x0 = sideProfile ? 13 : 14;
            int x1 = sideProfile ? 27 : 23;
            int y0Top = sideProfile ? 14 : 15;
            bool changed = false;

            for (int yTop = y0Top; yTop <= 25 && yTop < h; yTop++)
            {
                int yBottom = h - 1 - yTop;
                int row = yBottom * w;
                for (int x = x0; x < x1 && x < w; x++)
                {
                    int idx = row + x;
                    Color32 c = pixels[idx];
                    if (c.a == 0) continue;

                    bool clearFace = (sideProfile && x >= 15)
                        || (c.r < 52 && c.g < 52 && c.b < 62)
                        || (sideProfile && yTop >= 24 && x >= 12);
                    if (clearFace)
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        changed = true;
                    }
                }
            }

            if (!changed) return raw;

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            Vector2 normPivot = new Vector2(raw.pivot.x / w, raw.pivot.y / h);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), normPivot, raw.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sp.name = $"{raw.name}_Cutout";
            return sp;
        }

        private static Color32[] ReadSpritePixelsSafe(Sprite sp, int fullW, int fullH)
        {
            Texture2D srcTex = sp.texture;
            Rect texRect = sp.textureRect;
            int tw = Mathf.RoundToInt(texRect.width);
            int th = Mathf.RoundToInt(texRect.height);
            int rx = Mathf.RoundToInt(texRect.x);
            int ry = Mathf.RoundToInt(texRect.y);
            if (tw <= 0 || th <= 0) return null;

            Color[] srcColors = null;
            if (srcTex.isReadable)
            {
                srcColors = srcTex.GetPixels(rx, ry, tw, th);
            }
            else
            {
                RenderTexture rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                RenderTexture prev = RenderTexture.active;
                Texture2D tmp = null;
                try
                {
                    Graphics.Blit(srcTex, rt);
                    RenderTexture.active = rt;
                    tmp = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                    // ReadPixels dùng hệ tọa độ top-down trên RenderTexture tùy backend, nhưng trên Texture2D kết quả GetPixels đồng nhất
                    tmp.ReadPixels(new Rect(rx, srcTex.height - ry - th, tw, th), 0, 0);
                    tmp.Apply(false, false);
                    srcColors = tmp.GetPixels();
                }
                catch
                {
                    return null;
                }
                finally
                {
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    if (tmp != null) UnityEngine.Object.DestroyImmediate(tmp);
                }
            }

            if (srcColors == null) return null;
            Color32[] pixels = new Color32[fullW * fullH];
            Vector2 offset = sp.textureRectOffset;
            int ox = Mathf.Clamp(Mathf.RoundToInt(offset.x), 0, Mathf.Max(0, fullW - tw));
            int oy = Mathf.Clamp(Mathf.RoundToInt(offset.y), 0, Mathf.Max(0, fullH - th));
            for (int y = 0; y < th; y++)
            {
                int dstRow = (oy + y) * fullW + ox;
                int srcRow = y * tw;
                for (int x = 0; x < tw; x++)
                {
                    pixels[dstRow + x] = srcColors[srcRow + x];
                }
            }
            return pixels;
        }

        public static ItemVisualProfile GetItemVisualProfile(CharacterClassType cls, EquipmentSlot slot, string itemId)
        {
            string id = itemId ?? "";
            switch (slot)
            {
                case EquipmentSlot.Helmet:
                    return GetHelmetProfile(cls, id);
                case EquipmentSlot.Weapon:
                    return GetWeaponProfile(cls, id);
                case EquipmentSlot.Accessory:
                    return GetAccessoryProfile(cls, id);
                default:
                    return new ItemVisualProfile(16f, 16f, 20f);
            }
        }

        private static ItemVisualProfile GetHelmetProfile(CharacterClassType cls, string id)
        {
            switch (cls)
            {
                case CharacterClassType.Knight:
                    if (id == "knight_helm_common_4") return new ItemVisualProfile(15.0f, 17.5f, 15.5f, 0f, -0.5f, HelmetVisualMode.BandOverlay);
                    if (id == "knight_helm_common_2") return new ItemVisualProfile(16.0f, 17.0f, 20.5f, 0f, 2.0f, HelmetVisualMode.FullReplace);
                    if (id == "knight_helm_epic") return new ItemVisualProfile(16.0f, 17.5f, 22.0f, 0f, 1.0f, HelmetVisualMode.FullReplace);
                    if (id == "knight_helm_rare_4" || id == "knight_helm_epic_2") return new ItemVisualProfile(16.0f, 18.0f, 22.0f, 0f, 1.0f, HelmetVisualMode.FullReplace);
                    if (id == "knight_helm_legendary") return new ItemVisualProfile(16.0f, 19.0f, 17.5f, 0f, 1.0f, HelmetVisualMode.FullReplace);
                    if (id == "knight_helm_legendary_2") return new ItemVisualProfile(16.0f, 19.5f, 19.0f, 0f, 1.0f, HelmetVisualMode.FullReplace);
                    return new ItemVisualProfile(16.0f, 17.5f, 20.0f, 0f, 1.0f, HelmetVisualMode.FullReplace);

                case CharacterClassType.Mage:
                    if (id == "mage_helm_common" || id == "mage_helm_rare" || id == "mage_helm_rare_2" || id == "mage_helm_epic")
                        return new ItemVisualProfile(16.0f, 22.0f, 31.0f, 0f, -3.5f, HelmetVisualMode.WizardHat);
                    if (id == "mage_helm_common_2" || id == "mage_helm_rare_3" || id == "mage_helm_epic_2")
                        return new ItemVisualProfile(17.0f, 16.5f, 34.0f, 0f, -1.5f, HelmetVisualMode.CutoutHood);
                    if (id == "mage_helm_common_3")
                        return new ItemVisualProfile(16.0f, 18.5f, 26.5f, 0f, -1.5f, HelmetVisualMode.OpenCap);
                    if (id == "mage_helm_common_4" || id == "mage_helm_rare_4")
                        return new ItemVisualProfile(15.0f, 17.5f, 21.0f, 0f, -2.5f, HelmetVisualMode.BandOverlay);
                    if (id == "mage_helm_epic_3" || id == "mage_helm_legendary_2")
                        return new ItemVisualProfile(16.0f, 20.0f, 21.5f, 0f, -4.5f, HelmetVisualMode.Crown);
                    if (id == "mage_helm_legendary")
                        return new ItemVisualProfile(16.0f, 20.5f, 22.5f, 0f, -4.5f, HelmetVisualMode.Crown);
                    return new ItemVisualProfile(16.0f, 18.5f, 26.0f, 0f, -2.5f, HelmetVisualMode.OpenCap);

                case CharacterClassType.Archer:
                    if (id == "archer_helm_common" || id == "archer_helm_rare_2" || id == "archer_helm_epic")
                        return new ItemVisualProfile(12.5f, 17.5f, 18.5f, 0f, -1.0f, HelmetVisualMode.CutoutHood);
                    if (id == "archer_helm_common_2" || id == "archer_helm_rare_3")
                        return new ItemVisualProfile(15.5f, 19.5f, 17.0f, 0f, -2.5f, HelmetVisualMode.OpenCap);
                    if (id == "archer_helm_common_3" || id == "archer_helm_rare_4")
                        return new ItemVisualProfile(15.0f, 17.5f, 15.0f, 0f, -3.0f, HelmetVisualMode.BandOverlay);
                    if (id == "archer_helm_common_4" || id == "archer_helm_rare" || id == "archer_helm_epic_2")
                        return new ItemVisualProfile(15.0f, 20.0f, 17.5f, 0f, -2.8f, HelmetVisualMode.FeatherCap);
                    if (id == "archer_helm_epic_3" || id == "archer_helm_legendary")
                        return new ItemVisualProfile(16.0f, 19.5f, 16.5f, 0f, -3.0f, HelmetVisualMode.OpenCap);
                    if (id == "archer_helm_legendary_2")
                        return new ItemVisualProfile(16.0f, 20.5f, 15.5f, 0f, -3.2f, HelmetVisualMode.OpenCap);
                    return new ItemVisualProfile(16.0f, 19.0f, 17.0f, 0f, -2.5f, HelmetVisualMode.OpenCap);

                default:
                    return new ItemVisualProfile(16.0f, 16.0f, 18.0f);
            }
        }

        private static ItemVisualProfile GetWeaponProfile(CharacterClassType cls, string id)
        {
            switch (cls)
            {
                case CharacterClassType.Knight:
                    if (id == "knight_weapon_common_4" || id == "knight_weapon_rare_4" || id == "knight_weapon_legendary_2")
                        return new ItemVisualProfile(10.5f, 20.5f, 27.0f, -45.0f);
                    return new ItemVisualProfile(6.5f, 24.5f, 25.0f, -45.0f);

                case CharacterClassType.Mage:
                    if (id == "mage_weapon_common_2")
                        return new ItemVisualProfile(16.0f, 21.0f, 38.0f, 0f);
                    return new ItemVisualProfile(15.5f, 20.5f, 49.0f, 0f);

                case CharacterClassType.Archer:
                    if (id == "archer_weapon_common_3" || id == "archer_weapon_rare_3" || id == "archer_weapon_epic_3")
                        return new ItemVisualProfile(13.0f, 15.5f, 17.5f, 0f);
                    if (id == "archer_weapon_common" || id == "archer_weapon_common_2")
                        return new ItemVisualProfile(17.5f, 16.0f, 31.5f, 0f);
                    if (id == "archer_weapon_epic" || id == "archer_weapon_epic_2" || id == "archer_weapon_legendary_2")
                        return new ItemVisualProfile(20.5f, 15.5f, 28.5f, 0f);
                    return new ItemVisualProfile(20.5f, 16.0f, 31.0f, 0f);

                default:
                    return new ItemVisualProfile(16.0f, 16.0f, 26.0f, 0f);
            }
        }

        private static ItemVisualProfile GetAccessoryProfile(CharacterClassType cls, string id)
        {
            switch (cls)
            {
                case CharacterClassType.Knight:
                    if (id == "knight_acc_common_3") return new ItemVisualProfile(16.0f, 16.0f, 10.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.Gauntlet);
                    if (id == "knight_acc_common_4") return new ItemVisualProfile(16.0f, 14.0f, 9.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.Collar);
                    if (id == "knight_acc_rare_4") return new ItemVisualProfile(16.0f, 16.0f, 11.0f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.Belt);
                    if (id == "knight_acc_rare_3" || id == "knight_acc_epic_3" || id == "knight_acc_legendary")
                        return new ItemVisualProfile(16.0f, 15.0f, 8.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.Badge);
                    return new ItemVisualProfile(16.0f, 16.0f, 17.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.Shield);

                case CharacterClassType.Mage:
                    bool isBook = id == "mage_acc_common_2" || id == "mage_acc_common_4" || id == "mage_acc_rare_3" || id == "mage_acc_epic_3" || id == "mage_acc_legendary_2";
                    return new ItemVisualProfile(16.0f, 16.0f, isBook ? 16.0f : 15.0f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.MageFloating);

                case CharacterClassType.Archer:
                    if (id == "archer_acc_common" || id == "archer_acc_common_4" || id == "archer_acc_rare_2" || id == "archer_acc_epic_2")
                        return new ItemVisualProfile(16.0f, 16.0f, 14.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.ArcherQuiver);
                    if (id == "archer_acc_legendary_2")
                        return new ItemVisualProfile(16.0f, 16.0f, 18.0f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.ArcherWing);
                    if (id == "archer_acc_rare_4" || id == "archer_acc_legendary")
                        return new ItemVisualProfile(16.0f, 16.0f, 11.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.ArcherFeather);
                    if (id == "archer_acc_common_2" || id == "archer_acc_rare")
                        return new ItemVisualProfile(16.0f, 16.0f, 8.0f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.ArcherGlove);
                    if (id == "archer_acc_epic_3")
                        return new ItemVisualProfile(16.0f, 16.0f, 5.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.ArcherScope);
                    return new ItemVisualProfile(16.0f, 14.0f, 7.5f, 0f, 0f, HelmetVisualMode.FullReplace, AccessoryVisualCategory.ArcherNecklace);

                default:
                    return new ItemVisualProfile(16.0f, 16.0f, 14.0f);
            }
        }

        private static string ExtractBaseFrameName(string frameName)
        {
            if (string.IsNullOrEmpty(frameName)) return "";
            int pipeIdx = frameName.IndexOf('|');
            if (pipeIdx > 0) frameName = frameName.Substring(0, pipeIdx);

            // Đảm bảo chỉ giữ phần "<Class>_<State>_<Index>" nếu còn suffix nối thêm
            int first = frameName.IndexOf('_');
            if (first > 0)
            {
                int second = frameName.IndexOf('_', first + 1);
                if (second > first)
                {
                    int third = frameName.IndexOf('_', second + 1);
                    if (third > second)
                    {
                        return frameName.Substring(0, third);
                    }
                }
            }
            return frameName;
        }

        private static void EnsureFrameAnchorsBuilt()
        {
            if (_frameAnchorLookup != null) return;
            var map = new Dictionary<string, CharacterFrameAnchor>(96, StringComparer.OrdinalIgnoreCase);

            // ==================== 1. KNIGHT (29 frames, 140x140 PPU 70) ====================
            for (int i = 0; i < 11; i++)
            {
                float dy = (i <= 1 || i >= 9) ? 0f : (i == 2 || i == 3 || i == 7 || i == 8) ? 1f : 2f;
                map[$"Knight_Idle_{i}"] = new CharacterFrameAnchor(
                    68.5f, 48.0f + dy, 0f,
                    58.0f, 65.0f + dy * 0.5f, -50f, 1, true,
                    78.5f, 63.5f + dy * 0.5f, 0f, 2,
                    70.5f, 57.5f + dy, 68.5f, 54.0f + dy, 69.0f, 65.0f + dy * 0.5f,
                    0f, 0f, 0f, 0f,
                    65.0f, 52.5f + dy, 6f);
            }

            (float hx, float hy, float wx, float wy, float sx, float sy)[] kRun =
            {
                (81.0f, 48.5f, 79.0f, 66.5f, 86.0f, 62.0f),
                (81.5f, 47.5f, 82.0f, 66.0f, 86.0f, 59.0f),
                (81.5f, 47.5f, 81.0f, 66.0f, 86.0f, 60.0f),
                (81.5f, 48.5f, 80.0f, 67.0f, 86.0f, 62.0f),
                (81.5f, 49.5f, 78.0f, 69.0f, 86.0f, 63.0f),
                (81.5f, 48.5f, 77.0f, 68.0f, 88.0f, 58.0f),
                (81.5f, 48.5f, 78.0f, 68.0f, 87.0f, 60.0f),
                (81.5f, 49.5f, 78.0f, 69.0f, 86.0f, 62.0f),
            };
            for (int i = 0; i < kRun.Length; i++)
            {
                var r = kRun[i];
                map[$"Knight_Run_{i}"] = new CharacterFrameAnchor(
                    r.hx, r.hy, -6f,
                    r.wx, r.wy, -2f, 1, true,
                    r.sx, r.sy, -5f, 2,
                    r.hx - 2f, r.hy + 9.5f, r.hx - 1f, r.hy + 6f, r.hx - 5f, r.hy + 16.5f,
                    0f, 0f, 0f, 0f,
                    r.hx - 4.5f, r.hy + 5f, 18f);
            }

            (float hx, float hy, float wx, float wy, float wrot, int wo, bool wv, float sx, float sy, float hrot)[] kAtk =
            {
                (71.0f, 40.0f, 78.0f, 39.5f, 125f, 1, true, 79.0f, 58.0f, 6f),
                (70.0f, 41.5f, 75.0f, 40.5f, 152f, 1, true, 79.0f, 59.0f, 4f),
                (67.5f, 43.0f, 71.0f, 41.0f, 166f, 1, true, 79.0f, 60.0f, 4f),
                (68.5f, 43.0f, 72.0f, 41.0f, 166f, 1, true, 79.0f, 60.0f, 4f),
                (72.5f, 55.0f, 56.0f, 63.0f, -155f, -1, false, 77.0f, 62.0f, -12f),
                (75.5f, 55.0f, 52.0f, 63.0f, -162f, 1, true, 77.0f, 62.0f, -14f),
            };
            for (int i = 0; i < kAtk.Length; i++)
            {
                var a = kAtk[i];
                map[$"Knight_Attack_{i}"] = new CharacterFrameAnchor(
                    a.hx, a.hy, a.hrot,
                    a.wx, a.wy, a.wrot, a.wo, a.wv,
                    a.sx, a.sy, 0f, 2,
                    a.hx, a.hy + 9f, a.hx, a.hy + 5.5f, a.hx - 1.5f, a.hy + 16f,
                    0f, 0f, 0f, 0f,
                    a.hx - 5f, a.hy + 5f, i < 4 ? 12f : 22f);
            }

            (float hx, float hy, float wx, float wy, float sx, float sy, float hrot)[] kHit =
            {
                (68.5f, 48.0f, 58.0f, 65.0f, 78.5f, 63.5f, 0f),
                (65.0f, 51.0f, 64.0f, 66.0f, 79.0f, 62.0f, 6f),
                (62.0f, 51.5f, 70.5f, 66.5f, 80.5f, 61.0f, 10f),
                (64.5f, 51.5f, 73.5f, 66.5f, 82.0f, 61.0f, 8f),
            };
            for (int i = 0; i < kHit.Length; i++)
            {
                var h = kHit[i];
                map[$"Knight_Hit_{i}"] = new CharacterFrameAnchor(
                    h.hx, h.hy, h.hrot,
                    h.wx, h.wy, -50f, 1, true,
                    h.sx, h.sy, 0f, 2,
                    h.hx + 2f, h.hy + 9.5f, h.hx + 1f, h.hy + 6f, h.hx + 3f, h.hy + 16.5f,
                    0f, 0f, 0f, 0f,
                    h.hx - 4.5f, h.hy + 5f, 4f);
            }

            // ==================== 2. MAGE (26 frames, 231x190 PPU 95) ====================
            float[] mageIdleDy = { 0f, 1f, 2f, 3f, 2f, 1f };
            for (int i = 0; i < mageIdleDy.Length; i++)
            {
                float dy = mageIdleDy[i];
                map[$"Mage_Idle_{i}"] = new CharacterFrameAnchor(
                    112.0f, 68.5f + dy, 0f,
                    83.5f, 75.5f + dy * 0.2f, 0f, -1, true,
                    131.5f, 74.0f - dy * 0.6f, 0f, 2,
                    0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                    101.0f, 79.0f + dy, 4f);
            }

            (float hx, float hy, float wx, float wy, float sx1, float sy1, float sx2, float sy2)[] mRun =
            {
                (121.0f, 72.5f, 132.0f, 85.0f, 140f, 70f, 99f, 139f),
                (120.0f, 74.5f, 131.0f, 85.0f, 139f, 70f, 98f, 139f),
                (119.0f, 74.5f, 128.0f, 85.0f, 136f, 70f, 95f, 139f),
                (119.0f, 71.0f, 126.0f, 81.0f, 134f, 66f, 93f, 135f),
                (118.0f, 72.0f, 123.0f, 81.0f, 131f, 66f, 90f, 135f),
                (120.0f, 75.0f, 126.0f, 83.0f, 134f, 68f, 93f, 137f),
                (120.5f, 75.0f, 127.0f, 83.0f, 135f, 68f, 94f, 137f),
                (121.0f, 71.5f, 128.0f, 79.0f, 136f, 64f, 95f, 133f),
            };
            for (int i = 0; i < mRun.Length; i++)
            {
                var r = mRun[i];
                map[$"Mage_Run_{i}"] = new CharacterFrameAnchor(
                    r.hx, r.hy, -6f,
                    r.wx, r.wy, -36f, -1, true,
                    r.hx - 17f, r.hy - 1f, 4f, -1,
                    0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                    r.hx - 10f, r.hy + 12f, 18f,
                    r.sx1, r.sy1, r.sx2, r.sy2);
            }

            (float hx, float hy, float wx, float wy, float wrot, bool wv, float ax, float ay, float sx1, float sy1, float sx2, float sy2)[] mAtk =
            {
                (98.5f, 57.0f, 86.0f, 44.0f, 48f, true, 114.0f, 56.0f, 0f, 0f, 0f, 0f),
                (92.0f, 58.0f, 76.0f, 45.0f, 48f, true, 108.0f, 56.0f, 0f, 0f, 0f, 0f),
                (98.5f, 57.0f, 86.0f, 44.0f, 48f, true, 114.0f, 56.0f, 0f, 0f, 0f, 0f),
                (137.0f, 74.5f, 165.0f, 106.0f, 0f, false, 162.0f, 72.0f, 0f, 0f, 0f, 0f),
                (137.5f, 74.5f, 165.0f, 106.0f, 0f, false, 162.0f, 72.0f, 0f, 0f, 0f, 0f),
                (136.0f, 76.0f, 165.0f, 106.0f, 0f, false, 162.0f, 72.0f, 0f, 0f, 0f, 0f),
                (125.0f, 69.5f, 138.0f, 99.0f, -40f, true, 107.0f, 64.0f, 168f, 65f, 120f, 128f),
                (122.5f, 68.5f, 136.0f, 101.0f, -40f, true, 105.0f, 63.0f, 166f, 63f, 118f, 126f),
            };
            for (int i = 0; i < mAtk.Length; i++)
            {
                var a = mAtk[i];
                map[$"Mage_Attack_{i}"] = new CharacterFrameAnchor(
                    a.hx, a.hy, 0f,
                    a.wx, a.wy, a.wrot, -1, a.wv,
                    a.ax, a.ay, 0f, 2,
                    0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                    a.hx - 10f, a.hy + 12f, 10f,
                    a.sx1, a.sy1, a.sx2, a.sy2);
            }

            (float hx, float hy, float wx, float wy, float wrot, float ax, float ay, float sx1, float sy1, float sx2, float sy2)[] mHit =
            {
                (111.0f, 68.5f, 83.5f, 75.5f, 0f, 131.5f, 73.0f, 88f, 58f, 84f, 133f),
                (107.5f, 74.0f, 124.0f, 91.0f, -59f, 128.0f, 78.0f, 145f, 83f, 78f, 123f),
                (108.5f, 70.0f, 125.0f, 92.0f, -59f, 128.0f, 74.0f, 147f, 84f, 80f, 124f),
                (110.5f, 70.0f, 118.0f, 85.0f, -59f, 129.0f, 73.0f, 135f, 70f, 68f, 110f),
            };
            for (int i = 0; i < mHit.Length; i++)
            {
                var h = mHit[i];
                map[$"Mage_Hit_{i}"] = new CharacterFrameAnchor(
                    h.hx, h.hy, (i == 1 || i == 2) ? 6f : 0f,
                    h.wx, h.wy, h.wrot, -1, true,
                    h.ax, h.ay, 0f, 2,
                    0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                    h.hx - 9f, h.hy + 11f, 4f,
                    h.sx1, h.sy1, h.sx2, h.sy2);
            }

            // ==================== 3. ARCHER (27 frames, 100x100 PPU 55) ====================
            float[] archIdleDy = { 0f, 2f, 3f, 4f, 5f, 5f, 4f, 2f, 0f, 0f };
            float[] archBowDy = { 0f, 0f, 1f, 2f, 2f, 2f, 2f, 1f, 0f, 0f };
            for (int i = 0; i < archIdleDy.Length; i++)
            {
                float dy = archIdleDy[i];
                float bdy = archBowDy[i];
                map[$"Archer_Idle_{i}"] = new CharacterFrameAnchor(
                    54.0f, 38.0f + dy, 0f,
                    65.0f, 47.5f + bdy, 0f, 1, true,
                    48.0f, 38.5f + dy, -22f, -1,
                    0f, 0f, 54.0f, 43.5f + dy, 0f, 0f,
                    57.0f, 36.0f + dy, 59.5f, 46.5f + bdy,
                    49.0f, 42.5f + dy, 10f);
            }

            (float hx, float hy, float wx, float wy, float ex, float ey)[] aRun =
            {
                (55.0f, 39.5f, 61.0f, 47.5f, 58.0f, 38.0f),
                (55.5f, 40.5f, 62.0f, 48.5f, 58.0f, 39.0f),
                (55.5f, 41.0f, 62.5f, 47.0f, 58.0f, 40.0f),
                (55.0f, 40.5f, 64.5f, 46.5f, 58.0f, 39.0f),
                (54.5f, 39.5f, 65.5f, 44.5f, 58.0f, 38.0f),
                (55.0f, 40.5f, 64.5f, 46.5f, 58.0f, 39.0f),
                (55.5f, 41.0f, 63.5f, 47.5f, 58.0f, 40.0f),
                (55.0f, 40.5f, 62.5f, 47.5f, 58.0f, 39.0f),
            };
            for (int i = 0; i < aRun.Length; i++)
            {
                var r = aRun[i];
                map[$"Archer_Run_{i}"] = new CharacterFrameAnchor(
                    r.hx, r.hy, -5f,
                    r.wx, r.wy, -4f, 1, true,
                    r.hx - 6.5f, r.hy + 1.5f, -26f, -1,
                    0f, 0f, r.hx - 0.5f, r.hy + 6f, 0f, 0f,
                    r.ex, r.ey, r.wx - 4f, r.wy - 1f,
                    r.hx - 5.5f, r.hy + 5f, 20f);
            }

            (float hx, float hy, float wx, float wy, float ex, float ey)[] aAtk =
            {
                (49.0f, 33.5f, 63.0f, 42.5f, 50.0f, 32.0f),
                (49.0f, 33.5f, 64.0f, 41.5f, 50.0f, 32.0f),
                (49.0f, 33.5f, 65.5f, 38.5f, 51.0f, 32.0f),
                (49.0f, 33.5f, 65.5f, 38.5f, 51.0f, 32.0f),
                (49.0f, 33.5f, 66.5f, 38.5f, 51.0f, 32.0f),
                (49.0f, 33.5f, 65.5f, 38.5f, 51.0f, 32.0f),
            };
            for (int i = 0; i < aAtk.Length; i++)
            {
                var a = aAtk[i];
                map[$"Archer_Attack_{i}"] = new CharacterFrameAnchor(
                    a.hx, a.hy, 0f,
                    a.wx, a.wy, 0f, 1, true,
                    a.hx - 6f, a.hy + 1.5f, -20f, -1,
                    0f, 0f, a.hx + 0.5f, a.hy + 5.5f, 0f, 0f,
                    a.ex, a.ey, a.wx - 4f, a.wy,
                    a.hx - 5.5f, a.hy + 5f, 12f);
            }

            (float hx, float hy, float wx, float wy, float ex, float ey)[] aHit =
            {
                (45.5f, 38.5f, 57.0f, 43.5f, 47.0f, 37.0f),
                (47.0f, 38.5f, 60.0f, 47.5f, 48.0f, 37.0f),
                (49.0f, 36.5f, 64.0f, 48.5f, 51.0f, 35.0f),
            };
            for (int i = 0; i < aHit.Length; i++)
            {
                var h = aHit[i];
                map[$"Archer_Hit_{i}"] = new CharacterFrameAnchor(
                    h.hx, h.hy, 6f,
                    h.wx, h.wy, 0f, 1, true,
                    h.hx - 6f, h.hy + 1.5f, -15f, -1,
                    0f, 0f, h.hx + 0.5f, h.hy + 6f, 0f, 0f,
                    h.ex, h.ey, h.wx - 4f, h.wy,
                    h.hx - 5f, h.hy + 5f, 6f);
            }

            _frameAnchorLookup = map;
        }

        public static bool TryGetFrameAnchor(
            CharacterClassType cls,
            string frameName,
            VisualAnimState state,
            int frameIndex,
            out CharacterFrameAnchor anchor)
        {
            EnsureFrameAnchorsBuilt();
            string cleanName = ExtractBaseFrameName(frameName);
            if (!string.IsNullOrEmpty(cleanName) && _frameAnchorLookup.TryGetValue(cleanName, out anchor))
            {
                return true;
            }

            string prefix = cls switch
            {
                CharacterClassType.Knight => "Knight",
                CharacterClassType.Mage => "Mage",
                CharacterClassType.Archer => "Archer",
                _ => "Knight"
            };
            string stateToken = state switch
            {
                VisualAnimState.Run => "Run",
                VisualAnimState.Attack => "Attack",
                VisualAnimState.Hit => "Hit",
                _ => "Idle"
            };
            int clampedIdx = Mathf.Max(0, frameIndex);
            if (_frameAnchorLookup.TryGetValue($"{prefix}_{stateToken}_{clampedIdx}", out anchor))
            {
                return true;
            }
            if (_frameAnchorLookup.TryGetValue($"{prefix}_{stateToken}_0", out anchor))
            {
                return true;
            }
            return _frameAnchorLookup.TryGetValue($"{prefix}_Idle_0", out anchor);
        }

        /// <summary>
        /// Kiểm tra nhanh chuỗi tên frame thuộc trạng thái nào (Idle/Hit/Run/Attack/Death).
        /// </summary>
        public static bool FrameNameContains(string frameName, string token)
        {
            return !string.IsNullOrEmpty(frameName)
                && frameName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool EqRgb(Color32 c, byte r, byte g, byte b, int tol = 2)
        {
            return Mathf.Abs(c.r - r) <= tol && Mathf.Abs(c.g - g) <= tol && Mathf.Abs(c.b - b) <= tol;
        }

        private static bool IsKnightHelmetMetalOrVisor(Color32 c, int tol = 2)
        {
            return EqRgb(c, 0xC3, 0xCB, 0xDB, tol)
                || EqRgb(c, 0xA0, 0x96, 0xD1, tol)
                || EqRgb(c, 0x81, 0x70, 0x9A, tol)
                || EqRgb(c, 0x62, 0x50, 0x7E, tol)
                || EqRgb(c, 0x44, 0x2B, 0x61, tol)
                || EqRgb(c, 0x25, 0x2A, 0x32, tol)
                || EqRgb(c, 0x32, 0x22, 0x2E, tol);
        }

        private static bool IsMageOrbColor(Color32 c, int tol = 2)
        {
            return EqRgb(c, 0x33, 0xDA, 0xFF, tol)
                || EqRgb(c, 0xC1, 0xFF, 0xFA, tol)
                || EqRgb(c, 0x43, 0xC2, 0xDF, tol)
                || EqRgb(c, 0x7C, 0xE6, 0xFD, tol)
                || EqRgb(c, 0xCB, 0xF4, 0xFD, tol);
        }

        private static bool IsMageWoodColor(Color32 c, int tol = 2)
        {
            return EqRgb(c, 0x3D, 0x23, 0x10, tol)
                || EqRgb(c, 0x1E, 0x10, 0x06, tol)
                || EqRgb(c, 0x36, 0x1E, 0x0D, tol)
                || EqRgb(c, 0x48, 0x28, 0x11, tol)
                || EqRgb(c, 0x1F, 0x11, 0x07, tol)
                || EqRgb(c, 0x3E, 0x22, 0x11, tol)
                || EqRgb(c, 0x20, 0x12, 0x07, tol);
        }

        private static bool IsMageHoodOrTrimColor(Color32 c, int tol = 2)
        {
            return EqRgb(c, 0x25, 0x1E, 0x53, tol)
                || EqRgb(c, 0x3F, 0x36, 0x7A, tol)
                || EqRgb(c, 0x3B, 0x33, 0x77, tol)
                || EqRgb(c, 0x47, 0x28, 0x99, tol)
                || EqRgb(c, 0x6E, 0x49, 0xCD, tol)
                || EqRgb(c, 0x97, 0x6E, 0xFF, tol)
                || EqRgb(c, 0x6F, 0x1F, 0x73, tol)
                || EqRgb(c, 0x99, 0x36, 0x9E, tol)
                || EqRgb(c, 0xE8, 0xBC, 0x61, tol)
                || EqRgb(c, 0xFF, 0xCC, 0x62, tol)
                || EqRgb(c, 0x71, 0x23, 0x34, tol)
                || EqRgb(c, 0x61, 0x27, 0x34, tol);
        }

        private static float DistanceToSegment(float px, float py, float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float lenSq = dx * dx + dy * dy;
            if (lenSq <= 0.0001f) return Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            float t = Mathf.Clamp01(((px - x1) * dx + (py - y1) * dy) / lenSq);
            float cx = x1 + t * dx;
            float cy = y1 + t * dy;
            return Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        }

        private static bool IsArcherGreenTunic(Color32 c)
        {
            return c.g > c.r && c.g > c.b;
        }

        private static VisualAnimState InferAnimStateFromName(string frameName)
        {
            if (FrameNameContains(frameName, "Run")) return VisualAnimState.Run;
            if (FrameNameContains(frameName, "Attack")) return VisualAnimState.Attack;
            if (FrameNameContains(frameName, "Hit")) return VisualAnimState.Hit;
            return VisualAnimState.Idle;
        }

        private static int InferFrameIndexFromName(string frameName)
        {
            if (string.IsNullOrEmpty(frameName)) return 0;
            int lastUnderscore = frameName.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore + 1 < frameName.Length)
            {
                if (int.TryParse(frameName.Substring(lastUnderscore + 1), out int idx))
                    return idx;
            }
            return 0;
        }

        /// <summary>
        /// Tách sạch đồ mặc định theo từng frame (mũ sắt/mũ trùm, kiếm/gậy/cung cũ, sách bay Mage, ống tên Archer)
        /// đồng thời giữ lại tóc bạc/khuôn mặt/giáp chân và đổi bảng màu giáp trên mảng pixel bottom-to-top.
        /// Trả về true nếu có ít nhất 1 pixel được chỉnh sửa.
        /// </summary>
        public static bool RemapAndCleanPixels(
            Color32[] pixels,
            int texWidth,
            int texHeight,
            CharacterClassType cls,
            string frameName,
            string armorItemId,
            string weaponItemId,
            string helmetItemId,
            string accessoryItemId,
            IReadOnlyList<ColorReplacement> prebuiltRules = null,
            int tolerance = 2)
        {
            if (pixels == null || pixels.Length == 0 || texWidth <= 0 || texHeight <= 0) return false;
            if (pixels.Length < texWidth * texHeight) return false;

            IReadOnlyList<ColorReplacement> rules = prebuiltRules ?? BuildColorReplacements(cls, armorItemId, weaponItemId);
            bool hasWeapon = !string.IsNullOrEmpty(weaponItemId);
            bool hasHelmet = !string.IsNullOrEmpty(helmetItemId);
            bool hasAcc = !string.IsNullOrEmpty(accessoryItemId);

            string cleanFrameName = ExtractBaseFrameName(frameName);
            VisualAnimState animState = InferAnimStateFromName(cleanFrameName);
            int frameIdx = InferFrameIndexFromName(cleanFrameName);
            TryGetFrameAnchor(cls, cleanFrameName, animState, frameIdx, out CharacterFrameAnchor anchor);

            bool isIdle = animState == VisualAnimState.Idle;
            bool isHit = animState == VisualAnimState.Hit;
            bool isRun = animState == VisualAnimState.Run;
            bool isAttack = animState == VisualAnimState.Attack;

            ItemVisualProfile helmProfile = hasHelmet ? GetItemVisualProfile(cls, EquipmentSlot.Helmet, helmetItemId) : default;
            ItemVisualProfile accProfile = hasAcc ? GetItemVisualProfile(cls, EquipmentSlot.Accessory, accessoryItemId) : default;

            bool anyChanged = false;
            Color32 clear = Hex(0, 0, 0, 0);

            for (int yBottom = 0; yBottom < texHeight; yBottom++)
            {
                int yTop = texHeight - 1 - yBottom;
                int rowOffset = yBottom * texWidth;

                for (int x = 0; x < texWidth; x++)
                {
                    int idx = rowOffset + x;
                    Color32 c = pixels[idx];
                    if (c.a == 0) continue;

                    bool erase = false;
                    Color32? replaceOverride = null;

                    switch (cls)
                    {
                        case CharacterClassType.Knight:
                            if (string.Equals(cleanFrameName, "Knight_Hit_1", StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }
                            if (hasHelmet && helmProfile.helmMode != HelmetVisualMode.BandOverlay)
                            {
                                bool isHelmPixel = IsKnightHelmetMetalOrVisor(c, tolerance)
                                    || (yTop <= anchor.headY + 1.0f && EqRgb(c, 0xFC, 0xFC, 0xFC, tolerance));
                                if (isHelmPixel
                                    && Mathf.Abs(x - anchor.headX) <= 7.5f
                                    && yTop >= anchor.headY - 7.5f && yTop <= anchor.headY + 6.0f
                                    && !(yTop >= anchor.headY + 3.5f && Mathf.Abs(x - anchor.headX) >= 5.0f))
                                {
                                    erase = true;
                                }
                            }
                            if (!erase && hasWeapon)
                            {
                                bool isSlashFrame = isAttack && frameIdx == 4;
                                if (!isSlashFrame && EqRgb(c, 0xFC, 0xFC, 0xFC, tolerance))
                                {
                                    erase = true;
                                }
                                else if (!isSlashFrame && (EqRgb(c, 0xC3, 0xCB, 0xDB, tolerance) || EqRgb(c, 0xA0, 0x96, 0xD1, tolerance)))
                                {
                                    if ((isIdle || (isHit && frameIdx == 0)) && x >= 63 && x <= 74 && yTop >= 66) erase = true;
                                    else if (isHit && frameIdx > 0 && x >= 73 && x <= 89 && yTop >= 66) erase = true;
                                    else if (isRun && x >= 83 && yTop >= 66 && yTop <= 73) erase = true;
                                    else if (isAttack && (yTop <= 42 || (frameIdx == 5 && x <= 55))) erase = true;
                                }
                                else if (EqRgb(c, 0x14, 0x16, 0x1F, tolerance) || EqRgb(c, 0x25, 0x2A, 0x32, tolerance) || EqRgb(c, 0x32, 0x22, 0x2E, tolerance))
                                {
                                    if (Mathf.Abs(x - anchor.wpnX) <= 5.5f && Mathf.Abs(yTop - anchor.wpnY) <= 4.5f)
                                    {
                                        erase = true;
                                    }
                                }
                            }
                            break;

                        case CharacterClassType.Mage:
                            if (hasWeapon)
                            {
                                if (IsMageOrbColor(c, tolerance))
                                {
                                    erase = true;
                                }
                                else if (IsMageWoodColor(c, tolerance))
                                {
                                    bool isShaftMid = EqRgb(c, 0x3D, 0x23, 0x10, tolerance);
                                    if (isIdle || (isHit && frameIdx == 0))
                                    {
                                        if (x >= 80 && x <= 93 && yTop >= 54 && yTop <= 141) erase = true;
                                    }
                                    else if (isRun)
                                    {
                                        bool inWaistBook = yTop >= 92 && yTop <= 107 && x >= 98 && x <= 114;
                                        if (!inWaistBook && DistanceToSegment(x, yTop, anchor.staffSegX1, anchor.staffSegY1, anchor.staffSegX2, anchor.staffSegY2) <= 2.8f)
                                        {
                                            if (isShaftMid || yTop <= 91 || (x > 113 && yTop <= 114) || (x >= 94 && x <= 117 && yTop >= 112 && yTop <= 139))
                                            {
                                                erase = true;
                                            }
                                        }
                                    }
                                    else if (isAttack && frameIdx <= 2)
                                    {
                                        if (yTop <= 63 && x <= 125 && !(yTop >= 46 && x >= 92)) erase = true;
                                    }
                                    else if (isAttack && frameIdx <= 5)
                                    {
                                        if (x >= 162 && yTop >= 70) erase = true;
                                    }
                                    else if (isAttack)
                                    {
                                        bool inWaistBook = yTop >= 94 && yTop <= 107 && x >= 115 && x <= 132;
                                        if (!inWaistBook && yTop <= 130 && DistanceToSegment(x, yTop, anchor.staffSegX1, anchor.staffSegY1, anchor.staffSegX2, anchor.staffSegY2) <= 2.8f)
                                        {
                                            if (isShaftMid || yTop <= 92 || x >= 140 || yTop >= 115) erase = true;
                                        }
                                    }
                                    else if (isHit)
                                    {
                                        bool inWaistBook = yTop >= 92 && yTop <= 107 && x >= 112 && x <= 128;
                                        if (!inWaistBook && yTop >= 82 && yTop <= 126 && DistanceToSegment(x, yTop, anchor.staffSegX1, anchor.staffSegY1, anchor.staffSegX2, anchor.staffSegY2) <= 2.8f)
                                        {
                                            if (isShaftMid || x >= 126 || (x <= 118 && yTop <= 125)) erase = true;
                                        }
                                    }
                                }
                            }

                            if (!erase && hasAcc && isIdle && x >= 120 && yTop <= 85)
                            {
                                erase = true;
                            }

                            if (!erase && hasHelmet && IsMageHoodOrTrimColor(c, tolerance))
                            {
                                float dx = x - anchor.headX;
                                float dy = yTop - anchor.headY;
                                if (Mathf.Abs(dx) <= 22f && yTop <= anchor.headY + 12f)
                                {
                                    if (helmProfile.helmMode == HelmetVisualMode.CutoutHood)
                                    {
                                        if (dy <= -5f || dx <= -8f) erase = true;
                                        else replaceOverride = new Color32(54, 44, 72, c.a);
                                    }
                                    else
                                    {
                                        if (dy <= -5f || (dx <= -10f && dy <= 4f) || (dy <= -2f && dx >= 4f))
                                        {
                                            erase = true;
                                        }
                                        else
                                        {
                                            byte luma = (c.r + c.g + c.b) > 220 ? (byte)180 : (byte)142;
                                            byte gC = (c.r + c.g + c.b) > 220 ? (byte)174 : (byte)138;
                                            byte bC = (c.r + c.g + c.b) > 220 ? (byte)172 : (byte)138;
                                            replaceOverride = new Color32(luma, gC, bC, c.a);
                                        }
                                    }
                                }
                            }
                            break;

                        case CharacterClassType.Archer:
                            if (hasAcc
                                && (accProfile.accCategory == AccessoryVisualCategory.ArcherQuiver || accProfile.accCategory == AccessoryVisualCategory.ArcherWing)
                                && !IsArcherGreenTunic(c)
                                && Mathf.Abs(x - anchor.accX) <= 5.5f
                                && yTop >= anchor.accY - 8.5f && yTop <= anchor.accY + 2.0f)
                            {
                                bool isSkinOrEye = EqRgb(c, 0xF3, 0xD0, 0xB3, tolerance)
                                    || EqRgb(c, 0xDF, 0xA8, 0x7B, tolerance)
                                    || EqRgb(c, 0xC7, 0xA1, 0x82, tolerance)
                                    || EqRgb(c, 0x00, 0x00, 0x00, 1);
                                if (!isSkinOrEye) erase = true;
                            }
                            break;
                    }

                    if (erase)
                    {
                        pixels[idx] = clear;
                        anyChanged = true;
                        continue;
                    }

                    if (replaceOverride.HasValue)
                    {
                        pixels[idx] = replaceOverride.Value;
                        anyChanged = true;
                        continue;
                    }

                    if (rules != null)
                    {
                        for (int r = 0; r < rules.Count; r++)
                        {
                            Color32 src = rules[r].source;
                            if (Mathf.Abs(c.r - src.r) <= tolerance
                                && Mathf.Abs(c.g - src.g) <= tolerance
                                && Mathf.Abs(c.b - src.b) <= tolerance)
                            {
                                Color32 dst = rules[r].target;
                                pixels[idx] = dst.a == 0 ? dst : new Color32(dst.r, dst.g, dst.b, c.a);
                                anyChanged = true;
                                break;
                            }
                        }
                    }
                }
            }

            return anyChanged;
        }

        public static bool RemapPixels(Color32[] pixels, IReadOnlyList<ColorReplacement> rules, int tolerance = 2)
        {
            if (pixels == null || pixels.Length == 0 || rules == null || rules.Count == 0) return false;

            bool anyChanged = false;
            for (int p = 0; p < pixels.Length; p++)
            {
                Color32 c = pixels[p];
                if (c.a == 0) continue;

                for (int r = 0; r < rules.Count; r++)
                {
                    Color32 src = rules[r].source;
                    if (Mathf.Abs(c.r - src.r) <= tolerance
                        && Mathf.Abs(c.g - src.g) <= tolerance
                        && Mathf.Abs(c.b - src.b) <= tolerance)
                    {
                        Color32 dst = rules[r].target;
                        pixels[p] = dst.a == 0 ? dst : new Color32(dst.r, dst.g, dst.b, c.a);
                        anyChanged = true;
                        break;
                    }
                }
            }
            return anyChanged;
        }

        public static bool TryGetCloakColors(CharacterClassType cls, string armorItemId, out Color primary, out Color trim)
        {
            primary = Color.clear;
            trim = Color.clear;
            if (string.IsNullOrEmpty(armorItemId)) return false;

            if (armorItemId.Contains("_common") && cls != CharacterClassType.Mage)
            {
                return false;
            }

            if (ArmorPalettes.TryGetValue(armorItemId, out Color32[] pal) && pal.Length >= 6)
            {
                if (cls == CharacterClassType.Knight)
                {
                    Color32 cPri = pal[4];
                    Color32 cTrim = pal[3];
                    primary = new Color(cPri.r / 255f, cPri.g / 255f, cPri.b / 255f, 1f);
                    trim = new Color(cTrim.r / 255f, cTrim.g / 255f, cTrim.b / 255f, 1f);
                }
                else
                {
                    Color32 cPri = pal[2];
                    Color32 cTrim = pal[5];
                    primary = new Color(cPri.r / 255f, cPri.g / 255f, cPri.b / 255f, 1f);
                    trim = new Color(cTrim.r / 255f, cTrim.g / 255f, cTrim.b / 255f, 1f);
                }
                return true;
            }
            return false;
        }

        public static Color GetGlowTintForItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return Color.clear;
            if (itemId.Contains("_legendary_2"))
            {
                if (itemId.StartsWith("knight", StringComparison.Ordinal)) return new Color(0.45f, 0.85f, 1f, 0.80f);
                if (itemId.StartsWith("mage", StringComparison.Ordinal))   return new Color(1f, 0.72f, 0.22f, 0.82f);
                return new Color(0.45f, 1f, 0.80f, 0.80f);
            }
            if (itemId.Contains("_legendary"))
            {
                if (itemId.StartsWith("knight", StringComparison.Ordinal)) return new Color(1f, 0.48f, 0.16f, 0.78f);
                if (itemId.StartsWith("mage", StringComparison.Ordinal))   return new Color(0.85f, 0.34f, 1f, 0.78f);
                return new Color(1f, 0.78f, 0.22f, 0.78f);
            }
            if (itemId.Contains("_epic_2"))
            {
                return itemId.StartsWith("knight", StringComparison.Ordinal)
                    ? new Color(1f, 0.36f, 0.38f, 0.65f)
                    : new Color(0.76f, 0.50f, 1f, 0.64f);
            }
            if (itemId.Contains("_epic_3"))
            {
                return new Color(0.55f, 0.88f, 1f, 0.64f);
            }
            if (itemId.Contains("_epic"))
            {
                return itemId.StartsWith("archer", StringComparison.Ordinal)
                    ? new Color(0.38f, 0.86f, 1f, 0.62f)
                    : new Color(0.96f, 0.78f, 0.30f, 0.62f);
            }
            if (itemId.Contains("_rare"))
            {
                return new Color(0.34f, 0.70f, 1f, 0.38f);
            }
            return Color.clear;
        }
    }
}
