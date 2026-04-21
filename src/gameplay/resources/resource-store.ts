export type ResourceKind = 'wood' | 'stone' | 'iron' | 'meat' | 'food' | 'frostbloom';

export type ResourceSnapshot = Record<ResourceKind, number>;

export interface ResourceChangeEvent {
  kind: ResourceKind;
  delta: number;
  total: number;
}

export interface ResourceStore {
  get(kind: ResourceKind): number;
  add(kind: ResourceKind, amount: number): void;
  spend(kind: ResourceKind, amount: number): boolean;
  snapshot(): ResourceSnapshot;
  restore(snap: ResourceSnapshot): void;
  onChange(listener: (e: ResourceChangeEvent) => void): () => void;
}

export function createResourceStore(): ResourceStore {
  const totals: ResourceSnapshot = {
    wood: 0,
    stone: 0,
    iron: 0,
    meat: 0,
    food: 0,
    frostbloom: 0,
  };
  const listeners = new Set<(e: ResourceChangeEvent) => void>();

  const emit = (e: ResourceChangeEvent): void => {
    for (const l of listeners) {
      try {
        l(e);
      } catch (err) {
        console.error('[ResourceStore] listener threw:', err);
      }
    }
  };

  return {
    get(kind) {
      return totals[kind];
    },
    add(kind, amount) {
      if (amount <= 0) return;
      totals[kind] += amount;
      emit({ kind, delta: amount, total: totals[kind] });
    },
    spend(kind, amount) {
      if (totals[kind] < amount) return false;
      totals[kind] -= amount;
      emit({ kind, delta: -amount, total: totals[kind] });
      return true;
    },
    snapshot() {
      return { ...totals };
    },
    restore(snap) {
      for (const k of Object.keys(totals) as ResourceKind[]) {
        totals[k] = snap[k] ?? 0;
      }
    },
    onChange(listener) {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
  };
}
