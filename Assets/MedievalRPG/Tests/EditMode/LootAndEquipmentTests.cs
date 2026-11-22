using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Quiz;

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

        [Test]
        public void QuizManager_DefaultQuestionsAreAllEnglishB1OrB2AndValid()
        {
            GameObject go = new GameObject("QuizManagerTest");
            try
            {
                QuizManager qm = go.AddComponent<QuizManager>();
                qm.LoadDefaultQuestions();

                Assert.AreEqual(120, qm.questionBank.Count, "Phải nạp đủ 120 câu hỏi tiếng Anh (60 B1 + 60 B2) từ english_b1_b2.json");

                int b1Count = 0;
                int b2Count = 0;
                for (int i = 0; i < qm.questionBank.Count; i++)
                {
                    QuizQuestion q = qm.questionBank[i];
                    Assert.IsNotNull(q);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(q.category), $"Câu {i} thiếu category");

                    bool isB1 = q.category.StartsWith("B1", StringComparison.OrdinalIgnoreCase);
                    bool isB2 = q.category.StartsWith("B2", StringComparison.OrdinalIgnoreCase);
                    Assert.IsTrue(isB1 || isB2, $"Câu {i} phải thuộc chuẩn B1 hoặc B2, đang là: {q.category}");
                    if (isB1) b1Count++;
                    if (isB2) b2Count++;

                    Assert.IsFalse(string.IsNullOrWhiteSpace(q.questionText), $"Câu {i} rỗng");
                    Assert.IsNotNull(q.answers, $"Câu {i} thiếu danh sách đáp án");
                    Assert.AreEqual(4, q.answers.Length, $"Câu {i} phải có đúng 4 lựa chọn");
                    for (int a = 0; a < 4; a++)
                    {
                        Assert.IsFalse(string.IsNullOrWhiteSpace(q.answers[a]), $"Câu {i} đáp án {a} rỗng");
                    }
                    Assert.GreaterOrEqual(q.correctAnswerIndex, 0);
                    Assert.Less(q.correctAnswerIndex, 4);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(q.explanation), $"Câu {i} thiếu giải thích");
                }

                Assert.AreEqual(60, b1Count, "Phải có 60 câu trình độ B1");
                Assert.AreEqual(60, b2Count, "Phải có 60 câu trình độ B2");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlayerCampProgressManager_ShopPurchaseBuffsAndBattleSyncWorkCorrectly()
        {
            PlayerCampProgressManager camp = new PlayerCampProgressManager();
            camp.InitializeForTests(180);

            Assert.AreEqual(180, camp.Gold);
            Assert.IsTrue(camp.TryBuyConsumable(ConsumableItemType.WarElixir));
            Assert.AreEqual(115, camp.Gold);
            Assert.AreEqual(1, camp.GetConsumableCount(ConsumableItemType.WarElixir));

            Assert.IsTrue(camp.TryBuyCampBuff(CampBuffType.VitalityBlessing));
            Assert.AreEqual(55, camp.Gold);
            Assert.IsTrue(camp.IsCampBuffActive(CampBuffType.VitalityBlessing));
            Assert.IsFalse(camp.TryBuyCampBuff(CampBuffType.VitalityBlessing), "Không được mua trùng bùa đang bật");
            Assert.IsFalse(camp.TryBuyCampBuff(CampBuffType.FuryBlessing), "Không đủ vàng (55 < 75) phải trả về false");

            EquipmentStatBonus campBonus = camp.GetActiveCampBonusTotal();
            Assert.AreEqual(35, campBonus.maxHP);
            Assert.AreEqual(0, campBonus.physicalAttack);

            BattleInventory inv = new BattleInventory();
            camp.ApplyToBattleInventory(inv);
            Assert.AreEqual(1, inv.GetCount(ConsumableItemType.WarElixir));
            Assert.IsTrue(inv.TryConsume(ConsumableItemType.WarElixir));
            Assert.AreEqual(0, inv.GetCount(ConsumableItemType.WarElixir));

            camp.SyncAfterBattle(inv, consumeActiveBuffs: true);
            Assert.AreEqual(0, camp.GetConsumableCount(ConsumableItemType.WarElixir));
            Assert.IsFalse(camp.IsCampBuffActive(CampBuffType.VitalityBlessing), "Bùa 1 trận phải hết hiệu lực sau trận");
        }

        [Test]
        public void BattleInventory_SupportsAll5ConsumablesIncludingWarElixirAndIronTonic()
        {
            BattleInventory defaultInv = new BattleInventory(3, 2, 1);
            Assert.AreEqual(6, defaultInv.TotalItems);

            defaultInv.Reset(2, 1, 1, 2, 3);
            Assert.AreEqual(9, defaultInv.TotalItems);
            Assert.AreEqual(2, defaultInv.GetCount(ConsumableItemType.WarElixir));
            Assert.AreEqual(3, defaultInv.GetCount(ConsumableItemType.IronTonic));

            Assert.IsTrue(defaultInv.TryConsume(ConsumableItemType.WarElixir));
            Assert.IsTrue(defaultInv.TryConsume(ConsumableItemType.IronTonic));
            Assert.AreEqual(1, defaultInv.GetCount(ConsumableItemType.WarElixir));
            Assert.AreEqual(2, defaultInv.GetCount(ConsumableItemType.IronTonic));

            foreach (ConsumableItemType t in (ConsumableItemType[])Enum.GetValues(typeof(ConsumableItemType)))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(BattleInventory.GetDisplayName(t)));
                Assert.IsFalse(string.IsNullOrWhiteSpace(BattleInventory.GetEffectDescription(t)));
            }
        }

        [Test]
        public void Equipment_UpgradeSellBulkSellAndForge3To1WorkCorrectly()
        {
            EquipmentCatalog catalog = EquipmentCatalog.CreateRuntimeFallbackCatalog();
            PlayerEquipmentManager eqMgr = new PlayerEquipmentManager();
            try
            {
                eqMgr.InitializeForTests(catalog);
                EquipmentItemData commonSword = catalog.allItems.Find(
                    i => i.requiredClass == CharacterClassType.Knight && i.slot == EquipmentSlot.Weapon && i.rarity == ItemRarity.Common);
                Assert.IsNotNull(commonSword);

                OwnedEquipmentInstance inst1 = eqMgr.AddItem(commonSword, 1);
                Assert.AreEqual(0, inst1.upgradeLevel);
                int baseScore = commonSword.GetPowerScore(0);

                // Cường hóa +0 -> +1 -> +5
                int cost0 = commonSword.GetUpgradeCost(0);
                Assert.IsTrue(eqMgr.TryUpgradeItem(inst1.instanceId, cost0, out int spent0));
                Assert.AreEqual(cost0, spent0);
                Assert.AreEqual(1, inst1.upgradeLevel);
                Assert.Greater(commonSword.GetPowerScore(1), baseScore);

                Assert.IsTrue(eqMgr.Equip(inst1.instanceId));
                int atkPlus1 = eqMgr.GetTotalBonusesForClass(CharacterClassType.Knight).physicalAttack;
                Assert.GreaterOrEqual(atkPlus1, commonSword.bonusPhysicalAttack);

                for (int lv = 1; lv < EquipmentItemData.MaxUpgradeLevel; lv++)
                {
                    Assert.IsTrue(eqMgr.TryUpgradeItem(inst1.instanceId, 99999, out _));
                }
                Assert.AreEqual(EquipmentItemData.MaxUpgradeLevel, inst1.upgradeLevel);
                Assert.IsFalse(eqMgr.TryUpgradeItem(inst1.instanceId, 99999, out _), "Không vượt quá +5");

                int expectedSell = commonSword.GetSellGoldValue(EquipmentItemData.MaxUpgradeLevel);
                Assert.IsTrue(eqMgr.SellItem(inst1.instanceId, out int goldEarned));
                Assert.AreEqual(expectedSell, goldEarned);
                Assert.AreEqual(0, eqMgr.OwnedCount);

                // Bán nhanh đồ Thường rảnh +0
                eqMgr.AddItem(commonSword, 1);
                eqMgr.AddItem(commonSword, 1);
                int soldCount = eqMgr.SellAllUnequippedByMaxRarity(ItemRarity.Common, out int bulkGold);
                Assert.AreEqual(2, soldCount);
                Assert.Greater(bulkGold, 0);
                Assert.AreEqual(0, eqMgr.OwnedCount);

                // Ghép 3 đồ Thường -> 1 đồ Hiếm
                eqMgr.AddItem(commonSword, 1);
                eqMgr.AddItem(commonSword, 1);
                eqMgr.AddItem(commonSword, 1);
                Assert.AreEqual(3, eqMgr.CountUnequippedByRarity(ItemRarity.Common));
                Assert.IsTrue(eqMgr.TryForgeThreeIntoNextRarity(ItemRarity.Common, out EquipmentItemData forgedData, out OwnedEquipmentInstance forgedInst));
                Assert.IsNotNull(forgedData);
                Assert.IsNotNull(forgedInst);
                Assert.AreEqual(ItemRarity.Rare, forgedData.rarity);
                Assert.AreEqual(1, eqMgr.OwnedCount);
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
        public void PlayerCampProgress_HeroLevelStageUnlockMistakeReviewAndAchievementsWork()
        {
            PlayerCampProgressManager camp = new PlayerCampProgressManager();
            camp.InitializeForTests(200);

            int lv1Hp = camp.GetLevelBonusForClass(CharacterClassType.Knight).maxHP;
            int needExp = PlayerCampProgressManager.GetExpRequiredForNextLevel(1);
            camp.AwardPartyExp(needExp + 5, out List<CharacterClassType> ups);
            Assert.AreEqual(3, ups.Count, "Cả 3 anh hùng đều phải lên Lv.2");
            Assert.AreEqual(2, camp.GetHeroProgress(CharacterClassType.Knight).level);
            Assert.Greater(camp.GetLevelBonusForClass(CharacterClassType.Knight).maxHP, lv1Hp);

            Assert.IsFalse(camp.GetStageProgress(CampaignStageId.CryptChapter2).unlocked);
            camp.RecordStageResult(CampaignStageId.ForestChapter1, true, 6, 3, out int firstGold, out bool unlockedNext);
            Assert.Greater(firstGold, 0);
            Assert.IsTrue(unlockedNext);
            Assert.IsTrue(camp.GetStageProgress(CampaignStageId.CryptChapter2).unlocked);
            Assert.AreEqual(3, camp.GetStageProgress(CampaignStageId.ForestChapter1).bestStars);
            Assert.IsTrue(camp.IsAchievementUnlocked(AchievementId.FirstChapterClear));

            int goldBefore = camp.Gold;
            Assert.IsTrue(camp.TryClaimAchievement(AchievementId.FirstChapterClear, out int achGold));
            Assert.AreEqual(80, achGold);
            Assert.AreEqual(goldBefore + 80, camp.Gold);
            Assert.IsFalse(camp.TryClaimAchievement(AchievementId.FirstChapterClear, out _), "Không được nhận trùng thành tựu");

            camp.RecordQuizMistake("Q1");
            camp.RecordQuizMistake("Q2");
            camp.RecordQuizMistake("Q3");
            Assert.AreEqual(3, camp.MistakeCount);
            Assert.IsTrue(camp.ResolveQuizMistake("Q1"));
            Assert.IsTrue(camp.ResolveQuizMistake("Q2"));
            Assert.IsTrue(camp.ResolveQuizMistake("Q3"));
            Assert.AreEqual(0, camp.MistakeCount);
            Assert.AreEqual(3, camp.MistakesReviewedCount);
            Assert.IsTrue(camp.IsAchievementUnlocked(AchievementId.MistakeReviewer));
        }

        [Test]
        public void QuizManager_DifficultyFilterOnlyB1AndOnlyB2Work()
        {
            GameObject go = new GameObject("QuizFilterTest");
            try
            {
                QuizManager qm = go.AddComponent<QuizManager>();
                qm.LoadDefaultQuestions();

                for (int i = 0; i < 10; i++)
                {
                    QuizQuestion b1 = qm.GetRandomQuestion(QuizDifficultyFilter.OnlyB1);
                    Assert.IsNotNull(b1);
                    Assert.IsTrue(b1.category.StartsWith("B1", StringComparison.OrdinalIgnoreCase), $"Phải lọc đúng B1 nhưng ra {b1.category}");

                    QuizQuestion b2 = qm.GetRandomQuestion(QuizDifficultyFilter.OnlyB2);
                    Assert.IsNotNull(b2);
                    Assert.IsTrue(b2.category.StartsWith("B2", StringComparison.OrdinalIgnoreCase), $"Phải lọc đúng B2 nhưng ra {b2.category}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
