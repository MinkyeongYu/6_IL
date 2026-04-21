import { describe, expect, it } from 'vitest';
import { addComponent, addEntity, createGameWorld } from '@/ecs/world';
import { Position } from '@/ecs/components/position';
import { Velocity } from '@/ecs/components/velocity';
import { Lifetime } from '@/ecs/components/lifetime';
import { Health } from '@/ecs/components/health';
import { movementSystem } from '@/ecs/systems/movement';
import { lifetimeSystem } from '@/ecs/systems/lifetime';
import { cleanupSystem } from '@/ecs/systems/cleanup';

describe('movementSystem', () => {
  it('advances position by velocity * dt', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Position, e);
    addComponent(w, Velocity, e);
    Position.x[e] = 0;
    Position.y[e] = 0;
    Velocity.vx[e] = 100;
    Velocity.vy[e] = 50;
    w.deltaTime = 0.1;

    movementSystem(w);

    expect(Position.x[e]).toBeCloseTo(10);
    expect(Position.y[e]).toBeCloseTo(5);
  });
});

describe('lifetimeSystem', () => {
  it('decrements lifetime by dt', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Lifetime, e);
    Lifetime.remainingSec[e] = 1;
    w.deltaTime = 0.25;

    lifetimeSystem(w);

    expect(Lifetime.remainingSec[e]).toBeCloseTo(0.75);
  });

  it('expires without Health component is a no-op (only decrements)', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Lifetime, e);
    Lifetime.remainingSec[e] = 0.05;
    w.deltaTime = 0.1;

    lifetimeSystem(w);

    expect(Lifetime.remainingSec[e]).toBeLessThanOrEqual(0);
  });

  it('marks Health.dead = 1 when lifetime expires and entity has Health', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Lifetime, e);
    addComponent(w, Health, e);
    Lifetime.remainingSec[e] = 0.05;
    Health.current[e] = 10;
    Health.max[e] = 10;
    Health.dead[e] = 0;
    w.deltaTime = 0.1;

    lifetimeSystem(w);

    expect(Health.dead[e]).toBe(1);
  });
});

describe('cleanupSystem', () => {
  it('removes entities with Health.dead == 1', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Health, e);
    Health.current[e] = 0;
    Health.max[e] = 10;
    Health.dead[e] = 1;

    const removed = cleanupSystem(w);

    expect(removed).toContain(e);
  });

  it('does not remove alive entities', () => {
    const w = createGameWorld();
    const e = addEntity(w);
    addComponent(w, Health, e);
    Health.current[e] = 10;
    Health.max[e] = 10;
    Health.dead[e] = 0;

    const removed = cleanupSystem(w);

    expect(removed).not.toContain(e);
  });
});
