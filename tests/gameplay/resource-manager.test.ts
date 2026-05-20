import { describe, expect, it } from 'vitest';
import { ResourceManager, ResourceKind } from '@/gameplay/resource-manager';
import { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';

describe('ResourceManager', () => {
  function make() {
    const bus = new EventBus<GameEvents>();
    return { rm: new ResourceManager(bus), bus };
  }

  it('initializes with starting resources from balance config', () => {
    const { rm } = make();
    expect(rm.get(ResourceKind.Wood)).toBe(15);
    expect(rm.get(ResourceKind.Stone)).toBe(5);
    expect(rm.get(ResourceKind.Food)).toBe(5);
  });

  it('adds resources', () => {
    const { rm } = make();
    rm.add(ResourceKind.Wood, 10);
    expect(rm.get(ResourceKind.Wood)).toBe(25);
  });

  it('spends resources and returns true if sufficient', () => {
    const { rm } = make();
    expect(rm.spend(ResourceKind.Wood, 10)).toBe(true);
    expect(rm.get(ResourceKind.Wood)).toBe(5);
  });

  it('returns false and does not deduct if insufficient', () => {
    const { rm } = make();
    expect(rm.spend(ResourceKind.Wood, 100)).toBe(false);
    expect(rm.get(ResourceKind.Wood)).toBe(15);
  });

  it('emits resource:changed on add', () => {
    const { rm, bus } = make();
    let event: any;
    bus.on('resource:changed', (e) => { event = e; });
    rm.add(ResourceKind.Stone, 3);
    expect(event).toEqual({ kind: 'stone', delta: 3, total: 8 });
  });

  it('canAfford checks without spending', () => {
    const { rm } = make();
    expect(rm.canAfford(ResourceKind.Wood, 15)).toBe(true);
    expect(rm.canAfford(ResourceKind.Wood, 16)).toBe(false);
  });
});
