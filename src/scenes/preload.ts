import Phaser from 'phaser';

type PaintFn = (ctx: CanvasRenderingContext2D) => void;

type SheetFrame = {
  name: string;
  x: number;
  y: number;
  width: number;
  height: number;
};

function texture(scene: Phaser.Scene, key: string, width: number, height: number, paint: PaintFn): void {
  const canvasTexture = scene.textures.createCanvas(key, width, height);
  if (!canvasTexture) {
    throw new Error(`Unable to create canvas texture: ${key}`);
  }
  const ctx = canvasTexture.getContext();
  ctx.imageSmoothingEnabled = false;
  ctx.clearRect(0, 0, width, height);
  paint(ctx);
  canvasTexture.refresh();
}

function addSheetFrames(scene: Phaser.Scene, key: string, frames: SheetFrame[]): void {
  const sheet = scene.textures.get(key);
  for (const frame of frames) {
    if (!sheet.has(frame.name)) {
      sheet.add(frame.name, 0, frame.x, frame.y, frame.width, frame.height);
    }
  }
}

function rect(ctx: CanvasRenderingContext2D, color: string, x: number, y: number, w: number, h: number): void {
  ctx.fillStyle = color;
  ctx.fillRect(x, y, w, h);
}

function stroke(ctx: CanvasRenderingContext2D, color: string, x: number, y: number, w: number, h: number): void {
  ctx.strokeStyle = color;
  ctx.lineWidth = 2;
  ctx.strokeRect(x + 1, y + 1, w - 2, h - 2);
}

export class PreloadScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Preload' });
  }

  preload(): void {
    this.load.image('snow_tile', 'assets/tilesets/snowfield_base.png');
    this.load.image('props_sheet', 'assets/props/08_props.png');
    this.load.image('props_extra', 'assets/props/08b_props_extras.png');
    this.load.image('props_fence_trees', 'assets/props/08c_props_fence_trees.png');
    this.load.image('barricade_sheet', 'assets/buildings/07_items_barricades.png');
    this.load.image('prop_crate_stack', 'assets/props/40_crate_stack.png');
    this.load.image('fx_footprints', 'assets/fx/39_footprints.png');
    this.load.image('fx_warm_glow', 'assets/fx/41_warm_glow.png');
    this.load.spritesheet('player', 'assets/characters/player_survivor_axe.png', {
      frameWidth: 96,
      frameHeight: 96,
    });
    this.load.spritesheet('player_bow', 'assets/characters/player_survivor_bow.png', {
      frameWidth: 96,
      frameHeight: 96,
    });
    this.load.spritesheet('zombie', 'assets/characters/27_enemy_01_mecha_zombie.png', {
      frameWidth: 96,
      frameHeight: 96,
    });
    this.load.spritesheet('deer', 'assets/characters/24_animal_01_deer_anim.png', {
      frameWidth: 313,
      frameHeight: 313,
    });
  }

  create(): void {
    this.createSheetFrames();
    this.createTiles();
    this.createCharacters();
    this.createAnimations();
    this.createResources();
    this.createVillageProps();
    this.createEffects();

    this.scene.start('Onboarding');
  }

  private createSheetFrames(): void {
    addSheetFrames(this, 'props_sheet', [
      { name: 'pine_tree', x: 31, y: 33, width: 104, height: 185 },
      { name: 'watchtower', x: 193, y: 47, width: 99, height: 178 },
      { name: 'campfire', x: 318, y: 103, width: 105, height: 94 },
      { name: 'snow_fence_horizontal', x: 424, y: 116, width: 149, height: 67 },
      { name: 'cabin', x: 608, y: 54, width: 128, height: 156 },
    ]);
    addSheetFrames(this, 'props_extra', [
      { name: 'short_fence', x: 24, y: 24, width: 78, height: 86 },
      { name: 'snow_rocks', x: 145, y: 45, width: 97, height: 73 },
    ]);
    addSheetFrames(this, 'props_fence_trees', [
      { name: 'fence_vertical', x: 10, y: 15, width: 52, height: 103 },
      { name: 'bare_tree', x: 148, y: 18, width: 104, height: 100 },
      { name: 'snow_bush', x: 296, y: 53, width: 71, height: 53 },
      { name: 'stump', x: 417, y: 41, width: 75, height: 68 },
    ]);
    addSheetFrames(this, 'barricade_sheet', [
      { name: 'wood_barricade', x: 25, y: 28, width: 172, height: 76 },
      { name: 'stone_wall', x: 217, y: 35, width: 166, height: 72 },
      { name: 'spike_barricade', x: 407, y: 20, width: 91, height: 94 },
      { name: 'logs', x: 34, y: 153, width: 78, height: 53 },
      { name: 'small_rocks', x: 139, y: 151, width: 52, height: 47 },
    ]);
  }

  private createTiles(): void {
    texture(this, 'village_tile', 64, 64, (ctx) => {
      rect(ctx, '#b9d0e4', 0, 0, 64, 64);
      rect(ctx, '#c8dceb', 0, 0, 64, 64);
      rect(ctx, '#a8c1d7', 7, 17, 12, 2);
      rect(ctx, '#b1c8db', 24, 28, 11, 2);
      rect(ctx, '#9fb8cf', 44, 48, 10, 2);
      rect(ctx, '#dceaf4', 5, 7, 14, 2);
      rect(ctx, '#dceaf4', 36, 56, 12, 2);
    });

    texture(this, 'snow_patch', 26, 18, (ctx) => {
      rect(ctx, '#d9e9f5', 3, 4, 20, 8);
      rect(ctx, '#a2bfd9', 7, 12, 13, 3);
      rect(ctx, '#edf6fb', 8, 2, 8, 3);
    });

    texture(this, 'footprints', 28, 20, (ctx) => {
      rect(ctx, '#7695b8', 4, 4, 5, 3);
      rect(ctx, '#7695b8', 12, 9, 5, 3);
      rect(ctx, '#7695b8', 20, 14, 5, 3);
      rect(ctx, '#9bb8d4', 4, 7, 5, 1);
      rect(ctx, '#9bb8d4', 12, 12, 5, 1);
      rect(ctx, '#9bb8d4', 20, 17, 5, 1);
    });
  }

  private createCharacters(): void {
    // Character sheets are loaded from public assets. This hook remains for future generated fallbacks.
  }

  private createAnimations(): void {
    this.anims.create({
      key: 'zombie_walk',
      frames: this.anims.generateFrameNumbers('zombie', { start: 4, end: 7 }),
      frameRate: 6,
      repeat: -1,
    });
    this.anims.create({
      key: 'deer_run',
      frames: this.anims.generateFrameNumbers('deer', { start: 8, end: 11 }),
      frameRate: 7,
      repeat: -1,
    });
  }

  private createResources(): void {
    texture(this, 'tree', 52, 62, (ctx) => {
      rect(ctx, '#2d1a13', 23, 40, 8, 18);
      rect(ctx, '#142b35', 8, 31, 35, 13);
      rect(ctx, '#1e4650', 12, 21, 29, 14);
      rect(ctx, '#245965', 16, 11, 21, 15);
      rect(ctx, '#d8e7f2', 9, 30, 16, 5);
      rect(ctx, '#d8e7f2', 17, 20, 13, 4);
      rect(ctx, '#d8e7f2', 19, 10, 11, 4);
      rect(ctx, '#071015', 8, 43, 35, 3);
      stroke(ctx, '#0c171d', 7, 10, 37, 49);
    });

    texture(this, 'rock', 34, 28, (ctx) => {
      rect(ctx, '#263647', 5, 14, 24, 9);
      rect(ctx, '#4d6477', 8, 8, 17, 10);
      rect(ctx, '#7891a6', 12, 6, 10, 5);
      rect(ctx, '#c9d9e7', 10, 7, 8, 3);
      rect(ctx, '#172330', 5, 22, 24, 3);
      stroke(ctx, '#111923', 4, 6, 26, 19);
    });

    texture(this, 'stump', 28, 24, (ctx) => {
      rect(ctx, '#3a2217', 8, 8, 12, 12);
      rect(ctx, '#71472b', 6, 5, 16, 8);
      rect(ctx, '#c18a55', 9, 7, 10, 3);
      stroke(ctx, '#1a0f0b', 5, 5, 18, 16);
    });

    texture(this, 'small_rock', 24, 18, (ctx) => {
      rect(ctx, '#35495c', 4, 8, 16, 6);
      rect(ctx, '#6f879d', 8, 5, 10, 5);
      rect(ctx, '#152230', 4, 14, 16, 2);
    });
  }

  private createVillageProps(): void {
    texture(this, 'bonfire', 64, 64, (ctx) => {
      rect(ctx, '#2b1a13', 18, 45, 30, 7);
      rect(ctx, '#5a341f', 14, 48, 14, 5);
      rect(ctx, '#5a341f', 36, 48, 14, 5);
      rect(ctx, '#7a2d16', 25, 27, 14, 20);
      rect(ctx, '#ff6b20', 22, 24, 18, 24);
      rect(ctx, '#ffd05a', 28, 21, 10, 22);
      rect(ctx, '#fff0a0', 31, 28, 4, 11);
      stroke(ctx, '#1b100c', 13, 20, 38, 34);
    });

    texture(this, 'barricade', 34, 42, (ctx) => {
      for (let x = 4; x <= 24; x += 7) {
        rect(ctx, '#2b1a12', x, 8, 5, 28);
        rect(ctx, '#795137', x + 1, 6, 3, 29);
        rect(ctx, '#d9e9f5', x, 6, 5, 4);
      }
      rect(ctx, '#25150f', 2, 19, 30, 5);
      rect(ctx, '#7a5137', 2, 17, 30, 4);
      rect(ctx, '#25150f', 2, 30, 30, 5);
      rect(ctx, '#7a5137', 2, 28, 30, 4);
      stroke(ctx, '#120a07', 1, 5, 32, 32);
    });

    texture(this, 'cabin', 92, 82, (ctx) => {
      rect(ctx, '#1b120e', 14, 34, 64, 38);
      rect(ctx, '#6a4029', 18, 38, 56, 32);
      rect(ctx, '#3c2418', 18, 48, 56, 4);
      rect(ctx, '#3c2418', 18, 60, 56, 4);
      rect(ctx, '#14202a', 36, 53, 18, 18);
      rect(ctx, '#ffd47a', 61, 46, 9, 9);
      rect(ctx, '#233443', 8, 27, 76, 14);
      rect(ctx, '#d6e7f2', 11, 20, 70, 15);
      rect(ctx, '#94b4cf', 18, 16, 58, 6);
      stroke(ctx, '#0d0b0a', 8, 15, 76, 58);
    });

    texture(this, 'crate_stack', 44, 40, (ctx) => {
      rect(ctx, '#2a1a12', 4, 20, 18, 16);
      rect(ctx, '#7a5236', 6, 22, 14, 12);
      rect(ctx, '#2a1a12', 20, 14, 20, 21);
      rect(ctx, '#8b6040', 22, 16, 16, 17);
      rect(ctx, '#3b2518', 6, 27, 14, 3);
      rect(ctx, '#3b2518', 22, 24, 16, 3);
    });

    texture(this, 'log_stack', 50, 32, (ctx) => {
      rect(ctx, '#29160f', 5, 15, 36, 8);
      rect(ctx, '#8b5c38', 7, 13, 34, 5);
      rect(ctx, '#29160f', 12, 21, 36, 8);
      rect(ctx, '#9a6840', 14, 19, 32, 6);
      rect(ctx, '#d7a36b', 8, 14, 4, 4);
      rect(ctx, '#d7a36b', 40, 20, 4, 4);
    });
  }

  private createEffects(): void {
    texture(this, 'warm_glow', 160, 160, (ctx) => {
      const gradient = ctx.createRadialGradient(80, 80, 10, 80, 80, 80);
      gradient.addColorStop(0, 'rgba(255, 194, 84, 0.72)');
      gradient.addColorStop(0.45, 'rgba(255, 113, 39, 0.26)');
      gradient.addColorStop(1, 'rgba(255, 113, 39, 0)');
      ctx.fillStyle = gradient;
      ctx.fillRect(0, 0, 160, 160);
    });
  }
}
