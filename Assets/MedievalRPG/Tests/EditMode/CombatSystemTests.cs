using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Quiz;
using MedievalRPG.UI;
using MedievalRPG.Visuals;

namespace MedievalRPG.Tests.EditMode
{
    public class CombatSystemTests
    {
        [Test]
        public void CombatCalculator_PhysicalVsMagicalVsTrueDamage()
        {
            // Physical: 100 * (100 / (100 + 25)) = 80
            int phys = CombatCalculator.CalculateMitigatedDamage(100, DamageType.Physical, 25, 100, false);
            Assert.AreEqual(80, phys);

            // Magical: 100 * (100 / (100 + 100)) = 50
            int mag = CombatCalculator.CalculateMitigatedDamage(100, DamageType.Magical, 25, 100, false);
            Assert.AreEqual(50, mag);

            // Defending halves physical & magical
            int physDefend = CombatCalculator.CalculateMitigatedDamage(100, DamageType.Physical, 25, 100, true);
            Assert.AreEqual(40, physDefend);

            // TrueDamage ignores defense & defend
            int trueDmg = CombatCalculator.CalculateMitigatedDamage(37, DamageType.TrueDamage, 200, 200, true);
            Assert.AreEqual(37, trueDmg);
        }

        [Test]
        public void CombatCalculator_QuizStreakBoostsDamageAtThree()
        {
            int normal = CombatCalculator.CalculateSkillRawDamage(20, 1.0f, 0, 1.5f, false, 8, 2, 0f);
            int streak = CombatCalculator.CalculateSkillRawDamage(20, 1.0f, 0, 1.5f, false, 8, 3, 0f);

            Assert.AreEqual(28, normal);
            Assert.AreEqual(34, streak); // round(28 * 1.2) = 34
        }

        [Test]
        public void Unit_CooldownAndStatusEffectsWorkCorrectly()
        {
            GameObject go = new GameObject("TestUnit");
            try
            {
                Unit u = go.AddComponent<Unit>();
                u.maxHP = 100;
                u.currentHP = 100;
                u.maxMP = 50;
                u.currentMP = 50;
                u.defense = 10;

                SkillData skill = ScriptableObject.CreateInstance<SkillData>();
                skill.cooldownTurns = 1;

                u.StartSkillCooldown(skill);
                Assert.IsTrue(u.IsSkillOnCooldown(skill));
                Assert.AreEqual(2, u.GetSkillCooldown(skill));

                // Next turn start ticks 2 -> 1 (still locked for 1 full turn)
                u.OnTurnStart(out _, out _, out _);
                Assert.IsTrue(u.IsSkillOnCooldown(skill));
                Assert.AreEqual(1, u.GetSkillCooldown(skill));

                // Turn after ticks 1 -> 0 (ready!)
                u.OnTurnStart(out _, out _, out _);
                Assert.IsFalse(u.IsSkillOnCooldown(skill));

                // Burn + Stun test
                u.ApplyStatus(StatusEffectType.Burn, 1, 12);
                u.ApplyStatus(StatusEffectType.Stun, 1, 0);

                u.OnTurnStart(out int burnDmg, out bool wasStunned, out bool diedFromBurn);
                Assert.AreEqual(12, burnDmg);
                Assert.IsTrue(wasStunned);
                Assert.IsFalse(diedFromBurn);
                Assert.AreEqual(88, u.currentHP);
                Assert.IsFalse(u.IsStunned);

                Object.DestroyImmediate(skill);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Unit_BossPhase2AndShadowShieldBreak()
        {
            GameObject go = new GameObject("BossTest");
            try
            {
                Unit boss = go.AddComponent<Unit>();
                boss.isBoss = true;
                boss.maxHP = 200;
                boss.currentHP = 90; // <= 50%

                bool triggered = boss.TryTriggerPhase2();
                Assert.IsTrue(triggered);
                Assert.AreEqual(2, boss.ShadowShieldStacks);

                Assert.IsTrue(boss.StripShadowShield(out bool broken1));
                Assert.IsFalse(broken1);
                Assert.AreEqual(1, boss.ShadowShieldStacks);

                Assert.IsTrue(boss.StripShadowShield(out bool broken2));
                Assert.IsTrue(broken2);
                Assert.AreEqual(0, boss.ShadowShieldStacks);
                Assert.IsTrue(boss.IsStunned);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BattleInventory_TracksAndConsumesItems()
        {
            BattleInventory inv = new BattleInventory(3, 2, 1);
            Assert.AreEqual(6, inv.TotalItems);
            Assert.IsTrue(inv.TryConsume(ConsumableItemType.PhoenixFeather));
            Assert.AreEqual(0, inv.GetCount(ConsumableItemType.PhoenixFeather));
            Assert.IsFalse(inv.TryConsume(ConsumableItemType.PhoenixFeather));
            Assert.AreEqual(5, inv.TotalItems);
        }

        [Test]
        public void QuizManager_DoesNotRepeatUntilBankExhausted()
        {
            GameObject go = new GameObject("QuizTest");
            try
            {
                QuizManager qm = go.AddComponent<QuizManager>();
                qm.LoadDefaultQuestions();
                int count = qm.questionBank.Count;
                HashSet<string> seen = new HashSet<string>();

                for (int i = 0; i < count; i++)
                {
                    QuizQuestion q = qm.GetRandomQuestion();
                    Assert.IsTrue(seen.Add(q.questionText), $"Repeated question early at draw {i}");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CombatCalculator_CalculateStageStars_RequiresQuizAnsweredForKnowledgeStar()
        {
            // Tắt Quiz (0 câu trả lời): tối đa 2 sao dù cả đội còn sống
            Assert.AreEqual(2, CombatCalculator.CalculateStageStars(0, 0, 3, 3));
            Assert.AreEqual(1, CombatCalculator.CalculateStageStars(0, 0, 2, 3));

            // Bật Quiz: >= 75% và cả đội sống -> 3 sao
            Assert.AreEqual(3, CombatCalculator.CalculateStageStars(4, 3, 3, 3));
            Assert.AreEqual(2, CombatCalculator.CalculateStageStars(4, 2, 3, 3));
            Assert.AreEqual(2, CombatCalculator.CalculateStageStars(4, 4, 2, 3));
            Assert.AreEqual(1, CombatCalculator.CalculateStageStars(4, 0, 1, 3));
        }

        [Test]
        public void TimeScaleController_PauseTakesPrecedenceOverHitstopAndSpeed()
        {
            try
            {
                TimeScaleController.Reset();
                TimeScaleController.SetBaseSpeed(1.5f);
                Assert.AreEqual(1.5f, TimeScaleController.EffectiveTimeScale, 0.001f);
                Assert.AreEqual(1.5f, Time.timeScale, 0.001f);

                TimeScaleController.SetHitstop(true, 0.08f);
                Assert.IsTrue(TimeScaleController.IsHitstopActive);
                Assert.AreEqual(0.08f, TimeScaleController.EffectiveTimeScale, 0.001f);

                // Bấm Pause đúng lúc hitstop đang chạy
                TimeScaleController.SetPaused(true);
                Assert.IsTrue(TimeScaleController.IsPaused);
                Assert.AreEqual(0f, TimeScaleController.EffectiveTimeScale, 0.001f);
                Assert.AreEqual(0f, Time.timeScale, 0.001f);

                // Hitstop kết thúc sau đó không được bỏ Pause
                TimeScaleController.SetHitstop(false);
                Assert.IsTrue(TimeScaleController.IsPaused);
                Assert.AreEqual(0f, TimeScaleController.EffectiveTimeScale, 0.001f);
                Assert.AreEqual(0f, Time.timeScale, 0.001f);

                // Kích hoạt hitstop trong khi đang pause cũng bị chặn
                TimeScaleController.SetHitstop(true, 0.08f);
                Assert.AreEqual(0f, TimeScaleController.EffectiveTimeScale, 0.001f);

                // Bỏ Pause -> khôi phục đúng tốc độ 1.5x
                TimeScaleController.SetPaused(false);
                Assert.IsFalse(TimeScaleController.IsPaused);
                Assert.AreEqual(1.5f, TimeScaleController.EffectiveTimeScale, 0.001f);
                Assert.AreEqual(1.5f, Time.timeScale, 0.001f);
            }
            finally
            {
                TimeScaleController.Reset();
            }
        }

        [Test]
        public void QuizQuestion_CreateShuffledCopy_PreservesCorrectAnswerAndDoesNotMutateSource()
        {
            QuizQuestion source = new QuizQuestion(
                "B1 • Grammar",
                "Test question",
                new[] { "RightAnswer", "Wrong1", "Wrong2", "Wrong3" },
                0,
                "Explanation",
                7);

            HashSet<int> observedIndices = new HashSet<int>();
            for (int seed = 1; seed <= 30; seed++)
            {
                QuizQuestion copy = source.CreateShuffledCopy(new System.Random(seed));
                Assert.AreEqual(0, source.correctAnswerIndex);
                Assert.AreEqual("RightAnswer", source.answers[0]);
                Assert.AreEqual(4, copy.answers.Length);
                Assert.AreEqual("RightAnswer", copy.answers[copy.correctAnswerIndex]);
                Assert.IsTrue(copy.IsCorrect(copy.correctAnswerIndex));
                CollectionAssert.AreEquivalent(source.answers, copy.answers);
                observedIndices.Add(copy.correctAnswerIndex);
            }

            Assert.GreaterOrEqual(observedIndices.Count, 3, "Xáo trộn đáp án phải phân bố qua nhiều vị trí khác nhau");
        }

        [Test]
        public void QuizManager_QuestionBankHasBalancedAnswerPositionsAndDoesNotMutateBank()
        {
            GameObject go = new GameObject("QuizBalanceTest");
            try
            {
                QuizManager qm = go.AddComponent<QuizManager>();
                qm.LoadDefaultQuestions();
                Assert.AreEqual(120, qm.questionBank.Count);

                int[] bankCounts = new int[4];
                foreach (QuizQuestion q in qm.questionBank)
                {
                    Assert.IsNotNull(q.answers);
                    Assert.AreEqual(4, q.answers.Length);
                    Assert.GreaterOrEqual(q.correctAnswerIndex, 0);
                    Assert.Less(q.correctAnswerIndex, 4);
                    bankCounts[q.correctAnswerIndex]++;
                }

                for (int idx = 0; idx < 4; idx++)
                {
                    Assert.AreEqual(30, bankCounts[idx], $"Vị trí đáp án {idx} trong JSON gốc phải cân bằng 30 câu");
                }

                // Lấy snapshot của câu đầu tiên rồi gọi GetRandomQuestion 60 lần, đảm bảo bank gốc không bị sửa đổi
                string firstTextBefore = qm.questionBank[0].answers[qm.questionBank[0].correctAnswerIndex];
                int[] runtimeCounts = new int[4];
                for (int i = 0; i < 60; i++)
                {
                    QuizQuestion drawn = qm.GetRandomQuestion(QuizDifficultyFilter.AllB1B2);
                    runtimeCounts[drawn.correctAnswerIndex]++;
                }

                Assert.AreEqual(firstTextBefore, qm.questionBank[0].answers[qm.questionBank[0].correctAnswerIndex]);
                for (int idx = 0; idx < 4; idx++)
                {
                    Assert.Greater(runtimeCounts[idx], 0, $"Vị trí {idx} phải xuất hiện trong các lần rút ngẫu nhiên");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BattleUIManager_ResolvesVietnameseEnemyPortraitsAndEnemyCardFormatsMagicAttack()
        {
            GameObject go = new GameObject("PortraitTest");
            try
            {
                Unit u = go.AddComponent<Unit>();
                u.isEnemy = true;

                u.unitName = "Xương Cung Thủ";
                u.characterClass = CharacterClassType.Archer;
                Assert.AreEqual(BattleUIManager.UnitPortraitKind.Skeleton, BattleUIManager.ResolveUnitPortraitKind(u));

                u.unitName = "Tinh Anh Xương Cung Thủ";
                Assert.AreEqual(BattleUIManager.UnitPortraitKind.Skeleton, BattleUIManager.ResolveUnitPortraitKind(u));

                u.unitName = "Pháp Sư Hắc Ám";
                u.characterClass = CharacterClassType.Mage;
                Assert.AreEqual(BattleUIManager.UnitPortraitKind.ShadowMage, BattleUIManager.ResolveUnitPortraitKind(u));

                u.unitName = "Tinh Anh Pháp Sư Hắc Ám";
                Assert.AreEqual(BattleUIManager.UnitPortraitKind.ShadowMage, BattleUIManager.ResolveUnitPortraitKind(u));

                u.isBoss = true;
                u.unitName = "Dark Knight Boss";
                Assert.AreEqual(BattleUIManager.UnitPortraitKind.Boss, BattleUIManager.ResolveUnitPortraitKind(u));

                Assert.AreEqual("MAG 28", EnemyStatusCardUI.FormatAttackStatLabel(9, 28));
                Assert.AreEqual("ATK 38/MAG 21", EnemyStatusCardUI.FormatAttackStatLabel(38, 21));
                Assert.AreEqual("ATK 17", EnemyStatusCardUI.FormatAttackStatLabel(17, 2));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Assets_BasicHeroAttacksAreFreeManaAndEnemyPrefabsMatchClassData()
        {
            string[] heroClasses =
            {
                "Assets/MedievalRPG/ScriptableObjects/Classes/KnightClass.asset",
                "Assets/MedievalRPG/ScriptableObjects/Classes/MageClass.asset",
                "Assets/MedievalRPG/ScriptableObjects/Classes/ArcherClass.asset"
            };
            foreach (string clsPath in heroClasses)
            {
                CharacterClassData cls = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterClassData>(clsPath);
                Assert.IsNotNull(cls, $"Thiếu class asset: {clsPath}");
                Assert.IsNotNull(cls.skills);
                Assert.Greater(cls.skills.Count, 0);
                Assert.AreEqual(0, cls.skills[0].manaCost, $"Đánh thường của {cls.className} phải miễn phí MP");
            }

            string[] enemyPrefabs =
            {
                "Assets/MedievalRPG/Prefabs/GoblinRaider.prefab",
                "Assets/MedievalRPG/Prefabs/Skeleton.prefab",
                "Assets/MedievalRPG/Prefabs/SkeletonArcher.prefab",
                "Assets/MedievalRPG/Prefabs/ShadowMage.prefab",
                "Assets/MedievalRPG/Prefabs/DarkKnightBoss.prefab"
            };
            foreach (string prefabPath in enemyPrefabs)
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.IsNotNull(prefab, $"Thiếu prefab quái: {prefabPath}");
                Unit u = prefab.GetComponent<Unit>();
                Assert.IsNotNull(u);
                Assert.IsNotNull(u.classData, $"Prefab {prefabPath} thiếu classData");
                Assert.AreEqual(u.classData.maxHealth, u.maxHP, $"maxHP lệch classData trên {prefabPath}");
                Assert.AreEqual(u.classData.physicalAttack, u.damage, $"damage lệch classData trên {prefabPath}");
                Assert.AreEqual(u.classData.magicAttack, u.magicDamage, $"magicDamage lệch classData trên {prefabPath}");
            }
        }

        [Test]
        public void PlayerCampProgress_TracksTopicStatsAndMistakeWrongCounts()
        {
            PlayerCampProgressManager camp = PlayerCampProgressManager.Instance;
            try
            {
                camp.InitializeForTests();

                Assert.AreEqual("Tenses", PlayerCampProgressManager.NormalizeTopicKey("B1 • Tenses"));
                Assert.AreEqual("Modals", PlayerCampProgressManager.NormalizeTopicKey("B2 - Modal Verbs"));
                Assert.AreEqual("General", PlayerCampProgressManager.NormalizeTopicKey(""));

                camp.RecordQuizAttemptStats("B1 • Tenses", false, false);
                camp.RecordQuizAttemptStats("B1 • Tenses", false, false);
                camp.RecordQuizAttemptStats("B1 • Tenses", true, false);
                camp.RecordQuizAttemptStats("B2 • Passive Voice", true, false);
                camp.RecordQuizAttemptStats("B2 • Passive Voice", true, false);

                camp.GetOverallQuizAccuracy(out int totalAnswered, out int totalCorrect);
                Assert.AreEqual(5, totalAnswered);
                Assert.AreEqual(3, totalCorrect);

                List<TopicQuizStatEntry> sorted = camp.GetTopicStatsSortedByWeakness();
                Assert.AreEqual(2, sorted.Count);
                Assert.AreEqual("Tenses", sorted[0].topicKey);
                Assert.AreEqual("Passive Voice", sorted[1].topicKey);

                string summary = camp.GetWeakestTopicsSummary(2);
                StringAssert.Contains("Tenses 33% (1/3)", summary);

                const string sampleQ = "She ___ to school yesterday.";
                Assert.IsFalse(camp.HasQuizMistake(sampleQ));
                Assert.AreEqual(0, camp.GetMistakeWrongCount(sampleQ));

                camp.RecordQuizMistake(sampleQ);
                camp.RecordQuizMistake(sampleQ);
                Assert.IsTrue(camp.HasQuizMistake(sampleQ));
                Assert.AreEqual(2, camp.GetMistakeWrongCount(sampleQ));

                Assert.IsTrue(camp.ResolveQuizMistake(sampleQ));
                Assert.IsFalse(camp.HasQuizMistake(sampleQ));
                Assert.AreEqual(0, camp.GetMistakeWrongCount(sampleQ));
            }
            finally
            {
                camp.InitializeForTests();
            }
        }

        [Test]
        public void QuizManager_SpacedRepetitionPrioritizesHigherWrongCountAndPreservesNonRepeat()
        {
            List<QuizQuestion> bank = new List<QuizQuestion>
            {
                new QuizQuestion("B1 • Nouns", "Q0?", new[] { "A", "B", "C", "D" }, 0),
                new QuizQuestion("B1 • Tenses", "Q1?", new[] { "A", "B", "C", "D" }, 1),
                new QuizQuestion("B2 • Modals", "Q2?", new[] { "A", "B", "C", "D" }, 2)
            };
            List<int> available = new List<int> { 0, 1, 2 };
            List<string> mistakes = new List<string> { "Q1?", "Q2?" };
            System.Func<string, int> wrongCountFn = q => q == "Q2?" ? 3 : (q == "Q1?" ? 1 : 0);

            int pickedSpaced = QuizManager.SelectQuestionIndexForDraw(bank, available, mistakes, wrongCountFn, 0.25f, 0.10f, 0);
            Assert.AreEqual(2, pickedSpaced, "Khi roll trúng spaced repetition phải ưu tiên câu có wrongCount cao nhất");

            int pickedNormal = QuizManager.SelectQuestionIndexForDraw(bank, available, mistakes, wrongCountFn, 0.25f, 0.70f, 0);
            Assert.AreEqual(0, pickedNormal, "Khi roll trượt spaced repetition phải dùng fallbackChoice trong available");

            GameObject qmGo = new GameObject("QuizManagerSpacedTest");
            PlayerCampProgressManager camp = PlayerCampProgressManager.Instance;
            try
            {
                camp.InitializeForTests();
                QuizManager qm = qmGo.AddComponent<QuizManager>();
                qm.LoadDefaultQuestions();

                for (int i = 0; i < 5 && i < qm.questionBank.Count; i++)
                {
                    camp.RecordQuizMistake(qm.questionBank[i].questionText);
                }

                HashSet<string> seen = new HashSet<string>();
                for (int i = 0; i < qm.questionBank.Count; i++)
                {
                    QuizQuestion q = qm.GetRandomQuestion(QuizDifficultyFilter.AllB1B2);
                    Assert.IsNotNull(q);
                    Assert.IsTrue(seen.Add(q.questionText), $"Câu '{q.questionText}' không được lặp trong cùng 1 chu kỳ dù có Sổ tay lỗi sai");
                }
            }
            finally
            {
                Object.DestroyImmediate(qmGo);
                camp.InitializeForTests();
            }
        }

        [Test]
        public void QuizModalUI_PickTwoWrongOptionIndicesReturnsTwoDistinctWrongAnswers()
        {
            System.Random rng = new System.Random(42);
            for (int correct = 0; correct < 4; correct++)
            {
                int[] eliminated = QuizModalUI.PickTwoWrongOptionIndices(correct, rng);
                Assert.AreEqual(2, eliminated.Length);
                Assert.AreNotEqual(eliminated[0], eliminated[1]);
                for (int i = 0; i < eliminated.Length; i++)
                {
                    Assert.GreaterOrEqual(eliminated[i], 0);
                    Assert.Less(eliminated[i], 4);
                    Assert.AreNotEqual(correct, eliminated[i]);
                }
            }
        }

        [Test]
        public void EnemyAI_SmartTargetingAndShadowMageDarkAegisConfigured()
        {
            GameObject kGo = new GameObject("HeroKnight");
            GameObject mGo = new GameObject("HeroMage");
            GameObject aGo = new GameObject("HeroArcher");
            GameObject skelGo = new GameObject("EnemySkeletonArcher");
            GameObject bossGo = new GameObject("EnemyDarkKnightBoss");
            GameObject shadowGo = new GameObject("EnemyShadowMage");
            try
            {
                Unit knight = kGo.AddComponent<Unit>();
                knight.maxHP = 100; knight.currentHP = 90;

                Unit mage = mGo.AddComponent<Unit>();
                mage.maxHP = 100; mage.currentHP = 30; // 30% HP

                Unit heroArcher = aGo.AddComponent<Unit>();
                heroArcher.maxHP = 100; heroArcher.currentHP = 50; // 50% HP

                List<Unit> heroes = new List<Unit> { knight, mage, heroArcher };

                Unit skelArcher = skelGo.AddComponent<Unit>();
                skelArcher.isEnemy = true;
                skelArcher.characterClass = CharacterClassType.Archer;
                skelArcher.damage = 24;
                skelArcher.maxHP = 125; skelArcher.currentHP = 125;

                // Xương Cung Thủ tỉa anh hùng HP% thấp nhất (Mage 30%) bất kể randomPickIndex = 0 (Knight)
                Assert.AreEqual(mage, BattleSystem.SelectSmartEnemyTarget(skelArcher, heroes, 0));

                // Boss ưu tiên dứt điểm anh hùng <= 35% HP (Mage 30%) bất kể randomPickIndex = 2
                Unit boss = bossGo.AddComponent<Unit>();
                boss.isEnemy = true;
                boss.isBoss = true;
                Assert.AreEqual(mage, BattleSystem.SelectSmartEnemyTarget(boss, heroes, 2));

                // Khi Kỵ Sĩ bật Khiêu Khích, cả Cung Thủ và Boss đều phải đánh vào Kỵ Sĩ
                knight.ApplyStatus(StatusEffectType.Taunt, 2, 0);
                Assert.AreEqual(knight, BattleSystem.SelectSmartEnemyTarget(skelArcher, heroes, 1));
                Assert.AreEqual(knight, BattleSystem.SelectSmartEnemyTarget(boss, heroes, 1));

                // Kiểm tra Intent BẮN TỈA trên Xương Cung Thủ
                skelArcher.PlanNextIntent(1, false);
                Assert.AreEqual(EnemyIntentType.SingleAttack, skelArcher.PlannedIntent);
                StringAssert.StartsWith("BẮN TỈA ~", skelArcher.GetIntentDisplayText());

                // Kiểm tra ShadowMage_DarkAegis.asset và liên kết trên Class + Prefab
                SkillData darkAegis = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(
                    "Assets/MedievalRPG/ScriptableObjects/Skills/ShadowMage_DarkAegis.asset");
                Assert.IsNotNull(darkAegis, "Thiếu ShadowMage_DarkAegis.asset");
                Assert.IsTrue(darkAegis.IsHealSkill);
                Assert.AreEqual(14, darkAegis.manaCost);
                Assert.AreEqual(10, darkAegis.defenseBonus);

                CharacterClassData shadowClass = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterClassData>(
                    "Assets/MedievalRPG/ScriptableObjects/Classes/ShadowMageClass.asset");
                Assert.IsNotNull(shadowClass);
                Assert.IsTrue(shadowClass.skills.Contains(darkAegis), "ShadowMageClass.asset phải chứa ShadowMage_DarkAegis");

                GameObject shadowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/MedievalRPG/Prefabs/ShadowMage.prefab");
                Assert.IsNotNull(shadowPrefab);
                Unit prefabUnit = shadowPrefab.GetComponent<Unit>();
                Assert.IsNotNull(prefabUnit);
                Assert.IsTrue(prefabUnit.skills.Contains(darkAegis), "ShadowMage.prefab phải chứa ShadowMage_DarkAegis");

                Unit shadowUnit = shadowGo.AddComponent<Unit>();
                shadowUnit.InitializeFromClass(shadowClass);
                shadowUnit.isEnemy = true;

                // Khi đồng minh khỏe mạnh (hasWoundedEnemyAlly = false), Pháp Sư không chọn Dark Aegis
                shadowUnit.PlanNextIntent(1, false);
                Assert.AreNotEqual(darkAegis, shadowUnit.PlannedSkill);

                // Khi có đồng minh bị thương ở round 1, Pháp Sư ưu tiên Dark Aegis với Intent HỒI PHỤC / HỘ THÂN
                shadowUnit.PlanNextIntent(1, true);
                Assert.AreEqual(darkAegis, shadowUnit.PlannedSkill);
                Assert.AreEqual(EnemyIntentType.Buff, shadowUnit.PlannedIntent);
                Assert.AreEqual("HỒI PHỤC / HỘ THÂN", shadowUnit.GetIntentDisplayText());
            }
            finally
            {
                Object.DestroyImmediate(kGo);
                Object.DestroyImmediate(mGo);
                Object.DestroyImmediate(aGo);
                Object.DestroyImmediate(skelGo);
                Object.DestroyImmediate(bossGo);
                Object.DestroyImmediate(shadowGo);
            }
        }

        [Test]
        public void MedievalEase_CurvesAndOvershootBehaveCorrectly()
        {
            Assert.AreEqual(0f, MedievalEase.OutCubic(0f), 0.001f);
            Assert.AreEqual(1f, MedievalEase.OutCubic(1f), 0.001f);
            Assert.Greater(MedievalEase.OutCubic(0.5f), 0.5f);

            Assert.AreEqual(0f, MedievalEase.OutBack(0f), 0.001f);
            Assert.AreEqual(1f, MedievalEase.OutBack(1f), 0.001f);
            Assert.Greater(MedievalEase.OutBack(0.6f), 1f);

            Assert.AreEqual(0f, MedievalEase.InOutCubic(0f), 0.001f);
            Assert.AreEqual(0.5f, MedievalEase.InOutCubic(0.5f), 0.001f);
            Assert.AreEqual(1f, MedievalEase.InOutCubic(1f), 0.001f);
        }

        [Test]
        public void CombatCalculator_KnowledgeGaugePerfectGuardAndSynergyDamage()
        {
            Assert.AreEqual(100, CombatCalculator.MaxKnowledgeGauge);

            Assert.AreEqual(35, CombatCalculator.CalculateKnowledgeGain(true, true, true, false));
            Assert.AreEqual(25, CombatCalculator.CalculateKnowledgeGain(true, true, false, false));
            Assert.AreEqual(20, CombatCalculator.CalculateKnowledgeGain(true, true, false, true));
            Assert.AreEqual(-10, CombatCalculator.CalculateKnowledgeGain(true, false, false, false));
            Assert.AreEqual(15, CombatCalculator.CalculateKnowledgeGain(false, false, false, false));

            Assert.AreEqual(100, CombatCalculator.CalculateNextKnowledgeGauge(85, 25));
            Assert.AreEqual(0, CombatCalculator.CalculateNextKnowledgeGauge(5, -20));

            int quickDmg = CombatCalculator.CalculatePerfectGuardDamage(100, true, out int quickReflect);
            Assert.AreEqual(35, quickDmg);
            Assert.AreEqual(45, quickReflect);

            int normDmg = CombatCalculator.CalculatePerfectGuardDamage(100, false, out int normReflect);
            Assert.AreEqual(50, normDmg);
            Assert.AreEqual(20, normReflect);

            int synDmg = CombatCalculator.CalculateSynergyStrikeDamage(100, 3, 4);
            Assert.AreEqual(180, synDmg);
        }

        [Test]
        public void BattleSystem_ShouldTriggerDefensiveQuizRulesAndCooldown()
        {
            GameObject bossGo = new GameObject("BossTest");
            GameObject normalGo = new GameObject("GoblinTest");
            GameObject eliteGo = new GameObject("EliteTest");
            SkillData weakSingle = ScriptableObject.CreateInstance<SkillData>();
            SkillData heavySingle = ScriptableObject.CreateInstance<SkillData>();
            SkillData aoeSkill = ScriptableObject.CreateInstance<SkillData>();
            try
            {
                Unit boss = bossGo.AddComponent<Unit>();
                boss.isEnemy = true;
                boss.isBoss = true;
                boss.maxHP = 500;
                boss.currentHP = 500;

                Unit normal = normalGo.AddComponent<Unit>();
                normal.isEnemy = true;
                normal.unitName = "Goblin";
                normal.maxHP = 100;
                normal.currentHP = 100;

                Unit elite = eliteGo.AddComponent<Unit>();
                elite.isEnemy = true;
                elite.unitName = "Tinh Anh Cận Vệ";
                elite.maxHP = 250;
                elite.currentHP = 250;

                weakSingle.targetType = SkillTargetType.SingleEnemy;
                weakSingle.powerMultiplier = 1f;
                heavySingle.targetType = SkillTargetType.SingleEnemy;
                heavySingle.powerMultiplier = 1.25f;
                aoeSkill.targetType = SkillTargetType.AllEnemies;
                aoeSkill.powerMultiplier = 0.8f;

                Assert.IsFalse(BattleSystem.ShouldTriggerDefensiveQuiz(false, boss, weakSingle, 1, 3, -99));
                Assert.IsFalse(BattleSystem.ShouldTriggerDefensiveQuiz(true, boss, weakSingle, 1, 3, 3));
                Assert.IsTrue(BattleSystem.ShouldTriggerDefensiveQuiz(true, boss, weakSingle, 1, 3, 2));
                Assert.IsTrue(BattleSystem.ShouldTriggerDefensiveQuiz(true, elite, weakSingle, 1, 3, 0));
                Assert.IsFalse(BattleSystem.ShouldTriggerDefensiveQuiz(true, normal, weakSingle, 1, 3, 0));
                Assert.IsTrue(BattleSystem.ShouldTriggerDefensiveQuiz(true, normal, heavySingle, 1, 3, 0));
                Assert.IsTrue(BattleSystem.ShouldTriggerDefensiveQuiz(true, normal, aoeSkill, 3, 3, 0));
            }
            finally
            {
                Object.DestroyImmediate(bossGo);
                Object.DestroyImmediate(normalGo);
                Object.DestroyImmediate(eliteGo);
                Object.DestroyImmediate(weakSingle);
                Object.DestroyImmediate(heavySingle);
                Object.DestroyImmediate(aoeSkill);
            }
        }

        [Test]
        public void Unit_TakeDamageRespectsExternalMultiplierExceptTrueDamage()
        {
            GameObject go = new GameObject("GuardUnit");
            try
            {
                Unit u = go.AddComponent<Unit>();
                u.maxHP = 300;
                u.currentHP = 300;
                u.defense = 0;
                u.magicDefense = 0;

                int guardPhys = u.TakeDamage(100, DamageType.Physical, out bool died1, 0.5f);
                Assert.AreEqual(50, guardPhys);
                Assert.IsFalse(died1);

                int trueDmg = u.TakeDamage(40, DamageType.TrueDamage, out bool died2, 0.35f);
                Assert.AreEqual(40, trueDmg);
                Assert.IsFalse(died2);
                Assert.AreEqual(210, u.currentHP);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RoadsideEventCatalog_TriggersAndOffersValidChoices()
        {
            Assert.IsFalse(RoadsideEventCatalog.ShouldTriggerEventBeforeWave(0, false, 6));
            Assert.IsFalse(RoadsideEventCatalog.ShouldTriggerEventBeforeWave(1, false, 6));
            Assert.IsTrue(RoadsideEventCatalog.ShouldTriggerEventBeforeWave(3, false, 6));
            Assert.IsFalse(RoadsideEventCatalog.ShouldTriggerEventBeforeWave(4, true, 999));
            Assert.IsTrue(RoadsideEventCatalog.ShouldTriggerEventBeforeWave(6, true, 999));

            foreach (RoadsideEventType t in (RoadsideEventType[])System.Enum.GetValues(typeof(RoadsideEventType)))
            {
                RoadsideEventOffer offer = RoadsideEventCatalog.CreateOffer(t);
                Assert.IsNotNull(offer);
                Assert.IsNotEmpty(offer.title);
                Assert.AreEqual(3, offer.options.Count);
            }
        }

        [Test]
        public void QuizManager_StaticTopicLookupFindsMatchingTopicWithoutMutatingBank()
        {
            List<string> topics = QuizManager.GetAllTopicKeysStatic();
            Assert.IsNotNull(topics);
            Assert.Greater(topics.Count, 3);

            string targetTopic = topics[0];
            QuizQuestion picked = QuizManager.GetQuestionForTopicStatic(targetTopic, QuizDifficultyFilter.AllB1B2);
            Assert.IsNotNull(picked);
            Assert.AreEqual(targetTopic, PlayerCampProgressManager.NormalizeTopicKey(picked.category));
            Assert.AreEqual(4, picked.answers.Length);
        }

        [Test]
        public void UnitStatusVFX_TogglesStatusNodesAndHidesOnDeath()
        {
            GameObject go = new GameObject("StatusVFXUnit");
            try
            {
                Unit u = go.AddComponent<Unit>();
                u.maxHP = 100;
                u.currentHP = 100;
                u.isBoss = true;

                UnitStatusVFX vfx = UnitStatusVFX.AttachOrRefresh(u);
                Assert.IsNotNull(vfx);

                Transform stun0 = go.transform.Find("StatusVFXRoot/StunStar_0");
                Transform burn0 = go.transform.Find("StatusVFXRoot/BurnMote_0");
                Transform shield = go.transform.Find("StatusVFXRoot/ShieldAura");
                Transform rune0 = go.transform.Find("StatusVFXRoot/ShadowRune_0");
                Transform combo = go.transform.Find("StatusVFXRoot/ComboAura");

                Assert.IsNotNull(stun0);
                Assert.IsFalse(stun0.gameObject.activeSelf);

                u.ApplyStatus(StatusEffectType.Stun, 2, 0);
                u.ApplyStatus(StatusEffectType.Burn, 2, 10);
                u.SetDefending(true);
                vfx.SetComboAuraActive(true);
                Assert.IsTrue(stun0.gameObject.activeSelf);
                Assert.IsTrue(burn0.gameObject.activeSelf);
                Assert.IsTrue(shield.gameObject.activeSelf);
                Assert.IsTrue(combo.gameObject.activeSelf);

                u.currentHP = 40;
                Assert.IsTrue(u.TryTriggerPhase2());
                Assert.IsTrue(rune0.gameObject.activeSelf);

                u.TakeDamage(9999, DamageType.TrueDamage, out bool died);
                Assert.IsTrue(died);
                Assert.IsFalse(stun0.gameObject.activeSelf);
                Assert.IsFalse(burn0.gameObject.activeSelf);
                Assert.IsFalse(shield.gameObject.activeSelf);
                Assert.IsFalse(rune0.gameObject.activeSelf);
                Assert.IsFalse(combo.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PixelCombatVFXManager_ObjectPoolPrewarmsAndReleasesCleanly()
        {
            GameObject go = new GameObject("PoolTestVFX");
            try
            {
                PixelCombatVFXManager mgr = go.AddComponent<PixelCombatVFXManager>();
                mgr.EnsurePoolInitialized();
                Assert.AreEqual(8, mgr.TotalPoolCreated);
                Assert.AreEqual(0, mgr.ActivePooledCount);
                Assert.AreEqual(8, mgr.InactivePooledCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
