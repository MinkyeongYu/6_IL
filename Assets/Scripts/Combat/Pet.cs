using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 룬으로 소환되는 펫. Player 주변 궤도로 따라다니며 사거리 안 좀비 자동 공격.
    /// 무적 (HP/Collider 없음 → Zombie 가 타겟팅 안 함). Dog 는 근접, Hawk 는 원거리.
    /// </summary>
    public sealed class Pet : MonoBehaviour
    {
        public enum Kind { Dog, Hawk }
        public Kind Type = Kind.Dog;
        public Transform Player;
        public float FollowOffset = 1.6f;
        public float MoveSpeed = 6f;
        public float AttackRange = 4f;
        public int Damage = 5;
        public float AttackCooldown = 0.7f;
        public Color Tint = Color.white;

        private float _attackCd;
        private float _orbitPhase;

        public static GameObject Spawn(Kind kind, Transform player, int level = 1)
        {
            var go = new GameObject($"Pet_{kind}");
            go.transform.position = player.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
            go.transform.localScale = Vector3.one * (kind == Kind.Hawk ? 0.45f : 0.55f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;

            var cf = go.AddComponent<ColorFallback>();
            cf.Shape = kind == Kind.Hawk ? FallbackShape.Triangle : FallbackShape.Rounded;
            cf.Circle = false;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.1f, 0.1f, 0.1f, 1f);

            var pet = go.AddComponent<Pet>();
            pet.Type = kind;
            pet.Player = player;

            if (kind == Kind.Dog)
            {
                // 개는 지상 유닛 — 다른 유닛처럼 충돌 + Kinematic Rigidbody2D (적이 무시할 수 있도록 트리거 X 콜라이더)
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.bodyType = RigidbodyType2D.Dynamic; // 다른 유닛에 막히고 밀림
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.30f;
                pet.MoveSpeed = 6.5f;
                pet.AttackRange = 1.4f;
                pet.AttackCooldown = 0.55f;
                pet.Damage = 5 + (level - 1) * 3;
                pet.Tint = new Color(0.55f, 0.40f, 0.20f);
            }
            else // Hawk
            {
                pet.MoveSpeed = 7.5f;
                pet.AttackRange = 5.5f;
                pet.AttackCooldown = 0.9f;
                pet.Damage = 4 + (level - 1) * 3;
                pet.Tint = new Color(0.95f, 0.92f, 0.78f);
            }
            cf.Tint = pet.Tint;
            sr.color = pet.Tint;
            return go;
        }

        private void Start()
        {
            _orbitPhase = Random.Range(0f, Mathf.PI * 2f);
            if (Player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) Player = p.transform;
            }
        }

        private Rigidbody2D _rb;

        private void Move(Vector2 dir)
        {
            // Dog 처럼 Rigidbody2D 가 있으면 velocity 로 (충돌 정상 작동)
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) _rb.velocity = dir * MoveSpeed;
            else transform.position += (Vector3)(dir * MoveSpeed * Time.deltaTime);
        }

        private void StopMove()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) _rb.velocity = Vector2.zero;
        }

        private void Update()
        {
            if (Player == null) return;
            _attackCd -= Time.deltaTime;

            var target = FindNearestZombie(AttackRange + 2f);
            if (target != null)
            {
                float dist = Vector2.Distance(transform.position, target.transform.position);
                if (dist > AttackRange)
                {
                    Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
                    Move(dir);
                }
                else
                {
                    StopMove();
                    if (_attackCd <= 0f)
                    {
                        AttackZombie(target);
                        _attackCd = AttackCooldown;
                    }
                }
            }
            else
            {
                float t = Time.time * 1.2f + _orbitPhase;
                Vector2 dest = (Vector2)Player.position + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * FollowOffset;
                Vector2 toDest = dest - (Vector2)transform.position;
                if (toDest.magnitude > 0.1f) Move(toDest.normalized);
                else StopMove();
            }
        }

        private void AttackZombie(Zombie z)
        {
            if (Type == Kind.Hawk)
            {
                var go = new GameObject("HawkProj");
                go.transform.position = transform.position;
                go.transform.localScale = Vector3.one * 0.65f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 50;
                sr.color = new Color(1f, 0.95f, 0.5f);
                var proj = go.AddComponent<Projectile>();
                proj.Speed = 12f;
                proj.Damage = Damage;
                proj.HitRadius = 0.45f;
                proj.Aim(z, transform.position);
            }
            else
            {
                z.TakeDamage(Damage);
            }
        }

        private Zombie FindNearestZombie(float range)
        {
            var all = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
            Zombie best = null;
            float bestDist = range;
            foreach (var z in all)
            {
                if (z == null || z.IsDead) continue;
                float d = Vector2.Distance(transform.position, z.transform.position);
                if (d < bestDist) { best = z; bestDist = d; }
            }
            return best;
        }
    }
}
