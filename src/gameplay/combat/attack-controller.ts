import { defineQuery } from 'bitecs';
import { type GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Weapon } from '@/ecs/components/weapon';
import { Health } from '@/ecs/components/health';
import { ZombieTag } from '@/ecs/components/tags';
import { WEAPONS } from './weapon-definitions';
import { calcDamage } from './damage-calc';
import { distance } from '@/util/math';
import type { Rng } from '@/util/rng';

const zombieQuery = defineQuery([ZombieTag, Position, Health]);

/**
 * 플레이어 eid의 Weapon 쿨다운 감소 → 사거리 내 가장 가까운 좀비를 찾아 대미지.
 * 처치된 좀비의 eid 반환 (HUD/이벤트용).
 */
export function playerAttackSystem(
  world: GameWorld,
  playerEid: number,
  rng: Rng,
): number | null {
  const dt = world.deltaTime;
  const cd = Weapon.cooldown[playerEid] ?? 0;
  if (cd > 0) {
    Weapon.cooldown[playerEid] = cd - dt;
    return null;
  }
  const weapon = WEAPONS.longsword!;
  const px = Position.x[playerEid] ?? 0;
  const py = Position.y[playerEid] ?? 0;

  let nearest: { eid: number; dist: number } | null = null;
  const zombies = zombieQuery(world);
  for (const zeid of zombies) {
    const d = distance(px, py, Position.x[zeid] ?? 0, Position.y[zeid] ?? 0);
    if (d <= weapon.range && (!nearest || d < nearest.dist)) {
      nearest = { eid: zeid, dist: d };
    }
  }
  if (!nearest) return null;

  const dmg = calcDamage({
    base: weapon.baseDamage,
    armor: 0,
    critRoll: rng.next(),
    critChance: weapon.critChance,
    critMult: weapon.critMult,
  });
  const cur = Health.current[nearest.eid] ?? 0;
  const next = cur - dmg;
  Health.current[nearest.eid] = next;
  if (next <= 0) Health.dead[nearest.eid] = 1;
  Weapon.cooldown[playerEid] = weapon.cooldownSec;
  return nearest.eid;
}
