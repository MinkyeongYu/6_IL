import { defineQuery } from 'bitecs';
import type { GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Velocity } from '@/ecs/components/velocity';
import { DeerTag } from '@/ecs/components/tags';
import { distance, vec2Normalize } from '@/util/math';

const DEER_FLEE_RADIUS = 120;
const DEER_SPEED = 140;

const deerQuery = defineQuery([DeerTag, Position, Velocity]);

export function deerAiSystem(world: GameWorld, playerX: number, playerY: number): void {
  const deers = deerQuery(world);
  for (const eid of deers) {
    const dx = (Position.x[eid] ?? 0) - playerX;
    const dy = (Position.y[eid] ?? 0) - playerY;
    const d = distance(playerX, playerY, Position.x[eid] ?? 0, Position.y[eid] ?? 0);
    if (d < DEER_FLEE_RADIUS) {
      const [nx, ny] = vec2Normalize(dx, dy);
      Velocity.vx[eid] = nx * DEER_SPEED;
      Velocity.vy[eid] = ny * DEER_SPEED;
    } else {
      Velocity.vx[eid] = 0;
      Velocity.vy[eid] = 0;
    }
  }
}
