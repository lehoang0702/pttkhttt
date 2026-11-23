using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MedievalRPG.Combat;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Thẻ thông tin Quái vật / Boss đang được chọn mục tiêu (kèm trạng thái, Khiên Boss & hiệu ứng HP mượt).
    /// </summary>
    public class EnemyStatusCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image activeGlowBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Image hpTrailImage;
        [SerializeField] private TextMeshProUGUI hpValueText;
        [SerializeField] private GameObject defeatedBadge;
        [SerializeField] private GameObject statusPillRoot;
        [SerializeField] private TextMeshProUGUI statusPillText;

        private Unit _boundEnemy;
        private float _displayedHpRatio = 1f;
        private float _trailHpRatio = 1f;
        private bool _isEnemyTurn;
        private bool _isHovered;

        public event Action<Unit, bool, Vector3> OnPointerHoverChanged;

        public void Configure(
            Image glowBorder, Image portrait,
            TextMeshProUGUI enemyName, TextMeshProUGUI stats,
            Image hpFill, Image hpTrail, TextMeshProUGUI hpVal,
            GameObject deadBadge, GameObject statusRoot = null, TextMeshProUGUI statusTxt = null)
        {
            activeGlowBorder = glowBorder;
            portraitImage = portrait;
            nameText = enemyName;
            statsText = stats;
            hpFillImage = hpFill;
            hpTrailImage = hpTrail;
            hpValueText = hpVal;
            defeatedBadge = deadBadge;
            statusPillRoot = statusRoot;
            statusPillText = statusTxt;
        }

        public void Bind(Unit enemy, Sprite portraitSprite)
        {
            bool changedTarget = _boundEnemy != enemy;
            _boundEnemy = enemy;
            if (enemy == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (portraitImage != null && portraitSprite != null) portraitImage.sprite = portraitSprite;
            if (nameText != null) nameText.text = enemy.unitName;

            if (changedTarget)
            {
                _displayedHpRatio = enemy.maxHP > 0 ? (float)enemy.currentHP / enemy.maxHP : 0f;
                _trailHpRatio = _displayedHpRatio;
            }

            Refresh(false);
        }

        public static string FormatAttackStatLabel(int physicalDamage, int magicDamage)
        {
            if (magicDamage > physicalDamage) return $"MAG {magicDamage}";
            if (magicDamage >= 12 && physicalDamage >= 12) return $"ATK {physicalDamage}/MAG {magicDamage}";
            return $"ATK {physicalDamage}";
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
                string atkLabel = FormatAttackStatLabel(_boundEnemy.damage, _boundEnemy.magicDamage);
                statsText.text = $"Cấp {_boundEnemy.unitLevel} • {atkLabel} • DEF {_boundEnemy.EffectiveDefense} • SPD {_boundEnemy.speed}";
            }

            if (statusPillRoot != null && statusPillText != null)
            {
                if (_boundEnemy.ShadowShieldStacks > 0)
                {
                    statusPillRoot.SetActive(true);
                    statusPillText.text = $"KHIÊN TRI THỨC: {_boundEnemy.ShadowShieldStacks}";
                }
                else if (_boundEnemy.ActiveStatuses.Count > 0)
                {
                    statusPillRoot.SetActive(true);
                    statusPillText.text = _boundEnemy.ActiveStatuses[0].GetShortBadgeLabel();
                }
                else if (_boundEnemy.bonusDefense > 0)
                {
                    statusPillRoot.SetActive(true);
                    statusPillText.text = $"+{_boundEnemy.bonusDefense} GIÁP";
                }
                else
                {
                    statusPillRoot.SetActive(false);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_boundEnemy == null) return;
            _isHovered = true;
            OnPointerHoverChanged?.Invoke(_boundEnemy, true, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovered && _boundEnemy == null) return;
            _isHovered = false;
            OnPointerHoverChanged?.Invoke(_boundEnemy, false, transform.position);
        }

        private void OnDisable()
        {
            if (_isHovered)
            {
                _isHovered = false;
                OnPointerHoverChanged?.Invoke(_boundEnemy, false, transform.position);
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
