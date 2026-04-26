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
        public float SightRange = 9f;     // AttackRange 보다 넓음 — 적 발견 시 추격
        public int Damage = 6;
        public float AttackCooldown = 1.6f;
        public float ProjectileSpeed = 7f;

        [Header("Morale")]
        public int Morale = 100;

        [Header("Health")]
        public int MaxHp = 50;
        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        public float LastDamagedAt { get; private set; } = -100f;

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            LastDamagedAt = Time.time;
            // 채집/농사 중 피격 시 즉시 중단 — 다음 프레임 ComputeFollowVelocity 가 적 추격 시작.
            if (CurrentMode == Mode.Working || CurrentMode == Mode.Farming)
            {
                Target = null;
                CurrentMode = Mode.Follow;
            }
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            GameFeel.HitFlash(this, GetComponent<SpriteRenderer>());
            if (CurrentHp <= 0)
            {
                var s = GameSession.Instance;
                if (s != null) s.OnCompanionLost();
                Destroy(gameObject);
            }
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        public enum Mode { Follow, Working, Farming, Hiding, Fleeing }
        public enum Stance { Follow, Hold, Aggressive }
        public Mode CurrentMode { get; private set; } = Mode.Follow;
        public Stance CurrentStance = Stance.Follow;
        public Gatherable Target { get; private set; }
        public Transform FarmStation { get; private set; }
        public Building Shelter { get; private set; }
        private Vector3 _holdAnchor;

        public void SetStance(Stance s)
        {
            CurrentStance = s;
            if (s == Stance.Hold) _holdAnchor = transform.position;
        }

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

            if (CurrentHp <= 0) CurrentHp = MaxHp;

            // 자동 HP 바 부착 (씬에서 미리 설정 안 했을 때만)
            if (GetComponent<HpBarUi>() == null)
            {
                var hp = gameObject.AddComponent<HpBarUi>();
                hp.CompanionRef = this;
                hp.Offset = new Vector2(0f, 0.55f);
                hp.Size = new Vector2(0.7f, 0.10f);
                hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
                hp.FillColor = new Color(0.45f, 0.85f, 0.55f);
            }
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
                if (b == null) continue;
                if (b.Kind == BuildingKind.Barricade || b.Kind == BuildingKind.Fence) continue;
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

        private float _regenTimer;
        public float RegenIntervalSec = 4f;

        private void Update()
        {
            _attackCd -= Time.deltaTime;
            // 채집/농사 중엔 전투 안 함 — 피격 시에만 TakeDamage 가 모드 전환.
            bool combatAllowed = IsCombat
                && CurrentMode != Mode.Hiding
                && CurrentMode != Mode.Working
                && CurrentMode != Mode.Farming;
            if (combatAllowed) TryAttack();

            if (!IsDead)
            {
                _regenTimer += Time.deltaTime;
                if (_regenTimer >= RegenIntervalSec)
                {
                    _regenTimer = 0f;
                    if (CurrentHp < MaxHp) Heal(1);
                }
            }
        }

        private static readonly Vector2 VillageCenter = new Vector2(GameConstants.VillageCenterX, GameConstants.VillageCenterY);
        // VillageStarter.SpawnStarterVillage 의 halfSize 와 일치 — 펜스 사각 경계.
        private const float VillageHalfSize = 5f;
        private const float SprintSpeedMul = 2.2f; // 문 향해 달릴 때 속도 배수

        /// <summary>주어진 위치가 마을 펜스 사각형 안인지 (원형 거리 X, 사각 경계 O).</summary>
        private static bool IsInsideVillageBounds(Vector2 pos)
        {
            Vector2 d = pos - VillageCenter;
            return Mathf.Abs(d.x) < VillageHalfSize && Mathf.Abs(d.y) < VillageHalfSize;
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
            if (CurrentStance == Stance.Hold)
            {
                Vector2 toAnchor = (Vector2)_holdAnchor - (Vector2)transform.position;
                Vector2 sep = SeparationFromOthers();
                if (toAnchor.magnitude > 0.3f) return toAnchor.normalized * MoveSpeed * 0.7f + sep;
                return sep;
            }

            if (Player == null) return Vector2.zero;

            // 우선순위: 플레이어가 시야를 벗어났으면 적 무시하고 따라감.
            float playerDist = Vector2.Distance((Vector2)transform.position, (Vector2)Player.position);
            bool playerInSight = playerDist < SightRange;
            if (!playerInSight)
            {
                // formation slot 으로 빠르게 — 마을 사각/문 통과는 아래 분기에서 처리
                // 그냥 fall-through 해서 마을/문/포메이션 follow 로 진행
            }
            else if (IsCombat)
            {
                // 플레이어 시야 안 + 적 시야 안 → 적 우선 추격
                Transform enemy = FindAnyHostileInSight();
                if (enemy != null)
                {
                    Vector2 toE = (Vector2)enemy.position - (Vector2)transform.position;
                    float de = toE.magnitude;
                    Vector2 sep = SeparationFromOthers();
                    if (de > AttackRange * 0.85f) return toE.normalized * MoveSpeed + sep;
                    return sep; // 사거리 안 — 멈추고 자동 공격
                }
                // 적이 없고 플레이어가 가까이 있으면 자리 유지
                if (playerDist < FormationRadius * 1.5f) return SeparationFromOthers();
            }

            bool playerInside = IsInsideVillageBounds((Vector2)Player.position);
            bool selfInside = IsInsideVillageBounds((Vector2)transform.position);

            // 1) 플레이어 마을 안 + 동료 마을 안 → 자리 지킴 (앵커)
            if (playerInside && selfInside) return SeparationFromOthers();

            // 2) 플레이어와 동료가 펜스를 사이에 두고 다른 쪽 → 문 통과 2단계 경로
            //    Step 1: 문의 수직 축에 정렬 (좌우 오프셋 줄임)
            //    Step 2: 정렬되면 문 방향으로 직진해 통과
            if (playerInside != selfInside)
            {
                var door = Door.FindNearest((Vector2)transform.position);
                if (door != null)
                {
                    Vector2 doorPos = door.transform.position;
                    Vector2 outward = (doorPos - VillageCenter).normalized;        // 문이 마을 밖을 향하는 방향
                    Vector2 tangent = new Vector2(-outward.y, outward.x);          // 문의 좌우 축

                    Vector2 fromDoor = (Vector2)transform.position - doorPos;
                    float alongOutward = Vector2.Dot(fromDoor, outward);  // +: 동료가 문 바깥쪽 / -: 안쪽
                    float alongTangent = Vector2.Dot(fromDoor, tangent);  // 문 좌우 오프셋

                    // Step 1: 좌우 정렬 — 펜스 옆에 부딪히지 않도록 같은 거리에서 tangent=0 으로
                    if (Mathf.Abs(alongTangent) > 0.4f)
                    {
                        Vector2 alignTarget = doorPos + outward * alongOutward;
                        Vector2 toAlign = alignTarget - (Vector2)transform.position;
                        return toAlign.normalized * MoveSpeed * SprintSpeedMul;
                    }

                    // Step 2: 정렬 완료 — 문 통과 (selfInside면 바깥으로, 아니면 안으로)
                    Vector2 throughDir = selfInside ? outward : -outward;
                    return throughDir * MoveSpeed * SprintSpeedMul;
                }
            }

            // 3) 둘 다 마을 밖 → 일반 종대 follow
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
            // 채집 중엔 전투 무시 — 적 가까이 와도 채집 계속 (피격 시에만 TakeDamage 가 모드 전환).
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
            // 노출된 비전투 동료는 공포로 제자리에서 떨고만 있음. 새벽에 Follow 복귀.
            return Vector2.zero;
        }

        // 플레이어 진행 방향 캐시 — 정지 상태에서도 마지막 방향 유지.
        private static Vector2 _lastPlayerFacing = Vector2.down;

        private Vector2 GetFormationSlot()
        {
            if (Player == null) return transform.position;

            // 플레이어 velocity 로 facing 갱신
            var prb = Player.GetComponent<Rigidbody2D>();
            if (prb != null && prb.velocity.sqrMagnitude > 0.04f)
            {
                _lastPlayerFacing = prb.velocity.normalized;
            }

            Vector2 behind = -_lastPlayerFacing;
            Vector2 perp = new Vector2(-behind.y, behind.x); // 좌측 90°

            // 동료별 인덱스 (instanceID 정렬로 안정)
            var all = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int myId = GetInstanceID();
            int myIdx = 0;
            foreach (var c in all)
            {
                if (c != null && c != this && c.GetInstanceID() < myId) myIdx++;
            }

            // 일렬 종대로 뒤따름 — 한 줄에 2명씩, 점점 멀리.
            int row = myIdx / 2;          // 0,0,1,1,2,2,...
            int side = myIdx % 2;          // 0=좌(또는 단독), 1=우
            float behindDist = (row + 1) * FormationRadius * 0.9f;
            float sideOffset = 0f;
            if (myIdx > 0) sideOffset = (side == 0 ? -0.45f : 0.45f);
            return (Vector2)Player.position + behind * behindDist + perp * sideOffset;
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
            // 우선순위: 좀비 (밤) → 늑대 (낮의 위협). 사슴/토끼/맘모스 같은 평화 동물은 공격 안 함.
            MonoBehaviour target = FindNearestZombie(AttackRange);
            if (target == null) target = FindNearestHostileAnimal(AttackRange);
            if (target == null) return;
            SpawnProjectile(target);
            _attackCd = AttackCooldown * GetCampfireFireRateMul();
        }

        private WolfAi FindNearestHostileAnimal(float range)
        {
            var all = Object.FindObjectsByType<WolfAi>(FindObjectsSortMode.None);
            WolfAi best = null;
            float bestDist = range;
            foreach (var w in all)
            {
                if (w == null || w.CurrentHp <= 0) continue;
                float d = Vector2.Distance(transform.position, w.transform.position);
                if (d < bestDist) { best = w; bestDist = d; }
            }
            return best;
        }

        /// <summary>SightRange 안에서 좀비 우선, 없으면 늑대.</summary>
        private Transform FindAnyHostileInSight()
        {
            var z = FindNearestZombie(SightRange);
            if (z != null) return z.transform;
            var w = FindNearestHostileAnimal(SightRange);
            return w != null ? w.transform : null;
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

        private void SpawnProjectile(MonoBehaviour target)
        {
            var go = new GameObject("CompProjectile");
            go.transform.position = transform.position;
            go.transform.localScale = Vector3.one * 0.6f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            sr.color = new Color(0.55f, 1f, 0.7f);
            // Projectile.Awake 가 sprite 자체 생성 — ColorFallback 안 씀
            var proj = go.AddComponent<Projectile>();
            proj.Speed = ProjectileSpeed;
            proj.Damage = Damage;
            proj.HitRadius = 0.45f;
            proj.Aim(target, transform.position);
        }
    }
}
