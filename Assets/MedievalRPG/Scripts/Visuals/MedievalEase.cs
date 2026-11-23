using System;
using System.Collections;
using UnityEngine;

namespace MedievalRPG.Visuals
{
    /// <summary>
    /// Các hàm nội suy (Easing) và Coroutine Tween dùng chung, chạy theo unscaledDeltaTime mặc định
    /// để hiệu ứng UI và milestone vẫn mượt ngay cả khi Time.timeScale = 0 hoặc hitstop.
    /// </summary>
    public static class MedievalEase
    {
        public static float Linear(float t) => Mathf.Clamp01(t);

        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        public static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        public static IEnumerator Tween(
            float duration,
            Action<float> apply,
            Func<float, float> ease = null,
            bool unscaled = true)
        {
            if (apply == null) yield break;
            float safeDur = Mathf.Max(0.001f, duration);
            Func<float, float> easeFn = ease ?? OutCubic;

            float elapsed = 0f;
            while (elapsed < safeDur)
            {
                float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / safeDur);
                apply(easeFn(t));
                yield return null;
            }

            apply(1f);
        }
    }
}
