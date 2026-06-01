import {
  addComponent as bitAddComponent,
  addEntity as bitAddEntity,
  createWorld as bitCreateWorld,
  hasComponent as bitHasComponent,
  removeComponent as bitRemoveComponent,
  removeEntity as bitRemoveEntity,
  type Component,
  type IWorld,
} from 'bitecs';

/**
 * 게임 전용 월드 확장. bitecs IWorld에 게임 틱 정보를 올린다.
 */
export interface GameWorld extends IWorld {
  deltaTime: number;
  elapsed: number;
}

export function createGameWorld(): GameWorld {
  const w = bitCreateWorld() as GameWorld;
  w.deltaTime = 0;
  w.elapsed = 0;
  return w;
}

export function addEntity(world: GameWorld): number {
  return bitAddEntity(world);
}

export function removeEntity(world: GameWorld, eid: number): void {
  bitRemoveEntity(world, eid);
}

export function addComponent(world: GameWorld, component: Component, eid: number): void {
  bitAddComponent(world, component, eid);
}

export function removeComponent(world: GameWorld, component: Component, eid: number): void {
  bitRemoveComponent(world, component, eid);
}

export function hasComponent(world: GameWorld, component: Component, eid: number): boolean {
  return bitHasComponent(world, component, eid);
}
