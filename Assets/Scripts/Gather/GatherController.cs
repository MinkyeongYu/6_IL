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

        public float CompanionAssistRange = 4.0f;
        public int CompanionAssistMax = 2;
        private float _assistRecheck;

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
                // 유휴 동료 자동 합류 — 0.5초마다 탐색
                _assistRecheck -= Time.deltaTime;
                if (_assistRecheck <= 0f)
                {
                    _assistRecheck = 0.5f;
                    AssignNearbyHelpers();
                }
                if (_progress >= 1f)
                {
                    _active.OnGathered(Store);
                    _active = null;
                    _progress = 0f;
                }
            }
        }

        private void AssignNearbyHelpers()
        {
            if (_active == null) return;
            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            // Follow 모드 + 유휴 동료만 모음
            var candidates = new System.Collections.Generic.List<Companion>();
            int alreadyOnTask = 0;
            foreach (var c in comps)
            {
                if (c == null || c.IsDead) continue;
                if (c.CurrentMode == Companion.Mode.Working && c.Target == _active) { alreadyOnTask++; continue; }
                if (c.CurrentMode != Companion.Mode.Follow) continue;
                if (Vector2.Distance(c.transform.position, _active.transform.position) > CompanionAssistRange) continue;
                candidates.Add(c);
            }
            int slotsLeft = CompanionAssistMax - alreadyOnTask;
            if (slotsLeft <= 0) return;
            // 가까운 순 정렬
            candidates.Sort((a, b) =>
                Vector2.Distance(a.transform.position, _active.transform.position)
                .CompareTo(Vector2.Distance(b.transform.position, _active.transform.position)));
            int n = Mathf.Min(slotsLeft, candidates.Count);
            for (int i = 0; i < n; i++) candidates[i].AssignGather(_active);
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

        /// <summary>외부 (HUD 버튼 등) 에서 특정 Gatherable 채집 시작 강제.
        /// 다른 채집이 진행 중이어도 자동 취소하고 새 노드로 전환.</summary>
        public bool StartGathering(Gatherable g)
        {
            if (g == null) return false;
            _active = g;
            _progress = 0f;
            return true;
        }
    }
}
