using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 도주형 동물 AI. 플레이어가 일정 거리 안에 들어오면 도망, 그 외엔 정지.
    /// HP 시스템: TakeDamage 로 누적 피해 — Hp 가 0 이하가 되면 Gatherable.OnGathered 호출.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DeerAi : MonoBehaviour
    {
        public float FleeRadius = 120f;
        public float FleeSpeed = 140f;
        public int MaxHp = 1;
        public int CurrentHp { get; private set; }

        private Rigidbody2D _rb;
        private Transform _player;
        private SpriteRenderer _sr;

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

        public void InitHp(int hp)
        {
            MaxHp = hp;
            CurrentHp = hp;
        }

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

        private void FixedUpdate()
        {
            if (_player == null) { _rb.velocity = Vector2.zero; return; }
            Vector2 toMe = (Vector2)(transform.position - _player.position);
            float d = toMe.magnitude;
            if (d < FleeRadius)
            {
                _rb.velocity = toMe.normalized * FleeSpeed;
            }
            else
            {
                _rb.velocity = Vector2.zero;
            }
        }
    }
}
