using UnityEngine;

namespace IL6
{
    /// <summary>
    /// BalanceConfig/InputReader/PlayerController 체인 없이 직접 WASD → Rigidbody2D.velocity 적용.
    /// 입력 진단 + 일관된 5 u/s 이동 보장.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DebugMove : MonoBehaviour
    {
        public float Speed = 5f;
        public bool LogInput = false;
        public PlayerProgression Progression;

        public Vector2 LastInput { get; private set; }
        public float CurrentSpeed => LastInput.magnitude * Speed;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            if (Progression == null) Progression = GetComponent<PlayerProgression>();
        }

        private void Update()
        {
            float x = 0, y = 0;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1;
            LastInput = new Vector2(x, y).normalized;
            if (LogInput && LastInput.magnitude > 0.01f && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[DebugMove] input=({x},{y}) normalized={LastInput}");
            }
        }

        private void FixedUpdate()
        {
            float mul = Progression != null ? Progression.MoveSpeedMultiplier : 1f;
            _rb.velocity = LastInput * Speed * mul;
        }
    }
}
