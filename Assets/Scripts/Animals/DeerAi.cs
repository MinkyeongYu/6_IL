using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 사슴 AI: 플레이어가 일정 거리 안에 들어오면 도망. 그 외엔 정지.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DeerAi : MonoBehaviour
    {
        public float FleeRadius = 120f;
        public float FleeSpeed = 140f;

        private Rigidbody2D _rb;
        private Transform _player;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
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
