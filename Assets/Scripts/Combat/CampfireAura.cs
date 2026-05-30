using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 주변 반경 내 좀비에게 지속 화염 대미지. 건물을 방어하는 핵심 요소.
    /// 추가: 시야 반경(VisionMask 에 노출), 황금빛 글로우 이펙트, 밤 시간 HP 자동 소모.
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

        // ── 시야 반경 (VisionMask 에서 밝은 원으로 표시) ──────────────────
        /// <summary>기본 시야 반경 (유닛). HP에 따라 동적으로 축소됨.</summary>
        public float BaseVisionRadius = 3.0f;
        /// <summary>현재 HP 비율에 따라 보정된 실시간 시야 반경.</summary>
        public float VisionRadius { get; private set; }

        private float _tickAccum;
        private float _drainAccum;
        private SpriteRenderer _sr;
        private Color _baseColor;

        // ── 글로우 이펙트 ────────────────────────────────────────────────────
        private SpriteRenderer _glowSr;
        private float _glowPhase;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
            VisionRadius = BaseVisionRadius;
        }

        private void Start()
        {
            BuildGlow();
        }

        /// <summary>황금빛 반투명 원형 글로우 생성. 스프라이트 없으면 ColorFallback 으로 폴백.</summary>
        private void BuildGlow()
        {
            var go = new GameObject("CampfireGlow");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(4.0f, 4.0f, 1f);

            _glowSr = go.AddComponent<SpriteRenderer>();
            _glowSr.sortingOrder = 1; // 바닥, 스프라이트 아래

            // SpriteBank 시도
            Sprite glowSprite = SpriteBank.Load("Effects/41_warm_glow");
            if (glowSprite != null)
            {
                _glowSr.sprite = glowSprite;
            }
            else
            {
                // ColorFallback 원형 폴백
                var cf = go.AddComponent<ColorFallback>();
                cf.Tint = new Color(1f, 0.72f, 0.2f, 0.35f);
                cf.Shape = FallbackShape.Circle;
                cf.Circle = true;
                cf.PixelSize = 64;
            }
            _glowSr.color = new Color(1f, 0.72f, 0.2f, 0.35f);
        }

        public void AddFuel(float amount)
        {
            Fuel = Mathf.Clamp(Fuel + amount, 0f, MaxFuel);
        }

        private void Update()
        {
            // ── 연료 소진 ──────────────────────────────────────────────────
            if (Fuel > 0f) Fuel = Mathf.Max(0f, Fuel - BurnRatePerSec * Time.deltaTime);

            // ── 시각: 꺼지면 어두워짐 ──────────────────────────────────────
            if (_sr != null)
            {
                float k = IsActive ? Mathf.Lerp(0.5f, 1f, Fuel / MaxFuel) : 0.35f;
                var c = _baseColor; c.r *= k; c.g *= k; c.b *= k; _sr.color = c;
            }

            // ── 밤 HP Drain ────────────────────────────────────────────────
            bool isNight = false;
            var session = GameSession.Instance;
            if (session != null && session.Cycle != null)
                isNight = session.Cycle.Phase == Phase.Night;

            var building = GetComponent<Building>();
            if (isNight && building != null && building.CurrentHp > 0)
            {
                float drainPerSec = BalanceConfig.Instance.CampfireHpDrainPerSec;
                _drainAccum += Time.deltaTime;
                // 1초마다 정수 단위로 TakeDamage 호출 (스파이크 방지)
                if (_drainAccum >= 1f)
                {
                    int drain = Mathf.Max(1, Mathf.RoundToInt(drainPerSec * _drainAccum));
                    _drainAccum = 0f;
                    building.TakeDamage(drain);
                }
            }
            else
            {
                _drainAccum = 0f;
            }

            // ── VisionRadius: HP 비율에 따라 축소 ─────────────────────────
            if (building != null && building.MaxHp > 0)
            {
                float hpRatio = (float)building.CurrentHp / building.MaxHp;
                VisionRadius = BaseVisionRadius * (0.3f + 0.7f * hpRatio);
            }
            else
            {
                VisionRadius = BaseVisionRadius;
            }

            // ── 글로우 깜빡임 ──────────────────────────────────────────────
            if (_glowSr != null)
            {
                _glowPhase += Time.deltaTime * 1.8f;
                float flicker = 0.35f + 0.08f * Mathf.Sin(_glowPhase * 2.3f)
                                      + 0.04f * Mathf.Sin(_glowPhase * 5.7f);
                float active = IsActive ? 1f : 0.2f;
                _glowSr.color = new Color(1f, 0.72f, 0.2f, flicker * active);
            }

            if (!IsActive) return;

            // ── 좀비 대미지 틱 ─────────────────────────────────────────────
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
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, VisionRadius > 0 ? VisionRadius : BaseVisionRadius);
        }
    }
}
