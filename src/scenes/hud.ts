import Phaser from 'phaser';
import type { GameWorld } from '@/ecs/world';
import type { ResourceStore } from '@/gameplay/resources/resource-store';
import type { DayNightController } from '@/gameplay/cycle/day-night-controller';
import type { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';
import { ResourcePanel } from '@/ui/resource-panel';
import { HealthBar } from '@/ui/health-bar';
import { PhaseBanner } from '@/ui/phase-banner';

export class HudScene extends Phaser.Scene {
  private resources!: ResourceStore;
  private bus!: EventBus<GameEvents>;
  private cycle!: DayNightController;
  private playerEid!: number;
  private world!: GameWorld;
  private healthBar?: HealthBar;

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
    new PhaseBanner(this, Number(this.scale.width) / 2, 10, this.bus);
  }

  override update(): void {
    this.healthBar?.update();
  }
}
