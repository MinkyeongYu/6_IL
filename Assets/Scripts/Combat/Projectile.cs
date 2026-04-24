using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 직선 or 호밍 투사체. 타겟 히트 시 대미지 적용 후 파괴.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        public float Speed = 8f;
        public int Damage = 10;
        public float MaxLifetime = 3f;
        public float HitRadius = 0.35f;
        public Zombie Target;

        private Vector2 _fallbackDir = Vector2.right;
        private float _life;

        public void AimAt(Zombie target)
        {
            Target = target;
            if (target != null)
            {
                _fallbackDir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            }
        }

        private void Update()
        {
            _life += Time.deltaTime;
            if (_life > MaxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 dir;
            if (Target != null && !Target.IsDead)
            {
                dir = ((Vector2)Target.transform.position - (Vector2)transform.position).normalized;
            }
            else
            {
                dir = _fallbackDir;
            }
            transform.position += (Vector3)(dir * Speed * Time.deltaTime);

            if (Target != null && !Target.IsDead)
            {
                float d = Vector2.Distance(transform.position, Target.transform.position);
                if (d < HitRadius)
                {
                    Target.TakeDamage(Damage);
                    Destroy(gameObject);
                }
            }
        }
    }
}
