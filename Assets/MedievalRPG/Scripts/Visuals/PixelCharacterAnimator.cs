using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Visuals
{
    public enum PixelAnimState
    {
        Idle,
        Run,
        Attack,
        Hit,
        Death
    }

    /// <summary>
    /// Bộ hoạt ảnh khung hình pixel (Idle / Run / Attack / Hit / Death) với callback tại thời điểm va chạm,
    /// giữ đúng màu gốc (Boss tint), hỗ trợ bước vào trận, hồi sinh và fallback death khi thiếu frame chết.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelCharacterAnimator : MonoBehaviour
    {
        [Header("--- Thành phần hiển thị ---")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("--- Danh sách các khung hình (Sprites) ---")]
        public List<Sprite> idleFrames = new List<Sprite>();
        public List<Sprite> runFrames = new List<Sprite>();
        public List<Sprite> attackFrames = new List<Sprite>();
        public List<Sprite> hitFrames = new List<Sprite>();
        public List<Sprite> deathFrames = new List<Sprite>();

        [Header("--- Tốc độ và Hiệu ứng chuyển động ---")]
        [SerializeField] private float frameRate = 10f;
        [SerializeField] private float attackStepDistance = 0.8f;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

        private PixelAnimState _currentState = PixelAnimState.Idle;
        private Coroutine _currentAnimCoroutine;
        private Vector3 _originalPosition;
        private Vector3 _baseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private Quaternion _baseRotation = Quaternion.identity;
        private bool _isActionLocked;
        private bool _isDead;
        private bool _isTargetHighlighted;
        private GameObject _footShadowGo;
        private int _currentFrameIndex;
        private Sprite _lastRawSprite;
        private Func<Sprite, Sprite> _spriteOverrideResolver;

        private static Sprite _cachedShadowSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _cachedShadowSprite = null;
        }

        public PixelAnimState CurrentState => _currentState;
        public int CurrentFrameIndex => _currentFrameIndex;
        public Vector3 HomePosition => _originalPosition;
        public SpriteRenderer SpriteRendererComponent
        {
            get
            {
                if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
                return spriteRenderer;
            }
        }

        public void SetSpriteOverrideResolver(Func<Sprite, Sprite> resolver)
        {
            _spriteOverrideResolver = resolver;
            RefreshCurrentFrameSprite();
        }

        public void RefreshCurrentFrameSprite()
        {
            SpriteRenderer sr = SpriteRendererComponent;
            if (sr == null) return;
            Sprite source = _lastRawSprite != null ? _lastRawSprite : sr.sprite;
            if (source == null) return;
            sr.sprite = _spriteOverrideResolver != null ? _spriteOverrideResolver(source) : source;
        }

        private void ApplyFrameSprite(Sprite rawFrame, int frameIndex)
        {
            if (rawFrame == null) return;
            _lastRawSprite = rawFrame;
            _currentFrameIndex = frameIndex;
            SpriteRenderer sr = SpriteRendererComponent;
            if (sr != null)
            {
                sr.sprite = _spriteOverrideResolver != null ? _spriteOverrideResolver(rawFrame) : rawFrame;
            }
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                _baseColor = spriteRenderer.color;
            }
            _originalPosition = transform.position;
            _baseScale = transform.localScale;
            _baseRotation = transform.rotation;
        }

        public void CaptureBaseScale()
        {
            _baseScale = transform.localScale;
        }

        private void Start()
        {
            EnsureFootShadow();
            if (!_isActionLocked && !_isDead)
            {
                PlayIdle();
            }
        }

        private void EnsureFootShadow()
        {
            if (_footShadowGo == null)
            {
                Transform existing = transform.Find("FootShadow");
                if (existing != null)
                {
                    _footShadowGo = existing.gameObject;
                }
                else
                {
                    _footShadowGo = new GameObject("FootShadow");
                    _footShadowGo.transform.SetParent(transform, false);
                    SpriteRenderer shadowSr = _footShadowGo.AddComponent<SpriteRenderer>();
                    shadowSr.sprite = GetOrCreateShadowSprite();
                    shadowSr.color = new Color(0.03f, 0.04f, 0.07f, 0.42f);
                    shadowSr.sortingOrder = -1;
                }
            }

            Sprite refSprite = (idleFrames != null && idleFrames.Count > 0 && idleFrames[0] != null)
                ? idleFrames[0]
                : (spriteRenderer != null ? spriteRenderer.sprite : null);

            if (refSprite != null)
            {
                float sx = Mathf.Max(0.1f, Mathf.Abs(transform.localScale.x));
                float sy = Mathf.Max(0.1f, Mathf.Abs(transform.localScale.y));
                Bounds b = refSprite.bounds;
                float worldShadowW = Mathf.Clamp(b.size.x * sx * 1.12f, 0.72f, 1.32f);
                float worldShadowH = worldShadowW * 0.28f;
                // Sprite bóng gốc có kích thước 1.0 x 0.5 world units (32x16 @ 32 PPU)
                _footShadowGo.transform.localPosition = new Vector3(0f, b.min.y - (0.02f / sy), 0.02f);
                _footShadowGo.transform.localScale = new Vector3(worldShadowW / sx, (worldShadowH / 0.5f) / sy, 1f);
            }
            else
            {
                _footShadowGo.transform.localPosition = new Vector3(0f, -0.34f, 0.02f);
                _footShadowGo.transform.localScale = new Vector3(1.1f, 0.42f, 1f);
            }
        }

        private static Sprite GetOrCreateShadowSprite()
        {
            if (_cachedShadowSprite != null) return _cachedShadowSprite;
            const int w = 32, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - 15.5f) / 14f;
                    float ny = (y - 7.5f) / 6.5f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = r >= 1f ? 0f : (1f - r * r);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _cachedShadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return _cachedShadowSprite;
        }

        public void SetFacingRight(bool facingRight)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.flipX = !facingRight;
        }

        public void SetHomePosition(Vector3 homePos)
        {
            _originalPosition = homePos;
            transform.position = homePos;
        }

        public void SetBaseColor(Color tint)
        {
            _baseColor = tint;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && !_isDead)
            {
                RefreshIdleTint();
            }
        }

        public void SetSortingOrder(int order)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
        }

        public void StartMarchRun(float speedMultiplier = 1.2f)
        {
            if (_isDead) return;
            _isActionLocked = true;
            _currentState = PixelAnimState.Run;
            if (spriteRenderer != null) spriteRenderer.color = _baseColor;
            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(ContinuousMarchRoutine(speedMultiplier));
        }

        public void StopMarchRun()
        {
            if (_isDead) return;
            _isActionLocked = false;
            transform.position = _originalPosition;
            transform.localScale = _baseScale;
            PlayIdle();
        }

        private IEnumerator ContinuousMarchRoutine(float speedMultiplier)
        {
            List<Sprite> walkList = (runFrames != null && runFrames.Count > 0) ? runFrames : idleFrames;
            if (spriteRenderer == null || walkList == null || walkList.Count == 0) yield break;

            int idx = 0;
            float delay = 1f / Mathf.Max(1f, frameRate * Mathf.Max(0.5f, speedMultiplier));
            float t = 0f;
            while (!_isDead && _currentState == PixelAnimState.Run)
            {
                ApplyFrameSprite(walkList[idx], idx);
                idx = (idx + 1) % walkList.Count;
                t += delay;
                float bob = Mathf.Abs(Mathf.Sin(t * 14f)) * 0.045f;
                transform.position = _originalPosition + new Vector3(0f, bob, 0f);
                yield return new WaitForSeconds(delay);
            }
        }

        public void SetTargetHighlight(bool highlighted)
        {
            _isTargetHighlighted = highlighted;
            if (_isDead || _isActionLocked || spriteRenderer == null) return;
            RefreshIdleTint();
        }

        private void RefreshIdleTint()
        {
            if (spriteRenderer == null || _isDead) return;
            if (_isTargetHighlighted)
            {
                spriteRenderer.color = Color.Lerp(_baseColor, new Color(1f, 0.92f, 0.55f, _baseColor.a), 0.32f);
            }
            else
            {
                spriteRenderer.color = _baseColor;
            }
        }

        public void PlayEntranceStep(Vector3 startPos, Vector3 destinationPos, Action onComplete = null)
        {
            _originalPosition = destinationPos;
            transform.position = startPos;
            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(EntranceRoutine(startPos, destinationPos, onComplete));
        }

        private IEnumerator EntranceRoutine(Vector3 startPos, Vector3 destPos, Action onComplete)
        {
            _isActionLocked = true;
            _currentState = PixelAnimState.Run;

            List<Sprite> walkList = (runFrames != null && runFrames.Count > 0) ? runFrames : idleFrames;
            float duration = 0.45f;
            float elapsed = 0f;
            float frameTimer = 0f;
            float frameDelay = 1f / Mathf.Max(1f, frameRate);
            int idx = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = MedievalEase.OutCubic(t);
                transform.position = Vector3.Lerp(startPos, destPos, eased);

                if (walkList != null && walkList.Count > 0 && spriteRenderer != null)
                {
                    frameTimer += Time.deltaTime;
                    if (frameTimer >= frameDelay)
                    {
                        frameTimer -= frameDelay;
                        ApplyFrameSprite(walkList[idx], idx);
                        idx = (idx + 1) % walkList.Count;
                    }
                }
                yield return null;
            }

            transform.position = destPos;
            PixelCombatVFXManager.Instance?.SpawnDustPuff(destPos);

            // Landing squash & recovery
            float sqElapsed = 0f;
            const float sqDur = 0.11f;
            while (sqElapsed < sqDur)
            {
                sqElapsed += Time.deltaTime;
                float k = Mathf.Clamp01(sqElapsed / sqDur);
                float sx = Mathf.Lerp(1.12f, 1.0f, MedievalEase.OutBack(k));
                float sy = Mathf.Lerp(0.86f, 1.0f, MedievalEase.OutBack(k));
                transform.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
                yield return null;
            }
            transform.localScale = _baseScale;
            _isActionLocked = false;
            PlayIdle();
            onComplete?.Invoke();
        }

        public void PlayIdle()
        {
            if (_isDead || _isActionLocked) return;
            _currentState = PixelAnimState.Idle;
            RefreshIdleTint();

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(LoopAnimationRoutine(idleFrames));
        }

        public void PlayAttack(Vector3 targetWorldPos, Action onImpact = null, Action onComplete = null, float customStepDistance = -1f)
        {
            if (_isDead)
            {
                onImpact?.Invoke();
                onComplete?.Invoke();
                return;
            }

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(AttackSequenceRoutine(targetWorldPos, onImpact, onComplete, customStepDistance));
        }

        public void PlayHit()
        {
            if (_isDead) return;

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(HitSequenceRoutine());
        }

        public void PlayDeath()
        {
            if (_isDead) return;
            _isDead = true;
            _isActionLocked = false;
            _currentState = PixelAnimState.Death;
            if (_footShadowGo != null) _footShadowGo.SetActive(false);

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(DeathSequenceRoutine());
        }

        public void ReviveToIdle()
        {
            _isDead = false;
            _isActionLocked = false;
            transform.position = _originalPosition;
            transform.localScale = _baseScale;
            transform.rotation = _baseRotation;
            if (_footShadowGo != null) _footShadowGo.SetActive(true);
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = _baseColor;
            }
            PlayIdle();
        }

        public void PlayVictoryJump()
        {
            if (_isDead || _isActionLocked) return;
            StartCoroutine(VictoryJumpRoutine());
        }

        private IEnumerator VictoryJumpRoutine()
        {
            _isActionLocked = true;
            float elapsed = 0f;
            const float dur = 0.42f;
            while (elapsed < dur && !_isDead)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float yOffset = Mathf.Sin(t * Mathf.PI) * 0.32f;
                float stretch = t < 0.5f ? Mathf.Lerp(1.0f, 1.09f, t * 2f) : Mathf.Lerp(1.09f, 0.94f, (t - 0.5f) * 2f);
                transform.position = _originalPosition + new Vector3(0f, yOffset, 0f);
                transform.localScale = new Vector3(_baseScale.x * (2f - stretch), _baseScale.y * stretch, _baseScale.z);
                yield return null;
            }
            transform.position = _originalPosition;
            transform.localScale = _baseScale;
            _isActionLocked = false;
            if (!_isDead) PlayIdle();
        }

        private IEnumerator LoopAnimationRoutine(List<Sprite> frames)
        {
            if (frames == null || frames.Count == 0 || spriteRenderer == null) yield break;

            int index = 0;
            float delay = 1f / Mathf.Max(1f, frameRate);

            while (!_isDead)
            {
                ApplyFrameSprite(frames[index], index);
                index = (index + 1) % frames.Count;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator AttackSequenceRoutine(Vector3 targetWorldPos, Action onImpact, Action onComplete, float customStepDistance = -1f)
        {
            _isActionLocked = true;
            _currentState = PixelAnimState.Attack;

            if (spriteRenderer != null) spriteRenderer.color = _baseColor;

            Vector3 dir = (targetWorldPos - _originalPosition);
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
            else dir.Normalize();

            float stepDist = customStepDistance >= 0f ? customStepDistance : Mathf.Abs(attackStepDistance);
            Vector3 stepTarget = _originalPosition + dir * stepDist;

            // 0. Nhún nhẹ lấy đà (Squash anticipation)
            float anticElapsed = 0f;
            const float anticTime = 0.045f;
            while (anticElapsed < anticTime)
            {
                anticElapsed += Time.deltaTime;
                float k = Mathf.Clamp01(anticElapsed / anticTime);
                transform.localScale = new Vector3(
                    _baseScale.x * Mathf.Lerp(1f, 1.09f, k),
                    _baseScale.y * Mathf.Lerp(1f, 0.89f, k),
                    _baseScale.z);
                yield return null;
            }

            // 1. Lao nhanh về phía mục tiêu (OutCubic + slight stretch)
            float lungeTime = 0.13f;
            float elapsed = 0f;
            while (elapsed < lungeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lungeTime);
                float eased = MedievalEase.OutCubic(t);
                transform.position = Vector3.Lerp(_originalPosition, stepTarget, eased);
                transform.localScale = new Vector3(
                    _baseScale.x * Mathf.Lerp(1.09f, 1.04f, eased),
                    _baseScale.y * Mathf.Lerp(0.89f, 0.96f, eased),
                    _baseScale.z);
                yield return null;
            }
            transform.position = stepTarget;
            transform.localScale = _baseScale;

            // 2. Phát chuỗi frame tấn công và kích hoạt impact ở giữa đòn
            bool impactFired = false;
            if (attackFrames != null && attackFrames.Count > 0 && spriteRenderer != null)
            {
                float frameDelay = 1f / Mathf.Max(1f, frameRate * 1.25f);
                int impactFrameIndex = Mathf.Clamp(attackFrames.Count / 2, 0, attackFrames.Count - 1);

                for (int i = 0; i < attackFrames.Count; i++)
                {
                    ApplyFrameSprite(attackFrames[i], i);
                    if (i == impactFrameIndex && !impactFired)
                    {
                        impactFired = true;
                        onImpact?.Invoke();
                    }
                    yield return new WaitForSeconds(frameDelay);
                }
            }
            else
            {
                impactFired = true;
                onImpact?.Invoke();
                yield return new WaitForSeconds(0.22f);
            }

            if (!impactFired)
            {
                onImpact?.Invoke();
            }

            // 3. Lùi về vị trí đứng mượt mà (OutCubic)
            float returnTime = 0.16f;
            elapsed = 0f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnTime);
                transform.position = Vector3.Lerp(stepTarget, _originalPosition, MedievalEase.OutCubic(t));
                yield return null;
            }
            transform.position = _originalPosition;
            transform.localScale = _baseScale;

            _isActionLocked = false;
            PlayIdle();
            onComplete?.Invoke();
        }

        private IEnumerator HitSequenceRoutine()
        {
            _isActionLocked = true;
            _currentState = PixelAnimState.Hit;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = hitFlashColor;
            }

            // Rung nhẹ + squash khi nhận sát thương
            Vector3 knockOffset = new Vector3(transform.position.x >= 0f ? 0.08f : -0.08f, 0f, 0f);
            transform.position = _originalPosition + knockOffset;
            transform.localScale = new Vector3(_baseScale.x * 1.13f, _baseScale.y * 0.87f, _baseScale.z);

            if (hitFrames != null && hitFrames.Count > 0 && spriteRenderer != null)
            {
                float frameDelay = 1f / Mathf.Max(1f, frameRate);
                for (int i = 0; i < hitFrames.Count; i++)
                {
                    ApplyFrameSprite(hitFrames[i], i);
                    float k = (i + 1f) / hitFrames.Count;
                    transform.localScale = Vector3.Lerp(
                        new Vector3(_baseScale.x * 1.13f, _baseScale.y * 0.87f, _baseScale.z),
                        _baseScale,
                        MedievalEase.OutBack(k));
                    yield return new WaitForSeconds(frameDelay);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.18f);
            }

            transform.position = _originalPosition;
            transform.localScale = _baseScale;
            _isActionLocked = false;

            if (!_isDead)
            {
                PlayIdle();
            }
        }

        private IEnumerator DeathSequenceRoutine()
        {
            if (spriteRenderer == null) yield break;

            if (deathFrames != null && deathFrames.Count > 0)
            {
                spriteRenderer.color = _baseColor;
                float delay = 1f / Mathf.Max(1f, frameRate);
                for (int i = 0; i < deathFrames.Count; i++)
                {
                    ApplyFrameSprite(deathFrames[i], i);
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                // Fallback cho quái không có frame Death (Goblin / Skeleton): chớp hit -> nghiêng ngã & mờ dần
                if (hitFrames != null && hitFrames.Count > 0)
                {
                    ApplyFrameSprite(hitFrames[hitFrames.Count - 1], hitFrames.Count - 1);
                }

                float elapsed = 0f;
                float duration = 0.45f;
                Vector3 startPos = _originalPosition;
                Quaternion startRot = _baseRotation;
                Quaternion endRot = Quaternion.Euler(0f, 0f, spriteRenderer.flipX ? -75f : 75f);

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    transform.position = Vector3.Lerp(startPos, startPos + new Vector3(0f, -0.22f, 0f), t);
                    transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                    Color c = Color.Lerp(_baseColor, new Color(0.4f, 0.4f, 0.4f, 0f), t);
                    c.a = Mathf.Lerp(_baseColor.a, 0f, t);
                    spriteRenderer.color = c;
                    yield return null;
                }

                spriteRenderer.enabled = false;
            }
        }
    }
}
