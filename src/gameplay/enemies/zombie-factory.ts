import type Phaser from 'phaser';
import { addComponent, addEntity, type GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Velocity } from '@/ecs/components/velocity';
import { Health } from '@/ecs/components/health';
import { Collider } from '@/ecs/components/collider';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { ZombieTag } from '@/ecs/components/tags';
import { Ai } from '@/ecs/components/ai';
import { ZOMBIE } from '@/config/balance';

export function spawnZombie(
  world: GameWorld,
  scene: Phaser.Scene,
  spriteMap: Map<number, Phaser.GameObjects.Image>,
  nextGid: () => number,
  x: number,
  y: number,
): number {
  const eid = addEntity(world);
  addComponent(world, Position, eid);
  addComponent(world, Velocity, eid);
  addComponent(world, Health, eid);
  addComponent(world, Collider, eid);
  addComponent(world, SpriteRef, eid);
  addComponent(world, ZombieTag, eid);
  addComponent(world, Ai, eid);

  Position.x[eid] = x;
  Position.y[eid] = y;
  Velocity.vx[eid] = 0;
  Velocity.vy[eid] = 0;
  Health.current[eid] = ZOMBIE.maxHp;
  Health.max[eid] = ZOMBIE.maxHp;
  Health.dead[eid] = 0;
  Collider.radius[eid] = 11;
  Ai.mode[eid] = 1;
  Ai.attackCooldown[eid] = 0;

  const sprite = scene.add.image(x, y, 'zombie');
  const gid = nextGid();
  spriteMap.set(gid, sprite);
  SpriteRef.gid[eid] = gid;

  return eid;
}
