import type { GameWorld } from '../world';
import { Position } from '../components/position';
import { Velocity } from '../components/velocity';
import { movementQuery } from '../queries';

export function movementSystem(world: GameWorld): void {
  const dt = world.deltaTime;
  const entities = movementQuery(world);
  for (const eid of entities) {
    Position.x[eid]! += Velocity.vx[eid]! * dt;
    Position.y[eid]! += Velocity.vy[eid]! * dt;
  }
}
