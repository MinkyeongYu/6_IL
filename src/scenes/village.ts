import Phaser from 'phaser';
import { defineQuery } from 'bitecs';
import { type GameWorld } from '@/ecs/world';
import { createVillageGrid, type VillageGrid } from '@/gameplay/village/grid';
import { PlacementController } from '@/gameplay/village/placement-controller';
import type { ResourceStore } from '@/gameplay/resources/resource-store';
import type { DayNightController } from '@/gameplay/cycle/day-night-controller';
import type { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';
import { InputAdapter } from '@/gameplay/player/input-adapter';
import { VILLAGE_GRID_SIZE, TILE_SIZE } from '@/config/constants';
import { Position } from '@/ecs/components/position';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { PlayerTag } from '@/ecs/components/tags';

const playerQuery = defineQuery([PlayerTag]);

export class VillageScene extends Phaser.Scene {
  private world!: GameWorld;
  private grid!: VillageGrid;
  private resources!: ResourceStore;
  private cycle!: DayNightController;
  private bus!: EventBus<GameEvents>;
  private spriteMap!: Map<number, Phaser.GameObjects.Image>;
  private gidCounter = 10000;
  private placement!: PlacementController;
  private input2!: InputAdapter;
  private playerEid?: number;

  constructor() {
    super({ key: 'Village' });
  }

  private nextGid = (): number => this.gidCounter++;

  init(data: {
    world: GameWorld;
    resources: ResourceStore;
    bus: EventBus<GameEvents>;
    cycle: DayNightController;
  }): void {
    this.world = data.world;
    this.resources = data.resources;
    this.bus = data.bus;
    this.cycle = data.cycle;
    this.spriteMap = new Map();
  }

  create(): void {
    this.cameras.main.setBackgroundColor('#1b2434');
    this.input2 = new InputAdapter(this);

    this.grid = createVillageGrid(VILLAGE_GRID_SIZE, VILLAGE_GRID_SIZE, TILE_SIZE);
    this.drawGrid();

    // 플레이어 스프라이트 재등록 (마을 중앙 근처에 배치)
    const peid = playerQuery(this.world)[0];
    if (peid !== undefined) {
      this.playerEid = peid;
      Position.x[peid] = (VILLAGE_GRID_SIZE * TILE_SIZE) / 2;
      Position.y[peid] = (VILLAGE_GRID_SIZE * TILE_SIZE) / 2;
      const gid = this.nextGid();
      const spr = this.add.image(Position.x[peid], Position.y[peid], 'player');
      this.spriteMap.set(gid, spr);
      SpriteRef.gid[peid] = gid;
    }

    this.placement = new PlacementController(
      this,
      this.world,
      this.grid,
      this.resources,
      this.spriteMap,
      this.nextGid,
    );

    this.input.on('pointermove', (p: Phaser.Input.Pointer) => {
      this.placement.updateCursor(p.worldX, p.worldY);
    });
    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => {
      this.placement.confirmPlace(p.worldX, p.worldY);
    });

    this.input.keyboard?.on('keydown-ONE', () => this.placement.start('barricade'));
    this.input.keyboard?.on('keydown-TWO', () => this.placement.start('campfire'));

    // 새벽 시작 시 Snowfield로 복귀
    this.bus.on('dawn:started', () => {
      this.scene.start('Snowfield', {
        world: this.world,
        resources: this.resources,
        bus: this.bus,
        cycle: this.cycle,
      });
    });
  }

  private drawGrid(): void {
    const g = this.add.graphics({ lineStyle: { width: 1, color: 0x2a3644 } });
    for (let x = 0; x <= VILLAGE_GRID_SIZE; x++) {
      g.lineBetween(x * TILE_SIZE, 0, x * TILE_SIZE, VILLAGE_GRID_SIZE * TILE_SIZE);
    }
    for (let y = 0; y <= VILLAGE_GRID_SIZE; y++) {
      g.lineBetween(0, y * TILE_SIZE, VILLAGE_GRID_SIZE * TILE_SIZE, y * TILE_SIZE);
    }
  }

  override update(_t: number, dtMs: number): void {
    const dt = dtMs / 1000;
    this.world.deltaTime = dt;
    this.cycle.update(dt);

    if (this.input2.justPressed('cancel')) {
      this.placement.cancel();
    }
  }
}
