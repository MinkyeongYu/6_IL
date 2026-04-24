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

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<InputReader>();
            _balance = BalanceConfig.Instance;
            MaxHp = _balance.PlayerMaxHp;
            CurrentHp = MaxHp;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (IsDead) { _rb.velocity = Vector2.zero; return; }
            _rb.velocity = _input.MoveAxis * _balance.PlayerMoveSpeed;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
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
