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
                0 => new Color(0.95f, 0.95f, 1f),    // Longsword: 흰 칼날 (근접: 투사체 없음)
                1 => new Color(0.6f, 0.85f, 0.4f),   // Bow: 녹색 화살
                _ => Color.yellow,
            };
        }

        private static List<WeaponDefinition> Build()
        {
            return new List<WeaponDefinition>
            {
                // 근접: 느리지만 강타 (cd 0.7→1.0, dmg 16→28). 사거리 짧음.
                Make("longsword", "Longsword", dmg: 28, range: 1.8f, cd: 1.0f, projSpd: 0f, crit: 0.10f),
                // 원거리: 빠르지만 약함 (cd 1.1→0.55, dmg 10→6). 사거리 길고 투사체.
                Make("bow", "Bow", dmg: 6, range: 7.0f, cd: 0.55f, projSpd: 12f, crit: 0.12f),
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
