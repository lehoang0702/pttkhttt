using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Combat;
using MedievalRPG.Data;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Thẻ hiển thị trạng thái từng anh hùng (có thể bấm chọn làm mục tiêu hồi phục / vật phẩm).
    /// </summary>
    public class PartyMemberCardUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Thành phần hiển thị")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image activeGlowBorder;
        [SerializeField] private Image selectedAllyBorder;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image classIconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI statusBadgeText;
        [SerializeField] private GameObject statusBadgeRoot;
        [SerializeField] private GameObject activeTurnBadge;
        [SerializeField] private GameObject deadOverlay;

        [Header("Thanh máu & năng lượng")]
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Image hpTrailImage;
        [SerializeField] private TextMeshProUGUI hpValueText;
        [SerializeField] private Image mpFillImage;
        [SerializeField] private TextMeshProUGUI mpValueText;

        private Unit _boundUnit;
        private float _displayedHpRatio = 1f;
        private float _trailHpRatio = 1f;
        private float _displayedMpRatio = 1f;
        private bool _isCurrentTurn;

        public event Action<Unit> OnCardClicked;

        public void Configure(
            Image bg, Image glowBorder, Image selectedBorder, Image portrait, Image classIcon,
            TextMeshProUGUI heroName, TextMeshProUGUI heroLv,
            GameObject badgeRoot, TextMeshProUGUI badgeText,
            GameObject turnBadge, GameObject deadCover,
            Image hpFill, Image hpTrail, TextMeshProUGUI hpVal,
            Image mpFill, TextMeshProUGUI mpVal)
        {
            cardBackground = bg;
            activeGlowBorder = glowBorder;
            selectedAllyBorder = selectedBorder;
            portraitImage = portrait;
            classIconImage = classIcon;
            nameText = heroName;
            levelText = heroLv;
            statusBadgeRoot = badgeRoot;
            statusBadgeText = badgeText;
            activeTurnBadge = turnBadge;
            deadOverlay = deadCover;
            hpFillImage = hpFill;
            hpTrailImage = hpTrail;
            hpValueText = hpVal;
            mpFillImage = mpFill;
            mpValueText = mpVal;
        }

        public void Bind(Unit unit, Sprite portraitSprite, Sprite classIconSprite)
        {
            _boundUnit = unit;
            if (unit == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (cardBackground != null) cardBackground.raycastTarget = true;
            if (portraitImage != null && portraitSprite != null) portraitImage.sprite = portraitSprite;
            if (classIconImage != null && classIconSprite != null) classIconImage.sprite = classIconSprite;
            if (nameText != null) nameText.text = MedievalUITheme.FormatHeroDisplayName(unit.unitName);
            if (levelText != null) levelText.text = $"Lv.{unit.unitLevel}";

            _displayedHpRatio = unit.maxHP > 0 ? (float)unit.currentHP / unit.maxHP : 0f;
            _trailHpRatio = _displayedHpRatio;
            _displayedMpRatio = unit.maxMP > 0 ? (float)unit.currentMP / unit.maxMP : 0f;

            RefreshImmediate(false, false);
        }

        public void SetActiveTurn(bool isTurn)
        {
            _isCurrentTurn = isTurn;
            if (activeGlowBorder != null) activeGlowBorder.enabled = isTurn;
            if (activeTurnBadge != null) activeTurnBadge.SetActive(isTurn);
        }

        public void RefreshImmediate(bool isTurn, bool isSelectedAlly = false)
        {
            SetActiveTurn(isTurn);
            if (selectedAllyBorder != null) selectedAllyBorder.enabled = isSelectedAlly && !isTurn;
            if (_boundUnit == null) return;

            bool isDead = _boundUnit.currentHP <= 0;
            if (deadOverlay != null) deadOverlay.SetActive(isDead);
            if (portraitImage != null)
            {
                portraitImage.color = isDead ? new Color(0.42f, 0.42f, 0.46f, 0.85f) : Color.white;
            }

            if (hpValueText != null) hpValueText.text = $"HP {_boundUnit.currentHP}/{_boundUnit.maxHP}";
            if (mpValueText != null) mpValueText.text = $"MP {_boundUnit.currentMP}/{_boundUnit.maxMP}";

            UpdateStatusBadge(isDead);
        }

        private void UpdateStatusBadge(bool isDead)
        {
            if (statusBadgeRoot == null || statusBadgeText == null) return;

            if (isDead)
            {
                statusBadgeRoot.SetActive(true);
                statusBadgeText.text = "GỤC NGÃ";
                return;
            }

            if (_boundUnit.HasTaunt)
            {
                statusBadgeRoot.SetActive(true);
                statusBadgeText.text = "KHIÊU KHÍCH";
            }
            else if (_boundUnit.isDefending)
            {
                statusBadgeRoot.SetActive(true);
                statusBadgeText.text = "THỦ -50%";
            }
            else if (_boundUnit.ActiveStatuses.Count > 0)
            {
                statusBadgeRoot.SetActive(true);
                statusBadgeText.text = _boundUnit.ActiveStatuses[0].GetShortBadgeLabel();
            }
            else if (_boundUnit.bonusDefense > 0)
            {
                statusBadgeRoot.SetActive(true);
                statusBadgeText.text = $"+{_boundUnit.bonusDefense} GIÁP";
            }
            else
            {
                statusBadgeRoot.SetActive(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_boundUnit == null) return;
            AudioManager.Instance?.PlayButtonHover();
            OnCardClicked?.Invoke(_boundUnit);
        }

        private void Update()
        {
            if (_boundUnit == null) return;

            float targetHp = _boundUnit.maxHP > 0 ? Mathf.Clamp01((float)_boundUnit.currentHP / _boundUnit.maxHP) : 0f;
            float targetMp = _boundUnit.maxMP > 0 ? Mathf.Clamp01((float)_boundUnit.currentMP / _boundUnit.maxMP) : 0f;

            _displayedHpRatio = Mathf.Lerp(_displayedHpRatio, targetHp, Time.deltaTime * 10f);
            _trailHpRatio = targetHp < _trailHpRatio
                ? Mathf.Lerp(_trailHpRatio, targetHp, Time.deltaTime * 3.5f)
                : _displayedHpRatio;
            _displayedMpRatio = Mathf.Lerp(_displayedMpRatio, targetMp, Time.deltaTime * 10f);

            if (hpFillImage != null)
            {
                hpFillImage.fillAmount = _displayedHpRatio;
                hpFillImage.color = targetHp < 0.30f ? MedievalUITheme.HpWarning : MedievalUITheme.HpCrimson;
            }
            if (hpTrailImage != null) hpTrailImage.fillAmount = _trailHpRatio;
            if (mpFillImage != null) mpFillImage.fillAmount = _displayedMpRatio;

            if (_isCurrentTurn && activeGlowBorder != null)
            {
                float pulse = 0.72f + 0.28f * Mathf.Sin(Time.unscaledTime * 5f);
                Color c = MedievalUITheme.GoldSoft;
                c.a = pulse;
                activeGlowBorder.color = c;
            }
        }
    }
}
