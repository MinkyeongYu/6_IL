using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 의무실(Infirmary) 의 회복 오라. 반경 안 플레이어/동료에게 주기적으로 +HP.
    /// </summary>
    public sealed class HealingShrine : MonoBehaviour
    {
        public float Radius = 4.0f;
        public int HealAmount = 2;
        public float TickInterval = 3.0f;

        private float _accum;

        private void Update()
        {
            _accum += Time.deltaTime;
            if (_accum < TickInterval) return;
            _accum = 0f;

            // 플레이어
            var p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                var pc = p.GetComponent<PlayerController>();
                if (pc != null && !pc.IsDead
                    && Vector2.Distance(transform.position, p.transform.position) < Radius)
                {
                    pc.Heal(HealAmount);
                }
            }
            // 동료
            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in comps)
            {
                if (c == null || c.IsDead) continue;
                if (Vector2.Distance(transform.position, c.transform.position) >= Radius) continue;
                c.Heal(HealAmount);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.85f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
