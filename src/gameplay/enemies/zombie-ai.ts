import { defineQuery } from 'bitecs';
import { hasComponent, type GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Velocity } from '@/ecs/components/velocity';
import { Health } from '@/ecs/components/health';
import { Ai } from '@/ecs/components/ai';
import { ZombieTag, BarricadeTag, CampfireTag, PlayerTag } from '@/ecs/components/tags';
import { distance, vec2Normalize } from '@/util/math';
import { ZOMBIE } from '@/config/balance';

const zombieQuery = defineQuery([ZombieTag, Position, Velocity, Ai]);
const targetQuery = defineQuery([Position]);

export function zombieAiSystem(world: GameWorld): void {
  const dt = world.deltaTime;
  const zombies = zombieQuery(world);
  const targets = targetQuery(world);

  for (const zeid of zombies) {
    let nearest: { eid: number; dist: number } | null = null;
    const zx = Position.x[zeid] ?? 0;
    const zy = Position.y[zeid] ?? 0;
    for (const teid of targets) {
      if (teid === zeid) continue;
      const isTarget =
        hasComponent(world, PlayerTag, teid) ||
        hasComponent(world, BarricadeTag, teid) ||
        hasComponent(world, CampfireTag, teid);
      if (!isTarget) continue;
      const tx = Position.x[teid] ?? 0;
      const ty = Position.y[teid] ?? 0;
      const d = distance(zx, zy, tx, ty);
      if (!nearest || d < nearest.dist) nearest = { eid: teid, dist: d };
    }

    if (!nearest) {
      Velocity.vx[zeid] = 0;
      Velocity.vy[zeid] = 0;
      continue;
    }

    Ai.targetEid[zeid] = nearest.eid;

    if (nearest.dist <= ZOMBIE.attackRange) {
      Velocity.vx[zeid] = 0;
      Velocity.vy[zeid] = 0;
      const cd = Ai.attackCooldown[zeid] ?? 0;
      if (cd <= 0) {
        const cur = Health.current[nearest.eid] ?? 0;
        const nextHp = cur - ZOMBIE.attackDamage;
        Health.current[nearest.eid] = nextHp;
        if (nextHp <= 0) Health.dead[nearest.eid] = 1;
        Ai.attackCooldown[zeid] = ZOMBIE.attackCooldownSec;
      } else {
        Ai.attackCooldown[zeid] = cd - dt;
      }
    } else {
      const tx = Position.x[nearest.eid] ?? 0;
      const ty = Position.y[nearest.eid] ?? 0;
      const dx = tx - zx;
      const dy = ty - zy;
      const [nx, ny] = vec2Normalize(dx, dy);
      Velocity.vx[zeid] = nx * ZOMBIE.moveSpeed;
      Velocity.vy[zeid] = ny * ZOMBIE.moveSpeed;
      const cd = Ai.attackCooldown[zeid] ?? 0;
      if (cd > 0) Ai.attackCooldown[zeid] = cd - dt;
    }
  }
}
