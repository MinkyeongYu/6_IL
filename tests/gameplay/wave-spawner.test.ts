import { describe, expect, it } from 'vitest';
import { WaveSpawner } from '@/gameplay/wave-spawner';

describe('WaveSpawner', () => {
  it('generates wave 1 with base zombie count', () => {
    const spawner = new WaveSpawner();
    const wave = spawner.getWave(1, 1);
    expect(wave.count).toBeGreaterThanOrEqual(5);
    expect(wave.count).toBeLessThanOrEqual(10);
  });

  it('increases zombie count on later days', () => {
    const spawner = new WaveSpawner();
    const w1 = spawner.getWave(1, 1);
    const w5 = spawner.getWave(5, 1);
    expect(w5.count).toBeGreaterThan(w1.count);
  });

  it('generates 3 waves per night', () => {
    const spawner = new WaveSpawner();
    expect(spawner.totalWaves).toBe(3);
  });

  it('returns spawn positions around village edge', () => {
    const spawner = new WaveSpawner();
    const wave = spawner.getWave(1, 1);
    for (const pos of wave.positions) {
      const dist = Math.sqrt(pos.x * pos.x + pos.y * pos.y);
      expect(dist).toBeGreaterThan(300);
    }
  });
});
