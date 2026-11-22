using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;

namespace MedievalRPG.Tests.EditMode
{
    public class LootAndEquipmentTests
    {
        [Test]
        public void EquipmentCatalog_CoversAll3ClassesAll3SlotsAndAll4Rarities()
        {
            EquipmentCatalog catalog = EquipmentCatalog.CreateRuntimeFallbackCatalog();
            try
            {
                Assert.AreEqual(36, catalog.allItems.Count, "Phải có đủ 36 món (3 Class x 3 Slot x 4 Độ hiếm)");

                foreach (CharacterClassType cls in (CharacterClassType[])Enum.GetValues(typeof(CharacterClassType)))
                {
                    foreach (EquipmentSlot slot in (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot)))
                    {
                        int prevScore = -1;
                        foreach (ItemRarity rarity in (ItemRarity[])Enum.GetValues(typeof(ItemRarity)))
                        {
                            EquipmentItemData found = catalog.allItems.Find(
                                i => i.requiredClass == cls && i.slot == slot && i.rarity == rarity);
                            Assert.IsNotNull(found, $"Thiếu trang bị cho {cls} / {slot} / {rarity}");

                            int score = found.GetPowerScore();
                            Assert.Greater(score, prevScore, $"Trang bị cấp cao hơn phải mạnh hơn: {cls}/{slot}/{rarity}");
                            prevScore = score;
                        }
                    }
                }
            }
            finally
            {
                foreach (EquipmentItemData item in catalog.allItems)
                {
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
                }
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void LootSystem_DropChanceAndRarityIncreaseWithMapDepth()
        {
            const int totalWaves = 6;
            float prevChance = 0f;
            float prevCommonWeight = 101f;
            float prevLegendaryWeight = -1f;

            for (int w = 0; w < totalWaves; w++)
            {
                float chance = LootSystem.CalculateEquipmentDropChance(w, totalWaves, false);
                Assert.Greater(chance, prevChance, $"Tỉ lệ drop ở Wave {w + 1} phải cao hơn Wave {w}");
                prevChance = chance;

                float[] weights = LootSystem.GetRarityWeights(w, totalWaves, false);
                Assert.AreEqual(4, weights.Length);
                Assert.Less(weights[(int)ItemRarity.Common], prevCommonWeight, "Tỉ lệ Thường phải giảm khi đi xa");
                Assert.Greater(weights[(int)ItemRarity.Legendary], prevLegendaryWeight, "Tỉ lệ Huyền Thoại phải tăng khi đi xa");
                prevCommonWeight = weights[(int)ItemRarity.Common];
                prevLegendaryWeight = weights[(int)ItemRarity.Legendary];
            }

            // Boss cuối map luôn 100% rớt đồ và không bao giờ rớt đồ Thường
            Assert.AreEqual(1f, LootSystem.CalculateEquipmentDropChance(5, totalWaves, true), 0.0001f);
            float[] bossWeights = LootSystem.GetRarityWeights(5, totalWaves, true);
            Assert.AreEqual(0f, bossWeights[(int)ItemRarity.Common], 0.0001f);
            Assert.GreaterOrEqual(bossWeights[(int)ItemRarity.Epic] + bossWeights[(int)ItemRarity.Legendary], 75f);
        }

        [Test]
        public void LootSystem_BossAlwaysDropsTwoHighTierEquipment()
        {
            EquipmentCatalog catalog = EquipmentCatalog.CreateRuntimeFallbackCatalog();
            try
            {
                for (int seed = 1; seed <= 20; seed++)
                {
                    List<LootDropResult> drops = LootSystem.RollDropsForDefeatedEnemy(
                        catalog, 5, 6, isBoss: true, waveDropBonus: 0f, waveRarityLuck: 0.15f, quizStreak: 3, rng: new System.Random(seed));

                    List<EquipmentItemData> equipDrops = new List<EquipmentItemData>();
                    foreach (LootDropResult d in drops)
                    {
                        if (!d.isConsumableOnly && d.equipment != null) equipDrops.Add(d.equipment);
                    }

                    Assert.AreEqual(2, equipDrops.Count, $"Boss phải luôn rớt 2 trang bị (seed {seed})");
                    bool hasEpicOrHigher = false;
                    foreach (EquipmentItemData item in equipDrops)
                    {
                        Assert.GreaterOrEqual((int)item.rarity, (int)ItemRarity.Rare, "Boss không được rớt đồ Thường");
                        if (item.rarity >= ItemRarity.Epic) hasEpicOrHigher = true;
                    }
                    Assert.IsTrue(hasEpicOrHigher, "Boss phải chắc chắn có ít nhất 1 món Sử Thi hoặc Huyền Thoại");
                }
            }
            finally
            {
                foreach (EquipmentItemData item in catalog.allItems)
                {
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
                }
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayerEquipmentManager_EquipAndAutoEquipUpdatesUnitStatsCorrectly()
        {
            EquipmentCatalog catalog = EquipmentCatalog.CreateRuntimeFallbackCatalog();
            GameObject go = new GameObject("HeroEquipTest");
            try
            {
                PlayerEquipmentManager mgr = new PlayerEquipmentManager();
                mgr.InitializeForTests(catalog);

                EquipmentItemData commonSword = catalog.GetById("knight_weapon_common");
                EquipmentItemData legendSword = catalog.GetById("knight_weapon_legendary");
                EquipmentItemData epicStaff = catalog.GetById("mage_weapon_epic");
                EquipmentItemData rareBow = catalog.GetById("archer_weapon_rare");

                mgr.AddItem(commonSword, 1);
                mgr.AddItem(legendSword, 6);
                mgr.AddItem(epicStaff, 5);
                mgr.AddItem(rareBow, 3);

                int equippedCount = mgr.AutoEquipBestForAllClasses();
                Assert.AreEqual(3, equippedCount);
                Assert.AreEqual(legendSword, mgr.GetEquippedItemData(CharacterClassType.Knight, EquipmentSlot.Weapon));
                Assert.AreEqual(epicStaff, mgr.GetEquippedItemData(CharacterClassType.Mage, EquipmentSlot.Weapon));
                Assert.AreEqual(rareBow, mgr.GetEquippedItemData(CharacterClassType.Archer, EquipmentSlot.Weapon));

                Unit knight = go.AddComponent<Unit>();
                knight.characterClass = CharacterClassType.Knight;
                knight.maxHP = 160;
                knight.currentHP = 160;
                knight.damage = 22;
                knight.defense = 18;
                knight.speed = 9;

                EquipmentStatBonus knightBonus = mgr.GetTotalBonusesForClass(CharacterClassType.Knight);
                knight.ApplyEquipmentBonuses(knightBonus);

                Assert.AreEqual(160 + legendSword.bonusMaxHP, knight.maxHP);
                Assert.AreEqual(160 + legendSword.bonusMaxHP, knight.currentHP);
                Assert.AreEqual(22 + legendSword.bonusPhysicalAttack, knight.damage);

                // Gỡ đồ ra phải trả lại đúng chỉ số gốc
                Assert.IsTrue(mgr.Unequip(CharacterClassType.Knight, EquipmentSlot.Weapon));
                knight.ApplyEquipmentBonuses(mgr.GetTotalBonusesForClass(CharacterClassType.Knight));
                Assert.AreEqual(160, knight.maxHP);
                Assert.AreEqual(160, knight.currentHP);
                Assert.AreEqual(22, knight.damage);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                foreach (EquipmentItemData item in catalog.allItems)
                {
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
                }
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Unit_WaveScaleCreatesWeakEarlyEnemiesAndEliteLateEnemies()
        {
            GameObject g1 = new GameObject("WeakScout");
            GameObject g2 = new GameObject("EliteGuard");
            try
            {
                Unit scout = g1.AddComponent<Unit>();
                scout.unitName = "Goblin Raider";
                scout.maxHP = 80;
                scout.currentHP = 80;
                scout.damage = 16;
                scout.ApplyWaveScale(0.80f, false);
                Assert.AreEqual(64, scout.maxHP);
                Assert.AreEqual(13, scout.damage);
                Assert.AreEqual("Goblin Raider", scout.unitName);

                Unit elite = g2.AddComponent<Unit>();
                var eliteAnim = g2.AddComponent<MedievalRPG.Visuals.PixelCharacterAnimator>();
                elite.pixelAnimator = eliteAnim;
                elite.unitName = "Skeleton Warrior";
                elite.unitLevel = 2;
                elite.maxHP = 130;
                elite.currentHP = 130;
                elite.damage = 22;
                elite.ApplyWaveScale(1.25f, true);
                Assert.AreEqual(162, elite.maxHP);
                Assert.AreEqual(28, elite.damage);
                Assert.IsTrue(elite.unitName.StartsWith("Tinh Anh "));
                Assert.AreEqual(3, elite.unitLevel);
                Assert.Less(g2.GetComponent<SpriteRenderer>().color.g, 0.8f, "Quái Tinh Anh phải giữ viền/màu tím nổi bật");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(g1);
                UnityEngine.Object.DestroyImmediate(g2);
            }
        }
    }
}
