using System.Collections;
using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.Visuals
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animation Settings")]
        [SerializeField] private float idleFrameDuration = 0.5f;
        [SerializeField] private float stepDistance = 0.6f;
        [SerializeField] private float stepDuration = 0.15f;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

        private Sprite _idleSprite;
        private Sprite _idleSpriteAlt;
        private Sprite _actionSprite;
        private Sprite _hitSprite;

        private Vector3 _originalPosition;
        private Coroutine _idleCoroutine;
        private Coroutine _actionCoroutine;
        private bool _isActing;
        private bool _isDead;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            _originalPosition = transform.position;
        }

        public void SetupSprites(CharacterClassData classData)
        {
            if (classData == null) return;
            _idleSprite = classData.idleSprite;
            _idleSpriteAlt = classData.idleSpriteAlt != null ? classData.idleSpriteAlt : classData.idleSprite;
            _actionSprite = classData.actionSprite != null ? classData.actionSprite : classData.idleSprite;
            _hitSprite = classData.hitSprite != null ? classData.hitSprite : classData.idleSprite;

            StartIdleAnimation();
        }

        public void StartIdleAnimation()
        {
            if (_isDead) return;
            if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
            _idleCoroutine = StartCoroutine(IdleRoutine());
        }

        private IEnumerator IdleRoutine()
        {
            bool toggle = false;
            while (!_isDead)
            {
                if (!_isActing && spriteRenderer != null)
                {
                    spriteRenderer.sprite = toggle ? _idleSpriteAlt : _idleSprite;
                    toggle = !toggle;
                }
                yield return new WaitForSeconds(idleFrameDuration);
            }
        }

        public void PlayAttackAnimation(Vector3 targetPosition, System.Action onImpactCallback = null)
        {
            if (_isDead) return;
            if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
            _actionCoroutine = StartCoroutine(AttackRoutine(targetPosition, onImpactCallback));
        }

        private IEnumerator AttackRoutine(Vector3 targetPosition, System.Action onImpactCallback)
        {
            _isActing = true;
            Vector3 direction = (targetPosition - _originalPosition).normalized;
            Vector3 lungeTarget = _originalPosition + direction * stepDistance;

            // Step forward
            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(_originalPosition, lungeTarget, elapsed / stepDuration);
                yield return null;
            }

            // Switch to action sprite on impact
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = _actionSprite;
            }
            onImpactCallback?.Invoke();

            yield return new WaitForSeconds(0.25f);

            // Step back to original position
            elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(lungeTarget, _originalPosition, elapsed / stepDuration);
                yield return null;
            }
            transform.position = _originalPosition;

            _isActing = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = _idleSprite;
            }
        }

        public void PlayHitAnimation()
        {
            if (_isDead) return;
            StartCoroutine(HitRoutine());
        }

        private IEnumerator HitRoutine()
        {
            if (spriteRenderer == null) yield break;

            Color originalColor = spriteRenderer.color;
            Sprite prevSprite = spriteRenderer.sprite;

            spriteRenderer.sprite = _hitSprite;
            spriteRenderer.color = hitFlashColor;

            // Small shake
            Vector3 offsetPos = _originalPosition + new Vector3(-0.1f, 0f, 0f);
            transform.position = offsetPos;

            yield return new WaitForSeconds(0.12f);

            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);

            transform.position = _originalPosition;
            spriteRenderer.color = originalColor;

            if (!_isActing)
            {
                spriteRenderer.sprite = _idleSprite;
            }
        }

        public void PlayDeathAnimation()
        {
            _isDead = true;
            if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.sprite = _hitSprite;
            Color c = spriteRenderer.color;
            float elapsed = 0f;
            float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                spriteRenderer.color = new Color(c.r * (1f - t), c.g * (1f - t), c.b * (1f - t), 1f - t * 0.7f);
                yield return null;
            }
        }
    }
}
