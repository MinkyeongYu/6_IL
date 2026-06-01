import type { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';

export type Phase = 'day' | 'evening' | 'night' | 'dawn';

export interface PhaseDurations {
  dayDurationSec: number;
  eveningTransitionSec: number;
  nightDurationSec: number;
  dawnTransitionSec: number;
}

export interface DayNightSnapshot {
  day: number;
  phase: Phase;
  elapsedInPhase: number;
}

export interface DayNightController {
  readonly day: number;
  readonly phase: Phase;
  readonly elapsedInPhase: number;
  readonly phaseDurationSec: number;
  update(dt: number): void;
  snapshot(): DayNightSnapshot;
  restore(snap: DayNightSnapshot): void;
}

export function createDayNightController(
  durations: PhaseDurations,
  bus: EventBus<GameEvents>,
): DayNightController {
  let day = 1;
  let phase: Phase = 'day';
  let elapsed = 0;

  const durationOf = (p: Phase): number => {
    switch (p) {
      case 'day':
        return durations.dayDurationSec;
      case 'evening':
        return durations.eveningTransitionSec;
      case 'night':
        return durations.nightDurationSec;
      case 'dawn':
        return durations.dawnTransitionSec;
    }
  };

  const advance = (): void => {
    switch (phase) {
      case 'day':
        phase = 'evening';
        bus.emit('day:ended', { day });
        bus.emit('evening:started', { day });
        break;
      case 'evening':
        phase = 'night';
        bus.emit('night:started', { day });
        break;
      case 'night':
        phase = 'dawn';
        bus.emit('night:ended', { day });
        bus.emit('dawn:started', { day });
        break;
      case 'dawn':
        phase = 'day';
        day += 1;
        bus.emit('day:started', { day });
        break;
    }
    elapsed = 0;
  };

  return {
    get day() {
      return day;
    },
    get phase() {
      return phase;
    },
    get elapsedInPhase() {
      return elapsed;
    },
    get phaseDurationSec() {
      return durationOf(phase);
    },
    update(dt) {
      elapsed += dt;
      while (elapsed >= durationOf(phase)) {
        elapsed -= durationOf(phase);
        advance();
      }
    },
    snapshot() {
      return { day, phase, elapsedInPhase: elapsed };
    },
    restore(snap) {
      day = snap.day;
      phase = snap.phase;
      elapsed = snap.elapsedInPhase;
    },
  };
}
