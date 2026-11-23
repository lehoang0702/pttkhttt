using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Visuals;

namespace MedievalRPG.EditorTools
{
    /// <summary>
    /// Chuẩn hóa dữ liệu ScriptableObject cho hệ thống RPG (Status Effects cho Knight/Mage,
    /// bộ kỹ năng đầy đủ cho Goblin/Skeleton/DarkKnightBoss và Encounter 3 Wave mẫu).
    /// </summary>
    public static class MedievalRPGAssetSetup
    {
        private const string SkillsRoot = "Assets/MedievalRPG/ScriptableObjects/Skills";
        private const string ClassesRoot = "Assets/MedievalRPG/ScriptableObjects/Classes";
        private const string EncountersRoot = "Assets/MedievalRPG/ScriptableObjects/Encounters";
        private const string EquipmentRoot = "Assets/MedievalRPG/ScriptableObjects/Equipment";
        private const string ResourcesRoot = "Assets/MedievalRPG/Resources";
        private const string EquipmentCatalogPath = ResourcesRoot + "/MedievalEquipmentCatalog.asset";

        // Chuẩn hóa tỉ lệ hiển thị để 3 anh hùng (Knight / Mage / Archer) có chiều cao cơ thể tương đương (~0.80 world units)
        public const float KnightVisualScale = 1.45f;
        public const float MageVisualScale = 0.96f;
        public const float ArcherVisualScale = 1.32f;
        public const float DarkKnightBossVisualScale = 1.75f;

        [MenuItem("MedievalRPG/Setup RPG Skills & Waves")]
        public static void EnsureCombatDataSetup()
        {
            if (!AssetDatabase.IsValidFolder(EncountersRoot))
            {
                AssetDatabase.CreateFolder("Assets/MedievalRPG/ScriptableObjects", "Encounters");
            }

            // 1. Nâng cấp kỹ năng anh hùng sẵn có để kích hoạt hiệu ứng trạng thái
            SkillData shieldSlam = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Knight_ShieldSlam.asset");
            if (shieldSlam != null)
            {
                shieldSlam.statusEffect = StatusEffectType.Stun;
                shieldSlam.statusDurationTurns = 1;
                shieldSlam.applyStatusToSelf = false;
                EditorUtility.SetDirty(shieldSlam);
            }

            SkillData ironBulwark = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Knight_IronBulwark.asset");
            if (ironBulwark != null)
            {
                ironBulwark.statusEffect = StatusEffectType.Taunt;
                ironBulwark.statusDurationTurns = 2;
                ironBulwark.applyStatusToSelf = true;
                ironBulwark.description = "Dựng khiên thép (+15 Giáp) và Khiêu Khích thu hút đòn đánh kẻ địch trong 2 lượt.";
                EditorUtility.SetDirty(ironBulwark);
            }

            SkillData fireball = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Mage_Fireball.asset");
            if (fireball != null)
            {
                fireball.statusEffect = StatusEffectType.Burn;
                fireball.statusDurationTurns = 2;
                fireball.statusPotency = 8;
                fireball.applyStatusToSelf = false;
                EditorUtility.SetDirty(fireball);
            }

            // 2. Tạo kỹ năng cho quái vật & Boss
            SkillData goblinStab = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Goblin_Stab.asset");
            SkillData goblinPoison = CreateOrUpdateSkill(
                $"{SkillsRoot}/Goblin_PoisonDagger.asset",
                "Poison Dagger",
                "Phóng dao tẩm độc gây sát thương và thiêu đốt mục tiêu trong 2 lượt.",
                CharacterClassType.Knight,
                manaCost: 6, cooldown: 2, SkillTargetType.SingleEnemy, DamageType.Physical,
                powerMult: 0.95f, flatBonus: 2, heal: 0, defBonus: 0, bonusCrit: 0.05f, effTurns: 0,
                StatusEffectType.Burn, statusTurns: 2, statusPotency: 5, toSelf: false);

            SkillData skelCleave = CreateOrUpdateSkill(
                $"{SkillsRoot}/Skeleton_BoneCleave.asset",
                "Bone Cleave",
                "Vung rìu xương chém mạnh vào một mục tiêu.",
                CharacterClassType.Knight,
                manaCost: 0, cooldown: 0, SkillTargetType.SingleEnemy, DamageType.Physical,
                powerMult: 1.0f, flatBonus: 2, heal: 0, defBonus: 0, bonusCrit: 0.05f, effTurns: 0,
                StatusEffectType.None, statusTurns: 0, statusPotency: 0, toSelf: false);

            SkillData skelShield = CreateOrUpdateSkill(
                $"{SkillsRoot}/Skeleton_BoneShield.asset",
                "Bone Shield",
                "Gia cố khung xương tăng +12 Giáp trong 2 lượt.",
                CharacterClassType.Knight,
                manaCost: 8, cooldown: 2, SkillTargetType.Self, DamageType.Physical,
                powerMult: 0f, flatBonus: 0, heal: 0, defBonus: 12, bonusCrit: 0f, effTurns: 2,
                StatusEffectType.None, statusTurns: 0, statusPotency: 0, toSelf: true);

            SkillData bossSlash = CreateOrUpdateSkill(
                $"{SkillsRoot}/DarkKnight_ShadowSlash.asset",
                "Shadow Slash",
                "Đòn chém bóng tối nặng nề xuyên giáp.",
                CharacterClassType.Knight,
                manaCost: 0, cooldown: 0, SkillTargetType.SingleEnemy, DamageType.Physical,
                powerMult: 1.05f, flatBonus: 4, heal: 0, defBonus: 0, bonusCrit: 0.10f, effTurns: 0,
                StatusEffectType.None, statusTurns: 0, statusPotency: 0, toSelf: false);

            SkillData bossWave = CreateOrUpdateSkill(
                $"{SkillsRoot}/DarkKnight_AbyssalWave.asset",
                "Abyssal Wave",
                "Giải phóng sóng hắc ám quét qua toàn bộ đội hình anh hùng.",
                CharacterClassType.Knight,
                manaCost: 16, cooldown: 2, SkillTargetType.AllEnemies, DamageType.Magical,
                powerMult: 1.15f, flatBonus: 4, heal: 0, defBonus: 0, bonusCrit: 0f, effTurns: 0,
                StatusEffectType.Burn, statusTurns: 2, statusPotency: 6, toSelf: false);

            SkillData bossBulwark = CreateOrUpdateSkill(
                $"{SkillsRoot}/DarkKnight_DarkBulwark.asset",
                "Dark Bulwark",
                "Khoác giáp bóng đêm tăng +14 Giáp trong 2 lượt.",
                CharacterClassType.Knight,
                manaCost: 10, cooldown: 3, SkillTargetType.Self, DamageType.Physical,
                powerMult: 0f, flatBonus: 0, heal: 0, defBonus: 14, bonusCrit: 0f, effTurns: 2,
                StatusEffectType.None, statusTurns: 0, statusPotency: 0, toSelf: true);

            SkillData skelArrow = CreateOrUpdateSkill(
                $"{SkillsRoot}/Skeleton_BoneArrow.asset",
                "Bone Arrow",
                "Bắn mũi tên xương sắc bén với tỉ lệ chí mạng cao.",
                CharacterClassType.Archer,
                manaCost: 0, cooldown: 0, SkillTargetType.SingleEnemy, DamageType.Physical,
                powerMult: 1.0f, flatBonus: 2, heal: 0, defBonus: 0, bonusCrit: 0.12f, effTurns: 0,
                StatusEffectType.None, statusTurns: 0, statusPotency: 0, toSelf: false);

            SkillData skelVolley = CreateOrUpdateSkill(
                $"{SkillsRoot}/Skeleton_PoisonVolley.asset",
                "Poison Volley",
                "Bắn loạt tên độc gây sát thương diện rộng & thiêu đốt.",
                CharacterClassType.Archer,
                manaCost: 12, cooldown: 2, SkillTargetType.AllEnemies, DamageType.Physical,
                powerMult: 0.78f, flatBonus: 2, heal: 0, defBonus: 0, bonusCrit: 0.05f, effTurns: 0,
                StatusEffectType.Burn, statusTurns: 2, statusPotency: 5, toSelf: false);

            SkillData shadowOrb = CreateOrUpdateSkill(
                $"{SkillsRoot}/ShadowMage_DarkOrb.asset",
                "Dark Orb",
                "Phóng cầu hắc ám gây sát thương phép.",
                CharacterClassType.Mage,
                manaCost: 0, cooldown: 0, SkillTargetType.SingleEnemy, DamageType.Magical,
                powerMult: 1.0f, flatBonus: 3, heal: 0, defBonus: 0, bonusCrit: 0.06f, effTurns: 0,
                StatusEffectType.None, statusTurns: 0, statusPotency: 0, toSelf: false);

            SkillData shadowBlast = CreateOrUpdateSkill(
                $"{SkillsRoot}/ShadowMage_CursedBlast.asset",
                "Cursed Blast",
                "Vụ nổ lời nguyền thiêu đốt toàn bộ anh hùng.",
                CharacterClassType.Mage,
                manaCost: 14, cooldown: 2, SkillTargetType.AllEnemies, DamageType.Magical,
                powerMult: 0.85f, flatBonus: 3, heal: 0, defBonus: 0, bonusCrit: 0f, effTurns: 0,
                StatusEffectType.Burn, statusTurns: 2, statusPotency: 6, toSelf: false);

            SkillData shadowAegis = CreateOrUpdateSkill(
                $"{SkillsRoot}/ShadowMage_DarkAegis.asset",
                "Dark Aegis",
                "Hồi máu và tạo khiên hắc ám tăng +10 Giáp cho đồng minh quái thấp máu nhất.",
                CharacterClassType.Mage,
                manaCost: 14, cooldown: 3, SkillTargetType.SingleAlly, DamageType.Magical,
                powerMult: 0f, flatBonus: 0, heal: 24, defBonus: 10, bonusCrit: 0f, effTurns: 2,
                StatusEffectType.None, statusTurns: 0, statusPotency: 0, toSelf: false);

            // 3. Gán kỹ năng cho cả 3 Anh hùng & 3 Quái vật/Boss
            SkillData knightSlash = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Knight_Slash.asset");
            AssignSkillsListOnly($"{ClassesRoot}/KnightClass.asset", knightSlash, shieldSlam, ironBulwark);

            SkillData mageBolt = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Mage_ArcaneBolt.asset");
            if (mageBolt != null && mageBolt.manaCost != 0)
            {
                mageBolt.manaCost = 0;
                EditorUtility.SetDirty(mageBolt);
            }
            SkillData mageHeal = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Mage_HealingLight.asset");
            AssignSkillsListOnly($"{ClassesRoot}/MageClass.asset", mageBolt, fireball, mageHeal);

            SkillData archerQuick = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Archer_QuickShot.asset");
            SkillData archerPrecision = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Archer_PrecisionShot.asset");
            SkillData archerRain = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Archer_RainOfArrows.asset");
            AssignSkillsListOnly($"{ClassesRoot}/ArcherClass.asset", archerQuick, archerPrecision, archerRain);

            AssignSkillsToClass($"{ClassesRoot}/GoblinClass.asset", 1, false, goblinStab, goblinPoison);
            AssignSkillsToClass($"{ClassesRoot}/SkeletonClass.asset", 2, false, skelCleave, skelShield);
            AssignSkillsToClass($"{ClassesRoot}/DarkKnightBossClass.asset", 5, true, bossSlash, bossWave, bossBulwark);

            EnsureEnemyClassAsset(
                $"{ClassesRoot}/SkeletonArcherClass.asset",
                "Xương Cung Thủ",
                CharacterClassType.Archer,
                baseLevel: 2, hp: 125, mp: 45, atk: 24, mag: 9, def: 9, mdef: 8, spd: 16, crit: 0.14f,
                skelArrow, skelVolley);
            EnsureEnemyClassAsset(
                $"{ClassesRoot}/ShadowMageClass.asset",
                "Pháp Sư Hắc Ám",
                CharacterClassType.Mage,
                baseLevel: 3, hp: 130, mp: 75, atk: 9, mag: 28, def: 7, mdef: 22, spd: 14, crit: 0.08f,
                shadowOrb, shadowBlast, shadowAegis);

            EnsureVariantEnemyPrefab(
                "Assets/MedievalRPG/Prefabs/Archer.prefab",
                "Assets/MedievalRPG/Prefabs/SkeletonArcher.prefab",
                "Xương Cung Thủ",
                new Color(0.65f, 0.94f, 1.00f, 1f));
            EnsureVariantEnemyPrefab(
                "Assets/MedievalRPG/Prefabs/Mage.prefab",
                "Assets/MedievalRPG/Prefabs/ShadowMage.prefab",
                "Pháp Sư Hắc Ám",
                new Color(0.58f, 0.38f, 0.94f, 1f));

            // 4. Đồng bộ ClassData, Material, Sprites & BoxCollider2D trên toàn bộ 8 Prefab nhân vật/quái
            ConfigureHeroFramesAndEquipmentSpriteImports();
            SyncAllUnitPrefabs();

            // 5. Sinh 36 món Trang Bị (3 Class x 3 Slot x 4 Độ hiếm) và Resources/MedievalEquipmentCatalog.asset
            GenerateEquipmentAssetsAndCatalog();

            // 6. Tạo Bản Đồ 6 Đợt Leo Thang (Yếu -> Thành Đàn -> Mạnh Hơn -> Đàn Mạnh -> Tinh Anh -> Boss)
            EncounterData demoEncounter = AssetDatabase.LoadAssetAtPath<EncounterData>($"{EncountersRoot}/ForestDemoEncounter.asset");
            if (demoEncounter == null)
            {
                demoEncounter = ScriptableObject.CreateInstance<EncounterData>();
                AssetDatabase.CreateAsset(demoEncounter, $"{EncountersRoot}/ForestDemoEncounter.asset");
            }

            GameObject goblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/GoblinRaider.prefab");
            GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/Skeleton.prefab");
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/DarkKnightBoss.prefab");

            demoEncounter.encounterTitle = "Rừng Sương Mù - Chinh Phục 6 Đợt Quái";
            demoEncounter.waves = new List<BattleWave>
            {
                new BattleWave
                {
                    waveTitle = "Đợt 1: Trinh Sát Rừng Sâu",
                    waveSubtitle = "Quái yếu đơn lẻ • Khởi động",
                    isBossWave = false,
                    enemyStatMultiplier = 0.80f,
                    dropChanceBonus = 0f,
                    rarityLuckBonus = 0f,
                    enemyPrefabs = new List<GameObject> { goblinPrefab }
                },
                new BattleWave
                {
                    waveTitle = "Đợt 2: Đàn Goblin Phục Kích",
                    waveSubtitle = "Thành đàn 3 Goblin • Ưu tiên chiêu diện rộng",
                    isBossWave = false,
                    enemyStatMultiplier = 0.90f,
                    dropChanceBonus = 0.02f,
                    rarityLuckBonus = 0.03f,
                    enemyPrefabs = new List<GameObject> { goblinPrefab, goblinPrefab, goblinPrefab }
                },
                new BattleWave
                {
                    waveTitle = "Đợt 3: Vệ Binh Xương Khô",
                    waveSubtitle = "Quái mạnh hơn • Giáp cứng",
                    isBossWave = false,
                    enemyStatMultiplier = 1.00f,
                    dropChanceBonus = 0.05f,
                    rarityLuckBonus = 0.06f,
                    enemyPrefabs = new List<GameObject> { goblinPrefab, skeletonPrefab }
                },
                new BattleWave
                {
                    waveTitle = "Đợt 4: Tiểu Đội Hầm Ngục",
                    waveSubtitle = "Đàn quái hạng nặng (3 con)",
                    isBossWave = false,
                    enemyStatMultiplier = 1.08f,
                    dropChanceBonus = 0.08f,
                    rarityLuckBonus = 0.10f,
                    enemyPrefabs = new List<GameObject> { skeletonPrefab, goblinPrefab, skeletonPrefab }
                },
                new BattleWave
                {
                    waveTitle = "Đợt 5: Cận Vệ Tinh Anh",
                    waveSubtitle = "Tinh Anh áp sát Boss • Rớt đồ Sử Thi cao",
                    isBossWave = false,
                    enemyStatMultiplier = 1.25f,
                    dropChanceBonus = 0.12f,
                    rarityLuckBonus = 0.16f,
                    enemyPrefabs = new List<GameObject> { skeletonPrefab, skeletonPrefab }
                },
                new BattleWave
                {
                    waveTitle = "Đợt 6: Thủ Lĩnh Bóng Đêm",
                    waveSubtitle = "BOSS CUỐI MAP • Chắc chắn rớt 2 trang bị cao cấp",
                    isBossWave = true,
                    enemyStatMultiplier = 1.05f,
                    dropChanceBonus = 0f,
                    rarityLuckBonus = 0.15f,
                    enemyPrefabs = new List<GameObject> { bossPrefab }
                }
            };
            EditorUtility.SetDirty(demoEncounter);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("MedievalRPG/Generate Equipment Catalog (36 Items)")]
        public static EquipmentCatalog GenerateEquipmentAssetsAndCatalog()
        {
            if (!AssetDatabase.IsValidFolder(EquipmentRoot))
            {
                AssetDatabase.CreateFolder("Assets/MedievalRPG/ScriptableObjects", "Equipment");
            }
            if (!AssetDatabase.IsValidFolder(ResourcesRoot))
            {
                AssetDatabase.CreateFolder("Assets/MedievalRPG", "Resources");
            }

            Sprite iconSword  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Sprites/UI/Icons/Icon_Sword.png");
            Sprite iconMagic  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Sprites/UI/Icons/Icon_Magic.png");
            Sprite iconArrow  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Sprites/UI/Icons/Icon_Arrow.png");
            Sprite iconShield = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Sprites/UI/Icons/Icon_Shield.png");
            Sprite iconCrown  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Sprites/UI/Icons/Icon_Crown.png");

            EquipmentCatalog catalog = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>(EquipmentCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EquipmentCatalog>();
                AssetDatabase.CreateAsset(catalog, EquipmentCatalogPath);
            }

            catalog.allItems = new List<EquipmentItemData>();
            EquipmentCatalog.EquipmentBlueprint[] blueprints = EquipmentCatalog.GetDefaultBlueprints();
            for (int i = 0; i < blueprints.Length; i++)
            {
                EquipmentCatalog.EquipmentBlueprint bp = blueprints[i];
                string assetPath = $"{EquipmentRoot}/{bp.id}.asset";
                EquipmentItemData item = AssetDatabase.LoadAssetAtPath<EquipmentItemData>(assetPath);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<EquipmentItemData>();
                    AssetDatabase.CreateAsset(item, assetPath);
                }

                EquipmentCatalog.ApplyBlueprint(item, bp);
                Sprite customSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/MedievalRPG/Resources/EquipmentSprites/eq_{bp.id}.png");
                if (customSprite != null)
                {
                    item.icon = customSprite;
                }
                else if (bp.slot == EquipmentSlot.Armor)
                {
                    item.icon = iconShield;
                }
                else if (bp.slot == EquipmentSlot.Accessory || bp.slot == EquipmentSlot.Helmet)
                {
                    item.icon = iconCrown;
                }
                else
                {
                    item.icon = bp.cls == CharacterClassType.Mage ? iconMagic : (bp.cls == CharacterClassType.Archer ? iconArrow : iconSword);
                }

                EditorUtility.SetDirty(item);
                catalog.allItems.Add(item);
            }

            catalog.RebuildLookup();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MedievalRPGAssetSetup] Đã sinh {catalog.allItems.Count} trang bị cho đủ 3 Class x 4 Độ hiếm tại {EquipmentCatalogPath}.");
            return catalog;
        }

        private static SkillData CreateOrUpdateSkill(
            string path, string skillName, string desc, CharacterClassType reqClass,
            int manaCost, int cooldown, SkillTargetType targetType, DamageType dmgType,
            float powerMult, int flatBonus, int heal, int defBonus, float bonusCrit, int effTurns,
            StatusEffectType statusType, int statusTurns, int statusPotency, bool toSelf)
        {
            SkillData s = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(s, path);
            }

            s.skillName = skillName;
            s.description = desc;
            s.requiredClass = reqClass;
            s.manaCost = manaCost;
            s.cooldownTurns = cooldown;
            s.targetType = targetType;
            s.damageType = dmgType;
            s.powerMultiplier = powerMult;
            s.flatBonusDamage = flatBonus;
            s.baseHealAmount = heal;
            s.defenseBonus = defBonus;
            s.bonusCritRate = bonusCrit;
            s.effectDurationTurns = effTurns;
            s.statusEffect = statusType;
            s.statusDurationTurns = statusTurns;
            s.statusPotency = statusPotency;
            s.applyStatusToSelf = toSelf;

            EditorUtility.SetDirty(s);
            return s;
        }

        private static void AssignSkillsListOnly(string classPath, params SkillData[] list)
        {
            CharacterClassData cls = AssetDatabase.LoadAssetAtPath<CharacterClassData>(classPath);
            if (cls == null) return;
            cls.skills = new List<SkillData>();
            foreach (SkillData s in list)
            {
                if (s != null) cls.skills.Add(s);
            }
            EditorUtility.SetDirty(cls);
        }

        private static void AssignSkillsToClass(string classPath, int baseLevel, bool isBoss, params SkillData[] list)
        {
            CharacterClassData cls = AssetDatabase.LoadAssetAtPath<CharacterClassData>(classPath);
            if (cls == null) return;
            cls.baseLevel = baseLevel;
            cls.isBoss = isBoss;
            cls.skills = new List<SkillData>();
            foreach (SkillData s in list)
            {
                if (s != null) cls.skills.Add(s);
            }
            EditorUtility.SetDirty(cls);
        }

        private static void SyncAllUnitPrefabs()
        {
            Material spriteLitMat = null;
            GameObject knightAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/Knight.prefab");
            PixelCharacterAnimator knightAnimTemplate = null;
            if (knightAsset != null)
            {
                SpriteRenderer kSr = knightAsset.GetComponentInChildren<SpriteRenderer>(true);
                if (kSr != null) spriteLitMat = kSr.sharedMaterial;
                knightAnimTemplate = knightAsset.GetComponent<PixelCharacterAnimator>();
            }
            if (spriteLitMat == null)
            {
                spriteLitMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            }

            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Knight.prefab", $"{ClassesRoot}/KnightClass.asset", false, spriteLitMat, null, new Vector2(1.2f, 1.45f), KnightVisualScale);
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Mage.prefab", $"{ClassesRoot}/MageClass.asset", false, spriteLitMat, null, new Vector2(1.2f, 1.45f), MageVisualScale);
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Archer.prefab", $"{ClassesRoot}/ArcherClass.asset", false, spriteLitMat, null, new Vector2(1.2f, 1.45f), ArcherVisualScale);
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/GoblinRaider.prefab", $"{ClassesRoot}/GoblinClass.asset", true, spriteLitMat, null, new Vector2(1.25f, 1.45f), 1f);
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Skeleton.prefab", $"{ClassesRoot}/SkeletonClass.asset", true, spriteLitMat, null, new Vector2(1.25f, 1.45f), 1f);
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/SkeletonArcher.prefab", $"{ClassesRoot}/SkeletonArcherClass.asset", true, spriteLitMat, null, new Vector2(1.25f, 1.45f), ArcherVisualScale);
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/ShadowMage.prefab", $"{ClassesRoot}/ShadowMageClass.asset", true, spriteLitMat, null, new Vector2(1.25f, 1.45f), MageVisualScale);
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/DarkKnightBoss.prefab", $"{ClassesRoot}/DarkKnightBossClass.asset", true, spriteLitMat, knightAnimTemplate, new Vector2(1.45f, 1.75f), DarkKnightBossVisualScale);
        }

        private static void EnsureEnemyClassAsset(
            string path, string className, CharacterClassType classType,
            int baseLevel, int hp, int mp, int atk, int mag, int def, int mdef, int spd, float crit,
            params SkillData[] list)
        {
            CharacterClassData cls = AssetDatabase.LoadAssetAtPath<CharacterClassData>(path);
            if (cls == null)
            {
                cls = ScriptableObject.CreateInstance<CharacterClassData>();
                AssetDatabase.CreateAsset(cls, path);
            }
            cls.className = className;
            cls.classType = classType;
            cls.baseLevel = baseLevel;
            cls.isBoss = false;
            cls.maxHealth = hp;
            cls.maxMana = mp;
            cls.physicalAttack = atk;
            cls.magicAttack = mag;
            cls.physicalDefense = def;
            cls.magicDefense = mdef;
            cls.speed = spd;
            cls.critRate = crit;
            cls.critMultiplier = 1.5f;
            cls.skills = new List<SkillData>();
            foreach (SkillData s in list)
            {
                if (s != null) cls.skills.Add(s);
            }
            EditorUtility.SetDirty(cls);
        }

        private static void EnsureVariantEnemyPrefab(string sourcePrefabPath, string destPrefabPath, string unitTitle, Color tint)
        {
            if (!System.IO.File.Exists(sourcePrefabPath)) return;
            if (!System.IO.File.Exists(destPrefabPath))
            {
                AssetDatabase.CopyAsset(sourcePrefabPath, destPrefabPath);
                AssetDatabase.Refresh();
            }

            GameObject srcRoot = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            PixelCharacterAnimator srcAnim = srcRoot != null ? srcRoot.GetComponent<PixelCharacterAnimator>() : null;
            SpriteRenderer srcSr = srcRoot != null ? srcRoot.GetComponentInChildren<SpriteRenderer>(true) : null;

            GameObject root = PrefabUtility.LoadPrefabContents(destPrefabPath);
            try
            {
                Unit u = root.GetComponent<Unit>();
                if (u != null)
                {
                    u.unitName = unitTitle;
                    u.isEnemy = true;
                    EditorUtility.SetDirty(u);
                }

                PixelCharacterAnimator dstAnim = root.GetComponent<PixelCharacterAnimator>();
                if (dstAnim != null && srcAnim != null)
                {
                    dstAnim.idleFrames = new List<Sprite>(srcAnim.idleFrames);
                    dstAnim.runFrames = new List<Sprite>(srcAnim.runFrames);
                    dstAnim.attackFrames = new List<Sprite>(srcAnim.attackFrames);
                    dstAnim.hitFrames = new List<Sprite>(srcAnim.hitFrames);
                    dstAnim.deathFrames = new List<Sprite>(srcAnim.deathFrames);
                    dstAnim.SetFacingRight(false);
                    EditorUtility.SetDirty(dstAnim);
                }

                SpriteRenderer sr = root.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null)
                {
                    if (srcSr != null && srcSr.sprite != null) sr.sprite = srcSr.sprite;
                    sr.color = tint;
                    EditorUtility.SetDirty(sr);
                }
                PrefabUtility.SaveAsPrefabAsset(root, destPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SyncUnitPrefab(
            string prefabPath,
            string classPath,
            bool isEnemy,
            Material fallbackSpriteMat,
            PixelCharacterAnimator bossSpriteSource,
            Vector2 targetWorldColliderSize,
            float uniformScale = 1f)
        {
            if (!System.IO.File.Exists(prefabPath)) return;
            CharacterClassData cls = AssetDatabase.LoadAssetAtPath<CharacterClassData>(classPath);

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                float safeScale = Mathf.Max(0.1f, uniformScale);
                root.transform.localScale = new Vector3(safeScale, safeScale, 1f);

                Unit unit = root.GetComponent<Unit>();
                if (unit != null)
                {
                    unit.isEnemy = isEnemy;
                    if (cls != null)
                    {
                        unit.InitializeFromClass(cls);
                    }
                    EditorUtility.SetDirty(unit);
                }

                SpriteRenderer sr = root.GetComponentInChildren<SpriteRenderer>(true);
                PixelCharacterAnimator anim = root.GetComponent<PixelCharacterAnimator>();

                if (bossSpriteSource != null && anim != null)
                {
                    if (HasMissingFrames(anim.idleFrames)) anim.idleFrames = new List<Sprite>(bossSpriteSource.idleFrames);
                    if (HasMissingFrames(anim.runFrames)) anim.runFrames = new List<Sprite>(bossSpriteSource.runFrames);
                    if (HasMissingFrames(anim.attackFrames)) anim.attackFrames = new List<Sprite>(bossSpriteSource.attackFrames);
                    if (HasMissingFrames(anim.hitFrames)) anim.hitFrames = new List<Sprite>(bossSpriteSource.hitFrames);
                    if (HasMissingFrames(anim.deathFrames)) anim.deathFrames = new List<Sprite>(bossSpriteSource.deathFrames);
                    EditorUtility.SetDirty(anim);
                }

                if (sr != null)
                {
                    if (sr.sharedMaterial == null && fallbackSpriteMat != null)
                    {
                        sr.sharedMaterial = fallbackSpriteMat;
                    }
                    if (sr.sprite == null && anim != null && anim.idleFrames != null && anim.idleFrames.Count > 0)
                    {
                        sr.sprite = anim.idleFrames[0];
                    }
                    EditorUtility.SetDirty(sr);
                }

                BoxCollider2D col = root.GetComponent<BoxCollider2D>();
                if (col == null) col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(
                    Mathf.Round((targetWorldColliderSize.x / safeScale) * 100f) / 100f,
                    Mathf.Round((targetWorldColliderSize.y / safeScale) * 100f) / 100f);
                EditorUtility.SetDirty(col);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool HasMissingFrames(List<Sprite> frames)
        {
            if (frames == null || frames.Count == 0) return true;
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] == null) return true;
            }
            return false;
        }

        private static void ConfigureHeroFramesAndEquipmentSpriteImports()
        {
            string[] heroFolders =
            {
                "Assets/MedievalRPG/Characters/Knight",
                "Assets/MedievalRPG/Characters/Mage",
                "Assets/MedievalRPG/Characters/Archer"
            };
            string[] heroGuids = AssetDatabase.FindAssets("t:Texture2D", heroFolders);
            foreach (string guid in heroGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = false;
                if (!importer.isReadable) { importer.isReadable = true; changed = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }

            string equipSpriteDir = "Assets/MedievalRPG/Resources/EquipmentSprites";
            if (!AssetDatabase.IsValidFolder(equipSpriteDir)) return;
            string[] eqGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { equipSpriteDir });
            foreach (string guid in eqGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.1f) { importer.spritePixelsPerUnit = 100f; changed = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        }
    }
}
