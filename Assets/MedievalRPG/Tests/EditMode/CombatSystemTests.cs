using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.Quiz;

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
    }
}
