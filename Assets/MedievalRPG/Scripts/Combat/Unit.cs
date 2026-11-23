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

        // Base stats dùng làm gốc tính lại khi mặc/tháo trang bị hoặc scale theo đợt quái
        private bool _baseStatsCaptured;
        private int _baseMaxHP;
        private int _baseMaxMP;
        private int _baseDamage;
        private int _baseMagicDamage;
        private int _baseDefense;
        private int _baseMagicDefense;
        private int _baseSpeed;
        private float _baseCritRate;
        private EquipmentStatBonus _appliedEquipmentBonus;

        public EquipmentStatBonus AppliedEquipmentBonus => _appliedEquipmentBonus;

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
            else
            {
                if (!string.IsNullOrEmpty(unitName) && unitName.Contains("Boss"))
                {
                    isBoss = true;
                }
                CaptureBaseStats();
            }
        }

        private void Start()
        {
            RefreshEquipmentVisuals();
            if (Application.isPlaying)
            {
                UnitStatusVFX.AttachOrRefresh(this);
            }
        }

        public void SetDefending(bool defending)
        {
            isDefending = defending;
            OnStatsChanged?.Invoke(this);
        }

        public void RefreshEquipmentVisuals()
        {
            if (!Application.isPlaying) return;
            if (isEnemy)
            {
                CharacterEquipmentVisuals existing = GetComponent<CharacterEquipmentVisuals>();
                if (existing != null) existing.ClearVisuals();
                return;
            }

            CharacterEquipmentVisuals.AttachOrRefresh(this);
        }

        private void CaptureBaseStats()
        {
            _baseMaxHP = maxHP;
            _baseMaxMP = maxMP;
            _baseDamage = damage;
            _baseMagicDamage = magicDamage;
            _baseDefense = defense;
            _baseMagicDefense = magicDefense;
            _baseSpeed = speed;
            _baseCritRate = critRate;
            _baseStatsCaptured = true;
        }

        public void ApplyEquipmentBonuses(EquipmentStatBonus bonus)
        {
            if (!_baseStatsCaptured) CaptureBaseStats();

            int oldMaxHP = maxHP;
            int oldMaxMP = maxMP;
            _appliedEquipmentBonus = bonus;

            maxHP = Mathf.Max(1, _baseMaxHP + bonus.maxHP);
            maxMP = Mathf.Max(0, _baseMaxMP + bonus.maxMP);
            damage = Mathf.Max(1, _baseDamage + bonus.physicalAttack);
            magicDamage = Mathf.Max(0, _baseMagicDamage + bonus.magicAttack);
            defense = Mathf.Max(0, _baseDefense + bonus.physicalDefense);
            magicDefense = Mathf.Max(0, _baseMagicDefense + bonus.magicDefense);
            speed = Mathf.Max(1, _baseSpeed + bonus.speed);
            critRate = Mathf.Clamp(_baseCritRate + bonus.critRate, 0f, 0.85f);

            if (currentHP > 0)
            {
                int hpDelta = maxHP - oldMaxHP;
                if (hpDelta > 0) currentHP += hpDelta;
                currentHP = Mathf.Clamp(currentHP, 1, maxHP);
            }

            int mpDelta = maxMP - oldMaxMP;
            if (mpDelta > 0 && currentHP > 0) currentMP += mpDelta;
            currentMP = Mathf.Clamp(currentMP, 0, maxMP);

            RefreshEquipmentVisuals();
            OnStatsChanged?.Invoke(this);
        }

        public void ApplyWaveScale(float multiplier, bool isElite = false)
        {
            if (!_baseStatsCaptured) CaptureBaseStats();
            float mult = Mathf.Clamp(multiplier, 0.5f, 3.0f);

            maxHP = Mathf.Max(15, Mathf.RoundToInt(_baseMaxHP * mult));
            currentHP = maxHP;
            damage = Mathf.Max(4, Mathf.RoundToInt(_baseDamage * mult));
            magicDamage = Mathf.Max(0, Mathf.RoundToInt(_baseMagicDamage * mult));
            defense = Mathf.Max(0, Mathf.RoundToInt(_baseDefense * (0.5f + 0.5f * mult)));
            magicDefense = Mathf.Max(0, Mathf.RoundToInt(_baseMagicDefense * (0.5f + 0.5f * mult)));

            if (isElite && !isBoss)
            {
                if (!unitName.StartsWith("Tinh Anh"))
                {
                    unitName = $"Tinh Anh {unitName}";
                }
                unitLevel += 1;
                transform.localScale = transform.localScale * 1.12f;
                Color eliteTint = new Color(0.88f, 0.72f, 1f, 1f);
                if (pixelAnimator == null) pixelAnimator = GetComponent<PixelCharacterAnimator>();
                if (pixelAnimator != null)
                {
                    pixelAnimator.CaptureBaseScale();
                    pixelAnimator.SetBaseColor(eliteTint);
                }
                else
                {
                    SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.color = eliteTint;
                }
            }

            CaptureBaseStats();
            OnStatsChanged?.Invoke(this);
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
            CaptureBaseStats();

            OnStatsChanged?.Invoke(this);
        }

        public bool TakeDamage(int rawDmg)
        {
            TakeDamage(rawDmg, DamageType.Physical, out bool died);
            return died;
        }

        public int TakeDamage(int rawDmg, DamageType damageType, out bool died)
        {
            return TakeDamage(rawDmg, damageType, out died, 1f);
        }

        public int TakeDamage(int rawDmg, DamageType damageType, out bool died, float externalMultiplier)
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

            if (externalMultiplier > 0f && Mathf.Abs(externalMultiplier - 1f) > 0.001f && damageType != DamageType.TrueDamage)
            {
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * externalMultiplier));
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
            PlanNextIntent(currentRound, false);
        }

        public void PlanNextIntent(int currentRound, bool hasWoundedEnemyAlly)
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

            SkillData chosen = PickIntentSkill(currentRound, hasWoundedEnemyAlly);
            PlannedSkill = chosen;

            if (chosen == null)
            {
                PlannedIntent = EnemyIntentType.SingleAttack;
                PlannedDamagePreview = Mathf.RoundToInt(damage * (1f + AttackBonusPercent * 0.01f));
            }
            else if (chosen.IsSelfSupport || chosen.IsHealSkill)
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

        private SkillData PickIntentSkill(int currentRound, bool hasWoundedEnemyAlly)
        {
            if (skills == null || skills.Count == 0) return null;

            List<SkillData> usable = new List<SkillData>();
            List<SkillData> healSkills = new List<SkillData>();
            for (int i = 0; i < skills.Count; i++)
            {
                SkillData s = skills[i];
                if (s == null) continue;
                if (currentMP < s.manaCost || IsSkillOnCooldown(s)) continue;
                if (s.IsHealSkill)
                {
                    if (hasWoundedEnemyAlly)
                    {
                        healSkills.Add(s);
                        usable.Add(s);
                    }
                    continue;
                }
                if (s.IsSelfSupport && bonusDefense > 0) continue; // Không spam buff giáp khi đang còn giáp
                usable.Add(s);
            }

            if (usable.Count == 0)
            {
                for (int i = 0; i < skills.Count; i++)
                {
                    if (skills[i] != null && !skills[i].IsHealSkill) return skills[i];
                }
                return null;
            }

            // Pháp sư / quái hỗ trợ ưu tiên hồi máu & tạo khiên khi có đồng minh bị thương
            if (hasWoundedEnemyAlly && healSkills.Count > 0 && (currentRound % 2 == 1 || UnityEngine.Random.value < 0.6f))
            {
                return healSkills[0];
            }

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
                case EnemyIntentType.SingleAttack:
                    return characterClass == CharacterClassType.Archer
                        ? $"BẮN TỈA ~{PlannedDamagePreview}"
                        : $"ĐÁNH ~{PlannedDamagePreview}";
                case EnemyIntentType.DebuffAttack: return $"ĐỘC/ÁM ~{PlannedDamagePreview}";
                case EnemyIntentType.AoEAttack:    return $"DIỆN RỘNG ~{PlannedDamagePreview}";
                case EnemyIntentType.Buff:
                    return PlannedSkill != null && PlannedSkill.IsHealSkill
                        ? "HỒI PHỤC / HỘ THÂN"
                        : "TỤ LỰC / THỦ";
                default:                           return string.Empty;
            }
        }
    }
}
