import Phaser from 'phaser';

export class SnowfieldScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Snowfield' });
  }
  create(): void {
    this.cameras.main.setBackgroundColor('#9bb4c8');
    this.add.text(100, 100, 'Snowfield (WIP — Task 13)', { color: '#fff' });
  }
}
