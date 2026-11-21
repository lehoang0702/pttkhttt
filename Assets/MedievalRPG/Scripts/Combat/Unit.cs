using System;
using System.Collections.Generic;
using UnityEngine;
using MedievalRPG.Data;
using MedievalRPG.Visuals;

namespace MedievalRPG.Combat
{
    /// <summary>
    /// [Lớp Thực Thể Chiến Đấu - Unit]
    /// Kế thừa và mở rộng từ kiến trúc Turn-Based Combat kinh điển của Brackeys.
    /// Đại diện cho 1 nhân vật trong trận chiến (Hiệp sĩ Knight, Pháp sư Mage, Cung thủ Archer, hoặc Quái vật/Boss).
    /// Quản lý toàn bộ chỉ số sinh tồn (HP, MP), tấn công, phòng thủ, và kích hoạt hoạt ảnh (Animation).
    /// </summary>
    public class Unit : MonoBehaviour
    {
        [Header("--- Thuộc tính cơ bản (Brackeys) ---")]
        [Tooltip("Tên hiển thị của nhân vật trên giao diện HUD")]
        public string unitName = "Anh Hùng";

        [Tooltip("Cấp độ nhân vật")]
        public int unitLevel = 1;

        [Tooltip("Sức tấn công vật lý cơ bản")]
        public int damage = 20;

        [Tooltip("Lượng máu tối đa (Max HP)")]
        public int maxHP = 100;

        [Tooltip("Lượng máu hiện tại (Current HP)")]
        public int currentHP = 100;

        [Header("--- Chỉ số mở rộng phong cách RPG Medieval ---")]
        [Tooltip("Hệ phái nhân vật (Knight / Mage / Archer / Goblin / Boss)")]
        public CharacterClassType characterClass = CharacterClassType.Knight;

        [Tooltip("Năng lượng phép tối đa (Max MP) dùng để thi triển kỹ năng")]
        public int maxMP = 50;

        [Tooltip("Năng lượng phép hiện tại")]
        public int currentMP = 50;

        [Tooltip("Điểm phòng thủ vật lý (Giảm sát thương nhận vào)")]
        public int defense = 10;

        [Tooltip("Sức tấn công phép thuật")]
        public int magicDamage = 5;

        [Tooltip("Tốc độ hành động (quyết định thứ tự lượt đánh)")]
        public int speed = 10;

        [Range(0f, 1f)]
        [Tooltip("Tỉ lệ đánh chí mạng (Ví dụ: 0.25 = 25%)")]
        public float critRate = 0.05f;

        [Tooltip("Hệ số nhân sát thương khi đánh chí mạng (Ví dụ: 1.5 = x1.5 sát thương)")]
        public float critMultiplier = 1.5f;

        [Header("--- Trạng thái chiến đấu ---")]
        [Tooltip("Đang trong tư thế thủ (Giảm 50% sát thương nhận vào)")]
        public bool isDefending = false;

        [Tooltip("Giáp cộng thêm tạm thời từ kỹ năng")]
        public int bonusDefense = 0;

        [Tooltip("Số lượt còn lại của hiệu ứng tăng giáp")]
        public int defenseBuffTurns = 0;

        [Tooltip("Đánh dấu đây là quái vật / kẻ địch")]
        public bool isEnemy = false;

        public int EffectiveDefense => defense + bonusDefense;
        public int LastDamageTaken { get; private set; }
        public event Action<Unit> OnStatsChanged;

        [Header("--- Tham chiếu thành phần & Dữ liệu ---")]
        public CharacterClassData classData;
        public CharacterVisualController visual;
        public PixelCharacterAnimator pixelAnimator;
        public List<SkillData> skills = new List<SkillData>();

        private void Awake()
        {
            // Tự động tìm kiếm bộ điều khiển hình ảnh và hoạt ảnh nếu chưa gán
            if (visual == null) visual = GetComponent<CharacterVisualController>();
            if (pixelAnimator == null) pixelAnimator = GetComponent<PixelCharacterAnimator>();

            // Nạp chỉ số từ ScriptableObject nếu có
            if (classData != null)
            {
                InitializeFromClass(classData);
            }
        }

        /// <summary>
        /// Khởi tạo toàn bộ chỉ số nhân vật từ tệp dữ liệu CharacterClassData
        /// </summary>
        public void InitializeFromClass(CharacterClassData data)
        {
            if (data == null) return;
            classData = data;
            unitName = data.className;
            characterClass = data.classType;
            maxHP = data.maxHealth;
            currentHP = maxHP;
            maxMP = data.maxMana;
            currentMP = maxMP;
            damage = data.physicalAttack;
            magicDamage = data.magicAttack;
            defense = data.physicalDefense;
            speed = data.speed;
            critRate = data.critRate;
            critMultiplier = data.critMultiplier;
            skills = new List<SkillData>(data.skills);

            if (visual != null)
            {
                visual.SetupSprites(data);
            }
        }

        /// <summary>
        /// Phương thức nhận sát thương chuẩn Brackeys có áp dụng giáp phòng thủ RPG.
        /// Trả về TRUE nếu nhân vật bị hạ gục (HP <= 0), FALSE nếu còn sống.
        /// </summary>
        /// <param name="rawDmg">Lượng sát thương gốc từ đòn đánh</param>
        public bool TakeDamage(int rawDmg)
        {
            int finalDamage = rawDmg;

            // Công thức tính giảm trừ sát thương từ điểm giáp phòng thủ
            int effectiveDef = EffectiveDefense;
            if (effectiveDef > 0)
            {
                float reduction = 100f / (100f + effectiveDef);
                finalDamage = Mathf.RoundToInt(rawDmg * reduction);
            }

            // Nếu đang bật kỹ năng phòng ngự (Defend) -> giảm tiếp 50% sát thương
            if (isDefending)
            {
                finalDamage = Mathf.Max(1, finalDamage / 2);
            }

            // Sát thương tối thiểu luôn là 1 để tránh đánh không mất máu
            finalDamage = Mathf.Max(1, finalDamage);
            LastDamageTaken = finalDamage;
            currentHP -= finalDamage;

            // Kích hoạt hoạt ảnh bị trúng đòn (Hurt/Hit)
            if (pixelAnimator != null)
            {
                pixelAnimator.PlayHit();
            }
            else if (visual != null)
            {
                visual.PlayHitAnimation();
            }

            // Kiểm tra nhân vật đã chết chưa
            if (currentHP <= 0)
            {
                currentHP = 0;
                OnStatsChanged?.Invoke(this);
                // Kích hoạt hoạt ảnh gục ngã (Death)
                if (pixelAnimator != null)
                {
                    pixelAnimator.PlayDeath();
                }
                else if (visual != null)
                {
                    visual.PlayDeathAnimation();
                }
                return true; // Đã tử trận
            }

            OnStatsChanged?.Invoke(this);
            return false; // Vẫn còn sống
        }

        /// <summary>
        /// Hồi máu cho nhân vật (Không vượt quá Max HP)
        /// </summary>
        public void Heal(int amount)
        {
            currentHP += amount;
            if (currentHP > maxHP)
            {
                currentHP = maxHP;
            }
            OnStatsChanged?.Invoke(this);
        }

        /// <summary>
        /// Tiêu hao năng lượng Mana để dùng kỹ năng
        /// </summary>
        public bool UseMana(int amount)
        {
            if (currentMP < amount) return false; // Không đủ Mana
            currentMP -= amount;
            OnStatsChanged?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Hồi phục năng lượng Mana
        /// </summary>
        public void RestoreMana(int amount)
        {
            currentMP = Mathf.Min(maxMP, currentMP + amount);
            OnStatsChanged?.Invoke(this);
        }

        /// <summary>
        /// Tăng giáp tạm thời trong một số lượt
        /// </summary>
        public void ApplyDefenseBuff(int bonus, int turns)
        {
            bonusDefense = Mathf.Max(bonusDefense, bonus);
            defenseBuffTurns = Mathf.Max(defenseBuffTurns, turns);
            OnStatsChanged?.Invoke(this);
        }

        /// <summary>
        /// Xử lý logic khi bắt đầu lượt mới của nhân vật này
        /// </summary>
        public void OnTurnStart()
        {
            isDefending = false; // Hết trạng thái phòng thủ của lượt trước
            if (defenseBuffTurns > 0)
            {
                defenseBuffTurns--;
                if (defenseBuffTurns == 0)
                {
                    bonusDefense = 0;
                }
            }
            RestoreMana(4);      // Tự động hồi một lượng nhỏ Mana mỗi lượt
        }
    }
}
