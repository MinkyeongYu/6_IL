using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 직선/호밍 투사체. 타겟 살아있으면 호밍, 죽거나 사라지면 마지막 방향 직진.
    /// 히트 시 Zombie/DeerAi 처리 + Owner 의 PlayerProgression 효과 (독/슬로우/폭발) 적용.
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
            if (OwnerProgression.PoisonStacks > 0)
            {
                z.ApplyPoison(3f, OwnerProgression.PoisonStacks * 5);
            }
            if (OwnerProgression.IceStacks > 0)
            {
                z.ApplySlow(2f + OwnerProgression.IceStacks);
            }
            if (z.IsDead && OwnerProgression.DetonatorStacks > 0)
            {
                Detonate(z.transform.position, OwnerProgression.DetonatorStacks * 8);
            }
        }

        public static void Detonate(Vector3 pos, int dmg)
        {
            var hits = Physics2D.OverlapCircleAll(pos, 1.6f);
            foreach (var h in hits)
            {
                var zz = h.GetComponent<Zombie>();
                if (zz != null && !zz.IsDead) zz.TakeDamage(dmg);
            }
            // 시각: 짧은 주황 원 펄스 (1초 후 소멸)
            var fx = new GameObject("Boom");
            fx.transform.position = pos;
            fx.transform.localScale = Vector3.one * 1.6f;
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
    }
}
