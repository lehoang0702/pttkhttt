using System.Collections.Generic;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.UI;

namespace MedievalRPG.Visuals
{
    /// <summary>
    /// Hiệu ứng trạng thái liên tục (Persistent Status VFX) gắn trực tiếp trên nhân vật/quái:
    /// - Stun: 3 ngôi sao vàng xoay quanh đầu.
    /// - Burn: Hạt lửa đỏ cam bốc lên liên tục.
    /// - Taunt / Defend / DefenseUp: Vòng hào quang khiên bảo vệ.
    /// - AttackUp: Tia năng lượng tăng công.
    /// - ShadowShield (Boss): Các rune bóng tối quay theo quỹ đạo (đúng số tầng).
    /// - Quiz Combo Aura (Hero): Hào quang vàng-ngọc dưới chân khi đạt chuỗi Quiz >= 3.
    /// </summary>
    public class UnitStatusVFX : MonoBehaviour
    {
        private static Sprite _starSprite;
        private static Sprite _flameSprite;
        private static Sprite _ringSprite;
        private static Sprite _runeSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _starSprite = null;
            _flameSprite = null;
            _ringSprite = null;
            _runeSprite = null;
        }

        private Unit _unit;
        private Transform _vfxRoot;
        private readonly SpriteRenderer[] _stunStars = new SpriteRenderer[3];
        private readonly SpriteRenderer[] _burnMotes = new SpriteRenderer[4];
        private readonly SpriteRenderer[] _shadowRunes = new SpriteRenderer[2];
        private SpriteRenderer _shieldAura;
        private SpriteRenderer _attackAura;
        private SpriteRenderer _comboAura;

        private bool _comboActive;
        private float _headOffsetY = 0.72f;
        private readonly Vector3[] _burnPhase =
        {
            new Vector3(-0.22f, 0.1f, 0f),
            new Vector3(0.18f, 0.4f, 0.3f),
            new Vector3(-0.08f, 0.7f, 0.55f),
            new Vector3(0.24f, 0.2f, 0.8f)
        };

        public static UnitStatusVFX AttachOrRefresh(Unit unit)
        {
            if (unit == null) return null;
            UnitStatusVFX vfx = unit.GetComponent<UnitStatusVFX>();
            if (vfx == null)
            {
                vfx = unit.gameObject.AddComponent<UnitStatusVFX>();
            }
            vfx.Bind(unit);
            vfx.RefreshVisibility();
            return vfx;
        }

        public void SetComboAuraActive(bool active)
        {
            _comboActive = active;
            RefreshVisibility();
        }

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            EnsureNodesCreated();
        }

        private void OnEnable()
        {
            if (_unit != null)
            {
                _unit.OnStatsChanged -= HandleUnitStatsChanged;
                _unit.OnStatsChanged += HandleUnitStatsChanged;
            }
        }

        private void OnDisable()
        {
            if (_unit != null)
            {
                _unit.OnStatsChanged -= HandleUnitStatsChanged;
            }
        }

        public void Bind(Unit unit)
        {
            if (_unit != null) _unit.OnStatsChanged -= HandleUnitStatsChanged;
            _unit = unit;
            if (_unit != null && isActiveAndEnabled)
            {
                _unit.OnStatsChanged -= HandleUnitStatsChanged;
                _unit.OnStatsChanged += HandleUnitStatsChanged;
            }

            EnsureNodesCreated();
            ComputeHeadOffset();
        }

        private void HandleUnitStatsChanged(Unit _) => RefreshVisibility();

        private void ComputeHeadOffset()
        {
            _headOffsetY = 0.72f;
            if (_unit == null) return;
            SpriteRenderer sr = _unit.pixelAnimator != null
                ? _unit.pixelAnimator.SpriteRendererComponent
                : _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                _headOffsetY = Mathf.Clamp(sr.sprite.bounds.max.y + 0.06f, 0.55f, 1.35f);
            }
        }

        private void EnsureNodesCreated()
        {
            if (_vfxRoot != null) return;
            GameObject rootGo = new GameObject("StatusVFXRoot");
            rootGo.transform.SetParent(transform, false);
            _vfxRoot = rootGo.transform;

            for (int i = 0; i < _stunStars.Length; i++)
            {
                _stunStars[i] = CreateChildSprite($"StunStar_{i}", GetStarSprite(), MedievalUITheme.GoldSoft, 18, 0.55f);
            }
            for (int i = 0; i < _burnMotes.Length; i++)
            {
                _burnMotes[i] = CreateChildSprite($"BurnMote_{i}", GetFlameSprite(), MedievalUITheme.HpWarning, 17, 0.65f);
            }
            for (int i = 0; i < _shadowRunes.Length; i++)
            {
                _shadowRunes[i] = CreateChildSprite($"ShadowRune_{i}", GetRuneSprite(), MedievalUITheme.RarityEpic, 18, 0.90f);
            }

            _shieldAura = CreateChildSprite("ShieldAura", GetRingSprite(), MedievalUITheme.MpSapphire, 4, 1.0f);
            _attackAura = CreateChildSprite("AttackAura", GetRingSprite(), MedievalUITheme.BtnAttack, 4, 0.9f);
            _comboAura  = CreateChildSprite("ComboAura",  GetRingSprite(), MedievalUITheme.GoldSoft, 3, 1.15f);
        }

        private SpriteRenderer CreateChildSprite(string name, Sprite sprite, Color color, int sortingOrder, float scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_vfxRoot, false);
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            go.SetActive(false);
            return sr;
        }

        public void RefreshVisibility()
        {
            if (_vfxRoot == null) return;
            bool alive = _unit != null && _unit.IsAlive;

            bool stunned = alive && _unit.IsStunned;
            bool burned = alive && _unit.HasStatus(StatusEffectType.Burn);
            bool shielded = alive && (_unit.HasTaunt || _unit.isDefending || _unit.bonusDefense > 0 || _unit.HasStatus(StatusEffectType.DefenseUp));
            bool atkUp = alive && _unit.HasStatus(StatusEffectType.AttackUp);
            int shadowStacks = alive ? Mathf.Clamp(_unit.ShadowShieldStacks, 0, _shadowRunes.Length) : 0;
            bool showCombo = alive && !_unit.isEnemy && _comboActive;

            for (int i = 0; i < _stunStars.Length; i++)
            {
                if (_stunStars[i] != null) _stunStars[i].gameObject.SetActive(stunned);
            }
            for (int i = 0; i < _burnMotes.Length; i++)
            {
                if (_burnMotes[i] != null) _burnMotes[i].gameObject.SetActive(burned);
            }
            for (int i = 0; i < _shadowRunes.Length; i++)
            {
                if (_shadowRunes[i] != null) _shadowRunes[i].gameObject.SetActive(i < shadowStacks);
            }

            if (_shieldAura != null)
            {
                _shieldAura.gameObject.SetActive(shielded);
                if (shielded)
                {
                    Color c = _unit.HasTaunt ? MedievalUITheme.GoldPrimary : MedievalUITheme.MpSapphire;
                    c.a = 0.55f;
                    _shieldAura.color = c;
                }
            }
            if (_attackAura != null) _attackAura.gameObject.SetActive(atkUp);
            if (_comboAura != null) _comboAura.gameObject.SetActive(showCombo);
        }

        private void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;
            float t = Time.unscaledTime;

            // Giữ scale VFX ổn định dù parent scale lớn/nhỏ khác nhau (Knight 1.45, Mage 0.96, Boss 1.75)
            float parentSx = Mathf.Max(0.25f, Mathf.Abs(transform.localScale.x));
            float parentSy = Mathf.Max(0.25f, Mathf.Abs(transform.localScale.y));
            if (_vfxRoot != null)
            {
                _vfxRoot.localScale = new Vector3(1f / parentSx, 1f / parentSy, 1f);
            }

            // 1. Stun stars quay quanh đỉnh đầu
            for (int i = 0; i < _stunStars.Length; i++)
            {
                SpriteRenderer star = _stunStars[i];
                if (star == null || !star.gameObject.activeSelf) continue;
                float ang = t * 4.2f + i * (Mathf.PI * 2f / _stunStars.Length);
                float x = Mathf.Cos(ang) * 0.34f;
                float y = _headOffsetY * parentSy + Mathf.Sin(ang) * 0.10f;
                star.transform.localPosition = new Vector3(x, y, -0.04f);
                star.transform.localScale = Vector3.one * (0.48f + 0.14f * Mathf.Sin(ang));
            }

            // 2. Burn motes bốc lên từ thân
            for (int i = 0; i < _burnMotes.Length; i++)
            {
                SpriteRenderer mote = _burnMotes[i];
                if (mote == null || !mote.gameObject.activeSelf) continue;
                float cycle = Mathf.Repeat(t * 1.35f + _burnPhase[i].z, 1f);
                float x = _burnPhase[i].x + Mathf.Sin(t * 5f + i) * 0.08f;
                float y = -0.10f + cycle * 0.85f;
                mote.transform.localPosition = new Vector3(x, y, -0.04f);
                mote.transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 0.25f, cycle);
                Color c = Color.Lerp(MedievalUITheme.GoldSoft, MedievalUITheme.HpWarning, cycle);
                c.a = (1f - cycle) * 0.92f;
                mote.color = c;
            }

            // 3. Shield Aura dưới chân/quanh người
            if (_shieldAura != null && _shieldAura.gameObject.activeSelf)
            {
                float pulse = 1f + 0.08f * Mathf.Sin(t * 4.5f);
                _shieldAura.transform.localPosition = new Vector3(0f, -0.22f, 0.02f);
                _shieldAura.transform.localScale = new Vector3(1.18f * pulse, 0.44f * pulse, 1f);
                Color c = _shieldAura.color;
                c.a = 0.42f + 0.20f * Mathf.Sin(t * 4.5f);
                _shieldAura.color = c;
            }

            // 4. AttackUp Aura
            if (_attackAura != null && _attackAura.gameObject.activeSelf)
            {
                float pulse = 1f + 0.10f * Mathf.Sin(t * 5.2f + 1.2f);
                _attackAura.transform.localPosition = new Vector3(0f, -0.26f, 0.03f);
                _attackAura.transform.localScale = new Vector3(1.05f * pulse, 0.38f * pulse, 1f);
                Color c = MedievalUITheme.BtnAttack;
                c.a = 0.40f + 0.22f * Mathf.Sin(t * 5.2f);
                _attackAura.color = c;
            }

            // 5. Boss Shadow Shield Runes quay quanh Boss
            int activeRunes = Mathf.Clamp(_unit.ShadowShieldStacks, 0, _shadowRunes.Length);
            for (int i = 0; i < activeRunes; i++)
            {
                SpriteRenderer rune = _shadowRunes[i];
                if (rune == null || !rune.gameObject.activeSelf) continue;
                float ang = t * 2.8f + i * Mathf.PI;
                rune.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.56f, 0.28f + Mathf.Sin(ang) * 0.22f, -0.05f);
                rune.transform.Rotate(0f, 0f, 120f * Time.unscaledDeltaTime);
            }

            // 6. Quiz Combo Aura dưới chân anh hùng
            if (_comboAura != null && _comboAura.gameObject.activeSelf)
            {
                float pulse = 1f + 0.11f * Mathf.Sin(t * 6f);
                _comboAura.transform.localPosition = new Vector3(0f, -0.30f, 0.04f);
                _comboAura.transform.localScale = new Vector3(1.26f * pulse, 0.48f * pulse, 1f);
                Color c = MedievalUITheme.GoldSoft;
                c.a = 0.46f + 0.24f * Mathf.Sin(t * 6f);
                _comboAura.color = c;
            }
        }

        private static Sprite GetStarSprite()
        {
            if (_starSprite != null) return _starSprite;
            const int s = 10;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool cross = (x >= 4 && x <= 5 && y >= 1 && y <= 8) || (y >= 4 && y <= 5 && x >= 1 && x <= 8);
                    bool core = x >= 3 && x <= 6 && y >= 3 && y <= 6;
                    tex.SetPixel(x, y, (cross || core) ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            _starSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _starSprite;
        }

        private static Sprite GetFlameSprite()
        {
            if (_flameSprite != null) return _flameSprite;
            const int s = 10;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs(x - 4.5f);
                    float widthAtY = Mathf.Lerp(3.5f, 0.8f, y / 9f);
                    tex.SetPixel(x, y, (y >= 1 && y <= 8 && dx <= widthAtY) ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            _flameSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _flameSprite;
        }

        private static Sprite GetRingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            const int s = 32;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    tex.SetPixel(x, y, (d >= 10f && d <= 14.5f) ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _ringSprite;
        }

        private static Sprite GetRuneSprite()
        {
            if (_runeSprite != null) return _runeSprite;
            const int s = 12;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float manh = Mathf.Abs(x - 5.5f) + Mathf.Abs(y - 5.5f);
                    if (manh <= 2.2f) tex.SetPixel(x, y, Color.white);
                    else if (manh <= 4.6f) tex.SetPixel(x, y, new Color(0.8f, 0.6f, 1f, 0.85f));
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            _runeSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _runeSprite;
        }

        private static Texture2D CreateTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
