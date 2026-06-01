import type Phaser from 'phaser';
import type { VillageGrid } from './grid';
import type { ResourceStore } from '@/gameplay/resources/resource-store';
import {
  BUILDING_COSTS,
  BUILDING_FOOTPRINTS,
  spawnBuilding,
  type BuildingKind,
} from './building-factory';
import type { GameWorld } from '@/ecs/world';

export class PlacementController {
  private currentKind: BuildingKind | null = null;
  private cursor: Phaser.GameObjects.Rectangle | null = null;

  constructor(
    private readonly scene: Phaser.Scene,
    private readonly world: GameWorld,
    private readonly grid: VillageGrid,
    private readonly resources: ResourceStore,
    private readonly spriteMap: Map<number, Phaser.GameObjects.Image>,
    private readonly nextGid: () => number,
  ) {}

  start(kind: BuildingKind): void {
    this.cancel();
    this.currentKind = kind;
    const spec = BUILDING_FOOTPRINTS[kind];
    this.cursor = this.scene.add
      .rectangle(0, 0, spec.w * this.grid.tileSize, spec.h * this.grid.tileSize, 0x4a90e2, 0.3)
      .setOrigin(0, 0);
  }

  cancel(): void {
    this.currentKind = null;
    this.cursor?.destroy();
    this.cursor = null;
  }

  updateCursor(worldX: number, worldY: number): void {
    if (!this.currentKind || !this.cursor) return;
    const { tx, ty } = this.grid.worldToTile(worldX, worldY);
    this.cursor.x = tx * this.grid.tileSize;
    this.cursor.y = ty * this.grid.tileSize;
  }

  /** 현재 커서 위치에 건설 시도. 성공 여부 반환. */
  confirmPlace(worldX: number, worldY: number): boolean {
    if (!this.currentKind) return false;
    const spec = BUILDING_FOOTPRINTS[this.currentKind];
    const cost = BUILDING_COSTS[this.currentKind];
    const { tx, ty } = this.grid.worldToTile(worldX, worldY);

    if (!this.grid.isFree(tx, ty, spec.w, spec.h)) return false;
    if (this.resources.get(cost.kind) < cost.amount) return false;

    this.resources.spend(cost.kind, cost.amount);
    const centerX = tx * this.grid.tileSize + (spec.w * this.grid.tileSize) / 2;
    const centerY = ty * this.grid.tileSize + (spec.h * this.grid.tileSize) / 2;
    const eid = spawnBuilding(
      this.world,
      this.scene,
      this.spriteMap,
      this.nextGid,
      this.currentKind,
      centerX,
      centerY,
    );
    this.grid.place(tx, ty, spec.w, spec.h, eid);
    return true;
  }

  get isActive(): boolean {
    return this.currentKind !== null;
  }
}
