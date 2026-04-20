import Phaser from 'phaser';

export class BootScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Boot' });
  }

  create(): void {
    const { width, height } = this.scale;
    this.cameras.main.setBackgroundColor('#0c1626');

    this.add
      .text(width / 2, height / 2, '6_IL — boot', {
        fontFamily: 'ui-monospace, monospace',
        fontSize: '20px',
        color: '#d0d8e0',
      })
      .setOrigin(0.5);
  }
}
