using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 부하. Rigidbody2D 기반 이동으로 나무·바위·바리게이트·다른 유닛과 물리 충돌.
    /// - 평소엔 Player 주변 원형 대형 슬롯 유지 + 다른 동료와 분리
    /// - AssignGather: 지정 Gatherable 로 이동해 채집 후 복귀
    /// - 자동 공격: 사거리 내 가장 가까운 좀비에게 주기적으로 투사체 발사
    /// </summary>
    public sealed class Companion : MonoBehaviour
    {
        public Transform Player;
        public float FormationRadius = 1.7f;
        public float FollowDistance = 1.8f;
        public float FollowStopDistance = 0.25f;
        public float MoveSpeed = 4.5f;
        public float GatherReach = 0.7f;
        public float SeparationRadius = 0.9f;

        [Header("Combat")]
        public float AttackRange = 5f;
        public int Damage = 6;
        public float AttackCooldown = 1.6f;
        public float ProjectileSpeed = 7f;

        [Header("Morale (식량 부족 시 감소)")]
        public int Morale = 100;

        public enum Mode { Follow, Working }
        public Mode CurrentMode { get; private set; } = Mode.Follow;
        public Gatherable Target { get; private set; }

        private float _attackCd;
        private Rigidbody2D _rb;

        public void AssignGather(Gatherable target)
        {
            Target = target;
            CurrentMode = target != null ? Mode.Working : Mode.Follow;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            var col = GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
                col.radius = 0.35f;
            }
        }

        private void Start()
        {
            if (Player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) Player = p.transform;
            }
        }

        private void Update()
        {
            _attackCd -= Time.deltaTime;
            TryAttack();
        }

        private void FixedUpdate()
        {
            Vector2 desired = Vector2.zero;
            if (CurrentMode == Mode.Working) desired = ComputeWorkingVelocity();
            else desired = ComputeFollowVelocity();
            _rb.velocity = desired;
        }

        private Vector2 ComputeFollowVelocity()
        {
            if (Player == null) return Vector2.zero;
            Vector2 slot = GetFormationSlot();
            Vector2 toSlot = slot - (Vector2)transform.position;
            float d = toSlot.magnitude;

            Vector2 moveDir = Vector2.zero;
            if (d > FollowStopDistance)
            {
                moveDir = toSlot.normalized;
                float speedFactor = Mathf.Clamp01(d / FollowDistance);
                moveDir *= speedFactor;
            }
            moveDir += SeparationFromOthers();

            if (moveDir.sqrMagnitude < 0.0001f) return Vector2.zero;
            float mag = Mathf.Clamp01(moveDir.magnitude);
            return moveDir.normalized * MoveSpeed * mag;
        }

        private Vector2 ComputeWorkingVelocity()
        {
            if (Target == null)
            {
                CurrentMode = Mode.Follow;
                return Vector2.zero;
            }
            Vector2 toTarget = (Vector2)Target.transform.position - (Vector2)transform.position;
            float dist = toTarget.magnitude;
            if (dist < GatherReach)
            {
                var session = GameSession.Instance;
                if (session != null) Target.OnGathered(session.Resources);
                Target = null;
                CurrentMode = Mode.Follow;
                return Vector2.zero;
            }
            var dir = toTarget.normalized + SeparationFromOthers() * 0.5f;
            return dir.normalized * MoveSpeed;
        }

        private Vector2 GetFormationSlot()
        {
            if (Player == null) return transform.position;
            var all = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int n = Mathf.Max(1, all.Length);
            int myId = GetInstanceID();
            int myIdx = 0;
            foreach (var c in all)
            {
                if (c != null && c != this && c.GetInstanceID() < myId) myIdx++;
            }
            float angle = (myIdx / (float)n) * Mathf.PI * 2f + Mathf.PI * 0.5f;
            return (Vector2)Player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * FormationRadius;
        }

        private Vector2 SeparationFromOthers()
        {
            Vector2 sum = Vector2.zero;
            var all = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in all)
            {
                if (c == null || c == this) continue;
                Vector2 diff = (Vector2)transform.position - (Vector2)c.transform.position;
                float d = diff.magnitude;
                if (d > 0.001f && d < SeparationRadius)
                {
                    sum += diff.normalized * ((SeparationRadius - d) / SeparationRadius);
                }
            }
            return sum * 2f;
        }

        private void TryAttack()
        {
            if (_attackCd > 0f) return;
            var z = FindNearestZombie(AttackRange);
            if (z == null) return;
            SpawnProjectile(z);
            _attackCd = AttackCooldown * GetCampfireFireRateMul();
        }

        private float GetCampfireFireRateMul()
        {
            var auras = Object.FindObjectsByType<CampfireAura>(FindObjectsSortMode.None);
            foreach (var a in auras)
            {
                if (a == null) continue;
                if (Vector2.Distance(transform.position, a.transform.position) < a.Radius) return 0.7f;
            }
            return 1f;
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

        private void SpawnProjectile(Zombie target)
        {
            var go = new GameObject("CompProjectile");
            go.transform.position = transform.position;
            go.transform.localScale = Vector3.one * 0.28f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 1f, 0.7f);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 32;
            cf.OutlineWidth = 1;
            cf.OutlineColor = new Color(0.15f, 0.4f, 0.25f, 1f);

            var proj = go.AddComponent<Projectile>();
            proj.Speed = ProjectileSpeed;
            proj.Damage = Damage;
            proj.HitRadius = 0.4f;
            proj.Aim(target, transform.position);
        }
    }
}
