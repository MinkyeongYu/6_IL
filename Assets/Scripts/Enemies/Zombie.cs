using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 단일 좀비. 가장 가까운 살아있는 유닛(Player/Companion) 추격, 시야 밖이면 건물 공격.
    /// 상태이상: 독(누적 DoT), 슬로우(이속 절반).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Zombie : MonoBehaviour
    {
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;
        public float SightRange = 7f;

        // 상태이상
        public float PoisonRemainingSec { get; private set; }
        public int PoisonDps { get; private set; }
        public float SlowRemainingSec { get; private set; }

        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private BalanceConfig _balance;
        private float _attackCooldown;
        private float _poisonTick;
        private Transform _player;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _balance = BalanceConfig.Instance;
            MaxHp = _balance.ZombieMaxHp;
            CurrentHp = MaxHp;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        public void InitHp(int hp)
        {
            MaxHp = hp;
            CurrentHp = hp;
        }

        public float MoveSpeedMul = 1f;
        public int VariantDamageBonus = 0;

        // 원거리 변종 (Archer) — 사거리 안에 들어오면 정지하고 투사체.
        public bool IsRanged = false;
        public float RangedRange = 7f;
        public int RangedDamage = 6;
        public float RangedCooldown = 1.5f;
        private float _rangedCd;

        public void ApplyPoison(float duration, int dps)
        {
            PoisonRemainingSec = Mathf.Max(PoisonRemainingSec, duration);
            PoisonDps = Mathf.Max(PoisonDps, dps);
        }

        public void ApplySlow(float duration)
        {
            SlowRemainingSec = Mathf.Max(SlowRemainingSec, duration);
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private static NightController _cachedNight;

        private void Update()
        {
            if (IsDead) return;

            // 낮이면 좀비 자체 정리 — Night 가 아닌 어떤 페이즈에도 좀비 없음
            if (_cachedNight == null) _cachedNight = Object.FindFirstObjectByType<NightController>();
            if (_cachedNight != null && _cachedNight.CurrentPhase != Phase.Night)
            {
                Destroy(gameObject);
                return;
            }

            if (PoisonRemainingSec > 0f)
            {
                PoisonRemainingSec -= Time.deltaTime;
                _poisonTick += Time.deltaTime;
                if (_poisonTick >= 0.5f)
                {
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(PoisonDps * 0.5f));
                    _poisonTick = 0f;
                    TakeDamage(dmg);
                }
                if (PoisonRemainingSec <= 0f) PoisonDps = 0;
            }
            if (SlowRemainingSec > 0f) SlowRemainingSec -= Time.deltaTime;

            // 색 표시: 슬로우=청록, 독=녹색, 둘 다=청록 우선
            if (_sr != null)
            {
                if (SlowRemainingSec > 0f) _sr.color = Color.Lerp(_baseColor, new Color(0.4f, 0.85f, 1f), 0.45f);
                else if (PoisonRemainingSec > 0f) _sr.color = Color.Lerp(_baseColor, new Color(0.3f, 0.95f, 0.3f), 0.45f);
                else _sr.color = _baseColor;
            }
        }

        private void FixedUpdate()
        {
            if (IsDead) { _rb.velocity = Vector2.zero; return; }

            Transform target = FindNearestTarget();
            if (target == null) { _rb.velocity = Vector2.zero; return; }

            // 경로 차단 체크 — 좀비와 타겟 사이에 Building 이 있으면 그 건물을 우선 부숨
            Transform pathTarget = ResolveBlockedTarget(target);

            float slowMul = SlowRemainingSec > 0f ? 0.5f : 1f;
            float dist = Vector2.Distance(transform.position, pathTarget.position);

            // 원거리 변종 — RangedRange 안에 타겟 들어오면 정지 + 사격
            if (IsRanged && dist <= RangedRange && pathTarget == target)
            {
                _rb.velocity = Vector2.zero;
                _rangedCd -= Time.fixedDeltaTime;
                if (_rangedCd <= 0f)
                {
                    SpawnRangedShot(pathTarget);
                    _rangedCd = RangedCooldown;
                }
                return;
            }
            if (IsRanged && _rangedCd > 0f) _rangedCd -= Time.fixedDeltaTime;

            if (dist <= _balance.ZombieAttackRange)
            {
                _rb.velocity = Vector2.zero;
                _attackCooldown -= Time.fixedDeltaTime;
                if (_attackCooldown <= 0f)
                {
                    int dmg = _balance.ZombieAttackDamage + VariantDamageBonus;
                    var pc = pathTarget.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage(dmg);
                    var c = pathTarget.GetComponent<Companion>();
                    if (c != null) c.TakeDamage(dmg);
                    var b = pathTarget.GetComponent<Building>();
                    if (b != null) b.TakeDamage(dmg);
                    _attackCooldown = _balance.ZombieAttackCooldownSec;
                }
            }
            else
            {
                Vector2 dir = ((Vector2)pathTarget.position - (Vector2)transform.position).normalized;
                _rb.velocity = dir * _balance.ZombieMoveSpeed * slowMul * MoveSpeedMul;
                if (_attackCooldown > 0f) _attackCooldown -= Time.fixedDeltaTime;
            }
        }

        private void SpawnRangedShot(Transform target)
        {
            var go = new GameObject("ZombieArrow");
            go.transform.position = transform.position;
            go.transform.localScale = Vector3.one * 0.6f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            sr.color = new Color(0.4f, 0.85f, 0.4f); // 녹색 — 좀비 화살
            var p = go.AddComponent<ZombieProjectile>();
            p.Speed = 9f;
            p.Damage = RangedDamage;
            p.HitRadius = 0.5f;
            p.Aim(target);
        }

        private static readonly RaycastHit2D[] _pathHits = new RaycastHit2D[8];

        /// <summary>
        /// 좀비 → 원래 타겟 사이에 Building 콜라이더가 있는지 검사. 있으면 그 건물 transform 반환.
        /// 없으면 원래 타겟 그대로 반환. 자기 자신 콜라이더는 무시.
        /// </summary>
        private Transform ResolveBlockedTarget(Transform original)
        {
            // 타겟이 이미 Building 이면 차단 검사 의미 없음
            if (original == null || original.GetComponent<Building>() != null) return original;

            Vector2 from = transform.position;
            Vector2 to = original.position;
            int count = Physics2D.LinecastNonAlloc(from, to, _pathHits);
            Building bestBlock = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = _pathHits[i];
                if (h.collider == null) continue;
                if (h.collider.gameObject == gameObject) continue;
                var b = h.collider.GetComponent<Building>();
                if (b == null) continue;
                float d = h.distance;
                if (d < bestDist) { bestDist = d; bestBlock = b; }
            }
            return bestBlock != null ? bestBlock.transform : original;
        }

        private Transform FindNearestTarget()
        {
            Transform best = null;
            float bestDist = SightRange;

            if (_player != null)
            {
                var pc = _player.GetComponent<PlayerController>();
                if (pc != null && !pc.IsDead)
                {
                    float d = Vector2.Distance(transform.position, _player.position);
                    if (d < bestDist) { best = _player; bestDist = d; }
                }
            }

            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in comps)
            {
                if (c == null || c.IsDead) continue;
                if (c.CurrentMode == Companion.Mode.Hiding) continue;
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr != null && !sr.enabled) continue;
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < bestDist) { best = c.transform; bestDist = d; }
            }

            if (best != null) return best;

            var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            bestDist = float.MaxValue;
            foreach (var b in buildings)
            {
                if (b == null) continue;
                float d = Vector2.Distance(transform.position, b.transform.position);
                if (d < bestDist) { best = b.transform; bestDist = d; }
            }
            return best;
        }

        private static float _lastHitSfxAt;

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            GameFeel.HitFlash(this, _sr);
            GameFeel.FloatText(transform.position, $"-{amount}",
                new Color(1f, 0.85f, 0.4f));
            // 사운드 rate-limit (50ms 간격)
            if (Time.unscaledTime - _lastHitSfxAt > 0.05f)
            {
                _lastHitSfxAt = Time.unscaledTime;
                Sfx.Hit();
            }
            if (CurrentHp <= 0)
            {
                var prog = Object.FindFirstObjectByType<PlayerProgression>();
                if (prog != null) prog.GrantXp(1);
                if (GameSession.Instance != null) GameSession.Instance.OnZombieKilled();
                Color poofColor = _sr != null ? _sr.color : new Color(0.6f, 0.2f, 0.22f);
                GameFeel.DeathPoof(transform.position, poofColor, 0.7f * transform.localScale.x);
                Sfx.Death();
                Destroy(gameObject);
            }
        }
    }
}
