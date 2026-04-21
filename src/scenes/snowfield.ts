import Phaser from 'phaser';
import { defineQuery, hasComponent as bitHas } from 'bitecs';
import { createGameWorld, removeEntity, type GameWorld } from '@/ecs/world';
import { spawnPlayer } from '@/gameplay/player/player-factory';
import { InputAdapter } from '@/gameplay/player/input-adapter';
import { updatePlayerInput } from '@/gameplay/player/player-controller';
import { spawnTree } from '@/gameplay/gather/tree-factory';
import { spawnStone } from '@/gameplay/gather/stone-factory';
import { spawnDeer } from '@/gameplay/animals/deer-factory';
import { deerAiSystem } from '@/gameplay/animals/deer-ai';
import { GatherController } from '@/gameplay/gather/gather-controller';
import { movementSystem } from '@/ecs/systems/movement';
import { spriteSyncSystem } from '@/ecs/systems/sprite-sync';
import { cleanupSystem } from '@/ecs/systems/cleanup';
import { createResourceStore, type ResourceStore } from '@/gameplay/resources/resource-store';
import {
  createDayNightController,
  type DayNightController,
} from '@/gameplay/cycle/day-night-controller';
import { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';
import { DAY_CYCLE, VISION } from '@/config/balance';
import { GAME_WIDTH, GAME_HEIGHT, TILE_SIZE } from '@/config/constants';
import { VisionMask } from '@/gameplay/vision/vision-mask';
import { loadGame } from '@/gameplay/persistence/save-load';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { Position } from '@/ecs/components/position';
import { PlayerTag, TreeTag, StoneTag, DeerTag } from '@/ecs/components/tags';

const playerQuery = defineQuery([PlayerTag]);
const allPositionedQuery = defineQuery([Position]);

export class SnowfieldScene extends Phaser.Scene {
  private world!: GameWorld;
  private input2!: InputAdapter;
  private playerEid!: number;
  private spriteMap!: Map<number, Phaser.GameObjects.Image>;
  private gidCounter = 1;
  private resources!: ResourceStore;
  private gather!: GatherController;
  private bus!: EventBus<GameEvents>;
  private cycle!: DayNightController;
  private vision!: VisionMask;

  constructor() {
    super({ key: 'Snowfield' });
  }

  private nextGid = (): number => this.gidCounter++;

  private clearSnowfieldContent(): void {
    const ents = allPositionedQuery(this.world);
    for (const eid of ents) {
      if (
        bitHas(this.world, TreeTag, eid) ||
        bitHas(this.world, StoneTag, eid) ||
        bitHas(this.world, DeerTag, eid)
      ) {
        const gid = SpriteRef.gid[eid];
        if (gid !== undefined) {
          this.spriteMap.get(gid)?.destroy();
          this.spriteMap.delete(gid);
        }
        removeEntity(this.world, eid);
      }
    }
  }

  init(data: {
    world?: GameWorld;
    resources?: ResourceStore;
    bus?: EventBus<GameEvents>;
    cycle?: DayNightController;
  }): void {
    this.world = data.world ?? createGameWorld();
    this.resources = data.resources ?? createResourceStore();
    this.bus = data.bus ?? new EventBus<GameEvents>();
    this.cycle = data.cycle ?? createDayNightController(DAY_CYCLE, this.bus);

    // 첫 진입 시(전환에서 cycle 전달 안 됨) 세이브 복원
    if (!data.cycle) {
      const save = loadGame();
      if (save) {
        this.resources.restore(save.resources);
        this.cycle.restore({ day: save.currentDay, phase: 'day', elapsedInPhase: 0 });
      }
    }

    this.spriteMap = new Map();
  }

  create(): void {
    this.cameras.main.setBackgroundColor('#9bb4c8');
    this.input2 = new InputAdapter(this);

    // 플레이어가 이미 존재하면(씬 재진입) 엔티티 재사용, 스프라이트만 새로 등록
    const existingPlayerEid = playerQuery(this.world)[0];
    if (existingPlayerEid !== undefined) {
      this.playerEid = existingPlayerEid;
      const sprite = this.add.image(
        Position.x[existingPlayerEid] ?? 0,
        Position.y[existingPlayerEid] ?? 0,
        'player',
      );
      const gid = this.nextGid();
      this.spriteMap.set(gid, sprite);
      SpriteRef.gid[existingPlayerEid] = gid;
    } else {
      const { eid } = spawnPlayer(this.world, this, this.spriteMap, this.nextGid, 480, 270);
      this.playerEid = eid;
    }

    // 씬 재진입 시 자원/동물 초기화
    this.clearSnowfieldContent();

    // 근교 자원·동물 배치
    for (let i = 0; i < 6; i++) {
      spawnTree(this.world, this, this.spriteMap, this.nextGid, 100 + i * 70, 100);
      spawnTree(this.world, this, this.spriteMap, this.nextGid, 120 + i * 70, 450);
    }
    for (let i = 0; i < 3; i++) {
      spawnStone(this.world, this, this.spriteMap, this.nextGid, 150 + i * 120, 250);
    }
    for (let i = 0; i < 2; i++) {
      spawnDeer(this.world, this, this.spriteMap, this.nextGid, 700, 150 + i * 200);
    }

    this.gather = new GatherController(this.world, this.resources);

    // HUD 신 기동
    this.scene.launch('Hud', {
      resources: this.resources,
      bus: this.bus,
      cycle: this.cycle,
      playerEid: this.playerEid,
      world: this.world,
    });

    this.vision = new VisionMask(this, GAME_WIDTH, GAME_HEIGHT, VISION.dayRadiusTiles * TILE_SIZE);

    // 저녁 시작 시 Village 신으로 전환
    this.bus.on('evening:started', () => {
      this.scene.start('Village', {
        world: this.world,
        resources: this.resources,
        bus: this.bus,
        cycle: this.cycle,
      });
    });
  }

  override update(_t: number, dtMs: number): void {
    const dt = dtMs / 1000;
    this.world.deltaTime = dt;
    this.world.elapsed += dt;

    updatePlayerInput(this.playerEid, this.input2);
    if (this.input2.justPressed('interact')) {
      this.gather.tryStart(this.playerEid);
    }

    const px = Position.x[this.playerEid] ?? 0;
    const py = Position.y[this.playerEid] ?? 0;
    deerAiSystem(this.world, px, py);

    movementSystem(this.world);
    this.gather.update(dt);

    const removed = cleanupSystem(this.world);
    for (const eid of removed) {
      const gid = SpriteRef.gid[eid];
      if (gid !== undefined) {
        this.spriteMap.get(gid)?.destroy();
        this.spriteMap.delete(gid);
      }
    }

    spriteSyncSystem(this.world, this.spriteMap);

    this.cycle.update(dt);

    this.vision.update(Position.x[this.playerEid] ?? 0, Position.y[this.playerEid] ?? 0);
  }
}
