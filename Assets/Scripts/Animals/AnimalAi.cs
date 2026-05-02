using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 모든 야생 동물의 공통 베이스. HP/피격/회복/플레이어 캐싱 처리.
    /// 서브클래스는 DoBehavior() 만 구현 — 도주 (DeerAi) 또는 추격/공격 (WolfAi).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class AnimalAi : MonoBehaviour
    {
        public int MaxHp = 1;
        public int CurrentHp { get; protected set; }
        public bool IsDead => CurrentHp <= 0;

        public float RegenIntervalSec = 6f;

        protected Rigidbody2D _rb;
        protected SpriteRenderer _sr;
        protected Transform _player;
        private float _regenTimer;

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
            }
            _sr = GetComponent<SpriteRenderer>();
        }

        protected virtual void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
            if (CurrentHp <= 0) CurrentHp = MaxHp;
        }

        public void InitHp(int hp)
        {
            MaxHp = hp;
            CurrentHp = hp;
        }

        public virtual void TakeDamage(int amount)
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

        protected virtual void Update()
        {
            // HP 회복 — 살아있고 풀체력 아닐 때
            if (IsDead || CurrentHp >= MaxHp) return;
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= RegenIntervalSec)
            {
                _regenTimer = 0f;
                CurrentHp = Mathf.Min(MaxHp, CurrentHp + 1);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsDead) { if (_rb != null) _rb.velocity = Vector2.zero; return; }
            if (_player == null) { if (_rb != null) _rb.velocity = Vector2.zero; return; }
            DoBehavior();
        }

        /// <summary>서브클래스 별 행동 — _rb.velocity 세팅하면 됨.</summary>
        protected abstract void DoBehavior();
    }
}
