using System;
using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    public enum RuneKind
    {
        // 레거시 (단순 능력치) — 풀에서는 제거됐지만 enum 은 보존 (구 세이브 호환)
        DamageUp,
        FireRateUp,
        HpUp,
        RangeUp,
        MoveSpeedUp,
        // 효과 기반 룬
        PoisonBlade,
        IceArrow,
        MultiShot,
        Detonator,
        LightningStrike,
        SummonDog,
        SummonHawk,
        Vampirism,    // 신규: 처치 시 HP 회복
        Thorns,       // 신규: 피격 시 인근 적에게 반사 대미지
        Pierce,       // 신규: 투사체 관통 (1회 추가 적중)
        AllyBoost,    // 신규: 동료 데미지 +%
        ResourceGift, // 신규: 즉시 자원 획득 (스택 안 됨, 매번 효과 발동)
    }

    /// <summary>
    /// XP/레벨링 + 룬 스택 (각 룬 최대 3 — 1=기본, 2=소폭, 3=마스터).
    /// 동일 계열(독/얼음/번개) 중 2개 이상 마스터하면 시너지 +50% 효과.
    /// </summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        public int Xp { get; private set; }
        public int Level { get; private set; } = 1;
        // 밤당 2렙 정도가 한계가 되도록 곡선 가팔라짐. Lv 1→2 = 12, 2→3 = 16, 3→4 = 20 ...
        public int XpToNext { get; private set; } = 12;
        public bool LevelUpPending { get; private set; }

        public const int MaxStacks = 3;
        private readonly Dictionary<RuneKind, int> _stacks = new();

        public int GetStacks(RuneKind k) => _stacks.TryGetValue(k, out var v) ? v : 0;
        public bool IsMastered(RuneKind k) => GetStacks(k) >= MaxStacks;

        public bool ElementalMasterSynergy
        {
            get
            {
                int n = 0;
                if (IsMastered(RuneKind.PoisonBlade)) n++;
                if (IsMastered(RuneKind.IceArrow)) n++;
                if (IsMastered(RuneKind.LightningStrike)) n++;
                return n >= 2;
            }
        }

        public float SynergyMul => ElementalMasterSynergy ? 1.5f : 1f;

        // 파생 modifiers (Recompute 가 _stacks 기반으로 계산)
        public float DamageMultiplier { get; private set; } = 1f;
        public float CooldownMultiplier { get; private set; } = 1f;
        public int BonusMaxHp { get; private set; }
        public float ProjectileSpeedMultiplier { get; private set; } = 1f;
        public float BonusRange { get; private set; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;

        // 특수 룬 효과 값 (스택별)
        public int PoisonDpsCalc => Scaled(RuneKind.PoisonBlade, 5, 7, 18);
        public float PoisonDurationCalc => GetStacks(RuneKind.PoisonBlade) switch { 0 => 0f, 1 => 3f, 2 => 4f, 3 => 6f, _ => 0f };
        public float IceSlowDurationCalc => GetStacks(RuneKind.IceArrow) switch { 0 => 0f, 1 => 2f, 2 => 3f, 3 => 5f, _ => 0f };
        public float IceSlowFactorCalc => GetStacks(RuneKind.IceArrow) >= 3 ? 0.2f : 0.5f; // 마스터 = 80% 슬로우
        public int MultiShotExtra => GetStacks(RuneKind.MultiShot) switch { 0 => 0, 1 => 1, 2 => 2, 3 => 4, _ => 0 };
        public int DetonateDmg => Scaled(RuneKind.Detonator, 8, 12, 25);
        public float DetonateRadius => GetStacks(RuneKind.Detonator) switch { 0 => 0f, 1 => 1.6f, 2 => 1.8f, 3 => 2.5f, _ => 0f };
        public int LightningJumps => GetStacks(RuneKind.LightningStrike);
        public float LightningChance => GetStacks(RuneKind.LightningStrike) switch { 0 => 0f, 1 => 0.5f, 2 => 0.7f, 3 => 1f, _ => 0f };
        public int LightningDmg => Scaled(RuneKind.LightningStrike, 6, 9, 20);
        public int VampirismHeal => GetStacks(RuneKind.Vampirism) switch { 0 => 0, 1 => 2, 2 => 4, 3 => 9, _ => 0 };
        public int ThornsDmg => GetStacks(RuneKind.Thorns) switch { 0 => 0, 1 => 5, 2 => 9, 3 => 20, _ => 0 };
        public float ThornsRadius => GetStacks(RuneKind.Thorns) switch { 0 => 0f, 1 => 1.5f, 2 => 1.8f, 3 => 2.5f, _ => 0f };
        public int PierceExtraHits => GetStacks(RuneKind.Pierce) switch { 0 => 0, 1 => 1, 2 => 2, 3 => 4, _ => 0 };
        public float AllyDamageMul => GetStacks(RuneKind.AllyBoost) switch { 0 => 1f, 1 => 1.25f, 2 => 1.5f, 3 => 2.0f, _ => 1f };

        private int Scaled(RuneKind kind, int s1, int s2, int s3)
        {
            int s = GetStacks(kind);
            int v = s switch { 0 => 0, 1 => s1, 2 => s2, 3 => s3, _ => 0 };
            // 시너지 보너스 (요소 룬 한정)
            if (s > 0 && SynergyMul > 1f && IsElemental(kind)) v = Mathf.RoundToInt(v * SynergyMul);
            return v;
        }

        private static bool IsElemental(RuneKind k) =>
            k == RuneKind.PoisonBlade || k == RuneKind.IceArrow || k == RuneKind.LightningStrike;

        public readonly List<RuneKind> Applied = new();
        public event Action<RuneKind> OnRuneApplied;

        public void GrantXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                XpToNext = 8 + Level * 4;
                LevelUpPending = true;
                // 레벨이 오를수록 낮/밤이 길어짐 (+25% per level, 시작 1.0)
                if (GameSession.Instance != null && GameSession.Instance.Cycle != null)
                    GameSession.Instance.Cycle.LevelDurationMul = 1f + (Level - 1) * 0.25f;
            }
        }

        public void ApplyRune(RuneKind kind)
        {
            if (GetStacks(kind) >= MaxStacks) return;
            _stacks[kind] = GetStacks(kind) + 1;
            Applied.Add(kind);

            // 즉시 효과 (펫/자원 보급)
            if (kind == RuneKind.SummonDog || kind == RuneKind.SummonHawk)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    int level = GetStacks(kind);
                    var petKind = kind == RuneKind.SummonDog ? Pet.Kind.Dog : Pet.Kind.Hawk;
                    Pet.Spawn(petKind, player.transform, level);
                }
            }
            else if (kind == RuneKind.ResourceGift)
            {
                int s = GetStacks(kind);
                int wood = s switch { 1 => 10, 2 => 20, 3 => 40, _ => 0 };
                int stone = s switch { 1 => 5, 2 => 10, 3 => 20, _ => 0 };
                int food = s switch { 1 => 5, 2 => 10, 3 => 20, _ => 0 };
                int frost = s == 3 ? 1 : 0;
                var session = GameSession.Instance;
                if (session != null)
                {
                    if (wood > 0) session.Resources.Add(ResourceKind.Wood, wood);
                    if (stone > 0) session.Resources.Add(ResourceKind.Stone, stone);
                    if (food > 0) session.Resources.Add(ResourceKind.Food, food);
                    if (frost > 0) session.Resources.Add(ResourceKind.Frostbloom, frost);
                }
            }

            Recompute();
            LevelUpPending = false;
            OnRuneApplied?.Invoke(kind);
        }

        private void Recompute()
        {
            DamageMultiplier = GetStacks(RuneKind.DamageUp) switch { 0 => 1f, 1 => 1.25f, 2 => 1.5f, 3 => 2.2f, _ => 1f };
            CooldownMultiplier = GetStacks(RuneKind.FireRateUp) switch { 0 => 1f, 1 => 0.85f, 2 => 0.7f, 3 => 0.5f, _ => 1f };
            BonusMaxHp = GetStacks(RuneKind.HpUp) switch { 0 => 0, 1 => 25, 2 => 50, 3 => 120, _ => 0 };
            BonusRange = GetStacks(RuneKind.RangeUp) switch { 0 => 0f, 1 => 1f, 2 => 2f, 3 => 5f, _ => 0f };
            MoveSpeedMultiplier = GetStacks(RuneKind.MoveSpeedUp) switch { 0 => 1f, 1 => 1.15f, 2 => 1.25f, 3 => 1.6f, _ => 1f };
        }

        public List<RuneKind> PickThreeOffer(uint seed)
        {
            var rng = new SeededRng(seed ^ (uint)Level);
            // 효과 기반 룬만 — 단순 능력치(DamageUp/FireRateUp/HpUp/RangeUp/MoveSpeedUp) 제외.
            var pool = new List<RuneKind>();
            void AddIfRoom(RuneKind k, int weight)
            {
                if (GetStacks(k) < MaxStacks)
                    for (int i = 0; i < weight; i++) pool.Add(k);
            }
            AddIfRoom(RuneKind.PoisonBlade, 2);
            AddIfRoom(RuneKind.IceArrow, 2);
            AddIfRoom(RuneKind.LightningStrike, 2);
            AddIfRoom(RuneKind.MultiShot, 2);
            AddIfRoom(RuneKind.Detonator, 2);
            AddIfRoom(RuneKind.SummonDog, 2);
            AddIfRoom(RuneKind.SummonHawk, 2);
            AddIfRoom(RuneKind.Vampirism, 2);
            AddIfRoom(RuneKind.Thorns, 2);
            AddIfRoom(RuneKind.Pierce, 2);
            AddIfRoom(RuneKind.AllyBoost, 2);
            AddIfRoom(RuneKind.ResourceGift, 2);

            var pick = new List<RuneKind>();
            var used = new HashSet<RuneKind>();
            int tries = 0;
            while (pick.Count < 3 && pool.Count > 0 && tries < 80)
            {
                tries++;
                int idx = rng.IntRange(0, pool.Count - 1);
                var k = pool[idx];
                pool.RemoveAt(idx);
                if (used.Add(k)) pick.Add(k);
            }
            return pick;
        }

        public static string Title(RuneKind k) => k switch
        {
            RuneKind.PoisonBlade => "☠ 독 무기",
            RuneKind.IceArrow => "❄ 얼음 화살",
            RuneKind.LightningStrike => "⚡ 번개 강타",
            RuneKind.MultiShot => "✦ 다중 사격",
            RuneKind.Detonator => "💥 폭발 처치",
            RuneKind.SummonDog => "🐕 사냥개 소환",
            RuneKind.SummonHawk => "🦅 매 소환",
            RuneKind.Vampirism => "🩸 흡혈",
            RuneKind.Thorns => "🌵 가시",
            RuneKind.Pierce => "🏹 관통",
            RuneKind.AllyBoost => "🤝 동료 강화",
            RuneKind.ResourceGift => "📦 보급 상자",
            RuneKind.DamageUp => "⚔ 대미지",
            RuneKind.FireRateUp => "⚡ 공격속도",
            RuneKind.HpUp => "❤ 체력",
            RuneKind.RangeUp => "↔ 사거리",
            RuneKind.MoveSpeedUp => "👣 이동속도",
            _ => k.ToString(),
        };

        // 다음 단계 효과 미리보기 (현재 stacks 기준)
        public string DescribeNext(RuneKind k)
        {
            int next = GetStacks(k) + 1;
            string suffix = next == 3 ? "  ★ MASTER" : (next == 2 ? "  (강화)" : "");
            return DescribeAt(k, next) + suffix;
        }

        public static string DescribeAt(RuneKind k, int level)
        {
            switch (k)
            {
                case RuneKind.DamageUp:
                    return level switch { 1 => "대미지 +25%", 2 => "대미지 +50%", 3 => "대미지 +120%", _ => "" };
                case RuneKind.FireRateUp:
                    return level switch { 1 => "공속 +18%", 2 => "공속 +43%", 3 => "공속 +100%", _ => "" };
                case RuneKind.HpUp:
                    return level switch { 1 => "최대HP +25", 2 => "최대HP +50", 3 => "최대HP +120", _ => "" };
                case RuneKind.RangeUp:
                    return level switch { 1 => "사거리 +1u", 2 => "사거리 +2u", 3 => "사거리 +5u", _ => "" };
                case RuneKind.MoveSpeedUp:
                    return level switch { 1 => "이속 +15%", 2 => "이속 +25%", 3 => "이속 +60%", _ => "" };
                case RuneKind.PoisonBlade:
                    return level switch
                    {
                        1 => "적중 시 독 (3초, 5 DPS)",
                        2 => "독 강화 (4초, 7 DPS)",
                        3 => "맹독 (6초, 18 DPS)",
                        _ => ""
                    };
                case RuneKind.IceArrow:
                    return level switch
                    {
                        1 => "적중 시 50% 슬로우 2초",
                        2 => "슬로우 3초",
                        3 => "빙결 80% 5초",
                        _ => ""
                    };
                case RuneKind.LightningStrike:
                    return level switch
                    {
                        1 => "50% 확률 1회 체인 (6 dmg)",
                        2 => "70% 확률 2회 체인 (9 dmg)",
                        3 => "100% 확률 3회 체인 (20 dmg)",
                        _ => ""
                    };
                case RuneKind.MultiShot:
                    return level switch { 1 => "투사체 +1발", 2 => "투사체 +2발", 3 => "투사체 +4발 부채꼴", _ => "" };
                case RuneKind.Detonator:
                    return level switch
                    {
                        1 => "처치 시 1.6u 폭발 8 dmg",
                        2 => "1.8u 폭발 12 dmg",
                        3 => "거대 2.5u 폭발 25 dmg",
                        _ => ""
                    };
                case RuneKind.SummonDog:
                    return level switch
                    {
                        1 => "사냥개 1마리 (근접 5 dmg)",
                        2 => "사냥개 2마리 (각 8 dmg)",
                        3 => "사냥개 3마리 정예 (각 11 dmg)",
                        _ => ""
                    };
                case RuneKind.SummonHawk:
                    return level switch
                    {
                        1 => "매 1마리 (원거리 4 dmg)",
                        2 => "매 2마리 (각 7 dmg)",
                        3 => "매 3마리 정예 (각 10 dmg)",
                        _ => ""
                    };
                case RuneKind.Vampirism:
                    return level switch
                    {
                        1 => "처치 시 +2 HP",
                        2 => "처치 시 +4 HP",
                        3 => "처치 시 +9 HP",
                        _ => ""
                    };
                case RuneKind.Thorns:
                    return level switch
                    {
                        1 => "피격 시 1.5u 내 적에 5 반사",
                        2 => "피격 시 1.8u 내 적에 9 반사",
                        3 => "피격 시 2.5u 내 적에 20 반사",
                        _ => ""
                    };
                case RuneKind.Pierce:
                    return level switch
                    {
                        1 => "투사체 1회 추가 관통",
                        2 => "투사체 2회 추가 관통",
                        3 => "투사체 4회 추가 관통",
                        _ => ""
                    };
                case RuneKind.AllyBoost:
                    return level switch
                    {
                        1 => "동료 대미지 +25%",
                        2 => "동료 대미지 +50%",
                        3 => "동료 대미지 +100%",
                        _ => ""
                    };
                case RuneKind.ResourceGift:
                    return level switch
                    {
                        1 => "즉시 +10 Wood / +5 Stone / +5 Food",
                        2 => "즉시 +20 Wood / +10 Stone / +10 Food",
                        3 => "즉시 +40 Wood / +20 Stone / +20 Food / +1 Frostbloom",
                        _ => ""
                    };
            }
            return "";
        }
    }
}
