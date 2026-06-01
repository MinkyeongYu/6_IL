import { describe, expect, it } from 'vitest';
import { createDayNightController } from '@/gameplay/cycle/day-night-controller';
import { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';

const phases = {
  dayDurationSec: 5,
  eveningTransitionSec: 1,
  nightDurationSec: 3,
  dawnTransitionSec: 1,
};

describe('DayNightController', () => {
  it('starts in day phase on Day 1', () => {
    const bus = new EventBus<GameEvents>();
    const c = createDayNightController(phases, bus);
    expect(c.phase).toBe('day');
    expect(c.day).toBe(1);
  });

  it('transitions day -> evening after dayDurationSec', () => {
    const bus = new EventBus<GameEvents>();
    const c = createDayNightController(phases, bus);
    c.update(5);
    expect(c.phase).toBe('evening');
  });

  it('emits phase events', () => {
    const bus = new EventBus<GameEvents>();
    const log: string[] = [];
    bus.on('evening:started', () => log.push('evening'));
    bus.on('night:started', () => log.push('night'));
    bus.on('dawn:started', () => log.push('dawn'));
    bus.on('day:started', (p) => log.push(`day:${p.day}`));

    const c = createDayNightController(phases, bus);
    c.update(5); // day -> evening
    c.update(1); // evening -> night
    c.update(3); // night -> dawn
    c.update(1); // dawn -> day 2

    expect(log).toEqual(['evening', 'night', 'dawn', 'day:2']);
    expect(c.day).toBe(2);
  });

  it('snapshot and restore', () => {
    const bus = new EventBus<GameEvents>();
    const c = createDayNightController(phases, bus);
    c.update(2.5);
    const snap = c.snapshot();

    const c2 = createDayNightController(phases, bus);
    c2.restore(snap);
    expect(c2.phase).toBe('day');
    expect(c2.day).toBe(1);
    c2.update(2.5);
    expect(c2.phase).toBe('evening');
  });
});
