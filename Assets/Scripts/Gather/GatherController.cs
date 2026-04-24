using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 플레이어가 E를 누르면 가장 가까운 Gatherable을 시작.
    /// 진행 중에는 다른 채집 차단. 완료되면 자원 적립.
    /// </summary>
    public sealed class GatherController : MonoBehaviour
    {
        public float InteractRadius = 32f;
        public ResourceStore Store { get; set; }
        public PlayerController Player { get; set; }
        public InputReader Input { get; set; }

        private Gatherable _active;
        private float _progress;

        public bool IsActive => _active != null;
        public float Progress => _progress;
        public Gatherable ActiveTarget => _active;

        private void Update()
        {
            if (Store == null || Player == null || Input == null) return;

            if (Input.InteractPressed && _active == null)
            {
                TryStart();
            }

            if (_active != null)
            {
                _progress += Time.deltaTime / _active.DurationSec;
                if (_progress >= 1f)
                {
                    _active.OnGathered(Store);
                    _active = null;
                    _progress = 0f;
                }
            }
        }

        private void TryStart()
        {
            var hits = Physics2D.OverlapCircleAll(Player.transform.position, InteractRadius);
            Gatherable nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var h in hits)
            {
                var g = h.GetComponent<Gatherable>();
                if (g == null) continue;
                float d = Vector2.Distance(Player.transform.position, g.transform.position);
                if (d < nearestDist) { nearest = g; nearestDist = d; }
            }
            if (nearest != null)
            {
                _active = nearest;
                _progress = 0f;
            }
        }

        public void Cancel()
        {
            _active = null;
            _progress = 0f;
        }
    }
}
