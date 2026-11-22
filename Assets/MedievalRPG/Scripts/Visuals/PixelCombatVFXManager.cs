using System;
using System.Collections;
using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.Visuals
{
    /// <summary>
    /// Quản lý hiệu ứng chiến đấu Pixel-Art (Vệt chém, Đạn phép/Tên bay, Nổ AoE, Cột sáng hồi máu,
    /// Phá Khiên Bóng Tối, Mưa Tên, Bụi hành quân, Hitstop & Rung Camera nhẹ).
    /// Toàn bộ sprite hiệu ứng được sinh procedural sắc nét (FilterMode.Point).
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
        }

        private static Sprite _slashSprite;
        private static Sprite _orbSprite;
        private static Sprite _arrowSprite;
        private static Sprite _ringSprite;
        private static Sprite _shardSprite;
        private static Sprite _dustSprite;

        private Coroutine _shakeCoroutine;
        private Coroutine _hitstopCoroutine;
        private Vector3 _cameraRestPos;
        private bool _hasCameraRestPos;

        public static PixelCombatVFXManager EnsureExists()
        {
            if (Instance != null) return Instance;
            PixelCombatVFXManager existing = FindAnyObjectByType<PixelCombatVFXManager>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }
            GameObject go = new GameObject("PixelCombatVFXManager");
            return go.AddComponent<PixelCombatVFXManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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

        public IEnumerator PlayProjectileRoutine(Vector3 from, Vector3 to, Color color, bool isArrow, float duration, Action onHit)
        {
            GameObject go = new GameObject(isArrow ? "VFX_Arrow" : "VFX_Orb");
            go.transform.position = from + new Vector3(0f, 0.30f, -0.08f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isArrow ? GetArrowSprite() : GetOrbSprite();
            sr.color = color;
            sr.sortingOrder = 16;

            Vector3 start = from + new Vector3(0f, 0.30f, -0.08f);
            Vector3 end = to + new Vector3(0f, 0.30f, -0.08f);
            Vector3 dir = (end - start);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (isArrow) go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            float arcHeight = isArrow ? 0.42f : 0.14f;
            float elapsed = 0f;
            float dur = Mathf.Max(0.08f, duration);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                Vector3 pos = Vector3.Lerp(start, end, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                go.transform.position = pos;
                if (!isArrow)
                {
                    go.transform.Rotate(0f, 0f, 480f * Time.deltaTime);
                    go.transform.localScale = Vector3.one * (0.95f + 0.25f * Mathf.Sin(t * Mathf.PI));
                }
                yield return null;
            }

            Destroy(go);
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
            if (Time.timeScale <= 0.05f || _hitstopCoroutine != null) return;
            _hitstopCoroutine = StartCoroutine(HitstopRoutine(unscaledDuration));
        }

        private IEnumerator HitstopRoutine(float dur)
        {
            float prev = Time.timeScale;
            Time.timeScale = 0.08f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (Time.timeScale < 0.2f) Time.timeScale = prev > 0.1f ? prev : 1f;
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
            GameObject go = new GameObject("VFX_Slash");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, flipX ? 18f : -18f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSlashSprite();
            sr.color = color;
            sr.sortingOrder = 16;

            float elapsed = 0f;
            const float dur = 0.20f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(flipX ? 28f : -28f, flipX ? -24f : 24f, t));
                Color c = color;
                c.a = 1f - t * t;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        private IEnumerator ExplosionBurstRoutine(Vector3 pos, Color color, float radiusScale)
        {
            GameObject ring = new GameObject("VFX_Burst");
            ring.transform.position = pos;
            SpriteRenderer rSr = ring.AddComponent<SpriteRenderer>();
            rSr.sprite = GetRingSprite();
            rSr.color = color;
            rSr.sortingOrder = 16;

            // Vài tia lửa xung quanh
            const int sparkCount = 6;
            Transform[] sparks = new Transform[sparkCount];
            SpriteRenderer[] sparkSrs = new SpriteRenderer[sparkCount];
            Vector2[] dirs = new Vector2[sparkCount];
            for (int i = 0; i < sparkCount; i++)
            {
                float ang = (i / (float)sparkCount) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                dirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (1.25f * radiusScale);
                GameObject sp = new GameObject("VFX_Spark");
                sp.transform.position = pos;
                sp.transform.localScale = Vector3.one * 0.55f;
                SpriteRenderer sSr = sp.AddComponent<SpriteRenderer>();
                sSr.sprite = GetOrbSprite();
                sSr.color = Color.Lerp(color, Color.white, 0.35f);
                sSr.sortingOrder = 17;
                sparks[i] = sp.transform;
                sparkSrs[i] = sSr;
            }

            float elapsed = 0f;
            const float dur = 0.32f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.55f * radiusScale, t);
                Color rc = color;
                rc.a = 1f - t;
                rSr.color = rc;

                for (int i = 0; i < sparkCount; i++)
                {
                    if (sparks[i] == null) continue;
                    sparks[i].position = pos + (Vector3)(dirs[i] * t);
                    Color sc = sparkSrs[i].color;
                    sc.a = 1f - t;
                    sparkSrs[i].color = sc;
                }
                yield return null;
            }

            Destroy(ring);
            for (int i = 0; i < sparkCount; i++)
            {
                if (sparks[i] != null) Destroy(sparks[i].gameObject);
            }
        }

        private IEnumerator HealColumnRoutine(Vector3 worldPos)
        {
            GameObject ring = new GameObject("VFX_HealRing");
            ring.transform.position = worldPos + new Vector3(0f, -0.1f, -0.08f);
            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = GetRingSprite();
            sr.color = new Color(0.45f, 1f, 0.68f, 0.9f);
            sr.sortingOrder = 16;

            const int motes = 5;
            Transform[] mTr = new Transform[motes];
            SpriteRenderer[] mSr = new SpriteRenderer[motes];
            Vector3[] offsets = new Vector3[motes];
            for (int i = 0; i < motes; i++)
            {
                offsets[i] = new Vector3((i - 2) * 0.22f, UnityEngine.Random.Range(-0.15f, 0.25f), -0.08f);
                GameObject m = new GameObject("HealMote");
                m.transform.position = worldPos + offsets[i];
                m.transform.localScale = Vector3.one * 0.42f;
                SpriteRenderer s = m.AddComponent<SpriteRenderer>();
                s.sprite = GetOrbSprite();
                s.color = new Color(0.95f, 0.95f, 0.45f, 1f);
                s.sortingOrder = 17;
                mTr[i] = m.transform;
                mSr[i] = s;
            }

            float elapsed = 0f;
            const float dur = 0.55f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                ring.transform.position = worldPos + new Vector3(0f, Mathf.Lerp(-0.15f, 0.95f, t), -0.08f);
                ring.transform.localScale = new Vector3(1.1f, 0.55f, 1f) * (1f + 0.35f * t);
                Color c = sr.color;
                c.a = Mathf.Sin(t * Mathf.PI);
                sr.color = c;

                for (int i = 0; i < motes; i++)
                {
                    if (mTr[i] == null) continue;
                    mTr[i].position = worldPos + offsets[i] + new Vector3(0f, t * 1.15f, 0f);
                    Color mc = mSr[i].color;
                    mc.a = 1f - t;
                    mSr[i].color = mc;
                }
                yield return null;
            }

            Destroy(ring);
            for (int i = 0; i < motes; i++)
            {
                if (mTr[i] != null) Destroy(mTr[i].gameObject);
            }
        }

        private IEnumerator ShieldShatterRoutine(Vector3 center)
        {
            const int shards = 8;
            Transform[] tr = new Transform[shards];
            SpriteRenderer[] srs = new SpriteRenderer[shards];
            Vector2[] vels = new Vector2[shards];
            for (int i = 0; i < shards; i++)
            {
                float ang = (i / (float)shards) * Mathf.PI * 2f;
                vels[i] = new Vector2(Mathf.Cos(ang) * 1.9f, Mathf.Sin(ang) * 1.5f + 0.7f);
                GameObject sh = new GameObject("ShieldShard");
                sh.transform.position = center;
                sh.transform.localScale = Vector3.one * 0.75f;
                SpriteRenderer s = sh.AddComponent<SpriteRenderer>();
                s.sprite = GetShardSprite();
                s.color = new Color(0.75f, 0.45f, 1f, 1f);
                s.sortingOrder = 17;
                tr[i] = sh.transform;
                srs[i] = s;
            }

            float elapsed = 0f;
            const float dur = 0.45f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                for (int i = 0; i < shards; i++)
                {
                    tr[i].position = center + (Vector3)(vels[i] * t) + new Vector3(0f, -1.6f * t * t, 0f);
                    tr[i].Rotate(0f, 0f, 360f * Time.deltaTime);
                    Color c = srs[i].color;
                    c.a = 1f - t;
                    srs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < shards; i++) Destroy(tr[i].gameObject);
        }

        private IEnumerator DustPuffRoutine(Vector3 pos)
        {
            GameObject go = new GameObject("VFX_Dust");
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDustSprite();
            sr.color = new Color(0.76f, 0.66f, 0.52f, 0.75f);
            sr.sortingOrder = 4;

            float elapsed = 0f;
            const float dur = 0.28f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                go.transform.position = pos + new Vector3(-0.35f * t, 0.14f * t, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.15f, t);
                Color c = sr.color;
                c.a = (1f - t) * 0.75f;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
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
            // Mũi tên nhọn bên phải
            for (int y = 2; y <= 5; y++) tex.SetPixel(18, y, Color.white);
            for (int y = 2; y <= 5; y++) tex.SetPixel(19, y, Color.white);
            for (int y = 3; y <= 4; y++) { tex.SetPixel(20, y, Color.white); tex.SetPixel(21, y, Color.white); }
            // Lông đuôi
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

        private static Texture2D CreateTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
