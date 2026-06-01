import type Phaser from 'phaser';
import { addComponent, addEntity, type GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Collider } from '@/ecs/components/collider';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { Health } from '@/ecs/components/health';
import { StoneTag } from '@/ecs/components/tags';

export function spawnStone(
  world: GameWorld,
  scene: Phaser.Scene,
  spriteMap: Map<number, Phaser.GameObjects.Image>,
  nextGid: () => number,
  x: number,
  y: number,
): number {
  const eid = addEntity(world);
  addComponent(world, Position, eid);
  addComponent(world, Collider, eid);
  addComponent(world, SpriteRef, eid);
  addComponent(world, Health, eid);
  addComponent(world, StoneTag, eid);

  Position.x[eid] = x;
  Position.y[eid] = y;
  Collider.radius[eid] = 14;
  Health.current[eid] = 1;
  Health.max[eid] = 1;
  Health.dead[eid] = 0;

  const sprite = scene.add.image(x, y, 'stone');
  const gid = nextGid();
  spriteMap.set(gid, sprite);
  SpriteRef.gid[eid] = gid;

  return eid;
}
