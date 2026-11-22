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
        private Color _baseColor = Color.white;
        private Quaternion _baseRotation = Quaternion.identity;
        private bool _isActionLocked;
        private bool _isDead;
        private bool _isTargetHighlighted;

        public PixelAnimState CurrentState => _currentState;
        public Vector3 HomePosition => _originalPosition;

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
            _baseRotation = transform.rotation;
        }

        private void Start()
        {
            if (!_isActionLocked && !_isDead)
            {
                PlayIdle();
            }
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
                spriteRenderer.sprite = walkList[idx];
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
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                transform.position = Vector3.Lerp(startPos, destPos, eased);

                if (walkList != null && walkList.Count > 0 && spriteRenderer != null)
                {
                    frameTimer += Time.deltaTime;
                    if (frameTimer >= frameDelay)
                    {
                        frameTimer -= frameDelay;
                        spriteRenderer.sprite = walkList[idx];
                        idx = (idx + 1) % walkList.Count;
                    }
                }
                yield return null;
            }

            transform.position = destPos;
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

        public void PlayAttack(Vector3 targetWorldPos, Action onImpact = null, Action onComplete = null)
        {
            if (_isDead)
            {
                onImpact?.Invoke();
                onComplete?.Invoke();
                return;
            }

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(AttackSequenceRoutine(targetWorldPos, onImpact, onComplete));
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

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(DeathSequenceRoutine());
        }

        public void ReviveToIdle()
        {
            _isDead = false;
            _isActionLocked = false;
            transform.position = _originalPosition;
            transform.rotation = _baseRotation;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = _baseColor;
            }
            PlayIdle();
        }

        private IEnumerator LoopAnimationRoutine(List<Sprite> frames)
        {
            if (frames == null || frames.Count == 0 || spriteRenderer == null) yield break;

            int index = 0;
            float delay = 1f / Mathf.Max(1f, frameRate);

            while (!_isDead)
            {
                spriteRenderer.sprite = frames[index];
                index = (index + 1) % frames.Count;
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator AttackSequenceRoutine(Vector3 targetWorldPos, Action onImpact, Action onComplete)
        {
            _isActionLocked = true;
            _currentState = PixelAnimState.Attack;

            if (spriteRenderer != null) spriteRenderer.color = _baseColor;

            Vector3 dir = (targetWorldPos - _originalPosition);
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
            else dir.Normalize();

            float stepDist = Mathf.Abs(attackStepDistance);
            Vector3 stepTarget = _originalPosition + dir * stepDist;

            // 1. Lao nhanh về phía mục tiêu
            float lungeTime = 0.14f;
            float elapsed = 0f;
            while (elapsed < lungeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lungeTime);
                transform.position = Vector3.Lerp(_originalPosition, stepTarget, t);
                yield return null;
            }
            transform.position = stepTarget;

            // 2. Phát chuỗi frame tấn công và kích hoạt impact ở giữa đòn
            bool impactFired = false;
            if (attackFrames != null && attackFrames.Count > 0 && spriteRenderer != null)
            {
                float frameDelay = 1f / Mathf.Max(1f, frameRate * 1.25f);
                int impactFrameIndex = Mathf.Clamp(attackFrames.Count / 2, 0, attackFrames.Count - 1);

                for (int i = 0; i < attackFrames.Count; i++)
                {
                    spriteRenderer.sprite = attackFrames[i];
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

            // 3. Lùi về vị trí đứng
            float returnTime = 0.16f;
            elapsed = 0f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnTime);
                transform.position = Vector3.Lerp(stepTarget, _originalPosition, t);
                yield return null;
            }
            transform.position = _originalPosition;

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

            // Rung nhẹ khi nhận sát thương
            Vector3 knockOffset = new Vector3(transform.position.x >= 0f ? 0.08f : -0.08f, 0f, 0f);
            transform.position = _originalPosition + knockOffset;

            if (hitFrames != null && hitFrames.Count > 0 && spriteRenderer != null)
            {
                float frameDelay = 1f / Mathf.Max(1f, frameRate);
                for (int i = 0; i < hitFrames.Count; i++)
                {
                    spriteRenderer.sprite = hitFrames[i];
                    yield return new WaitForSeconds(frameDelay);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.18f);
            }

            transform.position = _originalPosition;
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
                    spriteRenderer.sprite = deathFrames[i];
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                // Fallback cho quái không có frame Death (Goblin / Skeleton): chớp hit -> nghiêng ngã & mờ dần
                if (hitFrames != null && hitFrames.Count > 0)
                {
                    spriteRenderer.sprite = hitFrames[hitFrames.Count - 1];
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
