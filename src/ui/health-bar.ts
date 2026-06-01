import type Phaser from 'phaser';
import { Health } from '@/ecs/components/health';

const WIDTH = 200;
const HEIGHT = 14;

export class HealthBar {
  private readonly bg: Phaser.GameObjects.Rectangle;
  private readonly fg: Phaser.GameObjects.Rectangle;

  constructor(
    scene: Phaser.Scene,
    x: number,
    y: number,
    private readonly eid: number,
  ) {
    this.bg = scene.add.rectangle(x, y, WIDTH, HEIGHT, 0x333333).setOrigin(0, 0);
    this.fg = scene.add.rectangle(x, y, WIDTH, HEIGHT, 0xe03a3a).setOrigin(0, 0);
  }

  update(): void {
    const cur = Health.current[this.eid] ?? 0;
    const max = Health.max[this.eid] ?? 1;
    const pct = Math.max(0, Math.min(1, cur / max));
    this.fg.width = WIDTH * pct;
  }

  destroy(): void {
    this.bg.destroy();
    this.fg.destroy();
  }
}
