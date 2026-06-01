import type { GameWorld } from '../world';
import { Lifetime } from '../components/lifetime';
import { Health } from '../components/health';
import { hasComponent } from '../world';
import { lifetimeQuery } from '../queries';

export function lifetimeSystem(world: GameWorld): void {
  const dt = world.deltaTime;
  const entities = lifetimeQuery(world);
  for (const eid of entities) {
    Lifetime.remainingSec[eid]! -= dt;
    if (Lifetime.remainingSec[eid]! <= 0 && hasComponent(world, Health, eid)) {
      Health.dead[eid] = 1;
    }
  }
}
