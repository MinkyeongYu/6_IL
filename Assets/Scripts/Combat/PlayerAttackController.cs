using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 사거리 내 가장 가까운 좀비를 찾아 자동 공격. ProjectileSpeed > 0 이면 투사체 스폰.
    /// Weapon 이 null 이면 런타임 기본값(Unity 유닛 스케일) 생성.
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
                Weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                Weapon.Id = "magic-bolt";
                Weapon.DisplayName = "Magic Bolt";
                Weapon.BaseDamage = 12;
                Weapon.Range = 6f;
                Weapon.CooldownSec = 1.1f;
                Weapon.CritChance = 0.12f;
                Weapon.CritMultiplier = 2f;
                Weapon.HitRadius = 0.4f;
                Weapon.ProjectileSpeed = 8f;
            }
        }

        private void Update()
        {
            if (Weapon == null) return;
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

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

            if (Weapon.ProjectileSpeed > 0f)
            {
                SpawnProjectile(nearest, dmg);
            }
            else
            {
                nearest.TakeDamage(dmg);
            }
            _cooldown = Weapon.CooldownSec;
        }

        private void SpawnProjectile(Zombie target, int dmg)
        {
            var go = new GameObject("Projectile");
            go.transform.position = _self.position;
            go.transform.localScale = Vector3.one * 0.35f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;
            sr.color = new Color(1f, 0.92f, 0.3f);

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 0.92f, 0.3f);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.8f, 0.5f, 0.1f, 1f);

            var proj = go.AddComponent<Projectile>();
            proj.Speed = Weapon.ProjectileSpeed;
            proj.Damage = dmg;
            proj.HitRadius = 0.4f;
            proj.AimAt(target);
        }

        public float CurrentCooldown => Mathf.Max(0f, _cooldown);
    }
}
