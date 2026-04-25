using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 늑대 — 포식자 AI. 시야 안의 플레이어/동료를 추격해 근접 공격. 항상 공격적.
    /// 죽으면 Gatherable 로 고기 1 정도 떨어뜨림 (보상은 적지만 위협적).
    /// 보통 무리 단위로 같이 스폰됨 — ProceduralSpawner 가 처리.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WolfAi : MonoBehaviour
    {
        public float SightRange = 9f;
        public float AttackRange = 1.2f;
        public float MoveSpeed = 4.0f;
        public int Damage = 6;
        public float AttackCooldown = 1.0f;

        public int MaxHp = 5;
        public int CurrentHp { get; private set; }

        private Rigidbody2D _rb;
        private Transform _player;
        private SpriteRenderer _sr;
        private float _attackCd;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
            if (CurrentHp <= 0) CurrentHp = MaxHp;
        }

        public void InitHp(int hp) { MaxHp = hp; CurrentHp = hp; }

        public void TakeDamage(int amount)
        {
            if (CurrentHp <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            GameFeel.HitFlash(this, _sr);
            GameFeel.FloatText(transform.position, $"-{amount}", new Color(1f, 0.85f, 0.4f));
            if (CurrentHp <= 0)
            {
                var g = GetComponent<Gatherable>();
                var session = GameSession.Instance;
                if (g != null && session != null) g.OnGathered(session.Resources);
                else Destroy(gameObject);
            }
        }

        private Transform FindTarget()
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
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < bestDist) { best = c.transform; bestDist = d; }
            }
            return best;
        }

        private float _regenTimer;
        public float RegenIntervalSec = 6f;

        private void Update()
        {
            if (CurrentHp <= 0 || CurrentHp >= MaxHp) return;
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= RegenIntervalSec)
            {
                _regenTimer = 0f;
                CurrentHp = Mathf.Min(MaxHp, CurrentHp + 1);
            }
        }

        private void FixedUpdate()
        {
            if (CurrentHp <= 0) { _rb.velocity = Vector2.zero; return; }
            var target = FindTarget();
            if (target == null) { _rb.velocity = Vector2.zero; return; }

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= AttackRange)
            {
                _rb.velocity = Vector2.zero;
                _attackCd -= Time.fixedDeltaTime;
                if (_attackCd <= 0f)
                {
                    var pc = target.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage(Damage);
                    var c = target.GetComponent<Companion>();
                    if (c != null) c.TakeDamage(Damage);
                    _attackCd = AttackCooldown;
                }
            }
            else
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                _rb.velocity = dir * MoveSpeed;
                if (_attackCd > 0f) _attackCd -= Time.fixedDeltaTime;
            }
        }
    }
}
