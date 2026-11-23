using UnityEngine;

namespace MedievalRPG.Combat
{
    /// <summary>
    /// Bộ điều phối Time.timeScale tập trung với thứ tự ưu tiên:
    /// Pause (0f) > Hitstop (0.08f) > BaseSpeed (tốc độ trận đấu).
    /// Ngăn việc hiệu ứng Hitstop ghi đè làm mất trạng thái Pause khi người chơi tạm dừng giữa chừng.
    /// </summary>
    public static class TimeScaleController
    {
        private static float _baseSpeed = 1f;
        private static bool _isPaused;
        private static bool _isHitstopActive;
        private static float _hitstopScale = 0.08f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Reset();
        }

        public static float BaseSpeed => _baseSpeed;
        public static bool IsPaused => _isPaused;
        public static bool IsHitstopActive => _isHitstopActive;

        public static float EffectiveTimeScale
        {
            get
            {
                if (_isPaused) return 0f;
                if (_isHitstopActive) return _hitstopScale;
                return _baseSpeed;
            }
        }

        public static void SetBaseSpeed(float speed)
        {
            _baseSpeed = speed < 0.5f ? 1f : Mathf.Min(4f, speed);
            Apply();
        }

        public static void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (paused)
            {
                _isHitstopActive = false;
            }
            Apply();
        }

        public static void SetHitstop(bool active, float hitstopScale = 0.08f)
        {
            if (active && _isPaused)
            {
                return;
            }

            _isHitstopActive = active;
            _hitstopScale = Mathf.Clamp(hitstopScale, 0.01f, 0.5f);
            Apply();
        }

        public static void Reset()
        {
            _baseSpeed = 1f;
            _isPaused = false;
            _isHitstopActive = false;
            _hitstopScale = 0.08f;
            Apply();
        }

        private static void Apply()
        {
            Time.timeScale = EffectiveTimeScale;
        }
    }
}
