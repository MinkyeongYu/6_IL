using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 망루. 야간(NightController.CurrentPhase == Phase.Night)에 시야 안 좀비를 자동 사격.
    /// 빌드 시 Building 컴포넌트와 함께 부착되어 HP·파괴 처리는 Building 이 담당.
    /// 사거리 안에 좀비가 있으면 매 FireInterval 마다 화살(Projectile) 발사.
    /// </summary>
    public sealed class Watchtower : MonoBehaviour
    {
        public float Range = 8f;
        public int Damage = 7;
        public float FireInterval = 1.2f;
        public float CompanionBoostRadius = 3.5f; // 망루 근처 동료 대미지 ×1.5
        public float CompanionDamageMul = 1.5f;

        private float _cd;
        private NightController _night;

        private void Start()
        {
            _night = Object.FindFirstObjectByType<NightController>();
        }

        private void Update()
        {
            _cd -= Time.deltaTime;
            // 낮에는 쉼 — 자원 절약 + 게임 흐름상 야간 방어용
            if (_night != null && _night.CurrentPhase != Phase.Night) return;
            if (_cd > 0f) return;

            var z = NearestZombie();
            if (z == null) return;
            FireAt(z);
            _cd = FireInterval;
        }

        private void FireAt(Zombie z)
        {
            var go = new GameObject("WatchtowerArrow");
            go.transform.position = transform.position + Vector3.up * 0.3f;
            go.transform.localScale = Vector3.one * 0.7f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            sr.color = new Color(1f, 0.95f, 0.55f);

            var p = go.AddComponent<Projectile>();
            p.Speed = 14f;
            p.Damage = Damage;
            p.HitRadius = 0.5f;
            p.Aim(z, transform.position);
        }

        private Zombie NearestZombie()
        {
            var all = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
            Zombie best = null;
            float bestDist = Range;
            foreach (var z in all)
            {
                if (z == null || z.IsDead) continue;
                float d = Vector2.Distance(transform.position, z.transform.position);
                if (d < bestDist) { best = z; bestDist = d; }
            }
            return best;
        }
    }
}
