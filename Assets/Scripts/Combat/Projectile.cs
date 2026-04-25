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

        private void Start()
        {
            // 본체 뒤에 1.6x 크기의 반투명 글로우 자식 — 같은 색상 그라데이션처럼 보이게.
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                _glow = new GameObject("__glow");
                _glow.transform.SetParent(transform, false);
                _glow.transform.localScale = Vector3.one * 1.7f;
                var gs = _glow.AddComponent<SpriteRenderer>();
                gs.sortingOrder = sr.sortingOrder - 1;
                var cf = _glow.AddComponent<ColorFallback>();
                Color c = sr.color;
                c.a = 0.35f;
                cf.Tint = c;
                cf.Shape = FallbackShape.Circle;
                cf.Circle = true;
                cf.PixelSize = 32;
                cf.OutlineWidth = 0;
                cf.OutlineColor = new Color(0, 0, 0, 0);
            }
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

            // 트레일: 0.04초 간격으로 fading 복제본 1개
            _trailTimer -= Time.deltaTime;
            if (_trailTimer <= 0f)
            {
                _trailTimer = 0.04f;
                SpawnTrailGhost();
            }

            if (targetAlive)
            {
                float d = Vector2.Distance(transform.position, _target.transform.position);
                if (d < HitRadius)
                {
                    DealDamage(_target);
                    Destroy(gameObject);
                }
            }
        }

        private void SpawnTrailGhost()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            var go = new GameObject("__pjtrail");
            go.transform.position = transform.position;
            go.transform.localScale = transform.lossyScale * 0.7f;
            var ts = go.AddComponent<SpriteRenderer>();
            ts.sortingOrder = sr.sortingOrder - 2;
            var cf = go.AddComponent<ColorFallback>();
            Color c = sr.color;
            c.a = 0.5f;
            cf.Tint = c;
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 16;
            cf.OutlineWidth = 0;
            cf.OutlineColor = new Color(0, 0, 0, 0);
            Destroy(go, 0.22f);
        }

        private static bool IsTargetAlive(MonoBehaviour target)
        {
            if (target == null) return false;
            if (target is Zombie z) return !z.IsDead;
            if (target is DeerAi) return true;
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
            if (target is DeerAi deer)
            {
                var g = deer.GetComponent<Gatherable>();
                var session = GameSession.Instance;
                if (g != null && session != null) g.OnGathered(session.Resources);
                else if (deer != null && deer.gameObject != null) Destroy(deer.gameObject);
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
