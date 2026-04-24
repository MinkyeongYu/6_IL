using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 주변 반경 내 좀비에게 지속 화염 대미지. 건물을 방어하는 핵심 요소.
    /// </summary>
    public sealed class CampfireAura : MonoBehaviour
    {
        public float Radius = 2.5f;
        public float DamagePerSecond = 6f;
        public float TickInterval = 0.5f;

        private float _tickAccum;

        private void Update()
        {
            _tickAccum += Time.deltaTime;
            if (_tickAccum < TickInterval) return;
            float dmgThisTick = DamagePerSecond * _tickAccum;
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmgThisTick));
            _tickAccum = 0f;

            var hits = Physics2D.OverlapCircleAll(transform.position, Radius);
            foreach (var h in hits)
            {
                var z = h.GetComponent<Zombie>();
                if (z != null && !z.IsDead) z.TakeDamage(rounded);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
