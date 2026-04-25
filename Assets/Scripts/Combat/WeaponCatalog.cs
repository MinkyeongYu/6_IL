using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 6종 기본 무기 풀 (런타임 생성 ScriptableObject). 모두 ProjectileSpeed > 0
    /// 으로 통일해서 시각 피드백 일관성 유지. 근접류는 빠르고 짧게, 활/지팡이는
    /// 느리고 길게.
    /// </summary>
    public static class WeaponCatalog
    {
        private static List<WeaponDefinition> _all;
        public static IReadOnlyList<WeaponDefinition> All
        {
            get
            {
                if (_all == null) _all = Build();
                return _all;
            }
        }

        public static WeaponDefinition Get(int idx)
        {
            if (_all == null) _all = Build();
            return _all[((idx % _all.Count) + _all.Count) % _all.Count];
        }

        public static Color ProjectileColor(int idx)
        {
            return idx switch
            {
                0 => new Color(0.95f, 0.95f, 1f),    // Longsword: white
                1 => new Color(0.7f, 0.7f, 0.5f),    // Spear: tan
                2 => new Color(0.6f, 0.85f, 0.4f),   // Bow: green
                3 => new Color(0.5f, 0.85f, 1f),     // FrostStaff: cyan
                4 => new Color(1f, 0.5f, 0.2f),      // Warhammer: orange
                5 => new Color(0.95f, 0.4f, 0.85f),  // DualDaggers: pink
                _ => Color.yellow,
            };
        }

        private static List<WeaponDefinition> Build()
        {
            return new List<WeaponDefinition>
            {
                Make("longsword", "Longsword", dmg: 14, range: 2.0f, cd: 0.7f, projSpd: 12f, crit: 0.1f),
                Make("spear", "Spear", dmg: 12, range: 3.0f, cd: 0.9f, projSpd: 10f, crit: 0.08f),
                Make("bow", "Bow", dmg: 10, range: 7.0f, cd: 1.1f, projSpd: 9f, crit: 0.12f),
                Make("frost-staff", "Frost Staff", dmg: 8, range: 5.0f, cd: 1.5f, projSpd: 6f, crit: 0.05f),
                Make("warhammer", "Warhammer", dmg: 22, range: 2.0f, cd: 1.6f, projSpd: 11f, crit: 0.06f),
                Make("dual-daggers", "Dual Daggers", dmg: 6, range: 1.5f, cd: 0.35f, projSpd: 14f, crit: 0.18f),
            };
        }

        private static WeaponDefinition Make(string id, string name, int dmg, float range, float cd, float projSpd, float crit)
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.Id = id; w.DisplayName = name;
            w.BaseDamage = dmg; w.Range = range; w.CooldownSec = cd;
            w.CritChance = crit; w.CritMultiplier = 2f;
            w.HitRadius = 0.4f; w.ProjectileSpeed = projSpd;
            return w;
        }
    }
}
