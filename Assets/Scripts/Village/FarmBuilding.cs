using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 일정 주기로 GameSession.Resources 에 식량 추가. 자원 cap 으로 자동 제한.
    /// </summary>
    public sealed class FarmBuilding : MonoBehaviour
    {
        public float IntervalSec = 12f;
        public int Yield = 1;

        private float _t;

        private void Update()
        {
            _t += Time.deltaTime;
            if (_t < IntervalSec) return;
            _t = 0f;
            var session = GameSession.Instance;
            if (session != null) session.Resources.Add(ResourceKind.Food, Yield);
        }
    }
}
