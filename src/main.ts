import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT } from '@/config/constants';
import { BootScene } from '@/scenes/boot';
import { PreloadScene } from '@/scenes/preload';
import { SnowfieldScene } from '@/scenes/snowfield';
import { VillageScene } from '@/scenes/village';
import { HudScene } from '@/scenes/hud';

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  parent: 'game',
  width: GAME_WIDTH,
  height: GAME_HEIGHT,
  backgroundColor: '#0a0a14',
  pixelArt: true,
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH,
  },
  scene: [BootScene, PreloadScene, SnowfieldScene, VillageScene, HudScene],
};

new Phaser.Game(config);
