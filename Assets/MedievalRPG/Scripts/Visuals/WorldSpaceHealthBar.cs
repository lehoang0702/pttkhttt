using UnityEngine;
using MedievalRPG.Combat;

namespace MedievalRPG.Visuals
{
    [RequireComponent(typeof(BattleUnit))]
    public class WorldSpaceHealthBar : MonoBehaviour
    {
        [SerializeField] private BattleUnit unit;
        [SerializeField] private Vector3 offset = new Vector3(0, 0.8f, 0);
        [SerializeField] private Vector2 barSize = new Vector2(0.9f, 0.12f);

        private Texture2D _bgTexture;
        private Texture2D _hpTexture;
        private Texture2D _mpTexture;

        private void Awake()
        {
            if (unit == null) unit = GetComponent<BattleUnit>();

            _bgTexture = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.85f));
            _hpTexture = MakeTex(2, 2, new Color(0.85f, 0.2f, 0.2f, 1f));
            _mpTexture = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.95f, 1f));
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            if (unit == null || unit.IsDead || Camera.main == null) return;

            Vector3 worldPos = transform.position + offset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0) return; // Behind camera

            float screenY = Screen.height - screenPos.y;
            float width = 64f;
            float hpHeight = 6f;
            float mpHeight = 4f;

            float hpPct = (float)unit.CurrentHealth / Mathf.Max(1, unit.MaxHealth);
            float mpPct = (float)unit.CurrentMana / Mathf.Max(1, unit.MaxMana);

            Rect bgRect = new Rect(screenPos.x - width / 2 - 1, screenY - 1, width + 2, hpHeight + mpHeight + 4);
            GUI.DrawTexture(bgRect, _bgTexture);

            Rect hpRect = new Rect(screenPos.x - width / 2, screenY, width * hpPct, hpHeight);
            GUI.DrawTexture(hpRect, _hpTexture);

            Rect mpRect = new Rect(screenPos.x - width / 2, screenY + hpHeight + 1, width * mpPct, mpHeight);
            GUI.DrawTexture(mpRect, _mpTexture);
        }
    }
}
