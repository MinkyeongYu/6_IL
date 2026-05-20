import { RESOURCES } from '@/config/balance';
import type { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';

export enum ResourceKind {
  Wood = 'wood',
  Stone = 'stone',
  Iron = 'iron',
  Meat = 'meat',
  Food = 'food',
  Frostbloom = 'frostbloom',
}

const STARTING: Record<ResourceKind, number> = {
  [ResourceKind.Wood]: RESOURCES.startingWood,
  [ResourceKind.Stone]: RESOURCES.startingStone,
  [ResourceKind.Iron]: RESOURCES.startingIron,
  [ResourceKind.Meat]: RESOURCES.startingMeat,
  [ResourceKind.Food]: RESOURCES.startingFood,
  [ResourceKind.Frostbloom]: RESOURCES.startingFrostbloom,
};

export class ResourceManager {
  private readonly amounts = new Map<ResourceKind, number>();

  constructor(private readonly bus: EventBus<GameEvents>) {
    for (const kind of Object.values(ResourceKind)) {
      this.amounts.set(kind, STARTING[kind]);
    }
  }

  get(kind: ResourceKind): number {
    return this.amounts.get(kind) ?? 0;
  }

  add(kind: ResourceKind, amount: number): void {
    const next = this.get(kind) + amount;
    this.amounts.set(kind, next);
    this.bus.emit('resource:changed', { kind, delta: amount, total: next });
  }

  spend(kind: ResourceKind, amount: number): boolean {
    const cur = this.get(kind);
    if (cur < amount) return false;
    const next = cur - amount;
    this.amounts.set(kind, next);
    this.bus.emit('resource:changed', { kind, delta: -amount, total: next });
    return true;
  }

  canAfford(kind: ResourceKind, amount: number): boolean {
    return this.get(kind) >= amount;
  }

  getAll(): Record<ResourceKind, number> {
    const result = {} as Record<ResourceKind, number>;
    for (const kind of Object.values(ResourceKind)) {
      result[kind] = this.get(kind);
    }
    return result;
  }
}
