import type Phaser from 'phaser';
import type { EventBus } from '@/events/event-bus';
import type { GameEvents } from '@/events/types';

export class PhaseBanner {
  private readonly text: Phaser.GameObjects.Text;

  constructor(scene: Phaser.Scene, x: number, y: number, bus: EventBus<GameEvents>) {
    this.text = scene.add
      .text(x, y, 'Day 1 — 낮', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '16px',
        color: '#ffd080',
      })
      .setOrigin(0.5, 0);

    bus.on('day:started', (p) => this.text.setText(`Day ${p.day} — 낮`));
    bus.on('evening:started', (p) => this.text.setText(`Day ${p.day} — 저녁 (30초 건설)`));
    bus.on('night:started', (p) => this.text.setText(`Day ${p.day} — 밤 (생존)`));
    bus.on('dawn:started', (p) => this.text.setText(`Day ${p.day} — 새벽`));
  }
}
