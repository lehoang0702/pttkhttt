using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Quiz;
using MedievalRPG.Visuals;

namespace MedievalRPG.Tests.EditMode
{
    public class LootAndEquipmentTests
    {
        [Test]
        public void EquipmentCatalog_CoversAll3ClassesAll4SlotsAnd13RarityVariantsEach()
        {
            EquipmentCatalog catalog = EquipmentCatalog.CreateRuntimeFallbackCatalog();
            try
            {
                Assert.AreEqual(156, catalog.allItems.Count, "Phải có đủ 156 món (3 Class x 4 Slot x 13 món)");

                var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (EquipmentItemData item in catalog.allItems)
                {
                    Assert.IsNotNull(item);
                    Assert.IsTrue(uniqueIds.Add(item.itemId), $"Trùng itemId: {item.itemId}");
                }

                var expectedCounts = new Dictionary<ItemRarity, int>
                {
                    { ItemRarity.Common, 4 },
                    { ItemRarity.Rare, 4 },
                    { ItemRarity.Epic, 3 },
                    { ItemRarity.Legendary, 2 },
                };

                foreach (CharacterClassType cls in (CharacterClassType[])Enum.GetValues(typeof(CharacterClassType)))
                {
                    foreach (EquipmentSlot slot in (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot)))
                    {
                        int prevTierMax = -1;
                        foreach (ItemRarity rarity in (ItemRarity[])Enum.GetValues(typeof(ItemRarity)))
                        {
                            List<EquipmentItemData> matches = catalog.allItems.FindAll(
                                i => i.requiredClass == cls && i.slot == slot && i.rarity == rarity);
                            Assert.AreEqual(expectedCounts[rarity], matches.Count,
                                $"Sai số lượng độ hiếm cho {cls} / {slot} / {rarity}");

                            int tierMin = int.MaxValue;
                            int tierMax = int.MinValue;
                            foreach (EquipmentItemData m in matches)
                            {
                                int score = m.GetPowerScore();
                                if (score < tierMin) tierMin = score;
                                if (score > tierMax) tierMax = score;
                            }

                            Assert.Greater(tierMin, prevTierMax,
                                $"Bậc {rarity} của {cls}/{slot} ({tierMin}) phải mạnh hơn toàn bộ bậc trước ({prevTierMax})");
                            prevTierMax = tierMax;
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

        [Test]
        public void EquipmentVisualCatalog_All156ItemsHaveSpritesInResources()
        {
            EquipmentCatalog.EquipmentBlueprint[] bps = EquipmentCatalog.GetDefaultBlueprints();
            Assert.AreEqual(156, bps.Length);
            foreach (EquipmentCatalog.EquipmentBlueprint bp in bps)
            {
                string expectedPath = Path.Combine(
                    Application.dataPath,
                    "MedievalRPG/Resources/EquipmentSprites",
                    $"eq_{bp.id}.png");
                Assert.IsTrue(File.Exists(expectedPath), $"Thiếu file sprite cho trang bị: {expectedPath}");
            }
        }

        [Test]
        public void EquipmentVisualCatalog_PaletteRemapCloakAndGlowWorkCorrectly()
        {
            // 1. Knight Armor Remap đổi đúng màu giáp nhưng không chạm màu da/tóc
            var knightRules = EquipmentVisualCatalog.BuildColorReplacements(
                CharacterClassType.Knight,
                "knight_armor_legendary_2",
                "knight_weapon_legendary_2");
            Assert.GreaterOrEqual(knightRules.Count, 6);

            Color32[] knightPixels =
            {
                new Color32(0x44, 0x2B, 0x61, 255), // Giáp tối -> phải đổi
                new Color32(0x8A, 0x0B, 0x41, 255), // Tunic -> phải đổi
                new Color32(0x79, 0x39, 0x0A, 255), // Da/boot -> giữ nguyên
            };
            bool changed = EquipmentVisualCatalog.RemapPixels(knightPixels, knightRules);
            Assert.IsTrue(changed);
            Assert.AreNotEqual(new Color32(0x44, 0x2B, 0x61, 255), knightPixels[0]);
            Assert.AreNotEqual(new Color32(0x8A, 0x0B, 0x41, 255), knightPixels[1]);
            Assert.AreEqual(new Color32(0x79, 0x39, 0x0A, 255), knightPixels[2]);

            // 2. Archer khi mặc cung mới phải ẩn cây cung gỗ cũ (alpha = 0), giữ nguyên màu da
            var archerRules = EquipmentVisualCatalog.BuildColorReplacements(
                CharacterClassType.Archer,
                "archer_armor_legendary",
                "archer_weapon_legendary_2");
            Color32[] archerPixels =
            {
                new Color32(0x61, 0x2A, 0x1D, 255), // Cung cũ -> ẩn
                new Color32(0xF3, 0xD0, 0xB3, 255), // Da mặt -> giữ nguyên
            };
            Assert.IsTrue(EquipmentVisualCatalog.RemapPixels(archerPixels, archerRules));
            Assert.AreEqual(0, archerPixels[0].a);
            Assert.AreEqual(new Color32(0xF3, 0xD0, 0xB3, 255), archerPixels[1]);

            // 3. Tách sạch đồ mặc định không cắt nhầm chi tiết nhân vật (Pure C# RemapAndCleanPixels)
            // Test trên lưới 150x150 cho Knight & Mage và 100x100 cho Archer
            const int kw = 140, kh = 140;
            Color32[] kGrid = new Color32[kw * kh];
            int Idx(int w, int h, int x, int yTop) => (h - 1 - yTop) * w + x;
            kGrid[Idx(kw, kh, 70, 44)] = new Color32(0xFC, 0xFC, 0xFC, 255); // Chỏm mũ sắt -> phải xóa
            kGrid[Idx(kw, kh, 65, 72)] = new Color32(0xFC, 0xFC, 0xFC, 255); // Lưỡi kiếm cũ ngang đùi -> phải xóa
            kGrid[Idx(kw, kh, 77, 72)] = new Color32(0xC3, 0xCB, 0xDB, 255); // Điểm sáng giáp đầu gối phải -> giữ nguyên
            Assert.IsTrue(EquipmentVisualCatalog.RemapAndCleanPixels(
                kGrid, kw, kh, CharacterClassType.Knight, "Knight_Idle_0",
                "knight_armor_legendary", "knight_weapon_legendary", "knight_helm_legendary", "knight_acc_epic"));
            Assert.AreEqual(0, kGrid[Idx(kw, kh, 70, 44)].a);
            Assert.AreEqual(0, kGrid[Idx(kw, kh, 65, 72)].a);
            Assert.AreNotEqual(0, kGrid[Idx(kw, kh, 77, 72)].a);

            const int mw = 231, mh = 190;
            Color32[] mIdle = new Color32[mw * mh];
            mIdle[Idx(mw, mh, 128, 74)] = new Color32(120, 80, 40, 255); // Sách bay cũ (Idle) -> phải xóa
            mIdle[Idx(mw, mh, 88, 60)] = new Color32(0x33, 0xDA, 0xFF, 255); // Đầu trượng cũ (Idle) -> phải xóa
            mIdle[Idx(mw, mh, 108, 60)] = new Color32(0x6E, 0x49, 0xCD, 255); // Chóp mũ trùm cũ -> phải xóa
            Assert.IsTrue(EquipmentVisualCatalog.RemapAndCleanPixels(
                mIdle, mw, mh, CharacterClassType.Mage, "Mage_Idle_0",
                "mage_armor_epic", "mage_weapon_legendary", "mage_helm_epic", "mage_acc_legendary"));
            Assert.AreEqual(0, mIdle[Idx(mw, mh, 128, 74)].a);
            Assert.AreEqual(0, mIdle[Idx(mw, mh, 88, 60)].a);
            Assert.AreEqual(0, mIdle[Idx(mw, mh, 108, 60)].a);

            // Trong Mage_Run không được xóa mặt/râu ở x=125, y=75!
            Color32[] mRun = new Color32[mw * mh];
            mRun[Idx(mw, mh, 125, 75)] = new Color32(187, 143, 130, 255);
            EquipmentVisualCatalog.RemapAndCleanPixels(
                mRun, mw, mh, CharacterClassType.Mage, "Mage_Run_0",
                "", "", "", "mage_acc_legendary");
            Assert.AreEqual(255, mRun[Idx(mw, mh, 125, 75)].a);

            // Archer_Idle: xóa ống tên cũ (x=49, yTop=35) nhưng giữ nguyên vai áo xanh (x=49, yTop=38) & tóc trắng (x=54, yTop=35)
            const int aw = 100, ah = 100;
            Color32[] aIdle = new Color32[aw * ah];
            aIdle[Idx(aw, ah, 49, 35)] = new Color32(230, 230, 230, 255); // Lông tên ống cũ -> phải xóa
            aIdle[Idx(aw, ah, 49, 38)] = new Color32(0x2C, 0x78, 0x24, 255); // Vai áo xanh -> giữ (và đổi màu)
            aIdle[Idx(aw, ah, 54, 35)] = new Color32(240, 240, 240, 255); // Tóc trắng -> giữ nguyên
            Assert.IsTrue(EquipmentVisualCatalog.RemapAndCleanPixels(
                aIdle, aw, ah, CharacterClassType.Archer, "Archer_Idle_0",
                "archer_armor_legendary", "archer_weapon_legendary", "archer_helm_epic_2", "archer_acc_legendary_2"));
            Assert.AreEqual(0, aIdle[Idx(aw, ah, 49, 35)].a);
            Assert.AreEqual(255, aIdle[Idx(aw, ah, 49, 38)].a);
            Assert.AreEqual(new Color32(240, 240, 240, 255), aIdle[Idx(aw, ah, 54, 35)]);

            // 4. Cloak chỉ bật với giáp hợp lệ (từ Rare trở lên cho Knight/Archer, mọi áo choàng cho Mage)
            Assert.IsFalse(EquipmentVisualCatalog.TryGetCloakColors(CharacterClassType.Knight, "knight_armor_common_2", out _, out _));
            Assert.IsTrue(EquipmentVisualCatalog.TryGetCloakColors(CharacterClassType.Knight, "knight_armor_epic_2", out _, out _));
            Assert.IsTrue(EquipmentVisualCatalog.TryGetCloakColors(CharacterClassType.Mage, "mage_armor_common_3", out _, out _));
            Assert.Greater(EquipmentVisualCatalog.GetGlowTintForItem("knight_weapon_legendary_2").a, 0.5f);
        }

        [Test]
        public void CharacterEquipmentVisuals_SyncsWeaponArmorHelmetAccessoryAndHidesForEnemies()
        {
            GameObject go = new GameObject("HeroEquipVisualTest");
            try
            {
                go.AddComponent<SpriteRenderer>();
                go.AddComponent<PixelCharacterAnimator>();
                Unit u = go.AddComponent<Unit>();
                u.isEnemy = false;
                u.characterClass = CharacterClassType.Knight;

                CharacterEquipmentVisuals vis = go.AddComponent<CharacterEquipmentVisuals>();
                vis.ApplyEquipmentState(
                    "knight_weapon_epic_2", 3,
                    "knight_armor_epic_2",
                    "knight_acc_rare_2", 1,
                    "knight_helm_legendary", 2);

                Assert.AreEqual("knight_weapon_epic_2", vis.EquippedWeaponId);
                Assert.AreEqual("knight_helm_legendary", vis.EquippedHelmetId);
                Assert.AreEqual("knight_armor_epic_2", vis.EquippedArmorId);
                Assert.AreEqual("knight_acc_rare_2", vis.EquippedAccessoryId);
                Assert.IsNotNull(go.transform.Find("Equip_Weapon"));
                Assert.IsNotNull(go.transform.Find("Equip_Helmet"));
                Assert.IsNotNull(go.transform.Find("Equip_Accessory"));
                Assert.IsNotNull(go.transform.Find("Equip_Cloak"));
                Assert.IsTrue(go.transform.Find("Equip_Cloak").gameObject.activeSelf);
                Assert.IsTrue(go.transform.Find("Equip_GlowAura").gameObject.activeSelf);

                // Nếu là quái vật (ShadowMage / SkeletonArcher dùng chung prefab), ClearVisuals phải tắt toàn bộ 4 slot
                u.isEnemy = true;
                vis.RefreshFromEquipmentManager();
                Assert.AreEqual("", vis.EquippedWeaponId);
                Assert.AreEqual("", vis.EquippedHelmetId);
                Assert.IsFalse(go.transform.Find("Equip_Cloak").gameObject.activeSelf);
                Assert.IsFalse(go.transform.Find("Equip_GlowAura").gameObject.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterEquipmentVisuals_PreservesFullCanvasPivotOnTightSpritesAndScalesVerticalItemsCorrectly()
        {
            // 1. Scale trang bị mảnh dọc (ví dụ trượng 12x30 hay cung 15x29) phải chia cho cạnh lớn hơn (maxDim), không phóng đại sai
            Texture2D verticalTex = new Texture2D(12, 30, TextureFormat.RGBA32, false);
            try
            {
                Sprite tallStaff = Sprite.Create(verticalTex, new Rect(0, 0, 12, 30), new Vector2(0.5f, 0.5f), 100f);
                float scale = CharacterEquipmentVisuals.ComputeNormalizedEquipmentScale(tallStaff, 0.30f);
                Assert.AreEqual(0.30f * (100f / 30f), scale, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(verticalTex);
            }

            // 2. Khi mặc đồ lên frame Archer (100x100, Tight-cropped), sprite sinh ra phải giữ nguyên 100x100 & pivot (50, 30)
            Sprite archerIdle0 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/MedievalRPG/Characters/Archer/Frames/Idle/Archer_Idle_0.png");
            Assert.IsNotNull(archerIdle0, "Thiếu sprite Archer_Idle_0.png");

            GameObject go = new GameObject("ArcherTightCropTest");
            try
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = archerIdle0;
                PixelCharacterAnimator anim = go.AddComponent<PixelCharacterAnimator>();
                anim.idleFrames.Add(archerIdle0);
                Unit u = go.AddComponent<Unit>();
                u.isEnemy = false;
                u.characterClass = CharacterClassType.Archer;

                CharacterEquipmentVisuals vis = go.AddComponent<CharacterEquipmentVisuals>();
                vis.ApplyEquipmentState(
                    "archer_weapon_common", 0,
                    "archer_armor_common_3",
                    "archer_acc_common", 0,
                    "archer_helm_common", 0);
                anim.RefreshCurrentFrameSprite();

                Assert.IsNotNull(sr.sprite);
                Assert.AreEqual(100f, sr.sprite.rect.width, 0.01f);
                Assert.AreEqual(100f, sr.sprite.rect.height, 0.01f);
                Assert.AreEqual(50f, sr.sprite.pivot.x, 0.5f);
                Assert.AreEqual(30f, sr.sprite.pivot.y, 0.5f);

                Transform wpnChild = go.transform.Find("Equip_Weapon");
                Assert.IsNotNull(wpnChild);
                SpriteRenderer wpnSr = wpnChild.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(wpnSr);
                Assert.IsNotNull(wpnSr.sprite);
                float expectedScale = CharacterEquipmentVisuals.ComputeNormalizedEquipmentScale(wpnSr.sprite, 31.5f / 55f);
                Assert.AreEqual(expectedScale, wpnChild.localScale.x, 0.001f);

                // Mũ trùm Archer phải hạ đúng tầm đầu (y ~ 0.59, không bay cao 0.71) và đã đục thoáng vùng mặt
                Transform helmChild = go.transform.Find("Equip_Helmet");
                Assert.IsNotNull(helmChild);
                Assert.Less(helmChild.localPosition.y, 0.65f);
                Assert.Greater(helmChild.localPosition.y, 0.52f);
                SpriteRenderer helmSr = helmChild.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(helmSr.sprite);
                Assert.AreNotEqual(EquipmentVisualCatalog.GetSpriteForItem("archer_helm_common"), helmSr.sprite);

                // Ống tên Archer nằm sát vai (y ~ 0.54) với góc nghiêng thuận theo lưng
                Transform accChild = go.transform.Find("Equip_Accessory");
                Assert.IsNotNull(accChild);
                Assert.Less(accChild.localPosition.y, 0.57f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // 3. Bộ font UiSans rõ nét đầy đủ 4 kiểu chữ tiếng Việt phải tồn tại trong Resources/MedievalFonts
            string fontDir = Path.Combine(Application.dataPath, "MedievalRPG/Resources/MedievalFonts");
            Assert.IsTrue(File.Exists(Path.Combine(fontDir, "UiSans-Regular.ttf")));
            Assert.IsTrue(File.Exists(Path.Combine(fontDir, "UiSans-Bold.ttf")));
            Assert.IsTrue(File.Exists(Path.Combine(fontDir, "UiSans-Italic.ttf")));
            Assert.IsTrue(File.Exists(Path.Combine(fontDir, "UiSans-BoldItalic.ttf")));
            StringAssert.Contains("ĐđƠơƯư", MedievalRPG.UI.RuntimeUIThemePatcher.VietnameseGlyphs);
        }

        [Test]
        public void EquipmentVisualCatalog_PerFrameAnchorsAndCleanMaskWorkAcrossStates()
        {
            // 1. Mọi frame của 3 class đều có anchor hợp lệ và Knight giữ nguyên giáp đầu gối trái khi xóa kiếm
            Assert.IsTrue(EquipmentVisualCatalog.TryGetFrameAnchor(CharacterClassType.Knight, "Knight_Idle_0", EquipmentVisualCatalog.VisualAnimState.Idle, 0, out _));
            Assert.IsTrue(EquipmentVisualCatalog.TryGetFrameAnchor(CharacterClassType.Knight, "Knight_Attack_4", EquipmentVisualCatalog.VisualAnimState.Attack, 4, out var kAtk4));
            Assert.IsFalse(kAtk4.wpnVisible, "Knight_Attack_4 hiển thị vệt chém lớn nên phải ẩn vũ khí con");
            Assert.IsTrue(EquipmentVisualCatalog.TryGetFrameAnchor(CharacterClassType.Mage, "Mage_Run_3", EquipmentVisualCatalog.VisualAnimState.Run, 3, out _));
            Assert.IsTrue(EquipmentVisualCatalog.TryGetFrameAnchor(CharacterClassType.Archer, "Archer_Attack_3", EquipmentVisualCatalog.VisualAnimState.Attack, 3, out _));

            const int kw = 140, kh = 140;
            Color32[] kGrid = new Color32[kw * kh];
            int Idx(int w, int h, int x, int yTop) => (h - 1 - yTop) * w + x;
            kGrid[Idx(kw, kh, 69, 49)] = new Color32(0x81, 0x70, 0x9A, 255); // Phần thân mũ sắt cũ -> phải xóa hết khi đội Thánh Quan
            kGrid[Idx(kw, kh, 60, 74)] = new Color32(0xC3, 0xCB, 0xDB, 255); // Giáp đầu gối trái -> phải giữ nguyên!
            kGrid[Idx(kw, kh, 65, 72)] = new Color32(0xC3, 0xCB, 0xDB, 255); // Mép lưỡi kiếm cũ -> phải xóa!

            Assert.IsTrue(EquipmentVisualCatalog.RemapAndCleanPixels(
                kGrid, kw, kh, CharacterClassType.Knight, "Knight_Idle_0",
                "", "knight_weapon_common", "knight_helm_legendary_2", ""));
            Assert.AreEqual(0, kGrid[Idx(kw, kh, 69, 49)].a);
            Assert.AreEqual(255, kGrid[Idx(kw, kh, 60, 74)].a);
            Assert.AreEqual(0, kGrid[Idx(kw, kh, 65, 72)].a);

            // 2. Mage đội vương miện: chóp nhọn trên đầu bị xóa, phần gáy dưới thành tóc bạc
            const int mw = 231, mh = 190;
            Color32[] mGrid = new Color32[mw * mh];
            mGrid[Idx(mw, mh, 112, 60)] = new Color32(0x6E, 0x49, 0xCD, 255); // Chóp trên -> xóa
            mGrid[Idx(mw, mh, 108, 70)] = new Color32(0x6E, 0x49, 0xCD, 255); // Tóc gáy sau -> đổi sang tóc bạc
            Assert.IsTrue(EquipmentVisualCatalog.RemapAndCleanPixels(
                mGrid, mw, mh, CharacterClassType.Mage, "Mage_Idle_0",
                "", "", "mage_helm_epic_3", ""));
            Assert.AreEqual(0, mGrid[Idx(mw, mh, 112, 60)].a);
            Assert.AreEqual(255, mGrid[Idx(mw, mh, 108, 70)].a);
            Color32 silverHair = mGrid[Idx(mw, mh, 108, 70)];
            Assert.GreaterOrEqual(silverHair.r, 135);
            Assert.LessOrEqual(Mathf.Abs(silverHair.r - silverHair.b), 12);
        }

        [Test]
        public void HeroPrefabs_HaveComparableVisualHeightsAndConsistentVariantScales()
        {
            float MeasurePrefabWorldHeight(string prefabPath, out float scaleY)
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.IsNotNull(prefab, $"Thiếu prefab: {prefabPath}");
                scaleY = Mathf.Abs(prefab.transform.localScale.y);
                PixelCharacterAnimator anim = prefab.GetComponent<PixelCharacterAnimator>();
                Assert.IsNotNull(anim, $"Prefab thiếu PixelCharacterAnimator: {prefabPath}");
                Assert.IsNotNull(anim.idleFrames);
                Assert.Greater(anim.idleFrames.Count, 0);
                Sprite idle0 = anim.idleFrames[0];
                Assert.IsNotNull(idle0);
                return idle0.bounds.size.y * scaleY;
            }

            float knightH = MeasurePrefabWorldHeight("Assets/MedievalRPG/Prefabs/Knight.prefab", out float knightScale);
            float mageH = MeasurePrefabWorldHeight("Assets/MedievalRPG/Prefabs/Mage.prefab", out float mageScale);
            float archerH = MeasurePrefabWorldHeight("Assets/MedievalRPG/Prefabs/Archer.prefab", out float archerScale);
            float bossH = MeasurePrefabWorldHeight("Assets/MedievalRPG/Prefabs/DarkKnightBoss.prefab", out _);
            MeasurePrefabWorldHeight("Assets/MedievalRPG/Prefabs/SkeletonArcher.prefab", out float skelArcherScale);
            MeasurePrefabWorldHeight("Assets/MedievalRPG/Prefabs/ShadowMage.prefab", out float shadowMageScale);

            float minHeroH = Mathf.Min(knightH, Mathf.Min(mageH, archerH));
            float maxHeroH = Mathf.Max(knightH, Mathf.Max(mageH, archerH));

            Assert.LessOrEqual(maxHeroH - minHeroH, 0.10f,
                $"Chiều cao hiển thị 3 anh hùng phải tương đồng nhau (Knight={knightH:F3}, Mage={mageH:F3}, Archer={archerH:F3})");
            Assert.Greater(bossH, knightH * 1.15f,
                $"DarkKnightBoss ({bossH:F3}) phải lớn hơn Knight thường ({knightH:F3})");
            Assert.AreEqual(archerScale, skelArcherScale, 0.001f);
            Assert.AreEqual(mageScale, shadowMageScale, 0.001f);
            Assert.Greater(knightScale, 1.0f);
        }
    }
}
