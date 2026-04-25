using System;
using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    public enum RuneKind
    {
        DamageUp,        // +25% 대미지
        FireRateUp,      // -15% 쿨다운
        HpUp,            // +25 최대 HP (즉시 회복)
        ProjectileSpeedUp, // +30% 투사체 속도
        RangeUp,         // +1u 사거리
        MoveSpeedUp,     // +15% 이동속도
    }

    /// <summary>
    /// 플레이어 XP/레벨링. 좀비 처치 시 GrantXp 호출. 레벨업되면 LevelUpPending=true 로
    /// UI 가 룬 선택 모달 띄우고 ApplyRune 호출. 적용된 룬은 PlayerAttackController /
    /// PlayerController / DebugMove 에서 읽어서 계산 수정.
    /// </summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        public int Xp { get; private set; }
        public int Level { get; private set; } = 1;
        public int XpToNext { get; private set; } = 5;
        public bool LevelUpPending { get; private set; }

        public float DamageMultiplier { get; private set; } = 1f;
        public float CooldownMultiplier { get; private set; } = 1f;
        public int BonusMaxHp { get; private set; }
        public float ProjectileSpeedMultiplier { get; private set; } = 1f;
        public float BonusRange { get; private set; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;

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
                XpToNext = 5 + Level * 3;
                LevelUpPending = true;
            }
        }

        public void ApplyRune(RuneKind kind)
        {
            Applied.Add(kind);
            switch (kind)
            {
                case RuneKind.DamageUp: DamageMultiplier *= 1.25f; break;
                case RuneKind.FireRateUp: CooldownMultiplier *= 0.85f; break;
                case RuneKind.HpUp: BonusMaxHp += 25; break;
                case RuneKind.ProjectileSpeedUp: ProjectileSpeedMultiplier *= 1.3f; break;
                case RuneKind.RangeUp: BonusRange += 1f; break;
                case RuneKind.MoveSpeedUp: MoveSpeedMultiplier *= 1.15f; break;
            }
            LevelUpPending = false;
            OnRuneApplied?.Invoke(kind);
        }

        public List<RuneKind> PickThreeOffer(uint seed)
        {
            var rng = new SeededRng(seed ^ (uint)Level);
            var all = new List<RuneKind>
            {
                RuneKind.DamageUp, RuneKind.FireRateUp, RuneKind.HpUp,
                RuneKind.ProjectileSpeedUp, RuneKind.RangeUp, RuneKind.MoveSpeedUp,
            };
            var pick = new List<RuneKind>();
            for (int i = 0; i < 3 && all.Count > 0; i++)
            {
                int idx = rng.IntRange(0, all.Count - 1);
                pick.Add(all[idx]);
                all.RemoveAt(idx);
            }
            return pick;
        }

        public static string Describe(RuneKind k) => k switch
        {
            RuneKind.DamageUp => "대미지 +25%",
            RuneKind.FireRateUp => "공격속도 +18% (쿨다운 -15%)",
            RuneKind.HpUp => "최대 HP +25",
            RuneKind.ProjectileSpeedUp => "투사체 속도 +30%",
            RuneKind.RangeUp => "사거리 +1u",
            RuneKind.MoveSpeedUp => "이동속도 +15%",
            _ => k.ToString(),
        };
    }
}
