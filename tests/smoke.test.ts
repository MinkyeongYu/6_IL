import { describe, expect, it } from 'vitest';

describe('smoke', () => {
  it('vitest is wired up', () => {
    expect(1 + 1).toBe(2);
  });

  it('TILE_SIZE constant is importable', async () => {
    const { TILE_SIZE } = await import('@/config/constants');
    expect(TILE_SIZE).toBe(32);
  });

  it('balance config is importable and frozen at type level', async () => {
    const { BALANCE } = await import('@/config/balance');
    expect(BALANCE.dayCycle.dayDurationSec).toBeGreaterThan(0);
    expect(BALANCE.vision.dayRadiusTiles).toBe(10);
  });
});
