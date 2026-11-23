using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.Visuals
{
    /// <summary>
    /// Vẽ con đường đất lát đá trung cổ trên mặt đất rừng (2D World Space)
    /// và điều khiển hiệu ứng cuộn đường + rừng parallax khi đội hình anh hùng di chuyển
    /// từ đợt quái này sang đợt quái khác.
    /// </summary>
    public class ForestRoadController : MonoBehaviour
    {
        [Header("Parallax Background References")]
        [SerializeField] private Transform skyLayer;
        [SerializeField] private Transform midgroundLayer;
        [SerializeField] private Transform foregroundLayer;
        [SerializeField] private TMP_FontAsset labelFont;

        public void OverrideFont(TMP_FontAsset font)
        {
            if (font != null) labelFont = font;
            if (_milestoneTextTMP != null && font != null) _milestoneTextTMP.font = font;
        }

        private readonly List<Transform> _roadTiles = new List<Transform>();
        private readonly List<Transform> _roadsideProps = new List<Transform>();
        private Transform _milestoneRoot;
        private TextMeshProUGUI _milestoneTextTMP;
        private SpriteRenderer _milestoneFlagSr;

        private float _roadTileWidth = 4.0f;
        private float _roadY = -1.28f;
        private float _midTileWidth;
        private float _fgTileWidth;
        private Vector3 _midOrigin;
        private Vector3 _fgOrigin;
        private Vector3 _skyOrigin;

        private static Sprite _cachedRoadSprite;
        private static Sprite _cachedPebbleBushSprite;
        private static Sprite _cachedMilestoneSprite;
        private static Sprite _cachedMoteSprite;
        private static Sprite _cachedLeafSprite;
        private static Sprite _cachedMistSprite;
        private static Sprite _cachedEventChestSprite;
        private static Sprite _cachedEventShrineSprite;
        private static Sprite _cachedEventCartSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticSprites()
        {
            _cachedRoadSprite = null;
            _cachedPebbleBushSprite = null;
            _cachedMilestoneSprite = null;
            _cachedMoteSprite = null;
            _cachedLeafSprite = null;
            _cachedMistSprite = null;
            _cachedEventChestSprite = null;
            _cachedEventShrineSprite = null;
            _cachedEventCartSprite = null;
        }

        private readonly List<Transform> _ambientMotes = new List<Transform>();
        private readonly List<SpriteRenderer> _ambientMoteSrs = new List<SpriteRenderer>();
        private readonly List<float> _ambientSpeeds = new List<float>();
        private readonly List<Transform> _weatherParticles = new List<Transform>();
        private readonly List<SpriteRenderer> _weatherSrs = new List<SpriteRenderer>();
        private readonly List<Vector2> _weatherVelocities = new List<Vector2>();
        private readonly List<Transform> _mistBands = new List<Transform>();
        private readonly List<SpriteRenderer> _mistSrs = new List<SpriteRenderer>();
        private GameObject _eventPropGo;
        private EnvironmentThemeType _currentTheme = EnvironmentThemeType.MistyForest;
        private bool _hasStarted;

        public bool HasRoadTiles => _roadTiles.Count > 0;

        public void SetMilestoneVisible(bool visible)
        {
            if (_milestoneRoot != null)
            {
                _milestoneRoot.gameObject.SetActive(visible);
            }
        }

        public void ConfigureParallax(Transform sky, Transform mid, Transform fg, TMP_FontAsset font = null)
        {
            skyLayer = sky;
            midgroundLayer = mid;
            foregroundLayer = fg;
            if (font != null) labelFont = font;
            CacheParallaxMetrics();
        }

        private void Awake()
        {
            CacheParallaxMetrics();
            if (_roadTiles.Count == 0)
            {
                BuildOrRebuildRoadVisuals();
            }
        }

        private void Start()
        {
            _hasStarted = true;
            SpawnAmbientMotes();
            SpawnThemeWeatherParticles();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;

            if (_ambientMotes.Count > 0)
            {
                for (int i = 0; i < _ambientMotes.Count; i++)
                {
                    Transform tr = _ambientMotes[i];
                    if (tr == null) continue;
                    float spd = _ambientSpeeds[i];
                    tr.position += new Vector3(-spd * 0.45f * dt, Mathf.Sin(t * 2.1f + i) * 0.14f * dt, 0f);
                    if (tr.position.x < -7.5f)
                    {
                        tr.position = new Vector3(7.5f, UnityEngine.Random.Range(-1.5f, 2.2f), 0.12f);
                    }
                    if (_ambientMoteSrs[i] != null)
                    {
                        Color c = _ambientMoteSrs[i].color;
                        c.a = 0.35f + 0.35f * Mathf.Sin(t * 3.0f + i * 1.3f);
                        _ambientMoteSrs[i].color = c;
                    }
                }
            }

            if (_weatherParticles.Count > 0)
            {
                for (int i = 0; i < _weatherParticles.Count; i++)
                {
                    Transform tr = _weatherParticles[i];
                    if (tr == null) continue;
                    Vector2 vel = _weatherVelocities[i];
                    tr.position += new Vector3(vel.x * dt, vel.y * dt, 0f);

                    if (_currentTheme == EnvironmentThemeType.MistyForest)
                    {
                        tr.Rotate(0f, 0f, 32f * dt);
                        if (tr.position.y < -2.3f || tr.position.x < -8.2f)
                        {
                            tr.position = new Vector3(UnityEngine.Random.Range(-3.5f, 8.2f), UnityEngine.Random.Range(2.4f, 3.6f), 0.10f);
                        }
                    }
                    else
                    {
                        tr.position += new Vector3(Mathf.Sin(t * 2.5f + i) * 0.16f * dt, 0f, 0f);
                        if (tr.position.y > 3.2f || tr.position.x < -8.2f || tr.position.x > 8.2f)
                        {
                            tr.position = new Vector3(UnityEngine.Random.Range(-7.0f, 7.0f), UnityEngine.Random.Range(-2.3f, -1.6f), 0.10f);
                        }
                        if (_weatherSrs[i] != null)
                        {
                            Color c = _weatherSrs[i].color;
                            float heightNorm = Mathf.Clamp01((tr.position.y + 2.3f) / 5.2f);
                            c.a = Mathf.Sin(heightNorm * Mathf.PI) * 0.78f;
                            _weatherSrs[i].color = c;
                        }
                    }
                }
            }

            if (_mistBands.Count > 0)
            {
                for (int i = 0; i < _mistBands.Count; i++)
                {
                    Transform mb = _mistBands[i];
                    if (mb == null) continue;
                    mb.position += new Vector3(-(0.28f + i * 0.12f) * dt, 0f, 0f);
                    if (mb.position.x < -11.5f)
                    {
                        mb.position = new Vector3(11.5f, mb.position.y, mb.position.z);
                    }
                }
            }
        }

        public void SetEnvironmentTheme(EnvironmentThemeType theme)
        {
            _currentTheme = theme;
            if (skyLayer == null) skyLayer = GameObject.Find("Forest_Background_Sky")?.transform;
            if (midgroundLayer == null) midgroundLayer = GameObject.Find("Forest_Background_Midground")?.transform;
            if (foregroundLayer == null) foregroundLayer = GameObject.Find("Forest_Foreground_Trees")?.transform;

            Color skyTint, midTint, fgTint, roadTint, moteColor;

            switch (theme)
            {
                case EnvironmentThemeType.AncientCrypt:
                    skyTint = new Color(0.26f, 0.24f, 0.54f, 1f);
                    midTint = new Color(0.34f, 0.31f, 0.56f, 1f);
                    fgTint = new Color(0.38f, 0.33f, 0.54f, 1f);
                    roadTint = new Color(0.54f, 0.58f, 0.74f, 1f);
                    moteColor = new Color(0.55f, 0.90f, 1.0f, 0.68f);
                    break;
                case EnvironmentThemeType.DarkCitadel:
                    skyTint = new Color(0.52f, 0.18f, 0.22f, 1f);
                    midTint = new Color(0.42f, 0.20f, 0.25f, 1f);
                    fgTint = new Color(0.35f, 0.19f, 0.24f, 1f);
                    roadTint = new Color(0.50f, 0.36f, 0.40f, 1f);
                    moteColor = new Color(1.0f, 0.44f, 0.28f, 0.72f);
                    break;
                default:
                    skyTint = Color.white;
                    midTint = Color.white;
                    fgTint = Color.white;
                    roadTint = Color.white;
                    moteColor = new Color(0.85f, 0.96f, 0.55f, 0.6f);
                    break;
            }

            ApplyLayerTint(skyLayer, skyTint);
            ApplyLayerTint(midgroundLayer, midTint);
            ApplyLayerTint(foregroundLayer, fgTint);

            for (int i = 0; i < _roadTiles.Count; i++)
            {
                if (_roadTiles[i] != null && _roadTiles[i].TryGetComponent<SpriteRenderer>(out var sr))
                {
                    sr.color = roadTint;
                }
            }
            for (int i = 0; i < _roadsideProps.Count; i++)
            {
                if (_roadsideProps[i] != null && _roadsideProps[i].TryGetComponent<SpriteRenderer>(out var sr))
                {
                    sr.color = fgTint;
                }
            }
            for (int i = 0; i < _ambientMoteSrs.Count; i++)
            {
                if (_ambientMoteSrs[i] != null) _ambientMoteSrs[i].color = moteColor;
            }

            if (_hasStarted && Application.isPlaying)
            {
                RefreshThemeWeatherVisuals();
            }
        }

        private static void ApplyLayerTint(Transform layer, Color tint)
        {
            if (layer == null) return;
            SpriteRenderer[] srs = layer.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < srs.Length; i++) srs[i].color = tint;
        }

        private void SpawnAmbientMotes()
        {
            if (_ambientMotes.Count > 0) return;
            Sprite moteSp = GetOrCreateMoteSprite();
            Color moteCol = _currentTheme == EnvironmentThemeType.AncientCrypt
                ? new Color(0.55f, 0.88f, 1f, 0.6f)
                : (_currentTheme == EnvironmentThemeType.DarkCitadel
                    ? new Color(1f, 0.46f, 0.30f, 0.6f)
                    : new Color(0.85f, 0.96f, 0.55f, 0.6f));

            for (int i = 0; i < 14; i++)
            {
                GameObject go = new GameObject($"AmbientMote_{i}");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(UnityEngine.Random.Range(-7.2f, 7.2f), UnityEngine.Random.Range(-1.4f, 2.2f), 0.12f);
                go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.45f, 0.9f);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = moteSp;
                sr.color = moteCol;
                sr.sortingOrder = -1;
                _ambientMotes.Add(go.transform);
                _ambientMoteSrs.Add(sr);
                _ambientSpeeds.Add(UnityEngine.Random.Range(0.55f, 1.35f));
            }
        }

        private static Sprite GetOrCreateMoteSprite()
        {
            if (_cachedMoteSprite != null) return _cachedMoteSprite;
            Texture2D tex = new Texture2D(6, 6, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 6; x++)
                    tex.SetPixel(x, y, (x >= 1 && x <= 4 && y >= 1 && y <= 4) ? Color.white : Color.clear);
            tex.Apply();
            _cachedMoteSprite = Sprite.Create(tex, new Rect(0, 0, 6, 6), new Vector2(0.5f, 0.5f), 32f);
            return _cachedMoteSprite;
        }

        private void CacheParallaxMetrics()
        {
            if (skyLayer != null) _skyOrigin = skyLayer.position;
            if (midgroundLayer != null)
            {
                _midOrigin = midgroundLayer.position;
                SpriteRenderer sr = midgroundLayer.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    _midTileWidth = sr.sprite.bounds.size.x * Mathf.Abs(midgroundLayer.lossyScale.x);
                }
            }
            if (foregroundLayer != null)
            {
                _fgOrigin = foregroundLayer.position;
                SpriteRenderer sr = foregroundLayer.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    _fgTileWidth = sr.sprite.bounds.size.x * Mathf.Abs(foregroundLayer.lossyScale.x);
                }
            }
        }

        public void BuildOrRebuildRoadVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            _roadTiles.Clear();
            _roadsideProps.Clear();
            _ambientMotes.Clear();
            _ambientMoteSrs.Clear();
            _ambientSpeeds.Clear();
            _weatherParticles.Clear();
            _weatherSrs.Clear();
            _weatherVelocities.Clear();
            _mistBands.Clear();
            _mistSrs.Clear();
            _eventPropGo = null;
            _milestoneRoot = null;
            _milestoneTextTMP = null;
            _milestoneFlagSr = null;

            Sprite roadSp = GetOrCreateRoadSprite();
            _roadTileWidth = roadSp.bounds.size.x;

            // Tải các đoạn đường liền mạch phủ toàn bộ màn hình [-12 .. +16]
            const int tileCount = 8;
            float startX = -12.0f;
            for (int i = 0; i < tileCount; i++)
            {
                GameObject tile = new GameObject($"RoadTile_{i}");
                tile.transform.SetParent(transform, false);
                tile.transform.position = new Vector3(startX + i * _roadTileWidth, _roadY, 0.25f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = roadSp;
                sr.sortingOrder = -2;
                _roadTiles.Add(tile.transform);
            }

            // Các bụi cỏ / đá ven lề đường tạo cảm giác chiều sâu khi hành quân
            Sprite bushSp = GetOrCreateRoadsidePropSprite();
            float[] propXs = { -5.5f, -2.4f, 0.8f, 3.6f, 6.8f, 9.8f };
            for (int i = 0; i < propXs.Length; i++)
            {
                GameObject p = new GameObject($"RoadProp_{i}");
                p.transform.SetParent(transform, false);
                bool topEdge = (i % 2 == 0);
                p.transform.position = new Vector3(propXs[i], topEdge ? (_roadY + 0.66f) : (_roadY - 0.68f), 0.20f);
                p.transform.localScale = Vector3.one * (topEdge ? 0.92f : 1.08f);
                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = bushSp;
                sr.sortingOrder = topEdge ? -1 : 1;
                _roadsideProps.Add(p.transform);
            }

            BuildMilestoneMarker();
            if (_hasStarted && Application.isPlaying)
            {
                SpawnAmbientMotes();
                SpawnThemeWeatherParticles();
            }
        }

        private void SpawnThemeWeatherParticles()
        {
            if (_weatherParticles.Count == 0)
            {
                for (int i = 0; i < 14; i++)
                {
                    GameObject go = new GameObject($"WeatherParticle_{i}");
                    go.transform.SetParent(transform, false);
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 1;
                    _weatherParticles.Add(go.transform);
                    _weatherSrs.Add(sr);
                    _weatherVelocities.Add(Vector2.zero);
                }
            }

            if (_mistBands.Count == 0)
            {
                Sprite mistSp = GetOrCreateMistSprite();
                for (int i = 0; i < 3; i++)
                {
                    GameObject m = new GameObject($"MistBand_{i}");
                    m.transform.SetParent(transform, false);
                    m.transform.position = new Vector3(-7f + i * 7.5f, _roadY - 0.32f + i * 0.22f, 0.14f);
                    m.transform.localScale = new Vector3(4.5f, 0.75f, 1f);
                    SpriteRenderer sr = m.AddComponent<SpriteRenderer>();
                    sr.sprite = mistSp;
                    sr.sortingOrder = -1;
                    _mistBands.Add(m.transform);
                    _mistSrs.Add(sr);
                }
            }

            RefreshThemeWeatherVisuals();
        }

        private void RefreshThemeWeatherVisuals()
        {
            Sprite leafSp = GetOrCreateLeafSprite();
            Sprite moteSp = GetOrCreateMoteSprite();

            for (int i = 0; i < _weatherParticles.Count; i++)
            {
                Transform tr = _weatherParticles[i];
                SpriteRenderer sr = _weatherSrs[i];
                if (tr == null || sr == null) continue;

                if (_currentTheme == EnvironmentThemeType.MistyForest)
                {
                    sr.sprite = leafSp;
                    sr.color = (i % 3 == 0)
                        ? new Color(0.84f, 0.74f, 0.34f, 0.78f)
                        : new Color(0.42f, 0.78f, 0.44f, 0.78f);
                    tr.position = new Vector3(UnityEngine.Random.Range(-7.2f, 7.8f), UnityEngine.Random.Range(-1.4f, 3.2f), 0.10f);
                    tr.localScale = Vector3.one * UnityEngine.Random.Range(0.75f, 1.15f);
                    _weatherVelocities[i] = new Vector2(UnityEngine.Random.Range(-0.95f, -0.45f), UnityEngine.Random.Range(-0.65f, -0.35f));
                }
                else if (_currentTheme == EnvironmentThemeType.AncientCrypt)
                {
                    sr.sprite = moteSp;
                    sr.color = new Color(0.45f, 0.88f, 1.0f, 0.72f);
                    tr.position = new Vector3(UnityEngine.Random.Range(-7.0f, 7.0f), UnityEngine.Random.Range(-2.2f, 2.4f), 0.10f);
                    tr.localScale = Vector3.one * UnityEngine.Random.Range(0.65f, 1.20f);
                    tr.rotation = Quaternion.identity;
                    _weatherVelocities[i] = new Vector2(UnityEngine.Random.Range(-0.22f, 0.22f), UnityEngine.Random.Range(0.32f, 0.65f));
                }
                else
                {
                    sr.sprite = moteSp;
                    sr.color = (i % 2 == 0)
                        ? new Color(1.0f, 0.48f, 0.18f, 0.85f)
                        : new Color(1.0f, 0.78f, 0.28f, 0.82f);
                    tr.position = new Vector3(UnityEngine.Random.Range(-7.0f, 7.0f), UnityEngine.Random.Range(-2.2f, 2.4f), 0.10f);
                    tr.localScale = Vector3.one * UnityEngine.Random.Range(0.55f, 1.05f);
                    tr.rotation = Quaternion.identity;
                    _weatherVelocities[i] = new Vector2(UnityEngine.Random.Range(-0.55f, -0.15f), UnityEngine.Random.Range(0.55f, 1.10f));
                }
            }

            Color mistCol = _currentTheme switch
            {
                EnvironmentThemeType.AncientCrypt => new Color(0.45f, 0.64f, 0.92f, 0.16f),
                EnvironmentThemeType.DarkCitadel  => new Color(0.68f, 0.20f, 0.18f, 0.16f),
                _                                 => new Color(0.80f, 0.92f, 0.82f, 0.15f)
            };
            for (int i = 0; i < _mistSrs.Count; i++)
            {
                if (_mistSrs[i] != null) _mistSrs[i].color = mistCol;
            }
        }

        public void ShowRoadsideEventProp(RoadsideEventType eventType)
        {
            HideRoadsideEventProp();
            _eventPropGo = new GameObject($"RoadsideEventProp_{eventType}");
            _eventPropGo.transform.SetParent(transform, false);
            _eventPropGo.transform.position = new Vector3(1.15f, _roadY + 0.25f, 0.08f);
            _eventPropGo.transform.localScale = Vector3.one * 1.45f;

            SpriteRenderer sr = _eventPropGo.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateEventPropSprite(eventType);
            sr.sortingOrder = 4;
        }

        public void HideRoadsideEventProp()
        {
            if (_eventPropGo != null)
            {
                Destroy(_eventPropGo);
                _eventPropGo = null;
            }
        }

        private static Sprite GetOrCreateLeafSprite()
        {
            if (_cachedLeafSprite != null) return _cachedLeafSprite;
            const int s = 8;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    tex.SetPixel(x, y, (Mathf.Abs(x - 3.5f) + Mathf.Abs(y - 3.5f) <= 2.6f) ? Color.white : Color.clear);
            tex.Apply();
            _cachedLeafSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _cachedLeafSprite;
        }

        private static Sprite GetOrCreateMistSprite()
        {
            if (_cachedMistSprite != null) return _cachedMistSprite;
            const int w = 64, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float ny = Mathf.Abs((y / (float)(h - 1)) * 2f - 1f);
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x / (float)(w - 1)) * 2f - 1f);
                    float a = Mathf.Clamp01(1f - nx * nx) * Mathf.Clamp01(1f - ny * ny);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _cachedMistSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return _cachedMistSprite;
        }

        private static Sprite GetOrCreateEventPropSprite(RoadsideEventType type)
        {
            if (type == RoadsideEventType.AncientRuneChest && _cachedEventChestSprite != null) return _cachedEventChestSprite;
            if (type == RoadsideEventType.SacredForestShrine && _cachedEventShrineSprite != null) return _cachedEventShrineSprite;
            if (type == RoadsideEventType.WanderingScholar && _cachedEventCartSprite != null) return _cachedEventCartSprite;

            const int w = 28, h = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            if (type == RoadsideEventType.AncientRuneChest)
            {
                Color wood = new Color(0.45f, 0.27f, 0.14f, 1f);
                Color gold = new Color(0.95f, 0.78f, 0.28f, 1f);
                Color rune = new Color(0.45f, 0.88f, 1.0f, 1f);
                for (int y = 4; y <= 18; y++)
                    for (int x = 4; x <= 23; x++)
                        tex.SetPixel(x, y, (x == 4 || x == 23 || y == 4 || y == 18 || y == 11) ? gold : wood);
                for (int y = 9; y <= 13; y++)
                    for (int x = 12; x <= 15; x++)
                        tex.SetPixel(x, y, rune);
                tex.Apply();
                _cachedEventChestSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 28f);
                return _cachedEventChestSprite;
            }

            if (type == RoadsideEventType.SacredForestShrine)
            {
                Color stone = new Color(0.44f, 0.48f, 0.54f, 1f);
                Color stoneLight = new Color(0.64f, 0.68f, 0.74f, 1f);
                Color glow = new Color(0.32f, 0.90f, 0.64f, 1f);
                for (int y = 2; y <= 6; y++)
                    for (int x = 5; x <= 22; x++) tex.SetPixel(x, y, stone);
                for (int y = 7; y <= 21; y++)
                    for (int x = 9; x <= 18; x++) tex.SetPixel(x, y, x < 14 ? stone : stoneLight);
                for (int y = 12; y <= 17; y++)
                    for (int x = 12; x <= 15; x++) tex.SetPixel(x, y, glow);
                tex.Apply();
                _cachedEventShrineSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 28f);
                return _cachedEventShrineSprite;
            }

            // WanderingScholar: bàn sách + đèn lồng
            Color cartWood = new Color(0.46f, 0.28f, 0.15f, 1f);
            Color cloth = new Color(0.24f, 0.42f, 0.68f, 1f);
            Color lantern = new Color(1.0f, 0.80f, 0.32f, 1f);
            for (int y = 5; y <= 14; y++)
                for (int x = 4; x <= 23; x++) tex.SetPixel(x, y, cartWood);
            for (int y = 15; y <= 23; y++)
                for (int x = 6; x <= 21; x++) tex.SetPixel(x, y, cloth);
            for (int y = 15; y <= 19; y++)
                for (int x = 21; x <= 24; x++) tex.SetPixel(x, y, lantern);
            tex.Apply();
            _cachedEventCartSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 28f);
            return _cachedEventCartSprite;
        }

        private void BuildMilestoneMarker()
        {
            GameObject post = new GameObject("WaveMilestonePost");
            post.transform.SetParent(transform, false);
            post.transform.position = new Vector3(0.45f, _roadY + 0.62f, 0.15f);
            SpriteRenderer sr = post.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateMilestoneSprite();
            sr.sortingOrder = -1;
            _milestoneFlagSr = sr;

            GameObject lblCanvasGo = new GameObject("MilestoneLabel");
            lblCanvasGo.transform.SetParent(post.transform, false);
            lblCanvasGo.transform.localPosition = new Vector3(0f, 0.36f, -0.02f);
            lblCanvasGo.transform.localScale = Vector3.one * 0.009f;

            Canvas c = lblCanvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 10;
            RectTransform cRt = lblCanvasGo.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(130f, 32f);

            UnityEngine.UI.Image bg = lblCanvasGo.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.08f, 0.10f, 0.15f, 0.92f);
            bg.raycastTarget = false;

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(lblCanvasGo.transform, false);
            _milestoneTextTMP = txtGo.AddComponent<TextMeshProUGUI>();
            if (labelFont != null) _milestoneTextTMP.font = labelFont;
            else if (TMP_Settings.defaultFontAsset != null) _milestoneTextTMP.font = TMP_Settings.defaultFontAsset;
            _milestoneTextTMP.text = "ĐỢT 1";
            _milestoneTextTMP.fontSize = 15f;
            _milestoneTextTMP.fontStyle = FontStyles.Bold;
            _milestoneTextTMP.color = new Color(0.95f, 0.80f, 0.42f, 1f);
            _milestoneTextTMP.alignment = TextAlignmentOptions.Center;
            _milestoneTextTMP.raycastTarget = false;

            RectTransform tRt = _milestoneTextTMP.rectTransform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(4f, 2f);
            tRt.offsetMax = new Vector2(-4f, -2f);

            _milestoneRoot = post.transform;
        }

        public void SetMilestone(int waveNumber, int totalWaves, bool isBoss)
        {
            if (_milestoneRoot == null) return;
            if (_milestoneTextTMP != null)
            {
                _milestoneTextTMP.text = isBoss ? "BOSS!" : $"ĐỢT {waveNumber}/{totalWaves}";
                _milestoneTextTMP.color = isBoss
                    ? new Color(1f, 0.42f, 0.36f, 1f)
                    : new Color(0.95f, 0.80f, 0.42f, 1f);
            }
            if (_milestoneFlagSr != null)
            {
                _milestoneFlagSr.color = isBoss ? new Color(1f, 0.72f, 0.72f, 1f) : Color.white;
            }
        }

        /// <summary>
        /// Cuộn con đường và cảnh rừng lùi về sau khi đội hình hành quân tiến tới đợt quái kế tiếp.
        /// </summary>
        public IEnumerator ScrollForwardRoutine(float duration, float roadDistance, int nextWaveNumber, int totalWaves, bool isBoss)
        {
            if (_roadTiles.Count == 0) BuildOrRebuildRoadVisuals();
            CacheParallaxMetrics();

            float elapsed = 0f;
            float prevProgress = 0f;
            bool milestoneSwapped = false;
            float dustTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                dustTimer += Time.deltaTime;
                if (dustTimer >= 0.18f)
                {
                    dustTimer = 0f;
                    PixelCombatVFXManager.Instance?.SpawnDustPuff(new Vector3(UnityEngine.Random.Range(-4.0f, -1.4f), -1.10f, 0f));
                }
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration));
                // Smooth acceleration + deceleration march curve
                float smooth = t * t * (3f - 2f * t);
                float deltaRoad = (smooth - prevProgress) * roadDistance;
                prevProgress = smooth;

                AdvanceRoadAndProps(deltaRoad);
                AdvanceParallaxLayer(foregroundLayer, _fgOrigin, _fgTileWidth, deltaRoad * 0.55f);
                AdvanceParallaxLayer(midgroundLayer, _midOrigin, _midTileWidth, deltaRoad * 0.25f);
                if (skyLayer != null)
                {
                    skyLayer.position -= new Vector3(deltaRoad * 0.04f, 0f, 0f);
                    if (skyLayer.position.x < _skyOrigin.x - 3.0f) skyLayer.position = _skyOrigin;
                }

                if (_milestoneRoot != null)
                {
                    _milestoneRoot.position -= new Vector3(deltaRoad, 0f, 0f);
                    if (!milestoneSwapped && t >= 0.52f)
                    {
                        milestoneSwapped = true;
                        SetMilestone(nextWaveNumber, totalWaves, isBoss);
                        _milestoneRoot.position = new Vector3(0.45f + (1f - smooth) * roadDistance * 0.85f, _roadY + 0.62f, 0.15f);
                    }
                }

                yield return null;
            }

            if (_milestoneRoot != null)
            {
                SetMilestone(nextWaveNumber, totalWaves, isBoss);
                _milestoneRoot.position = new Vector3(0.45f, _roadY + 0.62f, 0.15f);
            }
        }

        private void AdvanceRoadAndProps(float deltaX)
        {
            if (_roadTiles.Count == 0) return;

            float maxRight = float.MinValue;
            for (int i = 0; i < _roadTiles.Count; i++)
            {
                _roadTiles[i].position -= new Vector3(deltaX, 0f, 0f);
                if (_roadTiles[i].position.x > maxRight) maxRight = _roadTiles[i].position.x;
            }

            for (int i = 0; i < _roadTiles.Count; i++)
            {
                if (_roadTiles[i].position.x < -12.0f - _roadTileWidth)
                {
                    _roadTiles[i].position = new Vector3(maxRight + _roadTileWidth, _roadY, 0.25f);
                    maxRight = _roadTiles[i].position.x;
                }
            }

            for (int i = 0; i < _roadsideProps.Count; i++)
            {
                _roadsideProps[i].position -= new Vector3(deltaX, 0f, 0f);
                if (_roadsideProps[i].position.x < -9.5f)
                {
                    Vector3 pos = _roadsideProps[i].position;
                    pos.x += 19.0f;
                    _roadsideProps[i].position = pos;
                }
            }
        }

        private static void AdvanceParallaxLayer(Transform layer, Vector3 origin, float tileWidth, float deltaX)
        {
            if (layer == null || tileWidth <= 0.1f) return;
            layer.position -= new Vector3(deltaX, 0f, 0f);
            while (layer.position.x <= origin.x - tileWidth)
            {
                layer.position += new Vector3(tileWidth, 0f, 0f);
            }
        }

        public static Sprite GetOrCreateRoadSprite()
        {
            if (_cachedRoadSprite != null) return _cachedRoadSprite;

            const int w = 256;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color grassDeep = new Color(0.11f, 0.26f, 0.22f, 0.92f);
            Color grassEdge = new Color(0.20f, 0.42f, 0.29f, 1f);
            Color dirtBorder = new Color(0.32f, 0.23f, 0.15f, 1f);
            Color dirtBase = new Color(0.45f, 0.34f, 0.23f, 1f);
            Color dirtLight = new Color(0.55f, 0.42f, 0.29f, 1f);
            Color cobbleMain = new Color(0.49f, 0.46f, 0.42f, 1f);
            Color cobbleLight = new Color(0.62f, 0.58f, 0.52f, 1f);
            Color cobbleDark = new Color(0.33f, 0.29f, 0.24f, 1f);

            System.Random rng = new System.Random(1408);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int waveTop = 78 + ((x / 8) % 3 == 0 ? 2 : 0) - ((x / 12) % 2);
                    int waveBot = 16 + ((x / 10) % 3 == 1 ? 2 : 0);

                    if (y > waveTop + 6 || y < waveBot - 6)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    if (y > waveTop || y < waveBot)
                    {
                        tex.SetPixel(x, y, (rng.Next(0, 4) == 0) ? grassEdge : grassDeep);
                        continue;
                    }

                    if (y >= waveTop - 3 || y <= waveBot + 3)
                    {
                        tex.SetPixel(x, y, dirtBorder);
                        continue;
                    }

                    // Vệt bánh xe & lối mòn chính giữa đường
                    bool centerTrack = (y >= 40 && y <= 55);
                    Color baseCol = centerTrack ? dirtLight : dirtBase;
                    if (y == 32 || y == 33 || y == 62 || y == 63)
                    {
                        baseCol = dirtBorder; // vệt xe ngựa mờ
                    }

                    int noise = rng.Next(0, 14);
                    if (noise == 0) baseCol = cobbleDark;
                    else if (noise == 1 && !centerTrack) baseCol = dirtLight;
                    tex.SetPixel(x, y, baseCol);
                }
            }

            // Vẽ các viên đá cuội lát đường nổi bật dọc theo trục giữa con đường
            for (int sx = 6; sx < w - 12; sx += 16)
            {
                int cy = 44 + rng.Next(-14, 15);
                int rw = rng.Next(6, 11);
                int rh = rng.Next(4, 6);
                for (int dy = 0; dy < rh; dy++)
                {
                    for (int dx = 0; dx < rw; dx++)
                    {
                        int px = (sx + dx) % w;
                        int py = cy + dy;
                        if (py <= 22 || py >= 73) continue;
                        bool edge = dx == 0 || dx == rw - 1 || dy == 0 || dy == rh - 1;
                        Color c = edge ? cobbleDark : (dy == rh - 2 ? cobbleLight : cobbleMain);
                        tex.SetPixel(px, py, c);
                    }
                }
            }

            tex.Apply();
            _cachedRoadSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
            return _cachedRoadSprite;
        }

        private static Sprite GetOrCreateRoadsidePropSprite()
        {
            if (_cachedPebbleBushSprite != null) return _cachedPebbleBushSprite;
            const int w = 24;
            const int h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color rockDark = new Color(0.28f, 0.25f, 0.27f, 1f);
            Color rockLight = new Color(0.45f, 0.42f, 0.44f, 1f);
            Color leafDark = new Color(0.14f, 0.36f, 0.27f, 1f);
            Color leafBright = new Color(0.27f, 0.56f, 0.36f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inRock = x >= 2 && x <= 11 && y >= 2 && y <= 8 && (x + y > 5) && (x - y < 9);
                    bool inBush = x >= 10 && x <= 21 && y >= 2 && y <= 11 && ((x - 15) * (x - 15) + (y - 5) * (y - 5) <= 28);
                    if (inRock) tex.SetPixel(x, y, y > 5 ? rockLight : rockDark);
                    else if (inBush) tex.SetPixel(x, y, y > 6 ? leafBright : leafDark);
                    else tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            _cachedPebbleBushSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 32f);
            return _cachedPebbleBushSprite;
        }

        private static Sprite GetOrCreateMilestoneSprite()
        {
            if (_cachedMilestoneSprite != null) return _cachedMilestoneSprite;
            const int w = 20;
            const int h = 34;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color woodDark = new Color(0.32f, 0.20f, 0.11f, 1f);
            Color woodLight = new Color(0.54f, 0.36f, 0.20f, 1f);
            Color goldTrim = new Color(0.90f, 0.72f, 0.30f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x >= 8 && x <= 11 && y >= 1 && y <= 31)
                    {
                        tex.SetPixel(x, y, x == 8 ? woodDark : woodLight);
                    }
                    else if (y >= 18 && y <= 27 && x >= 2 && x <= 17)
                    {
                        bool rim = y == 18 || y == 27 || x == 2 || x == 17;
                        tex.SetPixel(x, y, rim ? goldTrim : woodDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            _cachedMilestoneSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 32f);
            return _cachedMilestoneSprite;
        }
    }
}
