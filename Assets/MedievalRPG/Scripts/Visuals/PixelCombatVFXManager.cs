using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using MedievalRPG.Combat;
using MedievalRPG.Data;
using MedievalRPG.UI;

namespace MedievalRPG.Visuals
{
    /// <summary>
    /// Quản lý hiệu ứng chiến đấu Pixel-Art dùng ObjectPool<SpriteRenderer> (zero-GC reuse):
    /// Vệt chém, Đạn phép/Tên bay, Nổ AoE, Cột sáng hồi máu, Phá Khiên Bóng Tối, Mưa Tên,
    /// Đỡ Đòn Hoàn Hảo, Tuyệt Kỹ Hợp Kích, Bụi hành quân, Confetti ăn mừng, Hitstop & Rung Camera.
    /// </summary>
    public class PixelCombatVFXManager : MonoBehaviour
    {
        public static PixelCombatVFXManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            _slashSprite = null;
            _orbSprite = null;
            _arrowSprite = null;
            _ringSprite = null;
            _shardSprite = null;
            _dustSprite = null;
            _confettiSprite = null;
        }

        private static Sprite _slashSprite;
        private static Sprite _orbSprite;
        private static Sprite _arrowSprite;
        private static Sprite _ringSprite;
        private static Sprite _shardSprite;
        private static Sprite _dustSprite;
        private static Sprite _confettiSprite;

        private ObjectPool<SpriteRenderer> _spritePool;
        private Transform _poolRoot;
        private Coroutine _shakeCoroutine;
        private Coroutine _hitstopCoroutine;
        private Vector3 _cameraRestPos;
        private bool _hasCameraRestPos;

        public int TotalPoolCreated { get; private set; }
        public int ActivePooledCount => _spritePool != null ? _spritePool.CountActive : 0;
        public int InactivePooledCount => _spritePool != null ? _spritePool.CountInactive : 0;

        public static PixelCombatVFXManager EnsureExists()
        {
            if (Instance != null) return Instance;
            PixelCombatVFXManager existing = FindAnyObjectByType<PixelCombatVFXManager>();
            if (existing != null)
            {
                Instance = existing;
                existing.EnsurePoolInitialized();
                return existing;
            }
            GameObject go = new GameObject("PixelCombatVFXManager");
            return go.AddComponent<PixelCombatVFXManager>();
        }

        private static void SafeDestroyGameObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                SafeDestroyGameObject(gameObject);
                return;
            }
            Instance = this;
            EnsurePoolInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                TimeScaleController.SetHitstop(false);
                Instance = null;
            }
            _spritePool?.Clear();
            _spritePool = null;
        }

        public void EnsurePoolInitialized()
        {
            if (_spritePool != null) return;

            if (_poolRoot == null)
            {
                GameObject root = new GameObject("PooledVFXRoot");
                root.transform.SetParent(transform, false);
                _poolRoot = root.transform;
            }

            _spritePool = new ObjectPool<SpriteRenderer>(
                createFunc: CreatePooledSprite,
                actionOnGet: sr =>
                {
                    if (sr == null) return;
                    sr.gameObject.SetActive(true);
                    sr.transform.localRotation = Quaternion.identity;
                    sr.transform.localScale = Vector3.one;
                    sr.flipX = false;
                    sr.flipY = false;
                    sr.color = Color.white;
                    sr.sortingOrder = 16;
                },
                actionOnRelease: sr =>
                {
                    if (sr == null) return;
                    sr.sprite = null;
                    sr.gameObject.SetActive(false);
                    if (_poolRoot != null) sr.transform.SetParent(_poolRoot, false);
                },
                actionOnDestroy: sr =>
                {
                    if (sr != null) SafeDestroyGameObject(sr.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 24,
                maxSize: 128);

            PrewarmPool(8);
        }

        private void PrewarmPool(int count)
        {
            if (_spritePool == null || count <= 0) return;
            List<SpriteRenderer> temp = new List<SpriteRenderer>(count);
            for (int i = 0; i < count; i++)
            {
                temp.Add(_spritePool.Get());
            }
            for (int i = 0; i < temp.Count; i++)
            {
                _spritePool.Release(temp[i]);
            }
        }

        private SpriteRenderer CreatePooledSprite()
        {
            TotalPoolCreated++;
            GameObject go = new GameObject($"VFX_Pooled_{TotalPoolCreated}");
            if (_poolRoot != null) go.transform.SetParent(_poolRoot, false);
            go.SetActive(false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 16;
            return sr;
        }

        private SpriteRenderer RentSprite(Sprite sprite, Vector3 worldPos, Color color, int sortingOrder = 16)
        {
            EnsurePoolInitialized();
            SpriteRenderer sr = _spritePool.Get();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.transform.position = worldPos;
            return sr;
        }

        private void ReturnSprite(SpriteRenderer sr)
        {
            if (sr == null) return;
            if (_spritePool != null) _spritePool.Release(sr);
            else SafeDestroyGameObject(sr.gameObject);
        }

        public void PlaySlashArc(Vector3 worldPos, Color color, bool flipX = false, float scale = 1.15f)
        {
            StartCoroutine(SlashArcRoutine(worldPos + new Vector3(0f, 0.3f, -0.08f), color, flipX, scale));
        }

        public void PlayExplosionBurst(Vector3 worldPos, Color color, float radiusScale = 1f)
        {
            StartCoroutine(ExplosionBurstRoutine(worldPos + new Vector3(0f, 0.25f, -0.08f), color, radiusScale));
        }

        public void PlayHealColumn(Vector3 worldPos)
        {
            StartCoroutine(HealColumnRoutine(worldPos));
        }

        public void PlayShieldShatter(Vector3 worldPos)
        {
            StartCoroutine(ShieldShatterRoutine(worldPos + new Vector3(0f, 0.35f, -0.08f)));
        }

        public void SpawnDustPuff(Vector3 worldPos)
        {
            StartCoroutine(DustPuffRoutine(worldPos + new Vector3(0f, -0.28f, -0.05f)));
        }

        public void PlayRainOfArrows(Vector3 centerWorldPos, Action onImpact = null)
        {
            StartCoroutine(RainOfArrowsRoutine(centerWorldPos, onImpact));
        }

        public void PlayPerfectGuardBurst(Vector3 worldPos, bool isQuickParry)
        {
            Color col = isQuickParry ? MedievalUITheme.GoldSoft : MedievalUITheme.MpSapphire;
            StartCoroutine(PerfectGuardRoutine(worldPos + new Vector3(0f, 0.28f, -0.08f), col, isQuickParry));
        }

        public void PlaySynergyUltimateVFX(Vector3 heroesCenter, IReadOnlyList<Vector3> enemyPositions)
        {
            StartCoroutine(PlaySynergyUltimateRoutine(heroesCenter, enemyPositions));
        }

        public IEnumerator PlaySynergyUltimateRoutine(Vector3 heroesCenter, IReadOnlyList<Vector3> enemyPositions)
        {
            PlayExplosionBurst(heroesCenter + new Vector3(0f, 0.25f, 0f), MedievalUITheme.GoldSoft, 1.55f);
            TriggerHitstop(0.065f);
            TriggerCameraShake(0.20f, 0.28f);

            if (enemyPositions != null)
            {
                for (int i = 0; i < enemyPositions.Count; i++)
                {
                    Vector3 ep = enemyPositions[i];
                    PlaySlashArc(ep, MedievalUITheme.GoldSoft, false, 1.65f);
                    PlayExplosionBurst(ep, new Color(0.35f, 0.85f, 1f, 1f), 1.45f);
                    PlayRainOfArrows(ep, null);
                    yield return new WaitForSeconds(0.06f);
                }
            }
        }

        public void PlayConfettiWorldBurst(Vector3 center, int count = 34)
        {
            StartCoroutine(ConfettiWorldBurstRoutine(center, count));
        }

        public IEnumerator PlayProjectileRoutine(Vector3 from, Vector3 to, Color color, bool isArrow, float duration, Action onHit)
        {
            Vector3 start = from + new Vector3(0f, 0.30f, -0.08f);
            Vector3 end = to + new Vector3(0f, 0.30f, -0.08f);
            SpriteRenderer sr = RentSprite(isArrow ? GetArrowSprite() : GetOrbSprite(), start, color, 16);

            Vector3 dir = (end - start);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (isArrow) sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            float arcHeight = isArrow ? 0.42f : 0.14f;
            float elapsed = 0f;
            float dur = Mathf.Max(0.08f, duration);

            try
            {
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    Vector3 pos = Vector3.Lerp(start, end, t);
                    pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                    sr.transform.position = pos;
                    if (!isArrow)
                    {
                        sr.transform.Rotate(0f, 0f, 480f * Time.deltaTime);
                        sr.transform.localScale = Vector3.one * (0.95f + 0.25f * Mathf.Sin(t * Mathf.PI));
                    }
                    yield return null;
                }
            }
            finally
            {
                ReturnSprite(sr);
            }

            onHit?.Invoke();
            if (!isArrow) PlayExplosionBurst(to, color, 0.9f);
        }

        public void TriggerCameraShake(float intensity = 0.14f, float duration = 0.20f)
        {
            if (!PlayerCampProgressManager.EnsureLoaded().CameraShakeEnabled) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                if (_hasCameraRestPos) cam.transform.position = _cameraRestPos;
            }
            _shakeCoroutine = StartCoroutine(CameraShakeRoutine(cam, intensity, duration));
        }

        public void TriggerHitstop(float unscaledDuration = 0.045f)
        {
            if (TimeScaleController.IsPaused || TimeScaleController.EffectiveTimeScale <= 0.05f || _hitstopCoroutine != null) return;
            _hitstopCoroutine = StartCoroutine(HitstopRoutine(unscaledDuration));
        }

        private IEnumerator HitstopRoutine(float dur)
        {
            TimeScaleController.SetHitstop(true, 0.08f);
            float elapsed = 0f;
            while (elapsed < dur && !TimeScaleController.IsPaused)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            TimeScaleController.SetHitstop(false);
            _hitstopCoroutine = null;
        }

        private IEnumerator CameraShakeRoutine(Camera cam, float intensity, float duration)
        {
            _cameraRestPos = cam.transform.position;
            _hasCameraRestPos = true;
            float elapsed = 0f;
            while (elapsed < duration && cam != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / duration);
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * intensity * fade;
                cam.transform.position = _cameraRestPos + new Vector3(rnd.x, rnd.y, 0f);
                yield return null;
            }
            if (cam != null) cam.transform.position = _cameraRestPos;
            _shakeCoroutine = null;
        }

        private IEnumerator SlashArcRoutine(Vector3 pos, Color color, bool flipX, float scale)
        {
            SpriteRenderer sr = RentSprite(GetSlashSprite(), pos, color, 16);
            sr.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, flipX ? 18f : -18f);

            try
            {
                float elapsed = 0f;
                const float dur = 0.20f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(flipX ? 28f : -28f, flipX ? -24f : 24f, t));
                    Color c = color;
                    c.a = 1f - t * t;
                    sr.color = c;
                    yield return null;
                }
            }
            finally
            {
                ReturnSprite(sr);
            }
        }

        private IEnumerator ExplosionBurstRoutine(Vector3 pos, Color color, float radiusScale)
        {
            SpriteRenderer rSr = RentSprite(GetRingSprite(), pos, color, 16);

            const int sparkCount = 6;
            SpriteRenderer[] sparkSrs = new SpriteRenderer[sparkCount];
            Vector2[] dirs = new Vector2[sparkCount];
            Color sparkTint = Color.Lerp(color, Color.white, 0.35f);
            for (int i = 0; i < sparkCount; i++)
            {
                float ang = (i / (float)sparkCount) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                dirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (1.25f * radiusScale);
                SpriteRenderer sSr = RentSprite(GetOrbSprite(), pos, sparkTint, 17);
                sSr.transform.localScale = Vector3.one * 0.55f;
                sparkSrs[i] = sSr;
            }

            try
            {
                float elapsed = 0f;
                const float dur = 0.32f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    float eased = MedievalEase.OutCubic(t);
                    rSr.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.55f * radiusScale, eased);
                    Color rc = color;
                    rc.a = 1f - t;
                    rSr.color = rc;

                    for (int i = 0; i < sparkCount; i++)
                    {
                        if (sparkSrs[i] == null) continue;
                        sparkSrs[i].transform.position = pos + (Vector3)(dirs[i] * eased);
                        sparkSrs[i].transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 0.1f, t);
                        Color sc = sparkTint;
                        sc.a = 1f - t;
                        sparkSrs[i].color = sc;
                    }
                    yield return null;
                }
            }
            finally
            {
                ReturnSprite(rSr);
                for (int i = 0; i < sparkCount; i++)
                {
                    ReturnSprite(sparkSrs[i]);
                }
            }
        }

        private IEnumerator HealColumnRoutine(Vector3 worldPos)
        {
            SpriteRenderer sr = RentSprite(GetRingSprite(), worldPos + new Vector3(0f, -0.1f, -0.08f),
                new Color(0.45f, 1f, 0.68f, 0.9f), 16);

            const int motes = 5;
            SpriteRenderer[] mSr = new SpriteRenderer[motes];
            Vector3[] offsets = new Vector3[motes];
            Color moteBaseCol = new Color(0.95f, 0.95f, 0.45f, 1f);
            for (int i = 0; i < motes; i++)
            {
                offsets[i] = new Vector3((i - 2) * 0.22f, UnityEngine.Random.Range(-0.15f, 0.25f), -0.08f);
                SpriteRenderer s = RentSprite(GetOrbSprite(), worldPos + offsets[i], moteBaseCol, 17);
                s.transform.localScale = Vector3.one * 0.42f;
                mSr[i] = s;
            }

            try
            {
                float elapsed = 0f;
                const float dur = 0.55f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    sr.transform.position = worldPos + new Vector3(0f, Mathf.Lerp(-0.15f, 0.95f, t), -0.08f);
                    sr.transform.localScale = new Vector3(1.1f, 0.55f, 1f) * (1f + 0.35f * t);
                    Color c = sr.color;
                    c.a = Mathf.Sin(t * Mathf.PI);
                    sr.color = c;

                    for (int i = 0; i < motes; i++)
                    {
                        if (mSr[i] == null) continue;
                        mSr[i].transform.position = worldPos + offsets[i] + new Vector3(0f, t * 1.15f, 0f);
                        Color mc = moteBaseCol;
                        mc.a = 1f - t;
                        mSr[i].color = mc;
                    }
                    yield return null;
                }
            }
            finally
            {
                ReturnSprite(sr);
                for (int i = 0; i < motes; i++)
                {
                    ReturnSprite(mSr[i]);
                }
            }
        }

        private IEnumerator ShieldShatterRoutine(Vector3 center)
        {
            const int shards = 8;
            SpriteRenderer[] srs = new SpriteRenderer[shards];
            Vector2[] vels = new Vector2[shards];
            Color shardCol = new Color(0.75f, 0.45f, 1f, 1f);
            for (int i = 0; i < shards; i++)
            {
                float ang = (i / (float)shards) * Mathf.PI * 2f;
                vels[i] = new Vector2(Mathf.Cos(ang) * 1.9f, Mathf.Sin(ang) * 1.5f + 0.7f);
                SpriteRenderer s = RentSprite(GetShardSprite(), center, shardCol, 17);
                s.transform.localScale = Vector3.one * 0.75f;
                srs[i] = s;
            }

            try
            {
                float elapsed = 0f;
                const float dur = 0.45f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    for (int i = 0; i < shards; i++)
                    {
                        if (srs[i] == null) continue;
                        srs[i].transform.position = center + (Vector3)(vels[i] * t) + new Vector3(0f, -1.6f * t * t, 0f);
                        srs[i].transform.Rotate(0f, 0f, 360f * Time.deltaTime);
                        Color c = shardCol;
                        c.a = 1f - t;
                        srs[i].color = c;
                    }
                    yield return null;
                }
            }
            finally
            {
                for (int i = 0; i < shards; i++) ReturnSprite(srs[i]);
            }
        }

        private IEnumerator DustPuffRoutine(Vector3 pos)
        {
            Color baseDust = new Color(0.76f, 0.66f, 0.52f, 0.75f);
            SpriteRenderer sr = RentSprite(GetDustSprite(), pos, baseDust, 4);

            try
            {
                float elapsed = 0f;
                const float dur = 0.28f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    sr.transform.position = pos + new Vector3(-0.35f * t, 0.14f * t, 0f);
                    sr.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.15f, t);
                    Color c = baseDust;
                    c.a = (1f - t) * 0.75f;
                    sr.color = c;
                    yield return null;
                }
            }
            finally
            {
                ReturnSprite(sr);
            }
        }

        private IEnumerator PerfectGuardRoutine(Vector3 pos, Color col, bool isQuickParry)
        {
            SpriteRenderer ring = RentSprite(GetRingSprite(), pos, col, 17);
            SpriteRenderer core = RentSprite(GetOrbSprite(), pos, Color.white, 18);

            try
            {
                float elapsed = 0f;
                float dur = isQuickParry ? 0.34f : 0.26f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    float eased = MedievalEase.OutBack(t);
                    ring.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, isQuickParry ? 1.85f : 1.45f, eased);
                    Color rc = col;
                    rc.a = 1f - t;
                    ring.color = rc;

                    core.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.1f, t);
                    Color cc = Color.Lerp(Color.white, col, t);
                    cc.a = 1f - t;
                    core.color = cc;
                    yield return null;
                }
            }
            finally
            {
                ReturnSprite(ring);
                ReturnSprite(core);
            }
        }

        private IEnumerator ConfettiWorldBurstRoutine(Vector3 center, int count)
        {
            int safeCount = Mathf.Clamp(count, 8, 48);
            Color[] palette =
            {
                MedievalUITheme.GoldSoft,
                MedievalUITheme.SuccessGreen,
                MedievalUITheme.MpSapphire,
                MedievalUITheme.RarityEpic,
                MedievalUITheme.HpWarning
            };

            SpriteRenderer[] pieces = new SpriteRenderer[safeCount];
            Vector2[] vels = new Vector2[safeCount];
            float[] spins = new float[safeCount];
            Color[] cols = new Color[safeCount];

            for (int i = 0; i < safeCount; i++)
            {
                Color c = palette[i % palette.Length];
                cols[i] = c;
                SpriteRenderer sr = RentSprite(GetConfettiSprite(), center, c, 20);
                sr.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.55f, 0.95f);
                pieces[i] = sr;
                float ang = UnityEngine.Random.Range(35f, 145f) * Mathf.Deg2Rad;
                float spd = UnityEngine.Random.Range(2.4f, 5.2f);
                vels[i] = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd);
                spins[i] = UnityEngine.Random.Range(-420f, 420f);
            }

            try
            {
                float elapsed = 0f;
                const float dur = 1.15f;
                while (elapsed < dur)
                {
                    float dt = Time.unscaledDeltaTime;
                    elapsed += dt;
                    float t = Mathf.Clamp01(elapsed / dur);
                    for (int i = 0; i < safeCount; i++)
                    {
                        if (pieces[i] == null) continue;
                        vels[i].y -= 6.5f * dt;
                        pieces[i].transform.position += (Vector3)(vels[i] * dt);
                        pieces[i].transform.Rotate(0f, 0f, spins[i] * dt);
                        Color c = cols[i];
                        c.a = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);
                        pieces[i].color = c;
                    }
                    yield return null;
                }
            }
            finally
            {
                for (int i = 0; i < safeCount; i++) ReturnSprite(pieces[i]);
            }
        }

        private IEnumerator RainOfArrowsRoutine(Vector3 center, Action onImpact)
        {
            const int count = 7;
            bool firedImpact = false;
            for (int i = 0; i < count; i++)
            {
                Vector3 dropTarget = center + new Vector3(UnityEngine.Random.Range(-1.4f, 1.4f), UnityEngine.Random.Range(-0.25f, 0.35f), 0f);
                Vector3 skyStart = dropTarget + new Vector3(-0.9f, 2.7f, 0f);
                Action hitCb = null;
                if (!firedImpact && i == count / 2)
                {
                    firedImpact = true;
                    hitCb = onImpact;
                }
                StartCoroutine(PlayProjectileRoutine(skyStart, dropTarget, new Color(0.65f, 0.95f, 0.55f, 1f), true, 0.20f, hitCb));
                yield return new WaitForSeconds(0.035f);
            }
        }

        // =====================================================================
        // PROCEDURAL PIXEL SPRITES (FILTERMODE.POINT)
        // =====================================================================
        private static Sprite GetSlashSprite()
        {
            if (_slashSprite != null) return _slashSprite;
            const int s = 32;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float nx = (x - 16f) / 14f;
                    float ny = (y - 16f) / 14f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    bool arc = r >= 0.55f && r <= 0.92f && nx > -0.25f && Mathf.Abs(ny) < 0.85f;
                    if (arc)
                    {
                        float alpha = r > 0.72f ? 1f : 0.65f;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            _slashSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _slashSprite;
        }

        private static Sprite GetOrbSprite()
        {
            if (_orbSprite != null) return _orbSprite;
            const int s = 16;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    if (d <= 2.6f) tex.SetPixel(x, y, Color.white);
                    else if (d <= 5.8f) tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.75f));
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            _orbSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _orbSprite;
        }

        private static Sprite GetArrowSprite()
        {
            if (_arrowSprite != null) return _arrowSprite;
            const int w = 24, h = 8;
            Texture2D tex = CreateTex(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, Color.clear);

            for (int x = 2; x < 18; x++)
            {
                tex.SetPixel(x, 3, new Color(0.85f, 0.72f, 0.45f, 1f));
                tex.SetPixel(x, 4, new Color(0.92f, 0.80f, 0.55f, 1f));
            }
            for (int y = 2; y <= 5; y++) tex.SetPixel(18, y, Color.white);
            for (int y = 2; y <= 5; y++) tex.SetPixel(19, y, Color.white);
            for (int y = 3; y <= 4; y++) { tex.SetPixel(20, y, Color.white); tex.SetPixel(21, y, Color.white); }
            tex.SetPixel(1, 2, Color.white); tex.SetPixel(2, 2, Color.white);
            tex.SetPixel(1, 5, Color.white); tex.SetPixel(2, 5, Color.white);
            tex.Apply();
            _arrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return _arrowSprite;
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

        private static Sprite GetShardSprite()
        {
            if (_shardSprite != null) return _shardSprite;
            const int s = 12;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    tex.SetPixel(x, y, (Mathf.Abs(x - 5.5f) + Mathf.Abs(y - 5.5f) <= 4.5f) ? Color.white : Color.clear);
            tex.Apply();
            _shardSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _shardSprite;
        }

        private static Sprite GetDustSprite()
        {
            if (_dustSprite != null) return _dustSprite;
            const int s = 12;
            Texture2D tex = CreateTex(s, s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(5.5f, 5.5f));
                    tex.SetPixel(x, y, d <= 4.5f ? new Color(1f, 1f, 1f, d <= 2.5f ? 0.9f : 0.45f) : Color.clear);
                }
            tex.Apply();
            _dustSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _dustSprite;
        }

        private static Sprite GetConfettiSprite()
        {
            if (_confettiSprite != null) return _confettiSprite;
            const int w = 8, h = 6;
            Texture2D tex = CreateTex(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, (x >= 1 && x <= 6 && y >= 1 && y <= 4) ? Color.white : Color.clear);
            tex.Apply();
            _confettiSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return _confettiSprite;
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
