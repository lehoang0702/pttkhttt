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

            // 3. Gán kỹ năng cho cả 3 Anh hùng & 3 Quái vật/Boss
            SkillData knightSlash = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Knight_Slash.asset");
            AssignSkillsListOnly($"{ClassesRoot}/KnightClass.asset", knightSlash, shieldSlam, ironBulwark);

            SkillData mageBolt = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Mage_ArcaneBolt.asset");
            SkillData mageHeal = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Mage_HealingLight.asset");
            AssignSkillsListOnly($"{ClassesRoot}/MageClass.asset", mageBolt, fireball, mageHeal);

            SkillData archerQuick = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Archer_QuickShot.asset");
            SkillData archerPrecision = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Archer_PrecisionShot.asset");
            SkillData archerRain = AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsRoot}/Archer_RainOfArrows.asset");
            AssignSkillsListOnly($"{ClassesRoot}/ArcherClass.asset", archerQuick, archerPrecision, archerRain);

            AssignSkillsToClass($"{ClassesRoot}/GoblinClass.asset", 1, false, goblinStab, goblinPoison);
            AssignSkillsToClass($"{ClassesRoot}/SkeletonClass.asset", 2, false, skelCleave, skelShield);
            AssignSkillsToClass($"{ClassesRoot}/DarkKnightBossClass.asset", 5, true, bossSlash, bossWave, bossBulwark);

            // 4. Đồng bộ ClassData, Material, Sprites & BoxCollider2D trên toàn bộ 6 Prefab nhân vật/quái
            SyncAllUnitPrefabs();

            // 5. Tạo Encounter 3 Wave Demo
            EncounterData demoEncounter = AssetDatabase.LoadAssetAtPath<EncounterData>($"{EncountersRoot}/ForestDemoEncounter.asset");
            if (demoEncounter == null)
            {
                demoEncounter = ScriptableObject.CreateInstance<EncounterData>();
                AssetDatabase.CreateAsset(demoEncounter, $"{EncountersRoot}/ForestDemoEncounter.asset");
            }

            GameObject goblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/GoblinRaider.prefab");
            GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/Skeleton.prefab");
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/DarkKnightBoss.prefab");

            demoEncounter.encounterTitle = "Rừng Sương Mù - Thử Thách Tri Thức";
            demoEncounter.waves = new List<BattleWave>
            {
                new BattleWave
                {
                    waveTitle = "Đợt 1: Toán Quân Goblin",
                    isBossWave = false,
                    enemyPrefabs = new List<GameObject> { goblinPrefab, goblinPrefab }
                },
                new BattleWave
                {
                    waveTitle = "Đợt 2: Vệ Binh Hầm Ngục",
                    isBossWave = false,
                    enemyPrefabs = new List<GameObject> { goblinPrefab, skeletonPrefab }
                },
                new BattleWave
                {
                    waveTitle = "Đợt 3: Thủ Lĩnh Bóng Đêm",
                    isBossWave = true,
                    enemyPrefabs = new List<GameObject> { bossPrefab }
                }
            };
            EditorUtility.SetDirty(demoEncounter);
            AssetDatabase.SaveAssets();
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

            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Knight.prefab", $"{ClassesRoot}/KnightClass.asset", false, spriteLitMat, null, new Vector2(1.2f, 1.45f));
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Mage.prefab", $"{ClassesRoot}/MageClass.asset", false, spriteLitMat, null, new Vector2(1.2f, 1.45f));
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Archer.prefab", $"{ClassesRoot}/ArcherClass.asset", false, spriteLitMat, null, new Vector2(1.2f, 1.45f));
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/GoblinRaider.prefab", $"{ClassesRoot}/GoblinClass.asset", true, spriteLitMat, null, new Vector2(1.25f, 1.45f));
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/Skeleton.prefab", $"{ClassesRoot}/SkeletonClass.asset", true, spriteLitMat, null, new Vector2(1.25f, 1.45f));
            SyncUnitPrefab("Assets/MedievalRPG/Prefabs/DarkKnightBoss.prefab", $"{ClassesRoot}/DarkKnightBossClass.asset", true, spriteLitMat, knightAnimTemplate, new Vector2(1.45f, 1.75f));
        }

        private static void SyncUnitPrefab(
            string prefabPath,
            string classPath,
            bool isEnemy,
            Material fallbackSpriteMat,
            PixelCharacterAnimator bossSpriteSource,
            Vector2 minColliderSize)
        {
            if (!System.IO.File.Exists(prefabPath)) return;
            CharacterClassData cls = AssetDatabase.LoadAssetAtPath<CharacterClassData>(classPath);

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
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
                    Mathf.Max(col.size.x, minColliderSize.x),
                    Mathf.Max(col.size.y, minColliderSize.y));
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
    }
}
