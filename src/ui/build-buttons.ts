import type Phaser from 'phaser';
import type { ResourceStore } from '@/gameplay/resources/resource-store';
import {
  BUILDING_COSTS,
  type BuildingKind,
} from '@/gameplay/village/building-factory';

const BTN_W = 120;
const BTN_H = 36;
const GAP = 8;

export interface BuildButtonOptions {
  scene: Phaser.Scene;
  x: number;
  y: number;
  resources: ResourceStore;
  onPick: (kind: BuildingKind) => void;
}

interface ButtonRef {
  bg: Phaser.GameObjects.Rectangle;
  label: Phaser.GameObjects.Text;
  kind: BuildingKind;
}

const BUTTONS: Array<{ kind: BuildingKind; label: string }> = [
  { kind: 'campfire', label: '🔥 모닥불' },
  { kind: 'barricade', label: '🪵 바리게이트' },
];

export class BuildButtons {
  private readonly buttons: ButtonRef[] = [];
  private readonly resources: ResourceStore;
  private readonly unsub: () => void;

  constructor(opts: BuildButtonOptions) {
    this.resources = opts.resources;
    BUTTONS.forEach((b, i) => {
      const x = opts.x;
      const y = opts.y + i * (BTN_H + GAP);
      const bg = opts.scene.add
        .rectangle(x, y, BTN_W, BTN_H, 0x2a3644)
        .setStrokeStyle(2, 0x4a90e2)
        .setOrigin(0, 0)
        .setInteractive({ useHandCursor: true });
      const label = opts.scene.add
        .text(x + BTN_W / 2, y + BTN_H / 2, '', {
          fontFamily: 'ui-monospace, monospace',
          fontSize: '12px',
          color: '#e0e6ed',
        })
        .setOrigin(0.5);

      bg.on('pointerdown', () => opts.onPick(b.kind));
      bg.on('pointerover', () => bg.setFillStyle(0x3a4654));
      bg.on('pointerout', () => bg.setFillStyle(0x2a3644));

      this.buttons.push({ bg, label, kind: b.kind });
    });
    this.refresh();
    this.unsub = opts.resources.onChange(() => this.refresh());
  }

  private refresh(): void {
    for (const btn of this.buttons) {
      const cost = BUILDING_COSTS[btn.kind];
      const have = this.resources.get(cost.kind);
      const def = BUTTONS.find((b) => b.kind === btn.kind)!;
      const enough = have >= cost.amount;
      btn.label.setText(`${def.label}\n🪵 ${cost.amount}`);
      btn.label.setColor(enough ? '#e0e6ed' : '#888888');
      btn.bg.setStrokeStyle(2, enough ? 0x4a90e2 : 0x555555);
    }
  }

  destroy(): void {
    this.unsub();
    for (const b of this.buttons) {
      b.bg.destroy();
      b.label.destroy();
    }
  }
}
