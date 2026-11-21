using UnityEngine;
using UnityEngine.UI;

namespace MedievalRPG.Combat
{
    /// <summary>
    /// [Bảng Hiển Thị Thông Số Nhân Vật - BattleHUD]
    /// Dựa trên kiến trúc chuẩn của Brackeys, bổ sung thêm thanh năng lượng (MP)
    /// và chữ hiển thị số máu/mana chi tiết.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        [Header("--- Các thành phần cơ bản (Brackeys) ---")]
        [Tooltip("Chữ hiển thị tên nhân vật")]
        public Text nameText;

        [Tooltip("Chữ hiển thị cấp độ nhân vật")]
        public Text levelText;

        [Tooltip("Thanh trượt hiển thị máu (HP Slider)")]
        public Slider hpSlider;

        [Header("--- Mở rộng RPG Trung Cổ ---")]
        [Tooltip("Thanh trượt hiển thị năng lượng (MP Slider)")]
        public Slider mpSlider;

        [Tooltip("Hiển thị chỉ số máu dạng số: Ví dụ '120 / 150 HP'")]
        public Text hpText;

        [Tooltip("Hiển thị chỉ số năng lượng dạng số: Ví dụ '40 / 50 MP'")]
        public Text mpText;

        /// <summary>
        /// Khởi tạo và cập nhật toàn bộ thông tin của nhân vật lên HUD
        /// </summary>
        public void SetHUD(Unit unit)
        {
            if (unit == null) return;

            if (nameText != null)
                nameText.text = unit.unitName;

            if (levelText != null)
                levelText.text = "Lv." + unit.unitLevel;

            if (hpSlider != null)
            {
                hpSlider.maxValue = unit.maxHP;
                hpSlider.value = unit.currentHP;
            }

            if (mpSlider != null)
            {
                mpSlider.maxValue = unit.maxMP;
                mpSlider.value = unit.currentMP;
            }

            UpdateTextValues(unit);
        }

        /// <summary>
        /// Cập nhật thanh máu khi bị nhận sát thương hoặc được hồi phục
        /// </summary>
        public void SetHP(int hp, int maxHp = 0)
        {
            if (hpSlider != null)
            {
                hpSlider.value = hp;
            }

            if (hpText != null)
            {
                int max = maxHp > 0 ? maxHp : (int)hpSlider.maxValue;
                hpText.text = $"{hp} / {max} HP";
            }
        }

        /// <summary>
        /// Cập nhật thanh năng lượng khi dùng kỹ năng
        /// </summary>
        public void SetMP(int mp, int maxMp = 0)
        {
            if (mpSlider != null)
            {
                mpSlider.value = mp;
            }

            if (mpText != null)
            {
                int max = maxMp > 0 ? maxMp : (int)mpSlider.maxValue;
                mpText.text = $"{mp} / {max} MP";
            }
        }

        /// <summary>
        /// Đồng bộ hiển thị chữ số
        /// </summary>
        private void UpdateTextValues(Unit unit)
        {
            if (hpText != null)
                hpText.text = $"{unit.currentHP} / {unit.maxHP} HP";

            if (mpText != null)
                mpText.text = $"{unit.currentMP} / {unit.maxMP} MP";
        }
    }
}
