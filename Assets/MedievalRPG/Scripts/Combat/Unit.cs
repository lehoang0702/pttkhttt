using System;
using System.Collections.Generic;
using UnityEngine;
using MedievalRPG.Data;
using MedievalRPG.Visuals;

namespace MedievalRPG.Combat
{
    /// <summary>
    /// Thực thể chiến đấu (Anh hùng hoặc Quái vật/Boss).
    /// Quản lý chỉ số sinh tồn, kháng vật lý/phép, cooldown kỹ năng, hiệu ứng trạng thái,
    /// cơ chế Khiên Bóng Tối của Boss và dự báo hành động (Enemy Intent).
    /// </summary>
    public class Unit : MonoBehaviour
    {
        [Header("--- Thuộc tính cơ bản ---")]
        public string unitName = "Anh Hùng";
        public int unitLevel = 1;
        public int damage = 20;
        public int maxHP = 100;
        public int currentHP = 100;

        [Header("--- Chỉ số RPG Trung Cổ ---")]
        public CharacterClassType characterClass = CharacterClassType.Knight;
        public int maxMP = 50;
        public int currentMP = 50;
        public int defense = 10;
        public int magicDefense = 8;
        public int magicDamage = 5;
        public int speed = 10;
        [Range(0f, 1f)]
        public float critRate = 0.05f;
        public float critMultiplier = 1.5f;
        public bool isBoss = false;

        [Header("--- Trạng thái chiến đấu ---")]
        public bool isDefending = false;
        public int bonusDefense = 0;
        public int defenseBuffTurns = 0;
        public bool isEnemy = false;

        [Header("--- Tham chiếu thành phần & Dữ liệu ---")]
        public CharacterClassData classData;
        public PixelCharacterAnimator pixelAnimator;
        public List<SkillData> skills = new List<SkillData>();

        private readonly Dictionary<SkillData, int> _skillCooldowns = new Dictionary<SkillData, int>();
        private readonly List<StatusEffectInstance> _activeStatuses = new List<StatusEffectInstance>();

        public int EffectiveDefense => defense + bonusDefense + GetStatusPotency(StatusEffectType.DefenseUp);
        public int EffectiveMagicDefense => magicDefense + (bonusDefense / 2) + (GetStatusPotency(StatusEffectType.DefenseUp) / 2);
        public int AttackBonusPercent => GetStatusPotency(StatusEffectType.AttackUp);
        public int LastDamageTaken { get; private set; }
        public bool IsAlive => currentHP > 0;
        public bool IsStunned => HasStatus(StatusEffectType.Stun);
        public bool HasTaunt => HasStatus(StatusEffectType.Taunt);
        public IReadOnlyList<StatusEffectInstance> ActiveStatuses => _activeStatuses;

        // Boss Phase 2 (Dark Knight Shadow Shield)
        public bool HasTriggeredPhase2 { get; private set; }
        public int ShadowShieldStacks { get; private set; }

        // Enemy Intent Preview
        public SkillData PlannedSkill { get; private set; }
        public EnemyIntentType PlannedIntent { get; private set; } = EnemyIntentType.None;
        public int PlannedDamagePreview { get; private set; }

        public event Action<Unit> OnStatsChanged;

        private void Awake()
        {
            if (pixelAnimator == null) pixelAnimator = GetComponent<PixelCharacterAnimator>();

            if (classData != null)
            {
                InitializeFromClass(classData);
            }
            else if (!string.IsNullOrEmpty(unitName) && unitName.Contains("Boss"))
            {
                isBoss = true;
            }
        }

        public void InitializeFromClass(CharacterClassData data)
        {
            if (data == null) return;

            classData = data;
            unitName = data.className;
            unitLevel = Mathf.Max(1, data.baseLevel);
            characterClass = data.classType;
            isBoss = data.isBoss || (!string.IsNullOrEmpty(data.className) && data.className.Contains("Boss"));

            maxHP = data.maxHealth;
            currentHP = maxHP;
            maxMP = data.maxMana;
            currentMP = maxMP;
            damage = data.physicalAttack;
            magicDamage = data.magicAttack;
            defense = data.physicalDefense;
            magicDefense = data.magicDefense;
            speed = data.speed;
            critRate = data.critRate;
            critMultiplier = data.critMultiplier;
            skills = new List<SkillData>(data.skills);

            _skillCooldowns.Clear();
            _activeStatuses.Clear();
            isDefending = false;
            bonusDefense = 0;
            defenseBuffTurns = 0;
            HasTriggeredPhase2 = false;
            ShadowShieldStacks = 0;
            ClearPlannedIntent();

            OnStatsChanged?.Invoke(this);
        }

        public bool TakeDamage(int rawDmg)
        {
            TakeDamage(rawDmg, DamageType.Physical, out bool died);
            return died;
        }

        public int TakeDamage(int rawDmg, DamageType damageType, out bool died)
        {
            died = false;
            if (currentHP <= 0)
            {
                LastDamageTaken = 0;
                return 0;
            }

            int finalDamage = CombatCalculator.CalculateMitigatedDamage(
                rawDmg,
                damageType,
                EffectiveDefense,
                EffectiveMagicDefense,
                isDefending);

            // Boss đang bật Khiên Bóng Tối giảm 35% sát thương nhận vào
            if (ShadowShieldStacks > 0 && damageType != DamageType.TrueDamage)
            {
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * 0.65f));
            }

            LastDamageTaken = finalDamage;
            currentHP = Mathf.Max(0, currentHP - finalDamage);

            if (currentHP <= 0)
            {
                died = true;
                _activeStatuses.Clear();
                isDefending = false;
                ClearPlannedIntent();
                pixelAnimator?.PlayDeath();
            }
            else
            {
                pixelAnimator?.PlayHit();
            }

            OnStatsChanged?.Invoke(this);
            return finalDamage;
        }

        public int Heal(int amount)
        {
            if (currentHP <= 0) return 0;
            int prev = currentHP;
            currentHP = Mathf.Min(maxHP, currentHP + Mathf.Max(0, amount));
            int gained = currentHP - prev;
            OnStatsChanged?.Invoke(this);
            return gained;
        }

        public int Revive(int hpAmount)
        {
            if (currentHP > 0) return 0;
            currentHP = Mathf.Clamp(hpAmount, 1, maxHP);
            _activeStatuses.Clear();
            isDefending = false;
            pixelAnimator?.ReviveToIdle();
            OnStatsChanged?.Invoke(this);
            return currentHP;
        }

        public bool UseMana(int amount)
        {
            if (amount <= 0) return true;
            if (currentMP < amount) return false;
            currentMP -= amount;
            OnStatsChanged?.Invoke(this);
            return true;
        }

        public void RestoreMana(int amount)
        {
            if (currentHP <= 0) return;
            currentMP = Mathf.Min(maxMP, currentMP + Mathf.Max(0, amount));
            OnStatsChanged?.Invoke(this);
        }

        public void ApplyDefenseBuff(int bonus, int turns)
        {
            bonusDefense = Mathf.Max(bonusDefense, bonus);
            defenseBuffTurns = Mathf.Max(defenseBuffTurns, turns);
            OnStatsChanged?.Invoke(this);
        }

        // =====================================================================
        // COOLDOWNS
        // =====================================================================
        public int GetSkillCooldown(SkillData skill)
        {
            if (skill == null) return 0;
            return _skillCooldowns.TryGetValue(skill, out int cd) ? cd : 0;
        }

        public bool IsSkillOnCooldown(SkillData skill) => GetSkillCooldown(skill) > 0;

        public void StartSkillCooldown(SkillData skill)
        {
            if (skill == null || skill.cooldownTurns <= 0) return;
            // +1 vì OnTurnStart của chính nhân vật ở lượt kế tiếp sẽ trừ đi 1
            _skillCooldowns[skill] = skill.cooldownTurns + 1;
            OnStatsChanged?.Invoke(this);
        }

        private void TickCooldowns()
        {
            if (_skillCooldowns.Count == 0) return;
            List<SkillData> keys = new List<SkillData>(_skillCooldowns.Keys);
            foreach (SkillData skill in keys)
            {
                int next = _skillCooldowns[skill] - 1;
                if (next <= 0) _skillCooldowns.Remove(skill);
                else _skillCooldowns[skill] = next;
            }
        }

        // =====================================================================
        // STATUS EFFECTS
        // =====================================================================
        public void ApplyStatus(StatusEffectType type, int turns, int potency = 0)
        {
            if (type == StatusEffectType.None || turns <= 0 || currentHP <= 0) return;

            StatusEffectInstance existing = GetStatus(type);
            if (existing != null)
            {
                existing.remainingTurns = Mathf.Max(existing.remainingTurns, turns);
                existing.potency = Mathf.Max(existing.potency, potency);
            }
            else
            {
                _activeStatuses.Add(new StatusEffectInstance(type, turns, potency));
            }
            OnStatsChanged?.Invoke(this);
        }

        public bool HasStatus(StatusEffectType type) => GetStatus(type) != null;

        public StatusEffectInstance GetStatus(StatusEffectType type)
        {
            for (int i = 0; i < _activeStatuses.Count; i++)
            {
                if (_activeStatuses[i].type == type) return _activeStatuses[i];
            }
            return null;
        }

        public int GetStatusPotency(StatusEffectType type)
        {
            StatusEffectInstance inst = GetStatus(type);
            return inst != null ? inst.potency : 0;
        }

        // =====================================================================
        // BOSS PHASE 2 & TURN LIFECYCLE
        // =====================================================================
        public bool TryTriggerPhase2()
        {
            if (!isBoss || HasTriggeredPhase2 || currentHP <= 0) return false;
            if (currentHP > maxHP * 0.5f) return false;

            HasTriggeredPhase2 = true;
            ShadowShieldStacks = 2;
            ApplyStatus(StatusEffectType.AttackUp, 3, 25);
            OnStatsChanged?.Invoke(this);
            return true;
        }

        public bool StripShadowShield(out bool shieldBroken)
        {
            shieldBroken = false;
            if (ShadowShieldStacks <= 0 || currentHP <= 0) return false;

            ShadowShieldStacks--;
            if (ShadowShieldStacks <= 0)
            {
                ShadowShieldStacks = 0;
                shieldBroken = true;
                ApplyStatus(StatusEffectType.Stun, 1, 0);
            }
            OnStatsChanged?.Invoke(this);
            return true;
        }

        public void OnTurnStart()
        {
            OnTurnStart(out _, out _, out _);
        }

        public void OnTurnStart(out int burnDamageTaken, out bool wasStunned, out bool diedFromBurn)
        {
            burnDamageTaken = 0;
            wasStunned = false;
            diedFromBurn = false;

            if (currentHP <= 0) return;

            isDefending = false;
            TickCooldowns();

            if (defenseBuffTurns > 0)
            {
                defenseBuffTurns--;
                if (defenseBuffTurns == 0)
                {
                    bonusDefense = 0;
                }
            }

            // Xử lý hiệu ứng trước khi đếm lùi lượt tồn tại
            StatusEffectInstance burn = GetStatus(StatusEffectType.Burn);
            if (burn != null && burn.potency > 0)
            {
                burnDamageTaken = TakeDamage(burn.potency, DamageType.TrueDamage, out diedFromBurn);
                if (diedFromBurn) return;
            }

            if (HasStatus(StatusEffectType.Stun))
            {
                wasStunned = true;
            }

            // Đếm lùi thời gian hiệu ứng còn lại
            for (int i = _activeStatuses.Count - 1; i >= 0; i--)
            {
                _activeStatuses[i].remainingTurns--;
                if (_activeStatuses[i].remainingTurns <= 0)
                {
                    _activeStatuses.RemoveAt(i);
                }
            }

            RestoreMana(4);
            OnStatsChanged?.Invoke(this);
        }

        // =====================================================================
        // ENEMY INTENT PLANNING
        // =====================================================================
        public void PlanNextIntent(int currentRound)
        {
            if (!isEnemy || currentHP <= 0)
            {
                ClearPlannedIntent();
                return;
            }

            if (IsStunned)
            {
                PlannedSkill = null;
                PlannedIntent = EnemyIntentType.None;
                PlannedDamagePreview = 0;
                OnStatsChanged?.Invoke(this);
                return;
            }

            SkillData chosen = PickIntentSkill(currentRound);
            PlannedSkill = chosen;

            if (chosen == null)
            {
                PlannedIntent = EnemyIntentType.SingleAttack;
                PlannedDamagePreview = Mathf.RoundToInt(damage * (1f + AttackBonusPercent * 0.01f));
            }
            else if (chosen.IsSelfSupport)
            {
                PlannedIntent = EnemyIntentType.Buff;
                PlannedDamagePreview = 0;
            }
            else
            {
                int baseAtk = chosen.damageType == DamageType.Magical ? magicDamage : damage;
                float mult = Mathf.Max(0.5f, chosen.powerMultiplier);
                float boost = 1f + AttackBonusPercent * 0.01f;
                PlannedDamagePreview = Mathf.RoundToInt(baseAtk * boost * mult) + chosen.flatBonusDamage;

                if (chosen.targetType == SkillTargetType.AllEnemies)
                {
                    PlannedIntent = EnemyIntentType.AoEAttack;
                }
                else if (chosen.statusEffect != StatusEffectType.None)
                {
                    PlannedIntent = EnemyIntentType.DebuffAttack;
                }
                else
                {
                    PlannedIntent = EnemyIntentType.SingleAttack;
                }
            }

            OnStatsChanged?.Invoke(this);
        }

        public void ClearPlannedIntent()
        {
            PlannedSkill = null;
            PlannedIntent = EnemyIntentType.None;
            PlannedDamagePreview = 0;
        }

        private SkillData PickIntentSkill(int currentRound)
        {
            if (skills == null || skills.Count == 0) return null;

            List<SkillData> usable = new List<SkillData>();
            for (int i = 0; i < skills.Count; i++)
            {
                SkillData s = skills[i];
                if (s == null) continue;
                if (currentMP < s.manaCost || IsSkillOnCooldown(s)) continue;
                if (s.IsSelfSupport && bonusDefense > 0) continue; // Không spam buff giáp khi đang còn giáp
                usable.Add(s);
            }

            if (usable.Count == 0) return skills[0];

            // Boss trong Phase 2 ưu tiên dùng kỹ năng mạnh / AoE khi hồi xong
            if (isBoss && HasTriggeredPhase2)
            {
                for (int i = usable.Count - 1; i >= 0; i--)
                {
                    if (usable[i].targetType == SkillTargetType.AllEnemies || usable[i].powerMultiplier >= 1.3f)
                    {
                        return usable[i];
                    }
                }
            }

            // Quái thường xen kẽ kỹ năng đặc biệt theo tỉ lệ hợp lý
            if (usable.Count > 1 && (currentRound % 2 == 0 || UnityEngine.Random.value < 0.45f))
            {
                return usable[UnityEngine.Random.Range(1, usable.Count)];
            }

            return usable[0];
        }

        public string GetIntentDisplayText()
        {
            if (IsStunned) return "BỊ CHOÁNG";
            switch (PlannedIntent)
            {
                case EnemyIntentType.SingleAttack: return $"ĐÁNH ~{PlannedDamagePreview}";
                case EnemyIntentType.DebuffAttack: return $"ĐỘC/ÁM ~{PlannedDamagePreview}";
                case EnemyIntentType.AoEAttack:    return $"DIỆN RỘNG ~{PlannedDamagePreview}";
                case EnemyIntentType.Buff:         return "TỤ LỰC / THỦ";
                default:                           return string.Empty;
            }
        }
    }
}
