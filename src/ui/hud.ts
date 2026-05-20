import Phaser from 'phaser';
import type { ResourceManager } from '@/gameplay/resource-manager';
import type { DayNightCycle } from '@/gameplay/day-night-cycle';
import { Health } from '@/ecs/components';

const PHASE_NAMES: Record<string, string> = {
  day: 'Day',
  evening: 'Dusk',
  night: 'Night',
  dawn: 'Dawn',
};

export class HUD {
  private readonly chrome: Phaser.GameObjects.Graphics;
  private readonly resText: Phaser.GameObjects.Text;
  private readonly dayText: Phaser.GameObjects.Text;
  private readonly waveText: Phaser.GameObjects.Text;
  private readonly bars: Phaser.GameObjects.Graphics;

  constructor(
    private readonly scene: Phaser.Scene,
    private readonly resources: ResourceManager,
    private readonly cycle: DayNightCycle,
    private readonly playerEid: number,
  ) {
    this.chrome = scene.add.graphics().setScrollFactor(0).setDepth(999);
    this.bars = scene.add.graphics().setScrollFactor(0).setDepth(1000);

    this.resText = scene.add
      .text(18, 48, '', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '16px',
        color: '#f5f1e8',
        stroke: '#10151c',
        strokeThickness: 4,
      })
      .setScrollFactor(0)
      .setDepth(1001);

    this.dayText = scene.add
      .text(scene.scale.width - 18, 14, '', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '17px',
        color: '#f5f1e8',
        stroke: '#10151c',
        strokeThickness: 4,
      })
      .setOrigin(1, 0)
      .setScrollFactor(0)
      .setDepth(1001);

    this.waveText = scene.add
      .text(scene.scale.width - 18, 45, '', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '14px',
        color: '#ffb3a7',
        stroke: '#10151c',
        strokeThickness: 4,
      })
      .setOrigin(1, 0)
      .setScrollFactor(0)
      .setDepth(1001);
  }

  setWaveText(text: string): void {
    this.waveText.setText(text);
  }

  update(): void {
    const res = this.resources.getAll();
    this.resText.setText(
      `Wood ${res.wood}   Meat ${res.meat}\nStone ${res.stone}  Food ${res.food}`,
    );

    const phaseName = PHASE_NAMES[this.cycle.phase] ?? this.cycle.phase;
    const remaining = Math.ceil(this.cycle.remainingSec);
    this.dayText.setText(`DAY ${this.cycle.day}  ${phaseName}  ${remaining}s`);

    const hp = Health.current[this.playerEid] ?? 0;
    const maxHp = Health.max[this.playerEid] ?? 1;
    const ratio = Math.max(0, hp / maxHp);
    const barX = 46;
    const barY = 15;
    const barW = 132;
    const barH = 12;

    this.chrome.clear();
    this.chrome.fillStyle(0x17191f, 0.9);
    this.chrome.fillRoundedRect(10, 10, 230, 82, 4);
    this.chrome.lineStyle(2, 0x6f4e3a, 1);
    this.chrome.strokeRoundedRect(10, 10, 230, 82, 4);
    this.chrome.fillStyle(0x17191f, 0.9);
    this.chrome.fillRoundedRect(this.scene.scale.width - 252, 10, 242, 68, 4);
    this.chrome.strokeRoundedRect(this.scene.scale.width - 252, 10, 242, 68, 4);

    this.bars.clear();
    this.drawMeter(barX, barY, barW, barH, ratio, 0xc94235);
    this.drawMeter(barX, barY + 17, barW, barH, 0.78, 0x46b9d2);

    this.bars.fillStyle(0xc94235);
    this.bars.fillRect(22, 14, 14, 14);
    this.bars.fillStyle(0x46b9d2);
    this.bars.fillRect(22, 31, 14, 14);
    this.bars.fillStyle(0x8a5b38);
    this.bars.fillRect(22, 52, 12, 10);
    this.bars.fillStyle(0xd06636);
    this.bars.fillRect(106, 52, 12, 10);
    this.bars.fillStyle(0x6b7487);
    this.bars.fillRect(22, 71, 12, 8);
    this.bars.fillStyle(0x8ac064);
    this.bars.fillRect(106, 71, 12, 8);
  }

  private drawMeter(
    x: number,
    y: number,
    width: number,
    height: number,
    ratio: number,
    color: number,
  ): void {
    this.bars.fillStyle(0x3a2b28);
    this.bars.fillRect(x, y, width, height);
    this.bars.fillStyle(color);
    this.bars.fillRect(x, y, width * ratio, height);
    this.bars.lineStyle(2, 0x0f1117, 1);
    this.bars.strokeRect(x, y, width, height);
  }
}
