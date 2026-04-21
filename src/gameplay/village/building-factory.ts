import type Phaser from 'phaser';
import { addComponent, addEntity, type GameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Health } from '@/ecs/components/health';
import { Collider } from '@/ecs/components/collider';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { BuildingTag, BarricadeTag, CampfireTag } from '@/ecs/components/tags';
import { BUILDING_HP } from '@/config/balance';

export type BuildingKind = 'campfire' | 'barricade';

interface BuildingSpec {
  w: number;
  h: number;
  texture: string;
  hp: number;
}

const FOOTPRINT: Record<BuildingKind, BuildingSpec> = {
  campfire: { w: 2, h: 2, texture: 'campfire', hp: BUILDING_HP.campfire },
  barricade: { w: 1, h: 1, texture: 'barricade', hp: BUILDING_HP.barricadeWood },
};

export function spawnBuilding(
  world: GameWorld,
  scene: Phaser.Scene,
  spriteMap: Map<number, Phaser.GameObjects.Image>,
  nextGid: () => number,
  kind: BuildingKind,
  worldX: number,
  worldY: number,
): number {
  const spec = FOOTPRINT[kind];
  const eid = addEntity(world);
  addComponent(world, Position, eid);
  addComponent(world, Health, eid);
  addComponent(world, Collider, eid);
  addComponent(world, SpriteRef, eid);
  addComponent(world, BuildingTag, eid);
  if (kind === 'campfire') addComponent(world, CampfireTag, eid);
  if (kind === 'barricade') addComponent(world, BarricadeTag, eid);

  Position.x[eid] = worldX;
  Position.y[eid] = worldY;
  Health.current[eid] = spec.hp;
  Health.max[eid] = spec.hp;
  Health.dead[eid] = 0;
  Collider.radius[eid] = spec.w * 16;

  const sprite = scene.add.image(worldX, worldY, spec.texture);
  const gid = nextGid();
  spriteMap.set(gid, sprite);
  SpriteRef.gid[eid] = gid;

  return eid;
}

export const BUILDING_COSTS: Record<BuildingKind, { kind: 'wood'; amount: number }> = {
  campfire: { kind: 'wood', amount: 5 },
  barricade: { kind: 'wood', amount: 5 },
};

export const BUILDING_FOOTPRINTS = FOOTPRINT;
