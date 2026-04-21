import type Phaser from 'phaser';
import type { ResourceStore } from '@/gameplay/resources/resource-store';

export class ResourcePanel {
  private readonly text: Phaser.GameObjects.Text;
  private readonly unsub: () => void;

  constructor(
    scene: Phaser.Scene,
    x: number,
    y: number,
    private readonly store: ResourceStore,
  ) {
    this.text = scene.add.text(x, y, '', {
      fontFamily: 'ui-monospace, monospace',
      fontSize: '14px',
      color: '#e0e6ed',
    });
    this.refresh();
    this.unsub = store.onChange(() => this.refresh());
  }

  private refresh(): void {
    const s = this.store.snapshot();
    this.text.setText(
      `Wood ${s.wood}  Stone ${s.stone}  Iron ${s.iron}  Meat ${s.meat}  Food ${s.food}`,
    );
  }

  destroy(): void {
    this.unsub();
    this.text.destroy();
  }
}
