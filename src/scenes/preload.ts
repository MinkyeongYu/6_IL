import Phaser from 'phaser';

type PaintFn = (ctx: CanvasRenderingContext2D) => void;

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

  create(): void {
    this.createTiles();
    this.createCharacters();
    this.createResources();
    this.createVillageProps();
    this.createEffects();

    this.scene.start('Game');
  }

  private createTiles(): void {
    texture(this, 'snow_tile', 64, 64, (ctx) => {
      rect(ctx, '#b8d1e8', 0, 0, 64, 64);
      rect(ctx, '#c6dcf0', 0, 0, 64, 64);
      rect(ctx, '#aac5df', 9, 42, 7, 2);
      rect(ctx, '#8fadca', 18, 45, 4, 2);
      rect(ctx, '#dcebf6', 35, 12, 12, 3);
      rect(ctx, '#9fbed9', 45, 50, 8, 2);
      rect(ctx, '#82a3c4', 58, 23, 2, 2);
      rect(ctx, '#e2f0f8', 4, 59, 13, 2);
      rect(ctx, '#b2cce4', 27, 31, 3, 2);
      rect(ctx, '#d5e7f4', 50, 5, 5, 2);
    });

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
    texture(this, 'player', 32, 36, (ctx) => {
      rect(ctx, '#151016', 12, 4, 10, 6);
      rect(ctx, '#b8442f', 9, 5, 14, 12);
      rect(ctx, '#f1b58b', 12, 12, 9, 8);
      rect(ctx, '#21191a', 10, 20, 13, 12);
      rect(ctx, '#6e4d36', 8, 19, 17, 6);
      rect(ctx, '#c8d3dd', 23, 13, 7, 3);
      rect(ctx, '#784b34', 7, 31, 7, 4);
      rect(ctx, '#784b34', 18, 31, 7, 4);
      rect(ctx, '#f2d2a8', 14, 15, 3, 2);
      stroke(ctx, '#120e12', 7, 4, 19, 31);
    });

    texture(this, 'zombie', 30, 34, (ctx) => {
      rect(ctx, '#111820', 8, 5, 15, 8);
      rect(ctx, '#303946', 7, 12, 17, 14);
      rect(ctx, '#1d252f', 5, 15, 5, 12);
      rect(ctx, '#1d252f', 22, 15, 5, 12);
      rect(ctx, '#2a323d', 9, 26, 5, 7);
      rect(ctx, '#2a323d', 18, 26, 5, 7);
      rect(ctx, '#e23a31', 13, 9, 4, 4);
      rect(ctx, '#ff8275', 14, 10, 2, 1);
      stroke(ctx, '#090d12', 5, 5, 22, 28);
    });

    texture(this, 'deer', 36, 28, (ctx) => {
      rect(ctx, '#3a271f', 5, 11, 22, 10);
      rect(ctx, '#2b1d18', 25, 8, 7, 7);
      rect(ctx, '#c7b18d', 29, 6, 2, 5);
      rect(ctx, '#241713', 6, 20, 4, 7);
      rect(ctx, '#241713', 21, 20, 4, 7);
      rect(ctx, '#6f4b34', 3, 12, 5, 5);
      rect(ctx, '#d8c6a0', 32, 11, 2, 2);
      stroke(ctx, '#17100d', 3, 7, 31, 20);
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
