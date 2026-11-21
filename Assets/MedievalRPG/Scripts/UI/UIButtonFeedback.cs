using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Hiệu ứng phản hồi mượt mà khi di chuột và bấm nút (chạy theo unscaledDeltaTime).
    /// Hỗ trợ callback hover để hiển thị mô tả kỹ năng / hành động rõ ràng.
    /// </summary>
    public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button targetButton;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressScale = 0.96f;
        [SerializeField] private float lerpSpeed = 16f;

        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private bool _isHovered;
        private bool _isPressed;
        private Vector3 _initialScale = Vector3.one;

        public string HoverTitle { get; set; }
        public string HoverDescription { get; set; }
        public event Action<UIButtonFeedback, bool> OnHoverChanged;

        private void Awake()
        {
            if (targetButton == null) targetButton = GetComponent<Button>();
            if (targetGraphic == null && targetButton != null) targetGraphic = targetButton.targetGraphic;
            if (targetGraphic == null) targetGraphic = GetComponent<Image>();

            _initialScale = transform.localScale;
            if (targetGraphic != null)
            {
                _baseColor = targetGraphic.color;
                _hasBaseColor = true;
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isPressed = false;
            transform.localScale = _initialScale;
        }

        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            _hasBaseColor = true;
            if (targetGraphic != null)
            {
                targetGraphic.color = color;
            }
        }

        private void Update()
        {
            bool interactable = targetButton == null || targetButton.interactable;
            float targetScaleFactor = 1f;
            if (interactable)
            {
                if (_isPressed) targetScaleFactor = pressScale;
                else if (_isHovered) targetScaleFactor = hoverScale;
            }

            Vector3 targetScale = _initialScale * targetScaleFactor;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * lerpSpeed);

            if (_hasBaseColor && targetGraphic != null)
            {
                Color desired = _baseColor;
                if (!interactable)
                {
                    desired = Color.Lerp(_baseColor, MedievalUITheme.BtnDisabled, 0.72f);
                    desired.a = 0.75f;
                }
                else if (_isPressed)
                {
                    desired = _baseColor * 0.85f;
                    desired.a = _baseColor.a;
                }
                else if (_isHovered)
                {
                    desired = Color.Lerp(_baseColor, Color.white, 0.16f);
                    desired.a = _baseColor.a;
                }

                targetGraphic.color = Color.Lerp(targetGraphic.color, desired, Time.unscaledDeltaTime * lerpSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            OnHoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            OnHoverChanged?.Invoke(this, false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (targetButton != null && !targetButton.interactable) return;
            _isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
        }
    }
}
