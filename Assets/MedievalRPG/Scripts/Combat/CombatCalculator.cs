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

        /// <summary>
        /// Tính số sao khi thắng màn (1-3 sao):
        /// - 1 sao mặc định khi chiến thắng.
        /// - +1 sao Tri Thức nếu đã trả lời ít nhất 1 câu Quiz và đạt >= 75% chính xác (tắt Quiz sẽ không nhận sao này).
        /// - +1 sao Sinh Tồn nếu toàn bộ đội hình còn sống khi kết thúc trận.
        /// </summary>
        public static int CalculateStageStars(
            int totalQuestionsAnswered,
            int correctAnswersCount,
            int aliveHeroesCount,
            int totalPartyCount)
        {
            int stars = 1;
            if (totalQuestionsAnswered > 0 && (float)correctAnswersCount / totalQuestionsAnswered >= 0.75f)
            {
                stars++;
            }
            if (totalPartyCount > 0 && aliveHeroesCount >= totalPartyCount)
            {
                stars++;
            }
            return stars;
        }

        public const int MaxKnowledgeGauge = 100;

        public static int CalculateKnowledgeGain(bool quizEnabled, bool isCorrect, bool isQuickMind, bool isDefensiveParry)
        {
            if (!quizEnabled) return 15;
            if (!isCorrect) return -10;
            if (isDefensiveParry) return 20;
            return isQuickMind ? 35 : 25;
        }

        public static int CalculateNextKnowledgeGauge(int currentGauge, int delta)
        {
            return Mathf.Clamp(currentGauge + delta, 0, MaxKnowledgeGauge);
        }

        public static int CalculatePerfectGuardDamage(int originalDamage, bool isQuickParry, out int reflectedDamage)
        {
            int safeDamage = Mathf.Max(1, originalDamage);
            if (isQuickParry)
            {
                reflectedDamage = Mathf.Max(12, Mathf.RoundToInt(safeDamage * 0.45f));
                return Mathf.Max(1, Mathf.RoundToInt(safeDamage * 0.35f));
            }

            reflectedDamage = Mathf.Max(6, Mathf.RoundToInt(safeDamage * 0.20f));
            return Mathf.Max(1, Mathf.RoundToInt(safeDamage * 0.50f));
        }

        public static int CalculateSynergyStrikeDamage(
            int totalPartyAttackPower,
            int aliveHeroCount,
            int quizStreak)
        {
            float heroBonus = 0.75f + Mathf.Max(1, aliveHeroCount) * 0.25f;
            float streakBoost = 1f + Mathf.Clamp(quizStreak, 0, 8) * 0.05f;
            return Mathf.Max(24, Mathf.RoundToInt(totalPartyAttackPower * heroBonus * streakBoost));
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
