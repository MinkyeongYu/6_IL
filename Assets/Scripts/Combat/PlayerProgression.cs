using System;
using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    public enum RuneKind
    {
        DamageUp,           // +25% 대미지
        FireRateUp,         // -15% 쿨다운
        HpUp,               // +25 최대 HP (즉시 회복)
        RangeUp,            // +1u 사거리
        MoveSpeedUp,        // +15% 이동속도
        // 특수 효과 룬
        PoisonBlade,        // 처치/타격 시 독 DoT 누적 (스택당 +5 DPS, 3초)
        IceArrow,           // 타격 시 적 이속 50% 슬로우 (스택당 +1초)
        MultiShot,          // 투사체 +1 / 멜리 다중 타격 +1
        Detonator,          // 처치 시 주변 폭발 (스택당 +8 대미지, 1.6u)
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

        public int PoisonStacks { get; private set; }
        public int IceStacks { get; private set; }
        public int MultiShotStacks { get; private set; }
        public int DetonatorStacks { get; private set; }

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
                case RuneKind.RangeUp: BonusRange += 1f; break;
                case RuneKind.MoveSpeedUp: MoveSpeedMultiplier *= 1.15f; break;
                case RuneKind.PoisonBlade: PoisonStacks++; break;
                case RuneKind.IceArrow: IceStacks++; break;
                case RuneKind.MultiShot: MultiShotStacks++; break;
                case RuneKind.Detonator: DetonatorStacks++; break;
            }
            LevelUpPending = false;
            OnRuneApplied?.Invoke(kind);
        }

        public List<RuneKind> PickThreeOffer(uint seed)
        {
            var rng = new SeededRng(seed ^ (uint)Level);
            // 가중치: 특수 룬이 일반 스탯 룬보다 자주 등장
            var pool = new List<RuneKind>
            {
                RuneKind.PoisonBlade, RuneKind.PoisonBlade,
                RuneKind.IceArrow, RuneKind.IceArrow,
                RuneKind.MultiShot, RuneKind.MultiShot,
                RuneKind.Detonator, RuneKind.Detonator,
                RuneKind.DamageUp, RuneKind.FireRateUp, RuneKind.HpUp,
                RuneKind.RangeUp, RuneKind.MoveSpeedUp,
            };
            var pick = new List<RuneKind>();
            var used = new HashSet<RuneKind>();
            int tries = 0;
            while (pick.Count < 3 && pool.Count > 0 && tries < 50)
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
            RuneKind.MultiShot => "✦ 다중 사격",
            RuneKind.Detonator => "💥 폭발 처치",
            RuneKind.DamageUp => "⚔ 대미지 강화",
            RuneKind.FireRateUp => "⚡ 공격속도",
            RuneKind.HpUp => "❤ 최대 체력",
            RuneKind.RangeUp => "↔ 사거리",
            RuneKind.MoveSpeedUp => "👣 이동속도",
            _ => k.ToString(),
        };

        public static string Describe(RuneKind k) => k switch
        {
            RuneKind.DamageUp => "대미지 +25%",
            RuneKind.FireRateUp => "쿨다운 -15% (공속↑)",
            RuneKind.HpUp => "최대 HP +25 / 즉시 회복 25",
            RuneKind.RangeUp => "사거리 +1u",
            RuneKind.MoveSpeedUp => "이동속도 +15%",
            RuneKind.PoisonBlade => "공격이 적중하면 독 누적\n3초간 +5 DPS (스택)",
            RuneKind.IceArrow => "공격이 적중하면 적 이속 -50%\n2초간 (스택당 +1초)",
            RuneKind.MultiShot => "투사체 +1발 / 멜리 +1대상\n(스택)",
            RuneKind.Detonator => "처치 시 주변 폭발\n반경 1.6u, +8 대미지 (스택)",
            _ => k.ToString(),
        };
    }
}
