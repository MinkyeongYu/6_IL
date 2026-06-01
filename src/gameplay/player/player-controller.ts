import { Velocity } from '@/ecs/components/velocity';
import { Health } from '@/ecs/components/health';
import { vec2Normalize } from '@/util/math';
import { PLAYER } from '@/config/balance';
import type { InputAdapter } from './input-adapter';

export function updatePlayerInput(eid: number, input: InputAdapter): void {
  if ((Health.current[eid] ?? 0) <= 0 || Health.dead[eid] === 1) {
    Velocity.vx[eid] = 0;
    Velocity.vy[eid] = 0;
    return;
  }
  const ax = input.axis('left', 'right');
  const ay = input.axis('up', 'down');
  const [nx, ny] = vec2Normalize(ax, ay);
  Velocity.vx[eid] = nx * PLAYER.moveSpeed;
  Velocity.vy[eid] = ny * PLAYER.moveSpeed;
}
