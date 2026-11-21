using TMPro;
using UnityEngine;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Chữ sát thương / hồi máu / trạng thái bay lên trên màn hình trận đấu.
    /// </summary>
    public class FloatingCombatText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float duration = 1.15f;
        [SerializeField] private float risePixels = 72f;

        private RectTransform _rect;
        private Vector2 _startAnchoredPos;
        private float _elapsed;
        private float _peakScale = 1.25f;

        public void Setup(TextMeshProUGUI tmp, CanvasGroup group)
        {
            label = tmp;
            canvasGroup = group;
            _rect = GetComponent<RectTransform>();
        }

        public void Play(string message, Color color, Vector2 anchoredPos, float fontSize = 30f, float peakScale = 1.28f)
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            _startAnchoredPos = anchoredPos;
            _rect.anchoredPosition = anchoredPos;
            _elapsed = 0f;
            _peakScale = peakScale;

            if (label != null)
            {
                label.text = message;
                label.color = color;
                label.fontSize = fontSize;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            transform.localScale = Vector3.one * 0.65f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.1f, duration));

            // Bật nảy nhanh ở đầu, trôi lên mượt mà và mờ dần ở cuối
            float popT = Mathf.Clamp01(_elapsed / 0.16f);
            float scale = popT < 1f
                ? Mathf.Lerp(0.65f, _peakScale, Mathf.Sin(popT * Mathf.PI * 0.5f))
                : Mathf.Lerp(_peakScale, 1.0f, Mathf.Clamp01((_elapsed - 0.16f) / 0.25f));
            transform.localScale = Vector3.one * scale;

            float easeOut = 1f - Mathf.Pow(1f - t, 2.2f);
            _rect.anchoredPosition = _startAnchoredPos + new Vector2(0f, risePixels * easeOut);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            }

            if (_elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
