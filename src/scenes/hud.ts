import Phaser from 'phaser';

export class HudScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Hud' });
  }
  create(): void {
    this.add.text(10, 10, 'HUD (WIP — Task 15)', { color: '#fff' });
  }
}
