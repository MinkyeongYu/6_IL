import { describe, expect, it } from 'vitest';
import { createGameWorld, addEntity, addComponent, hasComponent } from '@/ecs/world';
import {
  Position, Velocity, Health, Combat, PlayerTag, ZombieTag, DeerTag,
  ResourceNode, Warmth,
} from '@/ecs/components';

describe('ECS components', () => {
  it('attaches Position and Velocity to an entity', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Position, e);
    addComponent(w, Velocity, e);
    Position.x[e] = 100;
    Position.y[e] = 200;
    Velocity.vx[e] = 5;
    Velocity.vy[e] = -3;
    expect(Position.x[e]).toBe(100);
    expect(Velocity.vx[e]).toBe(5);
  });

  it('attaches Health with current and max', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Health, e);
    Health.current[e] = 100;
    Health.max[e] = 100;
    expect(Health.current[e]).toBe(100);
  });

  it('attaches Combat with weapon stats', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Combat, e);
    Combat.damage[e] = 25;
    Combat.range[e] = 48;
    Combat.cooldown[e] = 500;
    Combat.lastAttackTime[e] = 0;
    expect(Combat.damage[e]).toBe(25);
    expect(Combat.range[e]).toBe(48);
  });

  it('attaches tag components', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, PlayerTag, e);
    expect(hasComponent(w, PlayerTag, e)).toBe(true);
    expect(hasComponent(w, ZombieTag, e)).toBe(false);
  });

  it('attaches ResourceNode with type and amount', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, ResourceNode, e);
    ResourceNode.kind[e] = 0;
    ResourceNode.amount[e] = 5;
    ResourceNode.gatherTime[e] = 3000;
    expect(ResourceNode.kind[e]).toBe(0);
  });
});
