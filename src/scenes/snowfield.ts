import Phaser from 'phaser';
import { defineQuery, hasComponent as bitHas } from 'bitecs';
import { createGameWorld, removeEntity, type GameWorld } from '@/ecs/world';
import { spawnPlayer } from '@/gameplay/player/player-factory';
import { InputAdapter } from '@/gameplay/player/input-adapter';
import { updatePlayerInput } from '@/gameplay/player/player-controller';
import { spawnTree } from '@/gameplay/gather/tree-factory';
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
import { loadGame, clearSave } from '@/gameplay/persistence/save-load';
import { DeathOverlay } from '@/ui/death-overlay';
import { Health } from '@/ecs/components/health';
import { ChunkManager } from '@/gameplay/world/chunk-manager';
import { showOpening, showToast, getTutorialFlags, markSeen } from '@/ui/tutorial';
import { SpriteRef } from '@/ecs/components/sprite-ref';
import { Position } from '@/ecs/components/position';
import { PlayerTag, TreeTag, DeerTag } from '@/ecs/components/tags';

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
  private playerDied = false;
  private chunks!: ChunkManager;

  constructor() {
    super({ key: 'Snowfield' });
  }

  private nextGid = (): number => this.gidCounter++;

  private clearSnowfieldContent(): void {
    const ents = allPositionedQuery(this.world);
    for (const eid of ents) {
      if (bitHas(this.world, TreeTag, eid) || bitHas(this.world, DeerTag, eid)) {
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
    this.cameras.main.fadeIn(600, 0, 0, 0);
    this.input2 = new InputAdapter(this);

    // 플레이어가 이미 존재하면(씬 재진입) 엔티티 재사용, 스프라이트만 새로 등록
    const existingPlayerEid = playerQuery(this.world)[0];
    let playerSprite: Phaser.GameObjects.Image;
    if (existingPlayerEid !== undefined) {
      this.playerEid = existingPlayerEid;
      playerSprite = this.add.image(
        Position.x[existingPlayerEid] ?? 0,
        Position.y[existingPlayerEid] ?? 0,
        'player',
      );
      const gid = this.nextGid();
      this.spriteMap.set(gid, playerSprite);
      SpriteRef.gid[existingPlayerEid] = gid;
    } else {
      const { eid, sprite } = spawnPlayer(this.world, this, this.spriteMap, this.nextGid, 480, 270);
      this.playerEid = eid;
      playerSprite = sprite;
    }

    // 카메라가 플레이어 추종
    this.cameras.main.startFollow(playerSprite, true, 0.15, 0.15);

    // 씬 재진입 시 자원/동물 초기화
    this.clearSnowfieldContent();

    // 근교 시작 배치 (홈 지역; 멀리 가면 청크가 자동 생성)
    for (let i = 0; i < 6; i++) {
      spawnTree(this.world, this, this.spriteMap, this.nextGid, 100 + i * 70, 100);
      spawnTree(this.world, this, this.spriteMap, this.nextGid, 120 + i * 70, 450);
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

    this.chunks = new ChunkManager(this.world, this, this.spriteMap, this.nextGid);

    // 튜토리얼: 첫 진입 시 오프닝 + 첫 채집 안내
    const flags = getTutorialFlags();
    if (!flags.opening) {
      showOpening({
        scene: this,
        onComplete: () => {
          markSeen('opening');
          if (!flags.firstGather) {
            showToast({
              scene: this,
              title: '첫 채집',
              body:
                'WASD/방향키로 이동. 초록 나무 가까이 가서 E(또는 Space)로 벌목.\n' +
                '갈색 사슴 가까이서 E를 누르면 사냥. 자원은 좌상단에 표시됩니다.',
              onClose: () => markSeen('firstGather'),
            });
          }
        },
      });
    } else if (!flags.firstGather) {
      showToast({
        scene: this,
        title: '첫 채집',
        body: 'WASD로 이동. 나무/사슴 가까이서 E 키로 채집.',
        onClose: () => markSeen('firstGather'),
      });
    }

    // 저녁 시작 시 페이드 아웃 후 Village 신으로 전환
    this.bus.on('evening:started', () => {
      this.cameras.main.fadeOut(800, 0, 0, 0);
      this.cameras.main.once(Phaser.Cameras.Scene2D.Events.FADE_OUT_COMPLETE, () => {
        this.scene.start('Village', {
          world: this.world,
          resources: this.resources,
          bus: this.bus,
          cycle: this.cycle,
        });
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

    this.chunks.ensureLoaded(px, py);
    this.chunks.unloadFar(px, py);

    this.vision.update(px, py);

    if ((Health.current[this.playerEid] ?? 0) <= 0 && !this.playerDied) {
      this.playerDied = true;
      this.bus.emit('player:died', { day: this.cycle.day });
      DeathOverlay.show(this, () => {
        clearSave();
        window.location.reload();
      });
    }
  }
}
