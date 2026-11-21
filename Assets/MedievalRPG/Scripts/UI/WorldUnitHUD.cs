using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Combat;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Thanh máu mini & mũi tên báo hiệu lượt ngay phía trên từng nhân vật trên chiến trường.
    /// Hiển thị trên Canvas chính theo tọa độ màn hình để giữ độ sắc nét tuyệt đối.
    /// </summary>
    public class WorldUnitHUD : MonoBehaviour
    {
        [SerializeField] private Image hpFillImage;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private GameObject turnArrowRoot;
        [SerializeField] private GameObject defendBadgeRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        private Unit _unit;
        private RectTransform _rect;
        private RectTransform _parentRect;
        private Canvas _rootCanvas;
        private float _headOffsetWorld = 0.95f;
        private bool _isTurnActive;
        private Vector2 _arrowBaseAnchoredPos;
        private RectTransform _arrowRect;

        public void Configure(
            Image hpFill, TextMeshProUGUI nameTxt,
            GameObject turnArrow, GameObject defendBadge, CanvasGroup cg)
        {
            hpFillImage = hpFill;
            nameLabel = nameTxt;
            turnArrowRoot = turnArrow;
            defendBadgeRoot = defendBadge;
            canvasGroup = cg;
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
                hpFillImage.color = unit.isEnemy ? MedievalUITheme.DangerRed : MedievalUITheme.SuccessGreen;
            }

            gameObject.SetActive(true);
            Refresh(false);
        }

        public void Refresh(bool isActiveTurn)
        {
            _isTurnActive = isActiveTurn;
            if (turnArrowRoot != null) turnArrowRoot.SetActive(isActiveTurn && _unit != null && _unit.currentHP > 0);
            if (defendBadgeRoot != null && _unit != null)
            {
                defendBadgeRoot.SetActive(_unit.currentHP > 0 && (_unit.isDefending || _unit.bonusDefense > 0));
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
                hpFillImage.fillAmount = Mathf.Lerp(hpFillImage.fillAmount, targetFill, Time.deltaTime * 12f);
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
