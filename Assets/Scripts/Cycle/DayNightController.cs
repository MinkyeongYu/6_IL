using System;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    public enum Phase { Day, Evening, Night, Dawn }

    [Serializable]
    public class DayNightSnapshot
    {
        public int day = 1;
        public Phase phase = Phase.Day;
        public float elapsedInPhase;
    }

    /// <summary>
    /// 낮 → 저녁 → 밤 → 새벽 상태 머신. 외부 dt를 입력받아 진행.
    /// 각 페이즈 진입 시 EventBus로 이벤트 방출.
    /// </summary>
    public sealed class DayNightController
    {
        private readonly BalanceConfig _balance;

        public int Day { get; private set; } = 1;
        public Phase Phase { get; private set; } = Phase.Day;
        public float ElapsedInPhase { get; private set; }

        /// <summary>레벨이 오를수록 낮/밤 길이가 늘어남 — PlayerProgression 가 갱신.
        /// 단, 현재 페이즈 길이는 PhaseLockedDuration 에 진입 시점 값으로 캐시 — 도중 변경되어도 영향 X.</summary>
        public float LevelDurationMul = 1f;

        /// <summary>현재 페이즈 시작 시점에 잠긴 지속 시간 — 진행 중에는 변하지 않음.</summary>
        private float _phaseLockedDuration;

        public DayNightController(BalanceConfig balance)
        {
            _balance = balance;
            _phaseLockedDuration = ComputeDuration(Phase);
        }

        public float PhaseDurationSec => _phaseLockedDuration > 0f ? _phaseLockedDuration : ComputeDuration(Phase);

        public void Update(float dt)
        {
            ElapsedInPhase += dt;
            while (ElapsedInPhase >= _phaseLockedDuration)
            {
                ElapsedInPhase -= _phaseLockedDuration;
                Advance();
                _phaseLockedDuration = ComputeDuration(Phase);
            }
        }

        public DayNightSnapshot Snapshot() => new()
        {
            day = Day,
            phase = Phase,
            elapsedInPhase = ElapsedInPhase,
        };

        public void Restore(DayNightSnapshot snap)
        {
            if (snap == null) return;
            Day = snap.day;
            Phase = snap.phase;
            ElapsedInPhase = snap.elapsedInPhase;
            _phaseLockedDuration = ComputeDuration(Phase);
        }

        private float ComputeDuration(Phase p)
        {
            float baseDur = p switch
            {
                Phase.Day => _balance.DayDurationSec,
                Phase.Evening => _balance.EveningTransitionSec,
                Phase.Night => _balance.NightDurationSec,
                Phase.Dawn => _balance.DawnTransitionSec,
                _ => 1f,
            };
            if (p == Phase.Day || p == Phase.Night) return baseDur * Mathf.Max(1f, LevelDurationMul);
            return baseDur;
        }

        private void Advance()
        {
            switch (Phase)
            {
                case Phase.Day:
                    Phase = Phase.Evening;
                    EventBus.Instance.Emit(new DayPayload(Day)); // day:ended ↔ DayPayload (의미 구분은 컨텍스트로)
                    EventBus.Instance.Emit(new EveningStartedPayload(Day));
                    break;
                case Phase.Evening:
                    Phase = Phase.Night;
                    EventBus.Instance.Emit(new NightStartedPayload(Day));
                    break;
                case Phase.Night:
                    Phase = Phase.Dawn;
                    EventBus.Instance.Emit(new NightEndedPayload(Day));
                    EventBus.Instance.Emit(new DawnStartedPayload(Day));
                    break;
                case Phase.Dawn:
                    Phase = Phase.Day;
                    Day += 1;
                    EventBus.Instance.Emit(new DayStartedPayload(Day));
                    break;
            }
            ElapsedInPhase = 0;
        }
    }

    // 사이클 페이로드 (struct로 EventBus와 호환)
    public readonly struct DayStartedPayload { public readonly int Day; public DayStartedPayload(int d) { Day = d; } }
    public readonly struct EveningStartedPayload { public readonly int Day; public EveningStartedPayload(int d) { Day = d; } }
    public readonly struct NightStartedPayload { public readonly int Day; public NightStartedPayload(int d) { Day = d; } }
    public readonly struct NightEndedPayload { public readonly int Day; public NightEndedPayload(int d) { Day = d; } }
    public readonly struct DawnStartedPayload { public readonly int Day; public DawnStartedPayload(int d) { Day = d; } }
}
