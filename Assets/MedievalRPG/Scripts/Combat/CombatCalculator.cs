using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.Combat
{
    /// <summary>
    /// Các công thức tính toán chiến đấu thuần túy (stateless) để dễ kiểm thử và cân bằng.
    /// </summary>
    public static class CombatCalculator
    {
        public static int CalculateMitigatedDamage(
            int rawDamage,
            DamageType damageType,
            int effectivePhysicalDefense,
            int effectiveMagicDefense,
            bool isDefending)
        {
            float finalDamage = Mathf.Max(1, rawDamage);

            switch (damageType)
            {
                case DamageType.Physical:
                    float physReduction = 100f / (100f + Mathf.Max(0, effectivePhysicalDefense));
                    finalDamage = rawDamage * physReduction;
                    break;

                case DamageType.Magical:
                    float magReduction = 100f / (100f + Mathf.Max(0, effectiveMagicDefense));
                    finalDamage = rawDamage * magReduction;
                    break;

                case DamageType.TrueDamage:
                    finalDamage = rawDamage;
                    break;
            }

            if (isDefending && damageType != DamageType.TrueDamage)
            {
                finalDamage *= 0.5f;
            }

            return Mathf.Max(1, Mathf.RoundToInt(finalDamage));
        }

        public static int CalculateSkillRawDamage(
            int baseAttackPower,
            float powerMultiplier,
            int flatBonusDamage,
            float critMultiplier,
            bool isCrit,
            int quizBonusDamage,
            int quizStreak,
            float attackBonusPercent = 0f)
        {
            float attackBoost = 1f + Mathf.Max(0f, attackBonusPercent) * 0.01f;
            float streakBoost = quizStreak >= 3 ? 1.20f : 1.0f;
            float critFactor = isCrit ? Mathf.Max(1f, critMultiplier) : 1.0f;

            float scaled = (baseAttackPower * attackBoost * Mathf.Max(0f, powerMultiplier) * critFactor) + flatBonusDamage + quizBonusDamage;
            return Mathf.Max(1, Mathf.RoundToInt(scaled * streakBoost));
        }

        public static int CalculateHealAmount(int baseHealAmount, int magicAttack, int quizBonus)
        {
            return Mathf.Max(1, baseHealAmount + Mathf.RoundToInt(magicAttack * 0.45f) + quizBonus);
        }

        public static int CompareTurnOrder(Unit a, Unit b)
        {
            if (a == b) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int speedCmp = b.speed.CompareTo(a.speed);
            if (speedCmp != 0) return speedCmp;

            // Tie-breaker: heroes act before enemies
            if (a.isEnemy != b.isEnemy) return a.isEnemy ? 1 : -1;

            return a.GetEntityId().CompareTo(b.GetEntityId());
        }
    }
}
