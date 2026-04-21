import Phaser from 'phaser';

export class VillageScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Village' });
  }
  create(): void {
    this.cameras.main.setBackgroundColor('#1b2434');
    this.add.text(100, 100, 'Village (WIP — Task 14)', { color: '#fff' });
  }
}
