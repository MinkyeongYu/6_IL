using UnityEngine;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// 부하. Rigidbody2D + 콜라이더로 물리 충돌 보장.
    /// 모드:
    ///  Follow  — Player 주변 원형 대형
    ///  Working — Gatherable 채집 중
    ///  Farming — 밭에 배치돼 수확 대기
    ///  Hiding  — 비전투 동료가 건물 안에 숨음 (시각/콜라이더 비활성)
    ///  Fleeing — 숨은 건물 파괴 → 가까운 적에서 도망
    /// 자동 공격: IsCombat=true 일 때만, 사거리 내 좀비에게 투사체.
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
        public bool IsCombat = true;
        public float AttackRange = 5f;
        public int Damage = 6;
        public float AttackCooldown = 1.6f;
        public float ProjectileSpeed = 7f;

        [Header("Morale")]
        public int Morale = 100;

        public enum Mode { Follow, Working, Farming, Hiding, Fleeing }
        public Mode CurrentMode { get; private set; } = Mode.Follow;
        public Gatherable Target { get; private set; }
        public Transform FarmStation { get; private set; }
        public Building Shelter { get; private set; }

        private float _attackCd;
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private CircleCollider2D _col;
        private System.Action _unsubNight;
        private System.Action _unsubDawn;

        public void AssignGather(Gatherable target)
        {
            if (CurrentMode == Mode.Farming || CurrentMode == Mode.Hiding) return;
            Target = target;
            CurrentMode = target != null ? Mode.Working : Mode.Follow;
        }

        public void AssignFarm(Transform station)
        {
            if (CurrentMode == Mode.Hiding) return;
            FarmStation = station;
            CurrentMode = Mode.Farming;
        }

        public void ReleaseFarm()
        {
            FarmStation = null;
            CurrentMode = Mode.Follow;
        }

        public void HideInBuilding(Building b)
        {
            if (b == null) return;
            Shelter = b;
            b.HostedCompanions.Add(this);
            CurrentMode = Mode.Hiding;
            EnableVisuals(false);
            if (_rb != null) _rb.velocity = Vector2.zero;
            transform.position = b.transform.position;
        }

        public void ExposeAndFlee()
        {
            Shelter = null;
            CurrentMode = Mode.Fleeing;
            EnableVisuals(true);
        }

        private void EnableVisuals(bool on)
        {
            if (_sr != null) _sr.enabled = on;
            if (_col != null) _col.enabled = on;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            _col = GetComponent<CircleCollider2D>();
            if (_col == null)
            {
                _col = gameObject.AddComponent<CircleCollider2D>();
                _col.radius = 0.35f;
            }

            _sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (Player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) Player = p.transform;
            }
            _unsubNight = EventBus.Instance.Subscribe<NightStartedPayload>(_ => OnNightStarted());
            _unsubDawn = EventBus.Instance.Subscribe<DawnStartedPayload>(_ => OnDawnStarted());
        }

        private void OnDestroy()
        {
            _unsubNight?.Invoke();
            _unsubDawn?.Invoke();
            if (Shelter != null) Shelter.HostedCompanions.Remove(this);
        }

        private void OnNightStarted()
        {
            if (IsCombat) return;
            if (CurrentMode == Mode.Farming) return; // 밭에 배치된 비전투원은 그대로
            // 가까운 비-바리게이트 건물에 숨음
            var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            Building best = null;
            float bestDist = float.MaxValue;
            foreach (var b in buildings)
            {
                if (b == null || b.Kind == BuildingKind.Barricade) continue;
                float d = Vector2.Distance(transform.position, b.transform.position);
                if (d < bestDist) { best = b; bestDist = d; }
            }
            if (best != null) HideInBuilding(best);
        }

        private void OnDawnStarted()
        {
            if (CurrentMode == Mode.Hiding)
            {
                if (Shelter != null) Shelter.HostedCompanions.Remove(this);
                Shelter = null;
                CurrentMode = Mode.Follow;
                EnableVisuals(true);
            }
            if (CurrentMode == Mode.Fleeing) CurrentMode = Mode.Follow;
        }

        private void Update()
        {
            _attackCd -= Time.deltaTime;
            if (IsCombat && CurrentMode != Mode.Hiding) TryAttack();
        }

        private void FixedUpdate()
        {
            if (CurrentMode == Mode.Hiding) { if (_rb != null) _rb.velocity = Vector2.zero; return; }
            Vector2 desired;
            switch (CurrentMode)
            {
                case Mode.Working: desired = ComputeWorkingVelocity(); break;
                case Mode.Farming: desired = ComputeFarmingVelocity(); break;
                case Mode.Fleeing: desired = ComputeFleeVelocity(); break;
                default: desired = ComputeFollowVelocity(); break;
            }
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
            if (Target == null) { CurrentMode = Mode.Follow; return Vector2.zero; }
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

        private Vector2 ComputeFarmingVelocity()
        {
            if (FarmStation == null) { CurrentMode = Mode.Follow; return Vector2.zero; }
            Vector2 toFarm = (Vector2)FarmStation.position - (Vector2)transform.position;
            float d = toFarm.magnitude;
            if (d < 0.4f) return Vector2.zero;
            return toFarm.normalized * MoveSpeed * 0.6f;
        }

        private Vector2 ComputeFleeVelocity()
        {
            // 가장 가까운 좀비 반대 방향으로 도망
            var zombies = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
            Zombie nearest = null;
            float bestDist = float.MaxValue;
            foreach (var z in zombies)
            {
                if (z == null || z.IsDead) continue;
                float d = Vector2.Distance(transform.position, z.transform.position);
                if (d < bestDist) { nearest = z; bestDist = d; }
            }
            if (nearest != null && bestDist < 8f)
            {
                Vector2 away = ((Vector2)transform.position - (Vector2)nearest.transform.position).normalized;
                return away * MoveSpeed * 1.4f;
            }
            // 좀비 없으면 Follow 로 복귀
            CurrentMode = Mode.Follow;
            return Vector2.zero;
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
                if (c.CurrentMode == Mode.Hiding) continue;
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
