import { describe, expect, it } from 'vitest';
import { createVillageGrid } from '@/gameplay/village/grid';

describe('VillageGrid', () => {
  it('starts empty (all cells free)', () => {
    const grid = createVillageGrid(24, 24);
    expect(grid.isFree(0, 0, 1, 1)).toBe(true);
    expect(grid.isFree(10, 10, 3, 3)).toBe(true);
  });

  it('place reserves cells with entity id', () => {
    const grid = createVillageGrid(24, 24);
    const ok = grid.place(5, 5, 2, 2, 42);
    expect(ok).toBe(true);
    expect(grid.isFree(5, 5, 2, 2)).toBe(false);
    expect(grid.getCell(5, 5)).toBe(42);
    expect(grid.getCell(6, 6)).toBe(42);
  });

  it('place fails if overlap', () => {
    const grid = createVillageGrid(24, 24);
    grid.place(5, 5, 2, 2, 1);
    const ok = grid.place(6, 6, 2, 2, 2);
    expect(ok).toBe(false);
  });

  it('place fails out of bounds', () => {
    const grid = createVillageGrid(24, 24);
    expect(grid.place(-1, 0, 1, 1, 1)).toBe(false);
    expect(grid.place(23, 23, 2, 2, 1)).toBe(false);
  });

  it('remove frees the cells', () => {
    const grid = createVillageGrid(24, 24);
    grid.place(5, 5, 2, 2, 42);
    grid.remove(42);
    expect(grid.isFree(5, 5, 2, 2)).toBe(true);
  });

  it('tileToWorld and worldToTile round-trip', () => {
    const grid = createVillageGrid(24, 24, 32);
    const { x, y } = grid.tileToWorld(3, 5);
    expect(x).toBe(3 * 32 + 16);
    expect(y).toBe(5 * 32 + 16);
    const tile = grid.worldToTile(x, y);
    expect(tile).toEqual({ tx: 3, ty: 5 });
  });
});
