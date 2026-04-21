import Phaser from 'phaser';
import { generatePlaceholders, PLACEHOLDER_SPECS } from '@/util/placeholder-textures';

export class PreloadScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Preload' });
  }

  create(): void {
    generatePlaceholders(this, PLACEHOLDER_SPECS);
    this.scene.start('Snowfield');
  }
}
