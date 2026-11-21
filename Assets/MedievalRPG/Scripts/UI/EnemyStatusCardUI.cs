using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Combat;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Thẻ thông tin Quái vật / Boss ở góc trên bên phải với thanh HP mượt mà và hiệu ứng tụt máu.
    /// </summary>
    public class EnemyStatusCardUI : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image activeGlowBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Image hpTrailImage;
        [SerializeField] private TextMeshProUGUI hpValueText;
        [SerializeField] private GameObject defeatedBadge;

        private Unit _boundEnemy;
        private float _displayedHpRatio = 1f;
        private float _trailHpRatio = 1f;
        private bool _isEnemyTurn;

        public void Configure(
            Image glowBorder, Image portrait,
            TextMeshProUGUI enemyName, TextMeshProUGUI stats,
            Image hpFill, Image hpTrail, TextMeshProUGUI hpVal,
            GameObject deadBadge)
        {
            activeGlowBorder = glowBorder;
            portraitImage = portrait;
            nameText = enemyName;
            statsText = stats;
            hpFillImage = hpFill;
            hpTrailImage = hpTrail;
            hpValueText = hpVal;
            defeatedBadge = deadBadge;
        }

        public void Bind(Unit enemy, Sprite portraitSprite)
        {
            _boundEnemy = enemy;
            if (enemy == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (portraitImage != null && portraitSprite != null) portraitImage.sprite = portraitSprite;
            if (nameText != null) nameText.text = enemy.unitName;

            _displayedHpRatio = enemy.maxHP > 0 ? (float)enemy.currentHP / enemy.maxHP : 0f;
            _trailHpRatio = _displayedHpRatio;
            Refresh(false);
        }

        public void Refresh(bool isEnemyTurn)
        {
            _isEnemyTurn = isEnemyTurn;
            if (activeGlowBorder != null) activeGlowBorder.enabled = isEnemyTurn;
            if (_boundEnemy == null) return;

            bool isDead = _boundEnemy.currentHP <= 0;
            if (defeatedBadge != null) defeatedBadge.SetActive(isDead);
            if (portraitImage != null)
            {
                portraitImage.color = isDead ? new Color(0.45f, 0.45f, 0.48f, 0.85f) : Color.white;
            }
            if (hpValueText != null)
            {
                hpValueText.text = $"{_boundEnemy.currentHP} / {_boundEnemy.maxHP} HP";
            }
            if (statsText != null)
            {
                statsText.text = $"Cấp {_boundEnemy.unitLevel}  •  ATK {_boundEnemy.damage}  •  DEF {_boundEnemy.EffectiveDefense}";
            }
        }

        private void Update()
        {
            if (_boundEnemy == null) return;

            float targetHp = _boundEnemy.maxHP > 0 ? Mathf.Clamp01((float)_boundEnemy.currentHP / _boundEnemy.maxHP) : 0f;
            _displayedHpRatio = Mathf.Lerp(_displayedHpRatio, targetHp, Time.deltaTime * 10f);
            _trailHpRatio = targetHp < _trailHpRatio
                ? Mathf.Lerp(_trailHpRatio, targetHp, Time.deltaTime * 3.2f)
                : _displayedHpRatio;

            if (hpFillImage != null) hpFillImage.fillAmount = _displayedHpRatio;
            if (hpTrailImage != null) hpTrailImage.fillAmount = _trailHpRatio;

            if (_isEnemyTurn && activeGlowBorder != null)
            {
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5.5f);
                Color c = MedievalUITheme.DangerRed;
                c.a = pulse;
                activeGlowBorder.color = c;
            }
        }
    }
}
