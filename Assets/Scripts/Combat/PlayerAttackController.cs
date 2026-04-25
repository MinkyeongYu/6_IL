using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 사거리 내 가장 가까운 공격 대상(좀비/사슴)을 자동 공격.
    /// Weapon.ProjectileSpeed > 0 이면 투사체 발사, 아니면 즉시 대미지.
    /// Weapon 미할당 시 기본 Magic Bolt 런타임 생성.
    /// </summary>
    public sealed class PlayerAttackController : MonoBehaviour
    {
        public WeaponDefinition Weapon;
        public uint RngSeed = 2026u;
        public PlayerProgression Progression;

        public int WeaponIndex { get; private set; } = -1;
        public Color CurrentProjectileColor { get; private set; } = new Color(1f, 0.92f, 0.3f);

        public void SwitchToWeapon(int idx)
        {
            WeaponIndex = idx;
            Weapon = WeaponCatalog.Get(idx);
            CurrentProjectileColor = WeaponCatalog.ProjectileColor(WeaponIndex);
        }

        public void CycleWeapon(int delta)
        {
            int n = WeaponCatalog.All.Count;
            int next = WeaponIndex < 0 ? 0 : ((WeaponIndex + delta) % n + n) % n;
            SwitchToWeapon(next);
        }

        private float _cooldown;
        private SeededRng _rng;
        private Transform _self;

        private void Awake()
        {
            _self = transform;
            _rng = new SeededRng(RngSeed);
            if (Progression == null) Progression = GetComponent<PlayerProgression>();
            if (Weapon == null)
            {
                SwitchToWeapon(0); // Longsword 기본
            }
        }

        private void Update()
        {
            if (Weapon == null) return;
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            float effectiveRange = Weapon.Range + (Progression != null ? Progression.BonusRange : 0f);
            MonoBehaviour nearest = FindNearestTarget(effectiveRange);
            if (nearest == null) return;

            float dmgMul = Progression != null ? Progression.DamageMultiplier : 1f;
            int baseDmg = Mathf.RoundToInt(Weapon.BaseDamage * dmgMul);
            int dmg = DamageCalc.Compute(new DamageCalc.Input
            {
                Base = baseDmg,
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
                DealImmediate(nearest, dmg);
            }
            float cdMul = Progression != null ? Progression.CooldownMultiplier : 1f;
            _cooldown = Weapon.CooldownSec * cdMul;
        }

        private MonoBehaviour FindNearestTarget(float range)
        {
            var hits = Physics2D.OverlapCircleAll(_self.position, range);
            MonoBehaviour best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                MonoBehaviour candidate = null;
                var z = h.GetComponent<Zombie>();
                if (z != null && !z.IsDead)
                {
                    candidate = z;
                }
                else
                {
                    var d = h.GetComponent<DeerAi>();
                    if (d != null) candidate = d;
                }
                if (candidate == null) continue;
                float dist = Vector2.Distance(_self.position, candidate.transform.position);
                if (dist < bestDist) { best = candidate; bestDist = dist; }
            }
            return best;
        }

        private void SpawnProjectile(MonoBehaviour target, int dmg)
        {
            var go = new GameObject("Projectile");
            go.transform.position = _self.position;
            go.transform.localScale = Vector3.one * 0.35f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = CurrentProjectileColor;
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.2f, 0.2f, 0.2f, 1f);

            float speedMul = Progression != null ? Progression.ProjectileSpeedMultiplier : 1f;
            var proj = go.AddComponent<Projectile>();
            proj.Speed = Weapon.ProjectileSpeed * speedMul;
            proj.Damage = dmg;
            proj.HitRadius = 0.4f;
            proj.Aim(target, _self.position);
        }

        private void DealImmediate(MonoBehaviour target, int dmg)
        {
            if (target is Zombie z && !z.IsDead) z.TakeDamage(dmg);
            else if (target is DeerAi deer)
            {
                var g = deer.GetComponent<Gatherable>();
                if (g != null && GameSession.Instance != null) g.OnGathered(GameSession.Instance.Resources);
            }
        }

        public float CurrentCooldown => Mathf.Max(0f, _cooldown);
    }
}
