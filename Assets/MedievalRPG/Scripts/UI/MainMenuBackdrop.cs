using UnityEngine;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Hiệu ứng chuyển động nền nhẹ nhàng cho cảnh rừng ở màn hình Menu chính.
    /// </summary>
    public class MainMenuBackdrop : MonoBehaviour
    {
        [SerializeField] private Transform skyLayer;
        [SerializeField] private Transform midgroundLayer;
        [SerializeField] private Transform foregroundLayer;
        [SerializeField] private Transform[] heroShowcaseTargets;

        private Vector3 _skyBase;
        private Vector3 _midBase;
        private Vector3 _fgBase;
        private Vector3[] _heroBases;

        public void Configure(Transform sky, Transform mid, Transform fg, Transform[] heroes)
        {
            skyLayer = sky;
            midgroundLayer = mid;
            foregroundLayer = fg;
            heroShowcaseTargets = heroes;
            CacheBasePositions();
        }

        private void Awake()
        {
            CacheBasePositions();
        }

        private void CacheBasePositions()
        {
            if (skyLayer != null) _skyBase = skyLayer.position;
            if (midgroundLayer != null) _midBase = midgroundLayer.position;
            if (foregroundLayer != null) _fgBase = foregroundLayer.position;
            if (heroShowcaseTargets != null)
            {
                _heroBases = new Vector3[heroShowcaseTargets.Length];
                for (int i = 0; i < heroShowcaseTargets.Length; i++)
                {
                    if (heroShowcaseTargets[i] != null)
                    {
                        _heroBases[i] = heroShowcaseTargets[i].position;
                    }
                }
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float sway = Mathf.Sin(t * 0.25f);

            if (skyLayer != null) skyLayer.position = _skyBase + new Vector3(sway * 0.12f, 0f, 0f);
            if (midgroundLayer != null) midgroundLayer.position = _midBase + new Vector3(sway * 0.24f, 0f, 0f);
            if (foregroundLayer != null) foregroundLayer.position = _fgBase + new Vector3(sway * 0.36f, 0f, 0f);

            if (heroShowcaseTargets != null && _heroBases != null)
            {
                for (int i = 0; i < heroShowcaseTargets.Length && i < _heroBases.Length; i++)
                {
                    if (heroShowcaseTargets[i] == null) continue;
                    float bob = Mathf.Sin(t * 2.2f + i * 1.1f) * 0.035f;
                    heroShowcaseTargets[i].position = _heroBases[i] + new Vector3(0f, bob, 0f);
                }
            }
        }
    }
}
