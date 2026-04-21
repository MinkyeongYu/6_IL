import { describe, expect, it } from 'vitest';
import { createResourceStore, type ResourceKind } from '@/gameplay/resources/resource-store';

describe('ResourceStore', () => {
  it('initializes with zeros', () => {
    const store = createResourceStore();
    expect(store.get('wood')).toBe(0);
    expect(store.get('meat')).toBe(0);
    expect(store.get('food')).toBe(0);
  });

  it('add/spend adjusts totals', () => {
    const store = createResourceStore();
    store.add('wood', 10);
    expect(store.get('wood')).toBe(10);
    const ok = store.spend('wood', 3);
    expect(ok).toBe(true);
    expect(store.get('wood')).toBe(7);
  });

  it('spend returns false when insufficient', () => {
    const store = createResourceStore();
    store.add('wood', 2);
    const ok = store.spend('wood', 5);
    expect(ok).toBe(false);
    expect(store.get('wood')).toBe(2);
  });

  it('snapshot returns plain object', () => {
    const store = createResourceStore();
    store.add('wood', 3);
    store.add('meat', 5);
    const snap = store.snapshot();
    expect(snap.wood).toBe(3);
    expect(snap.meat).toBe(5);
  });

  it('restore loads from snapshot', () => {
    const store = createResourceStore();
    store.restore({ wood: 7, meat: 1, food: 2, frostbloom: 0 });
    expect(store.get('wood')).toBe(7);
    expect(store.get('meat')).toBe(1);
    expect(store.get('food')).toBe(2);
  });

  it('emits resource:changed event on add', () => {
    const store = createResourceStore();
    const events: Array<{ kind: ResourceKind; delta: number; total: number }> = [];
    store.onChange((e) => events.push(e));
    store.add('wood', 5);
    expect(events).toHaveLength(1);
    expect(events[0]).toEqual({ kind: 'wood', delta: 5, total: 5 });
  });
});
