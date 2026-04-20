import { describe, expect, it, vi } from 'vitest';
import { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';

describe('EventBus', () => {
  it('calls subscriber when event is emitted', () => {
    const bus = new EventBus<GameEvents>();
    const handler = vi.fn();

    bus.on('day:ended', handler);
    bus.emit('day:ended', { day: 1 });

    expect(handler).toHaveBeenCalledTimes(1);
    expect(handler).toHaveBeenCalledWith({ day: 1 });
  });

  it('does not call unsubscribed handlers', () => {
    const bus = new EventBus<GameEvents>();
    const handler = vi.fn();

    const unsub = bus.on('day:ended', handler);
    unsub();
    bus.emit('day:ended', { day: 1 });

    expect(handler).not.toHaveBeenCalled();
  });

  it('supports multiple subscribers on same event', () => {
    const bus = new EventBus<GameEvents>();
    const a = vi.fn();
    const b = vi.fn();

    bus.on('day:ended', a);
    bus.on('day:ended', b);
    bus.emit('day:ended', { day: 2 });

    expect(a).toHaveBeenCalledWith({ day: 2 });
    expect(b).toHaveBeenCalledWith({ day: 2 });
  });

  it('clear() removes all subscribers', () => {
    const bus = new EventBus<GameEvents>();
    const handler = vi.fn();

    bus.on('day:ended', handler);
    bus.clear();
    bus.emit('day:ended', { day: 1 });

    expect(handler).not.toHaveBeenCalled();
  });

  it('emitting isolates listener errors', () => {
    const bus = new EventBus<GameEvents>();
    const bad = vi.fn(() => {
      throw new Error('listener failure');
    });
    const good = vi.fn();

    bus.on('day:ended', bad);
    bus.on('day:ended', good);

    expect(() => bus.emit('day:ended', { day: 3 })).not.toThrow();
    expect(good).toHaveBeenCalledWith({ day: 3 });
  });
});
