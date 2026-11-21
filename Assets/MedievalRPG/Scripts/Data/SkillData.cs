using UnityEngine;

namespace MedievalRPG.Data
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Medieval RPG/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Basic Information")]
        public string skillName = "New Skill";
        [TextArea(2, 4)]
        public string description = "Skill description...";
        public CharacterClassType requiredClass;
        public Sprite icon;

        [Header("Cost & Targeting")]
        public int manaCost = 10;
        public int cooldownTurns = 0;
        public SkillTargetType targetType = SkillTargetType.SingleEnemy;

        [Header("Combat Effect")]
        public DamageType damageType = DamageType.Physical;
        [Tooltip("Multiplies physicalAttack (Physical) or magicAttack (Magical)")]
        public float powerMultiplier = 1.0f;
        public int flatBonusDamage = 0;
        public int baseHealAmount = 0;

        [Header("Buffs / Special Effects")]
        [Tooltip("Increases defense for self or target")]
        public int defenseBonus = 0;
        [Tooltip("Bonus crit chance for this attack (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float bonusCritRate = 0f;
        [Tooltip("Duration of buff/debuff in turns")]
        public int effectDurationTurns = 0;
    }
}
