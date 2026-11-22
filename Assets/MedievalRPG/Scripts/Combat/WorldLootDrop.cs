using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Data;
using MedievalRPG.UI;

namespace MedievalRPG.Combat
{
    /// <summary>
    /// Vật phẩm / rương chiến lợi phẩm rơi trực tiếp xuống mặt đất trên chiến trường 2D.
    /// Có hoạt ảnh nảy parabol, cột tia sáng phát hào quang theo đúng cấp độ hiếm
    /// (Thường / Hiếm / Sử Thi / Huyền Thoại), nhãn tên Tiếng Việt và click chuột hoặc [E] để nhặt.
    /// </summary>
    public class WorldLootDrop : MonoBehaviour
    {
        private static readonly Dictionary<ItemRarity, Sprite> _cachedChestSprites = new Dictionary<ItemRarity, Sprite>();
        private static Sprite _cachedBeamSprite;
        private static Sprite _cachedPillSprite;

        public LootDropResult DropData { get; private set; }
        public bool IsCollected { get; private set; }

        private SpriteRenderer _iconRenderer;
        private SpriteRenderer _beamRenderer;
        private BoxCollider2D _collider;
        private GameObject _labelRoot;
        private Vector3 _restPosition;
        private float _bobPhase;
        private bool _isLanded;
        private Color _rarityColor;
        private Coroutine _activeRoutine;

        public static WorldLootDrop Spawn(
            LootDropResult data,
            Vector3 startPos,
            Vector3 targetGroundPos,
            TMP_FontAsset labelFont = null)
        {
            GameObject root = new GameObject($"LootDrop_{data.DisplayName}");
            root.transform.position = startPos;

            WorldLootDrop drop = root.AddComponent<WorldLootDrop>();
            drop.Initialize(data, startPos, targetGroundPos, labelFont);
            return drop;
        }

        private void Initialize(LootDropResult data, Vector3 startPos, Vector3 groundPos, TMP_FontAsset labelFont)
        {
            DropData = data;
            _restPosition = groundPos;
            _bobPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _rarityColor = EquipmentItemData.GetRarityColor(data.DisplayRarity);

            // 1. Sprite rương / túi bảo vật trung cổ vẽ thủ tục theo màu độ hiếm
            _iconRenderer = gameObject.AddComponent<SpriteRenderer>();
            _iconRenderer.sprite = GetOrCreateChestSprite(data.DisplayRarity, data.isConsumableOnly);
            _iconRenderer.sortingOrder = 15;
            transform.localScale = data.DisplayRarity >= ItemRarity.Epic ? Vector3.one * 1.15f : Vector3.one;

            // 2. Cột tia sáng (Loot Beam) theo cấp độ hiếm
            GameObject beamGo = new GameObject("RarityBeam");
            beamGo.transform.SetParent(transform, false);
            beamGo.transform.localPosition = new Vector3(0f, 0.55f, 0.02f);
            float beamWidth = data.DisplayRarity switch
            {
                ItemRarity.Legendary => 0.42f,
                ItemRarity.Epic => 0.32f,
                ItemRarity.Rare => 0.22f,
                _ => 0.14f
            };
            float beamHeight = data.DisplayRarity switch
            {
                ItemRarity.Legendary => 2.15f,
                ItemRarity.Epic => 1.65f,
                ItemRarity.Rare => 1.15f,
                _ => 0.75f
            };
            beamGo.transform.localScale = new Vector3(beamWidth, beamHeight, 1f);
            _beamRenderer = beamGo.AddComponent<SpriteRenderer>();
            _beamRenderer.sprite = GetOrCreateBeamSprite();
            _beamRenderer.color = new Color(_rarityColor.r, _rarityColor.g, _rarityColor.b, data.DisplayRarity >= ItemRarity.Epic ? 0.62f : 0.36f);
            _beamRenderer.sortingOrder = 14;

            // 3. Collider đủ rộng để người chơi click nhặt dễ dàng
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = new Vector2(0.95f, 1.05f);
            _collider.offset = new Vector2(0f, 0.12f);

            // 4. Nhãn tên món đồ Tiếng Việt phía trên vật phẩm
            BuildWorldLabel(data, labelFont);

            _activeRoutine = StartCoroutine(BounceDropRoutine(startPos, groundPos));
        }

        private void BuildWorldLabel(LootDropResult data, TMP_FontAsset font)
        {
            _labelRoot = new GameObject("LootLabelCanvas");
            _labelRoot.transform.SetParent(transform, false);
            _labelRoot.transform.localPosition = new Vector3(0f, 0.58f, -0.05f);
            _labelRoot.transform.localScale = Vector3.one * 0.0082f;

            Canvas canvas = _labelRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 25;

            RectTransform cRt = _labelRoot.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(260f, 44f);

            Image bg = _labelRoot.AddComponent<Image>();
            bg.sprite = GetOrCreatePillSprite();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);
            bg.raycastTarget = false;

            // Viền mảnh theo độ hiếm
            Outline outline = _labelRoot.AddComponent<Outline>();
            outline.effectColor = _rarityColor;
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(_labelRoot.transform, false);
            TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            else if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;

            string prefix = data.equipment != null
                ? $"[{EquipmentItemData.GetRarityDisplayName(data.equipment.rarity)}] "
                : "+1 ";
            tmp.text = $"{prefix}{data.DisplayName}";
            tmp.fontSize = 18f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12f;
            tmp.fontSizeMax = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = _rarityColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;

            RectTransform tRt = tmp.rectTransform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(8f, 2f);
            tRt.offsetMax = new Vector2(-8f, -2f);
        }

        private IEnumerator BounceDropRoutine(Vector3 startPos, Vector3 endPos)
        {
            float duration = 0.48f;
            float elapsed = 0f;
            float arcHeight = 0.85f + (int)DropData.DisplayRarity * 0.14f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                float bounce = Mathf.Sin(t * Mathf.PI) * arcHeight;
                if (t > 0.72f)
                {
                    float subT = (t - 0.72f) / 0.28f;
                    bounce += Mathf.Sin(subT * Mathf.PI) * 0.18f;
                }
                pos.y += bounce;
                transform.position = pos;
                yield return null;
            }

            transform.position = endPos;
            _restPosition = endPos;
            _isLanded = true;
        }

        private void Update()
        {
            if (IsCollected || !_isLanded) return;

            float wave = Mathf.Sin(Time.unscaledTime * 3.6f + _bobPhase);
            transform.position = _restPosition + new Vector3(0f, wave * 0.055f, 0f);

            if (_beamRenderer != null)
            {
                float baseAlpha = DropData.DisplayRarity >= ItemRarity.Epic ? 0.56f : 0.30f;
                float pulse = baseAlpha + 0.18f * Mathf.Sin(Time.unscaledTime * 5.2f + _bobPhase);
                _beamRenderer.color = new Color(_rarityColor.r, _rarityColor.g, _rarityColor.b, pulse);
            }
        }

        public void PlayCollectAndDestroy(Vector3 flyTargetWorldPos, Action onDone = null)
        {
            if (IsCollected) return;
            IsCollected = true;
            if (_collider != null) _collider.enabled = false;
            if (_labelRoot != null) _labelRoot.SetActive(false);

            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(FlyToInventoryRoutine(flyTargetWorldPos, onDone));
        }

        private IEnumerator FlyToInventoryRoutine(Vector3 targetPos, Action onDone)
        {
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            float duration = 0.28f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t;
                Vector3 p = Vector3.Lerp(startPos, targetPos, eased);
                p.y += Mathf.Sin(t * Mathf.PI) * 0.65f;
                transform.position = p;
                transform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.25f, t);
                yield return null;
            }

            onDone?.Invoke();
            Destroy(gameObject);
        }

        private static Sprite GetOrCreateChestSprite(ItemRarity rarity, bool isConsumable)
        {
            ItemRarity key = isConsumable ? ItemRarity.Common : rarity;
            if (!isConsumable && _cachedChestSprites.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            const int size = 20;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++) tex.SetPixel(x, y, clear);
            }

            Color accent = EquipmentItemData.GetRarityColor(rarity);
            Color darkOutline = new Color(0.08f, 0.08f, 0.11f, 1f);
            Color woodBody = isConsumable
                ? new Color(0.24f, 0.56f, 0.42f, 1f)
                : new Color(0.46f, 0.28f, 0.15f, 1f);
            Color woodLight = isConsumable
                ? new Color(0.36f, 0.78f, 0.58f, 1f)
                : new Color(0.62f, 0.39f, 0.21f, 1f);
            Color trim = Color.Lerp(accent, Color.white, 0.18f);

            // Vẽ hình chiếc rương bảo vật 16x13 cân đối ở giữa texture 20x20
            for (int y = 3; y <= 15; y++)
            {
                for (int x = 2; x <= 17; x++)
                {
                    bool border = x == 2 || x == 17 || y == 3 || y == 15;
                    if (border)
                    {
                        tex.SetPixel(x, y, darkOutline);
                    }
                    else if (y == 10 || x == 4 || x == 15)
                    {
                        tex.SetPixel(x, y, trim);
                    }
                    else if (y >= 11)
                    {
                        tex.SetPixel(x, y, woodLight);
                    }
                    else
                    {
                        tex.SetPixel(x, y, woodBody);
                    }
                }
            }

            // Viên ngọc khóa phát sáng màu độ hiếm ở chính giữa nắp rương
            for (int y = 8; y <= 11; y++)
            {
                for (int x = 8; x <= 11; x++)
                {
                    bool gemCore = (x == 9 || x == 10) && (y == 9 || y == 10);
                    tex.SetPixel(x, y, gemCore ? Color.white : accent);
                }
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.35f), 32f);
            if (!isConsumable) _cachedChestSprites[key] = sp;
            return sp;
        }

        private static Sprite GetOrCreateBeamSprite()
        {
            if (_cachedBeamSprite != null) return _cachedBeamSprite;
            const int w = 16;
            const int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                float vFade = Mathf.Pow(1f - ny, 1.45f) * Mathf.Clamp01(ny * 6f);
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x / (float)(w - 1)) * 2f - 1f);
                    float hFade = Mathf.Pow(1f - nx, 1.8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, vFade * hFade));
                }
            }
            tex.Apply();
            _cachedBeamSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
            return _cachedBeamSprite;
        }

        private static Sprite GetOrCreatePillSprite()
        {
            if (_cachedPillSprite != null) return _cachedPillSprite;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _cachedPillSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
            return _cachedPillSprite;
        }
    }
}
