using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 울타리 문. 콜라이더가 있어 좀비/동물은 막히지만, 플레이어 콜라이더와는 충돌 무시 (통과 가능).
    /// 자기 위치에 변경된 플레이어 콜라이더가 들어오는 경우(씬 재로드 등) 도 처리.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Door : MonoBehaviour
    {
        // 정적 레지스트리 — 동료 길 찾기에서 가장 가까운 문을 빠르게 찾기 위함.
        private static readonly System.Collections.Generic.List<Door> _all = new();
        public static System.Collections.Generic.IReadOnlyList<Door> All => _all;

        public static Door FindNearest(Vector2 from)
        {
            Door best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _all.Count; i++)
            {
                var d = _all[i];
                if (d == null) continue;
                float dist = Vector2.Distance(from, d.transform.position);
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
            return best;
        }

        private Collider2D _self;
        private Collider2D _playerCol;

        private void Awake()
        {
            _self = GetComponent<Collider2D>();
            _all.Add(this);
        }

        private void Start()
        {
            TryHookPlayer();
        }

        private void Update()
        {
            // 플레이어가 늦게 생성되거나 재할당되면 다시 시도
            if (_playerCol == null) TryHookPlayer();
        }

        private void TryHookPlayer()
        {
            var p = GameObject.FindWithTag("Player");
            if (p == null) return;
            var col = p.GetComponent<Collider2D>();
            if (col == null) return;
            _playerCol = col;
            Physics2D.IgnoreCollision(_self, _playerCol, true);
        }

        private void OnDestroy()
        {
            _all.Remove(this);
            // Physics2D.IgnoreCollision 은 대상 콜라이더가 사라지면 자동 해제됨 — 별도 정리 불필요
        }
    }
}
