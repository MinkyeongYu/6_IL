using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 직선/호밍 투사체. 히트 시 OwnerProgression 의 누적 효과 (독/슬로우/번개체인/폭발) 적용.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        public float Speed = 8f;
        public int Damage = 10;
        public float MaxLifetime = 3f;
        public float HitRadius = 0.35f;
        public PlayerProgression OwnerProgression;

        private MonoBehaviour _target;
        private Vector2 _direction = Vector2.right;
        private float _life;
        private float _trailTimer;
        private GameObject _glow;

        // 모든 투사체가 공유하는 흰 원 스프라이트 — 한 번만 생성, 색은 sr.color 로.
        private static Sprite _sharedCircle;
        private static Sprite _sharedRing;

        private static Sprite GetCircleSprite()
        {
            if (_sharedCircle != null) return _sharedCircle;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = N / 2f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d2 = dx * dx + dy * dy;
                    bool inside = d2 < (r - 2) * (r - 2);
                    bool ring = d2 < r * r && !inside;
                    if (inside) tex.SetPixel(x, y, Color.white);
                    else if (ring) tex.SetPixel(x, y, new Color(0f, 0f, 0f, 1f));
                    else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            tex.Apply();
            _sharedCircle = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _sharedCircle;
        }

        private static Sprite GetGlowSprite()
        {
            if (_sharedRing != null) return _sharedRing;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = N / 2f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = x - r, dy = y - r;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float t = Mathf.Clamp01(1f - dist / r);
                    // 부드러운 가우시안풍 그라데이션
                    float a = t * t * t;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            _sharedRing = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return _sharedRing;
        }

        private void Awake()
        {
            // 본체 스프라이트 즉시 생성 — ColorFallback 실행 순서에 의존 X
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = GetCircleSprite();
                if (sr.color.a < 0.05f) sr.color = new Color(1f, 0.95f, 0.4f, 1f);
            }
        }

        private void Start()
        {
            // 큰 반투명 글로우 자식 — soft 그라데이션 스프라이트.
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            _glow = new GameObject("__glow");
            _glow.transform.SetParent(transform, false);
            _glow.transform.localScale = Vector3.one * 2.4f;
            var gs = _glow.AddComponent<SpriteRenderer>();
            gs.sortingOrder = sr.sortingOrder - 1;
            gs.sprite = GetGlowSprite();
            Color c = sr.color;
            if (c.a < 0.05f) c = new Color(1f, 0.95f, 0.4f, 1f);
            c.a = 0.6f;
            gs.color = c;
        }

        public void Aim(MonoBehaviour target, Vector3 spawnPos)
        {
            _target = target;
            if (target != null)
            {
                Vector2 d = (Vector2)target.transform.position - (Vector2)spawnPos;
                if (d.sqrMagnitude > 0.0001f) _direction = d.normalized;
            }
        }

        public void AimDirection(Vector2 dir)
        {
            if (dir.sqrMagnitude > 0.0001f) _direction = dir.normalized;
        }

        private void Update()
        {
            _life += Time.deltaTime;
            if (_life > MaxLifetime) { Destroy(gameObject); return; }

            bool targetAlive = IsTargetAlive(_target);
            if (targetAlive)
            {
                Vector2 toTarget = (Vector2)_target.transform.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.0001f) _direction = toTarget.normalized;
            }
            transform.position += (Vector3)(_direction * Speed * Time.deltaTime);

            // 트레일: 0.025초 간격으로 fading 복제본 1개
            _trailTimer -= Time.deltaTime;
            if (_trailTimer <= 0f)
            {
                _trailTimer = 0.025f;
                SpawnTrailGhost();
            }

            if (targetAlive)
            {
                float d = Vector2.Distance(transform.position, _target.transform.position);
                if (d < HitRadius)
                {
                    DealDamage(_target);
                    EnsurePierceInit();
                    // 관통 — 추가로 적중할 수 있는 횟수 남았으면 destroy 안 함
                    if (_pierceLeft > 0)
                    {
                        _pierceLeft--;
                        // 다음 타겟 찾기 — 이미 친 적은 다시 안 침
                        _hitOnce.Add(_target);
                        _target = FindNextTargetInLine();
                        if (_target == null) Destroy(gameObject);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        private int _pierceLeft = -1;
        private readonly System.Collections.Generic.HashSet<MonoBehaviour> _hitOnce = new();
        private void EnsurePierceInit()
        {
            if (_pierceLeft >= 0) return;
            _pierceLeft = OwnerProgression != null ? OwnerProgression.PierceExtraHits : 0;
        }

        private MonoBehaviour FindNextTargetInLine()
        {
            EnsurePierceInit();
            // 같은 방향으로 가까운 적 (Zombie 우선)
            Vector2 from = transform.position;
            float bestDist = 5f;
            MonoBehaviour best = null;
            var zs = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
            foreach (var z in zs)
            {
                if (z == null || z.IsDead || _hitOnce.Contains(z)) continue;
                Vector2 to = (Vector2)z.transform.position - from;
                if (Vector2.Dot(to.normalized, _direction) < 0.5f) continue; // 정면만
                float d = to.magnitude;
                if (d < bestDist) { bestDist = d; best = z; }
            }
            return best;
        }

        private void SpawnTrailGhost()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            var go = new GameObject("__pjtrail");
            go.transform.position = transform.position;
            go.transform.localScale = transform.lossyScale * 0.85f;
            var ts = go.AddComponent<SpriteRenderer>();
            ts.sortingOrder = sr.sortingOrder - 2;
            ts.sprite = GetGlowSprite();
            Color c = sr.color;
            if (c.a < 0.05f) c = new Color(1f, 0.95f, 0.4f, 1f);
            c.a = 0.6f;
            ts.color = c;
            Destroy(go, 0.35f);
        }

        private static bool IsTargetAlive(MonoBehaviour target)
        {
            if (target == null) return false;
            if (target is Zombie z) return !z.IsDead;
            if (target is AnimalAi a) return !a.IsDead;
            return false;
        }

        private void DealDamage(MonoBehaviour target)
        {
            if (target is Zombie z && !z.IsDead)
            {
                z.TakeDamage(Damage);
                ApplyEffectsTo(z);
                return;
            }
            if (target is AnimalAi animal)
            {
                animal.TakeDamage(Damage);
            }
        }

        public void ApplyEffectsTo(Zombie z)
        {
            if (OwnerProgression == null || z == null) return;
            if (OwnerProgression.GetStacks(RuneKind.PoisonBlade) > 0)
                z.ApplyPoison(OwnerProgression.PoisonDurationCalc, OwnerProgression.PoisonDpsCalc);
            if (OwnerProgression.GetStacks(RuneKind.IceArrow) > 0)
                z.ApplySlow(OwnerProgression.IceSlowDurationCalc);
            if (OwnerProgression.GetStacks(RuneKind.LightningStrike) > 0)
            {
                if (Random.value < OwnerProgression.LightningChance)
                    ChainLightning(z.transform.position, z, OwnerProgression.LightningJumps, OwnerProgression.LightningDmg);
            }
            if (z.IsDead && OwnerProgression.GetStacks(RuneKind.Detonator) > 0)
            {
                Detonate(z.transform.position, OwnerProgression.DetonateDmg, OwnerProgression.DetonateRadius);
            }
            // 흡혈 — 처치 시 플레이어 회복
            if (z.IsDead && OwnerProgression.VampirismHeal > 0)
            {
                var pc = OwnerProgression.GetComponent<PlayerController>();
                if (pc != null) pc.Heal(OwnerProgression.VampirismHeal);
            }
        }

        public static void Detonate(Vector3 pos, int dmg, float radius)
        {
            if (radius <= 0f) return;
            var hits = Physics2D.OverlapCircleAll(pos, radius);
            foreach (var h in hits)
            {
                var zz = h.GetComponent<Zombie>();
                if (zz != null && !zz.IsDead) zz.TakeDamage(dmg);
            }
            var fx = new GameObject("Boom");
            fx.transform.position = pos;
            fx.transform.localScale = Vector3.one * radius;
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 11;
            var cf = fx.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 0.55f, 0.15f, 0.7f);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 64;
            cf.OutlineWidth = 3;
            cf.OutlineColor = new Color(1f, 0.85f, 0.3f, 1f);
            Object.Destroy(fx, 0.4f);
        }

        public static void ChainLightning(Vector3 from, Zombie origin, int jumps, int dmg)
        {
            var visited = new HashSet<Zombie> { origin };
            Vector3 prevPos = from;
            for (int i = 0; i < jumps; i++)
            {
                var hits = Physics2D.OverlapCircleAll(prevPos, 3.5f);
                Zombie next = null;
                float bestDist = float.MaxValue;
                foreach (var h in hits)
                {
                    var z = h.GetComponent<Zombie>();
                    if (z == null || z.IsDead || visited.Contains(z)) continue;
                    float d = Vector2.Distance(prevPos, z.transform.position);
                    if (d < bestDist) { bestDist = d; next = z; }
                }
                if (next == null) break;
                next.TakeDamage(dmg);
                visited.Add(next);
                SpawnLightningVisual(prevPos, next.transform.position);
                prevPos = next.transform.position;
            }
        }

        private static void SpawnLightningVisual(Vector3 a, Vector3 b)
        {
            var fx = new GameObject("Bolt");
            Vector3 mid = (a + b) * 0.5f;
            float len = Vector3.Distance(a, b);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            fx.transform.position = mid;
            fx.transform.rotation = Quaternion.Euler(0, 0, angle);
            fx.transform.localScale = new Vector3(len, 0.08f, 1f);
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 12;
            var cf = fx.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.7f, 0.95f, 1f, 1f);
            cf.Shape = FallbackShape.Square;
            cf.Circle = false;
            cf.PixelSize = 32;
            cf.OutlineWidth = 0;
            Object.Destroy(fx, 0.18f);
        }
    }
}
