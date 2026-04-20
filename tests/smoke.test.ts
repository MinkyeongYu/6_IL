import { describe, expect, it } from 'vitest';

describe('smoke', () => {
  it('vitest is wired up', () => {
    expect(1 + 1).toBe(2);
  });

  it('TILE_SIZE constant is importable', async () => {
    const { TILE_SIZE } = await import('@/config/constants');
    expect(TILE_SIZE).toBe(32);
  });
});
