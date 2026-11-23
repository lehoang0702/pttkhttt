using TMPro;
using UnityEngine;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Quản lý tạo hiệu ứng chữ nổi (Damage / Crit / Heal / Shield) trên tầng UI trận đấu.
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private TMP_FontAsset fontAsset;

        public void OverrideFont(TMP_FontAsset font)
        {
            if (font != null) fontAsset = font;
        }

        public void Initialize(RectTransform container, TMP_FontAsset font)
        {
            containerRect = container;
            fontAsset = font;
        }

        public void SpawnAtWorldPosition(Vector3 worldPos, string text, Color color, float fontSize = 30f, float peakScale = 1.28f)
        {
            if (containerRect == null || Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos + new Vector3(0f, 0.85f, 0f));
            if (screenPos.z < 0f) return;

            Canvas rootCanvas = containerRect.GetComponentInParent<Canvas>();
            Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? rootCanvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, screenPos, uiCam, out Vector2 localPos))
            {
                return;
            }

            // Thêm lệch ngang nhẹ để hai chữ liên tiếp không đè lên nhau
            localPos.x += UnityEngine.Random.Range(-18f, 18f);

            GameObject go = new GameObject("FloatingText", typeof(RectTransform), typeof(CanvasGroup), typeof(FloatingCombatText));
            go.transform.SetParent(containerRect, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 64f);

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(9, 12, 18, 255);

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            FloatingCombatText fct = go.GetComponent<FloatingCombatText>();
            fct.Setup(tmp, cg);
            fct.Play(text, color, localPos, fontSize, peakScale);
        }
    }
}
