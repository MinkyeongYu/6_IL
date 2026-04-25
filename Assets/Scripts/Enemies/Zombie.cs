using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 단일 좀비. 가장 가까운 PlayerController/Building을 추격하고 사거리 안이면 공격.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Zombie : MonoBehaviour
    {
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        private Rigidbody2D _rb;
        private BalanceConfig _balance;
        private float _attackCooldown;
        private Transform _player;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _balance = BalanceConfig.Instance;
            MaxHp = _balance.ZombieMaxHp;
            CurrentHp = MaxHp;
        }

        /// <summary>외부 스포너가 보스/특수 좀비 HP 를 즉시 덮어씌울 때 사용.</summary>
        public void InitHp(int hp)
        {
            MaxHp = hp;
            CurrentHp = hp;
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void FixedUpdate()
        {
            if (IsDead) { _rb.velocity = Vector2.zero; return; }

            // 가장 가까운 타겟 (플레이어 우선; 향후 Building 포함)
            Transform target = FindNearestTarget();
            if (target == null) { _rb.velocity = Vector2.zero; return; }

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= _balance.ZombieAttackRange)
            {
                _rb.velocity = Vector2.zero;
                _attackCooldown -= Time.fixedDeltaTime;
                if (_attackCooldown <= 0f)
                {
                    var pc = target.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage(_balance.ZombieAttackDamage);
                    var b = target.GetComponent<Building>();
                    if (b != null) b.TakeDamage(_balance.ZombieAttackDamage);
                    _attackCooldown = _balance.ZombieAttackCooldownSec;
                }
            }
            else
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                _rb.velocity = dir * _balance.ZombieMoveSpeed;
                if (_attackCooldown > 0f) _attackCooldown -= Time.fixedDeltaTime;
            }
        }

        public float SightRange = 7f;

        private Transform FindNearestTarget()
        {
            // 1단계: 시야 안 (살아있고 보이는) 유닛 — Player + Companion
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
                if (c == null) continue;
                if (c.CurrentMode == Companion.Mode.Hiding) continue; // 숨은 동료는 안 보임
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr != null && !sr.enabled) continue;
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < bestDist) { best = c.transform; bestDist = d; }
            }

            if (best != null) return best;

            // 2단계: 시야 안에 살아있는 표적 없음 → 가까운 건물/바리게이트 공격
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

        public void TakeDamageByCompanion(int amount, Companion attacker)
        {
            // 동료가 때려도 동일 처리 (XP 는 PlayerProgression 으로 흐름)
            TakeDamage(amount);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp <= 0)
            {
                var prog = Object.FindFirstObjectByType<PlayerProgression>();
                if (prog != null) prog.GrantXp(1);
                Destroy(gameObject);
            }
        }
    }
}
