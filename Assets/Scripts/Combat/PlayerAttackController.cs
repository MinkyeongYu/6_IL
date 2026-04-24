using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 사거리 내 가장 가까운 좀비를 찾아 자동 공격. 무기 쿨다운 관리.
    /// </summary>
    public sealed class PlayerAttackController : MonoBehaviour
    {
        public WeaponDefinition Weapon;
        public uint RngSeed = 2026u;

        private float _cooldown;
        private SeededRng _rng;
        private Transform _self;

        private void Awake()
        {
            _self = transform;
            _rng = new SeededRng(RngSeed);
            if (Weapon == null)
            {
                Debug.LogWarning("[PlayerAttackController] Weapon not assigned. Will skip attacks.");
            }
        }

        private void Update()
        {
            if (Weapon == null) return;
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            // 사거리 내 좀비 탐색
            var hits = Physics2D.OverlapCircleAll(_self.position, Weapon.Range);
            Zombie nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var h in hits)
            {
                var z = h.GetComponent<Zombie>();
                if (z == null || z.IsDead) continue;
                float d = Vector2.Distance(_self.position, z.transform.position);
                if (d < nearestDist) { nearest = z; nearestDist = d; }
            }
            if (nearest == null) return;

            int dmg = DamageCalc.Compute(new DamageCalc.Input
            {
                Base = Weapon.BaseDamage,
                Armor = 0,
                CritRoll = _rng.Next(),
                CritChance = Weapon.CritChance,
                CritMult = Weapon.CritMultiplier,
            });
            nearest.TakeDamage(dmg);
            _cooldown = Weapon.CooldownSec;
        }

        public float CurrentCooldown => Mathf.Max(0f, _cooldown);
    }
}
