import { type GameWorld, removeEntity } from '../world';
import { Health } from '../components/health';
import { healthQuery } from '../queries';

export function cleanupSystem(world: GameWorld): number[] {
  const entities = healthQuery(world);
  const removed: number[] = [];
  for (const eid of entities) {
    if (Health.dead[eid] === 1) {
      removeEntity(world, eid);
      removed.push(eid);
    }
  }
  return removed;
}
