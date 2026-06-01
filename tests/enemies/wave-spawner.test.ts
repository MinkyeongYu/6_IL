import { describe, expect, it } from 'vitest';
import { createWaveSpawner } from '@/gameplay/enemies/wave-spawner';
import { createRng } from '@/util/rng';

describe('WaveSpawner', () => {
  it('plans a wave sized by day (simple scaling)', () => {
    const rng = createRng(1);
    const spawner = createWaveSpawner(rng);
    const plan = spawner.planWave(1);
    expect(plan.enemyCount).toBe(8); // Day 1: 8
    expect(plan.spawnPoints.length).toBeGreaterThan(0);
  });

  it('scales linearly each day', () => {
    const rng = createRng(1);
    const spawner = createWaveSpawner(rng);
    expect(spawner.planWave(2).enemyCount).toBe(12);
    expect(spawner.planWave(3).enemyCount).toBe(16);
    expect(spawner.planWave(5).enemyCount).toBe(24);
  });

  it('caps enemy count at high day', () => {
    const rng = createRng(1);
    const spawner = createWaveSpawner(rng);
    expect(spawner.planWave(100).enemyCount).toBeLessThanOrEqual(300);
  });

  it('same seed produces same spawn points', () => {
    const spawn1 = createWaveSpawner(createRng(42)).planWave(3);
    const spawn2 = createWaveSpawner(createRng(42)).planWave(3);
    expect(spawn1.spawnPoints).toEqual(spawn2.spawnPoints);
  });
});
