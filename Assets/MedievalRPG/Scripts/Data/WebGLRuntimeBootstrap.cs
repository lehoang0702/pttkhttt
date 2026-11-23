using UnityEngine;

namespace MedievalRPG.Data
{
    /// <summary>
    /// Tinh chỉnh frame rate và mức chất lượng tối ưu cho trình duyệt khi chạy bản WebGL.
    /// </summary>
    public static class WebGLRuntimeBootstrap
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureWebGLRuntime()
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;

            string[] qualityNames = QualitySettings.names;
            int lowIndex = -1;
            if (qualityNames != null)
            {
                for (int i = 0; i < qualityNames.Length; i++)
                {
                    if (string.Equals(qualityNames[i], "Low", System.StringComparison.OrdinalIgnoreCase))
                    {
                        lowIndex = i;
                        break;
                    }
                }
            }

            if (lowIndex < 0 && QualitySettings.count > 1)
            {
                lowIndex = 1;
            }

            if (lowIndex >= 0)
            {
                QualitySettings.SetQualityLevel(lowIndex, true);
                QualitySettings.vSyncCount = 0;
            }
        }
#endif
    }
}
