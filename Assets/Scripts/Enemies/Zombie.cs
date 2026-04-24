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

        private Transform FindNearestTarget()
        {
            Transform best = _player;
            float bestDist = best == null ? float.MaxValue : Vector2.Distance(transform.position, best.position);

            // 가까운 건물도 후보
            var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in buildings)
            {
                float d = Vector2.Distance(transform.position, b.transform.position);
                if (d < bestDist) { best = b.transform; bestDist = d; }
            }
            return best;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp <= 0) Destroy(gameObject);
        }
    }
}
