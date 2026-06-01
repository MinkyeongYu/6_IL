const EMPTY = 0;

export interface VillageGrid {
  readonly width: number;
  readonly height: number;
  readonly tileSize: number;
  isFree(tx: number, ty: number, w: number, h: number): boolean;
  place(tx: number, ty: number, w: number, h: number, eid: number): boolean;
  remove(eid: number): void;
  getCell(tx: number, ty: number): number;
  tileToWorld(tx: number, ty: number): { x: number; y: number };
  worldToTile(x: number, y: number): { tx: number; ty: number };
}

export function createVillageGrid(
  width: number,
  height: number,
  tileSize = 32,
): VillageGrid {
  const cells = new Uint32Array(width * height);

  const idx = (tx: number, ty: number): number => ty * width + tx;

  const inBounds = (tx: number, ty: number, w: number, h: number): boolean =>
    tx >= 0 && ty >= 0 && w > 0 && h > 0 && tx + w <= width && ty + h <= height;

  const readCell = (i: number): number => cells[i] ?? EMPTY;

  const isFree = (tx: number, ty: number, w: number, h: number): boolean => {
    if (!inBounds(tx, ty, w, h)) return false;
    for (let y = ty; y < ty + h; y++) {
      for (let x = tx; x < tx + w; x++) {
        if (readCell(idx(x, y)) !== EMPTY) return false;
      }
    }
    return true;
  };

  return {
    width,
    height,
    tileSize,
    isFree,
    place(tx, ty, w, h, eid) {
      if (!isFree(tx, ty, w, h)) return false;
      for (let y = ty; y < ty + h; y++) {
        for (let x = tx; x < tx + w; x++) {
          cells[idx(x, y)] = eid;
        }
      }
      return true;
    },
    remove(eid) {
      for (let i = 0; i < cells.length; i++) {
        if (readCell(i) === eid) cells[i] = EMPTY;
      }
    },
    getCell(tx, ty) {
      if (!inBounds(tx, ty, 1, 1)) return EMPTY;
      return readCell(idx(tx, ty));
    },
    tileToWorld(tx, ty) {
      return { x: tx * tileSize + tileSize / 2, y: ty * tileSize + tileSize / 2 };
    },
    worldToTile(x, y) {
      return { tx: Math.floor(x / tileSize), ty: Math.floor(y / tileSize) };
    },
  };
}
