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
import { VILLAGE_GRID_SIZE, TILE_SIZE, GAME_WIDTH, GAME_HEIGHT } from '@/config/constants';
import { VISION } from '@/config/balance';
import { VisionMask } from '@/gameplay/vision/vision-mask';
import { Position } from '@/ecs/components/position';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { PlayerTag } from '@/ecs/components/tags';
import { Health } from '@/ecs/components/health';
import { createWaveSpawner } from '@/gameplay/enemies/wave-spawner';
import { spawnZombie } from '@/gameplay/enemies/zombie-factory';
import { zombieAiSystem } from '@/gameplay/enemies/zombie-ai';
import { movementSystem } from '@/ecs/systems/movement';
import { spriteSyncSystem } from '@/ecs/systems/sprite-sync';
import { cleanupSystem } from '@/ecs/systems/cleanup';
import { createRng, type Rng } from '@/util/rng';
import { playerAttackSystem } from '@/gameplay/combat/attack-controller';
import { saveGame, clearSave } from '@/gameplay/persistence/save-load';
import { DeathOverlay } from '@/ui/death-overlay';
import { showToast, getTutorialFlags, markSeen } from '@/ui/tutorial';

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
  private rng: Rng = createRng(2026);
  private vision!: VisionMask;
  private playerDied = false;

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
    this.cameras.main.fadeIn(600, 0, 0, 0);
    this.input2 = new InputAdapter(this);

    this.grid = createVillageGrid(VILLAGE_GRID_SIZE, VILLAGE_GRID_SIZE, TILE_SIZE);
    this.drawGrid();

    // 플레이어 스프라이트 재등록 + 마을 중앙 배치
    const peid = playerQuery(this.world)[0];
    if (peid !== undefined) {
      this.playerEid = peid;
      Position.x[peid] = (VILLAGE_GRID_SIZE * TILE_SIZE) / 2;
      Position.y[peid] = (VILLAGE_GRID_SIZE * TILE_SIZE) / 2;
      const gid = this.nextGid();
      const spr = this.add.image(Position.x[peid], Position.y[peid], 'player', 0).setScale(0.48);
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

    this.bus.on('build:request', ({ kind }) => this.placement.start(kind));

    // 첫 밤 튜토리얼
    const flags = getTutorialFlags();
    if (!flags.firstNight) {
      showToast({
        scene: this,
        title: '밤이 다가옵니다',
        body:
          '왼쪽 하단 버튼으로 모닥불(🔥)과 바리게이트(🪵) 설치.\n' +
          '곧 좀비가 외곽에서 몰려옵니다. 새벽까지 살아남으세요.',
        onClose: () => markSeen('firstNight'),
      });
    }

    const isNight = this.cycle.phase === 'night' || this.cycle.phase === 'evening';
    const r = (isNight ? VISION.nightRadiusTiles : VISION.dayRadiusTiles) * TILE_SIZE;
    this.vision = new VisionMask(this, GAME_WIDTH, GAME_HEIGHT, r);

    this.bus.on('night:started', () => {
      this.vision.setRadius(VISION.nightRadiusTiles * TILE_SIZE);
    });
    this.bus.on('dawn:started', () => {
      this.vision.setRadius(VISION.dayRadiusTiles * TILE_SIZE);
    });

    // 밤 시작 시 좀비 웨이브 스폰
    this.bus.on('night:started', ({ day }) => {
      const spawner = createWaveSpawner(createRng(day * 1000));
      const plan = spawner.planWave(day);
      for (let i = 0; i < plan.enemyCount; i++) {
        const pt = plan.spawnPoints[i % plan.spawnPoints.length];
        if (!pt) continue;
        const jitterX = (Math.random() - 0.5) * 40;
        const jitterY = (Math.random() - 0.5) * 40;
        spawnZombie(this.world, this, this.spriteMap, this.nextGid, pt.x + jitterX, pt.y + jitterY);
      }
      this.bus.emit('wave:started', {
        day,
        waveIndex: 0,
        enemyCount: plan.enemyCount,
      });
    });

    // 새벽 시작 시 자동 저장 + 페이드 아웃 후 Snowfield로 복귀
    this.bus.on('dawn:started', () => {
      saveGame({
        version: 1,
        currentDay: this.cycle.day,
        resources: this.resources.snapshot(),
        weatherRng: 42,
      });
      this.cameras.main.fadeOut(800, 0, 0, 0);
      this.cameras.main.once(Phaser.Cameras.Scene2D.Events.FADE_OUT_COMPLETE, () => {
        this.scene.start('Snowfield', {
          world: this.world,
          resources: this.resources,
          bus: this.bus,
          cycle: this.cycle,
        });
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

    if (
      this.playerEid !== undefined &&
      (Health.current[this.playerEid] ?? 0) <= 0 &&
      !this.playerDied
    ) {
      this.playerDied = true;
      this.bus.emit('player:died', { day: this.cycle.day });
      DeathOverlay.show(this, () => {
        clearSave();
        window.location.reload();
      });
    }

    if (this.playerEid !== undefined) {
      playerAttackSystem(this.world, this.playerEid, this.rng);
    }
    zombieAiSystem(this.world);
    movementSystem(this.world);

    const removed = cleanupSystem(this.world);
    for (const eid of removed) {
      const gid = SpriteRef.gid[eid];
      if (gid !== undefined) {
        this.spriteMap.get(gid)?.destroy();
        this.spriteMap.delete(gid);
      }
      this.grid.remove(eid);
    }

    spriteSyncSystem(this.world, this.spriteMap);
    this.cycle.update(dt);

    if (this.input2.justPressed('cancel')) this.placement.cancel();

    if (this.playerEid !== undefined) {
      this.vision.update(Position.x[this.playerEid] ?? 0, Position.y[this.playerEid] ?? 0);
    }
  }
}
