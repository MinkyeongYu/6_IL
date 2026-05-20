import type Phaser from 'phaser';

type SpriteRef = Phaser.GameObjects.Sprite | Phaser.GameObjects.Rectangle;

export class SpriteRegistry {
  private readonly map = new Map<number, SpriteRef>();

  set(eid: number, sprite: SpriteRef): void {
    this.map.set(eid, sprite);
  }

  get(eid: number): SpriteRef | undefined {
    return this.map.get(eid);
  }

  delete(eid: number): void {
    this.map.delete(eid);
  }

  clear(): void {
    this.map.clear();
  }

  forEach(fn: (sprite: SpriteRef, eid: number) => void): void {
    this.map.forEach(fn);
  }
}
