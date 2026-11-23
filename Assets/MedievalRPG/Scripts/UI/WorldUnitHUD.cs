using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Combat;
using MedievalRPG.Data;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Thanh máu mini, mũi tên lượt, vòng chọn mục tiêu và dự báo hành động quái (Enemy Intent) trên chiến trường.
    /// </summary>
    public class WorldUnitHUD : MonoBehaviour
    {
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Image hpTrailImage;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private GameObject turnArrowRoot;
        [SerializeField] private GameObject defendBadgeRoot;
        [SerializeField] private GameObject targetReticleRoot;
        [SerializeField] private GameObject intentPillRoot;
        [SerializeField] private TextMeshProUGUI intentText;
        [SerializeField] private Image intentBackground;
        [SerializeField] private CanvasGroup canvasGroup;

        private Unit _unit;
        private RectTransform _rect;
        private RectTransform _parentRect;
        private Canvas _rootCanvas;
        private float _headOffsetWorld = 0.95f;
        private float _displayedHpRatio = 1f;
        private float _trailHpRatio = 1f;
        private bool _isTurnActive;
        private Vector2 _arrowBaseAnchoredPos;
        private RectTransform _arrowRect;

        public void ConfigureTrailImage(Image trailImg)
        {
            hpTrailImage = trailImg;
        }

        public void Configure(
            Image hpFill, TextMeshProUGUI nameTxt,
            GameObject turnArrow, GameObject defendBadge, CanvasGroup cg,
            GameObject targetReticle = null, GameObject intentRoot = null,
            TextMeshProUGUI intentLabel = null, Image intentBg = null)
        {
            hpFillImage = hpFill;
            nameLabel = nameTxt;
            turnArrowRoot = turnArrow;
            defendBadgeRoot = defendBadge;
            canvasGroup = cg;
            targetReticleRoot = targetReticle;
            intentPillRoot = intentRoot;
            intentText = intentLabel;
            intentBackground = intentBg;

            if (turnArrowRoot != null)
            {
                _arrowRect = turnArrowRoot.GetComponent<RectTransform>();
                _arrowBaseAnchoredPos = _arrowRect.anchoredPosition;
            }
        }

        public void Bind(Unit unit, RectTransform parentRect, float headOffsetY)
        {
            _unit = unit;
            _parentRect = parentRect;
            _headOffsetWorld = headOffsetY;
            _rect = GetComponent<RectTransform>();
            _rootCanvas = GetComponentInParent<Canvas>();

            if (unit == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (nameLabel != null)
            {
                nameLabel.text = unit.isEnemy ? unit.unitName : MedievalUITheme.FormatHeroDisplayName(unit.unitName);
            }
            if (hpFillImage != null)
            {
                EnsureTrailImageExists();
                hpFillImage.color = unit.isEnemy ? MedievalUITheme.DangerRed : MedievalUITheme.SuccessGreen;
                float initRatio = unit.maxHP > 0 ? Mathf.Clamp01((float)unit.currentHP / unit.maxHP) : 0f;
                _displayedHpRatio = initRatio;
                _trailHpRatio = initRatio;
                hpFillImage.fillAmount = initRatio;
                if (hpTrailImage != null) hpTrailImage.fillAmount = initRatio;
            }

            gameObject.SetActive(true);
            Refresh(false, false);
        }

        private void EnsureTrailImageExists()
        {
            if (hpTrailImage != null || hpFillImage == null) return;
            Transform barFrame = hpFillImage.transform.parent;
            if (barFrame == null) return;

            Transform existing = barFrame.Find("MiniHPTrailFill");
            if (existing != null && existing.TryGetComponent<Image>(out var exImg))
            {
                hpTrailImage = exImg;
                return;
            }

            GameObject go = new GameObject("MiniHPTrailFill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(barFrame, false);
            int fillIdx = hpFillImage.transform.GetSiblingIndex();
            go.transform.SetSiblingIndex(Mathf.Max(0, fillIdx));

            hpTrailImage = go.GetComponent<Image>();
            hpTrailImage.sprite = hpFillImage.sprite;
            hpTrailImage.type = Image.Type.Filled;
            hpTrailImage.fillMethod = Image.FillMethod.Horizontal;
            hpTrailImage.color = MedievalUITheme.HpTrailGold;
            hpTrailImage.raycastTarget = false;

            RectTransform rt = hpTrailImage.rectTransform;
            RectTransform fillRt = hpFillImage.rectTransform;
            rt.anchorMin = fillRt.anchorMin;
            rt.anchorMax = fillRt.anchorMax;
            rt.pivot = fillRt.pivot;
            rt.offsetMin = fillRt.offsetMin;
            rt.offsetMax = fillRt.offsetMax;
        }

        public void Refresh(bool isActiveTurn, bool isTargeted = false)
        {
            _isTurnActive = isActiveTurn;
            bool alive = _unit != null && _unit.currentHP > 0;

            if (turnArrowRoot != null) turnArrowRoot.SetActive(isActiveTurn && alive);
            if (defendBadgeRoot != null)
            {
                defendBadgeRoot.SetActive(alive && (_unit.isDefending || _unit.bonusDefense > 0 || _unit.ShadowShieldStacks > 0));
            }
            if (targetReticleRoot != null)
            {
                targetReticleRoot.SetActive(alive && isTargeted);
            }

            if (intentPillRoot != null && intentText != null)
            {
                if (alive && _unit.isEnemy)
                {
                    string intentStr = _unit.GetIntentDisplayText();
                    bool show = !string.IsNullOrEmpty(intentStr);
                    intentPillRoot.SetActive(show);
                    if (show)
                    {
                        intentText.text = _unit.ShadowShieldStacks > 0
                            ? $"{intentStr} • KHIÊN {_unit.ShadowShieldStacks}"
                            : intentStr;
                        if (intentBackground != null)
                        {
                            intentBackground.color = _unit.IsStunned
                                ? MedievalUITheme.BtnPrimaryGold
                                : (_unit.PlannedIntent == EnemyIntentType.Buff ? MedievalUITheme.BtnDefend : MedievalUITheme.DangerPanel);
                        }
                    }
                }
                else if (alive && _unit.ActiveStatuses.Count > 0)
                {
                    intentPillRoot.SetActive(true);
                    intentText.text = _unit.ActiveStatuses[0].GetShortBadgeLabel();
                    if (intentBackground != null) intentBackground.color = MedievalUITheme.BtnDefend;
                }
                else
                {
                    intentPillRoot.SetActive(false);
                }
            }
        }

        private void LateUpdate()
        {
            if (_unit == null || _parentRect == null || Camera.main == null)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                return;
            }

            bool alive = _unit.currentHP > 0;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alive ? 1f : 0f;
            }
            if (!alive) return;

            if (hpFillImage != null)
            {
                float targetFill = _unit.maxHP > 0 ? Mathf.Clamp01((float)_unit.currentHP / _unit.maxHP) : 0f;
                _displayedHpRatio = Mathf.Lerp(_displayedHpRatio, targetFill, Time.deltaTime * 12f);
                _trailHpRatio = targetFill < _trailHpRatio
                    ? Mathf.Lerp(_trailHpRatio, targetFill, Time.deltaTime * 3.0f)
                    : _displayedHpRatio;
                hpFillImage.fillAmount = _displayedHpRatio;
                if (!_unit.isEnemy)
                {
                    hpFillImage.color = targetFill < 0.30f ? MedievalUITheme.HpWarning : MedievalUITheme.SuccessGreen;
                }
                if (hpTrailImage != null)
                {
                    hpTrailImage.fillAmount = _trailHpRatio;
                }
            }

            Vector3 worldPos = _unit.transform.position + new Vector3(0f, _headOffsetWorld, 0f);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            Camera uiCam = (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _rootCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPos, uiCam, out Vector2 localPos))
            {
                _rect.anchoredPosition = localPos;
            }

            if (_isTurnActive && _arrowRect != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 6f) * 5f;
                _arrowRect.anchoredPosition = _arrowBaseAnchoredPos + new Vector2(0f, bob);
            }
        }
    }
}
