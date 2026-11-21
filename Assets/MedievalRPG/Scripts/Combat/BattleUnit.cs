using System;
using System.Collections.Generic;
using UnityEngine;
using MedievalRPG.Data;
using MedievalRPG.Visuals;

namespace MedievalRPG.Combat
{
    public class BattleUnit : MonoBehaviour
    {
        [Header("Class Data")]
        [SerializeField] private CharacterClassData classData;
        [SerializeField] private bool isEnemy = false;

        [Header("Components")]
        [SerializeField] private CharacterVisualController visualController;

        // Dynamic Combat Stats
        public int CurrentHealth { get; private set; }
        public int MaxHealth => classData != null ? classData.maxHealth : 100;
        public int CurrentMana { get; private set; }
        public int MaxMana => classData != null ? classData.maxMana : 50;

        public bool IsDead { get; private set; }
        public bool IsDefending { get; private set; }
        public bool IsEnemy => isEnemy;
        public CharacterClassData ClassData => classData;

        // Temporary battle buffs
        private int _bonusDefense = 0;
        private int _buffTurnsRemaining = 0;

        // Events for UI and Sound
        public event Action<BattleUnit, int, bool> OnDamaged; // unit, amount, isCrit
        public event Action<BattleUnit, int> OnHealed;        // unit, amount
        public event Action<BattleUnit> OnDefeated;
        public event Action<BattleUnit> OnStatsChanged;

        private void Awake()
        {
            if (visualController == null)
            {
                visualController = GetComponent<CharacterVisualController>();
            }
        }

        private void Start()
        {
            if (classData != null)
            {
                Initialize(classData);
            }
        }

        public void Initialize(CharacterClassData data)
        {
            classData = data;
            CurrentHealth = data.maxHealth;
            CurrentMana = data.maxMana;
            IsDead = false;
            IsDefending = false;
            _bonusDefense = 0;
            _buffTurnsRemaining = 0;

            if (visualController != null)
            {
                visualController.SetupSprites(data);
            }

            OnStatsChanged?.Invoke(this);
        }

        public void OnTurnStart()
        {
            if (IsDead) return;

            // Reset Guard state each turn
            IsDefending = false;

            // Tick buff duration
            if (_buffTurnsRemaining > 0)
            {
                _buffTurnsRemaining--;
                if (_buffTurnsRemaining == 0)
                {
                    _bonusDefense = 0;
                }
            }

            // Passive small mana recovery
            RestoreMana(3);
        }

        public void Defend()
        {
            if (IsDead) return;
            IsDefending = true;
            Debug.Log($"[{classData.className}] enters defensive stance (reduces damage taken by 50%).");
        }

        public void TakeDamage(int rawDamage, DamageType type, bool isCrit)
        {
            if (IsDead) return;

            int effectiveDef = (classData != null ? classData.physicalDefense : 0) + _bonusDefense;
            int effectiveMDef = (classData != null ? classData.magicDefense : 0);

            int finalDamage = rawDamage;

            switch (type)
            {
                case DamageType.Physical:
                    // Standard armor formula: damage * (100 / (100 + def))
                    float reduction = 100f / (100f + Mathf.Max(0, effectiveDef));
                    finalDamage = Mathf.RoundToInt(rawDamage * reduction);
                    break;

                case DamageType.Magical:
                    float mReduction = 100f / (100f + Mathf.Max(0, effectiveMDef));
                    finalDamage = Mathf.RoundToInt(rawDamage * mReduction);
                    break;

                case DamageType.TrueDamage:
                    // True damage ignores defense
                    break;
            }

            // Defending stance cuts final damage in half
            if (IsDefending)
            {
                finalDamage = Mathf.Max(1, finalDamage / 2);
            }

            finalDamage = Mathf.Max(1, finalDamage);

            CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);

            if (visualController != null)
            {
                visualController.PlayHitAnimation();
            }

            OnDamaged?.Invoke(this, finalDamage, isCrit);
            OnStatsChanged?.Invoke(this);

            Debug.Log($"[{classData.className}] took {finalDamage} {type} damage ({(isCrit ? "CRITICAL! " : "")}Remaining HP: {CurrentHealth}/{MaxHealth})");

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            int healAmount = Mathf.Min(amount, MaxHealth - CurrentHealth);
            CurrentHealth += healAmount;

            OnHealed?.Invoke(this, healAmount);
            OnStatsChanged?.Invoke(this);

            Debug.Log($"[{classData.className}] healed {healAmount} HP (Current HP: {CurrentHealth}/{MaxHealth})");
        }

        public void RestoreMana(int amount)
        {
            if (IsDead) return;
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
            OnStatsChanged?.Invoke(this);
        }

        public bool ConsumeMana(int amount)
        {
            if (CurrentMana < amount) return false;
            CurrentMana -= amount;
            OnStatsChanged?.Invoke(this);
            return true;
        }

        public void ApplyDefenseBuff(int bonus, int turns)
        {
            _bonusDefense = bonus;
            _buffTurnsRemaining = turns;
            Debug.Log($"[{classData.className}] gains +{bonus} Defense for {turns} turns!");
        }

        private void Die()
        {
            IsDead = true;
            CurrentHealth = 0;
            if (visualController != null)
            {
                visualController.PlayDeathAnimation();
            }
            OnDefeated?.Invoke(this);
            Debug.Log($"[{classData.className}] has fallen in battle!");
        }

        /// <summary>
        /// Executes a skill against target(s)
        /// </summary>
        public void ExecuteSkill(SkillData skill, List<BattleUnit> targets, Action onFinished = null)
        {
            if (IsDead || skill == null) return;

            if (skill.manaCost > 0 && !ConsumeMana(skill.manaCost))
            {
                Debug.LogWarning($"[{classData.className}] doesn't have enough mana for {skill.skillName}!");
                return;
            }

            Vector3 primaryTargetPos = targets.Count > 0 ? targets[0].transform.position : transform.position + Vector3.right;

            if (visualController != null)
            {
                visualController.PlayAttackAnimation(primaryTargetPos, () =>
                {
                    ApplySkillEffects(skill, targets);
                    onFinished?.Invoke();
                });
            }
            else
            {
                ApplySkillEffects(skill, targets);
                onFinished?.Invoke();
            }
        }

        private void ApplySkillEffects(SkillData skill, List<BattleUnit> targets)
        {
            // Self-buffs (e.g. Knight's Iron Bulwark)
            if (skill.defenseBonus > 0)
            {
                ApplyDefenseBuff(skill.defenseBonus, skill.effectDurationTurns);
            }

            // Healing skill (e.g. Mage's Healing Light)
            if (skill.baseHealAmount > 0)
            {
                int healValue = Mathf.RoundToInt(skill.baseHealAmount + (classData.magicAttack * 0.8f));
                foreach (var ally in targets)
                {
                    ally.Heal(healValue);
                }
            }

            // Offensive skill / Attack
            if (skill.powerMultiplier > 0f)
            {
                bool isMagic = skill.damageType == DamageType.Magical;
                int basePower = isMagic ? classData.magicAttack : classData.physicalAttack;
                float totalCritChance = classData.critRate + skill.bonusCritRate;

                foreach (var enemy in targets)
                {
                    bool isCrit = UnityEngine.Random.value < totalCritChance;
                    float multiplier = skill.powerMultiplier * (isCrit ? classData.critMultiplier : 1.0f);
                    int rawDamage = Mathf.RoundToInt((basePower * multiplier) + skill.flatBonusDamage);

                    enemy.TakeDamage(rawDamage, skill.damageType, isCrit);
                }
            }
        }
    }
}
