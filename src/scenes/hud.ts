import Phaser from 'phaser';
import type { GameWorld } from '@/ecs/world';
import type { ResourceStore } from '@/gameplay/resources/resource-store';
import type { DayNightController } from '@/gameplay/cycle/day-night-controller';
import type { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';
import { ResourcePanel } from '@/ui/resource-panel';
import { HealthBar } from '@/ui/health-bar';
import { PhaseBanner } from '@/ui/phase-banner';
import { WeaponSlot } from '@/ui/weapon-slot';
import { VillageArrow } from '@/ui/village-arrow';
import { FloatingHpBars } from '@/ui/floating-hp-bars';
import { BuildButtons } from '@/ui/build-buttons';
import { GAME_WIDTH, GAME_HEIGHT } from '@/config/constants';
import type { BuildingKind } from '@/gameplay/village/building-factory';

export class HudScene extends Phaser.Scene {
  private resources!: ResourceStore;
  private bus!: EventBus<GameEvents>;
  private cycle!: DayNightController;
  private playerEid!: number;
  private world!: GameWorld;
  private healthBar?: HealthBar;
  private weaponSlot?: WeaponSlot;
  private villageArrow?: VillageArrow;
  private floatingHpBars?: FloatingHpBars;
  private buildButtons?: BuildButtons;

  constructor() {
    super({ key: 'Hud' });
  }

  init(data: {
    resources: ResourceStore;
    bus: EventBus<GameEvents>;
    cycle: DayNightController;
    playerEid: number;
    world: GameWorld;
  }): void {
    this.resources = data.resources;
    this.bus = data.bus;
    this.cycle = data.cycle;
    this.playerEid = data.playerEid;
    this.world = data.world;
  }

  create(): void {
    new ResourcePanel(this, 10, 10, this.resources);
    this.healthBar = new HealthBar(this, 10, 30, this.playerEid);
    new PhaseBanner(this, GAME_WIDTH / 2, 10, this.bus);

    // 우하단 무기 슬롯
    this.weaponSlot = new WeaponSlot(this, GAME_WIDTH - 50, GAME_HEIGHT - 50, this.playerEid);

    // 화면 중앙 기준 마을 화살표
    this.villageArrow = new VillageArrow(this, this.playerEid);

    // 플로팅 HP 바 (Snowfield/Village 시점에 맞춰 표시)
    this.floatingHpBars = new FloatingHpBars(this, this.world);

    // 좌하단 건설 버튼 (저녁/밤에만 활성, 항상 표시)
    this.buildButtons = new BuildButtons({
      scene: this,
      x: 10,
      y: GAME_HEIGHT - 90,
      resources: this.resources,
      onPick: (kind: BuildingKind) => {
        this.bus.emit('build:request', { kind });
      },
    });
  }

  override update(): void {
    this.healthBar?.update();
    this.weaponSlot?.update();
    this.villageArrow?.update();
    this.floatingHpBars?.update();
  }
}
