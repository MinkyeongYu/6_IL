using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 플레이어에 붙어서 가장 가까운 좀비 방향으로 회전하는 작은 스프라이트 (무기).
    /// 공격 쿨다운 중이면 살짝 축소(펄스).
    /// </summary>
    public sealed class WeaponVisual : MonoBehaviour
    {
        public Color BladeColor = new Color(0.85f, 0.9f, 0.95f);
        public Color HiltColor = new Color(0.4f, 0.3f, 0.2f);
        public float HandleDistance = 0.5f;
        public Vector2 BladeSize = new Vector2(0.7f, 0.18f);

        private PlayerAttackController _attacker;
        private Transform _blade;

        private void Awake()
        {
            var pivot = new GameObject("WeaponPivot");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = Vector3.zero;
            _blade = pivot.transform;

            var blade = new GameObject("Blade");
            blade.transform.SetParent(_blade, false);
            blade.transform.localPosition = new Vector3(HandleDistance, 0f, 0f);
            blade.transform.localScale = new Vector3(BladeSize.x, BladeSize.y, 1f);
            var bSr = blade.AddComponent<SpriteRenderer>();
            bSr.sortingOrder = 11;
            bSr.color = BladeColor;
            var bCf = blade.AddComponent<ColorFallback>();
            bCf.Tint = BladeColor;
            bCf.Shape = FallbackShape.Rounded;
            bCf.Circle = false;
            bCf.PixelSize = 32;
            bCf.OutlineWidth = 2;
            bCf.OutlineColor = new Color(0.1f, 0.1f, 0.15f, 1f);

            var hilt = new GameObject("Hilt");
            hilt.transform.SetParent(_blade, false);
            hilt.transform.localPosition = new Vector3(HandleDistance - BladeSize.x * 0.55f, 0f, 0f);
            hilt.transform.localScale = new Vector3(0.15f, 0.3f, 1f);
            var hSr = hilt.AddComponent<SpriteRenderer>();
            hSr.sortingOrder = 12;
            hSr.color = HiltColor;
            var hCf = hilt.AddComponent<ColorFallback>();
            hCf.Tint = HiltColor;
            hCf.Shape = FallbackShape.Square;
            hCf.Circle = false;
            hCf.PixelSize = 16;
            hCf.OutlineWidth = 1;
            hCf.OutlineColor = new Color(0.1f, 0.1f, 0.1f, 1f);

            _attacker = GetComponent<PlayerAttackController>();
        }

        private void Update()
        {
            var target = FindNearestZombie();
            if (target != null)
            {
                Vector2 dir = (Vector2)(target.position - transform.position);
                if (dir.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    _blade.localRotation = Quaternion.Slerp(
                        _blade.localRotation,
                        Quaternion.Euler(0f, 0f, angle),
                        Time.deltaTime * 12f);
                }
            }

            // 공격 직후 살짝 펄스 (쿨다운 진행)
            if (_attacker != null && _attacker.Weapon != null)
            {
                float cd = _attacker.CurrentCooldown;
                float freshness = Mathf.Clamp01(cd / Mathf.Max(0.01f, _attacker.Weapon.CooldownSec));
                float scale = 1f + freshness * 0.3f;
                _blade.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private Transform FindNearestZombie()
        {
            var zombies = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (var z in zombies)
            {
                if (z == null || z.IsDead) continue;
                float d = Vector2.Distance(transform.position, z.transform.position);
                if (d < bestDist) { best = z.transform; bestDist = d; }
            }
            return best;
        }
    }
}
