using System;
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

        public DayNightController(BalanceConfig balance) { _balance = balance; }

        public float PhaseDurationSec => DurationOf(Phase);

        public void Update(float dt)
        {
            ElapsedInPhase += dt;
            while (ElapsedInPhase >= DurationOf(Phase))
            {
                ElapsedInPhase -= DurationOf(Phase);
                Advance();
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
        }

        private float DurationOf(Phase p) => p switch
        {
            Phase.Day => _balance.DayDurationSec,
            Phase.Evening => _balance.EveningTransitionSec,
            Phase.Night => _balance.NightDurationSec,
            Phase.Dawn => _balance.DawnTransitionSec,
            _ => 1f,
        };

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
