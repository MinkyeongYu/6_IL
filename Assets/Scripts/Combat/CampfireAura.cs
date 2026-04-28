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

        // 연료 — 100 단위, 초당 1 씩 감소 (약 100초 지속). 0 되면 꺼짐.
        public float MaxFuel = 100f;
        public float Fuel = 100f;
        public float BurnRatePerSec = 1f;
        public bool IsActive => Fuel > 0f;

        private float _tickAccum;
        private SpriteRenderer _sr;
        private Color _baseColor;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        public void AddFuel(float amount)
        {
            Fuel = Mathf.Clamp(Fuel + amount, 0f, MaxFuel);
        }

        private void Update()
        {
            // 연료 소진
            if (Fuel > 0f) Fuel = Mathf.Max(0f, Fuel - BurnRatePerSec * Time.deltaTime);

            // 시각: 꺼지면 어두워짐
            if (_sr != null)
            {
                float k = IsActive ? Mathf.Lerp(0.5f, 1f, Fuel / MaxFuel) : 0.35f;
                var c = _baseColor; c.r *= k; c.g *= k; c.b *= k; _sr.color = c;
            }

            if (!IsActive) return;

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

        /// <summary>마을 안에 활성 모닥불(연료 > 0) 이 1개라도 있나?</summary>
        public static bool VillageHasHeat(Vector2 villageCenter, float halfSize)
        {
            var auras = Object.FindObjectsByType<CampfireAura>(FindObjectsSortMode.None);
            foreach (var a in auras)
            {
                if (a == null || !a.IsActive) continue;
                Vector2 d = (Vector2)a.transform.position - villageCenter;
                if (Mathf.Abs(d.x) < halfSize && Mathf.Abs(d.y) < halfSize) return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
