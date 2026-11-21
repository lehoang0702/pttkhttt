using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Visuals
{
    /// <summary>
    /// Các trạng thái hoạt ảnh pixel cơ bản của nhân vật
    /// </summary>
    public enum PixelAnimState
    {
        Idle,      // Đứng thở (chờ lượt)
        Run,       // Chạy / Di chuyển
        Attack,    // Vung kiếm / Bắn cung / Niệm phép
        Hit,       // Bị trúng đòn (chớp đỏ)
        Death      // Gục ngã khi hết máu
    }

    /// <summary>
    /// [Bộ Hoạt Ảnh Pixel Độc Lập - PixelCharacterAnimator]
    /// Lấy cảm hứng từ triết lý frame-based của thư viện đồ họa pixel (gopxl/pixel).
    /// Giúp phát hoạt ảnh từng khung hình cực kỳ mượt mà, chính xác, không cần qua Animator Controller phức tạp của Unity.
    /// Rất phù hợp cho người mới bắt đầu làm game 2D Pixel Art!
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelCharacterAnimator : MonoBehaviour
    {
        [Header("--- Thành phần hiển thị ---")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("--- Danh sách các khung hình (Sprites) ---")]
        [Tooltip("Các khung hình trạng thái đứng yên (Idle)")]
        public List<Sprite> idleFrames = new List<Sprite>();

        [Tooltip("Các khung hình trạng thái chạy (Run)")]
        public List<Sprite> runFrames = new List<Sprite>();

        [Tooltip("Các khung hình xuất chiêu tấn công (Attack)")]
        public List<Sprite> attackFrames = new List<Sprite>();

        [Tooltip("Các khung hình khi bị trúng đòn (Hit)")]
        public List<Sprite> hitFrames = new List<Sprite>();

        [Tooltip("Các khung hình khi tử trận (Death)")]
        public List<Sprite> deathFrames = new List<Sprite>();

        [Header("--- Tốc độ và Hiệu ứng chuyển động ---")]
        [Tooltip("Số khung hình trên giây (FPS)")]
        [SerializeField] private float frameRate = 10f;

        [Tooltip("Khoảng cách nhân vật bước lên phía trước khi tấn công")]
        [SerializeField] private float attackStepDistance = 0.8f;

        [Tooltip("Màu chớp nháy khi bị trúng đòn (Đỏ nhạt)")]
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

        private PixelAnimState _currentState = PixelAnimState.Idle;
        private Coroutine _currentAnimCoroutine;
        private Vector3 _originalPosition;
        private bool _isActionLocked = false;
        private bool _isDead = false;

        public PixelAnimState CurrentState => _currentState;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            _originalPosition = transform.position;
        }

        private void Start()
        {
            PlayIdle();
        }

        /// <summary>
        /// Phát hoạt ảnh đứng thở (Idle) lặp vô tận
        /// </summary>
        public void PlayIdle()
        {
            if (_isDead || _isActionLocked) return;
            _currentState = PixelAnimState.Idle;

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(LoopAnimationRoutine(idleFrames));
        }

        /// <summary>
        /// Kích hoạt chuỗi động tác tấn công: bước lên -> vung vũ khí -> lùi về vị trí cũ -> quay về Idle
        /// </summary>
        /// <param name="targetWorldPos">Vị trí của mục tiêu để xác định hướng bước tới</param>
        public void PlayAttack(Vector3 targetWorldPos)
        {
            if (_isDead) return;

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(AttackSequenceRoutine(targetWorldPos));
        }

        /// <summary>
        /// Kích hoạt hoạt ảnh bị trúng đòn: chớp màu đỏ và rung nhẹ
        /// </summary>
        public void PlayHit()
        {
            if (_isDead) return;

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(HitSequenceRoutine());
        }

        /// <summary>
        /// Kích hoạt hoạt ảnh tử trận: phát chuỗi ngã xuống và giữ nguyên ở khung hình cuối
        /// </summary>
        public void PlayDeath()
        {
            _isDead = true;
            _currentState = PixelAnimState.Death;

            if (_currentAnimCoroutine != null) StopCoroutine(_currentAnimCoroutine);
            _currentAnimCoroutine = StartCoroutine(PlayOnceRoutine(deathFrames, true));
        }

        // =====================================================================
        // CÁC COROUTINE ĐIỀU KHIỂN CHUYỂN ĐỘNG & FRAME TỪNG BƯỚC
        // =====================================================================

        /// <summary>
        /// Phát lặp tuần hoàn một danh sách khung hình
        /// </summary>
        private IEnumerator LoopAnimationRoutine(List<Sprite> frames)
        {
            if (frames == null || frames.Count == 0) yield break;

            int index = 0;
            float delay = 1f / Mathf.Max(1f, frameRate);

            while (true)
            {
                spriteRenderer.sprite = frames[index];
                index = (index + 1) % frames.Count;
                yield return new WaitForSeconds(delay);
            }
        }

        /// <summary>
        /// Phát chuỗi khung hình một lần duy nhất
        /// </summary>
        private IEnumerator PlayOnceRoutine(List<Sprite> frames, bool holdLastFrame)
        {
            if (frames == null || frames.Count == 0) yield break;

            float delay = 1f / Mathf.Max(1f, frameRate);

            for (int i = 0; i < frames.Count; i++)
            {
                spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(delay);
            }

            if (!holdLastFrame && !_isDead)
            {
                PlayIdle();
            }
        }

        /// <summary>
        /// Chuỗi hành động tấn công hoàn chỉnh
        /// </summary>
        private IEnumerator AttackSequenceRoutine(Vector3 targetWorldPos)
        {
            _isActionLocked = true;
            _currentState = PixelAnimState.Attack;

            Vector3 direction = (targetWorldPos - _originalPosition).normalized;
            Vector3 stepTarget = _originalPosition + direction * attackStepDistance;

            // 1. Lao nhanh về phía mục tiêu
            float lungeTime = 0.12f;
            float elapsed = 0f;
            while (elapsed < lungeTime)
            {
                transform.position = Vector3.Lerp(_originalPosition, stepTarget, elapsed / lungeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = stepTarget;

            // 2. Phát lần lượt các frame tấn công
            if (attackFrames != null && attackFrames.Count > 0)
            {
                float frameDelay = 1f / Mathf.Max(1f, frameRate * 1.3f);
                for (int i = 0; i < attackFrames.Count; i++)
                {
                    spriteRenderer.sprite = attackFrames[i];
                    yield return new WaitForSeconds(frameDelay);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }

            // 3. Lùi về vị trí ban đầu
            float returnTime = 0.15f;
            elapsed = 0f;
            while (elapsed < returnTime)
            {
                transform.position = Vector3.Lerp(stepTarget, _originalPosition, elapsed / returnTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = _originalPosition;

            _isActionLocked = false;
            PlayIdle();
        }

        /// <summary>
        /// Chuỗi hiệu ứng khi bị đánh trúng
        /// </summary>
        private IEnumerator HitSequenceRoutine()
        {
            _isActionLocked = true;
            _currentState = PixelAnimState.Hit;

            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = hitFlashColor; // Chớp màu đỏ

            if (hitFrames != null && hitFrames.Count > 0)
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
                yield return new WaitForSeconds(0.2f);
            }

            spriteRenderer.color = originalColor; // Khôi phục màu sắc ban đầu
            _isActionLocked = false;

            if (!_isDead)
            {
                PlayIdle();
            }
        }
    }
}
