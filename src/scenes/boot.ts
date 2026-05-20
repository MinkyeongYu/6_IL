import Phaser from 'phaser';

export class BootScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Boot' });
  }

  create(): void {
    const { width, height } = this.scale;
    this.cameras.main.setBackgroundColor('#0c1626');

    this.add
      .text(width / 2, height / 2, '6IL', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '28px',
        color: '#f5f1e8',
        stroke: '#10151c',
        strokeThickness: 5,
      })
      .setOrigin(0.5);

    this.time.delayedCall(500, () => {
      this.scene.start('Preload');
    });
  }
}
