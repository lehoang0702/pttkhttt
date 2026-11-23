using System;
using System.Collections.Generic;
using UnityEngine;
using MedievalRPG.Combat;
using MedievalRPG.Data;

namespace MedievalRPG.Visuals
{
    /// <summary>
    /// Hiển thị trực tiếp cả 4 slot trang bị lên nhân vật 2D trong CampHub và BattleScene:
    /// - Đổi bảng màu 39 bộ Giáp theo Class (Palette Swap có cache).
    /// - Tách sạch vũ khí/mũ/sách bay/ống tên mặc định trên frame gốc trước khi gắn đồ mới để không bị chồng đè.
    /// - Gắn 4 lớp Sprite đồng bộ theo Idle/Run/Attack/FlipX: Vũ khí, Mũ, Phụ kiện và Áo choàng.
    /// </summary>
    public class CharacterEquipmentVisuals : MonoBehaviour
    {
        private readonly struct RemapCacheKey : IEquatable<RemapCacheKey>
        {
            public readonly EntityId sourceEntityId;
            public readonly string armorId;
            public readonly string weaponId;
            public readonly string helmetId;
            public readonly string accessoryId;

            public RemapCacheKey(
                EntityId sourceEntityId,
                string armorId,
                string weaponId,
                string helmetId,
                string accessoryId)
            {
                this.sourceEntityId = sourceEntityId;
                this.armorId = armorId ?? "";
                this.weaponId = weaponId ?? "";
                this.helmetId = helmetId ?? "";
                this.accessoryId = accessoryId ?? "";
            }

            public bool Equals(RemapCacheKey other) =>
                sourceEntityId == other.sourceEntityId
                && string.Equals(armorId, other.armorId, StringComparison.Ordinal)
                && string.Equals(weaponId, other.weaponId, StringComparison.Ordinal)
                && string.Equals(helmetId, other.helmetId, StringComparison.Ordinal)
                && string.Equals(accessoryId, other.accessoryId, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is RemapCacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = sourceEntityId.GetHashCode();
                    h = (h * 397) ^ armorId.GetHashCode();
                    h = (h * 397) ^ weaponId.GetHashCode();
                    h = (h * 397) ^ helmetId.GetHashCode();
                    h = (h * 397) ^ accessoryId.GetHashCode();
                    return h;
                }
            }
        }

        private readonly struct CloakCacheKey : IEquatable<CloakCacheKey>
        {
            public readonly Color32 primary;
            public readonly Color32 trim;

            public CloakCacheKey(Color32 primary, Color32 trim)
            {
                this.primary = primary;
                this.trim = trim;
            }

            public bool Equals(CloakCacheKey other) =>
                primary.r == other.primary.r && primary.g == other.primary.g && primary.b == other.primary.b && primary.a == other.primary.a
                && trim.r == other.trim.r && trim.g == other.trim.g && trim.b == other.trim.b && trim.a == other.trim.a;

            public override bool Equals(object obj) => obj is CloakCacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (primary.GetHashCode() * 397) ^ trim.GetHashCode();
                }
            }
        }

        private static readonly Dictionary<RemapCacheKey, Sprite> _remappedSpriteCache = new Dictionary<RemapCacheKey, Sprite>();
        private static readonly Dictionary<CloakCacheKey, Sprite> _cloakSpriteCache = new Dictionary<CloakCacheKey, Sprite>();
        private static Sprite _auraGlowSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _remappedSpriteCache.Clear();
            _cloakSpriteCache.Clear();
            _auraGlowSprite = null;
        }

        private Unit _unit;
        private PixelCharacterAnimator _animator;
        private SpriteRenderer _weaponRenderer;
        private SpriteRenderer _helmetRenderer;
        private SpriteRenderer _accessoryRenderer;
        private SpriteRenderer _cloakRenderer;
        private SpriteRenderer _weaponAuraRenderer;

        private CharacterClassType _classType = CharacterClassType.Knight;
        private string _weaponId = "";
        private int _weaponUpgrade;
        private string _helmetId = "";
        private int _helmetUpgrade;
        private string _armorId = "";
        private string _accessoryId = "";
        private int _accessoryUpgrade;
        private List<EquipmentVisualCatalog.ColorReplacement> _activeRules = new List<EquipmentVisualCatalog.ColorReplacement>();
        private bool _isSubscribed;

        public string EquippedWeaponId => _weaponId;
        public string EquippedHelmetId => _helmetId;
        public string EquippedArmorId => _armorId;
        public string EquippedAccessoryId => _accessoryId;

        public static CharacterEquipmentVisuals AttachOrRefresh(Unit unit)
        {
            if (unit == null) return null;
            CharacterEquipmentVisuals vis = unit.GetComponent<CharacterEquipmentVisuals>();
            if (vis == null)
            {
                if (unit.isEnemy) return null;
                vis = unit.gameObject.AddComponent<CharacterEquipmentVisuals>();
            }
            vis.RefreshFromEquipmentManager();
            return vis;
        }

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _animator = GetComponent<PixelCharacterAnimator>();
            if (_unit != null) _classType = _unit.characterClass;
        }

        private void OnEnable()
        {
            if (!_isSubscribed)
            {
                PlayerEquipmentManager.EnsureLoaded().OnEquipmentChanged += HandleEquipmentChanged;
                _isSubscribed = true;
            }
            RefreshFromEquipmentManager();
        }

        private void OnDisable()
        {
            if (_isSubscribed)
            {
                PlayerEquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
                _isSubscribed = false;
            }
        }

        private void HandleEquipmentChanged()
        {
            RefreshFromEquipmentManager();
        }

        public void RefreshFromEquipmentManager()
        {
            if (_unit == null) _unit = GetComponent<Unit>();
            if (_animator == null) _animator = GetComponent<PixelCharacterAnimator>();

            if (_unit != null && _unit.isEnemy)
            {
                ClearVisuals();
                return;
            }

            if (_unit != null) _classType = _unit.characterClass;

            PlayerEquipmentManager mgr = PlayerEquipmentManager.EnsureLoaded();
            OwnedEquipmentInstance wInst = mgr.GetEquippedInstance(_classType, EquipmentSlot.Weapon);
            OwnedEquipmentInstance hInst = mgr.GetEquippedInstance(_classType, EquipmentSlot.Helmet);
            OwnedEquipmentInstance aInst = mgr.GetEquippedInstance(_classType, EquipmentSlot.Armor);
            OwnedEquipmentInstance accInst = mgr.GetEquippedInstance(_classType, EquipmentSlot.Accessory);

            ApplyEquipmentState(
                mgr.ResolveData(wInst)?.itemId ?? "",
                wInst != null ? wInst.upgradeLevel : 0,
                mgr.ResolveData(aInst)?.itemId ?? "",
                mgr.ResolveData(accInst)?.itemId ?? "",
                accInst != null ? accInst.upgradeLevel : 0,
                mgr.ResolveData(hInst)?.itemId ?? "",
                hInst != null ? hInst.upgradeLevel : 0);
        }

        public void ApplyEquipmentState(
            string weaponItemId,
            int weaponUpgradeLevel,
            string armorItemId,
            string accessoryItemId,
            int accessoryUpgradeLevel = 0,
            string helmetItemId = "",
            int helmetUpgradeLevel = 0)
        {
            if (_unit == null) _unit = GetComponent<Unit>();
            if (_animator == null) _animator = GetComponent<PixelCharacterAnimator>();
            if (_unit != null) _classType = _unit.characterClass;

            _weaponId = weaponItemId ?? "";
            _weaponUpgrade = Mathf.Clamp(weaponUpgradeLevel, 0, EquipmentItemData.MaxUpgradeLevel);
            _helmetId = helmetItemId ?? "";
            _helmetUpgrade = Mathf.Clamp(helmetUpgradeLevel, 0, EquipmentItemData.MaxUpgradeLevel);
            _armorId = armorItemId ?? "";
            _accessoryId = accessoryItemId ?? "";
            _accessoryUpgrade = Mathf.Clamp(accessoryUpgradeLevel, 0, EquipmentItemData.MaxUpgradeLevel);

            _activeRules = EquipmentVisualCatalog.BuildColorReplacements(_classType, _armorId, _weaponId);

            bool needsFrameProcessing = !string.IsNullOrEmpty(_weaponId)
                || !string.IsNullOrEmpty(_helmetId)
                || !string.IsNullOrEmpty(_armorId)
                || !string.IsNullOrEmpty(_accessoryId);

            if (_animator != null)
            {
                _animator.SetSpriteOverrideResolver(needsFrameProcessing ? ResolveRemappedSprite : (Func<Sprite, Sprite>)null);
            }

            EnsureAttachmentRenderers();

            SetChildItemSprite(_weaponRenderer, EquipmentVisualCatalog.GetEquippedSpriteForItem(_weaponId, _classType, EquipmentSlot.Weapon));
            SetChildItemSprite(_helmetRenderer, EquipmentVisualCatalog.GetEquippedSpriteForItem(_helmetId, _classType, EquipmentSlot.Helmet));
            SetChildItemSprite(_accessoryRenderer, EquipmentVisualCatalog.GetEquippedSpriteForItem(_accessoryId, _classType, EquipmentSlot.Accessory));

            if (_cloakRenderer != null)
            {
                if (EquipmentVisualCatalog.TryGetCloakColors(_classType, _armorId, out Color primary, out Color trim))
                {
                    _cloakRenderer.sprite = GetOrCreateCloakSprite(primary, trim);
                    _cloakRenderer.gameObject.SetActive(true);
                }
                else
                {
                    _cloakRenderer.sprite = null;
                    _cloakRenderer.gameObject.SetActive(false);
                }
            }

            if (_weaponAuraRenderer != null)
            {
                Color glow = EquipmentVisualCatalog.GetGlowTintForItem(_weaponId);
                if (glow.a <= 0.01f && !string.IsNullOrEmpty(_weaponId) && _weaponUpgrade >= 3)
                {
                    glow = new Color(0.95f, 0.82f, 0.34f, 0.42f);
                }
                bool showAura = !string.IsNullOrEmpty(_weaponId) && glow.a > 0.01f;
                _weaponAuraRenderer.gameObject.SetActive(showAura);
                if (showAura)
                {
                    _weaponAuraRenderer.sprite = GetOrCreateAuraSprite();
                    _weaponAuraRenderer.color = glow;
                }
            }

            UpdateAttachmentTransforms();
        }

        private static void SetChildItemSprite(SpriteRenderer sr, Sprite sp)
        {
            if (sr == null) return;
            sr.sprite = sp;
            sr.gameObject.SetActive(sp != null);
        }

        public void ClearVisuals()
        {
            _weaponId = "";
            _helmetId = "";
            _armorId = "";
            _accessoryId = "";
            _activeRules.Clear();
            if (_animator != null) _animator.SetSpriteOverrideResolver(null);
            if (_weaponRenderer != null) _weaponRenderer.gameObject.SetActive(false);
            if (_helmetRenderer != null) _helmetRenderer.gameObject.SetActive(false);
            if (_accessoryRenderer != null) _accessoryRenderer.gameObject.SetActive(false);
            if (_cloakRenderer != null) _cloakRenderer.gameObject.SetActive(false);
            if (_weaponAuraRenderer != null) _weaponAuraRenderer.gameObject.SetActive(false);
        }

        private void EnsureAttachmentRenderers()
        {
            if (_weaponRenderer == null) _weaponRenderer = GetOrCreateChildRenderer("Equip_Weapon");
            if (_helmetRenderer == null) _helmetRenderer = GetOrCreateChildRenderer("Equip_Helmet");
            if (_accessoryRenderer == null) _accessoryRenderer = GetOrCreateChildRenderer("Equip_Accessory");
            if (_cloakRenderer == null) _cloakRenderer = GetOrCreateChildRenderer("Equip_Cloak");
            if (_weaponAuraRenderer == null) _weaponAuraRenderer = GetOrCreateChildRenderer("Equip_GlowAura");
        }

        private SpriteRenderer GetOrCreateChildRenderer(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName);
            if (existing == null) go.transform.SetParent(transform, false);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            if (_animator != null && _animator.SpriteRendererComponent != null)
            {
                sr.sharedMaterial = _animator.SpriteRendererComponent.sharedMaterial;
            }
            return sr;
        }

        private void LateUpdate()
        {
            if (_unit != null && _unit.isEnemy)
            {
                if (_weaponRenderer != null && _weaponRenderer.gameObject.activeSelf)
                {
                    ClearVisuals();
                }
                return;
            }
            UpdateAttachmentTransforms();
        }

        private void UpdateAttachmentTransforms()
        {
            if (_animator == null) _animator = GetComponent<PixelCharacterAnimator>();
            SpriteRenderer baseSr = _animator != null ? _animator.SpriteRendererComponent : GetComponent<SpriteRenderer>();
            if (baseSr == null) return;

            bool facingRight = !baseSr.flipX;
            int baseOrder = baseSr.sortingOrder;
            PixelAnimState state = _animator != null ? _animator.CurrentState : PixelAnimState.Idle;
            int frameIdx = _animator != null ? _animator.CurrentFrameIndex : 0;
            bool visible = baseSr.enabled && state != PixelAnimState.Death;

            EquipmentVisualCatalog.VisualAnimState vState = state switch
            {
                PixelAnimState.Run => EquipmentVisualCatalog.VisualAnimState.Run,
                PixelAnimState.Attack => EquipmentVisualCatalog.VisualAnimState.Attack,
                PixelAnimState.Hit => EquipmentVisualCatalog.VisualAnimState.Hit,
                _ => EquipmentVisualCatalog.VisualAnimState.Idle
            };

            Sprite baseSprite = baseSr.sprite;
            string frameName = baseSprite != null ? baseSprite.name : "";
            EquipmentVisualCatalog.TryGetFrameAnchor(_classType, frameName, vState, frameIdx, out EquipmentVisualCatalog.CharacterFrameAnchor anchor);
            GetCharFrameMetrics(_classType, baseSprite, out float pivPxX, out float pivTopY, out float charPpu);

            float t = Application.isPlaying ? Time.time : 0f;

            // 1. Vũ khí (ghim chính xác điểm grip của icon vào bàn tay từng frame)
            if (_weaponRenderer != null && _weaponRenderer.sprite != null)
            {
                bool showWeapon = visible && anchor.wpnVisible;
                _weaponRenderer.enabled = showWeapon;
                if (showWeapon)
                {
                    var wProfile = EquipmentVisualCatalog.GetItemVisualProfile(_classType, EquipmentSlot.Weapon, _weaponId);
                    float wpnRotZ = anchor.wpnRotZ + wProfile.angleOffset;
                    PlaceAnchoredSprite(
                        _weaponRenderer,
                        anchor.wpnX, anchor.wpnY, wpnRotZ,
                        wProfile.anchorX, wProfile.anchorY,
                        wProfile.charPixelSize,
                        facingRight, baseOrder + anchor.wpnOrderOff, -0.01f,
                        pivPxX, pivTopY, charPpu);

                    if (_weaponAuraRenderer != null && _weaponAuraRenderer.gameObject.activeSelf)
                    {
                        _weaponAuraRenderer.enabled = true;
                        Vector3 wpnPos = _weaponRenderer.transform.localPosition;
                        if (!facingRight) wpnPos.x = -wpnPos.x;
                        Vector3 auraOffset = _classType == CharacterClassType.Mage
                            ? wpnPos + (Quaternion.Euler(0f, 0f, wpnRotZ) * new Vector3(0f, 0.11f, 0f))
                            : wpnPos;
                        float auraScale = (wProfile.charPixelSize / charPpu) * 1.35f;
                        ApplyRawTransform(_weaponAuraRenderer, auraOffset, 0f, auraScale, facingRight, baseOrder + anchor.wpnOrderOff - 1);
                        Color baseGlow = EquipmentVisualCatalog.GetGlowTintForItem(_weaponId);
                        if (baseGlow.a <= 0.01f) baseGlow = new Color(0.95f, 0.82f, 0.34f, 0.42f);
                        float pulse = 0.72f + 0.28f * Mathf.Sin(t * 4.5f);
                        _weaponAuraRenderer.color = new Color(baseGlow.r, baseGlow.g, baseGlow.b, baseGlow.a * pulse);
                    }
                }
                else if (_weaponAuraRenderer != null)
                {
                    _weaponAuraRenderer.enabled = false;
                }
            }
            else if (_weaponAuraRenderer != null)
            {
                _weaponAuraRenderer.enabled = false;
            }

            // 2. Mũ (ghim đúng điểm tâm sọ/vành của từng loại mũ vào đầu từng frame)
            if (_helmetRenderer != null && _helmetRenderer.sprite != null)
            {
                _helmetRenderer.enabled = visible;
                if (visible)
                {
                    var hProfile = EquipmentVisualCatalog.GetItemVisualProfile(_classType, EquipmentSlot.Helmet, _helmetId);
                    float dxHead = (_classType == CharacterClassType.Archer && hProfile.helmMode == EquipmentVisualCatalog.HelmetVisualMode.CutoutHood) ? -1.5f : 0f;
                    PlaceAnchoredSprite(
                        _helmetRenderer,
                        anchor.headX + dxHead, anchor.headY + hProfile.yOffsetPx, anchor.headRotZ,
                        hProfile.anchorX, hProfile.anchorY,
                        hProfile.charPixelSize,
                        facingRight, baseOrder + 2, -0.02f,
                        pivPxX, pivTopY, charPpu);
                }
            }

            // 3. Phụ kiện (tự phân bố đúng vị trí cơ thể theo loại phụ kiện & từng frame)
            if (_accessoryRenderer != null && _accessoryRenderer.sprite != null)
            {
                _accessoryRenderer.enabled = visible;
                if (visible)
                {
                    var aProfile = EquipmentVisualCatalog.GetItemVisualProfile(_classType, EquipmentSlot.Accessory, _accessoryId);
                    GetAccessoryTarget(anchor, aProfile, t, out float ax, out float ay, out float arot, out int aorder, out float az);
                    PlaceAnchoredSprite(
                        _accessoryRenderer,
                        ax, ay, arot,
                        aProfile.anchorX, aProfile.anchorY,
                        aProfile.charPixelSize,
                        facingRight, baseOrder + aorder, az,
                        pivPxX, pivTopY, charPpu);
                }
            }

            // 4. Áo choàng (bám chặt ngay bả vai sau lưng từng frame, phấp phới nhẹ theo chuyển động)
            if (_cloakRenderer != null && _cloakRenderer.sprite != null)
            {
                _cloakRenderer.enabled = visible;
                if (visible)
                {
                    float flutter = Mathf.Sin(t * (state == PixelAnimState.Run ? 11f : 2.8f)) * (state == PixelAnimState.Run ? 7f : 2.5f);
                    float cloakScale = _classType switch
                    {
                        CharacterClassType.Knight => 0.96f,
                        CharacterClassType.Mage => 0.94f,
                        CharacterClassType.Archer => 0.88f,
                        _ => 0.95f
                    };
                    Vector3 cloakLocalPos = new Vector3(
                        (anchor.cloakX - pivPxX) / charPpu,
                        (pivTopY - anchor.cloakY) / charPpu,
                        0.02f);
                    ApplyRawTransform(_cloakRenderer, cloakLocalPos, anchor.cloakRotZ + flutter, cloakScale, facingRight, baseOrder - 2);
                }
            }
        }

        private static void GetCharFrameMetrics(
            CharacterClassType cls,
            Sprite baseSprite,
            out float pivPxX,
            out float pivTopY,
            out float charPpu)
        {
            if (baseSprite != null && baseSprite.rect.width > 1f && baseSprite.rect.height > 1f)
            {
                pivPxX = baseSprite.pivot.x;
                pivTopY = baseSprite.rect.height - baseSprite.pivot.y;
                charPpu = baseSprite.pixelsPerUnit > 0.01f ? baseSprite.pixelsPerUnit : 70f;
                return;
            }

            switch (cls)
            {
                case CharacterClassType.Mage:
                    pivPxX = 115.5f;
                    pivTopY = 136.8f;
                    charPpu = 95f;
                    break;
                case CharacterClassType.Archer:
                    pivPxX = 50f;
                    pivTopY = 70f;
                    charPpu = 55f;
                    break;
                default:
                    pivPxX = 70f;
                    pivTopY = 91f;
                    charPpu = 70f;
                    break;
            }
        }

        private static void GetAccessoryTarget(
            EquipmentVisualCatalog.CharacterFrameAnchor anchor,
            EquipmentVisualCatalog.ItemVisualProfile profile,
            float time,
            out float tx,
            out float ty,
            out float rotZ,
            out int orderOff,
            out float zOff)
        {
            switch (profile.accCategory)
            {
                case EquipmentVisualCatalog.AccessoryVisualCategory.Shield:
                    tx = anchor.accX;
                    ty = anchor.accY;
                    rotZ = anchor.accRotZ;
                    orderOff = anchor.accOrderOff;
                    zOff = -0.015f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.Gauntlet:
                    tx = anchor.accX - 1.5f;
                    ty = anchor.accY;
                    rotZ = anchor.accRotZ;
                    orderOff = anchor.accOrderOff;
                    zOff = -0.015f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.Collar:
                    tx = anchor.collarX;
                    ty = anchor.collarY;
                    rotZ = anchor.headRotZ - 6f;
                    orderOff = 1;
                    zOff = -0.012f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.Belt:
                    tx = anchor.beltX;
                    ty = anchor.beltY;
                    rotZ = anchor.headRotZ;
                    orderOff = 1;
                    zOff = -0.012f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.Badge:
                    tx = anchor.chestX;
                    ty = anchor.chestY;
                    rotZ = anchor.headRotZ;
                    orderOff = 1;
                    zOff = -0.012f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.MageFloating:
                    float floatPxY = Mathf.Sin(time * 3.6f) * 1.4f;
                    tx = anchor.accX;
                    ty = anchor.accY + floatPxY;
                    rotZ = anchor.accRotZ + Mathf.Sin(time * 2.2f) * 5f;
                    orderOff = anchor.accOrderOff;
                    zOff = anchor.accOrderOff < 0 ? 0.015f : -0.015f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.ArcherQuiver:
                    tx = anchor.accX;
                    ty = anchor.accY;
                    rotZ = anchor.accRotZ;
                    orderOff = anchor.accOrderOff;
                    zOff = 0.015f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.ArcherWing:
                    tx = anchor.accX - 1.5f;
                    ty = anchor.accY + 1.5f;
                    rotZ = anchor.accRotZ * 0.4f;
                    orderOff = anchor.accOrderOff;
                    zOff = 0.015f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.ArcherFeather:
                    tx = anchor.headX - 4.5f;
                    ty = anchor.headY - 2.5f;
                    rotZ = anchor.headRotZ + 15f;
                    orderOff = -1;
                    zOff = 0.012f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.ArcherGlove:
                    tx = anchor.gloveX;
                    ty = anchor.gloveY;
                    rotZ = 0f;
                    orderOff = 1;
                    zOff = -0.012f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.ArcherScope:
                    tx = anchor.eyeX + 1.0f;
                    ty = anchor.eyeY - 0.5f;
                    rotZ = 0f;
                    orderOff = 3;
                    zOff = -0.025f;
                    break;

                case EquipmentVisualCatalog.AccessoryVisualCategory.ArcherNecklace:
                    tx = anchor.collarX;
                    ty = anchor.collarY;
                    rotZ = 0f;
                    orderOff = 1;
                    zOff = -0.012f;
                    break;

                default:
                    tx = anchor.accX;
                    ty = anchor.accY;
                    rotZ = anchor.accRotZ;
                    orderOff = anchor.accOrderOff;
                    zOff = -0.015f;
                    break;
            }
        }

        private static void PlaceAnchoredSprite(
            SpriteRenderer sr,
            float targetCharX,
            float targetCharYTop,
            float rotZ,
            float itemAnchorX,
            float itemAnchorY,
            float charPxSize,
            bool facingRight,
            int sortingOrder,
            float zOffset,
            float pivPxX,
            float pivTopY,
            float charPpu)
        {
            Sprite sp = sr.sprite;
            float targetWorldSize = charPxSize / charPpu;
            float uniformScale = ComputeNormalizedEquipmentScale(sp, targetWorldSize);

            float targetLocalX = (targetCharX - pivPxX) / charPpu;
            float targetLocalY = (pivTopY - targetCharYTop) / charPpu;

            if (sp != null)
            {
                float sh = sp.rect.height;
                float ipx = sp.pivot.x;
                float ipyTop = sh - sp.pivot.y;
                float itemPpu = sp.pixelsPerUnit > 0.01f ? sp.pixelsPerUnit : 100f;

                float offWx = ((ipx - itemAnchorX) / itemPpu) * uniformScale;
                float offWy = ((itemAnchorY - ipyTop) / itemPpu) * uniformScale;
                float rad = rotZ * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                targetLocalX += offWx * cos - offWy * sin;
                targetLocalY += offWx * sin + offWy * cos;
            }

            ApplyRawTransform(sr, new Vector3(targetLocalX, targetLocalY, zOffset), rotZ, uniformScale, facingRight, sortingOrder);
        }

        public static float ComputeNormalizedEquipmentScale(Sprite sprite, float targetWorldSize)
        {
            float maxDim = sprite != null
                ? Mathf.Max(sprite.rect.width, sprite.rect.height)
                : 32f;
            float ppuNorm = sprite != null && maxDim > 0.01f
                ? sprite.pixelsPerUnit / maxDim
                : 100f / 32f;
            return targetWorldSize * ppuNorm;
        }

        private static void ApplyRawTransform(
            SpriteRenderer sr,
            Vector3 localPos,
            float rotZ,
            float uniformScale,
            bool facingRight,
            int sortingOrder)
        {
            float mirror = facingRight ? 1f : -1f;
            sr.transform.localPosition = new Vector3(localPos.x * mirror, localPos.y, localPos.z);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ * mirror);
            sr.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
            sr.flipX = !facingRight;
            sr.sortingOrder = sortingOrder;
        }

        private Sprite ResolveRemappedSprite(Sprite rawSprite)
        {
            if (rawSprite == null) return null;

            bool needsProcessing = !string.IsNullOrEmpty(_weaponId)
                || !string.IsNullOrEmpty(_helmetId)
                || !string.IsNullOrEmpty(_armorId)
                || !string.IsNullOrEmpty(_accessoryId);
            if (!needsProcessing) return rawSprite;

            RemapCacheKey key = new RemapCacheKey(rawSprite.GetEntityId(), _armorId, _weaponId, _helmetId, _accessoryId);
            if (_remappedSpriteCache.TryGetValue(key, out Sprite cached))
            {
                return cached != null ? cached : rawSprite;
            }

            Texture2D srcTex = rawSprite.texture;
            if (srcTex == null || !srcTex.isReadable)
            {
                return rawSprite;
            }

            Rect fullRect = rawSprite.rect;
            int fullW = Mathf.RoundToInt(fullRect.width);
            int fullH = Mathf.RoundToInt(fullRect.height);
            Rect texRect = rawSprite.textureRect;
            int tw = Mathf.RoundToInt(texRect.width);
            int th = Mathf.RoundToInt(texRect.height);
            int rx = Mathf.RoundToInt(texRect.x);
            int ry = Mathf.RoundToInt(texRect.y);
            if (fullW <= 0 || fullH <= 0 || tw <= 0 || th <= 0) return rawSprite;

            // Khôi phục pixel từ textureRect (có thể bị Unity Tight-crop) về đúng canvas đầy đủ (rawSprite.rect)
            // nhằm giữ nguyên hệ tọa độ xóa đồ cũ và vị trí pivot gốc của từng frame.
            Color[] srcColors = srcTex.GetPixels(rx, ry, tw, th);
            Color32[] pixels = new Color32[fullW * fullH];
            Vector2 rectOffset = rawSprite.textureRectOffset;
            int ox = Mathf.Clamp(Mathf.RoundToInt(rectOffset.x), 0, Mathf.Max(0, fullW - tw));
            int oy = Mathf.Clamp(Mathf.RoundToInt(rectOffset.y), 0, Mathf.Max(0, fullH - th));

            for (int y = 0; y < th; y++)
            {
                int dstRow = (oy + y) * fullW + ox;
                int srcRow = y * tw;
                for (int x = 0; x < tw; x++)
                {
                    pixels[dstRow + x] = srcColors[srcRow + x];
                }
            }

            bool changed = EquipmentVisualCatalog.RemapAndCleanPixels(
                pixels,
                fullW,
                fullH,
                _classType,
                rawSprite.name,
                _armorId,
                _weaponId,
                _helmetId,
                _accessoryId,
                _activeRules);

            if (!changed)
            {
                _remappedSpriteCache[key] = rawSprite;
                return rawSprite;
            }

            Texture2D newTex = new Texture2D(fullW, fullH, TextureFormat.RGBA32, false);
            newTex.filterMode = FilterMode.Point;
            newTex.wrapMode = TextureWrapMode.Clamp;
            newTex.SetPixels32(pixels);
            newTex.Apply(false, true);

            Vector2 normPivot = new Vector2(rawSprite.pivot.x / fullW, rawSprite.pivot.y / fullH);
            Sprite remapped = Sprite.Create(newTex, new Rect(0, 0, fullW, fullH), normPivot, rawSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            remapped.name = $"{rawSprite.name}|{_armorId}|{_weaponId}|{_helmetId}|{_accessoryId}";
            _remappedSpriteCache[key] = remapped;
            return remapped;
        }

        private static Sprite GetOrCreateCloakSprite(Color primary, Color trim)
        {
            Color32 p32 = primary;
            Color32 t32 = trim;
            CloakCacheKey key = new CloakCacheKey(p32, t32);
            if (_cloakSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            const int w = 14, h = 19;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(0.086f, 0.07f, 0.118f, 1f);
            Color dark = new Color(primary.r * 0.60f, primary.g * 0.60f, primary.b * 0.60f, 1f);
            Color hi = new Color(Mathf.Min(1f, primary.r * 1.18f), Mathf.Min(1f, primary.g * 1.18f), Mathf.Min(1f, primary.b * 1.18f), 1f);

            for (int yBottom = 0; yBottom < h; yBottom++)
            {
                int yTop = h - 1 - yBottom;
                if (yTop < 1 || yTop > 17)
                {
                    for (int x = 0; x < w; x++) tex.SetPixel(x, yBottom, clear);
                    continue;
                }

                float t = (yTop - 1) / 16f;
                int left = Mathf.RoundToInt(9.0f - 6.8f * Mathf.Pow(t, 0.78f));
                int right = Mathf.RoundToInt(12.2f - 3.8f * Mathf.Pow(t, 1.15f));
                for (int x = 0; x < w; x++)
                {
                    if (x < left || x > right)
                    {
                        tex.SetPixel(x, yBottom, clear);
                        continue;
                    }

                    bool isBorder = (x == left || x == right || yTop == 1 || yTop == 17);
                    if (isBorder) tex.SetPixel(x, yBottom, outline);
                    else if (yTop == 16) tex.SetPixel(x, yBottom, trim);
                    else if (x == left + 1) tex.SetPixel(x, yBottom, hi);
                    else if (x == right - 1) tex.SetPixel(x, yBottom, dark);
                    else tex.SetPixel(x, yBottom, primary);
                }
            }

            tex.Apply(false, true);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(11f / w, 1f - 2.5f / h), 70f);
            _cloakSpriteCache[key] = sp;
            return sp;
        }

        private static Sprite GetOrCreateAuraSprite()
        {
            if (_auraGlowSprite != null) return _auraGlowSprite;
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(15.5f, 15.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / 15.5f;
                    float a = d >= 1f ? 0f : Mathf.Pow(1f - d, 2.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply(false, true);
            _auraGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _auraGlowSprite;
        }
    }
}
