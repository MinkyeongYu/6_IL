import Phaser from 'phaser';
import { generatePlaceholders, PLACEHOLDER_SPECS } from '@/util/placeholder-textures';

export class PreloadScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Preload' });
  }

  preload(): void {
    this.load.spritesheet('player', 'assets/characters/player_survivor_axe_onehand.png', {
      frameWidth: 96,
      frameHeight: 96,
    });
    this.load.spritesheet('player_unarmed', 'assets/characters/player_survivor_unarmed.png', {
      frameWidth: 96,
      frameHeight: 96,
    });
    this.load.spritesheet('player_axe_big', 'assets/characters/player_survivor_axe.png', {
      frameWidth: 96,
      frameHeight: 96,
    });
    this.load.spritesheet('player_bow', 'assets/characters/player_survivor_bow.png', {
      frameWidth: 96,
      frameHeight: 96,
    });
  }

  create(): void {
    generatePlaceholders(this, PLACEHOLDER_SPECS);
    this.scene.start('Snowfield');
  }
}
