using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 플레이어 이동 + HP 관리. Rigidbody2D 사용.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(InputReader))]
    public sealed class PlayerController : MonoBehaviour
    {
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        private Rigidbody2D _rb;
        private InputReader _input;
        private BalanceConfig _balance;
        private PlayerProgression _progression;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<InputReader>();
            _balance = BalanceConfig.Instance;
            _progression = GetComponent<PlayerProgression>();
            MaxHp = _balance.PlayerMaxHp;
            CurrentHp = MaxHp;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.freezeRotation = true;

            if (_progression != null)
            {
                _progression.OnRuneApplied += OnRuneApplied;
            }
        }

        private void OnDestroy()
        {
            if (_progression != null) _progression.OnRuneApplied -= OnRuneApplied;
        }

        private void OnRuneApplied(RuneKind kind)
        {
            if (kind == RuneKind.HpUp)
            {
                MaxHp = _balance.PlayerMaxHp + _progression.BonusMaxHp;
                CurrentHp = Mathf.Min(MaxHp, CurrentHp + 25);
            }
        }

        private float _snowTimer;

        private void FixedUpdate()
        {
            if (IsDead) { _rb.velocity = Vector2.zero; return; }
            _rb.velocity = _input.MoveAxis * _balance.PlayerMoveSpeed;

            if (_rb.velocity.sqrMagnitude > 0.05f)
            {
                _snowTimer -= Time.fixedDeltaTime;
                if (_snowTimer <= 0f)
                {
                    _snowTimer = 0.16f;
                    GameFeel.SnowPuff(transform.position + Vector3.down * 0.2f);
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            GameFeel.HitFlash(this, GetComponent<SpriteRenderer>());
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        public void ResetState(Vector2 spawnPosition)
        {
            CurrentHp = MaxHp;
            transform.position = spawnPosition;
            _rb.velocity = Vector2.zero;
        }
    }
}
