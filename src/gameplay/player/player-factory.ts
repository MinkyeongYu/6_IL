import type Phaser from 'phaser';
import { addComponent, addEntity, type GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Velocity } from '@/ecs/components/velocity';
import { Health } from '@/ecs/components/health';
import { Collider } from '@/ecs/components/collider';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { PlayerTag } from '@/ecs/components/tags';
import { Weapon } from '@/ecs/components/weapon';
import { PLAYER } from '@/config/balance';
import { WEAPONS } from '@/gameplay/combat/weapon-definitions';

export interface PlayerHandles {
  eid: number;
  sprite: Phaser.GameObjects.Image;
  gid: number;
}

export function spawnPlayer(
  world: GameWorld,
  scene: Phaser.Scene,
  spriteMap: Map<number, Phaser.GameObjects.Image>,
  nextGid: () => number,
  x: number,
  y: number,
): PlayerHandles {
  const eid = addEntity(world);
  addComponent(world, Position, eid);
  addComponent(world, Velocity, eid);
  addComponent(world, Health, eid);
  addComponent(world, Collider, eid);
  addComponent(world, SpriteRef, eid);
  addComponent(world, PlayerTag, eid);
  addComponent(world, Weapon, eid);

  Position.x[eid] = x;
  Position.y[eid] = y;
  Velocity.vx[eid] = 0;
  Velocity.vy[eid] = 0;
  Health.current[eid] = PLAYER.maxHp;
  Health.max[eid] = PLAYER.maxHp;
  Health.dead[eid] = 0;
  Collider.radius[eid] = 12;
  Weapon.weaponId[eid] = 1;
  Weapon.cooldown[eid] = 0;
  Weapon.range[eid] = WEAPONS.longsword!.range;

  const sprite = scene.add.image(x, y, 'player', 0).setScale(0.48);
  const gid = nextGid();
  spriteMap.set(gid, sprite);
  SpriteRef.gid[eid] = gid;

  return { eid, sprite, gid };
}
